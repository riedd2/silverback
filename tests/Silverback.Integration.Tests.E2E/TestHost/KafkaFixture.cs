﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

[Trait("Category", "E2E:Kafka")]
public abstract class KafkaFixture : E2EFixture
{
    protected const string DefaultTopicName = "default-e2e-topic";

    protected const string DefaultGroupId = "e2e-consumer-group-1";

    private IInMemoryTopic? _defaultTopic;

    private IMockedConsumerGroup? _defaultConsumerGroup;

    private IKafkaTestingHelper? _testingHelper;

    protected KafkaFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    protected IKafkaTestingHelper Helper => _testingHelper ??= Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

    protected IInMemoryTopic DefaultTopic => _defaultTopic ??= Helper.GetTopic(DefaultTopicName);

    protected IMockedConsumerGroup DefaultConsumerGroup => _defaultConsumerGroup ??= Helper.GetConsumerGroup(DefaultGroupId);
}
