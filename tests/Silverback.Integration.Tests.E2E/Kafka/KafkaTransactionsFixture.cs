﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Transactions;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class KafkaTransactionsFixture : KafkaFixture
{
    public KafkaTransactionsFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task KafkaTransactions_ShouldProduceInTransaction()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("transactional-id")
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        using (IKafkaTransaction transaction = publisher.InitKafkaTransaction())
        {
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            // Not committed yet, so we expect no message to be consumed
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

            transaction.Commit();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
    }

    [Fact]
    public async Task KafkaTransactions_ShouldRollbackProduce()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("transactional-id")
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        using (IKafkaTransaction dummy = publisher.InitKafkaTransaction())
        {
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            // No commit means an implicit abort
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);
    }

    [Fact]
    public async Task KafkaTransactions_ShouldCreateTransactionPerConsumedPartition()
    {
        const int batchSize = 10;
        const int partitionsPerTopic = 3;
        int processedInputMessages = 0;
        int committedOutputMessages = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(partitionsPerTopic)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("transactional-id")
                                .Produce<TestEventTwo>(
                                    endpoint => endpoint.ProduceTo(
                                        "output",
                                        eventTwo => int.Parse(eventTwo!.ContentEventTwo!, CultureInfo.InvariantCulture))))
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint.ProduceTo(
                                        "input1",
                                        eventOne => int.Parse(eventOne!.ContentEventOne!, CultureInfo.InvariantCulture)))
                                .Produce<TestEventOne>(
                                    endpoint => endpoint.ProduceTo(
                                        "input2",
                                        eventOne => int.Parse(eventOne!.ContentEventOne!, CultureInfo.InvariantCulture))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("input")
                                .Consume(endpoint => endpoint.ConsumeFrom("input1", "input2").EnableBatchProcessing(batchSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("output")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("output"))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, IEventPublisher>(HandleInputBatch)
                .AddDelegateSubscriber<TestEventTwo>(HandleOutput)
                .AddIntegrationSpy());

        async ValueTask HandleInputBatch(IAsyncEnumerable<TestEventOne> batch, IEventPublisher publisher)
        {
            using IKafkaTransaction transaction = publisher.InitKafkaTransaction();

            await foreach (TestEventOne eventOne in batch)
            {
                await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = eventOne.ContentEventOne });
                Interlocked.Increment(ref processedInputMessages);
            }

            transaction.Commit();
        }

        void HandleOutput(TestEventTwo dummy) => Interlocked.Increment(ref committedOutputMessages);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        // Publish incomplete batches (1 message missing) to each partition
        for (int i = 0; i < batchSize - 1; i++)
        {
            for (int partition = 0; partition < partitionsPerTopic; partition++)
            {
                // The input and output topics are co-partitioned to ensure that each transaction is limited to a single partition to simplify the test
                await publisher.PublishAsync(new TestEventOne { ContentEventOne = partition.ToString(CultureInfo.InvariantCulture) });
            }
        }

        await AsyncTestingUtil.WaitAsync(() => processedInputMessages >= 2 * partitionsPerTopic * (batchSize - 1));
        committedOutputMessages.Should().Be(0);

        // Publish 1 extra message to complete the first 2 batches (1 per topic)
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = (partitionsPerTopic - 1).ToString(CultureInfo.InvariantCulture) });

        await AsyncTestingUtil.WaitAsync(() => committedOutputMessages >= batchSize * 2);
        await Task.Delay(100);
        committedOutputMessages.Should().Be(batchSize * 2);

        // Complete all batches
        for (int partition = 0; partition < partitionsPerTopic - 1; partition++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = partition.ToString(CultureInfo.InvariantCulture) });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        committedOutputMessages.Should().Be(batchSize * partitionsPerTopic * 2);
    }

    [Fact]
    public async Task KafkaTransactions_ShouldCreateTransactionPerConsumedPartition_WhenNotCoPartitioned()
    {
        const int batchSize = 10;
        const int partitionsPerTopic = 3;
        int processedInputMessages = 0;
        int committedOutputMessages = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(partitionsPerTopic)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("transactional-id")
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("output")))
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("input1"))
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("input2")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("input")
                                .Consume(endpoint => endpoint.ConsumeFrom("input1", "input2").EnableBatchProcessing(batchSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("output")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("output"))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, IEventPublisher>(HandleInputBatch)
                .AddDelegateSubscriber<TestEventTwo>(HandleOutput)
                .AddIntegrationSpy());

        async ValueTask HandleInputBatch(IAsyncEnumerable<TestEventOne> batch, IEventPublisher publisher)
        {
            using IKafkaTransaction transaction = publisher.InitKafkaTransaction();

            await foreach (TestEventOne dummy in batch)
            {
                await publisher.PublishAsync(new TestEventTwo());
                Interlocked.Increment(ref processedInputMessages);
            }

            transaction.Commit();
        }

        void HandleOutput(TestEventTwo dummy) => Interlocked.Increment(ref committedOutputMessages);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        // Publish incomplete batches (1 message missing) to each partition
        for (int i = 0; i < batchSize - 1; i++)
        {
            for (int partition = 0; partition < partitionsPerTopic; partition++)
            {
                await publisher.PublishAsync(new TestEventOne());
            }
        }

        await AsyncTestingUtil.WaitAsync(() => processedInputMessages >= 2 * partitionsPerTopic * (batchSize - 1));
        committedOutputMessages.Should().Be(0);

        // Publish 1 extra message to complete the first 2 batches (1 per topic)
        await publisher.PublishAsync(new TestEventOne());

        await Task.Delay(500);
        committedOutputMessages.Should().BeLessOrEqualTo(batchSize * 2);

        // Complete all batches
        for (int partition = 0; partition < partitionsPerTopic - 1; partition++)
        {
            await publisher.PublishAsync(new TestEventOne());
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync(false);
        committedOutputMessages.Should().Be(batchSize * partitionsPerTopic * 2);
    }

    [Fact]
    public async Task KafkaTransactions_ShouldCreateSingleTransactionWhenProcessingPartitionsTogether()
    {
        const int batchSize = 10;
        int processedInputMessages = 0;
        int committedOutputMessages = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("transactional-id")
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("output")))
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("input")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("input")
                                .ProcessAllPartitionsTogether()
                                .Consume(endpoint => endpoint.ConsumeFrom("input").EnableBatchProcessing(batchSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("output")
                                .Consume<TestEventThree>(endpoint => endpoint.ConsumeFrom("output"))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, IEventPublisher>(HandleInputBatch)
                .AddDelegateSubscriber<TestEventTwo>(HandleOutput)
                .AddIntegrationSpy());

        async ValueTask HandleInputBatch(IAsyncEnumerable<TestEventOne> batch, IEventPublisher publisher)
        {
            using IKafkaTransaction transaction = publisher.InitKafkaTransaction();

            await foreach (TestEventOne dummy in batch)
            {
                await publisher.PublishAsync(new TestEventTwo());
                Interlocked.Increment(ref processedInputMessages);
            }

            transaction.Commit();
        }

        void HandleOutput(TestEventTwo dummy) => Interlocked.Increment(ref committedOutputMessages);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        // Publish incomplete batch (1 message missing)
        for (int i = 0; i < batchSize - 1; i++)
        {
            await publisher.PublishAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => processedInputMessages >= batchSize - 1);
        await Task.Delay(100);
        committedOutputMessages.Should().Be(0);

        // Publish 1 extra message to complete the batch
        await publisher.PublishAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        committedOutputMessages.Should().Be(batchSize);
    }
}
