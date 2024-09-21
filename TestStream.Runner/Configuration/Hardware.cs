// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestStream.Runner
{
    /// <summary>
    /// Represent a hardware object.
    /// </summary>
    public class Hardware
    {
        /// <summary>
        /// Gets or sets the device firmware.
        /// </summary>
        public string Firmware { get; set; }

        /// <summary>
        /// Gets or sets the device port in WSL/Linux.
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Gets or sets the cgroup the device is part of
        /// </summary>
        public int CGroup { get; set; }

        /// <summary>
        /// Gets or sets the usb id of the device.
        /// </summary>
        public string UsbId { get; set; }
    }
}
