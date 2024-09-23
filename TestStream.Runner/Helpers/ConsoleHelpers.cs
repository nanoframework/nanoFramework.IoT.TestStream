// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace TestStream.Runner.Helpers
{
    internal class ConsoleHelpers
    {
        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteDash(string message)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(message);
        }

        public static void WriteUserAction(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  " + message);
            Console.ResetColor();
        }
    }
}
