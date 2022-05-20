using DOL.AI;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.DOL.AI.Brain;

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

            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            //seedling
            Model = 818;
            Size = 9;
            Name = "botonid seedling";

            Faction = FactionMgr.GetFactionByID(69);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(69));

            BotonidBrain sBrain = new BotonidBrain();
            SetOwnBrain(sBrain);

            //1.30min
            RespawnInterval = 90000;

            base.AddToWorld();
            return true;

            // 818 scaled to 9
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

            private bool transformed;

            public override int ThinkInterval => 1000;

            public override void Think()
            {
                if (HasAggro)
                {
                    if (!Body.IsWithinRadius(Body.TargetObject, 150)) return;
                    if (transformed) return;
                    foreach (GamePlayer player in Body.GetPlayersInRadius(400))
                    {
                        player.Out.SendMessage("The lure dissapears and a scourgin jumps out at " + player.Name + ".",
                            eChatType.CT_Say,
                            eChatLoc.CL_ChatWindow);
                        Transform(transformed); // scourgin
                        transformed = true;
                    }
                }
                else if (!Body.InCombatInLast(30 * 1000) && !HasAggro)
                {
                    if (!transformed) return;
                    Transform(transformed); //seedling
                    transformed = false;
                }

                base.Think();
            }

            private void Transform(bool transformed)
            {
                if (transformed)
                {
                    Body.Size = 9;
                    Body.Model = 818;
                    Body.Name = "botonid seedling";
                }
                else
                {
                    Body.Size = 50;
                    Body.Model = 914;
                    Body.Name = "scourgin";
                }
            }
        }
    }
}