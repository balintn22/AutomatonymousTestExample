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
        private static string[] _names = {
            "Agnes",
            "Albert",
            "Andrew",
            "Cyprian",
            "Cyril",
            "Florian",
            "Francis",
            "George",
            "James",
            "John",
            "Joseph",
            "Luke",
            "Mary",
            "Peter",
            "Patrick",
            "Paul",
        };
        private static int _nameIndex = 0;


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
            lock (_names)
            {
                // This message will create new person, who needs a new saga, that needs a new id.
                CorrelationId = correlationId;

                Name = _names[_nameIndex];
                _nameIndex = (_nameIndex + 1) % _names.Length;
            }
        }
    }
}
