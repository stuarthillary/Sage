/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.SimCore
{

    [TestClass]
    public class StateMachineTester
    {
        private static int _testCounter;
        private static Hashtable _batch;
        private static bool _outputEnabled = true;

        public StateMachineTester()
        {
            Init();
        }

        public enum States : int
        {
            Idle = 0, Validated = 1, Running = 2, Paused = 3, Finished = 4
        }

        [TestInitialize]
        public void Init()
        {
            _batch = new Hashtable();
            _testCounter = 0;
            _batch.Add("Batch", _testCounter);
        }

        [TestCleanup]
        public void Destroy()
        {
            Debug.WriteLine("Done.");
        }

        private static void CheckBatch(object userData)
        {
            IDictionary graphContext = userData as IDictionary;
            Assert.IsTrue(graphContext != null);
            Assert.IsTrue(graphContext["Batch"] != null);
            Assert.IsTrue(graphContext["Batch"].Equals((object)_testCounter));
            graphContext["Batch"] = ++_testCounter;
            Assert.IsTrue(graphContext["Batch"].Equals((object)_testCounter));
        }

        /// <summary>
        /// This test confirms some base information about the transition matrix.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test confirms some base information about the transition matrix.")]
        public void TestStateMachine()
        {

            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitiontoValid;
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += RollbackTransitiontoValid;
#if DEBUG
            Debug.WriteLine("Idle state is " + sm._TestGetStateNumber(States.Idle));
            Debug.WriteLine("Validated state is " + sm._TestGetStateNumber(States.Validated));
            Debug.WriteLine("Paused state is " + sm._TestGetStateNumber(States.Paused));
            Debug.WriteLine("Running state is " + sm._TestGetStateNumber(States.Running));
            Debug.WriteLine("Finished state is " + sm._TestGetStateNumber(States.Finished));

            Debug.WriteLine("Idle to Validated is valid? " + sm.TransitionHandler(States.Idle, States.Validated).IsValidTransition);
            Debug.WriteLine("Idle to Paused is valid? " + sm.TransitionHandler(States.Idle, States.Paused).IsValidTransition);
#endif
            sm.DoTransition(States.Validated, _batch);

        }

        /// <summary>
        /// This test confirms some base information about the transition matrix.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test confirms some base information about the transition matrix.")]
        public void TestStateMachinePerformance()
        {

            StateMachine sm = Initialize();
            sm.StructureLocked = false;
            _outputEnabled = false;
            //sm.TransitionHandler(States.Idle, States.Validated).Prepare += new PrepareTransitionEvent(PrepareToTransitiontoValidWithSuccess);
            //sm.TransitionHandler(States.Idle, States.Validated).Commit += new CommitTransitionEvent(CommitTransitiontoValid);
            //sm.TransitionHandler(States.Idle, States.Validated).Rollback += new RollbackTransitionEvent(RollbackTransitiontoValid);

            //Debug.WriteLine("Idle state is " + sm._TestGetStateNumber(States.Idle));
            //Debug.WriteLine("Validated state is " + sm._TestGetStateNumber(States.Validated));
            //Debug.WriteLine("Paused state is " + sm._TestGetStateNumber(States.Paused));
            //Debug.WriteLine("Running state is " + sm._TestGetStateNumber(States.Running));
            //Debug.WriteLine("Finished state is " + sm._TestGetStateNumber(States.Finished));

            //Debug.WriteLine("Idle to Validated is valid? " + sm.TransitionHandler(States.Idle, States.Validated).IsValidTransition);
            //Debug.WriteLine("Idle to Paused is valid? " + sm.TransitionHandler(States.Idle, States.Paused).IsValidTransition);

            for (int i = 0; i < 1000; i++)
            {
                sm.DoTransition(States.Validated, _batch);
                sm.DoTransition(States.Idle, _batch);
            }

            _outputEnabled = true;

        }


        /// <summary>
        /// This test has been set up so that it should succeed and endup in a 'Finished' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that it should succeed and end up in a 'Finished' state.")]
        public void TestTransitionSuccessWithFollowon()
        {
            StateMachine sm = Initialize(true);
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitiontoValid;
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += RollbackTransitiontoValid;

            sm.DoTransition(States.Validated, _batch);

            Assert.IsTrue(States.Finished.Equals(sm.State), "State machine did not transition to 'Finished' state");

        }

        /// <summary>
        /// This test has been set up so that it should succeed and endup in a 'Valid' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that it should succeed and end up in a 'Validated' state.")]
        public void TestTransitionSuccessWithoutFollowon()
        {
            StateMachine sm = Initialize(false);
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitiontoValid;
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += RollbackTransitiontoValid;

            sm.DoTransition(States.Validated, _batch);

            Assert.IsTrue(States.Validated.Equals(sm.State), "State machine did not transition to 'Validated' state");

        }

        /// <summary>
        /// This test has been set up so that the preparation fails, 
        /// which means the state machine has to stay in the 'Idle' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that the preparation fails, which means the state machine has to stay in the 'Idle' state.")]
        public void TestTransitionFailure()
        {
            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitiontoValidWithFailure;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitiontoValid;
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += RollbackTransitiontoValid;

            try
            {
                sm.DoTransition(States.Validated, _batch);
            }
            catch (TransitionFailureException tfe)
            {
                Debug.WriteLine(tfe);
            }
            Assert.IsTrue(States.Idle.Equals(sm.State), "State machine did not stay in 'Idle' state");

        }

        /// <summary>
        /// This test has been set up so that we attempt an illegle transition from 'Idle' to 'Paused', 
        /// which means the state machine has to stay in the 'Idle' state.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that we attempt an illegal transition from 'Idle' to 'Paused', "
                    + "which means the state machine has to stay in the 'Idle' state.")]
        public void TestTransitionIllegal()
        {
            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitiontoValid;
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += RollbackTransitiontoValid;

            try
            {
                sm.DoTransition(States.Paused, _batch);
            }
            catch (TransitionFailureException tfe)
            {
                Debug.WriteLine(tfe);
            }
            Assert.IsTrue(States.Idle.Equals(sm.State), "State machine did not stay in 'Idle' state");

        }

        /// <summary>
        /// This test has been set up so that we attempt to set up an illegle TransitionHandler from 'Idle' to 'Paused', 
        /// which means the state machine has to throw an ApplicationException.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that we attempt to set up an illegal TransitionHandler from 'Idle' to 'Paused', "
                    + "which means the state machine has to throw an ApplicationException.")]
        public void TestTransitionIllegalToo()
        {
            StateMachine sm = Initialize();
            sm.TransitionHandler(States.Idle, States.Paused).Prepare += PrepareToTransitiontoValidWithSuccess;
            Assert.ThrowsException<TransitionFailureException>(() => sm.DoTransition(States.Paused, _batch));
        }

        /// <summary>
        /// This test has been set up so that a complete cicle through all states successfully completes.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up so that a complete cycle through all states successfully completes.")]
        public void TestTransitionChainSuccess()
        {

            StateMachine sm = Initialize(false);
            sm.TransitionHandler(States.Finished, States.Idle).Prepare += PrepareToTransitionToIdleWithSuccess;
            sm.TransitionHandler(States.Finished, States.Idle).Commit += CommitTransitionToIdle;
            sm.TransitionHandler(States.Finished, States.Idle).Rollback += RollbackTransitionToIdle;

            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitiontoValid;
            sm.TransitionHandler(States.Idle, States.Validated).Rollback += RollbackTransitiontoValid;

            sm.TransitionHandler(States.Validated, States.Running).Prepare += PrepareToTransitionToRunningWithSuccess;
            sm.TransitionHandler(States.Validated, States.Running).Commit += CommitTransitionToRunning;
            sm.TransitionHandler(States.Validated, States.Running).Rollback += RollbackTransitionToRunning;

            sm.TransitionHandler(States.Paused, States.Running).Prepare += PrepareToTransitionToRunningWithSuccess;
            sm.TransitionHandler(States.Paused, States.Running).Commit += CommitTransitionToRunning;
            sm.TransitionHandler(States.Paused, States.Running).Rollback += RollbackTransitionToRunning;

            sm.TransitionHandler(States.Running, States.Paused).Prepare += PrepareToTransitionToPausedWithSuccess;
            sm.TransitionHandler(States.Running, States.Paused).Commit += CommitTransitionToPaused;
            sm.TransitionHandler(States.Running, States.Paused).Rollback += RollbackTransitionToPaused;

            sm.TransitionHandler(States.Running, States.Finished).Prepare += PrepareToTransitionToFinishedWithSuccess;
            sm.TransitionHandler(States.Running, States.Finished).Commit += CommitTransitionToFinished;
            sm.TransitionHandler(States.Running, States.Finished).Rollback += RollbackTransitionToFinished;

            sm.DoTransition(States.Validated, _batch);
            Assert.IsTrue(States.Validated.Equals(sm.State), "Transition chain did not move to the 'Validated' state.");
            sm.DoTransition(States.Running, _batch);
            Assert.IsTrue(States.Running.Equals(sm.State), "Transition chain did not move to the 'Running' state.");
            sm.DoTransition(States.Paused, _batch);
            Assert.IsTrue(States.Paused.Equals(sm.State), "Transition chain did not move to the 'Paused' state.");
            sm.DoTransition(States.Running, _batch);
            Assert.IsTrue(States.Running.Equals(sm.State), "Transition chain did not move to the 'Running' state.");
            sm.DoTransition(States.Finished, _batch);
            Assert.IsTrue(States.Finished.Equals(sm.State), "Transition chain did not move to the 'Finished' state.");

        }

        /// <summary>
        /// This test has been set up to see if multiple TransitionHandler can be defined successfully.
        /// </summary>
        [TestMethod]
        [FieldDescription("This test has been set up to see if multiple TransitionHandler can be defined successfully.")]
        public void TestTransitionMultipleHandlers()
        {

            StateMachine sm = Initialize(false);

            // Set up Prepare handlers.
            sm.UniversalTransitionHandler().Prepare += UniversalPrepareToTransition;

            sm.OutboundTransitionHandler(States.Idle).Prepare += PrepareToTransitionOutOfIdleWithSuccess_1;
            sm.OutboundTransitionHandler(States.Idle).Prepare += PrepareToTransitionOutOfIdleWithSuccess_2;
            sm.OutboundTransitionHandler(States.Idle).Prepare += PrepareToTransitionOutOfIdleWithSuccess_3;
            sm.OutboundTransitionHandler(States.Idle).Prepare += PrepareToTransitionOutOfIdleWithSuccess_4;

            sm.InboundTransitionHandler(States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess_1;
            sm.InboundTransitionHandler(States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess_2;
            sm.InboundTransitionHandler(States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess_3;
            sm.InboundTransitionHandler(States.Validated).Prepare += PrepareToTransitiontoValidWithSuccess_4;

            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitionIdletoValidWithSuccess_1;
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitionIdletoValidWithSuccess_2;
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitionIdletoValidWithSuccess_3;
            sm.TransitionHandler(States.Idle, States.Validated).Prepare += PrepareToTransitionIdletoValidWithSuccess_4;

            // Set up Commit handlers.
            sm.UniversalTransitionHandler().Commit += UniversalCommitTransition;

            sm.OutboundTransitionHandler(States.Idle).Commit += CommitTransitionOutOfIdle_1;
            sm.OutboundTransitionHandler(States.Idle).Commit += CommitTransitionOutOfIdle_2;
            sm.OutboundTransitionHandler(States.Idle).Commit += CommitTransitionOutOfIdle_3;
            sm.OutboundTransitionHandler(States.Idle).Commit += CommitTransitionOutOfIdle_4;

            sm.InboundTransitionHandler(States.Validated).Commit += CommitTransitionToValid_1;
            sm.InboundTransitionHandler(States.Validated).Commit += CommitTransitionToValid_2;
            sm.InboundTransitionHandler(States.Validated).Commit += CommitTransitionToValid_3;
            sm.InboundTransitionHandler(States.Validated).Commit += CommitTransitionToValid_4;

            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitionIdleToValid_1;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitionIdleToValid_2;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitionIdleToValid_3;
            sm.TransitionHandler(States.Idle, States.Validated).Commit += CommitTransitionIdleToValid_4;

            sm.DoTransition(States.Validated, _batch);

            Assert.IsTrue(States.Validated.Equals(sm.State), "");

        }

        /// <summary>
        /// This test has been set up to see if multiple TransitionHandler can successfully be defined in a sorted order.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test has been set up to see if multiple TransitionHandler can successfully be defined in a sorted order.")]
        public void TestTransitionMultipleHandlersSorted()
        {

            StateMachine sm = Initialize(false);

            // Set up Prepare handlers.
            sm.UniversalTransitionHandler().AddPrepareEvent(-2, UniversalPrepareToTransition);

            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(4, PrepareToTransitionOutOfIdleWithSuccess_1);
            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(3, PrepareToTransitionOutOfIdleWithSuccess_2);
            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(2, PrepareToTransitionOutOfIdleWithSuccess_3);
            sm.OutboundTransitionHandler(States.Idle).AddPrepareEvent(1, PrepareToTransitionOutOfIdleWithSuccess_4);

            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(4, PrepareToTransitiontoValidWithSuccess_1);
            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(3, PrepareToTransitiontoValidWithSuccess_2);
            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(2, PrepareToTransitiontoValidWithSuccess_3);
            sm.InboundTransitionHandler(States.Validated).AddPrepareEvent(1, PrepareToTransitiontoValidWithSuccess_4);

            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(4, PrepareToTransitionIdletoValidWithSuccess_1);
            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(3, PrepareToTransitionIdletoValidWithSuccess_2);
            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(2, PrepareToTransitionIdletoValidWithSuccess_3);
            sm.TransitionHandler(States.Idle, States.Validated).AddPrepareEvent(1, PrepareToTransitionIdletoValidWithSuccess_4);

            // Set up Commit handlers.
            sm.UniversalTransitionHandler().AddCommitEvent(-2, UniversalCommitTransition);

            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(4, CommitTransitionOutOfIdle_1);
            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(3, CommitTransitionOutOfIdle_2);
            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(2, CommitTransitionOutOfIdle_3);
            sm.OutboundTransitionHandler(States.Idle).AddCommitEvent(1, CommitTransitionOutOfIdle_4);

            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(4, CommitTransitionToValid_1);
            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(3, CommitTransitionToValid_2);
            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(2, CommitTransitionToValid_3);
            sm.InboundTransitionHandler(States.Validated).AddCommitEvent(1, CommitTransitionToValid_4);

            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(4, CommitTransitionIdleToValid_1);
            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(3, CommitTransitionIdleToValid_2);
            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(2, CommitTransitionIdleToValid_3);
            sm.TransitionHandler(States.Idle, States.Validated).AddCommitEvent(1, CommitTransitionIdleToValid_4);

            sm.DoTransition(States.Validated, _batch);

            Assert.IsTrue(States.Validated.Equals(sm.State), "");

        }

        #region Internal Methods

        private StateMachine Initialize()
        {
            return Initialize(true);
        }

        private StateMachine Initialize(bool enableAutoFollowOnStates)
        {

            StateMachineTestModel.EnableAutoFollowOnStates = enableAutoFollowOnStates;
            Model model = new StateMachineTestModel("SMTestModel");

            model.StateMachine.SetStateMethod(StateHandler, States.Idle);
            model.StateMachine.SetStateMethod(StateHandler, States.Validated);
            model.StateMachine.SetStateMethod(StateHandler, States.Running);
            model.StateMachine.SetStateMethod(StateHandler, States.Paused);
            model.StateMachine.SetStateMethod(StateHandler, States.Finished);

            return model.StateMachine;
        }

        sealed class StateMachineTestModel : Model
        {

            public static bool EnableAutoFollowOnStates = true;

            public StateMachineTestModel(string name)
                : base(name, Guid.NewGuid())
            {

            }
            protected override StateMachine CreateStateMachine()
            {
                bool[,] transitionMatrix =
                    new bool[5, 5] { {
                                        ///        IDL    VAL    RUN    PAU    FIN
                                        /* IDL */  false, true,  false, false, true},{
                                        /* VAL */  true,  false, true,  false, true},{
                                        /* RUN */  true,  false, false, true,  true},{
                                        /* PAU */  true,  false, true,  false, true},{
                                        /* FIN */  true,  false, false, false, false }};

                Enum[] followOnStates = null;
                if (EnableAutoFollowOnStates)
                {
                    followOnStates = [States.Idle, States.Running, States.Finished, States.Paused, States.Finished];
                }

                return new StateMachine(this, transitionMatrix, followOnStates, States.Idle);
            }

        }

        private void StateHandler(IModel model, object userData)
        {
            CheckBatch(userData);
            if (_outputEnabled)
            {
                Debug.WriteLine("The model " + model.Name + " is now in the " + model.StateMachine.State + " state.");
            }
        }

        #endregion

        #region A Gazillion Handlers.

        public ITransitionFailureReason PrepareToTransitionToIdleWithSuccess(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toIdle");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToIdleWithFailure(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toIdle (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Idle.", null);
        }

        public void CommitTransitionToIdle(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to Idle transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitionToIdle(IModel model, object userData, IReadOnlyList<ITransitionFailureReason> reasonsForFailure)
        {
            if (_outputEnabled)
                Debug.WriteLine("Rolling back Idle transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure)
            {
                if (_outputEnabled)
                    Debug.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition to Valid");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitiontoValidWithFailure(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition to Valid (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Valid.", null);
        }

        public void CommitTransitiontoValid(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to Valid transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitiontoValid(IModel model, object userData, IReadOnlyList<ITransitionFailureReason> reasonsForFailure)
        {
            if (_outputEnabled)
                Debug.WriteLine("Rolling back Valid transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure)
            {
                if (_outputEnabled)
                    Debug.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionToRunningWithSuccess(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toRunning");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToRunningWithFailure(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toRunning (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Running.", null);
        }

        public void CommitTransitionToRunning(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to Running transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitionToRunning(IModel model, object userData, IReadOnlyList<ITransitionFailureReason> reasonsForFailure)
        {
            if (_outputEnabled)
                Debug.WriteLine("Rolling back Running transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure)
            {
                Debug.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionToPausedWithSuccess(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toPaused");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToPausedWithFailure(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toPaused (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Paused.", null);
        }

        public void CommitTransitionToPaused(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to Paused transition.");
            CheckBatch(userData);
        }

        public void RollbackTransitionToPaused(IModel model, object userData, IReadOnlyList<ITransitionFailureReason> reasonsForFailure)
        {
            if (_outputEnabled)
                Debug.WriteLine("Rolling back Paused transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure)
            {
                Debug.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionToFinishedWithSuccess(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toFinished");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionToFinishedWithFailure(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition toFinished (failure)");
            CheckBatch(userData);
            return new SimpleTransitionFailureReason("Felt like rejecting transition to Finished.", null);
        }

        public void CommitTransitionToFinished(IModel model, object userData)
        {
            CheckBatch(userData);
            if (_outputEnabled)
                Debug.WriteLine("Committing to Finished transition.");
        }

        public void RollbackTransitionToFinished(IModel model, object userData, IReadOnlyList<ITransitionFailureReason> reasonsForFailure)
        {
            if (_outputEnabled)
                Debug.WriteLine("Rolling back Finished transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            foreach (ITransitionFailureReason tfr in reasonsForFailure)
            {
                Debug.WriteLine(tfr.Reason);
            }
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_1(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition out of Idle (1)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_2(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition out of Idle (2)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_3(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition out of Idle (3)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionOutOfIdleWithSuccess_4(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition out of Idle (4)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_1(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition into Valid (1)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_2(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition into Valid (2)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_3(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition into Valid (3)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitiontoValidWithSuccess_4(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition into Valid (4)");
            CheckBatch(userData);
            return null;
        }

        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_1(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition from Idle into Valid (1)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_2(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition from Idle into Valid (2)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_3(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition from Idle into Valid (3)");
            CheckBatch(userData);
            return null;
        }
        public ITransitionFailureReason PrepareToTransitionIdletoValidWithSuccess_4(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Preparing to transition from Idle into Valid (4)");
            return null;
            //CheckBatch(userData);
        }

        public void CommitTransitionOutOfIdle_1(IModel model, object userData)
        {
            CheckBatch(userData);
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle (1).");
        }

        public void CommitTransitionOutOfIdle_2(IModel model, object userData)
        {
            CheckBatch(userData);
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle (2).");
        }

        public void CommitTransitionOutOfIdle_3(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle (3).");
            CheckBatch(userData);
        }

        public void CommitTransitionOutOfIdle_4(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle (4).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_1(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition to Valid (1).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_2(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition to Valid (2).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_3(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition to Valid (3).");
            CheckBatch(userData);
        }

        public void CommitTransitionToValid_4(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition to Valid (4).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_1(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle to Valid (1).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_2(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle to Valid (2).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_3(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle to Valid (3).");
            CheckBatch(userData);
        }

        public void CommitTransitionIdleToValid_4(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Committing to transition from Idle to Valid (4).");
            CheckBatch(userData);
        }

        public ITransitionFailureReason UniversalPrepareToTransition(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Universal handler reports, Preparing to transition to Valid (success)");
            CheckBatch(userData);
            return null;
        }

        public void UniversalCommitTransition(IModel model, object userData)
        {
            if (_outputEnabled)
                Debug.WriteLine("Universal handler reports, Committing to Valid transition.");
            CheckBatch(userData);
        }

        public void UniversalRollbackTransition(IModel model, object userData, IList reasonsForFailure)
        {
            if (_outputEnabled)
                Debug.WriteLine("Universal handler reports, Rolling back transition.");
            CheckBatch(userData);
            if (reasonsForFailure == null)
                return;
            if (_outputEnabled)
            {
                foreach (ITransitionFailureReason tfr in reasonsForFailure)
                {
                    Debug.WriteLine(tfr.Reason);
                }
            }
        }



        #endregion
    }
}
