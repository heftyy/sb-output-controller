using System.Windows;
using System.Windows.Controls;

namespace SBOutputController
{
    public partial class FileBrowseControl : UserControl
    {
        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(nameof(FilePath), typeof(string),
                typeof(FileBrowseControl),
                new FrameworkPropertyMetadata(default(string),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty InitialDirectoryProperty =
        DependencyProperty.Register(nameof(InitialDirectory), typeof(string),
            typeof(FileBrowseControl), new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty InitialFilenameProperty =
            DependencyProperty.Register(nameof(InitialFilename), typeof(string),
                typeof(FileBrowseControl), new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty DefaultExtProperty =
            DependencyProperty.Register(nameof(DefaultExt), typeof(string),
                typeof(FileBrowseControl), new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register(nameof(Filter), typeof(string),
                typeof(FileBrowseControl), new FrameworkPropertyMetadata(""));

        public string FilePath
        {
            get => (string)GetValue(FilePathProperty);
            set
            {
                SetValue(FilePathProperty, value);
                RaiseEvent(new RoutedEventArgs(FilePathChangedEvent));
            }
        }

        public string InitialDirectory
        {
            get => (string)GetValue(InitialDirectoryProperty);
            set => SetValue(InitialDirectoryProperty, value);
        }

        public string InitialFilename
        {
            get => (string)GetValue(InitialFilenameProperty);
            set => SetValue(InitialFilenameProperty, value);
        }

        public string DefaultExt
        {
            get => (string)GetValue(DefaultExtProperty);
            set => SetValue(DefaultExtProperty, value);
        }

        public string Filter
        {
            get => (string)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }

        public static readonly RoutedEvent FilePathChangedEvent = EventManager.RegisterRoutedEvent("FilePathChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FileBrowseControl));

        public event RoutedEventHandler FilePathChanged
        {
            add { AddHandler(FilePathChangedEvent, value); }
            remove { RemoveHandler(FilePathChangedEvent, value); }
        }

        public FileBrowseControl()
        {
            InitializeComponent();
        }

        private void ButtonBrowseFilePath_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = InitialDirectory,
                FileName = InitialFilename,
                DefaultExt = DefaultExt,
                Filter = Filter,
                CheckFileExists = true
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                FilePath = dlg.FileName;
            }
        }
    }
}
