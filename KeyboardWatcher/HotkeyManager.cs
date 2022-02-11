using Ax.Fw.Windows.WinAPI;
using KeyboardWatcher.Data;
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
        private static readonly System.Timers.Timer p_timer = new(50);
        private static KeyExt[] p_uniqKeys;
        private static readonly object p_addRemoveKeysLocker = new();
        private static volatile int p_intLocker;
        private static readonly Dictionary<string, KeyExt[]> p_keysSets = new();

        /// <summary>
        ///     ATTENTION!
        ///     <para>As this event is static, it raise with all registered Keys, from all identifiers</para>
        /// </summary>
        public static event Action<KeyExt> KeyPressed;

        static HotkeyManager()
        {
            p_timer.Elapsed += TimerOnElapsed;
        }

        /// <summary>
        ///     Add Keys to listener
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="keysToHandle">Set of Keys to listen to</param>
        public static void AddKeys(string id, params KeyExt[] keysToHandle)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));
            if (keysToHandle == null || keysToHandle.Length == 0)
                throw new ArgumentNullException(nameof(keysToHandle));

            lock (p_addRemoveKeysLocker)
            {
                if (p_keysSets.Keys.Contains(id))
                    throw new InvalidOperationException("ID is already present");

                p_timer.Enabled = false;
                while (p_intLocker != 0)
                    Thread.Sleep(1);

                p_keysSets[id] = keysToHandle;
                RebuildUniqueKeys();
                p_timer.Enabled = true;
            }
        }

        /// <summary>
        ///     Removes set of Keys with certain identifier from listener
        /// </summary>
        /// <returns>True if identifier is present, false otherwise</returns>
        public static bool RemoveKeys(string id)
        {
            lock (p_addRemoveKeysLocker)
            {
                if (p_keysSets.ContainsKey(id))
                {
                    p_timer.Enabled = false;
                    while (p_intLocker != 0)
                    {
                        Thread.Sleep(1);
                    }
                    p_keysSets.Remove(id);
                    RebuildUniqueKeys();
                    p_timer.Enabled = p_uniqKeys.Length != 0;
                    return true;
                }
                return false;
            }
        }

        private static void RebuildUniqueKeys()
        {
            var list = new List<KeyExt>();
            foreach (KeyExt keyInfo in p_keysSets.Values.SelectMany(keys => keys))
            {
                if (!list.Any(l => l.Key == keyInfo.Key))
                {
                    list.Add(keyInfo);
                }
            }
            p_uniqKeys = list.ToArray();
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (p_intLocker != 0)
                return;
            Interlocked.Increment(ref p_intLocker);
            try
            {
                int currentTime = Environment.TickCount;
                bool alt = (NativeMethods.GetAsyncKeyState(Keys.Menu) & 0x8000) != 0;
                bool shift = (NativeMethods.GetAsyncKeyState(Keys.ShiftKey) & 0x8000) != 0;
                bool ctrl = (NativeMethods.GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
                foreach (KeyExt keyExt in p_uniqKeys)
                {
                    bool pressed = (NativeMethods.GetAsyncKeyState(keyExt.Key) & 0x8000) != 0;
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
                Interlocked.Decrement(ref p_intLocker);
            }
        }
        
    }
}
