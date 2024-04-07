﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ProducerEndpointBuilderSerializeAsAvroExtensionsFixture
{
    [Fact]
    public void SerializeAsAvro_ShouldThrow_WhenTypeNotSpecified()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        Action act = () => builder.SerializeAsAvro();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder.SerializeAsAvro().Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(serializer => serializer.UseModel<TestEventOne>())
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer_WhenUseModelIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(serializer => serializer.UseModel(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }
}
