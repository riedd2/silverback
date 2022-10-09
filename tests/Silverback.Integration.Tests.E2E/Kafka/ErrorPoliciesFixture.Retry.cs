﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesFixture
{
    [Fact]
    public async Task RetryPolicy_ShouldRetryProcessingMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        tryCount.Should().Be(11);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryProcessingMultipleTimes_WhenProcessingMultiplePartitions()
    {
        int tryCount = 0;
        int consumedCount = 0;
        SemaphoreSlim semaphore = new(0);

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
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(2).ThenSkip()))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne != "1")
            {
                Interlocked.Increment(ref consumedCount);
                return;
            }

            tryCount++;

            semaphore.Wait();
            semaphore.Release();
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await AsyncTestingUtil.WaitAsync(() => tryCount == 1);

        semaphore.Release();
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        tryCount.Should().Be(3);
        consumedCount.Should().Be(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryProcessingMultipleTimes_WhenProcessingMultiplePartitionsAllTogether()
    {
        int tryCount = 0;
        int consumedCount = 0;
        SemaphoreSlim semaphore = new(0);

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
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .ProcessAllPartitionsTogether()
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(2).ThenSkip()))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne != "1")
            {
                Interlocked.Increment(ref consumedCount);
                return;
            }

            tryCount++;

            semaphore.Wait();
            semaphore.Release();
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await AsyncTestingUtil.WaitAsync(() => tryCount == 1);

        semaphore.Release();
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        tryCount.Should().Be(3);
        consumedCount.Should().Be(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
    }

    [Fact]
    public async Task RetryPolicy_ShouldCommit_WhenSuccessfulAfterSomeRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldNotCommit_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryChunkedJsonMultipleTimes()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            if (tryCount % 2 != 0)
                throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "Long message one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "Long message two" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(8);
        Helper.Spy.RawOutboundEnvelopes.ForEach(
            envelope =>
            {
                envelope.RawMessage.ShouldNotBeNull();
                envelope.RawMessage.Length.Should().BeLessOrEqualTo(10);
            });

        tryCount.Should().Be(4);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("Long message one");
        Helper.Spy.InboundEnvelopes[1].Message.As<TestEventOne>().ContentEventOne.Should().Be("Long message one");
        Helper.Spy.InboundEnvelopes[2].Message.As<TestEventOne>().ContentEventOne.Should().Be("Long message two");
        Helper.Spy.InboundEnvelopes[3].Message.As<TestEventOne>().ContentEventOne.Should().Be("Long message two");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(8);
    }

    [SuppressMessage("ReSharper", "MustUseReturnValue", Justification = "Test code")]
    [Fact]
    public async Task RetryPolicy_ShouldRetryChunkedBinaryMessageMultipleTimes()
    {
        BinaryMessage message1 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "application/pdf" };
        BinaryMessage message2 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "text/plain" };

        int tryCount = 0;
        TestingCollection<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage)
        {
            if (binaryMessage.ContentType != "text/plain")
            {
                tryCount++;

                if (tryCount != 2)
                {
                    // Read first chunk only
                    byte[] buffer = new byte[10];
                    binaryMessage.Content!.Read(buffer, 0, 10);
                    throw new InvalidOperationException("Retry!");
                }
            }

            receivedFiles.Add(binaryMessage.Content.ReadAll());
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(2);

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.Should().Be(10));
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[2].Message.As<BinaryMessage>().ContentType.Should().Be("text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
        receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryEncryptedMessageMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        Stream rawMessage = await DefaultSerializers.Json.SerializeAsync(message);
        int tryCount = 0;

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
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReadAll().Should().NotBeEquivalentTo(rawMessage.ReReadAll());
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryEncryptedChunkedMessageMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        Stream rawMessage = await DefaultSerializers.Json.SerializeAsync(message);
        int tryCount = 0;

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
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(8);
        Helper.Spy.RawOutboundEnvelopes[0].RawMessage.ReReadAll().Should().NotBeEquivalentTo(rawMessage.Read(10));
        Helper.Spy.RawOutboundEnvelopes.ForEach(
            envelope =>
            {
                envelope.RawMessage.Should().NotBeNull();
                envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
            });
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryBatchMultipleTimes_WhenThrowingWhileEnumerating()
    {
        int tryMessageCount = 0;
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(2)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleBatch)
                .AddIntegrationSpy());

        async ValueTask HandleBatch(IAsyncEnumerable<IIntegrationEvent> batch)
        {
            await foreach (IIntegrationEvent dummy in batch)
            {
                tryMessageCount++;
                if (tryMessageCount != 2 && tryMessageCount != 4 && tryMessageCount != 5)
                    throw new InvalidOperationException($"Retry {tryMessageCount}!");
            }

            completedBatches++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(5);

        completedBatches.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryBatchMultipleTimes_WhenThrowingAfterEnumerationCompleted()
    {
        int tryCount = 0;
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(2)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleBatch)
                .AddIntegrationSpy());

        async ValueTask HandleBatch(IAsyncEnumerable<IIntegrationEvent> batch)
        {
            await foreach (IIntegrationEvent dummy in batch)
            {
            }

            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");

            completedBatches++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(6);

        completedBatches.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldStopConsumer_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
        consumer.Client.Status.Should().Be(ClientStatus.Disconnected);
    }
}