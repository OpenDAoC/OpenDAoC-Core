using System;
using DOL.GS;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;

namespace DOL.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Perfector
    //the link isnt corrently working so correct me if you see any timers wrong.


    //ML1 Cure NS - already handled in another area

    //ML2 GRP Cure Disease - already handled in another area

    //shared timer 1
    #region Perfecter-3
    [SpellHandlerAttribute("FOH")]
    public class FohHandler : FontSpellHandler
    {
        // constructor
        public FohHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            ApplyOnNPC = true;

            //Construct a new font.
            font = new GameFont();
            font.Model = 2585;
            font.Name = spell.Name;
            font.Realm = caster.Realm;
            font.X = caster.X;
            font.Y = caster.Y;
            font.Z = caster.Z;
            font.CurrentRegionID = caster.CurrentRegionID;
            font.Heading = caster.Heading;
            font.Owner = (GamePlayer)caster;

            // Construct the font spell
            dbs = new DBSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7245;
            dbs.ClientEffect = 7245;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Realm";
            dbs.Radius = 0;
            dbs.Type = ESpellType.HealOverTime.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			dbs.Message1 = spell.Message1;
			dbs.Message2 = spell.Message2;
			dbs.Message3 = spell.Message3;
			dbs.Message4 = spell.Message4;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            heal = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    #endregion

    //ML4 Greatness - passive increases 20% concentration

    //shared timer 1
    #region Perfecter-5
    [SpellHandlerAttribute("FOP")]
    public class FopHandler : FontSpellHandler
    {
        // constructor
        public FopHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            ApplyOnNPC = false;

            //Construct a new font.
            font = new GameFont();
            font.Model = 2583;
            font.Name = spell.Name;
            font.Realm = caster.Realm;
            font.X = caster.X;
            font.Y = caster.Y;
            font.Z = caster.Z;
            font.CurrentRegionID = caster.CurrentRegionID;
            font.Heading = caster.Heading;
            font.Owner = (GamePlayer)caster;

            // Construct the font spell
            dbs = new DBSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7212;
            dbs.ClientEffect = 7212;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Realm";
            dbs.Radius = 0;
            dbs.Type = ESpellType.PowerOverTime.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
			dbs.Message1 = spell.Message1;
			dbs.Message2 = spell.Message2;
			dbs.Message3 = spell.Message3;
			dbs.Message4 = spell.Message4;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            heal = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    #endregion

    //shared timer 1
    #region Perfecter-6
    [SpellHandlerAttribute("FOR")]
    public class ForHandler : FontSpellHandler
    {
        // constructor
        public ForHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            ApplyOnNPC = true;
            ApplyOnCombat = true;

            //Construct a new font.
            font = new GameFont();
            font.Model = 2581;
            font.Name = spell.Name;
            font.Realm = caster.Realm;
            font.X = caster.X;
            font.Y = caster.Y;
            font.Z = caster.Z;
            font.CurrentRegionID = caster.CurrentRegionID;
            font.Heading = caster.Heading;
            font.Owner = (GamePlayer)caster;

            // Construct the font spell
            dbs = new DBSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7214;
            dbs.ClientEffect = 7214;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Realm";
            dbs.Radius = 0;
            dbs.Type = ESpellType.MesmerizeDurationBuff.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            heal = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    #endregion

    //shared timer 2
    //ML7 Leaping Health - already handled in another area

    //no shared timer
    #region Perfecter-8
    [SpellHandlerAttribute("SickHeal")]
    public class SickHealHandler : RemoveSpellEffectHandler
    {
        // constructor
        public SickHealHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = new List<string>();
            m_spellTypesToRemove.Add("PveResurrectionIllness");
            m_spellTypesToRemove.Add("RvrResurrectionIllness");
        }
    }
    #endregion

    //shared timer 1
    #region Perfecter-9
    [SpellHandlerAttribute("FOD")]
    public class FodHandler : FontSpellHandler
    {
        // constructor
        public FodHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            ApplyOnCombat = true;

            //Construct a new font.
            font = new GameFont();
            font.Model = 2582;
            font.Name = spell.Name;
            font.Realm = caster.Realm;
            font.X = caster.X;
            font.Y = caster.Y;
            font.Z = caster.Z;
            font.CurrentRegionID = caster.CurrentRegionID;
            font.Heading = caster.Heading;
            font.Owner = (GamePlayer)caster;

            // Construct the font spell
            dbs = new DBSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7310;
            dbs.ClientEffect = 7310;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.PowerRend.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            heal = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }	
    #endregion

    //shared timer 2
    //ML10 Rampant Healing - already handled in another area

    #region PoT
    [SpellHandlerAttribute("PowerOverTime")]
    public class PotHandler : SpellHandler
    {
        /// <summary>
        /// Execute heal over time spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            // TODO: correct formula
            double eff = 1.25;
            if (Caster is GamePlayer)
            {
                double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
                if (lineSpec < 1)
                    lineSpec = 1;
                eff = 0.75;
                if (Spell.Level > 0)
                {
                    eff += (lineSpec - 1.0) / Spell.Level * 0.5;
                    if (eff > 1.25)
                        eff = 1.25;
                }
            }
            base.ApplyEffectOnTarget(target, eff);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new GameSpellEffect(this, Spell.Duration, Spell.Frequency, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
            //"{0} seems calm and healthy."
            Message.SystemToArea(effect.Owner, UtilCollection.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), EChatType.CT_Spell, effect.Owner);
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);
            OnDirectEffect(effect.Owner, effect.Effectiveness);
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target.InCombat) return;
            if (target.ObjectState != GameObject.eObjectState.Active) return;
            if (target.IsAlive == false) return;
            if (target is GamePlayer)
            {
                GamePlayer player = target as GamePlayer;
                if (player.CharacterClass.ID == (int)ECharacterClass.Vampiir 
                    || player.CharacterClass.ID == (int)ECharacterClass.MaulerHib
                    || player.CharacterClass.ID == (int)ECharacterClass.MaulerMid
                    || player.CharacterClass.ID == (int)ECharacterClass.MaulerAlb)
                    return;
            }

            base.OnDirectEffect(target, effectiveness);
            double heal = Spell.Value * effectiveness;
            if (heal < 0) target.Mana += (int)(-heal * target.MaxMana / 100);
            else target.Mana += (int)heal;
            //"You feel calm and healthy."
            MessageToLiving(target, Spell.Message1, EChatType.CT_Spell);
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            if (!noMessages)
            {
                //"Your meditative state fades."
                MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
                //"{0}'s meditative state fades."
                Message.SystemToArea(effect.Owner, UtilCollection.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), EChatType.CT_SpellExpires, effect.Owner);
            }
            return 0;
        }


        // constructor
        public PotHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region CCResist
    [SpellHandler("CCResist")]
    public class CcResistHandler : MasterLevelHandling
    {
        public override void OnEffectStart(GameSpellEffect effect)
        {
        	base.OnEffectStart(effect);
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.MesmerizeDurationReduction] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.StunDurationReduction] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.SpeedDecreaseDurationReduction] += (int)m_spell.Value;
             
            if (effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();       
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.MesmerizeDurationReduction] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.StunDurationReduction] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.SpeedDecreaseDurationReduction] -= (int)m_spell.Value;
            
            if (effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();  
            }
            return base.OnEffectExpires(effect,noMessages);
        }

        // constructor
        public CcResistHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}