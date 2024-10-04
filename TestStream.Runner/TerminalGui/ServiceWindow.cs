
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
            Title = "Scheduled Task";
            Label labelServiceDetails = new Label("Checking scheduled task details.")
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

            StartService = new CheckBox("Start task once finished.")
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

        // Documentations: https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/schtasks
        // https://learn.microsoft.com/en-us/windows/win32/taskschd/schtasks
        private void CheckServiceIsStopped()
        {
            int count = 5;
            bool isStopped = false;

            while (count-- > 0)
            {
                var running = ProcessHelpers.RunCommand("schtasks.exe", "/Query /TN \"TestStream.Runner\"", mergeOutputError: true);
                if (running.Contains("Running"))
                {
                    if (count == 4)
                    {
                        ProcessHelpers.RunCommand("schtasks.exe", "/End /TN \"TestStream.Runner\"", mergeOutputError: true);
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
                MessageBox.Query("Task is not stopped, please stop the task manually.", "OK");
                Application.RequestStop();
            }

            TerminalHelpers.LogInListView("Task is stopped or not created.", _serviceDetails, _lstView);
            CheckAndInstallAsService();
        }

        private void CheckAndInstallAsService()
        {
            var running = ProcessHelpers.RunCommand("schtasks.exe", "/Query /TN \"TestStream.Runner\"", mergeOutputError: true);
            bool createdProperly = false;
            if (running.Contains("ERROR"))
            {
                var ret = MessageBox.Query("Install task?", "Task is not installed. Click OK to install it.", "OK", "Cancel");
                if (ret == 0)
                {
                    var args = $"/create /TN \"TestStream.Runner\" /TR \"{Path.Combine(AppContext.BaseDirectory, "TestRunner.exe")}\" /SC ONSTART";
                    var created = ProcessHelpers.RunCommand("schtasks.exe", args, mergeOutputError: true);
                    if (created.Contains("SUCCESS"))
                    {
                        createdProperly = true;
                        TerminalHelpers.LogInListView("Task installed.", _serviceDetails, _lstView);
                    }
                }

                if (!createdProperly)
                {
                    MessageBox.Query("Task is not installed", "Please install the scheduled task manually or try again the setup. Make sure you are in elevated prompt.", "OK");
                }
            }
        }

        public static bool StartRunnerService()
        {
            int count = 5;
            bool isStarted = false;

            var running = ProcessHelpers.RunCommand("schtasks.exe", "/Run /TN \"TestStream.Runner\"", mergeOutputError: true);
            if (running.Contains("ERROR"))
            {
                return false;
            }

            while (count-- > 0)
            {
                running = ProcessHelpers.RunCommand("schtasks.exe", "/Query /TN \"TestStream.Runner\"", mergeOutputError: true);
                if (!running.Contains("Running"))
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
