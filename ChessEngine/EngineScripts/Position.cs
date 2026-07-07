using System.Collections.Generic;
using System;
using System.Linq;
using System.Numerics;
using System.Drawing;


namespace TOML
{
    public class Position
    {
        public ushort[] UshortBoard;
        public ushort HalfMoveClock;
        public ushort FullMoveNumber;
        public ushort EnPassantTargetSquare = PositionConstants.NoEnPassantMoves;
        public ushort CastlingAvailability; // positions from left to right 1,2,3,4 corresponds to kqKQ castling
        public ushort ActiveColor;

        //information about the position, pregenerated when position is created or updated
        
        public ulong ZobristHash;

        public ulong WhiteAttackingSquares;
        public ulong BlackAttackingSquares;

        public ulong[] AttackingSquaresBitBoard = new ulong[15];

        public ulong[] PinnedPiecesBitBoard = new ulong[9];

        public ulong InCheckValidSquares = ulong.MaxValue;
        public ulong GivingCheckBitBoard = 0;
        public int CheckCount = 0;

        public ulong[] BitBoards = new ulong[15];
        public ulong AllPieceBitBoard = 0;
        
        public Position(string fenString)
        {
            ushort steps = 0;
            ushort row = 7;
            ushort column = 0;

            bool recordedEnpassant = false;
            UshortBoard = new ushort[64];

            for (int StringPosition = 0; StringPosition < fenString.Length; StringPosition++)
            {

                char fenCharacter = fenString[StringPosition];
                ushort Position = GetBoardPosition(column, row);
                //Debug.Log(Position);
                if (fenCharacter == ' ')
                {
                    //finished a field move on to next one
                    steps++;
                    continue;
                }
                if (steps == 0)
                {
                    if (fenCharacter == '/')
                    {
                        row--; //change row
                        column = 0;
                        continue;

                    }
                    if (char.IsDigit(fenCharacter))
                    {
                        for (int num = 0; num < Convert.ToInt16(fenCharacter) - 48; num++)
                        {
                            UshortBoard[Position] = (int)PieceType.EMPTY;
                            column++;
                        }
                        continue;
                    }
                    int PieceColor = GetPieceColorInInt(fenCharacter);
                    int Piece = Conversions.FenToPieceType[char.ToLower(fenCharacter)];

                    ushort PieceValue = (ushort)(Piece + PieceColor);
                    UshortBoard[Position] = PieceValue;
                    BitBoards[PieceValue] += 1ul << Position;
                    column++;

                }
                if (steps == 1)
                {
                    //move color
                    if (fenCharacter == 'w') { ActiveColor = (int)PieceType.WHITE; } else { ActiveColor = (int)PieceType.BLACK; }
                }
                if (steps == 2)
                {
                    //castling availability
                    if (fenCharacter != ' ')
                    {
                        switch (fenCharacter)
                        {
                            case 'K': CastlingAvailability += 1; continue;
                            case 'Q': CastlingAvailability += 2; continue;
                            case 'k': CastlingAvailability += 4; continue;
                            case 'q': CastlingAvailability += 8; continue;
                            case '-': continue;
                            default: Console.WriteLine("not a valid castling availability letter"); continue;
                        }
                    }
                }
                if (steps == 3)
                {
                    if (fenCharacter == '-')
                    {
                        continue;
                    }
                    //enpassanttargetsqaure

                    if (recordedEnpassant == true) { continue; }
                    ushort file = Conversions.LetterToNumber[fenCharacter];
                    ushort rank = (ushort)(fenString[StringPosition + 1] - 49);
                    EnPassantTargetSquare = GetBoardPosition(file, rank);
                    recordedEnpassant = true;
                }
                if (steps == 4)
                {
                    int OriginalStringPos = StringPosition;
                    while (true)
                    {
                        if (fenString[StringPosition] == ' ')
                        {
                            HalfMoveClock = ushort.Parse(fenString[OriginalStringPos..StringPosition]);
                            steps++;
                            break;
                        }
                        StringPosition++;

                    }
                }
                if (steps == 5)
                {
                    FullMoveNumber = (ushort)fenCharacter;
                }
            }
            this.ZobristHash = ZobristHashing.GetZobristKey(this);
            UpdatePositionStats();
        }
        
        public Position(Position referencePosition)
        {
            this.UshortBoard = referencePosition.UshortBoard;
            this.HalfMoveClock = referencePosition.HalfMoveClock;
            this.FullMoveNumber = referencePosition.FullMoveNumber;
            this.EnPassantTargetSquare = referencePosition.EnPassantTargetSquare;
            this.CastlingAvailability = referencePosition.CastlingAvailability;
            this.ActiveColor = referencePosition.ActiveColor;

            this.ZobristHash = referencePosition.ZobristHash;
            referencePosition.BitBoards.CopyTo(this.BitBoards, 0);

        }

        //switches the active color on the board
        public void SwitchActiveColor()
        {

            switch (ActiveColor)
            {
                case (int)PieceType.WHITE: { ActiveColor = (int)PieceType.BLACK; return; }
                case (int)PieceType.BLACK: { ActiveColor = (int)PieceType.WHITE; return; }
                default: { return; }
            }
        }

        //plays a move on the board
        public UnMove MakeMove(Move move)
        {
            ushort StartingPosition = move.startPos;
            ushort EndingPosition = move.endPos;
            ushort piece = UshortBoard[StartingPosition];
            ushort PieceWithoutColor = GetPieceWithoutColor(piece);
            ushort Color = GetColor(piece);

            //remove the EnPassant square from zobrist hash if necessary
            if (EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
            {
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);
            }
            //update player to move in zobrist hash
            ZobristHash = ZobristHashing.UpdateSideToMove(ZobristHash);
            uint UnMoveInfo = 0;
            UnMoveInfo += (ushort)(EnPassantTargetSquare << 8);
            UnMoveInfo += CastlingAvailability;
            UnMoveInfo += (ushort)(HalfMoveClock << 15);
            UnMoveInfo += (ushort)(FullMoveNumber << 22);
            EnPassantTargetSquare = PositionConstants.NoEnPassantMoves;

            if (EndingPosition == 0 && ((CastlingAvailability >> 1) & 1) == 1) { CastlingAvailability -= 2; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 2); }
            if (EndingPosition == 7 && (CastlingAvailability & 1) == 1) { CastlingAvailability -= 1; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 1); }
            if (EndingPosition == 56 && ((CastlingAvailability >> 3) & 1) == 1) { CastlingAvailability -= 8; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 8); }
            if (EndingPosition == 63 && ((CastlingAvailability >> 2) & 1) == 1) { CastlingAvailability -= 4; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 4); }


            HalfMoveClock++;
            if (Color == Piece.BLACK)
            {
                FullMoveNumber++;
            }
            if (PieceWithoutColor == Piece.PAWN)
            {
                HalfMoveClock = 0; 
            }
            if (move.moveInfo == 1)
            {
                //double pawn push
                EnPassantTargetSquare = (ushort)((StartingPosition + EndingPosition) * 0.5);
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);

            } else if (move.moveInfo >> 3 == 1)
            {
                //pawn at end! handle promotion

                ushort PromotedPiece = (ushort)((move.moveInfo & 3) + 2 + ActiveColor);

                if ((move.moveInfo >> 2) == 3)
                {
                    //capture, must remove captured piece
                    UnMoveInfo += (ushort)(UshortBoard[EndingPosition] << 4);
                    RemovePiece(EndingPosition, UshortBoard[EndingPosition]);
                }
                RemovePiece(StartingPosition, UshortBoard[StartingPosition]);

                AddPiece(EndingPosition, PromotedPiece);

                SwitchActiveColor();
                return new UnMove(move, UnMoveInfo);
            } else if (move.moveInfo == 4)
            {
                // capture
                HalfMoveClock = 0;
                
                UnMoveInfo += (ushort)(UshortBoard[EndingPosition] << 4);
                RemovePiece(EndingPosition, UshortBoard[EndingPosition]);

            } else if (move.moveInfo == 5)
            {
                //ep capture!
                HalfMoveClock = 0;

                ushort CaptureSquare;

                if (move.endPos > 39)
                {
                    CaptureSquare = (ushort)(EndingPosition - 8);

                } else
                {
                    CaptureSquare = (ushort)(EndingPosition + 8);
                }

                UnMoveInfo += (ushort)(UshortBoard[CaptureSquare] << 4);
                RemovePiece(CaptureSquare, UshortBoard[CaptureSquare]);
            }
            if (PieceWithoutColor == (ushort)PieceType.KING)
            {
                if (Color == (ushort)PieceType.WHITE)
                {
                    ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, (CastlingAvailability & 3));
                    CastlingAvailability = (ushort)(CastlingAvailability & 12);

                    if (move.moveInfo == (ushort)MoveEncodingCodes.KINGCASTLE)
                    {
                        MovePiece(7, 5, (ushort)PieceType.WHITE + (ushort)PieceType.ROOK);
                    } else if (move.moveInfo == (int)MoveEncodingCodes.QUEENCASTLE)
                    {
                        MovePiece(0, 3, (ushort)PieceType.WHITE + (ushort)PieceType.ROOK);
                    }
                } else
                {
                    ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, (CastlingAvailability & 12));
                    CastlingAvailability = (ushort)(CastlingAvailability & 3);
                    if (move.moveInfo == (ushort)MoveEncodingCodes.KINGCASTLE)
                    {
                        MovePiece(63, 61, (ushort)PieceType.BLACK + (ushort)PieceType.ROOK);
                    } else if (move.moveInfo == (int)MoveEncodingCodes.QUEENCASTLE)
                    {
                        MovePiece(56, 59, (ushort)PieceType.BLACK + (ushort)PieceType.ROOK);
                    }
                }
            } else if (PieceWithoutColor == (int)PieceType.ROOK)
            {
                if (StartingPosition == 0 && ((CastlingAvailability >> 1) & 1) == 1) { CastlingAvailability -= 2; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 2); }
                if (StartingPosition == 7 && (CastlingAvailability & 1) == 1) { CastlingAvailability -= 1; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 1); }
                if (StartingPosition == 56 && ((CastlingAvailability >> 3) & 1) == 1) { CastlingAvailability -= 8; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 8); }
                if (StartingPosition == 63 && ((CastlingAvailability >> 2) & 1) == 1) { CastlingAvailability -= 4; ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, 4); }
            }
            MovePiece(StartingPosition, EndingPosition, piece);

            SwitchActiveColor();
            return new UnMove(move, UnMoveInfo);
        }

        public void UnMakeMove(UnMove move)
        {
            //unmove info
            //first 4, previous castling availability 
            //next 4, captured piece
            //next 7, en passaent target square
            //next 7, half move count
            //next 8, move count


            //revert castling rights 
            ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, CastlingAvailability);
            CastlingAvailability = (ushort)(move.UnMoveInfo & 15);
            ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, CastlingAvailability);

            //revert ep square
            if (EnPassantTargetSquare != PositionConstants.NoEnPassantMoves) {
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);
            }
            EnPassantTargetSquare = (ushort)((move.UnMoveInfo >> 8) & 127);
            if (EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
            {
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);
            }

            //update side to move
            ZobristHash = ZobristHashing.UpdateSideToMove(ZobristHash);
            SwitchActiveColor();

            HalfMoveClock = (ushort)((move.UnMoveInfo >> 15) & 127);
            FullMoveNumber = (ushort)(move.UnMoveInfo >> 22);

            ushort piece = UshortBoard[move.endPos];
            ushort Color = GetColor(piece);
            //castling logic
            if (move.moveInfo == (int)MoveEncodingCodes.KINGCASTLE)
            {
                if (Color == (int)PieceType.WHITE)
                {
                    MovePiece(5, 7, (int)PieceType.WHITE + (int)PieceType.ROOK);

                } else
                {
                    MovePiece(61, 63, (int)PieceType.BLACK + (int)PieceType.ROOK);

                }
            } else if (move.moveInfo == (int)MoveEncodingCodes.QUEENCASTLE)
            {
                if (Color == (int)PieceType.WHITE)
                {
                    MovePiece(3, 0, (int)PieceType.WHITE + (int)PieceType.ROOK);
                } else
                {
                    MovePiece(59, 56, (int)PieceType.BLACK + (int)PieceType.ROOK);
                }
            }
            if (move.moveInfo >> 3 == 1)
            {
                //undoing promotions
                ushort PromotedPiece = UshortBoard[move.endPos];

                RemovePiece(move.endPos, PromotedPiece);

                AddPiece(move.startPos, (ushort)(Piece.PAWN + Color));

                if ((move.moveInfo >> 2) == 3)
                {
                    //captured piece, must return it
                    ushort CapturedPiece = (ushort)((move.UnMoveInfo >> 4) & 15);
                    AddPiece(move.endPos, CapturedPiece);
                } 
                return;
            } else if (move.moveInfo == (ushort)MoveEncodingCodes.EPCAPTURE)
            {
                ushort CapturedPiece = (ushort)((move.UnMoveInfo >> 4) & 15);
                MovePiece(move.endPos, move.startPos, piece);
                if (move.endPos > 39)
                {
                    AddPiece((ushort)(move.endPos - 8), CapturedPiece);

                } else
                {
                    AddPiece((ushort)(move.endPos + 8), CapturedPiece);
                }
            } else if (move.moveInfo == (ushort)MoveEncodingCodes.CAPTURE)
            {
                ushort CapturedPiece = (ushort)((move.UnMoveInfo >> 4) & 15);
                MovePiece(move.endPos, move.startPos, piece);
                AddPiece(move.endPos, CapturedPiece);
            }  else
            {
                MovePiece(move.endPos, move.startPos, piece);
            }
        }
        public void UpdatePositionStats()
        {
            int KingPos = BitOperations.TrailingZeroCount(BitBoards[ActiveColor + Piece.KING]);
            ushort OppositeColor = PieceColors.GetOppositeColor(ActiveColor);

            InCheckValidSquares = ulong.MaxValue;
            CheckCount = 0;

            //update other bitboards
            AllPieceBitBoard = BitBoards[1] | BitBoards[2] | BitBoards[3] | BitBoards[4] | BitBoards[5] | BitBoards[6] | BitBoards[9] | BitBoards[10] | BitBoards[11] | BitBoards[12] | BitBoards[13] | BitBoards[14];

            BitBoards[(ushort)PieceType.WHITE] = BitBoards[1] | BitBoards[2] | BitBoards[3] | BitBoards[4] | BitBoards[5] | BitBoards[6];
            BitBoards[(ushort)PieceType.BLACK] = BitBoards[1 + (ushort)PieceType.BLACK] | BitBoards[2 + (ushort)PieceType.BLACK] | BitBoards[3 + (ushort)PieceType.BLACK] | BitBoards[4 + (ushort)PieceType.BLACK] | BitBoards[5 + (ushort)PieceType.BLACK] | BitBoards[6 + (ushort)PieceType.BLACK];

            ulong SameSideBitboard = BitBoards[ActiveColor];
            ulong DifSideBitboard = BitBoards[OppositeColor];


            //clear the old attacking squares
            //add the new attacking squares
            AttackingSquaresBitBoard[OppositeColor] = 0;
            AttackingSquaresBitBoard[OppositeColor] |= PrecomputedInfo.KingMovesBitBoard[BitOperations.TrailingZeroCount(BitBoards[OppositeColor + Piece.KING])];

            ulong PawnLeftAttackMask = 0;
            ulong PawnRightAttackMask = 0;

            ulong PawnBitBoard = BitBoards[OppositeColor + Piece.PAWN];

            if (ActiveColor == Piece.WHITE)
            {
                PawnLeftAttackMask = ((PawnBitBoard & ~BitBoardConstants.A_FILE) >> 9);
                PawnRightAttackMask = ((PawnBitBoard & ~BitBoardConstants.F_FILE) >> 7);
            } else
            {
                PawnLeftAttackMask = ((PawnBitBoard & ~BitBoardConstants.A_FILE) << 7);
                PawnRightAttackMask = ((PawnBitBoard & ~BitBoardConstants.F_FILE) << 9);
            }

            AttackingSquaresBitBoard[OppositeColor] |= PawnLeftAttackMask;
            AttackingSquaresBitBoard[OppositeColor] |= PawnRightAttackMask;

            AttackingSquaresBitBoard[OppositeColor + Piece.PAWN] = PawnLeftAttackMask | PawnRightAttackMask;

            if (ActiveColor == Piece.WHITE)
            {
                ulong Left = ((((1ul << KingPos) & ~BitBoardConstants.A_FILE) << 7) & BitBoards[OppositeColor + Piece.PAWN]);
                ulong Right = ((((1ul << KingPos) & ~BitBoardConstants.F_FILE) << 9) & BitBoards[OppositeColor + Piece.PAWN]);

                if (Left != 0)
                {
                    CheckCount++;
                    InCheckValidSquares &= Left;
                }
                if (Right != 0)
                {
                    CheckCount++;
                    InCheckValidSquares &= Right;
                }
            } else
            {
                ulong Left = ((((1ul << KingPos) & ~BitBoardConstants.A_FILE) >> 9) & BitBoards[OppositeColor + Piece.PAWN]);
                ulong Right = ((((1ul << KingPos) & ~BitBoardConstants.F_FILE) >> 7) & BitBoards[OppositeColor + Piece.PAWN]);
                if (Left != 0)
                {
                    CheckCount++;
                    InCheckValidSquares &= Left;
                }
                if (Right != 0)
                {
                    CheckCount++;
                    InCheckValidSquares &= Right;
                }
            }
            ulong KnightMask = BitBoards[OppositeColor + Piece.KNIGHT];
            while (KnightMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(KnightMask);
                KnightMask -= 1ul << pos;

                AttackingSquaresBitBoard[OppositeColor] |= PrecomputedInfo.KnightMovesBitBoard[pos];
            }

            //compute the queen with rooks and bishops
            ulong BishopMask = BitBoards[OppositeColor + Piece.BISHOP] | BitBoards[OppositeColor + Piece.QUEEN];
            while (BishopMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(BishopMask);
                BishopMask -= 1ul << pos;

                AttackingSquaresBitBoard[OppositeColor] |= GetBishopAttacks(pos, AllPieceBitBoard & PrecomputedInfo.BishopBlockingMasks[pos] & ~BitBoards[ActiveColor + Piece.KING]);
            }
            ulong RookMask = BitBoards[OppositeColor + Piece.ROOK] | BitBoards[OppositeColor + Piece.QUEEN];
            while (RookMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(RookMask);
                RookMask -= 1ul << pos;

                AttackingSquaresBitBoard[OppositeColor] |= GetRookAttacks(pos, AllPieceBitBoard & PrecomputedInfo.RookBlockingMasks[pos] & ~BitBoards[ActiveColor + Piece.KING]);
            }

            PinnedPiecesBitBoard[ActiveColor] = 0;

            ulong Checkers = 0;
            Checkers |= (BitBoards[OppositeColor + Piece.BISHOP] | BitBoards[OppositeColor + Piece.QUEEN]) & GetBishopAttacks(KingPos, AllPieceBitBoard & PrecomputedInfo.BishopBlockingMasks[KingPos]);
            Checkers |= (BitBoards[OppositeColor + Piece.ROOK] | BitBoards[OppositeColor + Piece.QUEEN]) & GetRookAttacks(KingPos, AllPieceBitBoard & PrecomputedInfo.RookBlockingMasks[KingPos]);

            ulong PossiblePinners = 0;
            PossiblePinners |= (BitBoards[OppositeColor + Piece.BISHOP] | BitBoards[OppositeColor + Piece.QUEEN]) & PrecomputedInfo.BishopMoves[KingPos];
            PossiblePinners |= (BitBoards[OppositeColor + Piece.ROOK] | BitBoards[OppositeColor + Piece.QUEEN]) & PrecomputedInfo.RookMoves[KingPos];

            while (PossiblePinners != 0)
            {
                int PossiblePinner = BitOperations.TrailingZeroCount(PossiblePinners);
                PossiblePinners -= 1ul << PossiblePinner;

                ulong PossibleBlockers = PrecomputedInfo.InBetweenBitBoard[KingPos][PossiblePinner] & AllPieceBitBoard;
                int PossiblePinned = BitOperations.TrailingZeroCount(PossibleBlockers);

                if (BitOperations.PopCount(PossibleBlockers) == 1 && PieceColors.GetColor(UshortBoard[PossiblePinned]) == ActiveColor)
                {
                    PinnedPiecesBitBoard[ActiveColor] |= 1ul << PossiblePinned;
                }
            }

            if (BitOperations.PopCount(Checkers) >0 )
            {
                CheckCount+= BitOperations.PopCount(Checkers);
                InCheckValidSquares &= Checkers + PrecomputedInfo.InBetweenBitBoard[KingPos][BitOperations.TrailingZeroCount(Checkers)];
            }

            if ((PrecomputedInfo.KnightMovesBitBoard[KingPos] & BitBoards[OppositeColor + Piece.KNIGHT]) != 0)
            {
                CheckCount++;
                InCheckValidSquares &= PrecomputedInfo.KnightMovesBitBoard[KingPos] & BitBoards[OppositeColor + Piece.KNIGHT];
            }
        }
        public static ushort GetBoardPosition(ushort column, ushort row)
        {
            return (ushort)((row * 8) + column);
        }
        public static void GetXYPosition(ushort position, out ushort x, out ushort y)
        {
            x = (ushort)(position & 7);
            y = (ushort)(position >> 3);
        }
        public static ushort GetColor(ushort piece)
        {
            return (ushort)(piece & 8);

        }
        public static ushort GetPieceWithoutColor(int piece)
        {
            return (ushort)(piece & 7);
        }
        public static ushort GetPieceColorInInt(char piece)
        {
            if (char.IsLower(piece) == true)
            {
                return (ushort)PieceType.BLACK;
            } else
            {
                return (ushort)PieceType.WHITE;
            }
        }
        public static string NumberToBoardLetter(ushort num)
        {
            GetXYPosition(num, out ushort x, out ushort y);
            y += 1;
            switch (x)
            {
                case 0: return "a" + y;
                case 1: return "b" + y;
                case 2: return "c" + y;
                case 3: return "d" + y;
                case 4: return "e" + y;
                case 5: return "f" + y;
                case 6: return "g" + y;
                case 7: return "h" + y;
                default: return "null";
            }
        }

        public static ulong GetBishopAttacks(int pos, ulong blockers)
        { 
            ulong Index = (blockers * MagicNumbers.BishopMagicNumbers[pos]) >> (64 - PrecomputedInfo.BishopBlockingMaskCount[pos]);
            return PrecomputedInfo.BishopMovesBitBoard[pos][Index];
        }
        public static ulong GetRookAttacks(int pos, ulong blockers)
        {
            ulong Index = (blockers * MagicNumbers.RookMagicNumbers[pos]) >> (64 - PrecomputedInfo.RookBlockingMaskCount[pos]);
            return PrecomputedInfo.RookMovesBitBoard[pos][Index];
        }
        public void MovePiece(ushort startPos, ushort endPos, ushort piece)
        {
            BitBoards[piece] -= 1ul << startPos;
            BitBoards[piece] += 1ul << endPos;
            UshortBoard[startPos] = (ushort)PieceType.EMPTY;
            UshortBoard[endPos] = piece;
            ZobristHash = ZobristHashing.MovePiece(ZobristHash, startPos, endPos, piece);
        }
        public void AddPiece(ushort pos, ushort piece)
        {
            BitBoards[piece] += 1ul << pos;
            UshortBoard[pos] = piece;
            ZobristHash = ZobristHashing.AddPiece(ZobristHash, pos, piece);
        }
        public void RemovePiece(ushort pos, ushort piece)
        {
            BitBoards[piece] -= 1ul << pos;
            UshortBoard[pos] = Piece.EMPTY;
            ZobristHash = ZobristHashing.RemovePiece(ZobristHash, pos, piece);
        }
    }
}