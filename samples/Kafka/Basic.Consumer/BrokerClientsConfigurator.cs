﻿using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.Basic.Common;

namespace Silverback.Samples.Kafka.Basic.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddKafkaClients(
                clients => clients

                    // The bootstrap server address is needed to connect
                    .WithBootstrapServers("PLAINTEXT://localhost:9092")

                    // Add a consumer
                    .AddConsumer(
                        consumer => consumer

                            // Set the consumer group id
                            .WithGroupId("sample-consumer")

                            // AutoOffsetReset.Earliest means that the consumer
                            // must start consuming from the beginning of the topic,
                            // if no offset was stored for this consumer group
                            .AutoResetOffsetToEarliest()

                            // Consume the SampleMessage from the samples-basic topic
                            .Consume<SampleMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom("samples-basic")

                                    // Retry twice to process each message in case of
                                    // exception, then skip it
                                    .OnError(policy => policy.Retry(2).ThenSkip()))));
    }
}
