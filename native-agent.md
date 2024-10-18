# Native agent installation

Follow the instruction that will looks like this:

![instructions](./docs/native-setup.png)

By default, the agent will advertise almost all your environement variables including the name of the user, some key directories, and many many other elements. You can get rid of this by setting up the VSO_AGENT_IGNORE environement variable. Run the script with this specific setup to create this environement variable **before** you setup the configuration.

```powershell
.\capacities.ps1 -IgnoreAllEnv $True -SkipCapabilities $true
```

Then, you can run the `.\config.cmd` where you will be prompted for:

* the serveur URL: `https://dev.azure.com/nanoframework`
* the authentication, use the default PAT
* paste your PAT token when asked
* the agent poolo is: `TestStream`
* use the default `_work` directory
* select if you want or not install the agent as a service or not

Copy the [capacities.ps1](./agent/capacities.ps1) file to the C`:\agent` folder.

## Pre requirements

You'll need to install on the machine:

* [.NET 8.0](https://dotnet.microsoft.com/en-us/download)
* [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)

## Setup capacities

Once you've been running the previous configuration, you'll need to create a `configuration.json` file in the `C:\agent` directory.

The configuration should like the devices you have and the serial ports associated. As an example:

```json
{
  "capabilities": {
    "ESP32_REV0": "COM7"
  }
}
```

You can add many boards and firmware as you want. It's important to set properly the serial port for each of them. With the same hardware, you can have multiple firmware.
In that case, they'll share the same serial port and that's perfectly ok. At a single point of time, only 1 remote pipeline is running, so there won't be any clonflict with the serial ports used.

Then in powershell, run the following commands:

```powershell
cd C:\agent
$env:AZP_TOKEN="yourlongtoken"
.\capacities.ps1
```

The token will only be used during the setup of the capacity and will be deleted after from the environement and from a temporary file that will be created.

> [!Important]
> You will have to run this script every time you change the capabilities, meaning, adding or removing hardware.

## Run the agent

Depending on what you have chosen during the installation, it will either run as a service, either as on demand. You can as well create a task.

> [!Important]
> When the PAT token expires, you'll need to rerun the configuration.
