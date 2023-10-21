using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS.Effects
{
    public class NfRaSputinsLegacyEffect : TimedEffect
    {
		private GamePlayer m_player = null;

        public NfRaSputinsLegacyEffect() : base(20000) { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
			m_player = target as GamePlayer;
			GameEventMgr.AddHandler(m_player, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
            GameEventMgr.AddHandler(m_player, GameLivingEvent.Dying, new CoreEventHandler(OnRemove));
			GameEventMgr.AddHandler(m_player, GamePlayerEvent.Quit, new CoreEventHandler(OnRemove));
			GameEventMgr.AddHandler(m_player, GamePlayerEvent.Linkdeath, new CoreEventHandler(OnRemove));
			GameEventMgr.AddHandler(m_player, GamePlayerEvent.RegionChanged, new CoreEventHandler(OnRemove));
        }

        private void OnAttacked(CoreEvent e, object sender, EventArgs args)
        {
            AttackedByEnemyEventArgs attackArgs = args as AttackedByEnemyEventArgs;
			if (attackArgs == null) return;			
			AttackData ad = null;
            ad = attackArgs.AttackData;

            int damageAbsorbed = (int)(ad.Damage + ad.CriticalDamage);
			
			if(m_player.Health<(damageAbsorbed+(int)Math.Round((double)m_player.MaxHealth/20))) m_player.Health+=damageAbsorbed;

        }

        private void OnRemove(CoreEvent e, object sender, EventArgs args)
        {
            //((GamePlayer)Owner).Out.SendMessage("Sputins Legacy grants you a damage immunity!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
			
			Stop();
        }

        public override void Stop()
        {
			if (m_player.EffectList.GetOfType<NfRaSputinsLegacyEffect>() != null) m_player.EffectList.Remove(this);
			GameEventMgr.RemoveHandler(m_player, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
            GameEventMgr.RemoveHandler(m_player, GameLivingEvent.Dying, new CoreEventHandler(OnRemove));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.Quit, new CoreEventHandler(OnRemove));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.Linkdeath, new CoreEventHandler(OnRemove));
			GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.RegionChanged, new CoreEventHandler(OnRemove));
            base.Stop();
        }
		
		
		
		
		
		
		
		

        public override string Name { get { return "Sputins Legacy"; } }

        public override ushort Icon { get { return 3069; } }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("The Healer won't die for 30sec.");
                return list;
            }
        }
    }
}