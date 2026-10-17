using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TOML
{
    static class MoveOrdering
    {
        public static void GenerateMoveValues(Position boardPosition, Span<int> moveValues, Span<Move> moves, byte moveCount, Move predictedGoodMove, int depthFromRoot)
        {
            for(int i = 0; i < moveCount; i++) 
            {
                Move move = moves[i];
                int moveValue = 0;
                ushort piece = PieceHelper.GetPieceWithoutColor(boardPosition.UshortBoard[move.StartPos]);
                ushort pieceValue = Evaluation.PieceTypeToPieceValue[piece];

                if (move.IsEqual(predictedGoodMove))
                {
                    moveValue += 200000; //arbitrary really large number, bumps move to the front
                } else if (KillerMoves.ContainsMove(move, depthFromRoot))
                {
                    moveValue += 100000; // arbitrary really large number, should be smaller than the TT bump though
                } else
                {
                    if ((move.MoveInfo & 4) == (ushort)MoveEncodingCodes.CAPTURE)
                    {
                        ushort CapturedPieceValue = Evaluation.PieceTypeToPieceValue[PieceHelper.GetPieceWithoutColor(boardPosition.UshortBoard[move.EndPos])];
                        moveValue += ((CapturedPieceValue * 10) - pieceValue);
                    }
                    if ((move.MoveInfo >> 3) == 1)
                    {
                        //promotion is likely to be good
                        moveValue += Evaluation.PieceTypeToPieceValue[(ushort)((move.MoveInfo & 3) + 2)];
                    }
                }

                moveValues[i] = moveValue;
            }
        }
        public static Move GetNextMove(Span<Move> moves, byte moveCount, Span<int> moveValues, int moveIndex)
        {
            Move pickedMove = moves[moveIndex];
            int highestValue = moveValues[moveIndex];
            int highestValueIndex = moveIndex;
            for (int i = moveIndex; i < moveCount; i++)
            {
                if (moveValues[i] > highestValue)
                {
                    highestValue = moveValues[i];
                    highestValueIndex = i;
                    pickedMove = moves[i];

                }
            }
            (moves[moveIndex], moves[highestValueIndex]) = (moves[highestValueIndex], moves[moveIndex]);
            (moveValues[moveIndex], moveValues[highestValueIndex]) = (moveValues[highestValueIndex], moveValues[moveIndex]);
            return pickedMove;
        }
    }
}