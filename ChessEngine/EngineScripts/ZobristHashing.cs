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
    // 769 - 784: castling 
    // 785 - 792: en passant file
    const int RandomNumArraySize = 793;

    static ulong[] PseudoRandomNumbers = new ulong[RandomNumArraySize];

    public static void GeneratePseudoRandomNumbers()
    {
        System.Random rnd = new System.Random();
        for (int i = 0; i < RandomNumArraySize; i++) {
            byte[] buffer = new byte[8];
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
    public static ulong ChangeCastling(ulong key, int castlingValue)
    {
        key ^= PseudoRandomNumbers[769 + castlingValue];

        return key;
    }
    public static ulong ChangeEnpassant(ulong key, int enpassantPos)
    {
        int File = enpassantPos >> 3;
        key ^= PseudoRandomNumbers[785 + File];

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
            PawnMask &= PawnMask - 1;

            key ^= PseudoRandomNumbers[pos];
        }
        PawnMask = position.BitBoards[Piece.PAWN + Piece.BLACK];
        while (PawnMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(PawnMask);
            PawnMask &= PawnMask - 1;

            key ^= PseudoRandomNumbers[pos + 64];
        }

        ulong KnightMask = position.BitBoards[Piece.KNIGHT + Piece.WHITE];
        while (KnightMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(KnightMask);
            KnightMask &= KnightMask - 1;

            key ^= PseudoRandomNumbers[pos + 128];
        }
        KnightMask = position.BitBoards[Piece.KNIGHT + Piece.BLACK];
        while (KnightMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(KnightMask);
            KnightMask &= KnightMask - 1;

            key ^= PseudoRandomNumbers[pos + 192];
        }

        ulong BishopMask = position.BitBoards[Piece.BISHOP + Piece.WHITE];
        while (BishopMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(BishopMask);
            BishopMask &= BishopMask - 1;

            key ^= PseudoRandomNumbers[pos + 256];
        }
        BishopMask = position.BitBoards[Piece.BISHOP + Piece.BLACK];
        while (BishopMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(BishopMask);
            BishopMask &= BishopMask - 1;

            key ^= PseudoRandomNumbers[pos + 320];
        }

        ulong RookMask = position.BitBoards[Piece.ROOK + Piece.WHITE];
        while (RookMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(RookMask);
            RookMask &= RookMask - 1;

            key ^= PseudoRandomNumbers[pos + 384];
        }
        RookMask = position.BitBoards[Piece.ROOK + Piece.BLACK];
        while (RookMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(RookMask);
            RookMask &= RookMask - 1;

            key ^= PseudoRandomNumbers[pos + 448];
        }

        ulong QueenMask = position.BitBoards[Piece.QUEEN + Piece.WHITE];
        while (QueenMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(QueenMask);
            QueenMask &= QueenMask - 1;

            key ^= PseudoRandomNumbers[pos + 512];
        }
        QueenMask = position.BitBoards[Piece.QUEEN + Piece.BLACK];
        while (QueenMask != 0)
        {
            int pos = BitOperations.TrailingZeroCount(QueenMask);
            QueenMask &= QueenMask - 1;

            key ^= PseudoRandomNumbers[pos + 576];
        }
        key ^= PseudoRandomNumbers[BitOperations.TrailingZeroCount(position.BitBoards[Piece.KING + Piece.WHITE]) + 640];
        key ^= PseudoRandomNumbers[BitOperations.TrailingZeroCount(position.BitBoards[Piece.KING + Piece.BLACK]) + 704];
        if (position.ActiveColor == (ushort)PieceType.BLACK)
        {
            key ^= PseudoRandomNumbers[768];
        }
        key ^= PseudoRandomNumbers[769 + position.CastlingAvailability];
        if (position.EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
        {
            key ^= PseudoRandomNumbers[785 + position.EnPassantTargetSquare % 8];
        }
        return key;
    }
}
