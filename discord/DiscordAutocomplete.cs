using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unoh.discord {

    public class DiscordFormatAutocomplete : IAutocompleteProvider {

        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) {
            Match match = ctx.Services.GetRequiredService<Match>();

            return Task.FromResult(match.Get().Formats.Select(iter => {
                return new DiscordAutoCompleteChoice(iter.Name, iter.Name);
            }));
        }
    }

    public class DiscordTeamAutocomplete : IAutocompleteProvider {

        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) {
            Match match = ctx.Services.GetRequiredService<Match>();

            IEnumerable<DiscordAutoCompleteChoice> teams = match.Get().Teams
                .Where(iter => iter.Tag.StartsWith(ctx.OptionValue.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
                .Select(iter => {
                return new DiscordAutoCompleteChoice(iter.Name, iter.Tag);
            });

            return Task.FromResult(teams);
        }
    }

}
