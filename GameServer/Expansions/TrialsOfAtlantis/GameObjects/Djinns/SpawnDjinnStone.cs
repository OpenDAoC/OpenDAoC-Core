namespace Core.GS.Expansions.TrialsOfAtlantis;

/// <summary>
/// Djinn stone (spawns ancient bound djinn).
/// </summary>
public class SpawnDjinnStone : DjinnStone
{
    /// <summary>
    /// Spawns the djinn as soon as the stone is added to
    /// the world.
    /// </summary>
    /// <returns></returns>
    public override bool AddToWorld()
    {
        if (Djinn == null)
            Djinn = new PermanentDjinn(this);

        return base.AddToWorld();
    }
}