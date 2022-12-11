﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Silverback.Messaging.Configuration.Mqtt;

// TODO: Test
internal class MqttConsumerEndpointsCache
{
    private readonly ConcurrentDictionary<string, MqttConsumerEndpoint> _endpoints = new();

    private readonly List<WildcardSubscription> _wildcardSubscriptions = new();

    public MqttConsumerEndpointsCache(MqttClientConfiguration configuration)
    {
        foreach (MqttConsumerEndpointConfiguration endpointConfiguration in configuration.ConsumerEndpoints)
        {
            foreach (string topic in endpointConfiguration.Topics)
            {
                ParsedTopic parsedTopic = new(topic);

                if (parsedTopic.Regex != null)
                {
                    _wildcardSubscriptions.Add(new WildcardSubscription(parsedTopic.Regex, endpointConfiguration));
                }
                else
                {
                    _endpoints.TryAdd(parsedTopic.Topic, new MqttConsumerEndpoint(parsedTopic.Topic, endpointConfiguration));
                }
            }
        }
    }

    public MqttConsumerEndpoint GetEndpoint(string topic)
    {
        if (_endpoints.TryGetValue(topic, out MqttConsumerEndpoint? endpoint))
            return endpoint;

        foreach (WildcardSubscription subscription in _wildcardSubscriptions)
        {
            if (subscription.Regex.IsMatch(topic))
            {
                endpoint = new MqttConsumerEndpoint(topic, subscription.EndpointConfiguration);
                _endpoints.TryAdd(topic, endpoint);
                return endpoint;
            }
        }

        throw new InvalidOperationException($"No configuration found for the specified topic '{topic}'.");
    }

    private record WildcardSubscription(Regex Regex, MqttConsumerEndpointConfiguration EndpointConfiguration);
}
