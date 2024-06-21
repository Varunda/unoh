using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.code.extension;
using unoh.config;

namespace unoh.discord {

    public class FlipButtons {

        public static DiscordButtonComponent ACCEPT(string team1, string team2, string format = "bo1")
            => new(DSharpPlus.ButtonStyle.Success, $"@accept.{team1}.{team2}.{format}", "Accept");

        public static DiscordButtonComponent NEXT_STEP() => new(DSharpPlus.ButtonStyle.Primary, "@next", "Next");

        public static DiscordButtonComponent COINFLIP_PICK(int index, string label) => new(DSharpPlus.ButtonStyle.Primary, $"@coinflip.{index}", label);

        public static DiscordButtonComponent PICK_FACTION(string faction, string label) => new(DSharpPlus.ButtonStyle.Primary, $"@pick-faction.{faction}", label);

        public static DiscordButtonComponent PICK_SIZE(string team, string side, string label) => new(DSharpPlus.ButtonStyle.Primary, $"@pick-side.{team}.{side}", label);

        public static DiscordButtonComponent FINALIZE() => new(DSharpPlus.ButtonStyle.Success, $"@finalize", "Confirm");

    }
}
