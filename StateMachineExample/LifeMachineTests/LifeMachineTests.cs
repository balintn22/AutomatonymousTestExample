using Automatonymous;
using Automatonymous.Testing;
using FluentAssertions;
using LifeMachine;
using LifeMachine.Messages;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LifeMachineTests
{
    [TestClass]
    public class MyProcessStateMachineTests
    {
        InMemoryTestHarness _harness;
        LifeStateMachine _machine;
        StateMachineSagaTestHarness<LifeState, LifeStateMachine> _sagaHarness;


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

            var sagaInstance = await FindSagaInstance(instanceId, _machine.Working);

            sagaInstance.Should().NotBeNull();
        }

        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInRecreating_AfterFinishWork()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));
            await _harness.InputQueueSendEndpoint.Send(new FinishWork(instanceId, amountPaid: 1));

            var sagaInstance = await FindSagaInstance(instanceId, _machine.Recreating);

            // Assert
            sagaInstance.Should().NotBeNull();
            sagaInstance.Saga.Sport.Should().NotBeNullOrEmpty();
            sagaInstance.Saga.Wealth.Should().Be(1);
        }

        #endregion State Transition Tests


        /// <summary>
        /// Gets a saga instence with a specific correlation id if it is in a specific state.
        /// If the instance is not yet in that state, waits for a certain amount of time.
        /// Returns null if the instance can not be found or is not in the desired state.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private async Task<ISagaInstance<LifeState>> FindSagaInstance(Guid correlationId, State state)
        {
            // It is important that the expected state is included
            // in the condition. Just fetching the instance by correlationid and then testing
            // CurrentState may fail, as setting state (saga execution being async)
            // may take some time.

            IList<Guid> matchingSagaIds = await _sagaHarness.Match(
                x => x.CorrelationId == correlationId && x.CurrentState == state.Name,
                new TimeSpan(0, 0, 30));

            if (matchingSagaIds.Count == 0)
                return null;

            return _sagaHarness.Sagas.FirstOrDefault(i => i.Saga.CorrelationId == correlationId);
        }
    }
}
