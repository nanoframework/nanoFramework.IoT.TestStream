[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![#yourfirstpr](https://img.shields.io/badge/first--timers--only-friendly-blue.svg)](https://github.com/nanoframework/Home/blob/main/CONTRIBUTING.md) [![Discord](https://img.shields.io/discord/478725473862549535.svg?logo=discord&logoColor=white&label=Discord&color=7289DA)](https://discord.gg/gCyBu8T)

![nanoFramework logo](https://raw.githubusercontent.com/nanoframework/Home/main/resources/logo/nanoFramework-repo-logo.png)

-----

# Welcome to the .NET **nanoFramework** nanoFramework.IoT.TestStream Library repository

The [`TestStream.Runner`](./TestStream.Runner) application is designed to manage device connections within a native windows, or WSL environment, run the ADO agent. It has a feature to setup new devices and is specifically focusing on identifying new serial ports created when new hardware is connected. It handles errors gracefully and provides feedback to the user through console messages and logging.

The setup feature allows to smoothly add new devices, bind them and attach them in full transparency into WSL. Configuration files are created so that users do not have to worry about the complexity behind.

It is also design to run all the needed background applications and docker containers in full transparency.

An automatic assisted setup with a nice Terminal.Gui UI is available. You also have the ability to install everything manually with an installation script which will smoothly install of all the needed components.

> [!Important]
> This repository also contains a POC with a dummy .NET nanoFramework application, associated library and tests. The configuration of the build system present in the [multi-stage.yml](multi-stage.yaml) pipeline is here only to be able to test the overall pipelines. All the associates files like the nuspec and any other element can be ignore.
> The only important code is present in the [TestStream.Runner](./TestStream.Runner) directory.

## Native Windows Agent

For a native Windows agent installation, see [this document](./native-agent.md).

## WSL compatible agent

For WSL agent installation, see [this document](./wsl-agent.md).

## Maintainer documentation

Please [check this page](./maintainer.md) for maintainer documentation and details.

## Missing elements

[ ] Create a robust error handling to restart the container, WSL or any other element when needed.
