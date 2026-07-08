using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace TOML
{
    public class Benchmark
    {
        private static Stopwatch stopWatch = new Stopwatch();
        public static bool PerftTest()
        {
            bool Correct = true;
            stopWatch.Restart();
            stopWatch.Start();


            Position TestPosition = new Position("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -");

            Correct &= PerftTest(TestPosition, 5, 193690690);

            TestPosition = new Position("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");

            Correct &= PerftTest(TestPosition, 6, 11030083);

            TestPosition = new Position("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");

            Correct &= PerftTest(TestPosition, 5, 15833292);

            TestPosition = new Position("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
            Console.WriteLine(TestPosition.CurrentPly);
            Correct &= PerftTest(TestPosition, 5, 89941194);

            TestPosition = new Position("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");

            Correct &= PerftTest(TestPosition, 4, 3894594);

            TestPosition = new Position("rnk2bnr/1p4pp/2p1Qp2/8/4N3/2N1qP2/PP4PP/3R1R1K b - - 0 1");

            Correct &= PerftTest(TestPosition, 4, 171795);

            TestPosition = new Position("r1bq1b1r/3pnk1p/n2Ppp2/P1P5/1BPNN1p1/1Q4Pp/5P1P/RR4K1 w - - 1 2");

            Correct &= PerftTest(TestPosition, 4, 1148820);

            TestPosition = new Position("8/8/8/1pb2k2/p7/8/1BK5/1R6 b - - 1 48");

            Correct &= PerftTest(TestPosition, 6, 50138232);

            TestPosition = new Position("8/8/4k3/8/2p5/8/B2P2K1/8 w - - 0 1");

            Correct &= PerftTest(TestPosition, 6, 1015133);

            TestPosition = new Position("8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1");

            Correct &= PerftTest(TestPosition, 6, 1440467);

            TestPosition = new Position("5k2/8/8/8/8/8/8/4K2R w K - 0 1");

            Correct &= PerftTest(TestPosition, 6, 661072);

            TestPosition = new Position("3k4/8/8/8/8/8/8/R3K3 w Q - 0 1");

            Correct &= PerftTest(TestPosition, 6, 803711);

            TestPosition = new Position("r3k2r/1b4bq/8/8/8/8/7B/R3K2R w KQkq - 0 1");

            Correct &= PerftTest(TestPosition, 5, 31912360);

            TestPosition = new Position("r3k2r/8/3Q4/8/8/5q2/8/R3K2R b KQkq - 0 1");

            Correct &= PerftTest(TestPosition, 5, 58773923);

            TestPosition = new Position("2K2r2/4P3/8/8/8/8/8/3k4 w - - 0 1");

            Correct &= PerftTest(TestPosition, 6, 3821001);

            TestPosition = new Position("8/8/1P2K3/8/2n5/1q6/8/5k2 b - - 0 1");

            Correct &= PerftTest(TestPosition, 5, 1004658);

            TestPosition = new Position("4k3/1P6/8/8/8/8/K7/8 w - - 0 1");

            Correct &= PerftTest(TestPosition, 6, 217342);

            TestPosition = new Position("8/P1k5/K7/8/8/8/8/8 w - - 0 1");

            Correct &= PerftTest(TestPosition, 8, 8110830);

            TestPosition = new Position("K1k5/8/P7/8/8/8/8/8 w - - 0 1");

            Correct &= PerftTest(TestPosition, 10, 5966690);

            TestPosition = new Position("8/k1P5/8/1K6/8/8/8/8 w - - 0 1");

            Correct &= PerftTest(TestPosition, 7, 567584);

            TestPosition = new Position("8/8/2k5/5q2/5n2/8/5K2/8 b - - 0 1");

            Correct &= PerftTest(TestPosition, 6, 3114998);

            //TestPosition = new Position("8/8/8/8/8/8/8/K6k w - - 99 1");

            //Correct &= PerftTest(TestPosition, 5, 10898);

            Console.WriteLine("Took " + (float)stopWatch.ElapsedMilliseconds / 1000);
            if (Correct == true)
            {
                Console.WriteLine("Perft Test Passed!");
            } else
            {
                Console.WriteLine("Perft Test failed :(");
            }
            return Correct;
        }
        public static bool FullPerftTest()
        {
            return true;
        }
        public static bool PerftTest(Position position, int depth, int expectedResult)
        {
            MoveGeneration moveGen = new MoveGeneration(position);
            int GivenResult = moveGen.GetTotalMoveCombinationCount(depth);
            if (GivenResult == expectedResult)
            {
                Console.WriteLine("At depth " + depth + " Got " + GivenResult + " Success");
            } else
            {
                Console.WriteLine("At depth " + depth + " Got " + GivenResult + " expected " + expectedResult);
            }
            return (GivenResult == expectedResult);
        }
    }
}
