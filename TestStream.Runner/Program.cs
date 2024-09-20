// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using TestStream.Runner;
using TestStream.Runner.UsbIp;

/// <summary>
/// Reprensent the main program.
/// </summary>
public class Program
{
    private static Configuration? _configuration;
    private static State? _state;
    private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// Main program entry point.
    /// </summary>
    public static void Main(string[] args)
    {
        // Check if the JSON file path is provided
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide the path to the JSON file as an argument.");
            return;
        }

        string jsonFilePath = args[0];

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"File not found: {jsonFilePath}");
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(jsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _configuration = JsonSerializer.Deserialize<Configuration>(jsonString, options);
            if (_configuration != null)
            {
                Console.WriteLine("Configuration deserialized successfully.");
            }
            else
            {
                Console.WriteLine("Configuration is null or not valid.");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while deserializing the JSON file: {ex.Message}");
        }

        // Get the state of usbipd
        _state = UsbipProcessor.GetState();
        if (_state == null)
        {
            Console.WriteLine("Can't get usbipd state. Make sure usbipd is properly installed.");
            return;
        }

        // Check that the busid from configuration is in the state
        foreach (var hardware in _configuration!.Hardware)
        {
            bool found = false;
            foreach (var device in _state.Devices)
            {
                if (hardware.UsbId == device.BusId)
                {
                    found = true;
                    var ret = UsbipProcessor.Bind(hardware.UsbId);
                    if (!ret)
                    {
                        Console.WriteLine($"Error binding device with busid {hardware.UsbId} to usbipd.");
                        break;
                    }

                    // Attach the device
                    Thread processThread = new Thread(async () => await UsbipProcessor.Attach(hardware.UsbId, _cancellationTokenSource.Token));
                    processThread.Start();

                    break;
                }
            }

            if (!found)
            {
                Console.WriteLine($"Device with busid {hardware.UsbId} not found in state.");
            }
        }

        // Run the docker container in wsl
        Thread processDocker = new Thread(async () => await RunDockerContainer(_cancellationTokenSource.Token));
        processDocker.Start();

        // Wait for a key press to exit
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        _cancellationTokenSource.Cancel();
    }

    public static async Task RunDockerContainer(CancellationToken token =default)
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "wsl";
            process.StartInfo.Arguments = $"docker run -e AZP_URL=\"https://dev.azure.com/nanoframework\" -e AZP_TOKEN=\"{_configuration.Config.Token}\" -e AZP_POOL=\"TestStream\" -e AZP_AGENT_NAME=\"Docker Agent - Linux\" --device-cgroup-rule='c 166:* rmw' -v /dev:/dev --device-cgroup-rule='c 188:* rmw' -v ./config:/azp/config azp-agent:linux";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(token);
            Console.WriteLine($"Process exited with code {process.ExitCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while running the external process: {ex.Message}");
        }
    }
}