using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.discord;

namespace unoh.step {

    public class PickFactionStep : IFlipStep {

        public string Name => "pick-faction";

        public DiscordMessageBuilder Create(MatchState state) {
            if (state.GetAvailableFactions().Count <= 0) {
                throw new Exception($"no factions available {string.Join(", ", state.Config.Factions)}");
            }

            DiscordMessageBuilder builder = new();
            builder.WithContent($"{state.GetCurrentTeamCaptainPings()}, pick a faction");

            DiscordEmbedBuilder embed = new();
            embed.Title = $"{state.GetCurrentTeam().Team.Tag}, pick a faction";
            embed.Description = "";
            builder.AddEmbed(embed);

            DiscordComponent[] comps = [];
            foreach (string faction in state.GetAvailableFactions()) {
                comps = comps.Append(FlipButtons.PICK_FACTION(faction, faction)).ToArray();
            }

            builder.AddComponents(comps);

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            string[] parts = args.Id.Split(".");
            if (parts.Length != 2) {
                throw new Exception($"expected 2 parts from {args.Id}");
            }

            state.SetFaction(state.GetCurrentTeamIndex(), parts[1]);

            DiscordMessageBuilder builder = new();

            DiscordEmbedBuilder embed = new();
            embed.Title = $"Faction picked";
            embed.Description = $"<@{args.User.Id}> picked {state.GetCurrentTeam().Faction} for {state.GetCurrentTeam().Team.Tag}\n\n";
            if (state.GetCurrentTeam().Faction == "VS") {
                embed.Color = DiscordColor.Purple;
            } else if (state.GetCurrentTeam().Faction == "NC") {
                embed.Color = DiscordColor.Blue;
            } else if (state.GetCurrentTeam().Faction == "TR") {
                embed.Color = DiscordColor.Red;
            }

            state.SwapTeam();
            embed.Description += $"[{state.Team1.Team.Tag}] {state.Team1.Team.Name}: {state.Team1.Faction}\n";
            embed.Description += $"[{state.Team2.Team.Tag}] {state.Team2.Team.Name}: {state.Team2.Faction}";

            builder.AddEmbed(embed);

            return Task.FromResult(builder);
        }

    }
}
