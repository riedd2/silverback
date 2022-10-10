﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Testing;

/// <inheritdoc cref="ITestingHelper" />
public interface IKafkaTestingHelper : ITestingHelper
{
    /// <summary>
    ///     Gets a collection of of <see cref="IMockedConsumerGroup" /> representing all known consumer groups.
    /// </summary>
    /// <returns>
    ///     The collection of <see cref="IMockedConsumerGroup" />.
    /// </returns>
    public IReadOnlyCollection<IMockedConsumerGroup> ConsumerGroups { get; }

    /// <summary>
    ///     Returns the <see cref="IMockedConsumerGroup" /> representing the consumer group with the specified id.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderKafkaTestingExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderKafkaTestingExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedConsumerGroup" />.
    /// </returns>
    IMockedConsumerGroup GetConsumerGroup(string groupId);

    /// <summary>
    ///     Returns the <see cref="IMockedConsumerGroup" /> representing the consumer group with the specified id.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderKafkaTestingExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderKafkaTestingExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedConsumerGroup" />.
    /// </returns>
    IMockedConsumerGroup GetConsumerGroup(string groupId, string bootstrapServers);

    /// <summary>
    ///     Returns the <see cref="IInMemoryTopic" /> with the specified name.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only. See <see cref="SilverbackBuilderKafkaTestingExtensions.UseMockedKafka" />
    ///     or <see cref="BrokerOptionsBuilderKafkaTestingExtensions.AddMockedKafka" />.
    /// </remarks>
    /// <param name="name">
    ///     The name of the topic.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker. This must be specified when testing with
    ///     multiple brokers.
    /// </param>
    /// <returns>
    ///     The <see cref="IInMemoryTopic" />.
    /// </returns>
    IInMemoryTopic GetTopic(string name, string? bootstrapServers = null);

    /// <summary>
    ///     Gets a new producer with the specified configuration.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    IProducer GetProducer(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction);
}
