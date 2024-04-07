﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Exposes the methods to write to the outbox.
/// </summary>
/// <remarks>
///     Used by the <see cref="OutboxProduceStrategy" />.
/// </remarks>
public interface IOutboxWriter
{
    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="outboxMessage">
    ///     The message to be stored in the outbox.
    /// </param>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" /> in the current scope.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null);

    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="outboxMessages">
    ///     The messages to be stored in the outbox.
    /// </param>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" /> in the current scope.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null);

    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="outboxMessages">
    ///     The messages to be stored in the outbox.
    /// </param>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" /> in the current scope.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null);
}
