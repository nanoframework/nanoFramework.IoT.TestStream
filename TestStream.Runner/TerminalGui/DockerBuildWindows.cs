// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.IoT.TestRunner.Helpers;
using Terminal.Gui;

namespace nanoFramework.IoT.TestRunner.TerminalGui
{
    internal class DockerBuildWindows : Window
    {
        private static List<string> _dockerBuild = new List<string>();
        private static ListView _lstView;

        public DockerBuildWindows()
        {
            Title = "Docker Build";
            Label labelDockerDetails = new Label("Checking docker image and building it.")
            {
                X = 0,
                Y = 0
            };
            _lstView = new ListView(_dockerBuild)
            {
                Y = Pos.Y(labelDockerDetails) + 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            Add(labelDockerDetails, _lstView);

            var btnFinish = new Button("Finish")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(this) - 3,
                IsDefault = true
            };
            btnFinish.Clicked += () =>
            {
                Application.RequestStop();
            };
            Add(btnFinish);

            new Thread(() => RunDockerBuildCheck()).Start();
        }

        public void RunDockerBuildCheck()
        {
            // Checking if the docker image is built or not, if not, build it
            var images = ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution} docker image inspect {Runner.OverallConfiguration.Config.DockerImage}");
            images = images.Trim('\n').Trim('\r');
            if (images == "[]")
            {
                TerminalHelpers.LogInListView("Docker image not found, building it. This will take some time, so relax and seat back!", _dockerBuild, _lstView);
                string pathToDockerfile = Path.GetDirectoryName(Runner.Options.ConfigFilePath);
                pathToDockerfile = ProcessHelpers.ConvertToWslPath(pathToDockerfile);
                ProcessHelpers.RunCommand("wsl", $"-d {Runner.OverallConfiguration.Config.WslDistribution} docker build -t {Runner.OverallConfiguration.Config.DockerImage} -f {pathToDockerfile}/azp-agent-linux.dockerfile {pathToDockerfile}", outPutFunction: (string str) => TerminalHelpers.LogInListView(str, _dockerBuild, _lstView));
                TerminalHelpers.LogInListView("Docker image built.", _dockerBuild, _lstView);
            }
            else
            {
                TerminalHelpers.LogInListView("Docker image found.", _dockerBuild, _lstView);
            }

            TerminalHelpers.LogInListView("Trying to start the service.", _dockerBuild, _lstView);

            // Check if the service needs to be started
            if (ServiceWindow.StartService.Checked)
            {
                // Start the service
                var ret = ServiceWindow.StartRunnerService();
                if (ret)
                {
                    TerminalHelpers.LogInListView($"Service now running properly.", _dockerBuild, _lstView);
                }
                else
                {
                    TerminalHelpers.LogInListView($"Service did not start properly. Make sure you have administrator priviledges and run again this setup.", _dockerBuild, _lstView);
                }
            }


            TerminalHelpers.LogInListView("Setup completed successfully.", _dockerBuild, _lstView);
            TerminalHelpers.LogInListView("Run the setup again to add another device.", _dockerBuild, _lstView);
        }
    }
}
