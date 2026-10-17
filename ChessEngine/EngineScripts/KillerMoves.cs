using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOML
{
    static class KillerMoves
    {

        public static Move[] PrimaryMove = new Move[byte.MaxValue];
        public static Move[] SecondaryMove = new Move[byte.MaxValue];

        public static bool ContainsMove(Move move, int depth)
        {
            return (PrimaryMove[depth].IsEqual(move) || SecondaryMove[depth].IsEqual(move));
        }
        public static void AddMove(Move move, int depth)
        {
            if (PrimaryMove[depth].IsEqual(move)) { return; }

            SecondaryMove[depth] = PrimaryMove[depth];
            PrimaryMove[depth] = move;
        }

        public static void ResetTable()
        {
            Array.Clear(PrimaryMove, 0, PrimaryMove.Length);
            Array.Clear(SecondaryMove, 0, SecondaryMove.Length);
        }
    }
}
