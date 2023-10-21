using System.Collections.Generic;
using Core.GS;

namespace Core.AI.Brain;

public class NjessiBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public NjessiBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
        
        _roamingPathPoints.Add(new Point3D(783055, 882613, 4613));
        _roamingPathPoints.Add(new Point3D(781504, 886149, 4613));
        _roamingPathPoints.Add(new Point3D(788057, 899051, 4613));
        _roamingPathPoints.Add(new Point3D(797231, 909562, 4613));
        _roamingPathPoints.Add(new Point3D(791084, 894015, 4613));
        _roamingPathPoints.Add(new Point3D(788652, 887943, 4613));
	}
	
	private List<Point3D> _roamingPathPoints = new List<Point3D>();
    private int _lastRoamIndex = 0;
    
    public override void Think()
	{
		
        Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
        #region WalkPoints
        if (!Body.InCombat && !HasAggro)
        {

            if (Body.IsWithinRadius(_roamingPathPoints[_lastRoamIndex], 100))
            {
	            _lastRoamIndex++;
            }

            if (_lastRoamIndex >= _roamingPathPoints.Count)
            {
	            _lastRoamIndex = 0;
            }
            else if(!Body.IsMoving) Body.WalkTo(_roamingPathPoints[_lastRoamIndex], 120);
            
        }
        #endregion
        if (Body.IsAlive)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (player != null && player.IsAlive && !AggroTable.ContainsKey(player) && player.Client.Account.PrivLevel == 1)
                    AggroTable.Add(player, 10);
            }
            foreach (GameNpc npc in Body.GetNPCsInRadius((ushort)AggroRange))
            {
                if (npc != null && npc.IsAlive && npc.Realm != Body.Realm && !AggroTable.ContainsKey(npc))
                    AggroTable.Add(npc, 10);
            }
        }
        base.Think();
	}
}