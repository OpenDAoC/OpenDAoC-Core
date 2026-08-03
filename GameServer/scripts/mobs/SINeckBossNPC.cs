using DOL.GS.PacketHandler;

namespace DOL.GS {
    public class SINeckBoss : GameNPC {

        public bool RoarAnnounced { get; set; }

        public SINeckBoss() : base()
        {
        }
		public override int GetResist(eDamageType damageType)
		{
			return 20;
		}
		public override int MaxHealth
		{
			get { return (6000 + (Level * 125)); }
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
		public override void OnAttackEnemy(AttackData ad)
        {
            if(ad != null && ad.Target != null && ad.Target.IsAlive && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
				if(!IsCasting && TargetObject is GameLiving { IsAlive: true } && Util.Chance(20))
				{
					if (CastSpell(SINeckBossDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)) && !RoarAnnounced)
					{
						RoarAnnounced = true;
						Message.MessageToArea(this, "A shockwave rolls off the beast, and those nearby are hammered by its roar!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
					}
				}
			}
            base.OnAttackEnemy(ad);
        }
		public Spell SINeckBossDD => ScriptSpells.GetOrCreate("SINeckBossDD", 50, static spell =>
		{
			spell.CastTime = 0;
			spell.Power = 0;
			spell.RecastDelay = 8;
			spell.ClientEffect = 9644;
			spell.Icon = 9644;
			spell.Damage = 300;
			spell.DamageType = (int)eDamageType.Spirit;
			spell.Name = "Sundering Roar";
			spell.Range = 450;
			spell.Radius = 350;
			spell.SpellID = 12000;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Type = eSpellType.DirectDamageNoVariance.ToString();
		});
	}
}
