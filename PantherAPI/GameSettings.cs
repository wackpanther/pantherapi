using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PantherAPI
{
    public class GameSettings
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DefaultSquadSize { get; set; } = 5;
        public List<GamePosition> Positions { get; set; } = new List<GamePosition>();
        public bool RequireInGameName { get; set; } = true;
        public bool RequireApprovedAlias { get; set; } = false;
    }
}
