using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TOML
{
    static class MoveOrdering
    {
        public static void GenerateMoveValues(Position boardPosition, Span<int> MoveValues, Span<Move> Moves, byte MoveCount, Move PredictedGoodMove, int depthFromRoot)
        {
            for(int i = 0; i < MoveCount; i++) 
            {
                Move move = Moves[i];
                int MoveValue = 0;
                ushort Piece = (ushort)(boardPosition.UshortBoard[move.startPos] & 7);
                ushort PieceValue = Evaluation.PieceTypeToPieceValue[Piece];

                if (move.IsEqual(PredictedGoodMove))
                {
                    MoveValue += 200000; //arbitrary really large number, bumps move to the front
                }
                if (KillerMoves.ContainsMove(move, depthFromRoot))
                {
                    MoveValue += 100000; // arbitrary really large number, should be smaller than the TT bump though
                }
                if ((move.moveInfo & 4) == (ushort)MoveEncodingCodes.CAPTURE)
                {
                    ushort CapturedPieceValue = Evaluation.PieceTypeToPieceValue[(ushort)(boardPosition.UshortBoard[move.endPos] & 7)];
                    MoveValue += ((CapturedPieceValue * 10) - PieceValue);
                } 
                if ((move.moveInfo >> 3) == 1)
                {
                    //promotion is likely to be good
                    MoveValue += Evaluation.PieceTypeToPieceValue[(ushort)((move.moveInfo & 3) + 2)];
                }

                MoveValues[i] = MoveValue;
            }
        }
        public static Move GetNextMove(Span<Move> Moves, byte MoveCount, Span<int> MoveValues, int MoveIndex)
        {
            Move PickedMove = Moves[MoveIndex];
            int HighestValue = MoveValues[MoveIndex];
            int HighestValueIndex = MoveIndex;
            for (int i = MoveIndex; i < MoveCount; i++)
            {
                if (MoveValues[i] > HighestValue)
                {
                    HighestValue = MoveValues[i];
                    HighestValueIndex = i;
                    PickedMove = Moves[i];

                }
            }
            (Moves[MoveIndex], Moves[HighestValueIndex]) = (Moves[HighestValueIndex], Moves[MoveIndex]);
            (MoveValues[MoveIndex], MoveValues[HighestValueIndex]) = (MoveValues[HighestValueIndex], MoveValues[MoveIndex]);
            return PickedMove;
        }
    }
}