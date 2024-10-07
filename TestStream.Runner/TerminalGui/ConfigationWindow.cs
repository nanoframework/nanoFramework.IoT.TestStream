// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.IoT.TestRunner.Configuration;
using System.Reflection;
using Terminal.Gui;

namespace nanoFramework.IoT.TestRunner.TerminalGui
{
    internal class ConfigationWindow : Window
    {
        public static OverallConfiguration OverallConfiguration { get; set; }

        public ConfigationWindow()
        {
            Title = "Setting up the main Configuration";
            var labelToDo = new Label("Please adjust the configuration and click Save.")
            {
                X = 0,
                Y = 0
            };

            // Start placing controls from the second row
            int y = 1;

            var configType = OverallConfiguration.Config.GetType();
            foreach (PropertyInfo property in configType.GetProperties())
            {
                string label = GenerateLabel(property.Name);
                var labelView = new Label(label)
                {
                    X = 1,
                    Y = y,
                };
                Add(labelView);

                var fieldView = GenerateField(property);
                if (fieldView is TextField field)
                {
                    field.X = 30;
                    field.Y = y;
                };
                if (fieldView is CheckBox checkBox)
                {
                    checkBox.X = 30;
                    checkBox.Y = y;
                }
                Add(fieldView);

                y += 2; // Move to the next row
            }

            var saveButton = new Button("Save")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(this) - 3,
                IsDefault = true
            };
            saveButton.Clicked += () =>
            {
                // We're done here, close the window
                Application.RequestStop();
            };
            Add(saveButton);
        }

        private static string GenerateLabel(string propertyName)
        {
            // Convert property name to a more readable label
            return string.Concat(propertyName.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
        }

        private static View GenerateField(PropertyInfo property)
        {
            // Generate a field based on the property type
            return property.PropertyType.Name switch
            {
                "String" => new TextField(property.GetValue(OverallConfiguration.Config).ToString()) { Width = Dim.Fill() },
                "Int32" => new TextField(property.GetValue(OverallConfiguration.Config).ToString()) { Width = Dim.Fill() },
                "Boolean" => new CheckBox(string.Empty, (bool)property.GetValue(OverallConfiguration.Config)),
                _ => new TextField("") { Width = Dim.Fill() }
            };
        }
    }
}
