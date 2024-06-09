﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MQTTnet.Client;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.MqttConfigProxies;

internal sealed class ProxyClassGenerator
{
    private readonly Type _proxiedType;

    private readonly object? _proxiedTypeInstance;

    private readonly State _state;

    private readonly string _generatedTypeName;

    private readonly StringBuilder _stringBuilder = new();

    public ProxyClassGenerator(Type proxiedType, State state)
    {
        _proxiedType = proxiedType;
        _state = state;
        _generatedTypeName = TypesMapper.GetProxyClassName(proxiedType);

        try
        {
            _proxiedTypeInstance = Activator.CreateInstance(proxiedType);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    public string Generate()
    {
        GenerateHeading();
        MapProperties();
        GenerateFooter();

        return _stringBuilder.ToString();
    }

    private void GenerateHeading()
    {
        _stringBuilder.AppendLine("/// <content>");
        _stringBuilder.AppendLine("///     The autogenerated part of the <see cref=\"MqttClientConfiguration\" /> class.");
        _stringBuilder.AppendLine("/// </content>");
        _stringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.MaintainabilityRules\", \"SA1402:File may only contain a single type\", Justification = \"Autogenerated all at once\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"Performance\", \"CA1819:Properties should not return arrays\", Justification = \"Generated according to wrapped class\")]");
        _stringBuilder.AppendLine($"public partial record {_generatedTypeName}");
        _stringBuilder.AppendLine("{");
    }

    private void MapProperties()
    {
        bool generateDefaultInstanceField = false;
        StringBuilder propertiesStringBuilder = new();
        StringBuilder buildMethodStringBuilder = new();

        foreach (PropertyInfo property in GetProxiedTypeProperties())
        {
            if (property.PropertyType.Namespace!.StartsWith("MQTTnet") && property.PropertyType.IsClass)
                _state.AddType(property.PropertyType);

            (string propertyType, bool isMapped) = TypesMapper.GetMappedPropertyTypeString(property.PropertyType);

            propertiesStringBuilder.AppendSummary(property);

            if (property.Name == "Uri")
                propertiesStringBuilder.AppendLine("    [SuppressMessage(\"Design\", \"CA1056:URI-like properties should not be strings\", Justification = \"Generated according to wrapped class.\")]");

            propertiesStringBuilder.Append($"    public {propertyType} {property.Name} {{ get; init; }}");

            if (_proxiedTypeInstance != null && !isMapped)
            {
                object? defaultValue = property.GetValue(_proxiedTypeInstance);

                if (defaultValue != ReflectionHelper.GetDefaultValue(property.PropertyType))
                {
                    generateDefaultInstanceField = true;
                    propertiesStringBuilder.Append($" = DefaultInstance.{property.Name};");
                }
            }

            propertiesStringBuilder.AppendLine();
            propertiesStringBuilder.AppendLine();

            buildMethodStringBuilder.AppendLine(
                isMapped
                    ? $"            {property.Name} = {property.Name}?.ToMqttNetType(),"
                    : $"            {property.Name} = {property.Name},");
        }

        if (generateDefaultInstanceField)
        {
            _stringBuilder.AppendLine($"    private static readonly {_proxiedType.FullName} DefaultInstance = new();");
            _stringBuilder.AppendLine();
        }

        _stringBuilder.Append(propertiesStringBuilder);
        _stringBuilder.AppendLine($"    private {_proxiedType.FullName} MapCore() =>");
        _stringBuilder.AppendLine("        new()");
        _stringBuilder.AppendLine("        {");
        _stringBuilder.Append(buildMethodStringBuilder);
        _stringBuilder.AppendLine("        };");
    }

    private IEnumerable<PropertyInfo> GetProxiedTypeProperties() =>
        ReflectionHelper.GetProperties(_proxiedType, true)
            .Where(
                property => property.DeclaringType != typeof(MqttClientOptions) ||
                            property.Name
                                is not nameof(MqttClientOptions.UserProperties)
                                and not nameof(MqttClientOptions.ProtocolVersion)
                                and not nameof(MqttClientOptions.ClientId)
                                and not nameof(MqttClientOptions.ChannelOptions)
                                and not nameof(MqttClientOptions.WillContentType)
                                and not nameof(MqttClientOptions.WillCorrelationData)
                                and not nameof(MqttClientOptions.WillMessageExpiryInterval)
                                and not nameof(MqttClientOptions.WillPayload)
                                and not nameof(MqttClientOptions.WillPayloadFormatIndicator)
                                and not nameof(MqttClientOptions.WillQualityOfServiceLevel)
                                and not nameof(MqttClientOptions.WillResponseTopic)
                                and not nameof(MqttClientOptions.WillRetain)
                                and not nameof(MqttClientOptions.WillTopic)
                                and not nameof(MqttClientOptions.WillUserProperties)
                                and not nameof(MqttClientOptions.WillDelayInterval))
            .Where(
                property => property.DeclaringType != typeof(MqttClientTlsOptions) ||
                            property.Name
                                is not nameof(MqttClientTlsOptions.ApplicationProtocols)
                                and not nameof(MqttClientTlsOptions.CipherSuitesPolicy)
                                and not nameof(MqttClientTlsOptions.EncryptionPolicy)
                                and not nameof(MqttClientTlsOptions.AllowRenegotiation)
                                and not nameof(MqttClientTlsOptions.TrustChain))
            .Where(
                property => property.DeclaringType != typeof(MqttClientTcpOptions) ||
                            property.Name is not nameof(MqttClientTcpOptions.TlsOptions)
#pragma warning disable CS0618 // Type or member is obsolete
                                and not nameof(MqttClientTcpOptions.Server)
                                and not nameof(MqttClientTcpOptions.Port))
#pragma warning restore CS0618 // Type or member is obsolete
            .Where(
                property => property.DeclaringType != typeof(MqttClientWebSocketOptions) ||
                            property.Name
                                is not nameof(MqttClientWebSocketOptions.TlsOptions)
                                and not nameof(MqttClientWebSocketOptions.ProxyOptions));

    private void GenerateFooter() => _stringBuilder.AppendLine("}");
}
