// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace TestStream.Runner.Configuration
{
    /// <summary>
    /// Hardware configuration.
    /// </summary>
    public class HardwareConfig
    {
        /// <summary>
        /// Gets or sets the capabilities..
        /// </summary>
        [JsonPropertyName("capabilities")]
        public Dictionary<string, string> Capabilities { get; set; }
    }
}
