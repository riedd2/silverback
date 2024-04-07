﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.Messaging.HealthChecks;

public class OutboundEndpointsHealthCheckServiceTests
{
    // TODO: Reimplement
    // [Fact]
    // public async Task PingAllEndpoints_AllEndpointsWorking_EachEndpointIsPinged()
    // {
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.IsConnected.Returns(true);
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     IProducer? producer1 = Substitute.For<IProducer>();
    //     IProducer? producer2 = Substitute.For<IProducer>();
    //     IProducer? producer3 = Substitute.For<IProducer>();
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint1")).Returns(Task.FromResult(producer1));
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint2")).Returns(Task.FromResult(producer2));
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint3")).Returns(Task.FromResult(producer3));
    //     IOutboundRoutingConfiguration? configuration = Substitute.For<IOutboundRoutingConfiguration>();
    //     configuration.Routes.Returns(
    //         new List<IOutboundRoute>
    //         {
    //             new OutboundRoute(typeof(TestEventOne), new TestProducerConfiguration("endpoint1")),
    //             new OutboundRoute(typeof(TestEventTwo), new TestProducerConfiguration("endpoint2")),
    //             new OutboundRoute(typeof(TestEventThree), new TestProducerConfiguration("endpoint3"))
    //         });
    //
    //     ProducersHealthCheckService service = new(configuration, new BrokerCollection(new[] { broker }));
    //
    //     await service.SendPingMessagesAsync();
    //
    //     await producer1.ReceivedWithAnyArgs(1).ProduceAsync((PingMessage?)null);
    //     await producer2.ReceivedWithAnyArgs(1).ProduceAsync((PingMessage?)null);
    //     await producer3.ReceivedWithAnyArgs(1).ProduceAsync((PingMessage?)null);
    // }
    //
    // [Fact]
    // public async Task PingAllEndpoints_AllEndpointsWorking_ResultsAreAllSuccess()
    // {
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.IsConnected.Returns(true);
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     IProducer? producer1 = Substitute.For<IProducer>();
    //     IProducer? producer2 = Substitute.For<IProducer>();
    //     IProducer? producer3 = Substitute.For<IProducer>();
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint1")).Returns(Task.FromResult(producer1));
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint2")).Returns(Task.FromResult(producer2));
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint3")).Returns(Task.FromResult(producer3));
    //     IOutboundRoutingConfiguration? configuration = Substitute.For<IOutboundRoutingConfiguration>();
    //     configuration.Routes.Returns(
    //         new List<IOutboundRoute>
    //         {
    //             new OutboundRoute(typeof(TestEventOne), new TestProducerConfiguration("endpoint1")),
    //             new OutboundRoute(typeof(TestEventTwo), new TestProducerConfiguration("endpoint2")),
    //             new OutboundRoute(typeof(TestEventThree), new TestProducerConfiguration("endpoint3"))
    //         });
    //
    //     ProducersHealthCheckService service = new(configuration, new BrokerCollection(new[] { broker }));
    //
    //     IReadOnlyCollection<EndpointCheckResult> results = await service.SendPingMessagesAsync();
    //
    //     results.ForEach(r => r.IsSuccessful.Should().BeTrue());
    // }
    //
    // [Fact]
    // public async Task PingAllEndpoints_SomeEndpointNotWorking_FailureIsProperlyReported()
    // {
    //     IBroker? broker = Substitute.For<IBroker>();
    //     broker.IsConnected.Returns(true);
    //     broker.ProducerConfigurationType.Returns(typeof(TestProducerConfiguration));
    //     broker.ConsumerConfigurationType.Returns(typeof(TestConsumerConfiguration));
    //     IProducer? producer1 = Substitute.For<IProducer>();
    //     IProducer? producer2 = Substitute.For<IProducer>();
    //     producer2.ProduceAsync((PingMessage?)null).ThrowsForAnyArgs<ProduceException>();
    //     IProducer? producer3 = Substitute.For<IProducer>();
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint1")).Returns(Task.FromResult(producer1));
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint2")).Returns(Task.FromResult(producer2));
    //     broker.GetProducerAsync(new TestProducerConfiguration("endpoint3")).Returns(Task.FromResult(producer3));
    //     IOutboundRoutingConfiguration? configuration = Substitute.For<IOutboundRoutingConfiguration>();
    //     configuration.Routes.Returns(
    //         new List<IOutboundRoute>
    //         {
    //             new OutboundRoute(typeof(TestEventOne), new TestProducerConfiguration("endpoint1")),
    //             new OutboundRoute(typeof(TestEventTwo), new TestProducerConfiguration("endpoint2")),
    //             new OutboundRoute(typeof(TestEventThree), new TestProducerConfiguration("endpoint3"))
    //         });
    //
    //     ProducersHealthCheckService service = new(configuration, new BrokerCollection(new[] { broker }));
    //
    //     List<EndpointCheckResult> results = (await service.SendPingMessagesAsync()).ToList();
    //
    //     results[0].EndpointName.Should().Be("endpoint1");
    //     results[0].IsSuccessful.Should().BeTrue();
    //     results[0].ErrorMessage.Should().BeNull();
    //
    //     results[1].EndpointName.Should().Be("endpoint2");
    //     results[1].IsSuccessful.Should().BeFalse();
    //     results[1].ErrorMessage.Should().NotBeNullOrEmpty();
    //
    //     results[2].EndpointName.Should().Be("endpoint3");
    //     results[2].IsSuccessful.Should().BeTrue();
    //     results[2].ErrorMessage.Should().BeNull();
    // }
}
