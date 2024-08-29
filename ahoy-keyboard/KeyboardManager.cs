using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ahoy_keyboard
{
    public class KeyboardManager
    {
        private List<KeyboardDevice> _keyboards = new List<KeyboardDevice>();
        public IReadOnlyList<KeyboardDevice> Keyboards => _keyboards.AsReadOnly();

        public event EventHandler KeyboardsChanged;

        public void RefreshKeyboards()
        {
            _keyboards.Clear();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Keyboard"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"].ToString();
                    string deviceId = obj["PNPDeviceID"].ToString();
                    bool isUSB = deviceId.Contains("USB");

                    KeyboardDevice device = new KeyboardDevice
                    {
                        Name = name,
                        Type = isUSB ? "USB" : "Internal",
                        IsActive = true,
                        DeviceId = deviceId
                    };

                    _keyboards.Add(device);
                }
            }

            KeyboardsChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsUsbKeyboardConnected()
        {
            return _keyboards.Any(k => k.Type == "USB");
        }

        public bool IsInternalKeyboardActive()
        {
            return _keyboards.Any(k => k.Type == "Internal" && k.IsActive);
        }

        public void ToggleKeyboardState(KeyboardDevice keyboard)
        {
            if (keyboard.IsActive)
                DisableKeyboard(keyboard);
            else
                EnableKeyboard(keyboard);
        }

        private void DisableKeyboard(KeyboardDevice keyboard)
        {
            // 키보드 비활성화 로직
            if (SetKeyboardState(keyboard.DeviceId, false))
            {
                keyboard.IsActive = false;
            } else
            {
                MessageBox.Show($"Failed to disable keyboard: {keyboard.Name}");
            }
        }

        private void EnableKeyboard(KeyboardDevice keyboard)
        {
            // 키보드 활성화 로직
            if (SetKeyboardState(keyboard.DeviceId, true))
            {
                keyboard.IsActive = true;
            } else
            {
                MessageBox.Show($"Failed to enable keyboard: {keyboard.Name}");
            }
        }

        [DllImport("newdev.dll", SetLastError = true)]
        private static extern bool DiUninstallDevice(IntPtr hwndParent, string instanceId, int flags, out bool rebootRequired);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, int MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, IntPtr DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        private bool SetKeyboardState(string deviceId, bool enable)
        {
            Guid keyboardGuid = new Guid("{4D36E96B-E325-11CE-BFC1-08002BE10318}");
            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref keyboardGuid, IntPtr.Zero, IntPtr.Zero, 0x00000002);

            if (deviceInfoSet == (IntPtr)(-1))
            {
                return false;
            }

            try
            {
                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                for (int i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
                {
                    IntPtr buffer = Marshal.AllocHGlobal(256);
                    try
                    {
                        int requiredSize;
                        if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, 256, out requiredSize))
                        {
                            string instanceId = Marshal.PtrToStringAuto(buffer);
                            Debug.WriteLine($"{deviceId} {instanceId}");
                            if (instanceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase))
                            {
                                bool rebootRequired;
                                if (enable)
                                {
                                    // 키보드 활성화 (재설치)
                                    return DiUninstallDevice(IntPtr.Zero, instanceId, 1, out rebootRequired);
                                } else
                                {
                                    // 키보드 비활성화
                                    return DiUninstallDevice(IntPtr.Zero, instanceId, 0, out rebootRequired);
                                }
                            }
                        }
                    } catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    } finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            } catch (Exception e)
            {
                Debug.WriteLine(e);
            } finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return false;
        }
    }
}
