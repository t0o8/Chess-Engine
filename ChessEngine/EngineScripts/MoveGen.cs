using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System;
using System.Drawing;

namespace TOML
{
    public static class MoveGeneration
    {
        public static ulong PositionsGenerated = 0;

        static Position BoardPosition;
        static ushort[] UshortBoard;
        static ushort ActiveColor;
        static ushort OppositeColor;
        static ulong AttackingSquares;
        static ulong SameColorBitBoard;
        static ulong DifColorBitBoard;

        static ushort KingPos;
        static byte MoveCount;

        public static void UpdateInfo()
        { 
            UshortBoard = BoardPosition.UshortBoard;
            ActiveColor = BoardPosition.ActiveColor;
            OppositeColor = BoardPosition.OppositeColor;
            AttackingSquares = BoardPosition.AttackingSquaresBitBoard[OppositeColor];

            SameColorBitBoard = BoardPosition.BitBoards[ActiveColor];
            DifColorBitBoard = BoardPosition.BitBoards[OppositeColor];

            KingPos = (ushort)BitOperations.TrailingZeroCount(BoardPosition.BitBoards[ActiveColor + Piece.KING]);

            MoveCount = 0;
        }
        public static bool IsPinned(ushort position)
        {
            return ((BoardPosition.PinnedPiecesBitBoard[BoardPosition.ActiveColor] >> position) & 1) == 1;
        }
        public static void GenerateLegalKingMoves(ushort piecePosition, Span<Move> moveList, bool GenerateOnlyCaptures = false)
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
                    moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                    MoveCount++;
                } else
                {
                    moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
            }
            if (GenerateOnlyCaptures == true || BoardPosition.CheckCount != 0) { return; }

            ulong Blockers = BoardPosition.AllPieceBitBoard | (BoardPosition.AttackingSquaresBitBoard[OppositeColor] & 0xfdfffffffffffffd);
            
            if (color == (int)PieceType.WHITE)
            {
                if ((MoveGenConstants.CastlingMasks[2] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 1) == 1)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE);
                    MoveCount++;
                }
                if ((MoveGenConstants.CastlingMasks[1] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 2) == 2)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE);
                    MoveCount++;
                }

            } else
            {
                if ((MoveGenConstants.CastlingMasks[8] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 4) == 4)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE);
                    MoveCount++;
                }
                if ((MoveGenConstants.CastlingMasks[4] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 8) == 8)
                {
                    moveList[MoveCount] =new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE);
                    MoveCount++;
                }
            } 
        }

        public static void GeneratePsuedoQuietKingMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong MoveMask = PrecomputedInfo.KingMovesBitBoard[piecePosition];
            MoveMask &= ~BoardPosition.AllPieceBitBoard;

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
            ulong Blockers = BoardPosition.AllPieceBitBoard;
            if (ActiveColor == (int)PieceType.WHITE)
            {
                if ((MoveGenConstants.CastlingMasks[2] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 1) == 1)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE);
                    MoveCount++;
                }
                if ((MoveGenConstants.CastlingMasks[1] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 2) == 2)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE);
                    MoveCount++;
                }

            } else
            {
                if ((MoveGenConstants.CastlingMasks[8] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 4) == 4)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition + 2), (int)MoveEncodingCodes.KINGCASTLE);
                    MoveCount++;
                }
                if ((MoveGenConstants.CastlingMasks[4] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 8) == 8)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition - 2), (int)MoveEncodingCodes.QUEENCASTLE);
                    MoveCount++;
                }
            }
        }
        public static void GeneratePsuedoCaptureKingMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong MoveMask = PrecomputedInfo.KingMovesBitBoard[piecePosition];

            MoveMask &= BoardPosition.BitBoards[BoardPosition.OppositeColor];

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoQuietKnightMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong MoveMask = PrecomputedInfo.KnightMovesBitBoard[piecePosition] & ~BoardPosition.AllPieceBitBoard & target;
            while (MoveMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, (ushort)pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoCaptureKnightMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong MoveMask = PrecomputedInfo.KnightMovesBitBoard[piecePosition] & BoardPosition.BitBoards[OppositeColor] & target;
            while (MoveMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, (ushort)pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        public static void GenerateLegalQuietKnightMoves(ushort piecePosition, Span<Move> moveList)
        {

            ulong MoveMask = PrecomputedInfo.KnightMovesBitBoard[piecePosition];
            MoveMask &= ~BoardPosition.AllPieceBitBoard;
            MoveMask &= BoardPosition.InCheckValidSquares;


            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);

                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GenerateLegalCaptureKnightMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong MoveMask = PrecomputedInfo.KnightMovesBitBoard[piecePosition];
            MoveMask &= BoardPosition.InCheckValidSquares;
            MoveMask &= DifColorBitBoard;

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);

                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoQuitePawnMoves(Span<Move> moveList, ulong target)
        {
            ulong PawnBitBoard = BoardPosition.BitBoards[ActiveColor + Piece.PAWN];

            if (ActiveColor == Piece.WHITE)
            {
                ulong QuitePawnMask = (~BoardPosition.AllPieceBitBoard) & (PawnBitBoard << 8) & target;
                ulong QuitePawnPromotionMask = QuitePawnMask & BitBoardConstants.EIGHT_RANK;
                QuitePawnMask &= ~BitBoardConstants.EIGHT_RANK;

                //cannot simplify as incheckvalid squares removes positions from quitepawnmask that might work for doublepawnpushmask
                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard << 8)) & ((PawnBitBoard & BitBoardConstants.SECOND_RANK) << 16) & target;

                while (QuitePawnPromotionMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnPromotionMask);
                    QuitePawnPromotionMask &= QuitePawnPromotionMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 8), pos, 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 8), pos, 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 8), pos, 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 8), pos, 11);

                    MoveCount += 4;
                }
                while (QuitePawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnMask);
                    QuitePawnMask &= QuitePawnMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 8), pos, (ushort)MoveEncodingCodes.QUIET);
                    MoveCount++;
                }
                while (DoublePawnPushMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(DoublePawnPushMask);
                    DoublePawnPushMask &= DoublePawnPushMask - 1;
                    moveList[MoveCount] = new Move((ushort)(pos - 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH);
                    MoveCount++;
                }
            } else
            {
                ulong QuitePawnMask = (~BoardPosition.AllPieceBitBoard) & (PawnBitBoard >> 8) & target;
                ulong QuitePromotionPawnMask = QuitePawnMask & BitBoardConstants.FIRST_RANK;
                QuitePawnMask &= ~BitBoardConstants.FIRST_RANK;

                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard >> 8)) & ((PawnBitBoard & BitBoardConstants.SEVENTH_RANK) >> 16) & target;

                while (QuitePawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePawnMask);
                    QuitePawnMask &= QuitePawnMask - 1;
                    moveList[MoveCount] = new Move((ushort)(pos + 8), pos, (ushort)MoveEncodingCodes.QUIET);
                    MoveCount++;
                }
                while (QuitePromotionPawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(QuitePromotionPawnMask);
                    QuitePromotionPawnMask &= QuitePromotionPawnMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 8), pos, 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 8), pos, 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 8), pos, 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 8), pos, 11);

                    MoveCount += 4;
                }
                while (DoublePawnPushMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(DoublePawnPushMask);
                    DoublePawnPushMask &= DoublePawnPushMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH);
                    MoveCount++;
                }
            }
        }
        public static void GeneratePsuedoCapturePawnMoves(Span<Move> moveList, ulong target)
        {
            ulong PawnBitBoard = BoardPosition.BitBoards[ActiveColor + Piece.PAWN];
            ulong PawnLeftAttackMask = BoardPosition.BitBoards[BoardPosition.OppositeColor] & target;
            ulong PawnRightAttackMask = BoardPosition.BitBoards[BoardPosition.OppositeColor] & target;

            if (BoardPosition.EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
            {
                ulong EnPassantMask = PawnBitBoard;
                int TargetSquare;
                if (ActiveColor == Piece.WHITE)
                {
                    TargetSquare = BoardPosition.EnPassantTargetSquare - 8;
                    EnPassantMask &= (~BitBoardConstants.A_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) >> 7) + (~BitBoardConstants.H_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) >> 9);
                } else
                {
                    TargetSquare = BoardPosition.EnPassantTargetSquare + 8;
                    EnPassantMask &= (~BitBoardConstants.A_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) << 9) + (~BitBoardConstants.H_FILE & (1ul << (BoardPosition.EnPassantTargetSquare)) << 7);
                }
                while (EnPassantMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(EnPassantMask);
                    EnPassantMask &= EnPassantMask - 1;

                    if (((target >> BoardPosition.EnPassantTargetSquare) & 1) == 1 || ((target >> TargetSquare) & 1) == 1)
                    {
                        moveList[MoveCount] = new Move(pos, BoardPosition.EnPassantTargetSquare, (ushort)MoveEncodingCodes.EPCAPTURE);
                        MoveCount++;
                    }
                }
            }
            if (ActiveColor == Piece.WHITE)
            {
                PawnLeftAttackMask &= ((PawnBitBoard & ~BitBoardConstants.A_FILE) << 7) & ~BoardPosition.BitBoards[ActiveColor];
                PawnRightAttackMask &= ((PawnBitBoard & ~BitBoardConstants.H_FILE) << 9) & ~BoardPosition.BitBoards[ActiveColor];

                ulong LeftPromotingPawns = BitBoardConstants.EIGHT_RANK & PawnLeftAttackMask;
                ulong RightPromotingPawns = BitBoardConstants.EIGHT_RANK & PawnRightAttackMask;
                //prevents the promotion pawns from being looped through again
                PawnLeftAttackMask &= ~BitBoardConstants.EIGHT_RANK;
                PawnRightAttackMask &= ~BitBoardConstants.EIGHT_RANK;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns &= LeftPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
                }

                while (RightPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(RightPromotingPawns);
                    RightPromotingPawns &= RightPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
                }

                while (PawnLeftAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnLeftAttackMask);
                    PawnLeftAttackMask &= PawnLeftAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
                while (PawnRightAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnRightAttackMask);
                    PawnRightAttackMask &= PawnRightAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
            } else
            {
                PawnLeftAttackMask &= ((PawnBitBoard & ~BitBoardConstants.A_FILE) >> 9) & ~SameColorBitBoard;
                PawnRightAttackMask &= ((PawnBitBoard & ~BitBoardConstants.H_FILE) >> 7) & ~SameColorBitBoard;

                ulong LeftPromotingPawns = BitBoardConstants.FIRST_RANK & PawnLeftAttackMask;
                ulong RightPromotingPawns = BitBoardConstants.FIRST_RANK & PawnRightAttackMask;
                PawnLeftAttackMask &= ~BitBoardConstants.FIRST_RANK;
                PawnRightAttackMask &= ~BitBoardConstants.FIRST_RANK;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns &= LeftPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
                }

                while (RightPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(RightPromotingPawns);
                    RightPromotingPawns &= RightPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
                }

                while (PawnLeftAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnLeftAttackMask);
                    PawnLeftAttackMask &= PawnLeftAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
                while (PawnRightAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnRightAttackMask);
                    PawnRightAttackMask &= PawnRightAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
            }
        }
        public static void GenerateAllLegalQuietPawnMoves(Span<Move> moveList)
        {
            ulong PawnBitBoard = BoardPosition.BitBoards[ActiveColor + Piece.PAWN];

            if (ActiveColor == Piece.WHITE)
            {
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

                    moveList[MoveCount] = new Move((ushort)(pos - 8), pos, 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 8), pos, 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 8), pos, 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 8), pos, 11);

                    MoveCount += 4;
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

                    moveList[MoveCount] = new Move((ushort)(pos - 8), pos, (ushort)MoveEncodingCodes.QUIET);
                    MoveCount++;
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
                    moveList[MoveCount] = new Move((ushort)(pos - 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH);
                    MoveCount++;
                }
            } else
            {
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
                    moveList[MoveCount] = new Move((ushort)(pos + 8), pos, (ushort)MoveEncodingCodes.QUIET);
                    MoveCount++;
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
                    
                    moveList[MoveCount] = new Move((ushort)(pos + 8), pos, 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 8), pos, 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 8), pos, 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 8), pos, 11);

                    MoveCount += 4;
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
                    moveList[MoveCount] = new Move((ushort)(pos + 16), pos, (ushort)MoveEncodingCodes.DOUBLEPAWNPUSH);
                    MoveCount++;
                }
            }
        }
        public static void GenerateAllLegalCapturePawnMoves(Span<Move> moveList)
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

                if (BoardPosition.InCheckValidSquares == ulong.MaxValue || BitOperations.TrailingZeroCount(BoardPosition.InCheckValidSquares) == TargetSquare)
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
                        moveList[MoveCount] = new Move(pos, BoardPosition.EnPassantTargetSquare, (ushort)MoveEncodingCodes.EPCAPTURE);
                        MoveCount++;
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
                    moveList[MoveCount] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
                }

                while (RightPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(RightPromotingPawns);
                    RightPromotingPawns &= RightPromotingPawns - 1;
                    if (IsPinned((ushort)(pos - 9)) == true && (PrecomputedInfo.LineBitBoard[pos - 9][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    moveList[MoveCount] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
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
                    moveList[MoveCount] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
                while (PawnRightAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(PawnRightAttackMask);
                    PawnRightAttackMask &= PawnRightAttackMask - 1;
                    if (IsPinned((ushort)(pos - 9)) == true && (PrecomputedInfo.LineBitBoard[pos - 9][KingPos] & (1ul << pos)) == 0)
                    {
                        //piece is pinned and can't move here
                        continue;
                    }
                    moveList[MoveCount] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
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
                    moveList[MoveCount] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
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
                    moveList[MoveCount] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 8);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 9);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 10);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE + 11);

                    MoveCount += 4;
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
                    moveList[MoveCount] = new Move((ushort)(pos + 9), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
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
                    moveList[MoveCount] = new Move((ushort)(pos + 7), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
            }
        }
        public static void GeneratePsuedoQuietBishopMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong AttackMask = PrecomputedInfo.BishopBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.BishopMagicNumbers[piecePosition];
            int ShiftNum = 64 - PrecomputedInfo.BishopBlockingMaskCount[piecePosition];

            ulong BlockersMask = AttackMask & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.BishopMovesBitBoard[piecePosition][Index] & target;

            MoveMask &= ~BoardPosition.AllPieceBitBoard;

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoCaptureBishopMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong AttackMask = PrecomputedInfo.BishopBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.BishopMagicNumbers[piecePosition];
            int ShiftNum = 64 - PrecomputedInfo.BishopBlockingMaskCount[piecePosition];

            ulong BlockersMask = AttackMask & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.BishopMovesBitBoard[piecePosition][Index] & target;

            MoveMask &= BoardPosition.BitBoards[BoardPosition.OppositeColor];

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }

        public static void GeneratePsuedoBishopMoves(ushort piecePosition, Span<Move> moveList, ulong target, bool GenerateOnlyCaptures)
        {
            ulong AttackMask = PrecomputedInfo.BishopBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.BishopMagicNumbers[piecePosition];
            int ShiftNum = PrecomputedInfo.BishopMagicShift[piecePosition];

            ulong BlockersMask = AttackMask & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.BishopMovesBitBoard[piecePosition][Index] & target;

            ulong CaptureMask = BoardPosition.BitBoards[BoardPosition.OppositeColor] & MoveMask;

            while (CaptureMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(CaptureMask);
                CaptureMask &= CaptureMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
            if (GenerateOnlyCaptures == true) { return; }

            ulong QuietMask = MoveMask &= ~BoardPosition.AllPieceBitBoard;

            while (QuietMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(QuietMask);
                QuietMask &= QuietMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GenerateLegalQuietBishopMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong SudoLegalMoves = PrecomputedInfo.BishopBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.BishopMagicNumbers[piecePosition];
            int ShiftNum = PrecomputedInfo.BishopMagicShift[piecePosition];

            ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.BishopMovesBitBoard[piecePosition][Index];

            MoveMask &= ~BoardPosition.AllPieceBitBoard;
            MoveMask &= BoardPosition.InCheckValidSquares;

            if (IsPinned(piecePosition))
            {
                MoveMask &= PrecomputedInfo.LineBitBoard[piecePosition][KingPos];
            }

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GenerateLegalCaptureBishopMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong SudoLegalMoves = PrecomputedInfo.BishopBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.BishopMagicNumbers[piecePosition];
            int ShiftNum = PrecomputedInfo.BishopMagicShift[piecePosition];

            ulong MoveMask = 0;

            ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            MoveMask = MoveMask | PrecomputedInfo.BishopMovesBitBoard[piecePosition][Index];

            MoveMask &= BoardPosition.InCheckValidSquares;
            MoveMask &= DifColorBitBoard;

            if (IsPinned(piecePosition))
            {
                MoveMask &= PrecomputedInfo.LineBitBoard[piecePosition][KingPos];
            }

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoQuietRookMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong AttackMask = PrecomputedInfo.RookBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.RookMagicNumbers[piecePosition];
            int ShiftNum = 64 - PrecomputedInfo.RookBlockingMaskCount[piecePosition];

            ulong BlockersMask = AttackMask & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.RookMovesBitBoard[piecePosition][Index] & target;

            MoveMask &= ~BoardPosition.AllPieceBitBoard;

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoCaptureRookMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong AttackMask = PrecomputedInfo.RookBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.RookMagicNumbers[piecePosition];
            int ShiftNum = 64 - PrecomputedInfo.RookBlockingMaskCount[piecePosition];

            ulong BlockersMask = AttackMask & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.RookMovesBitBoard[piecePosition][Index] & target;

            MoveMask &= BoardPosition.BitBoards[OppositeColor];

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoRookMoves(ushort piecePosition, Span<Move> moveList, ulong target, bool GenerateOnlyCaptures)
        {
            ulong AttackMask = PrecomputedInfo.RookBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.RookMagicNumbers[piecePosition];
            int ShiftNum = PrecomputedInfo.RookMagicShift[piecePosition];

            ulong BlockersMask = AttackMask & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.RookMovesBitBoard[piecePosition][Index] & target;

            ulong CaptureMask = MoveMask & BoardPosition.BitBoards[BoardPosition.OppositeColor];
            while (CaptureMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(CaptureMask);
                CaptureMask &= CaptureMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
            if (GenerateOnlyCaptures == true) { return; }

            ulong QuietMask = MoveMask & ~BoardPosition.AllPieceBitBoard;
            while (QuietMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(QuietMask);
                QuietMask &= QuietMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GenerateLegalQuietRookMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong SudoLegalMoves = PrecomputedInfo.RookBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.RookMagicNumbers[piecePosition];
            int ShiftNum = PrecomputedInfo.RookMagicShift[piecePosition];

            ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            ulong MoveMask = PrecomputedInfo.RookMovesBitBoard[piecePosition][Index];

            MoveMask &= ~BoardPosition.AllPieceBitBoard;
            MoveMask &= BoardPosition.InCheckValidSquares;

            if (IsPinned(piecePosition))
            {
                MoveMask &= PrecomputedInfo.LineBitBoard[piecePosition][KingPos];
            }

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GenerateLegalCaptureRookMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong SudoLegalMoves = PrecomputedInfo.RookBlockingMasks[piecePosition];
            ulong MagicNumber = MagicNumbers.RookMagicNumbers[piecePosition];
            int ShiftNum = PrecomputedInfo.RookMagicShift[piecePosition];
            ulong MoveMask = 0;

            ulong BlockersMask = SudoLegalMoves & BoardPosition.AllPieceBitBoard;
            ulong Index = (BlockersMask * MagicNumber) >> ShiftNum;

            MoveMask = MoveMask | PrecomputedInfo.RookMovesBitBoard[piecePosition][Index];

            MoveMask &= BoardPosition.InCheckValidSquares;
            MoveMask &= DifColorBitBoard;

            if (IsPinned(piecePosition))
            {
                MoveMask &= PrecomputedInfo.LineBitBoard[piecePosition][KingPos];
            }

            while (MoveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(MoveMask);
                MoveMask &= MoveMask - 1;
                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        //checks if EP move is possible for pins
        //assumes the piece is a pawn
        public static bool CanPlayEPMove(ushort startPos)
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
        public static byte GenerateAllPseudoLegalMoves(Position boardPosition, Span<Move> moveList, bool GenerateOnlyCaptures = false)
        {
            BoardPosition = boardPosition;
            if (BoardPosition.HalfMoveClock == 100)
            {
                //50 move rule triggered
                return 0;
            }
            MoveCount = 0;
            
            BoardPosition.UpdatePositionStats();
            UpdateInfo();

            ulong MoveMask = ulong.MaxValue;

            GeneratePsuedoCaptureKingMoves(KingPos, moveList);
            if (GenerateOnlyCaptures == false)
            {
                GeneratePsuedoQuietKingMoves(KingPos, moveList);
            }

            if (BoardPosition.CheckCount > 1) { 
                return MoveCount; 
            } else if (BoardPosition.CheckCount == 1)
            {
                MoveMask = BoardPosition.InCheckValidSquares;
            }
            GeneratePsuedoCapturePawnMoves(moveList, MoveMask);
            if (GenerateOnlyCaptures == false)
            {
                GeneratePsuedoQuitePawnMoves(moveList, MoveMask);
            }
            
            ulong KnightMask = BoardPosition.BitBoards[ActiveColor + Piece.KNIGHT] & ~BoardPosition.PinnedPiecesBitBoard[ActiveColor]; // pinned knights cannot move
            while (KnightMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(KnightMask);
                KnightMask &= KnightMask - 1;

                GeneratePsuedoCaptureKnightMoves(pos, moveList, MoveMask);

                if (GenerateOnlyCaptures == false)
                {
                    GeneratePsuedoQuietKnightMoves(pos, moveList, MoveMask);
                }
            }

            ulong BishopQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.BISHOP] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            while (BishopQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(BishopQueenMask);
                BishopQueenMask &= BishopQueenMask - 1;

                GeneratePsuedoBishopMoves(pos, moveList, MoveMask, GenerateOnlyCaptures);
            }
            ulong RookQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.ROOK] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            while (RookQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(RookQueenMask);
                RookQueenMask &= RookQueenMask - 1;
                GeneratePsuedoRookMoves(pos, moveList, MoveMask, GenerateOnlyCaptures);
            }
            return MoveCount;
        }

        public static byte GenerateAllLegalMoves(Position boardPosition, Span<Move> moveList, bool GenerateOnlyCaptures = false)
        {
            BoardPosition = boardPosition;
            if (BoardPosition.HalfMoveClock == 100)
            {
                //50 move rule triggered
                return 0;
            }

            BoardPosition.UpdatePositionStats();
            UpdateInfo();

            MoveCount = 0;

            GenerateLegalKingMoves(KingPos, moveList, GenerateOnlyCaptures);
            
            //double check, can only move king
            if (BoardPosition.CheckCount > 1) { return MoveCount; }

            GenerateAllLegalCapturePawnMoves(moveList);
            if (GenerateOnlyCaptures == false)
            {
                GenerateAllLegalQuietPawnMoves(moveList);
            }

            ulong KnightMask = BoardPosition.BitBoards[ActiveColor + Piece.KNIGHT] & ~BoardPosition.PinnedPiecesBitBoard[ActiveColor]; // pinned knights cannot move
            while (KnightMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(KnightMask);
                KnightMask &= KnightMask - 1;

                GenerateLegalCaptureKnightMoves(pos, moveList);

                if (GenerateOnlyCaptures == false)
                {
                    GenerateLegalQuietKnightMoves(pos, moveList);
                }
            }

            ulong BishopQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.BISHOP] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            while (BishopQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(BishopQueenMask);
                BishopQueenMask &= BishopQueenMask - 1;

                GenerateLegalCaptureBishopMoves(pos, moveList);
                if (GenerateOnlyCaptures == false)
                {
                    GenerateLegalQuietBishopMoves(pos, moveList);
                }
            }

            ulong RookQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.ROOK] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            while (RookQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(RookQueenMask);
                RookQueenMask &= RookQueenMask - 1;

                GenerateLegalCaptureRookMoves(pos, moveList);
                if (GenerateOnlyCaptures == false)
                {
                    GenerateLegalQuietRookMoves(pos, moveList);
                }
            }

            return MoveCount;
        }
        public static int GetTotalMoveCombinationCount(Position boardPosition, int depth)
        {
            int Count = 0;
            if (depth == 1)
            {
                Span<Move> moveList = stackalloc Move[218];
                byte e = GenerateAllLegalMoves(boardPosition, moveList);
                return e;
            }
            Span<Move> moves = stackalloc Move[218];
            byte MoveCount = GenerateAllLegalMoves(boardPosition, moves);

            for (int i = 0; i < MoveCount; i++)
            {
                Move move = moves[i];

                UnMove unmove = BoardPosition.MakeMove(move);
                PositionsGenerated++;
                Count += GetTotalMoveCombinationCount(boardPosition, depth - 1);
                BoardPosition.UnMakeMove(unmove);
            }
            
            return Count;
        }
        public static int GetTotalMoveCombinationCountTest(Position boardPosition, int depth)
        {
            int Count = 0;
            if (depth == 1)
            {
                Span<Move> moveList = stackalloc Move[218];
                byte e = GenerateAllLegalMoves(boardPosition, moveList);
                for (int i = 0; i < e; i++)
                {
                    Move move = moveList[i];
                    Console.WriteLine(Position.NumberToBoardLetter(move.startPos) + Position.NumberToBoardLetter(move.endPos) + " " + e);
                }
                return e;
            }
            Span<Move> moves = stackalloc Move[218];
            byte MoveCount = GenerateAllLegalMoves(boardPosition, moves);

            for (int i = 0; i < MoveCount; i++)
            {
                Move move = moves[i];

                UnMove unmove = BoardPosition.MakeMove(move);
                int e = GetTotalMoveCombinationCount(boardPosition, depth - 1);
                if (depth == 2)
                {
                    Console.WriteLine(Position.NumberToBoardLetter(move.startPos) + Position.NumberToBoardLetter(move.endPos) + " " + e);
                }
                Count += e;
                BoardPosition.UnMakeMove(unmove);
            }

            return Count;
        }
        public static int GetMoveCountTest(Position boardPosition, int depth)
        {
            int Count = 0;
            if (depth == 1)
            {
                Span<Move> moveList = stackalloc Move[218];
                byte MaxMoves = GenerateAllPseudoLegalMoves(boardPosition, moveList);
                for(int i = 0; i < MaxMoves; i++)
                {
                    if (boardPosition.IsLegal(moveList[i]))
                    {
                        Count++;
                        Console.WriteLine(Position.NumberToBoardLetter(moveList[i].startPos) + Position.NumberToBoardLetter(moveList[i].endPos) );
                    }
                }
                return Count;
            }

            Span<Move> moves = stackalloc Move[218];
            byte MoveCount = GenerateAllPseudoLegalMoves(boardPosition, moves);

            for (int i = 0; i < MoveCount; i++)
            {
                Move move = moves[i];

                if (boardPosition.IsLegal(move) == false)
                {
                    continue;
                }
                UnMove unmove = BoardPosition.MakeMove(move);
                int e = GetMoveCountTest(boardPosition, depth - 1);
                Count += e;
                BoardPosition.UnMakeMove(unmove);
            }

            return Count;
        }
    }
}
