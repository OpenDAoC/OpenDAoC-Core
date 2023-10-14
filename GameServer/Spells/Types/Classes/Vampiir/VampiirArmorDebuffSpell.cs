using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler("VampiirArmorDebuff")]
	public class VampiirArmorDebuffSpell : SpellHandler
	{
		private static EArmorSlot[] slots = new EArmorSlot[] { EArmorSlot.HEAD, EArmorSlot.TORSO, EArmorSlot.LEGS,  };
		private EArmorSlot m_slot = EArmorSlot.NOTSET;
		public EArmorSlot Slot { get { return m_slot; } }
		private int old_item_af = 0;
		private int old_item_abs = 0;
		private DbInventoryItem item = null;
		protected GamePlayer player=null;

		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, EAttackType.Spell, Caster);
			player = effect.Owner as GamePlayer;
			if (player == null) return;
			int slot=Util.Random(0, 2);
			m_slot = slots[slot];
			string msg = GlobalConstants.SlotToName((int)m_slot);
			MessageToCaster("You debuff " + effect.Owner.Name + "'s " + msg+"", EChatType.CT_Spell);
			foreach (GamePlayer visPlayer in player.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
				visPlayer.Out.SendSpellEffectAnimation(player, player, (ushort)(13180+slot), 0, false, 0x01);

			item = player.Inventory.GetItem((EInventorySlot)m_slot);
			
			if(item!=null)
			{
				old_item_af=item.DPS_AF;
				old_item_abs=item.SPD_ABS;
				item.DPS_AF -= (int)Spell.Value;
				item.SPD_ABS -= (int)Spell.ResurrectMana;
				if(item.DPS_AF<0) item.DPS_AF=0;
				if(item.SPD_ABS<0) item.SPD_ABS=0;
			
				player.Client.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { item });
				player.Out.SendCharStatsUpdate();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
				player.Out.SendUpdateWeaponAndArmorStats();
				player.Out.SendCharResistsUpdate();
				
				GameEventMgr.AddHandler(player,GamePlayerEvent.Linkdeath, new CoreEventHandler(EventAction));
				GameEventMgr.AddHandler(player,GamePlayerEvent.Quit, new CoreEventHandler(EventAction));
				GameEventMgr.AddHandler(player,GamePlayerEvent.RegionChanged, new CoreEventHandler(EventAction));
				GameEventMgr.AddHandler(player,GameLivingEvent.Dying, new CoreEventHandler(EventAction));
			}

			base.OnEffectStart(effect);
		}
		
		public void EventAction(CoreEvent e, object sender, EventArgs arguments)
		{
            if(player== null) return;
			RemoveEffect();
		}
		
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			RemoveEffect();
			return base.OnEffectExpires(effect,noMessages);
		}
		
		public void RemoveEffect()
		{
			if(player==null) return;
            GameSpellEffect effect=FindEffectOnTarget(player,this);
            if (effect!=null) effect.Cancel(false);
			if(item==null) return;
			
			item.DPS_AF=old_item_af;
			item.SPD_ABS=old_item_abs;
			
			player.Client.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { item });
			player.Out.SendCharStatsUpdate();
			player.UpdatePlayerStatus();
			player.Out.SendUpdatePlayer();
			player.Out.SendUpdateWeaponAndArmorStats();
			player.Out.SendCharResistsUpdate();
			
			GameEventMgr.RemoveHandler(player,GamePlayerEvent.Linkdeath, new CoreEventHandler(EventAction));
			GameEventMgr.RemoveHandler(player,GamePlayerEvent.Quit, new CoreEventHandler(EventAction));
			GameEventMgr.RemoveHandler(player,GamePlayerEvent.RegionChanged, new CoreEventHandler(EventAction));
			GameEventMgr.RemoveHandler(player,GameLivingEvent.Dying, new CoreEventHandler(EventAction));

		}
		
		public override IList<string> DelveInfo 
		{
			get 
			{
				var list = new List<string>(16);
				list.Add("Name: " + Spell.Name+"\n");
				list.Add("Description: " + Spell.Description+"\n");
				list.Add("Target: " + Spell.Target);
				list.Add("Casting time: " + (Spell.CastTime*0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
				if (Spell.Duration >= ushort.MaxValue*1000)
					list.Add("Duration: Permanent.");
				else if (Spell.Duration > 60000)
					list.Add(string.Format("Duration: {0}:{1} min", Spell.Duration/60000, (Spell.Duration%60000/1000).ToString("00")));
				else if (Spell.Duration != 0)
					list.Add("Duration: " + (Spell.Duration/1000).ToString("0' sec';'Permanent.';'Permanent.'"));
				if (Spell.RecastDelay > 60000)
					list.Add("Recast time: " + (Spell.RecastDelay/60000).ToString() + ":" + (Spell.RecastDelay%60000/1000).ToString("00") + " min");
				else if (Spell.RecastDelay > 0)
					list.Add("Recast time: " + (Spell.RecastDelay/1000).ToString() + " sec");
				if(Spell.Range != 0) list.Add("Range: " + Spell.Range);
				if(Spell.Power != 0) list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
				list.Add("Debuff Absorption : " + Spell.ResurrectMana);
				list.Add("Debuff Armor Factor : " + Spell.Value);
				return list;
			}
		}
		
		public VampiirArmorDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}