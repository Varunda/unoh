using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.discord;

namespace unoh.step {

    public class FinalizeStep : IFlipStep {

        public string Name => "finalize";

        public DiscordMessageBuilder Create(MatchState state) {
            DiscordMessageBuilder builder = new();
            builder.AddEmbed(_MakeEmbed(state));
            builder.AddComponents(FlipButtons.FINALIZE());

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            DiscordMessageBuilder builder = new();

            builder.AddEmbed(_MakeEmbed(state));
            builder.WithContent($"<@&{state.Config.StaffRoleId}> match ready");
            builder.AddMention(new RoleMention(state.Config.StaffRoleId));
            builder.WithAllowedMention(new RoleMention(state.Config.StaffRoleId));

            return Task.FromResult(builder);
        }

        private DiscordEmbedBuilder _MakeEmbed(MatchState state) {
            DiscordEmbedBuilder embed = new();
            embed.Title = $"{state.Team1.Team.Tag} v {state.Team2.Team.Tag}";
            embed.Description = "";

            embed.Description += $"{state.Team1.Tag} on {state.Team1.Faction}\n";
            embed.Description += $"{state.Team2.Tag} on {state.Team2.Faction}\n\n";

            List<MatchBase> bases = state.GetPickedBases();
            for (int i = 0; i < bases.Count; ++i) {
                MatchBase b = bases[i];

                embed.Description += $"**Map {i + 1}**\n";
                embed.Description += $"{state.Team1.Tag} starts {b.Team1Side}\n";
                embed.Description += $"{state.Team2.Tag} starts {b.Team2Side}\n";
            }

            embed.Color = DiscordColor.Green;

            return embed;
        }

    }
}
