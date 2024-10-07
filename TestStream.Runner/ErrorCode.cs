// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.IoT.TestRunner
{
    /// <summary>
    /// Represents the error codes that can be returned by the application.
    /// </summary>
    public enum ErrorCode
    {
        None = 0,
        Other = 1,
        WslFails = 1001,
        UsbipBindError = 1002,
        UsbipAttachError = 1003,
        DeviceNotFound = 1004,
        DockerImageNotFound = 1005,
        DockerImageBuildError = 1006,
        DockerImageBuilt = 1007,
        ConfigurationError = 1008,
    }
}
