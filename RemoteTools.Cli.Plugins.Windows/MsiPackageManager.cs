using Microsoft.Win32;
using RemoteTools.Cli.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Windows
{
    public class MsiPackageManager : IPackageManager
    {
        public Task<bool> CanInstallAsync(string name) =>
            Task.FromResult(Guid.TryParse(name, out Guid _) || (File.Exists(name) && Path.GetExtension(name).Equals(".msi", StringComparison.OrdinalIgnoreCase)));

        public Task InstallAsync(string name) =>
            Process.Start("msiexec", $"/i {name} /quiet /qn /norestart").WaitForExitAsync();

        public Task<bool> IsPackageInstalledAsync(string name)
        {
            if (OperatingSystem.IsWindows())
            {
                var packageKey = Registry.ClassesRoot.OpenSubKey($"Installer\\Products\\{name}");
                return Task.FromResult(packageKey?.GetValue("ProductName") != null);
            }

            return Task.FromResult(false);
        }
    }
}
