﻿using Backtrace.Base;
using Backtrace.Interfaces;
using Backtrace.Model;
using Backtrace.Model.Database;
using System;
using System.Collections.Generic;
#if !NET35
using System.Threading.Tasks;
#endif

namespace Backtrace
{
    /// <summary>
    /// Backtrace .NET Client 
    /// </summary>
    public class BacktraceClient : BacktraceBase, IBacktraceClient
    {
        /// <summary>
        /// Set an event executed before sending data to Backtrace API
        /// </summary>
        public Action<BacktraceReport> OnReportStart;

        /// <summary>
        /// Set an event executed after sending data to Backtrace API
        /// </summary>
        public Action<BacktraceResult> AfterSend;

        #region Constructor
#if !NETSTANDARD2_0
        /// <summary>
        /// Initializing Backtrace client instance
        /// </summary>
        /// <param name="sectionName">Backtrace configuration section in App.config or Web.config file. Default section is BacktraceCredentials</param>
        /// <param name="attributes">Client's attributes</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Numbers of records sending per one min</param>
        public BacktraceClient(
            string sectionName,
            string databasePath,
            Dictionary<string, object> attributes = null,
            uint reportPerMin = 3)
            : this(BacktraceCredentials.ReadConfigurationSection(sectionName),
                attributes, new BacktraceDatabase(new BacktraceDatabaseSettings(databasePath)),
                reportPerMin)
        { }


        /// <summary>
        /// Initializing Backtrace client instance
        /// </summary>
        /// <param name="sectionName">Backtrace configuration section in App.config or Web.config file. Default section is BacktraceCredentials</param>
        /// <param name="attributes">Client's attributes</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Numbers of records sending per one min</param>
        public BacktraceClient(
            string sectionName,
            IBacktraceDatabase backtraceDatabase,
            Dictionary<string, object> attributes = null,
            uint reportPerMin = 3)
            : this(BacktraceCredentials.ReadConfigurationSection(sectionName),
                attributes, backtraceDatabase, reportPerMin)
        { }


        /// <summary>
        /// Initializing Backtrace client instance
        /// </summary>
        /// <param name="sectionName">Backtrace configuration section in App.config or Web.config file. Default section is BacktraceCredentials</param>
        /// <param name="attributes">Client's attributes</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Numbers of records sending per one min</param>
        public BacktraceClient(
            BacktraceDatabaseSettings databaseSettings,
            string sectionName = "BacktraceCredentials",
            Dictionary<string, object> attributes = null,
            uint reportPerMin = 3)
            : base(BacktraceCredentials.ReadConfigurationSection(sectionName),
                attributes, new BacktraceDatabase(databaseSettings), reportPerMin)
        { }

        /// <summary>
        /// Initializing Backtrace client instance
        /// </summary>
        /// <param name="sectionName">Backtrace configuration section in App.config or Web.config file. Default section is BacktraceCredentials</param>
        /// <param name="attributes">Client's attributes</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Numbers of records sending per one min</param>
        public BacktraceClient(
            string sectionName = "BacktraceCredentials",
            Dictionary<string, object> attributes = null,
            IBacktraceDatabase database = null,
            uint reportPerMin = 3)
            : base(BacktraceCredentials.ReadConfigurationSection(sectionName),
                attributes, database, reportPerMin)
        { }
#endif
        /// <summary>
        /// Initializing Backtrace client instance with BacktraceCredentials
        /// </summary>
        /// <param name="setup">Backtrace client configuration</param>
        /// <param name="backtraceDatabase">Backtrace database</param>
        public BacktraceClient(BacktraceClientConfiguration setup, IBacktraceDatabase backtraceDatabase = null)
            : base(setup.Credentials, setup.ClientAttributes, backtraceDatabase, setup.ReportPerMin)
        { }
        /// <summary>
        /// Initializing Backtrace client instance with BacktraceCredentials
        /// </summary>
        /// <param name="backtraceCredentials">Backtrace credentials</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="attributes">Client's attributes</param>
        /// <param name="reportPerMin">Numbers of records sending per one minute</param>
        public BacktraceClient(
            BacktraceCredentials backtraceCredentials,
            BacktraceDatabaseSettings databaseSettings,
            Dictionary<string, object> attributes = null,
            uint reportPerMin = 3)
            : base(backtraceCredentials, attributes,
                  databaseSettings, reportPerMin)
        { }

        /// <summary>
        /// Initializing Backtrace client instance with BacktraceCredentials
        /// </summary>
        /// <param name="backtraceCredentials">Backtrace credentials</param>
        /// <param name="attributes">Client's attributes</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Numbers of records sending per one minute</param>
        public BacktraceClient(
            BacktraceCredentials backtraceCredentials,
            string databasePath,
            Dictionary<string, object> attributes = null,
            uint reportPerMin = 3)
            : base(backtraceCredentials, attributes,
                  new BacktraceDatabaseSettings(databasePath),
                  reportPerMin)
        { }

        /// <summary>
        /// Initializing Backtrace client instance with BacktraceCredentials
        /// </summary>
        /// <param name="backtraceCredentials">Backtrace credentials</param>
        /// <param name="attributes">Client's attributes</param>
        /// <param name="databaseSettings">Backtrace database settings</param>
        /// <param name="reportPerMin">Numbers of records sending per one minute</param>
        public BacktraceClient(
            BacktraceCredentials backtraceCredentials,
            Dictionary<string, object> attributes = null,
            IBacktraceDatabase database = null,
            uint reportPerMin = 3)
            : base(backtraceCredentials, attributes,
                  database, reportPerMin)
        { }
        #endregion

        #region Send synchronous
        /// <summary>
        /// Sending an exception to Backtrace API
        /// </summary>
        /// <param name="exception">Current exception</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public virtual BacktraceResult Send(
            Exception exception,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null)
        {
            var report = new BacktraceReport(exception, attributes, attachmentPaths);
            return Send(report);
        }

        /// <summary>
        /// Sending a message to Backtrace API
        /// </summary>
        /// <param name="message">Custom client message</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public virtual BacktraceResult Send(
            string message,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null)
        {
            var report = new BacktraceReport(message, attributes, attachmentPaths);
            return Send(report);
        }

        /// <summary>
        /// Sending a backtrace report to Backtrace API
        /// </summary>
        /// <param name="backtraceReport">Current report</param>
        public override BacktraceResult Send(BacktraceReport backtraceReport)
        {
            OnReportStart?.Invoke(backtraceReport);
            var result = base.Send(backtraceReport);
            AfterSend?.Invoke(result);
            return result;
        }
        #endregion

#if !NET35
        #region Send asynchronous
        /// <summary>
        /// Sending asynchronous Backtrace report to Backtrace API
        /// </summary>
        /// <param name="backtraceReport">Current report</param>
        /// <returns>Server response</returns>
        public override async Task<BacktraceResult> SendAsync(BacktraceReport backtraceReport)
        {
            OnReportStart?.Invoke(backtraceReport);
            var result = await base.SendAsync(backtraceReport);
            AfterSend?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Sending a message to Backtrace API
        /// </summary>
        /// <param name="message">Custom client message</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public async Task<BacktraceResult> SendAsync(
            string message,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null)
        {
            return await SendAsync(new BacktraceReport(message, attributes, attachmentPaths));
        }

        /// <summary>
        /// Sending asynchronous exception to Backtrace API
        /// </summary>
        /// <param name="exception">Current exception</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public virtual async Task<BacktraceResult> SendAsync(
            Exception exception,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null)
        {
            return await SendAsync(new BacktraceReport(exception, attributes, attachmentPaths));
        }
        #endregion
#endif
    }
}
