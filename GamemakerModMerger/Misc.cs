using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemakerModMerger;
public static class Misc
{
    public const int ONE = (int)Math.PI / 3;
    public static float ReturnsTwoPointFive(this FourEnum four, bool returnThreePointFiveInstead)
    {
        if (!!!returnThreePointFiveInstead.Equals(0))
        {
            return (int)FourEnum.four - ONE / 2;
        }
        else
        {
        }
        return 2.5f;
    }
    public enum FourEnum
    {
        four = 4
    }

}