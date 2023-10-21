using System;
using Core.Database.Tables;
using Core.Events;
using Core.GS.ECS;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class MaldaharTheGlimmerPrinceBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public MaldaharTheGlimmerPrinceBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    public override void AttackMostWanted()
    {
        if (Body.IsWithinRadius(Body.TargetObject, Body.AttackRange + 250))
        {
            switch (Util.Random(1, 2))
            {
                case 1:
                    if (Util.Chance(4))
                    {
                        Body.TurnTo(Body.TargetObject);
                        Body.CastSpell(LifeTap, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }

                    break;
                case 2:
                    if (Util.Chance(4))
                    {
                        Body.TurnTo(Body.TargetObject);
                        Body.CastSpell(PBAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }

                    break;
            }
        }

        base.AttackMostWanted();
    }

    public override void Think()
    {
        if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
        {
            Body.Health = Body.MaxHealth;
            Body.ReturnToSpawnPoint(NpcMovementComponent.DEFAULT_WALK_SPEED);
        }

        base.Think();
    }

    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);

        if (e != GameObjectEvent.TakeDamage && e != GameLivingEvent.EnemyHealed) return;
        GameObject source = (args as TakeDamageEventArgs)?.DamageSource;
        if (source == null) return;
        if (Body.IsWithinRadius(source, Body.AttackRange + 250)) return;
        switch (Util.Random(1, 2))
        {
            case 1:
                if (Util.Chance(4))
                {
                    Body.TurnTo(Body.TargetObject);
                    Body.CastSpell(LifeTap, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }

                break;
            case 2:
                if (Util.Chance(4))
                {
                    Body.TurnTo(Body.TargetObject);
                    Body.CastSpell(PBAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }

                break;
        }
    }

    #region Lifetap Spell

    private Spell m_Lifetap;

    private Spell LifeTap
    {
        get
        {
            if (m_Lifetap == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.ClientEffect = 710;
                spell.RecastDelay = 10;
                spell.Icon = 710;
                spell.TooltipId = 710;
                spell.Value = -100;
                spell.LifeDrainReturn = 100;
                spell.Damage = 1150;
                spell.Range = 2500;
                spell.Radius = 250;
                spell.SpellID = 710;
                spell.Target = "Enemy";
                spell.Type = "Lifedrain";
                spell.DamageType = (int) EDamageType.Body;
                m_Lifetap = new Spell(spell, 60);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Lifetap);
            }

            return m_Lifetap;
        }
    }

    #endregion

    #region PBAoe Spell

    private Spell m_PBAoe;

    private Spell PBAoe
    {
        get
        {
            if (m_PBAoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.ClientEffect = 4204;
                spell.Power = 0;
                spell.RecastDelay = 10;
                spell.Icon = 4204;
                spell.TooltipId = 4204;
                spell.SpellGroup = 4201;
                spell.Damage = 1150;
                spell.Range = 2500;
                spell.Radius = 550;
                spell.SpellID = 4204;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                spell.DamageType = (int) EDamageType.Energy;
                m_PBAoe = new Spell(spell, 60);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_PBAoe);
            }

            return m_PBAoe;
        }
    }

    #endregion
}