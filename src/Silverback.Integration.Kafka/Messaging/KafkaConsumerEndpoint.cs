﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public sealed class KafkaConsumerEndpoint : ConsumerEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        public KafkaConsumerEndpoint(params string[] names)
            : base("")
        {
            Names = names;

            if (names == null)
                return;

            Name = names.Length > 1
                ? "[" + string.Join(",", names) + "]"
                : names[0];
        }

        /// <summary>
        ///     Gets the names of the topics.
        /// </summary>
        public string[] Names { get; }

        /// <summary>
        ///     Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        ///     dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaConsumerConfig Configuration { get; set; } = new KafkaConsumerConfig();

        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        #region Equality

        public bool Equals(KafkaConsumerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(Configuration, other.Configuration);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KafkaConsumerEndpoint) obj);
        }

        #endregion
    }
}