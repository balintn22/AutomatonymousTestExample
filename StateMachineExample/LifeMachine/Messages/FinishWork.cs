using MassTransit;
using System;

namespace LifeMachine.Messages
{
    /// <summary>
    /// Represents the event when one finishes work and gets paid.
    /// Once finishing work, a person get paid.
    /// Implementing the CorrelatedBy(Guid) interface makes this message auto-correlated,
    /// see https://masstransit-project.com/MassTransit/usage/correlation.html
    /// </summary>
    public class FinishWork : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }

        /// <summary>Represents the money paid for your work.</summary>
        public double AmountPaid { get; set; }

        /// <summary>Specifies a sport one's doing for recreation.</summary>
        public string Sport { get; set; }
    }
}
