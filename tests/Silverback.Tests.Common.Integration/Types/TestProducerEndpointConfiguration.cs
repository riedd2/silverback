﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types;

public sealed record TestProducerEndpointConfiguration : ProducerEndpointConfiguration<TestProducerEndpoint>
{
    public TestProducerEndpointConfiguration()
    {
    }

    public TestProducerEndpointConfiguration(string topic, Type? messageType = null)
    {
        Endpoint = new TestStaticProducerEndpointResolver(topic);

        if (messageType != null)
            MessageType = messageType;
    }

    public TestProducerEndpointConfiguration(params string[] topic)
    {
        Endpoint = new TestDynamicProducerEndpointResolver(topic[0]);
    }

    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "A new instance is desired")]
    public static TestProducerEndpointConfiguration GetDefault() => new()
    {
        Endpoint = new TestStaticProducerEndpointResolver("test")
    };

    public TestProducerEndpoint GetDefaultEndpoint() =>
        (TestProducerEndpoint)Endpoint.GetEndpoint(null, this, Substitute.For<IServiceProvider>());
}