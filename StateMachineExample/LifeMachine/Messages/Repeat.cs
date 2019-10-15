using MassTransit;
using System;

namespace LifeMachine.Messages
{
    /// <summary>
    /// Once a person is done recreating, they repeat their daily cycle by going to work.
    /// Implementing the CorrelatedBy(Guid) interface makes this message auto-correlated,
    /// see https://masstransit-project.com/MassTransit/usage/correlation.html
    /// </summary>
    public class Repeat : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
    }
}
