/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Queues
{
    public class OldestShortestQueueStrategy : ISelectionStrategy
    {

        ICollection _queues = new List<IQueue>();
        readonly QueueLevelChangeEvent _qlce;
        private readonly List<Queue> _queueList = new List<Queue>();

        public OldestShortestQueueStrategy()
        {
            _qlce = new QueueLevelChangeEvent(OnQueueLevelChanged);
        }

        public ICollection Candidates
        {
            get
            {
                return _queues;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                foreach (Queue queue in _queues)
                    queue.LevelChangedEvent -= _qlce;
                _queues = value;
                foreach (Queue queue in _queues)
                {
                    queue.LevelChangedEvent += _qlce;
                    _queueList.Add(queue);
                }
            }
        }

        public object GetNext(object? context)
        {
            if (_queues.Count == 0)
                throw new ApplicationException("Queue selector has no queues to select from.");
            Queue nextQueue;
            lock (_queues)
            {
                nextQueue = _queueList[0];
                _queueList.RemoveAt(0);
            }
            return nextQueue;
        }

        private void OnQueueLevelChanged(int previous, int current, IQueue queue)
        {
            _queueList.Remove((Queue)queue); // Removes if present; already gone from GetNext.
            int i = 0;
            while (i < _queueList.Count)
            {
                if (_queueList[i].Count <= queue.Count)
                {
                    i++;
                    continue;
                }
                break;
            }
            _queueList.Insert(i, (Queue)queue);
        }
    }
}
