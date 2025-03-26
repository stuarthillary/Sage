/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.SimCore
{
    internal class TransitionHandler : ITransitionHandler
    {
        #region Prepare Event
        protected SortedList<double, PrepareTransitionEvent> prepareHandlers = new SortedList<double, PrepareTransitionEvent>();
        private double _nextPreparePriority = 0.0;

        public event PrepareTransitionEvent Prepare
        {
            add
            {
                AddPrepareEvent(_nextPreparePriority += double.Epsilon, value);
            }
            remove
            {
                RemovePrepareEvent(value);
            }
        }

        public void AddPrepareEvent(double priority, PrepareTransitionEvent pte)
        {
            if (!prepareHandlers.ContainsValue(pte))
            {
                prepareHandlers.Add(priority, pte);
            }
        }

        public void RemovePrepareEvent(PrepareTransitionEvent pte)
        {
            if (prepareHandlers.ContainsValue(pte))
            {
                prepareHandlers.Remove(prepareHandlers.GetKeyAtIndex(prepareHandlers.IndexOfValue(pte)));
            }
        }

        internal SortedList<double, PrepareTransitionEvent> PrepareHandlers
        {
            get
            {
                return prepareHandlers;
            }
        }
        #endregion Prepare Event

        #region Commit Event
        protected SortedList<double, CommitTransitionEvent> commitHandlers = new SortedList<double, CommitTransitionEvent>();
        private double _nextCommitPriority = 0.0;

        public event CommitTransitionEvent Commit
        {
            add
            {
                AddCommitEvent(_nextCommitPriority += double.Epsilon, value);
            }
            remove
            {
                RemoveCommitEvent(value);
            }
        }

        public void AddCommitEvent(double priority, CommitTransitionEvent cte)
        {
            if (!commitHandlers.ContainsValue(cte))
            {
                commitHandlers.Add(priority, cte);
            }
        }

        public void RemoveCommitEvent(CommitTransitionEvent cte)
        {
            if (commitHandlers.ContainsValue(cte))
            {
                commitHandlers.Remove(commitHandlers.GetKeyAtIndex(commitHandlers.IndexOfValue(cte)));
            }

        }

        internal SortedList<double, CommitTransitionEvent> CommitHandlers
        {
            get
            {
                return commitHandlers;
            }
        }
        #endregion

        #region Rollback Event
        protected SortedList<double, RollbackTransitionEvent> rollbackHandlers = new SortedList<double, RollbackTransitionEvent>();
        private double _nextRollbackPriority = 0.0;

        public event RollbackTransitionEvent Rollback
        {
            add
            {
                AddRollbackEvent(_nextRollbackPriority += double.Epsilon, value);
            }
            remove
            {
                RemoveRollbackEvent(value);
            }
        }

        public void AddRollbackEvent(double priority, RollbackTransitionEvent rte)
        {
            if (!rollbackHandlers.ContainsValue(rte))
            {
                rollbackHandlers.Add(priority, rte);
            }
        }

        public void RemoveRollbackEvent(RollbackTransitionEvent rte)
        {
            if (rollbackHandlers.ContainsValue(rte))
            {
                rollbackHandlers.Remove(rollbackHandlers.GetKeyAtIndex(rollbackHandlers.IndexOfValue(rte)));
            }
        }

        internal SortedList<double, RollbackTransitionEvent> RollbackHandlers
        {
            get
            {
                return rollbackHandlers;
            }
        }
        #endregion

        public bool IsValidTransition
        {
            get
            {
                return true;
            }
        }

        public IReadOnlyList<ITransitionFailureReason> DoPrepare(IModel model, object userData)
        {
            List<ITransitionFailureReason> al = new List<ITransitionFailureReason>();
            for (int i = 0; i < prepareHandlers.Count; i++)
            {
                PrepareTransitionEvent pte = prepareHandlers.GetValueAtIndex(i);
                ITransitionFailureReason result = pte(model, userData);
                if (result != null)
                    al.Add(result);
            }
            return al;
        }

        public void DoCommit(IModel model, object userData)
        {
            for (int i = 0; i < commitHandlers.Count; i++)
            {
                CommitTransitionEvent cte = commitHandlers.GetValueAtIndex(i);
                cte(model, userData);
            }
        }

        public void DoRollback(IModel model, object userData, IReadOnlyList<ITransitionFailureReason> failureReasons)
        {
            for (int i = 0; i < rollbackHandlers.Count; i++)
            {
                RollbackTransitionEvent rte = rollbackHandlers.GetValueAtIndex(i);
                rte(model, userData, failureReasons);
            }
        }

        public string Dump()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Recipients for \"Prepare\" event:");
            sb.Append("\t");
            sb.Append(prepareHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(prepareHandlers));

            sb.Append("Recipients for \"Rollback\" event:");
            sb.Append("\t");
            sb.Append(rollbackHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(rollbackHandlers));

            sb.Append("Recipients for \"Commit\" event:");
            sb.Append("\t");
            sb.Append(commitHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(commitHandlers));
            return sb.ToString();
        }

        private string DumpHandlers<TValue>(SortedList<double, TValue> handlers) where TValue: Delegate
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int i = 0;
            foreach (var de in handlers)
            {
                double pri = de.Key;
                Delegate del = de.Value;
                sb.Append("\t" + i + ".)\t[" + del.Target + "].[" + del.Method + "] @ pri = " + pri + "\r\n");
                i++;
            }
            return sb.ToString();
        }
    }

}
