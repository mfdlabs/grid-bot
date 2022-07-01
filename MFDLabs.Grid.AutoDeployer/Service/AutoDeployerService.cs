#define GRID_BOT

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Octokit;
using Microsoft.Win32;
using MFDLabs.Logging;
using MFDLabs.FileSystem;
using MFDLabs.Diagnostics;
using MFDLabs.Text.Extensions;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Diagnostics.Extensions;

namespace MFDLabs.Grid.AutoDeployer.Service
{
    internal sealed class AutoDeployerService
    {
        // language=regex
        /// <summary>
        /// <c>/(((\d{4,})\.(\d{2}).(\d{2}))-((\d{2}).(\d{2}).(\d{2}))_([a-zA-Z0-9\-_]+)_([a-f0-9]{7}))_?(((?i)(net(?i)(standard|coreapp)?)(([0-9]{1,3}\.?){1,2}))-(?i)(release|debug)(?i)(config(?i)(uration)?)?)?/g</c>
        /// <br/>
        /// <br/>
        /// https://regexr.com/6hqa4 <br/>
        /// https://regex101.com/r/og6X9j/1 <br/>
        /// <br/>
        /// Matches the following format:<br/>
        /// yyyy.MM.dd-hh.mm.ss_{branch}_{gitShortHash}_{targetFramework}-{configuration}<br/>
        ///<br/>
        /// Group 1: Matches default deployment format: yyyy.MM.dd-hh.mm.ss_{branch}_{gitShortHash}<br/>
        /// 		 Where {branch} is any a-zA-Z0-9 or the `-` character.<br/>
        /// 		 Where {gitShortHash} is a-z0-9 7 characters long exactly.<br/>
        /// Group 2: Matches the deployment date: yyyy.MM.dd<br/>
        /// Group 3: Matches the deployment year: yyyy<br/>
        /// Group 4: Matches the deployment month: MM<br/>
        /// Group 5: Matches the deployment day: dd<br/>
        /// Group 6: Matches the time: hh.mm.ss<br/>
        /// Group 7: Matches the hour: hh<br/>
        /// Group 8: Matches the minute: mm<br/>
        /// Group 9: Matches the second: ss<br/>
        /// Group 10: Matches the branch name in the format of a-zA-Z0-9\-<br/>
        /// Group 11: Matches the gitShortHash with the length of 7 exactly.<br/>
        /// Group 12: Matches the optional .NET configuration section: {targetFramework}-{configuration}<br/>
        /// 		  Where {targetFramework} is in the format of a case insensitive net(standard|coreapp)?\d(\.?)\d: net472, NET472, netstandard2.0, NeTsTanDard2.0, netcoreapp3.0, NETCoreApp3.1<br/>
        /// 		  Where {configuration} is the build configuration in the format of a case insensitive (release|debug)(config(uration)?): release, debug, Release, DEBUG, releaseConfig, ReleaseConfig, DebugCONFIG, DEBUGCONFIGURATION<br/>
        /// Group 13: Matches the targetFramework in the format of net(standard|coreapp)?\d(\.?)\d<br/>
        /// Group 14: Matches the name of the target framework net(standard|coreapp)<br/>
        /// Group 15: Matches the extended name of the target framework (standard|coreapp)<br/>
        /// Group 16: Matches the full number of the target framework version<br/>
        /// Group 17: Matches the number on the right of the \. in group 16.<br/>
        /// Group 18: Matches the build configuration (release|debug)<br/>
        /// Group 19: Matches the identifier that determines if this belongs to a configuration archive, (config(uration)?)<br/>
        /// Group 20: Matches the identifier that determines if this is the full `Configuration` string.<br/>
        ///<br/>
        /// NOTICE: For .NET Regex, the original match is capture group no.1, so group number 12 would 13 in .NET<br/>
        /// </summary>
        private const string DeploymentIdRegex = @"(((\d{4,})\.(\d{2}).(\d{2}))-((\d{2}).(\d{2}).(\d{2}))_([a-zA-Z0-9\-_]+)_([a-f0-9]{7}))_?(((?i)(net(?i)(standard|coreapp)?)(([0-9]{1,3}\.?){1,2}))-(?i)(release|debug)(?i)(config(?i)(uration)?)?)?";

        // language=regex
        /// <summary>
        /// \[(?i)(volatile)\]
        /// <br/>
        /// Matches the following string and its variants: [DEPLOY]
        /// </summary>
        private const string ShouldDeployPreReleaseSearchString = @"\[(?i)(deploy)\]";
        private const string UserAgent = "mfdlabs-github-api-client";
        private const string DeploymentMarkerFilename = ".mfdlabs-deployment-marker";
        
        private static User _githubUser;
        private static GitHubClient _gitHubClient;
        private static RegistryKey _versioningRegKey;

        private static bool _isRunning;
        private static string _cachedVersion;
        private static readonly List<string> _skippedVersions = new();

        #region "Constants"

        private static string _versioningRegSubKey;
        private static string _versioningRegVersionKeyName;
        private static string _deploymentPath;
        private static string _githubOrgOrAccountName;
        private static string _githubRepositoryName;
        private static string _githubToken;
        private static string _primaryDeploymentExecutable;
        private static TimeSpan _pollingInterval;

        #endregion

        public static void Stop(object s, EventArgs e)
        {
            EventLogLogger.Singleton.LifecycleEvent("Stopping...");
            _isRunning = false;
            _versioningRegKey?.Dispose();
        }

        public static void Start(object s, EventArgs e)
        {
            try { Work(); }
            catch (ArgumentException ex) { Logger.Singleton.Error(ex.Message); Environment.Exit(1); }
            catch (Exception ex) { Logger.Singleton.Error(ex); Environment.Exit(1); }
        }
        
        private static void Work()
        {
            _versioningRegSubKey = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.VersioningRegistrySubKey;
            _versioningRegVersionKeyName = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.VersioningRegistryVersionKeyName;
            _deploymentPath = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.DeploymentPath;
            _githubOrgOrAccountName = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.GithubAccountOrOrganizationName;
            _githubRepositoryName = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.GithubRepositoryName;
            _primaryDeploymentExecutable = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.DeploymentPrimaryExecutableName;
            _pollingInterval = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.PollingInterval;

            EventLogLogger.Singleton.LifecycleEvent("Starting...");

#if !DEBUG
            if (_pollingInterval < TimeSpan.FromSeconds(30))
                throw new ArgumentException("The Polling interval cannot be less than 30 seconds.");
#endif

            if (_deploymentPath.IsNullOrEmpty())
                throw new ArgumentException("The Deployment Path cannot be null or empty.");

            if (_primaryDeploymentExecutable.IsNullOrEmpty())
                throw new ArgumentException("The Primary Deployment Executable cannot be null or empty.");

            if (!FilesHelper.IsValidPath(_deploymentPath, false))
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
                EventLogLogger.Singleton.Info("Got version {0} from registry key HKLM:{1}.{2}", _cachedVersion, _versioningRegSubKey, _versioningRegVersionKeyName);

            _githubToken = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.GithubToken;
            var gheUrl = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.GithubEnterpriseUrl;

            if (_githubToken.IsNullOrEmpty())
                throw new ArgumentException("The Github Token cannot be null or empty.");

            var phv = new ProductHeaderValue(UserAgent);

            if (gheUrl.IsNullOrEmpty())
                _gitHubClient = new(phv);
            else
                _gitHubClient = new(phv, new Uri(gheUrl));

            _gitHubClient.Credentials = new(_githubToken);

            _githubUser = _gitHubClient.User.Current().Result;

            EventLogLogger.Singleton.LifecycleEvent("Authenticated to '{0}' as '{1}'. We are setup to fetch releases from '{2}/{3}'", _gitHubClient.BaseAddress, _githubUser.Email, _githubOrgOrAccountName, _githubRepositoryName);

            _isRunning = true;

            BackgroundWork(Run);
        }

        private static void Run()
        {
            SetupRegistry();
            SetupDeploymentPath();
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
                        EventLogLogger.Singleton.Warning("Skipping '{0}' because it had more or less than 4 assets.", r.TagName);
                        goto SLEEP;
                    }

                    var regex = $@"{r.TagName}_?(((?i)(net(?i)(standard|coreapp)?)(([0-9]{{1,3}}\.?){{1,2}}))-(?i)(release|debug)(?i)(config(?i)(uration)?)?)?(?i)(\.mfdlabs-(config-)?archive|Unpacker\.(ps1|bat))?";

                    foreach (var a in r.Assets)
                    {
                        if (!a.Name.IsMatch(regex))
                        {
                            SkipVersion(r.TagName);
                            EventLogLogger.Singleton.Warning("Skipping '{0}' because the asset '{1}' didn't match '{2}'.", r.TagName, a.Name, regex);
                            goto SLEEP;
                        }
                    }

#if GRID_BOT
                    if (ProcessHelper.GetProcessByName(_primaryDeploymentExecutable.ToLower().Replace(".exe", ""), out _))
                    {
                        InvokeMaintenanceCommandOnPreviousExe(r.TagName);
                        EventLogLogger.Singleton.Warning("Invoked upgrade message onto bot, sleep for 15 seconds to ensure it receives it.");
                        Thread.Sleep(TimeSpan.FromSeconds(15));
                    }
#endif

                    foreach (var a in r.Assets)
                    {
                        var fqp = Path.Combine(_deploymentPath, a.Name);
                        DownloadArtifact(a.Url, fqp);
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
                        FilesHelper.PollDeletionOfFileBlocking(versionDeploymentPath);
                        EventLogLogger.Singleton.Error("Unable to deploy version '{0}': The file '{1}' was not found.", r.TagName, primaryExe);
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

        private static void StartNewProcess(string primaryExe, string versionDeploymentPath)
        {
            var proc = new Process();
            proc.StartInfo.FileName = "powershell.exe";
            proc.StartInfo.Arguments = $"-ExecutionPolicy Unrestricted -Command \"Start-Process '{primaryExe}' -WindowStyle Maximized\"";
            proc.StartInfo.WorkingDirectory = versionDeploymentPath;
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
                Logger.Singleton.Warning("The process '{0}' is not running, ignoring...", name);
                return;
            }

            if (!SystemGlobal.ContextIsAdministrator()
#if NETFRAMEWORK // for now
                && pr.IsElevated()
#endif

                )
            {
                Logger.Singleton.Warning("The process '{0}' is running on a higher context than the current process, ignoring...", name);
                return;
            }

            KillAllProcessByName(name);

            Logger.Singleton.Info("Successfully closed process '{0}'.", name);
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
        public static void InvokeMaintenanceCommandOnPreviousExe(string tagName)
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


        private static void CleanupArtifacts(ReleaseAsset[] assets)
        {
            foreach (var asset in assets)
            {
                var fqp = Path.Combine(_deploymentPath, asset.Name);
                EventLogLogger.Singleton.Debug("Deleting asset '{0}' from '{1}'", asset.Name, _deploymentPath);
                FilesHelper.PollDeletionOfFileBlocking(fqp, 10);
            }
        }

        private static bool RunUnpacker(Release release, out string versionDeploymentPath)
        {
            var unpackerFile = Path.Combine(
                _deploymentPath,
                (
                    from f in release.Assets
                    where f.Name.IsMatch($@"{release.TagName}_?(((?i)(net(?i)(standard|coreapp)?)(([0-9]{{1,3}}\.?){{1,2}}))-(?i)(release|debug)(?i)(config(?i)(uration)?)?)?\.Unpacker\.ps1")
                    select f
                ).FirstOrDefault()?.Name
            );

            versionDeploymentPath = unpackerFile.Replace(".Unpacker.ps1", "");

            if (!File.Exists(unpackerFile))
            {
                EventLogLogger.Singleton.Error("Unkown unpacker script: {0}. Skipping version...", unpackerFile);
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
                EventLogLogger.Singleton.Error("Unpacker script '{0}' failed with exit code '{1}'. Skipping version...", unpackerFile, proc.ExitCode);
                return false;
            }

            if (!Directory.Exists(versionDeploymentPath))
            {
                EventLogLogger.Singleton.Error("Unpacker script '{0}' failed: The output deployment was not created at '{1}'. Skipping version...", unpackerFile, versionDeploymentPath);
                return false;
            }

            return true;
        }

        private static void DownloadArtifact(string uri, string outputPath)
        {
            EventLogLogger.Singleton.Debug("Downloading artifact '{0}' to '{1}'", uri, outputPath);

            using var webClient = new WebClient();
            webClient.Headers.Add("User-Agent", UserAgent);
            webClient.Headers.Add("Authorization", $"token {_githubToken}");
            webClient.Headers.Add("Accept", "application/octet-stream");
            webClient.DownloadFile(uri, outputPath);
        }

        private static void SkipVersion(string version) => _skippedVersions.Add(version);

        private static bool DetermineIfNewReleaseAvailable(out Release latestRelease)
        {
            latestRelease = null;

            // GetAll here because it will not fetch pre-release if we use GetLatest
            var releases = _gitHubClient.Repository.Release.GetAll(_githubOrgOrAccountName, _githubRepositoryName, new() { PageCount = 1, PageSize = 1 }).Result;

            if (!releases.Any()) return false;

            latestRelease = releases[0];
            var latestReleaseVersion = latestRelease.TagName;

            if (latestRelease.Draft) return false;
            if (latestReleaseVersion == _cachedVersion) return false;
            if (!latestReleaseVersion.IsMatch(DeploymentIdRegex, RegexOptions.Compiled)) return false;
            if (_skippedVersions.Contains(latestReleaseVersion)) return false;

            if (latestRelease.Prerelease)
            {
                if (!latestRelease.Name.IsMatch(ShouldDeployPreReleaseSearchString, RegexOptions.Compiled | RegexOptions.IgnoreCase))
                {
                    EventLogLogger.Singleton.Warning("{0} was marked as prerelease but had no deploy override text in the title, skipping...", latestReleaseVersion);
                    return false;
                }
            }

            EventLogLogger.Singleton.Debug("Got new release from repository {0}/{1}: {2}. Is Pre-Release: {3}", _githubOrgOrAccountName, _githubRepositoryName, latestReleaseVersion, latestRelease.Prerelease);

            return true;
        }

        private static void DetermineLatestVersion()
        {
            if (_cachedVersion != null) return;

            // There may be deployments in here already.
            // .NET deployments are normally in the format:
            // yyyy.MM.dd-hh.mm.ss_{branch}_{gitShortHash}-{targetFramework}-{configuration}

            var directoryMatches = from f in Directory.EnumerateDirectories(_deploymentPath)
                                   let match = f.Match(DeploymentIdRegex, RegexOptions.Compiled)
                                   where match.Success
                                   select match;

            if (!directoryMatches.Any())
            {
                EventLogLogger.Singleton.Warning("Not overwriting registry version as there was no deployments found at path '{0}'.", _deploymentPath);
                return;
            }

            // please look at the regex docs at the top of this file
            var sortedDirectories = from m in directoryMatches
                                    let year = m.Groups[3].Value.ToInt32()
                                    let month = m.Groups[4].Value.ToInt32()
                                    let day = m.Groups[5].Value.ToInt32()
                                    let hour = m.Groups[7].Value.ToInt32()
                                    let minute = m.Groups[8].Value.ToInt32()
                                    let second = m.Groups[9].Value.ToInt32()
                                    orderby new DateTime(year, month, day, hour, minute, second).Ticks descending
                                    select m;

            var currentDeployment = sortedDirectories.FirstOrDefault();

            if (currentDeployment != null)
            {
                _cachedVersion = currentDeployment.Groups[1].Value;

                EventLogLogger.Singleton.Info("Got version from directories: {0}. Updating registry.", _cachedVersion);

                WriteVersionToRegistry();

                return;
            }
        }

        private static void WriteVersionToRegistry()
        {
            if (_cachedVersion == null) return;
            if (_versioningRegKey == null) SetupRegistry();

            if (GetVersionFromRegistry() == _cachedVersion) return;

            _versioningRegKey?.SetValue(_versioningRegVersionKeyName, _cachedVersion);
        }

        private static string GetVersionFromRegistry() => _versioningRegKey?.GetValue(_versioningRegVersionKeyName, null) as string;

        private static void SetupRegistry()
        {
            if (_versioningRegKey == null)
            {
                // regKey doesn't exist. Create it.
                _versioningRegKey = Registry.LocalMachine.CreateSubKey(_versioningRegSubKey, true);
            }
        }

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

            if (FilesHelper.IsSubDir(winDir, deploymentPath))
            {
                EventLogLogger.Singleton.Error("Cannot create deployment directory as it is reserved by Windows.");
                Environment.Exit(1);
            }

            if (File.Exists(deploymentPath))
            {
                EventLogLogger.Singleton.Error("Unable to determine deployment directory, it exists already and is a file.");
                Environment.Exit(1);
            }

            if (!global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.CreateDeploymentPathIfNotExists)
            {
                EventLogLogger.Singleton.Error("Unable to determine deployment directory, it does not exist and the setting CreateDeploymentPathIfNotExists is false.");
                Environment.Exit(1);
            }

            Directory.CreateDirectory(deploymentPath);
            File.WriteAllText(markerFileName, "");
        }

        private static void BackgroundWork(Action action) => Task.Factory.StartNew(() => { try { action(); } catch (Exception ex) { EventLogLogger.Singleton.Error(ex); } });
    }
}
