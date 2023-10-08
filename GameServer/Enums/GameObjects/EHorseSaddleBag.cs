namespace DOL.GS;

public enum EHorseSaddleBag : byte
{
    None = 0x00,
    LeftFront = 0x01,
    RightFront = 0x02,
    LeftRear = 0x04,
    RightRear = 0x08,
    All = 0x0F
}