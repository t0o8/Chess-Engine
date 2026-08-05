using System.Collections;
using System.Collections.Generic;
using TOML;

namespace TOML{
    public struct TranspositionEntry
    {
        public ulong ZobristKey;
        public Move BestMove;
        public byte Depth;
        public int Eval;
        public byte NodeType; //0 = exact, 1 upper bound, 2 lowerbound


        public TranspositionEntry(ulong zobristKey, Move bestMove, byte depth, int eval, byte nodeType)
        {
            ZobristKey = zobristKey;
            BestMove = bestMove;
            Depth = depth;
            Eval = eval;
            NodeType = nodeType;
        }
    }
    public static class TranspositionTable
    {
        private const int TABLE_SIZE = 1048576;
        

        private static readonly TranspositionEntry[] Table = new TranspositionEntry[TABLE_SIZE];

        public static void StorePosition(ulong zobristKey, Move bestMove, byte depth, int eval, byte nodeType)
        {
            int Index = GetIndex(zobristKey);
            if (Table[Index].Depth < depth)
            {
                //don't override positions searched at a shallower depth
                Table[Index] = new TranspositionEntry(zobristKey, bestMove, depth, eval, nodeType);
            }
        }
        public static bool ContainsTransposition(ulong zobristKey)
        {
            if (Table[GetIndex(zobristKey)].ZobristKey == zobristKey)
            {
                return true;
            }
            return false;
        }
        public static ref TranspositionEntry GetTransposition(ulong zobristKey)
        {
            return ref Table[GetIndex(zobristKey)];
        }
        static int GetIndex(ulong zobristKey)
        {
            return (int)(zobristKey & (TABLE_SIZE - 1));
        }
        public static int GetSize()
        {
            int size = 0;
            foreach (TranspositionEntry Entry in Table)
            {
                if (Entry.Depth != 0)
                {
                    size++;
                }
            }
            return size;
        }
    }
}