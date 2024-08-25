// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Writes to the in-memory outbox.
/// </summary>
public class InMemoryOutboxWriter : IOutboxWriter
{
    private readonly InMemoryOutbox _outbox;

    private readonly ISilverbackLogger<InMemoryOutboxWriter> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOutboxWriter" /> class.
    /// </summary>
    /// <param name="outbox">
    ///     The in-memory outbox shared between the <see cref="InMemoryOutboxWriter" /> and <see cref="InMemoryOutboxReader" />.
    /// </param>
    /// <param name="logger">
    ///     The logger.
    /// </param>
    public InMemoryOutboxWriter(InMemoryOutbox outbox, ISilverbackLogger<InMemoryOutboxWriter> logger)
    {
        _outbox = Check.NotNull(outbox, nameof(outbox));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="AddAsync(OutboxMessage, ISilverbackContext)" />
    public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null)
    {
        WarnIfTransaction(context);

        _outbox.Add(outboxMessage);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.ISilverbackContext?)" />
    public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        WarnIfTransaction(context);

        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            _outbox.Add(outboxMessage);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IAsyncEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.ISilverbackContext?)" />
    public async Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        WarnIfTransaction(context);

        await foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            _outbox.Add(outboxMessage);
        }
    }

    private void WarnIfTransaction(ISilverbackContext? context)
    {
        if (context != null && context.TryGetStorageTransaction(out _))
        {
            _logger.LogOutboxUnsupportedTransaction();
        }
    }
}
