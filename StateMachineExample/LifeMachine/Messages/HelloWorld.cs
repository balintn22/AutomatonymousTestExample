using MassTransit;
using System;

namespace LifeMachine.Messages
{
    /// <summary>
    /// Represents the event of coming to this world.
    /// Implementing the CorrelatedBy(Guid) interface makes this message automatically
    /// initate new saga instances. TO do that, CorrelationId must be filled in.
    /// See https://masstransit-project.com/MassTransit/usage/correlation.html
    /// </summary>
    public class HelloWorld : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }

        /// <summary>Contains the name of the person coming to this world.</summary>
        public string Name { get; set; }


        /// <summary>
        /// Constructor.
        /// Enforces that CorrelationId be filled in, which is required for this
        /// message to start a new saga instance.
        /// </summary>
        /// <param name="correlationId"></param>
        public HelloWorld(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
