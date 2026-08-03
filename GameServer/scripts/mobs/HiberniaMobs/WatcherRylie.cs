using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class WatcherRylie : HideableNpc
	{
		public WatcherRylie() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167795);
			LoadTemplate(npcTemplate);
			Faction = FactionMgr.GetFactionByID(79);

			WatcherRylieBrain sbrain = new WatcherRylieBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			SetHidden(CurrentRegion.IsNightTime);
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class WatcherRylieBrain : StandardMobBrain
	{
		public WatcherRylieBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
		}
		public override void Think()
		{
			bool hidden = Body.CurrentRegion.IsNightTime && !Body.InCombat;

			if (((HideableNpc)Body).SetHidden(hidden) && !hidden)
				Message.MessageToArea(Body, "Watcher Rylie steps back onto her post as the light returns.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);

			if (Body.TargetObject != null && HasAggro)
			{
				if (PullFriends("RylieBaf", 2500) > 0)
					Message.MessageToArea(Body, "Watcher Rylie cries out, 'Defenders of Hibernia, to my side!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

				if (Body.TargetObject is GameLiving target && target.IsAlive)
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
						TryCastSpell(Rylie_stun, 100);
					else
						TryCastSpell(RylieDD, 100);
				}
			}
			base.Think();
		}
        #region Spells
        private static Spell RylieDD => ScriptSpells.GetOrCreate("RylieDD", 15, static spell =>
		{
			spell.CastTime = 3;
			spell.Power = 0;
			spell.RecastDelay = Util.Random(5, 7);
			spell.ClientEffect = 4111;
			spell.Icon = 4111;
			spell.Damage = 80;
			spell.DamageType = (int)eDamageType.Energy;
			spell.Name = "Energy Blast";
			spell.Range = 1500;
			spell.SpellID = 11949;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Type = eSpellType.DirectDamageNoVariance.ToString();
			spell.Uninterruptible = true;
		});
		private static Spell Rylie_stun => ScriptSpells.GetOrCreate("RylieStun", 15, static spell =>
		{
			spell.CastTime = 2;
			spell.RecastDelay = 0;
			spell.ClientEffect = 4125;
			spell.Icon = 4125;
			spell.TooltipId = 4125;
			spell.Duration = 5;
			spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
			spell.Name = "Stun";
			spell.Message1 = "You are stunned!";
			spell.Message2 = "{0} is stunned!";
			spell.Message3 = "You recover from the stun.";
			spell.Message4 = "{0} recovers from the stun.";
			spell.Range = 1500;
			spell.SpellID = 11950;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Type = eSpellType.Stun.ToString();
			spell.DamageType = (int)eDamageType.Energy;
			spell.Uninterruptible = true;
			spell.MoveCast = true;
		});
		#endregion
	}
}
