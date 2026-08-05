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

            KingPos = BoardPosition.GetKingPos(ActiveColor);
            MoveCount = 0;
        }
        public static void GeneratePsuedoQuietKingMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong moveMask = PrecomputedInfo.KingMovesBitBoard[piecePosition];
            moveMask &= ~BoardPosition.AllPieceBitBoard;

            while (moveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(moveMask);
                moveMask &= moveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
            ulong Blockers = BoardPosition.AllPieceBitBoard;
            if (ActiveColor == Piece.WHITE)
            {
                if ((MoveGenConstants.CastlingMasks[2] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 1) == 1)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition + 2), MoveEncodingCodes.KING_CASTLE);
                    MoveCount++;
                }
                if ((MoveGenConstants.CastlingMasks[1] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 2) == 2)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition - 2), MoveEncodingCodes.QUEEN_CASTLE);
                    MoveCount++;
                }

            } else
            {
                if ((MoveGenConstants.CastlingMasks[8] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 4) == 4)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition + 2), MoveEncodingCodes.KING_CASTLE);
                    MoveCount++;
                }
                if ((MoveGenConstants.CastlingMasks[4] & Blockers) == 0 && (BoardPosition.CastlingAvailability & 8) == 8)
                {
                    moveList[MoveCount] = new Move(piecePosition, (ushort)(piecePosition - 2), MoveEncodingCodes.QUEEN_CASTLE);
                    MoveCount++;
                }
            }
        }
        public static void GeneratePsuedoCaptureKingMoves(ushort piecePosition, Span<Move> moveList)
        {
            ulong moveMask = PrecomputedInfo.KingMovesBitBoard[piecePosition];

            moveMask &= BoardPosition.BitBoards[BoardPosition.OppositeColor];

            while (moveMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(moveMask);
                moveMask &= moveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoQuietKnightMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong moveMask = PrecomputedInfo.KnightMovesBitBoard[piecePosition] & ~BoardPosition.AllPieceBitBoard & target;
            while (moveMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(moveMask);
                moveMask &= moveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, (ushort)pos, (ushort)MoveEncodingCodes.QUIET);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoCaptureKnightMoves(ushort piecePosition, Span<Move> moveList, ulong target)
        {
            ulong moveMask = PrecomputedInfo.KnightMovesBitBoard[piecePosition] & BoardPosition.BitBoards[OppositeColor] & target;
            while (moveMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(moveMask);
                moveMask &= moveMask - 1;

                moveList[MoveCount] = new Move(piecePosition, (ushort)pos, (ushort)MoveEncodingCodes.CAPTURE);
                MoveCount++;
            }
        }
        public static void GeneratePsuedoQuitePawnMoves(Span<Move> moveList, ulong target)
        {
            ulong pawnBitBoard = BoardPosition.BitBoards[ActiveColor + Piece.PAWN];

            if (ActiveColor == Piece.WHITE)
            {
                ulong quitePawnMask = (~BoardPosition.AllPieceBitBoard) & (pawnBitBoard << 8) & target;
                ulong quitePawnPromotionMask = quitePawnMask & BitBoardConstants.EIGHT_RANK;
                quitePawnMask &= ~BitBoardConstants.EIGHT_RANK;

                //cannot simplify as incheckvalid squares removes positions from quitepawnmask that might work for doublepawnpushmask
                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard << 8)) & ((pawnBitBoard & BitBoardConstants.SECOND_RANK) << 16) & target;

                while (quitePawnPromotionMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(quitePawnPromotionMask);
                    quitePawnPromotionMask &= quitePawnPromotionMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 8), pos, MoveEncodingCodes.PROMOTION_KNIGHT_QUIET);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 8), pos, MoveEncodingCodes.PROMOTION_BISHOP_QUIET);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 8), pos, MoveEncodingCodes.PROMOTION_ROOK_QUIET);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 8), pos, MoveEncodingCodes.PROMOTION_QUEEN_QUIET);

                    MoveCount += 4;
                }
                while (quitePawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(quitePawnMask);
                    quitePawnMask &= quitePawnMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 8), pos, MoveEncodingCodes.QUIET);
                    MoveCount++;
                }
                while (DoublePawnPushMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(DoublePawnPushMask);
                    DoublePawnPushMask &= DoublePawnPushMask - 1;
                    moveList[MoveCount] = new Move((ushort)(pos - 16), pos, MoveEncodingCodes.DOUBLE_PAWN_PUSH);
                    MoveCount++;
                }
            } else
            {
                ulong quitePawnMask = (~BoardPosition.AllPieceBitBoard) & (pawnBitBoard >> 8) & target;
                ulong quitePromotionPawnMask = quitePawnMask & BitBoardConstants.FIRST_RANK;
                quitePawnMask &= ~BitBoardConstants.FIRST_RANK;

                ulong DoublePawnPushMask = (~BoardPosition.AllPieceBitBoard & ~(BoardPosition.AllPieceBitBoard >> 8)) & ((pawnBitBoard & BitBoardConstants.SEVENTH_RANK) >> 16) & target;

                while (quitePawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(quitePawnMask);
                    quitePawnMask &= quitePawnMask - 1;
                    moveList[MoveCount] = new Move((ushort)(pos + 8), pos, MoveEncodingCodes.QUIET);
                    MoveCount++;
                }
                while (quitePromotionPawnMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(quitePromotionPawnMask);
                    quitePromotionPawnMask &= quitePromotionPawnMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 8), pos, MoveEncodingCodes.PROMOTION_KNIGHT_QUIET);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 8), pos, MoveEncodingCodes.PROMOTION_BISHOP_QUIET);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 8), pos, MoveEncodingCodes.PROMOTION_ROOK_QUIET);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 8), pos, MoveEncodingCodes.PROMOTION_QUEEN_QUIET);

                    MoveCount += 4;
                }
                while (DoublePawnPushMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(DoublePawnPushMask);
                    DoublePawnPushMask &= DoublePawnPushMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 16), pos, MoveEncodingCodes.DOUBLE_PAWN_PUSH);
                    MoveCount++;
                }
            }
        }
        public static void GeneratePsuedoCapturePawnMoves(Span<Move> moveList, ulong target)
        {
            ulong pawnBitBoard = BoardPosition.BitBoards[ActiveColor + Piece.PAWN];
            ulong pawnLeftAttackMask = BoardPosition.BitBoards[BoardPosition.OppositeColor] & target;
            ulong pawnRightAttackMask = BoardPosition.BitBoards[BoardPosition.OppositeColor] & target;

            if (BoardPosition.EnPassantTargetSquare != PositionConstants.NO_EN_PASSANT_MOVES)
            {
                ulong EnPassantMask = pawnBitBoard;
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
                        moveList[MoveCount] = new Move(pos, BoardPosition.EnPassantTargetSquare, MoveEncodingCodes.EP_CAPTURE);
                        MoveCount++;
                    }
                }
            }
            if (ActiveColor == Piece.WHITE)
            {
                pawnLeftAttackMask &= ((pawnBitBoard & ~BitBoardConstants.A_FILE) << 7) & ~BoardPosition.BitBoards[ActiveColor];
                pawnRightAttackMask &= ((pawnBitBoard & ~BitBoardConstants.H_FILE) << 9) & ~BoardPosition.BitBoards[ActiveColor];

                ulong LeftPromotingPawns = BitBoardConstants.EIGHT_RANK & pawnLeftAttackMask;
                ulong RightPromotingPawns = BitBoardConstants.EIGHT_RANK & pawnRightAttackMask;
                //prevents the promotion pawns from being looped through again
                pawnLeftAttackMask &= ~BitBoardConstants.EIGHT_RANK;
                pawnRightAttackMask &= ~BitBoardConstants.EIGHT_RANK;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns &= LeftPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 7), pos, MoveEncodingCodes.PROMOTION_KNIGHT_CAPTURE);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 7), pos, MoveEncodingCodes.PROMOTION_BISHOP_CAPTURE);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 7), pos, MoveEncodingCodes.PROMOTION_ROOK_CAPTURE);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 7), pos, MoveEncodingCodes.PROMOTION_QUEEN_CAPTURE);

                    MoveCount += 4;
                }

                while (RightPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(RightPromotingPawns);
                    RightPromotingPawns &= RightPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 9), pos, MoveEncodingCodes.PROMOTION_KNIGHT_CAPTURE);
                    moveList[MoveCount + 1] = new Move((ushort)(pos - 9), pos, MoveEncodingCodes.PROMOTION_BISHOP_CAPTURE);
                    moveList[MoveCount + 2] = new Move((ushort)(pos - 9), pos, MoveEncodingCodes.PROMOTION_ROOK_CAPTURE);
                    moveList[MoveCount + 3] = new Move((ushort)(pos - 9), pos, MoveEncodingCodes.PROMOTION_QUEEN_CAPTURE);

                    MoveCount += 4;
                }

                while (pawnLeftAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(pawnLeftAttackMask);
                    pawnLeftAttackMask &= pawnLeftAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 7), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
                while (pawnRightAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(pawnRightAttackMask);
                    pawnRightAttackMask &= pawnRightAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos - 9), pos, (ushort)MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
            } else
            {
                pawnLeftAttackMask &= ((pawnBitBoard & ~BitBoardConstants.A_FILE) >> 9) & ~SameColorBitBoard;
                pawnRightAttackMask &= ((pawnBitBoard & ~BitBoardConstants.H_FILE) >> 7) & ~SameColorBitBoard;

                ulong LeftPromotingPawns = BitBoardConstants.FIRST_RANK & pawnLeftAttackMask;
                ulong RightPromotingPawns = BitBoardConstants.FIRST_RANK & pawnRightAttackMask;
                pawnLeftAttackMask &= ~BitBoardConstants.FIRST_RANK;
                pawnRightAttackMask &= ~BitBoardConstants.FIRST_RANK;

                while (LeftPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(LeftPromotingPawns);
                    LeftPromotingPawns &= LeftPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 9), pos, MoveEncodingCodes.PROMOTION_KNIGHT_CAPTURE);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 9), pos, MoveEncodingCodes.PROMOTION_BISHOP_CAPTURE);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 9), pos, MoveEncodingCodes.PROMOTION_ROOK_CAPTURE);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 9), pos, MoveEncodingCodes.PROMOTION_QUEEN_CAPTURE);

                    MoveCount += 4;
                }

                while (RightPromotingPawns != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(RightPromotingPawns);
                    RightPromotingPawns &= RightPromotingPawns - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 7), pos, MoveEncodingCodes.PROMOTION_KNIGHT_CAPTURE);
                    moveList[MoveCount + 1] = new Move((ushort)(pos + 7), pos, MoveEncodingCodes.PROMOTION_BISHOP_CAPTURE);
                    moveList[MoveCount + 2] = new Move((ushort)(pos + 7), pos, MoveEncodingCodes.PROMOTION_ROOK_CAPTURE);
                    moveList[MoveCount + 3] = new Move((ushort)(pos + 7), pos, MoveEncodingCodes.PROMOTION_QUEEN_CAPTURE);

                    MoveCount += 4;
                }

                while (pawnLeftAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(pawnLeftAttackMask);
                    pawnLeftAttackMask &= pawnLeftAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 9), pos, MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
                while (pawnRightAttackMask != 0)
                {
                    ushort pos = (ushort)BitOperations.TrailingZeroCount(pawnRightAttackMask);
                    pawnRightAttackMask &= pawnRightAttackMask - 1;

                    moveList[MoveCount] = new Move((ushort)(pos + 7), pos, MoveEncodingCodes.CAPTURE);
                    MoveCount++;
                }
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
        //checks if EP move is possible for pins
        //assumes the piece is a pawn


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

            ulong moveMask = ulong.MaxValue;

            GeneratePsuedoCaptureKingMoves(KingPos, moveList);
            if (GenerateOnlyCaptures == false)
            {
                GeneratePsuedoQuietKingMoves(KingPos, moveList);
            }

            if (BoardPosition.CheckCount > 1) { 
                return MoveCount; 
            } else if (BoardPosition.CheckCount == 1)
            {
                moveMask = BoardPosition.InCheckValidSquares;
            }
            GeneratePsuedoCapturePawnMoves(moveList, moveMask);
            if (GenerateOnlyCaptures == false)
            {
                GeneratePsuedoQuitePawnMoves(moveList, moveMask);
            }
            
            ulong knightMask = BoardPosition.BitBoards[ActiveColor + Piece.KNIGHT] & ~BoardPosition.PinnedPiecesBitBoard[ActiveColor]; // pinned knights cannot move
            while (knightMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(knightMask);
                knightMask &= knightMask - 1;

                GeneratePsuedoCaptureKnightMoves(pos, moveList, moveMask);

                if (GenerateOnlyCaptures == false)
                {
                    GeneratePsuedoQuietKnightMoves(pos, moveList, moveMask);
                }
            }

            ulong bishopQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.BISHOP] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            while (bishopQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(bishopQueenMask);
                bishopQueenMask &= bishopQueenMask - 1;

                GeneratePsuedoBishopMoves(pos, moveList, moveMask, GenerateOnlyCaptures);
            }
            ulong rookQueenMask = BoardPosition.BitBoards[ActiveColor + Piece.ROOK] | BoardPosition.BitBoards[ActiveColor + Piece.QUEEN];
            while (rookQueenMask != 0)
            {
                ushort pos = (ushort)BitOperations.TrailingZeroCount(rookQueenMask);
                rookQueenMask &= rookQueenMask - 1;
                GeneratePsuedoRookMoves(pos, moveList, moveMask, GenerateOnlyCaptures);
            }
            return MoveCount;
        }

        public static int GetTotalMoveCombinationCount(Position boardPosition, int depth)
        {
            int count = 0;
            if (depth == 1)
            {
                Span<Move> moveList = stackalloc Move[218];
                byte MaxMoves = GenerateAllPseudoLegalMoves(boardPosition, moveList);
                for (int i = 0; i < MaxMoves; i++)
                {
                    if (boardPosition.IsLegal(moveList[i]))
                    {
                        count++;
                    }
                }
                return count;
            }

            Span<Move> moves = stackalloc Move[218];
            byte moveCount = GenerateAllPseudoLegalMoves(boardPosition, moves);

            for (int i = 0; i < moveCount; i++)
            {
                Move move = moves[i];

                if (boardPosition.IsLegal(move) == false)
                {
                    continue;
                }
                UnMove unmove = BoardPosition.MakeMove(move);
                count += GetTotalMoveCombinationCount(boardPosition, depth - 1);
                BoardPosition.UnMakeMove(unmove);
            }

            return count;
        }
        public static int GetTotalMoveCombinationCountTest(Position boardPosition, int depth)
        {
            int count = 0;
            if (depth == 1)
            {
                Span<Move> moveList = stackalloc Move[218];
                byte MaxMoves = GenerateAllPseudoLegalMoves(boardPosition, moveList);
                for (int i = 0; i < MaxMoves; i++)
                {
                    if (boardPosition.IsLegal(moveList[i]))
                    {
                        count++;
                        Console.WriteLine(BoardHelper.NumberToBoardLetter(moveList[i].StartPos) + BoardHelper.NumberToBoardLetter(moveList[i].EndPos));
                    }
                }
                return count;
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
                int e = GetTotalMoveCombinationCountTest(boardPosition, depth - 1);
                count += e;
                BoardPosition.UnMakeMove(unmove);
            }

            return count;
        }
    }
}
