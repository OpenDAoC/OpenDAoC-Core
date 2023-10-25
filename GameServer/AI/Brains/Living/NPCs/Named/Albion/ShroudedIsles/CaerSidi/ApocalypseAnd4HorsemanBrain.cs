using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

#region Apocalypse Initializer
public class ApocalypseInitializerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ApocalypseInitializerBrain()
        : base()
    {
        ThinkInterval = 2000;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Apocalypse Initializer

#region 1st Horseman - Fames
public class FamesHorsemanBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FamesHorsemanBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 2000;
    }
    public static bool BafMobs = false;
    public static bool spawn_fate = false;
    public static bool StartedFames = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            BafMobs = false;
            StartedFames = false;
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }
        if (Body.InCombat || HasAggro || Body.attackComponent.AttackState == true)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
        {
            StartedFames = true;
            if (spawn_fate == false)
            {
                SpawnFateBearer();
                spawn_fate = true;
            }
            if (BafMobs == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "FamesBaf" && npc is GameEpicNpc)
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain);// add to aggro mobs with FamesBaf PackageID
                            BafMobs = true;
                        }
                    }
                }
            }
        }
        base.Think();
    }
    public void SpawnFateBearer()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
        GameEpicNpc Add = new GameEpicNpc();
        Add.LoadTemplate(npcTemplate);
        Add.X = Body.X - 100;
        Add.Y = Body.Y;
        Add.Z = Body.Z;
        Add.CurrentRegionID = Body.CurrentRegionID;
        Add.Heading = Body.Heading;
        Add.RespawnInterval = -1;
        Add.PackageID = "FamesBaf";
        Add.Faction = FactionMgr.GetFactionByID(64);
        Add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
        Add.AddToWorld();
    }
}
#endregion 1st Horseman - Fames

#region 2nd Horseman - Bellum
public class BellumHorsemanBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BellumHorsemanBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1200;
        ThinkInterval = 2000;
    }
    public static bool StartedBellum= false;
    public static bool SpawnWeapons = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            StartedBellum = false;
            SpawnWeapons = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is WarIncarnateCrushBrain || npc.Brain is WarIncarnateSlashBrain || npc.Brain is WarIncarnateThrustBrain)
                            {
                                npc.Die(Body);
                            }
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
        {
            StartedBellum = true;
            RemoveAdds = false;
            if(SpawnWeapons==false)
            {
                SpawnCrushWeapon();
                SpawnSlashWeapon();
                SpawnThrustWeapon();
                SpawnWeapons = true;
            }
        }
        base.Think();
    }

    public void SpawnCrushWeapon()
    {
        WarIncarnateCrush Add = new WarIncarnateCrush();
        Add.X = Body.X;
        Add.Y = Body.Y + 200;
        Add.Z = Body.Z;
        Add.CurrentRegionID = Body.CurrentRegionID;
        Add.MeleeDamageType = EDamageType.Crush;
        Add.Heading = Body.Heading;
        Add.RespawnInterval = -1;
        Add.PackageID = "BellumBaf";
        WarIncarnateCrushBrain adds = new WarIncarnateCrushBrain();
        Add.SetOwnBrain(adds);
        Add.AddToWorld();
    }
    public void SpawnSlashWeapon()
    {
        WarIncarnateSlash Add = new WarIncarnateSlash();
        Add.X = Body.X - 200;
        Add.Y = Body.Y;
        Add.Z = Body.Z;
        Add.CurrentRegionID = Body.CurrentRegionID;
        Add.MeleeDamageType = EDamageType.Slash;
        Add.Heading = Body.Heading;
        Add.RespawnInterval = -1;
        Add.PackageID = "BellumBaf";
        WarIncarnateSlashBrain adds = new WarIncarnateSlashBrain();
        Add.SetOwnBrain(adds);
        Add.AddToWorld();
    }
    public void SpawnThrustWeapon()
    {
        WarIncarnateThrust Add = new WarIncarnateThrust();
        Add.X = Body.X + 200;
        Add.Y = Body.Y;
        Add.Z = Body.Z;
        Add.CurrentRegionID = Body.CurrentRegionID;
        Add.MeleeDamageType = EDamageType.Thrust;
        Add.Heading = Body.Heading;
        Add.RespawnInterval = -1;
        Add.PackageID = "BellumBaf";
        WarIncarnateThrustBrain adds = new WarIncarnateThrustBrain();
        Add.SetOwnBrain(adds);
        Add.AddToWorld();
    }
}
#endregion 2nd Horseman - Bellum

#region Bellum adds (Crush DMG)
public class WarIncarnateCrushBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public WarIncarnateCrushBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
    }
    public void SpawnCrushWeapons()
    {
        for (int i = 0; i < 4; i++)
        {
            GameLiving ptarget = CalculateNextAttackTarget();
            WarIncarnateCrush Add = new WarIncarnateCrush();
            int random = Util.Random(1, 3);
            switch (random)
            {
                case 1:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 17, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Add.VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 70, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Add.VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 12, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.Standard);
                        Add.VisibleActiveWeaponSlots = 10;
                    }
                    break;
            }
            Add.X = Body.X + Util.Random(-200, 200);
            Add.Y = Body.Y + Util.Random(-200, 200);
            Add.Z = Body.Z;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.MeleeDamageType = EDamageType.Crush;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            WarIncarnateCrushBrain smb = new WarIncarnateCrushBrain();
            smb.AggroLevel = 100;
            smb.AggroRange = 1000;
            Add.AddBrain(smb);
            Add.AddToWorld();
            WarIncarnateCrushBrain brain = (WarIncarnateCrushBrain)Add.Brain;
            brain.AddToAggroList(ptarget, 1);
            Add.StartAttack(ptarget);
        }
    }
    public static bool spawn_copies = false;
    public override void Think()
    {
        if (Body.IsAlive)
        {
            if(spawn_copies==false)
            {
                SpawnCrushWeapons();
                spawn_copies = true;
            }
        }
        base.Think();
    }
}
#endregion Bellum adds (Crush DMG)

#region Bellum adds (Slash DMG)
public class WarIncarnateSlashBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public WarIncarnateSlashBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
    }
    public void SpawnSlashWeapons()
    {
        for (int i = 0; i < 4; i++)
        {
            GameLiving ptarget = CalculateNextAttackTarget();
            WarIncarnateSlash Add = new WarIncarnateSlash();
            int random = Util.Random(1, 4);
            switch (random)
            {
                case 1:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 6, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Add.VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 73, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Add.VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 67, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Add.VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 4:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.Standard);
                        Add.VisibleActiveWeaponSlots = 10;
                    }
                    break;
            }
            Add.X = Body.X + Util.Random(-200, 200);
            Add.Y = Body.Y + Util.Random(-200, 200);
            Add.Z = Body.Z;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.MeleeDamageType = EDamageType.Slash;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            WarIncarnateSlashBrain smb = new WarIncarnateSlashBrain();
            smb.AggroLevel = 100;
            smb.AggroRange = 1000;
            Add.AddBrain(smb);
            Add.AddToWorld();
            WarIncarnateSlashBrain brain = (WarIncarnateSlashBrain)Add.Brain;
            brain.AddToAggroList(ptarget, 1);
            Add.StartAttack(ptarget);
        }
    }
    public static bool spawn_copies2 = false;
    public override void Think()
    {
        if (Body.IsAlive)
        {
            if (spawn_copies2 == false)
            {
                SpawnSlashWeapons();
                spawn_copies2 = true;
            }
        }
        base.Think();
    }
}
#endregion Bellum adds (Slash DMG)

#region Bellum adds (Thrust DMG)
public class WarIncarnateThrustBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public WarIncarnateThrustBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
    }
    public void SpawnThrustWeapons()
    {
        for (int i = 0; i < 4; i++)
        {
            GameLiving ptarget = CalculateNextAttackTarget();
            WarIncarnateThrust Add = new WarIncarnateThrust();
            int random = Util.Random(1, 3);
            switch (random)
            {
                case 1:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 69, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Add.VisibleActiveWeaponSlots = 34;

                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 846, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Add.VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 886, 0, 0);
                        Add.Inventory = template.CloseTemplate();
                        Add.SwitchWeapon(EActiveWeaponSlot.Standard);
                        Add.VisibleActiveWeaponSlots = 255;
                    }
                    break;
            }
            Add.X = Body.X + Util.Random(-200, 200);
            Add.Y = Body.Y + Util.Random(-200, 200);
            Add.Z = Body.Z;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.MeleeDamageType = EDamageType.Thrust;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            WarIncarnateThrustBrain smb = new WarIncarnateThrustBrain();
            smb.AggroLevel = 100;
            smb.AggroRange = 1000;
            Add.AddBrain(smb);
            Add.AddToWorld();
            WarIncarnateThrustBrain brain = (WarIncarnateThrustBrain)Add.Brain;
            brain.AddToAggroList(ptarget, 1);
            Add.StartAttack(ptarget);
        }
    }
    public static bool spawn_copies3 = false;
    public override void Think()
    {
        if (Body.IsAlive)
        {
            if (spawn_copies3 == false)
            {
                SpawnThrustWeapons();
                spawn_copies3 = true;
            }
        }
        base.Think();
    }
}
#endregion Bellum adds (Thrust DMG)

#region 3rd Horseman - Morbus
public class MorbusHorsemanBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public MorbusHorsemanBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1200;
        ThinkInterval = 2000;
    }
    public static bool BafMobs3 = false;
    public static bool spawn_swarm = false;
    public static bool message_warning1 = false;
    public static bool IsBug = false;
    public static bool StartedMorbus = false;
    private bool RemoveAdds = false;
    public override void AttackMostWanted()
    {
        if (IsBug == true)
            return;
        else
        {
            base.AttackMostWanted();
        }
    }
    public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
    {
        if (IsBug == true)
            return;
        else
        {
            base.OnAttackedByEnemy(ad);
        }
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            StartedMorbus = false;
            BafMobs3 = false;
            message_warning1 = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.PackageID == "MorbusBaf" && npc.Brain is MorbusSwarmBrain)
                            {
                                npc.RemoveFromWorld();
                                MorbusHorseman.Morbus_Swarm_count = 0;
                            }
                        }
                    }
                }
                RemoveAdds = true;
            }
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
            BafMobs3 = false;
        }
        else if (Body.InCombatInLast(40 * 1000) == false && this.Body.InCombatInLast(45 * 1000))
        {
            Body.Health = Body.MaxHealth;
            Body.Model = 952;
            Body.Size = 140;
            IsBug = false;
            ClearAggroList();
        }
        if (HasAggro && Body.TargetObject != null)
            RemoveAdds = false;

        if (Body.InCombat || HasAggro || Body.attackComponent.AttackState == true)
        {
            StartedMorbus = true;
            if(MorbusHorseman.Morbus_Swarm_count > 0)
            {
                Body.Model = 771;
                Body.Size = 50;
                IsBug = true;                   
                if (message_warning1 == false)
                {
                    BroadcastMessage(String.Format("Morbus looks very pale as he slowly reads over the note."));
                    message_warning1 = true;
                }
                Body.StopAttack();
            }
            else
            {
                Body.Model = 952;
                Body.Size = 140;
                IsBug = false;
                message_warning1 = false;
            }
            if (BafMobs3 == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "MorbusBaf" && npc.Brain is MorbusSwarmBrain)
                        {
                            AddAggroListTo(npc.Brain as MorbusSwarmBrain);// add to aggro mobs with MorbusBaf PackageID
                            BafMobs3 = true;
                        }
                    }
                }
            }
            if(MorbusHorseman.Morbus_Swarm_count == 0)
            {
                if (spawn_swarm == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnSwarm), Util.Random(25000, 40000));//25s-40s

                    foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "MorbusBaf" && npc.Brain is MorbusSwarmBrain)
                            {
                                AddAggroListTo(npc.Brain as MorbusSwarmBrain);// add to aggro mobs with MorbusBaf PackageID
                            }
                        }
                    }
                    spawn_swarm = true;
                }
            }
        }
        base.Think();
    }
    public int SpawnSwarm(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            for (int i = 0; i < Util.Random(6,10); i++)
            {
                MorbusSwarm Add = new MorbusSwarm();
                Add.X = Body.X + Util.Random(-100, 100);
                Add.Y = Body.Y + Util.Random(-100, 100);
                Add.Z = Body.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.Heading = Body.Heading;
                Add.RespawnInterval = -1;
                Add.PackageID = "MorbusBaf";
                Add.AddToWorld();
                ++MorbusHorseman.Morbus_Swarm_count;
            }
        }
        spawn_swarm=false;
        return 0;
    }
}
#endregion 3rd Horseman - Morbus

#region Morbus Swarm
public class MorbusSwarmBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public MorbusSwarmBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
    }
    public override void Think()
    {
        if(Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (target != null && target.IsAlive)
            {
                if (Util.Chance(15) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
                    Body.CastSpell(BlackPlague, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        base.Think();
    }
    public Spell m_black_plague;

    public Spell BlackPlague
    {
        get
        {
            if (m_black_plague == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 10;
                spell.ClientEffect = 4375;
                spell.Icon = 4375;
                spell.Name = "Touch of Death";
                spell.Message1 = "You are diseased!";
                spell.Message2 = "{0} is diseased!";
                spell.Message3 = "You look healthy.";
                spell.Message4 = "{0} looks healthy again.";
                spell.TooltipId = 4375;
                spell.Range = 500;
                spell.Radius = 350;
                spell.Duration = 120;
                spell.SpellID = 11737;
                spell.Target = "Enemy";
                spell.Type = "Disease";
                spell.Uninterruptible = true;
                spell.DamageType = (int)EDamageType.Body; //Energy DMG Type
                m_black_plague = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_black_plague);
            }
            return m_black_plague;
        }
    }
}
#endregion Morbus Swarm

#region 4th Horseman - Funus
public class FunusHorsemanBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public FunusHorsemanBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1200;
        ThinkInterval = 2000;
    }
    public static bool BafMobs4 = false;
    public static bool StartedFunus = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            StartedFunus = false;
            BafMobs4 = false;
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }
        if (Body.TargetObject != null && HasAggro)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
        {
            StartedFunus = true;
            if (BafMobs4 == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "FunusBaf" && npc is GameEpicNpc)
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain);// add to aggro mobs with FamesBaf PackageID
                            BafMobs4 = true;
                        }
                    }
                }
            }
        }
        base.Think();
    }
}
#endregion 4th Horseman - Funus

#region Apocalypse
public class ApocalypseBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ApocalypseBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1300;
        ThinkInterval = 2000;
    }
    public static bool spawn_rain_of_fire = false;
    public static bool apoc_fly_phase = false;
    public static bool IsInFlyPhase = false;
    public static bool fly_phase1 = false;
    public static bool fly_phase2 = false;
    public static bool ApocAggro = false;
    public static bool pop_harbringers = false;
    public static bool StartedApoc = false;
    private bool RemoveAdds = false;

    public override void Think()
    {
        #region Reset boss
        if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
        {
            Body.Health = Body.MaxHealth;
            spawn_rain_of_fire = false;
            spawn_harbringers = false;

            apoc_fly_phase = false;
            IsInFlyPhase = false;
            fly_phase1 = false;
            fly_phase2 = false;
            ApocAggro = false;
            pop_harbringers = false;
            StartedApoc = false;
            Apocalypse.KilledEnemys = 0;
            HarbringerOfFate.HarbringersCount = 0;

            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is HarbringerOfFateBrain || npc.Brain is RainOfFireBrain)
                                npc.RemoveFromWorld();
                        }
                    }
                }
                RemoveAdds = true;
            }
            //ClearAggroList();
        }
        #endregion
        #region Boss combat
        if (Body.IsAlive)//bring mobs from rooms if mobs got set PackageID="ApocBaf"
        {
            StartedApoc = true;
            if (HasAggro && Body.TargetObject != null)
                RemoveAdds = false;

            if (ApocAggro == false && Body.HealthPercent <=99)//1st time apoc fly to celling
            {
                Point3D point1 = new Point3D();
                point1.X = Body.SpawnPoint.X; point1.Y = Body.SpawnPoint.Y + 100; point1.Z = Body.SpawnPoint.Z + 750;
                ClearAggroList();
                if (!Body.IsWithinRadius(point1, 100))
                {
                    Body.WalkTo(point1, 200);
                    IsInFlyPhase = true;
                }
                else
                {
                    if (fly_phase2 == false)
                    {
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(FlyPhaseStart), 500);
                        fly_phase2 = true;
                        foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                        {
                            if (player != null)
                                player.Out.SendMessage("Apocalypse says, 'Is it power? Fame? Fortune? Perhaps it is all three.'", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
                        }
                        ApocAggro = true;
                    }
                }
            }

            if (spawn_rain_of_fire==false)
            {
                SpawnRainOfFire();
                spawn_rain_of_fire = true;
            }
            if (HarbringerOfFate.HarbringersCount == 0)
            {
                if (spawn_harbringers == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnHarbringers), 500);
                    foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "ApocBaf" && npc.Brain is HarbringerOfFateBrain)
                                AddAggroListTo(npc.Brain as HarbringerOfFateBrain);// add to aggro mobs with ApocBaf PackageID
                        }
                    }
                    spawn_harbringers = true;
                }
            }
            if(Body.HealthPercent <= 50 && fly_phase1==false)//2nd time apoc fly to celling
            {
                Point3D point1 = new Point3D();
                point1.X = Body.SpawnPoint.X; point1.Y = Body.SpawnPoint.Y+100; point1.Z = Body.SpawnPoint.Z + 750;
                ClearAggroList();
                if (!Body.IsWithinRadius(point1, 100))
                {
                    Body.WalkTo(point1, 200);
                    IsInFlyPhase = true;                       
                }
                else
                {
                    if(fly_phase1 == false)
                    {
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(FlyPhaseStart), 500);
                        foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                        {
                            if (player != null)
                                player.Out.SendMessage("Apocalypse says, 'I wonder, also, about the motivation that drives one to such an audacious move.'", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
                        }
                       fly_phase1 = true;
                    }
                }
            }
            if (apoc_fly_phase == true)//here cast rain of fire from celling for 30s
            {
                foreach(GamePlayer player in Body.GetPlayersInRadius(1800))
                {
                    if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !AggroTable.ContainsKey(player))
                        AggroTable.Add(player, 200);
                }
                Body.SetGroundTarget(Body.X, Body.Y, Body.Z - 750);
                if (!Body.IsCasting)
                {
                    //Body.TurnTo(Body.GroundTarget.X, Body.GroundTarget.Y);
                    Body.CastSpell(Apoc_Gtaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                }
            }
        }
        #endregion
        base.Think();
    }
    #region Boss fly phase timers
    public int FlyPhaseStart(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            apoc_fly_phase = true;
            Body.MaxSpeedBase = 0;//make sure it will not move until phase ends
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(FlyPhaseDuration), 30000);//30s duration of phase
        }
        return 0;
    }
    public int FlyPhaseDuration(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            AttackMostWanted();
            IsInFlyPhase = false;
            apoc_fly_phase = false;
            Body.MaxSpeedBase = 300;
        }
        return 0;
    }
    #endregion

    #region Spawn Harbringers
    public static bool spawn_harbringers = false;
    public int SpawnHarbringers(EcsGameTimer timer)
    {
        if (Apocalypse.KilledEnemys == 4)//he doint it only once, spawning 2 harbringers is killed 4 players
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    player.Out.SendMessage("Apocalypse says, 'One has to wonder what kind of power lay behind that feat, for my harbingers of fate were no small adversaries.'", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
                }
            }
            for (int i = 0; i < 2; i++)
            {
                HarbringerOfFate Add = new HarbringerOfFate();
                Add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
                Add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
                Add.Z = Body.SpawnPoint.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.Heading = Body.Heading;
                Add.RespawnInterval = -1;
                Add.PackageID = "ApocBaf";
                Add.AddToWorld();
                ++HarbringerOfFate.HarbringersCount;
            }
        }
        if(Body.HealthPercent <= 50 && pop_harbringers==false)/// spawning another 2 harbringers if boss is at 50%
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    player.Out.SendMessage("Apocalypse says, 'In all of this, however, it would seem that you have overlooked " +
                        "the small matter of price for your actions. I am not the vengeful sort, so do not take this the wrong way," +
                        " but good harbingers are hard to come by. And, thanks to you, they will need to be replaced.'", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
                }
            }
            for (int i = 0; i < 2; i++)
            {
                HarbringerOfFate Add = new HarbringerOfFate();
                Add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
                Add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
                Add.Z = Body.SpawnPoint.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.Heading = Body.Heading;
                Add.RespawnInterval = -1;
                Add.PackageID = "ApocBaf";
                Add.AddToWorld();
                ++HarbringerOfFate.HarbringersCount;
            }
            pop_harbringers = true;
        }
        spawn_harbringers = false;
        return 0;
    }
    #endregion
    #region Spawn Rain of Fire mob
    public void SpawnRainOfFire()
    {
        RainOfFire Add = new RainOfFire();
        Add.X = Body.X;
        Add.Y = Body.Y;
        Add.Z = Body.Z + 940;
        Add.CurrentRegionID = Body.CurrentRegionID;
        Add.Heading = Body.Heading;
        Add.RespawnInterval = -1;
        Add.PackageID = "RainOfFire";
        Add.AddToWorld();
    }
    #endregion
    #region Apoc gtaoe spell for fly phases
    private Spell m_Apoc_Gtaoe;
    private Spell Apoc_Gtaoe
    {
        get
        {
            if (m_Apoc_Gtaoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 0;
                spell.ClientEffect = 368;
                spell.Icon = 368;
                spell.Damage = 650;
                spell.Name = "Rain of Fire";
                spell.Radius = 800;
                spell.Range = 2800;
                spell.SpellID = 11740;
                spell.Target = "Area";
                spell.Type = "DirectDamageNoVariance";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Heat;
                m_Apoc_Gtaoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Apoc_Gtaoe);
            }
            return m_Apoc_Gtaoe;
        }
    }
    #endregion
}
#endregion Apocalypse

#region Harbringer of Fate
public class HarbringerOfFateBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public HarbringerOfFateBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1200;
    }
    public override void Think()
    {
        if (Body.InCombat || HasAggro)
        {
        }
        base.Think();
    }
}
#endregion Harbringer of Fate

#region Rain of Fire
public class RainOfFireBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public RainOfFireBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 0;
    }
    public override void Think()
    {
        if (Body.IsAlive)
        {
            PickTarget();
        }
        base.Think();
    }
    List<GamePlayer> ApocPlayerList = new List<GamePlayer>();
    public GamePlayer dd_Target = null;
    public GamePlayer DD_Target
    {
        get { return dd_Target; }
        set { dd_Target = value; }
    }
    public static bool cast_dd = false;
    public static bool reset_cast = false;
    public void PickTarget()
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
        {
            if (player != null)
            {
                if (player.IsAlive)
                {
                    if (!ApocPlayerList.Contains(player))
                    {
                        ApocPlayerList.Add(player);
                    }
                }
            }
        }
        if (ApocPlayerList.Count > 0)
        {
            if (cast_dd == false)
            {
                GamePlayer ptarget = ((GamePlayer)(ApocPlayerList[Util.Random(1, ApocPlayerList.Count) - 1]));
                DD_Target = ptarget;
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDD), 6000);
                cast_dd = true;
                reset_cast = false;
            }
        }
    }
    private int CastDD(EcsGameTimer timer)
    {
        GameObject oldTarget = Body.TargetObject;
        Body.TargetObject = DD_Target;
        Body.TurnTo(DD_Target);
        if (Body.TargetObject != null)
        {
            Body.CastSpell(Apoc_Rain_of_Fire, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            if (reset_cast == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetCast), 25000);//25s recast
                reset_cast = true;
            }
        }
        DD_Target = null;
        if (oldTarget != null) Body.TargetObject = oldTarget;
        return 0;
    }
    public int ResetCast(EcsGameTimer timer)
    {
        cast_dd = false;
        return 0;
    }

    private Spell m_Apoc_Rain_of_Fire;
    private Spell Apoc_Rain_of_Fire
    {
        get
        {
            if (m_Apoc_Rain_of_Fire == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 360;
                spell.Icon = 360;
                spell.Damage = 800;
                spell.Name = "Rain of Fire";
                spell.Radius = 600;
                spell.Range = 2800;
                spell.SpellID = 11738;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Heat;
                m_Apoc_Rain_of_Fire = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Apoc_Rain_of_Fire);
            }
            return m_Apoc_Rain_of_Fire;
        }
    }
}
#endregion Rain of Fire