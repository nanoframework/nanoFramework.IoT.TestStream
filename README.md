[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=nanoFramework.IoT.TestStream&metric=alert_status)](https://sonarcloud.io/dashboard?id=nanoFramework.IoT.TestStream) [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=nanoFramework.IoT.TestStream&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=nanoFramework.IoT.TestStream) [![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![NuGet](https://img.shields.io/nuget/dt/nanoFramework.IoT.TestStream.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.IoT.TestStream/) [![#yourfirstpr](https://img.shields.io/badge/first--timers--only-friendly-blue.svg)](https://github.com/nanoframework/Home/blob/main/CONTRIBUTING.md) [![Discord](https://img.shields.io/discord/478725473862549535.svg?logo=discord&logoColor=white&label=Discord&color=7289DA)](https://discord.gg/gCyBu8T)

![nanoFramework logo](https://raw.githubusercontent.com/nanoframework/Home/main/resources/logo/nanoFramework-repo-logo.png)

-----

# Welcome to the .NET **nanoFramework** nanoFramework.IoT.TestStream Library repository

The [`TestStream.Runner`](TestStream.Runner) application is designed to manage device connections within a WSL environment, run the ADO agent. It has a feature to setup new devices and is specifically focusing on identifying new serial ports created when new hardware is connected. It handles errors gracefully and provides feedback to the user through console messages and logging.

The setup feature allows to smoothly add new devices, bind them and attach them in full transparency into WSL. Configuration files are created so that users do not have to worry about the complexity behind.

It is also design to run all the needed background applications and docker containers in full transparency.

An installation script is available allowing an easy and smooth installation of all the needed components.

## Installation of requirements

Clone the repository and go to the [/agent](./agent/) folder and run the [install.ps1](./agent/install.ps1) script. The script **must** run in an elevated privilege PowerShell (Run as administrator).

Parameters are available to allow skipping some of the installations:

* `SkipWSLInstallation` will skip the WSL installation.
* `SkipDockerInstallation` will skip the docker installation in WSL.
* `SkipUSBIPDInstallation` will skip the USBIOD installation on Windows.
* `WSLDistribution` allows you to setup a default WSL name for the installation. The default nam will be "Ubuntu".

Note that an Ubuntu based image is require for this installation script. If you wish to use another distribution, you will have to install docker in it. The rest will work the same way.

## Configuration files

2 configurations files are require:

* A TestStream.Runner configuration file that will be used to setup the devices, the mounting volumes in WSL, the token to connect the agent and the agent configuration. This file is mandatory and needs to contain valid information in its config section. An [example is provided](./agent/runner-configuration.json).
* An agent capabilities configuration file that will be used by the agent to shows its capabilities. This file can be generated during the initial setup.

> [!Important]
> The 2 configuration files cannot be in the same directory for security reasons. It is recommended to place the agent configuration file in a separate directory with no other files. [See the example](./agent/config).

## Setting up new devices

Run the TestStream.Runner with the following arguments in an elevated privilege prompt (Run as administrator):

```shell
TestStream.Runner -d path_to\runner-config.json -h path_to\config\configuration.json -s
```

You will be prompted to plug your device. The setup will takes couple of seconds. You will be prompt for the device firmware. Please refer to [nf-interpreter](https://github.com/nanoframework/nf-interpreter) for the list of firmware.

The TestStream.Runner configuration file will be updated and the agent configuration will be appended. It is indeed possible to have multiple firmware sharing the same serial port. So, please adjust manually the agent configuration file if needed.

Run as many times as you need to add different hardware or firmware the setup.

## Running TestStream.Runner

Run the TestStream.Runner with the following arguments in an elevated privilege prompt (Run as administrator):

```shell
TestStream.Runner -d path_to\runner-config.json -h path_to\config\configuration.json
```

The application will bind and attach the devices into WSL, run the docker container with the proper settings. In case a device is deattached, it will automatically reattach it.

> [!Important]
> In the current state of the application, no restart will be provided in case the container stops or WSL stops.

## Missing elements

[ ] Documentation in this page to build the docker file for the agent. Or possibly add this as an option in the TestRunner.
[ ] Create a robust error handling to restart the container, WSL or any other element when needed.
[ ] Add an option to create a service out of the runner to run in the background.
[ ] Package the agent dockerfile, scripts into the application so that, it can be installed easilly from a nuget.
[ ] Package the TestStream.Runner application as a nuget/dotnet tool for easy installation.
