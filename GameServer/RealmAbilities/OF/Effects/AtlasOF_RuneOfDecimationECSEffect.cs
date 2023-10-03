using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class AtlasOF_RuneOfDecimationECSEffect : ECSGameSpellEffect
    {
        public AtlasOF_RuneOfDecimationECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.RuneOfDecimation;
            this.NextTick = 1;
            //EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 7153; } }
        
        public override string Name { get { return "Rune Of Decimation"; } }
        public override bool HasPositiveEffect { get { return false; } }

        public override void OnEffectPulse()
        {
            bool shouldDetonate = false;
            GameLiving triggeringLiving = null;
            List<GameLiving> DetonateTargets = new List<GameLiving>();
            foreach (GamePlayer player in Owner?.GetPlayersInRadius((ushort)SpellHandler.Spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Owner, player, true))
                {
                    shouldDetonate = true;
                    if (triggeringLiving == null) triggeringLiving = player; 
                    DetonateTargets.Add(player);
                }
            }

            foreach (GameNPC living in Owner.GetNPCsInRadius((ushort)SpellHandler.Spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Owner, living, true))
                {
                    shouldDetonate = true;
                    if (triggeringLiving == null) triggeringLiving = living;
                    DetonateTargets.Add(living);
                }
            }

            if (shouldDetonate)
            {
                foreach (GamePlayer i_player in Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    i_player.Out.SendMessage($"{SpellHandler.Caster.Name}'s Rune of Decimation trap detonates!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                
                foreach (var target in DetonateTargets)
                {
                    if (triggeringLiving == null) continue;
                    
                    AttackData ad = new AttackData();
                    ad.Attacker = SpellHandler.Caster;
                    ad.Target = target;
                    ad.AttackType = AttackData.eAttackType.Spell;
                    ad.SpellHandler = SpellHandler;
                    ad.AttackResult = eAttackResult.HitUnstyled;
                    ad.IsSpellResisted = false;
                    ad.Damage = CalculateDamageWithFalloff((int)SpellHandler.Spell.Damage, triggeringLiving, target);
                    ad.DamageType = SpellHandler.Spell.DamageType;
                    
                    ad.Modifier = (int)(ad.Damage * (ad.Target.GetResist(ad.DamageType)) / -100.0);
                    ad.Damage += ad.Modifier;
                    
                    if (target is GamePlayer pl)
                    {
                        pl.Out.SendMessage($"You take {ad.Damage}({ad.Modifier}) damage from a Rune of Decimation!", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                        pl.Out.SendSpellCastAnimation(Owner, 7153, 1);
                    }

                    if(SpellHandler.Caster is GamePlayer c)
                        c.Out.SendMessage($"Your Rune of Decimation deals {ad.Damage}({ad.Modifier}) damage to {target?.Name}!", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    
                    target.DealDamage(ad);
                    //target.TakeDamage(SpellHandler.Caster, SpellHandler.Spell.DamageType, (int) SpellHandler.Spell.Damage, 0);
                }
                
                if(SpellHandler.Caster is GamePlayer pl2)
                    pl2.Out.SendSpellCastAnimation(Owner, 7153, 1);
                
                
                Owner.Die(Owner);
            }
            else
            {
                NextTick = 1000; //check again in a second
            }
            
            
            base.OnEffectPulse();
        }
        
        private int CalculateDamageWithFalloff(int initialDamage, GameLiving initTarget, GameLiving aetarget)
        {
            //Console.WriteLine($"initial {initialDamage} caster {initTarget} target {aetarget}");
            int modDamage = (int)Math.Round((decimal) (initialDamage * ((500-(initTarget.GetDistance(new Point2D(aetarget.X, aetarget.Y)))) / 500.0)));
            //Console.WriteLine($"distance {((500-(initTarget.GetDistance(new Point2D(aetarget.X, aetarget.Y)))) / 500.0)} Mod {modDamage}");
            return modDamage;
        }
    }
}
