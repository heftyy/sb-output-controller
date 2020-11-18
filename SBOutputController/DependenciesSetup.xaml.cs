using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SBOutputController
{
    public partial class DependenciesSetup
    {
        public DependenciesSetup(string sb_exe_path)
        {
            InitializeComponent();

            GridRequiredDll.IsEnabled = false;
            FileBrowserSBExecutablePath.FilePath = sb_exe_path;
        }

        private async void ListLoadedDlls_RegisterRequiredDll(object sender, RoutedEventArgs e)
        {
            SBRequiredDLL required_dll = (SBRequiredDLL)GridRequiredDll.SelectedItem;
            if (required_dll.Status == DLLLoadedStatus.Registered)
            {
                return;
            }

            string quoted_dll_path = String.Format("\"{0}\"", required_dll.FullPath);
            ProcessStartInfo start_info = new ProcessStartInfo("regsvr32.exe", quoted_dll_path)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(start_info);

            await Task.Delay(2000);

            GridRequiredDll.ItemsSource = SBController.GetRequiredDLLs(Path.GetDirectoryName(FileBrowserSBExecutablePath.FilePath));
            ButtonSetupFinish.IsEnabled = SBController.VerifySetup(FileBrowserSBExecutablePath.FilePath);
        }

        private void FileBrowserSBExecutablePath_FilePathChanged(object sender, RoutedEventArgs e)
        {
            string path = FileBrowserSBExecutablePath.FilePath;
            if (SBController.VerifyExecutablePath(path))
            {
                LabelExecutableNotLoaded.Visibility = Visibility.Collapsed;
                GridRequiredDll.IsEnabled = true;

                string sb_directory = Path.GetDirectoryName(path);
                GridRequiredDll.ItemsSource = SBController.GetRequiredDLLs(sb_directory);
                ButtonSetupFinish.IsEnabled = SBController.VerifySetup(FileBrowserSBExecutablePath.FilePath);
            }
            else
            {
                LabelExecutableNotLoaded.Visibility = Visibility.Visible;
                GridRequiredDll.IsEnabled = false;
                GridRequiredDll.ItemsSource = new List<SBRequiredDLL>();
                ButtonSetupFinish.IsEnabled = false;
            }
        }

        private void ButtonSetupFinish_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CheckboxRunOnStartup_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
                return;

#if DEBUG
            string application_name = "SBOutputController (Debug)";
#else
            string application_name = "SBOutputController";
#endif

            var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true);

            if (Properties.Settings.Default.RunOnStartup)
            {
                key.SetValue(application_name, System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                key.DeleteValue(application_name, false);
            }
        }
    }
}
