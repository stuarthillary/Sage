/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.SimCore
{
    internal class MergedTransitionHandler : TransitionHandler
    {
        public MergedTransitionHandler(TransitionHandler inbound,
                                       TransitionHandler across,
                                       TransitionHandler outbound,
                                       TransitionHandler universal)
        {

            MergePrepareHandlers(inbound.PrepareHandlers, outbound.PrepareHandlers, across.PrepareHandlers, universal.PrepareHandlers);
            MergeCommitHandlers(inbound.CommitHandlers, outbound.CommitHandlers, across.CommitHandlers, universal.CommitHandlers);
            MergeRollbackHandlers(inbound.RollbackHandlers, outbound.RollbackHandlers, across.RollbackHandlers, universal.RollbackHandlers);

        }

        private void MergePrepareHandlers(SortedList<double, PrepareTransitionEvent> src1,
                                          SortedList<double, PrepareTransitionEvent> src2,
                                          SortedList<double, PrepareTransitionEvent> src3,
                                          SortedList<double, PrepareTransitionEvent> src4)
        {
            int nextKey = 0;
            var enumerators = new List<IEnumerator<KeyValuePair<double, PrepareTransitionEvent>>>();
            enumerators.Add(src1.GetEnumerator());
            enumerators.Add(src2.GetEnumerator());
            enumerators.Add(src3.GetEnumerator());
            enumerators.Add(src4.GetEnumerator());

            var removees = new List<IEnumerator<KeyValuePair<double, PrepareTransitionEvent>>>();
            foreach (var enumerator in enumerators)
            {
                if (!enumerator.MoveNext())
                    removees.Add(enumerator);
            }
            foreach (var removee in removees)
                enumerators.Remove(removee);

            while (enumerators.Count != 0)
            {
                var hostEnum = enumerators[0];
                double lowest = hostEnum.Current.Key;
                foreach (var enumerator in enumerators)
                {
                    double thisKey = enumerator.Current.Key;
                    if (thisKey < lowest)
                    {
                        hostEnum = enumerator;
                        lowest = thisKey;
                    }
                }

                AddPrepareEvent(nextKey++, hostEnum.Current.Value);
                if (!hostEnum.MoveNext())
                    enumerators.Remove(hostEnum);
            }
        }

        private void MergeCommitHandlers(SortedList<double, CommitTransitionEvent> src1,
                                         SortedList<double, CommitTransitionEvent> src2,
                                         SortedList<double, CommitTransitionEvent> src3,
                                         SortedList<double, CommitTransitionEvent> src4)
        {
            int nextKey = 0;
            var enumerators = new List<IEnumerator<KeyValuePair<double, CommitTransitionEvent>>>();
            enumerators.Add(src1.GetEnumerator());
            enumerators.Add(src2.GetEnumerator());
            enumerators.Add(src3.GetEnumerator());
            enumerators.Add(src4.GetEnumerator());

            var removees = new List<IEnumerator<KeyValuePair<double, CommitTransitionEvent>>>();
            foreach (var enumerator in enumerators)
            {
                if (!enumerator.MoveNext())
                    removees.Add(enumerator);
            }
            foreach (var removee in removees)
                enumerators.Remove(removee);

            while (enumerators.Count != 0)
            {
                var hostEnum = enumerators[0];
                double lowest = hostEnum.Current.Key;
                foreach (var enumerator in enumerators)
                {
                    double thisKey = enumerator.Current.Key;
                    if (thisKey < lowest)
                    {
                        hostEnum = enumerator;
                        lowest = thisKey;
                    }
                }

                AddCommitEvent(nextKey++, hostEnum.Current.Value);
                if (!hostEnum.MoveNext())
                    enumerators.Remove(hostEnum);
            }
        }
        
        private void MergeRollbackHandlers(SortedList<double, RollbackTransitionEvent> src1,
                                           SortedList<double, RollbackTransitionEvent> src2,
                                           SortedList<double, RollbackTransitionEvent> src3,
                                           SortedList<double, RollbackTransitionEvent> src4)
        {
            int nextKey = 0;
            var enumerators = new List<IEnumerator<KeyValuePair<double, RollbackTransitionEvent>>>();
            enumerators.Add(src1.GetEnumerator());
            enumerators.Add(src2.GetEnumerator());
            enumerators.Add(src3.GetEnumerator());
            enumerators.Add(src4.GetEnumerator());

            var removees = new List<IEnumerator<KeyValuePair<double, RollbackTransitionEvent>>>();
            foreach (var enumerator in enumerators)
            {
                if (!enumerator.MoveNext())
                    removees.Add(enumerator);
            }
            foreach (var removee in removees)
                enumerators.Remove(removee);

            while (enumerators.Count != 0)
            {
                var hostEnum = enumerators[0];
                double lowest = hostEnum.Current.Key;
                foreach (var enumerator in enumerators)
                {
                    double thisKey = enumerator.Current.Key;
                    if (thisKey < lowest)
                    {
                        hostEnum = enumerator;
                        lowest = thisKey;
                    }
                }

                AddRollbackEvent(nextKey++, hostEnum.Current.Value);
                if (!hostEnum.MoveNext())
                    enumerators.Remove(hostEnum);
            }
        }
    }

}

