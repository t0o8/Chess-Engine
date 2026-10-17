using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOML
{
    public static class MoveEncodingCodes
    {
        public static ushort QUIET = 0;
        public static ushort DOUBLE_PAWN_PUSH = 1;
        public static ushort KING_CASTLE = 2;
        public static ushort QUEEN_CASTLE = 3;
        public static ushort CAPTURE = 4;
        public static ushort EP_CAPTURE = 5;

        public static ushort PROMOTION_KNIGHT_QUIET = 8;
        public static ushort PROMOTION_BISHOP_QUIET = 9;
        public static ushort PROMOTION_ROOK_QUIET = 10;
        public static ushort PROMOTION_QUEEN_QUIET = 11;

        public static ushort PROMOTION_KNIGHT_CAPTURE = 12;
        public static ushort PROMOTION_BISHOP_CAPTURE = 13;
        public static ushort PROMOTION_ROOK_CAPTURE = 14;
        public static ushort PROMOTION_QUEEN_CAPTURE = 15;
    }

    public readonly struct Move
    {
        public readonly ushort MoveValue;   //IIIIEEEEEESSSSSS
        // 8: promotion
        // 4: capture
        // 1: double pawn push
        // 2: king castle
        // 3: queen castle


        //1-4: knight, bishop, rook, queen for promotion

        public Move(ushort startPos, ushort endPos)
        {
            this.MoveValue = (ushort)(startPos + (endPos << 6));
        }
        public Move(ushort startPos, ushort endPos, ushort moveInfo)
        {
            this.MoveValue = (ushort)((startPos + (endPos << 6)) + (moveInfo << 12));
        }

        public readonly ushort StartPos => (ushort)(MoveValue & 63);
        public readonly ushort EndPos => (ushort)((MoveValue >> 6) & 63);
        public readonly ushort MoveInfo => (ushort)(MoveValue >> 12);

        public Move GetMoveWithMoveInfo(Position BoardPosition)
        {
            int piece = PieceHelper.GetPieceWithoutColor(BoardPosition.UshortBoard[this.StartPos]);

            Move move = new Move(this.StartPos, this.EndPos);

            if (piece == TOML.Piece.KING)
            {
                if (this.EndPos - this.StartPos == 2) { move = new Move(this.StartPos, this.EndPos, 2); }
                if (this.EndPos - this.StartPos == -2) { move = new Move(this.StartPos, this.EndPos, 3); }
            }
            if (piece == TOML.Piece.PAWN)
            {
                if (Math.Abs(this.EndPos - this.StartPos) == 16) { move = new Move(this.StartPos, this.EndPos, 1); }
                if (this.EndPos == BoardPosition.EnPassantTargetSquare) { move = new Move(this.StartPos, this.EndPos, 5); }
            }
            if (PieceHelper.GetColor(BoardPosition.UshortBoard[this.StartPos]) != PieceHelper.GetColor(BoardPosition.UshortBoard[this.EndPos]) && BoardPosition.UshortBoard[this.EndPos] != 0)
            {
                move = new Move(this.StartPos, this.EndPos, (ushort)(this.MoveInfo + 4));
            }
            return move;
        }
        public Move GetMoveWithMoveInfoPromotion(Position BoardPosition, char promotionChar)
        {
            Move move = new Move();
            promotionChar = char.ToLower(promotionChar);
            if (PieceHelper.GetColor(BoardPosition.UshortBoard[this.StartPos]) != PieceHelper.GetColor(BoardPosition.UshortBoard[this.EndPos]) && BoardPosition.UshortBoard[this.EndPos] != 0)
            {
                if (promotionChar == 'n')
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_KNIGHT_CAPTURE);
                } else if (promotionChar == 'b')
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_BISHOP_CAPTURE);
                } else if (promotionChar == 'r')
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_ROOK_CAPTURE);
                } else
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_QUEEN_CAPTURE);
                }
            } else
            {
                if (promotionChar == 'n')
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_KNIGHT_QUIET);
                } else if (promotionChar == 'b')
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_BISHOP_QUIET);
                } else if (promotionChar == 'r')
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_ROOK_QUIET);
                } else
                {
                    move = new Move(this.StartPos, this.EndPos, MoveEncodingCodes.PROMOTION_QUEEN_QUIET);
                }
            }


            return move;
        }

        public void Print()
        {
            Console.WriteLine("Move is " + this.StartPos + ", " + this.EndPos);
        }
        public string GetText()
        {
            return BoardHelper.NumberToBoardLetter(StartPos) + BoardHelper.NumberToBoardLetter(EndPos);
        }
        public bool IsEqual(Move comparisonMove)
        {
            if (comparisonMove.MoveValue == this.MoveValue)
            {
                return true;
            }
            return false;
        }
    }
    public readonly struct UnMove
    {
        public readonly ushort StartPos;
        public readonly ushort EndPos;
        public readonly ushort MoveInfo;
        public readonly uint UnMoveInfo;
        public UnMove(Move move, uint unMoveInfo)
        {
            this.StartPos = move.StartPos;
            this.EndPos = move.EndPos;
            this.MoveInfo = move.MoveInfo;
            this.UnMoveInfo = unMoveInfo;
        }
    }
}
