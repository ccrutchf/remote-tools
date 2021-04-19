using RemoteTools.Cli.Core;
using System;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.RemoteServer
{
    public class RemoteServerExecutionBackend : IExecutionBackend
    {
        public string NotSupportedMessage =>
            throw new NotImplementedException();

        public Task CloneAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task InstallAsync(string ansibleUrl)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsSupportedAsync() =>
            Task.FromResult(true);

        public Task StartAsync(string url)
        {
            throw new NotImplementedException();
        }
    }
}
