// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nanoFramework.IoT.TestRunner.Helpers;
using nanoFramework.IoT.TestRunner.UsbIp;
using System.Diagnostics;

namespace nanoFramework.IoT.TestRunner
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TestStream.Runner Starting.");
            await AttachUsbipAndRunDocker(stoppingToken);
        }

        private async Task AttachUsbipAndRunDocker(CancellationToken stoppingToken)
        {
            // Need to warm up WSL before
            ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution}", 5000, ignoreError: true);

            // Check that the busid from configuration is in the state
            foreach (var hardware in Runner.OverallConfiguration!.Hardware)
            {
                bool found = false;
                foreach (var device in Runner.State.Devices)
                {
                    if (hardware.UsbId == device.BusId)
                    {
                        _logger.LogInformation($"Found {hardware.UsbId}, will bind the device.");
                        found = true;
                        var ret = UsbipProcessor.Bind(hardware.UsbId);
                        if (!ret)
                        {
                            _logger.LogError($"Error binding device with busid {hardware.UsbId} to usbipd.");
                            break;
                        }

                        // Attach the device
                        Thread processThread = new Thread(async () => await UsbipProcessor.Attach(hardware.UsbId, true, stoppingToken));
                        processThread.Start();

                        break;
                    }
                }

                if (!found)
                {
                    _logger.LogError($"Device with busid {hardware.UsbId} not found in state.");
                    Runner.ErrorCode = ErrorCode.DeviceNotFound;
                    return;
                }
            }

            // Allow a bit of time for the devices to be attached
            await Task.Delay(5000);

            // Run the docker container in wsl
            await RunDockerContainer(stoppingToken);
        }

        public async Task RunDockerContainer(CancellationToken stoppingToken = default)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "wsl";
                string args = $"-d {Runner.OverallConfiguration.Config.WslDistribution} docker run -e AZP_URL=\"https://dev.azure.com/{Runner.OverallConfiguration.Config.Org}\" " +
                    $"-e AZP_TOKEN=\"{Runner.OverallConfiguration.Config.Token}\" " +
                    $"-e AZP_POOL=\"{Runner.OverallConfiguration.Config.Pool}\" " +
                    $"-e AZP_AGENT_NAME=\"{Runner.OverallConfiguration.Config.AgentName}\" ";
                // Adding al the cgroup rules
                foreach (var hardware in Runner.OverallConfiguration!.Hardware.Select(m => m.CGroup).Distinct())
                {
                    args += $"--device-cgroup-rule='c {hardware}:* rmw' ";
                }

                // Adding each mounting point for the serial ports
                foreach (var hardware in Runner.OverallConfiguration!.Hardware.Select(m => m.Port).Distinct())
                {
                    args += $"-v {hardware}:{hardware} ";
                }

                var pathConfig = ProcessHelpers.ConvertToWslPath(Path.GetDirectoryName(Runner.Options.ConfigHardwareFilePath));
                args += $"-v {pathConfig}:/azp/config {Runner.OverallConfiguration.Config.DockerImage}";

                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data?.Replace("\0", ""));
                process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data?.Replace("\0", ""));

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(stoppingToken);
                _logger.LogInformation($"Process exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while running the external process: {ex.Message}");
                Runner.ErrorCode = ErrorCode.WslFails;
            }
        }
    }
}
