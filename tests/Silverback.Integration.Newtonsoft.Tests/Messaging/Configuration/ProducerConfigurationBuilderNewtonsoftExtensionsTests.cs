﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Configuration;

public class ProducerConfigurationBuilderNewtonsoftExtensionsTests
{
    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft().Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer>();
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_Configure_SerializerAndOptionsSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft(
            serializer => serializer.Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })).Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer>().Settings.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_WithEncoding_SerializerAndOptionsSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft(
                serializer => serializer
                    .WithEncoding(MessageEncoding.Unicode))
            .Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer>().Encoding.Should().Be(MessageEncoding.Unicode);
    }
}
