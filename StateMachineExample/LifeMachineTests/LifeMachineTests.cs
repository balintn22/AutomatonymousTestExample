using Automatonymous;
using Automatonymous.Testing;
using FluentAssertions;
using LifeMachine;
using LifeMachine.Messages;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LifeMachineTests
{
    [TestClass]
    public class MyProcessStateMachineTests
    {
        InMemoryTestHarness _harness;
        LifeStateMachine _machine;
        StateMachineSagaTestHarness<LifeState, LifeStateMachine> _sagaHarness;

        private TimeSpan _testTimeout => Debugger.IsAttached ? TimeSpan.FromSeconds(50) : TimeSpan.FromSeconds(30);


        [TestInitialize]
        public async Task InitializeTestHarness()
        {
            _harness = new InMemoryTestHarness();
            _machine = new LifeStateMachine();
            _sagaHarness = _harness.StateMachineSaga<LifeState, LifeStateMachine>(_machine);

            // Note: This style can be used in old test environments, where InitializeTestHarness may not be async.
            //_harness.Start().Wait();
            await _harness.Start();
        }

        [TestCleanup]
        public async Task StopTestHarness()
        {
            await _harness.Stop();
        }


        #region State Transition Tests

        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInWorking_AfterHelloWorld()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));
            // Once the above messages are all consumed and processed,
            // the state machine should be in the Working state

            await AssertInstanceState(instanceId, _machine.Working);
        }

        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInRecreating_AfterFinishWork()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));
            await _harness.InputQueueSendEndpoint.Send(new FinishWork(instanceId, amountPaid: 1));

            // Assert
            await AssertInstanceState(instanceId, _machine.Recreating);
            var sagaInstance = GetSagaInstance(instanceId);
            sagaInstance.Should().NotBeNull();
            sagaInstance.Saga.Sport.Should().NotBeNullOrEmpty();
            sagaInstance.Saga.Wealth.Should().Be(1);
        }

        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInResting_AfterGotoBed()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));
            //await WaitForState(instanceId, _machine.Working);
            await _harness.InputQueueSendEndpoint.Send(new FinishWork(instanceId, amountPaid: 1));
            // Note: Without this wait, the test is not reliable. May have something
            // to do with message delivery/state persistence.
            // See https://masstransit-project.com/MassTransit/advanced/sagas/persistence.html#publishing-and-sending-from-sagas
            await WaitForState(instanceId, _machine.Recreating);
            await _harness.InputQueueSendEndpoint.Send(new GotoBed(instanceId));

            // Assert
            await AssertInstanceState(instanceId, _machine.Resting);
        }

        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInWorking_AfterRepeat()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));
            //await WaitForState(instanceId, _machine.Working);
            await _harness.InputQueueSendEndpoint.Send(new FinishWork(instanceId, amountPaid: 1));
            await WaitForState(instanceId, _machine.Recreating);
            await _harness.InputQueueSendEndpoint.Send(new GotoBed(instanceId));
            await WaitForState(instanceId, _machine.Resting);
            await _harness.InputQueueSendEndpoint.Send(new Repeat(instanceId));

            // Assert
            await AssertInstanceState(instanceId, _machine.Working);
        }

        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInFinal_AfterGoodbyeCruelWorld()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));
            //await WaitForState(instanceId, _machine.Working);
            await _harness.InputQueueSendEndpoint.Send(new FinishWork(instanceId, amountPaid: 1));
            await WaitForState(instanceId, _machine.Recreating);
            await _harness.InputQueueSendEndpoint.Send(new GotoBed(instanceId));
            await WaitForState(instanceId, _machine.Resting);
            await _harness.InputQueueSendEndpoint.Send(new GoodbyeCruelWorld(instanceId));

            // Assert
            await AssertInstanceState(instanceId, _machine.Final);
        }

        #endregion State Transition Tests


        #region Test Helpers

        /// <summary>
        /// Asserts whether the saga instance is in the expected state.
        /// </summary>
        private async Task AssertInstanceState(Guid correlationId, State expectedState)
        {
            // Wait for the instance to transition to the expected state
            ISagaInstance<LifeState> sagaInstance = await FindSagaInstance(correlationId, expectedState);

            if (sagaInstance == null)
            {
                // Instance is not in the correct state, fetch it anyway
                sagaInstance = _sagaHarness.Sagas.FirstOrDefault(i => i.Saga.CorrelationId == correlationId);
                Assert.Fail($"Saga instance {correlationId} was expected to be in {expectedState} state, but it is in {sagaInstance.Saga.CurrentState}.");
            }
        }

        /// <summary>
        /// Gets a saga instance with a specific correlation id if it is in a specific state.
        /// If the instance is not yet in that state, waits for a certain amount of time.
        /// Returns null if the instance can not be found or is not in the desired state.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private async Task<ISagaInstance<LifeState>> FindSagaInstance(Guid correlationId, State state)
        {
            ISagaInstance<LifeState> sagaInstance;
            if (state == _machine.Final)
            {   // Match doesn't seem to work with the Final state, check that by name
                var startTime = DateTime.Now;
                while (true)
                {
                    Thread.Sleep(100);
                    sagaInstance = _sagaHarness.Sagas.FirstOrDefault(instance => instance.Saga.CurrentState == "Final");
                    if (sagaInstance != null)
                        break;
                    if (DateTime.Now - startTime > _testTimeout)
                        break;
                };
            }
            else
            {
                // Wait till the saga instance transitions to the expected state - or fail after a timeout.
                IList<Guid> matchingSagaIds = await _sagaHarness.Match(
                    x => x.CorrelationId == correlationId && x.CurrentState == state.Name,
                    _testTimeout);

                if (matchingSagaIds.Count == 0)
                    return null;

                sagaInstance = _sagaHarness.Sagas.FirstOrDefault(i => i.Saga.CorrelationId == correlationId);
            }

            return sagaInstance;
        }

        /// <summary>
        /// Gets a saga instance by id.
        /// Returns null if an instance with that correlationid doesn't exist.
        /// </summary>
        private ISagaInstance<LifeState> GetSagaInstance(Guid correlationId)
        {
            return _sagaHarness.Sagas.FirstOrDefault(i => i.Saga.CorrelationId == correlationId);
        }

        /// <summary>Waits until the saga instance enters the desired state.</summary>
        private async Task WaitForState(Guid correlationId, State state)
        {
            await FindSagaInstance(correlationId, state);
        }

        #endregion Test Helpers
    }
}
