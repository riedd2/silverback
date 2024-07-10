﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public class MessageWrapperFixture
{
    private readonly SilverbackContext _context = new(Substitute.For<IServiceProvider>());

    private readonly IMessageWrapper _messageWrapper = new MessageWrapper();

    [Fact]
    public void Instance_ShouldReturnStaticInstance()
    {
        IMessageWrapper instance = MessageWrapper.Instance;

        instance.Should().NotBeNull();
        instance.Should().BeSameAs(MessageWrapper.Instance);
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceEnvelopes()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync(message, _context, [producer1, producer2]);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceConfiguredEnvelopes()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync(
            message,
            _context,
            [producer1, producer2],
            static envelope =>
            {
                envelope.Headers.Add("test", "value");
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceConfiguredEnvelope_WhenPassingArgument()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync(
            message,
            _context,
            [producer1, producer2],
            static (envelope, value) =>
            {
                envelope.Headers.Add("test", value);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            "value");

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task WrapAndProduceAsync_ShouldReturnHandledFlagAccordingToEnableSubscribing(
        bool enableSubscribing1,
        bool enableSubscribing2,
        bool expected)
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one", enableSubscribing1);
        (IProducer producer2, IProduceStrategyImplementation _) = CreateProducer("two", enableSubscribing2);

        bool result = await _messageWrapper.WrapAndProduceAsync(message, _context, [producer1, producer2]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task WrapAndProduceAsync_ShouldReturnHandledFlagAccordingToEnableSubscribing_WhenPassingArgument(
        bool enableSubscribing1,
        bool enableSubscribing2,
        bool expected)
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one", enableSubscribing1);
        (IProducer producer2, IProduceStrategyImplementation _) = CreateProducer("two", enableSubscribing2);

        bool result = await _messageWrapper.WrapAndProduceAsync(
            message,
            _context,
            [producer1, producer2],
            (_, _) =>
            {
            },
            1);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArray()));

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1, producer2]);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(2);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArray()));
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1, producer2],
            envelope =>
            {
                envelope.Headers.Add("x-index", ++count);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(2);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].Headers["x-index"].Should().Be("3");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Headers["x-index"].Should().Be("4");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForCollection_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArray()));

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1, producer2],
            static (envelope, counter) =>
            {
                envelope.Headers.Add("x-index", counter.Increment());
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            new Counter());

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(2);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].Headers["x-index"].Should().Be("3");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Headers["x-index"].Should().Be("4");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task WrapAndProduceBatchAsync_ShouldReturnHandledFlagForCollectionAccordingToEnableSubscribing(
        bool enableSubscribing1,
        bool enableSubscribing2,
        bool expected)
    {
        TestEventOne[] messages = [new TestEventOne(), new TestEventOne()];
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one", enableSubscribing1);
        (IProducer producer2, IProduceStrategyImplementation _) = CreateProducer("two", enableSubscribing2);

        bool result = await _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1, producer2]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task WrapAndProduceBatchAsync_ShouldReturnHandledFlagForCollectionAccordingToEnableSubscribing_WhenPassingArgument(
        bool enableSubscribing1,
        bool enableSubscribing2,
        bool expected)
    {
        TestEventOne[] messages = [new TestEventOne(), new TestEventOne()];
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one", enableSubscribing1);
        (IProducer producer2, IProduceStrategyImplementation _) = CreateProducer("two", enableSubscribing2);

        bool result = await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1, producer2],
            (_, _) =>
            {
            },
            1);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1]);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1],
            envelope =>
            {
                envelope.Headers.Add("x-index", ++count);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1],
            static (envelope, counter) =>
            {
                envelope.Headers.Add("x-index", counter.Increment());
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            new Counter());

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task WrapAndProduceBatchAsync_ShouldReturnHandledFlagForEnumerableAccordingToEnableSubscribing(
        bool enableSubscribing,
        bool expected)
    {
        IEnumerable<TestEventOne> messages = [new TestEventOne(), new TestEventOne()];
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one", enableSubscribing);

        bool result = await _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task WrapAndProduceBatchAsync_ShouldReturnHandledFlagForEnumerableAccordingToEnableSubscribing_WhenPassingArgument(
        bool enableSubscribing,
        bool expected)
    {
        IEnumerable<TestEventOne> messages = [new TestEventOne(), new TestEventOne()];
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one", enableSubscribing);

        bool result = await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1],
            (_, _) =>
            {
            },
            1);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForEnumerable()
    {
        IEnumerable<TestEventOne> messages = [new TestEventOne(), new TestEventOne()];
        IProducer producer1 = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1, producer2]);

        await act.Should().ThrowAsync<RoutingException>()
            .WithMessage(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or Array or any type implementing IReadOnlyCollection.");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForEnumerable_WhenPassingArgument()
    {
        IEnumerable<TestEventOne> messages = [new TestEventOne(), new TestEventOne()];
        IProducer producer1 = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1, producer2],
            (_, _) =>
            {
            },
            1);

        await act.Should().ThrowAsync<RoutingException>()
            .WithMessage(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or Array or any type implementing IReadOnlyCollection.");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForAsyncEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArrayAsync().SafeWait()));

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1]);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArrayAsync().SafeWait()));
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1],
            envelope =>
            {
                envelope.Headers.Add("x-index", ++count);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy1.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArrayAsync().SafeWait()));

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1],
            (envelope, counter) =>
            {
                envelope.Headers.Add("x-index", counter.Increment());
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            new Counter());

        await strategy1.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task WrapAndProduceBatchAsync_ShouldReturnHandledFlagForAsyncEnumerableAccordingToEnableSubscribing(
        bool enableSubscribing,
        bool expected)
    {
        IAsyncEnumerable<TestEventOne> messages = new TestEventOne[] { new(), new() }.ToAsyncEnumerable();
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one", enableSubscribing);

        bool result = await _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1]);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForAsyncEnumerable()
    {
        IAsyncEnumerable<TestEventOne> messages = new TestEventOne[] { new(), new() }.ToAsyncEnumerable();
        IProducer producer1 = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(messages, _context, [producer1, producer2]);

        await act.Should().ThrowAsync<RoutingException>()
            .WithMessage(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or Array or any type implementing IReadOnlyCollection.");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForAsyncEnumerable_WhenPassingArgument()
    {
        IAsyncEnumerable<TestEventOne> messages = new TestEventOne[] { new(), new() }.ToAsyncEnumerable();
        IProducer producer1 = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _context,
            [producer1, producer2],
            (_, _) =>
            {
            },
            1);

        await act.Should().ThrowAsync<RoutingException>()
            .WithMessage(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or Array or any type implementing IReadOnlyCollection.");
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
