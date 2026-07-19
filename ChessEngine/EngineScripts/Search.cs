using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;



namespace TOML
{
    [SkipLocalsInit]
    public class Search
    {
        public static int branches = 0;
        public static int perftcount = 0;
        public static int EvaluatedPositions = 0;
        public static int TranspositionsUsed = 0;

        public static bool SearchCancelled = false;

        public static Stopwatch NPSStopWatch = new Stopwatch();

        static byte CurrentSearchDepth = 1;
        


        //time limit is in miliseconds
        public static Move BeginSearch(Position boardPosition, int timeLimit)
        {
            TimeManager.StartStopWatch();
            TimeManager.SetTimeLimit(timeLimit);

            NPSStopWatch.Restart();
            EvaluatedPositions = 0;

            CurrentSearchDepth = 1;
            SearchCancelled = false;

            KillerMoves.ResetTable();

            Span<Move> MoveList = stackalloc Move[MoveGenConstants.MAX_POSSIBLE_LEGAL_MOVES];
            byte MoveCount = MoveGeneration.GenerateAllPseudoLegalMoves(boardPosition, MoveList);


            if (MoveCount == 0)
            {
                return new Move();
            }

            Span<int> MoveValueList = stackalloc int[MoveCount];
            
            Move BestMove = MoveList[0];
            //iterative deepening
            while (SearchCancelled == false)
            {

                int lowerBound = EvaluationConstants.ABSOLUTE_LOWER_BOUND;
                int upperBound = EvaluationConstants.ABSOLUTE_UPPER_BOUND;

                int PositionRelativeEval = lowerBound;

                Move BestMoveThisDepth = BestMove;

                MoveOrdering.GenerateMoveValues(boardPosition, MoveValueList, MoveList, MoveCount, BestMove, CurrentSearchDepth);

                for (int i = 0; i < MoveCount; i++)
                {
                    Move move = MoveOrdering.GetNextMove(MoveList, MoveCount, MoveValueList, i);
                    if (boardPosition.IsLegal(move) == false)
                    {
                        continue;
                    }
                    UnMove Unmove = boardPosition.MakeMove(move);
                    PositionRelativeEval = -GetEval(boardPosition, (byte)(CurrentSearchDepth - 1), -upperBound, -lowerBound);
                    boardPosition.UnMakeMove(Unmove);
                    //since we return 0 when search is cancelled we must check that the eval is from a search that isn't cancelled
                    if (SearchCancelled == true)
                    {
                        //if we fix the previous best move as the first move then any changes would be "better" at the new depth,
                        //since we've realized the previous move wasn't as good as we thought it to be 
                        return BestMoveThisDepth;
                    }

                    if (PositionRelativeEval > lowerBound)
                    {
                        //move gives better position!
                        BestMoveThisDepth = move;
                        //the lower bound should be best opponent so far. Ignore moves that are worse since we want the best position possible
                        lowerBound = PositionRelativeEval;
                    }
                }
                if (SearchCancelled == true)
                {
                    return BestMoveThisDepth;
                }
                BestMove = BestMoveThisDepth;
                int NPS = (int)((long)EvaluatedPositions / (long)Math.Max(NPSStopWatch.ElapsedMilliseconds, 1) * 1000);

                if (lowerBound >= -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    Console.WriteLine("info depth " + CurrentSearchDepth + " score mate " + Math.Ceiling((decimal)(-lowerBound - EvaluationConstants.CHECKMATE_BASE_EVAL)/2).ToString() + " nodes " + EvaluatedPositions + " nps " + NPS);
                } else if (lowerBound <= EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    Console.WriteLine("info depth " + CurrentSearchDepth + " score mate " + Math.Ceiling((decimal)(lowerBound - EvaluationConstants.CHECKMATE_BASE_EVAL)/2).ToString() + " nodes " + EvaluatedPositions +" nps " + NPS);
                } else 
                {
                    Console.WriteLine("info depth " + CurrentSearchDepth + " score cp " + lowerBound.ToString() + " nodes " + EvaluatedPositions +" nps " + NPS); //+ Position.NumberToBoardLetter(BestMove.startPos) + Position.NumberToBoardLetter(BestMove.endPos)
                }
                TranspositionTable.StorePosition(boardPosition.ZobristHash, BestMove, CurrentSearchDepth, lowerBound, NodeConstants.PV);
                CurrentSearchDepth++;
                if (CurrentSearchDepth == byte.MaxValue)
                {
                    return BestMove;
                }
            }
            return BestMove;
        }

        public static int GetEval(Position boardPosition, byte depth, int lowerBound, int upperBound)
        {
            TimeManager.CheckTime();

            if (SearchCancelled)
            {
                return 0;
            }
            if (boardPosition.FoundRepetition(boardPosition.ZobristHash) == true)
            {
                //draw by repetition
                return 0;
            }

            int DepthFromRoot = CurrentSearchDepth - depth + 1;
            int PositionRelativeEval = lowerBound;
            int OriginalLowerBound = lowerBound;
            Move PredictedBestMove = new Move();

            ref TranspositionEntry entry = ref TranspositionTable.GetTransposition(boardPosition.ZobristHash);
            if (entry.ZobristKey == boardPosition.ZobristHash)
            {
                TranspositionsUsed++;
                int ttEval = entry.Eval;
                if (ttEval > -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    ttEval -= DepthFromRoot;
                } else if (ttEval < EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    ttEval += DepthFromRoot;
                }
                if (entry.Depth >= depth && entry.NodeType == NodeConstants.PV)
                {
                    return ttEval;
                } else if (entry.Depth >= depth && entry.NodeType == NodeConstants.CUT && ttEval >= upperBound)
                {
                    return ttEval;
                } else if (entry.Depth >= depth && entry.NodeType == NodeConstants.ALL && ttEval <= lowerBound)
                {
                    return ttEval;
                } else
                {
                    PredictedBestMove = entry.BestMove;
                }
            }

            if (depth == 0)
            {
                return SearchCaptures(boardPosition, lowerBound, upperBound);
            }
            EvaluatedPositions++;
            Span<Move> MoveList = stackalloc Move[MoveGenConstants.MAX_POSSIBLE_LEGAL_MOVES];
            Byte MoveCount = MoveGeneration.GenerateAllPseudoLegalMoves(boardPosition, MoveList);

            bool HasLegalMove = false;
            //fix at some point
            Move BestMove = new Move();

            Span<int> MoveValueList = stackalloc int[MoveCount];
            MoveOrdering.GenerateMoveValues(boardPosition, MoveValueList, MoveList, MoveCount, PredictedBestMove, DepthFromRoot);

            int BestRelativeEval = EvaluationConstants.ABSOLUTE_LOWER_BOUND;

            //check to see if we can use futility pruning
            bool FutilityPruningPossible = true;

            if (boardPosition.CheckCount >0 || lowerBound <= EvaluationConstants.CHECKMATE_MINIMAL_EVAL || upperBound >= -EvaluationConstants.CHECKMATE_BASE_EVAL)
            {
                FutilityPruningPossible = false;
            }
            for (int i = 0; i < MoveCount; i++)
            {
                Move move = MoveOrdering.GetNextMove(MoveList, MoveCount, MoveValueList, i);
                if (boardPosition.IsLegal(move) == false)
                {
                    continue;
                }
                HasLegalMove = true;
                UnMove Unmove = boardPosition.MakeMove(move);

                //futility pruning
                if (FutilityPruningPossible && -Evaluation.GetRelativeEvaluation(boardPosition) < (lowerBound - 300) && boardPosition.CheckCount == 0 && (move.moveInfo & 4) != 4)
                {
                    boardPosition.UnMakeMove(Unmove);
                    continue;
                }

                PositionRelativeEval = -GetEval(boardPosition, (byte)(depth - 1), -upperBound, -lowerBound);
                boardPosition.UnMakeMove(Unmove);


                if (SearchCancelled)
                {
                    return 0;
                }

                if (PositionRelativeEval >= upperBound)
                {
                    int StoreEval = PositionRelativeEval;

                    // Adjust score before storing if it's checkmate
                    if (StoreEval > -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                    {
                        StoreEval += DepthFromRoot;
                    } else if (StoreEval < EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                    {
                        StoreEval -= DepthFromRoot;
                    }

                    TranspositionTable.StorePosition(boardPosition.ZobristHash, move, depth, StoreEval, NodeConstants.CUT);
                    KillerMoves.AddMove(move, DepthFromRoot);
                    return PositionRelativeEval;
                } else if (PositionRelativeEval > BestRelativeEval)
                {
                    //move gives better position!
                    BestMove = move;
                    BestRelativeEval = PositionRelativeEval;
                    
                }
                if (PositionRelativeEval > lowerBound)
                {
                    lowerBound = PositionRelativeEval;
                }
            }
            if (HasLegalMove == false)
            {
                if (boardPosition.CheckCount == 0)
                {
                    //stalemate
                    return 0;
                } else
                {
                    //checkmate
                    return EvaluationConstants.CHECKMATE_BASE_EVAL + DepthFromRoot;
                }
            }
            int storeEval = BestRelativeEval;

            // Adjust score before storing if it's checkmate
            if (storeEval > -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
            {
                storeEval += DepthFromRoot;
            } else if (storeEval < EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
            {
                storeEval -= DepthFromRoot;
            }

            if (BestRelativeEval > OriginalLowerBound)
            {
                TranspositionTable.StorePosition(boardPosition.ZobristHash, BestMove, depth, storeEval, NodeConstants.PV);
            } else
            {
                //no move is higher than lower bound. Thus this is an all-node
                TranspositionTable.StorePosition(boardPosition.ZobristHash, BestMove, depth, storeEval, NodeConstants.ALL);
            }
            return BestRelativeEval;
            //the position's evaluation is going the be the result of the best move the opponent can make
        }
        public static int SearchCaptures(Position boardPosition, int lowerBound, int upperBound)
        {
            TimeManager.CheckTime();

            if (SearchCancelled)
            {
                return 0;
            }

            int PositionEvaluation = Evaluation.GetRelativeEvaluation(boardPosition);
            EvaluatedPositions++;

            //null move observatrion
            if (PositionEvaluation >= upperBound) {
                return PositionEvaluation; } //position is too good. Opponent will avoid

            Span<Move> MoveList = stackalloc Move[MoveGenConstants.MAX_POSSIBLE_LEGAL_MOVES];
            Byte MoveCount = MoveGeneration.GenerateAllPseudoLegalMoves(boardPosition, MoveList, true);

            if (MoveCount == 0)
            {
                return PositionEvaluation;
            }

            Span<int> MoveValueList = stackalloc int[MoveCount];
            MoveOrdering.GenerateMoveValues(boardPosition, MoveValueList, MoveList, MoveCount, new Move(), 100);

            bool FoundLegalMove = false;

            //not all captures are forced, we only want to look at those which lead to a better position. Assume that the base evaluation is the evaluation of a "quiet" move
            //null move observation
            lowerBound = Math.Max(lowerBound, PositionEvaluation);
            
            for (int i = 0; i < MoveCount; i++)
            {
                Move move = MoveOrdering.GetNextMove(MoveList, MoveCount, MoveValueList, i);
                //delta pruning
                ushort PositionEvalIncrease = Evaluation.PieceTypeToPieceValue[PieceColors.GetColorlessPiece(boardPosition.UshortBoard[move.endPos])];
                ushort DeltaPruningSafetyMargin = 300;
                if ((move.moveInfo & 8) == 8)
                {
                    //promotion add promotion value 
                    PositionEvalIncrease += Evaluation.PieceTypeToPieceValue[Piece.QUEEN];
                }

                if (PositionEvaluation + PositionEvalIncrease + DeltaPruningSafetyMargin < lowerBound)
                {
                    //even after capture this position is still not good enough, ignore this move
                    continue;
                }
                if (boardPosition.IsLegal(move) == false)
                {
                    continue;
                } else
                {
                    FoundLegalMove = true;
                }

                UnMove Unmove = boardPosition.MakeMove(move);
                int Eval = -SearchCaptures(boardPosition, -upperBound, -lowerBound);
                boardPosition.UnMakeMove(Unmove);

                if (Eval >= upperBound)
                {
                    return upperBound;
                }
                if (Eval > lowerBound)
                {
                    lowerBound = Eval;
                }
            }
            if (FoundLegalMove == false)
            {
                return PositionEvaluation;
            }
            return lowerBound;
        }
        public static void CancelSearch()
        {
            SearchCancelled = true;
        }

    }
}