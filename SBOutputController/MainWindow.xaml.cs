using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using ContextMenu = System.Windows.Forms.ContextMenu;

namespace SBOutputController
{
    public partial class MainWindow
    {
        private SBController _sbConnectApi = null;
        private readonly KeyboardHook _keyboardHook = new KeyboardHook();
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private readonly ContextMenu _notifyIconContextMenu = new ContextMenu();

        private string SBDirectory
        {
            get => Path.GetDirectoryName(Properties.Settings.Default.SBExecutablePath);
        }

        static readonly private Color StatusSuccess = Color.FromRgb(85, 189, 90);
        static readonly private Color StatusFailure = Color.FromRgb(189, 92, 92);

        public MainWindow()
        {
            InitializeComponent();

            string version = "Manually built";
            Assembly this_assembly = Assembly.GetExecutingAssembly();
            Stream stream = this_assembly.GetManifestResourceStream(this_assembly.GetName().Name + ".version.txt");
            if (stream != null)
            {
                using (stream)
                using (StreamReader reader = new StreamReader(stream))
                {
                    version = reader.ReadToEnd();
                }
            }

            Title += " - " + version;

            _keyboardHook.KeyPressed += new EventHandler<KeyPressedEventArgs>(KeyboardHook_HotKeyPressed);

            HotKeyOutputHeadphones.HotKey = Properties.Settings.Default.HotKeyOutputHeadphones;
            HotKeyOutputSpeakers.HotKey = Properties.Settings.Default.HotKeyOutputSpeakers;
            HotKeyOutputToggle.HotKey = Properties.Settings.Default.HotKeyOutputToggle;

            var show_menu_item = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "&Show",
            };
            show_menu_item.Click += Window_Show;

            var exit_menu_item = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "E&xit"
            };
            exit_menu_item.Click += Window_Shutdown;

            _notifyIconContextMenu.MenuItems.Add(show_menu_item);
            _notifyIconContextMenu.MenuItems.Add(exit_menu_item);

            _notifyIcon.Icon = Properties.Resources.output_toggle_icon;
            _notifyIcon.Visible = false;
            _notifyIcon.Text = "SB Output Controller";
            _notifyIcon.DoubleClick += Window_Show;
            _notifyIcon.ContextMenu = _notifyIconContextMenu;

            CheckboxEqualizerAPO_Changed(this, null);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (!SBController.VerifySetup(Properties.Settings.Default.SBExecutablePath))
            {
                ButtonOpenSetup_Click(this, null);
            }
            else
            {
                SetupChanged(Properties.Settings.Default.SBExecutablePath);
            }
        }

        private void Window_Show(object sender, EventArgs args)
        {
            _notifyIcon.Visible = false;

            Show();
            Activate();
            WindowState = WindowState.Normal;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _notifyIcon.Visible = true;

            e.Cancel = true;
            Hide();
        }

        private void Window_Shutdown(object sender, EventArgs args)
        {
            _notifyIcon.Dispose();
            Properties.Settings.Default.Save();
            System.Windows.Application.Current.Shutdown();
        }

        private void KeyboardHook_HotKeyPressed(object sender, KeyPressedEventArgs e)
        {
            HotKey hotkey_pressed = e.HotKey;
            if (hotkey_pressed == Properties.Settings.Default.HotKeyOutputHeadphones)
            {
                ButtonSwitchToHeadphones_Click(sender, null);
            }
            else if (hotkey_pressed == Properties.Settings.Default.HotKeyOutputSpeakers)
            {
                ButtonSwitchToSpeakers_Click(sender, null);
            }
            else if (hotkey_pressed == Properties.Settings.Default.HotKeyOutputToggle)
            {
                ButtonToggleOutput_Click(sender, null);
            }
        }

        private void InitializeSBConnect()
        {
            Directory.SetCurrentDirectory(SBDirectory);

            _sbConnectApi = new SBController(SBDirectory);
            _sbConnectApi.OutputModeChangedEvent += SBConnect_OutputModeChanged;

            ListDevices.Items.Clear();
            foreach (DeviceWrapper device_wrapper in _sbConnectApi.GetDevices())
            {
                ListDevices.Items.Add(device_wrapper);
                if (device_wrapper.DeviceName == Properties.Settings.Default.LastSelectedDevice)
                {
                    ListDevices.SelectedItem = device_wrapper;
                }
            }
        }

        private void ButtonOpenSetup_Click(object sender, RoutedEventArgs e)
        {
            DependenciesSetup setup_window = new DependenciesSetup(Properties.Settings.Default.SBExecutablePath)
            {
                Owner = this
            };
            setup_window.ShowDialog();

            string exe_path = setup_window.FileBrowserSBExecutablePath.FilePath;
            SetupChanged(exe_path);
        }

        private void SetupChanged(string exe_path)
        {
            if (SBController.VerifySetup(exe_path))
            {
                Properties.Settings.Default.SBExecutablePath = exe_path;

                InitializeSBConnect();
                PanelSBConnect.IsEnabled = true;
                TextBoxSetupStatus.Background = new SolidColorBrush(StatusSuccess);
            }
            else
            {
                PanelSBConnect.IsEnabled = false;
                TextBoxSetupStatus.Background = new SolidColorBrush(StatusFailure);
            }
        }

        private void ListDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceWrapper device_wrapper = (DeviceWrapper)ListDevices.SelectedItem;
            if (device_wrapper == null || device_wrapper.DeviceName != "G6")
            {
                GridOutputButtonsAndHotKeys.IsEnabled = false;
                return;
            }

            DeviceOutputModes output_mode = _sbConnectApi.GetOutputModeForDevice(device_wrapper);
            UpdateDeviceOuputModeView(output_mode);
            GridOutputButtonsAndHotKeys.IsEnabled = true;
            Properties.Settings.Default.LastSelectedDevice = device_wrapper.DeviceName;
        }

        private void SBConnect_OutputModeChanged(object sender, OutputModeChangedEventArgs e)
        {
            UpdateDeviceOuputModeView(e.OutputMode);
            EqualizerConfig_FilePathChanged(this, null);
        }

        private void UpdateDeviceOuputModeView(DeviceOutputModes output_mode)
        {
            List<Image> output_mode_images = new List<Image>
            {
                OutputModeHeadphonesImage,
                OutputModeSpeakersImage,
                OutputModeErrorImage
            };

            void set_visible_image(Image visible_image)
            {
                foreach (Image image in output_mode_images)
                {
                    image.Visibility = image == visible_image ? Visibility.Visible : Visibility.Hidden;
                }
            }

            switch (output_mode)
            {
                case DeviceOutputModes.Headphones:
                    set_visible_image(OutputModeHeadphonesImage);
                    break;
                case DeviceOutputModes.Speakers:
                    set_visible_image(OutputModeSpeakersImage);
                    break;
                default:
                    set_visible_image(OutputModeErrorImage);
                    break;
            }
        }

        private void CheckboxEqualizerAPO_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;

            bool is_checked = CheckboxEqualizerAPO.IsChecked == true;
            FileBrowserHeadphonesConfig.IsEnabled = is_checked;
            FileBrowserSpeakersConfig.IsEnabled = is_checked;
            FileBrowserTargetConfig.IsEnabled = is_checked;

            EqualizerConfig_FilePathChanged(this, null);
        }

        private void ButtonSwitchToHeadphones_Click(object sender, RoutedEventArgs e)
        {
            DeviceWrapper device_wrapper = (DeviceWrapper)ListDevices.SelectedItem;
            _sbConnectApi.SwitchToOutputMode(device_wrapper, DeviceOutputModes.Headphones);
        }

        private void ButtonSwitchToSpeakers_Click(object sender, RoutedEventArgs e)
        {
            DeviceWrapper device_wrapper = (DeviceWrapper)ListDevices.SelectedItem;
            _sbConnectApi.SwitchToOutputMode(device_wrapper, DeviceOutputModes.Speakers);
        }

        private void ButtonToggleOutput_Click(object sender, RoutedEventArgs e)
        {
            DeviceWrapper device_wrapper = (DeviceWrapper)ListDevices.SelectedItem;
            DeviceOutputModes output_mode = _sbConnectApi.GetOutputModeForDevice(device_wrapper);
            DeviceOutputModes desired_output_mode = output_mode == DeviceOutputModes.Headphones ? DeviceOutputModes.Speakers : DeviceOutputModes.Headphones;
            _sbConnectApi.SwitchToOutputMode(device_wrapper, desired_output_mode);
        }

        private void HotKeyHeadphonesOutput_HotKeyChanged(object sender, RoutedEventArgs e)
        {
            _keyboardHook.UnregisterHotKey(Properties.Settings.Default.HotKeyOutputHeadphones);
            _keyboardHook.RegisterHotKey(HotKeyOutputHeadphones.HotKey);
            Properties.Settings.Default.HotKeyOutputHeadphones = HotKeyOutputHeadphones.HotKey;
            ListDevices.Focus();
        }
        private void HotKeySpeakersOutput_HotKeyChanged(object sender, RoutedEventArgs e)
        {
            _keyboardHook.UnregisterHotKey(Properties.Settings.Default.HotKeyOutputSpeakers);
            _keyboardHook.RegisterHotKey(HotKeyOutputSpeakers.HotKey);
            Properties.Settings.Default.HotKeyOutputSpeakers = HotKeyOutputSpeakers.HotKey;
            ListDevices.Focus();
        }
        private void HotKeyToggleOutput_HotKeyChanged(object sender, RoutedEventArgs e)
        {
            _keyboardHook.UnregisterHotKey(Properties.Settings.Default.HotKeyOutputToggle);
            _keyboardHook.RegisterHotKey(HotKeyOutputToggle.HotKey);
            Properties.Settings.Default.HotKeyOutputToggle = HotKeyOutputToggle.HotKey;
            ListDevices.Focus();
        }

        private void EqualizerConfig_FilePathChanged(object sender, RoutedEventArgs e)
        {
            DeviceWrapper device_wrapper = (DeviceWrapper)ListDevices.SelectedItem;
            if (device_wrapper == null || CheckboxEqualizerAPO.IsChecked == false)
            {
                return;
            }

            try
            {
                EqualizerAPO.UpdateEqualizer(_sbConnectApi.GetOutputModeForDevice(device_wrapper));
                FileBrowserTargetConfig.ToolTip = "Target config - config.txt by default";
                FileBrowserTargetConfig.TextBoxFilePath.Background = new SolidColorBrush(Colors.Transparent);
                FileBrowserTargetConfig.TextBoxFilePath.Foreground = new SolidColorBrush(Colors.SlateGray);
            }
            catch(Exception ex)
            {
                FileBrowserTargetConfig.ToolTip = string.Format("Target config - config.txt by default\n\nUpdating the EQ config file failed.\nError: {0}", ex.Message);
                FileBrowserTargetConfig.TextBoxFilePath.Background = new SolidColorBrush(StatusFailure);
                FileBrowserTargetConfig.TextBoxFilePath.Foreground = new SolidColorBrush(Colors.White);
            }
            
        }
    }
}
