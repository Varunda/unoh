using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.discord;

namespace unoh.step {

    public class CoinflipStep : IFlipStep {

        public string Name => "coinflip";

        public DiscordMessageBuilder Create(MatchState state) {
            DiscordMessageBuilder builder = new();

            state.SwapTeam();

            DiscordEmbedBuilder embed = new();
            embed.Title = "Flipping for first ban";
            embed.Description = $"Winner gets first map ban";
            builder.AddEmbed(embed);

            builder.WithContent($"{state.GetCurrentTeamCaptainPings()}, Heads or Tails?");
            builder.AddComponents(
                FlipButtons.COINFLIP_PICK(0, "Heads"),
                FlipButtons.COINFLIP_PICK(1, "Tails")
            );

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            string[] parts = args.Id.Split(".");
            if (parts.Length != 2) {
                throw new Exception($"failed to parse {args.Id} to a valid coinflip response");
            }

            int selectedTeamIndex = int.Parse(parts[1]);

            int teamIndex = Random.Shared.Next(0, 2);
            if (teamIndex == selectedTeamIndex) {
                state.SetTeam2();
                state.Team2.Faction = "TR";
                state.Team1.Faction = "VS";
            } else {
                state.SetTeam1();
                state.Team2.Faction = "VS";
                state.Team1.Faction = "TR";
            }

            DiscordMessageBuilder builder = new();

            string sidePicked = selectedTeamIndex == 0 ? "Heads" : "Tails";
            string side = teamIndex == 0 ? "Heads" : "Tails";
            string won = teamIndex == selectedTeamIndex ? "won" : "lost";

            DiscordEmbedBuilder embed = new();
            embed.Title = $"{state.Team2.Team.Tag} {won} the coin flip";
            embed.Description = $"{state.Team2.Team.Tag} picks {sidePicked}\nResult: **{side}**\n\n{state.Team2.Team.Tag} {won} the coin flip!\n\n";
            embed.Description += $"{state.Team1.Tag} will be on {state.Team1.Faction}\n";
            embed.Description += $"{state.Team2.Tag} will be on {state.Team2.Faction}\n";
            builder.AddEmbed(embed);

            return Task.FromResult(builder);
        }

    }
}
