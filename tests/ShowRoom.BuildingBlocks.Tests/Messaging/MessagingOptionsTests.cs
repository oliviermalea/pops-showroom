using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using ShowRoom.BuildingBlocks.Messaging;
using Xunit;

namespace ShowRoom.BuildingBlocks.Tests.Messaging;

public sealed class MessagingOptionsTests
{
    private static IConfiguration Config(params (string Key, string Value)[] entries)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(entries.ToDictionary(e => e.Key, e => (string?)e.Value))
            .Build();

    [Fact]
    public void FromConfiguration_binds_transport_RabbitMq_from_string()
    {
        var config = Config(
            ("Messaging:Enabled", "true"),
            ("Messaging:Transport", "RabbitMq"),
            ("Messaging:RabbitMqConnectionName", "messaging"));

        var sut = MessagingOptions.FromConfiguration(config);

        sut.Enabled.Should().BeTrue();
        sut.Transport.Should().Be(MessagingTransport.RabbitMq);
        sut.RabbitMqConnectionName.Should().Be("messaging");
    }

    [Fact]
    public void FromConfiguration_binds_transport_InMemory_from_string_case_insensitively()
    {
        // "inmemory" (lower-case) must still resolve — proves the TypeConverter runs (InMemory != default).
        var config = Config(
            ("Messaging:Enabled", "true"),
            ("Messaging:Transport", "inmemory"));

        var sut = MessagingOptions.FromConfiguration(config);

        sut.Transport.Should().Be(MessagingTransport.InMemory);
    }

    [Fact]
    public void FromConfiguration_uses_defaults_when_section_is_absent()
    {
        var sut = MessagingOptions.FromConfiguration(Config());

        sut.Enabled.Should().BeFalse();
        sut.Transport.Should().Be(MessagingTransport.RabbitMq);
        sut.RabbitMqConnectionName.Should().Be("messaging");
        sut.EnableRemoteInvocation.Should().BeTrue();
        sut.UseDurableLocalQueues.Should().BeFalse();
    }
}
