using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Ruckus : GameNPC
	{
		public Ruckus() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165469);
			LoadTemplate(npcTemplate);

			RuckusBrain sbrain = new RuckusBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class RuckusBrain : StandardMobBrain
	{
		public RuckusBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		private bool PrepareStun = false;

		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
            {
				if (!PrepareStun && Body.TargetObject is GameLiving target && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity)
					&& TryCastSpell(Ruckus_stun, 25, eEffect.Stun))
                {
					Message.MessageToArea(Body, "Ruckus channels his pent-up fury into a single stunning blow!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, 1500);
					PrepareStun = true;
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetStun), 20000);
                }
				if (TryCastSpell(RuckusDA, 100, eEffect.DamageAdd))
					Message.MessageToArea(Body, "Ruckus's fists take on an earthen sheen.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, 1500);
			}
			base.Think();
		}
		private int ResetStun(ECSGameTimer timer)
		{
			PrepareStun = false;
			return 0;
		}
		#region Spells
		private static Spell RuckusDA => ScriptSpells.GetOrCreate("RuckusDA", 20, static db =>
		{
			db.CastTime = 0;
			db.Power = 0;
			db.RecastDelay = 10;
			db.ClientEffect = 18;
			db.Icon = 18;
			db.Damage = 10;
			db.Duration = 10;
			db.DamageType = (int)eDamageType.Matter;
			db.Name = "Earthen Fury";
			db.Range = 1000;
			db.SpellID = 11942;
			db.Target = eSpellTarget.SELF.ToString();
			db.Type = eSpellType.DamageAdd.ToString();
			db.Uninterruptible = true;
		});
		private static Spell Ruckus_stun => ScriptSpells.GetOrCreate("RuckusStun", 20, static db =>
		{
			db.CastTime = 0;
			db.RecastDelay = 2;
			db.ClientEffect = 2165;
			db.Icon = 2132;
			db.TooltipId = 2132;
			db.Duration = 4;
			db.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
			db.Name = "Stun";
			db.Range = 400;
			db.SpellID = 11943;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.Stun.ToString();
		});
        #endregion
    }
}
