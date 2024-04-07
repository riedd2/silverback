// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="SqliteOutboxSettings" />.
/// </summary>
public class SqliteOutboxSettingsBuilder : IOutboxSettingsImplementationBuilder
{
    private readonly string _connectionString;

    private string? _tableName;

    private TimeSpan? _dbCommandTimeout;

    private TimeSpan? _createTableTimeout;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxSettingsBuilder" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the SQLite database.
    /// </param>
    public SqliteOutboxSettingsBuilder(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    ///     Sets the table name.
    /// </summary>
    /// <param name="tableName">
    ///     The name of the outbox table. If not specified, the default <c>"SilverbackOutbox"</c> will be used.
    /// </param>
    /// <returns>
    ///     The <see cref="SqliteOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    // TODO: Review method name (With...)
    public SqliteOutboxSettingsBuilder WithTableName(string tableName)
    {
        _tableName = Check.NotNullOrEmpty(tableName, nameof(tableName));
        return this;
    }

    /// <summary>
    ///     Sets the database command timeout.
    /// </summary>
    /// <param name="dbCommandTimeout">
    ///     The timeout for the database commands. The default is 10 seconds.
    /// </param>
    /// <returns>
    ///     The <see cref="SqliteOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SqliteOutboxSettingsBuilder WithDbCommandTimeout(TimeSpan dbCommandTimeout)
    {
        if (dbCommandTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(dbCommandTimeout), dbCommandTimeout, "The timeout must be greater than zero.");

        _dbCommandTimeout = dbCommandTimeout;
        return this;
    }

    /// <summary>
    ///     Sets the timeout for the table creation.
    /// </summary>
    /// <param name="createTableTimeout">
    ///     The timeout for the table creation. The default is 30 seconds.
    /// </param>
    /// <returns>
    ///     The <see cref="SqliteOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SqliteOutboxSettingsBuilder WithCreateTableTimeout(TimeSpan createTableTimeout)
    {
        if (createTableTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(createTableTimeout), createTableTimeout, "The timeout must be greater than zero.");

        _createTableTimeout = createTableTimeout;
        return this;
    }

    /// <inheritdoc cref="IOutboxSettingsImplementationBuilder.Build" />
    public OutboxSettings Build()
    {
        SqliteOutboxSettings settings = new(_connectionString);

        settings = settings with
        {
            TableName = _tableName ?? settings.TableName,
            DbCommandTimeout = _dbCommandTimeout ?? settings.DbCommandTimeout,
            CreateTableTimeout = _createTableTimeout ?? settings.CreateTableTimeout
        };

        settings.Validate();

        return settings;
    }
}
