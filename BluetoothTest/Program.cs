using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BluetoothTest
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        private static bool _workstationLocked;
        private static bool _doNotLock = false;
        private static System.Timers.Timer _timer = new System.Timers.Timer(1 * 60000);

        static void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                _workstationLocked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                _workstationLocked = false;
                _doNotLock = true;
                _timer.Enabled = true;

            }
        }

        static void Main(string[] args)
        {
            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            _timer.Elapsed += (sender, eventArgs) => {
                _doNotLock = false;
                _timer.Enabled = false;
            };

            //var devices = DiscoverBluetoothDevice();
            //Console.ReadLine();

            while (true)
            {
                var inRange = IsBluetoothDeviceInRange("C0CCF8ECF35F");

                Console.WriteLine(inRange ? "Device in range" : "Device not in range");
                if (!inRange && !_workstationLocked && !_doNotLock)
                {
                    Console.WriteLine("Locking workstation");
                    LockWorkStation();
                }

                Thread.Sleep(5000);
            }

            Console.ReadLine();
        }

        private static bool IsBluetoothDeviceInRange(string address)
        {
            bool inRange;
            Guid fakeUuid = new Guid("{F13F471D-47CB-41d6-9609-BAD0690BF891}");
            // A specially created value, so no matches.            

            BluetoothAddress btAddress;

            BluetoothAddress.TryParse(address, out btAddress);

            BluetoothDeviceInfo d = new BluetoothDeviceInfo(btAddress);
            try
            {
                var records = d.GetServiceRecords(fakeUuid);
                Debug.Assert(records.Length == 0, "Why are we getting any records?? len: " + records.Length);
                Console.WriteLine($"RSSI: {d.Rssi}");

                inRange = true;
            }
            catch (SocketException)
            {
                inRange = false;
            }

            return inRange;
        }

        private static BluetoothDeviceInfo[] DiscoverBluetoothDevice()
        {
            var btClient = new BluetoothClient();
            var devices = btClient.DiscoverDevices();

            Console.WriteLine("Bluetooth devices");
            foreach (var device in devices)
            {
                var blueToothInfo =
                    string.Format(
                        "- DeviceName: {0}{1}  Connected: {2}{1}  Address: {3}{1}  Last seen: {4}{1}  Last used: {5}{1}",
                        device.DeviceName, Environment.NewLine, device.Connected, device.DeviceAddress, device.LastSeen,
                        device.LastUsed);

                blueToothInfo += string.Format("  Class of device{0}   Device: {1}{0}   Major Device: {2}{0}   Service: {3}",
                    Environment.NewLine, device.ClassOfDevice.Device, device.ClassOfDevice.MajorDevice,
                    device.ClassOfDevice.Service);
                Console.WriteLine(blueToothInfo);
                Console.WriteLine();
            }

            return devices;
        }
    }
}
