using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Core
{
    public interface IExecutionBackend
    {
        string NotSupportedMessage { get; }

        Task CloneAsync(string url);
        Task InstallAsync(string ansibleUrl);
        Task<bool> IsSupportedAsync();
        Task StartAsync(string url);
    }
}
