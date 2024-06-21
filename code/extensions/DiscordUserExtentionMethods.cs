using DSharpPlus.Entities;

namespace unoh.code.extension {

    public static class DiscordUserExtentionMethods {

        public static string GetPing(this DiscordUser user) {
            return $"<@{user.Id}>";
        }

        public static string GetDisplay(this DiscordUser user) {
            return $"{user.Username}#{user.Discriminator} ({user.Id})";
        }

    }
}
