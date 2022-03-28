﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivitySourcesTests
{
    [Fact]
    public void StartConsumeActivity_WithoutActivityHeaders_ActivityIsNotModified()
    {
        IRawInboundEnvelope envelope = CreateInboundEnvelope(new MessageHeaderCollection());

        Activity activity = ActivitySources.StartConsumeActivity(envelope);

        activity.TraceStateString.Should().BeNull();
        activity.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void StartConsumeActivity_WithoutStateAndBaggageHeaders_ActivityStateAndBaggageAreNotSet()
    {
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        Activity activity = ActivitySources.StartConsumeActivity(envelope);

        activity.TraceStateString.Should().BeNull();
        activity.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void StartConsumeActivity_WithBaggageHeader_ActivityBaggageIsSet()
    {
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { DefaultMessageHeaders.TraceBaggage, "key1=value1" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        Activity activity = ActivitySources.StartConsumeActivity(envelope);

        activity.Baggage.Should().ContainEquivalentOf(new KeyValuePair<string, string>("key1", "value1"));
    }

    [Fact]
    public void StartConsumeActivity_WithStateHeader_ActivityStateIsSet()
    {
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { DefaultMessageHeaders.TraceState, "key1=value1" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        Activity activity = ActivitySources.StartConsumeActivity(envelope);

        activity.TraceStateString.Should().Be("key1=value1");
    }

    [Fact]
    public void StartConsumeActivity_WhenActivitySourceListener_StartsActivityThroughSource()
    {
        using TestActivityListener listener = new();

        IRawInboundEnvelope envelope = CreateInboundEnvelope(new MessageHeaderCollection());
        Activity activity = ActivitySources.StartConsumeActivity(envelope);

        listener.Activities.Should().Contain(activity);
    }

    private static IRawInboundEnvelope CreateInboundEnvelope(MessageHeaderCollection headers) =>
        new RawInboundEnvelope(
            Stream.Null,
            headers,
            new TestConsumerEndpointConfiguration("Endpoint").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset("key", "7"));

    private sealed class TestActivityListener : IDisposable
    {
        private readonly ActivityListener _listener;

        private readonly List<Activity> _activities = new();

        public TestActivityListener()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                ActivityStarted = activity => _activities.Add(activity),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public IEnumerable<Activity> Activities => _activities;

        public void Dispose() => _listener.Dispose();
    }
}
