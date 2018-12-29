﻿using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.<br/>
    /// This is the simplest implementation and it doesn't prevent duplicated processing of the same message. 
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Adapters.IInboundAdapter" />
    public class SimpleInboundAdapter : IInboundAdapter
    {
        private IBus _bus;
        private IBroker _broker;
        private IConsumer _consumer;

        /// <summary>
        /// Initializes the <see cref="T:Silverback.Messaging.Adapters.IInboundAdapter" />.
        /// </summary>
        /// <param name="bus">The internal <see cref="IBus" /> where the messages have to be relayed.</param>
        /// <param name="broker">The broker to be used.</param>
        /// <param name="endpoint">The endpoint this adapter has to connect to.</param>
        public void Init(IBus bus, IBroker broker, IEndpoint endpoint)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));

            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            Connect(endpoint);
        }

        /// <summary>
        /// Implements the logic to connect and start listening to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        protected virtual void Connect(IEndpoint endpoint)
        {
            if (_consumer != null)
                throw new InvalidOperationException("Connect was called twice.");

            _consumer = _broker.GetConsumer(endpoint);
            
            // TODO: Handle errors -> logging and stuff -> then?
            _consumer.Received += (_, envelope) => RelayMessage(envelope.Message);
        }

        /// <summary>
        /// Relays the message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual void RelayMessage(IIntegrationMessage message)
        {
            _bus.Publish(message);
        }
    }
}