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
            DiscordMessageBuilder builder = new();

            DiscordEmbedBuilder embed = new();

            state.SetFaction(state.GetCurrentTeamIndex(), state.GetCurrentTeam().Team.FactionPreference);

            if (state.Team1.Team.FactionPreference != state.Team2.Team.FactionPreference) {
                builder.WithContent($"{state.GetCurrentTeamCaptainPings()}, confirm factions");
                // if the faction prefs are different, both teams get it, and let the team confirm
                state.SwapTeam();
                state.SetFaction(state.GetCurrentTeamIndex(), state.GetCurrentTeam().Team.FactionPreference);
                state.SwapTeam();

                embed.Title = $"{state.GetCurrentTeam().Team.Tag}, confirm faction picks";
                embed.Description = $"{state.Team1.Tag} gets {state.Team1.Faction}\n{state.Team2.Tag} gets {state.Team2.Faction}\n-# Faction preferences are different, so both teams get their preference";

                builder.AddComponents(FlipButtons.PICK_FACTION("Confirm", "Confirm"));
            } else {
                state.SwapTeam();
                builder.WithContent($"{state.GetCurrentTeamCaptainPings()}, pick a faction");

                // otherwise, the faction prefs are the same, coin flip winner gets their faction, loser has to pick

                embed.Title = $"{state.GetCurrentTeam().Team.Tag}, pick a faction";
                embed.Description = "-# Faction preferences are the same, coin flip winner gets preference, other team picks from the other 2";

                DiscordComponent[] comps = [];
                foreach (string faction in state.GetAvailableFactions()) {
                    comps = comps.Append(FlipButtons.PICK_FACTION(faction, faction)).ToArray();
                }

                builder.AddComponents(comps);
            }

            builder.AddEmbed(embed);

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            string[] parts = args.Id.Split(".");
            if (parts.Length != 2) {
                throw new Exception($"expected 2 parts from {args.Id}");
            }

            DiscordMessageBuilder builder = new();
            DiscordEmbedBuilder embed = new();
            embed.Title = $"Factions picked";

            if (parts[1] == "Confirm") {
                embed.Description = $"";
            } else {
                state.SetFaction(state.GetCurrentTeamIndex(), parts[1]);

                embed.Description = $"<@{args.User.Id}> picked {state.GetCurrentTeam().Faction} for {state.GetCurrentTeam().Team.Tag}\n\n";
                if (state.GetCurrentTeam().Faction == "VS") {
                    embed.Color = DiscordColor.Purple;
                } else if (state.GetCurrentTeam().Faction == "NC") {
                    embed.Color = DiscordColor.Blue;
                } else if (state.GetCurrentTeam().Faction == "TR") {
                    embed.Color = DiscordColor.Red;
                }

                // remove swap team for now, the coin flip winner gets both faction and first ban
                //state.SwapTeam();
            }

            embed.Description += $"[{state.Team1.Team.Tag}] will be {state.Team1.Faction}\n";
            embed.Description += $"[{state.Team2.Team.Tag}] will be {state.Team2.Faction}";

            builder.AddEmbed(embed);

            return Task.FromResult(builder);
        }

    }
}
