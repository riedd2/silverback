﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Protocol;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The MQTT producer configuration.
/// </summary>
public sealed record MqttProducerEndpointConfiguration : ProducerEndpointConfiguration<MqttProducerEndpoint>
{
    // /// <summary>
    // ///     Gets the MQTT client configuration. This is actually a wrapper around the
    // ///     <see cref="MqttClientOptions" /> from the MQTTnet library.
    // /// </summary>
    // public MqttClientConfiguration Client { get; init; } = new();

    /// <summary>
    ///     Gets the quality of service level (at most once, at least once or exactly once).
    ///     The default is <see cref="MqttQualityOfServiceLevel.AtMostOnce" />.
    /// </summary>
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message have to be sent with the retain flag, causing them to be persisted on the broker.
    ///     The default is <c>false</c>.
    /// </summary>
    public bool Retain { get; init; }

    /// <summary>
    ///     Gets the message expiry interval in seconds. This interval defines the period of time that the broker stores the <i>PUBLISH</i>
    ///     message for any matching subscribers that are not currently connected. When no message expiry interval is set, the broker must
    ///     store the message for matching subscribers indefinitely.
    ///     The default is <c>null</c>.
    /// </summary>
    public uint? MessageExpiryInterval { get; init; }

    /// <inheritdoc cref="EndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Chunk is { Size: < int.MaxValue })
            throw new BrokerConfigurationException("Chunking cannot be enabled for MQTT. This is due to the limitations of the MQTT protocol.");
    }
}