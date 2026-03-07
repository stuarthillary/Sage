/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;


namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// Class ModelConfig is a collection of initialization parameters provided to the model.
    /// </summary>
    public class ModelConfig
    {
        private readonly Dictionary<string, string> _parameters;

        /// <summary>
        /// Creates a new ModelConfig with no parameters.
        /// </summary>
        public ModelConfig() : this((IDictionary<string, string>)null) { }

        /// <summary>
        /// Creates a new ModelConfig from the provided parameter map.
        /// </summary>
        /// <param name="parameters">The parameters to expose to the model.</param>
        public ModelConfig(IDictionary<string, string> parameters)
        {
            _parameters = parameters == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a new ModelConfig. Configuration sections are no longer read by the library.
        /// </summary>
        /// <param name="sectionName">The legacy configuration section name.</param>
        [Obsolete("ModelConfig no longer reads configuration sections. Use ModelConfig(IDictionary<string, string>) instead.")]
        public ModelConfig(string sectionName)
        {
            if (!string.IsNullOrWhiteSpace(sectionName))
            {
                _Debug.WriteLine(
                    $"ModelConfig no longer reads configuration sections. Provide values via ModelConfig(IDictionary<string, string>). Section: {sectionName}.");
            }
        }

        public string GetSimpleParameter(string key)
        {
            if (key == null)
                return null;

            string retval = null;
            if (_parameters.TryGetValue(key, out string value))
                retval = value;
            if (retval == null)
            {
                // TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
                _Debug.WriteLine("Application requested unfound parameter associated with key " + key + " in the model configuration.");
            }
            return retval;
        }

        /// <summary>
        /// Sets or replaces a simple parameter value.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        public void SetSimpleParameter(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            _parameters[key] = value;
        }
    }

}
