using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public enum eHorseSaddleBag : byte
    {
        None = 0x00,
        LeftFront = 0x01,
        RightFront = 0x02,
        LeftRear = 0x04,
        RightRear = 0x08,
        All = 0x0F
    }
}
