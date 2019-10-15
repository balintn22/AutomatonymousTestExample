using Automatonymous;
using System;

namespace LifeMachine
{
    /// <summary>
    /// Represents the state of a person's life.
    /// </summary>
    public class LifeState : SagaStateMachineInstance
    {
        #region Implement interface SagaStateMachineInstance

        public Guid CorrelationId { get; set; }

        #endregion Implement interface SagaStateMachineInstance

        /// <summary>Property to hold current state information for the saga.</summary>
        public string CurrentState { get; set; }



        /// <summary>Name of the person.</summary>
        public string Name { get; set; }

        /// <summary>One's salary is accumulating here.</summary>
        public double Wealth { get; set; }

        /// <summary>Contains the name of a sport while one1s recreating, empty otherwise.</summary>
        public string Sport { get; set; }
    }
}
