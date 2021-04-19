using Python.Runtime;
using RemoteTools.Cli.Core;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Windows
{
    public class WindowsPython : IPython
    {
        private static bool pathAppended;

        const string PYTHON_URL = "https://github.com/winpython/winpython/releases/download/4.0.20210307/Winpython64-3.9.2.0.exe";
        private readonly FileInfo pythonDirectoryInfo = new("python");

        public string PipPath =>
            Path.Join(pythonDirectoryInfo.FullName, "WPy64-3920", "python-3.9.2.amd64", "Scripts", "pip.exe");

        public string SitePackagesPath =>
            Path.Join(pythonDirectoryInfo.FullName, "WPy64-3920", "python-3.9.2.amd64", "Lib", "site-packages");

        public async Task InstallAsync()
        {
            var client = new WebClient();

            var pythonArchiveInfo = new FileInfo("python_archive.exe");
            var pythonDirectoryInfo = new FileInfo("python");

            if (!pythonArchiveInfo.Exists && !Directory.Exists(pythonDirectoryInfo.FullName))
            {
                await client.DownloadFileTaskAsync(new Uri(PYTHON_URL), pythonArchiveInfo.FullName);
            }

            if (!Directory.Exists(pythonDirectoryInfo.FullName))
            {
                var extractor = new SevenZipExtractor(pythonArchiveInfo.FullName);
                await extractor.ExtractArchiveAsync(pythonDirectoryInfo.FullName);
            }

            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", Path.Join(pythonDirectoryInfo.FullName, "WPy64-3920", "python-3.9.2.amd64", "python39.dll"));

            if (!pathAppended)
            {
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append(SitePackagesPath);
                }

                pathAppended = true;
            }

            if (pythonArchiveInfo.Exists)
            {
                pythonArchiveInfo.Delete();
            }
        }
    }
}
