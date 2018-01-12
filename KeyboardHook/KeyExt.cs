using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace KeyboardWatcher
{
    [DataContract]
    public class KeyExt : IEqualityComparer<KeyExt>
    {
        // do not create smth like "public static readonly KeyExt None = new KeyExt(Keys.None);",
        // this is a class, you will get a lot of bugs with references. Use "new KeyExt(Keys.None)"

        private static KeysConverter keysConverter = new KeysConverter();
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

        public static bool operator ==(KeyExt a, KeyExt b)
        {
            return a.Key == b.Key && a.Alt == b.Alt && a.Shift == b.Shift && a.Ctrl == b.Ctrl;
        }

        public static bool operator !=(KeyExt a, KeyExt b)
        {
            return !(a == b);
        }

        public override bool Equals(object other)
        {
            KeyExt otherKeyExt = other as KeyExt;
            return otherKeyExt != null && this == otherKeyExt;
        }

        public override int GetHashCode()
        {
            return (int)ConvertToKeys();
        }

        public Keys ConvertToKeys()
        {
            Keys key = Key;
            if (Alt)
            {
                key = key | Keys.Alt;
            }
            if (Shift)
            {
                key = key | Keys.Shift;
            }
            if (Ctrl)
            {
                key = key | Keys.Control;
            }
            return key;
        }

        public bool Equals(KeyExt x, KeyExt y)
        {
            return x == y;
        }

        public int GetHashCode(KeyExt obj)
        {
            return obj.GetHashCode();
        }

        public override string ToString()
        {
            return keysConverter.ConvertToInvariantString(ConvertToKeys());
        }

    }
}
