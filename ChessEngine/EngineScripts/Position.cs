using System.Numerics;

namespace TOML
{
    public struct PosState
    {
        public ulong ZobristKey;
        public ulong PinnedPieces;
        public ulong WhiteAttackingSquares;
        public ulong BlackAttackingSquares;

        public PosState(ulong zobristKey, ulong pinnedPieces, ulong whiteAttackingSquares, ulong blackAttackingSquares)
        {
            this.ZobristKey = zobristKey;
            this.PinnedPieces = pinnedPieces;
            this.WhiteAttackingSquares = whiteAttackingSquares;
            this.BlackAttackingSquares = blackAttackingSquares;
        }
    }
    public class PrevStates
    {
        int Index = 0;
        PosState[] States = new PosState[4096];

        public PosState GetPreviousPosState()
        {
            return States[Index - 1];
        }
        public void AddPosState(ulong zobristKey, ulong pinnedPieces, ulong whiteAttackingSquares, ulong blackAttackingSquares)
        {
            
            States[Index] = new PosState(zobristKey, pinnedPieces, whiteAttackingSquares, blackAttackingSquares);
            Index++;
        }
        public void RemovePosState()
        {
            Index--;
        }
    }
    public class Position
    {
        public ushort[] UshortBoard;
        public ushort HalfMoveClock = 0;
        public ushort FullMoveNumber = 0;
        public ushort EnPassantTargetSquare = PositionConstants.NoEnPassantMoves;
        public ushort CastlingAvailability; // positions from left to right 1,2,3,4 corresponds to qkQK castling
        public ushort ActiveColor;
        public ushort OppositeColor;
        public ushort CurrentPly;

        //information about the position, pregenerated when position is created or updated
        
        public ulong ZobristHash;
        public ulong[] RepetitionTable = new ulong[4096];

        public ulong[] AttackingSquaresBitBoard = new ulong[15];

        public ulong[] PinnedPiecesBitBoard = new ulong[9];

        public ulong InCheckValidSquares = ulong.MaxValue;
        public ulong GivingCheckBitBoard = 0;
        public int CheckCount = 0;

        public ulong[] BitBoards = new ulong[15];
        public ulong AllPieceBitBoard = 0;

        public PrevStates PreviousStates = new PrevStates();

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
                    if (fenCharacter == 'w') { ActiveColor = (int)PieceType.WHITE; OppositeColor = Piece.BLACK; } else { ActiveColor = (int)PieceType.BLACK; OppositeColor = Piece.WHITE; }
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
                    FullMoveNumber = ushort.Parse(fenCharacter.ToString());
                }
            }
            this.ZobristHash = ZobristHashing.GetZobristKey(this);
            CurrentPly = (ushort)(FullMoveNumber * 2);
            if (ActiveColor == Piece.BLACK)
            {
                CurrentPly++;
            }
            BitBoards[Piece.WHITE] = BitBoards[Piece.PAWN] | BitBoards[Piece.KNIGHT] | BitBoards[Piece.BISHOP] | BitBoards[Piece.ROOK] | BitBoards[Piece.QUEEN] | BitBoards[Piece.KING];
            BitBoards[Piece.BLACK] = BitBoards[1 + Piece.BLACK] | BitBoards[2 + Piece.BLACK] | BitBoards[3 + Piece.BLACK] | BitBoards[4 + Piece.BLACK] | BitBoards[5 + Piece.BLACK] | BitBoards[6 + Piece.BLACK];
            AllPieceBitBoard = BitBoards[Piece.WHITE] | BitBoards[Piece.BLACK];

            RepetitionTable[CurrentPly] = ZobristHash;
            UpdatePositionStats();
        }
        
        public Position(Position referencePosition)
        {
            referencePosition.UshortBoard.CopyTo(UshortBoard, 0);
            this.HalfMoveClock = referencePosition.HalfMoveClock;
            this.FullMoveNumber = referencePosition.FullMoveNumber;
            this.EnPassantTargetSquare = referencePosition.EnPassantTargetSquare;
            this.CastlingAvailability = referencePosition.CastlingAvailability;
            this.ActiveColor = referencePosition.ActiveColor;
            this.OppositeColor = referencePosition.OppositeColor;
            this.CurrentPly = referencePosition.CurrentPly;

            this.ZobristHash = referencePosition.ZobristHash;
            
            
            referencePosition.BitBoards.CopyTo(this.BitBoards, 0);
            referencePosition.RepetitionTable.CopyTo(this.RepetitionTable, 0);

        }

        //switches the active color on the board
        public void SwitchActiveColor()
        {
            ActiveColor ^= 8;
            OppositeColor ^= 8;
        }

        //plays a move on the board
        public UnMove MakeMove(Move move)
        {
            ushort StartingPosition = move.startPos;
            ushort EndingPosition = move.endPos;
            ushort piece = UshortBoard[StartingPosition];
            ushort PieceWithoutColor = GetPieceWithoutColor(piece);
            ushort Color = GetColor(piece);

            PreviousStates.AddPosState(ZobristHash, PinnedPiecesBitBoard[ActiveColor], AttackingSquaresBitBoard[0], AttackingSquaresBitBoard[8]);

            CurrentPly++;

            //remove the EnPassant square from zobrist hash if necessary
            if (EnPassantTargetSquare != PositionConstants.NoEnPassantMoves)
            {
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);
            }
            //update player to move in zobrist hash
            ZobristHash = ZobristHashing.UpdateSideToMove(ZobristHash);
            uint UnMoveInfo = 0;
            UnMoveInfo += (uint)(EnPassantTargetSquare << 8);
            UnMoveInfo += CastlingAvailability;
            UnMoveInfo += (uint)(HalfMoveClock << 15);
            UnMoveInfo += (uint)(FullMoveNumber << 22);
            EnPassantTargetSquare = PositionConstants.NoEnPassantMoves;

            ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, CastlingAvailability);
            CastlingAvailability &= PositionConstants.CASTLING_MASK[EndingPosition];
            CastlingAvailability &= PositionConstants.CASTLING_MASK[StartingPosition];
            ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, CastlingAvailability);

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
                EnPassantTargetSquare = (ushort)((StartingPosition + EndingPosition) >> 1);
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);

            } else if (move.moveInfo >> 3 == 1)
            {
                //pawn at end! handle promotion

                ushort PromotedPiece = (ushort)((move.moveInfo & 3) + 2 + ActiveColor);

                if ((move.moveInfo >> 2) == 3)
                {
                    //capture, must remove captured piece
                    UnMoveInfo += (uint)(UshortBoard[EndingPosition] << 4);
                    RemovePiece(EndingPosition, UshortBoard[EndingPosition]);
                }
                RemovePiece(StartingPosition, UshortBoard[StartingPosition]);

                AddPiece(EndingPosition, PromotedPiece);

                RepetitionTable[CurrentPly] = ZobristHash;
                SwitchActiveColor();
                return new UnMove(move, UnMoveInfo);
            } else if (move.moveInfo == 4)
            {
                // capture
                HalfMoveClock = 0;
                
                UnMoveInfo += (uint)(UshortBoard[EndingPosition] << 4);
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

                UnMoveInfo += (uint)(UshortBoard[CaptureSquare] << 4);
                RemovePiece(CaptureSquare, UshortBoard[CaptureSquare]);
            } else if (move.moveInfo == (ushort)MoveEncodingCodes.KINGCASTLE)
            {
                if (Color == Piece.WHITE)
                {
                    MovePiece(7, 5, (ushort)PieceType.WHITE + (ushort)PieceType.ROOK);
                } else
                {
                    MovePiece(63, 61, (ushort)PieceType.BLACK + (ushort)PieceType.ROOK);
                }
            } else if (move.moveInfo == (int)MoveEncodingCodes.QUEENCASTLE)
            {
                if (Color == Piece.WHITE)
                {
                    MovePiece(0, 3, (ushort)PieceType.WHITE + (ushort)PieceType.ROOK);
                } else
                {
                    MovePiece(56, 59, (ushort)PieceType.BLACK + (ushort)PieceType.ROOK);
                }
            }
            MovePiece(StartingPosition, EndingPosition, piece);
            RepetitionTable[CurrentPly] = ZobristHash;
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

            CurrentPly--;
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

            PosState PrevState = PreviousStates.GetPreviousPosState();

            PinnedPiecesBitBoard[ActiveColor] = PrevState.PinnedPieces;
            AttackingSquaresBitBoard[0] = PrevState.WhiteAttackingSquares;
            AttackingSquaresBitBoard[8] = PrevState.BlackAttackingSquares;
            PreviousStates.RemovePosState();

            //castling logic
            if (move.moveInfo == (int)MoveEncodingCodes.KINGCASTLE)
            {
                if (Color == Piece.WHITE)
                {
                    MovePiece(5, 7, Piece.WHITE + Piece.ROOK);

                } else
                {
                    MovePiece(61, 63, Piece.BLACK + Piece.ROOK);

                }
            } else if (move.moveInfo == (int)MoveEncodingCodes.QUEENCASTLE)
            {
                if (Color == Piece.WHITE)
                {
                    MovePiece(3, 0, Piece.WHITE + Piece.ROOK);
                } else
                {
                    MovePiece(59, 56, Piece.BLACK + Piece.ROOK);
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

            InCheckValidSquares = ulong.MaxValue;
            CheckCount = 0;

            //compute the queen with rooks and bishops

            ulong Checkers = 0;

            Checkers |= PrecomputedInfo.PawnAttacksBitBoard[KingPos][ActiveColor] & BitBoards[OppositeColor + Piece.PAWN];
            Checkers |= PrecomputedInfo.KnightMovesBitBoard[KingPos] & BitBoards[OppositeColor + Piece.KNIGHT];

            PinnedPiecesBitBoard[ActiveColor] = 0;

            Checkers |= (BitBoards[OppositeColor + Piece.BISHOP] | BitBoards[OppositeColor + Piece.QUEEN]) & GetBishopAttacks(KingPos, AllPieceBitBoard & PrecomputedInfo.BishopBlockingMasks[KingPos]);
            Checkers |= (BitBoards[OppositeColor + Piece.ROOK] | BitBoards[OppositeColor + Piece.QUEEN]) & GetRookAttacks(KingPos, AllPieceBitBoard & PrecomputedInfo.RookBlockingMasks[KingPos]);

            ulong PossiblePinners = 0;
            PossiblePinners |= (BitBoards[OppositeColor + Piece.BISHOP] | BitBoards[OppositeColor + Piece.QUEEN]) & PrecomputedInfo.BishopMoves[KingPos];
            PossiblePinners |= (BitBoards[OppositeColor + Piece.ROOK] | BitBoards[OppositeColor + Piece.QUEEN]) & PrecomputedInfo.RookMoves[KingPos];

            while (PossiblePinners != 0)
            {
                int PossiblePinner = BitOperations.TrailingZeroCount(PossiblePinners);
                PossiblePinners &= PossiblePinners - 1;
                ulong PossibleBlockers = PrecomputedInfo.InBetweenBitBoard[KingPos][PossiblePinner] & AllPieceBitBoard;
                int PossiblePinned = BitOperations.TrailingZeroCount(PossibleBlockers);
                
                if (BitOperations.PopCount(PossibleBlockers) == 1 && PieceColors.GetColor(UshortBoard[PossiblePinned]) == ActiveColor)
                {
                    PinnedPiecesBitBoard[ActiveColor] |= 1ul << PossiblePinned;
                }
            }
            CheckCount = BitOperations.PopCount(Checkers);
            if (CheckCount == 1)
            {
                InCheckValidSquares &= Checkers + PrecomputedInfo.InBetweenBitBoard[KingPos][BitOperations.TrailingZeroCount(Checkers)];
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
                return Piece.BLACK;
            } else
            {
                return Piece.WHITE;
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
        public bool IsPinned(ushort position, ushort color)
        {
            return ((PinnedPiecesBitBoard[color] >> position) & 1) == 1;
        }
        public bool CanPlayEPMove(ushort startPos)
        {
            //logic: EP is not possible only if the pawn pos is between the king and rook/queen
            //if there are more than 2 pieces between then there is at least 1 extra piece blocking the attack after pawns clear
            int KingPos = GetKingPos(ActiveColor);
            GetXYPosition(startPos, out ushort X, out ushort Y);
            ulong Rank = BitBoardConstants.FIRST_RANK << (Y * 8);

            if ((Rank & BitBoards[Piece.KING + ActiveColor]) == 0)
            {
                //king not on rank
                return true;
            }
            ulong PossiblePinners = Rank & (BitBoards[Piece.ROOK + OppositeColor] | BitBoards[Piece.QUEEN + OppositeColor]);
            while (PossiblePinners != 0)
            {
                int pos = BitOperations.TrailingZeroCount(PossiblePinners);
                PossiblePinners &= PossiblePinners - 1;

                ulong Blockers = PrecomputedInfo.InBetweenBitBoard[pos][KingPos] & AllPieceBitBoard;
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
        public bool IsLegal(Move move)
        {
            ushort startPos = move.startPos;
            ushort endPos = move.endPos;
            int KingPos = GetKingPos(ActiveColor);
            if (move.moveInfo == (ushort)MoveEncodingCodes.QUEENCASTLE)
            {
                return !(
                    IsAttacked((ushort)(move.startPos - 1), OppositeColor) ||
                    IsAttacked((ushort)(move.startPos - 2), OppositeColor) ||
                    IsAttacked(move.startPos, OppositeColor)
                    
                    );
            } else if (move.moveInfo == (ushort)MoveEncodingCodes.KINGCASTLE)
            {
                return !(
                    IsAttacked((ushort)(move.startPos + 1), OppositeColor) ||
                    IsAttacked((ushort)(move.startPos + 2), OppositeColor) ||
                    IsAttacked(move.startPos, OppositeColor));
            } else if (move.moveInfo == (ushort)MoveEncodingCodes.EPCAPTURE)
            {
                return CanPlayEPMove(move.startPos) && !(IsPinned(startPos, ActiveColor) && (PrecomputedInfo.LineBitBoard[startPos][KingPos] & (1ul << endPos)) == 0);
            } else if ((UshortBoard[startPos] & 7) == Piece.KING)
            {
                return !IsAttacked(endPos, OppositeColor);
            } else
            {
                //check for pins
                return (!IsPinned(startPos, ActiveColor) || (PrecomputedInfo.LineBitBoard[startPos][KingPos] & (1ul << endPos)) != 0);
            }
        }
        public int GetKingPos(ushort color)
        {
            return BitOperations.TrailingZeroCount(BitBoards[color + Piece.KING]);
        }
        public ulong GetKingBlockers(ushort color)
        {
            int KingPos = GetKingPos(color);
            ulong BlockerMask = GetBishopAttacks(KingPos, 0) * BitBoards[color];
            BlockerMask |= GetRookAttacks(KingPos, 0) * BitBoards[color];

            return BlockerMask;
        }
        //returns if the square is attacked by the color
        public bool IsAttacked(ushort pos, ushort color)
        {
            if ((PrecomputedInfo.KnightMovesBitBoard[pos] & BitBoards[color + Piece.KNIGHT]) != 0)
            {
                return true;
            } else if ((PrecomputedInfo.KingMovesBitBoard[pos] & BitBoards[color + Piece.KING]) != 0)
            {
                return true;
            } else if ((PrecomputedInfo.PawnAttacksBitBoard[pos][PieceColors.GetOppositeColor(color)] & BitBoards[color + Piece.PAWN]) != 0)
            {
                return true;
            } else if (((GetBishopAttacks(pos, PrecomputedInfo.BishopBlockingMasks[pos] & AllPieceBitBoard & ~BitBoards[PieceColors.GetOppositeColor(color) + Piece.KING]) & (BitBoards[color + Piece.BISHOP] | BitBoards[color + Piece.QUEEN])) | (GetRookAttacks(pos, PrecomputedInfo.RookBlockingMasks[pos] & AllPieceBitBoard & ~BitBoards[PieceColors.GetOppositeColor(color) + Piece.KING]) & (BitBoards[color + Piece.ROOK] | BitBoards[color + Piece.QUEEN]))) != 0)
            {
                return true;
            }
            return false;
        }
        public bool IsAttackedWithBB(ushort pos, ushort color)
        {
            return ((AttackingSquaresBitBoard[color] >> pos) & 1) == 1;
        }

        public static ulong GetBishopAttacks(int pos, ulong blockers)
        { 
            ulong Index = (blockers * MagicNumbers.BishopMagicNumbers[pos]) >> (PrecomputedInfo.BishopMagicShift[pos]);
            return PrecomputedInfo.BishopMovesBitBoard[pos][Index];
        }
        public static ulong GetRookAttacks(int pos, ulong blockers)
        {
            ulong Index = (blockers * MagicNumbers.RookMagicNumbers[pos]) >> (PrecomputedInfo.RookMagicShift[pos]);
            return PrecomputedInfo.RookMovesBitBoard[pos][Index];
        }
        public void MovePiece(ushort startPos, ushort endPos, ushort piece)
        {
            ulong startMask = 1ul << startPos;
            ulong endMask = 1ul << endPos;
            ulong moveMask = startMask | endMask;

            BitBoards[piece] ^= moveMask;
            BitBoards[GetColor(piece)] ^= moveMask;
            AllPieceBitBoard ^= moveMask;

            UshortBoard[startPos] = Piece.EMPTY;
            UshortBoard[endPos] = piece;
            ZobristHash = ZobristHashing.MovePiece(ZobristHash, startPos, endPos, piece);
        }
        public void AddPiece(ushort pos, ushort piece)
        {
            ulong mask = 1ul << pos;
            BitBoards[piece] |= mask;
            BitBoards[GetColor(piece)] |= mask;
            AllPieceBitBoard |= mask;

            UshortBoard[pos] = piece;
            ZobristHash = ZobristHashing.AddPiece(ZobristHash, pos, piece);
        }
        public void RemovePiece(ushort pos, ushort piece)
        {
            ulong Mask = ~(1ul << pos);
            BitBoards[piece] &= Mask;
            BitBoards[GetColor(piece)] &= Mask;
            AllPieceBitBoard &= Mask;

            UshortBoard[pos] = Piece.EMPTY;
            ZobristHash = ZobristHashing.RemovePiece(ZobristHash, pos, piece);
        }
        public bool FoundRepetition(ulong ZobristKey)
        {
            for (int i = 2; i <= HalfMoveClock; i +=2 )
            {
                if (RepetitionTable[CurrentPly - i] == ZobristKey)
                {
                    return true;
                }
            }
            return false;
        }
    }
}