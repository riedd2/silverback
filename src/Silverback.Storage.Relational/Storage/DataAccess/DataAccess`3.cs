// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Storage.Relational;
using Silverback.Util;

namespace Silverback.Storage.DataAccess;

internal abstract class DataAccess<TConnection, TTransaction, TParameter>
    where TConnection : DbConnection
    where TTransaction : DbTransaction
    where TParameter : DbParameter
{
    private readonly string _connectionString;

    protected DataAccess(string connectionString)
    {
        _connectionString = Check.NotNullOrEmpty(connectionString, nameof(connectionString));
    }

    public TParameter CreateParameter(string name, object? value) =>
        CreateParameterCore(name, value ?? DBNull.Value);

    public IReadOnlyCollection<T> ExecuteQuery<T>(
        Func<DbDataReader, T> projection,
        string sql,
        params TParameter[] parameters)
    {
        using DbCommandWrapper wrapper = GetCommand(sql, parameters);
        using DbDataReader reader = wrapper.Command.ExecuteReader();

        return Map(reader, projection).ToList();
    }

    public async Task<IReadOnlyCollection<T>> ExecuteQueryAsync<T>(
        Func<DbDataReader, T> projection,
        string sql,
        params TParameter[] parameters)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters).ConfigureAwait(false);
        using DbDataReader reader = await wrapper.Command.ExecuteReaderAsync().ConfigureAwait(false);

        return await MapAsync(reader, projection).ToListAsync().ConfigureAwait(false);
    }

    public T? ExecuteScalar<T>(string sql, params TParameter[] parameters)
    {
        using DbCommandWrapper wrapper = GetCommand(sql, parameters);

        object? result = wrapper.Command.ExecuteScalar();

        return result == DBNull.Value ? default : (T?)result;
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, params TParameter[] parameters)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters).ConfigureAwait(false);

        object? result = await wrapper.Command.ExecuteScalarAsync().ConfigureAwait(false);

        if (result == DBNull.Value)
            return default;

        return (T?)result;
    }

    public Task ExecuteNonQueryAsync(string sql, params TParameter[] parameters) => ExecuteNonQueryAsync(null, sql, parameters);

    public async Task ExecuteNonQueryAsync(SilverbackContext? context, string sql, params TParameter[] parameters)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters, true, context).ConfigureAwait(false);

        try
        {
            await wrapper.Command.ExecuteNonQueryAsync().ConfigureAwait(false);
            await wrapper.CommitOwnedTransactionAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            await wrapper.RollbackOwnedTransactionAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task ExecuteNonQueryAsync<T>(
        IEnumerable<T> items,
        string sql,
        TParameter[] parameters,
        Action<T, TParameter[]> parameterValuesProvider,
        SilverbackContext? context = null)
    {
        using DbCommandWrapper wrapper = await GetCommandAsync(sql, parameters, true, context).ConfigureAwait(false);

        try
        {
            foreach (T item in items)
            {
                parameterValuesProvider.Invoke(item, parameters);
                await wrapper.Command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await wrapper.CommitOwnedTransactionAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            await wrapper.RollbackOwnedTransactionAsync().ConfigureAwait(false);
            throw;
        }
    }

    protected abstract TConnection CreateConnection(string connectionString);

    protected abstract TParameter CreateParameterCore(string name, object value);

    private static IEnumerable<T> Map<T>(DbDataReader reader, Func<DbDataReader, T> projection)
    {
        while (reader.Read())
        {
            yield return projection(reader);
        }
    }

    private static async IAsyncEnumerable<T> MapAsync<T>(DbDataReader reader, Func<DbDataReader, T> projection)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            yield return projection(reader);
        }
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
    private DbCommandWrapper GetCommand(
        string sql,
        TParameter[] parameters,
        bool beginTransaction = false,
        SilverbackContext? context = null)
    {
        bool isNewConnection = false;
        bool isNewTransaction = false;
        DbConnection connection;

        DbTransaction? transaction = context.GetActiveDbTransaction<TTransaction>();

        if (transaction != null)
        {
            connection = transaction.Connection ?? throw new InvalidOperationException("Transaction.Connection is null");
        }
        else
        {
            connection = CreateConnection(_connectionString);
            isNewConnection = true;

            try
            {
                connection.Open();

                if (beginTransaction)
                {
                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                    isNewTransaction = true;
                }
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
        }

        DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);

        return new DbCommandWrapper(command, connection, isNewConnection, transaction, isNewTransaction);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Reviewed")]
    private async Task<DbCommandWrapper> GetCommandAsync(
        string sql,
        TParameter[] parameters,
        bool beginTransaction = false,
        SilverbackContext? context = null)
    {
        bool isNewConnection = false;
        bool isNewTransaction = false;
        DbConnection connection;

        DbTransaction? transaction = context.GetActiveDbTransaction<TTransaction>();

        if (transaction != null)
        {
            connection = transaction.Connection ?? throw new InvalidOperationException("Transaction.Connection is null");
        }
        else
        {
            connection = CreateConnection(_connectionString);
            isNewConnection = true;

            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                if (beginTransaction)
                {
                    transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted).ConfigureAwait(false);
                    isNewTransaction = true;
                }
            }
            catch (Exception)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);

        return new DbCommandWrapper(command, connection, isNewConnection, transaction, isNewTransaction);
    }

    private sealed class DbCommandWrapper : IDisposable
    {
        private readonly DbConnection _connection;

        private readonly bool _isConnectionOwner;

        private readonly DbTransaction? _transaction;

        private readonly bool _isTransactionOwner;

        public DbCommandWrapper(
            DbCommand command,
            DbConnection connection,
            bool isConnectionOwner,
            DbTransaction? transaction,
            bool isTransactionOwner)
        {
            Command = command;
            _connection = connection;
            _isConnectionOwner = isConnectionOwner;
            _transaction = transaction;
            _isTransactionOwner = isTransactionOwner;
        }

        public DbCommand Command { get; }

        public void Dispose()
        {
            Command.Dispose();

            if (_isTransactionOwner && _transaction != null)
                _transaction.Dispose();

            if (_isConnectionOwner)
                _connection.Dispose();
        }

        public ValueTask CommitOwnedTransactionAsync() =>
            _isTransactionOwner && _transaction != null ? new ValueTask(_transaction.CommitAsync()) : default;

        public ValueTask RollbackOwnedTransactionAsync() =>
            _isTransactionOwner && _transaction != null ? new ValueTask(_transaction.RollbackAsync()) : default;
    }
}
