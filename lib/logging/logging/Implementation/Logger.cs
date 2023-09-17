namespace Logging;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

#if CONCURRENT_LOGGING_ENABLED
using System.Threading.Tasks;
#endif

#if NETFRAMEWORK
using PInvoke;
#endif

// ReSharper disable LocalizableElement
// ReSharper disable InconsistentNaming
// ReSharper disable UseStringInterpolationWhenPossible

/// <inheritdoc cref="ILogger"/>
public class Logger : ILogger, IDisposable
{
    internal class LockedFileStream : Stream, IDisposable
    {
        private readonly FileStream _lock;
        private readonly StreamWriter _writer;

        public LockedFileStream(string path, Encoding encoding = null)
        {
            encoding ??= Encoding.Default;
            this._lock = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            this._writer = new(this._lock, encoding) { AutoFlush = true };
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _lock.Length;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Write(string text) => this._writer?.Write(text);
        public override void Write(byte[] buffer, int offset, int count) => this._writer?.Write(this._writer?.Encoding.GetString(buffer).ToCharArray(), offset, count);
        public override void Flush() => this._writer?.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();

        public new void Dispose()
        {
            GC.SuppressFinalize(this);
            this._writer?.Close();
            this._writer?.Dispose();
            this._lock?.Close();
            this._lock?.Dispose();
        }

    }

    /// <summary>
    /// Represents the a color related to an Ansi Color.
    /// </summary>
    protected enum LogColor
    {
        /// <summary>
        /// \x1b[0m
        /// </summary>
        Reset,

        /// <summary>
        /// \x1b[90m
        /// </summary>
        BrightBlack,

        /// <summary>
        /// \x1b[91m
        /// </summary>
        BrightRed,

        /// <summary>
        /// \x1b[92m
        /// </summary>
        BrightGreen,

        /// <summary>
        /// \x1b[93m
        /// </summary>
        BrightYellow,

        /// <summary>
        /// \x1b[94m
        /// </summary>
        BrightBlue,

        /// <summary>
        /// \x1b[95m
        /// </summary>
        BrightMagenta,

        /// <summary>
        /// \x1b[96m
        /// </summary>
        BrightCyan,

        /// <summary>
        /// \x1b[97m
        /// </summary>
        BrightWhite,
    }

    //////////////////////////////////////////////////////////////////////////////
    // Private Static Constructor
    //////////////////////////////////////////////////////////////////////////////

    static Logger()
    {
        Console.SetOut(
            new StreamWriter(
                Console.OpenStandardOutput(),
                Encoding.ASCII
            )
            { AutoFlush = true }
        );

        Console.SetError(
            new StreamWriter(
                Console.OpenStandardError(),
                Encoding.ASCII
            )
            { AutoFlush = true }
        );

#if NETFRAMEWORK
        var stdoutHandle = Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE);
        if (Kernel32.GetConsoleMode(stdoutHandle, out var consoleBufferModes) &&
            consoleBufferModes.HasFlag(Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING))
            return;

        consoleBufferModes |= Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        Kernel32.SetConsoleMode(stdoutHandle, consoleBufferModes);

        var stderrHandle = Kernel32.GetStdHandle(Kernel32.StdHandle.STD_ERROR_HANDLE);
        if (Kernel32.GetConsoleMode(stderrHandle, out consoleBufferModes) &&
            consoleBufferModes.HasFlag(Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING))
            return;

        consoleBufferModes |= Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        Kernel32.SetConsoleMode(stderrHandle, consoleBufferModes);
#endif
    }

    //////////////////////////////////////////////////////////////////////////////
    // Private Static Readonly Properties
    //////////////////////////////////////////////////////////////////////////////

    private const string _logFilenameFormat =
#if DEBUG
        "dev_" +
#endif
        "{0}_{1}_{2}_{3}_{4}.log";

    // language=regex
    private const string _loggerNameRegex = @"^[a-zA-Z0-9_\-\.]{1,100}$";

    private static string _logFileBaseDirectoryBacking;
    private static string _defaultLogFileDirectory => Environment.GetEnvironmentVariable("DEFAULT_LOG_FILE_DIRECTORY");
    private static string _logFileBaseDirectory
    {
        get
        {
            if (!string.IsNullOrEmpty(Logger._logFileBaseDirectoryBacking))
                return Logger._logFileBaseDirectoryBacking;

            try
            {
                if (!string.IsNullOrEmpty(_defaultLogFileDirectory))
                    Logger._logFileBaseDirectoryBacking = _defaultLogFileDirectory;

                return Logger._logFileBaseDirectoryBacking = Path.DirectorySeparatorChar == '/'
                    ? Path.Combine(Path.GetTempPath(), "mfdlabs", "logs")
                    : Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "MFDLABS", "Logs");
            }
            catch
            {
                // If it fails, default to the current working directory
                return Logger._logFileBaseDirectoryBacking = Directory.GetCurrentDirectory();
            }
        }
    }

    private const string _defaultLoggerNameConstant = "logger";
    private const LogLevel _defaultLogLevelConstant = LogLevel.Information;
    private static string _defaultLoggerName => Environment.GetEnvironmentVariable("DEFAULT_LOGGER_NAME") ?? "logger";
    private static LogLevel _defaultLogLevel
    {
        get
        {
            if (!Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("DEFAULT_LOG_LEVEL"), out var level))
                return Logger._defaultLogLevelConstant;

            return level;
        }
    }

    private static readonly Process _currentProcess = Process.GetCurrentProcess();

    private static readonly string _localIp = Logger._getLocalAddress();
    private static readonly string _hostname = Dns.GetHostName();
    private static readonly string _processId = Logger._currentProcess.Id.ToString("x");
    private static readonly string _platform = Environment.OSVersion.Platform.ToString().ToLower();
    private static readonly string _architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
    private static readonly string _dotnetVersion = Environment.Version.ToString();
    private static readonly string _architectureFmt = $"{_platform}-{_architecture}";
    private static readonly string _assemblyVersion = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();

    private static readonly List<Logger> _loggers = new();

    //////////////////////////////////////////////////////////////////////////////
    // Private Static Properties
    //////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Actual singleton instance
    /// </summary>
    protected static Logger _singleton;
    private static readonly object _singletonLock = new();

    /// <summary>
    /// Actual noop singleton instance
    /// </summary>
    protected static Logger _noopSingleton;
    private static readonly object _noopSingletonLock = new();

    private static bool? _hasTerminal;

    //////////////////////////////////////////////////////////////////////////////
    // Private Readonly Properties (Only set in the constructor)
    //////////////////////////////////////////////////////////////////////////////

    private readonly bool _cutLogPrefix;

    private readonly object _fileSystemLock = new();
    private readonly object _consoleLock = new();

    //////////////////////////////////////////////////////////////////////////////
    // Private Properties
    //////////////////////////////////////////////////////////////////////////////

    private string _name;
    private LogLevel _logLevel;
    private bool _logToConsole;
    private bool _logToFileSystem;
    private bool _logThreadId;
    private bool _logWithColor;

    private Logger.LockedFileStream _lockedFileWriteStream;
    private string _fileName;
    private string _fullyQualifiedFileName;

    private string _cachedNonColorPrefix;
    private string _cachedColorPrefix;

    private bool _disposed;

    //////////////////////////////////////////////////////////////////////////////
    // Private/Protected Static Helper Methods
    //////////////////////////////////////////////////////////////////////////////

    private static bool _getAddressByInterface(NetworkInterfaceType interfaceType, out string ip)
        => !string.IsNullOrEmpty(
            ip = NetworkInterface.GetAllNetworkInterfaces()
            .Where(item => item.NetworkInterfaceType == interfaceType &&
                            item.OperationalStatus == OperationalStatus.Up)
            .Select(item => item.GetIPProperties().UnicastAddresses)
            .Select(item => item.FirstOrDefault()?.Address)
            .FirstOrDefault()?
            .ToString()
        );

    private static string _getLocalAddress()
    {
        if (Logger._getAddressByInterface(NetworkInterfaceType.Wireless80211, out var ip))
            return ip;

        if (Logger._getAddressByInterface(NetworkInterfaceType.Ethernet, out ip))
            return ip;

        Logger._getAddressByInterface(NetworkInterfaceType.Loopback, out ip);

        return ip;
    }

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

    private static string _getCurrentThreadIdFormatted() => Thread.CurrentThread.ManagedThreadId
        .ToString("x")
        .PadLeft(4, '0');
    private static string _getProcessUptime() => (DateTime.Now - Logger._currentProcess.StartTime).TotalSeconds.ToString("f7");

    private static string _getNowAsIso() => DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ");
    private static string _getFileSafeNowAsIso() => Regex.Replace(Logger._getNowAsIso(), @"[^a-z0-9_-]", "", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace("-", "");

    /// <summary>
    /// Helper for getting the ANSI color code string by the specified Logger.LogColor.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns>The ANSI prefix, defaults to \u0000m.</returns>
    protected static string _getAnsiColorByLogColor(Logger.LogColor color)
        => color switch
        {
            Logger.LogColor.Reset => "\x1b[0m",
            Logger.LogColor.BrightBlack => "\x1b[90m",
            Logger.LogColor.BrightRed => "\x1b[91m",
            Logger.LogColor.BrightGreen => "\x1b[92m",
            Logger.LogColor.BrightYellow => "\x1b[93m",
            Logger.LogColor.BrightBlue => "\x1b[94m",
            Logger.LogColor.BrightMagenta => "\x1b[95m",
            Logger.LogColor.BrightCyan => "\x1b[96m",
            Logger.LogColor.BrightWhite => "\x1b[97m",
            _ => "\x1b[0m",
        };

    /// <summary>
    /// Gets a default color section which is Bright Black.
    /// 
    /// Like [{content}].
    /// </summary>
    /// <param name="content">The content to place in the section.</param>
    /// <returns>The colored section.</returns>
    protected static string _getColorSection(object content)
        => Logger._getColorSection(Logger.LogColor.BrightBlack, content);

    /// <summary>
    /// Get a constructed section with a color to it.
    /// 
    /// Like [{content}].
    /// </summary>
    /// <param name="color">The color of the content.</param>
    /// <param name="content">The content to place in the section.</param>
    /// <returns>The colored section.</returns>
    private static string _getColorSection(Logger.LogColor color, object content)
        => string.Format(
               "[{0}{1}{2}]",
               Logger._getAnsiColorByLogColor(color),
               content,
               Logger._getAnsiColorByLogColor(Logger.LogColor.Reset)
           );

    //////////////////////////////////////////////////////////////////////////////
    // Private/Protected Helper Methods
    //////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Gets the constructed short non-color prefix.
    /// You can override if you wish to modify the prefix.
    /// 
    /// This is used if <see cref="CutLogPrefix"/> is set.
    /// </summary>
    /// <returns>The short prefix.</returns>
    protected virtual string _getConstructedShortNonColorLogPrefix()
        => string.Format(
                "[{0}][{1}][{2}]",
                Logger._localIp,
                Logger._hostname,
                this._name
            );

    /// <summary>
    /// Gets the constructed long non-color prefix.
    /// You can override if you wish to modify the prefix.
    /// 
    /// This is used if <see cref="CutLogPrefix"/> is not set.
    /// </summary>
    /// <returns>The long prefix.</returns>
    protected virtual string _getConstructedLongNonColorLogPrefix()
        => string.Format(
                "[{0}][{1}][{2}][{3}][{4}][{5}]",
                Logger._processId,
                Logger._architectureFmt,
                Logger._dotnetVersion,
                Logger._localIp,
                Logger._hostname,
                this._name
            );

    private string _getNonColorLogPrefix()
    {
        if (this._cutLogPrefix)
            return this._cachedNonColorPrefix ??= this._getConstructedShortNonColorLogPrefix();

        return this._cachedNonColorPrefix ??= this._getConstructedLongNonColorLogPrefix();
    }

    /// <summary>
    /// Gets the constructed short color prefix.
    /// You can override if you wish to modify the prefix.
    /// 
    /// This is used if <see cref="CutLogPrefix"/> is set.
    /// </summary>
    /// <returns>The short prefix.</returns>
    protected virtual string _getConstructedShortColorLogPrefix()
        => string.Format(
                "{0}{1}{2}",
                Logger._getColorSection(Logger._localIp),
                Logger._getColorSection(Logger._hostname),
                Logger._getColorSection(this._name)
            );

    /// <summary>
    /// Gets the constructed long color prefix.
    /// You can override if you wish to modify the prefix.
    /// 
    /// This is used if <see cref="CutLogPrefix"/> is not set.
    /// </summary>
    /// <returns>The long prefix.</returns>
    protected virtual string _getConstructedLongColorLogPrefix()
        => string.Format(
                "{0}{1}{2}{3}{4}{5}",
                Logger._getColorSection(Logger._processId),
                Logger._getColorSection(Logger._architectureFmt),
                Logger._getColorSection(Logger._dotnetVersion),
                Logger._getColorSection(Logger._localIp),
                Logger._getColorSection(Logger._hostname),
                Logger._getColorSection(this._name)
            );

    private string _getColorLogPrefix()
    {
        if (this._cutLogPrefix)
            return this._cachedColorPrefix ??= this._getConstructedShortColorLogPrefix();

        return _cachedColorPrefix ??= this._getConstructedLongColorLogPrefix();
    }

    /// <summary>
    /// Constructs the full non-color log message.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="format">The format string.</param>
    /// <param name="args">The args for the format.</param>
    /// <returns>The constructed string.</returns>
    protected string _constructNonColorLogMessage(LogLevel logLevel, string format, params object[] args)
    {
        var formattedMessage = args is { Length: 0 }
            ? format
            : string.Format(format, args);

        if (_cutLogPrefix)
            return string.Format(
                "[{0}]{1}{2}[{3}] {4}\n",
                Logger._getNowAsIso(),
                this._logThreadId ? $"[{Logger._getCurrentThreadIdFormatted()}]" : "",
                this._getNonColorLogPrefix(),
                logLevel.ToString().ToUpper(),
                formattedMessage
            );

        return string.Format(
            "[{0}][{1}][{2}]{3}[{4}] {5}\n",
            Logger._getNowAsIso(),
            Logger._getProcessUptime(),
            this._logThreadId ? $"[{Logger._getCurrentThreadIdFormatted()}]" : "",
            this._getNonColorLogPrefix(),
            logLevel.ToString().ToUpper(),
            formattedMessage
        );
    }

    /// <summary>
    /// Constructs the full color log message.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="color">The log color.</param>
    /// <param name="format">The format string.</param>
    /// <param name="args">The args for the format.</param>
    /// <returns>The constructed string.</returns>
    protected string _constructColorLogMessage(LogLevel logLevel, Logger.LogColor color, string format, params object[] args)
    {
        var formattedMessage = args is { Length: 0 }
            ? format
            : string.Format(format, args);

        if (_cutLogPrefix)
            return string.Format(
                "{0}{1}{2}{3} {4}{5}{6}\n",
                Logger._getColorSection(Logger._getNowAsIso()),
                this._logThreadId ? Logger._getColorSection(Logger._getCurrentThreadIdFormatted()) : "",
                this._getColorLogPrefix(),
                Logger._getColorSection(color, logLevel.ToString().ToUpper()),
                Logger._getAnsiColorByLogColor(color),
                formattedMessage,
                Logger._getAnsiColorByLogColor(LogColor.Reset)
            );

        return string.Format(
            "{0}{1}{2}{3}{4} {5}{6}{7}\n",
            Logger._getColorSection(Logger._getNowAsIso()),
            Logger._getColorSection(Logger._getProcessUptime()),
            this._logThreadId ? Logger._getColorSection(Logger._getCurrentThreadIdFormatted()) : "",
            this._getColorLogPrefix(),
            Logger._getColorSection(color, logLevel.ToString().ToUpper()),
            Logger._getAnsiColorByLogColor(color),
            formattedMessage,
            Logger._getAnsiColorByLogColor(LogColor.Reset)
        );
    }

    private void _writeLogToFileSystem(LogLevel logLevel, string format, params object[] args)
    {
        lock (this._fileSystemLock)
        {
            if (!this._logToFileSystem) return;

            if (this._lockedFileWriteStream == null)
            {
                this._createFileName();
                this._createFileStream();
            }

            try
            {
                this._lockedFileWriteStream?.Write(this._constructNonColorLogMessage(logLevel, format, args));
            }
            catch (Exception ex)
            {
                this._logToFileSystem = false;
                this._closeFileStream();

                this.Warning("Unable to write to file stream due to \"{0}\". Disabling file stream.", ex.Message);
            }
        }
    }

    private void _writeLogToConsole(LogLevel logLevel, Logger.LogColor color, string format, params object[] args)
    {
        lock (this._consoleLock)
        {
            if (!this._logToConsole) return;

            var message = this._logWithColor
                ? this._constructColorLogMessage(logLevel, color, format, args)
                : this._constructNonColorLogMessage(logLevel, format, args);

            if (logLevel == LogLevel.Error)
                Console.Error.Write(message);
            else
                Console.Out.Write(message);
        }
    }

    private void _queueOrLog(LogLevel logLevel, Logger.LogColor color, string format, params object[] args)
    {
        if (this._disposed) throw new ObjectDisposedException(this.GetType().Name);

#if CONCURRENT_LOGGING_ENABLED
        Task.Factory.StartNew(() =>
        {
            try { this._writeLog(logLevel, color, format, args); }
            catch (Exception ex)
            {
#if DEBUG
                this.Warning("Error while performing log: {0}", ex);
#endif
            }
        });
#else
        try { this._writeLog(logLevel, color, format, args); }
        catch (Exception ex)
        {
#if DEBUG
            this.Warning("Error while performing log: {0}", ex);
#endif
        }
#endif
    }

    /// <summary>
    /// Writes the log message. Override if you have custom loggers.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="color">The color.</param>
    /// <param name="format">The format.</param>
    /// <param name="args">The format arga.</param>
    protected virtual void _writeLog(LogLevel logLevel, Logger.LogColor color, string format, params object[] args)
    {
        this._writeLogToConsole(logLevel, color, format, args);
        this._writeLogToFileSystem(logLevel, format, args);
    }

    private bool _checkLogLevel(LogLevel logLevel) => this._logLevel >= logLevel;
    private void _createFileStream() => this._lockedFileWriteStream ??= new(this._fullyQualifiedFileName);
    private void _closeFileStream()
    {
        this._lockedFileWriteStream?.Dispose();
        this._lockedFileWriteStream = null;

        this._fullyQualifiedFileName = null;
        this._fileName = null;
    }

    private void _createFileName()
    {
        this._fileName ??= string.Format(
            Logger._logFilenameFormat,
            this._name,
            Logger._assemblyVersion,
            Logger._getFileSafeNowAsIso(),
            Logger._processId,
            new Random().Next(1000, 99999999)
        );

        this._fullyQualifiedFileName ??= Path.Combine(Logger._logFileBaseDirectory, this._fileName);

        if (Directory.Exists(Logger._logFileBaseDirectory)) return;

        try
        {
            Directory.CreateDirectory(Logger._logFileBaseDirectory);
        }
        catch (IOException)
        {
            // Assume it's a file.
            this._logToFileSystem = false;
            this._closeFileStream();
            this.Warning("Unable to create log file directory. It already exists and is a file.");
        }
        catch (UnauthorizedAccessException)
        {
            this._logToFileSystem = false;
            this._closeFileStream();
            this.Warning("Unable to create log file directory. Please ensure that the current user has permission to create the directory '{0}'.", Logger._logFileBaseDirectory);
        }
    }

    //////////////////////////////////////////////////////////////////////////////
    // Public Static Helper Methods
    //////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Requests that the log file directory be cleared.
    /// </summary>
    public static void TryClearLocalLog()
    {
        Logger.Singleton.Log("Try clear local log files...");

        var fileSystemLoggers = Logger._loggers.Where(logger => logger._logToFileSystem == true);

        foreach (var logger in fileSystemLoggers)
        {
            lock (logger._fileSystemLock)
            {
                logger._logToFileSystem = false;
                logger._closeFileStream();
            }
        }

        if (Directory.Exists(Logger._logFileBaseDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(Logger._logFileBaseDirectory))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                }
            }
        }

        foreach (var logger in fileSystemLoggers)
        {
            lock (logger._fileSystemLock)
            {
                logger._logToFileSystem = true;
                logger._createFileName();
                logger._createFileStream();
            }
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
            foreach (var logger in Logger._loggers.Where(logger => logger._name != Logger._singleton?._name && logger._name != Logger._noopSingleton?._name))
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
    // ReSharper disable once MemberCanBeProtected.Global
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
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        if (!Regex.IsMatch(name, Logger._loggerNameRegex))
            throw new ArgumentException($"The logger name must match '{Logger._loggerNameRegex}'", nameof(name));

        lock (Logger._loggers)
            if (Logger._loggers.Any(logger => logger.Name == name))
                throw new InvalidOperationException($"A logger with the name of '{name}' already exists.");

#if !NETFRAMEWORK
        if (logWithColor)
            logWithColor = false; // Color is not allowed on other targets as it cause a lot of issues on Windows
#endif

        lock (Logger._loggers)
            Logger._loggers.Add(this);

        this._name = name;
        this._logLevel = logLevel;
        this._logToFileSystem = logToFileSystem;
        this._logToConsole = logToConsole;
        this._cutLogPrefix = cutLogPrefix;
        this._logThreadId = logThreadId;
        this._logWithColor = logWithColor;

        this._logToConsole = this._logToConsole && Logger._terminalAvailable();
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
    {
        get
        {
            lock (Logger._singletonLock)
                return Logger._singleton ??= new(
                    _defaultLoggerName,
                    _defaultLogLevel
                );
        }
    }

    /// <summary>
    /// Gets a singleton instance of the Logger class that Noops on each operation.
    /// </summary>
    public static Logger NoopSingleton
    {
        get
        {
            lock (Logger._noopSingletonLock)
                return Logger._noopSingleton ??= new(
                   "_noop",
                   LogLevel.None,
                   false,
                   false
               );
        }
    }


    ////////////////////////////////////////////////////////////////////////////////
    // Public Getters and Setters
    ////////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="ILogger.Name"/>
    public string Name
    {
        get => this._name;
        set
        {
            if (this._disposed) throw new ObjectDisposedException(this.GetType().Name);
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            if (!Regex.IsMatch(value, Logger._loggerNameRegex))
                throw new ArgumentException($"The logger name must match '{Logger._loggerNameRegex}'", nameof(value));
            if (_loggers.Any(logger => logger.Name == value && logger.Name != this._name))
                throw new InvalidOperationException($"A logger with the name of '{value}' already exists.");

            this._name = value;
        }
    }

    /// <inheritdoc cref="ILogger.LogLevel"/>
    public LogLevel LogLevel
    {
        get => this._logLevel;
        set
        {
            if (this._disposed) throw new ObjectDisposedException(this.GetType().Name);

            this._logLevel = value;
        }
    }

    /// <inheritdoc cref="ILogger.LogToFileSystem"/>
    public bool LogToFileSystem
    {
        get => this._logToFileSystem;
        set
        {
            if (this._disposed) throw new ObjectDisposedException(this.GetType().Name);

            lock (this._fileSystemLock)
            {
                if (value == this._logToFileSystem) return;

                this._logToFileSystem = value;

                if (value)
                {
                    this._createFileName();
                    this._createFileStream();
                }
                else
                {
                    this._closeFileStream();
                }
            }
        }
    }

    /// <inheritdoc cref="ILogger.LogToConsole"/>
    public bool LogToConsole
    {
        get => this._logToConsole;
        set
        {
            if (this._disposed) throw new ObjectDisposedException(this.GetType().Name);

            lock (this._consoleLock)
                this._logToConsole = value;
        }
    }

    /// <inheritdoc cref="ILogger.CutLogPrefix"/>
    public bool CutLogPrefix => this._cutLogPrefix;

    /// <inheritdoc cref="ILogger.LogThreadId"/>
    public bool LogThreadId
    {
        get => this._logThreadId;
        set
        {
            if (this._disposed) throw new ObjectDisposedException(this.GetType().Name);

            this._logThreadId = value;
        }
    }

    /// <inheritdoc cref="ILogger.LogWithColor"/>
    public bool LogWithColor
    {
        get => this._logWithColor;
        set
        {
            if (this._disposed) throw new ObjectDisposedException(this.GetType().Name);

            this._logWithColor = value;
        }
    }

    /// <inheritdoc cref="ILogger.FileName"/>
    public string FileName => this._fileName;

    /// <inheritdoc cref="ILogger.FullyQualifiedFileName"/>
    public string FullyQualifiedFileName => this._fullyQualifiedFileName;


    ////////////////////////////////////////////////////////////////////////////////
    // Public Log Methods
    ////////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="ILogger.Log(string, object[])"/>
    public void Log(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        if (args == null) throw new ArgumentNullException(nameof(args));

        if (!this._checkLogLevel(LogLevel.Information)) return;

        this._queueOrLog(LogLevel.Information, Logger.LogColor.BrightWhite, format, args);
    }

    /// <inheritdoc cref="ILogger.Log(Func{string})"/>
    public void Log(Func<string> messageGetter)
    {
        if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

        if (!this._checkLogLevel(LogLevel.Information)) return;

        this._queueOrLog(LogLevel.Information, Logger.LogColor.BrightWhite, messageGetter());
    }

    /// <inheritdoc cref="ILogger.Warning(string, object[])"/>
    public void Warning(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        if (args == null) throw new ArgumentNullException(nameof(args));

        if (!this._checkLogLevel(LogLevel.Warning)) return;

        this._queueOrLog(LogLevel.Warning, Logger.LogColor.BrightYellow, format, args);
    }

    /// <inheritdoc cref="ILogger.Warning(Func{string})"/>
    public void Warning(Func<string> messageGetter)
    {
        if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

        if (!this._checkLogLevel(LogLevel.Warning)) return;

        this._queueOrLog(LogLevel.Warning, Logger.LogColor.BrightYellow, messageGetter());
    }

    /// <inheritdoc cref="ILogger.Trace(string, object[])"/>
    public void Trace(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        if (args == null) throw new ArgumentNullException(nameof(args));

        if (!this._checkLogLevel(LogLevel.Trace)) return;

        var formattedMessage = args is { Length: 0 }
            ? format
            : string.Format(format, args);

        var message = string.Format("{0}\n{1}", formattedMessage, Environment.StackTrace);

        this._queueOrLog(LogLevel.Trace, Logger.LogColor.BrightMagenta, message);
    }

    /// <inheritdoc cref="ILogger.Trace(Func{string})"/>
    public void Trace(Func<string> messageGetter)
    {
        if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

        if (!this._checkLogLevel(LogLevel.Trace)) return;

        var message = string.Format("{0}\n{1}", messageGetter(), Environment.StackTrace);

        this._queueOrLog(LogLevel.Trace, Logger.LogColor.BrightMagenta, message);
    }

    /// <inheritdoc cref="ILogger.Debug(string, object[])"/>
    public void Debug(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        if (args == null) throw new ArgumentNullException(nameof(args));

        if (!this._checkLogLevel(LogLevel.Debug)) return;

        this._queueOrLog(LogLevel.Debug, Logger.LogColor.BrightMagenta, format, args);
    }

    /// <inheritdoc cref="ILogger.Debug(Func{string})"/>
    public void Debug(Func<string> messageGetter)
    {
        if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

        if (!this._checkLogLevel(LogLevel.Debug)) return;

        this._queueOrLog(LogLevel.Debug, Logger.LogColor.BrightMagenta, messageGetter());
    }

    /// <inheritdoc cref="ILogger.Information(string, object[])"/>
    public void Information(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        if (args == null) throw new ArgumentNullException(nameof(args));

        if (!this._checkLogLevel(LogLevel.Information)) return;

        this._queueOrLog(LogLevel.Information, Logger.LogColor.BrightBlue, format, args);
    }

    /// <inheritdoc cref="ILogger.Information(Func{string})"/>
    public void Information(Func<string> messageGetter)
    {
        if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

        if (!this._checkLogLevel(LogLevel.Information)) return;

        this._queueOrLog(LogLevel.Information, Logger.LogColor.BrightBlue, messageGetter());
    }

    /// <inheritdoc cref="ILogger.Error(Exception)"/>
    public void Error(Exception ex)
    {
        if (ex == null) throw new ArgumentNullException(nameof(ex));

        if (!this._checkLogLevel(LogLevel.Error)) return;

        this._queueOrLog(LogLevel.Error, Logger.LogColor.BrightRed, ex.ToString());
    }

    /// <inheritdoc cref="ILogger.Error(string, object[])"/>
    public void Error(string format, params object[] args)
    {
        if (format == null) throw new ArgumentNullException(nameof(format));
        if (args == null) throw new ArgumentNullException(nameof(args));

        if (!this._checkLogLevel(LogLevel.Error)) return;

        this._queueOrLog(LogLevel.Error, Logger.LogColor.BrightRed, format, args);
    }

    /// <inheritdoc cref="ILogger.Error(Func{string})"/>
    public void Error(Func<string> messageGetter)
    {
        if (messageGetter == null) throw new ArgumentNullException(nameof(messageGetter));

        if (!this._checkLogLevel(LogLevel.Error)) return;

        this._queueOrLog(LogLevel.Error, Logger.LogColor.BrightRed, messageGetter());
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (this._disposed) return;

        GC.SuppressFinalize(this);

        if (this._name == Logger._singleton?._name) return;
        if (this._name == Logger._noopSingleton?._name) return;

        lock (Logger._loggers)
            Logger._loggers.Remove(this);
        this._closeFileStream();

        this._disposed = true;
    }
}
