﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Broker
{
    public class BrokerTest
    {
        private readonly TestBroker _broker = new TestBroker();

        [Fact]
        public void GetProducer_ReturnInstance()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);

            producer.Should().NotBeNull();
        }

        [Fact]
        public void GetProducer_SameEndpoint_ReturnCachedInstance()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);
            var producer2 = _broker.GetProducer(TestEndpoint.Default);

            producer2.Should().BeSameAs(producer);
        }

        [Fact]
        public void GetProducer_DifferentEndpoint_ReturnNewInstance()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);
            var producer2 = _broker.GetProducer(new TestEndpoint("test2"));

            producer2.Should().NotBeSameAs(producer);
        }

        [Fact]
        public void GetConsumer_ReturnInstance()
        {
            var consumer = _broker.GetConsumer(TestEndpoint.Default);

            consumer.Should().NotBeNull();
        }

        [Fact]
        public void GetConsumer_SameEndpoint_ReturnCachedInstance()
        {
            var consumer = _broker.GetConsumer(TestEndpoint.Default);
            var consumer2 = _broker.GetConsumer(new TestEndpoint("test2"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void GetConsumer_DifferentEndpoint_ReturnNewInstance()
        {
            var consumer = _broker.GetConsumer(TestEndpoint.Default);
            var consumer2 = _broker.GetConsumer(new TestEndpoint("test2"));

            consumer2.Should().NotBeSameAs(consumer);
        }

        [Fact]
        public void Produce_IntegrationMessage_IdIsSet()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);

            var message = new TestEventOne();

            producer.Produce(message);

            message.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ProduceAsync_IntegrationMessage_IdIsSet()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);

            var message = new TestEventOne();

            await producer.ProduceAsync(message);

            message.Id.Should().NotBeEmpty();
        }
    }
}