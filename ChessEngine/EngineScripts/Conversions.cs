using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOML
{
    public static class Conversions
    {
        public static Dictionary<char, ushort> LetterToNumber = new Dictionary<char, ushort>() {
            {'a', 0 },
            {'b', 1 },
            {'c', 2 },
            {'d', 3 },
            {'e', 4 },
            {'f', 5 },
            {'g', 6 },
            {'h', 7 },
        };
        public static Dictionary<char, ushort> FenToPieceType = new Dictionary<char, ushort>() {
            {'p', 1 },
            {'n', 2 },
            {'b', 3 },
            {'r', 4 },
            {'q', 5 },
            {'k', 6 },
        };
    }
}
