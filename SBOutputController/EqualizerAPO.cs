using System.IO;

namespace SBOutputController
{
    class EqualizerAPO
    {
        public static void UpdateEqualizer(DeviceOutputModes output_mode) 
        {
            string source_path = output_mode == DeviceOutputModes.Headphones ? Properties.Settings.Default.EqualizerFilePathHeadphones : Properties.Settings.Default.EqualizerFilePathSpeakers;
            string target_path = Properties.Settings.Default.EqualizerFilePathConfig;

            string source_content = File.ReadAllText(source_path);
            File.WriteAllText(target_path, source_content);
        }
    }
}
