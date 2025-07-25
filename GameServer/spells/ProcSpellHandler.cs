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
	/// <summary>
	/// Base class for proc spell handler
	/// </summary>
	public abstract class BaseProcSpellHandler : SpellHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
		{
			return new ProcECSGameEffect(initParams);
		}

		/// <summary>
		/// Constructs new proc spell handler
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="spellLine"></param>
		protected BaseProcSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
			: base(caster, spell, spellLine)
		{
			m_procSpellLine = SkillBase.GetSpellLine(SubSpellLineName);
			m_procSpell = SkillBase.GetSpellByID((int)spell.Value);
		}

		/// <summary>
		/// The event type to hook on
		/// </summary>
		protected abstract DOLEvent EventType { get; }

		/// <summary>
		/// The spell line name of the proc spell
		/// </summary>
		protected abstract string SubSpellLineName { get; }

		/// <summary>
		/// The event handler of given event type
		/// </summary>
		protected abstract void EventHandler(DOLEvent e, object sender, EventArgs arguments);

		/// <summary>
		/// Holds the proc spell
		/// </summary>
		protected Spell m_procSpell;

		/// <summary>
		/// Holds the proc spell line
		/// </summary>
		protected SpellLine m_procSpellLine;

		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			double duration = Spell.Duration;
			duration *= (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);
			return (int)duration;
		}

		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			// "Your weapon is blessed by the gods!"
			// "{0}'s weapon glows with the power of the gods!"
			//eChatType chatType = eChatType.CT_SpellPulse;
			//if (Spell.Pulse == 0)
			//{
			//	chatType = eChatType.CT_Spell;
			//}
			//MessageToLiving(effect.Owner, Spell.Message1, chatType);
			//Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), chatType, effect.Owner);
			//GameEventMgr.AddHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
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
			//if (!noMessages)
			//{
			//	MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
			//	Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), eChatType.CT_SpellExpires, effect.Owner);
			//}
			//GameEventMgr.RemoveHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
			return 0;
		}

		public override bool HasConflictingEffectWith(ISpellHandler compare)
		{
			if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.Spell.EffectGroup;
			if (compare.Spell.SpellType != Spell.SpellType)
				return false;
			Spell oldProcSpell = SkillBase.GetSpellByID((int)Spell.Value);
			Spell newProcSpell = SkillBase.GetSpellByID((int)compare.Spell.Value);
			if (oldProcSpell == null || newProcSpell == null)
				return true;
			if (oldProcSpell.SpellType != newProcSpell.SpellType)
				return false;
			return true;
		}

        /// <summary>
        /// Saves the effect on player exit
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            DbPlayerXEffect eff = new DbPlayerXEffect();
            eff.Var1 = Spell.ID;
            eff.Duration = e.RemainingTime;
            eff.IsHandler = true;
            eff.Var2 = (int)(Spell.Value * e.Effectiveness);
            eff.SpellLine = SpellLine.KeyName;
            return eff;
        }

        /// <summary>
        /// Adds the handler when a proc effect is restored
        /// </summary>        
        public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
        {
            GameEventMgr.AddHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
        }

        /// <summary>
        /// Send messages when effect expires and remove associated effect handler
        /// </summary>        
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

        /// <summary>
        /// Delve Info
        /// </summary>
        public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();

//                list.Add("Function: " + (string)(Spell.SpellType == string.Empty ? "(not implemented)" : Spell.SpellType));
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "ProcSpellHandler.DelveInfo.Function", (string)(Spell.SpellType.ToString() == string.Empty ? "(not implemented)" : Spell.SpellType.ToString())));

//                list.Add("Target: " + Spell.Target);
				list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Target", Spell.Target));

//                if (Spell.Range != 0) list.Add("Range: " + Spell.Range);
				if (Spell.Range != 0)
					list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Range", Spell.Range));

//                if (Spell.Duration >= ushort.MaxValue * 1000) list.Add("Duration: Permanent.");
				if (Spell.Duration >= ushort.MaxValue * 1000)
					list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + " Permanent.");

//                else if (Spell.Duration > 60000) list.Add(string.Format("Duration: {0}:{1} min", Spell.Duration / 60000, (Spell.Duration % 60000 / 1000).ToString("00")));
				else if (Spell.Duration > 60000)
					list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + "min"));

				else if (Spell.Duration != 0) list.Add("Duration: " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
				if (Spell.Power != 0) list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
				list.Add("Casting time: " + (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
				if (Spell.RecastDelay > 60000) list.Add("Recast time: " + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
				else if (Spell.RecastDelay > 0) list.Add("Recast time: " + (Spell.RecastDelay / 1000).ToString() + " sec");
				if (Spell.Concentration != 0) list.Add("Concentration cost: " + Spell.Concentration);
				if (Spell.Radius != 0) list.Add("Radius: " + Spell.Radius);

				// Recursion check
				byte nextDelveDepth = (byte)(DelveInfoDepth + 1);
				if (nextDelveDepth > MAX_DELVE_RECURSION)
				{
					list.Add("(recursion - see server logs)");
					log.ErrorFormat("Spell delve info recursion limit reached. Source spell ID: {0}, Sub-spell ID: {1}", m_spell.ID, m_procSpell.ID);
				}
				else
				{
					// add subspell specific informations
					list.Add(" "); //empty line
					list.Add("Sub-spell informations: ");
					list.Add(" "); //empty line
					ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(Caster, m_procSpell, m_procSpellLine);
					if (subSpellHandler == null)
					{
						list.Add("unable to create subspell handler: '" + SubSpellLineName + "', " + m_spell.Value);
						return list;
					}
					subSpellHandler.DelveInfoDepth = nextDelveDepth;
					// Get delve info of sub-spell
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

	/// <summary>
	/// This class contains data for OffensiveProc spells
	/// </summary>
	[SpellHandler(eSpellType.OffensiveProc)]
	public class OffensiveProcSpellHandler : BaseProcSpellHandler
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The event type to hook on
		/// </summary>
		protected override DOLEvent EventType
		{
			get { return GameLivingEvent.AttackFinished; }
		}

		/// <summary>
		/// The spell line name of the proc spell
		/// </summary>
		protected override string SubSpellLineName
		{
			get { return "OffensiveProc"; }
		}

        ///// <summary>
        ///// Handler fired whenever effect target attacks
        ///// </summary>
        ///// <param name="e"></param>
        ///// <param name="sender"></param>
        ///// <param name="arguments"></param>
        protected override void EventHandler(DOLEvent e, object sender, EventArgs arguments) { }

        public  void EventHandler(AttackData ad)
        {
            //AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;

            //if (args == null || args.AttackData == null || args.AttackData.AttackType == AttackData.eAttackType.Spell)
            //    return;

            //AttackData ad = args.AttackData;
            if (ad.AttackResult != eAttackResult.HitUnstyled && ad.AttackResult != eAttackResult.HitStyle)
                return;

            int baseChance = Spell.Frequency / 100;

            if (baseChance < 1)
                baseChance = 1;

            if (Util.Chance(baseChance))
            {
                ISpellHandler handler = ScriptMgr.CreateSpellHandler((GameLiving)ad.Attacker, m_procSpell, m_procSpellLine);
                
                if (handler != null)
                {
                    handler.Spell.Level = Spell.Level;

                    switch (m_procSpell.Target)
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

        // constructor
        public OffensiveProcSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

	/// <summary>
	/// This class contains data for DefensiveProc spells
	/// </summary>
	[SpellHandler(eSpellType.DefensiveProc)]
	public class DefensiveProcSpellHandler : BaseProcSpellHandler
	{
		/// <summary>
		/// The event type to hook on
		/// </summary>
		protected override DOLEvent EventType
		{
			get { return GameLivingEvent.AttackedByEnemy; }
		}

		/// <summary>
		/// The spell line name of the proc spell
		/// </summary>
		protected override string SubSpellLineName
		{
			get { return "DefensiveProc"; }
		}

		/// <summary>
		/// Handler fired whenever effect target is attacked
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected override void EventHandler(DOLEvent e, object sender, EventArgs arguments) { }

		public void EventHandler(AttackData ad)
		{
			//AttackedByEnemyEventArgs args = arguments as AttackedByEnemyEventArgs;
			//if (args == null || args.AttackData == null || args.AttackData.AttackType == AttackData.eAttackType.Spell)
			//	return;

			//AttackData ad = args.AttackData;
			if (ad.AttackResult != eAttackResult.HitUnstyled && ad.AttackResult != eAttackResult.HitStyle)
				return;

			int baseChance = Spell.Frequency / 100;

			if (baseChance < 1)
				baseChance = 1;

			if (Util.Chance(baseChance))
			{
				ISpellHandler handler = ScriptMgr.CreateSpellHandler(ad.Target, m_procSpell, m_procSpellLine);

				if (handler != null)
				{
					switch (m_procSpell.Target)
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

		// constructor
		public DefensiveProcSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
	
	[SpellHandler(eSpellType.OffensiveProcPvE)]
	public class OffensiveProcPvESpellHandler : OffensiveProcSpellHandler
	{
		/// <summary>
		/// Handler fired whenever effect target is attacked
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected override void EventHandler( DOLEvent e, object sender, EventArgs arguments )
		{
			AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
			if (args == null || args.AttackData == null)
				return;

			GameNPC target = args.AttackData.Target as GameNPC;
			
			if(target != null && !(target.Brain is IControlledBrain && ((IControlledBrain)target.Brain).GetPlayerOwner() != null))
				base.EventHandler(e, sender, arguments);
		}

		public OffensiveProcPvESpellHandler( GameLiving caster, Spell spell, SpellLine line ) : base( caster, spell, line ) { }
	}
}
