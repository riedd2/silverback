﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal sealed class ConsumeLoopHandler : IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly KafkaConsumer _consumer;

    private readonly ISilverbackLogger _logger;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly ConsumerChannelsManager _channelsManager;

    private readonly OffsetsTracker? _offsetsTracker;

    private CancellationTokenSource _cancellationTokenSource = new();

    private TaskCompletionSource<bool>? _consumeTaskCompletionSource;

    private bool _isDisposed;

    public ConsumeLoopHandler(
        KafkaConsumer consumer,
        ConsumerChannelsManager channelsManager,
        OffsetsTracker? offsetsTracker,
        ISilverbackLogger logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _channelsManager = Check.NotNull(channelsManager, nameof(channelsManager));
        _offsetsTracker = offsetsTracker;
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public string Id { get; } = Guid.NewGuid().ToString();

    public Task Stopping => _consumeTaskCompletionSource?.Task ?? Task.CompletedTask;

    public bool IsConsuming { get; private set; }

    public void Start()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (IsConsuming)
            return;

        IsConsuming = true;

        if (_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        if (_consumeTaskCompletionSource == null || _consumeTaskCompletionSource.Task.IsCompleted)
            _consumeTaskCompletionSource = new TaskCompletionSource<bool>();

        TaskCompletionSource<bool>? taskCompletionSource = _consumeTaskCompletionSource;
        CancellationToken cancellationToken = _cancellationTokenSource.Token;

        Task.Factory.StartNew(
                () => ConsumeAsync(taskCompletionSource, cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .FireAndForget();
    }

    public Task StopAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (!IsConsuming)
            return Stopping;

        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "Stopping ConsumeLoopHandler... | instanceId: {instanceId}",
            () => new object[] { Id });

        _cancellationTokenSource.Cancel();

        IsConsuming = false;

        return Stopping;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "Disposing ConsumeLoopHandler... | instanceId: {instanceId}",
            () => new object[] { Id });

        AsyncHelper.RunSynchronously(StopAsync);
        _cancellationTokenSource.Dispose();

        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "ConsumeLoopHandler disposed. | instanceId: {instanceId}",
            () => new object[] { Id });

        _isDisposed = true;
    }

    private async Task ConsumeAsync(
        TaskCompletionSource<bool> taskCompletionSource,
        CancellationToken cancellationToken)
    {
        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;
        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "Starting consume loop... | instanceId: {instanceId}, taskId: {taskId}",
            () => new object[]
            {
                Id,
                taskCompletionSource.Task.Id
            });

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!ConsumeOnce(cancellationToken))
                break;
        }

        _logger.LogConsumerLowLevelTrace(
            _consumer,
            "Consume loop stopped. | instanceId: {instanceId}, taskId: {taskId}",
            () => new object[]
            {
                Id,
                taskCompletionSource.Task.Id
            });

        taskCompletionSource.TrySetResult(true);

        // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
        // synchronously to stay within a single long-running thread with the Consume loop.
        // The call to DisconnectAsync is the only exception since we are exiting anyway and Consume will
        // not be called anymore.
        if (!cancellationToken.IsCancellationRequested)
            await _consumer.Client.DisconnectAsync().ConfigureAwait(false);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private bool ConsumeOnce(CancellationToken cancellationToken)
    {
        try
        {
            ConsumeResult<byte[]?, byte[]?> consumeResult = _consumer.Client.Consume(cancellationToken);

            _logger.LogConsuming(consumeResult, _consumer);

            _offsetsTracker?.TrackOffset(consumeResult.TopicPartitionOffset);
            _channelsManager.Write(consumeResult, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogConsumingCanceled(_consumer, ex);
        }
        catch (ChannelClosedException ex)
        {
            // Ignore the ChannelClosedException as it might be thrown in case of retry
            // (see ConsumerChannelsManager.Reset method)
            _logger.LogConsumingCanceled(_consumer, ex);
        }
        catch (Exception ex)
        {
            AutoRecoveryIfEnabled(ex);
            return false;
        }

        return true;
    }

    private void AutoRecoveryIfEnabled(Exception ex)
    {
        if (!_consumer.Configuration.EnableAutoRecovery)
        {
            _logger.LogKafkaExceptionNoAutoRecovery(_consumer, ex);
            return;
        }

        _logger.LogKafkaExceptionAutoRecovery(_consumer, ex);

        _consumer.TriggerReconnectAsync().FireAndForget();
    }
}
