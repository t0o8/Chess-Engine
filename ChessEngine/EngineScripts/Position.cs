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
        private PosState[] States = new PosState[4096];

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
        public ushort EnPassantTargetSquare = PositionConstants.NO_EN_PASSANT_MOVES;
        public ushort CastlingAvailability; // positions from left to right 1,2,3,4 corresponds to qkQK castling
        public ushort ActiveColor;
        public ushort OppositeColor;
        public ushort CurrentPly;
        
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

            for (int stringPosition = 0; stringPosition < fenString.Length; stringPosition++)
            {

                char fenCharacter = fenString[stringPosition];
                ushort position = BoardHelper.GetBoardPosition(column, row);
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
                            UshortBoard[position] = Piece.EMPTY;
                            column++;
                        }
                        continue;
                    }
                    int pieceColor = PieceHelper.GetPieceColorInInt(fenCharacter);
                    int piece = Conversions.FenToPieceType[char.ToLower(fenCharacter)];

                    ushort pieceValue = (ushort)(piece + pieceColor);
                    UshortBoard[position] = pieceValue;
                    BitBoards[pieceValue] += 1ul << position;
                    column++;

                }
                if (steps == 1)
                {
                    //move color
                    if (fenCharacter == 'w') { ActiveColor = Piece.WHITE; OppositeColor = Piece.BLACK; } else { ActiveColor = Piece.BLACK; OppositeColor = Piece.WHITE; }
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
                    ushort rank = (ushort)(fenString[stringPosition + 1] - 49);
                    EnPassantTargetSquare = BoardHelper.GetBoardPosition(file, rank);
                    recordedEnpassant = true;
                }
                if (steps == 4)
                {
                    int originalStringPos = stringPosition;
                    while (true)
                    {
                        if (fenString[stringPosition] == ' ')
                        {
                            HalfMoveClock = ushort.Parse(fenString[originalStringPos..stringPosition]);
                            steps++;
                            break;
                        }
                        stringPosition++;

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
            ushort startingPosition = move.StartPos;
            ushort endingPosition = move.EndPos;
            ushort piece = UshortBoard[startingPosition];
            ushort pieceWithoutColor = PieceHelper.GetPieceWithoutColor(piece);
            ushort color = PieceHelper.GetColor(piece);

            PreviousStates.AddPosState(ZobristHash, PinnedPiecesBitBoard[ActiveColor], AttackingSquaresBitBoard[0], AttackingSquaresBitBoard[8]);

            CurrentPly++;

            //remove the EnPassant square from zobrist hash if necessary
            if (EnPassantTargetSquare != PositionConstants.NO_EN_PASSANT_MOVES)
            {
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);
            }
            //update player to move in zobrist hash
            ZobristHash = ZobristHashing.UpdateSideToMove(ZobristHash);
            uint unMoveInfo = 0;
            unMoveInfo += (uint)(EnPassantTargetSquare << 8);
            unMoveInfo += CastlingAvailability;
            unMoveInfo += (uint)(HalfMoveClock << 15);
            unMoveInfo += (uint)(FullMoveNumber << 22);
            EnPassantTargetSquare = PositionConstants.NO_EN_PASSANT_MOVES;

            ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, CastlingAvailability);
            CastlingAvailability &= PositionConstants.CASTLING_MASK[endingPosition];
            CastlingAvailability &= PositionConstants.CASTLING_MASK[startingPosition];
            ZobristHash = ZobristHashing.ChangeCastling(ZobristHash, CastlingAvailability);

            HalfMoveClock++;
            if (color == Piece.BLACK)
            {
                FullMoveNumber++;
            }
            if (pieceWithoutColor == Piece.PAWN)
            {
                HalfMoveClock = 0; 
            }
            if (move.MoveInfo == 1)
            {
                //double pawn push
                EnPassantTargetSquare = (ushort)((startingPosition + endingPosition) >> 1);
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);

            } else if (move.MoveInfo >> 3 == 1)
            {
                //pawn at end! handle promotion

                ushort PromotedPiece = (ushort)((move.MoveInfo & 3) + 2 + ActiveColor);

                if ((move.MoveInfo >> 2) == 3)
                {
                    //capture, must remove captured piece
                    unMoveInfo += (uint)(UshortBoard[endingPosition] << 4);
                    RemovePiece(endingPosition, UshortBoard[endingPosition]);
                }
                RemovePiece(startingPosition, UshortBoard[startingPosition]);

                AddPiece(endingPosition, PromotedPiece);

                RepetitionTable[CurrentPly] = ZobristHash;
                SwitchActiveColor();
                return new UnMove(move, unMoveInfo);
            } else if (move.MoveInfo == MoveEncodingCodes.CAPTURE)
            {
                // capture
                HalfMoveClock = 0;

                unMoveInfo += (uint)(UshortBoard[endingPosition] << 4);
                RemovePiece(endingPosition, UshortBoard[endingPosition]);

            } else if (move.MoveInfo == MoveEncodingCodes.EP_CAPTURE)
            {
                //ep capture!
                HalfMoveClock = 0;

                ushort CaptureSquare;

                if (move.EndPos > 39)
                {
                    CaptureSquare = (ushort)(endingPosition - 8);

                } else
                {
                    CaptureSquare = (ushort)(endingPosition + 8);
                }

                unMoveInfo += (uint)(UshortBoard[CaptureSquare] << 4);
                RemovePiece(CaptureSquare, UshortBoard[CaptureSquare]);
            } else if (move.MoveInfo == MoveEncodingCodes.KING_CASTLE)
            {
                if (color == Piece.WHITE)
                {
                    MovePiece(7, 5, Piece.WHITE + Piece.ROOK);
                } else
                {
                    MovePiece(63, 61, Piece.BLACK + Piece.ROOK);
                }
            } else if (move.MoveInfo == MoveEncodingCodes.QUEEN_CASTLE)
            {
                if (color == Piece.WHITE)
                {
                    MovePiece(0, 3, Piece.WHITE + Piece.ROOK);
                } else
                {
                    MovePiece(56, 59, Piece.BLACK + Piece.ROOK);
                }
            }
            MovePiece(startingPosition, endingPosition, piece);
            RepetitionTable[CurrentPly] = ZobristHash;
            SwitchActiveColor();
            return new UnMove(move, unMoveInfo);
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
            if (EnPassantTargetSquare != PositionConstants.NO_EN_PASSANT_MOVES) {
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);
            }
            EnPassantTargetSquare = (ushort)((move.UnMoveInfo >> 8) & 127);
            if (EnPassantTargetSquare != PositionConstants.NO_EN_PASSANT_MOVES)
            {
                ZobristHash = ZobristHashing.ChangeEnpassant(ZobristHash, EnPassantTargetSquare);
            }

            //update side to move
            ZobristHash = ZobristHashing.UpdateSideToMove(ZobristHash);
            SwitchActiveColor();

            HalfMoveClock = (ushort)((move.UnMoveInfo >> 15) & 127);
            FullMoveNumber = (ushort)(move.UnMoveInfo >> 22);

            ushort piece = UshortBoard[move.EndPos];
            ushort color = PieceHelper.GetColor(piece);

            PosState prevState = PreviousStates.GetPreviousPosState();

            PinnedPiecesBitBoard[ActiveColor] = prevState.PinnedPieces;
            AttackingSquaresBitBoard[0] = prevState.WhiteAttackingSquares;
            AttackingSquaresBitBoard[8] = prevState.BlackAttackingSquares;
            PreviousStates.RemovePosState();

            //castling logic
            if (move.MoveInfo == MoveEncodingCodes.KING_CASTLE)
            {
                if (color == Piece.WHITE)
                {
                    MovePiece(5, 7, Piece.WHITE + Piece.ROOK);

                } else
                {
                    MovePiece(61, 63, Piece.BLACK + Piece.ROOK);

                }
            } else if (move.MoveInfo == MoveEncodingCodes.QUEEN_CASTLE)
            {
                if (color == Piece.WHITE)
                {
                    MovePiece(3, 0, Piece.WHITE + Piece.ROOK);
                } else
                {
                    MovePiece(59, 56, Piece.BLACK + Piece.ROOK);
                }
            }
            if (move.MoveInfo >> 3 == 1)
            {
                //undoing promotions
                ushort promotedPiece = UshortBoard[move.EndPos];

                RemovePiece(move.EndPos, promotedPiece);

                AddPiece(move.StartPos, (ushort)(Piece.PAWN + color));

                if ((move.MoveInfo >> 2) == 3)
                {
                    //captured piece, must return it
                    ushort capturedPiece = (ushort)((move.UnMoveInfo >> 4) & 15);
                    AddPiece(move.EndPos, capturedPiece);
                }
                    return;
                } else if (move.MoveInfo == MoveEncodingCodes.EP_CAPTURE)
                {
                ushort capturedPiece = (ushort)((move.UnMoveInfo >> 4) & 15);
                MovePiece(move.EndPos, move.StartPos, piece);
                if (move.EndPos > 39)
                {
                    AddPiece((ushort)(move.EndPos - 8), capturedPiece);

                } else
                {
                    AddPiece((ushort)(move.EndPos + 8), capturedPiece);
                }
            } else if (move.MoveInfo == MoveEncodingCodes.CAPTURE)
            {
                ushort capturedPiece = (ushort)((move.UnMoveInfo >> 4) & 15);
                MovePiece(move.EndPos, move.StartPos, piece);
                AddPiece(move.EndPos, capturedPiece);
            }  else
            {
                MovePiece(move.EndPos, move.StartPos, piece);
            }
        }
        public void UpdatePositionStats()
        {
            int kingPos = BitOperations.TrailingZeroCount(BitBoards[ActiveColor + Piece.KING]);

            InCheckValidSquares = ulong.MaxValue;
            CheckCount = 0;

            //compute the queen with rooks and bishops

            ulong checkers = 0;

            checkers |= PrecomputedInfo.PawnAttacksBitBoard[kingPos][ActiveColor] & BitBoards[OppositeColor + Piece.PAWN];
            checkers |= PrecomputedInfo.KnightMovesBitBoard[kingPos] & BitBoards[OppositeColor + Piece.KNIGHT];

            PinnedPiecesBitBoard[ActiveColor] = 0;

            checkers |= (BitBoards[OppositeColor + Piece.BISHOP] | BitBoards[OppositeColor + Piece.QUEEN]) & GetBishopAttacks(kingPos, AllPieceBitBoard & PrecomputedInfo.BishopBlockingMasks[kingPos]);
            checkers |= (BitBoards[OppositeColor + Piece.ROOK] | BitBoards[OppositeColor + Piece.QUEEN]) & GetRookAttacks(kingPos, AllPieceBitBoard & PrecomputedInfo.RookBlockingMasks[kingPos]);

            ulong possiblePinners = 0;
            possiblePinners |= (BitBoards[OppositeColor + Piece.BISHOP] | BitBoards[OppositeColor + Piece.QUEEN]) & PrecomputedInfo.BishopMoves[kingPos];
            possiblePinners |= (BitBoards[OppositeColor + Piece.ROOK] | BitBoards[OppositeColor + Piece.QUEEN]) & PrecomputedInfo.RookMoves[kingPos];

            while (possiblePinners != 0)
            {
                int possiblePinner = BitOperations.TrailingZeroCount(possiblePinners);
                possiblePinners &= possiblePinners - 1;
                ulong possibleblockers = PrecomputedInfo.InBetweenBitBoard[kingPos][possiblePinner] & AllPieceBitBoard;
                int possiblePinned = BitOperations.TrailingZeroCount(possibleblockers);
                
                if (BitOperations.PopCount(possibleblockers) == 1 && PieceHelper.GetColor(UshortBoard[possiblePinned]) == ActiveColor)
                {
                    PinnedPiecesBitBoard[ActiveColor] |= 1ul << possiblePinned;
                }
            }
            CheckCount = BitOperations.PopCount(checkers);
            if (CheckCount == 1)
            {
                InCheckValidSquares &= checkers + PrecomputedInfo.InBetweenBitBoard[kingPos][BitOperations.TrailingZeroCount(checkers)];
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
            int kingPos = GetKingPos(ActiveColor);
            ushort y = BoardHelper.GetYPos(startPos);
            ulong rank = BitBoardConstants.FIRST_RANK << (y * 8);

            if ((rank & BitBoards[Piece.KING + ActiveColor]) == 0)
            {
                //king not on rank
                return true;
            }
            ulong possiblePinners = rank & (BitBoards[Piece.ROOK + OppositeColor] | BitBoards[Piece.QUEEN + OppositeColor]);
            while (possiblePinners != 0)
            {
                int pos = BitOperations.TrailingZeroCount(possiblePinners);
                possiblePinners &= possiblePinners - 1;

                ulong blockers = PrecomputedInfo.InBetweenBitBoard[pos][kingPos] & AllPieceBitBoard;
                if (((blockers >> startPos) & 1) == 0)
                {
                    //pawn isn't here
                    continue;
                } else if (BitOperations.PopCount(blockers) < 3)
                {
                    return false;
                }
            }
            return true;
        }
        public bool IsLegal(Move move)
        {
            ushort startPos = move.StartPos;
            ushort endPos = move.EndPos;
            int kingPos = GetKingPos(ActiveColor);
            if (move.MoveInfo == MoveEncodingCodes.QUEEN_CASTLE)
            {
                return !(
                    IsAttacked((ushort)(move.StartPos - 1), OppositeColor) ||
                    IsAttacked((ushort)(move.StartPos - 2), OppositeColor) ||
                    IsAttacked(move.StartPos, OppositeColor)
                    
                    );
            } else if (move.MoveInfo == MoveEncodingCodes.KING_CASTLE)
            {
                return !(
                    IsAttacked((ushort)(move.StartPos + 1), OppositeColor) ||
                    IsAttacked((ushort)(move.StartPos + 2), OppositeColor) ||
                    IsAttacked(move.StartPos, OppositeColor));
            } else if (move.MoveInfo == MoveEncodingCodes.EP_CAPTURE)
            {
                return CanPlayEPMove(move.StartPos) && !(IsPinned(startPos, ActiveColor) && (PrecomputedInfo.LineBitBoard[startPos][kingPos] & (1ul << endPos)) == 0);
            } else if ((UshortBoard[startPos] & 7) == Piece.KING)
            {
                return !IsAttacked(endPos, OppositeColor);
            } else
            {
                //check for pins
                return (!IsPinned(startPos, ActiveColor) || (PrecomputedInfo.LineBitBoard[startPos][kingPos] & (1ul << endPos)) != 0);
            }
        }
        public ushort GetKingPos(ushort color)
        {
            return (ushort)BitOperations.TrailingZeroCount(BitBoards[color + Piece.KING]);
        }
        public ulong GetKingblockers(ushort color)
        {
            int kingPos = GetKingPos(color);
            ulong blockerMask = GetBishopAttacks(kingPos, 0) * BitBoards[color];
            blockerMask |= GetRookAttacks(kingPos, 0) * BitBoards[color];

            return blockerMask;
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
            } else if ((PrecomputedInfo.PawnAttacksBitBoard[pos][PieceHelper.GetOppositeColor(color)] & BitBoards[color + Piece.PAWN]) != 0)
            {
                return true;
            } else if (((GetBishopAttacks(pos, PrecomputedInfo.BishopBlockingMasks[pos] & AllPieceBitBoard & ~BitBoards[PieceHelper.GetOppositeColor(color) + Piece.KING]) & (BitBoards[color + Piece.BISHOP] | BitBoards[color + Piece.QUEEN])) | (GetRookAttacks(pos, PrecomputedInfo.RookBlockingMasks[pos] & AllPieceBitBoard & ~BitBoards[PieceHelper.GetOppositeColor(color) + Piece.KING]) & (BitBoards[color + Piece.ROOK] | BitBoards[color + Piece.QUEEN]))) != 0)
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
            ulong index = (blockers * MagicNumbers.BishopMagicNumbers[pos]) >> (PrecomputedInfo.BishopMagicShift[pos]);
            return PrecomputedInfo.BishopMovesBitBoard[pos][index];
        }
        public static ulong GetRookAttacks(int pos, ulong blockers)
        {
            ulong index = (blockers * MagicNumbers.RookMagicNumbers[pos]) >> (PrecomputedInfo.RookMagicShift[pos]);
            return PrecomputedInfo.RookMovesBitBoard[pos][index];
        }
        public void MovePiece(ushort startPos, ushort endPos, ushort piece)
        {
            ulong startMask = 1ul << startPos;
            ulong endMask = 1ul << endPos;
            ulong moveMask = startMask | endMask;

            BitBoards[piece] ^= moveMask;
            BitBoards[PieceHelper.GetColor(piece)] ^= moveMask;
            AllPieceBitBoard ^= moveMask;

            UshortBoard[startPos] = Piece.EMPTY;
            UshortBoard[endPos] = piece;
            ZobristHash = ZobristHashing.MovePiece(ZobristHash, startPos, endPos, piece);
        }
        public void AddPiece(ushort pos, ushort piece)
        {
            ulong mask = 1ul << pos;
            BitBoards[piece] |= mask;
            BitBoards[PieceHelper.GetColor(piece)] |= mask;
            AllPieceBitBoard |= mask;

            UshortBoard[pos] = piece;
            ZobristHash = ZobristHashing.AddPiece(ZobristHash, pos, piece);
        }
        public void RemovePiece(ushort pos, ushort piece)
        {
            ulong mask = ~(1ul << pos);
            BitBoards[piece] &= mask;
            BitBoards[PieceHelper.GetColor(piece)] &= mask;
            AllPieceBitBoard &= mask;

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
    public static class BoardHelper
    {
        public static ushort GetXPos(ushort pos)
        {
            return (ushort)(pos & 7);
        }
        public static ushort GetYPos(ushort pos)
        {
            return (ushort)(pos >> 3);
        }
        public static ushort NumLetterToBoardPos(string str)
        {
            int xValue = Conversions.LetterToNumber[str[0]];
            int yValue = (Int32.Parse(str[1].ToString()) - 1);

            return (ushort)(xValue + (yValue * 8));
        }
        public static ushort GetBoardPosition(ushort column, ushort row)
        {
            return (ushort)((row * 8) + column);
        }
        public static string NumberToBoardLetter(ushort num)
        {
            ushort x = GetXPos(num);
            ushort y = GetYPos(num);
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
    }
}