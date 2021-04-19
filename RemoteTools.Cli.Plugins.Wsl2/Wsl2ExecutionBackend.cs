using RemoteTools.Cli.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Wsl2
{
    public class Wsl2ExecutionBackend : IExecutionBackend
    {
        private const string X64_Kernel_Url = "https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi";
        private const string X64_Kernel_Package_Name = "wsl_update_x64.msi";

        private const string X64_Ubuntu2004_Url = "https://aka.ms/wslubuntu2004";
        private const string X64_Ubuntu2004_Package_Name = "ubuntu2004.appx";

        private readonly IFeatureManager featureManager;
        private readonly IOperatingSystem operatingSystem;
        private readonly IEnumerable<IPackageManager> packageManagers;

        public string NotSupportedMessage =>
            "The WSL2 backend is only supported on Windows 10 build 20262 or higher with an x86_64 CPU.  Please select a different backend.";

        public Wsl2ExecutionBackend(IFeatureManager featureManager, IOperatingSystem operatingSystem, IEnumerable<IPackageManager> packageManagers)
        {
            this.featureManager = featureManager;
            this.operatingSystem = operatingSystem;
            this.packageManagers = packageManagers;
        }

        public async Task CloneAsync(string url)
        {
            var folderName = GetPathFromGitUrl(url);
            var process = Process.Start(new ProcessStartInfo
            {
                Arguments = "ls -1 ~",
                FileName = "wsl",
                RedirectStandardOutput = true
            });

            await process.WaitForExitAsync();
            bool cloned = false;
            while (process.StandardOutput.Peek() != -1)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line.Trim() == folderName)
                {
                    cloned = true;
                    break;
                }
            }

            if (!cloned)
            {
                await Process.Start("wsl", $"cd ~;git clone {url}").WaitForExitAsync();
            }
        }

        public async Task InstallAsync(string ansibleUrl)
        {
            await InstallWindowsFeaturesAsync();
            await InstallKernelAsync();
            await InstallDistroAsync();
            await UpdateAsync();
            await InstallAnsibleAsync();
            await InstallDependenciesAsync(ansibleUrl);
        }

        public Task<bool> IsSupportedAsync() =>
            Task.FromResult(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362) && RuntimeInformation.OSArchitecture == Architecture.X64);

        public async Task StartAsync(string url)
        {
            var folderName = GetPathFromGitUrl(url);
            await Process.Start("wsl", $"export DISPLAY=$(ip route|awk '/^default/{{print $3}}'):0.0; cd ~/{folderName}; ./cli shell").WaitForExitAsync();
        }

        private string ConvertToWslPath(string path) =>
            path.Replace("\\", "/").Replace("C:", "/mnt/c");

        private string GetPathFromGitUrl(string url) =>
            Path.GetFileNameWithoutExtension(url.Substring(url.LastIndexOf('/') + 1));

        private async Task<bool> GetIsUbuntuInstalledAsync()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                Arguments = "-l",
                FileName = "wsl.exe",
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.Unicode
            });

            await process.WaitForExitAsync();
            bool installed = false;
            while (process.StandardOutput.Peek() != -1)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                installed |= line.Trim().StartsWith("Ubuntu-20.04");

                if (installed)
                {
                    break;
                }
            }

            return installed;
        }

        private async Task<int> GetWslVersionAsync()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                Arguments = "--status",
                FileName = "wsl.exe",
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.Unicode
            });

            await process.WaitForExitAsync();
            int versionNumber = -1;
            while (process.StandardOutput.Peek() != -1)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line.StartsWith("Default Version"))
                {
                    versionNumber = int.Parse(line.Replace("Default Version:", string.Empty).Trim());
                    break;
                }
            }

            return versionNumber;
        }

        private async Task InstallAnsibleAsync()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                Arguments = "which ansible-playbook",
                FileName = "wsl.exe",
                RedirectStandardOutput = true
            });

            await process.WaitForExitAsync();
            var location = (await process.StandardOutput.ReadToEndAsync()).Trim();

            if (string.IsNullOrWhiteSpace(location))
            {
                await Process.Start("wsl", "sudo apt install ansible").WaitForExitAsync();
            }
        }

        private async Task InstallDependenciesAsync(string ansibleUrl)
        {
            var configDirectory = operatingSystem.GetConfigDirectory();
            var playbookPath = Path.Join(configDirectory.FullName, "playbook.yml");

            if (!File.Exists(playbookPath))
            {
                using var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(ansibleUrl, playbookPath);
            }

            var wslPlaybookPath = ConvertToWslPath(playbookPath);
            var process = Process.Start(new ProcessStartInfo
            {
                Arguments = $"ansible-playbook --connection=local --inventory 127.0.0.1, '{wslPlaybookPath}' --check",
                FileName = "wsl.exe",
                RedirectStandardOutput = true
            });

            await process.WaitForExitAsync();
            bool ansibleChanges = false;
            while (process.StandardOutput.Peek() != -1)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                ansibleChanges |= line.Contains("changed: [127.0.0.1]");
            }

            if (ansibleChanges)
            {
                await Process.Start(new ProcessStartInfo
                {
                    Arguments = $"sudo ansible-playbook --connection=local --inventory 127.0.0.1, '{wslPlaybookPath}'",
                    FileName = "wsl.exe"
                }).WaitForExitAsync();
            }
        }

        private async Task InstallDistroAsync()
        {
            string targetDistroPath = Path.Join(operatingSystem.GetConfigDirectory().FullName, X64_Ubuntu2004_Package_Name);
            if (!File.Exists(targetDistroPath))
            {
                using var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(X64_Ubuntu2004_Url, targetDistroPath);
            }

            var canAppxInstallPackage = await Task.WhenAll(from p in packageManagers
                                                           select p.CanInstallAsync(targetDistroPath));
            var appxPackageManager = (from z in packageManagers.Zip(canAppxInstallPackage, (p, r) => (PackageManager: p, CanInstall: r))
                                      where z.CanInstall
                                      select z.PackageManager).Single();

            if (!await appxPackageManager.IsPackageInstalledAsync("CanonicalGroupLimited.Ubuntu20.04onWindows"))
            {
                await appxPackageManager.InstallAsync(targetDistroPath);
            }

            if (!await GetIsUbuntuInstalledAsync())
            {
                Console.WriteLine("Ubuntu 20.04 WSL not installed.  Please launch Ubuntu 20.04 LTS from your start menu and try again.");
                Environment.Exit(-1);
            }
        }

        private async Task InstallKernelAsync()
        {
            const string msiGuid = "EAEFC39172D024543907FE37FBC5086B";

            string targetKernelPath = Path.Join(operatingSystem.GetConfigDirectory().FullName, X64_Kernel_Package_Name);
            if (!File.Exists(targetKernelPath))
            {
                using var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(X64_Kernel_Url, targetKernelPath);
            }

            var canMsiInstallPackage = await Task.WhenAll(from p in packageManagers
                                                          select p.CanInstallAsync(targetKernelPath));
            var msiPackageManager = (from z in packageManagers.Zip(canMsiInstallPackage, (p, r) => (PackageManager: p, CanInstall: r))
                                     where z.CanInstall
                                     select z.PackageManager).Single();

            if (!await msiPackageManager.IsPackageInstalledAsync(msiGuid))
            {
                await msiPackageManager.InstallAsync(targetKernelPath);
            }
        }

        private async Task InstallWindowsFeaturesAsync()
        {
            bool rebootRequired = false;

            if (!WslExeExists())
            {
                if (!await featureManager.IsFeatureEnabledAsync("Microsoft-Windows-Subsystem-Linux"))
                {
                    await featureManager.EnableFeatureAsync("Microsoft-Windows-Subsystem-Linux");
                    rebootRequired = true;
                }
            }

            if (await GetWslVersionAsync() != 2)
            {
                if (!await featureManager.IsFeatureEnabledAsync("VirtualMachinePlatform"))
                {
                    await featureManager.EnableFeatureAsync("VirtualMachinePlatform");
                    rebootRequired = true;
                }
                else
                {
                    await Process.Start("wsl --set-default-version 2").WaitForExitAsync();
                }
            }

            if (rebootRequired)
            {
                Console.WriteLine("Your computer needs to reboot.  Press any key to reboot now...");
                Console.ReadKey(true);
                await operatingSystem.SetupRunOnNextBootAsync();
                operatingSystem.RebootNow();
            }
        }

        private async Task UpdateAsync()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                Arguments = "apt-get upgrade -s",
                FileName = "wsl.exe",
                RedirectStandardOutput = true
            });

            await process.WaitForExitAsync();
            bool updatesPending = false;
            while (process.StandardOutput.Peek() != -1)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                var match = Regex.Match(line, "([0-9]+) upgraded");

                if (match.Success)
                {
                    updatesPending = match.Groups[1].Value != "0";
                }
            }

            if (updatesPending || true)
            {
                await Process.Start("wsl", "sudo apt update && sudo apt upgrade -y").WaitForExitAsync();
            }
        }

        private bool WslExeExists() =>
            File.Exists(Path.Join("C:", "Windows", "System32", "wsl.exe"));
    }
}
