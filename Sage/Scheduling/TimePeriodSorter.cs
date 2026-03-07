/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.Scheduling
{

    /// <summary>
    /// An implementer of IComparer&lt;TimePeriod&gt; that can be used to sort a collection of
    /// ITimePeriodReadOnly objects. The 'sortOnWhat' parameter allows the
    /// user to choose whether to sort on start time, duration or end time.
    /// </summary>
    public class TimePeriodSorter : IComparer<ITimePeriod>
    {
        private static TimePeriodSorter? _byIncreasingStartTime;
        private static TimePeriodSorter? _byIncreasingDuration;
        private static TimePeriodSorter? _byIncreasingEndTime;
        private static TimePeriodSorter? _byDecreasingStartTime;
        private static TimePeriodSorter? _byDecreasingDuration;
        private static TimePeriodSorter? _byDecreasingEndTime;

        private TimePeriodPart _sortOnWhat;
        private int _ascending;

        private TimePeriodSorter(TimePeriodPart sortOnWhat, bool ascending)
        {
            _sortOnWhat = sortOnWhat;
            _ascending = ascending ? 1 : -1;
        }

        #region IComparer<TimePeriod> Members

        public int Compare(ITimePeriod? tp1, ITimePeriod? tp2)
        {
            if (ReferenceEquals(tp1, tp2))
                return 0;
            if (tp1 is null)
                return -1;
            if (tp2 is null)
                return 1;
            switch (_sortOnWhat)
            {
                case TimePeriodPart.StartTime:
                    return _ascending * Comparer.Default.Compare(tp1.StartTime, tp2.StartTime);
                case TimePeriodPart.Duration:
                    return _ascending * Comparer.Default.Compare(tp1.Duration, tp2.Duration);
                case TimePeriodPart.EndTime:
                    return _ascending * Comparer.Default.Compare(tp1.EndTime, tp2.EndTime);
                default:
                    throw new ApplicationException("Unknown part of TimePeriod, " + _sortOnWhat + ", used for sorting.");
            }
        }

        #endregion

        public static TimePeriodSorter ByIncreasingStartTime
        {
            get
            {
                return _byIncreasingStartTime ??= new TimePeriodSorter(TimePeriodPart.StartTime, true);
            }
        }

        public static TimePeriodSorter ByIncreasingDuration
        {
            get
            {
                return _byIncreasingDuration ??= new TimePeriodSorter(TimePeriodPart.Duration, true);
            }
        }

        public static TimePeriodSorter ByIncreasingEndTime
        {
            get
            {
                return _byIncreasingEndTime ??= new TimePeriodSorter(TimePeriodPart.EndTime, true);
            }
        }
        public static TimePeriodSorter ByDecreasingStartTime
        {
            get
            {
                return _byDecreasingStartTime ??= new TimePeriodSorter(TimePeriodPart.StartTime, false);
            }
        }

        public static TimePeriodSorter ByDecreasingDuration
        {
            get
            {
                return _byDecreasingDuration ??= new TimePeriodSorter(TimePeriodPart.Duration, false);
            }
        }

        public static TimePeriodSorter ByDecreasingEndTime
        {
            get
            {
                return _byDecreasingEndTime ??= new TimePeriodSorter(TimePeriodPart.EndTime, false);
            }
        }
    }
}
