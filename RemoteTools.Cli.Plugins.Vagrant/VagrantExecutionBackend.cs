using RemoteTools.Cli.Core;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Vagrant
{
    public class VagrantExecutionBackend : IExecutionBackend
    {
        public string NotSupportedMessage =>
            "The Vagrant backend is only supported by computers with an x86_64 CPU.  Please select a different backend.";

        public Task CloneAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task InstallAsync(string ansibleUrl)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsSupportedAsync() =>
            Task.FromResult(RuntimeInformation.OSArchitecture == Architecture.X64);

        public Task StartAsync(string url)
        {
            throw new NotImplementedException();
        }
    }
}
