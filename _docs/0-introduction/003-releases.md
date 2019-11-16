---
title:  Releases
permalink: /docs/releases
toc: false
---

## **1.0.0**

**What's new**
* Message size optimization (no wrappers anymore)
* Better headers usage: identifiers, types, chunks information, etc. are now all sent in the headers
* Reviewed severity of some log entries
* Cleaner internal implementation
* Better exception handling (flattening of `AggregateException`)
* Upgrade to [Confluent.Kafka 1.2.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.2.2)
* The Kafka consumer automatically recovers from fatal errors (can be disabled via Endpoint configuration)
* Support for .Net Core 3.0 and Entity Framework Core 3.0
* Refactored packages (EF binding logic is now in a single package, versioned after the related EF version)
* Better and cleaner configuration API (see for example [Using the Bus]({{ site.baseurl }}/docs/quickstart/bus) and [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors))
* Some performance improvements and optimizations (including [#37](https://github.com/BEagle1984/silverback/issues/37))
* Improved database locks mechanism (used also to run the `OutboundQueueWorker`)

**Breaking Changes**
* By default the messages published via `IPublisher` that are routed to an outbound endpoint are not sent through to the internal bus and cannot therfore be subscribed locally, within the same process (see [Outbound Connector]({{ site.baseurl }}/docs/configuration/outbound))
* Some changes in `IInboundMessage` and `IOutboundMessage` interfaces
* Changes to the schema of the outbox table (`Silverback.Messaging.Connectors.Model.OutboundMessage`)
* The configuration fluent API changed quite a bit, refer to the current documentation (e.g. [Using the Bus]({{ site.baseurl }}/docs/quickstart/bus) and [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))
* `Silverback.Integration.EntityFrameworkCore` and `Silverback.EventSourcing.EntityFrameworkCore` have been deprecated (`Silverback.Core.EntityFrameworkCore` contains all the necessary logic to use EF as store)
* `KeyMemberAttribute` has been renamed to `PartitioningKeyMemberAttribute` (see [Kafka Partitioning]({{ site.baseurl }}/docs/advanced/partitioning))

**Fixes**
* Fixed issue requiring types not implementing `IMessage` to be registered with `HandleMessagesOfType<T>` to consume them [[#33](https://github.com/BEagle1984/silverback/issues/33)]
* Mitigated issue causing the `DistributedBackgroundService` to sometime fail to acquire the database lock [[#39](https://github.com/BEagle1984/silverback/issues/39)]
* Fixed partition key value being lost when using the `DeferredOutboundConnector`
* Other small fixes to improve stability and reliability

## 0.10.0

**What's new**
* Better error handling: now all exceptions, including the ones thrown by the `MessageSerializer` can be handled through the error policies
* Improved logs: promoted some important logs to Information level, writing all processing errors as (at least) Warning and improved logged information quality (logged attributes)
* Add ability to modify messages and headers when moving them via `MoveMessageErrorPolicy`
* Message processing refactoring leading to cleaner, more extensible and predictable API and behavior

**Fixes**
* Several other small (and not so small) issues and bugs

## 0.8.0 - 0.9.0

Released two versions mostly to fix bugs, do some small adjustments according to some user feedbacks and update the external dependencies (e.g. Confluent.Kafka 1.0.1).

**Fixes**
* Fixed exception loading error policies from json in Silverback.Integration.Configuration [[#24](https://github.com/BEagle1984/silverback/issues/24)]

## 0.7.0

**What's new**
* [Confluent.Kafka 1.0.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.0.0) has finally been released and it has been integrated and tested with this version of Silverback
* Created a simple event store that perfectly integrates with the rest of the Silverback framework (see [Event Sourcing]({{ site.baseurl }}/docs/quickstart/event-sourcing))
* Silverback.Integration.InMemory to mock the message broker behavior in your unit tests
* Several small optimizations and improvements

## 0.6.0

**What's new**
* Added support for message headers (only accessible from [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors) or "low-level" [Broker]({{ site.baseurl }}/docs/advanced/broker) implementation)
* Simplified message subscription even further: now all public methods of the types implementing the marker interface `ISubscriber` are automatically subscribed by default without having to annotate them with the `SubscribeAttribute` (this behavior is customizable)
* Upgrade to [Confluent.Kafka 1.0.0-RC1](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.0-RC1)

## 0.3.x - 0.5.x

Some releases where done adding quite a few features-

**What's new**
* Silverback.Integration.Configuration package to load the inbound/outbound configuration from the app.settings json
* Batch processing
* Parallel subscribers
* Delegate subscription as an alternative to `SubscribeAttribute` based subscription
* Improved support for Rx.net
* Support for legacy messages and POCO classes
* Offset storage as an alternative and more optimized way to guarantee exactly once processing, storing just the offset of the last message instead of logging every message  (see [Inbound Connector]({{ site.baseurl }}/docs/configuration/inbound))
* Behaviors as a convenient way to implement your cross-cutting concerns (like logging, validation, etc.) to be plugged into the internal bus publishing pipeline (see [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors))
* Message chunking to automatically split the larger messages and rebuild them on the other end (see [Chunking]({{ site.baseurl }}/docs/advanced/chunking))
* much more...a huge amount of refactorings

**Fixes**
* Several fixes and optimizations


## 0.3.2

The very first public release of Silverback! It included:
* In-process message bus
* Inbound/outbound connector for message broker abstraction
* Kafka broker implementation
* Outbox table pattern implementation
* Exactly once processing
* ...