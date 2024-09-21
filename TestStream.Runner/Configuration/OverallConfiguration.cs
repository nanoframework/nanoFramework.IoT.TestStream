// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestStream.Runner
{
    /// <summary>
    /// Represent a configuration object.
    /// </summary>
    internal class OverallConfiguration
    {
        /// <summary>
        /// Gets or sets the hardware.
        /// </summary>
        public List<Hardware> Hardware { get; set; }

        /// <summary>
        /// Gets or sets the config.
        /// </summary>
        public Config Config { get; set; }
    }
}
