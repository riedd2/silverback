﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Silverback.Storage;

internal static class DbContextExtensions
{
    public static void UseTransactionIfAvailable(this DbContext dbContext, SilverbackContext? context)
    {
        DbTransaction? transaction = context?.GetActiveDbTransaction<DbTransaction>();

        if (transaction != null)
            dbContext.Database.UseTransaction(transaction);
    }
}
