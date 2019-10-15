using Automatonymous.Testing;
using FluentAssertions;
using LifeMachine;
using LifeMachine.Messages;
using MassTransit.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LifeMachineTests
{
    [TestFixture]
    public class MyProcessStateMachineTests
    {
        InMemoryTestHarness _harness;
        LifeStateMachine _machine;
        StateMachineSagaTestHarness<LifeState, LifeStateMachine> _sagaHarness;


        [SetUp]
        public void InitializeTestHarness()
        {
            _harness = new InMemoryTestHarness();
            _machine = new LifeStateMachine();
            _sagaHarness = _harness.StateMachineSaga<LifeState, LifeStateMachine>(_machine);

            _harness.Start().Wait();
        }

        [TearDown]
        public void StopTestHarness()
        {
            _harness.Stop();
        }


        [Test]
        public async Task LifeStateMachine_ShouldBeInWorking_AfterHelloWorld()
        {
            var sagaId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(sagaId));
            // Once the above messages are all consumed and processed,
            // the state machine should be in the Working state
            // Note about the test: it is important that the expcted state is included
            // in the condition. Just fetching the saga by correlationid and testing
            // CurrentState may fail, as setting state (saga execution being async)
            // takes some time.
            IList<Guid> matchingSagaIds = await _sagaHarness.Match(
                instance => instance.CorrelationId == sagaId
                    && instance.CurrentState == _machine.Working.Name,
                new TimeSpan(0, 0, 30));
        }

        [Test]
        public async Task LifeStateMachine_ShouldBeInRecreating_AfterFinishWork()
        {
            var instanceId = Guid.NewGuid();

            await _harness.InputQueueSendEndpoint.Send(new HelloWorld(instanceId));

            // This makes the test stable, which hints a message delivery concurrency issue...
            await _sagaHarness.Match(
                x => x.CorrelationId == instanceId && x.CurrentState == _machine.Working.Name,
                new TimeSpan(0, 0, 30));

            await _harness.InputQueueSendEndpoint.Send(new FinishWork(instanceId, amountPaid: 1));

            IList<Guid> matchingSagaIds = await _sagaHarness.Match(
                x => x.CorrelationId == instanceId && x.CurrentState == _machine.Recreating.Name,
                new TimeSpan(0, 0, 30));

            // Grab the instance and Assert stuff...
            matchingSagaIds.Count.Should().Be(1);
            ISagaInstance<LifeState> instance = _sagaHarness.Sagas.First(i => i.Saga.CorrelationId == instanceId);
            instance.Saga.Sport.Should().NotBeNullOrEmpty();
            instance.Saga.Wealth.Should().Be(1);
        }
    }
}
