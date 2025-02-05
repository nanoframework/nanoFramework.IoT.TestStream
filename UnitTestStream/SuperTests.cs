using nanoFramework.IoT.TestStream;
using nanoFramework.TestFramework;
using System;

namespace UnitTestStream
{
    [TestClass]
    public class SuperTests
    {
        [TestMethod]
        public void TestAdd()
        {
            // Arrange
            TestStream testStream = new TestStream();

            // Act
            int result = testStream.Add(1, 2);

            //Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void TestSubtract()
        {
            // Arrange
            TestStream testStream = new TestStream();

            // Act
            int result = testStream.Subtract(2, 1);

            //Assert
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestMultiply()
        {
            // Arrange
            TestStream testStream = new TestStream();

            // Act
            int result = testStream.Multiply(2, 3);

            //Assert
            Assert.AreEqual(6, result);
        }

        [TestMethod]
        [DataRow(4)]
        public void TestOpenPinBlinkAndClose(int pinNumber)
        {
            // Arrange
            TestStream testStream = new TestStream();

            // Act
            testStream.OpenPinBlinkAndClose(pinNumber);

            // Assert
            // No assert, just check the output
            OutputHelper.WriteLine($"Pin {pinNumber} blinked!");
        }
    }
}
