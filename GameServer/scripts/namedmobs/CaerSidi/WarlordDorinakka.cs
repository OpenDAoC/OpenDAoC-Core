namespace DOL.GS.Scripts
{
	public class WarlordDorinakka : GameNPC
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

		public override short MaxSpeedBase
		{
			get => (short)(191 + (Level * 2));
			set => m_maxSpeedBase = value;
		}
		public override int MaxHealth => 20000;

		public override int AttackRange
		{
			get => 180;
			set { }
		}
		
		public override byte ParryChance => 100;

		public override bool AddToWorld()
		{
			Level = 81;
			Gender = eGender.Neutral;
			BodyType = 5; // giant
			MaxDistance = 1500;
			TetherRange = 2000;
			RoamingRange = 400;
			Realm = eRealm.None;
			ParryChance = 100; // 100% parry chance
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167787);
			LoadTemplate(npcTemplate);
			base.AddToWorld();
			return true;
		}
		
		public override void Die(GameObject killer)
		{
			// debug
			log.Debug($"{Name} killed by {killer.Name}");
            
			GamePlayer playerKiller = killer as GamePlayer;

			if (playerKiller?.Group != null)
			{
				foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
				{
					AtlasROGManager.GenerateOrbAmount(groupPlayer,5000);
				}
			}
			DropLoot(killer);
			base.Die(killer);
		}
	}
}