using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.code.extension;
using unoh.config;
using unoh.step;

namespace unoh {

    public class MatchManager {

        private readonly ILogger<MatchManager> _Logger;
        private readonly Match _Match;
        private readonly MatchSteps _MatchSteps;

        public MatchManager(ILogger<MatchManager> logger,
            Match match, MatchSteps matchSteps) {

            _Logger = logger;

            _Match = match;
            _MatchSteps = matchSteps;
        }

        /// <summary>
        ///     create a new thread for the flip process, which will also perform the first step
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Create(ComponentInteractionCreateEventArgs ctx) {
            await ctx.Interaction.DeferAsync(true);

            string[] parts = ctx.Id.Split(".");
            if (parts.Length != 4 || parts[0] != "@accept") {
                await ctx.Interaction.EditResponseErrorEmbed($"bad component interaction: id is not length 4 or [0]@accept [id={ctx.Id}]");
                return;
            }

            DiscordMessage? msg = ctx.Message;
            if (msg == null) {
                await ctx.Interaction.EditResponseErrorEmbed($"failed to find message from context?");
                return;
            }

            string team1 = parts[1];
            string team2 = parts[2];
            string format = parts[3];

            TourneyTeam? t1 = _Match.GetTeamByTag(team1);
            if (t1 == null) {
                await ctx.Interaction.EditResponseErrorEmbed($"failed to find team1 {team1}");
                return;
            }

            TourneyTeam? t2 = _Match.GetTeamByTag(team2);
            if (t2 == null) {
                await ctx.Interaction.EditResponseErrorEmbed($"failed to find team2 {team2}");
                return;
            }

            MatchFormat? matchFormat = _Match.GetFormat(format);
            if (matchFormat == null) {
                await ctx.Interaction.EditResponseErrorEmbed($"failed to find format {format}");
                return;
            }

            foreach (DiscordActionRowComponent? compRow in msg.Components) {
                if (compRow == null) { continue; }

                foreach (DiscordComponent? comp in compRow.Components) {
                    if (comp == null) { continue; }

                    if (comp.Type == DSharpPlus.ComponentType.Button) {
                        ((DiscordButtonComponent)comp).Disable();
                    }
                }
            }
            await msg.ModifyAsync(new DiscordMessageBuilder(msg));

            DiscordThreadChannel thread = await msg.Channel.CreateThreadAsync(msg, $"Match {t1.Tag} / {t2.Tag}", DSharpPlus.AutoArchiveDuration.Day);
            MatchState state = _Match.Create(thread.Id, t1, t2, _Match.Get(), matchFormat);

            DiscordMessageBuilder builder = new();
            DiscordEmbedBuilder embed = new();
            embed.Title = $"Flip started!";

            embed.Description = $"Team 1: {t1.Tag}\n";
            embed.Description += $"- {string.Join(", ", t1.Captains.Select(iter => $"<@{iter}>"))}\n";
            embed.Description += $"Team 2: {t2.Tag}\n";
            embed.Description += $"- {string.Join(", ", t2.Captains.Select(iter => $"<@{iter}>"))}\n";

            embed.Description += $"Format: **{matchFormat.Name}**\n";
            foreach (string step in matchFormat.Steps) {
                embed.Description += $"- {step}\n";
            }

            foreach (ulong captainId in t1.Captains) {
                builder.WithAllowedMention(new UserMention(captainId));
                builder.AddMention(new UserMention(captainId));
            }
            foreach (ulong captainId in t2.Captains) {
                builder.WithAllowedMention(new UserMention(captainId));
                builder.AddMention(new UserMention(captainId));
            }

            builder.AddEmbed(embed);
            await thread.SendMessageAsync(builder);

            await SendStepInThread(state, thread);

            await ctx.Interaction.DeleteOriginalResponseAsync();
        }

        /// <summary>
        ///     handle an interaction, which will advance the flip steps to the next stage
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>
        ///     true if the the match state was successfully advanced to the next step,
        ///     or <c>false</c> if it failed for some reason
        /// </returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> PerformInteraction(ComponentInteractionCreateEventArgs ctx) {
            await ctx.Interaction.DeferAsync(true);
            string[] parts = ctx.Id.Split(".");

            string cmd = parts[0];
            if (ctx.Channel.IsThread == false) {
                _Logger.LogError($"not in a thread");
                return false;
            }

            ulong threadId = ctx.Channel.Id;

            MatchState? state = _Match.GetState(threadId);
            if (state == null) {
                _Logger.LogError($"missing match state {threadId}");
                return false;
            }

            bool correctUser = state.GetCurrentTeam().Team.Captains.Contains(ctx.User.Id);
            if (correctUser == false) {
                await ctx.Interaction.EditResponseErrorEmbed($"It is not your turn! {state.GetCurrentTeamCaptainPings()} can use this");
                return false;
            }

            string stepName = state.GetStepName();
            IFlipStep step = _MatchSteps.GetStep(stepName) ?? throw new Exception($"failed to find step {stepName}");

            DiscordMessageBuilder response = await step.Update(state, ctx);
            List<IMention> mentions = [];
            foreach (ulong captainId in state.Team1.Team.Captains) {
                mentions.Add(new UserMention(captainId));
            }
            foreach (ulong captainId in state.Team2.Team.Captains) {
                mentions.Add(new UserMention(captainId));
            }

            response.WithAllowedMentions(mentions);
            await ctx.Channel.SendMessageAsync(response);

            if (state.NextStep() == false) {
                _Logger.LogInformation($"state setup is done!");
            } else {
                await SendStepInThread(state, ctx.Channel);
            }

            await ctx.Interaction.DeleteOriginalResponseAsync();

            return true;
        }

        private async Task SendStepInThread(MatchState state, DiscordChannel channel) {
            string stepName = state.GetStepName();

            IFlipStep? step = _MatchSteps.GetStep(stepName);
            if (step == null) {
                _Logger.LogWarning($"failed to find step [stepName={stepName}]");
                return;
            }

            _Logger.LogDebug($"sending next step setup [step.Name={step.Name}]");
            DiscordMessageBuilder builder = step.Create(state);

            if (builder.Components.Count == 0) {
                _Logger.LogWarning($"no components returned from step, cannot continue! [step.Name={step.Name}]");
            }

            List<IMention> mentions = [];
            foreach (ulong captainId in state.Team1.Team.Captains) {
                mentions.Add(new UserMention(captainId));
            }
            foreach (ulong captainId in state.Team2.Team.Captains) {
                mentions.Add(new UserMention(captainId));
            }

            builder.WithAllowedMentions(mentions);
            await channel.SendMessageAsync(builder);
        }

    }
}
