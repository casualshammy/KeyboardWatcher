#nullable enable
using Ax.Fw.Windows.WinAPI;
using KeyboardWatcher.Data;
using KeyboardWatcher.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;

namespace KeyboardWatcher
{
    public class ObservableHotkeyManager : IObservableHotkeyManager
    {
        private static readonly ConcurrentDictionary<Guid, IReadOnlyList<KeyExt>> p_keysSets = new();
        private static readonly Subject<KeyExt> p_keyPressedFlow = new();
        private static readonly object p_timerSubscriptionLock = new();

        private static IDisposable? p_timerSubscription;
        private static ImmutableHashSet<KeyExt> p_uniqKeys = ImmutableHashSet<KeyExt>.Empty;

        private readonly Guid p_guid = Guid.NewGuid();
        private bool p_disposedValue;

        public ObservableHotkeyManager(IReadOnlyList<KeyExt> _keysToWatch)
        {
            p_keysSets.TryAdd(p_guid, _keysToWatch);
            RebuildUniqueKeys();

            if (p_timerSubscription == null)
                lock (p_timerSubscriptionLock)
                    if (p_timerSubscription == null)
                    {
                        p_timerSubscription = Observable
                            .Interval(TimeSpan.FromMilliseconds(50))
                            .Subscribe(_ =>
                            {
                                var currentTime = Environment.TickCount;
                                var isAltPressed = (NativeMethods.GetAsyncKeyState(Keys.Menu) & 0x8000) != 0;
                                var isShiftPressed = (NativeMethods.GetAsyncKeyState(Keys.ShiftKey) & 0x8000) != 0;
                                var IsCtrlPressed = (NativeMethods.GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
                                foreach (var keyExt in p_uniqKeys)
                                {
                                    var isPressed = (NativeMethods.GetAsyncKeyState(keyExt.Key) & 0x8000) != 0;
                                    if (isPressed && (!keyExt.Pressed || currentTime - keyExt.PressedStartTime >= 500))
                                        p_keyPressedFlow.OnNext(new KeyExt(keyExt.Key, isAltPressed, isShiftPressed, IsCtrlPressed));

                                    if (isPressed && !keyExt.Pressed)
                                        keyExt.PressedStartTime = currentTime;

                                    keyExt.Pressed = isPressed;
                                }
                            });
                    }

            KeyPressed = p_keyPressedFlow
                .Where(_x => p_keysSets[p_guid].Any(_l => _l.Equals(_x)));
        }

        public IObservable<KeyExt> KeyPressed { get; }

        private void RebuildUniqueKeys()
        {
            var builder = ImmutableHashSet.CreateBuilder<KeyExt>();
            foreach (var keyInfo in p_keysSets.Values.SelectMany(_ => _))
                builder.Add(keyInfo);

            p_uniqKeys = builder.ToImmutable();
        }

        protected virtual void Dispose(bool _disposing)
        {
            if (!p_disposedValue)
            {
                if (_disposing)
                {
                    p_keysSets.TryRemove(p_guid, out _);
                    RebuildUniqueKeys();

                    if (p_keysSets.Count == 0)
                        lock (p_timerSubscriptionLock)
                        {
                            if (p_keysSets.Count == 0)
                            {
                                p_timerSubscription?.Dispose();
                                p_timerSubscription = null;
                            }
                        }
                }

                p_disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(_disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
