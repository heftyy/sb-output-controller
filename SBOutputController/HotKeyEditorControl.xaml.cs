using System.Windows;
using System.Windows.Input;

namespace SBOutputController
{
    public partial class HotKeyEditorControl
    {
        public static readonly DependencyProperty HotKeyProperty =
            DependencyProperty.Register(nameof(HotKey), typeof(HotKey),
                typeof(HotKeyEditorControl),
                new FrameworkPropertyMetadata(default(HotKey),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public HotKey HotKey
        {
            get => (HotKey)GetValue(HotKeyProperty);
            set
            {
                SetValue(HotKeyProperty, value);
                RaiseEvent(new RoutedEventArgs(HotKeyChangedEvent));
            }
        }

        public static readonly RoutedEvent HotKeyChangedEvent = EventManager.RegisterRoutedEvent("HotKeyChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HotKeyEditorControl));

        public event RoutedEventHandler HotKeyChanged
        {
            add { AddHandler(HotKeyChangedEvent, value); }
            remove { RemoveHandler(HotKeyChangedEvent, value); }
        }

        public HotKeyEditorControl()
        {
            InitializeComponent();

            GotKeyboardFocus += HotKeyEditorControl_GotKeyboardFocus;
        }

        private void HotKeyEditorControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            HotKeyTextBox.Text = "Recording...";
        }

        private void HotKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textbox shortcuts working
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None && (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                HotKey = null;
                return;
            }

            // If no actual key was pressed or no modifiers were held - return
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps ||
                modifiers == ModifierKeys.None)
            {
                return;
            }

            // Update the value
            HotKey = new HotKey(key, modifiers);
        }
    }
}
