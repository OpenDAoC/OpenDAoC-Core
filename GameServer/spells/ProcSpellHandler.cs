using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    public abstract class BaseProcSpellHandler : SpellHandler
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly SpellLine _buffSpellLine;
        protected readonly Spell _procSpell;
        protected SpellLine _procSpellLine;

        protected abstract DOLEvent EventType { get; }

        protected BaseProcSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            _buffSpellLine = spellLine;
            _procSpell = SkillBase.GetSpellByID((int) spell.Value);
        }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            // If the target of the buff and the caster are the same entity, we use the buff spell line for the proc spell.
            // This means that the player will benefit from their specialization, stats, RAs, etc.
            // If they're different, we can't really use the buff spell line, so we use the item effects spell line as a way to reduce variance.
            // Ideally, proc spells should use the buff spell line and the caster's specialization, stats, RAs, etc. But this isn't currently supported.
            _procSpellLine = initParams.Target == Caster ? _buffSpellLine : SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
            return ECSGameEffectFactory.Create(initParams, static (in ECSGameEffectInitParams i) => new ProcECSGameEffect(i));
        }

        protected abstract void EventHandler(DOLEvent e, object sender, EventArgs arguments);

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = Spell.Duration;
            duration *= 1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01;
            return (int) duration;
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.Spell.EffectGroup;

            if (compare.Spell.SpellType != Spell.SpellType)
                return false;

            Spell oldProcSpell = SkillBase.GetSpellByID((int) Spell.Value);
            Spell newProcSpell = SkillBase.GetSpellByID((int) compare.Spell.Value);

            if (oldProcSpell == null || newProcSpell == null)
                return true;

            if (oldProcSpell.SpellType != newProcSpell.SpellType)
                return false;

            return true;
        }

        public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            return new()
            {
                Var1 = Spell.ID,
                Duration = e.RemainingTime,
                IsHandler = true,
                Var2 = (int) (Spell.Value * e.Effectiveness),
                SpellLine = SpellLine.KeyName
            };
        }

        public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
        {
            GameEventMgr.AddHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
        }

        public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
        {
            GameEventMgr.RemoveHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));

            if (!noMessages && Spell.Pulse == 0)
            {
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
                Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
            }

            return 0;
        }

        public override IList<string> DelveInfo
        {
            get
            {
                List<string> list = new();
                GameClient client = (Caster as GamePlayer).Client;

                if (client == null)
                    return list;

                list.Add(LanguageMgr.GetTranslation(client, "ProcSpellHandler.DelveInfo.Function", string.IsNullOrEmpty(Spell.SpellType.ToString()) ? "(not implemented)" : Spell.SpellType.ToString()));
                list.Add(LanguageMgr.GetTranslation(client, "DelveInfo.Target", Spell.Target));

                if (Spell.Range != 0)
                    list.Add(LanguageMgr.GetTranslation(client, "DelveInfo.Range", Spell.Range));

                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation(client, "DelveInfo.Duration") + " Permanent.");
                else if (Spell.Duration > 60000)
                    list.Add($"{LanguageMgr.GetTranslation(client, "DelveInfo.Duration")}{Spell.Duration / 60000}:{Spell.Duration % 60000 / 1000:00}min");
                else if (Spell.Duration != 0)
                    list.Add($"Duration: {Spell.Duration / 1000:0' sec';'Permanent.';'Permanent.'}");

                if (Spell.Power != 0)
                    list.Add($"Power cost: {Spell.Power:0;0'%'}");

                list.Add($"Casting time: {Spell.CastTime * 0.001:0.0## sec;-0.0## sec;'instant'}");

                if (Spell.RecastDelay > 60000)
                    list.Add($"Recast time: {Spell.RecastDelay / 60000}:{Spell.RecastDelay % 60000 / 1000:00} min");
                else if (Spell.RecastDelay > 0)
                    list.Add($"Recast time: {Spell.RecastDelay / 1000} sec");

                if (Spell.Concentration != 0)
                    list.Add($"Concentration cost: {Spell.Concentration}");

                if (Spell.Radius != 0)
                    list.Add($"Radius: {Spell.Radius}");

                byte nextDelveDepth = (byte) (DelveInfoDepth + 1);

                if (nextDelveDepth > MAX_DELVE_RECURSION)
                {
                    list.Add("(recursion - see server logs)");

                    if (log.IsErrorEnabled)
                        log.Error($"Spell delve info recursion limit reached. Source spell ID: {m_spell.ID}, Sub-spell ID: {_procSpell.ID}");
                }
                else
                {
                    list.Add(" ");
                    list.Add("Sub-spell information: ");
                    list.Add(" ");
                    ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(Caster, _procSpell, _procSpellLine);

                    if (subSpellHandler == null)
                    {
                        list.Add($"unable to create sub-spell handler: '{_procSpellLine}', {m_spell.Value}");
                        return list;
                    }

                    subSpellHandler.DelveInfoDepth = nextDelveDepth;
                    IList<string> subSpellDelve = subSpellHandler.DelveInfo;

                    if (subSpellDelve.Count > 0)
                    {
                        subSpellDelve.RemoveAt(0);
                        list.AddRange(subSpellDelve);
                    }
                }

                return list;
            }
        }
    }

    [SpellHandler(eSpellType.OffensiveProc)]
    public class OffensiveProcSpellHandler : BaseProcSpellHandler
    {
        protected override DOLEvent EventType => GameLivingEvent.AttackFinished;

        public OffensiveProcSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        protected override void EventHandler(DOLEvent e, object sender, EventArgs arguments) { }

        public void EventHandler(AttackData ad)
        {
            if (ad.AttackResult is not eAttackResult.HitUnstyled and not eAttackResult.HitStyle)
                return;

            int baseChance = Spell.Frequency / 100;

            if (Util.Chance(baseChance))
            {
                ISpellHandler handler = ScriptMgr.CreateSpellHandler(ad.Attacker, _procSpell, _procSpellLine);

                if (handler != null)
                {
                    handler.Spell.Level = Spell.Level;

                    switch (_procSpell.Target)
                    {
                        case eSpellTarget.ENEMY:
                        {
                            handler.StartSpell(ad.Target);
                            break;
                        }
                        default:
                        {
                            handler.StartSpell(ad.Attacker);
                            break;
                        }
                    }
                }
            }
        }
    }

    [SpellHandler(eSpellType.DefensiveProc)]
    public class DefensiveProcSpellHandler : BaseProcSpellHandler
    {
        protected override DOLEvent EventType => GameLivingEvent.AttackedByEnemy;

        public DefensiveProcSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        protected override void EventHandler(DOLEvent e, object sender, EventArgs arguments) { }

        public void EventHandler(AttackData ad)
        {
            if (ad.AttackResult is not eAttackResult.HitUnstyled and not eAttackResult.HitStyle)
                return;

            int baseChance = Spell.Frequency / 100;

            if (Util.Chance(baseChance))
            {
                ISpellHandler handler = ScriptMgr.CreateSpellHandler(ad.Target, _procSpell, _procSpellLine);

                if (handler != null)
                {
                    switch (_procSpell.Target)
                    {
                        case eSpellTarget.ENEMY:
                        {
                            handler.StartSpell(ad.Attacker);
                            break;
                        }
                        default:
                        {
                            handler.StartSpell(ad.Target);
                            break;
                        }
                    }
                }
            }
        }
    }

    [SpellHandler(eSpellType.OffensiveProcPvE)]
    public class OffensiveProcPvESpellHandler : OffensiveProcSpellHandler
    {
        public OffensiveProcPvESpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        protected override void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackFinishedEventArgs args || args.AttackData == null)
                return;

            if (args.AttackData.Target is GameNPC target && (target.Brain is not IControlledBrain brain || brain.GetPlayerOwner() == null))
                base.EventHandler(e, sender, arguments);
        }
    }
}
