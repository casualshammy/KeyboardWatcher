using System.Windows.Forms;

namespace KeyboardWatcher.Data;

public readonly record struct KeyExt(Keys Key, bool Shift, bool Alt, bool Ctrl)
{
  private static readonly KeysConverter p_keysConverter = new();

  public Keys ConvertToKeys()
  {
    Keys key = Key;
    if (Alt)
    {
      key |= Keys.Alt;
    }
    if (Shift)
    {
      key |= Keys.Shift;
    }
    if (Ctrl)
    {
      key |= Keys.Control;
    }
    return key;
  }

  public override string ToString() => p_keysConverter.ConvertToInvariantString(ConvertToKeys()) ?? string.Empty;

};