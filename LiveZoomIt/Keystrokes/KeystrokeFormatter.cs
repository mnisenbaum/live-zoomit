using System.Windows.Input;

namespace LiveZoomIt.Keystrokes;

/// <summary>Uma tecla apertada, com o estado dos modificadores naquele momento.</summary>
/// <param name="Character">Caractere sem Shift que a tecla produz no layout atual, se houver.</param>
public readonly record struct KeyChord(Key Key, char? Character, bool Ctrl, bool Alt, bool Shift, bool Win);

/// <summary>Texto a exibir. Digitação comum (<see cref="IsTyping"/>) é agrupada numa mesma etiqueta.</summary>
public readonly record struct Keystroke(string Text, bool IsTyping);

public static class KeystrokeFormatter
{
    /// <summary>Descreve a tecla, ou null para modificadores soltos (Ctrl, Shift…), que só aparecem combinados.</summary>
    public static Keystroke? Describe(KeyChord chord)
    {
        if (IsModifier(chord.Key))
            return null;

        bool command = chord.Ctrl || chord.Alt || chord.Win;
        if (!command)
        {
            if (chord.Key == Key.Space)
                return new Keystroke(" ", IsTyping: true);
            if (chord.Character is char letter && char.IsLetter(letter))
                return new Keystroke((chord.Shift ? char.ToUpperInvariant(letter) : char.ToLowerInvariant(letter)).ToString(), IsTyping: true);
            // Com Shift o símbolo depende do layout (Shift+1 = "!" ou outro); mostra a combinação.
            if (!chord.Shift && chord.Character is char symbol && !char.IsControl(symbol))
                return new Keystroke(symbol.ToString(), IsTyping: true);
        }

        var parts = new List<string>(5);
        if (chord.Ctrl) parts.Add("Ctrl");
        if (chord.Alt) parts.Add("Alt");
        if (chord.Shift) parts.Add("Shift");
        if (chord.Win) parts.Add("Win");
        parts.Add(KeyName(chord.Key, chord.Character));
        return new Keystroke(string.Join(" + ", parts), IsTyping: false);
    }

    public static bool IsModifier(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System;

    private static string KeyName(Key key, char? character)
    {
        switch (key)
        {
            case Key.Return: return "Enter";
            case Key.Escape: return "Esc";
            case Key.Back: return "Backspace";
            case Key.Delete: return "Del";
            case Key.Insert: return "Ins";
            case Key.Tab: return "Tab";
            case Key.Space: return "Espaço";
            case Key.Up: return "↑";
            case Key.Down: return "↓";
            case Key.Left: return "←";
            case Key.Right: return "→";
            case Key.PageUp: return "PgUp";
            case Key.PageDown: return "PgDn";
            case Key.Snapshot: return "PrtSc";
            case Key.CapsLock: return "Caps Lock";
            case Key.Apps: return "Menu";
        }

        if (key is >= Key.D0 and <= Key.D9)
            return ((char)('0' + (key - Key.D0))).ToString();
        if (key is >= Key.NumPad0 and <= Key.NumPad9)
            return $"Num {(char)('0' + (key - Key.NumPad0))}";
        if (key is >= Key.A and <= Key.Z or >= Key.F1 and <= Key.F24)
            return key.ToString();
        if (character is char c && !char.IsControl(c))
            return char.ToUpperInvariant(c).ToString();
        return key.ToString();
    }
}
