using DOL.Database;

namespace DOL.GS.Scripts
{
    public class BaneOfHope : GameEpicBoss
    {
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * 30;
        }


        public override short MaxSpeedBase
        {
            get => (short) (191 + (Level * 2));
            set => m_maxSpeedBase = value;
        }

        public override int MaxHealth => 20000;

        public override int AttackRange
        {
            get => 180;
            set { }
        }

        public override bool AddToWorld()
        {
            RespawnInterval = 10;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158245);
            LoadTemplate(npcTemplate);
            base.AddToWorld();
            return true;
        }

        public override void Die(GameObject killer)
        {
            MoveTo(CurrentRegionID, 31154, 30913, 13950, 3043);
            base.Die(killer);
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                if (HealthPercent % 10 == 0)
                {
                    var location = Util.Random(0, 100);

                    if (location <= 33)
                    {
                        MoveTo(CurrentRegionID, 34496, 30879, 14551, 1045);
                    }
                    else if (location is > 33 and <= 66)
                    {
                        MoveTo(CurrentRegionID, 37377, 30154, 13973, 978);
                    }
                    else
                    {
                        MoveTo(CurrentRegionID, 38292, 31794, 13940, 986);
                    }
                }
            }

            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
    }
}