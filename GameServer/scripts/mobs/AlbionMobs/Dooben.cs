using DOL.AI.Brain;

namespace DOL.GS
{
	public class Dooben : GameNPC
	{
		public Dooben() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12676);
			LoadTemplate(npcTemplate);

			DoobenBrain sbrain = new DoobenBrain();
			if (NPCTemplate != null)
			{
				sbrain.AggroLevel = NPCTemplate.AggroLevel;
				sbrain.AggroRange = NPCTemplate.AggroRange;
			}
			SetOwnBrain(sbrain);
			return base.AddToWorld();
		}

		public override void OnAttackEnemy(AttackData ad)
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle) && Util.Chance(45))
				CastSpell(DoobenDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			base.OnAttackEnemy(ad);
		}

		private static Spell DoobenDD => ScriptSpells.GetOrCreate("DoobenSandStrike", 10, db =>
		{
			db.CastTime = 0;
			db.Power = 0;
			db.RecastDelay = 2;
			db.ClientEffect = 127;
			db.Icon = 127;
			db.Damage = 25;
			db.DamageType = (int) eDamageType.Spirit;
			db.Name = "Sand Strike";
			db.Range = 350;
			db.SpellID = 11988;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DirectDamageNoVariance.ToString();
		});
	}
}
namespace DOL.AI.Brain
{
	public class DoobenBrain : AmbientEffectBrain
	{
		protected override ushort AmbientEffectId => 479;
		protected override int AmbientMinIntervalMs => 1600;
		protected override int AmbientMaxIntervalMs => 1600;
		protected override bool ShouldPlayAmbientEffect => !HasAggro;
	}
}
