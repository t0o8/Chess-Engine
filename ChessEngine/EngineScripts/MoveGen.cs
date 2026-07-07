using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace TOML
{
    public class MoveGeneration
    {
        public static ulong PositionsGenerated = 0;

        Position BoardPosition;
        ushort[] UshortBoard;
        ushort ActiveColor;
        ushort OppositeColor;
        ulong AttackingSquares;
        ulong SameColorBitBoard;
        ulong DifColorBitBoard;

        ushort KingPos;

        List<Move> Moves = new List<Move>();

        public static Stopwatch unMakeStopWatch = new Stopwatch();
        public static Stopwatch MakeStopWatch = new Stopwatch();
        public static Stopwatch MoveGenStopWatch = new Stopwatch();
        public static Stopwatch testWatch = new Stopwatch();

        public MoveGeneration(Position boardPosition)
        {
            BoardPosition = boardPosition;
            UpdateInfo();

            Moves = new List<Move>();
        }
        public void UpdateInfo()
        {
            UshortBoard = BoardPosition.UshortBoard;
            ActiveColor = BoardPosition.ActiveColor;
            OppositeColor = PieceColors.GetOppositeColor(ActiveColor);
            AttackingSquares = BoardPosition.AttackingSquaresBitBoard[OppositeColor];

            SameColorBitBoard = BoardPosition.BitBoards[1 + ActiveColor] | BoardPosition.BitBoards[2 + ActiveColor] | BoardPosition.BitBoards[3 + ActiveColor] |  BoardPosition.BitBoards[4 + ActiveColor]  | BoardPosition.BitBoards[5 + ActiveColor] | BoardPosition.BitBoards[6 + ActiveColor];
            DifColorBitBoard = BoardPosition.BitBoards[1 + OppositeColor] | BoardPosition.BitBoards[2 + OppositeColor] | BoardPosition.BitBoards[3 + OppositeColor] | BoardPosition.BitBoards[4 + OppositeColor] | BoardPosition.BitBoards[5 + OppositeColor] | BoardPosition.BitBoards[6 + OppositeColor];

            KingPos = (ushort)BitOperations.TrailingZeroCount(BoardPosition.BitBoards[ActiveColor + Piece.KING]);

            Moves.Clear();
        }
        public bool IsPinned(ushort position)
        {
            if (((BoardPosition.PinnedPiecesBitBoard[ActiveColor] >> position) & 1) ==1)
            {
                return true;
            } 
            return false;
        }
        public bool ContainsAttackingSquare(ushort position)
        {
            if (((AttackingSquares >> position) & 1) == 1)
            {
                return true;
            }  else
            {
                return false;
            }
        }

        public void GenerateLegalKingMoves(ushort piecePosition, bool GenerateOnlyCaptures = false)

        {
            ushort Piece = UshortBoard[piecePosition];
            ushort color = Position.GetColor(Piece);

            ulong MoveMask = PrecomputedInfo.KingMovesBitBoard[piecePosition];

            MoveMask &= ~SameColorBitBoard;
            MoveMask &= ~AttackingSquares;

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask -= 1ul << pos;

                if (UshortBoard[pos] == (int)PieceType.EMPTY)
                {
                    if (GenerateOnlyCaptures == true) { continue; }
                    Moves.Add(new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET));
                } else
                {
                    Moves.Add(new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE));
                }

            }
            if (GenerateOnlyCaptures == true || BoardPosition.InCheckValidSquares != ulong.MaxValue) { return; }

            if ((BoardPosition.CastlingAvailability & 1) == 1 && color == (int)PieceType.WHITE)
            {
                if (UshortBoard[5] == (int)PieceType.EMPTY && UshortBoard[6] == (int)PieceType.EMPTY && ContainsAttackingSquare(5) == false && ContainsAttackingSquare(6) == false) { Moves.Add(new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE)); }
            }
            if (((BoardPosition.CastlingAvailability >> 1) & 1) == 1 && color == (int)PieceType.WHITE)
            {
                if (UshortBoard[1] == (int)PieceType.EMPTY && UshortBoard[2] == (int)PieceType.EMPTY && UshortBoard[3] == (int)PieceType.EMPTY && ContainsAttackingSquare(3) == false && ContainsAttackingSquare(2) == false) { Moves.Add(new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE)); }
            }
            if (((BoardPosition.CastlingAvailability >> 2) & 1) == 1 && color == (int)PieceType.BLACK)
            {
                if (UshortBoard[61] == (int)PieceType.EMPTY && UshortBoard[62] == (int)PieceType.EMPTY && ContainsAttackingSquare(61) == false && ContainsAttackingSquare(62) == false) { Moves.Add(new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE)); }
            }
            if (((BoardPosition.CastlingAvailability >> 3) & 1) == 1 && color == (int)PieceType.BLACK)
            {
                if (UshortBoard[57] == (int)PieceType.EMPTY && UshortBoard[58] == (int)PieceType.EMPTY && UshortBoard[59] == (int)PieceType.EMPTY && ContainsAttackingSquare(59) == false && ContainsAttackingSquare(58) == false) { Moves.Add(new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE)); }
            }
            return;
        }

        public void GenerateLegalKnightMoves(ushort piecePosition, bool GenerateOnlyCaptures = false)
        {

            if (IsPinned(piecePosition) == true)
            {
                //piece is pinned and can't move as knights cannot move onto a pin ray
                return;
            }

            ulong MoveMask = PrecomputedInfo.KnightMovesBitBoard[piecePosition];
            MoveMask &= ~SameColorBitBoard;
            MoveMask &= BoardPosition.InCheckValidSquares;

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);

                MoveMask -= 1ul << pos;
                if (UshortBoard[pos] == (int)PieceType.EMPTY)
                {
                    if (GenerateOnlyCaptures == true)
                    {
                        continue;
                    }
                    Moves.Add(new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET));
                    continue;
                }
                Moves.Add(new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE));
            }
        }

        public void GenerateAllLegalPawnMoves(bool GenerateOnlyCaptures = false)
        {
            ulong AFile = 0x101010101010101;
            ulong FFile = 0x8080808080808080;

            ulong EighthRank = 0xff00000000000000;
            ulong SecondRank = 0xff00;
            ulong SeventhRank = 0xff000000000000;
            ulong FirstRank = 0xff;

            ulong PawnBitBoard = BoardPosition.BitBoards[ActiveColor + Piece.PAWN];
            ulong PawnLeftAttackMask = DifColorBitBoard;
            ulong PawnRightAttackMask = DifColorBitBoard;

            if (BoardPosition.EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
            {
                int TargetSquare;
                if (ActiveColor == Piece.WHITE)
                {
                    TargetSquare = BoardPosition.EnPassantTargetSquare - 8;
                } else
                {
                    TargetSquare = BoardPosition.EnPassantTargetSquare + 8;
                }

                if (BoardPosition.InCheckValidSquares == ulong.MaxValue || BitOperations.TrailingZeroCount(BoardPosition.InCheckValidSquares)  == TargetSquare)
                {
                    ulong EnPassantMask = PawnBitBoard;
                    if (ActiveColor == Piece.WHITE)
                    {
                        EnPassantMask &= (~AFile & (1ul << (BoardPosition.EnPassantTargetSquare)) >> 7) + (~FFile & (1ul << (BoardPosition.EnPassantTargetSquare)) >> 9);
                    } else
                    {
                        EnPassantMask &= (~AFile & (1ul << (BoardPosition.EnPassantTargetSquare)) << 9) + (~FFile & (1ul << (BoardPosition.EnPassantTargetSquare)) << 7);
                    }
                    while (EnPassantMask != 0)
                    {
                        ushort pos = (ushort)BitOperations.TrailingZeroCount(EnPassantMask);
                        EnPassantMask -= 1ul << pos;
                        if (CanPlayEPMove(pos) == false)
                        {
                            continue;
                        }
                        if (IsPinned(pos) == true && (PrecomputedInfo.LineBitBoard[pos][KingPos] & (1ul << BoardPosition.EnPassantTargetSquare)) == 0)
                        {
                            continue;
                        }
                        Moves.Add(new Move(pos, BoardPosition.EnPassantTargetSquare, (ushort)MoveEncodingCodes.EPCAPTURE));
                    }
                }
            }
            if (ActiveColor == Piece.WHITE)
            {
                PawnLeftAttackMask &= ((PawnBitBoard & ~AFile) << 7) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;
                PawnRightAttackMask &= ((PawnBitBoard & ~FFile) << 9) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;

                ulong LeftPromotingPawns = EighthRank & PawnLeftAttackMask;
                ulong RightPromotingPawns = EighthRank & PawnRightAttackMask;
                //prevents the promotion pawns from being looped through again
                PawnLeftAttackMask &= ~EighthRank;
                PawnRightAttackMask &= ~EighthRank;

                ulong QuitePawnMask = (~BoardPosition.AllPieceBitBoard) & (PawnBitBoard << 8) & BoardPosition.InCheckValidSquares;
                ulong QuitePawnPromotionMask = QuitePawnMask & EighthRank;
                QuitePawnMask &= ~EighthRank;
                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard << 8)) & ((PawnBitBoard & SecondRank )<< 16) & BoardPosition.InCheckValidSquares;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns -= (1ul << pos);
                    if (IsPinned((ushort)(pos - 7)) == true && (PrecomputedInfo.LineBitBoard[pos - 7][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 8));
                    Moves.Add(new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 9));
                    Moves.Add(new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 10));
                    Moves.Add(new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 11));
                }

                while (RightPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(RightPromotingPawns);
                    RightPromotingPawns -= (1ul << pos);
                    if (IsPinned((ushort)(pos - 9)) == true && (PrecomputedInfo.LineBitBoard[pos - 9][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 8));
                    Moves.Add(new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 9));
                    Moves.Add(new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 10));
                    Moves.Add(new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 11));
                }

                while (PawnLeftAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnLeftAttackMask);
                    PawnLeftAttackMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos - 7)) == true && (PrecomputedInfo.LineBitBoard[pos - 7][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE));
                }
                while (PawnRightAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnRightAttackMask);
                    PawnRightAttackMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos - 9)) == true && (PrecomputedInfo.LineBitBoard[pos - 9][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE));
                }
                if (GenerateOnlyCaptures == true)
                {
                    return;
                }
                while (QuitePawnPromotionMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnPromotionMask);
                    QuitePawnPromotionMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos - 8)) == true && (PrecomputedInfo.LineBitBoard[pos - 8][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 8), pos, 8));
                    Moves.Add(new Move((ushort)(pos - 8), pos, 9));
                    Moves.Add(new Move((ushort)(pos - 8), pos, 10));
                    Moves.Add(new Move((ushort)(pos - 8), pos, 11));
                }
                while (QuitePawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnMask);
                    QuitePawnMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos - 8)) == true && (PrecomputedInfo.LineBitBoard[pos - 8][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 8), pos, (ushort)MoveEncodingCodes.QUIET));
                }
                while (DoublePawnPushMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(DoublePawnPushMask);
                    DoublePawnPushMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos - 16)) == true && (PrecomputedInfo.LineBitBoard[pos - 16][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH));
                }
            } else
            {
                PawnLeftAttackMask &= ((PawnBitBoard & ~AFile) >> 9) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;
                PawnRightAttackMask &= ((PawnBitBoard & ~FFile) >> 7) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;

                ulong LeftPromotingPawns = FirstRank & PawnLeftAttackMask;
                ulong RightPromotingPawns = FirstRank & PawnRightAttackMask;
                PawnLeftAttackMask &= ~FirstRank;
                PawnRightAttackMask &= ~FirstRank;
                ulong QuitePawnMask = (~BoardPosition.AllPieceBitBoard) & (PawnBitBoard >> 8) & BoardPosition.InCheckValidSquares;
                ulong QuitePromotionPawnMask = QuitePawnMask & FirstRank;
                QuitePawnMask &= ~FirstRank;

                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard >> 8)) & ((PawnBitBoard & SeventhRank) >> 16) & BoardPosition.InCheckValidSquares;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns -= (1ul << pos);
                    if (IsPinned((ushort)(pos + 9)) == true && (PrecomputedInfo.LineBitBoard[pos + 9][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 8));
                    Moves.Add(new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 9));
                    Moves.Add(new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 10));
                    Moves.Add(new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 11));
                }

                while (RightPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(RightPromotingPawns);
                    RightPromotingPawns -= (1ul << pos);
                    if (IsPinned((ushort)(pos + 7)) == true && (PrecomputedInfo.LineBitBoard[pos + 7][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 8));
                    Moves.Add(new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 9));
                    Moves.Add(new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 10));
                    Moves.Add(new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 11));
                }

                while (PawnLeftAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnLeftAttackMask);
                    PawnLeftAttackMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos + 9)) == true && (PrecomputedInfo.LineBitBoard[pos + 9][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE));
                }
                while (PawnRightAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnRightAttackMask);
                    PawnRightAttackMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos + 7)) == true && (PrecomputedInfo.LineBitBoard[pos + 7][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE));
                }
                if (GenerateOnlyCaptures == true)
                {
                    return;
                }
                while (QuitePawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnMask);
                    QuitePawnMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos + 8)) == true && (PrecomputedInfo.LineBitBoard[pos + 8][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 8), pos, (ushort)MoveEncodingCodes.QUIET));
                }
                while (QuitePromotionPawnMask !=0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePromotionPawnMask);
                    QuitePromotionPawnMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos + 8)) == true && (PrecomputedInfo.LineBitBoard[pos + 8][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 8), pos, 8));
                    Moves.Add(new Move((ushort)(pos + 8), pos, 9));
                    Moves.Add(new Move((ushort)(pos + 8), pos, 10));
                    Moves.Add(new Move((ushort)(pos + 8), pos, 11));
                }
                while (DoublePawnPushMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(DoublePawnPushMask);
                    DoublePawnPushMask -= (1ul << pos);
                    if (IsPinned((ushort)(pos + 16)) == true && (PrecomputedInfo.LineBitBoard[pos + 16][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH));
                }
            }
        }

        public void GenerateLegalSlidyMoves(ushort piecePosition,bool GenerateOnlyCaptures = false)
        {
            ushort Piece = UshortBoard[piecePosition];
            ushort PieceWithoutColor = PieceColors.GetColorlessPiece(Piece);

            ulong SudoLegalMoves;
            ulong MagicNumber;
            int ShiftNum;
            ulong MoveMask = 0;
            if (PieceWithoutColor != (ushort)PieceType.ROOK)
            {
                SudoLegalMoves = PrecomputedInfo.BishopBlockingMasks[piecePosition];
                MagicNumber = MagicNumbers.BishopMagicNumbers[piecePosition];
                ShiftNum = 64 - PrecomputedInfo.BishopBlockingMaskCount[piecePosition];

                ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
                ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

                MoveMask = MoveMask | PrecomputedInfo.BishopMovesBitBoard[piecePosition][Index];
            } 
            if (PieceWithoutColor != (ushort)PieceType.BISHOP)
            {
                
                SudoLegalMoves = PrecomputedInfo.RookBlockingMasks[piecePosition];
                MagicNumber = MagicNumbers.RookMagicNumbers[piecePosition];
                ShiftNum = 64 - PrecomputedInfo.RookBlockingMaskCount[piecePosition];

                ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
                ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

                MoveMask = MoveMask | PrecomputedInfo.RookMovesBitBoard[piecePosition][Index];
            } 
            MoveMask &= ~SameColorBitBoard;
            MoveMask &= BoardPosition.InCheckValidSquares;

            if (IsPinned(piecePosition))
            {
                MoveMask &= PrecomputedInfo.LineBitBoard[piecePosition][KingPos];
            }

            while (MoveMask !=0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask -= 1ul << pos;
                ulong IsCapture = (DifColorBitBoard >> pos) & 1;
                if (IsCapture == 0 && GenerateOnlyCaptures == true)
                {
                    continue;
                }
                ushort MoveCode = (IsCapture == 1) ? (ushort)MoveEncodingCodes.CAPTURE : (ushort)MoveEncodingCodes.QUIET;

                Moves.Add(new Move(piecePosition, pos, MoveCode));
            }
            return;
        }

        //checks if EP move is possible
        //assumes the move is EP
        public bool CanPlayEPMove(ushort startPos)
        {
            //logic: EP is not possible only if the pawn pos is between the king and rook/queen
            //if there are more than 2 pieces between then there is at least 1 extra piece blocking the attack after pawns clear

            bool RowContainsRook = false;
            bool FoundKing = false;
            ushort KingPos = 0;
            ushort RookPos = 0;
            ushort BlockingPieces = 0;
            
            Position.GetXYPosition(startPos, out ushort X, out ushort Y);
            for (int i = Y * 8; i < (Y * 8) + 8; i++)
            {
                if (UshortBoard[i] == PieceColors.GetOppositeColor(ActiveColor) + (int)PieceType.ROOK || UshortBoard[i] == PieceColors.GetOppositeColor(ActiveColor) + (int)PieceType.QUEEN)
                {
                    RowContainsRook = true;
                    RookPos = (ushort)i;
                    continue;
                }
                if (UshortBoard[i] == ActiveColor + (int)PieceType.KING)
                {
                    FoundKing = true;
                    KingPos = (ushort)i;
                    continue;
                }
                if ((KingPos != 0)^(RookPos !=0) == true && UshortBoard[i] !=(ushort)PieceType.EMPTY)
                {
                    BlockingPieces++;
                }
            }
            if (FoundKing == true && RowContainsRook == true)
            {
                if ((KingPos < startPos && startPos < RookPos) || (RookPos < startPos && startPos < KingPos))
                {
                    if (BlockingPieces > 2)
                    {
                        //no enpassent available!
                        return true;
                    } else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //generates legal moves for a piece given its position
        public Move[] GenerateAllLegalMoves(bool GenerateOnlyCaptures = false)
        {
            if (BoardPosition.HalfMoveClock == 100)
            {
                //50 move rule triggered
                return new Move[0];
            }
            testWatch.Start();

            BoardPosition.UpdatePositionStats();
            UpdateInfo();
            testWatch.Stop();
            //Move[] Moves = new Move[218];
            //ushort Count = 0;

            // List<Move> movesList = new List<Move>();

            GenerateLegalKingMoves(KingPos, GenerateOnlyCaptures);
            
            //double check, can only move king
            if (BoardPosition.CheckCount > 1) { return Moves.ToArray(); }

            GenerateAllLegalPawnMoves();

            ulong KnightMask = BoardPosition.BitBoards[ActiveColor + Piece.KNIGHT];
            while (KnightMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(KnightMask);
                KnightMask -= 1ul << pos;

                GenerateLegalKnightMoves(pos, GenerateOnlyCaptures);
            }
            ulong SlidyPieceMask = BoardPosition.BitBoards[ActiveColor + Piece.BISHOP] | BoardPosition.BitBoards[ActiveColor + Piece.ROOK] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            while (SlidyPieceMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(SlidyPieceMask);
                SlidyPieceMask -= 1ul << pos;

                GenerateLegalSlidyMoves(pos, GenerateOnlyCaptures);
            }
            return Moves.ToArray();
        }
        public int GetTotalMoveCombinationCount(int depth)
        {
            
            int Count = 0;
            if (depth == 1)
            {
                MoveGenStopWatch.Start();
                int e = GenerateAllLegalMoves().Length;
                MoveGenStopWatch.Stop();
                return e;
            }
            MoveGenStopWatch.Start();
            Move[] moves = GenerateAllLegalMoves();
            MoveGenStopWatch.Stop();
            foreach (Move move in moves)
            {
                MakeStopWatch.Start();
                UnMove unmove =  BoardPosition.MakeMove(move);
                PositionsGenerated++;
                MakeStopWatch.Stop();
                UpdateInfo();
                Count += GetTotalMoveCombinationCount(depth - 1);
                unMakeStopWatch.Start();
                BoardPosition.UnMakeMove(unmove);
                unMakeStopWatch.Stop();
               
            }
            
            return Count;
        }
        public int GetTotalMoveCombinationCountTest(int depth)
        {

            int Count = 0;
            if (depth == 1)
            {
                MoveGenStopWatch.Start();
                int e = GenerateAllLegalMoves().Length;
                MoveGenStopWatch.Stop();
                return e;
            }
            MoveGenStopWatch.Start();
            Move[] moves = GenerateAllLegalMoves();
            MoveGenStopWatch.Stop();
            foreach (Move move in moves)
            {
                MakeStopWatch.Start();
                UnMove unmove = BoardPosition.MakeMove(move);
                MakeStopWatch.Stop();
                int e = GetTotalMoveCombinationCount(depth - 1);
                if (depth == 1)
                {
                    Console.WriteLine(Position.NumberToBoardLetter(move.startPos) + Position.NumberToBoardLetter(move.endPos) + " " + e);
                }
                UpdateInfo();
                Count += GetTotalMoveCombinationCount(depth - 1);
                unMakeStopWatch.Start();
                BoardPosition.UnMakeMove(unmove);
                unMakeStopWatch.Stop();

            }

            return Count;
        }

    }
}
