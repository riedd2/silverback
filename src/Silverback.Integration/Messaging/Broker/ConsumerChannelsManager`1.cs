﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal abstract class ConsumerChannelsManager<TChannel> : IDisposable
    where TChannel : IConsumerChannel
{
    private readonly IConsumer _consumer;

    private readonly ISilverbackLogger _logger;

    private bool _isDisposed;

    protected ConsumerChannelsManager(IConsumer consumer, ISilverbackLogger logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public Task Stopping => Task.WhenAll(GetChannels().Select(channel => channel.ReadTask));

    public Task StopReadingAsync() => Task.WhenAll(GetChannels().Select(StopReadingAsync));

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract IEnumerable<TChannel> GetChannels();

    protected void StartReading(TChannel channel)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        // Clear the current activity to ensure we don't propagate the previous traceId (e.g. when restarting because of a rollback)
        Activity.Current = null;

        if (channel.ReadCancellationToken.IsCancellationRequested && !channel.ReadTask.IsCompleted)
        {
            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Deferring processing loop startup of channel {channel}...",
                () => [channel.Id]);

            // If the cancellation is still pending await it and restart after successful stop
            Task.Run(
                    async () =>
                    {
                        await channel.ReadTask.ConfigureAwait(false);
                        StartReading(channel);
                    })
                .FireAndForget();

            return;
        }

        if (!channel.StartReading())
            return;

        Task.Run(() => ReadChannelAsync(channel)).FireAndForget();
    }

    protected virtual async Task StopReadingAsync(TChannel channel)
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "Stopping processing loop of channel {channel}...",
            () => [channel.Id]);

        await channel.StopReadingAsync().ConfigureAwait(false);
        channel.Complete();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _isDisposed)
            return;

        StopReadingAsync().SafeWait();
        GetChannels().OfType<IDisposable>().ForEach(channel => channel.Dispose());

        _logger.LogConsumerLowLevelTrace(_consumer, "All channels disposed.");

        _isDisposed = true;
    }

    protected abstract Task ReadChannelOnceAsync(TChannel channel);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task ReadChannelAsync(TChannel channel)
    {
        try
        {
            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Starting processing loop of channel {channel}...",
                () => [channel.Id]);

            while (!channel.ReadCancellationToken.IsCancellationRequested)
            {
                _logger.LogConsumerLowLevelTrace(
                    _consumer,
                    "Reading channel {channel}...",
                    () => [channel.Id]);

                await ReadChannelOnceAsync(channel).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Exiting processing loop of channel {channel} (operation canceled).",
                () => [channel.Id]);
        }
        catch (Exception ex)
        {
            if (ex is not ConsumerPipelineFatalException)
                _logger.LogConsumerFatalError(_consumer, ex);

            await channel.NotifyReadingStoppedAsync(true).ConfigureAwait(false);

            await _consumer.Client.DisconnectAsync().ConfigureAwait(false);
        }

        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "Exited processing loop of channel {channel}.",
            () => [channel.Id]);

        await channel.NotifyReadingStoppedAsync(false).ConfigureAwait(false);
    }
}
