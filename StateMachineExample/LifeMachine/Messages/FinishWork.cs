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
        private string[] _sports = {
            "football",
            "basketball",
            "rollerskating",
            "swimming",
            "skydiving",
        };


        public Guid CorrelationId { get; set; }

        /// <summary>Represents the money paid for your work.</summary>
        public double AmountPaid { get; set; }

        /// <summary>Specifies a sport one's doing for recreation.</summary>
        public string Sport { get; set; }


        public FinishWork(Guid correlationId, double amountPaid)
        {
            CorrelationId = correlationId;
            AmountPaid = amountPaid;
            Sport = _sports[new Random().Next(0, _sports.Length)];
        }
    }
}
