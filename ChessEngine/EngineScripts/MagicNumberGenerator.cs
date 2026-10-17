using System;
using System.Collections;
using System.Collections.Generic;
using TOML;

namespace TOML {
    public class MagicNumberGenerator
    {
        public static bool RunGeneration = false;
        public static ulong[] QueenMagicNumbers = new ulong[64];
        public static ulong[] BishopMagicNumbers = { 567365517705248, 565166710329360, 1127008043008516, 573953726750720, 299342040662016, 143006304829444, 71485978847284, 18709146126464, 4417445430282, 2207747476737, 4402375163936, 2242006487428, 1169304846336, 558618398720, 283736547648, 208440657921, 1125934535280649, 562967267640321, 281483700994434, 140754938888192, 140892117698560, 140750641733632, 70370959040560, 35186553651728, 571783092700176, 285873294026760, 37383664042056, 285873157703696, 145136751288320, 70644259688472, 141287613427712, 70647950737920, 286493648101376, 145204273283593, 21998840318081, 4415226904864, 71471477293248, 141295834169600, 283682900541696, 141295834169600, 143005500588544, 71488388997184, 13197494977536, 120863131648, 4505455371264, 282578800285184, 565167297397016, 282578800285184, 71485978847284, 36318278141960, 139654594560, 545783809, 17742299136, 4415371214856, 1130332447375744, 565166710329360, 18709146126464, 208440657921, 554174598, 2131968, 136380672, 17247543808, 4417445430282, 567365517705248 };
        public static ulong[] RookMagicNumbers = { 36028945266638992, 18014673924263936, 36063981659521160, 36037593188532352, 36033195100602496, 36030996780449793, 36029896673198592, 72059243863277578, 140738029420672, 70369282098240, 140806243484033, 140771851239552, 140789053130752, 562958544405513, 141287277732096, 140741783349376, 36833648001026, 72568338710914, 18692773511424, 388127873051904, 141287378387968, 282575067938824, 9899899748356, 2199027613980, 70370891670144, 70370893758848, 17602929754240, 8798248898560, 4400194519168, 2201172574336, 2491082082321, 4423816355921, 35459325493376, 35186527977476, 35185630384384, 8798248898560, 13196328962048, 39651272376352, 17597084992008, 277595821057, 70379481628708, 70506451582976, 35735235330064, 563535143895072, 8796160163968, 3940958979358736, 9932380569602, 277109407748, 35459325493376, 35208132821632, 17594402734464, 8864829284608, 4398986068096, 2201172574336, 1683728892928, 279182576128, 38491769634817, 35768760271107, 8933549867075, 9501021343745, 563019212130314, 281509403558401, 829197256740, 143093137538 };

        public static int GetBishopFoundMagicNum()
        {
            int num = 0;
            for (int i = 0; i < 64; i++)
            {
                if (BishopMagicNumbers[i] != 0)
                {
                    num++;
                }
            }
            return num;
        }
        public static int GetRookFoundMagicNum()
        {
            int num = 0;
            for (int i = 0; i < 64; i++)
            {
                if (RookMagicNumbers[i] != 0)
                {
                    num++;
                }
            }
            return num;
        }
        public static void StopGeneration()
        {
            RunGeneration = false;
        }
        public static void StartMagicNumberGeneration()
        {
            System.Random rnd = new System.Random();
            RunGeneration = true;
            while (RunGeneration)
            {
                byte[] buffer = new byte[8];
                rnd.NextBytes(buffer);
                ulong Rnd1 = BitConverter.ToUInt64(buffer, 0);
                rnd.NextBytes(buffer);
                ulong Rnd2 = BitConverter.ToUInt64(buffer, 0);
                rnd.NextBytes(buffer);
                ulong Rnd3 = BitConverter.ToUInt64(buffer, 0);
                ulong AttemptNum = Rnd1 & Rnd2 & Rnd3;
                for (int AttemptSquare = 0; AttemptSquare < 64; AttemptSquare++)
                {
                    bool Passed = true;
                    bool[] UniqueIndexs = new bool[GetBlockerMaskSize(PrecomputedInfo.BishopBlockingMasks[AttemptSquare])];

                    for (ulong BlockerIndex = 0; BlockerIndex < GetBlockerMaskSize(PrecomputedInfo.BishopBlockingMasks[AttemptSquare]); BlockerIndex++)
                    {
                        
                        ulong AttemptIndex = (GetBlockerMask(PrecomputedInfo.BishopBlockingMasks[AttemptSquare], BlockerIndex) * AttemptNum) >> (int)(64 - PrecomputedInfo.BishopBlockingMaskCount[AttemptSquare]);
                        if (UniqueIndexs[AttemptIndex] == true)
                        {
                            Passed = false;
                            break;
                        } else
                        {
                            UniqueIndexs[AttemptIndex] = true;
                        }
                    }
                    if (Passed == true)
                    {
                        //working magic number
                        if (BishopMagicNumbers[AttemptSquare] == 0)
                        {
                            BishopMagicNumbers[AttemptSquare] = AttemptNum;
                        } else if (BishopMagicNumbers[AttemptSquare] > AttemptNum)
                        {
                            Console.WriteLine("Override");
                            BishopMagicNumbers[AttemptSquare] = AttemptNum;
                        }
                        
                    }

                    Passed = true;
                    UniqueIndexs = new bool[GetBlockerMaskSize(PrecomputedInfo.RookBlockingMasks[AttemptSquare])];

                    for (ulong BlockerIndex = 0; BlockerIndex < GetBlockerMaskSize(PrecomputedInfo.RookBlockingMasks[AttemptSquare]); BlockerIndex++)
                    {
                        ulong AttemptIndex = (GetBlockerMask(PrecomputedInfo.RookBlockingMasks[AttemptSquare], BlockerIndex) * AttemptNum) >> (int)(64 - PrecomputedInfo.RookBlockingMaskCount[AttemptSquare]);
                        if (UniqueIndexs[AttemptIndex] == true)
                        {
                            Passed = false;
                            break;
                        } else
                        {
                            UniqueIndexs[AttemptIndex] = true;
                        }
                    }
                    if (Passed == true)
                    {
                        //working magic number
                        if (RookMagicNumbers[AttemptSquare] == 0)
                        {
                            RookMagicNumbers[AttemptSquare] = AttemptNum;
                        } else if (RookMagicNumbers[AttemptSquare] > AttemptNum)
                        {
                            Console.WriteLine("Override");
                            RookMagicNumbers[AttemptSquare] = AttemptNum;
                        }

                    }
                }
            }
        }
        public static ulong GetBlockerMaskSize(ulong blockerBitBoard)
        {
            int MaskIndex = 0;
            ulong Mask = 0;
            for (int i = 0; i < 64; i++)
            {
                if (((blockerBitBoard >> i) & 1) == 1)
                {
                    Mask += (1ul << MaskIndex);
                    MaskIndex++;
                }
            }
            return Mask + 1UL;
        }
        public static ulong GetBlockerMask(ulong blockerBitBoard, ulong blockerIndex)
        {
            int MaskIndex = 0;
            ulong Mask = 0;
            for (int i = 0; i < 64; i++)
            {
                if (((blockerBitBoard >> i) & 1) == 1)
                {
                    if (((blockerIndex >> MaskIndex) & 1) == 1)
                    {
                        Mask += (1UL << i);
                    }
                    MaskIndex++;
                }
            }
            return Mask;
        }
    }
}
