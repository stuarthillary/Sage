#nullable disable
/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// An exception that is fired if and when a transition fails for a reason
    /// internal to the state machine - currently, this is only in the case of
    /// a request to perform an illegal state transition.
    /// </summary>
    public class TransitionFailureException : Exception
    {

        private readonly List<ITransitionFailureReason> _reasons;

        private static string MessageFromReasons(IReadOnlyList<ITransitionFailureReason> reasons)
        {
            return string.Join(Environment.NewLine, reasons.Select(r => r.Reason));
        }

        /// <summary>
        /// Creates a TransitionFailureException around a list of failure reasons.
        /// </summary>
        /// <param name="reasons">A list of failure reasons.</param>
        public TransitionFailureException(IReadOnlyList<ITransitionFailureReason> reasons) : base(MessageFromReasons(reasons))
        {
            _reasons = new List<ITransitionFailureReason>(reasons);
        }

        /// <summary>
        /// Creates a TransitionFailureException around a single reason.
        /// </summary>
        /// <param name="reason">The TransitionFailureReason.</param>
        public TransitionFailureException(ITransitionFailureReason reason) : this(new List<ITransitionFailureReason>(){reason})
        {
        }

        /// <summary>
        /// Gives the caller access to the list of failure reasons.
        /// </summary>
        public IReadOnlyList<ITransitionFailureReason> Reasons
        {
            get
            {
                return _reasons;
            }
        }

        /// <summary>
        /// Provides a human-readable representation of the failure exception,
        /// in the form of a narrative describing the failure reasons.
        /// </summary>
        /// <returns>A narrative describing the failure reasons.</returns>
        public override string ToString()
        {
            int nr = _reasons.Count;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Failure making model transition request. ");
            if (nr == 1)
            {
                sb.Append("There is 1 reason why:");
            }
            else
            {
                sb.Append("There are " + nr + " reasons why:");
            }

            foreach (ITransitionFailureReason itfr in _reasons)
            {
                sb.Append("\r\n\t");
                sb.Append(itfr.Reason);
            }

            return sb.ToString();
        }
    }
}

