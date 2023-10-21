﻿using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS
{
    public class ChargeEcsAbilityEffect : EcsGameAbilityEffect
    {
        public ChargeEcsAbilityEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.Charge;
            EffectService.RequestStartEffect(this);
        }

        protected ushort m_startModel = 0;

        public override ushort Icon {
            get
            {
                if (Owner is GameNpc) return 411;
                else return 3034;
            }
        }
        public override string Name { get { return "Charge"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            //Send messages
            if (OwnerPlayer != null)
            {
                // "You begin to charge wildly!"
                OwnerPlayer.Out.SendMessage($"You are now charging {OwnerPlayer.TargetObject?.Name}!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                // "{0} begins charging wildly!"
                MessageUtil.SystemToArea(OwnerPlayer, LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.ChargeEffect.AreaStartCharge",OwnerPlayer.GetName(0, true)), EChatType.CT_System, OwnerPlayer);
            }
            else if (Owner is GameNpc)
            {
                IControlledBrain icb = ((GameNpc)Owner).Brain as IControlledBrain;
                if (icb != null && icb.Body != null)
                {
                    GamePlayer playerowner = icb.GetPlayerOwner();

                    if (playerowner != null)
                    {
                        playerowner.Out.SendMessage("The " + icb.Body.Name + " charges its prey!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
                    }
                }
            }
            else
                return;

            //m_startTick = living.CurrentRegion.Time;
            foreach (GamePlayer t_player in Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                t_player.Out.SendSpellEffectAnimation(Owner, Owner, 7035, 0, false, 1);
            }

            ////sets player into combat mode
            //living.LastAttackTickPvP = m_startTick;
            //ArrayList speedSpells = new ArrayList();
            //lock (living.EffectList)
            //{
            //    foreach (IGameEffect effect in living.EffectList)
            //    {
            //        if (effect is GameSpellEffect == false) continue;
            //        if ((effect as GameSpellEffect).Spell.SpellType == eSpellType.SpeedEnhancement)
            //            speedSpells.Add(effect);
            //    }
            //}
            //foreach (GameSpellEffect spell in speedSpells)
            //    spell.Cancel(false);
            //m_living.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, this, PropertyCalc.MaxSpeedCalculator.SPEED3);
            //m_living.TempProperties.setProperty("Charging", true);
            //if (m_living is GamePlayer)
            //    ((GamePlayer)m_living).Out.SendUpdateMaxSpeed();
            //StartTimers();
            //m_living.EffectList.Add(this);
        }
        public override void OnStopEffect()
        {
            Cancel(false);
        }

        public void Cancel(bool playerCancel)
        {
            //m_living.TempProperties.removeProperty("Charging");
            //m_living.EffectList.Remove(this);
            //m_living.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);
            //Send messages
            if (OwnerPlayer != null)
            {
                //GamePlayer player = m_living as GamePlayer;
                //player.Out.SendUpdateMaxSpeed();
                
                // "You no longer seem so crazy!"
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.ChargeEffect.EndCharge"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                // "{0} ceases their charge!"
                MessageUtil.SystemToArea(OwnerPlayer, LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.ChargeEffect.AreaEndCharge", OwnerPlayer.GetName(0, true)), EChatType.CT_System, OwnerPlayer);
            }
            else if (Owner is GameNpc)
            {
                IControlledBrain icb = ((GameNpc)Owner).Brain as IControlledBrain;
                if (icb != null && icb.Body != null)
                {
                    GamePlayer playerowner = icb.GetPlayerOwner();

                    if (playerowner != null)
                    {
                        playerowner.Out.SendMessage("The " + icb.Body.Name + " ceases its charge!", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
                    }
                }
            }
        }
    }
}
