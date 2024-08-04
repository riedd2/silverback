﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

public partial class IntegrationPublisherExtensionsFixture
{
    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForMappedCollection()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()));

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" });

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(3);
        capturedEnvelopes1[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[2].Message.Should().Be(null);
        capturedEnvelopes1[2].Endpoint.RawName.Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(3);
        capturedEnvelopes2[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[2].Message.Should().Be(null);
        capturedEnvelopes2[2].Endpoint.RawName.Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedCollection()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()));
        int count = 0;

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (envelope, source) => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(3);
        capturedEnvelopes1[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes1[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes1[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[2].Message.Should().Be(null);
        capturedEnvelopes1[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes1[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes1[2].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(3);
        capturedEnvelopes2[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].GetKafkaKey().Should().Be("4");
        capturedEnvelopes2[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].GetKafkaKey().Should().Be("5");
        capturedEnvelopes2[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[2].Message.Should().Be(null);
        capturedEnvelopes2[2].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[2].GetKafkaKey().Should().Be("6");
        capturedEnvelopes2[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes2[2].Headers["x-topic"].Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedCollection_WhenPassingArgument()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()));

        _publisher.WrapAndPublishBatch(
            sources,
            static (source, counter) =>
            {
                counter.Increment();
                return source == null ? null : new TestEventOne { Content = $"{source}-{counter.Value}" };
            },
            static (envelope, source, counter) => envelope
                .SetKafkaKey($"{counter.Value}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter());

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(3);
        capturedEnvelopes1[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1-1" });
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes1[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2-2" });
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes1[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[2].Message.Should().Be(null);
        capturedEnvelopes1[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes1[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes1[2].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(3);
        capturedEnvelopes2[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1-4" });
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].GetKafkaKey().Should().Be("4");
        capturedEnvelopes2[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2-5" });
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].GetKafkaKey().Should().Be("5");
        capturedEnvelopes2[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[2].Message.Should().Be(null);
        capturedEnvelopes2[2].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[2].GetKafkaKey().Should().Be("6");
        capturedEnvelopes2[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes2[2].Headers["x-topic"].Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldPublishToInternalBusForMappedCollectionAccordingToEnableSubscribing()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy1.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy2.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy2.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy3.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy3.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" });

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldPublishToInternalBusForConfiguredMappedCollectionAccordingToEnableSubscribing()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy1.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy2.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy2.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy3.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy3.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _) =>
            {
            });

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldPublishToInternalBusForConfiguredMappedCollectionAccordingToEnableSubscribing_WhenPassingArgument()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy1.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy2.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy2.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy3.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy3.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));

        _publisher.WrapAndPublishBatch(
            sources,
            (source, _) => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _, _) =>
            {
            },
            1);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }
}
