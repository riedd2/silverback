﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Rabbit;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public sealed class RabbitProducer : Producer<RabbitBroker, RabbitProducerEndpoint>, IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly IRabbitConnectionFactory _connectionFactory;

        private readonly IOutboundLogger<Producer> _logger;

        private readonly Channel<QueuedMessage> _queueChannel = Channel.CreateUnbounded<QueuedMessage>();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly Dictionary<string, IModel> _rabbitChannels = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public RabbitProducer(
            RabbitBroker broker,
            RabbitProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            IOutboundLogger<Producer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            _connectionFactory = serviceProvider.GetRequiredService<IRabbitConnectionFactory>();

            _logger = Check.NotNull(logger, nameof(logger));

            Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Flush();

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _rabbitChannels.Values.ForEach(channel => channel.Dispose());
            _rabbitChannels.Clear();
        }

        /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope)" />
        protected override IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() => ProduceCoreAsync(envelope));

        /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope,Action,Action{Exception})" />
        [SuppressMessage("", "VSTHRD110", Justification = "Result observed via ContinueWith")]
        protected override void ProduceCore(
            IOutboundEnvelope envelope,
            Action onSuccess,
            Action<Exception> onError)
        {
            Check.NotNull(envelope, nameof(envelope));

            var queuedMessage = new QueuedMessage(envelope);

            AsyncHelper.RunValueTaskSynchronously(() => _queueChannel.Writer.WriteAsync(queuedMessage));

            queuedMessage.TaskCompletionSource.Task.ContinueWith(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                        onSuccess.Invoke();
                    else
                        onError.Invoke(task.Exception);
                },
                TaskScheduler.Default);
        }

        /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var queuedMessage = new QueuedMessage(envelope);

            await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);
            await queuedMessage.TaskCompletionSource.Task.ConfigureAwait(false);

            return null;
        }

        /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope,Action,Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            IOutboundEnvelope envelope,
            Action onSuccess,
            Action<Exception> onError)
        {
            Check.NotNull(envelope, nameof(envelope));

            var queuedMessage = new QueuedMessage(envelope);

            await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);

            queuedMessage.TaskCompletionSource.Task.ContinueWith(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                        onSuccess.Invoke();
                    else
                        onError.Invoke(task.Exception);
                },
                TaskScheduler.Default)
                .FireAndForget();
        }

        private static string GetRoutingKey(IEnumerable<MessageHeader> headers) =>
            headers?.FirstOrDefault(header => header.Name == RabbitMessageHeaders.RoutingKey)?.Value ??
            string.Empty;

        [SuppressMessage("", "CA1031", Justification = "Exception logged/returned")]
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var queuedMessage = await _queueChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        PublishToChannel(queuedMessage.Envelope);

                        queuedMessage.TaskCompletionSource.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        queuedMessage.TaskCompletionSource.SetException(
                            new ProduceException(
                                "Error occurred producing the message. See inner exception for details.",
                                ex));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogProducerQueueProcessingCanceled(this);
            }
        }

        private void PublishToChannel(IRawOutboundEnvelope envelope)
        {
            if (!_rabbitChannels.TryGetValue(envelope.ActualEndpointName, out var channel))
            {
                channel = _connectionFactory.GetChannel(Endpoint, envelope.ActualEndpointName);
                _rabbitChannels[envelope.ActualEndpointName] = channel;
            }

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true; // TODO: Make it configurable?
            properties.Headers = envelope.Headers.ToDictionary(
                header => header.Name,
                header => (object?)header.Value);

            string? routingKey;

            switch (Endpoint)
            {
                case RabbitQueueProducerEndpoint queueEndpoint:
                    routingKey = queueEndpoint.Name;
                    channel.BasicPublish(
                        string.Empty,
                        routingKey,
                        properties,
                        envelope.RawMessage.ReadAll());
                    break;
                case RabbitExchangeProducerEndpoint exchangeEndpoint:
                    routingKey = GetRoutingKey(envelope.Headers);
                    channel.BasicPublish(
                        exchangeEndpoint.Name,
                        routingKey,
                        properties,
                        envelope.RawMessage.ReadAll());
                    break;
                default:
                    throw new ArgumentException("Unhandled endpoint type.");
            }

            if (Endpoint.ConfirmationTimeout.HasValue)
                channel.WaitForConfirmsOrDie(Endpoint.ConfirmationTimeout.Value);
        }

        private void Flush()
        {
            _queueChannel.Writer.Complete();
            _queueChannel.Reader.Completion.Wait();
        }

        private class QueuedMessage
        {
            public QueuedMessage(IRawOutboundEnvelope envelope)
            {
                Envelope = envelope;
                TaskCompletionSource = new TaskCompletionSource<IBrokerMessageIdentifier?>();
            }

            public IRawOutboundEnvelope Envelope { get; }

            public TaskCompletionSource<IBrokerMessageIdentifier?> TaskCompletionSource { get; }
        }
    }
}
