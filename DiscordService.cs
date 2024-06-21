using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.ButtonCommands.EventArgs;
using DSharpPlus.ButtonCommands.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.code.extension;
using unoh.config;
using unoh.discord;

namespace unoh {

    public class DiscordService : BackgroundService {

        private readonly ILogger<DiscordService> _Logger;

        private readonly MatchManager _MatchManager;

        private readonly DiscordWrapper _Discord;
        private readonly SlashCommandsExtension _SlashCommands;
        private IOptions<DiscordOptions> _DiscordOptions;

        private bool _IsConnected = false;
        private const string SERVICE_NAME = "discord";

        private Dictionary<ulong, ulong> _CachedMembership = new();

        public DiscordService(ILogger<DiscordService> logger, ILoggerFactory loggerFactory,
            IOptions<DiscordOptions> discordOptions, IServiceProvider services,
            DiscordWrapper discord, MatchManager matchManager) {

            _Logger = logger;

            _DiscordOptions = discordOptions;

            if (_DiscordOptions.Value.GuildId == 0) {
                throw new ArgumentException($"GuildId is 0, must be set. Try running dotnet user-secrets set Discord:GuildId $VALUE");
            }

            if (_DiscordOptions.Value.ChannelId == 0) {
                throw new ArgumentException($"ChannelId is 0, must be set. Try running dotnet user-secrets set Discord:ChannelId $VALUE");
            }

            _Discord = discord;

            _Discord.Get().Ready += Client_Ready;
            _Discord.Get().InteractionCreated += Generic_Interaction_Created;
            _Discord.Get().ContextMenuInteractionCreated += Generic_Interaction_Created;
            _Discord.Get().GuildAvailable += Guild_Available;
            _Discord.Get().ComponentInteractionCreated += Component_Interact;

            _SlashCommands = _Discord.Get().UseSlashCommands(new SlashCommandsConfiguration() {
                Services = services
            });
            _SlashCommands.SlashCommandErrored += Slash_Command_Errored;

            // these commands are global when ran in live, but to test them locally
            //      they are setup in the home server as well (quicker to update)
            if (_DiscordOptions.Value.RegisterGlobalCommands == true) {
                _Logger.LogDebug($"registing commands globally");
                _SlashCommands.RegisterCommands<FlipCommand>();
            } else {
                _Logger.LogDebug($"registing commands in home server [GuildId={_DiscordOptions.Value.GuildId}]");
                _SlashCommands.RegisterCommands<FlipCommand>(_DiscordOptions.Value.GuildId);
            }
            _MatchManager = matchManager;
        }

        public async override Task StartAsync(CancellationToken cancellationToken) {
            try {
                await _Discord.Get().ConnectAsync();

                IReadOnlyList<DiscordApplicationCommand> cmds = await _Discord.Get().GetGuildApplicationCommandsAsync(_DiscordOptions.Value.GuildId);
                _Logger.LogDebug($"Have {cmds.Count} commands");
                foreach (DiscordApplicationCommand cmd in cmds) {
                    _Logger.LogDebug($"{cmd.Id} {cmd.Name}: {cmd.Description}");
                }

                IReadOnlyList<DiscordApplicationCommand> globalCmds = await _Discord.Get().GetGlobalApplicationCommandsAsync();
                foreach (DiscordApplicationCommand cmd in globalCmds) {
                    await _Discord.Get().DeleteGlobalApplicationCommandAsync(cmd.Id);
                }

                await base.StartAsync(cancellationToken);
            } catch (Exception ex) {
                _Logger.LogError(ex, "Error in start up of DiscordService");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"started {SERVICE_NAME}");

            while (stoppingToken.IsCancellationRequested == false) {
                try {
                    await Task.Delay(1000, stoppingToken);
                    if (_IsConnected == false) {
                        continue;
                    }
                } catch (Exception ex) when (stoppingToken.IsCancellationRequested == false) {
                    _Logger.LogError(ex, "error sending message");
                } catch (Exception) when (stoppingToken.IsCancellationRequested == true) {
                    _Logger.LogInformation($"Stopping {SERVICE_NAME}");
                }
            }
        }

        /// <summary>
        ///     Get a <see cref="DiscordMember"/> from an ID
        /// </summary>
        /// <param name="memberID">ID of the Discord member to get</param>
        /// <returns>
        ///     The <see cref="DiscordMember"/> with the corresponding ID, or <c>null</c>
        ///     if the user could not be found in any guild the bot is a part of
        /// </returns>
        private async Task<DiscordMember?> GetDiscordMember(ulong memberID) {
            // check if cached
            if (_CachedMembership.TryGetValue(memberID, out ulong guildID) == true) {
                DiscordGuild? guild = await _Discord.Get().TryGetGuild(guildID);
                if (guild == null) {
                    _Logger.LogWarning($"Failed to get guild {guildID} from cached membership for member {memberID}");
                } else {
                    DiscordMember? member = await guild.TryGetMember(memberID);
                    // if the member is null, and was cached, then cache is bad
                    if (member == null) {
                        _Logger.LogWarning($"Failed to get member {memberID} from guild {guildID}");
                        _CachedMembership.Remove(memberID);
                    } else {
                        _Logger.LogDebug($"Found member {memberID} from guild {guildID} (cached)");
                        return member;
                    }
                }
            }

            // check each guild and see if it contains the target member
            foreach (KeyValuePair<ulong, DiscordGuild> entry in _Discord.Get().Guilds) {
                DiscordMember? member = await entry.Value.TryGetMember(memberID);

                if (member != null) {
                    _Logger.LogDebug($"Found member {memberID} from guild {entry.Value.Id}");
                    _CachedMembership[memberID] = entry.Value.Id;
                    return member;
                }
            }

            _Logger.LogWarning($"Cannot get member {memberID}, not cached and not in any guilds");

            return null;
        }

        /// <summary>
        ///     Event handler for when the client is ready
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task Client_Ready(DiscordClient sender, ReadyEventArgs args) {
            _Logger.LogInformation($"Discord client connected");

            _IsConnected = true;

            DiscordGuild? guild = await sender.GetGuildAsync(_DiscordOptions.Value.GuildId);
            if (guild == null) {
                _Logger.LogError($"Failed to get guild {_DiscordOptions.Value.GuildId} (what was passed in the options)");
            } else {
                _Logger.LogInformation($"Successfully found home guild '{guild.Name}'/{guild.Id}");
            }

            DiscordChannel? channel = await sender.GetChannelAsync(_DiscordOptions.Value.ChannelId);
            if (channel == null) {
                _Logger.LogWarning($"Failed to find channel {_DiscordOptions.Value.ChannelId}");
            }
        }

        private Task Guild_Available(DiscordClient sender, GuildCreateEventArgs args) {
            DiscordGuild? guild = args.Guild;
            if (guild == null) {
                _Logger.LogDebug($"no guild");
                return Task.CompletedTask;
            }

            _Logger.LogDebug($"guild available: {guild.Id} / {guild.Name}");
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Event handler for both types of interaction (slash commands and context menu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Task Generic_Interaction_Created(DiscordClient sender, InteractionCreateEventArgs args) {
            DiscordInteraction interaction = args.Interaction;
            string user = interaction.User.GetDisplay();

            string interactionMethod = "slash";

            DiscordUser? targetMember = null;
            DiscordMessage? targetMessage = null;

            if (args is ContextMenuInteractionCreateEventArgs contextArgs) {
                targetMember = contextArgs.TargetUser;
                targetMessage = contextArgs.TargetMessage;
                interactionMethod = "context menu";
            }

            string feedback = $"{user} used '{interaction.Data.Name}' (a {interaction.Type}) as a {interactionMethod}: ";

            if (targetMember != null) {
                feedback += $"[target member: (user) {targetMember.GetDisplay()}]";
            }
            if (targetMessage != null) {
                feedback += $"[target message: (channel) {targetMessage.Id}] [author: (user) {targetMessage.Author.GetDisplay()}]";
            }

            if (targetMessage == null && targetMember == null) {
                feedback += $"{interaction.Data.Name} {GetCommandString(interaction.Data.Options)}";
            }

            _Logger.LogDebug(feedback);

            return Task.CompletedTask;
        }

        private async Task Component_Interact(DiscordClient sender, ComponentInteractionCreateEventArgs args) {
            _Logger.LogDebug($"interaction with component [user={args.User.GetDisplay()}] [type={args.Interaction.Type}] "
                + $"[id={args.Id}] [guild={args.Guild?.Id}] [channel={args.Channel?.Id}]");

            string id = args.Id;
            string[] parts = id.Split(".");

            if (parts.Length <= 0) {
                await args.Interaction.CreateImmediateText($"no args? failed to parse {args.Id}");
                return;
            }

            string cmd = parts[0];

            try {
                if (cmd == "@accept") {
                    await _MatchManager.Create(args);
                } else {
                    // it's possible to have an interaction that is not valid,
                    // for example, the captain of one team isn't allowed to press buttons when its
                    // the other teams turn, so we need a way to prevent the buttons from being disabled
                    bool disableButtons = await _MatchManager.PerformInteraction(args);

                    // disable the components of the message acted on
                    if (disableButtons == true && args.Message.Components.Count > 0) {
                        DiscordMessage msg = args.Message;
                        try {
                            foreach (DiscordActionRowComponent? compRow in msg.Components) {
                                if (compRow == null) { continue; }

                                foreach (DiscordComponent? comp in compRow.Components) {
                                    if (comp == null) { continue; }

                                    if (comp.Type == ComponentType.Button) {
                                        ((DiscordButtonComponent)comp).Disable();
                                    } else if (comp.Type == ComponentType.StringSelect) {
                                        ((DiscordSelectComponent)comp).Disable();
                                    } else {
                                        _Logger.LogWarning($"unchecked component type [comp.Type={comp.Type}]");
                                    }
                                }
                            }
                            _Logger.LogDebug($"disabling components of message acted on [msg.Id={msg.Id}]");
                            await msg.ModifyAsync(new DiscordMessageBuilder(msg));
                        } catch (Exception ex) {
                            if (ex is BadRequestException rex) {
                                _Logger.LogError(rex, $"400 bad request disabling buttons: {rex.Errors}");
                            } else {
                                _Logger.LogError(ex, $"failed to updated message");
                            }
                        }
                    } else {
                        _Logger.LogTrace($"previous message has no components [msg.Id={args.Message.Id}]");
                    }

                }
            } catch (BadRequestException ex) {
                _Logger.LogError(ex, $"400 bad request handling {id}: {ex.Errors}");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to handle {id}");
            }
        }

        /// <summary>
        ///     Event handler for when an exception is thrown during the execution of a button command
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task Button_Command_Error(ButtonCommandsExtension ext, ButtonCommandErrorEventArgs args) {
            _Logger.LogError($"Error executing button command {args.CommandName}: {args.Exception} :: {args.Exception.InnerException?.Message}");

            try {
                // if the response has already started, this won't be null, indicating to instead update the response
                DiscordMessage? msg = await args.Context.Interaction.GetOriginalResponseAsync();

                string error = $"Error executing button command `{args.CommandName}`: {args.Exception.GetType().Name} - {args.Exception.Message}";
                if (args.Exception.InnerException != null) {
                    error += $". Caused by: {args.Exception.InnerException.GetType().Name} - {args.Exception.InnerException.Message}";
                }

                if (msg == null) {
                    // if it is null, then no respons has been started, so one is created
                    // if you attempt to create a response for one that already exists, then a 400 is thrown
                    await args.Context.Interaction.CreateImmediateText(error, true);
                } else {
                    await args.Context.Interaction.EditResponseText(error);
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error updating interaction response with error");
            }
        }

        /// <summary>
        ///     Event handler for when a slash command fails
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task Slash_Command_Errored(SlashCommandsExtension ext, SlashCommandErrorEventArgs args) {
            if (args.Exception is SlashExecutionChecksFailedException failedCheck) {
                string feedback = "Check failed:\n";

                foreach (SlashCheckBaseAttribute check in failedCheck.FailedChecks) {
                    feedback += $"Unchecked check type: {check.GetType()}";
                    _Logger.LogError($"Unchecked check type: {check.GetType()}");
                }

                await args.Context.CreateImmediateText(feedback, true);

                return;
            }

            _Logger.LogError(args.Exception, $"error executing slash command: {args.Context.CommandName}");

            if (args.Exception is BadRequestException badRequest) {
                _Logger.LogError($"errors in request [url={badRequest.WebRequest.Url}] [errors={badRequest.Errors}]");
            }

            try {
                // if the response has already started, this won't be null, indicating to instead update the response
                DiscordMessage? msg = null;
                try {
                    msg = await args.Context.GetOriginalResponseAsync();
                } catch (NotFoundException) {
                    msg = null;
                }

                if (msg == null) {
                    // if it is null, then no respons has been started, so one is created
                    // if you attempt to create a response for one that already exists, then a 400 is thrown
                    await args.Context.CreateImmediateText($"Error executing slash command: {args.Exception.Message}", true);
                } else {
                    await args.Context.EditResponseText($"Error executing slash command: {args.Exception.Message}");
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error sending error message to Discord");
            }
        }

        /// <summary>
        ///     Transform the options used in an interaction into a string that can be viewed
        /// </summary>
        /// <param name="options"></param>
        private string GetCommandString(IEnumerable<DiscordInteractionDataOption>? options) {
            if (options == null) {
                options = new List<DiscordInteractionDataOption>();
            }

            string s = "";

            foreach (DiscordInteractionDataOption opt in options) {
                s += $"[{opt.Name}=";

                if (opt.Type == ApplicationCommandOptionType.Attachment) {
                    s += $"(Attachment)";
                } else if (opt.Type == ApplicationCommandOptionType.Boolean) {
                    s += $"(bool) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Channel) {
                    s += $"(channel) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Integer) {
                    s += $"(int) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Mentionable) {
                    s += $"(mentionable) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Number) {
                    s += $"(number) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Role) {
                    s += $"(role) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.String) {
                    s += $"(string) '{opt.Value}'";
                } else if (opt.Type == ApplicationCommandOptionType.SubCommand) {
                    s += GetCommandString(opt.Options);
                } else if (opt.Type == ApplicationCommandOptionType.SubCommandGroup) {
                    s += GetCommandString(opt.Options);
                } else if (opt.Type == ApplicationCommandOptionType.User) {
                    s += $"(user) {opt.Value}";
                } else {
                    _Logger.LogError($"Unchecked {nameof(DiscordInteractionDataOption)}.{nameof(DiscordInteractionDataOption.Type)}: {opt.Type}, value={opt.Value}");
                    s += $"[{opt.Name}=(UNKNOWN {opt.Type}) {opt.Value}]";
                }

                s += "]";
            }

            return s;
        }

    }
}
