#define GRID_BOT

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Octokit;
using Octokit.GraphQL;

using Microsoft.Win32;

using Logging;

using MFDLabs.FileSystem;
using MFDLabs.Diagnostics;
using MFDLabs.Text.Extensions;
using MFDLabs.Threading.Extensions;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Configuration.Extensions;

// ReSharper disable InconsistentNaming
// ReSharper disable EmptyGeneralCatchClause
// ReSharper disable InvalidXmlDocComment

/** TODO: Report to Infrastructure that node deployed this App **/
/** TODO: Expose some API to force Rollbacks and Force Deployments? **/
/** TODO: Rate limit detection for not just the REST API but also the GraphQL API **/
/** TODO: Store some stuff in the registry like skipped version. **/

namespace MFDLabs.Grid.AutoDeployer.Service
{
    internal static class AutoDeployerService
    {
        private class MinimalRelease
        {
            public class MinimalReleaseAsset
            {
                public string Name { get; set; }
                public string Url { get; set; }
            }

            public string TagName { get; set; }
            public string Name { get; set; }
            public bool Draft { get; set; }
            public bool Prerelease { get; set; }
            public List<MinimalReleaseAsset> Assets { get; set; }

            public string NextPageCursor { get; set; }
        }

        // language=regex
        private const string DeploymentIdRegex = @"(?<app_prefix>(?<app_prefix_name>[a-zA-Z0-9\-_\.]{1,500})_)?(?<version_string>(?<date>(?<year>\d{4,})\.(?<month>\d{2}).(?<day>\d{2}))-(?<time>(?<hour>\d{2}).(?<minute>\d{2}).(?<second>\d{2}))_(?<branch>[a-zA-Z0-9\-_]{1,255})_(?<commit_sha>[a-f0-9]{7}))_?(?<dotnet_configuration>(?<dotnet_version>(?i)(?<dotnet_brand>net(?i)(?<dotnet_extended_brand>standard|coreapp)?)(?<dotnet_version_id>(?<dotnet_version_id_right>[0-9]{1,3}\.?){1,2}))-(?i)(?<dotnet_build_configuration>release|debug)(?i)(?<config_archive_extended_text>config(?i)(uration)?)?)?";

        // language=regex
        /// <summary>
        /// \[(?i)(deploy)\]
        /// <br/>
        /// Matches the following string and its variants: [DEPLOY]
        /// </summary>
        private const string ShouldDeployPreReleaseSearchString = @"\[(?i)(deploy)\]";
        private const string UserAgent = "mfdlabs-github-api-client";
        private const string DeploymentMarkerFilename = ".mfdlabs-deployment-marker";

        private static User _githubUser;
        private static GitHubClient _gitHubClient;
        private static Octokit.GraphQL.Connection _gitHubQLClient;
        private static RegistryKey _versioningRegKey;
        private static FileLock _markerLock;

        private static bool _isRunning;
        private static string _cachedVersion;
        private static readonly List<string> _skippedVersions = new();
        private static readonly CancellationTokenSource _stopSignal = new();
        private static ILogger _logger;

        #region "Constants"

        private static string _versioningRegSubKey;
        private static string _versioningRegVersionKeyName;
        private static string _deploymentPath;
        private static string _githubOrgOrAccountName;
        private static string _githubRepositoryName;
        private static string _githubToken;
        private static string _primaryDeploymentExecutable;
        private static string _deploymentAppName;
        private static TimeSpan _pollingInterval;
        private static TimeSpan _skippedVersionsInvalidationInterval;

        #endregion

        public static void Stop()
        {
            _logger.Information("Stopping...");

            _isRunning = false;
            _versioningRegKey?.Dispose();
            _markerLock?.Dispose();
            _stopSignal?.Cancel();
        }

        public static void Start(ILogger logger)
        {
            _logger = logger;

            try { Work(); }
            catch (ArgumentException ex) { _logger.Error(ex.Message); Stop(); }
            catch (Exception ex) { _logger.Error(ex); Stop(); }
        }

        private static bool CachedVersionExistsRemotely(out bool wasRatelimited, out TimeSpan? retryAfter)
        {
            wasRatelimited = false;
            retryAfter = null;

            try
            {
                return _gitHubClient.Repository.Release.Get(
                    owner: _githubOrgOrAccountName,
                    name: _githubRepositoryName,
                    tag: _cachedVersion
                ).Sync() != null;
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or (HttpStatusCode)429 or HttpStatusCode.Forbidden)
            {
                if (ex.StatusCode is HttpStatusCode.NotFound) { wasRatelimited = false; return false; }

                // Determine the amount of time we should wait before trying again
                var rateLimitRemaining = ex.HttpResponse.Headers
                    .FirstOrDefault(h => h.Key == "X-RateLimit-Remaining")
                    .Value?.ToInt32();

                if (rateLimitRemaining != 0) return false;
                wasRatelimited = true;

                var rateLimitReset = ex.HttpResponse.Headers
                    .FirstOrDefault(h => h.Key == "X-RateLimit-Reset")
                    .Value?.ToInt32();

                if (rateLimitReset == null) return false;

                var rateLimitResetDateTime = DateTimeOffset.FromUnixTimeSeconds(rateLimitReset.Value);

                retryAfter = rateLimitResetDateTime.Subtract(DateTimeOffset.UtcNow);

                return false;

            }
            /* Last cases for this, we assume it is either timeout or 500. */
            catch (Exception ex) when (ex is HttpRequestException or ApiException)
            {
                _logger.Warning(ex.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }

        private static DateTime GetDateTimeFromVersionString(this string str)
        {
            if (str == null) return DateTime.MinValue;

            var groups = str.Match(DeploymentIdRegex).Groups;
            if (groups == null)
                throw new ArgumentException($"Invalid version string: {str}");

            var year = groups["year"].Value.ToInt32();
            var month = groups["month"].Value.ToInt32();
            var day = groups["day"].Value.ToInt32();
            var hour = groups["hour"].Value.ToInt32();
            var minute = groups["minute"].Value.ToInt32();
            var second = groups["second"].Value.ToInt32();

            return new DateTime(year, month, day, hour, minute, second);
        }

        private static void Work()
        {
            _versioningRegSubKey = global::Grid.AutoDeployer.Properties.Settings.Default.VersioningRegistrySubKey;
            _versioningRegVersionKeyName = global::Grid.AutoDeployer.Properties.Settings.Default.VersioningRegistryVersionKeyName;
            _deploymentPath = global::Grid.AutoDeployer.Properties.Settings.Default.DeploymentPath;
            _githubOrgOrAccountName = global::Grid.AutoDeployer.Properties.Settings.Default.GithubAccountOrOrganizationName;
            _githubRepositoryName = global::Grid.AutoDeployer.Properties.Settings.Default.GithubRepositoryName;
            _primaryDeploymentExecutable = global::Grid.AutoDeployer.Properties.Settings.Default.DeploymentPrimaryExecutableName;
            _pollingInterval = global::Grid.AutoDeployer.Properties.Settings.Default.PollingInterval;
            _skippedVersionsInvalidationInterval = global::Grid.AutoDeployer.Properties.Settings.Default.SkippedVersionInvalidationInterval;
            _deploymentAppName = global::Grid.AutoDeployer.Properties.Settings.Default.DeploymentAppName?.ToLower();

#if !DEBUG
            if (_pollingInterval < TimeSpan.FromSeconds(30))
                throw new ArgumentException("The Polling interval cannot be less than 30 seconds.");
#endif

            if (_skippedVersionsInvalidationInterval < TimeSpan.FromMinutes(1))
                throw new ArgumentException("The Skipped Version Invalidation interval cannot be less than 1 minute.");

            if (_deploymentPath.IsNullOrEmpty())
                throw new ArgumentException("The Deployment Path cannot be null or empty.");

            if (_primaryDeploymentExecutable.IsNullOrEmpty())
                throw new ArgumentException("The Primary Deployment Executable cannot be null or empty.");

            if (_deploymentAppName.IsNullOrEmpty())
                throw new ArgumentException("The Deployment App Name cannot be null or empty.");

            if (!_deploymentPath.IsValidPath())
                throw new ArgumentException("The Deployment Path is not a valid fileSystem path.");

            if (_versioningRegSubKey.IsNullOrEmpty())
                throw new ArgumentException("The Versioning Registry Sub Key cannot be null or empty.");

            if (_versioningRegVersionKeyName.IsNullOrEmpty())
                throw new ArgumentException("The Versioning Registry Version Key Name cannot be null or empty.");

            if (_githubOrgOrAccountName.IsNullOrEmpty())
                throw new ArgumentException("The Github Organiztion Or Account name cannot be null or empty.");

            if (_githubRepositoryName.IsNullOrEmpty())
                throw new ArgumentException("The Github Repository name cannot be null or empty.");

            _versioningRegKey = Registry.LocalMachine.OpenSubKey(_versioningRegSubKey, true);
            _cachedVersion = _versioningRegKey?.GetValue(_versioningRegVersionKeyName, null) as string;

            if (_cachedVersion != null)
                _logger.Information("Got version {0} from registry key HKLM:{1}.{2}", _cachedVersion, _versioningRegSubKey, _versioningRegVersionKeyName);

            _githubToken = global::Grid.AutoDeployer.Properties.Settings.Default.GithubToken.FromEnvironmentExpression<string>();
            var gheUrl = global::Grid.AutoDeployer.Properties.Settings.Default.GithubEnterpriseUrl;

            if (_githubToken.IsNullOrEmpty())
                throw new ArgumentException("The Github Token cannot be null or empty.");

            var ghApiPhv = new Octokit.ProductHeaderValue(UserAgent);
            var ghQlPhv = new Octokit.GraphQL.ProductHeaderValue(UserAgent);

            if (gheUrl.IsNullOrEmpty())
            {
                _gitHubClient = new(ghApiPhv);
                _gitHubQLClient = new(ghQlPhv, _githubToken);
            }
            else
            {
                var uri = new Uri(gheUrl);

                _gitHubClient = new(ghApiPhv, uri);
                _gitHubQLClient = new(ghQlPhv, new($"{uri.Scheme}://{uri.Host}/graphql"), _githubToken);
            }

            _gitHubClient.Credentials = new(_githubToken);

            _githubUser = _gitHubClient.User.Current().SyncOrDefault();

            if (_githubUser == null)
                throw new ArgumentException($"Unable to authenticate to '{_gitHubClient.BaseAddress}' with token '{_githubToken}'");

            // Technically Rest API is not required, but we only use it for authentication.

            _logger.Debug("Authenticated to Rest API '{0}' as '{1}'.", _gitHubClient.BaseAddress, _githubUser.Email);
            _logger.Debug("Authenticated to GraphQL API '{0}' as '{1}'. We are setup to fetch releases from '{2}/{3}'", _gitHubQLClient.Uri, _githubUser.Email, _githubOrgOrAccountName, _githubRepositoryName);

            _isRunning = true;

            BackgroundWork(Run);
            BackgroundWork(SkippedVersionInvalidationWork);

            try
            {
                Task.Delay(-1, _stopSignal.Token).Wait();
            }
            catch (TaskCanceledException) {} // Ignore
            catch (AggregateException ex) when (ex.InnerExceptions.Any(e => e is TaskCanceledException) || ex.InnerException is TaskCanceledException) {} // Ignore
        }

        private static void SkippedVersionInvalidationWork()
        {
            _logger.Information("Starting Skipped Version Invalidation Thread...");

            // Invalidates the _skippedVersions list.
            // This is in case a user updates a release by fixing an error, or marks a pre-release for deployment.
            while (_isRunning)
            {
                Thread.Sleep(_skippedVersionsInvalidationInterval);
                _skippedVersions.Clear();
            }
        }

        private static void Run()
        {
            try
            {
                SetupRegistry();
                SetupDeploymentPath();
                _markerLock = new FileLock(Path.Combine(_deploymentPath, DeploymentMarkerFilename));
                _markerLock.Lock();
                PurgeCachedVersionIfNoLongerExists(out _, out _);
                DetermineLatestVersion();
                LaunchIfNotRunning();

                while (_isRunning)
                {
                    if (DetermineIfNewReleaseAvailable(out var r))
                    {
                        // We have the release, it should have the following files:
                        // A .mfdlabs-config-archive file -> This file contains .config files
                        // A .Unpacker.ps1 file -> This file runs a 7Zip command that unpacks the contents to the archives into the directory it's in.
                        // A .Unpacker.bat file -> This file is a wrapper to the .ps1 file.
                        // A .mfdlabs-archive file -> Contains everything else other than the .config files.
                        // There should only be 4 assets in the release, and each of these should match the deployment id regex and have any of those extensions at the end.
                        if (r.Assets.Count != 4)
                        {
                            SkipVersion(r.TagName);
                            _logger.Warning("Skipping '{0}' because it had more or less than 4 assets.", r.TagName);
                            goto SLEEP;
                        }

                        var regex = $@"{r.TagName}_?(((?i)(net(?i)(standard|coreapp)?)(([0-9]{{1,3}}\.?){{1,2}}))-(?i)(release|debug)(?i)(config(?i)(uration)?)?)?(?i)(\.mfdlabs-(config-)?archive|Unpacker\.(ps1|bat))?";

                        foreach (var a in r.Assets)
                        {
                            if (!a.Name.IsMatch(regex))
                            {
                                SkipVersion(r.TagName);
                                _logger.Warning("Skipping '{0}' because the asset '{1}' didn't match '{2}'.", r.TagName, a.Name, regex);
                                goto SLEEP;
                            }
                        }

#if GRID_BOT
                        if (ProcessHelper.GetProcessByName(_primaryDeploymentExecutable.ToLower().Replace(".exe", ""), out _))
                        {
                            InvokeMaintenanceCommandOnPreviousExe(r.TagName);
                            _logger.Warning("Invoked upgrade message onto bot, sleep for 15 seconds to ensure it receives it.");
                            Thread.Sleep(TimeSpan.FromSeconds(15));
                        }
#endif

                        foreach (var a in r.Assets)
                        {
                            var fqp = Path.Combine(_deploymentPath, a.Name);
                            DownloadArtifact(a.Name, a.Url, fqp);
                        }

                        if (!RunUnpacker(r, out var versionDeploymentPath))
                        {
                            CleanupArtifacts(r.Assets.ToArray());
                            try { Directory.Delete(versionDeploymentPath, true); } catch { }
                            try { Directory.Delete(Path.Combine(_deploymentPath, r.TagName), true); } catch { }
                            SkipVersion(r.TagName);
                            goto SLEEP;
                        }
                        CleanupArtifacts(r.Assets.ToArray());

                        var primaryExe = Path.Combine(versionDeploymentPath, _primaryDeploymentExecutable);

                        if (!File.Exists(primaryExe))
                        {
                            SkipVersion(r.TagName);
                            versionDeploymentPath.PollDeletionBlocking();
                            _logger.Error("Unable to deploy version '{0}': The file '{1}' was not found.", r.TagName, primaryExe);
                            goto SLEEP;
                        }

                        KillAllProcessByNameSafe(_primaryDeploymentExecutable);
                        StartNewProcess(primaryExe, versionDeploymentPath);

                        _cachedVersion = r.TagName;
                        WriteVersionToRegistry();
                    }

SLEEP:
                    Thread.Sleep(_pollingInterval);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Stop();
            }
        }

        private static void PurgeCachedVersionIfNoLongerExists(out bool remoteVersionCheckWasRatelimited, out TimeSpan? retryAfter)
        {
            remoteVersionCheckWasRatelimited = false;
            retryAfter = null;

            if (_cachedVersion == null || (CachedVersionExistsOnSystem() && CachedVersionExistsRemotely(out remoteVersionCheckWasRatelimited, out retryAfter)))
                return;

            _logger.Warning("Unkown version '{0}' found in registry key HKLM:{1}.{2}.", _cachedVersion, _versioningRegSubKey, _versioningRegVersionKeyName);
            _cachedVersion = null;
            _versioningRegKey.DeleteValue(_versioningRegVersionKeyName);
        }

        private static bool CachedVersionExistsOnSystem()
        {
            if (_cachedVersion == null) return false;
            return (from f in Directory.EnumerateDirectories(_deploymentPath) where f.Contains(_cachedVersion) select f).Any();
        }

        private static void LaunchIfNotRunning()
        {
            if (_cachedVersion == null) return;
            if (ProcessHelper.GetProcessByName(_primaryDeploymentExecutable.ToLower().Replace(".exe", ""), out _)) return;

            var fqn = (from f in Directory.EnumerateDirectories(_deploymentPath) where f.Contains(_cachedVersion) select f).FirstOrDefault();
            if (fqn == null) return;

            var versionDeploymentPath = Path.Combine(_deploymentPath, fqn);
            var primaryExe = Path.Combine(versionDeploymentPath, _primaryDeploymentExecutable);
            StartNewProcess(primaryExe, versionDeploymentPath);
        }

        private static void StartNewProcess(string primaryExe, string workingDirectory)
        {
            var proc = new Process();
            proc.StartInfo.FileName = "powershell.exe";
            proc.StartInfo.Arguments = $"-ExecutionPolicy Unrestricted -Command \"Start-Process '{primaryExe}' -WindowStyle Maximized\"";
            proc.StartInfo.WorkingDirectory = workingDirectory;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            if (SystemGlobal.ContextIsAdministrator())
                proc.StartInfo.Verb = "runas";
            proc.Start();
        }

        private static void KillAllProcessByNameSafe(string name)
        {
            if (!ProcessHelper.GetProcessByName(name.ToLower().Replace(".exe", ""), out var pr))
            {
                _logger.Warning("The process '{0}' is not running, ignoring...", name);
                return;
            }

            if (!SystemGlobal.ContextIsAdministrator() && pr.IsElevated())
            {
                _logger.Warning("The process '{0}' is running on a higher context than the current process, ignoring...", name);
                return;
            }

            KillAllProcessByName(name);

            _logger.Information("Successfully closed process '{0}'.", name);
        }

        private static void KillAllProcessByName(string name)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/f /t /im {name}"
            };

            if (SystemGlobal.ContextIsAdministrator())
                psi.Verb = "runas";

            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();
            proc.WaitForExit();
        }

#if GRID_BOT
        private static void InvokeMaintenanceCommandOnPreviousExe(string tagName)
        {
            BackgroundWork(() =>
            {
                try
                {
                    // the grid bot's tcp invoker port is always 47001
                    using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sock.SendTo(Encoding.ASCII.GetBytes(tagName), new IPEndPoint(IPAddress.Loopback, 47001));
                }
                catch { }
            });
        }
#endif


        private static void CleanupArtifacts(IEnumerable<MinimalRelease.MinimalReleaseAsset> assets)
        {
            foreach (var asset in assets)
            {
                var fqp = Path.Combine(_deploymentPath, asset.Name);
                _logger.Debug("Deleting asset '{0}' from '{1}'", asset.Name, _deploymentPath);
                fqp.PollDeletionBlocking();
            }
        }

        private static bool RunUnpacker(MinimalRelease release, out string versionDeploymentPath)
        {
            var unpackerFile = Path.Combine(
                _deploymentPath,
                (
                    from f in release.Assets
                    where f.Name.IsMatch(
                        $@"{release.TagName}_?(((?i)(net(?i)(standard|coreapp)?)(([0-9]{{1,3}}\.?){{1,2}}))-(?i)(release|debug)(?i)(config(?i)(uration)?)?)?\.Unpacker\.ps1"
                    )
                    select f
                ).FirstOrDefault()?.Name!
            );

            versionDeploymentPath = unpackerFile.Replace(".Unpacker.ps1", "");

            if (!File.Exists(unpackerFile))
            {
                _logger.Error("Unkown unpacker script: {0}. Skipping version...", unpackerFile);
                return false;
            }

            var proc = new Process();
            proc.StartInfo.FileName = "powershell.exe";
            proc.StartInfo.Arguments = $"-ExecutionPolicy Unrestricted \"{unpackerFile}\"";
            proc.StartInfo.WorkingDirectory = _deploymentPath;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            if (SystemGlobal.ContextIsAdministrator())
                proc.StartInfo.Verb = "runas";
            proc.Start();
            proc.WaitForExit(20000);

            if (proc.ExitCode != 0)
            {
                _logger.Error("Unpacker script '{0}' failed with exit code '{1}'. Skipping version...", unpackerFile, proc.ExitCode);
                return false;
            }

            if (!Directory.Exists(versionDeploymentPath))
            {
                _logger.Error("Unpacker script '{0}' failed: The output deployment was not created at '{1}'. Skipping version...", unpackerFile, versionDeploymentPath);
                return false;
            }

            return true;
        }

        private static void DownloadArtifact(string name, string uri, string outputPath)
        {
            using var webClient = new WebClient();
            var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            webClient.DownloadProgressChanged += (_, e) =>
            {
                // Only log progress every 5%
                if (e.ProgressPercentage % 5 != 0) return;

                var kibRead = e.BytesReceived / 1024f;
                var kibTotal = e.TotalBytesToReceive / 1024f;

                _logger.Debug("Downloading {0} {1}% ({2}KiB/{3}KiB)...", name, e.ProgressPercentage, kibRead, kibTotal);
            };

            webClient.DownloadFileCompleted += (_, e) =>
            {
                if (e.Error != null)
                {
                    _logger.Error("Failed to download '{0}': {1}", uri, e.Error.Message);
                    waitHandle.Set();
                    waitHandle.Dispose();
                    return;
                }

                waitHandle.Set();
                waitHandle.Dispose();
            };

            webClient.Headers.Add("User-Agent", UserAgent);
            webClient.Headers.Add("Authorization", $"token {_githubToken}"); // If using GraphQL API, this is not needed as it gives you the raw S3 URL
            webClient.Headers.Add("Accept", "application/octet-stream");
            webClient.DownloadFileAsync(new(uri), outputPath);

            waitHandle.WaitOne();
        }

        private static void SkipVersion(string version)
        {
            _logger.Warning("Skipping version '{0}'...", version);

            _skippedVersions.Add(version);
        }

        private static bool DetermineIfNewReleaseAvailable(out MinimalRelease latestRelease)
        {
            try
            {
                string nextPageCursor = null;

                while (true)
                {
                    latestRelease = null;

                    PurgeCachedVersionIfNoLongerExists(out var wasRatelimited, out var retryAfter);

                    if (wasRatelimited && retryAfter.HasValue)
                    {
                        _logger.Warning("Ratelimited by GitHub API. Yield for {0} seconds...", retryAfter);
                        Thread.Sleep(retryAfter.Value);
                        return false;
                    }

                    var releasesQuery = (
                        from release in
                            new Query()
                                .Repository(
                                    name: _githubRepositoryName,
                                    owner: _githubOrgOrAccountName,
                                    followRenames: true
                                )
                                .Releases(
                                    first: 1,
                                    orderBy: new Octokit.GraphQL.Model.ReleaseOrder
                                    {
                                        Field = Octokit.GraphQL.Model.ReleaseOrderField.CreatedAt,
                                        Direction = Octokit.GraphQL.Model.OrderDirection.Desc
                                    },
                                    after: nextPageCursor
                                )
                                select from node in release.Nodes
                                    select new MinimalRelease
                                    {
                                        Name = node.Name,
                                        TagName = node.TagName,
                                        Draft = node.IsDraft,
                                        Prerelease = node.IsPrerelease,
                                        NextPageCursor = release.PageInfo.EndCursor,
                                        Assets = (from asset in
                                            node.ReleaseAssets(
                                                4,
                                                null,
                                                null,
                                                null,
                                                null
                                            )
                                            select from assetNode in
                                                asset.Nodes
                                                select new MinimalRelease.MinimalReleaseAsset
                                                {
                                                    Name = assetNode.Name,
                                                    Url = assetNode.Url
                                                }).ToList()
                                            }).Compile();

                    var releases = _gitHubQLClient
                        .Run(releasesQuery)
                        .Sync()
                        .ToList();

                    if (!releases.Any()) return false;

                    latestRelease = releases.FirstOrDefault();
                    if (latestRelease == null) return false;
                    var latestReleaseVersion = latestRelease.TagName;

                    if (latestRelease.Draft)
                    {
                        nextPageCursor = latestRelease.NextPageCursor;
                        continue;
                    }
                    if (latestReleaseVersion == _cachedVersion && CachedVersionExistsOnSystem()) return false;
                    if (!latestReleaseVersion.IsMatch(DeploymentIdRegex, RegexOptions.Compiled))
                    {
                        nextPageCursor = latestRelease.NextPageCursor;
                        continue;
                    }
                    if (_skippedVersions.Contains(latestReleaseVersion))
                    {
                        nextPageCursor = latestRelease.NextPageCursor;
                        continue;
                    }

                    var isFirstPagedItem = nextPageCursor == null; // empty is last page

                    var cachedVersionDate = _cachedVersion.GetDateTimeFromVersionString().Ticks;
                    var newVersionDate = latestReleaseVersion.GetDateTimeFromVersionString().Ticks;

                    // is rollback
                    var isOldRelease = newVersionDate < cachedVersionDate;
                    var isRollback = isOldRelease && isFirstPagedItem;

                    if (isOldRelease && !isRollback)
                    {
                        // Skip this release and ignore the rest of the page
                        _logger.Warning("{0} is older than the current version {1} but was not marked for roll-back. Skipping...", latestReleaseVersion, _cachedVersion);
                        SkipVersion(latestReleaseVersion);
                        return false;
                    }

                    if (latestRelease.Prerelease)
                    {
                        if (!latestRelease.Name.IsMatch(ShouldDeployPreReleaseSearchString, RegexOptions.Compiled | RegexOptions.IgnoreCase))
                        {
                            _logger.Warning("{0} was marked as prerelease but had no deploy override text in the title, skipping...", latestReleaseVersion);
                            SkipVersion(latestReleaseVersion);
                            nextPageCursor = latestRelease.NextPageCursor;
                            continue;
                        }

                        _logger.Warning("{0} IS MARKED TO DEPLOY BUT IS PRE-RELEASE, THIS COULD POTENTIALLY BE A DANGEROUS DEPLOYMENT!", latestReleaseVersion);
                    }

                    var groups = latestRelease.Name.Match(DeploymentIdRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase).Groups;
                    var appNameGroup = groups["app_prefix_name"];

                    if (appNameGroup.Success && !appNameGroup.Value.IsMatch(_deploymentAppName, RegexOptions.Compiled | RegexOptions.IgnoreCase))
                    {
                        _logger.Warning("'{0}' was marked as for app '{1}' but we are deploying for app '{2}', skipping...", latestReleaseVersion, appNameGroup.Value, _deploymentAppName);
                        SkipVersion(latestReleaseVersion);
                        nextPageCursor = latestRelease.NextPageCursor;
                        continue;
                    }

                    // We assume no app prefix means the app name is the same as the release name

                    _logger.Debug("Got new release from repository {0}/{1}: {2}. Is Pre-Release: {3} Is Old-Release: {4} Is Rollback: {5}", _githubOrgOrAccountName, _githubRepositoryName, latestReleaseVersion, latestRelease.Prerelease, isOldRelease, isRollback);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                latestRelease = null;

                return false;
            }
        }

        private static void DetermineLatestVersion()
        {
            if (_cachedVersion != null) return;

            // There may be deployments in here already.
            // .NET deployments are normally in the format:
            // yyyy.MM.dd-hh.mm.ss_{branch}_{gitShortHash}-{targetFramework}-{configuration}

            var directoryMatches = (from f in Directory.EnumerateDirectories(_deploymentPath)
                                    let match = f.Match(DeploymentIdRegex, RegexOptions.Compiled)
                                    where match.Success
                                    select match).ToList();

            if (!directoryMatches.Any())
            {
                _logger.Warning("Not overwriting registry version as there was no deployments found at path '{0}'.", _deploymentPath);
                return;
            }

            // please look at the regex docs at the top of this file
            var sortedDirectories = from m in directoryMatches
                                    let year = m.Groups["year"].Value.ToInt32()
                                    let month = m.Groups["month"].Value.ToInt32()
                                    let day = m.Groups["day"].Value.ToInt32()
                                    let hour = m.Groups["hour"].Value.ToInt32()
                                    let minute = m.Groups["minute"].Value.ToInt32()
                                    let second = m.Groups["second"].Value.ToInt32()
                                    orderby new DateTime(year, month, day, hour, minute, second).Ticks descending
                                    select m;

            var currentDeployment = sortedDirectories.FirstOrDefault();

            if (currentDeployment == null) return;

            _cachedVersion = currentDeployment.Groups["version_string"].Value;
            _logger.Information("Got version from directories: {0}. Updating registry.", _cachedVersion);

            WriteVersionToRegistry();
        }

        private static void WriteVersionToRegistry()
        {
            if (_cachedVersion == null) return;
            if (_versioningRegKey == null) SetupRegistry();

            if (GetVersionFromRegistry() == _cachedVersion) return;

            _versioningRegKey?.SetValue(_versioningRegVersionKeyName, _cachedVersion);
        }

        private static string GetVersionFromRegistry() => _versioningRegKey?.GetValue(_versioningRegVersionKeyName, null) as string;
        private static void SetupRegistry() => _versioningRegKey ??= Registry.LocalMachine.CreateSubKey(_versioningRegSubKey, true);

        private static void SetupDeploymentPath()
        {
            var deploymentPath = _deploymentPath;

            var markerFileName = Path.Combine(deploymentPath, DeploymentMarkerFilename);

            if (Directory.Exists(deploymentPath))
            {
                // There is normally a file in here that marks this as reserved to this tool.
                if (File.Exists(markerFileName)) return;
            }

            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            if (winDir.IsSubDir(deploymentPath))
            {
                _logger.Error("Cannot create deployment directory as it is reserved by Windows.");
                Environment.Exit(1);
            }

            if (File.Exists(deploymentPath))
            {
                _logger.Error("Unable to determine deployment directory, it exists already and is a file.");
                Environment.Exit(1);
            }

            if (!global::Grid.AutoDeployer.Properties.Settings.Default.CreateDeploymentPathIfNotExists)
            {
                _logger.Error("Unable to determine deployment directory, it does not exist and the setting CreateDeploymentPathIfNotExists is false.");
                Environment.Exit(1);
            }

            Directory.CreateDirectory(deploymentPath);
            File.WriteAllText(markerFileName, "");
        }

        private static void BackgroundWork(Action action) => Task.Factory.StartNew(() => { try { action(); } catch (Exception ex) { _logger.Error(ex); } });
    }
}
