using System.Collections.Generic;

namespace Core.GS.AI.Brains;

public class SkeletalSacristanBrain : StandardMobBrain
{
    public SkeletalSacristanBrain()
    {
        _roamingPathPoints.Add(new Point3D(31826, 32256, 16750));
        _roamingPathPoints.Add(new Point3D(32846, 32250, 16750));
        _roamingPathPoints.Add(new Point3D(35357, 32243, 16494));
        _roamingPathPoints.Add(new Point3D(35408, 35788, 16494));
        _roamingPathPoints.Add(new Point3D(33112, 35808, 16750));
        _roamingPathPoints.Add(new Point3D(30259, 35800, 16750));
        _roamingPathPoints.Add(new Point3D(30238, 32269, 16750));
    }

    private List<Point3D> _roamingPathPoints = new List<Point3D>();
    private int _lastRoamIndex = 0;

    public override void Think()
    {
        if (Body.IsAlive)
        {
            if (Body.IsWithinRadius(_roamingPathPoints[_lastRoamIndex], 100))
            {
                _lastRoamIndex++;
            }

            if(_lastRoamIndex >= _roamingPathPoints.Count)
            {
                _lastRoamIndex = 0;
                Body.ReturnToSpawnPoint(NpcMovementComponent.DEFAULT_WALK_SPEED);
            }
            else if(!Body.IsMoving)
                Body.WalkTo(_roamingPathPoints[_lastRoamIndex], (short)Util.Random(195, 250));
        }

        if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
        {
            ClearAggroList();
            Body.Health = Body.MaxHealth;
        }
        base.Think();
    }
}