#region License
//-----------------------------------------------------------------------------
// The MIT License (MIT)
//
// Copyright (c) 2009 VosSoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
#endregion

namespace VosSoft.Xna.GameConsole
{
    public class KeyboardLayout
    {
        private const uint KLF_ACTIVATE = 1; //activate the layout
        private const int KL_NAMELENGTH = 9; // length of the keyboard buffer
        private const string LANG_EN_US = "00000409";
        private const string LANG_HE_IL = "0001101A";

        [DllImport("user32.dll")]
        private static extern long LoadKeyboardLayout(string pwszKlid, // input locale identifier
            uint flags // input locale identifier options
            );

        [DllImport("user32.dll")]
        private static extern long GetKeyboardLayoutName(StringBuilder pwszKlid
            //[out] string that receives the name of the locale identifier
            );

        public static string GetName()
        {
            StringBuilder name = new StringBuilder(KL_NAMELENGTH);
            GetKeyboardLayoutName(name);
            return name.ToString();
        }
    }

    public class CharacterEventArgs : EventArgs
    {
        private readonly char _character;
        private readonly int _lParam;

        public CharacterEventArgs(char character, int lParam)
        {
            this._character = character;
            this._lParam = lParam;
        }

        public char Character {
            get { return _character; }
        }

        public int Param {
            get { return _lParam; }
        }

        public int RepeatCount {
            get { return _lParam & 0xffff; }
        }

        public bool ExtendedKey {
            get { return (_lParam & (1 << 24)) > 0; }
        }

        public bool AltPressed {
            get { return (_lParam & (1 << 29)) > 0; }
        }

        public bool PreviousState {
            get { return (_lParam & (1 << 30)) > 0; }
        }

        public bool TransitionState {
            get { return (_lParam & (1 << 31)) > 0; }
        }
    }

    public class KeyEventArgs : EventArgs
    {
        public KeyEventArgs(Keys keyCode)
        {
            KeyCode = keyCode;
        }

        public Keys KeyCode { get; private set; }
    }

    public delegate void CharEnteredHandler(CharacterEventArgs e);

    public delegate void KeyEventHandler(KeyEventArgs e);

    public static class EventInput
    {
        /// <summary>
        /// Event raised when a character has been entered.
        /// </summary>
        public static event CharEnteredHandler CharEntered;

        /// <summary>
        /// Event raised when a key has been pressed down. May fire multiple times due to keyboard repeat.
        /// </summary>
        public static event KeyEventHandler KeyDown;

        /// <summary>
        /// Event raised when a key has been released.
        /// </summary>
        public static event KeyEventHandler KeyUp;

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static bool _initialized;
        private static IntPtr _prevWndProc;
        private static WndProc _hookProcDelegate;
        private static IntPtr _hImc;

        //various Win32 constants that we need
        private const int GWL_WNDPROC = -4;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_CHAR = 0x102;
        private const int WM_IME_SETCONTEXT = 0x0281;
        private const int WM_INPUTLANGCHANGE = 0x51;
        private const int WM_GETDLGCODE = 0x87;
        private const int WM_IME_COMPOSITION = 0x10f;
        private const int DLGC_WANTALLKEYS = 4;

        //Win32 functions that we're using
        [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hImc);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        /// <summary>
        /// Initialize the TextInput with the given GameWindow.
        /// </summary>
        /// <param name="window"> The XNA window to which text input should be linked.</param>
        public static void Initialize(GameWindow window)
        {
            if (_initialized) {
                throw new InvalidOperationException("TextInput.Initialize can only be called once!");
            }

            _hookProcDelegate = HookProc;
            _prevWndProc =
                (IntPtr)
                    SetWindowLong(window.Handle, GWL_WNDPROC,
                        (int)Marshal.GetFunctionPointerForDelegate(_hookProcDelegate));

            _hImc = ImmGetContext(window.Handle);
            _initialized = true;
        }

        private static IntPtr HookProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr returnCode = CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);

            switch (msg) {
                case WM_GETDLGCODE:
                    returnCode = (IntPtr)(returnCode.ToInt32() | DLGC_WANTALLKEYS);
                    break;

                case WM_KEYDOWN:
                    if (KeyDown != null) {
                        KeyDown(new KeyEventArgs((Keys)wParam));
                    }
                    break;

                case WM_KEYUP:
                    if (KeyUp != null) {
                        KeyUp(new KeyEventArgs((Keys)wParam));
                    }
                    break;

                case WM_CHAR:
                    if (CharEntered != null) {
                        CharEntered(new CharacterEventArgs((char)wParam, lParam.ToInt32()));
                    }
                    break;

                case WM_IME_SETCONTEXT:
                    if (wParam.ToInt32() == 1) {
                        ImmAssociateContext(hWnd, _hImc);
                    }
                    break;

                case WM_INPUTLANGCHANGE:
                    ImmAssociateContext(hWnd, _hImc);
                    returnCode = (IntPtr)1;
                    break;
            }

            return returnCode;
        }
    }
}