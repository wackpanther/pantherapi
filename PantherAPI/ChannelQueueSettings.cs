using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PantherAPI
{
    public class ChannelQueueSettings
    {
        public int Id { get; set; }
        public string Channel { get; set; }
        public int DefaultLimit { get; set; } = 5;

        public string DefaultGame { get; set; } = "Any";

        public List<GameSettings> Games { get; set; } = new List<GameSettings>();

        public string DefaultPosition { get; set; } = "Any";

        public ChannelQueueSettings(){ }

        public ChannelQueueSettings(string ChannelName)
        {
            Channel = ChannelName;
        }

    }
}
