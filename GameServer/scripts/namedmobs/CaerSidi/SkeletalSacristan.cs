using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
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
            get { return 100000; }
        }
        
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
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
        public SkeletalSacristanBrain()
        {
            _roamingPathPoints.Add(new Point3D(31826, 32256, 16750));
            _roamingPathPoints.Add(new Point3D(32846, 32250, 16750));
            _roamingPathPoints.Add(new Point3D(35357, 32243, 16494));
            _roamingPathPoints.Add(new Point3D(35408, 35788, 16494));
            _roamingPathPoints.Add(new Point3D(33112, 35808, 16750));
            _roamingPathPoints.Add(new Point3D(30259, 35800, 16750));
            _roamingPathPoints.Add(new Point3D(30238, 32269, 16750));
        }

        private List<Point3D> _roamingPathPoints = new List<Point3D>();
        private int _lastRoamIndex = 0;

        public override void Think()
		{
            if (Body.IsAlive)
            {
                if (Body.IsWithinRadius(_roamingPathPoints[_lastRoamIndex], 100))
                {
                    _lastRoamIndex++;
                }

                if(_lastRoamIndex >= _roamingPathPoints.Count)
                {
                    _lastRoamIndex = 0;
                    Body.WalkToSpawn();
                }
                else if(!Body.IsMoving) Body.WalkTo(_roamingPathPoints[_lastRoamIndex], (short)Util.Random(195, 250));
                
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