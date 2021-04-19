using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Core
{
    public interface IOperatingSystem
    {
        Task<bool> ElevateAsync();
        DirectoryInfo GetConfigDirectory();
        void RebootNow();
        Task SetupRunOnNextBootAsync();
    }
}
