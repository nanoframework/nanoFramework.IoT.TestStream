using nanoFramework.IoT.TestStream;
using System;
using System.Diagnostics;
using System.Threading;

namespace ApplicationTestStream
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            TestStream testStream = new TestStream();
            testStream.OpenPinBlinkAndClose(4);

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
