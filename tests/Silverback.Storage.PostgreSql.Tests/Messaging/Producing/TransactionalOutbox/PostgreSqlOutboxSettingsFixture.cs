// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Producing.TransactionalOutbox;

public class PostgreSqlOutboxSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetConnectionStringWithDefaultTableName()
    {
        PostgreSqlOutboxSettings settings = new("connection-string");

        settings.ConnectionString.Should().Be("connection-string");
        settings.TableName.Should().Be("SilverbackOutbox");
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnPostgreSqlAdvisoryLockSettings()
    {
        PostgreSqlOutboxSettings outboxSettings = new("connection-string");

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.Should().BeOfType<PostgreSqlAdvisoryLockSettings>();
        lockSettings.As<PostgreSqlAdvisoryLockSettings>().LockName.Should().Be($"outbox.{"connection-string".GetSha256Hash()}.SilverbackOutbox");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        PostgreSqlOutboxSettings outboxSettings = new("connection-string");

        Action act = outboxSettings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsEmptyOrWhitespace(string? tableName)
    {
        PostgreSqlOutboxSettings outboxSettings = new("connection-string")
        {
            TableName = tableName!
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The outbox table name is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        PostgreSqlOutboxSettings outboxSettings = new(connectionString!);

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The connection string is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsZero()
    {
        PostgreSqlOutboxSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsLessThanZero()
    {
        PostgreSqlOutboxSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsZero()
    {
        PostgreSqlOutboxSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsLessThanZero()
    {
        PostgreSqlOutboxSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }
}
