// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Diagnostics;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Mqtt;

internal class DefaultMqttNetLogger : IMqttNetLogger
{
    private readonly ISilverbackLogger<DefaultMqttNetLogger> _logger;

    public DefaultMqttNetLogger(ISilverbackLogger<DefaultMqttNetLogger> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled => true;

    public void Publish(
        MqttNetLogLevel logLevel,
        string? source,
        string message,
        object[]? parameters,
        Exception? exception)
    {
        switch (logLevel)
        {
            case MqttNetLogLevel.Verbose:
                _logger.LogMqttClientVerbose(source, message, parameters, exception);
                break;
            case MqttNetLogLevel.Info:
                _logger.LogMqttClientInformation(source, message, parameters, exception);
                break;
            case MqttNetLogLevel.Warning:
                _logger.LogMqttClientWarning(source, message, parameters, exception);
                break;
            case MqttNetLogLevel.Error:
                _logger.LogMqttClientError(source, message, parameters, exception);
                break;
            default:
                throw new InvalidOperationException("Unexpected MqttNetLogLevel");
        }
    }
}
