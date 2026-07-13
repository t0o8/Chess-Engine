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

            SameColorBitBoard = BoardPosition.BitBoards[ActiveColor];
            DifColorBitBoard = BoardPosition.BitBoards[OppositeColor];

            KingPos = (ushort)BitOperations.TrailingZeroCount(BoardPosition.BitBoards[ActiveColor + Piece.KING]);

            Moves.Clear();
        }
        public bool IsPinned(ushort position)
        {
            return ((BoardPosition.PinnedPiecesBitBoard[ActiveColor] >> position) & 1) == 1;
        }

        public void GenerateLegalKingMoves(ushort piecePosition, bool GenerateOnlyCaptures = false)
        {
            ushort color = ActiveColor;
            ulong MoveMask = PrecomputedInfo.KingMovesBitBoard[piecePosition];

            MoveMask &= ~SameColorBitBoard;
            MoveMask &= ~AttackingSquares;

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;

                if (UshortBoard[pos] == Piece.EMPTY)
                {
                    if (GenerateOnlyCaptures == true) { continue; }
                    Moves.Add(new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET));
                } else
                {
                    Moves.Add(new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE));
                }

            }
            if (GenerateOnlyCaptures == true || BoardPosition.InCheckValidSquares != ulong.MaxValue) { return; }

            ulong Blockers = BoardPosition.AllPieceBitBoard | (BoardPosition.AttackingSquaresBitBoard[OppositeColor] & 0xfdfffffffffffffd);
            
            if (color == (int)PieceType.WHITE)
            {
                if ((MoveGenConstants.CastlingMasks[2] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 1) == 1)
                {
                    Moves.Add(new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE));
                }
                if ((MoveGenConstants.CastlingMasks[1] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 2) == 2)
                {
                    Moves.Add(new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE));
                }

            } else
            {
                if ((MoveGenConstants.CastlingMasks[8] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 4) == 4)
                {
                    Moves.Add(new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE));
                }
                if ((MoveGenConstants.CastlingMasks[4] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 8) == 8)
                {
                    Moves.Add(new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE));
                }
            } 
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

                MoveMask &= MoveMask - 1;
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
            ulong PawnBitBoard = BoardPosition.BitBoards[ActiveColor + Piece.PAWN];
            ulong PawnLeftAttackMask = DifColorBitBoard;
            ulong PawnRightAttackMask = DifColorBitBoard;

            if (BoardPosition.EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
            {
                int TargetSquare;
                ulong EnPassantMask = PawnBitBoard;
                if (ActiveColor == Piece.WHITE)
                {
                    TargetSquare = BoardPosition.EnPassantTargetSquare - 8;
                    EnPassantMask &= (~BitBoardConstants.A_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) >> 7) + (~BitBoardConstants.H_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) >> 9);
                } else
                {
                    TargetSquare = BoardPosition.EnPassantTargetSquare + 8;
                    EnPassantMask &= (~BitBoardConstants.A_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) << 9) + (~BitBoardConstants.H_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) << 7);
                }

                if (BoardPosition.InCheckValidSquares == ulong.MaxValue || BitOperations.TrailingZeroCount(BoardPosition.InCheckValidSquares)  == TargetSquare)
                {
                    while (EnPassantMask != 0)
                    {
                        ushort pos = (ushort)BitOperations.TrailingZeroCount(EnPassantMask);
                        EnPassantMask &= EnPassantMask - 1;
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
                PawnLeftAttackMask &= ((PawnBitBoard & ~BitBoardConstants.A_FILE) << 7) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;
                PawnRightAttackMask &= ((PawnBitBoard & ~BitBoardConstants.H_FILE) << 9) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;

                ulong LeftPromotingPawns = BitBoardConstants.EIGHT_RANK & PawnLeftAttackMask;
                ulong RightPromotingPawns = BitBoardConstants.EIGHT_RANK & PawnRightAttackMask;
                //prevents the promotion pawns from being looped through again
                PawnLeftAttackMask &= ~BitBoardConstants.EIGHT_RANK;
                PawnRightAttackMask &= ~BitBoardConstants.EIGHT_RANK;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns &= LeftPromotingPawns - 1;
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
                    RightPromotingPawns &= RightPromotingPawns -1 ;
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
                    PawnLeftAttackMask &= PawnLeftAttackMask - 1;
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
                    PawnRightAttackMask &= PawnRightAttackMask -1 ;
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
                ulong QuitePawnMask = (~BoardPosition.AllPieceBitBoard) & (PawnBitBoard << 8) & BoardPosition.InCheckValidSquares;
                ulong QuitePawnPromotionMask = QuitePawnMask & BitBoardConstants.EIGHT_RANK;
                QuitePawnMask &= ~BitBoardConstants.EIGHT_RANK;

                //cannot simplify as incheckvalid squares removes positions from quitepawnmask that might work for doublepawnpushmask
                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard << 8)) & ((PawnBitBoard & BitBoardConstants.SECOND_RANK) << 16) & BoardPosition.InCheckValidSquares;

                while (QuitePawnPromotionMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnPromotionMask);
                    QuitePawnPromotionMask &= QuitePawnPromotionMask - 1;
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
                    QuitePawnMask &= QuitePawnMask - 1;
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
                    DoublePawnPushMask &= DoublePawnPushMask - 1;
                    if (IsPinned((ushort)(pos - 16)) == true && (PrecomputedInfo.LineBitBoard[pos - 16][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos - 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH));
                }
            } else
            {
                PawnLeftAttackMask &= ((PawnBitBoard & ~BitBoardConstants.A_FILE) >> 9) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;
                PawnRightAttackMask &= ((PawnBitBoard & ~BitBoardConstants.H_FILE) >> 7) & BoardPosition.InCheckValidSquares & ~SameColorBitBoard;

                ulong LeftPromotingPawns = BitBoardConstants.FIRST_RANK & PawnLeftAttackMask;
                ulong RightPromotingPawns = BitBoardConstants.FIRST_RANK & PawnRightAttackMask;
                PawnLeftAttackMask &= ~BitBoardConstants.FIRST_RANK;
                PawnRightAttackMask &= ~BitBoardConstants.FIRST_RANK;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns &= LeftPromotingPawns - 1;
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
                    RightPromotingPawns &= RightPromotingPawns - 1;
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
                    PawnLeftAttackMask &= PawnLeftAttackMask - 1;
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
                    PawnRightAttackMask &= PawnRightAttackMask - 1;
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

                ulong QuitePawnMask = (~BoardPosition.AllPieceBitBoard) & (PawnBitBoard >> 8) & BoardPosition.InCheckValidSquares;
                ulong QuitePromotionPawnMask = QuitePawnMask & BitBoardConstants.FIRST_RANK;
                QuitePawnMask &= ~BitBoardConstants.FIRST_RANK;

                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard >> 8)) & ((PawnBitBoard & BitBoardConstants.SEVENTH_RANK) >> 16) & BoardPosition.InCheckValidSquares;

                while (QuitePawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnMask);
                    QuitePawnMask &= QuitePawnMask - 1;
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
                    QuitePromotionPawnMask &= QuitePromotionPawnMask - 1;
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
                    DoublePawnPushMask &= DoublePawnPushMask - 1;
                    if (IsPinned((ushort)(pos + 16)) == true && (PrecomputedInfo.LineBitBoard[pos + 16][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    Moves.Add(new Move((ushort)(pos + 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH));
                }
            }
        }
        public void GenerateLegalBishopMoves(ushort piecePosition, bool GenerateOnlyCaptures = false)
        {
            ulong SudoLegalMoves = PrecomputedInfo.BishopBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.BishopMagicNumbers[piecePosition];
            int ShiftNum = 64 - PrecomputedInfo.BishopBlockingMaskCount[piecePosition];

            ulong MoveMask = 0;

            ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            MoveMask = MoveMask | PrecomputedInfo.BishopMovesBitBoard[piecePosition][Index];

            MoveMask &= ~SameColorBitBoard;
            MoveMask &= BoardPosition.InCheckValidSquares;

            if (IsPinned(piecePosition))
            {
                MoveMask &= PrecomputedInfo.LineBitBoard[piecePosition][KingPos];
            }

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                ulong IsCapture = (DifColorBitBoard >> pos) & 1;
                if (IsCapture == 0 && GenerateOnlyCaptures == true)
                {
                    continue;
                }
                ushort MoveCode = (IsCapture == 1) ? (ushort)MoveEncodingCodes.CAPTURE : (ushort)MoveEncodingCodes.QUIET;

                Moves.Add(new Move(piecePosition, pos, MoveCode));
            }
        }
        public void GenerateLegalRookMoves(ushort piecePosition, bool GenerateOnlyCaptures = false)
        {
            ulong SudoLegalMoves = PrecomputedInfo.RookBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.RookMagicNumbers[piecePosition];
            int ShiftNum = 64 - PrecomputedInfo.RookBlockingMaskCount[piecePosition];
            ulong MoveMask = 0;

            ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            MoveMask = MoveMask | PrecomputedInfo.RookMovesBitBoard[piecePosition][Index];

            MoveMask &= ~SameColorBitBoard;
            MoveMask &= BoardPosition.InCheckValidSquares;

            if (IsPinned(piecePosition))
            {
                MoveMask &= PrecomputedInfo.LineBitBoard[piecePosition][KingPos];
            }

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                ulong IsCapture = (DifColorBitBoard >> pos) & 1;
                if (IsCapture == 0 && GenerateOnlyCaptures == true)
                {
                    continue;
                }
                ushort MoveCode = (IsCapture == 1) ? (ushort)MoveEncodingCodes.CAPTURE : (ushort)MoveEncodingCodes.QUIET;

                Moves.Add(new Move(piecePosition, pos, MoveCode));
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
        }

        //checks if EP move is possible for pins
        //assumes the piece is a pawn
        public bool CanPlayEPMove(ushort startPos)
        {
            //logic: EP is not possible only if the pawn pos is between the king and rook/queen
            //if there are more than 2 pieces between then there is at least 1 extra piece blocking the attack after pawns clear
            
            Position.GetXYPosition(startPos, out ushort X, out ushort Y);
            ulong Rank = BitBoardConstants.FIRST_RANK << (Y * 8);

            if ((Rank & BoardPosition.BitBoards[Piece.KING + ActiveColor]) == 0) {
                //king not on rank
                return true;
            }
            ulong PossiblePinners = Rank & (BoardPosition.BitBoards[Piece.ROOK + OppositeColor] | BoardPosition.BitBoards[Piece.QUEEN + OppositeColor]);
            while (PossiblePinners != 0)
            {
                int pos = BitOperations.TrailingZeroCount(PossiblePinners);
                PossiblePinners &= PossiblePinners - 1;

                ulong Blockers = PrecomputedInfo.InBetweenBitBoard[pos][KingPos] & BoardPosition.AllPieceBitBoard;
                if (((Blockers >> startPos) & 1) == 0)
                {
                    //pawn isn't here
                    continue;
                } else if (BitOperations.PopCount(Blockers) < 3) 
                {
                    return false;
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
                KnightMask &= KnightMask - 1;

                GenerateLegalKnightMoves(pos, GenerateOnlyCaptures);
            }

            ulong BishopQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.BISHOP] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            
            while (BishopQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(BishopQueenMask);
                BishopQueenMask &= BishopQueenMask - 1;

                GenerateLegalBishopMoves(pos, GenerateOnlyCaptures);
            }

            ulong RookQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.ROOK] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];

            while (RookQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(RookQueenMask);
                RookQueenMask &= RookQueenMask - 1;

                GenerateLegalRookMoves(pos, GenerateOnlyCaptures);
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
