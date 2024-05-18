﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

internal class MqttClientsInitializer : BrokerClientsInitializer
{
    private readonly MqttClientsConfigurationActions _configurationActions;

    private readonly IMqttClientWrapperFactory _clientWrapperFactory;

    private readonly IProducerLogger<MqttProducer> _producerLogger;

    private readonly IBrokerBehaviorsProvider<IProducerBehavior> _producerBehaviorsProvider;

    private readonly IConsumerLogger<MqttConsumer> _consumerLogger;

    private readonly IBrokerBehaviorsProvider<IConsumerBehavior> _consumerBehaviorsProvider;

    public MqttClientsInitializer(
        MqttClientsConfigurationActions configurationActions,
        IMqttClientWrapperFactory clientWrapperFactory,
        IProducerLogger<MqttProducer> producerLogger,
        IBrokerBehaviorsProvider<IProducerBehavior> producerBehaviorsProvider,
        IConsumerLogger<MqttConsumer> consumerLogger,
        IBrokerBehaviorsProvider<IConsumerBehavior> consumerBehaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<MqttClientsInitializer> logger)
        : base(serviceProvider, logger)
    {
        _configurationActions = Check.NotNull(configurationActions, nameof(configurationActions));
        _clientWrapperFactory = Check.NotNull(clientWrapperFactory, nameof(clientWrapperFactory));
        _producerLogger = Check.NotNull(producerLogger, nameof(producerLogger));
        _producerBehaviorsProvider = Check.NotNull(producerBehaviorsProvider, nameof(producerBehaviorsProvider));
        _consumerLogger = Check.NotNull(consumerLogger, nameof(consumerLogger));
        _consumerBehaviorsProvider = Check.NotNull(consumerBehaviorsProvider, nameof(consumerBehaviorsProvider));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifecycle handled by the ProducerCollection")]
    internal IReadOnlyCollection<MqttProducer> InitializeProducers(string name, MqttClientConfiguration configuration, bool routing = true)
    {
        IMqttClientWrapper mqttClientWrapper = _clientWrapperFactory.Create(name, configuration);

        AddClient(mqttClientWrapper);

        return InitializeProducers(name, configuration, mqttClientWrapper, routing);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifecycle handled by the BrokerClientCollection")]
    protected override void InitializeCore()
    {
        foreach (MergedAction<MqttClientConfigurationBuilder> mergedAction in _configurationActions.ConfigurationActions)
        {
            MqttClientConfigurationBuilder configurationBuilder = new(ServiceProvider);
            mergedAction.Action.Invoke(configurationBuilder);
            MqttClientConfiguration configuration = configurationBuilder.Build();

            IMqttClientWrapper mqttClientWrapper = _clientWrapperFactory.Create(mergedAction.Key, configurationBuilder.Build());

            AddClient(mqttClientWrapper);

            InitializeProducers(mergedAction.Key, configuration, mqttClientWrapper);
            InitializeConsumer(mergedAction.Key, configuration, mqttClientWrapper);
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifecycle handled by the ConsumerCollection")]
    private void InitializeConsumer(string id, MqttClientConfiguration configuration, IMqttClientWrapper mqttClientWrapper)
    {
        if (configuration.ConsumerEndpoints.Count < 1)
            return;

        MqttClientConfiguration consumerConfiguration = configuration with
        {
            ProducerEndpoints = ValueReadOnlyCollection.Empty<MqttProducerEndpointConfiguration>()
        };

        MqttConsumer consumer = new(
            id,
            mqttClientWrapper,
            consumerConfiguration,
            _consumerBehaviorsProvider,
            ServiceProvider,
            _consumerLogger);

        AddConsumer(consumer);
    }

    private List<MqttProducer> InitializeProducers(
        string name,
        MqttClientConfiguration configuration,
        IMqttClientWrapper mqttClientWrapper,
        bool routing = true)
    {
        int i = 0;
        List<MqttProducer> producers = [];

        foreach (MqttProducerEndpointConfiguration? endpointConfiguration in configuration.ProducerEndpoints)
        {
            MqttClientConfiguration producerConfiguration = configuration with
            {
                ConsumerEndpoints = ValueReadOnlyCollection.Empty<MqttConsumerEndpointConfiguration>(),
                ProducerEndpoints = new[] { endpointConfiguration }.AsValueReadOnlyCollection()
            };

            MqttProducer producer = new(
                configuration.ProducerEndpoints.Count > 0 ? $"{name}-{++i}" : name,
                mqttClientWrapper,
                producerConfiguration,
                _producerBehaviorsProvider,
                ServiceProvider,
                _producerLogger);

            AddProducer(producer, routing);

            producers.Add(producer);
        }

        return producers;
    }
}
