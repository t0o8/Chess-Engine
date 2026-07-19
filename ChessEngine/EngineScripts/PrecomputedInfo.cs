using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net;

namespace TOML
{
    public static class PrecomputedInfo
    {
        public readonly static short[] DirectionOffset = new short[8] { -8, 8, -1, 1, -9, 9, 7, -7 };
        public readonly static short[] BaseKnightMoves = new short[8] { -17, -15, -10, -6, 6, 10, 15, 17 };
        public readonly static int[][] BaseKnightMoveXY = new int[8][] { new int[2] { -1, -2 }, new int[2] { 1, -2 }, new int[2] { -2, -1 }, new int[2] { 2, -1 }, new int[2] { -2, 1 }, new int[2] { 2, 1 }, new int[2] { -1, 2 }, new int[2] { 1, 2 } };
        public readonly static int[][] BaseKingMoveXY = new int[8][] { new int[2] { 0, -1 }, new int[2] { 0, 1 }, new int[2] { -1, 0 }, new int[2] { 1, 0 }, new int[2] { -1, -1 }, new int[2] { 1, 1 }, new int[2] { -1, 1 }, new int[2] { 1, -1 } };
        public readonly static short[][] NumSquaresToEdge = new short[64][];
        public readonly static short[][] PossibleKnightMoves = new short[64][];
        public readonly static short[][] PossibleKingMoves = new short[64][];

        public readonly static ulong[] BishopMoves = new ulong[64];
        public readonly static ulong[] RookMoves = new ulong[64];
        public readonly static ulong[] QueenMoves = new ulong[64];

        public readonly static ulong[] BishopBlockingMasks = new ulong[64];
        public readonly static ulong[] RookBlockingMasks = new ulong[64];
        public readonly static ulong[] QueenBlockingMasks = new ulong[64];

        public readonly static ulong[] BishopMovesCount = new ulong[64];
        public readonly static ulong[] RookMovesCount = new ulong[64];
        public readonly static ulong[] QueenMovesCount = new ulong[64];

        public readonly static int[] QueenBlockingMaskCount = new int[64];
        public readonly static int[] BishopBlockingMaskCount = new int[64];
        public readonly static int[] RookBlockingMaskCount = new int[64];

        public readonly static int[] BishopMagicShift = new int[64];
        public readonly static int[] RookMagicShift = new int[64];

        public readonly static ulong[][] BishopMovesBitBoard = new ulong[64][];
        public readonly static ulong[][] RookMovesBitBoard = new ulong[64][];

        public readonly static ulong[] KnightMovesBitBoard = new ulong[64];
        public readonly static ulong[] KingMovesBitBoard = new ulong[64];

        public readonly static ulong[][] PawnAttacksBitBoard = new ulong[64][];

        public readonly static ulong[][] InBetweenBitBoard = new ulong[64][];
        public readonly static ulong[][] LineBitBoard = new ulong[64][];

        public readonly static ulong[][] PassedPawnMask = new ulong[64][];

        public static void PrecomputeInfo()
        {
            for (int i = 0; i < 64; i++)
            {
                int XPos = i & 7;
                int YPos = i >> 3;

                int Left = XPos;
                int Right = 7 - XPos;
                int Down = YPos;
                int Up = 7 - YPos;

                NumSquaresToEdge[i] = new short[8]
                {
                    (short)Down,
                    (short)Up,
                    (short)Left,
                    (short)Right,
                    (short)Math.Min(Down, Left),
                    (short)Math.Min(Up, Right),
                    (short)Math.Min(Up, Left),
                    (short)Math.Min(Down, Right),
                };
                //precompute bitboards

                //pawns
                PawnAttacksBitBoard[i] = new ulong[9];
                if (XPos > 0)
                {
                    //right
                    if (YPos < 7)
                    {
                        PawnAttacksBitBoard[i][0] |= 1ul << i + 7;
                    }
                    if (YPos > 0)
                    {
                        PawnAttacksBitBoard[i][8] |= 1ul << i - 9;
                    }
                }
                if (XPos < 7)
                {
                    //left
                    if (YPos < 7)
                    {
                        PawnAttacksBitBoard[i][0] |= 1ul << i + 9;
                    }
                    if (YPos > 0)
                    {
                        PawnAttacksBitBoard[i][8] |= 1ul << i - 7;
                    }
                }

                InBetweenBitBoard[i] = new ulong[64];
                LineBitBoard[i] = new ulong[64];
                for (int DirectionIndex = 0; DirectionIndex < 8; DirectionIndex++)
                {
                    ulong InBetweenMoves = 0;
                    ulong Total = 0;
                    for (ushort Step = 1; Step < NumSquaresToEdge[i][DirectionIndex] + 1; Step++)
                    {
                        int Move = (DirectionOffset[DirectionIndex] * Step);
                        int Endpos = (i + Move);

                        QueenMoves[i] += (1UL << Endpos);
                        QueenMovesCount[i]++;

                        if (Step != NumSquaresToEdge[i][DirectionIndex])
                        {
                            QueenBlockingMasks[i] |= 1UL << Endpos;
                            QueenBlockingMaskCount[i]++;
                        }
                        InBetweenBitBoard[i][Endpos] = InBetweenMoves;
                        InBetweenMoves |= 1ul << Endpos;
                        Total |= 1ul << Endpos;
                    }
                    for (ushort Step = 0; Step < NumSquaresToEdge[i][DirectionIndex ^ 1] + 1; Step++)
                    {
                        int Move = (DirectionOffset[DirectionIndex ^ 1] * Step);
                        int Endpos = (i + Move);
                        Total |= 1ul << Endpos;
                    }
                    for (ushort Step = 1; Step < NumSquaresToEdge[i][DirectionIndex] + 1; Step++)
                    {
                        int Move = (DirectionOffset[DirectionIndex] * Step);
                        int Endpos = (i + Move);
                        LineBitBoard[i][Endpos] = Total;
                    }
                }
                for (int DirectionIndex = 4; DirectionIndex < 8; DirectionIndex++)
                {
                    for (ushort Step = 1; Step < NumSquaresToEdge[i][DirectionIndex] + 1; Step++)
                    {
                        short Move = (short)(DirectionOffset[DirectionIndex] * Step);
                        ushort Endpos = (ushort)(i + Move);

                        BishopMoves[i] += (1ul << Endpos);
                        BishopMovesCount[i]++;

                        if (Step != NumSquaresToEdge[i][DirectionIndex])
                        {
                            BishopBlockingMasks[i] += (1ul << Endpos);
                            BishopBlockingMaskCount[i]++;
                        }
                    }
                }
                BishopMagicShift[i] = 64 - BishopBlockingMaskCount[i];
                BishopMovesBitBoard[i] = new ulong[MagicNumberGenerator.GetBlockerMaskSize(BishopBlockingMasks[i])];
                for (ulong BlockerIndex = 0; BlockerIndex < MagicNumberGenerator.GetBlockerMaskSize(BishopBlockingMasks[i]); BlockerIndex++)
                {
                    ulong BlockerMask = MagicNumberGenerator.GetBlockerMask(BishopBlockingMasks[i], BlockerIndex);
                    ulong MoveMask = 0;
                    for (int DirectionIndex = 4; DirectionIndex < 8; DirectionIndex++)
                    {
                        for (ushort Step = 1; Step < NumSquaresToEdge[i][DirectionIndex] + 1; Step++)
                        {
                            int Move = (DirectionOffset[DirectionIndex] * Step);
                            int Endpos = (i + Move);

                            ulong isPiece = (BlockerMask >> Endpos) & 1ul;

                            MoveMask += (1ul << Endpos);

                            if (isPiece == 1)
                            {
                                break;
                            }
                        }
                    }
                    BishopMovesBitBoard[i][MagicNumberGenerator.GetBlockerMask(BishopBlockingMasks[i], BlockerIndex) * MagicNumberGenerator.BishopMagicNumbers[i] >> (64 - BishopBlockingMaskCount[i])] = MoveMask;
                }
                for (int DirectionIndex = 0; DirectionIndex < 4; DirectionIndex++)
                {
                    for (ushort Step = 1; Step < NumSquaresToEdge[i][DirectionIndex] + 1; Step++)
                    {
                        short Move = (short)(DirectionOffset[DirectionIndex] * Step);
                        ushort Endpos = (ushort)(i + Move);

                        RookMoves[i] += (1ul << Endpos);
                        RookMovesCount[i]++;

                        if (Step != NumSquaresToEdge[i][DirectionIndex])
                        {
                            RookBlockingMasks[i] += (1ul << Endpos);
                            RookBlockingMaskCount[i]++;

                        }
                    }
                }
                RookMagicShift[i] = 64 - RookBlockingMaskCount[i];
                RookMovesBitBoard[i] = new ulong[MagicNumberGenerator.GetBlockerMaskSize(RookBlockingMasks[i])];
                for (ulong BlockerIndex = 0; BlockerIndex < MagicNumberGenerator.GetBlockerMaskSize(RookBlockingMasks[i]); BlockerIndex++)
                {
                    ulong BlockerMask = MagicNumberGenerator.GetBlockerMask(RookBlockingMasks[i], BlockerIndex);
                    ulong MoveMask = 0;
                    for (int DirectionIndex = 0; DirectionIndex < 4; DirectionIndex++)
                    {
                        for (ushort Step = 1; Step < NumSquaresToEdge[i][DirectionIndex] + 1; Step++)
                        {
                            int Move = (DirectionOffset[DirectionIndex] * Step);
                            int Endpos = (i + Move);

                            ulong isPiece = (BlockerMask >> Endpos) & 1ul;

                            MoveMask += (1ul << Endpos);

                            if (isPiece == 1)
                            {
                                break;
                            }
                        }
                    }
                    RookMovesBitBoard[i][MagicNumberGenerator.GetBlockerMask(RookBlockingMasks[i], BlockerIndex) * MagicNumberGenerator.RookMagicNumbers[i] >> (64 - RookBlockingMaskCount[i])] = MoveMask;
                }
                //do knight stuff now
                List<short> KnightMoves = new List<short>();
                for (int MoveNum = 0; MoveNum < 8; MoveNum++)
                {
                    int XMove = BaseKnightMoveXY[MoveNum][0];
                    int YMove = BaseKnightMoveXY[MoveNum][1];

                    int NewXPos = XPos + XMove;
                    int NewYPos = YPos + YMove;

                    if (NewXPos < 0 || NewXPos > 7) { continue; }
                    if (NewYPos < 0 || NewYPos > 7) { continue; }

                    KnightMoves.Add(BaseKnightMoves[MoveNum]);

                    KnightMovesBitBoard[i] += 1ul << (BaseKnightMoves[MoveNum] + i);
                }
                PossibleKnightMoves[i] = KnightMoves.ToArray();

                //do king stuff now
                List<short> KingMoves = new List<short>();
                for (int MoveNum = 0; MoveNum < 8; MoveNum++)
                {
                    int XMove = BaseKingMoveXY[MoveNum][0];
                    int YMove = BaseKingMoveXY[MoveNum][1];

                    int NewXPos = XPos + XMove;
                    int NewYPos = YPos + YMove;

                    if (NewXPos < 0 || NewXPos > 7) { continue; }
                    if (NewYPos < 0 || NewYPos > 7) { continue; }

                    KingMoves.Add(DirectionOffset[MoveNum]);

                    KingMovesBitBoard[i] += 1ul << (DirectionOffset[MoveNum] + i);
                }
                PossibleKingMoves[i] = KingMoves.ToArray();

                //do pawn stuff now

                ushort File = 0;
                Position.GetXYPosition((ushort)i, out File, out ushort Y);
                ulong Mask = 0;
                if (File != 0)
                {
                    Mask |= BitBoardConstants.A_FILE << (File - 1);
                }
                if (File != 7)
                {
                    Mask |= BitBoardConstants.A_FILE << (File + 1);
                }

                Mask |= BitBoardConstants.A_FILE << File;
                ulong WhiteMask = Mask & ulong.MaxValue << ((Y + 1) * 8);
                ulong BlackMask = Mask & ulong.MaxValue >> (8 - (Y + 1) * 8);
                PassedPawnMask[i] = new ulong[9];
                PassedPawnMask[i][Piece.WHITE] = WhiteMask;
                PassedPawnMask[i][Piece.BLACK] = BlackMask;

            }
            Console.WriteLine("precompute info done");
        }
    }
}

