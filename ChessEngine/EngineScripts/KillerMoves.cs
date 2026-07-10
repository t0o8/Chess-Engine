using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOML
{
    static class KillerMoves
    {

        public static Move[,] MovesTable = new Move[byte.MaxValue, 2];

        public static bool ContainsMove(Move move, int depth)
        {
            if (MovesTable[depth, 0].Equals(move) || MovesTable[depth, 1].Equals(move)) { return true; }
            return false;
        }
        public static void AddMove(Move move, int depth)
        {
            if (MovesTable[depth, 0].Equals(move)) { return; }

            MovesTable[depth, 1] = MovesTable[depth, 0];
            MovesTable[depth, 0] = move;
        }

        public static void ResetTable()
        {
            Array.Clear(MovesTable, 0, MovesTable.Length);
        }
    }
}
