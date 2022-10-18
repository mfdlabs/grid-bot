#if NETFRAMEWORK

using System;
using System.Web;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using MFDLabs.ErrorHandling.Extensions;

namespace MFDLabs
{
    public interface IExceptionHandlerListener
    {
        // Called when an exception has been logged.
        void ExceptionLogged();
    }
    // Throw this exception or an exception derived from this if you don't want it to show up in the MFDLABS Event Log.
    // This should be used to return an error to the user, but something that technically isn't broken in the system.
    // For example failing validation for a control would be a good candidate as it is nothing for us to fix in the code base.
    public abstract class NotLoggedException : Exception
    {
        public NotLoggedException(string reason) : base(reason) { }
        public NotLoggedException(string reason, Exception inner) : base(reason, inner) { }
        public NotLoggedException() { }
    }


    public class ExceptionHandler
    {
        private static readonly Dictionary<int, PresentableSqlErrors> PresentableSqlErrorsList;
        private static readonly List<IExceptionHandlerListener> Listeners = new List<IExceptionHandlerListener>();

        static ExceptionHandler()
        {
            //Populate the list of SQL Errors that should be present to the Presentation Layer
            PresentableSqlErrorsList = new Dictionary<int, PresentableSqlErrors>();
            foreach (var err in Converters.EnumToList<PresentableSqlErrors>()) 
                PresentableSqlErrorsList.Add((int)err, err);
        }

        public static void AddListener(IExceptionHandlerListener listener)
        {
            lock (Listeners) 
                Listeners.Add(listener);
        }
        public static void RemoveListener(IExceptionHandlerListener listener)
        {
            lock (Listeners) 
                Listeners.Remove(listener);
        }

        public static string GetInnerText(Exception ex) 
            => ex.InnerException != null ? GetInnerText(ex.InnerException) : ex.ToString();
        public static string GetText(Exception ex)
        {
            string s;
            if (ex.InnerException != null)
                s = ex + "\n\nCaused by:\n" + GetText(ex.InnerException);
            else
                s = ex.ToString();

            return s;
        }
        public static void LogException(string errorMessage) => LogException(new ApplicationException(errorMessage));
        public static void LogException(Exception ex) => LogException(ex, EventLogEntryType.Error);
        public static void LogException(string errorMessage, EventLogEntryType type) => LogException(new ApplicationException(errorMessage), type);
        public static void LogException(Exception ex, EventLogEntryType type) => LogException(ex, type, 1);
        public static void LogException(string errorMessage, string sourceName) => LogException(new ApplicationException(errorMessage), sourceName);
        public static void LogException(string errorMessage, EventLogEntryType type, string sourceName) 
            => LogException(new ApplicationException(errorMessage), type, sourceName);
        public static void LogException(Exception ex, string sourceName) => LogException(ex, EventLogEntryType.Error, 1, sourceName);
        public static void LogException(Exception ex, EventLogEntryType type, string sourceName) => LogException(ex, type, 1, sourceName);
        public static void LogException(string errorMessage, EventLogEntryType type, int eventId) => LogException(new ApplicationException(errorMessage), type, eventId);
        public static void LogException(Exception ex, EventLogEntryType type, int eventId) => LogException(ex, type, eventId, LogInstaller.LogName);
        public static void LogException(string errorMessage, EventLogEntryType type, int eventId, string sourceName) 
            => LogException(new ApplicationException(errorMessage), type, eventId, sourceName);
        public static void LogException(Exception ex, EventLogEntryType type, int eventId, string sourceName)
        {
            short category = 0;
            string s;
            
            if (HttpContext.Current == null)
                s = GetText(ex);
            else
            {
                s = ex.Message + "\n\n";
                s += "Url: " + HttpContext.Current.Request.Url + "\n";
                if (HttpContext.Current.Request.UrlReferrer != null)
                    s += "Referrer: " + HttpContext.Current.Request.UrlReferrer + "\n";
                if (CrawlerChecker.IsCrawler())
                {
                    s += "Identity: Crawler\n";
                    category = 3;
                }
                else if (HttpContext.Current.User != null)
                    if (HttpContext.Current.User.Identity != null)
                    {
                        s += "Identity: \"" + HttpContext.Current.User.Identity.Name + "\"\n";
                        category = 2;
                    }
                    else
                    {
                        s += "Identity: null\n";
                        category = 1;
                    }
                s += "\n";
                s += GetText(ex);
            }

            if (ex is NotLoggedException) return;
            
            EventLog.WriteEntry(sourceName, s, type, eventId, category);
            lock (Listeners)
                foreach (var listener in Listeners)
                    listener.ExceptionLogged();
        }

        public static void HandleSqlException(System.Data.SqlClient.SqlException eSql)
        {
            if (!PresentableSqlErrorsList.TryGetValue(eSql.Number, out var result)) throw eSql;
            
            //Log and throw a presentable exception so that the presentation layer can present it to the user
            var pe = new PresentableException(result.ToString(), result.ToDescription(), eSql.Message, eSql);
            LogException(pe, System.Diagnostics.EventLogEntryType.Warning);
            throw pe;
            //If not a presentable exception, continue to throw it as an unhandled exception
        }

#region |CustomExceptions|
        //Custom exception class that should be presented to the Presentation Layer and not left unhandled
        public class PresentableException : ApplicationException
        {
            public string PresentationErrorCode { get; }
            public string PresentationErrorMessage { get; }

            public PresentableException(string presentationErrorMessage, string errorMessage)
                : this(string.Empty, presentationErrorMessage, errorMessage) { }

            public PresentableException(string presentationErrorMessage, string errorMessage, Exception innerException)
                : this(string.Empty, presentationErrorMessage, errorMessage, innerException) { }

            public PresentableException(string presentationErrorCode, string presentationErrorMessage, string errorMessage, Exception innerException = null)
                : base(errorMessage, innerException)
            {
                PresentationErrorMessage = presentationErrorMessage;
                PresentationErrorCode = presentationErrorCode;
            }
        }
#endregion

#region |ExceptionEnums|
        //SQL Errors that should be presented to the Presentation Layer - value of the enum must be the Error Number returned by the SQL Server
        public enum PresentableSqlErrors
        {
            [Description("Search Query is malformed, please check the search terms and try your search again.")]
            SearchQueryMalformed = 7630
        }
#endregion
    }
}

#endif