# Setup for the agent

## What needs to be installed in the image

Dockerfile should contain:

* mono-complete to be able to run the tests.
* dotnet SDK 8.0 to be able to run and install nanoff.
* VSTest from Visual Studio from the latest nuget to run the test. This cannot be installed as a task because it does only support Windows and Visual Studio. This is where mono plays.
* powershell so that script can run.

## USB access with WSL2

To be able to access serial ports to flash and run the tests, it is needed to install, after setting up properly WSL2, [USBIPD-WIN](https://learn.microsoft.com/en-us/windows/wsl/connect-usb).

Just follow the instructions and make sure you are using the very latest WSL2 kernel by running `wsl --update` **before** the installation of USBIP. [Current verion here](https://github.com/dorssel/usbipd-win/releases/download/v4.3.0/usbipd-win_4.3.0.msi). 

You will need then to attach properly in WSL the vendor ID and product ID associated to your compatible .NET nanoFramework device.