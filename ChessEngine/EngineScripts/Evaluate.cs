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
        private const ushort KING = 0;

        /*public static Dictionary<ushort, ushort> PieceTypeToPieceValue = new Dictionary<ushort, ushort>() {
            {(ushort)PieceType.EMPTY , PAWN }, //empty will mean it is enpassent
            {(ushort)PieceType.PAWN , PAWN },
            {(ushort)PieceType.KNIGHT , KNIGHT },
            {(ushort)PieceType.BISHOP , BISHOP },
            {(ushort)PieceType.ROOK , ROOK },
            {(ushort)PieceType.QUEEN , QUEEN },
            {(ushort)PieceType.KING , 0 },
        };*/

        public static ushort[] PieceTypeToPieceValue = new ushort[]
        {
            PAWN, PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
        };

        public static int[] KnightPositionBonuses = new int[]
        {
            -12, -10,  -7,  -7,  -7,  -7, -10, -12,
            -10,  -7,  -3,  -3,  -3,  -3,  -7, -10,
             -7,  -5,   2,   2,   2,   2,  -5,  -7,
             -7,  -5,   2,   6,   6,   2,  -5,  -7,
             -7,  -5,   2,   6,   6,   2,  -5,  -7,
             -7,  -5,   2,   2,   2,   2,  -5,  -7,
            -10,  -7,  -3,  -3,  -3,  -3,  -7, -10,
           -12, -10,  -7,  -7,  -7,  -7, -10, -12,
        };

        public static int[] BishopPositionBonuses = new int[]
                {
            -12, -11, -11, -10, -10, -11, -11, -12,
            -11, -11, -10,  -8,  -8, -10, -11, -11,
            -11, -10,  -7,  -2,  -2,  -7, -10, -11,
            -10,  -5,   0,   5,   5,   0,  -5, -10,
            -10,  -5,   0,   5,   5,   0,  -5, -10,
            -11, -10,  -7,  -2,  -2,  -7, -10, -11,
            -11, -11, -10,  -8,  -8, -10, -11, -11,
            -12, -11, -11, -10, -10, -11, -11, -12,
        };
        public static int[] RookPositionBonuses = new int[]
        {
              0,   0,   0,   0,   0,   0,   0,   0,
              0,   0,   0,   0,   0,   0,   0,   0,
            -25, -10,   0,   0,   0,   0, -40,  -5,
            -25, -13,   0,   5,   5,   0, -13, -10,
            -25, -13,   0,   5,   5,   0, -13, -10,
            -25, -21, -17, -15, -15, -17, -21, -25,
            -25, -23, -21, -17, -17, -21, -23, -25,
            -25, -25, -25, -25, -25, -25, -25, -25,
        };
        public static int[] QueenPositionBonuses = new int[]
        {
            -12, -11, -11, -10, -10, -11, -11, -12,
            -11, -11, -10,  -8,  -8, -10, -11, -11,
            -11, -10,  -7,  -2,  -2,  -7, -10, -11,
            -10,  -5,   0,   5,   5,   0,  -5, -10,
            -10,  -5,   0,   5,   5,   0,  -5, -10,
            -11, -10,  -7,  -2,  -2,  -7, -10, -11,
            -11, -11, -10,  -8,  -8, -10, -11, -11,
            -12, -11, -11, -10, -10, -11, -11, -12,
        };

        public static int[] PawnPositionBonuses = new int[]
        {
              0,   0,   0,   0,   0,   0,   0,   0,
             60,  50,  50,  50,  50,  50,  50,  60,
             30,  30,  30,  45,  45,  30,  30,  30,
              0,   0,  10,  40,  40,  10,   0,   0,
            -40, -40,   0,  40,  40,   0, -40, -40,
             30, -40, -20,  30,  30, -40, -40,  30,
             40,  40,  40,   0,   0,   0,  40,  40,
              0,   0,   0,   0,   0,   0,   0,   0,
        };

        public static int[] CenterBonuses = new int[]
        {
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,  30,   30,   0,   0,   0,
            0,   0,   0,  30,  30,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
        };

        public static int[] PassedPawnBonuses = new int[] { 0, 5, 5, 10, 15, 25, 70 };

        public static int[] PawnChainBonuses = new int[] { 0, 20, 35, 50, 60, 70, 75, 80 };
        public static ulong GetPassedPawnMask(int pos, ushort color)
        {
            return PrecomputedInfo.PassedPawnMask[pos][color];
            ushort File = 0;
            Position.GetXYPosition((ushort)pos, out File, out ushort Y);
            ulong Mask = 0;
            if (File != 0)
            {
                Mask |= BitBoardConstants.A_FILE << (File - 1);
            }
            if (File != 7)
            {
                Mask |= BitBoardConstants.A_FILE << (File + 1);
            }

            Mask |= BitBoardConstants.A_FILE << File;
            if (color == Piece.WHITE)
            {
                Mask &= ulong.MaxValue << ((Y + 1) * 8);
            } else
            {
                Mask &= ulong.MaxValue >> (8 - (Y + 1) * 8);

            }

            return Mask;
        }


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


            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.WHITE]) * KNIGHT;
            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.WHITE]) * BISHOP;
            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.WHITE]) * ROOK;
            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.WHITE]) * QUEEN;

            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.BLACK]) * KNIGHT;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.BLACK]) * BISHOP;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.BLACK]) * ROOK;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.BLACK]) * QUEEN;

            //further penalize being down pieces
            if (WhiteScore > BlackScore + 100)
            {
                WhiteScore += 150;
            } else if (BlackScore > WhiteScore + 100)
            {
                BlackScore += 150;
            }

            WhiteScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.WHITE]) * PAWN;
            BlackScore += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.BLACK]) * PAWN;


            ulong Mask = boardPosition.BitBoards[Piece.KNIGHT + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                WhiteScore += KnightPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.BISHOP + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                WhiteScore += BishopPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.ROOK + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                WhiteScore += RookPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.QUEEN + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                WhiteScore += QueenPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.PAWN + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;
                if ((GetPassedPawnMask(pos, Piece.WHITE) & boardPosition.BitBoards[Piece.PAWN + Piece.BLACK]) == 0)
                {
                    WhiteScore += PassedPawnBonuses[pos >> 3];
                }

                ushort File = 0;
                ulong IsolatedMask = 0;
                Position.GetXYPosition((ushort)pos, out File, out ushort Y);
                if (File != 0)
                {
                    IsolatedMask |= BitBoardConstants.A_FILE << (File - 1);
                }
                if (File != 7)
                {
                    IsolatedMask |= BitBoardConstants.A_FILE << (File + 1);
                }
                if ((IsolatedMask & boardPosition.BitBoards[Piece.WHITE + Piece.PAWN]) == 0)
                {
                    //isolated

                    WhiteScore -= 20;
                }

                //WhiteScore += PawnPositionBonuses[pos]; 
            }

            Mask = boardPosition.BitBoards[Piece.KNIGHT + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                BlackScore += KnightPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.BISHOP + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                BlackScore += BishopPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.ROOK + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                BlackScore += RookPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.QUEEN + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                BlackScore += QueenPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.PAWN + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                if ((GetPassedPawnMask(pos, Piece.BLACK) & boardPosition.BitBoards[Piece.PAWN + Piece.WHITE]) == 0)
                {
                    BlackScore += PassedPawnBonuses[7 - (pos >> 3)];
                }

                ushort File = 0;
                ulong IsolatedMask = 0;
                Position.GetXYPosition((ushort)pos, out File, out ushort Y);
                if (File != 0)
                {
                    IsolatedMask |= BitBoardConstants.A_FILE << (File - 1);
                }
                if (File != 7)
                {
                    IsolatedMask |= BitBoardConstants.A_FILE << (File + 1);
                }
                if ((IsolatedMask & boardPosition.BitBoards[Piece.BLACK + Piece.PAWN]) == 0)
                {
                    //isolated
                    BlackScore -= 20;
                }

                //BlackScore += PawnPositionBonuses[64 - pos]; //flip the score table
            }

            //calculate pawn chain bonuses
            //not working
            //WhiteScore += PawnChainBonuses[BitOperations.PopCount(boardPosition.BitBoards[Piece.WHITE + Piece.PAWN] & boardPosition.AttackingSquaresBitBoard[Piece.WHITE + Piece.PAWN])];
            //BlackScore += PawnChainBonuses[BitOperations.PopCount(boardPosition.BitBoards[Piece.BLACK + Piece.PAWN] & boardPosition.AttackingSquaresBitBoard[Piece.BLACK + Piece.PAWN])];
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
