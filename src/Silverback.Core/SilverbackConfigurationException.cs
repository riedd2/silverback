﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback;

/// <summary>
///     The exception that is thrown when the specified configuration is not valid.
/// </summary>
[SuppressMessage("Usage", "CA2237:Mark ISerializable types with serializable", Justification = "Not required anymore")]
[ExcludeFromCodeCoverage]
public class SilverbackConfigurationException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackConfigurationException" /> class.
    /// </summary>
    public SilverbackConfigurationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackConfigurationException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public SilverbackConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackConfigurationException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public SilverbackConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}