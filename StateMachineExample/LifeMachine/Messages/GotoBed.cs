using MassTransit;
using System;

namespace LifeMachine.Messages
{
    /// <summary>
    /// Represents the event of finishing resting an getting up from bed.
    /// Implementing the CorrelatedBy(Guid) interface makes this message auto-correlated,
    /// see https://masstransit-project.com/MassTransit/usage/correlation.html
    /// </summary>
    public class GotoBed : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
    }
}
