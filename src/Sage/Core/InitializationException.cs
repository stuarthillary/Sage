/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Core
{
    /// <summary>
    /// InitializationException summary
    /// </summary>
    [Serializable]
    public class InitializationException : Exception
    {
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        #region public ctors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public InitializationException()
        {
        }
        /// <summary>
        /// Creates a new instance of this class with a specific message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public InitializationException(string message) : base(message) { }
        /// <summary>
        /// Creates a new instance of this class with a specific message and an inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception inner exception.</param>
        public InitializationException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
    }
}

