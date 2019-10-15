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

            // This would be the way to fix the issue mentioned in test
            //  LifeStateMachine_ShouldBeInRecreating_AfterFinishWork()
            //  but don't know how to apply it to _harness.
            //  See this https://masstransit-project.com/MassTransit/advanced/sagas/persistence.html#publishing-and-sending-from-sagas
            //  and this https://stackoverflow.com/questions/55144350/masstransit-saga-azure-service-bus-receive-endpoint-setup
            //var bus = Bus.Factory.CreateUsingInMemory(cfg => {
            //    cfg.ReceiveEndpoint("queue", e => { e.UseInMemoryOutbox(); });
            //});

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


        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInWorking_AfterHelloWorld()
        {
            var sagaId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(sagaId));
            // Once the above messages are all consumed and processed,
            // the state machine should be in the Working state
            // Note about the test: it is important that the expected state is included
            // in the condition. Just fetching the instance by correlationid and then testing
            // CurrentState may fail, as setting state (saga execution being async)
            // may take some time.
            IList<Guid> matchingSagaIds = await _sagaHarness.Match(
                instance => instance.CorrelationId == sagaId
                    && instance.CurrentState == _machine.Working.Name,
                new TimeSpan(0, 0, 30));
        }

        [TestMethod]
        public async Task LifeStateMachine_ShouldBeInRecreating_AfterFinishWork()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));
            await _harness.InputQueueSendEndpoint.Send(new FinishWork(instanceId, amountPaid: 1));

            IList<Guid> matchingSagaIds = await _sagaHarness.Match(
                x => x.CorrelationId == instanceId && x.CurrentState == _machine.Recreating.Name,
                new TimeSpan(0, 0, 30));

            // Assert
            matchingSagaIds.Count.Should().Be(1);
            ISagaInstance<LifeState> instance = _sagaHarness.Sagas.First(i => i.Saga.CorrelationId == instanceId);
            instance.Saga.Sport.Should().NotBeNullOrEmpty();
            instance.Saga.Wealth.Should().Be(1);
        }
    }
}
