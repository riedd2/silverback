﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages;

/// <summary>
///     A message that triggers an action with a result <typeparamref name="TResult" />.
/// </summary>
/// <typeparam name="TResult">
///     The type of the result being returned.
/// </typeparam>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Marker interface")]
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used by the Publisher")]
public interface ICommand<out TResult> : ICommand
{
}
