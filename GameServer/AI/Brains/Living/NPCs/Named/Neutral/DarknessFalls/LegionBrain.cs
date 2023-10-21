using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.AI.Brain;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Scripts;
using log4net;

namespace Core.GS.AI.Brains;

#region Legion
public class LegionBrain : EpicBossBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    
    public LegionBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 850;
    }
    public static bool RemoveAdds = false;
    public static bool IsCreatingSouls = false;
    public static bool CanThrow = false;
    public static bool CanPbaoe = false;
    #region Health check bools
    public static bool adds1 = false;
    public static bool adds2 = false;
    public static bool adds3 = false;
    public static bool adds4 = false;
    public static bool adds5 = false;
    public static bool adds6 = false;
    public static bool adds7 = false;
    public static bool adds8 = false;
    public static bool adds9 = false;
    public static bool adds10 = false;
    public static bool adds11 = false;
    public static bool adds12 = false;
    public static bool adds13 = false;
    public static bool adds14 = false;
    public static bool adds15 = false;
    public static bool adds16 = false;
    public static bool adds17 = false;
    public static bool adds18 = false;
    public static bool adds19 = false;
    #endregion

    public override void Think()
    {
        if(!CheckProximityAggro())
        {
            IsCreatingSouls = false;
            CanThrow = false;
            #region Health check bools
            adds1 = false;
            adds2 = false;
            adds3 = false;
            adds4 = false;
            adds5 = false;
            adds6 = false;
            adds7 = false;
            adds8 = false;
            adds9 = false;
            adds10 = false;
            adds11 = false;
            adds12 = false;
            adds13 = false;
            adds14 = false;
            adds15 = false;
            adds16 = false;
            adds17 = false;
            adds18 = false;
            adds19 = false;
            #endregion

            if (Port_Enemys.Count > 0)//clear port players
                Port_Enemys.Clear();
            if (randomlyPickedPlayers.Count > 0)//clear randomly picked players
                randomlyPickedPlayers.Clear();

            var throwPlayer = Body.TempProperties.GetProperty<EcsGameTimer>("legion_throw");//cancel teleport
            if (throwPlayer != null)
            {
                throwPlayer.Stop();
                Body.TempProperties.RemoveProperty("legion_throw");
            }
            var castaoe = Body.TempProperties.GetProperty<EcsGameTimer>("legion_castaoe");//cancel cast aoe
            if (castaoe != null)
            {
                castaoe.Stop();
                Body.TempProperties.RemoveProperty("legion_castaoe");
            }
        }
        if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
        {
            Body.Health = Body.MaxHealth;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.Brain is LegionAddBrain)
                        npc.RemoveFromWorld();
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            DestroyDamnBubble();
            if(bladeturnConsumed >= 5 && !CanPbaoe)
            {
                ReleaseAoeLifetap();
                EcsGameTimer castAoe = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetAoe), 10000);
                Body.TempProperties.SetProperty("legion_castaoe", castAoe);
                CanPbaoe = true;
            }
            #region Legion health checks
            if (Body.HealthPercent <= 95 && Body.HealthPercent > 90 && !adds1)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds1 = true;
            }
            if (Body.HealthPercent <= 90 && Body.HealthPercent > 85 && !adds2)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds2 = true;
            }
            if (Body.HealthPercent <= 85 && Body.HealthPercent > 80 && !adds3)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds3 = true;
            }
            if (Body.HealthPercent <= 80 && Body.HealthPercent > 75 && !adds4)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds4 = true;
            }
            if (Body.HealthPercent <= 75 && Body.HealthPercent > 70 && !adds5)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds5 = true;
            }
            if (Body.HealthPercent <= 70 && Body.HealthPercent > 65 && !adds6)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds6 = true;
            }
            if (Body.HealthPercent <= 65 && Body.HealthPercent > 60 && !adds7)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds7 = true;
            }
            if (Body.HealthPercent <= 60 && Body.HealthPercent > 55 && !adds8)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds8 = true;
            }
            if (Body.HealthPercent <= 55 && Body.HealthPercent > 50 && !adds9)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds9 = true;
            }
            if (Body.HealthPercent <= 50 && Body.HealthPercent > 45 && !adds10)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds10 = true;
            }
            if (Body.HealthPercent <= 45 && Body.HealthPercent > 40 && !adds11)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds11 = true;
            }
            if (Body.HealthPercent <= 40 && Body.HealthPercent > 35 && !adds12)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds12 = true;
            }
            if (Body.HealthPercent <= 35 && Body.HealthPercent > 30 && !adds13)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds13 = true;
            }
            if (Body.HealthPercent <= 30 && Body.HealthPercent > 25 && !adds14)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds14 = true;
            }
            if (Body.HealthPercent <= 25 && Body.HealthPercent > 20 && !adds15)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds15 = true;
            }
            if (Body.HealthPercent <= 20 && Body.HealthPercent > 15 && !adds16)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds16 = true;
            }
            if (Body.HealthPercent <= 15 && Body.HealthPercent > 10 && !adds17)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds17 = true;
            }
            if (Body.HealthPercent <= 10 && Body.HealthPercent > 5 && !adds18)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds18 = true;
            }
            if (Body.HealthPercent <= 5 && Body.HealthPercent > 0 && !adds19)
            {
                SpawnAdds();
                spawnAmount = 0;
                PlayerCountInLegionLair = 0;
                adds19 = true;
            }
            #endregion
            if (!CanThrow)
            {
                EcsGameTimer throwPlayer = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(40000, 65000));//throw players
                Body.TempProperties.SetProperty("legion_throw", throwPlayer);
                CanThrow = true;
            }
        }

        base.Think();
    }
    private int bladeturnConsumed = 0;
    private void DestroyDamnBubble()
    {
        if (Body.TargetObject != null && HasAggro)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (Util.Chance(100))
            {
                if (target.effectListComponent.ContainsEffectForEffectType(EEffect.Bladeturn) && target != null && target.IsAlive)
                {
                    var effect = EffectListService.GetEffectOnTarget(target, EEffect.Bladeturn);
                    if (effect != null)
                    {
                        EffectService.RequestImmediateCancelEffect(effect);//remove bladeturn effect here
                        bladeturnConsumed++;
                        if(target is GamePlayer player)
                        {
                            if (player != null && player.IsAlive)
                                player.Out.SendMessage("Legion consume your bladeturn effect!", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
                        }
                    }
                }
            }
        }
    }
    public void ReleaseAoeLifetap()
    {
        if (Body.TargetObject != null)
        {
            if (!Body.IsCasting)
            {
                BroadcastMessage("Legion unleashing massive soul consumption blast.");
                Body.CastSpell(LegionLifetapAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
        }
        bladeturnConsumed = 0;
    }
    private int ResetAoe(EcsGameTimer timer)
    {
        CanPbaoe = false;
        return 0;
    }
    public static int PlayerCountInLegionLair = 0;
    public static int spawnAmount = 0;
    private void SpawnAdds()
    {
        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            foreach (GamePlayer playerNearby in Body.GetPlayersInRadius(2000))
            {
                if (playerNearby != null && playerNearby.Client.Account.PrivLevel == 1)
                {
                    PlayerCountInLegionLair++;
                }
                if (PlayerCountInLegionLair < 4)
                    spawnAmount = 1;
                if (PlayerCountInLegionLair > 4)
                    spawnAmount = PlayerCountInLegionLair / 4;
            }
        }
        if (PlayerCountInLegionLair > 0 && spawnAmount > 0)
        {
            //log.Warn("PlayerCountInLegionLair = " + PlayerCountInLegionLair + " and spawnAmount = "+ spawnAmount);
            for (int i = 0; i < spawnAmount; i++)
            {
                var level = Util.Random(52, 58);

                LegionAdd add = new LegionAdd();
                add.X = Body.X + Util.Random(-150, 150);
                add.Y = Body.Y + Util.Random(-150, 150);
                add.Z = Body.Z;
                add.CurrentRegionID = 249;
                add.IsWorthReward = false;
                add.Level = (byte)level;
                add.AddToWorld();
            }
        }
    }
    #region Legion Port
    List<GamePlayer> Port_Enemys = new List<GamePlayer>();
    List<GamePlayer> randomlyPickedPlayers = new List<GamePlayer>();
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }
    public static List<t> GetRandomElements<t>(IEnumerable<t> list, int elementsCount)//pick X elements from list
    {
        return list.OrderBy(x => Guid.NewGuid()).Take(elementsCount).ToList();
    }
    private static int topPlayersToIngore = 5;//we determine here how many players from top aggro table will be ignored in teleporting
    private static Random random = new Random();
    private int ThrowPlayer(EcsGameTimer timer)
    {
        if (Body.IsAlive && HasAggro)
        {
            IDictionary<GameLiving, long> aggroList = (Body.Brain as LegionBrain).AggroTable;
            IOrderedEnumerable<KeyValuePair<GameLiving, long>> tempAggroTable = aggroList.OrderByDescending(x => x.Value).Skip(topPlayersToIngore).OrderBy(x => random.Next());
            foreach(KeyValuePair<GameLiving,long> items in tempAggroTable)
            {
                if (items.Key != null && items.Key.IsAlive && items.Key is GamePlayer player)
                {
                    if (!Port_Enemys.Contains(player))
                    {
                        Port_Enemys.Add(player);
                        //log.Debug($"Adding player: Name = {player.Name}");
                    }
                }
            }

            if (Port_Enemys.Count > 0)
            {
                randomlyPickedPlayers = GetRandomElements(Port_Enemys, Util.Random(8, 16));//pick 5-8players from list to new list

                if (randomlyPickedPlayers.Count > 0)
                {
                    foreach (GamePlayer player in randomlyPickedPlayers)
                    {
                        if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && HasAggro && player.IsWithinRadius(Body, 2500))
                        {
                            player.MoveTo(249, 48200, 49566, 20833, 1028);
                            //player.BroadcastUpdate();
                        }
                    }
                    randomlyPickedPlayers.Clear();//clear list after port
                }
            }
            CanThrow = false;// set to false, so can throw again
            Port_Enemys.Clear();
        }
        return 0;
    }
    #endregion
    #region Spells
    private Spell m_LegionLifetapAoe;
    public Spell LegionLifetapAoe
    {
        get
        {
            if (m_LegionLifetapAoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.Power = 0;
                spell.RecastDelay = 5;
                spell.ClientEffect = 9191;
                spell.Icon = 9191;
                spell.Damage = 1000;
                spell.DamageType = (int)EDamageType.Body;
                spell.Name = "Lifetap";
                spell.Range = 0;
                spell.Radius = 1000;
                spell.SpellID = 12013;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                m_LegionLifetapAoe = new Spell(spell, 60);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LegionLifetapAoe);
            }
            return m_LegionLifetapAoe;
        }
    }
    #endregion
}
#endregion Legion

#region Legion adds
public class LegionAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LegionAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
    }

    public override void Think()
    {
        if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
        {
            Body.RemoveFromWorld();
        }
        base.Think();
    }
}
#endregion Legion adds