using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardWatcher
{
    internal class Win32Imports
    {
        [DllImport("user32.dll")]
        internal static extern ushort GetAsyncKeyState(Keys vKey);

    }
}
