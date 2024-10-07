// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.IoT.TestRunner.Configuration;
using nanoFramework.IoT.TestRunner.Helpers;
using nanoFramework.IoT.TestRunner.UsbIp;
using Terminal.Gui;
using Label = Terminal.Gui.Label;

namespace nanoFramework.IoT.TestRunner.TerminalGui
{
    internal class DeviceWindow : Window
    {
        private static ListView _statusLabel;
        private static List<string> _status = new();
        private static TextField _firmware;
        private static Button _addButton;
        private static Button _nextButton;

        public static Hardware NewHardware { get; internal set; }

        public DeviceWindow()
        {
            // Reset the new hardware
            NewHardware = null;

            Title = "Setting up the devices";
            // Start placing controls from the second row

            Label labelListeDevices = new Label("List of devices")
            {
                X = 0,
                Y = 0
            };
            Add(labelListeDevices);

            int y = 1;
            // Add existing hardware
            foreach (var hardware in Runner.OverallConfiguration.Hardware)
            {
                var label = new Label($"USB ID: {hardware.UsbId} - Firmware: {hardware.Firmware} - Port: {hardware.Port} - CGroup: {hardware.CGroup}")
                {
                    X = 1,
                    Y = y
                };
                Add(label);
                y += 1;
            }

            // Add an empty label for the status
            _statusLabel = new ListView(_status)
            {
                X = 0,
                Y = y++,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 6
            };

            Add(_statusLabel);

            var firmwareLabel = new Label("Firmware name:")
            {
                X = 1,
                Y = Pos.Bottom(this) - 4
            };

            Add(firmwareLabel);

            // Add a button to add a new device
            _addButton = new Button("Add device")
            {
                X = 1,
                Y = Pos.Bottom(this) - 3,
            };

            // Add a button to go to the next configuration
            _nextButton = new Button("Next")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(this) - 3,
                IsDefault = true
            };

            _addButton.Clicked += AddButtonClicked;

            _nextButton.Clicked += () =>
            {
                Application.RequestStop();
            };

            Add(_addButton, _nextButton);

            // Add a text field for the firmware name
            _firmware = new TextField("")
            {
                X = 16,
                Y = Pos.Bottom(this) - 4,
                Width = Dim.Fill() - 1
            };

            Add(_firmware);
            _addButton.SetFocus();
        }

        private void AddButtonClicked()
        {
            _addButton.Enabled = false;
            _nextButton.Enabled = false;
            ProcessAddDevice();
            _addButton.Enabled = true;
            _nextButton.Enabled = true;
            // for some reasons if this is not done, then it's not possible to click any
            // of the bottons. It would theorythically be possible to stay on this window
            Application.RequestStop();
        }

        private void ProcessAddDevice()
        {
            if (string.IsNullOrEmpty(_firmware.Text.ToString()))
            {
                TerminalHelpers.LogInListView($"Not a valid firmware name{Environment.NewLine}", _status, _statusLabel);
                return;
            }

            Runner.State = UsbipProcessor.GetState();
            var previousState = Runner.State;
            MessageBox.Query("Add device", "Please plug in the device you want to use. This will take a bit of time.", "OK");

            Runner.State = UsbipProcessor.GetState();
            // Find the new device
            Device? newDevice = null;

            foreach (var device in Runner.State.Devices)
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
                TerminalHelpers.LogInListView("No new device found. Please retry running the setup.", _status, _statusLabel);
                Runner.ErrorCode = ErrorCode.DeviceNotFound;
                return;
            }

            TerminalHelpers.LogInListView($"New device found: {newDevice?.Description} with busid {newDevice?.BusId}.", _status, _statusLabel);
            TerminalHelpers.LogInListView($"Now, binding and attaching the device to usbipd and checking the serial port.", _status, _statusLabel);

            // Warm up wsl
            ReportProgress();
            ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution}", 5000, ignoreError: true);
            ReportProgress();

            var ports = string.Empty;
            ports += ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyACM\"");
            ReportProgress();
            ports += ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyUSB\"");
            ReportProgress();

            if (UsbipProcessor.Bind(newDevice!.BusId))
            {
                UsbipProcessor.Attach(newDevice!.BusId, false).GetAwaiter().GetResult();
                ReportProgress();
                TerminalHelpers.LogInListView($"Device attached to usbipd.", _status, _statusLabel);
                ReportProgress();
            }
            else
            {
                TerminalHelpers.LogInListView($"Error binding device with busid {newDevice!.BusId} to usbipd.", _status, _statusLabel);
                Runner.ErrorCode = ErrorCode.UsbipBindError;
                return;
            }

            // We need to let the time to WSL kernel to see the new hardware
            Thread.Sleep(2000);
            var newports = string.Empty;
            newports += ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyACM\"");
            ReportProgress();
            newports += ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution} -- /bin/bash -c \"ls /dev | grep ttyUSB\"");
            ReportProgress();

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
                TerminalHelpers.LogInListView($"New port found: {newPort}", _status, _statusLabel);
                Application.Refresh();
            }
            else
            {
                TerminalHelpers.LogInListView($"No new port found. Please retry running the setup.", _status, _statusLabel);
                Application.Refresh();
                return;
            }

            // Checking which cgroup is the device part of
            var cgroup = ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution} -- /bin/bash -c \"ls -al /dev/{newPort}\"");
            ReportProgress();
            int cgroupint = -1;
            try
            {
                var split = cgroup.Split(' ');
                cgroupint = int.Parse(split[4].Trim(','));
            }
            catch (Exception ex)
            {
                TerminalHelpers.LogInListView($"Error parsing cgroup: {cgroup}", _status, _statusLabel);
                Application.Refresh();
                return;
            }

            TerminalHelpers.LogInListView($"Device is part of cgroup {cgroupint}", _status, _statusLabel);
            Application.Refresh();

            // Create the hardware configuration if needed
            if (Runner.OverallConfiguration!.Hardware == null)
            {
                Runner.OverallConfiguration!.Hardware = new List<Hardware>();
            }

            // Check if the device is already in the configuration
            bool foundHardware = false;
            foreach (var hardware in Runner.OverallConfiguration!.Hardware)
            {
                if (hardware.UsbId == newDevice.BusId)
                {
                    TerminalHelpers.LogInListView($"Device already in configuration, updating it.", _status, _statusLabel);
                    // Replacing the other values
                    hardware.CGroup = cgroupint;
                    hardware.Firmware = _firmware.Text.ToString();
                    hardware.Port = $"/dev/{newPort}";
                    foundHardware = true;

                    NewHardware = hardware;
                    MessageBox.Query("Add device", "Device already in configuration, updating it.", "OK");
                }
            }

            if (!foundHardware)
            {
                var hardware = new Hardware
                {
                    UsbId = newDevice.BusId,
                    CGroup = cgroupint,
                    Firmware = _firmware.Text.ToString(),
                    Port = $"/dev/{newPort}"
                };

                NewHardware = hardware;
                Runner.OverallConfiguration!.Hardware.Add(hardware);
                MessageBox.Query("Add device", "Added new device successfully.", "OK");
            }
        }

        private void ReportProgress()
        {
            _status[_status.Count - 1] = _status[_status.Count - 1] + ".";
            Application.Refresh();
        }
    }
}
