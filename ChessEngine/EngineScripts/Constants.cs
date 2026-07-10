using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;


namespace TOML
{
    static class PositionConstants
    {
        public const ushort NoEnPassantMoves = 127;

    }
    static class EvaluationConstants
    {
        public const int ABSOLUTE_LOWER_BOUND = -999999;
        public const int ABSOLUTE_UPPER_BOUND = 999999;

        public const int CHECKMATE_BASE_EVAL = -99999; //this value must be much lower than any eval a position can have but still around 50-100 higher than the absolute lower bound so that when the depth is subtracted it's still higher than the lower bound
        public const int CHECKMATE_MINIMAL_EVAL = CHECKMATE_BASE_EVAL + byte.MaxValue;
    }
    static class NodeConstants
    {
        public const byte PV = 0; // exact eval value
        public const byte CUT = 1; // lower bound
        public const byte ALL = 2; // upper bound
    }

    static class MagicNumbers
    {
        public static ulong[] BishopMagicNumbers = { 567365517705248, 565166710329360, 1127008043008516, 573953726750720, 299342040662016, 143006304829444, 71485978847284, 18709146126464, 4417445430282, 2207747476737, 4402375163936, 2242006487428, 1169304846336, 558618398720, 283736547648, 208440657921, 1125934535280649, 562967267640321, 281483700994434, 140754938888192, 140892117698560, 140750641733632, 70370959040560, 35186553651728, 571783092700176, 285873294026760, 37383664042056, 285873157703696, 145136751288320, 70644259688472, 141287613427712, 70647950737920, 286493648101376, 145204273283593, 21998840318081, 4415226904864, 71471477293248, 141295834169600, 283682900541696, 141295834169600, 143005500588544, 71488388997184, 13197494977536, 120863131648, 4505455371264, 282578800285184, 565167297397016, 282578800285184, 71485978847284, 36318278141960, 139654594560, 545783809, 17742299136, 4415371214856, 1130332447375744, 565166710329360, 18709146126464, 208440657921, 554174598, 2131968, 136380672, 17247543808, 4417445430282, 567365517705248 };
        public static ulong[] RookMagicNumbers = { 36028945266638992, 18014673924263936, 36063981659521160, 36037593188532352, 36033195100602496, 36030996780449793, 36029896673198592, 72059243863277578, 140738029420672, 70369282098240, 140806243484033, 140771851239552, 140789053130752, 562958544405513, 141287277732096, 140741783349376, 36833648001026, 72568338710914, 18692773511424, 388127873051904, 141287378387968, 282575067938824, 9899899748356, 2199027613980, 70370891670144, 70370893758848, 17602929754240, 8798248898560, 4400194519168, 2201172574336, 2491082082321, 4423816355921, 35459325493376, 35186527977476, 35185630384384, 8798248898560, 13196328962048, 39651272376352, 17597084992008, 277595821057, 70379481628708, 70506451582976, 35735235330064, 563535143895072, 8796160163968, 3940958979358736, 9932380569602, 277109407748, 35459325493376, 35208132821632, 17594402734464, 8864829284608, 4398986068096, 2201172574336, 1683728892928, 279182576128, 38491769634817, 35768760271107, 8933549867075, 9501021343745, 563019212130314, 281509403558401, 829197256740, 143093137538 };
    }
    static class BitBoardConstants
    {
        public static ulong A_FILE = 0x101010101010101;
        public static ulong F_FILE = 0x8080808080808080;

        public static ulong FIRST_RANK = 0xff;
        public static ulong SECOND_RANK = 0xff00;
        public static ulong SEVENTH_RANK = 0xff000000000000;
        public static ulong EIGHT_RANK = 0xff00000000000000;
    }
    public struct Move
    {
        readonly ushort moveValue;   //IIIIEEEEEESSSSSS
        //public readonly ushort startPos;
        //public readonly ushort endPos;
        //public ushort moveInfo;

        // 8: promotion
        // 4: capture
        // 1: double pawn push
        // 2: king castle
        // 3: queen castle
     

        //1-4: knight, bishop, rook, queen for promotion

        public Move(ushort startPos, ushort endPos)
        {
            this.moveValue = (ushort)(startPos + (endPos << 6));
        }
        public Move(ushort startPos, ushort endPos, ushort moveInfo)
        {
            this.moveValue = (ushort)((startPos + (endPos << 6)) + (moveInfo << 12));
        }

        public readonly ushort startPos => (ushort)(moveValue & 63);
        public readonly ushort endPos => (ushort)((moveValue >> 6) & 63);
        public readonly ushort moveInfo => (ushort)(moveValue >> 12);

        public Move GetMoveWithMoveInfo(Position BoardPosition)
        {
            int piece = PieceColors.GetColorlessPiece(BoardPosition.UshortBoard[this.startPos]);

            Move move = new Move(this.startPos, this.endPos);

            if (piece == TOML.Piece.KING)
            {
                if (this.endPos - this.startPos == 2) { move = new Move(this.startPos, this.endPos, 2); }
                if (this.endPos - this.startPos == -2) { move = new Move(this.startPos, this.endPos, 3); }
            }
            if (piece == TOML.Piece.PAWN)
            {
                if (Math.Abs(this.endPos - this.startPos) == 16) { move = new Move(this.startPos, this.endPos, 1); }
                if (this.endPos == BoardPosition.EnPassantTargetSquare) { move = new Move(this.startPos, this.endPos, 5); }
            }
            if (PieceColors.GetColor(BoardPosition.UshortBoard[this.startPos]) != PieceColors.GetColor(BoardPosition.UshortBoard[this.endPos]) && BoardPosition.UshortBoard[this.endPos] != 0)
            {
                move = new Move(this.startPos, this.endPos, (ushort)(this.moveInfo + 4));
            }
            return move;
        }
        public Move GetMoveWithMoveInfoPromotion(Position BoardPosition, char promotionChar)
        {
            int piece = PieceColors.GetColorlessPiece(BoardPosition.UshortBoard[this.startPos]);

            Move move = new Move(this.startPos, this.endPos);

            if (PieceColors.GetColor(BoardPosition.UshortBoard[this.startPos]) != PieceColors.GetColor(BoardPosition.UshortBoard[this.endPos]) && BoardPosition.UshortBoard[this.endPos] != 0)
            {
                if (promotionChar == 'k')
                {
                    move = new Move(this.startPos, this.endPos, 12);
                } else if (promotionChar == 'b')
                {
                    move = new Move(this.startPos, this.endPos, 13);
                } else if (promotionChar == 'r')
                {
                    move = new Move(this.startPos, this.endPos, 14);
                } else
                {
                    move = new Move(this.startPos, this.endPos, 15);
                }
            } else
            {
                if (promotionChar == 'k')
                {
                    move = new Move(this.startPos, this.endPos, 8);
                } else if (promotionChar == 'b')
                {
                    move = new Move(this.startPos, this.endPos, 9);
                } else if (promotionChar == 'r')
                {
                    move = new Move(this.startPos, this.endPos, 10);
                } else
                {
                    move = new Move(this.startPos, this.endPos, 11);
                }
            }

            
            return move;
        }

        public void Print()
        {
            Console.WriteLine("Move is " + this.startPos + ", " + this.endPos);
        }
        public bool IsEqual(Move comparisonMove)
        {
            if (comparisonMove.moveValue == this.moveValue)
            {
                return true;
            }
            return false;
        }
    }
    public struct UnMove
    {
        public ushort startPos;
        public ushort endPos;
        public ushort moveInfo;
        public uint UnMoveInfo;
        public UnMove(Move move, uint unMoveInfo)
        {
            this.startPos = move.startPos;
            this.endPos = move.endPos;
            this.moveInfo = move.moveInfo;
            this.UnMoveInfo = unMoveInfo;
        }
    }
    enum GenerateMoveType
    {
        Legal,
        PseudoLegal,
    }
    
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
    static class Conversions
    {
        public static Dictionary<char, ushort> LetterToNumber = new Dictionary<char, ushort>() {
            {'a', 0 },
            {'b', 1 },
            {'c', 2 },
            {'d', 3 },
            {'e', 4 },
            {'f', 5 },
            {'g', 6 },
            {'h', 7 },
        };
        public static Dictionary<char, ushort> FenToPieceType = new Dictionary<char, ushort>() {
            {'p', 1 },
            {'n', 2 },
            {'b', 3 },
            {'r', 4 },
            {'q', 5 },
            {'k', 6 },
        };
    }
    enum MoveEncodingCodes
    {
        QUIET,
        DOUBLEPAWNPUSH,
        KINGCASTLE,
        QUEENCASTLE,
        CAPTURE,
        EPCAPTURE
    }
    enum PieceType
    {
        EMPTY,
        PAWN,
        KNIGHT,
        BISHOP,
        ROOK,
        QUEEN,
        KING,
        WHITE = 0,
        BLACK = 8,
    }

    static class PieceColors
    {
        public static ushort GetColor(ushort piece)
        {
            return (ushort)(piece & 8);
        }
        public static bool AreSameColor(ushort piece1, ushort piece2)
        {
            return GetColor(piece1) == GetColor(piece2);
        } 
        public static ushort GetColorlessPiece(ushort piece)
        {
            return (ushort)(piece & 7);
        }
        public static ushort GetOppositeColor(ushort color)
        {
            if (color == (ushort)PieceType.WHITE)
            {
                return (ushort)PieceType.BLACK;
            } else
            {
                return (ushort)PieceType.WHITE;
            } 
        }
    }
    static class BoardPositionHelper
    {
        public static ushort NumLetterToBoardPos(string str)
        {
            int XValue = Conversions.LetterToNumber[str[0]];
            int YValue = (Int32.Parse(str[1].ToString()) - 1);

            return (ushort)(XValue + (YValue * 8));
        }
    }
}