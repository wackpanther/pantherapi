using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PantherAPI
{
    /// <summary>
    /// An object describing the user in queue
    /// </summary>
    public class QueuedUser
    {
        /// <summary>
        /// The identifier for this particular user
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The platform username of the user
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// When the user was queued
        /// </summary>
        public DateTime TimeQueued { get; set; }
        /// <summary>
        /// The name of the game this user is queued for
        /// </summary>
        public string Game { get; set; }
        /// <summary>
        /// The user's in game name for this game
        /// </summary>
        public string InGameName { get; set; }
        /// <summary>
        /// Which position the user would like to play
        /// </summary>
        public string Position { get; set; }
        /// <summary>
        /// The user's priority in queue (default: 10)
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// When the user last played a game, used for tracking and queuing if they get added back to queue (players who haven't played get priority)
        /// </summary>
        public DateTime? LastPlayed { get; set; }
        /// <summary>
        /// Whether the user is currently skipped
        /// </summary>
        public bool Skipped { get; set; }
        /// <summary>
        /// Whether the user is removed from queue
        /// </summary>
        public bool Removed { get; set; }

        public QueuedUser() { }

        public QueuedUser(string userName, string game, string position, string inGameName) 
        {
            Username = userName;
            Game = game;
            Position = position;
            InGameName = inGameName;
            TimeQueued = DateTime.Now;
        }
    }
}
