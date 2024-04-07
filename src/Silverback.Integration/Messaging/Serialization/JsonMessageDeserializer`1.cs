﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the JSON messages into an instance of <typeparamref name="TMessage" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be deserialized.
/// </typeparam>
public sealed class JsonMessageDeserializer<TMessage> : IJsonMessageDeserializer, IEquatable<JsonMessageDeserializer<TMessage>>
{
    private readonly Type _type = typeof(TMessage);

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders { get; } = typeof(TMessage) == typeof(object) || typeof(TMessage).IsInterface;

    /// <inheritdoc cref="IJsonMessageDeserializer.Options" />
    public JsonSerializerOptions? Options { get; set; } = new();

    /// <inheritdoc cref="IJsonMessageDeserializer.TypeHeaderBehavior" />
    public JsonMessageDeserializerTypeHeaderBehavior TypeHeaderBehavior { get; set; }

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public async ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        Type type = GetBaseType(headers);

        if (messageStream == null)
            return new DeserializedMessage(null, type);

        if (messageStream.CanSeek && messageStream.Length == 0)
            return new DeserializedMessage(null, type);

        object deserializedObject = await JsonSerializer.DeserializeAsync(messageStream, type, Options).ConfigureAwait(false) ??
                                    throw new MessageSerializerException("The deserialization returned null.");

        return new DeserializedMessage(deserializedObject, type);
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new JsonMessageSerializer { Options = Options };

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(JsonMessageDeserializer<TMessage>? other) => ComparisonHelper.JsonEquals(this, other);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((JsonMessageDeserializer<TMessage>?)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(1, typeof(TMessage).Name);

    private Type GetBaseType(MessageHeaderCollection headers) =>
        TypeHeaderBehavior switch
        {
            JsonMessageDeserializerTypeHeaderBehavior.Optional => SerializationHelper.GetTypeFromHeaders(headers, _type),
            JsonMessageDeserializerTypeHeaderBehavior.Mandatory => SerializationHelper.GetTypeFromHeaders(headers) ??
                                                                   throw new InvalidOperationException($"Message type header ({DefaultMessageHeaders.MessageType}) not found."),
            JsonMessageDeserializerTypeHeaderBehavior.Ignore => _type,
            _ => throw new InvalidOperationException("Unexpected JsonMessageDeserializerTypeHeaderBehavior")
        };
}
