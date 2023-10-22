using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class QueenKulaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public QueenKulaBrain()
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
    #region Teleport Player & PlayerInCenter()
    public static GamePlayer randomtarget = null;
    public static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    public static bool IsTargetPicked = false;
    List<GamePlayer> Port_Enemys = new List<GamePlayer>();
    public int PickPlayer(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!Port_Enemys.Contains(player))
                        {
                            if (!player.effectListComponent.ContainsEffectForEffectType(EEffect.Mez) && !player.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff)
                                && (!player.effectListComponent.ContainsEffectForEffectType(EEffect.MezImmunity) || !player.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity))
                                && player != Body.TargetObject)
                            {
                                Port_Enemys.Add(player);
                            }
                        }
                    }
                }
            }
            if (Port_Enemys.Count == 0)
            {
                RandomTarget = null;//reset random target to null
                IsTargetPicked = false;
            }
            else
            {
                if (Port_Enemys.Count > 0)
                {
                    GamePlayer Target = (GamePlayer)Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
                    RandomTarget = Target;
                    if (RandomTarget.IsAlive && RandomTarget != null)
                    {
                        RandomTarget.MoveTo(160, 34128, 56095, 11898, 2124);
                        Port_Enemys.Remove(RandomTarget);
                        RandomTarget = null;//reset random target to null
                        IsTargetPicked = false;
                    }
                }
            }
        }
        return 0;
    }
    public static bool message1 = false;
    public void PlayerInCenter()
    {
        Point3D FrostPoint = new Point3D();
        FrostPoint.X = 34128; FrostPoint.Y = 56095; FrostPoint.Z = 11989;
        foreach(GamePlayer player in Body.GetPlayersInRadius(8000))
        {
            if (player != null)
            {
                if (player.IsAlive && player.IsWithinRadius(FrostPoint, 300))
                {
                    GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
                    Body.TargetObject = player;//set target to randomly picked
                    switch (Util.Random(1, 2))
                    {
                        case 1:
                            {//check here if target is not already mezzed or rotted or got mezzimmunity
                                if (!player.effectListComponent.ContainsEffectForEffectType(EEffect.Mez) && !player.effectListComponent.ContainsEffectForEffectType(EEffect.MezImmunity)
                                && !player.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff))
                                {
                                    Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast mezz
                                }
                            }
                            break;
                        case 2:
                            {//check here if target is not mezzed already or rooted or got snare immunity
                                if (!player.effectListComponent.ContainsEffectForEffectType(EEffect.Mez) && !player.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff) 
                                && !player.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity))
                                {
                                    Body.CastSpell(Root, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast root
                                }
                            }
                            break;
                    }
                    if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
                    Body.StartAttack(oldTarget);//start attack old target
                }
            }
        }
    }
    #endregion
    #region OnAttackedByEnemy()
    public static bool IsPulled1 = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (npc.IsAlive && npc.Brain is KingTuscarBrain brain)
                    {
                        if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
                            brain.AddToAggroList(target, 10);
                    }
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    #endregion
    #region Think()
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165083);
            Body.Strength = npcTemplate.Strength;
            IsTargetPicked =false;
            IsPulled1 = false;
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            message1 = false;
        }
        if (Body.TargetObject != null && HasAggro)
        {
            PlayerInCenter();//method that check if player enter to frozen circle
            if (message1 == false)
            {
                BroadcastMessage(String.Format("Queen Kula grins maliciously, 'So you got past all my Hrimthursa Guardians!" +
                    " These Hrimthursa are useless and arrogant! I'm going to show you what I've been wanting to teach you for a long time." +
                    " The merciless who are not afraid of death will survive in this brutal world! I am merciless I'm not afraid of death!'"));
                message1 = true;
            }
            if (IsTargetPicked == false)
            {
                if (KingTuscar.KingTuscarCount == 1)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickPlayer), Util.Random(15000, 25000));//timer to port and pick player
                }
                else if(KingTuscar.KingTuscarCount == 0)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickPlayer), Util.Random(8000, 12000));//timer to port and pick player
                }
                IsTargetPicked = true;
            }
            if(Body.TargetObject != null)
            {
                if (Body.TargetObject is GamePlayer)
                {
                    GamePlayer player = Body.TargetObject as GamePlayer;
                    if (player.effectListComponent.ContainsEffectForEffectType(EEffect.Mez))
                    {
                        RemoveFromAggroList(player);
                    }
                    if (player.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff))
                    {
                        RemoveFromAggroList(player);
                    }
                }
                if (KingTuscar.KingTuscarCount == 1)
                {
                    Body.Strength = 350;//if king is up it will deal less dmg
                }
                if (KingTuscar.KingTuscarCount == 0 || Body.HealthPercent <= 50)
                {
                    Body.Strength = 500;//king is dead so more dmg
                }
                Body.styleComponent.NextCombatStyle = QueenKula.taunt;
            }
        }
        base.Think();
    }
    #endregion
    #region Spells
    protected Spell m_mezSpell;
    protected Spell Mezz
    {
        get
        {
            if (m_mezSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 5;
                spell.ClientEffect = 3308;
                spell.Icon = 3308;
                spell.Name = "Mesmerize";
                spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
                spell.TooltipId = 3308;
                spell.Range = 1500;
                spell.SpellID = 11750;
                spell.Duration = 80;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = "Mesmerize";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Spirit; //Spirit DMG Type
                m_mezSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
            }
            return m_mezSpell;
        }
    }
    protected Spell m_RootSpell;
    protected Spell Root
    {
        get
        {
            if (m_RootSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 5;
                spell.ClientEffect = 177;
                spell.Icon = 177;
                spell.Name = "Ice Touch";
                spell.Description = "Target is rooted in place for the spell's duration.";
                spell.TooltipId = 177;
                spell.Value = 99;
                spell.Range = 1500;
                spell.SpellID = 11751;
                spell.Duration = 80;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.SpeedDecrease.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Spirit; //Spirit DMG Type
                m_RootSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RootSpell);
            }
            return m_RootSpell;
        }
    }
    #endregion
}