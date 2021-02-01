using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PantherAPI
{
    /// <summary>
    /// An object describing a user who has been bonked
    /// </summary>
    public class BonkedUser
    {
        /// <summary>
        /// The id describing this particular user
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The platform username of the user
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// The date the user was (last) bonked)
        /// </summary>
        public DateTime DateBonked { get; set; }
        /// <summary>
        /// The date the user will be released from jail
        /// </summary>
        public DateTime ReleaseDate { get; set; }
        /// <summary>
        /// The next data the user will be eligible to break out of jail
        /// </summary>
        public DateTime NextJailBreakChance { get; set; }
        public BonkedUser() { }

        /// <summary>
        /// Creates a new user with prefilled properties
        /// </summary>
        /// <param name="Bonkee">The username of the user</param>
        /// <param name="JailTime">How long the user should be in jail</param>
        public BonkedUser(string Bonkee, TimeSpan JailTime)
        {
            Username = Bonkee;
            DateBonked = DateTime.Now;
            ReleaseDate = DateTime.Now + JailTime;
            NextJailBreakChance = DateTime.Now;
        }
    }
}
