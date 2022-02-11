using KeyboardWatcher.Data;
using System;

namespace KeyboardWatcher.Interfaces
{
    public interface IObservableHotkeyManager : IDisposable
    {
        IObservable<KeyExt> KeyPressed { get; }
    }
}