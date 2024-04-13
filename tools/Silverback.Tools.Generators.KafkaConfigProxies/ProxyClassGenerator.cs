﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

internal sealed class ProxyClassGenerator
{
    private readonly Type _proxiedType;

    private readonly string _generatedClassName;

    private readonly bool _isChildType;

    private readonly StringBuilder _stringBuilder = new();

    public ProxyClassGenerator(Type proxiedType)
    {
        _proxiedType = proxiedType;
        _isChildType = _proxiedType != typeof(ClientConfig);
        _generatedClassName = _isChildType ? $"Kafka{proxiedType.Name}uration" : "KafkaClientConfiguration";
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
        _stringBuilder.AppendLine(
            _isChildType
                ? $"///     The autogenerated part of the <see cref=\"{_generatedClassName}\" /> class."
                : $"///     The autogenerated part of the <see cref=\"{_generatedClassName}{{TClientConfig}}\" /> class.");
        _stringBuilder.AppendLine("/// </content>");
        _stringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.MaintainabilityRules\", \"SA1402:File may only contain a single type\", Justification = \"Autogenerated all at once\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"Design\", \"CA1044:Properties should not be write only\", Justification = \"Accessors generated according to wrapped class\")]");
        _stringBuilder.AppendLine(
            _isChildType
                ? $"public partial record {_generatedClassName}"
                : $"public partial record {_generatedClassName}<TClientConfig>");
        _stringBuilder.AppendLine("{");
        _stringBuilder.AppendLine();
    }

    private void MapProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(_proxiedType, !_isChildType)
                .Where(
                    property => !IgnoredProperties.Contains(property) &&
                                property.Name != "EnableAutoCommit" &&
                                property.Name != "GroupId");

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType, true);

            _stringBuilder.AppendSummary(property);

            if (property.Name.EndsWith("Url") && property.PropertyType == typeof(string))
                _stringBuilder.AppendLine("    [SuppressMessage(\"Design\", \"CA1056:URI-like properties should not be strings\", Justification = \"Generated according to wrapped class.\")]");

            _stringBuilder.AppendLine($"    public {propertyType} {property.Name}");
            _stringBuilder.AppendLine("    {");

            if (property.GetGetMethod() != null)
                _stringBuilder.AppendLine($"        get => ClientConfig.{property.Name};");

            if (property.Name == "DeliveryReportFields")
            {
                _stringBuilder.AppendLine("        init");
                _stringBuilder.AppendLine("        {");
                _stringBuilder.AppendLine("            if (value != null)");
                _stringBuilder.AppendLine($"                ClientConfig.{property.Name} = value;");
                _stringBuilder.AppendLine("        }");
            }
            else if (property.GetSetMethod() != null)
            {
                _stringBuilder.AppendLine($"        init => ClientConfig.{property.Name} = value;");
            }

            _stringBuilder.AppendLine("    }");
            _stringBuilder.AppendLine();
        }
    }

    private void GenerateFooter() => _stringBuilder.AppendLine("}");
}