﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing;

/// <summary>
///     The strategy used to produce the messages.
/// </summary>
public interface IProduceStrategyImplementation
{
    /// <summary>
    ///     Produces the message in the specified envelope.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" /> containing the message to be produced.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task ProduceAsync(IOutboundEnvelope envelope);

    /// <summary>
    ///     Produces the messages in the specified envelopes.
    /// </summary>
    /// <param name="envelopes">
    ///     The <see cref="IOutboundEnvelope" /> containing the messages to be produced.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task ProduceAsync(IEnumerable<IOutboundEnvelope> envelopes);

    /// <summary>
    ///     Produces the messages in the specified envelopes.
    /// </summary>
    /// <param name="envelopes">
    ///     The <see cref="IOutboundEnvelope" /> containing the messages to be produced.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task ProduceAsync(IAsyncEnumerable<IOutboundEnvelope> envelopes);
}
