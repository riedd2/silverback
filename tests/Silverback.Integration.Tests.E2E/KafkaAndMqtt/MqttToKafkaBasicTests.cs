﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.KafkaAndMqtt;

public class MqttToKafkaBasicTests : KafkaTestFixture
{
    public MqttToKafkaBasicTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboundAndInbound_MqttToKafka_ProducedAndConsumed()
    {
        int eventOneCount = 0;
        int eventTwoCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedMqtt()
                        .AddMockedKafka())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<TestEventOne>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(consumer => consumer.ConsumeFrom(DefaultTopicName)))
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            configuration => configuration
                                .WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<TestEventTwo>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddDelegateSubscriber2<TestEventOne, TestEventTwo>(HandleEventOne)
                .AddDelegateSubscriber2<TestEventTwo>(HandleEventTwo));

        TestEventTwo HandleEventOne(TestEventOne eventOne)
        {
            Interlocked.Increment(ref eventOneCount);
            return new TestEventTwo { Content = eventOne.Content };
        }

        void HandleEventTwo(TestEventTwo message) => Interlocked.Increment(ref eventTwoCount);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        IMqttTestingHelper mqttTestingHelper = Host.ServiceProvider.GetRequiredService<IMqttTestingHelper>();

        await mqttTestingHelper.WaitUntilConnectedAsync();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => eventOneCount >= 15);
        await AsyncTestingUtil.WaitAsync(() => eventTwoCount >= 15);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        eventOneCount.Should().Be(15);
        eventTwoCount.Should().Be(15);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }
}
