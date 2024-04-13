﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

public partial class SilverbackBuilderFixture
{
    [Fact]
    public void WithLogLevels_ShouldSetLogLevelsDictionary()
    {
        ServiceCollection services = [];

        services
            .AddFakeLogger()
            .AddSilverback()
            .WithLogLevels(
                configurator => configurator
                    .SetLogLevel(CoreLogEvents.BackgroundServiceException.EventId, LogLevel.Information)
                    .SetLogLevel(CoreLogEvents.LockAcquired.EventId, LogLevel.Warning));

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<ISilverbackLogger<object>>().Should().NotBeNull();
        serviceProvider.GetRequiredService<LogLevelDictionary>().Should().HaveCount(2);
    }
}