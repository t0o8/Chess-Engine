using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TOML
{
    public class Search
    {
        public static int branches = 0;
        public static int perftcount = 0;
        public static int EvaluatedPositions = 0;
        public static int TranspositionsUsed = 0;

        public static bool SearchCancelled = false;
        static DateTime SearchStartTime = System.DateTime.Now;


        //time limit is in miliseconds
        public static Move BeginSearch(Position boardPositionReference, int timeLimit)
        {
            TimeManager.StartStopWatch();
            TimeManager.SetTimeLimit(timeLimit);

            byte Depth = 1;
            SearchStartTime = System.DateTime.Now;
            SearchCancelled = false;

            Position boardPosition = new Position(boardPositionReference);

            MoveGeneration MoveGen = new MoveGeneration(boardPosition);
            Move[] MoveArray = MoveGen.GenerateAllLegalMoves();

            if (MoveArray.Length == 0)
            {
                return new Move();
            }

            MoveOrdering MoveOrder = new MoveOrdering(MoveArray, boardPosition);
            Move BestMove = MoveArray[0];
            //iterative deepening
            while (SearchCancelled == false)
            {
                int lowerBound = EvaluationConstants.ABSOLUTE_LOWER_BOUND;
                int upperBound = EvaluationConstants.ABSOLUTE_UPPER_BOUND;

                int PositionRelativeEval = lowerBound;

                Move BestMoveThisDepth = MoveArray[0];

                MoveOrder.OrderMoves(BestMove);

                for (int i = 0; i < MoveArray.Length; i++)
                {
                    Move move = MoveOrder.GetNextMove();

                    //since we return 0 when search is cancelled we must check that the eval is from a search that isn't cancelled
                    if (SearchCancelled == true)
                    {
                        return BestMove;
                    }

                    UnMove Unmove = boardPosition.MakeMove(move);
                    PositionRelativeEval = -GetEval(boardPosition, Depth, -upperBound, -lowerBound);

                    boardPosition.UnMakeMove(Unmove);

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
                    return BestMove;
                }
                BestMove = BestMoveThisDepth;
                Console.WriteLine("serached to depth: " + Depth + " bestmove = " + Position.NumberToBoardLetter(BestMove.startPos) + Position.NumberToBoardLetter(BestMove.endPos) + "eval: " + lowerBound);
                Depth++;
                TranspositionTable.StorePosition(boardPosition.ZobristHash, BestMove, Depth, lowerBound, NodeConstants.PV);
                if (Depth == byte.MaxValue)
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
            int PositionRelativeEval = lowerBound;
            int OriginalLowerBound = lowerBound;
            Move PredictedBestMove = new Move();

            if (TranspositionTable.ContainsTransposition(boardPosition.ZobristHash))
            {
                TranspositionEntry entry = TranspositionTable.GetTransposition(boardPosition.ZobristHash);
                TranspositionsUsed++;
                if (entry.Depth >= depth && entry.NodeType == NodeConstants.PV)
                {
                    return entry.Eval;
                } else
                {
                    if (entry.Depth >= depth && entry.NodeType == NodeConstants.CUT && entry.Eval > upperBound)
                    {
                        return entry.Eval;
                    }
                    PredictedBestMove = entry.BestMove;
                } 
            }

            MoveGeneration MoveGen = new MoveGeneration(boardPosition);
            Move[] MoveArray = MoveGen.GenerateAllLegalMoves();

            if (MoveArray.Length == 0)
            {
                if (boardPosition.CheckCount == 0)
                {
                    //stalemate
                    return 0;
                } else
                {
                    //checkmate
                    return EvaluationConstants.CHECKMATE_BASE_EVAL - depth;
                }
            }
            if (boardPosition.FoundRepetition(boardPosition.ZobristHash) == true)
            {
                //draw by repetition
                return 0;
            }

            Move BestMove = MoveArray[0];

            if (depth == 0)
            {
                return SearchCaptures(boardPosition, lowerBound, upperBound);
            }

            MoveOrdering MoveOrder = new MoveOrdering(MoveArray, boardPosition);
            MoveOrder.OrderMoves(PredictedBestMove);

            int BestRelativeEval = EvaluationConstants.ABSOLUTE_LOWER_BOUND;

            for (int i = 0; i < MoveArray.Length; i++)
            {
                Move move = MoveOrder.GetNextMove();
                UnMove Unmove = boardPosition.MakeMove(move);

                PositionRelativeEval = -GetEval(boardPosition, (byte)(depth - 1), -upperBound, -lowerBound);
                boardPosition.UnMakeMove(Unmove);

                if (SearchCancelled)
                {
                    SearchCancelled = true;
                    return 0;
                }

                if (PositionRelativeEval >= upperBound)
                {
                    TranspositionTable.StorePosition(boardPosition.ZobristHash, move, depth, PositionRelativeEval, NodeConstants.CUT);
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
            if (BestRelativeEval > OriginalLowerBound)
            {
                TranspositionTable.StorePosition(boardPosition.ZobristHash, BestMove, depth, BestRelativeEval, NodeConstants.PV);
            } else
            {
                //no move is higher than lower bound. Thus this is an all-node
                TranspositionTable.StorePosition(boardPosition.ZobristHash, BestMove, depth, BestRelativeEval, NodeConstants.ALL);
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

            MoveGeneration MoveGen = new MoveGeneration(boardPosition);
            Move[] MoveArray = MoveGen.GenerateAllLegalMoves(true);

            int PositionEvaluation = Evaluation.GetRelativeEvaluation(boardPosition);
            EvaluatedPositions++;

            //null move observatrion
            if (PositionEvaluation >= upperBound) { return PositionEvaluation; } //position is too good. Opponent will avoid

            if (MoveArray.Length == 0)
            {

                return PositionEvaluation;
            }

            MoveOrdering MoveOrder = new MoveOrdering(MoveArray, boardPosition);

            //not all captures are forced, we only want to look at those which lead to a better position. Assume that the base evaluation is the evaluation of a "quiet" move
            lowerBound = Math.Max(lowerBound, PositionEvaluation);
            foreach (Move move in MoveOrder.GetOrderedMoves())
            {
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
            return lowerBound;
        }
        public static void CancelSearch()
        {
            SearchCancelled = true;
        }

    }
}