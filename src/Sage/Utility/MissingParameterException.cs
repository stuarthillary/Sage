/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// MissingParameterException is thrown when a required parameter is missing. Typically used in a late bound, read-from-name/value pair collection scenario.
    /// </summary>
    [Serializable]
    public class MissingParameterException : Exception
    {
        // For best practice guidelines regarding the creation of new exception types, see
        //    https://msdn.microsoft.com/en-us/library/5b2yeyab(v=vs.110).aspx
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public MissingParameterException()
        {
        }

        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MissingParameterException(string message) : base(message) { }

        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public MissingParameterException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
    }
}

