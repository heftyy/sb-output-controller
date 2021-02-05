using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace SBOutputController
{
    public sealed class KeyboardHook : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private class Window : NativeWindow, IDisposable
        {
            private static readonly int WM_HOTKEY = 0x0312;

            public Window()
            {
                // create the handle for the window.
                this.CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                // check if we got a hot key pressed.
                if (m.Msg == WM_HOTKEY)
                {
                    // get the keys.
                    Key key = KeyInterop.KeyFromVirtualKey(((int)m.LParam >> 16) & 0xFFFF);
                    ModifierKeys modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    KeyPressedNative?.Invoke(this, new KeyPressedEventArgs(new HotKey(key, modifier)));
                }
            }

            public event EventHandler<KeyPressedEventArgs> KeyPressedNative;

            public void Dispose()
            {
                this.DestroyHandle();
            }
        }

        private readonly Window _window = new Window();
        private readonly Dictionary<HotKey, int> _registeredHotKeys = new Dictionary<HotKey, int>();
        private int _currentId;

        public KeyboardHook()
        {
            // register the event of the inner native window.
            _window.KeyPressedNative += delegate (object sender, KeyPressedEventArgs args)
            {
                KeyPressed(this, args);
            };
        }

        public void RegisterHotKey(HotKey hotkey)
        {
            if (hotkey == null || hotkey.Key == Key.None)
                return;

            // increment the counter.
            _currentId += 1;

            // register the hot key.
            if (!RegisterHotKey(_window.Handle, _currentId, (uint)hotkey.Modifiers, (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key)))
                throw new InvalidOperationException("Couldn’t register the hot key.");

            _registeredHotKeys[hotkey] = _currentId;
        }

        public void UnregisterHotKey(HotKey hotkey)
        {
            int id_to_unregister;
            if (hotkey != null && _registeredHotKeys.TryGetValue(hotkey, out id_to_unregister))
            {
                UnregisterHotKey(_window.Handle, id_to_unregister);
                _registeredHotKeys.Remove(hotkey);
            }
        }

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        public void Dispose()
        {
            // unregister all the registered hot keys.
            foreach(int id in _registeredHotKeys.Values)
            {
                UnregisterHotKey(_window.Handle, id);
            }

            // dispose the inner native window.
            _window.Dispose();
        }
    }

    public class KeyPressedEventArgs : EventArgs
    {
        internal KeyPressedEventArgs(HotKey hotkey)
        {
            HotKey = hotkey;
        }

        public HotKey HotKey { get; }
    }
}
