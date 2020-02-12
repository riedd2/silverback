﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public sealed class RabbitExchangeConsumerEndpoint
        : RabbitConsumerEndpoint,
            IEquatable<RabbitExchangeConsumerEndpoint>
    {
        public RabbitExchangeConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the exchange configuration.
        /// </summary>
        public RabbitExchangeConfig Exchange { get; set; } = new RabbitExchangeConfig();

        /// <summary>
        ///     Gets or sets the desired queue name. If null or empty a random name will be generated by RabbitMQ.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        ///     Gets or sets the routing key (aka binding key) to be used to bind with the exchange.
        /// </summary>
        public string RoutingKey { get; set; }

        public override void Validate()
        {
            base.Validate();

            if (Exchange == null)
                throw new EndpointConfigurationException("Exchange cannot be null");

            Exchange.Validate();
        }

        public override string GetUniqueConsumerGroupName() => (!string.IsNullOrEmpty(QueueName))
            ? QueueName
            : Name;
        
        #region Equality

        public bool Equals(RabbitExchangeConsumerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) &&
                   Equals(Exchange, other.Exchange) &&
                   string.Equals(QueueName, other.QueueName, StringComparison.InvariantCulture) &&
                   string.Equals(RoutingKey, other.RoutingKey, StringComparison.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitExchangeConsumerEndpoint) obj);
        }

        #endregion
    }
}