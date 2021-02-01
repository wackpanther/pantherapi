using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static PantherAPI.Utils;

namespace PantherAPI.Controllers
{
    /// <summary>
    /// A basic queue system for channels to operate different concurrent queues for games with their own settings for roles, party size, and in-game name information
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class QueueController : Controller
    {
        string Channel;
        string RequestUser;
        ChannelQueueSettings settings;

        public QueueController()
        {
        }

        private void Setup()
        {
            Channel = GetChannel(Request.Headers);
            RequestUser = GetUser(Request.Headers);
            using (var db = new LiteDatabase(@"/pantherttv/channelsettings.db"))
            {
                var coll = db.GetCollection<ChannelQueueSettings>("queuesettings");
                if (coll.FindOne(u => u.Channel == Channel) is ChannelQueueSettings s)
                {
                    settings = s;
                }
                else
                {
                    settings = new ChannelQueueSettings(Channel);
                    coll.Insert(settings);
                }
            }
        }

        /// <summary>
        /// Looks up the next <paramref name="Limit"/> of users in queue sorted by priority and time queued 
        /// </summary>
        /// <param name="Position">(optional) The position the player plays/prefers</param>
        /// <param name="Game">(optional) The game (queue) to line up to play</param>
        /// <param name="Limit">(optional) How many players to return</param>
        /// <param name="Remove">(optional) A flag that tells the system to remove the players from queue after looking them up</param>
        /// <returns>A <see cref="JsonResult"/> containing a list of <see cref="QueuedUser"/> in the order they should be played</returns>
        [HttpGet]
        public JsonResult NextInQueue([FromQuery] string Position = "Any", [FromQuery] string Game = "Any", [FromQuery] int Limit = 5, [FromQuery] bool Remove = false)
        {
            Setup();
            List<QueuedUser> queue = GetQueue(Channel, Game, Limit, Remove);

            return Json(queue);
        }

        /// <summary>
        /// Returns the position of a user in queue
        /// </summary>
        /// <param name="User">(optional) The username of the user to check, if not provided, the user that called the command is used instead</param>
        /// <param name="Game">(optional) The game/queue to check, defaults to the default game for the channel</param>
        /// <returns><see cref="string"/> describing the user's position in queue</returns>
        [HttpGet]
        [Route("UserPosition")]
        public string UserPosition([FromQuery] string User = null, [FromQuery] string Game = "Any")
        {
            Setup();
            User ??= RequestUser;
            return string.Format("{0} is currently {1} in queue", User, GetUserPositionInQueue(User, Channel, Game).Stringify("next"));
        }

        /// <summary>
        /// Adds a user to the queue
        /// </summary>
        /// <param name="InGameName">The in-game name for the user (e.g, summoner name for League). May be optional depending on game settings for the channel</param>
        /// <param name="Game">The name of the game/queue to queue for, will default to default queue for the channel if not provided</param>
        /// <param name="Position">The preferred position the user would like to play (e.g., "Jungle" for League)</param>
        /// <param name="Priority">Defaults to 10, but can be set lower/higher and will override queue times if set lower, down to 0</param>
        /// <returns><see cref="string"/> indicating whether the queue attempt was successful</returns>
        [HttpGet]
        [Route("Add")]
        public string QueueUser([FromQuery] string InGameName = null, [FromQuery] string Game = "Any", [FromQuery] string Position = "Any", [FromQuery] int Priority = 10)
        {
            Setup();
            string User = RequestUser;
            
            if (Game == "Any")
            {
                Game = settings.DefaultGame ?? "Any";
            }

            bool RequireIGN = (settings.Games.FirstOrDefault(g => g.Name == Game) is GameSettings g && g.RequireInGameName == true && string.IsNullOrEmpty(InGameName));

            using (var db = new LiteDatabase(@"/pantherttv/gamequeue.db"))
            {
                var GameQueue = db.GetCollection<QueuedUser>(string.Format("gamequeue_{0}", Channel));
                if (GameQueue.FindOne(u => u.Game == Game && u.Username == User) is QueuedUser qu)
                {
                    if (RequireIGN && string.IsNullOrEmpty(qu.InGameName))
                    {
                        return string.Format("The queue now requires an in-game name, {0}. Try the command again, but include your in-game name.", User);
                    }

                    if (qu.Skipped == true || qu.LastPlayed != null || qu.Removed == true)
                    {
                        qu.Skipped = false;
                        qu.Removed = false;
                        qu.TimeQueued = DateTime.Now;
                        qu.InGameName ??= InGameName;
                        GameQueue.Update(qu);
                        return string.Format("{0} is now in the {1} queue!",qu.Username, qu.Game == "Any" ? "game" : qu.Game);
                    }
                    else
                    {
                        qu.Priority = Priority;
                        qu.Position = Position;
                        qu.InGameName = InGameName?? qu.InGameName;
                        GameQueue.Update(qu);
                        return string.Format("{0} is already in queue", qu.Username);
                    }
                }
                else
                {
                    if (RequireIGN)
                    {
                        return string.Format("The new queue requires an in-game name the first time you queue up, {0}. Try the command again, but include your in-game name.",User);
                    }
                    QueuedUser newUser = new QueuedUser(User, Game, Position, InGameName);
                    GameQueue.Insert(newUser);
                    GameQueue.EnsureIndex(u => u.Username);
                    return string.Format("{0} is now in the {1} queue!",newUser.Username, newUser.Game == "Any" ? "game" : newUser.Game);
                }
            }
        }

        /// <summary>
        /// Removes the user from a queue
        /// </summary>
        /// <param name="User">The username to de-queue, defaults to the calling user if not provided</param>
        /// <param name="Game">The queue/game to remove the user from</param>
        /// <param name="Skip">If true, the user will simply be skipped instead of removed (experimental in v.1)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Remove")]
        public string DeQueueUser([FromQuery] string User = null, [FromQuery] string Game = "Any", [FromQuery] bool Skip = false)
        {
            Setup();
            User ??= RequestUser;
            using (var db = new LiteDatabase(@"/pantherttv/gamequeue.db"))
            {
                var qcollection = db.GetCollection<QueuedUser>(string.Format("gamequeue_{0}", Channel));
                if(qcollection.FindOne(u => u.Username == User && u.Game == Game) is QueuedUser user)
                {
                    user.Removed = true;
                    user.LastPlayed = DateTime.Now;
                    qcollection.Update(user);
                    return string.Format("Successfully removed {0} from the {1}queue", User, Game == "Any" ? "" : string.Concat(Game," "));
                }
                else
                {
                    return string.Format("{0} wasn't found in the {1}queue!", User, Game == "Any" ? "" : string.Concat(Game, " "));
                }
            }
        }

        /// <summary>
        /// Clears the entire queue
        /// </summary>
        /// <param name="Game">The queue/game to clear</param>
        /// <returns>A message indicating whether the attempt was successful</returns>
        [HttpGet]
        [Route("Clear")]
        public string ClearQueue([FromQuery] string Game = "Any")
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/gamequeue.db"))
            {
                var qcollection = db.GetCollection<QueuedUser>(string.Format("gamequeue_{0}", Channel));
                foreach(QueuedUser u in qcollection.Find(u => u.Removed != true && (Game == "Any" || u.Game == Game)))
                {
                    u.Skipped = false;
                    u.Removed = true;
                    u.LastPlayed = DateTime.Now;
                    u.Priority = 10;
                    qcollection.Update(u);
                }
            }
            return string.Format("The {0}queue has been cleared!", Game == "Any" ? "" : string.Concat(Game, " "));
        }

        /// <summary>
        /// Admin feature- allows a channel to setup a new game/queue in advance
        /// </summary>
        /// <param name="Name">The name of the game, which should also be used when queueing</param>
        /// <param name="QueueSize">How many players to return at a time by default (can be overriden at runtime)</param>
        /// <param name="RequireInGameName">Whether a user needs to provide an in-game name for this game</param>
        /// <param name="RequireAlias">Whether a user needs to provide a position or alias when queuing</param>
        /// <returns>A message indicating whether the attempt was successful</returns>
        [HttpGet]
        [Route("SetupGame")]
        public string AddGame([FromQuery] string Name, [FromQuery] int QueueSize = 5, [FromQuery] bool RequireInGameName = true, [FromQuery] bool RequireAlias = false)
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/channelsettings.db"))
            {
                var coll = db.GetCollection<ChannelQueueSettings>("queuesettings");
                bool Update = false;
                if (settings.Games.FirstOrDefault(g => g.Name == Name) is GameSettings eGame)
                {
                    eGame.DefaultSquadSize = QueueSize;
                    eGame.RequireInGameName = RequireInGameName;
                    eGame.RequireApprovedAlias = RequireAlias;
                    Update = true;
                }
                else
                {
                    GameSettings newGame = new GameSettings();

                    newGame.Name = Name;
                    newGame.DefaultSquadSize = QueueSize;
                    newGame.RequireInGameName = RequireInGameName;
                    newGame.RequireApprovedAlias = RequireAlias;

                    settings.Games.Add(newGame);
                }

                if(settings.Games.Count == 1 && settings.DefaultGame == "Any")
                {
                    settings.DefaultGame = Name;
                }

                coll.Update(settings);

                return string.Format("{0} was {1} your list of games!", Name, Update ? "updated; it was already in": "added to");
            }
        }

        /// <summary>
        /// Admin feature - Adds a position to a game already setup for the channel
        /// </summary>
        /// <param name="Game">The name of the game/queue to update</param>
        /// <param name="Position">The name of the position to add</param>
        /// <returns>A message indicating whether the attempt was successful</returns>
        [HttpGet]
        [Route("AddPosition")]
        public string AddPosition([FromQuery] string Game, [FromQuery] string Position)
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/channelsettings.db"))
            {
                var coll = db.GetCollection<ChannelQueueSettings>("queuesettings");
                if(settings.Games.FirstOrDefault(g => g.Name == Game) is GameSettings newGame)
                {
                    if(newGame.Positions.FirstOrDefault(p => p.PositionName == Position) is GamePosition p)
                    {
                        return string.Format("{0} has already been added to {1} for this channel!", Position, newGame.Name);
                    }
                    else
                    {
                        newGame.Positions.Add(new GamePosition(Position));
                        coll.Update(settings);
                        return string.Format("{0} added to {1} for this channel!", Position, newGame.Name);
                    }
                }
                else
                {
                    return string.Format("{0} isn't in this channel's list of supported games yet.", Game);
                }
            }
        }

        /// <summary>
        /// Adds an alias to a game position (e.g., "Jng" for "Jungle" in League)
        /// </summary>
        /// <param name="Game">The name of the game/queue</param>
        /// <param name="Position">The position to add this alias to</param>
        /// <param name="Alias">The alias to add</param>
        /// <returns>A message indicating whether the attempt was successful</returns>
        [HttpGet]
        [Route("AddAlias")]
        public string AddAlias([FromQuery] string Game, [FromQuery] string Position, [FromQuery] string Alias)
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/channelsettings.db"))
            {
                var coll = db.GetCollection<ChannelQueueSettings>("queuesettings");
                if (settings.Games.FirstOrDefault(g => g.Name == Game) is GameSettings newGame)
                {
                    if (newGame.Positions.FirstOrDefault(p => p.PositionName == Position) is GamePosition p)
                    {
                        if(p.Aliases.FirstOrDefault(p => p == Alias) is string a)
                        {
                            return string.Format("{0} is already an alias for {1}!", Alias, Position);
                        }
                        else
                        {
                            p.Aliases.Add(Alias);
                        }
                    }
                    else
                    {
                        GamePosition gp = new GamePosition(Position);
                        gp.Aliases.Add(Alias);
                        newGame.Positions.Add(gp);
                        coll.Update(settings);
                    }
                    return string.Format("{0} added as an alias to {1} for this channel!", Alias, Position);
                }
                else
                {
                    return string.Format("{0} isn't in this channel's list of supported games yet.", Game);
                }
            }
        }

        /// <summary>
        /// Returns a given user's position in queue
        /// </summary>
        /// <param name="User">The username to lookup</param>
        /// <param name="Channel">The channel-specific collection to use</param>
        /// <param name="Game">The name of the game/queue</param>
        /// <returns>a zero-based integer describing the user's position in queue</returns>
        private int GetUserPositionInQueue(string User, string Channel, string Game)
        {
            return GetQueue(Channel, Game).FindIndex(u => u.Username == User);
        }

        /// <summary>
        /// Gets the next players from queue
        /// </summary>
        /// <param name="Channel">The channel name</param>
        /// <param name="Game">The name of the game/queue</param>
        /// <param name="Limit">The number of users to return</param>
        /// <param name="Remove">Whether the players should be marked as removed</param>
        /// <returns>A list of <see cref="QueuedUser"/> in queue order</returns>
        private List<QueuedUser> GetQueue(string Channel, string Game, int? Limit = null, bool Remove = false)
        {
            using (var db = new LiteDatabase(@"/pantherttv/gamequeue.db"))
            {
                var qcollection = db.GetCollection<QueuedUser>(string.Format("gamequeue_{0}", Channel));
                var queuedUsers = qcollection
                    .Query()
                    .Where(u => u.Game == Game && u.Removed == false)
                    .ToList()
                    .OrderBy(u => u.Priority)
                    .ThenBy(u => u.TimeQueued)
                    .ThenByDescending(u => u.LastPlayed)
                    .Take(Limit ?? 5);

                if (Remove == true)
                {
                    foreach (QueuedUser u in queuedUsers)
                    {
                        u.Removed = true;
                        qcollection.Update(u);
                    }
                }

                return queuedUsers.ToList();
            }
        }
    }
}