using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HookDemo
{
    public partial class Form1 : Form
    {
        KeyboardHookCallback _keyboardHookCallback;
        MouseHookCallback _mouseHookCallback;
        IntPtr _keyboardHookHandle, _mouseHookHandle;

        public Form1()
        {
            InitializeComponent();

            // SetWindowsHookExに渡すコールバックメソッドをフィールドで保持しておく
            // フィールドで保持しておかないとガベージコレクション関係のエラーが発生する
            _keyboardHookCallback = KeyCallback;
            _mouseHookCallback = MouseCallback;

            var hInstance = Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]);

            // SetWindowsHookExでフックを開始します。戻り値はフック解除に必要なのでフィールドで保持しておきます。

            // キーボードフック開始
            _keyboardHookHandle = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _keyboardHookCallback, hInstance, 0);

            // マウスフック開始
            _mouseHookHandle = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _mouseHookCallback, hInstance, 0);
        }

        // 終了時にフック解除
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_keyboardHookHandle != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_keyboardHookHandle);
                _keyboardHookHandle = IntPtr.Zero;
            }

            if (_mouseHookHandle != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_mouseHookHandle);
                _mouseHookHandle = IntPtr.Zero;
            }

            base.OnClosing(e);
        }

        // キー入力が発生したら呼び出されるメソッド
        IntPtr KeyCallback(int nCode, uint wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            // KBDLLHOOKSTRUCTについてはこちらを参照
            // https://docs.microsoft.com/ja-jp/windows/win32/api/winuser/ns-winuser-kbdllhookstruct

            // 入力の発生したキー
            var key = (Keys)lParam.VirtualKeyCode;

            // キーが押されたかどうか
            var isPressed = wParam == Native.WM_KEY_DOWN || wParam == Native.WM_SYS_KEY_DOWN;

            // アプリケーションによって入力されたかどうか
            var isInjected = (lParam.Flag & Native.INJECTED_KEY) != 0;

            Console.WriteLine($"[{isInjected}, {isPressed}]{key}");

            // 入力キャンセルする場合 return (IntPtr)1;
            return Native.CallNextHookEx(_keyboardHookHandle, nCode, wParam, ref lParam);
        }

        // マウスの入力が発生したら呼び出されるメソッド
        IntPtr MouseCallback(int nCode, uint wParam, ref MSLLHOOKSTRUCT lParam)
        {
            // MSLLHOOKSTRUCTについてはこちらを参照
            // https://docs.microsoft.com/ja-jp/windows/win32/api/winuser/ns-winuser-msllhookstruct

            // アプリケーションによって入力されたかどうか
            var isInjected = (lParam.Flags & 0x1) == 1;

            // マウスの移動があったら
            if (wParam == Native.WM_MOUSE_MOVE)
            {
                var x = lParam.Point.X;
                var y = lParam.Point.Y;
                Console.WriteLine($"[{isInjected}]{x}, {y}");
                // キャンセルする場合は return (IntPtr)1;
                return Native.CallNextHookEx(_mouseHookHandle, nCode, wParam, ref lParam);
            }

            // マウススクロールされたら
            if (wParam == Native.WM_WHEEL)
            {
                if (lParam.MouseData > 0)
                {
                    Console.WriteLine($"[{isInjected}]スクロールアップ");
                }
                else
                {
                    Console.WriteLine($"[{isInjected}]スクロールダウン");
                }

                // キャンセルする場合は return (IntPtr)1;
                return Native.CallNextHookEx(_mouseHookHandle, nCode, wParam, ref lParam);
            }

            // マウスの値（Keys）と 押されたかどうかを取得する
            var (button, isPressed) = wParam switch
            {
                Native.WM_LEFT_DOWN => (Keys.LButton, true),
                Native.WM_LEFT_UP => (Keys.LButton, false),
                Native.WM_RIGHT_DOWN => (Keys.RButton, true),
                Native.WM_RIGHT_UP => (Keys.RButton, false),
                Native.WM_MIDDLE_DOWN => (Keys.MButton, true),
                Native.WM_MIDDLE_UP => (Keys.MButton, false),
                Native.WM_XBUTTON_DOWN => (lParam.MouseData >> 16) == 1 ? (Keys.XButton1, true) : (Keys.XButton2, true),
                Native.WM_XBUTTON_UP => (lParam.MouseData >> 16) == 1 ? (Keys.XButton1, false) : (Keys.XButton2, false),
                _ => (Keys.None, false),
            };

            Console.WriteLine($"[{isInjected}, {isPressed}]{button}");

            // キャンセルする場合は return (IntPtr)1;
            return Native.CallNextHookEx(_mouseHookHandle, nCode, wParam, ref lParam);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public int VirtualKeyCode;
        public int ScanCode;
        public int Flag;
        public int Time;
        public IntPtr Info;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public Point Point;
        public int MouseData;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    public delegate IntPtr KeyboardHookCallback(int nCode, uint wParam, ref KBDLLHOOKSTRUCT lParam);

    public delegate IntPtr MouseHookCallback(int nCode, uint wParam, ref MSLLHOOKSTRUCT lParam);

    class Native
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookCallback lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, MouseHookCallback lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, uint msg, ref KBDLLHOOKSTRUCT kbdllhookstruct);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, uint msg, ref MSLLHOOKSTRUCT mouse);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(int KeyCode);

        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;

        public const int WM_MOUSE_MOVE = 512;
        public const int WM_LEFT_DOWN = 0x201;
        public const int WM_LEFT_UP = 0x202;
        public const int WM_RIGHT_DOWN = 0x204;
        public const int WM_RIGHT_UP = 0x205;
        public const int WM_MIDDLE_DOWN = 0x207;
        public const int WM_MIDDLE_UP = 0x208;
        public const int WM_XBUTTON_DOWN = 0x20B;
        public const int WM_XBUTTON_UP = 0x20C;
        public const int WM_WHEEL = 522;

        public const uint WM_KEY_DOWN = 0x100;
        public const uint WM_KEY_UP = 0x101;
        public const uint WM_SYS_KEY_DOWN = 0x104;
        public const uint WM_SYS_KEY_UP = 0x105;

        public const int EXTENDED_KEY = 0x0;
        public const int INJECTED_KEY = 0x10;
        public const int ALTDOWN_KEY = 0x1;
    }
}