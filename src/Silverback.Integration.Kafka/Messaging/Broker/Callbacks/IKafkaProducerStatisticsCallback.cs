// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Callbacks.Statistics;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnProducerStatistics" /> event handler.
/// </summary>
public interface IKafkaProducerStatisticsCallback : IBrokerCallback
{
    /// <summary>
    ///     Called on statistics events.
    /// </summary>
    /// <remarks>
    ///     You can enable statistics and set the statistics interval using the <c>StatisticsIntervalMs</c>
    ///     configuration property (disabled by default).
    /// </remarks>
    /// <param name="statistics">
    ///     The deserialized statistics.
    /// </param>
    /// <param name="rawStatistics">
    ///     The raw statistics string.
    /// </param>
    /// <param name="producer">
    ///     The related producer instance.
    /// </param>
    void OnProducerStatistics(KafkaStatistics? statistics, string rawStatistics, KafkaProducer producer);
}
