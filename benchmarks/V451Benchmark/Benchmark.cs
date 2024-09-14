﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Benchmarks.Latest;

public abstract class Benchmark
{
    private IPublisher? _publisher;

    protected IHost? Host { get; private set; }

    protected IPublisher Publisher => _publisher ?? throw new InvalidOperationException("The publisher is not initialized yet.");

    [GlobalSetup]
    public virtual void Setup()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(
                services =>
                {
                    ConfigureServices(services);
                    ConfigureSilverback(services.AddSilverback());
                })
            .Build();

        _publisher = Host.Services.GetRequiredService<IPublisher>();
    }

    [GlobalCleanup]
    public virtual void Cleanup() => Host?.Dispose();

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected virtual void ConfigureSilverback(ISilverbackBuilder silverbackBuilder)
    {
    }
}