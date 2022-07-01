using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using MFDLabs.Networking;
using MFDLabs.FileSystem;
using MFDLabs.Diagnostics;
using MFDLabs.Text.Extensions;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;

#if CONCURRENT_LOGGING_ENABLED
using Microsoft.Ccr.Core;
#endif

namespace MFDLabs.Logging
{
    public enum LogColor
    {
        Reset,
        BrightBlack,
        BrightRed,
        BrightGreen,
        BrightYellow,
        BrightBlue,
        BrightMagenta,
        BrightCyan,
        BrightWhite,
    }

    public class Logger : ILogger, IDisposable
    {
        //////////////////////////////////////////////////////////////////////////////
        // Private Static Constructor
        //////////////////////////////////////////////////////////////////////////////

        static Logger()
        {
            var stdout = Console.OpenStandardOutput();
            var stdoutStream = new System.IO.StreamWriter(stdout, System.Text.Encoding.ASCII)
            {
                AutoFlush = true
            };
            Console.SetOut(stdoutStream);
        }

        //////////////////////////////////////////////////////////////////////////////
        // Private Static Readonly Properties
        //////////////////////////////////////////////////////////////////////////////

        private const string _logFilenameFormat =
#if DEBUG
            "dev_" +
#endif
            "{0}_{1}_{2}_{3}.log";

        // language=regex
        private const string _loggerNameRegex = @"^[a-zA-Z0-9_\-\.]{1,75}$";

        private static string _logFileBaseDirectoryBacking = null;
        private static string _logFileBaseDirectory
        {
            get
            {
                try
                {

                    if (_logFileBaseDirectoryBacking == null)
                        _logFileBaseDirectoryBacking = Path.DirectorySeparatorChar == '/'
                            ? Path.Combine(Path.GetTempPath(), "mfdlabs", "logs")
                            : Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "MFDLABS", "Logs");

                    return _logFileBaseDirectoryBacking;
                }
                catch
                {
                    // If it fails, default to the current working directory
                    if (_logFileBaseDirectoryBacking == null)
                        _logFileBaseDirectoryBacking = Directory.GetCurrentDirectory();

                    return _logFileBaseDirectoryBacking;
                }
            }
        }

        private static readonly string _localIp = NetworkingGlobal.GetLocalIp();
        private static readonly string _hostname = SystemGlobal.GetMachineHost();
        private static readonly string _processId = SystemGlobal.CurrentProcess.Id.ToString("X");
        private static readonly string _platform = SystemGlobal.CurrentPlatform;
        private static readonly string _architecture = SystemGlobal.CurrentDeviceArch;
        private static readonly string _dotnetVersion = SystemGlobal.Version;
        private static readonly string _architectureFmt = $"{_platform}-{_architecture}";

        private static readonly List<Logger> _loggers = new();

        //////////////////////////////////////////////////////////////////////////////
        // Private Static Properties
        //////////////////////////////////////////////////////////////////////////////

        protected static Logger _singleton = null;
        protected static Logger _noopSingleton = null;

        private static bool? _hasTerminal = null;

        //////////////////////////////////////////////////////////////////////////////
        // Private Readonly Properties (Only set in the constructor)
        //////////////////////////////////////////////////////////////////////////////

        private readonly bool _cutLogPrefix = true;

        private readonly object _fileSystemLock = new();
        private readonly object _consoleLock = new();

#if CONCURRENT_LOGGING_ENABLED

        private readonly Port<Action> _messageQueue;
        private readonly DispatcherQueue _dispatcherQueue;

#endif

        //////////////////////////////////////////////////////////////////////////////
        // Private Properties
        //////////////////////////////////////////////////////////////////////////////

        protected string _name = null;
        private LogLevel _logLevel = LogLevel.Information;
        private bool _logToConsole = true;
        private bool _logToFileSystem = true;
        private bool _logThreadId = false;
        private bool _logWithColor = false;

        private FileSystemHelper.LockedFileStream _lockedFileWriteStream = null;
        private string _fileName = null;
        private string _fullyQualifiedFileName = null;

        private string _cachedNonColorPrefix = null;
        private string _cachedColorPrefix = null;

        private bool _disposed = false;

        //////////////////////////////////////////////////////////////////////////////
        // Private Static Helper Methods
        //////////////////////////////////////////////////////////////////////////////

        private static bool _terminalAvailable()
        {
            if (_hasTerminal.HasValue) return _hasTerminal.Value;

            try
            {
                _ = Console.WindowHeight;

                return _hasTerminal ??= true;
            }
            catch
            {
                return _hasTerminal ??= false;
            }
        }

        private static string _getCurrentThreadIdFormatted() => Thread.CurrentThread.ManagedThreadId.ToString("x")?.PadLeft(4, '0') ?? "<unknown>";
        private static string _getProcessUptime() => (DateTime.Now - SystemGlobal.CurrentProcess.StartTime).TotalSeconds.ToString("f7");

        private static string _getAnsiColorByLogColor(LogColor color)
            => color switch
            {
                LogColor.Reset => "\x1b[0m",
                LogColor.BrightBlack => "\x1b[90m",
                LogColor.BrightRed => "\x1b[91m",
                LogColor.BrightGreen => "\x1b[92m",
                LogColor.BrightYellow => "\x1b[93m",
                LogColor.BrightBlue => "\x1b[94m",
                LogColor.BrightMagenta => "\x1b[95m",
                LogColor.BrightCyan => "\x1b[96m",
                LogColor.BrightWhite => "\x1b[97m",
                _ => "\x1b[0m",
            };
        private static string _getColorSection(object content) => _getColorSection(LogColor.BrightBlack, content);
        private static string _getColorSection(LogColor color, object content)
            => string.Format(
                   "[{0}{1}{2}]",
                   _getAnsiColorByLogColor(color),
                   content.ToString(),
                   _getAnsiColorByLogColor(LogColor.Reset)
               );

        //////////////////////////////////////////////////////////////////////////////
        // Private/Protected Helper Methods
        //////////////////////////////////////////////////////////////////////////////

        protected virtual string _getNonColorLogPrefix()
        {
            if (_cutLogPrefix)
                return _cachedNonColorPrefix ??= string.Format(
                    "[{0}][{1}][{2}]",
                    _localIp,
                    _hostname,
                    _name
                );

            return _cachedNonColorPrefix ??= string.Format(
                "[{0}][{1}][{2}][{3}][{4}][{5}]",
                _processId,
                _architectureFmt,
                _dotnetVersion,
                _localIp,
                _hostname,
                _name
            );
        }

        protected virtual string _getColorPrefix()
        {
            if (_cutLogPrefix)
                return _cachedColorPrefix ??= string.Format(
                    "{0}{1}{2}",
                    _getColorSection(_localIp),
                    _getColorSection(_hostname),
                    _getColorSection(_name)
                );

            return _cachedColorPrefix ??= string.Format(
                "{0}{1}{2}{3}{4}{5}",
                _getColorSection(_processId),
                _getColorSection(_architectureFmt),
                _getColorSection(_dotnetVersion),
                _getColorSection(_localIp),
                _getColorSection(_hostname),
                _getColorSection(_name)
            );
        }

        protected virtual string _constructLoggerMessage(LogLevel logLevel, string format, params object[] args)
        {
            var formattedMessage = string.Format(format, args);

            if (_cutLogPrefix)
                return string.Format(
                    "[{0}]{1}{2}[{3}] {4}\n",
                    DateTimeGlobal.GetUtcNowAsIso(),
                    _logThreadId ? $"[{_getCurrentThreadIdFormatted()}]" : "",
                    _getNonColorLogPrefix(),
                    logLevel.ToString().ToUpper(),
                    formattedMessage
                );

            return string.Format(
                "[{0}][{1}][{2}]{3}[{4}] {5}\n",
                DateTimeGlobal.GetUtcNowAsIso(),
                _getProcessUptime(),
                _logThreadId ? $"[{_getCurrentThreadIdFormatted()}]" : "",
                _getNonColorLogPrefix(),
                logLevel.ToString().ToUpper(),
                formattedMessage
            );
        }

        protected virtual string _constructColoredLoggerMessage(LogLevel logLevel, LogColor color, string format, params object[] args)
        {
            var formattedMessage = string.Format(format, args);

            if (_cutLogPrefix)
                return string.Format(
                    "{0}{1}{2}{3} {4}{5}{6}\n",
                    _getColorSection(DateTimeGlobal.GetUtcNowAsIso()),
                    _logThreadId ? _getColorSection(_getCurrentThreadIdFormatted()) : "",
                    _getColorPrefix(),
                    _getColorSection(color, logLevel.ToString().ToUpper()),
                    _getAnsiColorByLogColor(color),
                    formattedMessage,
                    _getAnsiColorByLogColor(LogColor.Reset)
                );

            return string.Format(
                "{0}{1}{2}{3}{4} {5}{6}{7}\n",
                _getColorSection(DateTimeGlobal.GetUtcNowAsIso()),
                _getColorSection(_getProcessUptime()),
                _logThreadId ? _getColorSection(_getCurrentThreadIdFormatted()) : "",
                _getColorPrefix(),
                _getColorSection(color, logLevel.ToString().ToUpper()),
                _getAnsiColorByLogColor(color),
                formattedMessage,
                _getAnsiColorByLogColor(LogColor.Reset)
            );
        }

        private void _logLocally(LogLevel logLevel, string format, params object[] args)
        {
            if (!_logToFileSystem) return;

            if (_lockedFileWriteStream == null)
            {
                _createFileName();
                _createFileStream();
            }

            lock (_fileSystemLock)
            {
                try
                {
                    _lockedFileWriteStream?.AppendText(_constructLoggerMessage(logLevel, format, args));
                }
                catch (Exception ex)
                {
                    _logToFileSystem = false;
                    _closeFileStream();

                    Warning("Unable to write to file stream due to \"{0}\". Disabling file stream.", ex.Message);
                }
            }
        }

        private void _logConsole(LogLevel logLevel, LogColor color, string format, params object[] args)
        {
            if (!_logToConsole) return;

            lock (_consoleLock)
            {

                var message = _logWithColor
                    ? _constructColoredLoggerMessage(logLevel, color, format, args)
                    : _constructLoggerMessage(logLevel, format, args);

                Console.Write(message);
            }
        }

        private void _queueOrLogInternal(LogLevel logLevel, LogColor color, string format, params object[] args)
        {
            if (_disposed) throw new ObjectDisposedException(_name);

#if CONCURRENT_LOGGING_ENABLED
            _messageQueue.Post(() =>
            {
                try { _log(logLevel, color, format, args); } catch { }
            });
#else
            try { _log(logLevel, color, format, args); } catch { }
#endif
        }

        protected virtual void _log(LogLevel logLevel, LogColor color, string format, params object[] args)
        {
            _logConsole(logLevel, color, format, args);
            _logLocally(logLevel, format, args);
        }

        private bool _checkLogLevel(LogLevel logLevel) => _logLevel >= logLevel;

        private void _createFileStream()
        {
            _lockedFileWriteStream ??= new(_fullyQualifiedFileName);
        }

        private void _closeFileStream()
        {
            _lockedFileWriteStream?.Dispose();
            _lockedFileWriteStream = null;

            _fullyQualifiedFileName = null;
            _fileName = null;
        }

        private void _createFileName()
        {
            _fileName ??= string.Format(
                _logFilenameFormat,
                _name,
                SystemGlobal.AssemblyVersion,
                DateTimeGlobal.GetFileSafeUtcNowAsIso(),
                _processId
            );

            _fullyQualifiedFileName ??= Path.Combine(_logFileBaseDirectory, _fileName);

            if (!Directory.Exists(_logFileBaseDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_logFileBaseDirectory);
                }
                catch (IOException)
                {
                    // Assume it's a file.
                    _logToFileSystem = false;
                    _closeFileStream();
                    Warning("Unable to create log file directory. It already exists and is a file.");

                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    _logToFileSystem = false;
                    _closeFileStream();
                    Warning("Unable to create log file directory. Please ensure that the current user has permission to create directories.");

                    return;
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        // Public Static Helper Methods
        //////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Requests that the log file directory be cleared.
        /// </summary>
        /// <param name="override">If true, the log file directory will be cleared regardless of settings.</param>
        public static void TryClearLocalLog(bool @override = false)
        {
            Logger.Singleton.Log("Try clear local log files...");

            try
            {
                if (global::MFDLabs.Logging.Properties.Settings.Default.PersistLocalLogs)
                {
                    if (@override)
                    {
                        Logger.Singleton.Warning("Override flag set. Clearing local log files.");
                    } else
                    {
                        Logger.Singleton.Warning("Local log files will not be cleared because PersistLocalLogs is set to true.");
                        return;
                    }
                }

                Logger.Singleton.Log("Clearing local log files...");

                foreach (var logger in _loggers) logger._closeFileStream();

                if (Directory.Exists(_logFileBaseDirectory))
                {
                    Directory.Delete(_logFileBaseDirectory, true);
                    Directory.CreateDirectory(_logFileBaseDirectory);
                }

                foreach (var logger in _loggers)
                {
                    if (logger._logToFileSystem)
                    {
                        logger._createFileName();
                        logger._createFileStream();
                    }
                }
            }
            catch (IOException)
            {
                // Assume it's a file.
                Logger.Singleton.Error("Unable to create log file directory. It already exists and is a file.");
                return;
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Singleton.Error("Unable to create or delete log file directory. Please ensure that the current user has permission to create and delete directories.");
                return;
            }
            catch (Exception ex)
            {
                Logger.Singleton.Error("Unable to clear local log files due to \"{0}\".", ex.Message);
            }
        }

        /// <summary>
        /// Tries to clear out all tracked loggers.
        /// </summary>
        public static void TryClearAllLoggers()
        {
            Logger.Singleton.Log("Try clear all loggers...");

            try
            {
                foreach (var logger in _loggers)
                    if (logger._name != _singleton?._name && logger._name != _noopSingleton?._name)
                        logger.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Singleton.Error("Unable to clear all loggers due to \"{0}\".", ex.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Public Constructor
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a new instance of <see cref="Logger"/>.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="logLevel">The log level of the logger.</param>
        /// <param name="logToFileSystem">If true, the logger will log to the file system.</param>
        /// <param name="logToConsole">If true, the logger will log to the console.</param>
        /// <param name="cutLogPrefix">If true, the logger will cut the log prefix.</param>
        /// <param name="logThreadId">If true, the logger will log with a thread id.</param>
        /// <param name="logWithColor">If true, the logger will console log with colors.</param>
        /// <exception cref="ArgumentNullException">The name is null or empty.</exception>
        /// <exception cref="ArgumentException">The name does not match the logger name Regex.</exception>
        /// <exception cref="InvalidOperationException">There is an already existing logger with the specified name.</exception>
        /// <remarks>
        /// If you do not require a specific logger, use <see cref="Logger.Singleton"/> instead.
        /// </remarks>
        public Logger(
            string name,
            LogLevel logLevel = LogLevel.Information,
            bool logToFileSystem = true,
            bool logToConsole = true,
            bool cutLogPrefix = true,
            bool logThreadId = false,
            bool logWithColor = false
        )
        {
            if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));
            if (!name.IsMatch(_loggerNameRegex))
                throw new ArgumentException($"The logger name must match '{_loggerNameRegex}'", nameof(name));

            if (_loggers.Where(logger => logger.Name == name).Any())
                throw new InvalidOperationException($"A logger with the name of '{name}' already exists.");

            _loggers.Add(this);

            _name = name;
            _logLevel = logLevel;
            _logToFileSystem = logToFileSystem;
            _logToConsole = logToConsole;
            _cutLogPrefix = cutLogPrefix;
            _logThreadId = logThreadId;
            _logWithColor = logWithColor;

            _logToConsole = _logToConsole && _terminalAvailable();

#if CONCURRENT_LOGGING_ENABLED
            _messageQueue = new();
            _dispatcherQueue = new PatchedDispatcherQueue($"Logger '{_name}' Dispatcher Queue", new(0, $"Logger '{_name}' Dispatcher"));
            Arbiter.Activate(_dispatcherQueue, Arbiter.Receive(true, _messageQueue, action => action()));            
#endif
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Public Static Getters
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a singleton instance of the Logger class.
        /// </summary>
        /// <remarks>
        ///     This is the recommended way to get a logger if you do not require a specific logger.
        /// </remarks>
        public static Logger Singleton
            => _singleton ??= new(
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerName,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLogLevel,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogToFileSystem,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogToConsole,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerCutLogPrefix,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogThreadId,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogWithColor
                );

        /// <summary>
        /// Gets a singleton instance of the Logger class that Noops on each operation.
        /// </summary>
        public static Logger NoopSingleton
           => _noopSingleton ??= new(
                   "_noop",
                   LogLevel.None,
                   false,
                   false,
                   true,
                   false,
                   false
               );


        ////////////////////////////////////////////////////////////////////////////////
        // Public Getters and Setters
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the name of the logger.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_disposed) throw new ObjectDisposedException(_name);
                if (value.IsNullOrEmpty()) throw new ArgumentNullException(nameof(value));
                if (!value.IsMatch(_loggerNameRegex))
                    throw new ArgumentException($"The logger name must match '{_loggerNameRegex}'", nameof(value));
                if (_loggers.Where(logger => logger.Name == value && logger.Name != _name).Any())
                    throw new InvalidOperationException($"A logger with the name of '{value}' already exists.");

                if (value != _name)
                    _name = value;
            }
        }

        /// <summary>
        /// Gets the log level of the logger.
        /// </summary>
        public LogLevel LogLevel
        {
            get => _logLevel;
            set
            {
                if (_disposed) throw new ObjectDisposedException(_name);
                
                if (value != _logLevel)
                    _logLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets the value that determines if this logger will log to the file system.
        /// </summary>
        public bool LogToFileSystem
        {
            get => _logToFileSystem;
            set
            {
                if (_disposed) throw new ObjectDisposedException(_name);

                if (value != _logToFileSystem)
                {
                    _logToFileSystem = value;

                    if (value == true)
                    {
                        _createFileName();
                        _createFileStream();
                    }
                    else
                    {
                        _closeFileStream();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the value that determines if this logger will log to the console.
        /// </summary>
        public bool LogToConsole
        {
            get => _logToConsole;
            set
            {
                if (_disposed) throw new ObjectDisposedException(_name);

                if (value != _logToConsole)
                    _logToConsole = value;
            }
        }

        /// <summary>
        /// Gets the value that determines if this logger will cut the log prefix.
        /// </summary>
        public bool CutLogPrefix => _cutLogPrefix;

        /// <summary>
        /// Gets the value that determines if this logger will log the thread ID.
        /// </summary>
        public bool LogThreadId
        {
            get => _logThreadId;
            set
            {
                if (_disposed) throw new ObjectDisposedException(_name);

                if (value != _logThreadId)
                    _logThreadId = value;
            }
        }

        /// <summary>
        /// Gets the value that determines if this logger will log with color.
        /// </summary>
        public bool LogWithColor
        {
            get => _logWithColor;
            set
            {
                if (_disposed) throw new ObjectDisposedException(_name);

                if (value != _logWithColor)
                    _logWithColor = value;
            }
        }

        /// <summary>
        /// Gets the log file name.
        /// </summary>
        public string FileName => _fileName;

        /// <summary>
        /// Gets the fully qualified log file name.
        /// </summary>
        public string FullyQualifiedFileName => _fullyQualifiedFileName;

        /// <summary>
        /// This property is unused.
        /// </summary>
        [Obsolete("This property is unused. Please use Logger.LogLevel.")]
        public Func<LogLevel> MaxLogLevel { get; set; }

        ////////////////////////////////////////////////////////////////////////////////
        // Public Log Methods
        ////////////////////////////////////////////////////////////////////////////////

        public void Log(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.Information)) return;

            _queueOrLogInternal(LogLevel.Information, LogColor.BrightBlue, format, args);
        }
        public void Log(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.Information)) return;

            _queueOrLogInternal(LogLevel.Information, LogColor.BrightBlue, messageGetter());
        }
        public void Debug(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.Verbose)) return;

            _queueOrLogInternal(LogLevel.Verbose, LogColor.BrightMagenta, format, args);
        }
        public void Debug(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.Verbose)) return;

            _queueOrLogInternal(LogLevel.Verbose, LogColor.BrightMagenta, messageGetter());
        }
        public void Error(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            if (!_checkLogLevel(LogLevel.Error)) return;

            _queueOrLogInternal(LogLevel.Error, LogColor.BrightRed, ex.ToDetailedString());
        }
        public void Error(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.Error)) return;

            _queueOrLogInternal(LogLevel.Error, LogColor.BrightRed, format, args);
        }
        public void Error(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.Error)) return;

            _queueOrLogInternal(LogLevel.Error, LogColor.BrightRed, messageGetter());
        }
        public void Info(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.Information)) return;

            _queueOrLogInternal(LogLevel.Information, LogColor.BrightBlue, format, args);
        }
        public void Info(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.Information)) return;

            _queueOrLogInternal(LogLevel.Information, LogColor.BrightBlue, messageGetter());
        }
        public void Warning(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.Warning)) return;

            _queueOrLogInternal(LogLevel.Warning, LogColor.BrightYellow, format, args);
        }
        public void Warning(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.Warning)) return;

            _queueOrLogInternal(LogLevel.Warning, LogColor.BrightYellow, messageGetter());
        }
        public void Trace(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.Verbose)) return;

            _queueOrLogInternal(LogLevel.Verbose, LogColor.BrightMagenta, new Exception(string.Format(format, args)).ToDetailedString());
        }
        public void Trace(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.Verbose)) return;

            _queueOrLogInternal(LogLevel.Verbose, LogColor.BrightCyan, new Exception(messageGetter()).ToDetailedString());
        }
        public void Verbose(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.Verbose)) return;

            _queueOrLogInternal(LogLevel.Verbose, LogColor.BrightCyan, format, args);
        }
        public void Verbose(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.Verbose)) return;

            _queueOrLogInternal(LogLevel.Verbose, LogColor.BrightCyan, messageGetter());
        }
        public void LifecycleEvent(string format, params object[] args)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (args == null) throw new ArgumentNullException(nameof(args));

            if (!_checkLogLevel(LogLevel.LifecycleEvent)) return;

            _queueOrLogInternal(LogLevel.LifecycleEvent, LogColor.BrightGreen, format, args);
        }
        public void LifecycleEvent(Func<string> messageGetter)
        {
            if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

            if (!_checkLogLevel(LogLevel.LifecycleEvent)) return;

            _queueOrLogInternal(LogLevel.LifecycleEvent, LogColor.BrightGreen, messageGetter());
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_name == _singleton._name) return;
            if (_name == _noopSingleton._name) return;

            _loggers.Remove(this);
            GC.SuppressFinalize(this);
            _closeFileStream();

#if CONCURRENT_LOGGING_ENABLED
            _dispatcherQueue.Dispose();
#endif

            _disposed = true;
        }
    }
}
