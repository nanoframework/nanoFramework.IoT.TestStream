// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;

namespace TestStream.Runner.UsbIp
{
    /// <summary>
    /// Represents a usbipd processor.
    /// </summary>
    internal class UsbipProcessor
    {
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

                process.OutputDataReceived += (sender, e) => output+= e.Data;
                process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                state = JsonSerializer.Deserialize<State>(output);
                Console.WriteLine($"Process exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while running the external process: {ex.Message}");
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
                process.StartInfo.Arguments = $"bind -b {busid}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                process.WaitForExit();                
                Console.WriteLine($"Process exited with code {process.ExitCode}");
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while running the external process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attach a device to usbipd and reattach in case the device is unplugged and replugged.
        /// </summary>
        /// <param name="busid">A valid usb id.</param>
        /// <param name="cancellationToken">A cancellation token to stop the auto attach.</param>
        /// <returns>A task.</returns>
        public static async Task Attach(string busid, CancellationToken cancellationToken = default)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "usbipd.exe";
                process.StartInfo.Arguments = $"attach --wsl -b {busid} --auto-attach";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                //process.OutputDataReceived += (sender, e) => output += e.Data;
                process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

                process.Start();
                //process.BeginOutputReadLine();
                //process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);                
                Console.WriteLine($"Process exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while running the external process: {ex.Message}");
            }
        }
    }
}
