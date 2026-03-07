/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.ItemBased.Queues
{
    public class ShortestQueueStrategy : ISelectionStrategy
    {

        ICollection _queues = new ArrayList();

        public ShortestQueueStrategy()
        {
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
                _queues = value;
            }
        }

        public object GetNext(object? context)
        {
            if (_queues.Count == 0)
                throw new ApplicationException("Queue selector has no queues to select from.");
            int emptiestCount = int.MaxValue;
            Queue? nextQueue = null;
            foreach (Queue queue in _queues)
            {
                if (queue.Count < emptiestCount)
                {
                    emptiestCount = queue.Count;
                    nextQueue = queue;
                }
            }
            return nextQueue ?? throw new ApplicationException("Queue selector has no queues to select from.");
        }
    }
}
