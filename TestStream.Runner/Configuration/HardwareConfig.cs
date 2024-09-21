using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestStream.Runner.Configuration
{
    internal class HardwareConfig
    {
        [JsonPropertyName("capabilities")]
        public Dictionary<string, string> Capabilities { get; set; }
    }
}
