using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Core
{
    public interface IPython
    {
        string PipPath { get; }
        string SitePackagesPath { get; }

        Task InstallAsync();
    }
}
