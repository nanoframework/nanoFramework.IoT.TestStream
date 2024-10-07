// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.IoT.TestRunner.UsbIp
{
    /// <summary>
    /// Represents a device object.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Gets or sets the bus id.
        /// </summary>
        public string BusId { get; set; }

        /// <summary>
        /// Gets or sets the client ip address.
        /// </summary>
        public object ClientIPAddress { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the instance id.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the device is forced to be shared.
        /// </summary>
        public bool IsForced { get; set; }

        /// <summary>
        /// Gets or sets the GUID.
        /// </summary>
        public string PersistedGuid { get; set; }

        /// <summary>
        /// Gets or sets the sub instance id.
        /// </summary>
        public object StubInstanceId { get; set; }
    }
}
