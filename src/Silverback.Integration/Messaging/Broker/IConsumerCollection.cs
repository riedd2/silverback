// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Holds a reference to all the configured <see cref="IConsumer" />.
/// </summary>
public interface IConsumerCollection : IReadOnlyList<IConsumer>;