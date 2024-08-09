﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Configuration;

public partial class BrokerOptionsBuilderMemoryExtensionsFixture
{
    [Fact]
    public void AddInMemoryKafkaOffsetStore_ShouldConfigureOffsetStoreFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddInMemoryKafkaOffsetStore()));

        IKafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();

        IKafkaOffsetStore store = factory.GetStore(new InMemoryKafkaOffsetStoreSettings(), serviceProvider);

        store.Should().BeOfType<InMemoryKafkaOffsetStore>();
    }

    [Fact]
    public void UseInMemoryKafkaOffsetStore_ShouldOverrideAllOffsetStoreSettingsTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().UseInMemoryKafkaOffsetStore()));

        KafkaOffsetStoreFactory factory = serviceProvider.GetRequiredService<KafkaOffsetStoreFactory>();

        factory.AddFactory<KafkaOffsetStoreSettings1>((_, _) => new KafkaOffsetStore1());
        factory.AddFactory<KafkaOffsetStoreSettings2>((_, _) => new KafkaOffsetStore2());

        IKafkaOffsetStore store1 = factory.GetStore(new KafkaOffsetStoreSettings1(), serviceProvider);
        IKafkaOffsetStore store2 = factory.GetStore(new KafkaOffsetStoreSettings2(), serviceProvider);

        store1.Should().BeOfType<InMemoryKafkaOffsetStore>();
        store2.Should().BeOfType<InMemoryKafkaOffsetStore>();
    }

    private record KafkaOffsetStoreSettings1 : KafkaOffsetStoreSettings;

    private record KafkaOffsetStoreSettings2 : KafkaOffsetStoreSettings;

    private class KafkaOffsetStore1 : IKafkaOffsetStore
    {
        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) => throw new NotSupportedException();

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) => throw new NotSupportedException();
    }

    private class KafkaOffsetStore2 : IKafkaOffsetStore
    {
        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) => throw new NotSupportedException();

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) => throw new NotSupportedException();
    }
}
