using TOML;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Numerics;

public static class ZobristHashing
{
    // 0 - 63: white pawns
    // 64 - 127: black pawns
    // 128 - 191: white knight
    // 192 - 255: black knight
    // 256 - 319: white bishop
    // 320 - 383: black bishop
    // 384 - 447: white rook
    // 448 - 511: black rook
    // 512 - 575: white queen
    // 576 - 639: black queen
    // 640 - 703: white king
    // 704 - 767: black king
    // 768: side to move is black
    // 769 - 772: castling in order KQkq
    // 773 - 780: en passant file
    const int RandomNumArraySize = 781;

    static ulong[] PseudoRandomNumbers = new ulong[RandomNumArraySize];

    public static void GeneratePseudoRandomNumbers()
    {
        for (int i = 0; i < RandomNumArraySize; i++) {
            byte[] buffer = new byte[8];
            System.Random rnd = new System.Random();
            rnd.NextBytes(buffer);
            PseudoRandomNumbers[i] =  BitConverter.ToUInt64(buffer, 0);
        }
    }
    public static ulong MovePiece(ulong key, int startPos, int endPos, int piece)
    {
        int startPosIndex = (128 * ((piece & 7) - 1)) + (((piece >> 3) & 1) * 64) + startPos;
        int endPosIndex = (128 * ((piece & 7) - 1)) + (((piece >> 3) & 1) * 64) + endPos;

        key ^= PseudoRandomNumbers[startPosIndex];
        key ^= PseudoRandomNumbers[endPosIndex];

        return key;
    }
    public static ulong AddPiece(ulong key, int pos, int piece)
    {
        int posIndex = (128 * ((piece & 7) - 1)) + (((piece >> 3) & 1) * 64) + pos;
        key ^= PseudoRandomNumbers[posIndex];

        return key;
    }
    public static ulong RemovePiece(ulong key, int pos, int piece)
    {
        int posIndex = (128 * ((piece & 7) - 1)) + (((piece >> 3) & 1) * 64) + pos;
        
        key ^= PseudoRandomNumbers[posIndex];
        return key;
    }
    //castling index is the actual value
    public static ulong ChangeCastling(ulong key, int castlingValue)
    {
        if ((castlingValue & 1) == 1)
        {
            key ^= PseudoRandomNumbers[769];
        } 
        if ((castlingValue & 2) == 2)
        {
            key ^= PseudoRandomNumbers[770];
        } 
        if ((castlingValue & 4) == 4)
        {
            key ^= PseudoRandomNumbers[771];
        } 
        if ((castlingValue & 8) == 8)
        {
            key ^= PseudoRandomNumbers[772];
        }

        return key;
    }
    public static ulong ChangeEnpassant(ulong key, int enpassantPos)
    {
        int File = enpassantPos % 8;
        key ^= PseudoRandomNumbers[773 + File];

        return key;
    }
    public static ulong UpdateSideToMove(ulong key)
    {
        key ^= PseudoRandomNumbers[768];
        return key;
    }
    public static ulong GetZobristKey(Position position)
    {
        ulong key = 0;

        ulong PawnMask = position.BitBoards[Piece.PAWN + Piece.WHITE];
        while (PawnMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(PawnMask);
            PawnMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos];
        }
        PawnMask = position.BitBoards[Piece.PAWN + Piece.BLACK];
        while (PawnMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(PawnMask);
            PawnMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 64];
        }

        ulong KnightMask = position.BitBoards[Piece.PAWN + Piece.WHITE];
        while (KnightMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(KnightMask);
            KnightMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 128];
        }
        KnightMask = position.BitBoards[Piece.PAWN + Piece.BLACK];
        while (KnightMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(KnightMask);
            KnightMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 192];
        }

        ulong BishopMask = position.BitBoards[Piece.PAWN + Piece.WHITE];
        while (BishopMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(BishopMask);
            BishopMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 256];
        }
        BishopMask = position.BitBoards[Piece.PAWN + Piece.BLACK];
        while (BishopMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(BishopMask);
            BishopMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 320];
        }

        ulong RookMask = position.BitBoards[Piece.PAWN + Piece.WHITE];
        while (BishopMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(RookMask);
            RookMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 384];
        }
        RookMask = position.BitBoards[Piece.PAWN + Piece.BLACK];
        while (RookMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(RookMask);
            RookMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 448];
        }

        ulong QueenMask = position.BitBoards[Piece.PAWN + Piece.WHITE];
        while (BishopMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(QueenMask);
            QueenMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 512];
        }
        QueenMask = position.BitBoards[Piece.PAWN + Piece.BLACK];
        while (QueenMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(QueenMask);
            QueenMask -= 1ul << pos;

            key ^= PseudoRandomNumbers[pos + 576];
        }
        key ^= PseudoRandomNumbers[BitOperations.TrailingZeroCount(position.BitBoards[Piece.KING + Piece.WHITE]) + 640];
        key ^= PseudoRandomNumbers[BitOperations.TrailingZeroCount(position.BitBoards[Piece.KING + Piece.WHITE]) + 704];
        if (position.ActiveColor == (ushort)PieceType.BLACK)
        {
            key ^= PseudoRandomNumbers[768];
        }
        if ((position.CastlingAvailability & 1) == 1)
        {
            key ^= PseudoRandomNumbers[769];
        }
        if ((position.CastlingAvailability & 2) == 2)
        {
            key ^= PseudoRandomNumbers[770];
        }
        if ((position.CastlingAvailability & 4) == 4)
        {
            key ^= PseudoRandomNumbers[771];
        }
        if ((position.CastlingAvailability & 8) == 8)
        {
            key ^= PseudoRandomNumbers[772];
        }
        if (position.EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
        {
            key ^= PseudoRandomNumbers[773 +  position.EnPassantTargetSquare % 8];
        }
        return key;
    }
}
