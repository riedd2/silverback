// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.EndpointResolvers;

internal class NullProducerEndpointResolver<TEndpoint> : NullProducerEndpointResolver, IProducerEndpointResolver<TEndpoint>
    where TEndpoint : ProducerEndpoint
{
    public static readonly NullProducerEndpointResolver<TEndpoint> Instance = new();

    private NullProducerEndpointResolver()
    {
    }
}
