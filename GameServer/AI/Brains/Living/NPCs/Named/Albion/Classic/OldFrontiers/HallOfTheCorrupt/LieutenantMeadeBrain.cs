using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class LieutenantMeadeBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LieutenantMeadeBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    public static bool CanWalk = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            this.Body.Health = this.Body.MaxHealth;              
            CanWalk = false;
            lock (Body.effectListComponent.EffectsLock)
            {
                var effects = Body.effectListComponent.GetAllPulseEffects();
                for (int i = 0; i < effects.Count; i++)
                {
                    EcsPulseEffect effect = effects[i];
                    if (effect == null)
                        continue;

                    if (effect == null)
                        continue;
                    if (effect.SpellHandler.Spell.Pulse == 1)
                    {
                        EffectService.RequestCancelConcEffect(effect);//cancel here all pulse effect
                    }
                }
            }
        }
        if (Body.InCombat && HasAggro)
        {
            Body.CastSpell(Meade_Pulse, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            if (Body.TargetObject != null)
            {
                GameLiving living = Body.TargetObject as GameLiving;
                float angle = Body.TargetObject.GetAngle(Body);
                if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                {
                    Body.styleComponent.NextCombatStyle = LieutenantMeade.slam;//check if target has stun or immunity if not slam
                }
                if (living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                {
                    if (CanWalk == false)
                    {
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkSide), 500);
                        CanWalk = true;
                    }
                }
                if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
                {
                    Body.styleComponent.NextCombatStyle = LieutenantMeade.Side;
                }
                if(!living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                {
                    Body.styleComponent.NextCombatStyle = LieutenantMeade.Taunt;
                }
                if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) && !living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                {
                    CanWalk = false;//reset flag 
                }
            }
        }
        base.Think();
    }
    public int WalkSide(EcsGameTimer timer)
    {
        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            if (Body.TargetObject is GameLiving)
            {
                GameLiving living = Body.TargetObject as GameLiving;
                float angle = living.GetAngle(Body);
                Point2D positionalPoint;
                positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (90 * (4096.0 / 360.0))), 65);
                //Body.WalkTo(positionalPoint.X, positionalPoint.Y, living.Z, 280);
                Body.X = positionalPoint.X;
                Body.Y = positionalPoint.Y;
                Body.Z = living.Z;
                Body.Heading = 1250;
            }
        }
        return 0;
    }
    private Spell m_Meade_Pulse;

    private Spell Meade_Pulse
    {
        get
        {
            if (m_Meade_Pulse == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 8;
                spell.ClientEffect = 9637;
                spell.Icon = 9637;
                spell.TooltipId = 9637;
                spell.Value = 21;
                spell.Name = "Aching Curse";
                spell.Description = "Target does 21% less damage with melee attacks.";
                spell.Message1 = "Your attacks lose effectiveness as your will to fight is sapped!";
                spell.Message2 = "{0} seems to have lost some will to fight!";
                spell.Pulse = 1;
                spell.Duration = 10;
                spell.Frequency = 100;
                spell.Radius = 350;
                spell.Range = 0;
                spell.SpellID = 11782;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.MeleeDamageDebuff.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Spirit;
                m_Meade_Pulse = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Meade_Pulse);
            }
            return m_Meade_Pulse;
        }
    }
}