/* This source code licensed under the GNU Affero General Public License */

using Xunit;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.Core
{


    public class DiscreteTester : IDisposable
    {
        public DiscreteTester()
        {
            Init();
        }

        private int _dotick = 31;
        private DateTime _timelast = new DateTime();
        private TimeSpan _timedifference = TimeSpan.FromMinutes(10);


        private void Init()
        {
        }

        public void Dispose()
        {
            Debug.WriteLine("Done.");
        }

        [Fact]
        [Highpoint.Sage.Utility.FieldDescription("This test check if a defined metronome fires the defined amount of events in the correct constant time difference")]
        public void TestDiscreteModel()
        {
            Model model = new Model();

            SimpleMetronome sm = SimpleMetronome.CreateMetronome(model.Executive, DateTime.Now, DateTime.Now + TimeSpan.FromHours(5), _timedifference);
            sm.TickEvent += sm_TickEvent;

            model.Start();

            Assert.True(_dotick == 0, "Tick event did not fire 30 times");
        }

        private void sm_TickEvent(IExecutive exec, object userData)
        {
            Console.WriteLine($"{exec.Now}, {_timelast}, {_timedifference}");
            if (_timelast > DateTime.MinValue)
            {
                Assert.True(_timelast + _timedifference == exec.Now, "Tick does not happen at correct time difference");
            }
            Debug.WriteLine(exec.Now + " : Tick happened.");
            _dotick--;
            _timelast = exec.Now;
        }
    }
}

