using Microsoft.Win32;
using RemoteTools.Cli.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Plugins.Windows
{
    public class WindowsOperatingSystem : IOperatingSystem
    {
        public async Task<bool> ElevateAsync()
        {
            if (!UACHelper.UACHelper.IsElevated)
            {
                await Process.Start(new ProcessStartInfo
                {
                    Arguments = string.Join(' ', Environment.GetCommandLineArgs()),
                    FileName = Assembly.GetEntryAssembly().Location,
                    Verb = "runAs"
                }).WaitForExitAsync();
                return true;
            }

            return false;
        }

        public DirectoryInfo GetConfigDirectory()
        {
            var path = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ccrutchf",
                "RemoteTools",
                Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location));

            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            return directoryInfo;
        }

        public void RebootNow() =>
            Process.Start("shutdown", "/r /t 0");

        public Task SetupRunOnNextBootAsync()
        {
            if (OperatingSystem.IsWindows())
            {
                var exeInfo = new FileInfo(Assembly.GetEntryAssembly().Location);

                var runOnce = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce");
                runOnce.SetValue(exeInfo.Name, string.Join(' ', new string[] { exeInfo.FullName }.Concat(Environment.GetCommandLineArgs())));
            }

            return Task.FromResult(0);
        }
    }
}
