using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.discord;

namespace unoh.step {

    public class RandomMapStep : IFlipStep {

        public string Name => "random-map";

        public DiscordMessageBuilder Create(MatchState state) {
            DiscordMessageBuilder builder = new();

            DiscordEmbedBuilder embed = new();
            embed.Title = $"Selecting a random map";

            embed.Description = "";
            List<string> bases = state.GetUnbannedBases();
            for (int i = 0; i < bases.Count; ++i) {
                string b = bases[i];

                embed.Description += $"[{i + 1}] {b}\n";
            }
            embed.Description += "\n";

            int selectedBaseIndex = Random.Shared.Next(0, bases.Count);
            string selectedBase = bases[selectedBaseIndex];

            embed.Description += $"Rolled a {selectedBaseIndex + 1}\n";
            embed.Description += $"Result: [{selectedBaseIndex + 1}] **{selectedBase}**\n";

            state.AddBase(selectedBase);

            builder.AddEmbed(embed);

            builder.AddComponents(FlipButtons.NEXT_STEP());

            return builder;
        }

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args) {
            DiscordMessageBuilder builder = new();

            DiscordEmbedBuilder embed = new();
            embed.Title = $"Map selected";
            embed.Description = $"{state.GetPickedBases().Last().Base} picked";

            builder.AddEmbed(embed);

            state.SwapTeam();

            return Task.FromResult(builder);
        }

    }
}
