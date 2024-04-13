// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.Util;

public static class MessageSerializerExtensions
{
    public static async ValueTask<Stream> SerializeAsync(
        this IMessageSerializer serializer,
        object message,
        MessageHeaderCollection? headers = null) =>
        await serializer.SerializeAsync(message, headers ?? [], NullProducerEndpoint.Instance) ??
        throw new InvalidOperationException("Serializer returned null");

    public static Stream Serialize(
        this IMessageSerializer serializer,
        object message,
        MessageHeaderCollection? headers = null) =>
        SerializeAsync(serializer, message, headers).SafeWait() ??
        throw new InvalidOperationException("Serializer returned null");

    public static byte[] SerializeToBytes(
        this IMessageSerializer serializer,
        object message,
        MessageHeaderCollection? headers = null) =>
        Serialize(serializer, message, headers).ReadAll() ?? throw new InvalidOperationException("Serializer returned null");

    private record NullProducerEndpoint : ProducerEndpoint
    {
        private NullProducerEndpoint()
            : base("null", new NullProducerEndpointConfiguration())
        {
        }

        public static NullProducerEndpoint Instance { get; } = new();

        private record NullProducerEndpointConfiguration : ProducerEndpointConfiguration<NullProducerEndpoint>;
    }
}