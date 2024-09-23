// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommandLine;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using TestStream.Runner;
using TestStream.Runner.Configuration;
using TestStream.Runner.Helpers;
using TestStream.Runner.UsbIp;

/// <summary>
/// Reprensent the main program.
/// </summary>
public class Program
{
    private static OverallConfiguration? _configuration;
    private static HardwareConfig? _hardwareConfiguration;
    private static State? _state;
    private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private static int _returnvalue = 0;

    /// <summary>
    /// Gets the commandline options.
    /// </summary>
    public static CommandlineOptions Options { get; internal set; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    public static ILogger Logger { get; internal set; }

    /// <summary>
    /// Main program entry point.
    /// </summary>
    public static int Main(string[] args)
    {
        Parser.Default.ParseArguments<CommandlineOptions>(args)
                               .WithParsed<CommandlineOptions>(RunLogic)
                               .WithNotParsed(HandleErrors);
        return _returnvalue;
    }

    /// <summary>
    /// Run the logic of the app with the given parameters.
    /// </summary>
    /// <param name="o">Parsed commandline options.</param>
    private static void RunLogic(CommandlineOptions o)
    {
        Options = o;

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(o.Verbosity); // Set the desired log level here
        });
        Logger = loggerFactory.CreateLogger<Program>();

        // Check the configuration
        if (!File.Exists(o.ConfigFilePath))
        {
            Logger.LogError($"Configuration file not found: {o.ConfigFilePath}");
            _returnvalue = 1;
            return;
        }

        // Check if the path to the hardware configuration file is different than the overall configuration
        if (Path.GetFullPath(o.ConfigFilePath) == Path.GetFullPath(o.ConfigHardwareFilePath))
        {
            Logger.LogError("The path to the hardware configuration file is the same as the overall configuration file. You **must** use different path for security reasons.");
            _returnvalue = 1;
            return;
        }

        // Check the configuration
        if (!File.Exists(o.ConfigHardwareFilePath))
        {
            // Creates any missing directories
            Directory.CreateDirectory(Path.GetDirectoryName(o.ConfigHardwareFilePath)!);
        }
        else
        {
            try
            {
                string jsonString = File.ReadAllText(o.ConfigHardwareFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _hardwareConfiguration = JsonSerializer.Deserialize<HardwareConfig>(jsonString, options);

                if (_hardwareConfiguration == null)
                {
                    Logger.LogError("Hardware configuration did not deserialized successfully.");
                }
                else
                {
                    Logger.LogInformation("Configuration deserialized successfully.");

                    // Print the capabilities
                    foreach (var capability in _hardwareConfiguration.Capabilities)
                    {
                        Logger.LogDebug($"Key: {capability.Key}, Value: {capability.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"An error occurred while deserializing the JSON file: {ex.Message}");
            }
        }

        try
        {
            string jsonString = File.ReadAllText(o.ConfigFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _configuration = JsonSerializer.Deserialize<OverallConfiguration>(jsonString, options);
            if (_configuration != null)
            {
                Logger.LogInformation("Configuration deserialized successfully.");
            }
            else
            {
                Logger.LogError("Configuration is null or not valid.");
                _returnvalue = 1;
                return;
            }

            // Check that there is a token
            if (string.IsNullOrEmpty(_configuration.Config.Token))
            {
                Logger.LogError("Token is not set in the configuration file.");
                _returnvalue = 1;
                return;
            }

            // Check that there is a github id
            if (string.IsNullOrEmpty(_configuration.Config.GithubId))
            {
                Logger.LogError("GithubId is not set in the configuration file.");
                _returnvalue = 1;
                return;
            }

            // Check that there is an organization
            if (string.IsNullOrEmpty(_configuration.Config.Org))
            {
                Logger.LogError("Organization is not set in the configuration file.");
                _returnvalue = 1;
                return;
            }

            // Check that there is a pool
            if (string.IsNullOrEmpty(_configuration.Config.Pool))
            {
                Logger.LogError("Pool is not set in the configuration file.");
                _returnvalue = 1;
                return;
            }

            // Check if the agent name is set
            if (string.IsNullOrEmpty(_configuration.Config.AgentName))
            {
                _configuration.Config.AgentName = _configuration.Config.GithubId;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error occurred while deserializing the JSON file: {ex.Message}");
        }

        // Get the state of usbipd
        _state = UsbipProcessor.GetState();
        if (_state == null)
        {
            Logger.LogError("Can't get usbipd state. Make sure usbipd is properly installed.");
            _returnvalue = 1;
            return;
        }

        if (o.Setup)
        {
            CreateSetup();
        }
        else
        {
            AttachUsbipAndRunDocker();

            // Wait for a key press to exit
            ConsoleHelpers.WriteUserAction("Press any key to exit...");
            Console.ReadKey();
            _cancellationTokenSource.Cancel();
        }
    }

    /// <summary>
    /// On parameter errors, we set the returnvalue to 1 to indicated an error.
    /// </summary>
    /// <param name="errors">List or errors (ignored).</param>
    private static void HandleErrors(IEnumerable<Error> errors)
    {
        _returnvalue = 1;
    }

    private static void CreateSetup()
    {
        var previousState = _state;
        ConsoleHelpers.WriteDash("Please plug in the device you want to use.");
        ConsoleHelpers.WriteUserAction("Press any key when plugged");
        Console.ReadKey();
        _state = UsbipProcessor.GetState();
        // Find the new device
        Device? newDevice = null;

        foreach (var device in _state.Devices)
        {
            bool found = false;
            foreach (var previousDevice in previousState.Devices)
            {
                if (device.BusId == previousDevice.BusId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                newDevice = device;
                break;
            }
        }

        if (newDevice == null)
        {
            ConsoleHelpers.WriteError("No new device found. Please retry running the setup.");
            _returnvalue = 1;
            return;
        }

        ConsoleHelpers.WriteDash($"New device found: {newDevice?.Description} with busid {newDevice?.BusId}.");
        Console.WriteLine("Now, binding and attaching the device to usbipd and checking the serial port.");

        // Warm up wsl
        RunCommand("wsl", $"-d {_configuration.Config.WslDistribution}", 5000);

        var ports = string.Empty;
        ports += RunCommand("wsl", $"-d {_configuration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyACM\"");
        ports += RunCommand("wsl", $"-d {_configuration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyUSB\"");

        if (UsbipProcessor.Bind(newDevice!.BusId))
        {
            UsbipProcessor.Attach(newDevice!.BusId, false).GetAwaiter().GetResult();
            Console.WriteLine("Device attached to usbipd.");
        }
        else
        {
            ConsoleHelpers.WriteError($"Error binding device with busid {newDevice!.BusId} to usbipd.");
            _returnvalue = 1;
            return;
        }

        Console.WriteLine("Checking the serial port again.");
        // We need to let the time to WSL kernel to see the new hardware
        Thread.Sleep(2000);
        var newports = string.Empty;
        newports += RunCommand("wsl", $"-d {_configuration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyACM\"");
        newports += RunCommand("wsl", $"-d {_configuration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyUSB\"");

        // Find the new port created
        var newPort = string.Empty;
        foreach (var port in newports.Split('\n'))
        {
            if (!ports.Contains(port))
            {
                newPort = port;
                break;
            }
        }

        if (newPort != string.Empty)
        {
            newPort = newPort.Trim('\r');
            ConsoleHelpers.WriteWarning($"New port found: {newPort}");
        }
        else
        {
            ConsoleHelpers.WriteError("No new port found. Please retry running the setup.");
            _returnvalue = 1;
            return;
        }

        // Checking which cgroup is the device part of
        var cgroup = RunCommand("wsl", $"-d {_configuration.Config.WslDistribution} -- /bin/bash -c \"ls -al /dev/{newPort}\"");
        int cgroupint = -1;
        try
        {
            var split = cgroup.Split(' ');
            cgroupint = int.Parse(split[4].Trim(','));
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteError($"Error parsing cgroup: {ex.Message}");
            _returnvalue = 1;
            return;
        }

        ConsoleHelpers.WriteWarning($"Device is part of cgroup {cgroupint}");

        ConsoleHelpers.WriteDash(string.Empty);
        ConsoleHelpers.WriteUserAction("Please write the exact firmware of the device and press enter:");
        var firmware = ReadLineWithDisplay();
        Console.WriteLine($"Firmware: {firmware}");

        // Create the hardware configuration if needed
        if (_configuration!.Hardware == null)
        {
            _configuration!.Hardware = new List<Hardware>();
        }

        // Check if the device is already in the configuration
        bool foundHardware = false;
        foreach (var hardware in _configuration!.Hardware)
        {
            if (hardware.UsbId == newDevice.BusId)
            {
                ConsoleHelpers.WriteWarning("Device already in configuration.");
                // Replacing the other values
                hardware.CGroup = cgroupint;
                hardware.Firmware = firmware;
                hardware.Port = $"/dev/{newPort}";
                foundHardware = true;
            }
        }

        if (!foundHardware)
        {
            _configuration!.Hardware.Add(new Hardware() { UsbId = newDevice.BusId, Port = newPort, Firmware = firmware, CGroup = cgroupint });
        }

        // Save the configuration
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(Options.ConfigFilePath, JsonSerializer.Serialize(_configuration, options));

        // Make sure the configuration class is created
        if (_hardwareConfiguration == null)
        {
            _hardwareConfiguration = new HardwareConfig();
            _hardwareConfiguration.Capabilities = new Dictionary<string, string>();
        }

        // Write also the agent configuration capabilities
        // We do not override anything as it is possible to setup multiple firmware with the same serial port
        // The adjustment will have to be done by the user
        _hardwareConfiguration.Capabilities.Add(firmware, $"/dev/{newPort}");

        File.WriteAllText(Options.ConfigHardwareFilePath, JsonSerializer.Serialize(_hardwareConfiguration, options));

        // Checking if the docker image is built or not, if not, build it
        var images = RunCommand("wsl", $"-d {_configuration.Config.WslDistribution} docker image inspect {_configuration.Config.DockerImage}");
        images = images.Trim('\n').Trim('\r');
        if (images == "[]")
        {
            ConsoleHelpers.WriteDash(string.Empty);
            ConsoleHelpers.WriteWarning("Docker image not found, building it. This will take some time, so relax and seat back!");
            string pathToDockerfile = Path.GetDirectoryName(Options.ConfigFilePath);
            pathToDockerfile = ConvertToWslPath(pathToDockerfile);
            RunCommand("wsl", $"-d {_configuration.Config.WslDistribution} docker build -t {_configuration.Config.DockerImage} -f {pathToDockerfile}/azp-agent-linux.dockerfile {pathToDockerfile}", outputConsole: true);
            Console.WriteLine("Docker image built.");
        }
        else
        {
            Console.WriteLine("Docker image found.");
        }

        ConsoleHelpers.WriteDash("Setup completed successfully.");
        Console.WriteLine("Run the setup again to add another device.");
    }

    private static void AttachUsbipAndRunDocker()
    {
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
                        Logger.LogError($"Error binding device with busid {hardware.UsbId} to usbipd.");
                        break;
                    }

                    // Attach the device
                    Thread processThread = new Thread(async () => await UsbipProcessor.Attach(hardware.UsbId, true, _cancellationTokenSource.Token));
                    processThread.Start();

                    break;
                }
            }

            if (!found)
            {
                Logger.LogError($"Device with busid {hardware.UsbId} not found in state.");
            }
        }

        // Allow a bit of time for the devices to be attached
        Thread.Sleep(5000);

        // Run the docker container in wsl
        Thread processDocker = new Thread(async () => await RunDockerContainer(_cancellationTokenSource.Token));
        processDocker.Start();
    }

    public static string RunCommand(string command, string arguments, int wait = Timeout.Infinite, bool outputConsole = false)
    {
        string output = string.Empty;
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output += e.Data + Environment.NewLine;
                    if (outputConsole)
                    {
                        Console.WriteLine(e.Data);
                    }
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (outputConsole)
                    {
                        Console.WriteLine(e.Data);
                    }
                    else
                    {
                        Logger.LogError(e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit(wait);

            Logger.LogInformation($"Process exited with code {process.ExitCode}");
        }
        catch (Exception ex)
        {
            // wsl is a specific case as we need to warm wsl and it will return an error as we let it run
            if ((command != "wsl") && (arguments != $"-d {_configuration.Config.WslDistribution}"))
            {
                Program.Logger.LogError($"An error occurred while running {command} {arguments}: {ex.Message}");
            }
        }

        return output;
    }

    public static async Task RunDockerContainer(CancellationToken token = default)
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "wsl";
            string args = $"-d {_configuration.Config.WslDistribution} docker run -e AZP_URL=\"https://dev.azure.com/{_configuration.Config.Org}\" -e AZP_TOKEN=\"{_configuration.Config.Token}\" -e AZP_POOL=\"{_configuration.Config.Pool}\" -e AZP_AGENT_NAME=\"{_configuration.Config.AgentName}\" ";
            // Adding al the cgroup rules
            foreach (var hardware in _configuration!.Hardware.Select(m => m.CGroup).Distinct())
            {
                args += $"--device-cgroup-rule='c {hardware}:* rmw' ";
            }

            // Adding each mounting point for the serial ports
            foreach (var hardware in _configuration!.Hardware.Select(m => m.Port).Distinct())
            {
                args += $"-v {hardware}:{hardware} ";
            }

            var pathConfig = ConvertToWslPath(Path.GetDirectoryName(Options.ConfigHardwareFilePath));
            args += $"-v {pathConfig}:/azp/config {_configuration.Config.DockerImage}";

            process.StartInfo.Arguments = args;
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

    private static string ConvertToWslPath(string windowsPath)
    {
        if (string.IsNullOrWhiteSpace(windowsPath))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(windowsPath));
        }

        // Replace backslashes with forward slashes
        string wslPath = windowsPath.Replace('\\', '/');

        // Extract the drive letter and convert it to lowercase
        char driveLetter = char.ToLower(wslPath[0]);

        // Remove the colon and prepend /mnt/
        wslPath = $"/mnt/{driveLetter}{wslPath.Substring(2)}";

        return wslPath;
    }

    // Method to read input while displaying it
    private static string ReadLineWithDisplay()
    {
        string input = string.Empty;
        int charValue;

        while ((charValue = Console.Read()) != '\n')
        {
            char c = (char)charValue;

            // Handle carriage return in case of Windows-style line endings
            if (c == '\r')
            {
                break;
            }

            // Handle backspace
            if (c == '\b' && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                // Erase the last character
                Console.Write("\b \b");
            }
            else if (c != '\b')
            {
                input += c;
                Console.Write(c);
            }
        }
        Console.WriteLine(); // Move to the next line after Enter is pressed
        return input;
    }
}