﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.IoT.TestRunner.Configuration
{
    /// <summary>
    /// Represent a configuration object.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization.
        /// </summary>
        public string Org { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pool.
        /// </summary>
        public string Pool { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the github id.
        /// </summary>
        public string GithubId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the WSL distribution. Default is Ubuntu.
        /// </summary>
        public string WslDistribution { get; set; } = "Ubuntu";

        /// <summary>
        /// Gets or sets the agent name.
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the docker image.
        /// </summary>
        public string DockerImage { get; set; } = "azp-agent:linux";
    }
}
