// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace nanoFramework.IoT.TestRunner.Helpers
{
    /// <summary>
    /// Represents a helper class for running processes.
    /// </summary>
    internal class ProcessHelpers
    {
        public delegate void CustomOutput(string output);

        /// <summary>
        /// Runs a command with the specified arguments.
        /// </summary>
        /// <param name="command">The process to run.</param>
        /// <param name="arguments">The process arguments.</param>
        /// <param name="wait">Timeout to wait for the process before killing it.</param>
        /// <param name="outputConsole">True to output the details to the console.</param>
        /// <param name="useShell">True to spone an external shell.</param>
        /// <param name="ignoreError">True to ignore errors.</param>
        /// <returns>The output of the console as a string.</returns>
        public static string RunCommand(string command, string arguments, int wait = Timeout.Infinite, bool outputConsole = false, bool useShell = false, bool ignoreError = false, CustomOutput outPutFunction = null, bool mergeOutputError = false)
        {
            string output = string.Empty;
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = useShell;
                process.StartInfo.RedirectStandardOutput = !useShell;
                process.StartInfo.RedirectStandardError = !useShell;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output += e.Data.Replace("\0", string.Empty) + Environment.NewLine;
                        if (outputConsole)
                        {
                            Console.WriteLine(e.Data.Replace("\0", string.Empty));
                        }

                        outPutFunction?.Invoke(e.Data.Replace("\0", string.Empty));
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        if (outputConsole)
                        {
                            Console.WriteLine(e.Data.Replace("\0", string.Empty));
                        }
                        else if (outPutFunction is null)
                        {
                            Runner.Logger.LogError(e.Data.Replace("\0", string.Empty));
                        }

                        if (mergeOutputError)
                        {
                            output += e.Data.Replace("\0", string.Empty) + Environment.NewLine;
                        }

                        outPutFunction?.Invoke(e.Data.Replace("\0", string.Empty));
                    }
                };

                process.Start();
                if (!useShell)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                process.WaitForExit(wait);

                Runner.Logger.LogInformation($"Process exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                // wsl is a specific case as we need to warm wsl and it will return an error as we let it run
                if (ignoreError)
                {
                    Runner.Logger.LogError($"An error occurred while running {command} {arguments}: {ex.Message}");
                }
            }

            return output;
        }

        /// <summary>
        /// Converts a Windows path to a WSL path.
        /// </summary>
        /// <param name="windowsPath">A Windows path.</param>
        /// <returns>A WSL path.</returns>
        /// <exception cref="ArgumentException">Path cannot be null or empty.</exception>
        public static string ConvertToWslPath(string windowsPath)
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
    }
}
