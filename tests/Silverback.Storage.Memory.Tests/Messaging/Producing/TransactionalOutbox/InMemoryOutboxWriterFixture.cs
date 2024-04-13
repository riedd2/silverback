// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Producing.TransactionalOutbox;

public class InMemoryOutboxWriterFixture
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null);

    [Fact]
    public async Task AddAsync_ShouldAddItemToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new([0x01], null, Endpoint);
        OutboxMessage outboxMessage2 = new([0x02], null, Endpoint);
        OutboxMessage outboxMessage3 = new([0x03], null, Endpoint);
        await outboxWriter.AddAsync(outboxMessage1);
        await outboxWriter.AddAsync(outboxMessage2);
        await outboxWriter.AddAsync(outboxMessage3);

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemsToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new([0x01], null, Endpoint);
        OutboxMessage outboxMessage2 = new([0x02], null, Endpoint);
        OutboxMessage outboxMessage3 = new([0x03], null, Endpoint);
        await outboxWriter.AddAsync([outboxMessage1, outboxMessage2, outboxMessage3]);

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldAddAsyncItemsToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new([0x01], null, Endpoint);
        OutboxMessage outboxMessage2 = new([0x02], null, Endpoint);
        OutboxMessage outboxMessage3 = new([0x03], null, Endpoint);
        await outboxWriter.AddAsync(new[] { outboxMessage1, outboxMessage2, outboxMessage3 }.ToAsyncEnumerable());

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).Should().BeEquivalentTo(new[] { outboxMessage1, outboxMessage2, outboxMessage3 });
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemToCorrectStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings1 = new("outbox1");
        InMemoryOutboxSettings outboxSettings2 = new("outbox2");
        IOutboxWriter outboxWriter1 = writerFactory.GetWriter(outboxSettings1, serviceProvider);
        IOutboxWriter outboxWriter2 = writerFactory.GetWriter(outboxSettings2, serviceProvider);

        await outboxWriter1.AddAsync(new OutboxMessage([0x01], null, Endpoint));
        await outboxWriter2.AddAsync(new OutboxMessage([0x02], null, Endpoint));

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox1 = outboxFactory.GetOutbox(outboxSettings1);
        InMemoryOutbox outbox2 = outboxFactory.GetOutbox(outboxSettings2);
        outbox1.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x01 }
            });
        outbox2.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x02 }
            });
    }
}