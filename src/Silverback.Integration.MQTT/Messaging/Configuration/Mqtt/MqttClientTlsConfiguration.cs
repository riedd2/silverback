// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Client;
using Silverback.Collections;
using Silverback.Configuration;
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
using System.Net.Security;
#endif

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The TLS configuration.
/// </summary>
public partial record MqttClientTlsConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientTlsConfiguration" /> class.
    /// </summary>
    [SuppressMessage("Security", "CA5398:Avoid hardcoded SslProtocols values", Justification = "Needed for proper initialization")]
    public MqttClientTlsConfiguration()
    {
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
        SslProtocol = SslProtocols.Tls13;
#else
        SslProtocol = SslProtocols.Tls12;
#endif
    }

    /// <summary>
    ///     Gets the client side certificates.
    /// </summary>
    public IValueReadOnlyCollection<X509Certificate>? Certificates { get; init; }

#if NETCOREAPP3_1 || NET5_0_OR_GREATER
    /// <summary>
    ///     Gets the TLS protocols to use.
    /// </summary>
    public IValueReadOnlyCollection<SslApplicationProtocol>? ApplicationProtocols { get; init; }

    /// <summary>
    ///     Gets the <see cref="System.Net.Security.CipherSuitesPolicy" />.
    /// </summary>
    public CipherSuitesPolicy? CipherSuitesPolicy { get; init; }
#endif

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        // Nothing to validate
    }

    internal MqttClientTlsOptions ToMqttNetType()
    {
        MqttClientTlsOptions options = MapCore();

        options.Certificates = Certificates?.ToList();

#if NETCOREAPP3_1 || NET5_0_OR_GREATER
        options.ApplicationProtocols = ApplicationProtocols?.ToList();
        options.CipherSuitesPolicy = CipherSuitesPolicy;
#endif

        return options;
    }
}
