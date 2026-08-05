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

            Span<Move> moveList = stackalloc Move[MoveGenConstants.MAX_POSSIBLE_LEGAL_MOVES];
            byte moveCount = MoveGeneration.GenerateAllPseudoLegalMoves(boardPosition, moveList);


            if (moveCount == 0)
            {
                return new Move();
            }

            Span<int> moveValueList = stackalloc int[moveCount];
            
            Move bestMove = moveList[0];
            //iterative deepening
            while (SearchCancelled == false)
            {

                int lowerBound = EvaluationConstants.ABSOLUTE_LOWER_BOUND;
                int upperBound = EvaluationConstants.ABSOLUTE_UPPER_BOUND;

                int positionRelativeEval = lowerBound;

                Move bestMoveThisDepth = bestMove;

                MoveOrdering.GenerateMoveValues(boardPosition, moveValueList, moveList, moveCount, bestMove, CurrentSearchDepth);

                for (int i = 0; i < moveCount; i++)
                {
                    Move move = MoveOrdering.GetNextMove(moveList, moveCount, moveValueList, i);
                    if (boardPosition.IsLegal(move) == false)
                    {
                        continue;
                    }
                    UnMove unmove = boardPosition.MakeMove(move);
                    positionRelativeEval = -GetEval(boardPosition, (byte)(CurrentSearchDepth - 1), -upperBound, -lowerBound);
                    boardPosition.UnMakeMove(unmove);
                    //since we return 0 when search is cancelled we must check that the eval is from a search that isn't cancelled
                    if (SearchCancelled == true)
                    {
                        //if we fix the previous best move as the first move then any changes would be "better" at the new depth,
                        //since we've realized the previous move wasn't as good as we thought it to be 
                        return bestMoveThisDepth;
                    }

                    if (positionRelativeEval > lowerBound)
                    {
                        //move gives better position!
                        bestMoveThisDepth = move;
                        //the lower bound should be best opponent so far. Ignore moves that are worse since we want the best position possible
                        lowerBound = positionRelativeEval;
                    }
                }
                if (SearchCancelled == true)
                {
                    return bestMoveThisDepth;
                }
                bestMove = bestMoveThisDepth;
                int NPS = (int)((long)EvaluatedPositions / (long)Math.Max(NPSStopWatch.ElapsedMilliseconds, 1) * 1000);

                if (lowerBound >= -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    Console.WriteLine("info depth " + CurrentSearchDepth + " score mate " + Math.Ceiling((decimal)(-lowerBound - EvaluationConstants.CHECKMATE_BASE_EVAL)/2).ToString() + " nodes " + EvaluatedPositions + " nps " + NPS);
                } else if (lowerBound <= EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    Console.WriteLine("info depth " + CurrentSearchDepth + " score mate " + Math.Ceiling((decimal)(lowerBound - EvaluationConstants.CHECKMATE_BASE_EVAL)/2).ToString() + " nodes " + EvaluatedPositions +" nps " + NPS);
                } else 
                {
                    Console.WriteLine("info depth " + CurrentSearchDepth + " score cp " + lowerBound.ToString() + " nodes " + EvaluatedPositions +" nps " + NPS); //+ Position.NumberToBoardLetter(bestMove.startPos) + Position.NumberToBoardLetter(bestMove.endPos)
                }
                TranspositionTable.StorePosition(boardPosition.ZobristHash, bestMove, CurrentSearchDepth, lowerBound, NodeConstants.PV);
                CurrentSearchDepth++;
                if (CurrentSearchDepth == byte.MaxValue)
                {
                    return bestMove;
                }
            }
            return bestMove;
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

            int depthFromRoot = CurrentSearchDepth - depth + 1;
            int positionRelativeEval = lowerBound;
            int originalLowerBound = lowerBound;
            Move predictedbestMove = new Move();

            ref TranspositionEntry entry = ref TranspositionTable.GetTransposition(boardPosition.ZobristHash);
            if (entry.ZobristKey == boardPosition.ZobristHash)
            {
                TranspositionsUsed++;
                int ttEval = entry.Eval;
                if (ttEval > -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    ttEval -= depthFromRoot;
                } else if (ttEval < EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                {
                    ttEval += depthFromRoot;
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
                    predictedbestMove = entry.BestMove;
                }
            }

            if (depth == 0)
            {
                return SearchCaptures(boardPosition, lowerBound, upperBound);
            }
            EvaluatedPositions++;
            Span<Move> moveList = stackalloc Move[MoveGenConstants.MAX_POSSIBLE_LEGAL_MOVES];
            Byte moveCount = MoveGeneration.GenerateAllPseudoLegalMoves(boardPosition, moveList);

            bool hasLegalMove = false;
            //fix at some point
            Move bestMove = new Move();

            Span<int> moveValueList = stackalloc int[moveCount];
            MoveOrdering.GenerateMoveValues(boardPosition, moveValueList, moveList, moveCount, predictedbestMove, depthFromRoot);

            int bestRelativeEval = EvaluationConstants.ABSOLUTE_LOWER_BOUND;

            //check to see if we can use futility pruning
            bool futilityPruningPossible = true;

            if (boardPosition.CheckCount >0 || lowerBound <= EvaluationConstants.CHECKMATE_MINIMAL_EVAL || upperBound >= -EvaluationConstants.CHECKMATE_BASE_EVAL)
            {
                futilityPruningPossible = false;
            }
            for (int i = 0; i < moveCount; i++)
            {
                Move move = MoveOrdering.GetNextMove(moveList, moveCount, moveValueList, i);
                if (boardPosition.IsLegal(move) == false)
                {
                    continue;
                }
                hasLegalMove = true;
                UnMove unmove = boardPosition.MakeMove(move);

                //futility pruning
                if (futilityPruningPossible && -Evaluation.GetRelativeEvaluation(boardPosition) < (lowerBound - 300) && boardPosition.CheckCount == 0 && (move.MoveInfo & 4) != 4)
                {
                    boardPosition.UnMakeMove(unmove);
                    continue;
                }

                positionRelativeEval = -GetEval(boardPosition, (byte)(depth - 1), -upperBound, -lowerBound);
                boardPosition.UnMakeMove(unmove);


                if (SearchCancelled)
                {
                    return 0;
                }

                if (positionRelativeEval >= upperBound)
                {
                    int cutNodeStoreEval = positionRelativeEval;

                    // Adjust score before storing if it's checkmate
                    if (cutNodeStoreEval > -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                    {
                        cutNodeStoreEval += depthFromRoot;
                    } else if (cutNodeStoreEval < EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
                    {
                        cutNodeStoreEval -= depthFromRoot;
                    }

                    TranspositionTable.StorePosition(boardPosition.ZobristHash, move, depth, cutNodeStoreEval, NodeConstants.CUT);
                    KillerMoves.AddMove(move, depthFromRoot);
                    return positionRelativeEval;
                } else if (positionRelativeEval > bestRelativeEval)
                {
                    //move gives better position!
                    bestMove = move;
                    bestRelativeEval = positionRelativeEval;
                    
                }
                if (positionRelativeEval > lowerBound)
                {
                    lowerBound = positionRelativeEval;
                }
            }
            if (hasLegalMove == false)
            {
                if (boardPosition.CheckCount == 0)
                {
                    //stalemate
                    return 0;
                } else
                {
                    //checkmate
                    return EvaluationConstants.CHECKMATE_BASE_EVAL + depthFromRoot;
                }
            }
            int pvNodeStoreEval = bestRelativeEval;

            // Adjust score before storing if it's checkmate
            if (pvNodeStoreEval > -EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
            {
                pvNodeStoreEval += depthFromRoot;
            } else if (pvNodeStoreEval < EvaluationConstants.CHECKMATE_MINIMAL_EVAL)
            {
                pvNodeStoreEval -= depthFromRoot;
            }

            if (bestRelativeEval > originalLowerBound)
            {
                TranspositionTable.StorePosition(boardPosition.ZobristHash, bestMove, depth, pvNodeStoreEval, NodeConstants.PV);
            } else
            {
                //no move is higher than lower bound. Thus this is an all-node
                TranspositionTable.StorePosition(boardPosition.ZobristHash, bestMove, depth, pvNodeStoreEval, NodeConstants.ALL);
            }
            return bestRelativeEval;
            //the position's evaluation is going the be the result of the best move the opponent can make
        }
        public static int SearchCaptures(Position boardPosition, int lowerBound, int upperBound)
        {
            TimeManager.CheckTime();

            if (SearchCancelled)
            {
                return 0;
            }

            int positionEvaluation = Evaluation.GetRelativeEvaluation(boardPosition);
            EvaluatedPositions++;

            //null move observatrion
            if (positionEvaluation >= upperBound) {
                return positionEvaluation; } //position is too good. Opponent will avoid

            Span<Move> moveList = stackalloc Move[MoveGenConstants.MAX_POSSIBLE_LEGAL_MOVES];
            Byte moveCount = MoveGeneration.GenerateAllPseudoLegalMoves(boardPosition, moveList, true);

            if (moveCount == 0)
            {
                return positionEvaluation;
            }

            Span<int> moveValueList = stackalloc int[moveCount];
            MoveOrdering.GenerateMoveValues(boardPosition, moveValueList, moveList, moveCount, new Move(), 100);

            bool foundLegalMove = false;

            //not all captures are forced, we only want to look at those which lead to a better position. Assume that the base evaluation is the evaluation of a "quiet" move
            //null move observation
            lowerBound = Math.Max(lowerBound, positionEvaluation);
            
            for (int i = 0; i < moveCount; i++)
            {
                Move move = MoveOrdering.GetNextMove(moveList, moveCount, moveValueList, i);
                //delta pruning
                ushort positionEvalIncrease = Evaluation.PieceTypeToPieceValue[PieceHelper.GetPieceWithoutColor(boardPosition.UshortBoard[move.EndPos])];
                if ((move.MoveInfo & 8) == 8)
                {
                    //promotion add promotion value 
                    positionEvalIncrease += Evaluation.PieceTypeToPieceValue[Piece.QUEEN];
                }

                if (positionEvaluation + positionEvalIncrease + SearchConstants.DELTA_PRUNING_SAFETY_MARGIN < lowerBound)
                {
                    //even after capture this position is still not good enough, ignore this move
                    continue;
                }
                if (boardPosition.IsLegal(move) == false)
                {
                    continue;
                } else
                {
                    foundLegalMove = true;
                }

                UnMove unmove = boardPosition.MakeMove(move);
                int eval = -SearchCaptures(boardPosition, -upperBound, -lowerBound);
                boardPosition.UnMakeMove(unmove);

                if (eval >= upperBound)
                {
                    return upperBound;
                }
                if (eval > lowerBound)
                {
                    lowerBound = eval;
                }
            }
            if (foundLegalMove == false)
            {
                return positionEvaluation;
            }
            return lowerBound;
        }
        public static void CancelSearch()
        {
            SearchCancelled = true;
        }

    }
}