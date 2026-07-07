using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TOML
{
    static class TimeManager
    {
        static Stopwatch stopWatch = new Stopwatch();
        static int Counter;
        static int TimeLimit;

        public static void SetTimeLimit(int timeLimit)
        {
            TimeLimit = timeLimit;
        }

        public static void StartStopWatch()
        {
            stopWatch.Restart();
        }
        public static void CheckTime()
        {
            Counter++;
            if ((Counter & 2047) == 2047) {
                Counter = 0;
                if (stopWatch.ElapsedMilliseconds >= TimeLimit)
                {
                    Search.CancelSearch();
                }
            }
        }
    }
}
