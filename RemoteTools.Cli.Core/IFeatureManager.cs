using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Core
{
    public interface IFeatureManager
    {
        Task EnableFeatureAsync(string featureName);
        Task<bool> IsFeatureEnabledAsync(string featureName);
    }
}
