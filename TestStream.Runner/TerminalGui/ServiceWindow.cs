
using nanoFramework.IoT.TestRunner.Helpers;
using Terminal.Gui;

namespace nanoFramework.IoT.TestRunner.TerminalGui
{
    internal class ServiceWindow : Window
    {
        private static List<string> _serviceDetails = new List<string>();
        private static ListView _lstView;

        /// <summary>
        /// Gets or sets the start service checkbox.
        /// </summary>
        public static CheckBox StartService { get; internal set; }

        public ServiceWindow()
        {
            Title = "Service Window";
            Label labelServiceDetails = new Label("Checking service details.")
            {
                X = 0,
                Y = 0
            };
            Add(labelServiceDetails);

            _lstView = new ListView(_serviceDetails)
            {
                Y = Pos.Y(labelServiceDetails) + 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            Add(_lstView);

            var btnNext = new Button("Next")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(this) - 3,
                IsDefault = true
            };
            Add(btnNext);
            btnNext.Clicked += () =>
            {
                Application.RequestStop();
            };

            StartService = new CheckBox("Start Service once finished.")
            {
                X = 0,
                Y = Pos.Bottom(this) - 3
            };
            StartService.Checked = true;

            Add(StartService);

            Loaded += () =>
            {
                Application.DoEvents();
                CheckServiceIsStopped();
                Thread.Sleep(200);
                Application.RequestStop();
            };
        }
        private void CheckServiceIsStopped()
        {
            int count = 5;
            bool isStopped = false;

            while (count-- > 0)
            {
                var running = ProcessHelpers.RunCommand("sc.exe", "query \"TestStream.Runner\"");
                if (running.Contains("RUNNING"))
                {
                    if (count == 4)
                    {
                        ProcessHelpers.RunCommand("sc.exe", "stop \"TestStream.Runner\"");
                    }

                    Thread.Sleep(1000);
                }
                else
                {
                    isStopped = true;
                    break;
                }
            }

            if (!isStopped)
            {
                MessageBox.Query("Service is not stopped, please stop the service manually.", "OK");
                Application.RequestStop();
            }

            TerminalHelpers.LogInListView("Service is stopped or not created.", _serviceDetails, _lstView);
            CheckAndInstallAsService();
        }

        private void CheckAndInstallAsService()
        {
            var running = ProcessHelpers.RunCommand("sc.exe", "query \"TestStream.Runner\"");
            bool createdProperly = false;
            if (running.Contains("FAILED 1060"))
            {
                var ret = MessageBox.Query("Install service?", "Service is not installed. Click OK to install it.", "OK", "Cancel");
                if (ret == 0)
                {
                    var args = $"create \"TestStream.Runner\" binPath= \"{Path.Combine(AppContext.BaseDirectory, "TestRunner.exe")}\" start= auto type= own";
                    var created = ProcessHelpers.RunCommand("sc.exe", args);
                    if (created.Contains("SUCCESS"))
                    {
                        createdProperly = true;
                        TerminalHelpers.LogInListView("Service installed.", _serviceDetails, _lstView);
                    }
                }

                if (!createdProperly)
                {
                    MessageBox.Query("Service is not installed", "Please install the service manually or try again the setup. Make sure you are in elevated prompt.", "OK");
                }
            }
        }

        public static bool StartRunnerService()
        {
            int count = 5;
            bool isStarted = false;

            var running = ProcessHelpers.RunCommand("sc.exe", "start \"TestStream.Runner\"");
            if (running.Contains("FAILED 1053"))
            {
                return false;
            }

            while (count-- > 0)
            {
                running = ProcessHelpers.RunCommand("sc.exe", "query \"TestStream.Runner\"");
                if (!running.Contains("RUNNING"))
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    isStarted = true;
                    break;
                }
            }

            return isStarted;
        }
    }
}
