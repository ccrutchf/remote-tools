using Python.Runtime;
using RemoteTools.Cli.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.PyPlot
{
    public class PyPlot : IPlot
    {
        private readonly IPython python;

        public PyPlot(IPython python)
        {
            this.python = python;
        }

        public async Task ShowAsync()
        {
            await InitializeAsync();

            using (Py.GIL())
            {
                dynamic np = Py.Import("numpy");
                dynamic plt = Py.Import("matplotlib.pyplot");

                var t = np.arange(0.0f, 2.0f, 0.01f);

                plt.plot(t);
                plt.show();
            }
        }

        private async Task InitializeAsync()
        {
            await python.InstallAsync();
            InstallMatPlotLib();
        }

        private void InstallMatPlotLib()
        {
            if (!Directory.Exists(Path.Join(python.SitePackagesPath, "matplotlib")))
            {
                Process.Start(python.PipPath, "install matplotlib");
            }
        }
    }
}
