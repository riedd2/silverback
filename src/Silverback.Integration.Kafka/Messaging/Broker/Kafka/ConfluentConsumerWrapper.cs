﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal class ConfluentConsumerWrapper : BrokerClient, IConfluentConsumerWrapper
{
    private readonly ConsumerConfig _confluentConfiguration;

    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly IServiceProvider _serviceProvider;

    private readonly ISilverbackLogger _logger;

    private readonly IConfluentConsumerBuilder _consumerBuilder;

    private readonly IConfluentAdminClientFactory _adminClientFactory;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private IConsumer<byte[]?, byte[]?>? _confluentConsumer;

    private IAdminClient? _adminClient;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private KafkaConsumer? _consumer;

    public ConfluentConsumerWrapper(
        string name,
        IConfluentConsumerBuilder consumerBuilder,
        KafkaConsumerConfiguration configuration,
        IConfluentAdminClientFactory adminClientFactory,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        IKafkaOffsetStoreFactory offsetStoreFactory,
        IServiceProvider serviceProvider,
        ISilverbackLogger logger)
        : base(name, logger)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _confluentConfiguration = configuration.ToConfluentConfig();
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));

        _consumerBuilder = Check.NotNull(consumerBuilder, nameof(consumerBuilder))
            .SetConfig(_confluentConfiguration)
            .SetEventsHandlers(this, Configuration, _brokerClientCallbacksInvoker, _logger);
        _adminClientFactory = Check.NotNull(adminClientFactory, nameof(adminClientFactory));
    }

    public KafkaConsumerConfiguration Configuration { get; }

    public IReadOnlyList<TopicPartition> Assignment => (IReadOnlyList<TopicPartition>?)_confluentConsumer?.Assignment ?? [];

    public KafkaConsumer Consumer
    {
        get => _consumer ?? throw new InvalidOperationException("The consumer is not initialized yet.");
        set => _consumer = Check.NotNull(value, nameof(value));
    }

    public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken)
    {
        if (Status is not (ClientStatus.Initialized or ClientStatus.Initializing))
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        return _confluentConsumer.Consume(cancellationToken);
    }

    public void StoreOffset(TopicPartitionOffset topicPartitionOffset)
    {
        if (Status is not (ClientStatus.Initialized or ClientStatus.Disconnecting))
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        if (Configuration.CommitOffsets)
            _confluentConsumer.StoreOffset(topicPartitionOffset);
    }

    public void Commit()
    {
        if (Status is not ClientStatus.Initialized and not ClientStatus.Disconnecting)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        if (!Configuration.CommitOffsets)
            return;

        try
        {
            List<TopicPartitionOffset>? offsets = _confluentConsumer.Commit();

            CommittedOffsets committedOffsets = new(
                offsets.Select(offset => new TopicPartitionOffsetError(offset, new Error(ErrorCode.NoError))).ToList(),
                new Error(ErrorCode.NoError));

            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                callback =>
                    callback.OnOffsetsCommitted(committedOffsets, Consumer));
        }
        catch (TopicPartitionOffsetException ex)
        {
            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                callback =>
                    callback.OnOffsetsCommitted(new CommittedOffsets(ex.Results, ex.Error), Consumer));

            throw;
        }
        catch (KafkaException ex)
        {
            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                callback =>
                    callback.OnOffsetsCommitted(new CommittedOffsets(null, ex.Error), Consumer));
        }
    }

    public void Seek(TopicPartitionOffset topicPartitionOffset)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        _confluentConsumer.Seek(topicPartitionOffset);
    }

    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        _confluentConsumer.Pause(partitions);
    }

    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        _confluentConsumer.Resume(partitions);
    }

    protected override async ValueTask ConnectCoreAsync()
    {
        _confluentConsumer = _consumerBuilder.Build();

        if (Configuration.IsStaticAssignment)
            await PerformStaticAssignmentAsync().ConfigureAwait(false);
        else
            Subscribe();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    protected override ValueTask DisconnectCoreAsync()
    {
        if (!Configuration.EnableAutoCommit)
            Commit();

        _confluentConsumer?.Close();
        _confluentConsumer?.Dispose();
        _confluentConsumer = null;

        return default;
    }

    private async ValueTask PerformStaticAssignmentAsync()
    {
        List<TopicPartitionOffset> assignment = [];

        try
        {
            foreach (KafkaConsumerEndpointConfiguration endpointConfiguration in Configuration.Endpoints)
            {
                await foreach (TopicPartitionOffset topicPartitionOffset in GetAssignmentAsync(endpointConfiguration))
                {
                    assignment.Add(topicPartitionOffset);
                }
            }
        }
        finally
        {
            _adminClient?.Dispose();
            _adminClient = null;
        }

        _confluentConsumer!.Assign(assignment);

        assignment.ForEach(topicPartitionOffset => _logger.LogPartitionStaticallyAssigned(topicPartitionOffset, Consumer));
    }

    private async IAsyncEnumerable<TopicPartitionOffset> GetAssignmentAsync(KafkaConsumerEndpointConfiguration endpointConfiguration)
    {
        StoredOffsetsLoader storedOffsetsLoader = new(_offsetStoreFactory, Configuration, _serviceProvider);

        foreach (TopicPartitionOffset topicPartitionOffset in endpointConfiguration.TopicPartitions)
        {
            if (topicPartitionOffset.Partition == Partition.Any && endpointConfiguration.PartitionOffsetsProvider != null)
            {
                IEnumerable<TopicPartitionOffset> providedTopicPartitionOffsets =
                    await GetAssignmentViaProviderAsync(endpointConfiguration.PartitionOffsetsProvider, topicPartitionOffset).ConfigureAwait(false);

                foreach (TopicPartitionOffset providedTopicPartitionOffset in providedTopicPartitionOffsets)
                {
                    yield return storedOffsetsLoader.ApplyStoredOffset(providedTopicPartitionOffset);
                }
            }
            else
            {
                yield return storedOffsetsLoader.ApplyStoredOffset(topicPartitionOffset);
            }
        }
    }

    private async ValueTask<IEnumerable<TopicPartitionOffset>> GetAssignmentViaProviderAsync(
        Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>> partitionOffsetsProvider,
        TopicPartitionOffset topicPartitionOffset)
    {
        _adminClient ??= _adminClientFactory.GetClient(_confluentConfiguration);

        List<TopicPartition> availablePartitions =
            _adminClient
                .GetMetadata(topicPartitionOffset.Topic, Configuration.GetMetadataTimeout)
                .Topics[0].Partitions
                .Select(metadata => new TopicPartition(topicPartitionOffset.Topic, metadata.PartitionId))
                .ToList();

        return await partitionOffsetsProvider.Invoke(availablePartitions).ConfigureAwait(false);
    }

    private void Subscribe() =>
        _confluentConsumer!.Subscribe(
            Configuration.Endpoints
                .SelectMany(endpoint => endpoint.TopicPartitions)
                .Select(topicPartitionOffset => topicPartitionOffset.Topic)
                .Distinct());
}
