using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.config;
using unoh.discord;

namespace unoh.step {

    public class PickMapSideStep : IFlipStep {
        public string Name => "pick-side";

        public DiscordMessageBuilder Create(MatchState state) {
            DiscordMessageBuilder builder = new();

            DiscordEmbedBuilder embed = new();

            MatchBase? unpickedBase = state.GetUnsidedBase();
            if (unpickedBase == null) {
                embed.Title = $"no bases to pick?";
                builder.AddEmbed(embed);
                return builder;
            }

            TourneyBase? b = state.GetBase(unpickedBase.Base) ?? throw new Exception($"failed to find base {unpickedBase.Base}");
            builder.WithContent($"{state.GetCurrentTeamCaptainPings()}, pick a starting side for {unpickedBase.Base}");

            embed.Title = $"{state.GetCurrentTeam().Tag}, pick a starting side";
            embed.Description = $"**Base**: {unpickedBase.Base}\n**Sides**: {string.Join(", ", b.Sides)}";
            embed.Color = DiscordColor.CornflowerBlue;
            builder.AddEmbed(embed);

            List<string> pickedSides = [];
            if (unpickedBase.Team1Side != null) { pickedSides.Add(unpickedBase.Team1Side); }
            if (unpickedBase.Team2Side != null) { pickedSides.Add(unpickedBase.Team2Side); }

            List<string> availableSides = b.Sides.Where(iter => pickedSides.Contains(iter) == false).ToList();
            if (availableSides.Count == 0) {
                throw new Exception($"no sides left to pick");
            }

            builder.AddComponents(
                availableSides.Select(iter => {
                    return FlipButtons.PICK_SIZE(state.GetCurrentTeam().Team.Tag, iter, iter);
                })
            );

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            string[] parts = args.Id.Split(".");
            if (parts.Length != 3) {
                throw new Exception($"expected 3 parts from {args.Id}, got {parts.Length} instead");
            }

            string teamTag = parts[1];
            string side = parts[2];

            MatchBase unpickedBase = state.GetUnsidedBase() ?? throw new Exception($"expected an unsided base");
            TourneyBase b = state.GetBase(unpickedBase.Base) ??  throw new Exception($"failed to find base {unpickedBase.Base}");

            int index = state.GetCurrentTeamIndex();
            if (index == 0) {
                unpickedBase.Team1Side = side;
                unpickedBase.Team2Side = b.Sides.First(iter => iter != unpickedBase.Team1Side);
            } else if (index == 1) {
                unpickedBase.Team2Side = side;
                unpickedBase.Team1Side = b.Sides.First(iter => iter != unpickedBase.Team2Side);
            }

            DiscordMessageBuilder builder = new();
            DiscordEmbedBuilder embed = new();
            embed.Title = $"{state.GetCurrentTeam().Team.Tag} picks {side}";
            embed.Description = $"**Base**: {b.Name}\n";
            embed.Description += $"[{state.Team1.Tag}] {state.Team1.Team.Name} starts {unpickedBase.Team1Side ?? "_unpicked_"}\n";
            embed.Description += $"[{state.Team2.Tag}] {state.Team2.Team.Name} starts {unpickedBase.Team2Side ?? "_unpicked_"}";

            builder.AddEmbed(embed);

            return Task.FromResult(builder);
        }
    }
}
