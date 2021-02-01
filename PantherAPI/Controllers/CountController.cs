using LiteDB;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PantherAPI.Utils;

namespace PantherAPI.Controllers
{
    /// <summary>
    /// Enables a flxible count solution for all chatbots
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class CountController : Controller
    {
        string Channel;
        //string RequestUser;

        public CountController() {}

        /// <summary>
        /// Wraps the channel/user name parsing in a convenient method
        /// </summary>
        private void Setup()
        {
            Channel = GetChannel(Request.Headers);
            //RequestUser = GetUser(Request.Headers);
        }

        /// <summary>
        /// Adds a number to the count for a given counter
        /// </summary>
        /// <param name="Name">The name of the counter to use</param>
        /// <param name="Label">The past-tense verb to use for this counter (e.g., "@channel has <c>died</c> 17 times"</param>
        /// <param name="Amount">The number to add, defaults to +1</param>
        /// <returns>A string describing the current count</returns>
        [HttpGet]
        public string Count([FromQuery] string Name, [FromQuery] string Label = null, [FromQuery] int Amount = 1)
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/count.db"))
            {
                var cc = db.GetCollection<Counter>(string.Format("counters_{0}", Channel));
                Counter current;
                if (cc.FindOne(c => c.Name == Name) is Counter c)
                {
                    current = c;
                    current.Add(Amount);
                    if(Label != null && current.Label != Label)
                    {
                        current.Label = Label;
                    }
                    cc.Update(current);
                }
                else
                {
                    current = new Counter(Name, Label, Amount);
                    cc.Insert(current);
                }

                if (current.Label == null)
                {
                    return string.Format("{0}: {1}", Name, current.CurrentCount);
                }
                else
                {
                    return string.Format("{0} has {1} {2}", string.Concat("@", Channel), current.Label, current.CurrentCount.MultiStringify("zero times"));
                }
            }
        }

        /// <summary>
        /// Removes an amount from the count (but a counter cannot be negative)
        /// </summary>
        /// <param name="Name">The counter to use</param>
        /// <param name="Label">The past-tense verb to use for this counter (e.g., "@channel has <c>died</c> 17 times"</param>
        /// <param name="Amount">The amount to subtract, defaults to -1</param>
        /// <returns>A string describing the current count</returns>
        [HttpGet]
        [Route("Subtract")]
        public string Subtract([FromQuery] string Name, [FromQuery] string Label = null, [FromQuery] int Amount = 1)
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/count.db"))
            {
                var cc = db.GetCollection<Counter>(string.Format("counters_{0}", Channel));
                Counter current;
                if (cc.FindOne(c => c.Name == Name) is Counter c)
                {
                    current = c;
                    current.Subtract(Amount);
                    if (Label != null && current.Label != Label)
                    {
                        current.Label = Label;
                    }
                    cc.Update(current);
                }
                else
                {
                    current = new Counter(Name, Label);
                    cc.Insert(current);
                }

                if (current.Label == null)
                {
                    return string.Format("{0}: {1}", Name, current.CurrentCount);
                }
                else
                {
                    return string.Format("{0} has {1} {2}", string.Concat("@", Channel), current.Label, current.CurrentCount.MultiStringify("zero times"));
                }
            }
        }

        /// <summary>
        /// Resets the given counter
        /// </summary>
        /// <param name="Name">The name of the counter to clear</param>
        /// <returns>A string indicating whether the attempt was successful</returns>
        [HttpGet]
        [Route("Reset")]
        public string Reset([FromQuery] string Name)
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/count.db"))
            {
                var cc = db.GetCollection<Counter>(string.Format("counters_{0}", Channel));
                Counter current;
                if (cc.FindOne(c => c.Name == Name) is Counter c)
                {
                    current = c;
                    current.Reset();
                    cc.Update(current);
                    return "{0} have been reset".Format(Name);
                }
                else
                {
                    return "{0} aren't being tracked yet".Format(Name);
                }
            }
        }

        /// <summary>
        /// Defines a counter ahead of time for a channel (making many parameters optional in the add/subtract calls)
        /// </summary>
        /// <param name="Name">The name of the counter to establish</param>
        /// <param name="Label">The past-tense verb to use for this counter (e.g., "@channel has <c>died</c> 17 times"</param>
        /// <param name="Seed">An (optional) value to start counting from</param>
        /// <returns>A string indicating whether the attempt was successful</returns>
        [HttpGet]
        [Route("Define")]
        public string Define([FromQuery] string Name, [FromQuery] string Label, [FromQuery] int Seed = 0)
        {
            Setup();
            using (var db = new LiteDatabase(@"/pantherttv/count.db"))
            {
                var cc = db.GetCollection<Counter>(string.Format("counters_{0}", Channel));
                if (cc.FindOne(c => c.Name == Name) is Counter c)
                {
                    return "{0} already exists for this channel and is currently at {1}. The label has been updated.".Format(Name, c.CurrentCount);
                }
                else
                {
                    cc.Insert(new Counter(Name, Label, Seed));

                    return "{0} counter has been setup, and will look like: '{1} has {2} {3}'".Format(Name, Channel,Label,new Random().Next(1,47).MultiStringify(""));
                }
            }
        }
    }
}
