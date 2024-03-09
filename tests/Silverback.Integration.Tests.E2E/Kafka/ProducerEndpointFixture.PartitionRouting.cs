﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointFixture
{
    [Fact]
    public async Task ProducerEndpoint_ShouldProduceToSpecifiedPartition()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, 1))
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo(DefaultTopicName, 3)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne());
        }

        DefaultTopic.Partitions[0].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[1].TotalMessagesCount.Should().Be(5);
        DefaultTopic.Partitions[2].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[3].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[4].TotalMessagesCount.Should().Be(0);

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne());
            await publisher.PublishEventAsync(new TestEventTwo());
        }

        DefaultTopic.Partitions[0].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[1].TotalMessagesCount.Should().Be(8);
        DefaultTopic.Partitions[2].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[3].TotalMessagesCount.Should().Be(3);
        DefaultTopic.Partitions[4].TotalMessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceToPredictablePartition_WhenKafkaKeyIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "1" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "2" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "3" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "4" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = "5" });

        DefaultTopic.Partitions[0].TotalMessagesCount.Should().Be(2);
        DefaultTopic.Partitions[1].TotalMessagesCount.Should().Be(1);
        DefaultTopic.Partitions[2].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[3].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[4].TotalMessagesCount.Should().Be(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceToRandomPartition_WhenKafkaKeyIsNullOrEmpty()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventWithStringKafkaKey { KafkaKey = null, Content = "1" });
        await publisher.PublishEventAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "2" });
        await publisher.PublishEventAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "3" });
        await publisher.PublishEventAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "4" });
        await publisher.PublishEventAsync(new TestEventWithStringKafkaKey { KafkaKey = null, Content = "5" });

        DefaultTopic.Partitions.Where(partition => partition.TotalMessagesCount >= 1).Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceToSpecifiedPartitionRegardlessOfKafkaKey()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName, 3)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "1" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "2" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "3" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "4" });
        await publisher.PublishEventAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = "5" });

        DefaultTopic.Partitions[0].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[1].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[2].TotalMessagesCount.Should().Be(0);
        DefaultTopic.Partitions[3].TotalMessagesCount.Should().Be(5);
        DefaultTopic.Partitions[4].TotalMessagesCount.Should().Be(0);
    }
}
