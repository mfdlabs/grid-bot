using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace MFDLabs
{
    public class CrawlerChecker
    {

        public static bool IsCrawler(HttpRequest request)
        {
            // Rely on Microsoft's detection as a first pass, but some crawlers slip by that, so detect more by user agent
            bool isCrawler = request.Browser.Crawler;
            if (!isCrawler)
            {
                if (String.IsNullOrEmpty(request.UserAgent))
                {
                    // Any real user's Web browser always has a user agent, so if we don't see one...
                    isCrawler = true;
                }
                else
                {
                    Regex regEx = new Regex("Slurp|slurp|ask|Ask|Teoma|teoma|Scooter|Mercator|MSNBOT|Gulliver|Spider|spider|Archiver|archiver|Crawler|crawler|Bot |Bot -|Bot/|bot |bot -|bot/");
                    isCrawler = regEx.Match(request.UserAgent).Success;
                }
            }
            return isCrawler;
        }
        public static bool IsCrawler()
        {
            return IsCrawler(HttpContext.Current.Request);
        }
    }
}
