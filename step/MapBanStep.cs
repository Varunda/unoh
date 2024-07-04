using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.config;

namespace unoh.step {

    public class MapBanStep : IFlipStep {

        public string Name => "map-ban";

        public DiscordMessageBuilder Create(MatchState state) {
            if (state.GetUnbannedBases().Count == 0) {
                throw new Exception($"0 bases in match state?");
            }

            DiscordMessageBuilder builder = new();
            builder.WithContent($"{state.GetCurrentTeamCaptainPings()}, ban a map");

            DiscordEmbedBuilder embed = new();
            embed.Title = $"{state.GetCurrentTeam().Team.Tag}, ban a map";
            embed.Description = "**Map pool**:\n";
            for (int i = 0; i < state.Config.Bases.Count; ++i) {
                TourneyBase iter = state.Config.Bases[i];

                if (state.GetUnbannedBases().Contains(iter.Name)) {
                    embed.Description += $"{iter.Name}\n";
                } else {
                    embed.Description += $"~~{iter.Name}~~\n";
                }
            }
            embed.Color = DiscordColor.Red;
            builder.AddEmbed(embed);

            List<DiscordSelectComponentOption> options = state.GetUnbannedBases().Select(iter => {
                return new DiscordSelectComponentOption(iter, iter);
            }).ToList();

            DiscordSelectComponent dropdown = new($"@ban-map", "Ban a map...", options);
            builder.AddComponents(dropdown);

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            string[] maps = args.Values;
            if (maps.Length != 1) {
                throw new Exception($"expected 1 value, got {maps.Length} instead");
            }

            string map = maps[0];
            state.RemoveBase(map);

            DiscordMessageBuilder builder = new();

            DiscordEmbedBuilder embed = new();
            embed.Title = $"Map banned";
            embed.Description = $"<@{args.User.Id}> banned {map}";
            embed.Color = DiscordColor.Purple;

            builder.AddEmbed(embed);

            state.SwapTeam();

            return Task.FromResult(builder);
        }

    }
}
