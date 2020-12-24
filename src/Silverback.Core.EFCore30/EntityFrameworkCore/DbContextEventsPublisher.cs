﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Silverback.EntityFrameworkCore
{
    /// <summary>
    ///     Exposes some methods to handle domain events as part of the SaveChanges transaction.
    /// </summary>
    public class DbContextEventsPublisher
    {
        private readonly Action<object> _clearEventsAction;

        private readonly DbContext _dbContext;

        private readonly Func<object, IEnumerable<object>?> _eventsSelector;

        private readonly IPublisher _publisher;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbContextEventsPublisher" /> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be used to publish the events to the internal bus.
        /// </param>
        /// <param name="dbContext">
        ///     The <see cref="DbContext" /> to be scanned for domain events.
        /// </param>
        public DbContextEventsPublisher(IPublisher publisher, DbContext dbContext)
            : this(
                e => (e as IMessagesSource)?.GetMessages(),
                e => (e as IMessagesSource)?.ClearMessages(),
                publisher,
                dbContext)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbContextEventsPublisher" /> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be used to publish the events to the internal bus.
        /// </param>
        /// <param name="dbContext">
        ///     The <see cref="DbContext" /> to be scanned for domain events.
        /// </param>
        /// <param name="eventsSelector">
        ///     The custom delegate to be used to get the events out of the entities being saved.
        /// </param>
        /// <param name="clearEventsAction">
        ///     The custom delegate to be used to clear the events from the entities after they have been published.
        /// </param>
        public DbContextEventsPublisher(
            Func<object, IEnumerable<object>?> eventsSelector,
            Action<object> clearEventsAction,
            IPublisher publisher,
            DbContext dbContext)
        {
            _eventsSelector = Check.NotNull(eventsSelector, nameof(eventsSelector));
            _clearEventsAction = Check.NotNull(clearEventsAction, nameof(clearEventsAction));
            _publisher = Check.NotNull(publisher, nameof(publisher));
            _dbContext = Check.NotNull(dbContext, nameof(dbContext));
        }

        /// <summary>
        ///     Publishes the domain events generated by the tracked entities and then executes the provided save
        ///     changes procedure.
        /// </summary>
        /// <param name="saveChanges">
        ///     The delegate to the original <c>SaveChanges</c> method.
        /// </param>
        /// <returns>
        ///     The number of entities saved to the database.
        /// </returns>
        public int ExecuteSaveTransaction(Func<int> saveChanges)
        {
            Check.NotNull(saveChanges, nameof(saveChanges));

            return ExecuteSaveTransactionAsync(() => Task.FromResult(saveChanges()), false).Result;
        }

        /// <summary>
        ///     Publishes the domain events generated by the tracked entities and then executes the provided save
        ///     changes procedure.
        /// </summary>
        /// <param name="saveChangesAsync">
        ///     The delegate to the original <c>SaveChangesAsync</c> method.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     number of entities saved to the database.
        /// </returns>
        public Task<int> ExecuteSaveTransactionAsync(Func<Task<int>> saveChangesAsync)
        {
            Check.NotNull(saveChangesAsync, nameof(saveChangesAsync));

            return ExecuteSaveTransactionAsync(saveChangesAsync, true);
        }

        private async Task<int> ExecuteSaveTransactionAsync(Func<Task<int>> saveChanges, bool executeAsync)
        {
            await PublishEventAsync<TransactionStartedEvent>(executeAsync).ConfigureAwait(false);

            var saved = false;
            try
            {
                await PublishDomainEventsAsync(executeAsync).ConfigureAwait(false);

                int result = await saveChanges().ConfigureAwait(false);

                saved = true;

                await PublishEventAsync<TransactionCompletedEvent>(executeAsync).ConfigureAwait(false);

                return result;
            }
            catch
            {
                if (!saved)
                    await PublishEventAsync<TransactionAbortedEvent>(executeAsync).ConfigureAwait(false);

                throw;
            }
        }

        [SuppressMessage("", "VSTHRD103", Justification = Justifications.ExecutesSyncOrAsync)]
        private async Task PublishDomainEventsAsync(bool executeAsync)
        {
            var events = GetDomainEvents();

            // Keep publishing events fired inside the event handlers
            while (events.Any())
            {
                if (executeAsync)
                    await _publisher.PublishAsync(events).ConfigureAwait(false);
                else
                    _publisher.Publish(events);

                events = GetDomainEvents();
            }
        }

        private IReadOnlyCollection<object> GetDomainEvents() =>
            _dbContext.ChangeTracker.Entries().SelectMany(
                entityEntry =>
                {
                    var selected = _eventsSelector(entityEntry.Entity)?.ToList();

                    // Clear all events to avoid firing the same event multiple times during the recursion
                    _clearEventsAction(entityEntry.Entity);

                    return selected ?? Enumerable.Empty<object>();
                }).ToList();

        [SuppressMessage("", "VSTHRD103", Justification = Justifications.ExecutesSyncOrAsync)]
        private async Task PublishEventAsync<TEvent>(bool executeAsync)
            where TEvent : new()
        {
            if (executeAsync)
                await _publisher.PublishAsync(new TEvent()).ConfigureAwait(false);
            else
                _publisher.Publish(new TEvent());
        }
    }
}
