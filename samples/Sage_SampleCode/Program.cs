/* This source code licensed under the GNU Affero General Public License */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Highpoint.Sage.Examples
{
    static class Program
    {

        static void Main(string[] args)
        {

            Demonstrate(Highpoint.Sage.Examples.Executive.SynchronousEvents.HelloWorld.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.SynchronousEvents.TwoCallbacksOutOfSequence.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.SynchronousEvents.CallbacksWithPriorities.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.SynchronousEvents.UserData_FollowOn_SelfImposedDelay.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.SynchronousEvents.ExecCatchesRuntimeExceptionFromSynchronousEvent.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.SynchronousEvents.RescindingSynchEvent.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.SynchronousEvents.MoreRescindingPlusAgentBased.Run);

            Demonstrate(Highpoint.Sage.Examples.Executive.DetachableEvents.BasicWithSuspends.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.DetachableEvents.SuspendsWithMixedModeAgents.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.DetachableEvents.UsesJoining.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.DetachableEvents.RescindMultipleDetachables.Run);

            Demonstrate(Highpoint.Sage.Examples.Executive.AdvancedTopics.Metronomes.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.AdvancedTopics.PauseAndResume.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.AdvancedTopics.UseExecController.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.AdvancedTopics.ExecEventModelAndStates.Run);
            Demonstrate(Highpoint.Sage.Examples.Executive.AdvancedTopics.DaemonEvents.Run);

            Demonstrate(Highpoint.Sage.Examples.StateManagement.InAgents.Run);
            Demonstrate(Highpoint.Sage.Examples.StateManagement.InUserData.Run);
            Demonstrate(Highpoint.Sage.Examples.StateManagement.OnTheStackFrame.Run);

            Demonstrate(Highpoint.Sage.Examples.RandomServer.SimpleDefaultServer.Run);
            Demonstrate(Highpoint.Sage.Examples.RandomServer.DecorrellatedActivities.Run);

            Demonstrate(Highpoint.Sage.Examples.StateMachine.Basic.Default.Run);
            Demonstrate(Highpoint.Sage.Examples.StateMachine.Basic.SimpleCustomWithInitialization.Run);
            Demonstrate(Highpoint.Sage.Examples.StateMachine.Basic.SimpleEnumStateMachine.Run);

            Demonstrate(Highpoint.Sage.Examples.Model.Basic.DefaultModel.Run);
            Demonstrate(Highpoint.Sage.Examples.Model.Basic.SimpleCustomWithInitialization.Run);

            Demonstrate(Highpoint.Sage.Examples.Resources.Basic.ServicePoolExample.Run);
            Demonstrate(Highpoint.Sage.Examples.Resources.Basic.ServicePoolExampleWithSynchronousEvents.Run);
            Demonstrate(Highpoint.Sage.Examples.Resources.Advanced.OptimalResourceAcquisition.Run);

            Demonstrate(Highpoint.Sage.Examples.SequenceControl.Basic.TaskGraphDemo.Run);

            outputDocumentation();
        }

        private static void outputDocumentation()
        {
            if(_collectDocs == null)
                return;

            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            File.WriteAllText(Path.Combine(assemblyDirectory, "sampledocs.txt"), _sb.ToString());
        }

        private static bool _prompts = false;
        private static readonly string _markerLine = new string('-', 79);

        private static void Demonstrate(Action run)
        {
            _collectDocs?.Invoke(run); // Collect documentation only if s_collectDocs isn't null.

            if (!_prompts)
                Console.WriteLine(_markerLine);

            string demoName = run.Method.DeclaringType?.Name;
            if (_prompts)
            {
                Console.Clear();
                Console.WriteLine();
            }

            object[] oa = run.Method.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string message = "";
            if (oa.Length == 1)
                message = ((DescriptionAttribute)oa[0]).Description;

            Console.WriteLine("Demo : {0}\r\n\r\n{1}", demoName, message);
            if (_prompts)
            {
                Console.WriteLine("Press any key to run the demo");
                Console.ReadKey();
            }
            run();
            if (_prompts)
            {
                Console.WriteLine("Press any key to continue ('U' to run unprompted.)");
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.U)
                    _prompts = false;
            }
        }

        private static readonly StringBuilder _sb = new StringBuilder();
        private static string _feature = "";
        private static string _subFeature = "";
        private static readonly Action<Action> _collectDocs = CreateDocs; // Uncomment this line to turn on doc creation.
        // private static readonly Action<Action> _collectDocs = null; // Uncomment this line to turn off doc creation.
        private static void CreateDocs(Action run)
        {
            string @namespace = run.Method.DeclaringType?.Namespace;
            string demoNamespace = @namespace.StartsWith("Highpoint.Sage.Examples.", StringComparison.Ordinal) ? @namespace.Substring(24) : @namespace;
            Debug.Assert(!string.IsNullOrEmpty(demoNamespace));
            if (demoNamespace.Contains(".", StringComparison.Ordinal))
            {
                string demoFeature = demoNamespace.Substring(0, demoNamespace.IndexOf('.', StringComparison.Ordinal));
                if (!string.Equals(demoFeature, _feature, StringComparison.Ordinal))
                {
                    _sb.AppendLine(string.Format("<h2>{0}</h2>", demoFeature));
                    _feature = demoFeature;
                }


                string subFeature = demoNamespace.Substring(demoNamespace.IndexOf('.', StringComparison.Ordinal) + 1);
                Debug.Assert(!string.IsNullOrEmpty(subFeature));
                if (!string.Equals(subFeature, _subFeature, StringComparison.Ordinal))
                {
                    _sb.AppendLine(string.Format("<h3>{0}</h3>", subFeature));
                    _subFeature = subFeature;
                }
            }
            else
            {
                string demoFeature = demoNamespace;
                if (!string.Equals(demoFeature, _feature, StringComparison.Ordinal))
                {
                    _sb.AppendLine(string.Format("<h2>{0}</h2>", demoFeature));
                    _feature = demoFeature;
                }
            }

            string demoName = run.Method.DeclaringType?.Name;
            _sb.AppendLine(string.Format("<h4>{0}</h4>", demoName));

            object[] oa = run.Method.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string message = oa.Length == 1 ? ((DescriptionAttribute)oa[0]).Description.Replace("\r\n\r\n", "</p>\r\n<p>", StringComparison.Ordinal) : "ERROR";
            _sb.AppendLine($"<p>{message}</p>");
        }
    }
}
