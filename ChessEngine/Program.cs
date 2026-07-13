// See https://aka.ms/new-console-template for more information


using System.Numerics;
using TOML;

Main.Start();

while (true)
{
    string Msg = Console.ReadLine();

    if (string.IsNullOrEmpty(Msg)) continue;

    string[] SplitString = Msg.Split(" ");
    

    string Cmd = SplitString[0];

    switch (Cmd)
    {
        case "quit":
            Search.CancelSearch();
            return;
            break;
        case "uci":
            Console.WriteLine("id name SkibEngine V2.0");
            Console.WriteLine("id author Jason Jiang");
            Console.WriteLine("uciok");
            break;
        case "position":

            int movesIndex = Array.IndexOf(SplitString, "moves");

            if (SplitString.Length > 1 && SplitString[1] == "startpos")
            {
                Main.SetupPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            } else if (SplitString.Contains("fen"))
            {
                int fenStartIndex = Array.IndexOf(SplitString, "fen") + 1;
                int fenEndIndex = movesIndex != -1 ? movesIndex : SplitString.Length;
                string fen = string.Join(" ", SplitString[fenStartIndex..fenEndIndex]);
                Main.SetupPosition(fen);
            }
            if (movesIndex != -1)
            {
                for (int i = movesIndex + 1; i < SplitString.Length; i++)
                {
                    int StartPos = BoardPositionHelper.NumLetterToBoardPos(SplitString[i][0].ToString() + SplitString[i][1].ToString());
                    int EndPos = BoardPositionHelper.NumLetterToBoardPos(SplitString[i][2].ToString() + SplitString[i][3].ToString());

                    Move move;

                    if (SplitString[i].Length > 4)
                    {
                        //promotion
                        move = new Move((ushort)StartPos, (ushort)EndPos).GetMoveWithMoveInfoPromotion(Main.position, SplitString[i][4]);
                    } else
                    {
                        move = new Move((ushort)StartPos, (ushort)EndPos).GetMoveWithMoveInfo(Main.position);
                    }

                    Main.position.MakeMove(move);
                }
            }
            break;
        case "isready":
            Console.WriteLine("readyok");
            break;
        case "go":
            //no ponder support yet
            if (SplitString.Contains("ponder"))
            {
                break;
            }
            for (int i = 1; i < SplitString.Length; i++)
            {
                if (SplitString[i] == "wtime")
                {
                    Main.WhiteTime = int.Parse(SplitString[i + 1]);
                    i++;
                } else if (SplitString[i] == "btime")
                {
                    Main.BlackTime = int.Parse(SplitString[i + 1]);
                    i++;
                } else if (SplitString[i] == "movetime")
                {
                    Main.SearchTime = int.Parse(SplitString[i + 1]);
                    i++;
                }
            }

            await Task.Run(() =>
            {
                Move FoundMove = Main.BeginSearch();
                if (FoundMove.moveInfo > 7)
                {
                    char PromotionAddOn;
                    
                    if ((FoundMove.moveInfo & 3) == 0)
                    {
                        PromotionAddOn = 'n';
                    } else if ((FoundMove.moveInfo & 3) == 1)
                    {
                        PromotionAddOn = 'b';
                    } else if ((FoundMove.moveInfo & 3) == 2)
                    {
                        PromotionAddOn = 'r';
                    } else
                    {
                        PromotionAddOn = 'q';
                    }
                    Console.WriteLine("bestmove " + Position.NumberToBoardLetter(FoundMove.startPos) + Position.NumberToBoardLetter(FoundMove.endPos) + PromotionAddOn);
                } else
                {
                    Console.WriteLine("bestmove " + Position.NumberToBoardLetter(FoundMove.startPos) + Position.NumberToBoardLetter(FoundMove.endPos));
                }
            });
            break;
        case "perfttest":
            Benchmark.PerftTest();
            break;
    }
}

