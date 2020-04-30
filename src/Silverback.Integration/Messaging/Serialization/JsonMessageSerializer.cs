﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes the messages in JSON format and relies on some added headers
    ///     to determine the message type upon deserialization. This default serializer
    ///     is ideal when the producer and the consumer are both using Silverback.
    /// </summary>
    /// <inheritdoc cref="IMessageSerializer" />
    public class JsonMessageSerializer : IMessageSerializer
    {
        /// <summary>
        ///     Gets the default static instance of <see cref="JsonMessageSerializer"/>.
        /// </summary>
        public static JsonMessageSerializer Default { get; } = new JsonMessageSerializer();
        
        /// <summary>
        ///     Gets or sets the message encoding. The default is UTF8.
        /// </summary>
        [DefaultValue("UTF8")]
        public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

        /// <summary>
        ///     Gets or sets the settings to be applied to the Json.NET serializer.
        /// </summary>
        public JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public virtual byte[] Serialize(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            switch (message)
            {
                case null:
                    return null;
                case byte[] bytes:
                    return bytes;
            }

            var type = message.GetType();
            var json = JsonConvert.SerializeObject(message, type, Settings);

            messageHeaders.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

            return GetEncoding().GetBytes(json);
        }

        public virtual object Deserialize(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            if (message == null || message.Length == 0)
                return null;

            var json = GetEncoding().GetString(message);
            var type = SerializationHelper.GetTypeFromHeaders(messageHeaders);

            return JsonConvert.DeserializeObject(json, type, Settings);
        }

        public virtual Task<byte[]> SerializeAsync(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Serialize(message, messageHeaders, context));

        public virtual Task<object> DeserializeAsync(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Deserialize(message, messageHeaders, context));

        protected System.Text.Encoding GetEncoding() =>
            Encoding switch
            {
                MessageEncoding.Default => System.Text.Encoding.Default,
                MessageEncoding.ASCII => System.Text.Encoding.ASCII,
                MessageEncoding.UTF8 => System.Text.Encoding.UTF8,
                MessageEncoding.UTF32 => System.Text.Encoding.UTF32,
                MessageEncoding.Unicode => System.Text.Encoding.Unicode,
                _ => throw new InvalidOperationException("Unhandled encoding.")
            };
    }

    /// <summary>
    ///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
    /// </summary>
    /// <typeparam name="TMessage">The type of the messages to be serialized and/or deserialized.</typeparam>
    /// <inheritdoc />
    public class JsonMessageSerializer<TMessage> : JsonMessageSerializer
    {
        private readonly Type _type = typeof(TMessage);
        
        public override byte[] Serialize(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            switch (message)
            {
                case null:
                    return null;
                case byte[] bytes:
                    return bytes;
            }

            var json = JsonConvert.SerializeObject(message, _type, Settings);

            return GetEncoding().GetBytes(json);
        }

        public override object Deserialize(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null || message.Length == 0)
                return null;

            var json = GetEncoding().GetString(message);

            return JsonConvert.DeserializeObject(json, _type, Settings);
        }
    }
}