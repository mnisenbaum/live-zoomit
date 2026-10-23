namespace LiveZoomIt.Keystrokes;

/// <summary>
/// Etiquetas visíveis da exibição de teclas. Digitação seguida vira uma etiqueta só ("olá mundo"),
/// e a mesma combinação repetida vira um contador ("Ctrl + Z ×3").
/// </summary>
public sealed class KeystrokeLog
{
    public static readonly TimeSpan JoinWindow = TimeSpan.FromSeconds(1.5);
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(2.5);
    public const int MaxEntries = 4;
    public const int MaxTypingLength = 40;

    public sealed class Entry(string text, bool isTyping, TimeSpan time)
    {
        public string BaseText { get; internal set; } = text;
        public bool IsTyping { get; } = isTyping;
        public int Count { get; internal set; } = 1;
        public TimeSpan LastUpdate { get; internal set; } = time;

        public string Text => IsTyping
            ? BaseText.Length > MaxTypingLength ? "…" + BaseText[^(MaxTypingLength - 1)..] : BaseText
            : Count > 1 ? $"{BaseText}  ×{Count}" : BaseText;
    }

    private readonly List<Entry> _entries = [];

    public IReadOnlyList<Entry> Entries => _entries;

    public void Add(Keystroke keystroke, TimeSpan now)
    {
        if (_entries.Count > 0)
        {
            var last = _entries[^1];
            bool recent = now - last.LastUpdate <= JoinWindow;
            if (recent && keystroke.IsTyping && last.IsTyping)
            {
                last.BaseText += keystroke.Text;
                last.LastUpdate = now;
                return;
            }
            if (recent && !keystroke.IsTyping && !last.IsTyping && last.BaseText == keystroke.Text)
            {
                last.Count++;
                last.LastUpdate = now;
                return;
            }
        }

        _entries.Add(new Entry(keystroke.Text, keystroke.IsTyping, now));
        if (_entries.Count > MaxEntries)
            _entries.RemoveAt(0);
    }

    /// <summary>Remove as etiquetas expiradas; devolve true se algo mudou.</summary>
    public bool Prune(TimeSpan now) => _entries.RemoveAll(e => now - e.LastUpdate > Lifetime) > 0;
}
