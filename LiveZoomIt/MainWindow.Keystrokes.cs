using System.Runtime.InteropServices;
using System.Windows.Input;
using LiveZoomIt.Keystrokes;

namespace LiveZoomIt;

/// <summary>Exibição das teclas apertadas (Ctrl+5), para demonstrações e gravações.</summary>
public partial class MainWindow
{
    private KeystrokeWindow? _keystrokes;
    private IntPtr _keyboardHook;
    private Native.LowLevelHookProc? _keyboardHookProc; // mantém o delegate vivo enquanto o hook existir

    private bool KeystrokesVisible => _keyboardHook != IntPtr.Zero;

    private void ToggleKeystrokes()
    {
        if (KeystrokesVisible)
        {
            RemoveKeyboardHook();
            _keystrokes?.Hide();
            ShowToast("Exibição de teclas desligada");
            return;
        }

        _keyboardHookProc ??= OnLowLevelKeyboard;
        _keyboardHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _keyboardHookProc, Native.GetModuleHandle("user32.dll"), 0);
        if (_keyboardHook == IntPtr.Zero)
        {
            ShowToast("Não foi possível ativar a exibição de teclas");
            return;
        }

        _keystrokes ??= new KeystrokeWindow();
        _keystrokes.Show();
        ShowToast("Exibição de teclas ligada — cuidado ao digitar senhas", 4);
    }

    private void RemoveKeyboardHook()
    {
        if (_keyboardHook == IntPtr.Zero)
            return;
        Native.UnhookWindowsHookEx(_keyboardHook);
        _keyboardHook = IntPtr.Zero;
    }

    /// <summary>Hook global de teclado: só lê a tecla e agenda a etiqueta; nunca bloqueia nem consome a tecla.</summary>
    private IntPtr OnLowLevelKeyboard(int nCode, IntPtr wParam, IntPtr lParam)
    {
        int message = wParam.ToInt32();
        if (nCode >= 0 && message is Native.WM_KEYDOWN or Native.WM_SYSKEYDOWN)
        {
            var info = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
            // MapVirtualKey não mexe no estado de acentos (diferente de ToUnicode), então não atrapalha a digitação.
            uint mapped = Native.MapVirtualKey(info.vkCode, Native.MAPVK_VK_TO_CHAR);
            var chord = new KeyChord(
                KeyInterop.KeyFromVirtualKey((int)info.vkCode),
                mapped == 0 ? null : (char)(mapped & 0xFFFF),
                Native.IsKeyDown(Native.VK_CONTROL),
                Native.IsKeyDown(Native.VK_MENU),
                Native.IsKeyDown(Native.VK_SHIFT),
                Native.IsKeyDown(Native.VK_LWIN) || Native.IsKeyDown(Native.VK_RWIN));

            if (KeystrokeFormatter.Describe(chord) is { } keystroke)
                Dispatcher.InvokeAsync(() => _keystrokes?.Push(keystroke));
        }
        return Native.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }
}
