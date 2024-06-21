using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.config;

namespace unoh {

    public class Match {

        private readonly ILogger<Match> _Logger;
        private readonly IOptions<MatchConfig> _Config;

        private readonly Dictionary<ulong, MatchState> _Matches = new();

        public Match(ILogger<Match> logger, IOptions<MatchConfig> config) {
            _Logger = logger;
            _Config = config;
        }

        public MatchConfig Get() {
            return _Config.Value;
        }

        public MatchState? GetState(ulong threadId) {
            return _Matches.GetValueOrDefault(threadId);
        }

        public MatchState Create(ulong threadId, TourneyTeam team1, TourneyTeam team2, MatchConfig config, MatchFormat format) {
            if (_Matches.ContainsKey(threadId)) {
                throw new Exception($"thread {threadId} already has a state");
            }

            MatchState state = new(team1, team2, config, format);

            _Matches.Add(threadId, state);

            return state;
        }

        public MatchFormat? GetFormat(string format) {
            return _Config.Value.Formats.FirstOrDefault(iter => iter.Name.ToLower() == format.ToLower());
        }

        public TourneyTeam? GetTeamOfUser(ulong userID) {
            return _Config.Value.Teams.FirstOrDefault(iter => iter.Captains.Contains(userID));
        }

        public TourneyTeam? GetTeamByTag(string tag) {
            return _Config.Value.Teams.FirstOrDefault(iter => iter.Tag.ToLower() == tag.ToLower());
        }

    }
}
