﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeScopedSubscriber()
    {
        TestingCollection<TestEventOne> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber(_ => new SimpleSubscriber(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeTransientSubscriber()
    {
        TestingCollection<TestEventOne> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddTransientSubscriber(_ => new SimpleSubscriber(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSingletonSubscriber()
    {
        TestingCollection<TestEventOne> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleSubscriber(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSyncAndAsyncSubscribedMethods()
    {
        TestingCollection<TestEventOne> syncMessages = [];
        TestingCollection<TestEventOne> asyncMessages = [];
        TestingCollection<TestEventOne> asyncValueTaskMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SyncAndAsyncSubscriber(syncMessages, asyncMessages, asyncValueTaskMessages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        syncMessages.Should().HaveCount(2);
        asyncMessages.Should().HaveCount(2);
        asyncValueTaskMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeAllSubscribers()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleSubscriber(messages1))
                .AddSingletonSubscriber(new SimpleOtherSubscriber(messages2)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        messages1.Should().HaveCount(2);
        messages2.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeAllPublicAndDecoratedMethods_WhenSubscribingWithDefaultSettings()
    {
        TestingCollection<TestEventOne> publicMessages = [];
        TestingCollection<TestEventOne> publicDecoratedMessages = [];
        TestingCollection<TestEventOne> privateMessages = [];
        TestingCollection<TestEventOne> privateDecoratedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new VisibilitiesSubscriber(publicMessages, publicDecoratedMessages, privateMessages, privateDecoratedMessages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        publicMessages.Should().HaveCount(2);
        publicDecoratedMessages.Should().HaveCount(2);
        privateMessages.Should().BeEmpty();
        privateDecoratedMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeDecoratedMethodsOnly_WhenAutoSubscriptionIsDisabled()
    {
        TestingCollection<TestEventOne> publicMessages = [];
        TestingCollection<TestEventOne> publicDecoratedMessages = [];
        TestingCollection<TestEventOne> privateMessages = [];
        TestingCollection<TestEventOne> privateDecoratedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(
                    new VisibilitiesSubscriber(publicMessages, publicDecoratedMessages, privateMessages, privateDecoratedMessages),
                    false));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        publicMessages.Should().BeEmpty();
        publicDecoratedMessages.Should().HaveCount(2);
        privateMessages.Should().BeEmpty();
        privateDecoratedMessages.Should().HaveCount(2);
    }

    [Fact]
    public void Publish_ShouldInvokeSubscribersSequentially_WhenExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleExclusiveSubscriber(messages1, messages2)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribersSequentially_WhenExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleExclusiveSubscriber(messages1, messages2)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }

    [Fact]
    public void Publish_ShouldInvokeSubscribersInParallel_WhenNotExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleNotExclusiveSubscriber(messages1, messages2)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribersInParallel_WhenNotExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleNotExclusiveSubscriber(messages1, messages2)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }

    private class SimpleSubscriber
    {
        private readonly TestingCollection<TestEventOne> _messages;

        public SimpleSubscriber(TestingCollection<TestEventOne> messages)
        {
            _messages = messages;
        }

        [UsedImplicitly]
        public void Subscriber(TestEventOne message) => _messages.Add(message);
    }

    private class SimpleOtherSubscriber
    {
        private readonly TestingCollection<TestEventOne> _messages;

        public SimpleOtherSubscriber(TestingCollection<TestEventOne> messages)
        {
            _messages = messages;
        }

        [UsedImplicitly]
        public void Subscriber(TestEventOne message) => _messages.Add(message);
    }

    private class SyncAndAsyncSubscriber
    {
        private readonly TestingCollection<TestEventOne> _syncMessages;

        private readonly TestingCollection<TestEventOne> _asyncMessages;

        private readonly TestingCollection<TestEventOne> _asyncValueTaskMessages;

        public SyncAndAsyncSubscriber(TestingCollection<TestEventOne> syncMessages, TestingCollection<TestEventOne> asyncMessages, TestingCollection<TestEventOne> asyncValueTaskMessages)
        {
            _syncMessages = syncMessages;
            _asyncMessages = asyncMessages;
            _asyncValueTaskMessages = asyncValueTaskMessages;
        }

        [UsedImplicitly]
        public void SyncSubscriber(TestEventOne message) => _syncMessages.Add(message);

        [UsedImplicitly]
        public Task AsyncSubscriber(TestEventOne message) => _asyncMessages.AddAsync(message).AsTask();

        [UsedImplicitly]
        public ValueTask AsyncValueTaskSubscriber(TestEventOne message) => _asyncValueTaskMessages.AddAsync(message);
    }

    private class VisibilitiesSubscriber
    {
        private readonly TestingCollection<TestEventOne> _publicMessages;

        private readonly TestingCollection<TestEventOne> _publicDecoratedMessages;

        private readonly TestingCollection<TestEventOne> _privateMessages;

        private readonly TestingCollection<TestEventOne> _privateDecoratedMessages;

        public VisibilitiesSubscriber(TestingCollection<TestEventOne> publicMessages, TestingCollection<TestEventOne> publicDecoratedMessages, TestingCollection<TestEventOne> privateMessages, TestingCollection<TestEventOne> privateDecoratedMessages)
        {
            _publicMessages = publicMessages;
            _publicDecoratedMessages = publicDecoratedMessages;
            _privateMessages = privateMessages;
            _privateDecoratedMessages = privateDecoratedMessages;
        }

        [UsedImplicitly]
        public void PublicSubscriber(TestEventOne message) => _publicMessages.Add(message);

        [Subscribe]
        [UsedImplicitly]
        public void PublicDecoratedSubscriber(TestEventOne message) => _publicDecoratedMessages.Add(message);

        [UsedImplicitly]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Silverback")]
        private void PrivateSubscriber(TestEventOne message) => _privateMessages.Add(message);

        [Subscribe]
        [UsedImplicitly]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Silverback")]
        private void PrivateDecoratedSubscriber(TestEventOne message) => _privateDecoratedMessages.Add(message);
    }

    private class SimpleExclusiveSubscriber
    {
        private readonly TestingCollection<TestEventOne> _messages1;

        private readonly TestingCollection<TestEventOne> _messages2;

        private int _executingCount;

        public SimpleExclusiveSubscriber(TestingCollection<TestEventOne> messages1, TestingCollection<TestEventOne> messages2)
        {
            _messages1 = messages1;
            _messages2 = messages2;
        }

        [Subscribe(Exclusive = true)]
        [UsedImplicitly]
        public async Task Subscriber1Async(TestEventOne message) => await ExecuteAsync(message, _messages1);

        [Subscribe(Exclusive = true)]
        [UsedImplicitly]
        public async Task Subscriber2Async(TestEventOne message) => await ExecuteAsync(message, _messages2);

        private async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            Interlocked.Increment(ref _executingCount);

            if (_executingCount > 1)
                throw new InvalidOperationException("Exclusive subscriber is already executing.");

            await messages.AddAsync(message);
            await Task.Delay(100);
            Interlocked.Decrement(ref _executingCount);
        }
    }

    private sealed class SimpleNotExclusiveSubscriber : IDisposable
    {
        private readonly TestingCollection<TestEventOne> _messages1;

        private readonly TestingCollection<TestEventOne> _messages2;

        private readonly CountdownEvent _countdownEvent = new(2);

        public SimpleNotExclusiveSubscriber(TestingCollection<TestEventOne> messages1, TestingCollection<TestEventOne> messages2)
        {
            _messages1 = messages1;
            _messages2 = messages2;
        }

        [Subscribe(Exclusive = false)]
        [UsedImplicitly]
        public async Task Subscriber1Async(TestEventOne message) => await ExecuteAsync(message, _messages1);

        [Subscribe(Exclusive = false)]
        [UsedImplicitly]
        public async Task Subscriber2Async(TestEventOne message) => await ExecuteAsync(message, _messages2);

        public void Dispose() => _countdownEvent.Dispose();

        private async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            if (!_countdownEvent.Signal())
                _countdownEvent.WaitOrThrow();

            await messages.AddAsync(message);
        }
    }
}
