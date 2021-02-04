using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PantherAPI
{
    public static class Utils
    {
        public static string Stringify(this int number, string ZeroMsg)
        {
            if (number <= 0)
            {
                return ZeroMsg;
            }

            number += 1;

            if (number.ToString().EndsWith("1"))
            {
                return string.Concat(number.ToString(), "st");
            }
            else if (number.ToString().EndsWith("2"))
            {
                return string.Concat(number.ToString(), "nd");
            }
            else if (number.ToString().EndsWith("3"))
            {
                return string.Concat(number.ToString(), "rd");
            }
            else
            {
                return string.Concat(number.ToString(), "th");
            }
        }

        public static string MultiStringify(this int number, string ZeroMsg)
        {
            if (number == 0)
            {
                return ZeroMsg;
            }

            if (number == 1)
            {
                return "once";
            }
            else if (number == 2)
            {
                return "twice";
            }
            else
            {
                return string.Concat(number.ToString(), " times");
            }
        }

        public static string Format(this string Template, params object?[] args)
        {
            return string.Format(Template, args);
        }

        public static string GetChannel(IHeaderDictionary headers)
        {
            if (HttpUtility.ParseQueryString(headers["Nightbot-Channel"])["name"] is string Channel)
            {
                return Channel;
            }
            else
            {
                throw new ArgumentNullException("ChannelName", "Oops! Channel couldn't be retrieved from your request.");
            }
        }
        public static string GetUser(IHeaderDictionary headers)
        {
            if (HttpUtility.ParseQueryString(headers["Nightbot-User"])["name"] is string User)
            {
                return User.StartsWith("@") ? User : string.Concat("@", User);
            }
            else
            {
                throw new ArgumentNullException("UserName", "Oops! User couldn't be determined from your request.");
            }
        }
    }
}
