using Ax.Fw.Windows.WinAPI;
using KeyboardWatcher.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace KeyboardWatcher;

public static class HotkeyManager
{
  private static readonly System.Timers.Timer p_timer = new(50);
  private static KeyExt[] p_uniqKeys;
  private static readonly object p_addRemoveKeysLocker = new();
  private static volatile int p_intLocker;
  private static readonly Dictionary<string, KeyExt[]> p_keysSets = [];
  private static readonly Dictionary<Keys, long?> p_lastPressed = [];

  /// <summary>
  ///     ATTENTION!
  ///     <para>As this event is static, it raise with all registered Keys, from all identifiers</para>
  /// </summary>
  public static event Action<KeyExt>? KeyPressed;

  static HotkeyManager()
  {
    p_uniqKeys = [];
    p_timer.Elapsed += TimerOnElapsed;
  }

  /// <summary>
  ///     Add Keys to listener
  /// </summary>
  /// <param name="_id">Unique identifier</param>
  /// <param name="_keysToHandle">Set of Keys to listen to</param>
  public static void AddKeys(string _id, params KeyExt[] _keysToHandle)
  {
    if (string.IsNullOrWhiteSpace(_id))
      throw new ArgumentNullException(nameof(_id));
    if (_keysToHandle == null || _keysToHandle.Length == 0)
      throw new ArgumentNullException(nameof(_keysToHandle));

    lock (p_addRemoveKeysLocker)
    {
      if (p_keysSets.Keys.Contains(_id))
        throw new InvalidOperationException("ID is already present");

      p_timer.Enabled = false;
      while (p_intLocker != 0)
        Thread.Sleep(1);

      p_keysSets[_id] = _keysToHandle;
      RebuildUniqueKeys();
      p_timer.Enabled = true;
    }
  }

  /// <summary>
  ///     Removes set of Keys with certain identifier from listener
  /// </summary>
  /// <returns>True if identifier is present, false otherwise</returns>
  public static bool RemoveKeys(string _id)
  {
    lock (p_addRemoveKeysLocker)
    {
      if (p_keysSets.ContainsKey(_id))
      {
        p_timer.Enabled = false;
        while (p_intLocker != 0)
          Thread.Sleep(1);

        p_keysSets.Remove(_id);
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
    foreach (var keyInfo in p_keysSets.Values.SelectMany(_keys => _keys))
      if (!list.Any(_l => _l.Key == keyInfo.Key))
        list.Add(keyInfo);

    p_uniqKeys = [.. list];
  }

  private static void TimerOnElapsed(object? _sender, ElapsedEventArgs _elapsedEventArgs)
  {
    if (p_intLocker != 0)
      return;

    Interlocked.Increment(ref p_intLocker);
    try
    {
      var ticksMs = Environment.TickCount64;
      var altPressed = (NativeMethods.GetAsyncKeyState(Keys.Menu) & 0x8000) != 0;
      var shiftPressed = (NativeMethods.GetAsyncKeyState(Keys.ShiftKey) & 0x8000) != 0;
      var ctrlPressed = (NativeMethods.GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
      foreach (var keyExt in p_uniqKeys)
      {
        var key = keyExt.Key;
        var keyPressed = (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0;
        if (keyPressed)
        {
          var lastPressedMs = p_lastPressed.GetValueOrDefault(key);
          if (lastPressedMs == null || ticksMs - lastPressedMs.Value >= 500)
            KeyPressed?.Invoke(new KeyExt(key, shiftPressed, altPressed, ctrlPressed));

          if (lastPressedMs == null)
            p_lastPressed[key] = ticksMs;
        }
        else
        {
          p_lastPressed[key] = null;
        }
      }
    }
    finally
    {
      Interlocked.Decrement(ref p_intLocker);
    }
  }

}
