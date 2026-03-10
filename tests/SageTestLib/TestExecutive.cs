/* This source code licensed under the GNU Affero General Public License */

using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Highpoint.Sage.Core
{


    public class ExecTester : IDisposable
    {

        #region Private Fields
        private int NUM_EVENTS = 12;
        private Random _random = new Random(1000);
        private int _validateCount;
        private int _validatePriority;
        private DateTime _validateWhen;
        private bool _error;
        private ArrayList _validateUnRequest;

        private ExecEventType _execEventType = ExecEventType.Synchronous;
        
        // Lock object to serialize tests that modify shared ExecFactory state
        private static readonly object _execFactoryLock = new object();
        #endregion Private Fields

        public ExecTester()
        {
            Init();
        }


        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
                    GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events.")]
        public void TestExecutiveCount()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validate variable
            _validateCount = 0;
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveCount");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                ++_validateCount;
                Debug.WriteLine("Primary requesting event number " + _validateCount);
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverCount), when, priority, null, _execEventType);
            }

            if (_validateCount != NUM_EVENTS)
            {
                Debug.WriteLine("Number of submitted event requests don't equal supposed number of : " + NUM_EVENTS);
            }

            Debug.WriteLine("");

            exec.Start();

            // test validate variable
            Assert.True(0 == _validateCount, "Executive did not submit all events");

            Debug.WriteLine("");
        }

        private void ExecEventRecieverCount(IExecutive exec, object userData)
        {
            --_validateCount;
        }

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events, 
        /// using RequestEvent method without the event type parameter.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events, using RequestEvent method without the event type parameter.")]
        public void TestExecutiveCountDefaultParameter()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validate variable
            _validateCount = 0;
            Debug.WriteLine("");
            Debug.WriteLine("Start test ExecEventRecieverCountLessParameter");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                ++_validateCount;
                Debug.WriteLine("Primary requesting event number " + _validateCount);
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverCountLessParameter), when, priority, null);
            }

            if (_validateCount != NUM_EVENTS)
            {
                Debug.WriteLine("Number of submitted event requests don't equal supposed number of : " + NUM_EVENTS);
            }

            Debug.WriteLine("");

            exec.Start();

            // test validate variable
            Assert.True(0 == _validateCount, "Executive did not submit all events");

            Debug.WriteLine("");
        }

        private void ExecEventRecieverCountLessParameter(IExecutive exec, object userData)
        {
            --_validateCount;
        }

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events, 
        /// ordered by the requested callback priority at the same callback time.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events, ordered by the requested callback priority at the same callback time.")]
        public void TestExecutivePriority()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            int priority;

            // initialize validation variables
            _error = false;
            _validatePriority = 0;
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutivePriority");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                priority = (int)(_random.NextDouble() * 100);
                if (_validatePriority < priority)
                {
                    _validatePriority = priority;
                }
                Debug.WriteLine("Primary requesting event service for " + now + ", at priority " + priority);
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverPriority), now, priority, priority, _execEventType);
            }

            Debug.WriteLine("");

            exec.Start();

            Debug.WriteLine("");

            // test validate variable
            Assert.True(!_error, "Executive did not submit events in the order of the correct priority");

            Debug.WriteLine("");
        }

        private void ExecEventRecieverPriority(IExecutive exec, object userData)
        {
            if (_validatePriority < (int)userData)
            {
                _error = true;
            }
            _validatePriority = (int)userData;
            Debug.WriteLine("Primary fireing event with priority " + (int)userData);
        }

        /// <summary>
        /// Checks to see that an executive can store & service all submitted events, 
        /// ordered by the requested callback time.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can store & service all submitted events, ordered by the requested callback time.")]
        public void TestExecutiveWhen()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;

            // initialize validation variables
            _error = false;
            _validateWhen = new DateTime(now.Ticks);
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveWhen");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                Debug.WriteLine("Primary requesting event service for " + when);
                //if (m_validateWhen.Ticks < when.Ticks) {m_validateWhen = when;}
                exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverWhen), when, 0, when, _execEventType);
            }

            Debug.WriteLine("");

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.True(!_error, "Executive did not submit events in correct date/time order");

            Debug.WriteLine("");
        }

        private void ExecEventRecieverWhen(IExecutive exec, object userData)
        {
            if (_validateWhen.Ticks > ((DateTime)userData).Ticks)
            {
                _error = true;
            }
            _validateWhen = (DateTime)userData;
            Debug.WriteLine("Primary fireing event at date/time " + (DateTime)userData);
        }

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, 
        /// identifying the events by a hash code
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a hash code.")]
        public void TestExecutiveUnRequestHash()
        {

            string[] eventUserData = new string[] { "Cat", "Dog", "Bat", "Frog", "Mink", "Bee", "Bird", "Worm", "Horse", "Moose", "Bear", "Platypus" };
            ArrayList eventsToRemove = new ArrayList(new string[] { "Cat", "Bat", "Bee", "Bird", "Bear", "Platypus" });
            //ArrayList eventsThatShouldRemain = new ArrayList(new string[]{"Dog","Frog","Mink","Worm","Horse","Moose"});

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestHash");
            Debug.WriteLine("");

            ArrayList eventIDsForRemoval = new ArrayList();
            foreach (string eud in eventUserData)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Trace.Write("Primary requesting event service with user data \"" + eud + "\", and eventID ");
                long eventID = exec.RequestEvent(new ExecEventReceiver(ExecEventRecieverUnRequestHash), when, priority, eud, _execEventType);
                Debug.WriteLine(eventID + ".");
                if (eventsToRemove.Contains(eud))
                {
                    eventIDsForRemoval.Add(eventID);
                    Debug.WriteLine("\tWe will be requesting the removal of this event.");
                }
            }

            foreach (long eventID in eventIDsForRemoval)
            {
                Debug.WriteLine("Unrequesting event # " + eventID);
                _validateUnRequest.Add(eventID);
                exec.UnRequestEvent(eventID);
            }

            Debug.WriteLine("");

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.True(!_error, "Executive did fire a unrequested event");

            Debug.WriteLine("");
        }

        private void ExecEventRecieverUnRequestHash(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("Primary firing event with user data = \"" + userData + "\"");
        }

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, 
        /// identifying the events by a target object.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a target object.")]
        public void TestExecutiveUnRequestTarget()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;
            OtherTarget ot = null;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestTarget");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting event service " + i);
                switch (i)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 7:
                    case 11:
                        _validateUnRequest.Add(i);
                        ot = new OtherTarget(_validateUnRequest, _error);
                        ExecEventReceiver eer = new ExecEventReceiver(ot.ExecEventRecieverUnRequestEventReceiver);
                        exec.RequestEvent(eer, when, priority, i, _execEventType);
                        exec.UnRequestEvents(ot);
                        break;
                    default:
                        exec.RequestEvent(new ExecEventReceiver(this.ExecEventRecieverUnRequestEventReceiver), when, priority, i, _execEventType);
                        break;
                };
            }


            Debug.WriteLine("");

            // AEL			exec.UnRequestEvents(new OtherTarget(m_validateUnRequest, m_error));

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.True(!_error, "Executive did fire a unrequested event");

            Debug.WriteLine("");
        }

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, identifying the events by a delegate method.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a delegate method.")]
        public void TestExecutiveUnRequestDelegate()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestDelegate");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting event service " + i);
                switch (i)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 7:
                    case 11:
                        _validateUnRequest.Add(i);
                        ExecEventReceiver eer = new ExecEventReceiver(this.ExecEventRecieverUnRequestDelegate);
                        exec.RequestEvent(eer, when, priority, i, _execEventType);
                        exec.UnRequestEvents((Delegate)eer);
                        break;
                    default:
                        exec.RequestEvent(new ExecEventReceiver(this.ExecEventRecieverUnRequestEventReceiver), when, priority, i, _execEventType);
                        break;
                };
            }


            Debug.WriteLine("");

            // AEL		exec.UnRequestEvents((Delegate)(new ExecEventReceiver(this.ExecEventRecieverUnRequestDelegate)));

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.True(!_error, "Executive did fire a unrequested event");

            Debug.WriteLine("");
        }

        /// <summary>
        /// Checks to see that an executive can start, then stop and restart.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can start, then stop and restart.")]
        public void TestExecutiveStopStart()
        {

            // use System.Threading;

            DateTime startTime;
            System.Threading.Thread starter, interrupter;
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            // First get the duration w/o pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new ParameterizedThreadStart(StartExec));
            starter.Start(exec);
            starter.Join();
            TimeSpan shortDuration = DateTime.Now - startTime;
            Debug.WriteLine("Duration w/o pause is " + shortDuration.TotalSeconds + " seconds.");
            exec.Reset();

            // Now get the duration w/ pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new ParameterizedThreadStart(StartExec));
            interrupter = new System.Threading.Thread(new ParameterizedThreadStart(StopAndRestartExec));
            starter.Start(exec);
            interrupter.Start(exec);
            interrupter.Join();
            TimeSpan pauseDuration = DateTime.Now - startTime;

            // Finally, assess the comparative durations to ensure the test passed.
            Debug.WriteLine("Total test duration was " + pauseDuration.TotalSeconds + " seconds.");
            TimeSpan minAcceptableDuration = shortDuration + TimeSpan.FromMilliseconds(1500);
            Assert.True(pauseDuration > minAcceptableDuration,
                "Test duration of less than " + minAcceptableDuration.TotalSeconds
                + " seconds indicates a failure to properly stop and restart.");
        }


        /// <summary>
        /// Checks to see that an executive can start, then stop and restart.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can start, then stop and restart.")]
        public void TestExecutivePauseResume()
        {
            DateTime startTime;
            System.Threading.Thread starter, pauser;
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            InstrumentExecutiveStates(exec);

            // First get the duration w/o pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(StartExec));
            starter.Start(exec);
            starter.Join();
            TimeSpan shortDuration = DateTime.Now - startTime;
            Debug.WriteLine("Duration w/o pause is " + shortDuration.TotalSeconds + " seconds.");
            exec.Reset();

            // Now get the duration w/ pause.
            startTime = DateTime.Now;
            starter = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(StartExec));
            pauser = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(PauseAndResumeExec));
            starter.Start(exec);
            pauser.Start(exec);
            starter.Join();
            TimeSpan pauseDuration = DateTime.Now - startTime;

            // Finally, assess the comparative durations to ensure the test passed.
            Debug.WriteLine("Total test duration was " + pauseDuration.TotalSeconds + " seconds.");
            TimeSpan minAcceptableDuration = shortDuration + TimeSpan.FromMilliseconds(1500);
            Assert.True(pauseDuration > minAcceptableDuration,
                "Test duration of less than " + minAcceptableDuration.TotalSeconds + " seconds indicates a failure to properly stop and restart.");
        }

        private void InstrumentExecutiveStates(IExecutive exec)
        {
            exec.ExecutiveStarted_SingleShot += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Started (single shot).");
            });
            exec.ExecutiveStarted += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Started.");
            });
            exec.ExecutivePaused += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Paused.");
            });
            exec.ExecutiveResumed += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Resumed.");
            });
            exec.ExecutiveStopped += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Stopped.");
            });
            exec.ExecutiveFinished += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Finished.");
            });
            exec.ExecutiveAborted += new ExecutiveEvent(delegate (IExecutive e)
            {
                Debug.WriteLine("Executive Aborted.");
            });
        }

        #region TestExecutiveStopStart() and TestExecutivePauseResume() support methods.

        private void StartExec(object obj)
        {
            IExecutive exec = (IExecutive)obj;
            Debug.WriteLine("\r\n" + "Starting exec..." + "\r\n");
            DateTime startTime = new DateTime(2006, 5, 16);
            exec.RequestEvent(new ExecEventReceiver(SteadyStateEventStream), startTime, 1, 400);
            exec.Start();
        }

        private void StopAndRestartExec(object obj)
        {
            System.Threading.Thread.Sleep(1000);
            IExecutive exec = (IExecutive)obj;
            Debug.WriteLine("\r\n" + "Pausing for two seconds..." + "\r\n");
            Debug.WriteLine("Before pause, Exec state is " + exec.State);
            exec.Stop();
            System.Threading.Thread.Sleep(2000);
            Debug.WriteLine("After pause, Exec state is " + exec.State);
            Debug.WriteLine("\r\n" + "Resuming..." + "\r\n");
            exec.Start();
            Debug.WriteLine("Exec state is now " + exec.State);
        }

        private void PauseAndResumeExec(object obj)
        {
            System.Threading.Thread.Sleep(1000);
            IExecutive exec = (IExecutive)obj;
            Debug.WriteLine("\r\n" + "Pausing for two seconds..." + "\r\n");
            Debug.WriteLine("Before pause, Exec state is " + exec.State);
            exec.Pause();
            System.Threading.Thread.Sleep(2000);
            Debug.WriteLine("After pause, Exec state is " + exec.State);
            Debug.WriteLine("\r\n" + "Resuming..." + "\r\n");
            exec.Resume();
            Debug.WriteLine("Exec state is now " + exec.State);
        }

        private void SteadyStateEventStream(IExecutive exec, object userData)
        {
            int evtNum = (int)userData;
            if (evtNum % 20 == 0)
            {
                Debug.WriteLine(evtNum);
            }
            if (evtNum > 0)
            {
                evtNum--;
                DateTime nextEventTime = exec.Now + TimeSpan.FromMinutes(5);
                exec.RequestEvent(new ExecEventReceiver(SteadyStateEventStream),
                    nextEventTime, 1, evtNum);
            }
            System.Threading.Thread.Sleep(10); // Just to make the test discernible to the eye.
        }

        #endregion

        /// <summary>
        /// Checks to see that an executive can unrequest submitted events, 
        /// identifying the events by a selector.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can unrequest submitted events, identifying the events by a selector.")]
        public void TestExecutiveUnRequestSelector()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;
            //			IExecEventSelector ees = null;

            // initialize validation variables
            _error = false;
            _validateUnRequest = new ArrayList();
            Debug.WriteLine("");
            Debug.WriteLine("Start test TestExecutiveUnRequestSelector");
            Debug.WriteLine("");

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting event service " + i);
                switch (i)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 7:
                    case 11:
                        //						m_validateUnRequest.Add(i);
                        //						ees = new ExecEventSelectorByTargetType(this.GetType());
                        //						ExecEventReceiver eer = new ExecEventReceiver(ees.SelectThisEvent(eer,when,priority,i,m_execEventType));
                        //						exec.RequestEvent(eer,when,priority,i,m_execEventType);
                        //						exec.UnRequestEvents(ees);
                        break;
                    default:
                        exec.RequestEvent(new ExecEventReceiver(this.ExecEventRecieverUnRequestEventReceiver), when, priority, i, _execEventType);
                        break;
                };
            }


            Debug.WriteLine("");

            // AEL			exec.UnRequestEvents(new ExecEventSelectorByTargetType(this));

            exec.Start();

            Debug.WriteLine("");

            // test validation variable
            Assert.True(!_error, "Executive fired a unrequested event");

            Debug.WriteLine("");
        }

        #region TestExecutiveUnRequestSelector() support methods.

        private void ExecEventRecieverUnRequestEventReceiver(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("Primary firing event number" + (int)userData);
        }

        private void ExecEventRecieverUnRequestDelegate(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("ERROR: Primary firing unrequested event number" + (int)userData);
        }

        #endregion

        /// <summary>
        /// Checks to see that an executive can handle seperate threads.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that an executive can handle seperate threads.")]
        public void TestThreadSepFunctionality()
        {

            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime now = DateTime.Now;
            DateTime when;
            double priority;

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting detachable event service for " + when + ", at priority " + priority);
                exec.RequestEvent(new ExecEventReceiver(TimeSeparatedTask), when, priority, "Task " + i, ExecEventType.Detachable);
            }

            exec.Start();

            Debug.WriteLine("\r\n\r\n\r\nNow going to do it again after a 1.5 second pause.\r\n\r\n\r\n");
            System.Threading.Thread.Sleep(1500);

            exec = ExecFactory.Instance.CreateExecutive();
            now = DateTime.Now;

            for (int i = 0; i < NUM_EVENTS; i++)
            {
                when = new DateTime(now.Ticks + _random.Next());
                priority = _random.NextDouble();
                Debug.WriteLine("Primary requesting detachable event service for " + when + ", at priority " + priority);
                exec.RequestEvent(new ExecEventReceiver(TimeSeparatedTask), when, priority, "Task " + i, ExecEventType.Detachable);
            }

            exec.Start();

        }

        #region Test Times
        readonly string[] _testTimes = new string[]{"9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 7:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 12:21:18 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 8:00:00 AM",
                                             "9/1/1998 12:00:00 PM",
                                             "9/1/1998 12:00:00 PM",
                                             "9/1/1998 12:00:00 PM",
                                             "9/1/1998 12:27:31 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:29 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:26 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:24 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 12:27:23 AM",
                                             "9/1/1998 12:42:36 AM",
                                             "9/1/1998 3:48:41 AM",
                                             "9/1/1998 3:48:42 AM",
                                             "9/1/1998 3:48:44 AM",
                                             "9/1/1998 3:48:47 AM",
                                             "9/1/1998 3:48:49 AM",
                                             "9/1/1998 12:48:58 AM",
                                             "9/1/1998 1:03:54 AM",
                                             "9/1/1998 12:48:56 AM",
                                             "9/1/1998 1:03:54 AM",
                                             "9/1/1998 12:45:20 AM",
                                             "9/1/1998 1:06:52 AM",
                                             "9/1/1998 1:06:39 AM"};
        #endregion Test Times

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test the Heap collection.")]
        public void RecreateFailure()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.Core.ExecutiveFastLight, Sage", Guid.NewGuid());
            foreach (string s in _testTimes)
            {
                DateTime dt = DateTime.Parse(s);
                exec.RequestEvent(new ExecEventReceiver(DoIt), dt, 0.0, dt.ToString());
            }


            _lastNow = exec.Now;
            exec.Start();
        }

        DateTime _lastNow;
        private void DoIt(IExecutive exec, object userData)
        {
            string errMsg = "";
            if (exec.Now < _lastNow)
                errMsg = "<-- CAUSALITY VIOLATION!";
            _lastNow = exec.Now;
            Console.WriteLine("At " + exec.Now + ", servicing event that was requested for " + userData.ToString() + errMsg);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test different mechanisms for acquiring an instance of IExecutive.")]
        public void TestExecAcquisition()
        {

            // using Highpoint.Sage.Core;

            // Obtain an executive of the default type and unspecified Guid from the factory.
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive();
            // ... or ...
            // Obtain an executive of the default type and specified Guid from the factory.
            IExecutive exec2 = ExecFactory.Instance.CreateExecutive(Guid.NewGuid());
            // ... or ...
            // Obtain an executive of the specified type and Guid from the factory.
            IExecutive exec3 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.Core.Executive",
                                                                    Guid.NewGuid());
            // ... or ...
            // Obtain an executive of the specified type and Guid from the factory.
            IExecutive exec4 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.Core.ExecutiveFastLight",
                                                                    Guid.NewGuid());

            Console.WriteLine(exec1.ToString());
            Console.WriteLine(exec2.ToString());
            Console.WriteLine(exec3.ToString());
            Console.WriteLine(exec4.ToString());
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test the event that is supposed to fire each time the clock is supposed to change.")]
        public void TestClockAboutToChangeEvent()
        {

            IExecutive exec1 = ExecFactory.Instance.CreateExecutive();
            exec1.ClockAboutToChange += new ExecutiveEvent(exec1_ClockAboutToChange);
            _result = "";
            DateTime t0 = new DateTime(2007, 5, 16, 12, 34, 56);

            // Daemon event. Should never fire.
            exec1.RequestDaemonEvent(new ExecEventReceiver(MyExecEventReceiver2), new DateTime(2025, 12, 25), 0.0, null);

            int[] increments = new int[] { 1, 2, 3, 0, 0, 2, 0, 2, 3, 0, 0, 0, 4, 0 };
            foreach (int increment in increments)
            {
                exec1.RequestEvent(new ExecEventReceiver(MyExecEventReceiver2), t0, 0.0, null);
                t0 += TimeSpan.FromMinutes(increment);
            }

            exec1.Start();

            Console.WriteLine(_result);

            Assert.Equal("16/05/2007 12:34:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:34:56 PM, is about to change.\r\n16/05/2007 12:35:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:35:56 PM, is about to change.\r\n16/05/2007 12:37:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:37:56 PM, is about to change.\r\n16/05/2007 12:40:56 PM : Event is firing.\r\n16/05/2007 12:40:56 PM : Event is firing.\r\n16/05/2007 12:40:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:40:56 PM, is about to change.\r\n16/05/2007 12:42:56 PM : Event is firing.\r\n16/05/2007 12:42:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:42:56 PM, is about to change.\r\n16/05/2007 12:44:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:44:56 PM, is about to change.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n16/05/2007 12:47:56 PM : Event is firing.\r\n\tClock, currently at 16/05/2007 12:47:56 PM, is about to change.\r\n16/05/2007 12:51:56 PM : Event is firing.\r\n", _result);
        }

        private string _result = null;
        void exec1_ClockAboutToChange(IExecutive exec)
        {
            _result += ("\tClock, currently at " + getExecNow(exec) + ", is about to change.\r\n");
        }

        void MyExecEventReceiver2(IExecutive exec, object userData)
        {
            _result += (getExecNow(exec) + " : Event is firing.\r\n");
        }

        private string getExecNow(IExecutive exec)
        {
            return exec.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
        }

        private int _exec1_ExecutiveStarted_SingleShot_Count = 0;
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test the single shot event (fires once when the first event is about to fire, then unregisters.")]
        public void TestSingleShotEvent()
        {
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive();
            _exec1_ExecutiveStarted_SingleShot_Count = 0;
            exec1.ExecutiveStarted_SingleShot += new ExecutiveEvent(exec1_ExecutiveStarted_SingleShot);
            exec1.Start();
            exec1.Start();
            Debug.Assert(_exec1_ExecutiveStarted_SingleShot_Count == 1);
        }

        void exec1_ExecutiveStarted_SingleShot(IExecutive exec)
        {
            _exec1_ExecutiveStarted_SingleShot_Count++;
        }

        private int _eventCountCeil = 100000;
        private double _pctSynchronous = 0.50;
        private double[] _arrPctSynch = new double[] { 1.0, 0.5, 0.2, 0.1, 0.0 };
        private int[] _arrEcc = new int[] { 10000, 100000, 1000000 };

        private void TestPerformanceMultiple()
        {
            foreach (double pctSynch in _arrPctSynch)
            {
                _pctSynchronous = pctSynch;
                foreach (int ecc in _arrEcc)
                {
                    _eventCountCeil = ecc;
                    TestPerformance();
                }
            }
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test the Executive's capability to run fast with a range of event type mixes.")]
        public void TestPerformance()
        {
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.Core.Executive", Guid.NewGuid());
            Randoms.RandomServer rsvr = new Highpoint.Sage.Randoms.RandomServer(012345, 1000);
            Randoms.IRandomChannel rch = rsvr.GetRandomChannel(987654321, 1000);
            DateTime timeCursor = new DateTime(2009, 1, 1, 0, 0, 0);

            _eventCountCeil = 180000;
            _pctSynchronous = 0.0;

            for (int i = 0; i < 100; i++)
            {
                timeCursor += TimeSpan.FromMinutes(rch.NextDouble() * 100);
                exec1.RequestEvent(new ExecEventReceiver(PerfTestExecute), timeCursor, 0.0, rch);
            }
            DateTime then = DateTime.Now;
            exec1.Start();
            TimeSpan howLong = DateTime.Now - then;
            Console.WriteLine("Serviced " + exec1.EventCount + " events in " + howLong.TotalMilliseconds + " msec.");
        }

        private void PerfTestExecute(IExecutive exec, object userData)
        {
            Randoms.IRandomChannel rch = (Randoms.IRandomChannel)userData;
            if (rch.NextDouble() < 0.405 && exec.EventCount < _eventCountCeil)
            {
                int nNewEvents = rch.Next(1, 5);
                for (int i = 0; i < nNewEvents; i++)
                {
                    DateTime when = exec.Now + TimeSpan.FromMinutes(rch.NextDouble() * 100);
                    if (rch.NextDouble() < _pctSynchronous)
                    {
                        exec.RequestEvent(new ExecEventReceiver(PerfTestExecute), when, 0.0, rch);
                    }
                    else
                    {
                        exec.RequestEvent(new ExecEventReceiver(PerfTestExecute), when, 0.0, rch, ExecEventType.Detachable);
                    }
                }
            }
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Test the Executive's capability to execute a simple event join.")]
        public void TestEventJoinDetachable()
        {
            IExecutive exec1 = ExecFactory.Instance.CreateExecutive("Highpoint.Sage.Core.Executive", Guid.NewGuid());
            DateTime setupTime = new DateTime(2008, 11, 24, 12, 15, 44);
            ExecEventType eet = ExecEventType.Detachable; // Don't change this one. Join must be done on a detachable event.
            exec1.RequestEvent(new ExecEventReceiver(JoinDetachableSetup), setupTime, 0.0, null, eet);
            exec1.Start();

        }

        private void JoinDetachableSetup(IExecutive exec, object userData)
        {
            DateTime[] whens = new DateTime[3];
            whens[0] = new DateTime(2008, 11, 25, 12, 15, 44);
            whens[1] = new DateTime(2008, 11, 26, 12, 15, 44);
            whens[2] = new DateTime(2008, 11, 27, 12, 15, 44);
            List<long> eventKeys = new List<long>();
            ExecEventType eet = ExecEventType.Synchronous;
            for (int i = 0; i < 3; i++)
            {
                if (eet == ExecEventType.Synchronous)
                {
                    eventKeys.Add(exec.RequestEvent(new ExecEventReceiver(DoItWithoutSuspension), whens[i], 0.0, null, eet));
                }
                else if (eet == ExecEventType.Detachable)
                {
                    eventKeys.Add(exec.RequestEvent(new ExecEventReceiver(DoItWithSuspension), whens[i], 0.0, null, eet));
                }
            }
            Console.WriteLine(exec.Now + " : Waiting to join.");
            exec.Join(eventKeys.ToArray());
            Console.WriteLine(exec.Now + " : Done waiting to join.");
        }

        private void DoItWithSuspension(IExecutive exec, object userData)
        {
            Console.WriteLine(exec.Now + " : Starting \"DoItWithSuspension\"");
            exec.CurrentEventController.SuspendUntil(exec.Now + TimeSpan.FromMinutes(5.0));
            Console.WriteLine(exec.Now + " : Finished \"DoItWithSuspension\"");
        }

        private void DoItWithoutSuspension(IExecutive exec, object userData)
        {
            Console.WriteLine(exec.Now + " : Doing \"DoItWithoutSuspension\"");
        }

        // ── Collection-migration coverage tests ──────────────────────────────────
        // These tests document the behaviors of IExecutive.EventList and
        // IExecutive.LiveDetachableEvents that must survive the Phase 1 and Phase 2
        // collection migrations.

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that events queued on the executive appear in EventList in chronological order.")]
        public void TestEventListContainsQueuedEvents()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime t1 = new DateTime(2025, 1, 1, 0, 0, 0);
            DateTime t2 = t1.AddHours(1);
            DateTime t3 = t1.AddHours(2);

            exec.RequestEvent(new ExecEventReceiver((e, ud) => { }), t3, 0.0, "third");
            exec.RequestEvent(new ExecEventReceiver((e, ud) => { }), t1, 0.0, "first");
            exec.RequestEvent(new ExecEventReceiver((e, ud) => { }), t2, 0.0, "second");

            IReadOnlyList<IExecEvent> eventList = exec.EventList;
            Assert.Equal(3, eventList.Count);

            // EventList snapshot is sorted chronologically
            Assert.Equal(t1, eventList[0].When);
            Assert.Equal(t2, eventList[1].When);
            Assert.Equal(t3, eventList[2].When);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that EventList.IsReadOnly is true — callers cannot mutate the queue through the list reference.")]
        public void TestEventListIsReadOnly()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            exec.RequestEvent(new ExecEventReceiver((e, ud) => { }), new DateTime(2025, 6, 1), 0.0, null);

            IReadOnlyList<IExecEvent> eventList = exec.EventList;
            Assert.IsAssignableFrom<IList>(eventList);
            Assert.True(((IList)eventList).IsReadOnly, "EventList must be read-only so callers cannot corrupt the event queue");
        }

        // Fields used by TestLiveDetachableEventsContainsRunningEvent
        private int _liveDetachCountDuringEvent = -1;

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that LiveDetachableEvents contains the running event during execution and is empty after the executive completes.")]
        public void TestLiveDetachableEventsContainsRunningEvent()
        {
            _liveDetachCountDuringEvent = -1;
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            DateTime when = new DateTime(2025, 3, 15, 10, 0, 0);
            exec.RequestEvent(new ExecEventReceiver(CaptureDetachableCount), when, 0.0, null, ExecEventType.Detachable);
            exec.Start();

            Assert.Equal(1, _liveDetachCountDuringEvent);
            Assert.Empty(exec.LiveDetachableEvents);
        }

        private void CaptureDetachableCount(IExecutive exec, object userData)
        {
            _liveDetachCountDuringEvent = exec.LiveDetachableEvents.Count;
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that LiveDetachableEvents.IsReadOnly is true — callers cannot mutate the live-event list.")]
        public void TestLiveDetachableEventsIsReadOnly()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            Assert.IsAssignableFrom<IList>(exec.LiveDetachableEvents);
            Assert.True(((IList)exec.LiveDetachableEvents).IsReadOnly, "LiveDetachableEvents must be read-only so callers cannot corrupt the running-event list");
        }

        // ── Phase 2 prep tests ────────────────────────────────────────────────────
        // These tests are marked [Ignore] because they require the public API type
        // changes in Phase 2 (ArrayList → IReadOnlyList<T>).  They will remain
        // ignored (and compilable) until those changes are applied.

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Phase 2: Verifies that IExecutive.EventList is typed as IReadOnlyList<IExecEvent>.")]
        public void TestEventListTypedAsIReadOnlyList()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            // After Phase 2 this cast must succeed; currently EventList returns IList.
            Assert.IsAssignableFrom<IReadOnlyList<IExecEvent>>(exec.EventList);
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Phase 2: Verifies that IExecutive.LiveDetachableEvents is typed as IReadOnlyList<IDetachableEventController>.")]
        public void TestLiveDetachableEventsTypedAsIReadOnlyList()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            // After Phase 2 this cast must succeed; currently LiveDetachableEvents returns ArrayList.
            Assert.IsAssignableFrom<IReadOnlyList<IDetachableEventController>>(exec.LiveDetachableEvents);
        }

        #region Internal Methods

        private void TimeSeparatedTask(IExecutive exec, object userData)
        {
            IDetachableEventController dec = exec.CurrentEventController;

            Debug.WriteLine(exec.Now + " : " + userData.ToString() + " performing initialization of detachable task on thread " + System.Threading.Thread.CurrentThread.GetHashCode());

            while (_random.Next(3) < 2)
            {

                DateTime when = exec.Now + TimeSpan.FromDays(1.5);

                Debug.WriteLine("Suspending task until " + when);

                dec.SuspendUntil(when);

                Debug.WriteLine(exec.Now + " : " + userData.ToString() + " performing continuation of detachable task on thread " + System.Threading.Thread.CurrentThread.GetHashCode());
            }
        }

        private void MyExecEventReceiver(IExecutive exec, object userData)
        {
            if (_random.NextDouble() > .15)
            {
                DateTime when = new DateTime(exec.Now.Ticks + _random.Next());
                Debug.WriteLine("Secondary requesting event service for " + when + ".");
                exec.RequestEvent(new ExecEventReceiver(MyExecEventReceiver), when, _random.NextDouble(), null, _execEventType);
            }

            Debug.WriteLine("Running event at time " + exec.Now + ", and priority level " + exec.CurrentPriorityLevel + " on thread " + System.Threading.Thread.CurrentThread.GetHashCode());

            //if ( m_random.NextDouble() < .05 ) {
            //    Debug.WriteLine("Putting task to sleep at time " + exec.Now + ".");
            //Thread.CurrentThread.Suspend();
            //    Thread.Sleep(1000);
            //}
        }

        /// <summary>
        /// Test 1: Verifies that exceptions thrown in event handlers propagate out of Start().
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that exceptions thrown in event handlers propagate out of Start().")]
        public void Executive_EventHandlerException_PropagatesToCaller()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            bool eventFired = false;

            exec.RequestEvent((e, userData) =>
            {
                eventFired = true;
                throw new InvalidOperationException("Test exception from handler");
            }, DateTime.MinValue + TimeSpan.FromMinutes(10), 0.0, null, ExecEventType.Synchronous);

            RuntimeException ex = Assert.Throws<RuntimeException>(() => exec.Start());
            Assert.True(eventFired, "Event should have fired before exception was thrown");
            Assert.NotNull(ex.InnerException);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        /// <summary>
        /// Test 2 & 3 combined: Verifies causality violation behavior (throw vs ignore).
        /// NOTE: Due to Executive's static _ignoreCausalityViolations field, these must be tested together.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies causality violation behavior - throw when not ignored, silent when ignored.")]
        public void Executive_CausalityViolation_BehaviorTest()
        {
            lock (_execFactoryLock)
            {
                // PART 1: Test that causality violations throw when not ignored
                ExecFactory.Configure(new ExecFactoryOptions(), new ExecutiveOptions { IgnoreCausalityViolations = false });
                
                // Reset the ExecFactory singleton to pick up new configuration
                ResetExecFactorySingleton();

                try
                {
                    IExecutive exec1 = ExecFactory.Instance.CreateExecutive();

                    bool outerEventFired = false;

                    DateTime futureTime = DateTime.MinValue + TimeSpan.FromMinutes(100);
                    DateTime pastTime = DateTime.MinValue + TimeSpan.FromMinutes(50);

                    exec1.RequestEvent((e, userData) =>
                    {
                        outerEventFired = true;
                        // Try to schedule an event in the past (causality violation)
                        e.RequestEvent((innerExec, innerData) => { }, pastTime, 0.0, null, ExecEventType.Synchronous);
                    }, futureTime, 0.0, null, ExecEventType.Synchronous);

                    // The CausalityException will be caught by Executive and re-thrown as RuntimeException
                    RuntimeException ex = Assert.Throws<RuntimeException>(() => exec1.Start());
                    Assert.True(outerEventFired, "Outer event should have fired");
                    Assert.NotNull(ex.InnerException);
                    Assert.IsType<CausalityException>(ex.InnerException);
                }
                finally
                {
                    // Restore default setting for subsequent tests
                    ExecFactory.Configure(new ExecFactoryOptions(), new ExecutiveOptions { IgnoreCausalityViolations = true });
                    ResetExecFactorySingleton();
                }

                // PART 2: Test that causality violations are silently ignored when option is set
                IExecutive exec2 = ExecFactory.Instance.CreateExecutive();

                bool outerEventFired2 = false;
                bool innerEventFired2 = false;

                DateTime futureTime2 = DateTime.MinValue + TimeSpan.FromMinutes(100);
                DateTime pastTime2 = DateTime.MinValue + TimeSpan.FromMinutes(50);

                exec2.RequestEvent((e, userData) =>
                {
                    outerEventFired2 = true;
                    // Try to schedule an event in the past - should be silently ignored
                    e.RequestEvent((innerExec, innerData) =>
                    {
                        innerEventFired2 = true;
                    }, pastTime2, 0.0, null, ExecEventType.Synchronous);
                }, futureTime2, 0.0, null, ExecEventType.Synchronous);

                exec2.Start();

                Assert.True(outerEventFired2, "Outer event should have fired (part 2)");
                // Inner event should NOT fire because it was in the past and was ignored
                Assert.False(innerEventFired2, "Inner event should not have fired (silently ignored)");
                Assert.Equal(ExecState.Finished, exec2.State);
            }
        }

        /// <summary>
        /// Helper method to reset ExecFactory singleton using reflection.
        /// Required to force ExecFactory to recreate with new configuration.
        /// </summary>
        private void ResetExecFactorySingleton()
        {
            var field = typeof(ExecFactory).GetField("_instance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (field != null)
            {
                field.SetValue(null, null);
            }
        }

        /// <summary>
        /// Test 4: Verifies that Reset clears the queue and resets Now to initial state.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that Reset clears the queue and resets Now to initial state.")]
        public void Executive_Reset_ClearsQueueAndResetsNow()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            
            int eventCount = 0;
            DateTime eventTime = DateTime.MinValue + TimeSpan.FromMinutes(100);

            // Schedule some events
            exec.RequestEvent((e, userData) => { eventCount++; }, eventTime, 0.0, null, ExecEventType.Synchronous);
            exec.RequestEvent((e, userData) => { eventCount++; }, eventTime + TimeSpan.FromMinutes(10), 0.0, null, ExecEventType.Synchronous);

            exec.Start();
            Assert.Equal(2, eventCount);
            Assert.True(exec.Now > DateTime.MinValue, "Executive should have advanced in time");

            // Reset the executive
            exec.Reset();

            // Verify state after reset
            Assert.Equal(DateTime.MinValue, exec.Now);
            Assert.Equal(ExecState.Stopped, exec.State);

            // Schedule a new event after reset and verify it fires
            eventCount = 0;
            exec.RequestEvent((e, userData) => { eventCount++; }, DateTime.MinValue + TimeSpan.FromMinutes(50), 0.0, null, ExecEventType.Synchronous);
            exec.Start();
            Assert.Equal(1, eventCount);
        }

        /// <summary>
        /// Test 5: Verifies that events at the same time and priority are dispatched in submission order (FIFO).
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that events at the same time and priority are dispatched in submission order (FIFO).")]
        public void Executive_SameTimeEqualPriority_DispatchedBySubmissionOrder()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            
            List<int> executionOrder = new List<int>();
            DateTime when = DateTime.MinValue + TimeSpan.FromMinutes(100);
            double priority = 5.0;

            // Schedule 5 events at the same time with the same priority
            for (int i = 0; i < 5; i++)
            {
                int index = i; // Capture for closure
                exec.RequestEvent((e, userData) =>
                {
                    executionOrder.Add((int)userData);
                }, when, priority, index, ExecEventType.Synchronous);
            }

            exec.Start();

            // Verify they executed in submission order (FIFO)
            Assert.Equal(5, executionOrder.Count);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, executionOrder);
        }

        /// <summary>
        /// Test 6: Verifies that an executive with an empty queue starts and finishes gracefully.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that an executive with an empty queue starts and finishes gracefully.")]
        public void Executive_EmptyQueue_StartsAndFinishesGracefully()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();
            
            // Start with no events scheduled
            exec.Start();

            // Verify graceful completion
            Assert.Equal(ExecState.Finished, exec.State);
            Assert.Equal(DateTime.MinValue, exec.Now);
        }

        /// <summary>
        /// Test 7: Verifies that the same seed produces deterministic, identical output.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that the same seed produces deterministic, identical output.")]
        public void Executive_Determinism_SameSeedProducesSameOutput()
        {
            List<(DateTime when, int userData)> run1 = new List<(DateTime, int)>();
            List<(DateTime when, int userData)> run2 = new List<(DateTime, int)>();

            // Run 1
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();
                Random rng = new Random(42);

                for (int i = 0; i < 10; i++)
                {
                    int index = i;
                    DateTime when = DateTime.MinValue + TimeSpan.FromMinutes(rng.Next(1, 1000));
                    double priority = rng.NextDouble();
                    
                    exec.RequestEvent((e, userData) =>
                    {
                        run1.Add((e.Now, (int)userData));
                    }, when, priority, index, ExecEventType.Synchronous);
                }

                exec.Start();
            }

            // Run 2 - identical scenario
            {
                IExecutive exec = ExecFactory.Instance.CreateExecutive();
                Random rng = new Random(42);

                for (int i = 0; i < 10; i++)
                {
                    int index = i;
                    DateTime when = DateTime.MinValue + TimeSpan.FromMinutes(rng.Next(1, 1000));
                    double priority = rng.NextDouble();
                    
                    exec.RequestEvent((e, userData) =>
                    {
                        run2.Add((e.Now, (int)userData));
                    }, when, priority, index, ExecEventType.Synchronous);
                }

                exec.Start();
            }

            // Verify both runs produced identical sequences
            Assert.Equal(run1.Count, run2.Count);
            for (int i = 0; i < run1.Count; i++)
            {
                Assert.Equal(run1[i].when, run2[i].when);
                Assert.Equal(run1[i].userData, run2[i].userData);
            }
        }

        // ── Priority 2 executive tests ────────────────────────────────────────────
        // These 8 tests cover RequestImmediateEvent, ResubmitEventAtTime,
        // UnRequestEvents with a selector, empty-queue removal, daemon event
        // behavior, Pause/Resume, ExecState lifecycle, and post-Finished RequestEvent.

        /// <summary>
        /// P2-Test 1: Verifies that RequestImmediateEvent schedules at the current exec time,
        /// causing it to fire before a previously-queued future event.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that RequestImmediateEvent fires before a future-scheduled event already in queue.")]
        public void Executive_RequestImmediateEvent_FiresBeforeQueuedFutureEvents()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            List<string> firingOrder = new List<string>();
            DateTime t50  = DateTime.MinValue + TimeSpan.FromMinutes(50);
            DateTime t100 = DateTime.MinValue + TimeSpan.FromMinutes(100);

            // Pre-queue an event at T+100.
            exec.RequestEvent((e, ud) => { firingOrder.Add("future-T100"); },
                t100, 0.0, null, ExecEventType.Synchronous);

            // At T+50, request an immediate event (effective time == exec.Now == T+50).
            exec.RequestEvent((e, ud) =>
            {
                e.RequestImmediateEvent((ie, iud) => { firingOrder.Add("immediate"); },
                    null, ExecEventType.Synchronous);
                firingOrder.Add("trigger-T50");
            }, t50, 0.0, null, ExecEventType.Synchronous);

            exec.Start();

            Assert.Equal(3, firingOrder.Count);
            // Order must be: trigger fires at T+50, then immediate (also at T+50, same priority
            // but submitted right after trigger so next FIFO), then the T+100 future event.
            Assert.Equal("trigger-T50",  firingOrder[0]);
            Assert.Equal("immediate",    firingOrder[1]);
            Assert.Equal("future-T100",  firingOrder[2]);
        }

        /// <summary>
        /// P2-Test 2: Verifies that ResubmitEventAtTime moves a queued event to a new time.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that ResubmitEventAtTime reschedules a queued event to the new time.")]
        public void Executive_ResubmitEventAtTime_ReschedulesEvent()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            DateTime t50  = DateTime.MinValue + TimeSpan.FromMinutes(50);
            DateTime t100 = DateTime.MinValue + TimeSpan.FromMinutes(100);
            DateTime t200 = DateTime.MinValue + TimeSpan.FromMinutes(200);

            List<DateTime> firedAt = new List<DateTime>();

            // Event B will be resubmitted to T+200 from event A's handler.
            long eventBKey = exec.RequestEvent((e, ud) => { firedAt.Add(e.Now); },
                t100, 0.0, "B", ExecEventType.Synchronous);

            exec.RequestEvent((e, ud) =>
            {
                // Resubmit event B (which is still in queue) to T+200, keeping the old one too.
                e.ResubmitEventAtTime(eventBKey, t200, deleteOldOne: false);
            }, t50, 0.0, "A", ExecEventType.Synchronous);

            exec.Start();

            // Event B should fire twice: once at T+100 (original) and once at T+200 (resubmitted).
            Assert.Equal(2, firedAt.Count);
            Assert.Equal(t100, firedAt[0]);
            Assert.Equal(t200, firedAt[1]);
        }

        /// <summary>
        /// P2-Test 3: Verifies that UnRequestEvents with an IExecEventSelector removes only the
        /// targeted events, leaving the rest to fire normally.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that UnRequestEvents with a selector removes only events matching the selector's predicate.")]
        public void Executive_UnRequestEvent_WithEventSelector()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            List<string> firedUserData = new List<string>();
            DateTime t = DateTime.MinValue + TimeSpan.FromMinutes(10);
            ExecEventReceiver callback = (e, ud) => { firedUserData.Add((string)ud); };

            exec.RequestEvent(callback, t,                        0.0, "keep-A",  ExecEventType.Synchronous);
            exec.RequestEvent(callback, t + TimeSpan.FromMinutes(1), 0.0, "remove-1", ExecEventType.Synchronous);
            exec.RequestEvent(callback, t + TimeSpan.FromMinutes(2), 0.0, "keep-B",  ExecEventType.Synchronous);
            exec.RequestEvent(callback, t + TimeSpan.FromMinutes(3), 0.0, "remove-2", ExecEventType.Synchronous);

            // Selector: target events whose userData string starts with "remove".
            IExecEventSelector selector = new UserDataPrefixSelector("remove");
            exec.UnRequestEvents(selector);

            exec.Start();

            Assert.Equal(2, firedUserData.Count);
            Assert.Contains("keep-A", firedUserData);
            Assert.Contains("keep-B", firedUserData);
            Assert.DoesNotContain("remove-1", firedUserData);
            Assert.DoesNotContain("remove-2", firedUserData);
        }

        /// <summary>
        /// P2-Test 4: Verifies that calling UnRequestEvents on an executive with no events
        /// (using a selector that matches nothing) does not throw any exception.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that UnRequestEvents with a selector on an empty queue does not throw.")]
        public void Executive_UnRequestEvent_OnEmptyQueue_DoesNotThrow()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            // No events scheduled — selector matches nothing.
            IExecEventSelector selector = new UserDataPrefixSelector("anything");
            exec.UnRequestEvents(selector); // Queues the removal.

            // Start with empty queue — removal processes harmlessly.
            var ex = Record.Exception(() => exec.Start());
            Assert.Null(ex);
            Assert.Equal(ExecState.Finished, exec.State);
        }

        /// <summary>
        /// P2-Test 5: Verifies that daemon events do NOT fire when they are the only events
        /// remaining in the queue — the executive exits the dispatch loop without firing them.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that daemon events are skipped when they are the only remaining events — they do not keep the simulation alive.")]
        public void Executive_DaemonEvent_FiresWhenQueueEmptied()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            bool regularFired = false;
            bool daemonFired  = false;

            DateTime t100 = DateTime.MinValue + TimeSpan.FromMinutes(100);
            DateTime t200 = DateTime.MinValue + TimeSpan.FromMinutes(200);

            exec.RequestEvent((e, ud) => { regularFired = true; },
                t100, 0.0, null, ExecEventType.Synchronous);

            exec.RequestDaemonEvent((e, ud) => { daemonFired = true; },
                t200, 0.0, null);

            exec.Start();

            // Regular event fires; daemon event must NOT fire (only daemons remain after T+100).
            Assert.True(regularFired,  "Regular event should have fired.");
            Assert.False(daemonFired,  "Daemon event must NOT fire when it is the only event remaining.");
            Assert.Equal(ExecState.Finished, exec.State);
        }

        /// <summary>
        /// P2-Test 6: Verifies that Pause() mid-simulation suspends dispatch and Resume() continues it.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that Pause() halts event dispatch mid-simulation and Resume() allows it to complete.")]
        public void Executive_PauseAndResume_ContinuesCorrectly()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            int firedCount = 0;
            var eventOneFired = new System.Threading.ManualResetEventSlim(false);
            DateTime t = DateTime.MinValue + TimeSpan.FromMinutes(10);

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                exec.RequestEvent((e, ud) =>
                {
                    firedCount++;
                    // Signal after event 1 fires, then pause immediately.
                    if (idx == 1)
                    {
                        eventOneFired.Set();
                        e.Pause();
                        // Brief sleep so PauseManager thread can acquire _runLock
                        // before this handler returns and the exec loop continues.
                        Thread.Sleep(50);
                    }
                }, t + TimeSpan.FromMinutes(idx * 10), 0.0, null, ExecEventType.Synchronous);
            }

            Thread execThread = new Thread(() => exec.Start());
            execThread.IsBackground = true;
            execThread.Start();

            // Wait for event 1 to fire and the pause to be requested.
            Assert.True(eventOneFired.Wait(TimeSpan.FromSeconds(5)), "Event 1 should have fired within 5 s");

            // Wait for state to transition to Paused.
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (exec.State != ExecState.Paused && DateTime.UtcNow < deadline)
                Thread.Sleep(5);

            Assert.Equal(ExecState.Paused, exec.State);

            // Events 0 and 1 must have fired; events 2 and 3 must not have.
            Assert.Equal(2, firedCount);

            exec.Resume();
            execThread.Join(TimeSpan.FromSeconds(5));

            Assert.Equal(4, firedCount);
            Assert.Equal(ExecState.Finished, exec.State);
        }

        /// <summary>
        /// P2-Test 7: Verifies that ExecState transitions correctly through the full lifecycle:
        /// Stopped → Running → Finished → (after Reset) → Stopped.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies ExecState transitions through Stopped, Running, Finished, and back to Stopped after Reset.")]
        public void Executive_ExecState_TransitionsThroughLifecycle()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            // Initial state.
            Assert.Equal(ExecState.Stopped, exec.State);

            ExecState? stateInsideHandler = null;

            exec.RequestEvent((e, ud) =>
            {
                stateInsideHandler = e.State;
            }, DateTime.MinValue + TimeSpan.FromMinutes(10), 0.0, null, ExecEventType.Synchronous);

            exec.Start();

            // During event dispatch the state must have been Running.
            Assert.Equal(ExecState.Running, stateInsideHandler);

            // After Start() returns the state must be Finished.
            Assert.Equal(ExecState.Finished, exec.State);

            // After Reset() the state must return to Stopped.
            exec.Reset();
            Assert.Equal(ExecState.Stopped, exec.State);
        }

        /// <summary>
        /// P2-Test 8: Verifies that calling RequestEvent on a Finished executive throws
        /// ApplicationException (as documented in Executive.cs line ~359).
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that RequestEvent on a Finished executive throws ApplicationException.")]
        public void Executive_RequestEvent_AfterFinished_ThrowsOrIgnores()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            // Start with no events — exec immediately reaches Finished.
            exec.Start();
            Assert.Equal(ExecState.Finished, exec.State);

            // Attempting to schedule a new event on a Finished executive must throw.
            ApplicationException ex = Assert.Throws<ApplicationException>(() =>
                exec.RequestEvent((e, ud) => { },
                    DateTime.MinValue + TimeSpan.FromMinutes(10), 0.0, null,
                    ExecEventType.Synchronous));

            Assert.Contains("Finished", ex.Message, StringComparison.Ordinal);
        }

        #region Priority 3 — Nice-to-have tests

        /// <summary>
        /// P3-Test 1: Verifies that calling Abort() from inside a synchronous event handler stops all
        /// subsequent event dispatch. Events scheduled after the aborted point must not execute.
        /// Start() must return without throwing, and State must be Finished.
        /// Abort() internally calls Reset(), so Now is DateTime.MinValue after Start() returns.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that Abort() called from inside a handler stops all subsequent event dispatch and leaves the executive in Finished state.")]
        public void Executive_Abort_FromHandler_StopsSimulation()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            bool t1Fired = false;
            bool t3Fired = false;

            DateTime t1 = DateTime.MinValue + TimeSpan.FromMinutes(10);
            DateTime t2 = DateTime.MinValue + TimeSpan.FromMinutes(20);
            DateTime t3 = DateTime.MinValue + TimeSpan.FromMinutes(30);

            exec.RequestEvent((e, ud) => { t1Fired = true; }, t1, 0.0, null, ExecEventType.Synchronous);
            exec.RequestEvent((e, ud) => { e.Abort(); },      t2, 0.0, null, ExecEventType.Synchronous);
            exec.RequestEvent((e, ud) => { t3Fired = true; }, t3, 0.0, null, ExecEventType.Synchronous);

            // Abort() sets _abortRequested; RuntimeException is NOT re-thrown when _abortRequested is true.
            exec.Start();

            Assert.True(t1Fired,  "Event at T1 (before abort) must have fired.");
            Assert.False(t3Fired, "Event at T3 (after abort) must NOT fire.");
            Assert.Equal(ExecState.Finished, exec.State);
            // Abort() calls Reset() which sets _now = DateTime.MinValue.
            Assert.Equal(DateTime.MinValue, exec.Now);
        }

        /// <summary>
        /// P3-Test 2: Verifies that exec.Now advances precisely to the scheduled dispatch time
        /// at each event invocation, and that exec.Now returns DateTime.MinValue after Reset().
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that exec.Now equals the scheduled dispatch time inside each handler, and resets to DateTime.MinValue after Reset().")]
        public void Executive_Now_AdvancesToMatchScheduledEventTime()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            DateTime t1 = DateTime.MinValue + TimeSpan.FromMinutes(100);
            DateTime t2 = DateTime.MinValue + TimeSpan.FromMinutes(200);
            DateTime t3 = DateTime.MinValue + TimeSpan.FromMinutes(300);

            DateTime? capturedT1 = null, capturedT2 = null, capturedT3 = null;

            exec.RequestEvent((e, ud) => { capturedT1 = e.Now; }, t1, 0.0, null, ExecEventType.Synchronous);
            exec.RequestEvent((e, ud) => { capturedT2 = e.Now; }, t2, 0.0, null, ExecEventType.Synchronous);
            exec.RequestEvent((e, ud) => { capturedT3 = e.Now; }, t3, 0.0, null, ExecEventType.Synchronous);

            exec.Start();

            Assert.Equal(t1, capturedT1);
            Assert.Equal(t2, capturedT2);
            Assert.Equal(t3, capturedT3);

            exec.Reset();
            Assert.Equal(DateTime.MinValue, exec.Now);
        }

        /// <summary>
        /// P3-Test 3: Verifies that EventCount accurately tracks the number of events dispatched
        /// on each run and that it restarts from zero at the beginning of every new run.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that EventCount equals the number of events dispatched on a run, and restarts from zero on each new run after Reset().")]
        public void Executive_EventCount_TracksAllFiredEvents()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            const int firstRunEvents = 5;
            for (int i = 0; i < firstRunEvents; i++)
            {
                exec.RequestEvent((e, ud) => { },
                    DateTime.MinValue + TimeSpan.FromMinutes(i + 1), 0.0, null, ExecEventType.Synchronous);
            }

            exec.Start();
            Assert.Equal((uint)firstRunEvents, exec.EventCount);

            exec.Reset();

            const int secondRunEvents = 3;
            for (int i = 0; i < secondRunEvents; i++)
            {
                exec.RequestEvent((e, ud) => { },
                    DateTime.MinValue + TimeSpan.FromMinutes(i + 1), 0.0, null, ExecEventType.Synchronous);
            }

            exec.Start();
            Assert.Equal((uint)secondRunEvents, exec.EventCount);
        }

        /// <summary>
        /// P3-Test 4: Verifies that RunNumber is -1 before any run and increments by one
        /// on each Start() call after Reset().
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that RunNumber is -1 before the first run and increments by 1 on each Start() after Reset().")]
        public void Executive_RunNumber_IncrementsAcrossMultipleRuns()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            Assert.Equal(-1, exec.RunNumber);

            exec.Start(); // First run (empty queue — finishes immediately).
            Assert.Equal(0, exec.RunNumber);

            exec.Reset();
            exec.RequestEvent((e, ud) => { },
                DateTime.MinValue + TimeSpan.FromMinutes(1), 0.0, null, ExecEventType.Synchronous);
            exec.Start(); // Second run.
            Assert.Equal(1, exec.RunNumber);

            exec.Reset();
            exec.RequestEvent((e, ud) => { },
                DateTime.MinValue + TimeSpan.FromMinutes(1), 0.0, null, ExecEventType.Synchronous);
            exec.Start(); // Third run.
            Assert.Equal(2, exec.RunNumber);
        }

        /// <summary>
        /// P3-Test 5: Verifies that CurrentEventType is None before dispatch begins, equals
        /// Synchronous while a synchronous handler is executing, and returns to None afterward.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Verifies that CurrentEventType returns None before/after dispatch and Synchronous while a synchronous handler is executing.")]
        public void Executive_CurrentEventType_IsSynchronousDuringHandler()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            ExecEventType capturedBefore = exec.CurrentEventType;
            ExecEventType? capturedDuring = null;

            exec.RequestEvent((e, ud) =>
            {
                capturedDuring = e.CurrentEventType;
            }, DateTime.MinValue + TimeSpan.FromMinutes(1), 0.0, null, ExecEventType.Synchronous);

            exec.Start();

            Assert.Equal(ExecEventType.None,        capturedBefore);
            Assert.Equal(ExecEventType.Synchronous, capturedDuring);
            Assert.Equal(ExecEventType.None,        exec.CurrentEventType);
        }

        /// <summary>
        /// P3-Test 6: Schedules 1000 events at random times and verifies all fire in
        /// non-decreasing time order and that EventCount matches the total scheduled.
        /// This validates the heap-based priority queue under high load.
        /// </summary>
        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("Schedules 1000 events at random times and verifies all fire in correct non-decreasing time order with correct EventCount.")]
        public void Executive_LargeVolume_EventsFireInCorrectTimeOrder()
        {
            IExecutive exec = ExecFactory.Instance.CreateExecutive();

            const int N = 1000;
            var rng = new Random(42);
            var firingTimes = new List<DateTime>(N);

            for (int i = 0; i < N; i++)
            {
                DateTime t = DateTime.MinValue + TimeSpan.FromMinutes(rng.Next(1, 100_000));
                exec.RequestEvent((e, ud) =>
                {
                    firingTimes.Add(e.Now);
                }, t, 0.0, null, ExecEventType.Synchronous);
            }

            exec.Start();

            Assert.Equal(N,      firingTimes.Count);
            Assert.Equal((uint)N, exec.EventCount);

            for (int i = 1; i < firingTimes.Count; i++)
            {
                Assert.True(firingTimes[i] >= firingTimes[i - 1],
                    $"Event {i} fired at {firingTimes[i]} before event {i - 1} at {firingTimes[i - 1]}");
            }
        }

        #endregion Priority 3

        #endregion
    }

    /// <summary>
    /// IExecEventSelector implementation used by Priority 2 tests.
    /// Selects events whose userData is a string starting with the given prefix.
    /// </summary>
    internal class UserDataPrefixSelector : IExecEventSelector
    {
        private readonly string _prefix;
        public UserDataPrefixSelector(string prefix) { _prefix = prefix; }

        public bool SelectThisEvent(ExecEventReceiver eer, DateTime when, double priority, object userData, ExecEventType eet)
            => userData is string s && s.StartsWith(_prefix, StringComparison.Ordinal);
    }

    public class OtherTarget
    {

        ArrayList _validateUnRequest;
        bool _error;

        public OtherTarget(ArrayList pvalidateUnRequest, bool perror)
        {
            _validateUnRequest = pvalidateUnRequest;
            _error = perror;
        }

        public void ExecEventRecieverUnRequestEventReceiver(IExecutive exec, object userData)
        {
            if (_validateUnRequest.Contains(userData))
            {
                _error = true;
            }
            Debug.WriteLine("ERROR: Primary firing unrequested event number" + (int)userData);
        }

    }

    public class ExecEventSelectorByTargetType : IExecEventSelector
    {

        private Type _type;

        public ExecEventSelectorByTargetType(System.Type targetType)
        {

            _type = targetType;

        }

        #region IExecEventSelector Members

        public bool SelectThisEvent(Highpoint.Sage.Core.ExecEventReceiver eer, DateTime when, double priority, object userData, Highpoint.Sage.Core.ExecEventType eet)
        {

            return (_type.Equals(eer.Target.GetType()));

        }

        #endregion

    }

}

