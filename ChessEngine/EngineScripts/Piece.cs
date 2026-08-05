using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOML
{
    public static class Piece
    {
        public const ushort EMPTY = 0;
        public const ushort PAWN = 1;
        public const ushort KNIGHT = 2;
        public const ushort BISHOP = 3;
        public const ushort ROOK = 4;
        public const ushort QUEEN = 5;
        public const ushort KING = 6;

        public const ushort WHITE = 0;
        public const ushort BLACK = 8;
    }
    public static class PieceHelper
    {
        public static ushort GetPieceColorInInt(char piece)
        {
            if (char.IsLower(piece) == true)
            {
                return Piece.BLACK;
            } else
            {
                return Piece.WHITE;
            }
        }
        public static ushort GetPieceWithoutColor(int piece)
        {
            return (ushort)(piece & 7);
        }
        public static ushort GetColor(ushort piece)
        {
            return (ushort)(piece & 8);

        }
        public static ushort GetOppositeColor(ushort color)
        {
            return (ushort)(color ^ 8);
        }
    }
}
