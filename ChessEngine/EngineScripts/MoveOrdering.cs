using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TOML
{
    class MoveOrdering
    {
        Position boardPosition;
        int[] MoveValues;
        public Move[] Moves;
        Move PredictedGoodMove;

        int MoveIndex;
        public MoveOrdering(Move[] moves, Position boardPosition)
        {
            Moves = moves;
            MoveValues = new int[moves.Length];
            this.boardPosition = boardPosition;
            MoveIndex = 0;
        }
        public void GenerateMoveValues()
        {
            for(int i = 0; i < Moves.Length; i++) 
            {
                Move move = Moves[i];
                int MoveValue = 0;
                ushort Piece = (ushort)(boardPosition.UshortBoard[move.startPos] & 7);
                ushort PieceValue = Evaluation.PieceTypeToPieceValue[Piece];

                if (move.IsEqual(PredictedGoodMove))
                {
                    MoveValue += 200000; //arbitrary really large number, bumps move to the front
                }
                if ((move.moveInfo) == (ushort)MoveEncodingCodes.CAPTURE)
                {
                    ushort CapturedPieceValue = Evaluation.PieceTypeToPieceValue[(ushort)(boardPosition.UshortBoard[move.endPos] & 7)];
                    MoveValue += ((CapturedPieceValue * 10) - PieceValue);
                } 
                if ((move.moveInfo >> 3) == 1)
                {
                    //promotion is likely to be good
                    MoveValue += Evaluation.PieceTypeToPieceValue[(ushort)((move.moveInfo & 3) + 2)];
                }
                if ((((boardPosition.AttackingSquaresBitBoard[(boardPosition.ActiveColor ^ 8) + (ushort)PieceType.PAWN]) >> move.endPos) & 1) == 1)
                {
                    //moving onto square attacked by pawn is bad
                    MoveValue -= PieceValue - 50;
                } else if (((boardPosition.AttackingSquaresBitBoard[boardPosition.ActiveColor ^ 8] >> move.endPos) & 1) == 1)
                {
                    MoveValue -= Math.Max(0, PieceValue - 200) ;
                }
                this.MoveValues[i] = MoveValue;
            }
        }
        public Move[] GetOrderedMoves()
        {
            GenerateMoveValues();
            OrderMoves();
            return Moves;
        }
        //rearranges moves but guarentees the "best" move is first. Not working or used right now
        public Move[] GetOrderedMoves(Move bestMove)
        {
            GenerateMoveValues();
            PredictedGoodMove = bestMove;
            return Moves;
        }
        public void OrderMoves(Move bestMove)
        {
            PredictedGoodMove = bestMove;
            GenerateMoveValues();
            MoveIndex = 0;
        }
        public Move GetNextMove()
        {
            Move PickedMove = Moves[MoveIndex];
            int HighestValue = MoveValues[MoveIndex];
            int HighestValueIndex = MoveIndex;
            for (int i = MoveIndex + 1; i < Moves.Length; i++)
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
            MoveIndex++;
            return PickedMove;
        }
        public void OrderMoves()
        {
            for (int i = 0; i < Moves.Length; i++)
            {
                float HighestValue = MoveValues[i];
                int HighestValueIndex = i;
                for (int v = i; v < Moves.Length; v++)
                {
                    if (MoveValues[v] > HighestValue)
                    {
                        HighestValue = MoveValues[v];
                        HighestValueIndex = v;
                    }
                }
                (Moves[i], Moves[HighestValueIndex]) = (Moves[HighestValueIndex], Moves[i]);
                (MoveValues[i], MoveValues[HighestValueIndex]) = (MoveValues[HighestValueIndex], MoveValues[i]);
            }
        }
    }
}