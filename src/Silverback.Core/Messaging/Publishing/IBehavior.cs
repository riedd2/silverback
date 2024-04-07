﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Can be used to build a custom pipeline, plugging some functionality into the
///     <see cref="IPublisher" />.
/// </summary>
public interface IBehavior
{
    /// <summary>
    ///     Process, handles or transforms the messages being published to the internal bus.
    /// </summary>
    /// <param name="message">
    ///     The message being published.
    /// </param>
    /// <param name="next">
    ///     The next behavior in the pipeline.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     result values (if any).
    /// </returns>
    ValueTask<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next);
}
