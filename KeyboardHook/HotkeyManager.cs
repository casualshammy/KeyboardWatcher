using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace KeyboardWatcher
{
    public static class HotkeyManager
    {
        private static readonly System.Timers.Timer _timer = new System.Timers.Timer(50);
        private static KeyExt[] _uniqKeys;
        private static readonly object _locker = new object();
        private static int _intLocker;
        private static readonly Dictionary<string, KeyExt[]> KeysSets = new Dictionary<string, KeyExt[]>();

        /// <summary>
        ///     ATTENTION!
        ///     <para>As this event is static, it raise with all registered Keys, from all identifiers</para>
        /// </summary>
        public static event Action<KeyExt> KeyPressed;

        static HotkeyManager()
        {
            _timer.Elapsed += TimerOnElapsed;
        }

        /// <summary>
        ///     Add Keys to listener
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="keysToHandle">Set of Keys to listen to</param>
        public static void AddKeys(string id, params KeyExt[] keysToHandle)
        {
            lock (_locker)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("string.IsNullOrWhiteSpace(id) must return false", "id");
                }
                if (KeysSets.Keys.Contains(id))
                {
                    throw new ArgumentException("ID is already present", "id");
                }
                if (keysToHandle == null || keysToHandle.Length == 0)
                {
                    throw new ArgumentException("You should specify at least one key", "keysToHandle");
                }
                _timer.Enabled = false;
                while (_intLocker != 0)
                {
                    Thread.Sleep(1);
                }
                KeysSets[id] = keysToHandle;
                RebuildUniqueKeys();
                _timer.Enabled = true;
            }
        }

        /// <summary>
        ///     Removes set of Keys with certain identifier from listener
        /// </summary>
        /// <returns>True if identifier is present, false otherwise</returns>
        public static bool RemoveKeys(string id)
        {
            lock (_locker)
            {
                if (KeysSets.ContainsKey(id))
                {
                    _timer.Enabled = false;
                    while (_intLocker != 0)
                    {
                        Thread.Sleep(1);
                    }
                    KeysSets.Remove(id);
                    RebuildUniqueKeys();
                    if (_uniqKeys.Length != 0)
                    {
                        _timer.Enabled = true;
                    }
                    return true;
                }
                return false;
            }
        }

        private static void RebuildUniqueKeys()
        {
            List<KeyExt> list = new List<KeyExt>();
            foreach (KeyExt keyInfo in KeysSets.Values.SelectMany(keys => keys))
            {
                if (!list.Any(l => l.Key == keyInfo.Key))
                {
                    list.Add(keyInfo);
                }
            }
            _uniqKeys = list.ToArray();
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Interlocked.Increment(ref _intLocker);
            try
            {
                int currentTime = Environment.TickCount;
                bool alt = (Win32Imports.GetAsyncKeyState(Keys.Menu) & 0x8000) != 0;
                bool shift = (Win32Imports.GetAsyncKeyState(Keys.ShiftKey) & 0x8000) != 0;
                bool ctrl = (Win32Imports.GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
                foreach (KeyExt keyExt in _uniqKeys)
                {
                    bool pressed = (Win32Imports.GetAsyncKeyState(keyExt.Key) & 0x8000) != 0;
                    if (pressed && (!keyExt.Pressed || currentTime - keyExt.PressedStartTime >= 500) && KeyPressed != null)
                    {
                        KeyPressed(new KeyExt(keyExt.Key, alt, shift, ctrl));
                    }
                    if (pressed && !keyExt.Pressed)
                    {
                        keyExt.PressedStartTime = currentTime;
                    }
                    keyExt.Pressed = pressed;
                }
            }
            finally
            {
                Interlocked.Decrement(ref _intLocker);
            }
        }
        
    }
}
