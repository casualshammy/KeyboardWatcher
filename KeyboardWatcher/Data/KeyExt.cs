using System;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace KeyboardWatcher.Data
{
    [DataContract]
    public class KeyExt : IEquatable<KeyExt>
    {
        // do not create smth like "public static readonly KeyExt None = new KeyExt(Keys.None);",
        // this is a class, you will get a lot of bugs with references. Use "new KeyExt(Keys.None)"

        private static KeysConverter p_keysConverter = new();
        internal bool Pressed;
        internal int PressedStartTime = 0;

        [DataMember(Name = "Key")]
        public readonly Keys Key;

        [DataMember(Name = "Shift")]
        public readonly bool Shift;

        [DataMember(Name = "Alt")]
        public readonly bool Alt;

        [DataMember(Name = "Ctrl")]
        public readonly bool Ctrl;

        public KeyExt(Keys key, bool alt = false, bool shift = false, bool ctrl = false)
        {
            Key = key & ~Keys.Control & ~Keys.Shift & ~Keys.Alt;
            Pressed = false;
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }

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

        public override string ToString()
        {
            return p_keysConverter.ConvertToInvariantString(ConvertToKeys());
        }


        #region IEquatable<KeyExt>

        public bool Equals(KeyExt _other)
        {
            if (ReferenceEquals(this, _other))
                return true;
            if (_other is null)
                return false;
            return Key == _other.Key && Alt == _other.Alt && Shift == _other.Shift && Ctrl == _other.Ctrl;
        }

        public override bool Equals(object other)
        {
            return Equals(other as KeyExt);
        }

        public static bool operator ==(KeyExt a, KeyExt b)
        {
            if (a is null)
            {
                return b is null;
            }
            return a.Equals(b);
        }

        public static bool operator !=(KeyExt a, KeyExt b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return (int)ConvertToKeys();
        }

        #endregion

    }
}
