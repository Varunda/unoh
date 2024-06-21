using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unoh.config;
using unoh.step;

namespace unoh {

    public class MatchState {

        public MatchTeam Team1 { get; }

        public MatchTeam Team2 { get; }

        public MatchConfig Config { get; set; }

        public MatchFormat Format { get; set; }

        private List<string> _BasesLeft { get; set; } = [];

        private List<string> _FactionsLeft { get; set; } = [];

        private List<MatchBase> _SelectedBases { get; set; } = [];

        private int _CurrentTeam = 0;
        private int _StepIndex = 0;

        public MatchState(TourneyTeam team1, TourneyTeam team2,
            MatchConfig config, MatchFormat format) {

            Team1 = new MatchTeam(team1);
            Team2 = new MatchTeam(team2);
            Config = config;
            Format = format;

            _BasesLeft = new List<string>(Config.Bases.Select(iter => iter.Name));
            _FactionsLeft = new List<string>(Config.Factions);
        }

        public string GetStepName() => Format.Steps[_StepIndex];

        public bool NextStep() {
            if (_StepIndex == Format.Steps.Count - 1) {
                return false;
            }

            ++_StepIndex;

            return true;
        }

        public List<MatchBase> GetPickedBases() => new List<MatchBase>(_SelectedBases);

        public MatchBase? GetUnsidedBase() {
            return _SelectedBases.FirstOrDefault(iter => iter.Team1Side == null || iter.Team2Side == null);
        }

        public MatchTeam GetCurrentTeam() => (_CurrentTeam == 0) ? Team1 : Team2;

        public int GetCurrentTeamIndex() => _CurrentTeam;

        public void SetTeam1() { _CurrentTeam = 0; }
        public void SetTeam2() { _CurrentTeam = 1; }

        public void SwapTeam() {
            if (_CurrentTeam == 0) {
                SetTeam2();
            } else {
                SetTeam1();
            }
        }

        public List<string> GetUnbannedBases() {
            return new List<string>(_BasesLeft);
        }

        public void RemoveBase(string name) {
            _BasesLeft.Remove(name);
        }
        
        public void AddBase(string name) {
            if (_BasesLeft.Contains(name) == false) {
                throw new Exception($"cannot add base, base is not left");
            }

            _SelectedBases.Add(new MatchBase() {
                Base = name
            });
            _BasesLeft.Remove(name);
        }

        public string GetCurrentTeamCaptainPings() {
            return string.Join(", ", GetCurrentTeam().Team.Captains.Select(iter => $"<@{iter}>"));
        }

        public List<string> GetAvailableFactions() {
            return new List<string>(_FactionsLeft);
        }

        public TourneyBase? GetBase(string name) {
            return Config.Bases.FirstOrDefault(iter => iter.Name.ToLower().Trim() == name.ToLower().Trim());
        }

        public void SetFaction(int teamIndex, string faction) {
            if (teamIndex == 0) {
                if (Team1.Faction != null) {
                    throw new Exception($"Team1 already had faction set");
                }

                Team1.Faction = faction;
            } else {
                if (Team2.Faction != null) {
                    throw new Exception($"Team2 already had faction set");
                }

                Team2.Faction = faction;
            }

            _FactionsLeft.Remove(faction);
        }

    }

    public class MatchTeam {

        public TourneyTeam Team { get; }

        public string? Faction { get; set; }

        public MatchTeam(TourneyTeam team) {
            Team = team;
        }

        public string Tag { get { return Team.Tag; } }

    }

    public class MatchBase {

        public string Base { get; set; } = "";

        public string? Team1Side { get; set; }

        public string? Team2Side { get; set; }

    }

}
