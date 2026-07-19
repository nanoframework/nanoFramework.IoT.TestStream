# Native agent installation

## Prerequisites

Before attempting a first install, you will need to install these dependencies on your PC:

* [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download).
* [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48).
* Optional Windows updates and/or manual installation of serial port drivers for your nanoFramework-compatible device(s).

Also make sure that a maintainer has granted you a Personal Access Token.

## Agent installation

Download the [latest agent installation runner](https://github.com/microsoft/azure-pipelines-agent/releases) and extract it to `C:\agents\`.

> [!Important]
> By default, the agent will advertise almost all your environment variables, including the username, some key directories, and many other elements. You can get rid of this by setting up the VSO_AGENT_IGNORE environment variable. Run the script with this specific setup to create this environment variable **before** you set up the configuration.

Download this latest repository source.
From your source download directory, open the terminal as administrator, then:

(you may need to change the execeution properties in windowsdeveloper options and/or `Set-ExecutionPolicy Bypass -Scope Process`, ) until we have signed the file

Copy the [capabilities.ps1](./agent/capabilities.ps1) file to the `C:\agents` folder, and then run:
```powershell
.\capabilities.ps1 -IgnoreAllEnv $True -SkipCapabilities $True
```

Then, you can run `./config.cmd`, where you will be prompted for:

* the server URL: `https://dev.azure.com/nanoframework`
* the authentication, use the default PAT
* paste your Personal Access Token when asked
* the agent pool is: `TestStream`
* the agent name is: `<your github id>-testrunner`
* use the default `_work` directory
* select whether you want to install the agent as a service





## Setup agent capabilities

Once you've run the previous configuration, you'll need to create a `configuration.json` file in the `C:\agents` directory (or wherever you placed the directory).

The configuration should reflect the devices you have and the associated serial ports. As an example:

```json
{
  "capabilities": {
    "ESP32_REV0": "COM7"
  }
}
```

You can add as many nanoFramework-compatible hardware devices as you want. It is important to set the serial port used properly for each device added.

> [!Important]
> You can also share the same hardware device with different nf-interpreter versions. Although they'll share the same serial port, at a single point in time only 1 remote pipeline is running, so there won't be any conflict with the serial ports used.

Then in PowerShell, run the following commands:

```powershell
cd C:\agents
$env:AZP_TOKEN="yourPersonalAccessToken"
.\capabilities.ps1
```

The token will only be used during capability setup, and will then be deleted from the environment and from a temporary file that is created.

> [!Important]
> You will have to run this script every time you change the capabilities, meaning, adding or removing hardware.

## Run the agent

Depending on what you have chosen during the installation, it will either run as a service, or on demand. You could also create an OS task for your specific needs.

> [!Important]
> When the Personal Access Token expires, you will need to rerun the configuration with your new one, there will not be a warning that it has expired.
