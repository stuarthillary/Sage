/* This source code licensed under the GNU Affero General Public License */

using System;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.Core
{
    /// <summary>
    /// MissingParameterException is thrown when a required parameter is missing. Typically used in a late bound, read-from-name/value pair collection scenario.
    /// </summary>
    [Serializable]
    public class RuntimeException : Exception
    {

        #region public ctors
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class.
        /// </summary>
        public RuntimeException()
        {
        }
        /// <summary>
        /// Creates a new instance of the <see cref="T:AnalysisFailedException"/> class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public RuntimeException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
    }
}

