// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     The <see cref="InMemoryKafkaOffsetStore" /> settings.
/// </summary>
public record InMemoryKafkaOffsetStoreSettings : KafkaOffsetStoreSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryKafkaOffsetStoreSettings" /> class.
    /// </summary>
    public InMemoryKafkaOffsetStoreSettings()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryKafkaOffsetStoreSettings" /> class.
    /// </summary>
    /// <param name="offsetStoreName">
    ///     The name of the offsetStore.
    /// </param>
    public InMemoryKafkaOffsetStoreSettings(string offsetStoreName)
    {
        OffsetStoreName = offsetStoreName;
    }

    /// <summary>
    ///     Gets the name of the offsetStore.
    /// </summary>
    public string OffsetStoreName { get; init; } = "default";

    /// <inheritdoc cref="KafkaOffsetStoreSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(OffsetStoreName))
            throw new SilverbackConfigurationException("The offset store name is required.");
    }
}
