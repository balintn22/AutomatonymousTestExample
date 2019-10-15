using Automatonymous;
using Automatonymous.Binders;
using LifeMachine.Messages;
using System;

namespace LifeMachine
{
    /// <summary>
    /// Implements a simple, sample Automatonymous state machine.
    /// </summary>
    public class LifeMachineSaga : MassTransitStateMachine<LifeState>
    {
        public State Working { get; private set; }
        public State Resting { get; private set; }
        public State Recreating { get; private set; }

        public Event<FinishWork> FinishWorkEvent { get; private set; }
        public Event<GoodbyeCruelWorld> GoodbyeCruelWorldEvent { get; private set; }
        public Event<GotoBed> GotoBed { get; private set; }
        public Event<HelloWorld> HelloWorldEvent { get; private set; }
        public Event<Repeat> RepeatEvent { get; private set; }


        public LifeMachineSaga()
        {
            // Define current state property
            InstanceState(x => x.CurrentState);

            // Set up events handled and how the correlation id is mapped
            Event(() => FinishWorkEvent);
            Event(() => GoodbyeCruelWorldEvent);
            Event(() => GotoBed);
            Event(() => HelloWorldEvent);
            Event(() => RepeatEvent);

            // Define state machine
            // Mention ALL states and ALL state transitions here.
            Initially(
                When(HelloWorldEvent)
                    .Then(context => {
                        context.Instance.CorrelationId = context.Data.CorrelationId;
                        context.Instance.Name = context.Data.Name;
                    })
                    .Then(context => Console.WriteLine($"{context.Instance.Name} was born. Starting work."))
                    .TransitionTo(Working)
                );

            During(Working,
                When(FinishWorkEvent)
                    .Then(context => {
                        context.Instance.Wealth += context.Data.AmountPaid;
                        context.Instance.Sport = context.Data.Sport;
                    })
                    .Then(context => Console.WriteLine($"{context.Instance.Name} finished work, starting {context.Instance.Sport}."))
                    .TransitionTo(Recreating)
                );

            During(Recreating,
                When(GotoBed)
                    .Then(context => context.Instance.Sport = null)
                    .Then(context => Console.WriteLine($"{context.Instance.Name} finished recreation, going to bed."))
                    .TransitionTo(Resting)
                );

            During(Resting,
                When(RepeatEvent)
                    .Then(context => Console.WriteLine($"{context.Instance.Name} finished resting, starting it all over again."))
                    .TransitionTo(Working),
                When(GoodbyeCruelWorldEvent)
                    .Finalize()   // Successful saga completion
                );

            // Remove saga state from storage
            SetCompletedWhenFinalized();
        }

    }
}
