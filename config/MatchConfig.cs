using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unoh.config {

    public class MatchConfig {

        public List<TourneyBase> Bases { get; set; } = [];

        public List<string> Factions { get; set; } = [];

        public ulong StaffRoleId { get; set; } = 0;

        public List<TourneyTeam> Teams { get; set; } = [];

        public List<MatchFormat> Formats { get; set; } = [];

    }

    public class TourneyBase {

        public string Name { get; set; } = "";

        public List<string> Sides { get; set; } = [];

    }

    public class TourneyTeam {

        public string Name { get; set; } = "";

        public string Tag { get; set; } = "";

        public List<ulong> Captains { get; set; } = [];

    }

    public class MatchFormat {

        public string Name { get; set; } = "";

        public List<string> Steps { get; set; } = [];

    }

}
