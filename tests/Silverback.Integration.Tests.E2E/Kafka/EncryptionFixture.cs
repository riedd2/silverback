﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class EncryptionFixture : KafkaFixture
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    private static readonly byte[] AesEncryptionKey2 = BytesUtil.GetRandomBytes(32);

    public EncryptionFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Encryption_ShouldEncryptAndDecrypt()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        TestEventOne message2 = new() { ContentEventOne = "Message 2" };

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
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
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message1);
        await publisher.PublishEventAsync(message2);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
        Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
        Helper.Spy.OutboundEnvelopes.ForEach(envelope => envelope.Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull());

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task Encryption_ShouldAllowKeyRotation()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        TestEventTwo message2 = new() { ContentEventTwo = "Message 2" };
        const string keyIdentifier1 = "my-encryption-key-1";
        const string keyIdentifier2 = "my-encryption-key-2";

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey, keyIdentifier1))
                                .Produce<TestEventTwo>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey2, keyIdentifier2)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(
                                            keyIdentifier => keyIdentifier switch
                                            {
                                                keyIdentifier1 => AesEncryptionKey,
                                                _ => AesEncryptionKey2
                                            }))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message1);
        await publisher.PublishEventAsync(message2);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
        Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier1);
        Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
        Helper.Spy.OutboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier2);

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
        Helper.Spy.InboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier1);
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        Helper.Spy.InboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().Be(keyIdentifier2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task Encryption_ShouldUseDefaultKey_WhenRotatingButMissingKeyHeader()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        TestEventOne message2 = new() { ContentEventOne = "Message 2" };

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(
                                            keyIdentifier => keyIdentifier switch
                                            {
                                                "another-encryption-key-id" => AesEncryptionKey2,
                                                _ => AesEncryptionKey
                                            }))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message1);
        await publisher.PublishEventAsync(message2);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
        Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull();
        Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
        Helper.Spy.OutboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
        Helper.Spy.InboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull();
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        Helper.Spy.InboundEnvelopes[1].Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).Should().BeNull();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task Encryption_ShouldEncryptAndDecryptChunkedJson()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        Stream rawMessageStream1 = DefaultSerializers.Json.Serialize(message1);

        TestEventOne message2 = new() { ContentEventOne = "Message 2" };
        Stream rawMessageStream2 = DefaultSerializers.Json.Serialize(message2);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
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
                                        .DecryptUsingAes(AesEncryptionKey))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message1);
        await publisher.PublishEventAsync(message2);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(12);

        for (int i = 0; i < 6; i++)
        {
            Helper.Spy.RawOutboundEnvelopes[i].RawMessage.Should().NotBeNull();
            Helper.Spy.RawOutboundEnvelopes[i].RawMessage!.Length.Should().BeLessOrEqualTo(10);
            Helper.Spy.RawOutboundEnvelopes[i].RawMessage.ReReadAll().Should()
                .NotBeEquivalentTo(rawMessageStream1.ReReadAll().Skip(i * 10).Take(10));
        }

        for (int i = 0; i < 6; i++)
        {
            Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.Should().NotBeNull();
            Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage!.Length.Should().BeLessOrEqualTo(10);
            Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.ReReadAll().Should()
                .NotBeEquivalentTo(rawMessageStream2.ReReadAll().Skip(i * 10).Take(10));
        }

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(12);
    }

    [Fact]
    public async Task Encryption_ShouldEncryptAndDecryptBinaryMessages()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain"
        };

        TestingCollection<byte[]?> receivedFiles = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
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
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeOfType<SymmetricEncryptStream>();
        Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeOfType<SymmetricEncryptStream>();

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task Encryption_ShouldEncryptAndDecryptChunkedBinaryMessage()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "text/plain"
        };

        List<byte[]?> receivedFiles = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
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
                                        .EnableChunking(10)
                                        .EncryptUsingAes(AesEncryptionKey)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(12);

        for (int i = 0; i < 6; i++)
        {
            Helper.Spy.RawOutboundEnvelopes[i].RawMessage.Should().NotBeNull();
            Helper.Spy.RawOutboundEnvelopes[i].RawMessage!.Length.Should().BeLessOrEqualTo(10);
            Helper.Spy.RawOutboundEnvelopes[i].RawMessage.ReReadAll().Should()
                .NotBeEquivalentTo(message1.Content.ReReadAll().Skip(i * 10).Take(10));
        }

        for (int i = 0; i < 6; i++)
        {
            Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.Should().NotBeNull();
            Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage!.Length.Should().BeLessOrEqualTo(10);
            Helper.Spy.RawOutboundEnvelopes[i + 6].RawMessage.ReReadAll().Should()
                .NotBeEquivalentTo(message2.Content.ReReadAll().Skip(i * 10).Take(10));
        }

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }
}