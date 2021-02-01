using LiteDB;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PantherAPI.Controllers
{
    /// <summary>
    /// Enables the !bonk ability for a given channel, using nightbot headers
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class BonkController : Controller
    {
        /// <summary>
        /// The default amount of time to place a bonked user in jail
        /// </summary>
        TimeSpan DefaultJailTime = TimeSpan.FromMinutes(5);
        /// <summary>
        /// The maximum minutes allowed per request
        /// </summary>
        int MaxMinutes = 300;
        /// <summary>
        /// The maximum sentence from any source (jailbreak, added time, etc)
        /// </summary>
        TimeSpan MaxSentence = TimeSpan.FromHours(24);

        /// <summary>
        /// Bonks a user, returning a message that user has been bonked and when the user will be released from jail
        /// </summary>
        /// <param name="Bonkee">A string containing the username of the bonked user</param>
        /// <param name="JailMinutes">An int32 representing the number of minutes a user should be jailed for</param>
        /// <returns>A string with a message that should get printed directly to chat</returns>
        [HttpGet]
        public string Bonk([FromQuery]string Bonkee, [FromQuery]int JailMinutes = 0)
        {
            if(Bonkee == "null" || Bonkee.Contains("[username]"))
            {
                return "No username provided! Try '!bonk @[username]'";
            }
            if (!Request.Headers.ContainsKey("Nightbot-Channel"))
            {
                return "Oops! Channel couldn't be determined from your request.";
            }

            string Channel = HttpUtility.ParseQueryString(Request.Headers["Nightbot-Channel"])["name"];

            TimeSpan jailTime = JailMinutes == 0 ? DefaultJailTime : TimeSpan.FromMinutes(Math.Min(JailMinutes,MaxMinutes));
            Bonkee = Bonkee.StartsWith("@") ? Bonkee : string.Concat("@", Bonkee);

            Bonkee = Bonkee.Replace(" ", "");

            BonkedUser bonkedUser;
            //Next: Add bonkee to jail
            using (var db = new LiteDatabase(@"/pantherttv/hornyjail.db"))
            {
                var BonkedUsers = db.GetCollection<BonkedUser>(string.Format("bonkedusers_{0}", Channel));
                bonkedUser = BonkedUsers.FindOne(x => x.Username == Bonkee);
                if (bonkedUser != null)
                {
                    DateTime NewReleaseDate = 
                    bonkedUser.ReleaseDate = bonkedUser.ReleaseDate > DateTime.Now ? bonkedUser.ReleaseDate + jailTime : DateTime.Now + jailTime;
                    BonkedUsers.Update(bonkedUser);
                }
                else
                {
                    bonkedUser = new BonkedUser(Bonkee, jailTime);
                    BonkedUsers.Insert(bonkedUser);
                }
                BonkedUsers.EnsureIndex(x => x.Username);
            }
            //return successful message
            return string.Format("{0} has been bonked and is in jail until {1}", bonkedUser.Username, bonkedUser.ReleaseDate.ToString("g",CultureInfo.CreateSpecificCulture("en-US")));
        }

        /// <summary>
        /// Returns a JSON Array of currently jailed users
        /// </summary>
        /// <returns>A JsonResult list of <see cref="BonkedUser"/></returns>
        [HttpGet]
        [Route("InJail")]
        public JsonResult Jailed()
        {
            string Channel = HttpUtility.ParseQueryString(Request.Headers["Nightbot-Channel"])["name"];

            List<string> InJailList;

            using (var db = new LiteDatabase(@"/pantherttv/hornyjail.db"))
            {
                var BonkedUsers = db.GetCollection<BonkedUser>(string.Format("bonkedusers_{0}", Channel));
                InJailList = BonkedUsers.Query()
                    .Where(x => x.ReleaseDate > DateTime.Now)
                    .OrderBy(x => x.DateBonked)
                    .Select(x => x.Username)
                    .ToList();
            }
            return Json(InJailList);
        }

        /// <summary>
        /// A minigame wherein a user may attempt a jailbreak, and is either jailed for longer or freed based on random chance
        /// </summary>
        /// <param name="Bonkee"></param>
        /// <returns>A <see cref="string"/> message indicting whether the attempt was successful or not</returns>
        [HttpGet]
        [Route("jailbreak")]
        public string Jailbreak([FromQuery] string Bonkee)
        {
            if (Bonkee == "null" || Bonkee.Contains("[username]"))
            {
                return "No username provided! Try '!jailbreak @[username]'";
            }
            if (!Request.Headers.ContainsKey("Nightbot-Channel"))
            {
                return "Oops! Channel couldn't be determined from your request.";
            }

            string Channel = HttpUtility.ParseQueryString(Request.Headers["Nightbot-Channel"])["name"];

            TimeSpan jailTime = TimeSpan.FromMinutes(MaxMinutes);
            Bonkee = Bonkee.StartsWith("@") ? Bonkee : string.Concat("@", Bonkee);

            Bonkee = Bonkee.Replace(" ", "");

            BonkedUser bonkedUser;
            //Next: Add bonkee to jail
            using (var db = new LiteDatabase(@"/pantherttv/hornyjail.db"))
            {
                var BonkedUsers = db.GetCollection<BonkedUser>(string.Format("bonkedusers_{0}",Channel));
                bonkedUser = BonkedUsers.FindOne(x => x.Username == Bonkee);
                if (bonkedUser != null)
                {
                    if (bonkedUser.NextJailBreakChance < DateTime.Now)
                    {
                        return string.Format("{0} won't be eligible for another jailBreak until {1}", bonkedUser.Username, bonkedUser.NextJailBreakChance);
                    }
                    Random rand = new Random();
                    bool success = rand.NextDouble() > 0.5;
                    bonkedUser.ReleaseDate = success ? DateTime.Now : bonkedUser.ReleaseDate + jailTime;
                    bonkedUser.NextJailBreakChance = DateTime.Now + TimeSpan.FromHours(1);
                    BonkedUsers.Update(bonkedUser);
                    if (success)
                    {
                        return string.Format("{0} escaped and is now out of horny jail!", Bonkee);
                    }
                    else
                    {
                        return string.Format("{0} was caught trying to escape and won't be released until {1}", Bonkee, bonkedUser.ReleaseDate);
                    }
                }
                else
                {
                    return string.Format("{0} is not in jail!", Bonkee);
                }
            }
        }
    }
}
