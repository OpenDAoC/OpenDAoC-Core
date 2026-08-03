using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public abstract class KoalinthNpc : GameNPC
	{
		public KoalinthNpc() : base() { }

		protected abstract int TemplateId { get; }
		protected abstract StandardMobBrain CreateBrain();

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(TemplateId);
			LoadTemplate(npcTemplate);

			SetOwnBrain(CreateBrain());
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public abstract class KoalinthBrain : StandardMobBrain
	{
		public KoalinthBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
		}

		protected abstract string BafPackageId { get; }
		protected abstract Spell HasteDebuff { get; }

		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				if (PullFriends(BafPackageId, 1500) > 0)
					Message.MessageToArea(Body, $"{Body.Name} gurgles a war cry, and the tribe surges forth!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

				TryCastSpell(HasteDebuff, 25, eEffect.MeleeHasteDebuff);
			}
			base.Think();
		}

		protected static Spell CreateHasteDebuff(string cacheKey, int spellId)
		{
			return ScriptSpells.GetOrCreate(cacheKey, 13, static (db, spellId) =>
			{
				db.CastTime = 0;
				db.RecastDelay = 7;
				db.Power = 6;
				db.Duration = 45;
				db.ClientEffect = 723;
				db.Icon = 723;
				db.Name = "Inflict Suffering";
				db.Description = "Target's attack speed reduced by 17%.";
				db.Message1 = "Your limbs grow heavy with suffering!";
				db.Message2 = "{0}'s movements grow sluggish.";
				db.Message3 = "The suffering leaves your limbs.";
				db.Message4 = "{0} shakes off the suffering.";
				db.Range = 1500;
				db.Value = 17;
				db.SpellID = spellId;
				db.Target = eSpellTarget.ENEMY.ToString();
				db.Type = eSpellType.CombatSpeedDebuff.ToString();
				db.DamageType = (int)eDamageType.Body;
			}, spellId);
		}
	}
}
