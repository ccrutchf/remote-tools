using RemoteTools.Cli.Core;
using System;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Native
{
    public class NativeExecutionBackend : IExecutionBackend
    {
        public string NotSupportedMessage =>
            "The native backend is only supported on Linux.  Please select a different backend.";

        public Task CloneAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task InstallAsync(string ansibleUrl)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsSupportedAsync() =>
            Task.FromResult(OperatingSystem.IsLinux());

        public Task StartAsync(string url)
        {
            throw new NotImplementedException();
        }
    }
}
