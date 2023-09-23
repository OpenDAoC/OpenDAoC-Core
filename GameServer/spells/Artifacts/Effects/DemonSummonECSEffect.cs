using System;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

public class DemonSummonECSEffect : ECSGameSpellEffect
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private BracerOfZo zoHandler;
    
    public DemonSummonECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
    {
        zoHandler = SpellHandler as BracerOfZo;
        EffectType = eEffect.Pet;
    }

    public override void OnStartEffect()
    {
        if (zoHandler == null) return;
        INpcTemplate template = NpcTemplateMgr.GetTemplate(SpellHandler.Spell.LifeDrainReturn);
        if (template == null)
        {
            String errorMessage = String.Format("NPC template {0} is missing, spell ID = {1}", SpellHandler.Spell.LifeDrainReturn, SpellHandler.Spell.ID);
            if (log.IsWarnEnabled) log.Warn(errorMessage);
            if (Caster is GamePlayer p && p.Client.Account.PrivLevel > 1) p.MessageToSelf(errorMessage, eChatType.CT_Skill);
        }

        Point2D spawnPoint = Caster.GetPointFromHeading( Caster.Heading, 64 );
        int i = 0;
        for(i=0;i<3;i++)
        {               
            zoHandler.Demons[i] = new ZoarkatPet(template);
            zoHandler.Demons[i].SetOwnBrain(new ProcPetBrain(Caster));
            zoHandler.Demons[i].X = spawnPoint.X + Util.Random(20,40) - Util.Random(20,40);
            zoHandler.Demons[i].Y = spawnPoint.Y + Util.Random(20,40) - Util.Random(20,40);
            zoHandler.Demons[i].Z = Caster.Z;
            zoHandler.Demons[i].CurrentRegion = Caster.CurrentRegion;
            zoHandler.Demons[i].Heading = (ushort)((Caster.Heading + 2048) % 4096);
            zoHandler.Demons[i].Realm = Caster.Realm;
            zoHandler.Demons[i].CurrentSpeed = 0;
            zoHandler.Demons[i].Level = 36;
            zoHandler.Demons[i].Flags |= GameNPC.eFlags.FLYING;
            zoHandler.Demons[i].AddToWorld();
            (zoHandler.Demons[i].Brain as IOldAggressiveBrain)?.AddToAggroList(Caster.TargetObject as GameLiving, 1);
            (zoHandler.Demons[i].Brain as ProcPetBrain)?.Think();
        }
    }

    public override void OnStopEffect()
    {
        foreach (var demon in zoHandler.Demons)
        {
            demon.Health = 0;
            demon.Delete();
        }
    }
}