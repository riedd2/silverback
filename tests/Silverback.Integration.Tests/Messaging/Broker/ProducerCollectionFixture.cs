﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ProducerCollectionFixture
{
    [Fact]
    public void Add_ShouldAddProducer()
    {
        ProducerCollection producerCollection = [];
        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic1"));

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));

        producerCollection.Add(producer1);
        producerCollection.Add(producer2);

        producerCollection.Should().HaveCount(2);
        producerCollection.Should().BeEquivalentTo(new[] { producer1, producer2 });
    }

    [Fact]
    public void Add_ShouldThrow_WhenFriendlyNameNotUnique()
    {
        ProducerCollection producerCollection = [];
        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "one"
            });

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "two"
            });

        IProducer producer2Bis = Substitute.For<IProducer>();
        producer2Bis.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "two"
            });

        producerCollection.Add(producer1);
        producerCollection.Add(producer2);

        Action act = () => producerCollection.Add(producer2Bis);

        act.Should().Throw<InvalidOperationException>().WithMessage("A producer endpoint with the name 'two' has already been added.");
    }

    [Fact]
    public void GetProducerForEndpoint_ShouldReturnProducerByEndpointName()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic1"));
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        producerCollection.Add(producer2);

        IProducer producer = producerCollection.GetProducerForEndpoint("topic1");

        producer.Should().Be(producer1);
    }

    [Fact]
    public void GetProducerForEndpoint_ShouldReturnProducerByFriendlyName()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "one"
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                FriendlyName = "two"
            });
        producerCollection.Add(producer2);

        IProducer producer = producerCollection.GetProducerForEndpoint("two");

        producer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducerForEndpoint_ShouldThrow_WhenProducerNotFound()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic1"));
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        producerCollection.Add(producer2);

        Action act = () => producerCollection.GetProducerForEndpoint("not-configured-topic");

        act.Should().Throw<InvalidOperationException>().WithMessage("No producer has been configured for endpoint 'not-configured-topic'.");
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForMessage()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer eventOneProducer = producerCollection.GetProducersForMessage(new TestEventOne()).Single();
        IProducer eventTwoProducer = producerCollection.GetProducersForMessage(new TestEventTwo()).Single();

        eventOneProducer.Should().Be(producer1);
        eventTwoProducer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForEnumerable()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IEnumerable<TestEventOne> events1 = [new TestEventOne(), new TestEventOne()];
        IEnumerable<TestEventTwo> events2 = [new TestEventTwo(), new TestEventTwo()];

        IProducer eventOneProducer = producerCollection.GetProducersForMessage(events1).Single();
        IProducer eventTwoProducer = producerCollection.GetProducersForMessage(events2).Single();

        eventOneProducer.Should().Be(producer1);
        eventTwoProducer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForAsyncEnumerable()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IAsyncEnumerable<TestEventOne> events1 = new[] { new TestEventOne(), new TestEventOne() }.ToAsyncEnumerable();
        IAsyncEnumerable<TestEventTwo> events2 = new[] { new TestEventTwo(), new TestEventTwo() }.ToAsyncEnumerable();

        IProducer eventOneProducer = producerCollection.GetProducersForMessage(events1).Single();
        IProducer eventTwoProducer = producerCollection.GetProducersForMessage(events2).Single();

        eventOneProducer.Should().Be(producer1);
        eventTwoProducer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnAllProducersForMessage()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer2);

        IReadOnlyCollection<IProducer> eventOneProducers = producerCollection.GetProducersForMessage(new TestEventOne());
        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(new TestEventTwo());

        eventOneProducers.Should().HaveCount(2);
        eventTwoProducers.Should().BeEmpty();
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForTombstone()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer tombstoneProducer1 = producerCollection.GetProducersForMessage(new Tombstone<TestEventOne>("42")).Single();
        IProducer tombstoneProducer2 = producerCollection.GetProducersForMessage(new Tombstone<TestEventTwo>("42")).Single();

        tombstoneProducer1.Should().Be(producer1);
        tombstoneProducer2.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldIgnoreNonRoutingProducers()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer2, false);

        IReadOnlyCollection<IProducer> eventOneProducers = producerCollection.GetProducersForMessage(new TestEventOne());

        eventOneProducers.Should().HaveCount(1);
        eventOneProducers.Single().Should().Be(producer1);
    }

    [Fact]
    public void GetProducersForMessage_ShouldIgnoreNonRoutingProducersForTombstone()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer2, false);

        IReadOnlyCollection<IProducer> eventOneProducers = producerCollection.GetProducersForMessage(new Tombstone<TestEventOne>("42"));

        eventOneProducers.Should().HaveCount(1);
        eventOneProducers.Single().Should().Be(producer1);
    }

    [Fact]
    public void GetProducersForMessage_ShouldIgnoreNonRoutingProducersForEnumerable()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer2, false);

        IReadOnlyCollection<IProducer> eventOneProducers = producerCollection.GetProducersForMessage(new[] { new TestEventOne() });

        eventOneProducers.Should().HaveCount(1);
        eventOneProducers.Single().Should().Be(producer1);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnEmptyCollection_WhenNoProducerIsCompatibleWithMessage()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(new TestEventTwo());

        eventTwoProducers.Should().BeEmpty();
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnEmptyCollection_WhenNoProducerIsCompatibleWithEnumerable()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IEnumerable<TestEventTwo> events2 = [new TestEventTwo(), new TestEventTwo()];

        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(events2);

        eventTwoProducers.Should().BeEmpty();
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnEmptyCollection_WhenNoProducerIsCompatibleWithAsyncEnumerable()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IAsyncEnumerable<TestEventTwo> events2 = new[] { new TestEventTwo(), new TestEventTwo() }.ToAsyncEnumerable();

        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(events2);

        eventTwoProducers.Should().BeEmpty();
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForMessageType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer eventOneProducer = producerCollection.GetProducersForMessage(typeof(TestEventOne)).Single();
        IProducer eventTwoProducer = producerCollection.GetProducersForMessage(typeof(TestEventTwo)).Single();

        eventOneProducer.Should().Be(producer1);
        eventTwoProducer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnAllProducersForMessageType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer2);

        IReadOnlyCollection<IProducer> eventOneProducers = producerCollection.GetProducersForMessage(typeof(TestEventOne));
        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(typeof(TestEventTwo));

        eventOneProducers.Should().HaveCount(2);
        eventTwoProducers.Should().BeEmpty();
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForTombstoneType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer tombstoneProducer1 = producerCollection.GetProducersForMessage(typeof(Tombstone<TestEventOne>)).Single();
        IProducer tombstoneProducer2 = producerCollection.GetProducersForMessage(typeof(Tombstone<TestEventTwo>)).Single();

        tombstoneProducer1.Should().Be(producer1);
        tombstoneProducer2.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForMessageWithHeadersType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer resolvedProducer1 = producerCollection.GetProducersForMessage(typeof(MessageWithHeaders<TestEventOne>)).Single();
        IProducer resolvedProducer2 = producerCollection.GetProducersForMessage(typeof(MessageWithHeaders<TestEventTwo>)).Single();

        resolvedProducer1.Should().Be(producer1);
        resolvedProducer2.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForMessageWithHeadersEnumerableType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer resolvedProducer1 = producerCollection.GetProducersForMessage(typeof(List<MessageWithHeaders<TestEventOne>>)).Single();
        IProducer resolvedProducer2 = producerCollection.GetProducersForMessage(typeof(List<MessageWithHeaders<TestEventTwo>>)).Single();

        resolvedProducer1.Should().Be(producer1);
        resolvedProducer2.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnEmptyCollection_WhenNoProducerIsCompatibleWithMessageType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(typeof(TestEventTwo));

        eventTwoProducers.Should().BeEmpty();
    }
}