using Core.GS.AI;
using Core.GS.Effects;

namespace Core.GS.Scripts.Custom;

public class MainTeleporterBrain : StandardMobBrain
{
    public override void Think()
    {
        OldFrontierTeleporter teleporter = Body as OldFrontierTeleporter;

        GameSpellEffect effect = null;

        foreach (GameSpellEffect activeEffect in teleporter.EffectList)
        {
            if (activeEffect.Name == "TELEPORTER_EFFECT")
            {
                effect = activeEffect;
            }
        }

        if (effect != null || teleporter.IsCasting)
            return;

        teleporter.StartTeleporting();
    }
}