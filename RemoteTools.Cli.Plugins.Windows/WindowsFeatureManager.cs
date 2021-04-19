using Microsoft.Dism;
using RemoteTools.Cli.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Windows
{
    public class WindowsFeatureManager : IFeatureManager
    {
        private readonly IOperatingSystem operatingSystem;
        private bool disposed = false;
        private DismSession session;

        public WindowsFeatureManager(IOperatingSystem operatingSystem)
        {
            this.operatingSystem = operatingSystem;
        }

        ~WindowsFeatureManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                session.Close();
                session.Dispose();
                DismApi.Shutdown();
                disposed = true;

                GC.SuppressFinalize(this);
            }
        }

        public async Task EnableFeatureAsync(string featureName) =>
            DismApi.EnableFeatureByPackageName(await GetDismSessionAsync(), featureName, null, false, true);

        public async Task<bool> IsFeatureEnabledAsync(string featureName) =>
            DismApi.GetFeatureInfo(await GetDismSessionAsync(), featureName).FeatureState == DismPackageFeatureState.Installed;

        private async Task<DismSession> GetDismSessionAsync()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WindowsFeatureManager));
            }

            if (session == null)
            {
                await operatingSystem.ElevateAsync();

                DismApi.Initialize(DismLogLevel.LogErrors);

                session = DismApi.OpenOnlineSessionEx(new DismSessionOptions
                {
                    ThrowExceptionOnRebootRequired = false
                });
            }

            return session;
        }
    }
}
