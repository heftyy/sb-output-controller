using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;

namespace SBOutputController
{
    [TypeConverter(typeof(HotKeyConverter))]
    public class HotKey
    {
        public Key Key { get; } = Key.None;

        public ModifierKeys Modifiers { get; } = ModifierKeys.None;

        public HotKey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public HotKey(string hotkey)
        {
            Modifiers = ModifiersFromString(hotkey);
            Key = KeyFromString(hotkey);
        }

        private ModifierKeys ModifiersFromString(string hotkey)
        {
            ModifierKeys modifiers = ModifierKeys.None;

            if (hotkey.Contains("Ctrl"))
                modifiers |= ModifierKeys.Control;
            if (hotkey.Contains("Shift"))
                modifiers |= ModifierKeys.Shift;
            if (hotkey.Contains("Alt"))
                modifiers |= ModifierKeys.Alt;
            if (hotkey.Contains("Win"))
                modifiers |= ModifierKeys.Windows;

            return modifiers;
        }

        private Key KeyFromString(string hotkey)
        {
            hotkey = hotkey.Replace("Ctrl", "").Replace("Shift", "").Replace("Alt", "").Replace("Win", "").Replace(" ", "").Replace("+", "");
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (hotkey == key.ToString())
                {
                    return key;
                }
            }
            return Key.None;
        }

        public override string ToString()
        {
            if (Key == Key.None)
                return "Unassigned";

            var str = new StringBuilder();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                str.Append("Ctrl + ");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                str.Append("Shift + ");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                str.Append("Alt + ");
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                str.Append("Win + ");

            str.Append(Key);

            return str.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is HotKey key &&
                   Key == key.Key &&
                   Modifiers == key.Modifiers;
        }

        public static bool operator==(HotKey lhs, HotKey rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator!=(HotKey lhs, HotKey rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            int hashCode = 34518437;
            hashCode = hashCode * -1521134295 + Key.GetHashCode();
            hashCode = hashCode * -1521134295 + Modifiers.GetHashCode();
            return hashCode;
        }
    }

    public class HotKeyConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type source_type)
        {
            return source_type == typeof(string) || base.CanConvertFrom(context, source_type);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string string_value && !string.IsNullOrWhiteSpace(string_value))
            {
                return new HotKey(string_value);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
