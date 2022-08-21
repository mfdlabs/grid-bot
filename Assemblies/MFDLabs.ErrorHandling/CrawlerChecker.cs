#if NETFRAMEWORK

using System.Web;
using System.Text.RegularExpressions;
using MFDLabs.Text.Extensions;

namespace MFDLabs
{
    public static class CrawlerChecker
    {
        public static bool IsCrawler(HttpRequest request)
        {
            // Rely on Microsoft's detection as a first pass, but some crawlers slip by that, so detect more by user agent
            var isCrawler = request.Browser.Crawler;
            
            if (isCrawler) return true;
            
            if (request.UserAgent.IsNullOrEmpty())
            {
                // Any real user's Web browser always has a user agent, so if we don't see one...
                isCrawler = true;
            }
            else
            {
                var regEx = new Regex("Slurp|slurp|ask|Ask|Teoma|teoma|Scooter|Mercator|MSNBOT|Gulliver|Spider|spider|Archiver|archiver|Crawler|crawler|Bot |Bot -|Bot/|bot |bot -|bot/");
                isCrawler = regEx.Match(request.UserAgent ?? string.Empty).Success;
            }
            return isCrawler;
        }
        public static bool IsCrawler() => IsCrawler(HttpContext.Current.Request);
    }
}

#endif