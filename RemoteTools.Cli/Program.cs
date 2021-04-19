using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using RemoteTools.Cli.Core;
using RemoteTools.Cli.Plugins.Native;
using RemoteTools.Cli.Plugins.PyPlot;
using RemoteTools.Cli.Plugins.RemoteServer;
using RemoteTools.Cli.Plugins.Vagrant;
using RemoteTools.Cli.Plugins.Windows;
using RemoteTools.Cli.Plugins.Wsl2;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli
{
    class Program
    {
        [Option(Description = "The URL where the Ansible playbook resides.")]
        public string AnsibleUrl { get; }

        [Option(Description = "The execution backend to be used for execution.")]
        public ExecutionBackend Backend { get; }

        [Option(Description = "The URL to clone.")]
        public string Url { get; }

        static Task<int> Main(string[] args)
        {
            ConsoleErrorWriterDecorator.SetToConsole();

            return CommandLineApplication.ExecuteAsync<Program>(args);
        }

        private async Task<int> OnExecute()
        {
            var services = ConfigureServices();

            var executionBackendService = services.GetService<IExecutionBackend>();
            if (!await executionBackendService.IsSupportedAsync())
            {
                await Console.Error.WriteLineAsync(executionBackendService.NotSupportedMessage);

                return 1;
            }

            var executionBackend = services.GetService<IExecutionBackend>();

            //var plot = services.GetRequiredService<IPlot>();
            //await plot.ShowAsync();

            await executionBackend.InstallAsync(AnsibleUrl);
            await executionBackend.CloneAsync(Url);
            await executionBackend.StartAsync(Url);

            return 0;
        }

        private ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            ConfigureBackendServices(services);

            if (OperatingSystem.IsWindows())
            {
                ConfigureWindowsServices(services);
            }
            else if (OperatingSystem.IsLinux())
            {
                ConfigureLinuxServices(services);
            }
            else if (OperatingSystem.IsMacOS())
            {
                ConfigureMacOsServices(services);
            }

            services.AddTransient<IPlot, PyPlot>();

            return services.BuildServiceProvider();
        }

        private void ConfigureBackendServices(ServiceCollection services)
        {
            switch (Backend)
            {
                case ExecutionBackend.Vagrant:
                    services.AddTransient<IExecutionBackend, VagrantExecutionBackend>();
                    break;
                case ExecutionBackend.Wsl2:
                    services.AddTransient<IExecutionBackend, Wsl2ExecutionBackend>();
                    break;
                case ExecutionBackend.RemoteServer:
                    services.AddTransient<IExecutionBackend, RemoteServerExecutionBackend>();
                    break;
                case ExecutionBackend.Native:
                default:
                    services.AddTransient<IExecutionBackend, NativeExecutionBackend>();
                    break;
            }
        }

        private void ConfigureWindowsServices(ServiceCollection services)
        {
            services.AddTransient<IPython, WindowsPython>();
            services.AddSingleton<IFeatureManager, WindowsFeatureManager>();
            services.AddTransient<IOperatingSystem, WindowsOperatingSystem>();
            services.AddTransient<IPackageManager, MsiPackageManager>();
            services.AddTransient<IPackageManager, AppxPackageManager>();
        }

        private void ConfigureLinuxServices(ServiceCollection services)
        {

        }

        private void ConfigureMacOsServices(ServiceCollection services)
        {

        }
    }
}
