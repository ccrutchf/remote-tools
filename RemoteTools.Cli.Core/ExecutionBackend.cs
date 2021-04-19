using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Core
{
    public enum ExecutionBackend
    {
        Native,
        Vagrant,
        Wsl2,
        RemoteServer
    }
}
