using System.Windows.Input;
using System.Windows.Media;
using LiveZoomIt.Drawing;
using LiveZoomIt.Keystrokes;
using LiveZoomIt.Settings;

namespace LiveZoomIt.Tests;

public class HotkeyParserTests
{
    [Theory]
    [InlineData("Ctrl+2", HotkeyParser.ModControl, 0x32u, "Ctrl+2")]
    [InlineData("ctrl + shift + q", HotkeyParser.ModControl | HotkeyParser.ModShift, 0x51u, "Ctrl+Shift+Q")]
    [InlineData("Shift+Ctrl+Q", HotkeyParser.ModControl | HotkeyParser.ModShift, 0x51u, "Ctrl+Shift+Q")]
    [InlineData("Alt+F5", HotkeyParser.ModAlt, 0x74u, "Alt+F5")]
    [InlineData("Win+Control+Z", HotkeyParser.ModControl | HotkeyParser.ModWin, 0x5Au, "Ctrl+Win+Z")]
    [InlineData("F9", 0u, 0x78u, "F9")]
    public void ParsesValidHotkeys(string text, uint modifiers, uint virtualKey, string display)
    {
        Assert.True(HotkeyParser.TryParse(text, out var hotkey));
        Assert.Equal(modifiers, hotkey.Modifiers);
        Assert.Equal(virtualKey, hotkey.VirtualKey);
        Assert.Equal(display, hotkey.Display);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Q")]            // letra sem modificador roubaria a digitação
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Shift")]
    [InlineData("Ctrl+Banana")]
    [InlineData("Ctrl+A+B")]
    [InlineData("Ctrl+55")]
    public void RejectsInvalidHotkeys(string text) => Assert.False(HotkeyParser.TryParse(text, out _));
}

public class SettingsLoaderTests
{
    [Fact]
    public void EmptyObject_GivesDefaultsWithoutWarnings()
    {
        var loaded = SettingsLoader.FromJson("{}");

        Assert.Empty(loaded.Warnings);
        Assert.Equal(6, loaded.Hotkeys.Count);
        Assert.Equal("Ctrl+2", loaded.Hotkeys[HotkeyAction.ToggleDraw].Display);
        Assert.Equal([Key.R, Key.G, Key.B, Key.Y, Key.O, Key.P], loaded.Colors.Keys);
        Assert.Equal(Key.R, loaded.StartColor);
    }

    [Fact]
    public void DefaultFileRoundTripsWithoutWarnings()
    {
        var loaded = SettingsLoader.FromJson(SettingsLoader.Serialize(new AppSettings()));
        Assert.Empty(loaded.Warnings);
    }

    [Fact]
    public void DefaultFileIsReadableByHand()
    {
        string json = SettingsLoader.Serialize(new AppSettings());

        Assert.Contains("\"Ctrl+Shift+Q\"", json);
        Assert.DoesNotContain("\\u", json);
    }

    [Fact]
    public void BrokenJson_FallsBackToDefaultsWithWarning()
    {
        var loaded = SettingsLoader.FromJson("{ \"PenWidth\": ");

        Assert.Single(loaded.Warnings);
        Assert.Equal(4, loaded.Settings.PenWidth);
    }

    [Fact]
    public void AcceptsCommentsTrailingCommasAndAnyCase()
    {
        var loaded = SettingsLoader.FromJson("""
            {
              // espessura inicial
              "penwidth": 8,
            }
            """);

        Assert.Empty(loaded.Warnings);
        Assert.Equal(8, loaded.Settings.PenWidth);
    }

    [Fact]
    public void CustomColors_ReplaceDefaultsAndRejectReservedKeys()
    {
        var loaded = SettingsLoader.FromJson("""
            { "Colors": { "M": "#800080", "c": "Cyan", "E": "#FFFFFF", "X": "não-é-cor" }, "StartColor": "M" }
            """);

        Assert.Equal([Key.M, Key.C], loaded.Colors.Keys);
        Assert.Equal(Color.FromRgb(0x80, 0x00, 0x80), loaded.Colors[Key.M]);
        Assert.Equal(Colors.Cyan, loaded.Colors[Key.C]);
        Assert.Equal(Key.M, loaded.StartColor);
        Assert.Equal(2, loaded.Warnings.Count); // E reservada, X inválida
    }

    [Fact]
    public void StartColorMissingFromColors_UsesFirstColor()
    {
        var loaded = SettingsLoader.FromJson("""{ "Colors": { "M": "#800080" } }""");

        Assert.Equal(Key.M, loaded.StartColor);
        Assert.Single(loaded.Warnings);
    }

    [Fact]
    public void InvalidHotkey_RevertsToDefault()
    {
        var loaded = SettingsLoader.FromJson("""{ "Hotkeys": { "ToggleDraw": "Ctrl+Banana" } }""");

        Assert.Equal("Ctrl+2", loaded.Hotkeys[HotkeyAction.ToggleDraw].Display);
        Assert.Single(loaded.Warnings);
    }

    [Fact]
    public void EmptyHotkey_DisablesActionButNeverQuit()
    {
        var loaded = SettingsLoader.FromJson("""{ "Hotkeys": { "ToggleSpotlight": "", "Quit": "" } }""");

        Assert.False(loaded.Hotkeys.ContainsKey(HotkeyAction.ToggleSpotlight));
        Assert.Equal("Ctrl+Shift+Q", loaded.Hotkeys[HotkeyAction.Quit].Display);
    }

    [Fact]
    public void DuplicateHotkey_IsIgnoredWithWarning()
    {
        var loaded = SettingsLoader.FromJson("""{ "Hotkeys": { "ToggleSpotlight": "Ctrl+2" } }""");

        Assert.False(loaded.Hotkeys.ContainsKey(HotkeyAction.ToggleSpotlight));
        Assert.Single(loaded.Warnings);
    }

    [Fact]
    public void OutOfRangeNumbers_AreClamped()
    {
        var loaded = SettingsLoader.FromJson("""
            { "PenWidth": 100, "Magnifier": { "Zoom": 20 }, "Spotlight": { "Dim": 0 } }
            """);

        Assert.Equal(40, loaded.Settings.PenWidth);
        Assert.Equal(8, loaded.Settings.Magnifier.Zoom);
        Assert.Equal(0.1, loaded.Settings.Spotlight.Dim);
        Assert.Equal(3, loaded.Warnings.Count);
    }

    [Fact]
    public void RelativeCaptureFolder_IsRejected()
    {
        var loaded = SettingsLoader.FromJson("""{ "CaptureFolder": "capturas" }""");

        Assert.Equal("", loaded.Settings.CaptureFolder);
        Assert.Single(loaded.Warnings);
    }
}

public class KeystrokeFormatterTests
{
    private static Keystroke? Describe(Key key, char? ch = null, bool ctrl = false, bool alt = false, bool shift = false, bool win = false) =>
        KeystrokeFormatter.Describe(new KeyChord(key, ch, ctrl, alt, shift, win));

    [Fact]
    public void PlainLettersAreTypedInLowerCase() => Assert.Equal(new Keystroke("a", true), Describe(Key.A, 'A'));

    [Fact]
    public void ShiftLettersAreTypedInUpperCase() => Assert.Equal(new Keystroke("A", true), Describe(Key.A, 'A', shift: true));

    [Fact]
    public void LayoutLettersLikeCedillaAreTyped() => Assert.Equal(new Keystroke("ç", true), Describe(Key.Oem1, 'Ç'));

    [Fact]
    public void SpaceIsTyped() => Assert.Equal(new Keystroke(" ", true), Describe(Key.Space, ' '));

    [Fact]
    public void ControlCombinationsAreChords() =>
        Assert.Equal(new Keystroke("Ctrl + Shift + T", false), Describe(Key.T, 'T', ctrl: true, shift: true));

    [Fact]
    public void ShiftWithSymbolShowsTheCombination() =>
        Assert.Equal(new Keystroke("Shift + 1", false), Describe(Key.D1, '1', shift: true));

    [Theory]
    [InlineData(Key.Return, '\r', "Enter")]
    [InlineData(Key.Escape, '\u001b', "Esc")]
    [InlineData(Key.Back, '\b', "Backspace")]
    [InlineData(Key.Up, null, "↑")]
    [InlineData(Key.F5, null, "F5")]
    public void SpecialKeysHaveFriendlyNames(Key key, char? ch, string expected) =>
        Assert.Equal(new Keystroke(expected, false), Describe(key, ch));

    [Theory]
    [InlineData(Key.LeftCtrl)]
    [InlineData(Key.RightShift)]
    [InlineData(Key.LWin)]
    public void LoneModifiersAreHidden(Key key) => Assert.Null(Describe(key));
}

public class KeystrokeLogTests
{
    private static readonly TimeSpan T0 = TimeSpan.FromSeconds(10);
    private static Keystroke Typed(string s) => new(s, true);
    private static Keystroke Chord(string s) => new(s, false);

    [Fact]
    public void ConsecutiveTypingJoinsIntoOneEntry()
    {
        var log = new KeystrokeLog();
        log.Add(Typed("o"), T0);
        log.Add(Typed("i"), T0 + TimeSpan.FromSeconds(0.5));

        Assert.Equal("oi", Assert.Single(log.Entries).Text);
    }

    [Fact]
    public void PauseStartsNewTypingEntry()
    {
        var log = new KeystrokeLog();
        log.Add(Typed("o"), T0);
        log.Add(Typed("i"), T0 + KeystrokeLog.JoinWindow + TimeSpan.FromSeconds(0.1));

        Assert.Equal(2, log.Entries.Count);
    }

    [Fact]
    public void RepeatedChordShowsCounter()
    {
        var log = new KeystrokeLog();
        log.Add(Chord("Ctrl + Z"), T0);
        log.Add(Chord("Ctrl + Z"), T0 + TimeSpan.FromSeconds(0.2));
        log.Add(Chord("Ctrl + Z"), T0 + TimeSpan.FromSeconds(0.4));

        Assert.Equal("Ctrl + Z  ×3", Assert.Single(log.Entries).Text);
    }

    [Fact]
    public void KeepsOnlyTheNewestEntries()
    {
        var log = new KeystrokeLog();
        for (int i = 0; i < KeystrokeLog.MaxEntries + 2; i++)
            log.Add(Chord($"F{i + 1}"), T0);

        Assert.Equal(KeystrokeLog.MaxEntries, log.Entries.Count);
        Assert.Equal("F3", log.Entries[0].Text);
    }

    [Fact]
    public void LongTypingIsTruncatedAtTheStart()
    {
        var log = new KeystrokeLog();
        log.Add(Typed(new string('a', 30) + new string('b', 30)), T0);

        string text = log.Entries[0].Text;
        Assert.Equal(KeystrokeLog.MaxTypingLength, text.Length);
        Assert.StartsWith("…", text);
        Assert.EndsWith("bbbb", text);
    }

    [Fact]
    public void PruneRemovesExpiredEntries()
    {
        var log = new KeystrokeLog();
        log.Add(Chord("Ctrl + C"), T0);

        Assert.False(log.Prune(T0 + KeystrokeLog.Lifetime));
        Assert.True(log.Prune(T0 + KeystrokeLog.Lifetime + TimeSpan.FromMilliseconds(1)));
        Assert.Empty(log.Entries);
    }
}

public class MagnifierZoomTests
{
    [Theory]
    [InlineData(2, 1, 3)]
    [InlineData(2, -1, 1.5)]
    [InlineData(8, 1, 8)]
    [InlineData(1.5, -1, 1.5)]
    [InlineData(2.4, 1, 3)] // valor fora da lista é aproximado antes do passo
    public void StepsThroughLevels(double current, int direction, double expected) =>
        Assert.Equal(expected, MagnifierZoom.Step(current, direction));
}

public class LaserFadeCustomTimingTests
{
    [Fact]
    public void UsesConfiguredTimes()
    {
        var hold = TimeSpan.FromSeconds(2);
        var fade = TimeSpan.FromSeconds(1);

        Assert.Equal(1, LaserFade.Opacity(TimeSpan.FromSeconds(1.5), hold, fade));
        Assert.Equal(0.5, LaserFade.Opacity(TimeSpan.FromSeconds(2.5), hold, fade), 6);
        Assert.Equal(0, LaserFade.Opacity(TimeSpan.FromSeconds(3), hold, fade));
    }
}
