﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Vim;
using Vim.Extensions;
using Vim.UI.Wpf;

namespace VsVim
{
    /// <summary>
    /// KeyBinding in Visual Studio as set through the Key board part of the Environment options
    /// panel
    /// </summary>
    public sealed class KeyBinding : IEquatable<KeyBinding>
    {
        private readonly Lazy<string> _commandString;

        public readonly string Scope;
        public readonly IEnumerable<KeyInput> KeyInputs;

        public KeyInput FirstKeyInput
        {
            get { return KeyInputs.First(); }
        }

        /// <summary>
        /// Visual Studio string which is the equivalent of this KeyBinding instance
        /// </summary>
        public string CommandString
        {
            get { return _commandString.Value; }
        }

        public KeyBinding(string scope, KeyInput input)
        {
            Scope = scope;
            KeyInputs = Enumerable.Repeat(input, 1);
            _commandString = new Lazy<string>(CreateCommandString);
        }

        public KeyBinding(string scope, IEnumerable<KeyInput> inputs)
        {
            Scope = scope;
            KeyInputs = inputs.ToList();
            _commandString = new Lazy<string>(CreateCommandString);
        }

        #region Equality

        public override int GetHashCode()
        {
            return Scope.GetHashCode() ^ CommandString.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as KeyBinding;
            return Equals(other);
        }

        public bool Equals(KeyBinding other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }
            var comp = StringComparer.OrdinalIgnoreCase;
            return
                comp.Equals(Scope, other.Scope)
                && comp.Equals(CommandString, other.CommandString);
        }

        public static bool operator ==(KeyBinding left, KeyBinding right)
        {
            return EqualityComparer<KeyBinding>.Default.Equals(left, right);
        }

        public static bool operator !=(KeyBinding left, KeyBinding right)
        {
            return !EqualityComparer<KeyBinding>.Default.Equals(left, right);
        }

        #endregion

        private string CreateCommandString()
        {
            var builder = new StringBuilder();
            builder.Append(Scope);
            builder.Append("::");
            var isFirst = true;
            foreach (var input in KeyInputs)
            {
                if (!isFirst)
                {
                    builder.Append(", ");
                }
                isFirst = false;
                AppendCommandForSingle(input, builder);
            }

            return builder.ToString();
        }

        private static void AppendCommandForSingle(KeyInput input, StringBuilder builder)
        {
            if (0 != (input.KeyModifiers & KeyModifiers.Control))
            {
                builder.Append("Ctrl+");
            }
            if (0 != (input.KeyModifiers & KeyModifiers.Shift))
            {
                builder.Append("Shift+");
            }
            if (0 != (input.KeyModifiers & KeyModifiers.Alt))
            {
                builder.Append("Alt+");
            }

            EnsureVsMap();
            var query = s_vsMap.Where(x => x.Value == input.Key);
            if (Char.IsLetter(input.Char))
            {
                builder.Append(Char.ToUpper(input.Char));
            }
            else if (query.Any())
            {
                builder.Append(query.First().Key);
            }
            else if (input.Char == ' ')
            {
                builder.Append("Space");
            }
            else
            {
                builder.Append(input.Char);
            }
        }

        public override string ToString()
        {
            return CommandString;
        }

        public static string CreateKeyBindingStringForSingleKeyInput(KeyInput input)
        {
            StringBuilder builder = new StringBuilder();
            AppendCommandForSingle(input, builder);
            return builder.ToString();
        }

        #region Parsing Methods

        private static string[] s_modifierPrefix = new string[] { "Shift", "Alt", "Ctrl" };
        private static Dictionary<string, VimKey> s_vsMap;

        private static void BuildVsMap()
        {
            var map = new Dictionary<string, VimKey>(StringComparer.OrdinalIgnoreCase);
            map.Add("Down Arrow", VimKey.Down);
            map.Add("Up Arrow", VimKey.Up);
            map.Add("Left Arrow", VimKey.Left);
            map.Add("Right Arrow", VimKey.Right);
            map.Add("Bkspce", VimKey.Back);
            map.Add("PgDn", VimKey.PageDown);
            map.Add("PgUp", VimKey.PageUp);
            map.Add("Ins", VimKey.Insert);
            map.Add("Del", VimKey.Delete);
            map.Add("Esc", VimKey.Escape);
            map.Add("Break", VimKey.Break);
            map.Add("Num +", VimKey.KeypadPlus);
            map.Add("Num -", VimKey.KeypadMinus);
            map.Add("Num /", VimKey.KeypadDivide);
            map.Add("Num *", VimKey.KeypadMultiply);
            map.Add("Enter", VimKey.Enter);
            map.Add("Tab", VimKey.Tab);
            map.Add("Home", VimKey.Home);
            map.Add("End", VimKey.End);
            map.Add("F1", VimKey.F1);
            map.Add("F2", VimKey.F2);
            map.Add("F3", VimKey.F3);
            map.Add("F4", VimKey.F4);
            map.Add("F5", VimKey.F5);
            map.Add("F6", VimKey.F6);
            map.Add("F7", VimKey.F7);
            map.Add("F8", VimKey.F8);
            map.Add("F9", VimKey.F9);
            map.Add("F10", VimKey.F10);
            map.Add("F11", VimKey.F11);
            map.Add("F12", VimKey.F12);

            s_vsMap = map;
        }

        private static void EnsureVsMap()
        {
            if (null == s_vsMap)
            {
                BuildVsMap();
            }
        }

        private static bool TryConvertToModifierKeys(string mod, out KeyModifiers modKeys)
        {
            var comp = StringComparer.OrdinalIgnoreCase;
            if (comp.Equals(mod, "shift"))
            {
                modKeys = KeyModifiers.Shift;
            }
            else if (comp.Equals(mod, "ctrl"))
            {
                modKeys = KeyModifiers.Control;
            }
            else if (comp.Equals(mod, "alt"))
            {
                modKeys = KeyModifiers.Alt;
            }
            else
            {
                modKeys = KeyModifiers.None;
                return false;
            }

            return true;
        }

        private static KeyInput ConvertToKeyInput(string keystroke)
        {
            if (keystroke.Length == 1)
            {
                var opt = InputUtil.TryCharToKeyInput(keystroke[0]);
                if (opt.IsSome())
                {
                    // Visual Studio doesn't differentiate between upper and lower case
                    // alpha characters.  Use all lower case for simplicity elsewhere
                    var v = opt.Value;
                    if (Char.IsLetter(v.Char) && 0 != (KeyModifiers.Shift & v.KeyModifiers))
                    {
                        opt = InputUtil.TryChangeKeyModifiers(v, v.KeyModifiers & ~KeyModifiers.Shift);
                    }
                }

                if (opt.IsSome())
                {
                    return opt.Value;
                }
            }

            KeyInput vs = null;
            if (TryConvertVsSpecificKey(keystroke, out vs))
            {
                return vs;
            }

            try
            {
                var key = (Key)Enum.Parse(typeof(Key), keystroke, ignoreCase: true);
                return KeyUtil.ConvertToKeyInput(key);
            }
            catch (Exception)
            {

            }

            return null;
        }

        /// <summary>
        /// Maybe convert a Visual Studio specific keystroke
        /// </summary>
        private static bool TryConvertVsSpecificKey(string keystroke, out KeyInput ki)
        {
            EnsureVsMap();
            VimKey wellKnownKey;
            if (s_vsMap.TryGetValue(keystroke, out wellKnownKey))
            {
                ki = InputUtil.VimKeyToKeyInput(wellKnownKey);
                return true;
            }

            if (keystroke.StartsWith("Num ", StringComparison.OrdinalIgnoreCase))
            {
                ki = null;
                switch (keystroke.ToLower())
                {
                    case "num +":
                        ki = InputUtil.VimKeyToKeyInput(VimKey.KeypadPlus);
                        break;
                    case "num /":
                        ki = InputUtil.VimKeyToKeyInput(VimKey.KeypadDivide);
                        break;
                    case "num *":
                        ki = InputUtil.VimKeyToKeyInput(VimKey.KeypadMultiply);
                        break;
                    case "num -":
                        ki = InputUtil.VimKeyToKeyInput(VimKey.KeypadMinus);
                        break;
                }
                return ki != null;
            }

            ki = null;
            return false;
        }

        private static KeyInput ParseOne(string entry)
        {
            // If it's of length 1 it can only be a single keystroke entry
            if (entry.Length == 1)
            {
                return ConvertToKeyInput(entry);
            }

            // First get rid of the Modifiers
            var mod = KeyModifiers.None;
            while (s_modifierPrefix.Any(x => entry.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                var index = entry.IndexOf('+');
                if (index < 0)
                {
                    return null;
                }

                var value = entry.Substring(0, index);
                var modKeys = KeyModifiers.None;
                if (!TryConvertToModifierKeys(value, out modKeys))
                {
                    return null;
                }
                mod |= modKeys;
                entry = entry.Substring(index + 1).TrimStart();
            }

            var ki = ConvertToKeyInput(entry);
            if (ki == null)
            {
                return null;
            }

            if (mod != KeyModifiers.None)
            {
                var opt = InputUtil.TryChangeKeyModifiers(ki, ki.KeyModifiers | mod);
                ki = opt.IsSome()
                    ? opt.Value
                    : null;
            }

            return ki;
        }

        /// <summary>
        /// Parse the key binding format as described by the Command.Bindings documentation
        /// 
        /// http://msdn.microsoft.com/en-us/library/envdte.command.bindings.aspx
        /// </summary>
        public static KeyBinding Parse(string binding)
        {
            KeyBinding keyBinding;
            if (!TryParse(binding, out keyBinding))
            {
                throw new ArgumentException("Invalid key binding");
            }

            return keyBinding;
        }

        public static bool TryParse(string binding, out KeyBinding keyBinding)
        {
            keyBinding = default(KeyBinding);
            var scopeEnd = binding.IndexOf(':');
            if (scopeEnd < 0)
            {
                return false;
            }

            var scope = binding.Substring(0, scopeEnd);
            var rest = binding.Substring(scopeEnd + 2);
            var entries = rest
                .Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => ParseOne(x));
            if (entries.Any(x => x == null))
            {
                return false;
            }

            keyBinding = new KeyBinding(scope, entries);
            return true;
        }

        #endregion
    }
}
