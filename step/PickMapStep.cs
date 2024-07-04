using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unoh.step {
    public class PickMapStep : IFlipStep {

        public string Name => "pick-map";

        public DiscordMessageBuilder Create(MatchState state) {

            DiscordMessageBuilder builder = new();
            builder.WithContent($"{state.GetCurrentTeamCaptainPings()}, pick a map");

            DiscordEmbedBuilder embed = new();
            embed.Title = $"{state.GetCurrentTeam().Tag}, pick a map";
            embed.Description = "";

            List<string> bases = state.GetUnbannedBases();
            for (int i = 0; i < bases.Count; ++i) {
                string b = bases[i];

                embed.Description += $"[{i + 1}] {b}\n";
            }
            embed.Description += "\n";
            embed.Color = DiscordColor.SpringGreen;

            List<DiscordSelectComponentOption> options = bases.Select(iter => {
                return new DiscordSelectComponentOption(iter, iter);
            }).ToList();

            DiscordSelectComponent dropdown = new($"@pick-map", "Pick a map...", options);
            builder.AddComponents(dropdown);

            builder.AddEmbed(embed);

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            string[] maps = args.Values;
            if (maps.Length != 1) {
                throw new Exception($"expected 1 value, got {maps.Length} instead");
            }

            string map = maps[0];
            state.AddBase(map);

            DiscordMessageBuilder builder = new();

            DiscordEmbedBuilder embed = new();
            embed.Title = $"Map picked";
            embed.Description = $"<@{args.User.Id}> pick {map}";
            embed.Color = DiscordColor.Green;

            builder.AddEmbed(embed);

            state.SwapTeam();

            return Task.FromResult(builder);
        }
    }
}
