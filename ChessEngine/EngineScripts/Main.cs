using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TOML
{
    public class Main
    {

        public static int WhiteTime;
        public static int BlackTime;

        public static int SearchTime = 0;


        public static Position position;
        // Start is called before the first frame update
        public static void Start()
        {
            PrecomputedInfo.PrecomputeInfo();
            ZobristHashing.GeneratePseudoRandomNumbers();

            Console.WriteLine("engine ready");

        }
        public static void SetupPosition(string fenString)
        {
            position = new Position(fenString);
        }
        public static Move BeginSearch()
        {
            int TimeLeft;
            if (SearchTime != 0)
            {
                return Search.BeginSearch(position, (int)(SearchTime * 0.95f));
            } else
            {
                if (position.ActiveColor == Piece.WHITE)
                {
                    TimeLeft = WhiteTime;
                } else
                {
                    TimeLeft = BlackTime;
                }
                return Search.BeginSearch(position, TimeLeft / 20);
            }
        }
    }
}


