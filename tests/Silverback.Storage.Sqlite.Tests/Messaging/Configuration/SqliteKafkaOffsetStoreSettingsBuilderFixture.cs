// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public class SqliteKafkaOffsetStoreSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildDefaultSettings()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.Build();

        settings.Should().BeOfType<SqliteKafkaOffsetStoreSettings>();
        settings.Should().BeEquivalentTo(new SqliteKafkaOffsetStoreSettings("connection-string"));
    }

    [Fact]
    public void WithTableName_ShouldSetOffsetStoreTableName()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithTableName("test-offsetStore").Build();

        settings.As<SqliteKafkaOffsetStoreSettings>().TableName.Should().Be("test-offsetStore");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(20)).Build();

        settings.As<SqliteKafkaOffsetStoreSettings>().DbCommandTimeout.Should().Be(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        SqliteKafkaOffsetStoreSettingsBuilder builder = new("connection-string");

        KafkaOffsetStoreSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(40)).Build();

        settings.As<SqliteKafkaOffsetStoreSettings>().CreateTableTimeout.Should().Be(TimeSpan.FromSeconds(40));
    }
}
