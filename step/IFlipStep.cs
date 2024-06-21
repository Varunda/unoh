using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unoh.step {

    public interface IFlipStep {

        public string Name { get; }

        public DiscordMessageBuilder Create(MatchState state);

        public Task<DiscordMessageBuilder> Update(MatchState state, ComponentInteractionCreateEventArgs args);

    }
}
