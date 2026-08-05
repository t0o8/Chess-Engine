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

        public static readonly ushort[] PieceTypeToPieceValue = new ushort[]
        {
            PAWN, PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
        };

        public static readonly int[] KnightPositionBonuses = new int[]
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

        public static readonly int[] BishopPositionBonuses = new int[]
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
        public static readonly int[] RookPositionBonuses = new int[]
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
        public static readonly int[] QueenPositionBonuses = new int[]
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

        public static readonly int[] PawnPositionBonuses = new int[]
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
            0,   0,   0,  30,  30,   0,   0,   0,
            0,   0,   0,  30,  30,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
        };

        public static readonly int[] PassedPawnBonuses = new int[] { 0, 5, 5, 10, 25, 35, 70 };

        public static readonly int[] PawnChainBonuses = new int[] { 0, 20, 35, 50, 60, 70, 75, 80 };
        public static ulong GetPassedPawnMask(int pos, ushort color)
        {
            return PrecomputedInfo.PassedPawnMask[pos][color];
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
            int whiteScore = CalculatePieceValueEval(boardPosition, Piece.WHITE);
            int blackScore = CalculatePieceValueEval(boardPosition, Piece.BLACK);

            //further penalize being down pieces
            if (whiteScore > blackScore + 100)
            {
                whiteScore += 150;
            } else if (blackScore > whiteScore + 100)
            {
                blackScore += 150;
            }

            ulong Mask = boardPosition.BitBoards[Piece.KNIGHT + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                whiteScore += KnightPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.BISHOP + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                whiteScore += BishopPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.ROOK + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                whiteScore += RookPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.QUEEN + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                whiteScore += QueenPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.PAWN + Piece.WHITE];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;
                if ((GetPassedPawnMask(pos, Piece.WHITE) & boardPosition.BitBoards[Piece.PAWN + Piece.BLACK]) == 0)
                {
                    whiteScore += PassedPawnBonuses[pos >> 3];
                }

                ushort File = BoardHelper.GetXPos((ushort)pos);
                ulong IsolatedMask = 0;
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

                    whiteScore -= 20;
                }

                //WhiteScore += PawnPositionBonuses[pos]; 
            }

            Mask = boardPosition.BitBoards[Piece.KNIGHT + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                blackScore += KnightPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.BISHOP + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                blackScore += BishopPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.ROOK + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                blackScore += RookPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.QUEEN + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                blackScore += QueenPositionBonuses[pos];
            }
            Mask = boardPosition.BitBoards[Piece.PAWN + Piece.BLACK];
            while (Mask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(Mask);
                Mask &= Mask - 1;

                if ((GetPassedPawnMask(pos, Piece.BLACK) & boardPosition.BitBoards[Piece.PAWN + Piece.WHITE]) == 0)
                {
                    blackScore += PassedPawnBonuses[7 - (pos >> 3)];
                }

                ushort File = BoardHelper.GetXPos((ushort)pos);
                ulong IsolatedMask = 0;
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
                    blackScore -= 20;
                }

                //BlackScore += PawnPositionBonuses[64 - pos]; //flip the score table
            }

            //calculate pawn chain bonuses
            //not working
            //WhiteScore += PawnChainBonuses[BitOperations.PopCount(boardPosition.BitBoards[Piece.WHITE + Piece.PAWN] & boardPosition.AttackingSquaresBitBoard[Piece.WHITE + Piece.PAWN])];
            //BlackScore += PawnChainBonuses[BitOperations.PopCount(boardPosition.BitBoards[Piece.BLACK + Piece.PAWN] & boardPosition.AttackingSquaresBitBoard[Piece.BLACK + Piece.PAWN])];
            return whiteScore - blackScore;
        }
        public static int CalculatePieceValueEval(Position boardPosition, ushort color)
        {
            int score = BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + color]) * KNIGHT;
            score += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + color]) * BISHOP;
            score += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + color]) * ROOK;
            score += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + color]) * QUEEN;
            score += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + color]) * PAWN;

            return score;
        }
        public static int SimpleEvaluation(Position boardPosition)
        {
            int whiteCount = 0;
            int blackCount = 0;

            whiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.WHITE]) * PAWN;
            whiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.WHITE]) * KNIGHT;
            whiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.WHITE]) * BISHOP;
            whiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.WHITE]) * ROOK;
            whiteCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.WHITE]) * QUEEN;

            blackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.PAWN + Piece.BLACK]) * PAWN;
            blackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.KNIGHT + Piece.BLACK]) * KNIGHT;
            blackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.BISHOP + Piece.BLACK]) * BISHOP;
            blackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.ROOK + Piece.BLACK]) * ROOK;
            blackCount += BitOperations.PopCount(boardPosition.BitBoards[Piece.QUEEN + Piece.BLACK]) * QUEEN;

            return whiteCount - blackCount;
        }
    }
}
