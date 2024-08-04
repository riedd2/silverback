﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public class ParentBuilderGenerator : BuilderGenerator
{
    public ParentBuilderGenerator(Type proxiedType)
        : base(proxiedType)
    {
    }

    public override string Generate()
    {
        GenerateInterfaceHeading();
        MapInterfaceProperties();
        GenerateBasicFooter();

        StringBuilder.AppendLine();

        GenerateClientsBuilderClassHeading();
        MapClientsBuilderClassProperties();
        GenerateBasicFooter();

        StringBuilder.AppendLine();

        return base.Generate();
    }

    protected override string GetClassSummary() => $"The autogenerated part of the <see cref=\"{GeneratedClassName}{{TConfig, TConfluentConfig, TBuilder}}\" /> class.";

    protected override string GetClassSignature() => $"public partial class {GeneratedClassName}<TConfig, TConfluentConfig, TBuilder> : IKafkaClientConfigurationBuilder";

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "False positive, it makes no sense")]
    protected override void MapClassProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(ProxiedType, true)
                .Where(property => !IgnoredProperties.Contains(property));

        StringBuilder fieldsStringBuilder = new();
        StringBuilder propertiesStringBuilder = new();
        StringBuilder interfaceStringBuilder = new();
        StringBuilder buildMethodStringBuilder = new();
        StringBuilder buildMethodStringBuilder2 = new();

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType);
            string argument = property.Name.ToCamelCase();
            string field = $"_{argument}";
            string visibility = MustBeInternal(property.Name) ? "internal" : "public partial";

            if (property.Name == nameof(Config.CancellationDelayMaxMs))
                fieldsStringBuilder.AppendLine($"    private {propertyType}? {field};");
            else
                fieldsStringBuilder.AppendLine($"    private {propertyType} {field};");

            fieldsStringBuilder.AppendLine();

            propertiesStringBuilder.AppendLine($"    {visibility} TBuilder With{property.Name}({propertyType} {argument})");
            propertiesStringBuilder.AppendLine("    {");
            propertiesStringBuilder.AppendLine($"        {field} = {argument};");
            propertiesStringBuilder.AppendLine("        return This;");
            propertiesStringBuilder.AppendLine("    }");
            propertiesStringBuilder.AppendLine();

            interfaceStringBuilder.Append($"    void IKafkaClientConfigurationBuilder.With{property.Name}({propertyType} {argument})");
            interfaceStringBuilder.AppendLine($" => With{property.Name}({argument});");
            interfaceStringBuilder.AppendLine();

            if (property.Name == nameof(Config.CancellationDelayMaxMs))
            {
                buildMethodStringBuilder2.AppendLine($"        if ({field}.HasValue)");
                buildMethodStringBuilder2.AppendLine("        {");
                buildMethodStringBuilder2.AppendLine("            config = config with");
                buildMethodStringBuilder2.AppendLine("            {");
                buildMethodStringBuilder2.AppendLine($"                {property.Name} = {field}");
                buildMethodStringBuilder2.AppendLine("            };");
                buildMethodStringBuilder2.AppendLine("        }");
            }
            else
            {
                buildMethodStringBuilder.AppendLine($"            {property.Name} = {field},");
            }
        }

        StringBuilder.Append(fieldsStringBuilder);
        StringBuilder.Append(propertiesStringBuilder);
        StringBuilder.Append(interfaceStringBuilder);
        StringBuilder.AppendLine("    /// <summary>");
        StringBuilder.AppendLine("    ///     Builds the configuration.");
        StringBuilder.AppendLine("    /// </summary>");
        StringBuilder.AppendLine("    /// <returns>");
        StringBuilder.AppendLine("    ///     The configuration.");
        StringBuilder.AppendLine("    /// </returns>");
        StringBuilder.AppendLine("    protected virtual TConfig BuildCore()");
        StringBuilder.AppendLine("    {");
        StringBuilder.AppendLine("        TConfig config = new()");
        StringBuilder.AppendLine("        {");
        StringBuilder.Append(buildMethodStringBuilder);
        StringBuilder.AppendLine("        };");
        StringBuilder.AppendLine();
        StringBuilder.Append(buildMethodStringBuilder2);
        StringBuilder.AppendLine();
        StringBuilder.AppendLine("        return config;");
        StringBuilder.AppendLine("    }");
    }

    private static bool MustBeInternal(string propertyName) =>
        propertyName is "AllowAutoCreateTopics"
            or "TopicMetadataRefreshSparse"
            or "EnableSslCertificateVerification"
            or "EnableSaslOauthbearerUnsecureJwt"
            or "ApiVersionRequest"
            or "SocketKeepaliveEnable"
            or "SocketNagleDisable"
            or "EnableMetricsPush";

    private void GenerateInterfaceHeading()
    {
        StringBuilder.AppendLine("/// <summary>");
        StringBuilder.AppendLine("///     Builds the <see cref=\"KafkaProducerConfiguration\" /> or <see cref=\"KafkaConsumerConfiguration\" />.");
        StringBuilder.AppendLine("/// </summary>");
        StringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.MaintainabilityRules\", \"SA1402:File may only contain a single type\", Justification = \"Autogenerated all at once\")]");
        StringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.DocumentationRules\", \"SA1600:Elements should be documented\", Justification = \"Internal interface\")]");
        StringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.OrderingRules\", \"SA1201:Elements should appear in the correct order\", Justification = \"Autogenerated\")]");
        StringBuilder.AppendLine($"internal interface I{GeneratedClassName}");
        StringBuilder.AppendLine("{");
    }

    private void GenerateClientsBuilderClassHeading()
    {
        StringBuilder.AppendLine("/// <content>");
        StringBuilder.AppendLine("///     The autogenerated part of the <see cref=\"KafkaClientsConfigurationBuilder\" /> class.");
        StringBuilder.AppendLine("/// </content>");
        StringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.MaintainabilityRules\", \"SA1402:File may only contain a single type\", Justification = \"Autogenerated all at once\")]");
        StringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.DocumentationRules\", \"SA1600:Elements should be documented\", Justification = \"Documented in other partial\")]");
        StringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.DocumentationRules\", \"SA1601:Partial elements should be documented\", Justification = \"Autogenerated\")]");
        StringBuilder.AppendLine("public partial class KafkaClientsConfigurationBuilder");
        StringBuilder.AppendLine("{");
    }

    private void MapInterfaceProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(ProxiedType, true)
                .Where(property => !IgnoredProperties.Contains(property));

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType);
            string valueVariableName = property.Name.ToCamelCase();

            StringBuilder.AppendLine($"    void With{property.Name}({propertyType} {valueVariableName});");
            StringBuilder.AppendLine();
        }
    }

    private void MapClientsBuilderClassProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(ProxiedType, true)
                .Where(
                    property => !IgnoredProperties.Contains(property) &&
                                property.Name != "ClientId");

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType);
            string valueVariableName = property.Name.ToCamelCase();
            string visibility = MustBeInternal(property.Name) ? "internal" : "public partial";

            StringBuilder.AppendLine($"    {visibility} KafkaClientsConfigurationBuilder With{property.Name}({propertyType} {valueVariableName})");
            StringBuilder.AppendLine("    {");
            StringBuilder.AppendLine($"        _sharedConfigurationActions.Add(builder => builder.With{property.Name}({valueVariableName}));");
            StringBuilder.AppendLine("        return this;");
            StringBuilder.AppendLine("    }");
            StringBuilder.AppendLine();
        }
    }
}