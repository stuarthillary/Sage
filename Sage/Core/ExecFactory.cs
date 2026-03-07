#nullable disable
/* This source code licensed under the GNU Affero General Public License */
//Comment this out to time-bound the ability to obtain and run an executive.
#define TIME_BOUNDED

using System;
using System.Diagnostics;
using System.Reflection;

#if LICENSING_ENABLED
using Highpoint.Sage.Licensing;
#endif // LICENSING_ENABLED


namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// ExecFactory produces instances of objects that implement IExecutive.
    /// </summary>
    public class ExecFactory
    {

#if LICENSING_ENABLED
        private static bool _licenseChecked = true;
#endif // LICENSING_ENABLED
        private static readonly object _lock = new object();
        private static volatile ExecFactory _instance;
        private static ExecFactoryOptions _options = new ExecFactoryOptions();
        private static ExecutiveOptions _executiveOptions = new ExecutiveOptions();

        private readonly ExecFactoryOptions _instanceOptions;
        private readonly ExecutiveOptions _instanceExecutiveOptions;
        private string _requiredType;

        private ExecFactory(ExecFactoryOptions options = null, ExecutiveOptions executiveOptions = null)
        {
            _instanceOptions = options ?? new ExecFactoryOptions();
            _instanceExecutiveOptions = executiveOptions ?? new ExecutiveOptions();
            _requiredType = _instanceOptions.DefaultExecutiveType;
#if LICENSING_ENABLED
#if TIME_BOUNDED
            DateTime expiry = new DateTime(2016, 12, 31);
#else
            DateTime expiry = DateTime.MaxValue;
#endif // TIME_BOUNDED

            if (!_licenseChecked || DateTime.Now > expiry ) {
                if (Debugger.IsAttached) {
                    if (!LicenseManager.Check()) {
                        MessageBox.Show("Sage� Simulation and Modeling Library license is invalid.", "Licensing Error");
                    }
                }

                try {
                    LicenseManager.ReportUsage( );
                } catch (WebException) {
                    Console.WriteLine( "Failure to report usage." );
                } catch (Exception) {
                    Console.WriteLine( "Failure to report usage." );
                }

                _licenseChecked = true;
            }

#if TIME_BOUNDED
            Console.WriteLine("Trial license expiration, {0}.", expiry);
#endif // TIME_BOUNDED
#endif // LICENSING_ENABLED


        }

        /// <summary>
        /// Provides a reference to the one ExecFactory in the current Application Context.
        /// </summary>
        public static ExecFactory Instance
        {
            get
            {

                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new ExecFactory(_options, _executiveOptions);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Configures ExecFactory defaults. Call before first use to override defaults.
        /// </summary>
        /// <param name="options">The factory options to apply.</param>
        /// <param name="executiveOptions">The default executive options to apply.</param>
        public static void Configure(ExecFactoryOptions options, ExecutiveOptions executiveOptions = null)
        {
            _options = options ?? new ExecFactoryOptions();
            _executiveOptions = executiveOptions ?? new ExecutiveOptions();
        }

        /// <summary>
        /// Creates a copy of the default type of executive.
        /// </summary>
        /// <returns>An instance of the default executive type.</returns>
        public IExecutive CreateExecutive()
        {
            return CreateExecutive(Guid.NewGuid());
        }

        /// <summary>
        /// Creates an executive of the built-in type specified by the provided enumeration.
        /// </summary>
        /// <param name="execType">Type of the executive.</param>
        /// <returns>IExecutive.</returns>
        public IExecutive CreateExecutive(ExecType execType)
        {
            return CreateExecutive(execType, Guid.NewGuid());
        }

        /// <summary>
        /// Creates an executive of the built-in type specified by the provided enumeration.
        /// </summary>
        /// <param name="execType">Type of the execute.</param>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>IExecutive.</returns>
        public IExecutive CreateExecutive(ExecType execType, Guid guid)
        {
            switch (execType)
            {
                case ExecType.FullFeatured:
                    return CreateExecutive(typeof(Highpoint.Sage.SimCore.Executive).FullName, guid);
                case ExecType.SingleThreaded:
                    return CreateExecutive(typeof(Highpoint.Sage.SimCore.ExecutiveFastLight).FullName, guid);
                default:
                    throw new ApplicationException("Attempt to create an instance of an unsupported executive (" + execType + ").");
            }
        }

        public IExecutive CreateExecutive(string typeName, Guid guid)
        {

            Type type = Type.GetType(typeName);
            if (type == null)
                throw new ApplicationException("Attempt to create an executive of type \"" + typeName + "\", but that type does not exist.");
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            //Binder binder = null;
            //ParameterModifier[] modifiers = null;

            ConstructorInfo ci = type.GetConstructor(bindingAttr, null, new[] { typeof(Guid), typeof(ExecutiveOptions) }, null);
            object objExec;
            if (ci != null)
            {
                objExec = ci.Invoke(new object[] { guid, _instanceExecutiveOptions });
            }
            else
            {
                ci = type.GetConstructor(bindingAttr, null, new[] { typeof(Guid), typeof(ExecutiveOptions), typeof(Highpoint.Sage.Diagnostics.DiagnosticsOptions) }, null);
                if (ci != null)
                {
                    objExec = ci.Invoke(new object[] { guid, _instanceExecutiveOptions, null });
                }
                else
                {
                    ci = type.GetConstructor(bindingAttr, null, new[] { typeof(Guid) }, null);
                    if (ci == null)
                        throw new ApplicationException("Attempt to create an executive without a matching constructor.");
                    objExec = ci.Invoke(new object[] { guid });
                }
            }
            IExecutive exec = (IExecutive)objExec;

            return exec;

        }


        /// <summary>
        /// Creates a copy of the default type of executive.
        /// </summary>
        /// <param name="execGuid">The guid to be assigned to the new executive.</param>
        /// <returns>A copy of the default executive.</returns>
        public IExecutive CreateExecutive(Guid execGuid)
        {
            if (string.IsNullOrWhiteSpace(_requiredType))
            {
                _requiredType = _instanceOptions.DefaultExecutiveType;
            }
            return CreateExecutive(_requiredType, execGuid);
        }
    }
}

