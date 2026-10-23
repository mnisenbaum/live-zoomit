using System.Windows.Input;

namespace LiveZoomIt.Settings;

public enum HotkeyAction
{
    ToggleDraw,
    ToggleMagnifier,
    ToggleSpotlight,
    ToggleClickHighlights,
    ToggleKeystrokes,
    Quit,
}

/// <summary>Atalho global já interpretado, pronto para RegisterHotKey.</summary>
public readonly record struct Hotkey(uint Modifiers, uint VirtualKey, string Display);

/// <summary>Interpreta textos como "Ctrl+Shift+Q" ou "Alt+F5".</summary>
public static class HotkeyParser
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        uint modifiers = 0;
        Key? key = null;
        foreach (string part in text.Split('+', StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= ModControl; continue;
                case "alt": modifiers |= ModAlt; continue;
                case "shift": modifiers |= ModShift; continue;
                case "win" or "windows": modifiers |= ModWin; continue;
            }
            if (key is not null || !TryParseKey(part, out var parsed))
                return false;
            key = parsed;
        }

        // Sem modificador, só teclas de função: uma letra sozinha roubaria a digitação do sistema todo.
        if (key is not { } k || (modifiers == 0 && k is < Key.F1 or > Key.F24))
            return false;

        hotkey = new Hotkey(modifiers, (uint)KeyInterop.VirtualKeyFromKey(k), Format(modifiers, k));
        return true;
    }

    private static bool TryParseKey(string text, out Key key)
    {
        key = default;
        if (text.Length == 1 && char.IsAsciiDigit(text[0]))
        {
            key = Key.D0 + (text[0] - '0');
            return true;
        }
        if (text.Length == 1 && char.IsAsciiLetter(text[0]))
        {
            key = Key.A + (char.ToUpperInvariant(text[0]) - 'A');
            return true;
        }
        return text.Length > 1
            && !int.TryParse(text, out _)
            && Enum.TryParse(text, ignoreCase: true, out key)
            && key != Key.None
            && !IsModifier(key);
    }

    private static bool IsModifier(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System;

    private static string Format(uint modifiers, Key key)
    {
        var parts = new List<string>();
        if ((modifiers & ModControl) != 0) parts.Add("Ctrl");
        if ((modifiers & ModAlt) != 0) parts.Add("Alt");
        if ((modifiers & ModShift) != 0) parts.Add("Shift");
        if ((modifiers & ModWin) != 0) parts.Add("Win");
        parts.Add(key is >= Key.D0 and <= Key.D9 ? ((char)('0' + (key - Key.D0))).ToString() : key.ToString());
        return string.Join("+", parts);
    }
}
