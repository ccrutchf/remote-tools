using RemoteTools.Cli.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Windows
{
    public class AppxPackageManager : IPackageManager
    {
        public Task<bool> CanInstallAsync(string name) =>
            Task.FromResult(File.Exists(name) && Path.GetExtension(name).Equals(".appx", StringComparison.OrdinalIgnoreCase));

        public Task InstallAsync(string name)
        {
            using var ps = PowerShell.Create();
            ps.AddScript("Import-Module Appx -UseWindowsPowerShell");
            ps.AddScript($"Install-AppxPackage '{name}'");

            return ps.InvokeAsync();
        }

        public async Task<bool> IsPackageInstalledAsync(string name)
        {
            using var ps = PowerShell.Create();
            ps.AddScript("Import-Module Appx -UseWindowsPowerShell");
            ps.AddScript($"Get-AppxPackage '{name}'");

            return await ps.InvokeAsync() != null;
        }
    }
}
