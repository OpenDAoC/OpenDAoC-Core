using System.Collections.Generic;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_BatteryOfLifeECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_BatteryOfLifeECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.BatteryOfLife;
            EffectService.RequestStartEffect(this);
        }
        
        private int m_HealthPool = 0;

        public override ushort Icon { get { return 4274; } }
        public override string Name { get { return "Battery Of Life"; } }
        public override bool HasPositiveEffect { get { return true; } }
        
        private bool HealthPoolRemains
        {
            get { return m_HealthPool > 0; }
        }

        public override void OnStartEffect()
        {
            m_HealthPool = (int) (1000 * (1 + (OwnerPlayer.GetModified(eProperty.BuffEffectiveness) * 0.01)));
            this.NextTick = 1;
            base.OnStartEffect();
        }

        public override void OnEffectPulse()
        {
            //delete if we're empty
            if(!HealthPoolRemains) EffectService.RequestImmediateCancelEffect(this);
            
            if (OwnerPlayer.Group != null)
            {
                Dictionary<GameLiving, int> livingToHeal = new Dictionary<GameLiving, int>();
                foreach (var living in OwnerPlayer.Group.GetMembersInTheGroup())
                {
                    if (living.IsWithinRadius(OwnerPlayer, 1500) 
                        && living.Health < living.MaxHealth 
                        && living != OwnerPlayer) //doesn't heal the owner
                    {
                        livingToHeal.Add(living, living.Health);
                    }
                }

                //Fen found this on stack overflow https://stackoverflow.com/questions/289/how-do-you-sort-a-dictionary-by-value
                //and https://stackoverflow.com/questions/3066182/convert-an-iorderedenumerablekeyvaluepairstring-int-into-a-dictionarystrin
                //Here we sort by health to put lowest at the first. I apologize for this line but it does the job o7
                var sortedLiving = (from entry in livingToHeal orderby entry.Value ascending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);

                foreach (var healed in sortedLiving)
                {
                    GameLiving currentLiving = healed.Key;
                    int difference = currentLiving.MaxHealth - currentLiving.Health;
                    
                    if(!HealthPoolRemains) EffectService.RequestImmediateCancelEffect(this);
                    if (m_HealthPool > difference)
                    {
                        if(currentLiving is GamePlayer ptarg) ptarg.Out.SendMessage($"{this.OwnerName}'s Battery of Life heals you for {difference} health points!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                        if(Owner is GamePlayer pcaster) pcaster.Out.SendMessage($"Your Battery of Life heals {currentLiving.Name} for {difference} health points!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                        currentLiving.ChangeHealth(OwnerPlayer, eHealthChangeType.Spell, difference);
                        m_HealthPool -= difference;
                    }
                    else
                    {
                        currentLiving.ChangeHealth(OwnerPlayer, eHealthChangeType.Spell, m_HealthPool);
                        m_HealthPool = 0;
                    }
                }
            }

            this.NextTick = 1000;
            base.OnEffectPulse();
        }
        
    }
}
