using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Core
{
    public interface IPackageManager
    {
        Task<bool> CanInstallAsync(string name);
        Task InstallAsync(string name);
        Task<bool> IsPackageInstalledAsync(string name);
    }
}
