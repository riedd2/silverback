﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Behaviors;

/// <summary>
///     Can be used to build a custom pipeline, plugging some functionality into the
///     <see cref="IProducer" />.
/// </summary>
public interface IProducerBehavior : IBrokerBehavior, ISorted
{
    /// <summary>
    ///     Process, handles or transforms the message being produced.
    /// </summary>
    /// <param name="context">
    ///     The context that is passed along the behaviors pipeline.
    /// </param>
    /// <param name="next">
    ///     The next behavior in the pipeline.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next);
}
