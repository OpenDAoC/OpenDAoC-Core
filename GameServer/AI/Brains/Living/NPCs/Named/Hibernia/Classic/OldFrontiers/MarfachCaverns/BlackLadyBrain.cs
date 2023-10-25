using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

#region Black Lady
class BlackLadyBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BlackLadyBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        ThinkInterval = 2000;
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if(!CheckProximityAggro())
        {
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is OgressBrain)
                        {
                            npc.RemoveFromWorld();
                            Ogress.OgressCount = 0;
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if(Body.TargetObject != null && HasAggro)
        {
            RemoveAdds = false;
            Body.CastSpell(BlackLady_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

            if(Util.Chance(20))
            {
                SpawnOgress();
            }
        }
        base.Think();
    }
    public void SpawnOgress()
    {
        if (Ogress.OgressCount < 5)
        {
            switch (Util.Random(1, 2))
            {
                case 1:
                    {
                        Ogress Add1 = new Ogress();
                        Add1.X = 29654;
                        Add1.Y = 37219;
                        Add1.Z = 14953;
                        Add1.CurrentRegionID = 276;
                        Add1.RespawnInterval = -1;
                        Add1.Heading = 3618;
                        Add1.AddToWorld();
                    }
                    break;
                case 2:
                    {
                        Ogress Add2 = new Ogress();
                        Add2.X = 29646;
                        Add2.Y = 38028;
                        Add2.Z = 14967;
                        Add2.CurrentRegionID = 276;
                        Add2.RespawnInterval = -1;
                        Add2.Heading = 2114;
                        Add2.AddToWorld();
                    }
                    break;
            }
        }
    }
    public Spell m_BlackLady_DD;
    public Spell BlackLady_DD
    {
        get
        {
            if (m_BlackLady_DD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3.5;
                spell.RecastDelay = Util.Random(6,12);
                spell.ClientEffect = 4568;
                spell.Icon = 4568;
                spell.TooltipId = 4568;
                spell.Damage = 400;
                spell.Name = "Void Strike";
                spell.Radius = 350;
                spell.Range = 1800;
                spell.SpellID = 11787;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_BlackLady_DD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BlackLady_DD);
            }

            return m_BlackLady_DD;
        }
    }
}
#endregion Black Lady

#region Ogress
class OgressBrain : StandardMobBrain
{
    public OgressBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1200;
    }

    public override void Think()
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
        {
            if (player != null)
            {
                if (player.IsAlive && player.IsVisibleTo(Body) && player.Client.Account.PrivLevel == 1 &&
                    (player.PlayerClass.ID == 6 || player.PlayerClass.ID == 10 || player.PlayerClass.ID == 48
                     || player.PlayerClass.ID == 46 || player.PlayerClass.ID == 47 || player.PlayerClass.ID == 42 ||
                     player.PlayerClass.ID == 28 || player.PlayerClass.ID == 26))
                {
                    if (!AggroTable.ContainsKey(player))
                    {
                        AggroTable.Add(player, 150);
                        Body.StartAttack(player);
                    }
                }
                else
                {
                    if (!AggroTable.ContainsKey(player))
                    {
                        AggroTable.Add(player, 10);
                        Body.StartAttack(player);
                    }
                }
            }
        }

        base.Think();
    }
}
#endregion Ogress