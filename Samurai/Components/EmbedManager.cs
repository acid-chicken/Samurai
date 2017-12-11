using System;
using System.Reflection;
using Discord;

namespace AcidChicken.Samurai.Components
{
    using static Program;

    public static class EmbedManager
    {
        public static EmbedFooterBuilder CurrentFooter { get; } =
            new EmbedFooterBuilder()
                .WithText($"{DiscordClient.CurrentUser.Username} {((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute))).InformationalVersion}")
                .WithIconUrl(DiscordClient.CurrentUser.GetAvatarUrl());
    }
}
