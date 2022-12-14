// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using Silverback.Util;

namespace Silverback.Storage;

/// <summary>
///     Adds the storage specific methods to the <see cref="SilverbackContext" />.
/// </summary>
// TODO: Test?
public static class SilverbackContextStorageExtensions
{
    private const int StorageTransactionObjectTypeId = 1;

    /// <summary>
    ///     Specifies an existing <see cref="DbTransaction" /> to be used for database operations.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction.
    /// </param>
    public static void EnlistTransaction(this SilverbackContext context, object transaction) =>
        Check.NotNull(context, nameof(context)).SetObject(StorageTransactionObjectTypeId, transaction);

    /// <summary>
    ///     Checks whether a storage transaction is set and returns it.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public static bool TryGetStorageTransaction(this SilverbackContext context, out object? transaction) =>
        Check.NotNull(context, nameof(context)).TryGetObject(StorageTransactionObjectTypeId, out transaction);
}