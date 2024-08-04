﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public partial class MessageWrapperFixture
{
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly IMessageWrapper _messageWrapper = new MessageWrapper();

    public MessageWrapperFixture()
    {
        _publisher.Context.Returns(new SilverbackContext(Substitute.For<IServiceProvider>()));
    }

    [Fact]
    public void Instance_ShouldReturnStaticInstance()
    {
        IMessageWrapper instance = MessageWrapper.Instance;

        instance.Should().NotBeNull();
        instance.Should().BeSameAs(MessageWrapper.Instance);
    }

    private static (IProducer Producer, IProduceStrategyImplementation Strategy) CreateProducer(string topic, bool enableSubscribing = false)
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration(topic)
            {
                Strategy = Substitute.For<IProduceStrategy>(),
                EnableSubscribing = enableSubscribing,
                Endpoint = new TestStaticProducerEndpointResolver(topic)
            });
        IProduceStrategyImplementation produceStrategyImplementation = Substitute.For<IProduceStrategyImplementation>();
        producer.EndpointConfiguration.Strategy.Build(
            Arg.Any<IServiceProvider>(),
            Arg.Any<ProducerEndpointConfiguration>()).Returns(produceStrategyImplementation);
        return (producer, produceStrategyImplementation);
    }
}