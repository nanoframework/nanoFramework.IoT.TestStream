using CommandLine;
using Microsoft.Extensions.Logging;

namespace TestStream.Runner.Configuration
{
    /// <summary>
    /// Represent the commandline options.
    /// </summary>
    public class CommandlineOptions
    {
        /// <summary>
        /// Gets or sets the configuration json file path.
        /// </summary>
        [Option('c', "configuration", Required = true, HelpText = "Path to the configuration file.")]
        public string ConfigFilePath { get; set; }
        
        /// <summary>
        /// Gets or sets the configuration hardware json file path.
        /// </summary>
        [Option('d', "hardwareconfig", Required = true, HelpText = "Path to the hardware configuration file.")]
        public string ConfigHardwareFilePath { get; set; }

        /// <summary>
        /// Gets or sets the setup flag.
        /// </summary>
        [Option('s', "setup", Required = false, HelpText = "Setup the environment.")]
        public bool Setup { get; set; }

        /// <summary>
        /// Gets or sets the verbosity level.
        /// </summary>
        [Option('v', "verbosity", Required = false, HelpText = "Sets the verbosity level.")]
        public LogLevel Verbosity { get; set; } = LogLevel.Error;
    }
}
