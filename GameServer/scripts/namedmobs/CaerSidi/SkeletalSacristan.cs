using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class SkeletalSacristan : GameEpicBoss
    {
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override int MaxHealth
        {
            get { return 200000; }
        }
        public override bool HasAbility(string keyName)
        {         
            if (IsAlive && keyName == "CCImmunity")
                return true;

            return base.HasAbility(keyName);
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override bool AddToWorld()
		{
			Model = 916;
			Name = "Skeletal Sacristan";
			Size = 85;
			Level = 77;
			Gender = eGender.Neutral;
			BodyType = 11; // undead
			MaxDistance = 0;
			TetherRange = 0;
			RoamingRange = 0;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            SkeletalSacristanBrain.point7check = false;
            SkeletalSacristanBrain.point1check = false;
            SkeletalSacristanBrain.point2check = false;
            SkeletalSacristanBrain.point3check = false;
            SkeletalSacristanBrain.point4check = false;
            SkeletalSacristanBrain.point5check = false;
            SkeletalSacristanBrain.point6check = false;
            SkeletalSacristanBrain.ToSpawn = false;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166180);
			LoadTemplate(npcTemplate);
			SkeletalSacristanBrain sBrain = new SkeletalSacristanBrain();
			SetOwnBrain(sBrain);
			base.AddToWorld();
            SaveIntoDatabase();
            LoadedFromScript = false;
			return true;
		}
	    [ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Skeletal Sacristan NPC Initializing...");
		}

        public override void WalkToSpawn(short speed)
        {
            if (IsAlive)
                return;
            base.WalkToSpawn(speed);
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override bool IsVisibleToPlayers => true;
    }
    
}
namespace DOL.AI.Brain
{
	public class SkeletalSacristanBrain : StandardMobBrain
	{
		public static bool point1check = false;
		public static bool point2check = false;
		public static bool point3check = false;
		public static bool point4check = false;
		public static bool point5check = false;
		public static bool point6check = false;
		public static bool point7check = false;
        public static bool ToSpawn = false;

        public override void Think()
		{
			Point3D point1 = new Point3D(31826, 32256, 16750);
			Point3D point2 = new Point3D(32846, 32250, 16750);
			Point3D point3 = new Point3D(35357, 32243, 16494);
			Point3D point4 = new Point3D(35408, 35788, 16494);
			Point3D point5 = new Point3D(33112, 35808, 16750);
			Point3D point6 = new Point3D(30259, 35800, 16750);
			Point3D point7 = new Point3D(30238, 32269, 16750);

            if (Body.IsAlive)
            {
                if (!Body.IsWithinRadius(point1, 30) && point1check == false)
                {
                    Body.WalkTo(point1, (short)Util.Random(195, 250));
                }
                else
                {
                    point1check = true;
                    point7check = false;
                    if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
                    {
                        Body.WalkTo(point2, (short)Util.Random(195, 250));
                    }
                    else
                    {
                        point2check = true;
                        if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
                            point3check == false)
                        {
                            Body.WalkTo(point3, (short)Util.Random(195, 250));
                        }
                        else
                        {
                            point3check = true;
                            if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
                                point3check == true && point4check == false)
                            {
                                Body.WalkTo(point4, (short)Util.Random(195, 250));
                            }
                            else
                            {
                                point4check = true;
                                if (!Body.IsWithinRadius(point5, 30) && point1check == true && point2check == true &&
                                    point3check == true && point4check == true && point5check == false)
                                {
                                    Body.WalkTo(point5, (short)Util.Random(195, 250));
                                }
                                else
                                {
                                    point5check = true;
                                    if (!Body.IsWithinRadius(point6, 30) && point1check == true && point2check == true &&
                                    point3check == true && point4check == true && point5check == true && point6check == false)
                                    {
                                        Body.WalkTo(point6, (short)Util.Random(195, 250));
                                    }
                                    else
                                    {
                                        point6check = true;
                                        if (!Body.IsWithinRadius(point7, 30) && point1check == true && point2check == true &&
                                        point3check == true && point4check == true && point5check == true && point6check == true && point7check == false)
                                        {
                                            Body.WalkTo(point7, (short)Util.Random(195, 250));
                                        }
                                        else
                                        {
                                            point7check = true;
                                            point1check = false;
                                            point2check = false;
                                            point3check = false;
                                            point4check = false;
                                            point5check = false;
                                            point6check = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                ClearAggroList();
                Body.Health = Body.MaxHealth;
            }
            base.Think();
		}
		
	}
}