﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Validation;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Encrypt(new SymmetricEncryptionSettings()).Build();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Constructor_ShouldSetDisplayName()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "display-name");

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.DisplayName.Should().Be("display-name (test)");
    }

    [Fact]
    public void SerializeUsing_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        BinaryMessageSerializer serializer = new();

        TestProducerEndpointConfiguration endpoint = builder.SerializeUsing(serializer).Build();

        endpoint.Serializer.Should().BeSameAs(serializer);
    }

    [Fact]
    public void Encrypt_ShouldSetEncryptionSettings()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        SymmetricEncryptionSettings encryptionSettings = new()
        {
            AlgorithmName = "TripleDES",
            Key = new byte[10]
        };

        TestProducerEndpointConfiguration endpoint = builder.Encrypt(encryptionSettings).Build();

        endpoint.Encryption.Should().BeSameAs(encryptionSettings);
    }

    [Fact]
    public void UseStrategy_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-endpoint");
        OutboxProduceStrategy strategy = new(new InMemoryOutboxSettings());

        TestProducerEndpointConfiguration endpoint = builder.UseStrategy(strategy).Build();

        endpoint.Strategy.Should().BeSameAs(strategy);
    }

    [Fact]
    public void ProduceDirectly_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder.ProduceDirectly().Build();

        endpoint.Strategy.Should().BeOfType<DefaultProduceStrategy>();
    }

    [Fact]
    public void ProduceToOutbox_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-endpoint");

        InMemoryOutboxSettings settings = new();
        TestProducerEndpointConfiguration endpoint = builder.ProduceToOutbox(settings).Build();

        endpoint.Strategy.Should().BeOfType<OutboxProduceStrategy>();
        endpoint.Strategy.As<OutboxProduceStrategy>().Settings.Should().Be(settings);
    }

    [Fact]
    public void ProduceToOutbox_ShouldSetStrategyUsingBuilder()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-endpoint");

        TestProducerEndpointConfiguration endpoint = builder
            .ProduceToOutbox(outbox => outbox.UseMemory().WithName("test-outbox"))
            .Build();

        endpoint.Strategy.Should().BeOfType<OutboxProduceStrategy>();
        endpoint.Strategy.As<OutboxProduceStrategy>().Settings.As<InMemoryOutboxSettings>().OutboxName.Should().Be("test-outbox");
    }

    [Fact]
    public void EnableChunking_ShouldSetChunkSettings()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder.EnableChunking(42, false).Build();

        endpoint.Chunk.Should().NotBeNull();
        endpoint.Chunk!.Size.Should().Be(42);
        endpoint.Chunk!.AlwaysAddHeaders.Should().BeFalse();
    }

    [Fact]
    public void Build_ShouldSetMessageValidationModeToLogWarningByDefault()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void DisableMessageValidation_ShouldSetMessageValidationMode()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder.DisableMessageValidation().Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.None);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsFalse()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder.ValidateMessage(false).Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsTrue()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder.ValidateMessage(true).Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.ThrowException);
    }
}
