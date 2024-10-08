// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace nanoFramework.IoT.TestRunner.UsbIp
{
    /// <summary>
    /// Represents a usbipd processor.
    /// </summary>
    internal class UsbipProcessor
    {
        private static ILogger Logger { get; set; } = Runner.Logger;

        /// <summary>
        /// Gets the state of the usbipd.
        /// </summary>
        /// <returns>A valid state or null in case of problem.</returns>
        public static State? GetState()
        {
            State state = null!;
            try
            {
                string output = string.Empty;
                Process process = new Process();
                process.StartInfo.FileName = "usbipd.exe";
                process.StartInfo.Arguments = "state";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, e) => output += e.Data;
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.LogError(e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                state = JsonSerializer.Deserialize<State>(output);
                Logger.LogInformation($"Process usbpid state exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"An error occurred while running usbpid state: {ex.Message}");
            }

            return state;
        }

        /// <summary>
        /// Bind a device to usbipd.
        /// </summary>
        /// <param name="busid">A valid usb id.</param>
        /// <returns>True for success.</returns>
        public static bool Bind(string busid)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "usbipd.exe";
                process.StartInfo.Arguments = $"bind -b {busid} --force";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                process.WaitForExit();
                Logger.LogInformation($"Process usbpid bind exited with code {process.ExitCode}");
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"An error occurred while running usbpid bind: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attach a device to usbipd and reattach in case the device is unplugged and replugged.
        /// </summary>
        /// <param name="busid">A valid usb id.</param>
        /// <param name="cancellationToken">A cancellation token to stop the auto attach.</param>
        /// <returns>A task.</returns>
        public static async Task Attach(string busid, bool autoAttach, CancellationToken cancellationToken = default)
        {
            string output = string.Empty;
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "usbipd.exe";
                process.StartInfo.Arguments = $"attach --wsl -b {busid} {(autoAttach ? "--auto-attach" : string.Empty)}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output += e.Data;
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.LogError(e.Data);
                    }
                };

                process.Start();

                // We don't need to redirect, keeping for debug purposes
#if DEBUG
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
#endif

                if (autoAttach)
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                else
                {
                    process.WaitForExit();
                }

                Logger.LogInformation($"Process usbpid attach exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"An error occurred while running usbpid attach: {ex.Message}");
                Runner.ErrorCode = ErrorCode.UsbipAttachError;
            }
        }
    }
}
