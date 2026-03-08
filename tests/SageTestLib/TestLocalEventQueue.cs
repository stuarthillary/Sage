/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Core;
//using System.Collections;
using Xunit;
using System;
using System.Diagnostics;

//using ProcessStep = Highpoint.Sage.Servers.SimpleServerWithPreQueue;

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// Summary description for zTestLocalEventQueue.
    /// </summary>

    public class LocalEventQueueTester : IHasName, IDisposable
    {

        #region MSTest Goo

        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        private int _numEvents;
        private LocalEventQueue _leq;

        [Fact]
        public void TestLocalEventQueue()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            _numEvents = 10;
            _leq = new LocalEventQueue(exec, 4, new ExecEventReceiver(DoSomething));

            DateTime when = DateTime.Now;
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);

            exec.Start();

        }

        [Fact]
        public void TestLocalEventQueue2()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            _numEvents = 10;
            _leq = new LocalEventQueue(exec, 2, new ExecEventReceiver(DoSomething));

            DateTime when = DateTime.Now;
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            when += TimeSpan.FromMinutes(5);
            _leq.Enqueue(_numEvents--, when);
            Console.WriteLine(_leq.EarliestCompletionTime.ToString());

            exec.Start();

        }

        private void DoSomething(IExecutive exec, object userData)
        {
            string msg = "";
            if (!_leq.IsEmpty)
                msg = " - the new head of the event queue will happen at " + _leq.EarliestCompletionTime.ToString();
            Console.WriteLine(exec.Now.ToString() + " : Receiving event " + userData.ToString() + msg + ".");
            if (_numEvents > 0)
            {
                DateTime when = exec.Now + TimeSpan.FromMinutes(10);
                _leq.Enqueue(_numEvents--, when);
            }
        }

        public string Name
        {
            get
            {
                return "Local event queue tester";
            }
        }
    }
}
