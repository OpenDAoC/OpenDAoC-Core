using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class BotonidSeedling : GameNPC
    {
        public BotonidSeedling() : base()
        {
        }

        public BotonidSeedling(ABrain defaultBrain) : base(defaultBrain)
        {
        }

        public BotonidSeedling(INpcTemplate template) : base(template)
        {
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165666);
            LoadTemplate(npcTemplate);

            //seedling
            Model = 818;
            Size = 9;
            Name = "botonid seedling";

            Faction = FactionMgr.GetFactionByID(69);

            BotonidBrain sBrain = new BotonidBrain();
            SetOwnBrain(sBrain);

            //1.30min
            RespawnInterval = 90000;

            return base.AddToWorld();
        }
    }
}

namespace DOL.AI.Brain
{
    public class BotonidBrain : StandardMobBrain
    {
        public BotonidBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        private bool isScourgin;

        public override int ThinkInterval => 1000;

        public override void Think()
        {
            if (HasAggro)
            {
                if (!Body.IsWithinRadius(Body.TargetObject, 150)) return;
                if (!isScourgin)
                {
                    Transform(true);
                    isScourgin = true;

                    if (Body.TargetObject != null)
                        Message.MessageToArea(Body, $"The lure disappears and a scourgin lunges at {Body.TargetObject.Name}!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, 400);
                    else
                        Message.MessageToArea(Body, "The lure disappears and a scourgin springs out!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, 400);
                }
            }
            else if (!Body.InCombatInLast(30 * 1000) && !HasAggro)
            {
                if (isScourgin)
                {
                    Transform(false);
                    isScourgin = false;
                }
            }

            base.Think();
        }

        private void Transform(bool toScourgin)
        {
            if (toScourgin)
            {
                Body.Size = 50;
                Body.Model = 914;
                Body.Name = "scourgin";
            }
            else
            {
                Body.Size = 9;
                Body.Model = 818;
                Body.Name = "botonid seedling";
            }
        }
    }
}