/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{
    /// <summary>
    /// MissingParameterException is thrown when a required parameter is missing. Typically used in a late bound, read-from-name/value pair collection scenario.
    /// </summary>
    [Serializable]
    public class VaporPressureException : Exception
    {
        private readonly ReasonCode _reason = ReasonCode.UnderPressure;
        
        // For best practice guidelines regarding the creation of new exception types, see
        //    https://msdn.microsoft.com/en-us/library/5b2yeyab(v=vs.110).aspx
        
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public VaporPressureException()
        {
        }
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public VaporPressureException(string ofWhat, double atPressure, double minSupportedPressure)
        : base(string.Format("Attempt to compute vapor pressure of {0} at a pressure of {1} Pascals is unsupported. Please use a value greater than {2} Pascals for absolute pressure.",
                ofWhat, atPressure, minSupportedPressure))
        {
            _reason = ReasonCode.UnderPressure;
        }
        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public VaporPressureException(string message) : base(message) { }
        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public VaporPressureException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
        public ReasonCode Reason => _reason;

        public enum ReasonCode
        {
            UnderPressure
        }
    }
}

