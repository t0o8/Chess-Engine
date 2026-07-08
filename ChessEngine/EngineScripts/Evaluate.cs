using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace TOML
{
    class Evaluation
    {
        private const ushort EMPTY = 0;
        private const ushort PAWN = 100;
        private const ushort KNIGHT = 300;
        private const ushort BISHOP = 300;
        private const ushort ROOK = 500;
        private const ushort QUEEN = 900;

        public static Dictionary<ushort, ushort> PieceTypeToPieceValue = new Dictionary<ushort, ushort>() {
            {(ushort)PieceType.EMPTY , PAWN }, //empty will mean it is enpassent
            {(ushort)PieceType.PAWN , PAWN },
            {(ushort)PieceType.KNIGHT , KNIGHT },
            {(ushort)PieceType.BISHOP , BISHOP },
            {(ushort)PieceType.ROOK , ROOK },
            {(ushort)PieceType.QUEEN , QUEEN },
            {(ushort)PieceType.KING , 0 },
        };

        public static int[] KnightPositionBonuses = new int[]
        {
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -30, -15, -15, -15, -15, -30, -40,
            -30, -20,  10,  10,  10,  10, -20, -30,
            -30, -20,  10,  25,  25,  10, -20, -30,
            -30, -20,  10,  25,  25,  10, -20, -30,
            -30, -20,  10,  10,  10,  10, -20, -30,
            -40, -30, -15, -15, -15, -15, -30, -40,
            -50, -40, -30, -30, -30, -30, -40, -50,
        };
        public static int[] BishopPositionBonuses = new int[]
        {
            -50, -47, -45, -43, -43, -45, -47, -50,
            -47, -45, -40, -35, -35, -40, -45, -47,
            -45, -40, -30, -10, -10, -30, -40, -45,
            -40, -20,   0,  20,  20,   0, -20, -40,
            -40, -20,   0,  20,  20,   0, -20, -40,
            -45, -40, -30, -10, -10, -30, -40, -45,
            -47, -45, -40, -35, -35, -40, -45, -47,
            -50, -47, -45, -43, -43, -45, -47, -50,
        };
        public static int[] RookPositionBonuses = new int[]
        {
            -50, -47, -45, -43, -43, -45, -47, -50,
            -47, -45, -40, -35, -35, -40, -45, -47,
            -45, -40, -30, -10, -10, -30, -40, -45,
            -40, -20,   0,  20,  20,   0, -20, -40,
            -40, -20,   0,  20,  20,   0, -20, -40,
            -45, -40, -30, -10, -10, -30, -40, -45,
            -47, -45, -40, -35, -35, -40, -45, -47,
            -50, -47, -45, -43, -43, -45, -47, -50,
        };
        public static int[] QueenPositionBonuses = new int[]
        {
            -50, -47, -45, -43, -43, -45, -47, -50,
            -47, -45, -40, -35, -35, -40, -45, -47,
            -45, -40, -30, -10, -10, -30, -40, -45,
            -40, -20,   0,  20,  20,   0, -20, -40,
            -40, -20,   0,  20,  20,   0, -20, -40,
            -45, -40, -30, -10, -10, -30, -40, -45,
            -47, -45, -40, -35, -35, -40, -45, -47,
            -50, -47, -45, -43, -43, -45, -47, -50,
        };

        public static int[] PawnPositionBonuses = new int[]
        {
              0,   0,   0,   0,   0,   0,   0,   0,
             60,  50,  50,  50,  50,  50,  50,  60,
             30,  30,  30,  30,  30,  30,  30,  30,
              0,   0,  10,  25,  25,  10,   0,   0,
            -40, -40,   0,  20,  20,   0, -40, -40,
            -40, -40, -20,  10,  10, -40, -40, -40,
             40,  40,  40,   0,   0,   0,  40,  40,
              0,   0,   0,   0,   0,   0,   0,   0,
        };

        public static int GetRelativeEvaluation(Position boardPosition)
        {
            if (boardPosition.ActiveColor == Piece.WHITE)
            {
                return Evaluate(boardPosition);
            } else
            {
                return -Evaluate(boardPosition);
            }
        }
        
        //returns an evaluation only based on added up piece values for both sides
        public static int Evaluate(Position boardPosition)
        {
            int WhiteScore = 0;
            int BlackScore = 0;

            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.WHITE]) * PAWN;
            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.WHITE]) * KNIGHT;
            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.WHITE]) * BISHOP;
            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.WHITE]) * ROOK;
            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.WHITE]) * QUEEN;

            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.BLACK]) * PAWN;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.BLACK]) * KNIGHT;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.BLACK]) * BISHOP;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.BLACK]) * ROOK;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.BLACK]) * QUEEN;

            ulong Mask = boardPosition.BitBoards[Piece.KNIGHT + Piece.WHITE];
            while (Mask !=0) {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                WhiteScore += KnightPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.BISHOP + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                WhiteScore += BishopPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.ROOK + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                WhiteScore += RookPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.QUEEN + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                WhiteScore += QueenPositionBonuses[pos];
            }
            /*Mask = boardPosition.BitBoards[Piece.PAWN + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                WhiteScore += PawnPositionBonuses[pos];
            }*/
            Mask = boardPosition.BitBoards[Piece.KNIGHT + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                BlackScore += KnightPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.BISHOP + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                BlackScore += BishopPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.ROOK + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                BlackScore += RookPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.QUEEN + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                BlackScore += QueenPositionBonuses[pos];
            }
            /*Mask = boardPosition.BitBoards[Piece.PAWN + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask -= 1ul << pos;

                BlackScore += PawnPositionBonuses[64 - pos]; //flip the score table
            }*/

            return WhiteScore - BlackScore;
        }

        public static int SimpleEvaluation(Position boardPosition)
        {
            int WhiteCount = 0;
            int BlackCount = 0;

            WhiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.WHITE]) * PAWN;
            WhiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.WHITE]) * KNIGHT;
            WhiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.WHITE]) * BISHOP;
            WhiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.WHITE]) * ROOK;
            WhiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.WHITE]) * QUEEN;

            BlackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.BLACK]) * PAWN;
            BlackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.BLACK]) * KNIGHT;
            BlackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.BLACK]) * BISHOP;
            BlackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.BLACK]) * ROOK;
            BlackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.BLACK]) * QUEEN;

            return WhiteCount - BlackCount;
        }
    }
}
