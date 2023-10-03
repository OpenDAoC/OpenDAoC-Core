using System;
using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class Antipass : GameNPC
    {
        public override bool AddToWorld()
        {
            this.SetOwnBrain(new AntipassBrain());
            Brain.Start();
            base.AddToWorld();
            Name = "No Pass";
            Flags |= GameNPC.eFlags.PEACE;
            //Flags |= (uint)GameNPC.eFlags.CANTTARGET;
            Flags |= GameNPC.eFlags.FLYING;      
            Model = 10;
            Size = 50;
            Level = 90;
            MaxSpeedBase = 0;
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class AntipassBrain : StandardMobBrain
    {
        public AntipassBrain()
            : base()
        {
            ThinkInterval = 50;
            AggroLevel = 100;
            AggroRange = 400;
        }

        public override void Think()
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (player.Client.Account.PrivLevel != 3)
                {
                    double angle = 0.00153248422;
                    player.MoveTo(player.CurrentRegionID, (int)(Body.X - ((AggroRange + 10) * Math.Sin(angle * Body.Heading))), (int)(Body.Y + ((AggroRange + 10) * Math.Cos(angle * Body.Heading))), Body.Z, player.Heading);
                }
            }
        }
    }
}