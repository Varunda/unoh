using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.code.extension;
using unoh.config;

namespace unoh.discord {

    public class FlipCommand : ApplicationCommandModule {

        public ILogger<FlipCommand> _Logger { private get; set; } = default!;
        public Match _Match { private get; set; } = default!;

        /// <summary>
        ///     start a flip
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        [SlashCommand("flip", "do a match flip")]
        public async Task Flip(InteractionContext ctx,
            [Option("Team", "team tag ")] string tag) {

            TourneyTeam? sourceTeam = _Match.GetTeamOfUser(ctx.User.Id);
            if (sourceTeam == null) {
                await ctx.CreateImmediateText($"You are not a team captain! Use `/teams` to list all teams and captains", ephemeral: true);
                return;
            }

            TourneyTeam? team = _Match.GetTeamByTag(tag);
            if (team == null) {
                await ctx.CreateImmediateText($"failed to find team '{tag}'. Use `/teams` to list all teams", ephemeral: true);
                return;
            }

            DiscordEmbedBuilder builder = new();
            builder.Title = $"Match: {sourceTeam.Tag} / {team.Tag}";
            builder.WithAuthor($"{ctx.User.Username}");
            builder.Description = $"Flipping for match between {sourceTeam.Tag} and {team.Tag}\n\n";

            DiscordInteractionResponseBuilder response = new();
            response.WithContent($"{string.Join(", ", team.Captains.Select(iter => $"<@{iter}>"))}, press to accept");
            foreach (ulong captainId in team.Captains) {
                response.AddMention(new UserMention(captainId));
            }

            response.AddEmbed(builder);
            response.AddComponents(FlipButtons.ACCEPT(sourceTeam.Tag, team.Tag));

            await ctx.CreateResponseAsync(response);
        }

        /// <summary>
        ///     list all configured teams
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [SlashCommand("teams", "list teams")]
        public async Task Teams(InteractionContext ctx) {

            DiscordEmbedBuilder builder = new();
            builder.Title = "Teams";

            builder.Description = "";
            foreach (TourneyTeam team in _Match.Get().Teams) {
                builder.Description += $"[{team.Tag}] {team.Name}\n";
                builder.Description += $"- {string.Join(", ", team.Captains.Select(iter => $"<@{iter}>"))}\n\n";
            }

            await ctx.CreateResponseAsync(builder, ephemeral: true);
        }

    }
}
