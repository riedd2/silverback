﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

internal sealed class BuilderGenerator
{
    private readonly Type _proxiedType;

    private readonly string _generatedClassName;

    private readonly bool _isChildType;

    private readonly StringBuilder _stringBuilder = new();

    public BuilderGenerator(Type proxiedType)
    {
        _proxiedType = proxiedType;
        _isChildType = _proxiedType != typeof(ClientConfig);
        _generatedClassName = _isChildType ? $"KafkaClient{_proxiedType.Name}urationBuilder" : "KafkaClientConfigurationBuilder";
    }

    public string Generate()
    {
        GenerateHeading();
        MapProperties();
        GenerateFooter();

        return _stringBuilder.ToString();
    }

    private static bool MustBeInternal(string propertyName) =>
        propertyName == "EnableAutoCommit" ||
        propertyName == "EnablePartitionEof" ||
        propertyName == "AllowAutoCreateTopics" ||
        propertyName == "EnableDeliveryReports" ||
        propertyName == "EnableIdempotence";

    private void GenerateHeading()
    {
        _stringBuilder.AppendLine("/// <content>");
        _stringBuilder.AppendLine(
            _isChildType
                ? $"///     The autogenerated part of the <see cref=\"{_generatedClassName}\" /> class."
                : $"///     The autogenerated part of the <see cref=\"{_generatedClassName}{{TClientConfig,TBuilder}}\" /> class.");
        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("/// </content>");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1649\", Justification = \"Autogenerated all at once\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1402\", Justification = \"Autogenerated all at once\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"CA1200\", Justification = \"Summary copied from wrapped class\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1623\", Justification = \"Summary copied from wrapped class\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1629\", Justification = \"Summary copied from wrapped class\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.DocumentationRules\", \"SA1625:Element documentation should not be copied and pasted\", Justification = \"Summary copied from wrapped class\")]");

        _stringBuilder.AppendLine(
            _isChildType
                ? $"public partial class {_generatedClassName}"
                : $"public partial class {_generatedClassName}<TClientConfig, TBuilder>");

        _stringBuilder.AppendLine("{");
        _stringBuilder.AppendLine();
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "False positive, it makes no sense")]
    private void MapProperties()
    {
        foreach (PropertyInfo property in ReflectionHelper.GetProperties(_proxiedType, !_isChildType))
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType, true);
            string valueVariableName = property.Name.ToCamelCase();
            string visibility = MustBeInternal(property.Name) ? "internal" : "public";

            WriteMethodSummary(property, valueVariableName);

            _stringBuilder.AppendLine(
                _isChildType
                    ? $"    {visibility} {_generatedClassName} With{property.Name}({propertyType} {valueVariableName})"
                    : $"    {visibility} TBuilder With{property.Name}({propertyType} {valueVariableName})");
            _stringBuilder.AppendLine("    {");
            _stringBuilder.AppendLine($"        {_proxiedType.Name}.{property.Name} = {valueVariableName};");
            _stringBuilder.AppendLine("        return This;");
            _stringBuilder.AppendLine("    }");
            _stringBuilder.AppendLine();
        }
    }

    private void WriteMethodSummary(PropertyInfo property, string valueVariableName)
    {
        SummaryText summaryText = DocumentationHelper.GetSummary(property);

        _stringBuilder.AppendLine("    /// <summary>");
        _stringBuilder.Append(summaryText.Main);
        _stringBuilder.AppendLine("    /// </summary>");
        _stringBuilder.AppendLine($"    /// <param name=\"{valueVariableName}\">");
        _stringBuilder.Append(summaryText.Main);
        _stringBuilder.AppendLine("    /// </param>");

        _stringBuilder.AppendLine("    /// <returns>");
        _stringBuilder.AppendLine("    /// The client configuration builder so that additional calls can be chained.");
        _stringBuilder.AppendLine("    /// </returns>");
    }

    private void GenerateFooter()
    {
        _stringBuilder.AppendLine("}");

        if (_isChildType)
            return;

        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("/// <content>");
        _stringBuilder.AppendLine($"///     The autogenerated part of the <see cref=\"{_generatedClassName}\" /> class.");
        _stringBuilder.AppendLine("/// </content>");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1649\", Justification = \"Autogenerated all at once\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1402\", Justification = \"Autogenerated all at once\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"CA1200\", Justification = \"Summary copied from wrapped class\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1623\", Justification = \"Summary copied from wrapped class\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"\", \"SA1629\", Justification = \"Summary copied from wrapped class\")]");
        _stringBuilder.AppendLine("[SuppressMessage(\"StyleCop.CSharp.DocumentationRules\", \"SA1625:Element documentation should not be copied and pasted\", Justification = \"Summary copied from wrapped class\")]");
        _stringBuilder.AppendLine($"public partial class {_generatedClassName}");
        _stringBuilder.AppendLine("{");
        _stringBuilder.AppendLine("}");
    }
}
