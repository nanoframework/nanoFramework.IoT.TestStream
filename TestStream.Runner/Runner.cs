// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using nanoFramework.IoT.TestRunner.Configuration;
using nanoFramework.IoT.TestRunner.Helpers;
using nanoFramework.IoT.TestRunner.TerminalGui;
using nanoFramework.IoT.TestRunner.UsbIp;
using System.Text.Json;
using System.Text.RegularExpressions;
using Terminal.Gui;

namespace nanoFramework.IoT.TestRunner
{
    /// <summary>
    /// Reprensent the main program.
    /// </summary>
    public class Runner
    {
        private static HardwareConfig? _hardwareConfiguration;
        private static IHost _host;

        /// <summary>
        /// Gets or sets the return value.
        /// </summary>
        public static ErrorCode ErrorCode { get; set; } = ErrorCode.None;

        /// <summary>
        /// Gets or sets the state of usbipd.
        /// </summary>
        public static State? State { get; set; }

        /// <summary>
        /// Gets the overall configuration.
        /// </summary>
        public static OverallConfiguration? OverallConfiguration { get; internal set; }

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
            // Get things prepared for the logger and service
            var builder = Host.CreateApplicationBuilder(args);

            // To be adjusted for the proper level, here mainly for debugging
            builder.Logging.AddEventLog(
                eventLogSettings =>
                {
                    eventLogSettings.LogName = "Application";
                    eventLogSettings.SourceName = "TestStream.Runner";
                    eventLogSettings.Filter = (category, level) =>
                    {
                        return level >= LogLevel.Information;
                    };
                });

            builder.Services.AddHostedService<Worker>();
            LoggerProviderOptions.RegisterProviderOptions<
                EventLogSettings, EventLogLoggerProvider>(builder.Services);

            _host = builder.Build();

            Logger = _host.Services.GetRequiredService<ILogger<Runner>>();


            Parser.Default.ParseArguments<CommandlineOptions>(args)
                                   .WithParsed<CommandlineOptions>(RunLogic)
                                   .WithNotParsed(HandleErrors);
            return (int)ErrorCode;
        }

        /// <summary>
        /// Run the logic of the app with the given parameters.
        /// </summary>
        /// <param name="o">Parsed commandline options.</param>
        private static void RunLogic(CommandlineOptions o)
        {
            Options = o;

            // Check the configuration
            if (!File.Exists(o.ConfigFilePath))
            {
                // Check if we can use the default configuration file in the same directory
                if (File.Exists(Path.Combine(AppContext.BaseDirectory, "agent", "runner-configuration.json")))
                {
                    o.ConfigFilePath = Path.Combine(AppContext.BaseDirectory, "agent", "runner-configuration.json");
                }
                else
                {
                    Logger.LogError($"Configuration file not found: {o.ConfigFilePath}");
                    ErrorCode = ErrorCode.ConfigurationError;
                    return;
                }
            }

            // Check if the path to the hardware configuration file is different than the overall configuration
            if (!string.IsNullOrEmpty(o.ConfigHardwareFilePath) && (Path.GetFullPath(o.ConfigFilePath) == Path.GetFullPath(o.ConfigHardwareFilePath)))
            {
                Logger.LogError("The path to the hardware configuration file is the same as the overall configuration file. You **must** use different path for security reasons.");
                ErrorCode = ErrorCode.ConfigurationError;
                return;
            }

            // Check the configuration
            if (!File.Exists(o.ConfigHardwareFilePath))
            {
                // Chedck if the path is not empty
                if (string.IsNullOrEmpty(o.ConfigHardwareFilePath))
                {
                    // Create a default path in /agent/config
                    o.ConfigHardwareFilePath = Path.Combine(AppContext.BaseDirectory, "agent", "config", "configuration.json");
                }

                // Creates any missing directories
                Directory.CreateDirectory(Path.GetDirectoryName(o.ConfigHardwareFilePath)!);
            }

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

            try
            {
                string jsonString = File.ReadAllText(o.ConfigFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                OverallConfiguration = JsonSerializer.Deserialize<OverallConfiguration>(jsonString, options);
                if (OverallConfiguration != null)
                {
                    Logger.LogInformation("Configuration deserialized successfully.");
                }
                else
                {
                    Logger.LogError("Configuration is null or not valid.");
                    ErrorCode = ErrorCode.ConfigurationError;
                    return;
                }

                if (!o.Setup)
                {
                    // Check that there is a token
                    if (string.IsNullOrEmpty(OverallConfiguration.Config.Token))
                    {
                        Logger.LogError("Token is not set in the configuration file.");
                        ErrorCode = ErrorCode.ConfigurationError;
                        return;
                    }

                    // Check that there is a github id
                    if (string.IsNullOrEmpty(OverallConfiguration.Config.GithubId))
                    {
                        Logger.LogError("GithubId is not set in the configuration file.");
                        ErrorCode = ErrorCode.ConfigurationError;
                        return;
                    }

                    // Check that there is an organization
                    if (string.IsNullOrEmpty(OverallConfiguration.Config.Org))
                    {
                        Logger.LogError("Organization is not set in the configuration file.");
                        ErrorCode = ErrorCode.ConfigurationError;
                        return;
                    }

                    // Check that there is a pool
                    if (string.IsNullOrEmpty(OverallConfiguration.Config.Pool))
                    {
                        Logger.LogError("Pool is not set in the configuration file.");
                        ErrorCode = ErrorCode.ConfigurationError;
                        return;
                    }
                }

                // Check if the agent name is set
                if (string.IsNullOrEmpty(OverallConfiguration.Config.AgentName))
                {
                    OverallConfiguration.Config.AgentName = OverallConfiguration.Config.GithubId;
                }

            }
            catch (Exception ex)
            {
                Logger.LogError($"An error occurred while deserializing the JSON file: {ex.Message}");
            }

            if (o.Setup)
            {
                CreateSetup();
            }
            else
            {
                // Get the state of usbipd
                State = UsbipProcessor.GetState();
                if (State == null)
                {
                    Logger.LogError("Can't get usbipd state. Make sure usbipd is properly installed.");
                    ErrorCode = ErrorCode.UsbipBindError;
                    return;
                }

                _host.Run();
            }
        }


        /// <summary>
        /// On parameter errors, we set the returnvalue to 1 to indicated an error.
        /// </summary>
        /// <param name="errors">List or errors (ignored).</param>
        private static void HandleErrors(IEnumerable<Error> errors)
        {
            ErrorCode = ErrorCode.Other;
        }

        private static void CreateSetup()
        {
            Application.Init();

            // Stop the service and install it if not
            Application.Run<ServiceWindow>();

            // Make sure we have the proper OverallConfiguration.Config
            ConfigationWindow.OverallConfiguration = OverallConfiguration;
            Application.Run<ConfigationWindow>();

            // Save the configuration
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            OverallConfiguration = ConfigationWindow.OverallConfiguration;
            File.WriteAllText(Options.ConfigFilePath, JsonSerializer.Serialize(OverallConfiguration, options));

            // Going through the setup for WSL if needed, check with 'wsl -v' if it is installed
            var isInWSL = ProcessHelpers.RunCommand("wsl", "-v");
            bool isInstalled = false;
            if (!string.IsNullOrEmpty(isInWSL))
            {
                // Check if the version is 2.x.x.x
                isInstalled = Regex.IsMatch(isInWSL, @"WSL version: 2\.\d+\.\d+\.\d+");
            }

            if (!isInstalled)
            {
                var res = MessageBox.Query("WSL Installation", "WSL is not installed. Do you want to install WSL2 before continuing with Docker and all the needed elements?", "Yes", "No");
                if (res == 0)
                {
                    var install = ProcessHelpers.RunCommand("powershell.exe",
                        $"-ExecutionPolicy Restricted -ExecutionPolicy Bypass -File \"{Path.Combine(AppContext.BaseDirectory, "agent", "install.ps1")}\" -WSLDistribution {OverallConfiguration.Config.WslDistribution}",
                        outputConsole: true,
                        useShell: true);
                }
            }
            else
            {
                // Check if the distribution is installed
                var installed = ProcessHelpers.RunCommand("wsl", "-l -q");
                if (!installed.Contains(OverallConfiguration.Config.WslDistribution))
                {
                    var res = MessageBox.Query("WSL Distribution Installation", "WSL distribution is not installed. Do you want to install it before continuing with Docker and all the needed elements?", "Yes", "No");
                    if (res == 0)
                    {
                        var install = ProcessHelpers.RunCommand("powershell.exe",
                            $"-ExecutionPolicy Restricted -ExecutionPolicy Bypass -File \"{Path.Combine(AppContext.BaseDirectory, "agent", "install.ps1")}\" -WSLDistribution {OverallConfiguration.Config.WslDistribution}",
                            outputConsole: true,
                            useShell: true);

                        res = MessageBox.Query("WSL Distribution Installation", "If you saw that the system needs to be rebooted, please click reboot and rerun this setup.", "Reboot", "Continue");
                        if (res == 0)
                        {
                            ProcessHelpers.RunCommand("shutdown", "/r /t 0", useShell: true);
                        }
                    }
                }
            }

            // Check if USBIP is installed
            var usbipInstalled = ProcessHelpers.RunCommand("usbipd", "--version");
            if (!usbipInstalled.StartsWith("4.3.0"))
            {
                var res = MessageBox.Query("USBIP Installation", "USBIP is not installed. Do you want to install it before continuing with Docker and all the needed elements?", "Yes", "No");
                if (res == 0)
                {
                    var install = ProcessHelpers.RunCommand("powershell.exe",
                        $"-ExecutionPolicy Restricted -ExecutionPolicy Bypass -File \"{Path.Combine(AppContext.BaseDirectory, "agent", "install.ps1")}\" -SkipWSLInstallation -SkipDockerInstallation",
                        outputConsole: true,
                        useShell: true);
                }
            }

            // Check if Docker is installed
            var dockerInstalled = ProcessHelpers.RunCommand("wsl", "docker --version");
            if (string.IsNullOrEmpty(dockerInstalled))
            {
                var res = MessageBox.Query("Doncker Installation", "Docker is not installed. Do you want to install it before continuing with Docker and all the needed elements?", "Yes", "No");
                if (res == 0)
                {
                    var install = ProcessHelpers.RunCommand("powershell.exe",
                        $"-ExecutionPolicy Restricted -ExecutionPolicy Bypass -File \"{Path.Combine(AppContext.BaseDirectory, "agent", "install.ps1")}\" -SkipWSLInstallation -SkipUSBIPDInstallation",
                        outputConsole: true,
                        useShell: true);
                }
            }

            var previousHardware = OverallConfiguration!.Hardware;
            Application.Run<DeviceWindow>();

            // Save the configuration
            File.WriteAllText(Options.ConfigFilePath, JsonSerializer.Serialize(OverallConfiguration,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            ));

            // Make sure the configuration class is created
            if (_hardwareConfiguration == null)
            {
                _hardwareConfiguration = new HardwareConfig();
                _hardwareConfiguration.Capabilities = new Dictionary<string, string>();
            }

            // Write also the agent configuration capabilities
            // We do not override anything as it is possible to setup multiple firmware with the same serial port
            // The adjustment will have to be done by the user
            if (DeviceWindow.NewHardware is not null)
            {
                _hardwareConfiguration.Capabilities.Add(DeviceWindow.NewHardware.Firmware, DeviceWindow.NewHardware.Port);
            }

            File.WriteAllText(Options.ConfigHardwareFilePath, JsonSerializer.Serialize(_hardwareConfiguration, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            Application.Run<DockerBuildWindows>();

            Application.Shutdown();
        }
    }
}