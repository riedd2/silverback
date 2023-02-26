﻿using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Samples.Kafka.Avro.Producer;

public class ProducerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly ILogger<ProducerBackgroundService> _logger;

    public ProducerBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ProducerBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create a service scope and resolve the IPublisher
        // (the IPublisher cannot be resolved from the root scope and cannot
        // therefore be directly injected into the BackgroundService)
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        int number = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProduceMessageAsync(publisher, ++number);

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task ProduceMessageAsync(IPublisher publisher, int number)
    {
        try
        {
            await publisher.PublishAsync(
                new AvroMessage
                {
                    number = number.ToString(CultureInfo.InvariantCulture)
                });

            _logger.LogInformation("Produced {Number}", number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {Number}", number);
        }
    }
}
