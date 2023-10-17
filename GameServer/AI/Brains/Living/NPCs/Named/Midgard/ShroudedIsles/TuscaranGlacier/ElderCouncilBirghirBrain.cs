using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

public class ElderCouncilBirghirBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ElderCouncilBirghirBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static GamePlayer randomtarget = null;
    public static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    public int PickPlayer(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            IList enemies = new ArrayList(AggroTable.Keys);
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!AggroTable.ContainsKey(player))
                            AggroTable.Add(player, 1);
                    }
                }
            }
            if (enemies.Count == 0)
            {/*do nothing*/}
            else
            {
                List<GameLiving> damage_enemies = new List<GameLiving>();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null)
                        continue;
                    if (!(enemies[i] is GameLiving))
                        continue;
                    if (!(enemies[i] as GameLiving).IsAlive)
                        continue;
                    GameLiving living = null;
                    living = enemies[i] as GameLiving;
                    if (living.IsVisibleTo(Body) && Body.TargetInView && living is GamePlayer)
                    {
                        damage_enemies.Add(enemies[i] as GameLiving);
                    }
                }
                if (damage_enemies.Count > 0)
                {
                    GamePlayer Target = (GamePlayer)damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                    RandomTarget = Target; //randomly picked target is now RandomTarget
                    if (RandomTarget.IsVisibleTo(Body) && Body.TargetInView && RandomTarget != null && RandomTarget.IsAlive)
                    {
                        GameLiving oldTarget = Body.TargetObject as GameLiving; //old target
                        Body.TargetObject = RandomTarget; //set target to randomly picked
                        Body.TurnTo(RandomTarget);
                        switch (Util.Random(1, 2)) //pick one of 2 spells to cast
                        {
                            case 1:
                                {
                                    Body.CastSpell(Icelord_Bolt,SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); //bolt
                                }
                                break;
                            case 2:
                                {
                                    Body.CastSpell(Icelord_dd,SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); //dd cold
                                }
                                break;
                        }

                        RandomTarget = null; //reset random target to null
                        if (oldTarget != null) Body.TargetObject = oldTarget; //return to old target
                        Body.StartAttack(oldTarget); //start attack old target
                        IsTargetPicked = false;
                    }
                }
            }
        }
        return 0;
    }
    public static bool IsTargetPicked = false;
    public static bool message1 = false;
    public static bool IsPulled = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled == false)
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is ElderCouncilGuthlacBrain)
                    {
                        AddAggroListTo(npc.Brain as ElderCouncilGuthlacBrain);
                        IsPulled = true;
                    }
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            message1 = false;
            IsTargetPicked = false;
            IsPulled = false;
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            Body.Health = Body.MaxHealth;

        if (HasAggro && Body.TargetObject != null)
        {
            if (message1 == false)
            {
                if (Body.TargetObject is GamePlayer && Body.TargetObject != null)
                {
                    GamePlayer player = Body.TargetObject as GamePlayer;
                    if (player != null && player.IsAlive)
                    {
                        BroadcastMessage(String.Format(Body.Name + " Impossible! An ugly " + player.PlayerClass.Name + " there? How could this be? Guthlac, we must defend our Queen and King!"));
                        message1 = true;
                    }
                }
            }
            if (IsTargetPicked == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickPlayer), Util.Random(15000, 20000));
                IsTargetPicked = true;
            }
            if(!Body.IsCasting)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (Util.Chance(20))
                {                    
                    if(target != null && target.IsAlive && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StrConDebuff))
                        Body.CastSpell(Icelord_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
                if (Util.Chance(20))
                {
                    if (target != null && target.IsAlive && !target.effectListComponent.ContainsEffectForEffectType(EEffect.MeleeHasteDebuff))
                        Body.CastSpell(Icelord_Haste_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
        }
        base.Think();
    }
    #region Spells
    private Spell m_Icelord_SC_Debuff;
    private Spell Icelord_SC_Debuff
    {
        get
        {
            if (m_Icelord_SC_Debuff == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 30;
                spell.Duration = 60;
                spell.ClientEffect = 2767;
                spell.Icon = 2767;
                spell.Name = "Debuff S/C";
                spell.TooltipId = 2767;
                spell.Range = 1500;
                spell.Value = 80;
                spell.Radius = 450;
                spell.SpellID = 11928;
                spell.Target = "Enemy";
                spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_Icelord_SC_Debuff = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_SC_Debuff);
            }
            return m_Icelord_SC_Debuff;
        }
    }
    private Spell m_Icelord_Haste_Debuff;
    private Spell Icelord_Haste_Debuff
    {
        get
        {
            if (m_Icelord_Haste_Debuff == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 30;
                spell.Duration = 60;
                spell.ClientEffect = 5427;
                spell.Icon = 5427;
                spell.Name = "Haste Debuff";
                spell.TooltipId = 5427;
                spell.Range = 1500;
                spell.Value = 19;
                spell.SpellID = 11929;
                spell.Target = "Enemy";
                spell.Type = ESpellType.CombatSpeedDebuff.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_Icelord_Haste_Debuff = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_Haste_Debuff);
            }
            return m_Icelord_Haste_Debuff;
        }
    }
    private Spell m_Icelord_Bolt;
    private Spell Icelord_Bolt
    {
        get
        {
            if (m_Icelord_Bolt == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 2;
                spell.RecastDelay = 0;
                spell.ClientEffect = 4559;
                spell.Icon = 4559;
                spell.TooltipId = 4559;
                spell.Damage = 400;
                spell.Name = "Frost Sphere";
                spell.Range = 1800;
                spell.SpellID = 11749;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.Bolt.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_Icelord_Bolt = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_Bolt);
            }
            return m_Icelord_Bolt;
        }
    }
    private Spell m_Icelord_dd;
    private Spell Icelord_dd
    {
        get
        {
            if (m_Icelord_dd == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 0;
                spell.ClientEffect = 2511;
                spell.Icon = 2511;
                spell.TooltipId = 2511;
                spell.Damage = 650;
                spell.Name = "Frost Strike";
                spell.Range = 1800;
                spell.SpellID = 11750;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_Icelord_dd = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_dd);
            }
            return m_Icelord_dd;
        }
    }
    #endregion
}