// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Lock;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="SqliteOutboxWriter" /> and <see cref="SqliteOutboxReader" /> settings.
/// </summary>
public record SqliteOutboxSettings : OutboxSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxSettings" /> class.
    /// </summary>
    public SqliteOutboxSettings()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteOutboxSettings" /> class.
    /// </summary>
    /// <param name="connectionString">
    ///     The connection string to the Sqlite database.
    /// </param>
    /// <param name="tableName">
    ///     The name of the outbox table. If not specified, the default <c>"SilverbackOutbox"</c> will be used.
    /// </param>
    public SqliteOutboxSettings(string connectionString, string? tableName = null)
    {
        ConnectionString = connectionString;

        if (tableName != null)
            TableName = tableName;
    }

    /// <summary>
    ///     Gets the connection string to the Sqlite database.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the outbox table. The default is <c>"SilverbackOutbox"</c>.
    /// </summary>
    public string TableName { get; init; } = "Silverback_Outbox";

    /// <summary>
    ///     Returns an instance of <see cref="InMemoryLockSettings" />, since there is no distributed lock implementation for Sqlite and
    ///     the in-memory lock is enough for testing.
    /// </summary>
    /// <returns>
    ///     The <see cref="InMemoryLockSettings" />.
    /// </returns>
    public override DistributedLockSettings GetCompatibleLockSettings() =>
        new InMemoryLockSettings($"outbox.{ConnectionString}.{TableName}");

    /// <inheritdoc cref="OutboxSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new SilverbackConfigurationException("The connection string is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new SilverbackConfigurationException("The outbox table name is required.");
    }
}
