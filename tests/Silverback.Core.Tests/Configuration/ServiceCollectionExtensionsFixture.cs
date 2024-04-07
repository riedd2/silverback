﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

public class ServiceCollectionExtensionsFixture
{
    [Fact]
    public void AddSilverback_ShouldReturnSilverbackBuilder()
    {
        ServiceCollection serviceCollection = [];

        SilverbackBuilder builder = serviceCollection.AddSilverback();

        builder.Should().BeOfType<SilverbackBuilder>();
        builder.Services.Should().BeSameAs(serviceCollection);
    }

    [Fact]
    public void AddSilverback_ShouldRegisterBasicServices()
    {
        ServiceCollection serviceCollection = [];

        serviceCollection.AddFakeLogger().AddSilverback();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        serviceProvider.GetService<BusOptions>().Should().BeOfType<BusOptions>();
        serviceProvider.GetService<IPublisher>().Should().BeOfType<Publisher>();
    }

    [Fact]
    public void AddSilverback_ShouldBeIdempotent()
    {
        ServiceCollection serviceCollection = [];

        serviceCollection.AddSilverback();
        serviceCollection.AddSilverback();

        serviceCollection.GetAll<BusOptions>().Should().HaveCount(1);
    }

    [Fact]
    public void ConfigureSilverback_ShouldReturnSilverbackBuilder()
    {
        ServiceCollection serviceCollection = [];

        SilverbackBuilder builder = serviceCollection.ConfigureSilverback();

        builder.Should().BeOfType<SilverbackBuilder>();
        builder.Services.Should().BeSameAs(serviceCollection);
    }

    [Fact]
    public void AddSilverback_ShouldRegisterDefaultDistributedLockFactory()
    {
        ServiceCollection serviceCollection = [];

        serviceCollection.AddFakeLogger().AddSilverback();

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IDistributedLockFactory factory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        DistributedLockFactory defaultFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        factory.Should().NotBeNull();
        factory.Should().BeSameAs(defaultFactory);
    }
}
