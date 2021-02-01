using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PantherAPI
{
    public class GamePosition
    {
        public int Id { get; set; }
        public string PositionName { get; set; }
        public List<string> Aliases { get; set; } = new List<string>();

        public GamePosition() { }

        public GamePosition(string name)
        {
            PositionName = name;
        }
    }
}
