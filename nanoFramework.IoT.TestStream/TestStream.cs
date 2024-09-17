using System;
using System.Device.Gpio;

namespace nanoFramework.IoT.TestStream
{
    public class TestStream
    {
        public int Add(int a, int b) => a + b;

        public int Subtract(int a, int b) => a - b;

        public void OpenPinBlinkAndClose(int pinNumber)
        {
         GpioController gpioController = new GpioController();
            gpioController.OpenPin(pinNumber, PinMode.Output);

            bool val = false;
            for(int i = 0; i < 10; i++)
            {
                gpioController.Write(pinNumber, val);
                val = !val;
                System.Threading.Thread.Sleep(500);
            }

            gpioController.ClosePin(pinNumber);
        }
    }
}
