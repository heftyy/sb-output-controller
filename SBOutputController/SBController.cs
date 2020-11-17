using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SBOutputController
{
    public class DeviceWrapper
    {
        public DeviceWrapper(dynamic device)
        {
            DativeDevice = device;
            DeviceName = device.DeviceName;
        }

        public string DeviceName { get; }
        public dynamic DativeDevice { get; }
    }

    class NoDevicesFound : Exception
    {
        public NoDevicesFound(string message)
            : base(message)
        { }
    }

    class MethodNotFound : Exception
    {
        public MethodNotFound(string message)
            : base(message)
        { }
    }

    enum DeviceOutputModes : uint
    {
        Speakers = 2U,
        Headphones = 4U
    }

    class OutputModeChangedEventArgs : EventArgs
    {
        public OutputModeChangedEventArgs(DeviceOutputModes output_mode)
        {
            OutputMode = output_mode;
        }

        public DeviceOutputModes OutputMode { get; }
    }

    public enum DLLLoadedStatus
    {
        Missing,
        Registered
    }

    public readonly struct SBRequiredDLL
    {
        public SBRequiredDLL(string cls_id, string relative_path, string full_path, DLLLoadedStatus dll_status)
        {
            ClsId = cls_id;
            RelativePath = relative_path;
            FullPath = full_path;
            Status = dll_status;
        }

        public string ClsId { get; }
        public string RelativePath { get; }
        public string FullPath { get; }
        public DLLLoadedStatus Status { get; }
    }

    class SBController
    {
        public static readonly Dictionary<string, string> RequiredDllsList = new Dictionary<string, string>()
        {
            { "{495E4C24-85ED-4f19-885E-C2D01D7EA26C}", @"Platform\SndCrUSB.dll" }
        };

        public static string SbConnectExecutable = @"Creative.SBCommand.exe";
        public static string SbConnectPath = @"C:\Program Files (x86)\Creative\Sound Blaster Command\";

        public event EventHandler<OutputModeChangedEventArgs> OutputModeChangedEvent;

        public SBController(string sb_directory)
        {
            _sbDirectory = sb_directory;
            _activeDevice = null;

            Initialize();
        }

        private void Initialize()
        {
            string log4net_dll_path = Path.Combine(_sbDirectory, "Package", "log4net.dll");
            string devices_dll_path = Path.Combine(_sbDirectory, "Platform", "Creative.Platform.Devices.dll");

            Assembly.LoadFrom(log4net_dll_path);
            _sbDevicesDLL = Assembly.LoadFrom(devices_dll_path);
            Type device_manager_type = _sbDevicesDLL.GetType("Creative.Platform.Devices.Models.DeviceManager", true);
            Type device_endpoint_selection_service_type = _sbDevicesDLL.GetType("Creative.Platform.Devices.Selections.DeviceEndpointSelectionService", true);

            MethodInfo device_manager_instance_method = device_manager_type.GetMethod("get_Instance");
            if (device_manager_instance_method == null)
            {
                throw new MethodNotFound("Failed to find DeviceManager.Instance");
            }
            _deviceManager = device_manager_instance_method.Invoke(null, null);
            _deviceManager.Initialize();

            if (_deviceManager.DiscoveredDevices.Count == 0)
            {
                throw new NoDevicesFound("No devices found");
            }

            MethodInfo endpoint_service_instance_method = device_endpoint_selection_service_type.GetMethod("get_Instance");
            if (endpoint_service_instance_method == null)
            {
                throw new MethodNotFound("Failed to find DeviceEndpointSelectionService.Instance");
            }
            _deviceEndpointSelectionService = endpoint_service_instance_method.Invoke(null, null);
        }

        public static bool VerifySetup(string exe_path)
        {
            if (!VerifyExecutablePath(exe_path))
                return false;

            string sb_directory = Path.GetDirectoryName(exe_path);
            List<SBRequiredDLL> required_dll_status = GetRequiredDLLs(sb_directory);

            return required_dll_status.All(dll => dll.Status == DLLLoadedStatus.Registered);
        }

        public static bool VerifyExecutablePath(string exe_path)
        {
            return exe_path != null && exe_path.EndsWith(SbConnectExecutable) && File.Exists(exe_path);
        }

        public static List<SBRequiredDLL> GetRequiredDLLs(string sb_directory)
        {
            var result = new List<SBRequiredDLL>();

            foreach(var item in RequiredDllsList)
            {
                using (var classes_root_key = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.ClassesRoot, Microsoft.Win32.RegistryView.Default))
                {
                    string cls_id = item.Key;

                    var cls_id_key =
                        classes_root_key.OpenSubKey(@"Wow6432Node\CLSID\" + cls_id) ??
                        classes_root_key.OpenSubKey(@"CLSID\" + cls_id);

                    string expected_dll_full_path = Path.Combine(sb_directory, item.Value);

                    if (cls_id_key == null)
                    {
                        result.Add(new SBRequiredDLL(cls_id, item.Value, expected_dll_full_path, DLLLoadedStatus.Missing));
                        continue;
                    }

                    var inproc_key = cls_id_key.OpenSubKey("InprocServer32");
                    cls_id_key.Dispose();
                    if (inproc_key == null)
                    {
                        result.Add(new SBRequiredDLL(cls_id, item.Value, expected_dll_full_path, DLLLoadedStatus.Missing));
                        continue;
                    }

                    string reg_dll_full_path = inproc_key.GetValue(null).ToString();
                    inproc_key.Dispose();

                    if (reg_dll_full_path != null && string.Compare(reg_dll_full_path, expected_dll_full_path, true) == 0)
                    {
                        result.Add(new SBRequiredDLL(cls_id, item.Value, expected_dll_full_path, DLLLoadedStatus.Registered));
                    }
                    else
                    {
                        result.Add(new SBRequiredDLL(cls_id, item.Value, expected_dll_full_path, DLLLoadedStatus.Missing));
                    }
                }
            }

            return result;
        }

        public void SetActiveDevice(DeviceWrapper device_wrapper)
        {
            if (_activeDevice == null || _activeDevice.DativeDevice != device_wrapper.DativeDevice)
            {
                _activeDevice = device_wrapper;
                _outputModeFeature = _deviceEndpointSelectionService.GetAggregatedFeature(device_wrapper.DativeDevice, "MultiplexOutputFeatureId");

                Type value_changed_delegate_type = _sbDevicesDLL.GetType("Creative.Platform.Devices.Selections.EffectParameterValueChangedDelegate");
                dynamic value_changed_delegate = Delegate.CreateDelegate(value_changed_delegate_type, this, "OnOutputModeChanged");
                // EffectParameterValueChangedHanlder is the "correct" spelling.... Thanks Creative
                _outputModeFeature.EffectParameterValueChangedHanlder += value_changed_delegate;
            }
        }

        public List<DeviceWrapper> GetDevices()
        {
            List<DeviceWrapper> devices = new List<DeviceWrapper>();

            foreach (var device in _deviceManager.DiscoveredDevices)
            {
                devices.Add(new DeviceWrapper(device));
            }

            return devices;
        }

        public void OnOutputModeChanged(object parameters)
        {
            if (_activeDevice != null)
            {
                DeviceOutputModes output_mode = GetOutputModeForDevice(_activeDevice);
                OutputModeChangedEvent(this, new OutputModeChangedEventArgs(output_mode));
            }
        }

        public DeviceOutputModes GetOutputModeForDevice(DeviceWrapper device_wrapper)
        {
            SetActiveDevice(device_wrapper);

            uint output_mode_value = _outputModeFeature.GetValue<uint>("MultiplexOutputParameterId");
            return (DeviceOutputModes)output_mode_value;
        }

        public void SwitchToOutputMode(DeviceWrapper device_wrapper, DeviceOutputModes requested_output_mode_enum)
        {
            SetActiveDevice(device_wrapper);

            uint requested_output_mode = (uint)requested_output_mode_enum;
            uint output_mode_value = _outputModeFeature.GetValue<uint>("MultiplexOutputParameterId");
            if (output_mode_value != requested_output_mode)
            {
                _outputModeFeature.SetValue<uint>(requested_output_mode, "MultiplexOutputParameterId");
            }
        }

        private Assembly _sbDevicesDLL;

        private dynamic _deviceManager;
        private dynamic _deviceEndpointSelectionService;
        private dynamic _outputModeFeature;

        private readonly string _sbDirectory;

        private DeviceWrapper _activeDevice;
    }
}
