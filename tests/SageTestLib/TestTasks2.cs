/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Graphs;
using Highpoint.Sage.Graphs.Analysis;
using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;

namespace SchedulerDemoMaterial
{

    [TestClass]
    public class TaskTester
    {
        [TestMethod]
        public void TestBaseFunctionality()
        {

            long oneDay = TimeSpan.FromDays(1.0).Ticks;
            long oneHour = TimeSpan.FromHours(1.0).Ticks;

            Model model = new Model();
            model.AddService<ITaskManagementService>(new TaskManagementService());

            Task[] tasks = new Task[] { new MyTask(model, 0), new DelayTask(model, 1, oneDay), new MyTask(model, 2), new DelayTask(model, 3, oneHour) };

            int[] from = new int[] { 0, 0, 1, 2 };//,4,5,6,7,8};
            int[] to = new int[] { 1, 3, 2, 3 };//,4,6,5,9,1};

            ArrayList childTasks = new ArrayList();
            foreach (Task task in tasks)
                childTasks.Add(task);

            for (int ndx = 0; ndx < to.Length; ndx++)
            {

                Task taskA = (Task)((Edge)childTasks[from[ndx]]);
                Task taskB = (Task)((Edge)childTasks[to[ndx]]);

                Debug.WriteLine($"Considering a connection between {taskA.Name} and {taskB.Name}.");

                int forward = PathLength.ShortestPathLength(taskA, taskB);
                int backward = PathLength.ShortestPathLength(taskB, taskA);

                Debug.WriteLine($"Forward path length is {forward}, and reverse path length is {backward}.");

                if ((forward == int.MaxValue) && (backward == int.MaxValue))
                {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine($"{taskB.Name} will follow {taskA.Name}.");
                }
                else if ((forward != int.MaxValue) && (backward == int.MaxValue))
                {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine($"{taskB.Name} will follow {taskA.Name}.");
                }
                else if ((forward == int.MaxValue) && (backward != int.MaxValue))
                {
                    taskB.AddSuccessor(taskA);
                    Debug.WriteLine("{1} will follow {0}.", taskB.Name, taskA.Name);
                }
                else
                {
                    throw new ApplicationException($"Cycle exists between {taskA.Name} and {taskB.Name}.");
                }
            }

            Task topTask = new Task(model, "Parent", Guid.NewGuid());
            topTask.AddChildEdges(childTasks);
            TaskProcessor tp = new TaskProcessor(model, "Task Processor", topTask);
            tp.SetStartTime(DateTime.Now);
            model.GetService<ITaskManagementService>().AddTaskProcessor(tp);

            model.StateMachine.InboundTransitionHandler(model.GetStartEnum()).Commit += new CommitTransitionEvent(OnModelStarting);

            model.Start();
            if (model.StateMachine.State.Equals(StateMachine.GenericStates.Running))
            {
                Debug.WriteLine("Error attempting to transition to Started state.");
            }
        }

        void OnModelStarting(IModel model, object userData)
        {
            Debug.WriteLine($"Model {model.Name} starting.");
        }


        sealed class MyTask : Highpoint.Sage.Graphs.Tasks.Task
        {

            public MyTask(IModel model, int i) : base(model, "Task #" + i, Guid.NewGuid()) { }

            protected override void DoTask(IDictionary graphContext)
            {
                Debug.WriteLine(Name + " is of type \"MyTask\" and therefore reports that it is executing.");
            }

        }

        sealed class DelayTask : Highpoint.Sage.Graphs.Tasks.Task
        {
            long _delay;

            public DelayTask(IModel model, int i, long delay) : base(model, "Task #" + i, Guid.NewGuid())
            {
                _delay = delay;
            }
        }
    }
}
