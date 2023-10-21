namespace DOL.AI.Brain;

#region Host Initializer
public class HostInitializerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HostInitializerBrain()
        : base()
    {
        ThinkInterval = 2000;
    }

    public override void Think()
    {
        base.Think();
    }
}
#endregion Host Initializer

#region Host
public class HostBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HostBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 2500;
    }
    public static bool BafHost = false;
    public static bool BafMobs = false;

    #region path points checks

    public static bool path1 = false;
    public static bool path11 = false;
    public static bool path21 = false;
    public static bool path31 = false;
    public static bool path41 = false;
    public static bool path2 = false;
    public static bool path12 = false;
    public static bool path22 = false;
    public static bool path39 = false;
    public static bool path42 = false;
    public static bool path3 = false;
    public static bool path13 = false;
    public static bool path23 = false;
    public static bool path32 = false;
    public static bool path43 = false;
    public static bool path4 = false;
    public static bool path14 = false;
    public static bool path24 = false;
    public static bool path33 = false;
    public static bool path44 = false;
    public static bool path5 = false;
    public static bool path15 = false;
    public static bool path25 = false;
    public static bool path34 = false;
    public static bool path45 = false;
    public static bool path6 = false;
    public static bool path16 = false;
    public static bool path26 = false;
    public static bool path35 = false;
    public static bool path46 = false;
    public static bool path7 = false;
    public static bool path17 = false;
    public static bool path27 = false;
    public static bool path36 = false;
    public static bool path47 = false;
    public static bool path8 = false;
    public static bool path18 = false;
    public static bool path28 = false;
    public static bool path37 = false;
    public static bool path48 = false;
    public static bool path9 = false;
    public static bool path19 = false;
    public static bool path29 = false;
    public static bool path38 = false;
    public static bool path49 = false;
    public static bool path10 = false;
    public static bool path20 = false;
    public static bool path30 = false;
    public static bool path40 = false;
    public static bool path50 = false;
    public static bool path51 = false;
    public static bool walkback = false;

    #endregion
    public void HostPath()
    {
        #region path glocs

        Point3D point1 = new Point3D();
        point1.X = 26749;
        point1.Y = 29730;
        point1.Z = 17871; //3th floor
        Point3D point2 = new Point3D();
        point2.X = 26180;
        point2.Y = 30241;
        point2.Z = 17871;
        Point3D point3 = new Point3D();
        point3.X = 25743;
        point3.Y = 30447;
        point3.Z = 17861;
        Point3D point4 = new Point3D();
        point4.X = 25154;
        point4.Y = 30151;
        point4.Z = 17861;
        Point3D point5 = new Point3D();
        point5.X = 24901;
        point5.Y = 29673;
        point5.Z = 17861;
        Point3D point6 = new Point3D();
        point6.X = 25376;
        point6.Y = 29310;
        point6.Z = 17861; // stairs start
        Point3D point7 = new Point3D();
        point7.X = 25360;
        point7.Y = 29635;
        point7.Z = 17866;
        Point3D point8 = new Point3D();
        point8.X = 25608;
        point8.Y = 29967;
        point8.Z = 17702;
        Point3D point9 = new Point3D();
        point9.X = 25984;
        point9.Y = 29902;
        point9.Z = 17534;
        Point3D point10 = new Point3D();
        point10.X = 26121;
        point10.Y = 29617;
        point10.Z = 17405;
        Point3D point11 = new Point3D();
        point11.X = 25889;
        point11.Y = 29309;
        point11.Z = 17251;
        Point3D point12 = new Point3D();
        point12.X = 25453;
        point12.Y = 29390;
        point12.Z = 17051;
        Point3D point13 = new Point3D();
        point13.X = 25372;
        point13.Y = 29775;
        point13.Z = 16897;
        Point3D point14 = new Point3D();
        point14.X = 25946;
        point14.Y = 29958;
        point14.Z = 16638;
        Point3D point15 = new Point3D();
        point15.X = 26116;
        point15.Y = 29523;
        point15.Z = 16495;
        Point3D point16 = new Point3D();
        point16.X = 26106;
        point16.Y = 29305;
        point16.Z = 16495; //start 2nd floor
        Point3D point17 = new Point3D();
        point17.X = 25061;
        point17.Y = 29335;
        point17.Z = 16495;
        Point3D point18 = new Point3D();
        point18.X = 25046;
        point18.Y = 30229;
        point18.Z = 16495;
        Point3D point19 = new Point3D();
        point19.X = 25686;
        point19.Y = 30428;
        point19.Z = 16495;
        Point3D point20 = new Point3D();
        point20.X = 26832;
        point20.Y = 29793;
        point20.Z = 16495;
        Point3D point21 = new Point3D();
        point21.X = 25718;
        point21.Y = 29012;
        point21.Z = 16495;
        Point3D point22 = new Point3D();
        point22.X = 25358;
        point22.Y = 29563;
        point22.Z = 16495; //ebd of 2nd floor/starting stairs
        Point3D point23 = new Point3D();
        point23.X = 25426;
        point23.Y = 29842;
        point23.Z = 16406;
        Point3D point24 = new Point3D();
        point24.X = 25842;
        point24.Y = 29983;
        point24.Z = 16223;
        Point3D point25 = new Point3D();
        point25.X = 26129;
        point25.Y = 29643;
        point25.Z = 16039;
        Point3D point26 = new Point3D();
        point26.X = 25714;
        point26.Y = 29267;
        point26.Z = 15796;
        Point3D point27 = new Point3D();
        point27.X = 25345;
        point27.Y = 29587;
        point27.Z = 15588;
        Point3D point28 = new Point3D();
        point28.X = 25711;
        point28.Y = 29995;
        point28.Z = 15357;
        Point3D point29 = new Point3D();
        point29.X = 26123;
        point29.Y = 29645;
        point29.Z = 15122; //start 1st floor/ end of stairs
        Point3D point30 = new Point3D();
        point30.X = 25796;
        point30.Y = 28979;
        point30.Z = 15120;
        Point3D point31 = new Point3D();
        point31.X = 24729;
        point31.Y = 29725;
        point31.Z = 15119;
        Point3D point32 = new Point3D();
        point32.X = 25695;
        point32.Y = 30592;
        point32.Z = 15119;
        Point3D point33 = new Point3D();
        point33.X = 26792;
        point33.Y = 29721;
        point33.Z = 15119;
        Point3D point34 = new Point3D();
        point34.X = 26102;
        point34.Y = 29302;
        point34.Z = 15120; //end of floor 1// going all way up now
        Point3D point35 = new Point3D();
        point35.X = 26085;
        point35.Y = 29802;
        point35.Z = 15192;
        Point3D point36 = new Point3D();
        point36.X = 25487;
        point36.Y = 29903;
        point36.Z = 15457;
        Point3D point37 = new Point3D();
        point37.X = 25370;
        point37.Y = 29483;
        point37.Z = 15625;
        Point3D point38 = new Point3D();
        point38.X = 25873;
        point38.Y = 29309;
        point38.Z = 15872;
        Point3D point39 = new Point3D();
        point39.X = 26103;
        point39.Y = 29695;
        point39.Z = 16058;
        Point3D point40 = new Point3D();
        point40.X = 25693;
        point40.Y = 29975;
        point40.Z = 16284;
        Point3D point41 = new Point3D();
        point41.X = 25352;
        point41.Y = 29538;
        point41.Z = 16495; //stairs entering 2nd floor
        Point3D point42 = new Point3D();
        point42.X = 25775;
        point42.Y = 29107;
        point42.Z = 16495;
        Point3D point43 = new Point3D();
        point43.X = 26114;
        point43.Y = 29597;
        point43.Z = 16495;
        Point3D point44 = new Point3D();
        point44.X = 25730;
        point44.Y = 29985;
        point44.Z = 16722;
        Point3D point45 = new Point3D();
        point45.X = 25368;
        point45.Y = 29610;
        point45.Z = 16957;
        Point3D point46 = new Point3D();
        point46.X = 25705;
        point46.Y = 29283;
        point46.Z = 17169;
        Point3D point47 = new Point3D();
        point47.X = 26109;
        point47.Y = 29587;
        point47.Z = 17393;
        Point3D point48 = new Point3D();
        point48.X = 25759;
        point48.Y = 30023;
        point48.Z = 17632;
        Point3D point49 = new Point3D();
        point49.X = 25359;
        point49.Y = 29578;
        point49.Z = 17871; //starting 3th floor
        Point3D point50 = new Point3D();
        point50.X = 25809;
        point50.Y = 29142;
        point50.Z = 17871;
        Point3D point51 = new Point3D();
        point51.X = 26344;
        point51.Y = 29391;
        point51.Z = 17871;
        Point3D spawn = new Point3D();
        spawn.X = 26995;
        spawn.Y = 29733;
        spawn.Z = 17871;

        #endregion path glocs

        if (!Body.InCombat && !HasAggro)
        {
            #region AllPathChecksHere

            if (!Body.IsWithinRadius(point1, 30) && path1 == false)
            {
                Body.WalkTo(point1, 100);
            }
            else
            {
                path1 = true;
                walkback = false;
                if (!Body.IsWithinRadius(point2, 30) && path1 == true && path2 == false)
                {
                    Body.WalkTo(point2, 100);
                }
                else
                {
                    path2 = true;
                    if (!Body.IsWithinRadius(point3, 30) && path1 == true && path2 == true && path3 == false)
                    {
                        Body.WalkTo(point3, 100);
                    }
                    else
                    {
                        path3 = true;
                        if (!Body.IsWithinRadius(point4, 30) && path1 == true && path2 == true && path3 == true &&
                            path4 == false)
                        {
                            Body.WalkTo(point4, 100);
                        }
                        else
                        {
                            path4 = true;
                            if (!Body.IsWithinRadius(point5, 30) && path1 == true && path2 == true &&
                                path3 == true && path4 == true && path5 == false)
                            {
                                Body.WalkTo(point5, 100);
                            }
                            else
                            {
                                path5 = true;
                                if (!Body.IsWithinRadius(point6, 30) && path1 == true && path2 == true &&
                                    path3 == true && path4 == true && path5 == true && path6 == false)
                                {
                                    Body.WalkTo(point6, 100);
                                }
                                else
                                {
                                    path6 = true;
                                    if (!Body.IsWithinRadius(point7, 30) && path1 == true && path2 == true &&
                                        path3 == true && path4 == true && path5 == true && path6 == true &&
                                        path7 == false)
                                    {
                                        Body.WalkTo(point7, 100);
                                    }
                                    else
                                    {
                                        path7 = true;
                                        if (!Body.IsWithinRadius(point8, 30) && path1 == true && path2 == true &&
                                            path3 == true && path4 == true && path5 == true && path6 == true &&
                                            path7 == true && path8 == false)
                                        {
                                            Body.WalkTo(point8, 100);
                                        }
                                        else
                                        {
                                            path8 = true;
                                            if (!Body.IsWithinRadius(point9, 30) && path1 == true &&
                                                path2 == true && path3 == true && path4 == true && path5 == true &&
                                                path6 == true && path7 == true && path8 == true && path9 == false)
                                            {
                                                Body.WalkTo(point9, 100);
                                            }
                                            else
                                            {
                                                path9 = true;
                                                if (!Body.IsWithinRadius(point10, 30) && path1 == true &&
                                                    path2 == true && path3 == true && path4 == true &&
                                                    path5 == true && path6 == true && path7 == true &&
                                                    path8 == true && path9 == true && path10 == false)
                                                {
                                                    Body.WalkTo(point10, 100);
                                                }
                                                else
                                                {
                                                    path10 = true;
                                                    if (!Body.IsWithinRadius(point11, 30) && path1 == true &&
                                                        path2 == true && path3 == true && path4 == true &&
                                                        path5 == true && path6 == true && path7 == true &&
                                                        path8 == true && path9 == true && path10 == true
                                                        && path11 == false)
                                                    {
                                                        Body.WalkTo(point11, 100);
                                                    }
                                                    else
                                                    {
                                                        path11 = true;
                                                        if (!Body.IsWithinRadius(point12, 30) && path1 == true &&
                                                            path2 == true && path3 == true && path4 == true &&
                                                            path5 == true && path6 == true && path7 == true &&
                                                            path8 == true && path9 == true && path10 == true
                                                            && path11 == true && path12 == false)
                                                        {
                                                            Body.WalkTo(point12, 100);
                                                        }
                                                        else
                                                        {
                                                            path12 = true;
                                                            if (!Body.IsWithinRadius(point13, 30) &&
                                                                path1 == true && path2 == true && path3 == true &&
                                                                path4 == true && path5 == true && path6 == true &&
                                                                path7 == true && path8 == true && path9 == true &&
                                                                path10 == true
                                                                && path11 == true && path12 == true &&
                                                                path13 == false)
                                                            {
                                                                Body.WalkTo(point13, 100);
                                                            }
                                                            else
                                                            {
                                                                path13 = true;
                                                                if (!Body.IsWithinRadius(point14, 30) &&
                                                                    path1 == true && path2 == true &&
                                                                    path3 == true && path4 == true &&
                                                                    path5 == true && path6 == true &&
                                                                    path7 == true && path8 == true &&
                                                                    path9 == true && path10 == true
                                                                    && path11 == true && path12 == true &&
                                                                    path13 == true && path14 == false)
                                                                {
                                                                    Body.WalkTo(point14, 100);
                                                                }
                                                                else
                                                                {
                                                                    path14 = true;
                                                                    if (!Body.IsWithinRadius(point15, 30) &&
                                                                        path1 == true && path2 == true &&
                                                                        path3 == true && path4 == true &&
                                                                        path5 == true && path6 == true &&
                                                                        path7 == true && path8 == true &&
                                                                        path9 == true && path10 == true
                                                                        && path11 == true && path12 == true &&
                                                                        path13 == true && path14 == true &&
                                                                        path15 == false)
                                                                    {
                                                                        Body.WalkTo(point15, 100);
                                                                    }
                                                                    else
                                                                    {
                                                                        path15 = true;
                                                                        if (!Body.IsWithinRadius(point16, 30) &&
                                                                            path1 == true && path2 == true &&
                                                                            path3 == true && path4 == true &&
                                                                            path5 == true && path6 == true &&
                                                                            path7 == true && path8 == true &&
                                                                            path9 == true && path10 == true
                                                                            && path11 == true && path12 == true &&
                                                                            path13 == true && path14 == true &&
                                                                            path15 == true && path16 == false)
                                                                        {
                                                                            Body.WalkTo(point16, 100);
                                                                        }
                                                                        else
                                                                        {
                                                                            path16 = true;
                                                                            if (!Body.IsWithinRadius(point17, 30) &&
                                                                             path1 == true && path2 == true &&
                                                                             path3 == true && path4 == true &&
                                                                             path5 == true && path6 == true &&
                                                                             path7 == true && path8 == true &&
                                                                             path9 == true && path10 == true
                                                                             && path11 == true &&
                                                                             path12 == true && path13 == true &&
                                                                             path14 == true && path15 == true &&
                                                                             path16 == true && path17 == false)
                                                                            {
                                                                                Body.WalkTo(point17, 100);
                                                                            }
                                                                            else
                                                                            {
                                                                                path17 = true;
                                                                                if (!Body.IsWithinRadius(point18,
                                                                                     30) && path1 == true &&
                                                                                 path2 == true &&
                                                                                 path3 == true &&
                                                                                 path4 == true &&
                                                                                 path5 == true &&
                                                                                 path6 == true &&
                                                                                 path7 == true &&
                                                                                 path8 == true &&
                                                                                 path9 == true && path10 == true
                                                                                 && path11 == true &&
                                                                                 path12 == true &&
                                                                                 path13 == true &&
                                                                                 path14 == true &&
                                                                                 path15 == true &&
                                                                                 path16 == true &&
                                                                                 path17 == true &&
                                                                                 path18 == false)
                                                                                {
                                                                                    Body.WalkTo(point18, 100);
                                                                                }
                                                                                else
                                                                                {
                                                                                    path18 = true;
                                                                                    if (!Body.IsWithinRadius(
                                                                                         point19, 30) &&
                                                                                     path1 == true &&
                                                                                     path2 == true &&
                                                                                     path3 == true &&
                                                                                     path4 == true &&
                                                                                     path5 == true &&
                                                                                     path6 == true &&
                                                                                     path7 == true &&
                                                                                     path8 == true &&
                                                                                     path9 == true &&
                                                                                     path10 == true
                                                                                     && path11 == true &&
                                                                                     path12 == true &&
                                                                                     path13 == true &&
                                                                                     path14 == true &&
                                                                                     path15 == true &&
                                                                                     path16 == true &&
                                                                                     path17 == true &&
                                                                                     path18 == true &&
                                                                                     path19 == false)
                                                                                    {
                                                                                        Body.WalkTo(point19, 100);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        path19 = true;
                                                                                        if (!Body.IsWithinRadius(
                                                                                             point20, 30) &&
                                                                                         path1 == true &&
                                                                                         path2 == true &&
                                                                                         path3 == true &&
                                                                                         path4 == true &&
                                                                                         path5 == true &&
                                                                                         path6 == true &&
                                                                                         path7 == true &&
                                                                                         path8 == true &&
                                                                                         path9 == true &&
                                                                                         path10 == true
                                                                                         && path11 == true &&
                                                                                         path12 == true &&
                                                                                         path13 == true &&
                                                                                         path14 == true &&
                                                                                         path15 == true &&
                                                                                         path16 == true &&
                                                                                         path17 == true &&
                                                                                         path18 == true &&
                                                                                         path19 == true &&
                                                                                         path20 == false)
                                                                                        {
                                                                                            Body.WalkTo(point20,
                                                                                                100);
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            path20 = true;
                                                                                            if (!Body
                                                                                                 .IsWithinRadius(
                                                                                                     point21,
                                                                                                     30) &&
                                                                                             path1 == true &&
                                                                                             path2 == true &&
                                                                                             path3 == true &&
                                                                                             path4 == true &&
                                                                                             path5 == true &&
                                                                                             path6 == true &&
                                                                                             path7 == true &&
                                                                                             path8 == true &&
                                                                                             path9 == true &&
                                                                                             path10 == true
                                                                                             && path11 ==
                                                                                             true &&
                                                                                             path12 == true &&
                                                                                             path13 == true &&
                                                                                             path14 == true &&
                                                                                             path15 == true &&
                                                                                             path16 == true &&
                                                                                             path17 == true &&
                                                                                             path18 == true &&
                                                                                             path19 == true &&
                                                                                             path20 == true
                                                                                             && path21 == false)
                                                                                            {
                                                                                                Body.WalkTo(point21,
                                                                                                    100);
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                path21 = true;
                                                                                                if (!Body
                                                                                                     .IsWithinRadius(
                                                                                                         point22,
                                                                                                         30) &&
                                                                                                 path1 ==
                                                                                                 true &&
                                                                                                 path2 ==
                                                                                                 true &&
                                                                                                 path3 ==
                                                                                                 true &&
                                                                                                 path4 ==
                                                                                                 true &&
                                                                                                 path5 ==
                                                                                                 true &&
                                                                                                 path6 ==
                                                                                                 true &&
                                                                                                 path7 ==
                                                                                                 true &&
                                                                                                 path8 ==
                                                                                                 true &&
                                                                                                 path9 ==
                                                                                                 true &&
                                                                                                 path10 == true
                                                                                                 && path11 ==
                                                                                                 true &&
                                                                                                 path12 ==
                                                                                                 true &&
                                                                                                 path13 ==
                                                                                                 true &&
                                                                                                 path14 ==
                                                                                                 true &&
                                                                                                 path15 ==
                                                                                                 true &&
                                                                                                 path16 ==
                                                                                                 true &&
                                                                                                 path17 ==
                                                                                                 true &&
                                                                                                 path18 ==
                                                                                                 true &&
                                                                                                 path19 ==
                                                                                                 true &&
                                                                                                 path20 == true
                                                                                                 && path21 ==
                                                                                                 true &&
                                                                                                 path22 ==
                                                                                                 false)
                                                                                                {
                                                                                                    Body.WalkTo(
                                                                                                        point22,
                                                                                                        100);
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    path22 = true;
                                                                                                    if (!Body
                                                                                                         .IsWithinRadius(
                                                                                                             point23,
                                                                                                             30) &&
                                                                                                     path1 ==
                                                                                                     true &&
                                                                                                     path2 ==
                                                                                                     true &&
                                                                                                     path3 ==
                                                                                                     true &&
                                                                                                     path4 ==
                                                                                                     true &&
                                                                                                     path5 ==
                                                                                                     true &&
                                                                                                     path6 ==
                                                                                                     true &&
                                                                                                     path7 ==
                                                                                                     true &&
                                                                                                     path8 ==
                                                                                                     true &&
                                                                                                     path9 ==
                                                                                                     true &&
                                                                                                     path10 ==
                                                                                                     true
                                                                                                     &&
                                                                                                     path11 ==
                                                                                                     true &&
                                                                                                     path12 ==
                                                                                                     true &&
                                                                                                     path13 ==
                                                                                                     true &&
                                                                                                     path14 ==
                                                                                                     true &&
                                                                                                     path15 ==
                                                                                                     true &&
                                                                                                     path16 ==
                                                                                                     true &&
                                                                                                     path17 ==
                                                                                                     true &&
                                                                                                     path18 ==
                                                                                                     true &&
                                                                                                     path19 ==
                                                                                                     true &&
                                                                                                     path20 ==
                                                                                                     true
                                                                                                     &&
                                                                                                     path21 ==
                                                                                                     true &&
                                                                                                     path22 ==
                                                                                                     true &&
                                                                                                     path23 ==
                                                                                                     false)
                                                                                                    {
                                                                                                        Body.WalkTo(
                                                                                                            point23,
                                                                                                            100);
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        path23 =
                                                                                                            true;
                                                                                                        if (!Body
                                                                                                             .IsWithinRadius(
                                                                                                                 point24,
                                                                                                                 30) &&
                                                                                                         path1 ==
                                                                                                         true &&
                                                                                                         path2 ==
                                                                                                         true &&
                                                                                                         path3 ==
                                                                                                         true &&
                                                                                                         path4 ==
                                                                                                         true &&
                                                                                                         path5 ==
                                                                                                         true &&
                                                                                                         path6 ==
                                                                                                         true &&
                                                                                                         path7 ==
                                                                                                         true &&
                                                                                                         path8 ==
                                                                                                         true &&
                                                                                                         path9 ==
                                                                                                         true &&
                                                                                                         path10 ==
                                                                                                         true
                                                                                                         &&
                                                                                                         path11 ==
                                                                                                         true &&
                                                                                                         path12 ==
                                                                                                         true &&
                                                                                                         path13 ==
                                                                                                         true &&
                                                                                                         path14 ==
                                                                                                         true &&
                                                                                                         path15 ==
                                                                                                         true &&
                                                                                                         path16 ==
                                                                                                         true &&
                                                                                                         path17 ==
                                                                                                         true &&
                                                                                                         path18 ==
                                                                                                         true &&
                                                                                                         path19 ==
                                                                                                         true &&
                                                                                                         path20 ==
                                                                                                         true
                                                                                                         &&
                                                                                                         path21 ==
                                                                                                         true &&
                                                                                                         path22 ==
                                                                                                         true &&
                                                                                                         path23 ==
                                                                                                         true &&
                                                                                                         path24 ==
                                                                                                         false)
                                                                                                        {
                                                                                                            Body
                                                                                                                .WalkTo(
                                                                                                                    point24,
                                                                                                                    100);
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            path24 =
                                                                                                                true;
                                                                                                            if
                                                                                                                (!
                                                                                                                     Body
                                                                                                                         .IsWithinRadius(
                                                                                                                             point25,
                                                                                                                             30) &&
                                                                                                                 path1 ==
                                                                                                                 true &&
                                                                                                                 path2 ==
                                                                                                                 true &&
                                                                                                                 path3 ==
                                                                                                                 true &&
                                                                                                                 path4 ==
                                                                                                                 true &&
                                                                                                                 path5 ==
                                                                                                                 true &&
                                                                                                                 path6 ==
                                                                                                                 true &&
                                                                                                                 path7 ==
                                                                                                                 true &&
                                                                                                                 path8 ==
                                                                                                                 true &&
                                                                                                                 path9 ==
                                                                                                                 true &&
                                                                                                                 path10 ==
                                                                                                                 true
                                                                                                                 &&
                                                                                                                 path11 ==
                                                                                                                 true &&
                                                                                                                 path12 ==
                                                                                                                 true &&
                                                                                                                 path13 ==
                                                                                                                 true &&
                                                                                                                 path14 ==
                                                                                                                 true &&
                                                                                                                 path15 ==
                                                                                                                 true &&
                                                                                                                 path16 ==
                                                                                                                 true &&
                                                                                                                 path17 ==
                                                                                                                 true &&
                                                                                                                 path18 ==
                                                                                                                 true &&
                                                                                                                 path19 ==
                                                                                                                 true &&
                                                                                                                 path20 ==
                                                                                                                 true
                                                                                                                 &&
                                                                                                                 path21 ==
                                                                                                                 true &&
                                                                                                                 path22 ==
                                                                                                                 true &&
                                                                                                                 path23 ==
                                                                                                                 true &&
                                                                                                                 path24 ==
                                                                                                                 true &&
                                                                                                                 path25 ==
                                                                                                                 false)
                                                                                                            {
                                                                                                                Body
                                                                                                                    .WalkTo(
                                                                                                                        point25,
                                                                                                                        100);
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                path25 =
                                                                                                                    true;
                                                                                                                if
                                                                                                                    (!
                                                                                                                         Body
                                                                                                                             .IsWithinRadius(
                                                                                                                                 point26,
                                                                                                                                 30) &&
                                                                                                                     path1 ==
                                                                                                                     true &&
                                                                                                                     path2 ==
                                                                                                                     true &&
                                                                                                                     path3 ==
                                                                                                                     true &&
                                                                                                                     path4 ==
                                                                                                                     true &&
                                                                                                                     path5 ==
                                                                                                                     true &&
                                                                                                                     path6 ==
                                                                                                                     true &&
                                                                                                                     path7 ==
                                                                                                                     true &&
                                                                                                                     path8 ==
                                                                                                                     true &&
                                                                                                                     path9 ==
                                                                                                                     true &&
                                                                                                                     path10 ==
                                                                                                                     true
                                                                                                                     &&
                                                                                                                     path11 ==
                                                                                                                     true &&
                                                                                                                     path12 ==
                                                                                                                     true &&
                                                                                                                     path13 ==
                                                                                                                     true &&
                                                                                                                     path14 ==
                                                                                                                     true &&
                                                                                                                     path15 ==
                                                                                                                     true &&
                                                                                                                     path16 ==
                                                                                                                     true &&
                                                                                                                     path17 ==
                                                                                                                     true &&
                                                                                                                     path18 ==
                                                                                                                     true &&
                                                                                                                     path19 ==
                                                                                                                     true &&
                                                                                                                     path20 ==
                                                                                                                     true
                                                                                                                     &&
                                                                                                                     path21 ==
                                                                                                                     true &&
                                                                                                                     path22 ==
                                                                                                                     true &&
                                                                                                                     path23 ==
                                                                                                                     true &&
                                                                                                                     path24 ==
                                                                                                                     true &&
                                                                                                                     path25 ==
                                                                                                                     true &&
                                                                                                                     path26 ==
                                                                                                                     false)
                                                                                                                {
                                                                                                                    Body
                                                                                                                        .WalkTo(
                                                                                                                            point26,
                                                                                                                            100);
                                                                                                                }
                                                                                                                else
                                                                                                                {
                                                                                                                    path26 =
                                                                                                                        true;
                                                                                                                    if
                                                                                                                        (!
                                                                                                                             Body
                                                                                                                                 .IsWithinRadius(
                                                                                                                                     point27,
                                                                                                                                     30) &&
                                                                                                                         path1 ==
                                                                                                                         true &&
                                                                                                                         path2 ==
                                                                                                                         true &&
                                                                                                                         path3 ==
                                                                                                                         true &&
                                                                                                                         path4 ==
                                                                                                                         true &&
                                                                                                                         path5 ==
                                                                                                                         true &&
                                                                                                                         path6 ==
                                                                                                                         true &&
                                                                                                                         path7 ==
                                                                                                                         true &&
                                                                                                                         path8 ==
                                                                                                                         true &&
                                                                                                                         path9 ==
                                                                                                                         true &&
                                                                                                                         path10 ==
                                                                                                                         true
                                                                                                                         &&
                                                                                                                         path11 ==
                                                                                                                         true &&
                                                                                                                         path12 ==
                                                                                                                         true &&
                                                                                                                         path13 ==
                                                                                                                         true &&
                                                                                                                         path14 ==
                                                                                                                         true &&
                                                                                                                         path15 ==
                                                                                                                         true &&
                                                                                                                         path16 ==
                                                                                                                         true &&
                                                                                                                         path17 ==
                                                                                                                         true &&
                                                                                                                         path18 ==
                                                                                                                         true &&
                                                                                                                         path19 ==
                                                                                                                         true &&
                                                                                                                         path20 ==
                                                                                                                         true
                                                                                                                         &&
                                                                                                                         path21 ==
                                                                                                                         true &&
                                                                                                                         path22 ==
                                                                                                                         true &&
                                                                                                                         path23 ==
                                                                                                                         true &&
                                                                                                                         path24 ==
                                                                                                                         true &&
                                                                                                                         path25 ==
                                                                                                                         true &&
                                                                                                                         path26 ==
                                                                                                                         true &&
                                                                                                                         path27 ==
                                                                                                                         false)
                                                                                                                    {
                                                                                                                        Body
                                                                                                                            .WalkTo(
                                                                                                                                point27,
                                                                                                                                100);
                                                                                                                    }
                                                                                                                    else
                                                                                                                    {
                                                                                                                        path27 =
                                                                                                                            true;
                                                                                                                        if
                                                                                                                            (!
                                                                                                                                 Body
                                                                                                                                     .IsWithinRadius(
                                                                                                                                         point28,
                                                                                                                                         30) &&
                                                                                                                             path1 ==
                                                                                                                             true &&
                                                                                                                             path2 ==
                                                                                                                             true &&
                                                                                                                             path3 ==
                                                                                                                             true &&
                                                                                                                             path4 ==
                                                                                                                             true &&
                                                                                                                             path5 ==
                                                                                                                             true &&
                                                                                                                             path6 ==
                                                                                                                             true &&
                                                                                                                             path7 ==
                                                                                                                             true &&
                                                                                                                             path8 ==
                                                                                                                             true &&
                                                                                                                             path9 ==
                                                                                                                             true &&
                                                                                                                             path10 ==
                                                                                                                             true
                                                                                                                             &&
                                                                                                                             path11 ==
                                                                                                                             true &&
                                                                                                                             path12 ==
                                                                                                                             true &&
                                                                                                                             path13 ==
                                                                                                                             true &&
                                                                                                                             path14 ==
                                                                                                                             true &&
                                                                                                                             path15 ==
                                                                                                                             true &&
                                                                                                                             path16 ==
                                                                                                                             true &&
                                                                                                                             path17 ==
                                                                                                                             true &&
                                                                                                                             path18 ==
                                                                                                                             true &&
                                                                                                                             path19 ==
                                                                                                                             true &&
                                                                                                                             path20 ==
                                                                                                                             true
                                                                                                                             &&
                                                                                                                             path21 ==
                                                                                                                             true &&
                                                                                                                             path22 ==
                                                                                                                             true &&
                                                                                                                             path23 ==
                                                                                                                             true &&
                                                                                                                             path24 ==
                                                                                                                             true &&
                                                                                                                             path25 ==
                                                                                                                             true &&
                                                                                                                             path26 ==
                                                                                                                             true &&
                                                                                                                             path27 ==
                                                                                                                             true &&
                                                                                                                             path28 ==
                                                                                                                             false)
                                                                                                                        {
                                                                                                                            Body
                                                                                                                                .WalkTo(
                                                                                                                                    point28,
                                                                                                                                    100);
                                                                                                                        }
                                                                                                                        else
                                                                                                                        {
                                                                                                                            path28 =
                                                                                                                                true;
                                                                                                                            if
                                                                                                                                (!
                                                                                                                                     Body
                                                                                                                                         .IsWithinRadius(
                                                                                                                                             point29,
                                                                                                                                             30) &&
                                                                                                                                 path1 ==
                                                                                                                                 true &&
                                                                                                                                 path2 ==
                                                                                                                                 true &&
                                                                                                                                 path3 ==
                                                                                                                                 true &&
                                                                                                                                 path4 ==
                                                                                                                                 true &&
                                                                                                                                 path5 ==
                                                                                                                                 true &&
                                                                                                                                 path6 ==
                                                                                                                                 true &&
                                                                                                                                 path7 ==
                                                                                                                                 true &&
                                                                                                                                 path8 ==
                                                                                                                                 true &&
                                                                                                                                 path9 ==
                                                                                                                                 true &&
                                                                                                                                 path10 ==
                                                                                                                                 true
                                                                                                                                 &&
                                                                                                                                 path11 ==
                                                                                                                                 true &&
                                                                                                                                 path12 ==
                                                                                                                                 true &&
                                                                                                                                 path13 ==
                                                                                                                                 true &&
                                                                                                                                 path14 ==
                                                                                                                                 true &&
                                                                                                                                 path15 ==
                                                                                                                                 true &&
                                                                                                                                 path16 ==
                                                                                                                                 true &&
                                                                                                                                 path17 ==
                                                                                                                                 true &&
                                                                                                                                 path18 ==
                                                                                                                                 true &&
                                                                                                                                 path19 ==
                                                                                                                                 true &&
                                                                                                                                 path20 ==
                                                                                                                                 true
                                                                                                                                 &&
                                                                                                                                 path21 ==
                                                                                                                                 true &&
                                                                                                                                 path22 ==
                                                                                                                                 true &&
                                                                                                                                 path23 ==
                                                                                                                                 true &&
                                                                                                                                 path24 ==
                                                                                                                                 true &&
                                                                                                                                 path25 ==
                                                                                                                                 true &&
                                                                                                                                 path26 ==
                                                                                                                                 true &&
                                                                                                                                 path27 ==
                                                                                                                                 true &&
                                                                                                                                 path28 ==
                                                                                                                                 true &&
                                                                                                                                 path29 ==
                                                                                                                                 false)
                                                                                                                            {
                                                                                                                                Body
                                                                                                                                    .WalkTo(
                                                                                                                                        point29,
                                                                                                                                        100);
                                                                                                                            }
                                                                                                                            else
                                                                                                                            {
                                                                                                                                path29 =
                                                                                                                                    true;
                                                                                                                                if
                                                                                                                                    (!
                                                                                                                                         Body
                                                                                                                                             .IsWithinRadius(
                                                                                                                                                 point30,
                                                                                                                                                 30) &&
                                                                                                                                     path1 ==
                                                                                                                                     true &&
                                                                                                                                     path2 ==
                                                                                                                                     true &&
                                                                                                                                     path3 ==
                                                                                                                                     true &&
                                                                                                                                     path4 ==
                                                                                                                                     true &&
                                                                                                                                     path5 ==
                                                                                                                                     true &&
                                                                                                                                     path6 ==
                                                                                                                                     true &&
                                                                                                                                     path7 ==
                                                                                                                                     true &&
                                                                                                                                     path8 ==
                                                                                                                                     true &&
                                                                                                                                     path9 ==
                                                                                                                                     true &&
                                                                                                                                     path10 ==
                                                                                                                                     true
                                                                                                                                     &&
                                                                                                                                     path11 ==
                                                                                                                                     true &&
                                                                                                                                     path12 ==
                                                                                                                                     true &&
                                                                                                                                     path13 ==
                                                                                                                                     true &&
                                                                                                                                     path14 ==
                                                                                                                                     true &&
                                                                                                                                     path15 ==
                                                                                                                                     true &&
                                                                                                                                     path16 ==
                                                                                                                                     true &&
                                                                                                                                     path17 ==
                                                                                                                                     true &&
                                                                                                                                     path18 ==
                                                                                                                                     true &&
                                                                                                                                     path19 ==
                                                                                                                                     true &&
                                                                                                                                     path20 ==
                                                                                                                                     true
                                                                                                                                     &&
                                                                                                                                     path21 ==
                                                                                                                                     true &&
                                                                                                                                     path22 ==
                                                                                                                                     true &&
                                                                                                                                     path23 ==
                                                                                                                                     true &&
                                                                                                                                     path24 ==
                                                                                                                                     true &&
                                                                                                                                     path25 ==
                                                                                                                                     true &&
                                                                                                                                     path26 ==
                                                                                                                                     true &&
                                                                                                                                     path27 ==
                                                                                                                                     true &&
                                                                                                                                     path28 ==
                                                                                                                                     true &&
                                                                                                                                     path29 ==
                                                                                                                                     true &&
                                                                                                                                     path30 ==
                                                                                                                                     false)
                                                                                                                                {
                                                                                                                                    Body
                                                                                                                                        .WalkTo(
                                                                                                                                            point30,
                                                                                                                                            100);
                                                                                                                                }
                                                                                                                                else
                                                                                                                                {
                                                                                                                                    path30 =
                                                                                                                                        true;
                                                                                                                                    if
                                                                                                                                        (!
                                                                                                                                             Body
                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                     point31,
                                                                                                                                                     30) &&
                                                                                                                                         path1 ==
                                                                                                                                         true &&
                                                                                                                                         path2 ==
                                                                                                                                         true &&
                                                                                                                                         path3 ==
                                                                                                                                         true &&
                                                                                                                                         path4 ==
                                                                                                                                         true &&
                                                                                                                                         path5 ==
                                                                                                                                         true &&
                                                                                                                                         path6 ==
                                                                                                                                         true &&
                                                                                                                                         path7 ==
                                                                                                                                         true &&
                                                                                                                                         path8 ==
                                                                                                                                         true &&
                                                                                                                                         path9 ==
                                                                                                                                         true &&
                                                                                                                                         path10 ==
                                                                                                                                         true
                                                                                                                                         &&
                                                                                                                                         path11 ==
                                                                                                                                         true &&
                                                                                                                                         path12 ==
                                                                                                                                         true &&
                                                                                                                                         path13 ==
                                                                                                                                         true &&
                                                                                                                                         path14 ==
                                                                                                                                         true &&
                                                                                                                                         path15 ==
                                                                                                                                         true &&
                                                                                                                                         path16 ==
                                                                                                                                         true &&
                                                                                                                                         path17 ==
                                                                                                                                         true &&
                                                                                                                                         path18 ==
                                                                                                                                         true &&
                                                                                                                                         path19 ==
                                                                                                                                         true &&
                                                                                                                                         path20 ==
                                                                                                                                         true
                                                                                                                                         &&
                                                                                                                                         path21 ==
                                                                                                                                         true &&
                                                                                                                                         path22 ==
                                                                                                                                         true &&
                                                                                                                                         path23 ==
                                                                                                                                         true &&
                                                                                                                                         path24 ==
                                                                                                                                         true &&
                                                                                                                                         path25 ==
                                                                                                                                         true &&
                                                                                                                                         path26 ==
                                                                                                                                         true &&
                                                                                                                                         path27 ==
                                                                                                                                         true &&
                                                                                                                                         path28 ==
                                                                                                                                         true &&
                                                                                                                                         path29 ==
                                                                                                                                         true &&
                                                                                                                                         path30 ==
                                                                                                                                         true
                                                                                                                                         &&
                                                                                                                                         path31 ==
                                                                                                                                         false)
                                                                                                                                    {
                                                                                                                                        Body
                                                                                                                                            .WalkTo(
                                                                                                                                                point31,
                                                                                                                                                100);
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                    {
                                                                                                                                        path31 =
                                                                                                                                            true;
                                                                                                                                        if
                                                                                                                                            (!
                                                                                                                                                 Body
                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                         point32,
                                                                                                                                                         30) &&
                                                                                                                                             path1 ==
                                                                                                                                             true &&
                                                                                                                                             path2 ==
                                                                                                                                             true &&
                                                                                                                                             path3 ==
                                                                                                                                             true &&
                                                                                                                                             path4 ==
                                                                                                                                             true &&
                                                                                                                                             path5 ==
                                                                                                                                             true &&
                                                                                                                                             path6 ==
                                                                                                                                             true &&
                                                                                                                                             path7 ==
                                                                                                                                             true &&
                                                                                                                                             path8 ==
                                                                                                                                             true &&
                                                                                                                                             path9 ==
                                                                                                                                             true &&
                                                                                                                                             path10 ==
                                                                                                                                             true
                                                                                                                                             &&
                                                                                                                                             path11 ==
                                                                                                                                             true &&
                                                                                                                                             path12 ==
                                                                                                                                             true &&
                                                                                                                                             path13 ==
                                                                                                                                             true &&
                                                                                                                                             path14 ==
                                                                                                                                             true &&
                                                                                                                                             path15 ==
                                                                                                                                             true &&
                                                                                                                                             path16 ==
                                                                                                                                             true &&
                                                                                                                                             path17 ==
                                                                                                                                             true &&
                                                                                                                                             path18 ==
                                                                                                                                             true &&
                                                                                                                                             path19 ==
                                                                                                                                             true &&
                                                                                                                                             path20 ==
                                                                                                                                             true
                                                                                                                                             &&
                                                                                                                                             path21 ==
                                                                                                                                             true &&
                                                                                                                                             path22 ==
                                                                                                                                             true &&
                                                                                                                                             path23 ==
                                                                                                                                             true &&
                                                                                                                                             path24 ==
                                                                                                                                             true &&
                                                                                                                                             path25 ==
                                                                                                                                             true &&
                                                                                                                                             path26 ==
                                                                                                                                             true &&
                                                                                                                                             path27 ==
                                                                                                                                             true &&
                                                                                                                                             path28 ==
                                                                                                                                             true &&
                                                                                                                                             path29 ==
                                                                                                                                             true &&
                                                                                                                                             path30 ==
                                                                                                                                             true
                                                                                                                                             &&
                                                                                                                                             path31 ==
                                                                                                                                             true &&
                                                                                                                                             path32 ==
                                                                                                                                             false)
                                                                                                                                        {
                                                                                                                                            Body
                                                                                                                                                .WalkTo(
                                                                                                                                                    point32,
                                                                                                                                                    100);
                                                                                                                                        }
                                                                                                                                        else
                                                                                                                                        {
                                                                                                                                            path32 =
                                                                                                                                                true;
                                                                                                                                            if
                                                                                                                                                (!
                                                                                                                                                     Body
                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                             point33,
                                                                                                                                                             30) &&
                                                                                                                                                 path1 ==
                                                                                                                                                 true &&
                                                                                                                                                 path2 ==
                                                                                                                                                 true &&
                                                                                                                                                 path3 ==
                                                                                                                                                 true &&
                                                                                                                                                 path4 ==
                                                                                                                                                 true &&
                                                                                                                                                 path5 ==
                                                                                                                                                 true &&
                                                                                                                                                 path6 ==
                                                                                                                                                 true &&
                                                                                                                                                 path7 ==
                                                                                                                                                 true &&
                                                                                                                                                 path8 ==
                                                                                                                                                 true &&
                                                                                                                                                 path9 ==
                                                                                                                                                 true &&
                                                                                                                                                 path10 ==
                                                                                                                                                 true
                                                                                                                                                 &&
                                                                                                                                                 path11 ==
                                                                                                                                                 true &&
                                                                                                                                                 path12 ==
                                                                                                                                                 true &&
                                                                                                                                                 path13 ==
                                                                                                                                                 true &&
                                                                                                                                                 path14 ==
                                                                                                                                                 true &&
                                                                                                                                                 path15 ==
                                                                                                                                                 true &&
                                                                                                                                                 path16 ==
                                                                                                                                                 true &&
                                                                                                                                                 path17 ==
                                                                                                                                                 true &&
                                                                                                                                                 path18 ==
                                                                                                                                                 true &&
                                                                                                                                                 path19 ==
                                                                                                                                                 true &&
                                                                                                                                                 path20 ==
                                                                                                                                                 true
                                                                                                                                                 &&
                                                                                                                                                 path21 ==
                                                                                                                                                 true &&
                                                                                                                                                 path22 ==
                                                                                                                                                 true &&
                                                                                                                                                 path23 ==
                                                                                                                                                 true &&
                                                                                                                                                 path24 ==
                                                                                                                                                 true &&
                                                                                                                                                 path25 ==
                                                                                                                                                 true &&
                                                                                                                                                 path26 ==
                                                                                                                                                 true &&
                                                                                                                                                 path27 ==
                                                                                                                                                 true &&
                                                                                                                                                 path28 ==
                                                                                                                                                 true &&
                                                                                                                                                 path29 ==
                                                                                                                                                 true &&
                                                                                                                                                 path30 ==
                                                                                                                                                 true
                                                                                                                                                 &&
                                                                                                                                                 path31 ==
                                                                                                                                                 true &&
                                                                                                                                                 path32 ==
                                                                                                                                                 true &&
                                                                                                                                                 path33 ==
                                                                                                                                                 false)
                                                                                                                                            {
                                                                                                                                                Body
                                                                                                                                                    .WalkTo(
                                                                                                                                                        point33,
                                                                                                                                                        100);
                                                                                                                                            }
                                                                                                                                            else
                                                                                                                                            {
                                                                                                                                                path33 =
                                                                                                                                                    true;
                                                                                                                                                if
                                                                                                                                                    (!
                                                                                                                                                         Body
                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                 point34,
                                                                                                                                                                 30) &&
                                                                                                                                                     path1 ==
                                                                                                                                                     true &&
                                                                                                                                                     path2 ==
                                                                                                                                                     true &&
                                                                                                                                                     path3 ==
                                                                                                                                                     true &&
                                                                                                                                                     path4 ==
                                                                                                                                                     true &&
                                                                                                                                                     path5 ==
                                                                                                                                                     true &&
                                                                                                                                                     path6 ==
                                                                                                                                                     true &&
                                                                                                                                                     path7 ==
                                                                                                                                                     true &&
                                                                                                                                                     path8 ==
                                                                                                                                                     true &&
                                                                                                                                                     path9 ==
                                                                                                                                                     true &&
                                                                                                                                                     path10 ==
                                                                                                                                                     true
                                                                                                                                                     &&
                                                                                                                                                     path11 ==
                                                                                                                                                     true &&
                                                                                                                                                     path12 ==
                                                                                                                                                     true &&
                                                                                                                                                     path13 ==
                                                                                                                                                     true &&
                                                                                                                                                     path14 ==
                                                                                                                                                     true &&
                                                                                                                                                     path15 ==
                                                                                                                                                     true &&
                                                                                                                                                     path16 ==
                                                                                                                                                     true &&
                                                                                                                                                     path17 ==
                                                                                                                                                     true &&
                                                                                                                                                     path18 ==
                                                                                                                                                     true &&
                                                                                                                                                     path19 ==
                                                                                                                                                     true &&
                                                                                                                                                     path20 ==
                                                                                                                                                     true
                                                                                                                                                     &&
                                                                                                                                                     path21 ==
                                                                                                                                                     true &&
                                                                                                                                                     path22 ==
                                                                                                                                                     true &&
                                                                                                                                                     path23 ==
                                                                                                                                                     true &&
                                                                                                                                                     path24 ==
                                                                                                                                                     true &&
                                                                                                                                                     path25 ==
                                                                                                                                                     true &&
                                                                                                                                                     path26 ==
                                                                                                                                                     true &&
                                                                                                                                                     path27 ==
                                                                                                                                                     true &&
                                                                                                                                                     path28 ==
                                                                                                                                                     true &&
                                                                                                                                                     path29 ==
                                                                                                                                                     true &&
                                                                                                                                                     path30 ==
                                                                                                                                                     true
                                                                                                                                                     &&
                                                                                                                                                     path31 ==
                                                                                                                                                     true &&
                                                                                                                                                     path32 ==
                                                                                                                                                     true &&
                                                                                                                                                     path33 ==
                                                                                                                                                     true &&
                                                                                                                                                     path34 ==
                                                                                                                                                     false)
                                                                                                                                                {
                                                                                                                                                    Body
                                                                                                                                                        .WalkTo(
                                                                                                                                                            point34,
                                                                                                                                                            100);
                                                                                                                                                }
                                                                                                                                                else
                                                                                                                                                {
                                                                                                                                                    path34 =
                                                                                                                                                        true;
                                                                                                                                                    if
                                                                                                                                                        (!
                                                                                                                                                             Body
                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                     point35,
                                                                                                                                                                     30) &&
                                                                                                                                                         path1 ==
                                                                                                                                                         true &&
                                                                                                                                                         path2 ==
                                                                                                                                                         true &&
                                                                                                                                                         path3 ==
                                                                                                                                                         true &&
                                                                                                                                                         path4 ==
                                                                                                                                                         true &&
                                                                                                                                                         path5 ==
                                                                                                                                                         true &&
                                                                                                                                                         path6 ==
                                                                                                                                                         true &&
                                                                                                                                                         path7 ==
                                                                                                                                                         true &&
                                                                                                                                                         path8 ==
                                                                                                                                                         true &&
                                                                                                                                                         path9 ==
                                                                                                                                                         true &&
                                                                                                                                                         path10 ==
                                                                                                                                                         true
                                                                                                                                                         &&
                                                                                                                                                         path11 ==
                                                                                                                                                         true &&
                                                                                                                                                         path12 ==
                                                                                                                                                         true &&
                                                                                                                                                         path13 ==
                                                                                                                                                         true &&
                                                                                                                                                         path14 ==
                                                                                                                                                         true &&
                                                                                                                                                         path15 ==
                                                                                                                                                         true &&
                                                                                                                                                         path16 ==
                                                                                                                                                         true &&
                                                                                                                                                         path17 ==
                                                                                                                                                         true &&
                                                                                                                                                         path18 ==
                                                                                                                                                         true &&
                                                                                                                                                         path19 ==
                                                                                                                                                         true &&
                                                                                                                                                         path20 ==
                                                                                                                                                         true
                                                                                                                                                         &&
                                                                                                                                                         path21 ==
                                                                                                                                                         true &&
                                                                                                                                                         path22 ==
                                                                                                                                                         true &&
                                                                                                                                                         path23 ==
                                                                                                                                                         true &&
                                                                                                                                                         path24 ==
                                                                                                                                                         true &&
                                                                                                                                                         path25 ==
                                                                                                                                                         true &&
                                                                                                                                                         path26 ==
                                                                                                                                                         true &&
                                                                                                                                                         path27 ==
                                                                                                                                                         true &&
                                                                                                                                                         path28 ==
                                                                                                                                                         true &&
                                                                                                                                                         path29 ==
                                                                                                                                                         true &&
                                                                                                                                                         path30 ==
                                                                                                                                                         true
                                                                                                                                                         &&
                                                                                                                                                         path31 ==
                                                                                                                                                         true &&
                                                                                                                                                         path32 ==
                                                                                                                                                         true &&
                                                                                                                                                         path33 ==
                                                                                                                                                         true &&
                                                                                                                                                         path34 ==
                                                                                                                                                         true &&
                                                                                                                                                         path35 ==
                                                                                                                                                         false)
                                                                                                                                                    {
                                                                                                                                                        Body
                                                                                                                                                            .WalkTo(
                                                                                                                                                                point35,
                                                                                                                                                                100);
                                                                                                                                                    }
                                                                                                                                                    else
                                                                                                                                                    {
                                                                                                                                                        path35 =
                                                                                                                                                            true;
                                                                                                                                                        if
                                                                                                                                                            (!
                                                                                                                                                                 Body
                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                         point36,
                                                                                                                                                                         30) &&
                                                                                                                                                             path1 ==
                                                                                                                                                             true &&
                                                                                                                                                             path2 ==
                                                                                                                                                             true &&
                                                                                                                                                             path3 ==
                                                                                                                                                             true &&
                                                                                                                                                             path4 ==
                                                                                                                                                             true &&
                                                                                                                                                             path5 ==
                                                                                                                                                             true &&
                                                                                                                                                             path6 ==
                                                                                                                                                             true &&
                                                                                                                                                             path7 ==
                                                                                                                                                             true &&
                                                                                                                                                             path8 ==
                                                                                                                                                             true &&
                                                                                                                                                             path9 ==
                                                                                                                                                             true &&
                                                                                                                                                             path10 ==
                                                                                                                                                             true
                                                                                                                                                             &&
                                                                                                                                                             path11 ==
                                                                                                                                                             true &&
                                                                                                                                                             path12 ==
                                                                                                                                                             true &&
                                                                                                                                                             path13 ==
                                                                                                                                                             true &&
                                                                                                                                                             path14 ==
                                                                                                                                                             true &&
                                                                                                                                                             path15 ==
                                                                                                                                                             true &&
                                                                                                                                                             path16 ==
                                                                                                                                                             true &&
                                                                                                                                                             path17 ==
                                                                                                                                                             true &&
                                                                                                                                                             path18 ==
                                                                                                                                                             true &&
                                                                                                                                                             path19 ==
                                                                                                                                                             true &&
                                                                                                                                                             path20 ==
                                                                                                                                                             true
                                                                                                                                                             &&
                                                                                                                                                             path21 ==
                                                                                                                                                             true &&
                                                                                                                                                             path22 ==
                                                                                                                                                             true &&
                                                                                                                                                             path23 ==
                                                                                                                                                             true &&
                                                                                                                                                             path24 ==
                                                                                                                                                             true &&
                                                                                                                                                             path25 ==
                                                                                                                                                             true &&
                                                                                                                                                             path26 ==
                                                                                                                                                             true &&
                                                                                                                                                             path27 ==
                                                                                                                                                             true &&
                                                                                                                                                             path28 ==
                                                                                                                                                             true &&
                                                                                                                                                             path29 ==
                                                                                                                                                             true &&
                                                                                                                                                             path30 ==
                                                                                                                                                             true
                                                                                                                                                             &&
                                                                                                                                                             path31 ==
                                                                                                                                                             true &&
                                                                                                                                                             path32 ==
                                                                                                                                                             true &&
                                                                                                                                                             path33 ==
                                                                                                                                                             true &&
                                                                                                                                                             path34 ==
                                                                                                                                                             true &&
                                                                                                                                                             path35 ==
                                                                                                                                                             true &&
                                                                                                                                                             path36 ==
                                                                                                                                                             false)
                                                                                                                                                        {
                                                                                                                                                            Body
                                                                                                                                                                .WalkTo(
                                                                                                                                                                    point36,
                                                                                                                                                                    100);
                                                                                                                                                        }
                                                                                                                                                        else
                                                                                                                                                        {
                                                                                                                                                            path36 =
                                                                                                                                                                true;
                                                                                                                                                            if
                                                                                                                                                                (!
                                                                                                                                                                     Body
                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                             point37,
                                                                                                                                                                             30) &&
                                                                                                                                                                 path1 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path2 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path3 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path4 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path5 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path6 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path7 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path8 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path9 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path10 ==
                                                                                                                                                                 true
                                                                                                                                                                 &&
                                                                                                                                                                 path11 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path12 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path13 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path14 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path15 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path16 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path17 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path18 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path19 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path20 ==
                                                                                                                                                                 true
                                                                                                                                                                 &&
                                                                                                                                                                 path21 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path22 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path23 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path24 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path25 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path26 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path27 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path28 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path29 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path30 ==
                                                                                                                                                                 true
                                                                                                                                                                 &&
                                                                                                                                                                 path31 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path32 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path33 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path34 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path35 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path36 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path37 ==
                                                                                                                                                                 false)
                                                                                                                                                            {
                                                                                                                                                                Body
                                                                                                                                                                    .WalkTo(
                                                                                                                                                                        point37,
                                                                                                                                                                        100);
                                                                                                                                                            }
                                                                                                                                                            else
                                                                                                                                                            {
                                                                                                                                                                path37 =
                                                                                                                                                                    true;
                                                                                                                                                                if
                                                                                                                                                                    (!
                                                                                                                                                                         Body
                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                 point38,
                                                                                                                                                                                 30) &&
                                                                                                                                                                     path1 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path2 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path3 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path4 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path5 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path6 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path7 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path8 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path9 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path10 ==
                                                                                                                                                                     true
                                                                                                                                                                     &&
                                                                                                                                                                     path11 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path12 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path13 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path14 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path15 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path16 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path17 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path18 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path19 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path20 ==
                                                                                                                                                                     true
                                                                                                                                                                     &&
                                                                                                                                                                     path21 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path22 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path23 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path24 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path25 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path26 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path27 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path28 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path29 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path30 ==
                                                                                                                                                                     true
                                                                                                                                                                     &&
                                                                                                                                                                     path31 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path32 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path33 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path34 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path35 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path36 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path37 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path38 ==
                                                                                                                                                                     false)
                                                                                                                                                                {
                                                                                                                                                                    Body
                                                                                                                                                                        .WalkTo(
                                                                                                                                                                            point38,
                                                                                                                                                                            100);
                                                                                                                                                                }
                                                                                                                                                                else
                                                                                                                                                                {
                                                                                                                                                                    path38 =
                                                                                                                                                                        true;
                                                                                                                                                                    if
                                                                                                                                                                        (!
                                                                                                                                                                             Body
                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                     point39,
                                                                                                                                                                                     30) &&
                                                                                                                                                                         path1 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path2 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path3 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path4 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path5 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path6 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path7 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path8 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path9 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path10 ==
                                                                                                                                                                         true
                                                                                                                                                                         &&
                                                                                                                                                                         path11 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path12 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path13 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path14 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path15 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path16 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path17 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path18 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path19 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path20 ==
                                                                                                                                                                         true
                                                                                                                                                                         &&
                                                                                                                                                                         path21 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path22 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path23 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path24 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path25 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path26 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path27 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path28 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path29 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path30 ==
                                                                                                                                                                         true
                                                                                                                                                                         &&
                                                                                                                                                                         path31 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path32 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path33 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path34 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path35 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path36 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path37 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path38 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path39 ==
                                                                                                                                                                         false)
                                                                                                                                                                    {
                                                                                                                                                                        Body
                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                point39,
                                                                                                                                                                                100);
                                                                                                                                                                    }
                                                                                                                                                                    else
                                                                                                                                                                    {
                                                                                                                                                                        path39 =
                                                                                                                                                                            true;
                                                                                                                                                                        if
                                                                                                                                                                            (!
                                                                                                                                                                                 Body
                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                         point40,
                                                                                                                                                                                         30) &&
                                                                                                                                                                             path1 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path2 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path3 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path4 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path5 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path6 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path7 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path8 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path9 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path10 ==
                                                                                                                                                                             true
                                                                                                                                                                             &&
                                                                                                                                                                             path11 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path12 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path13 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path14 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path15 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path16 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path17 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path18 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path19 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path20 ==
                                                                                                                                                                             true
                                                                                                                                                                             &&
                                                                                                                                                                             path21 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path22 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path23 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path24 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path25 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path26 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path27 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path28 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path29 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path30 ==
                                                                                                                                                                             true
                                                                                                                                                                             &&
                                                                                                                                                                             path31 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path32 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path33 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path34 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path35 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path36 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path37 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path38 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path39 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path40 ==
                                                                                                                                                                             false)
                                                                                                                                                                        {
                                                                                                                                                                            Body
                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                    point40,
                                                                                                                                                                                    100);
                                                                                                                                                                        }
                                                                                                                                                                        else
                                                                                                                                                                        {
                                                                                                                                                                            path40 =
                                                                                                                                                                                true;
                                                                                                                                                                            if
                                                                                                                                                                                (!
                                                                                                                                                                                     Body
                                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                                             point41,
                                                                                                                                                                                             30) &&
                                                                                                                                                                                 path1 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path2 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path3 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path4 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path5 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path6 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path7 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path8 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path9 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path10 ==
                                                                                                                                                                                 true
                                                                                                                                                                                 &&
                                                                                                                                                                                 path11 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path12 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path13 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path14 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path15 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path16 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path17 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path18 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path19 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path20 ==
                                                                                                                                                                                 true
                                                                                                                                                                                 &&
                                                                                                                                                                                 path21 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path22 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path23 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path24 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path25 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path26 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path27 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path28 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path29 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path30 ==
                                                                                                                                                                                 true
                                                                                                                                                                                 &&
                                                                                                                                                                                 path31 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path32 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path33 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path34 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path35 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path36 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path37 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path38 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path39 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path40 ==
                                                                                                                                                                                 true
                                                                                                                                                                                 &&
                                                                                                                                                                                 path41 ==
                                                                                                                                                                                 false)
                                                                                                                                                                            {
                                                                                                                                                                                Body
                                                                                                                                                                                    .WalkTo(
                                                                                                                                                                                        point41,
                                                                                                                                                                                        100);
                                                                                                                                                                            }
                                                                                                                                                                            else
                                                                                                                                                                            {
                                                                                                                                                                                path41 =
                                                                                                                                                                                    true;
                                                                                                                                                                                if
                                                                                                                                                                                    (!
                                                                                                                                                                                         Body
                                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                                 point42,
                                                                                                                                                                                                 30) &&
                                                                                                                                                                                     path1 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path2 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path3 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path4 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path5 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path6 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path7 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path8 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path9 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path10 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path11 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path12 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path13 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path14 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path15 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path16 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path17 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path18 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path19 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path20 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path21 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path22 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path23 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path24 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path25 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path26 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path27 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path28 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path29 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path30 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path31 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path32 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path33 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path34 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path35 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path36 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path37 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path38 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path39 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path40 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path41 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path42 ==
                                                                                                                                                                                     false)
                                                                                                                                                                                {
                                                                                                                                                                                    Body
                                                                                                                                                                                        .WalkTo(
                                                                                                                                                                                            point42,
                                                                                                                                                                                            100);
                                                                                                                                                                                }
                                                                                                                                                                                else
                                                                                                                                                                                {
                                                                                                                                                                                    path42 =
                                                                                                                                                                                        true;
                                                                                                                                                                                    if
                                                                                                                                                                                        (!
                                                                                                                                                                                             Body
                                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                                     point43,
                                                                                                                                                                                                     30) &&
                                                                                                                                                                                         path1 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path2 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path3 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path4 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path5 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path6 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path7 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path8 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path9 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path10 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path11 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path12 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path13 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path14 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path15 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path16 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path17 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path18 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path19 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path20 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path21 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path22 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path23 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path24 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path25 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path26 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path27 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path28 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path29 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path30 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path31 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path32 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path33 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path34 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path35 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path36 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path37 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path38 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path39 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path40 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path41 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path42 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path43 ==
                                                                                                                                                                                         false)
                                                                                                                                                                                    {
                                                                                                                                                                                        Body
                                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                                point43,
                                                                                                                                                                                                100);
                                                                                                                                                                                    }
                                                                                                                                                                                    else
                                                                                                                                                                                    {
                                                                                                                                                                                        path43 =
                                                                                                                                                                                            true;
                                                                                                                                                                                        if
                                                                                                                                                                                            (!
                                                                                                                                                                                                 Body
                                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                                         point44,
                                                                                                                                                                                                         30) &&
                                                                                                                                                                                             path1 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path2 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path3 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path4 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path5 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path6 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path7 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path8 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path9 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path10 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path11 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path12 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path13 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path14 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path15 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path16 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path17 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path18 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path19 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path20 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path21 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path22 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path23 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path24 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path25 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path26 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path27 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path28 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path29 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path30 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path31 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path32 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path33 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path34 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path35 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path36 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path37 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path38 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path39 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path40 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path41 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path42 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path43 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path44 ==
                                                                                                                                                                                             false)
                                                                                                                                                                                        {
                                                                                                                                                                                            Body
                                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                                    point44,
                                                                                                                                                                                                    100);
                                                                                                                                                                                        }
                                                                                                                                                                                        else
                                                                                                                                                                                        {
                                                                                                                                                                                            path44 =
                                                                                                                                                                                                true;
                                                                                                                                                                                            if
                                                                                                                                                                                                (!
                                                                                                                                                                                                     Body
                                                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                                                             point45,
                                                                                                                                                                                                             30) &&
                                                                                                                                                                                                 path1 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path2 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path3 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path4 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path5 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path6 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path7 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path8 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path9 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path10 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path11 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path12 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path13 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path14 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path15 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path16 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path17 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path18 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path19 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path20 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path21 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path22 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path23 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path24 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path25 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path26 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path27 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path28 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path29 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path30 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path31 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path32 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path33 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path34 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path35 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path36 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path37 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path38 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path39 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path40 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path41 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path42 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path43 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path44 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path45 ==
                                                                                                                                                                                                 false)
                                                                                                                                                                                            {
                                                                                                                                                                                                Body
                                                                                                                                                                                                    .WalkTo(
                                                                                                                                                                                                        point45,
                                                                                                                                                                                                        100);
                                                                                                                                                                                            }
                                                                                                                                                                                            else
                                                                                                                                                                                            {
                                                                                                                                                                                                path45 =
                                                                                                                                                                                                    true;
                                                                                                                                                                                                if
                                                                                                                                                                                                    (!
                                                                                                                                                                                                         Body
                                                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                                                 point46,
                                                                                                                                                                                                                 30) &&
                                                                                                                                                                                                     path1 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path2 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path3 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path4 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path5 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path6 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path7 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path8 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path9 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path10 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path11 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path12 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path13 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path14 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path15 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path16 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path17 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path18 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path19 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path20 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path21 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path22 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path23 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path24 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path25 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path26 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path27 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path28 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path29 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path30 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path31 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path32 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path33 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path34 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path35 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path36 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path37 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path38 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path39 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path40 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path41 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path42 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path43 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path44 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path45 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path46 ==
                                                                                                                                                                                                     false)
                                                                                                                                                                                                {
                                                                                                                                                                                                    Body
                                                                                                                                                                                                        .WalkTo(
                                                                                                                                                                                                            point46,
                                                                                                                                                                                                            100);
                                                                                                                                                                                                }
                                                                                                                                                                                                else
                                                                                                                                                                                                {
                                                                                                                                                                                                    path46 =
                                                                                                                                                                                                        true;
                                                                                                                                                                                                    if
                                                                                                                                                                                                        (!
                                                                                                                                                                                                             Body
                                                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                                                     point47,
                                                                                                                                                                                                                     30) &&
                                                                                                                                                                                                         path1 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path2 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path3 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path4 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path5 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path6 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path7 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path8 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path9 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path10 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path11 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path12 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path13 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path14 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path15 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path16 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path17 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path18 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path19 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path20 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path21 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path22 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path23 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path24 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path25 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path26 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path27 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path28 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path29 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path30 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path31 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path32 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path33 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path34 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path35 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path36 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path37 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path38 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path39 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path40 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path41 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path42 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path43 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path44 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path45 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path46 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path47 ==
                                                                                                                                                                                                         false)
                                                                                                                                                                                                    {
                                                                                                                                                                                                        Body
                                                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                                                point47,
                                                                                                                                                                                                                100);
                                                                                                                                                                                                    }
                                                                                                                                                                                                    else
                                                                                                                                                                                                    {
                                                                                                                                                                                                        path47 =
                                                                                                                                                                                                            true;
                                                                                                                                                                                                        if
                                                                                                                                                                                                            (!
                                                                                                                                                                                                                 Body
                                                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                                                         point48,
                                                                                                                                                                                                                         30) &&
                                                                                                                                                                                                             path1 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path2 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path3 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path4 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path5 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path6 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path7 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path8 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path9 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path10 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path11 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path12 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path13 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path14 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path15 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path16 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path17 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path18 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path19 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path20 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path21 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path22 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path23 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path24 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path25 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path26 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path27 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path28 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path29 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path30 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path31 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path32 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path33 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path34 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path35 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path36 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path37 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path38 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path39 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path40 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path41 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path42 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path43 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path44 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path45 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path46 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path47 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path48 ==
                                                                                                                                                                                                             false)
                                                                                                                                                                                                        {
                                                                                                                                                                                                            Body
                                                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                                                    point48,
                                                                                                                                                                                                                    100);
                                                                                                                                                                                                        }
                                                                                                                                                                                                        else
                                                                                                                                                                                                        {
                                                                                                                                                                                                            path48 =
                                                                                                                                                                                                                true;
                                                                                                                                                                                                            if
                                                                                                                                                                                                                (!
                                                                                                                                                                                                                     Body
                                                                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                                                                             point49,
                                                                                                                                                                                                                             30) &&
                                                                                                                                                                                                                 path1 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path2 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path3 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path4 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path5 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path6 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path7 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path8 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path9 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path10 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path11 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path12 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path13 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path14 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path15 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path16 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path17 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path18 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path19 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path20 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path21 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path22 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path23 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path24 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path25 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path26 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path27 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path28 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path29 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path30 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path31 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path32 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path33 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path34 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path35 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path36 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path37 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path38 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path39 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path40 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path41 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path42 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path43 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path44 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path45 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path46 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path47 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path48 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path49 ==
                                                                                                                                                                                                                 false)
                                                                                                                                                                                                            {
                                                                                                                                                                                                                Body
                                                                                                                                                                                                                    .WalkTo(
                                                                                                                                                                                                                        point49,
                                                                                                                                                                                                                        100);
                                                                                                                                                                                                            }
                                                                                                                                                                                                            else
                                                                                                                                                                                                            {
                                                                                                                                                                                                                path49 =
                                                                                                                                                                                                                    true;
                                                                                                                                                                                                                if
                                                                                                                                                                                                                    (!
                                                                                                                                                                                                                         Body
                                                                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                                                                 point50,
                                                                                                                                                                                                                                 30) &&
                                                                                                                                                                                                                     path1 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path2 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path3 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path4 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path5 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path6 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path7 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path8 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path9 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path10 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path11 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path12 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path13 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path14 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path15 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path16 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path17 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path18 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path19 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path20 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path21 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path22 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path23 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path24 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path25 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path26 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path27 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path28 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path29 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path30 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path31 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path32 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path33 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path34 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path35 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path36 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path37 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path38 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path39 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path40 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path41 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path42 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path43 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path44 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path45 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path46 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path47 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path48 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path49 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path50 ==
                                                                                                                                                                                                                     false)
                                                                                                                                                                                                                {
                                                                                                                                                                                                                    Body
                                                                                                                                                                                                                        .WalkTo(
                                                                                                                                                                                                                            point50,
                                                                                                                                                                                                                            100);
                                                                                                                                                                                                                }
                                                                                                                                                                                                                else
                                                                                                                                                                                                                {
                                                                                                                                                                                                                    path50 =
                                                                                                                                                                                                                        true;
                                                                                                                                                                                                                    if
                                                                                                                                                                                                                        (!
                                                                                                                                                                                                                             Body
                                                                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                                                                     point51,
                                                                                                                                                                                                                                     30) &&
                                                                                                                                                                                                                         path1 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path2 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path3 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path4 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path5 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path6 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path7 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path8 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path9 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path10 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path11 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path12 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path13 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path14 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path15 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path16 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path17 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path18 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path19 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path20 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path21 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path22 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path23 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path24 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path25 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path26 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path27 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path28 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path29 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path30 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path31 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path32 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path33 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path34 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path35 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path36 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path37 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path38 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path39 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path40 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path41 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path42 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path43 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path44 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path45 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path46 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path47 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path48 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path49 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path50 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path51 ==
                                                                                                                                                                                                                         false)
                                                                                                                                                                                                                    {
                                                                                                                                                                                                                        Body
                                                                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                                                                point51,
                                                                                                                                                                                                                                100);
                                                                                                                                                                                                                    }
                                                                                                                                                                                                                    else
                                                                                                                                                                                                                    {
                                                                                                                                                                                                                        path51 =
                                                                                                                                                                                                                            true;
                                                                                                                                                                                                                        if
                                                                                                                                                                                                                            (!
                                                                                                                                                                                                                                 Body
                                                                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                                                                         spawn,
                                                                                                                                                                                                                                         30) &&
                                                                                                                                                                                                                             path1 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path2 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path3 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path4 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path5 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path6 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path7 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path8 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path9 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path10 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path11 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path12 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path13 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path14 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path15 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path16 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path17 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path18 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path19 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path20 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path21 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path22 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path23 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path24 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path25 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path26 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path27 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path28 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path29 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path30 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path31 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path32 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path33 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path34 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path35 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path36 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path37 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path38 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path39 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path40 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path41 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path42 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path43 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path44 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path45 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path46 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path47 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path48 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path49 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path50 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path51 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             walkback ==
                                                                                                                                                                                                                             false)
                                                                                                                                                                                                                        {
                                                                                                                                                                                                                            Body
                                                                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                                                                    spawn,
                                                                                                                                                                                                                                    100);
                                                                                                                                                                                                                        }
                                                                                                                                                                                                                        else
                                                                                                                                                                                                                        {
                                                                                                                                                                                                                            walkback =
                                                                                                                                                                                                                                true;
                                                                                                                                                                                                                            path1 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path11 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path21 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path31 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path41 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path51 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path2 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path12 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path22 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path32 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path42 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path3 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path13 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path23 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path33 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path43 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path4 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path14 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path24 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path34 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path44 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path5 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path15 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path25 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path35 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path45 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path6 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path16 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path26 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path36 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path46 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path7 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path17 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path27 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path37 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path47 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path8 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path18 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path28 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path38 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path48 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path9 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path19 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path29 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path39 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path49 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path10 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path20 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path30 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path40 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                            path50 =
                                                                                                                                                                                                                                false;
                                                                                                                                                                                                                        }
                                                                                                                                                                                                                    }
                                                                                                                                                                                                                }
                                                                                                                                                                                                            }
                                                                                                                                                                                                        }
                                                                                                                                                                                                    }
                                                                                                                                                                                                }
                                                                                                                                                                                            }
                                                                                                                                                                                        }
                                                                                                                                                                                    }
                                                                                                                                                                                }
                                                                                                                                                                            }
                                                                                                                                                                        }
                                                                                                                                                                    }
                                                                                                                                                                }
                                                                                                                                                            }
                                                                                                                                                        }
                                                                                                                                                    }
                                                                                                                                                }
                                                                                                                                            }
                                                                                                                                        }
                                                                                                                                    }
                                                                                                                                }
                                                                                                                            }
                                                                                                                        }
                                                                                                                    }
                                                                                                                }
                                                                                                            }
                                                                                                        }
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            #endregion
        }
    }
    #region Set Baf Mob stats
    public void SetMobstats()
    {
        if (Body.TargetObject != null && (Body.InCombat || HasAggro || Body.attackComponent.AttackState == true)) //if in combat
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.PackageID == "HostBaf" && npc.NPCTemplate != null)
                    {
                        if (BafMobs == true && npc.TargetObject == Body.TargetObject)
                        {
                            npc.MaxDistance = 10000; //set mob distance to make it reach target
                            npc.TetherRange = 10000; //set tether to not return to home
                            if (!npc.IsWithinRadius(Body.TargetObject, 100))
                            {
                                npc.MaxSpeedBase = 300; //speed is is not near to reach target faster
                            }
                            else
                                npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
                        }
                    }
                }
            }
        }
        else //if not in combat
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.PackageID == "HostBaf" && npc.NPCTemplate != null)
                    {
                        if (BafMobs == false)
                        {
                            npc.MaxDistance = npc.NPCTemplate.MaxDistance; //return distance to normal
                            npc.TetherRange = npc.NPCTemplate.TetherRange; //return tether to normal
                            npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
                        }
                    }
                }
            }
        }
    }
    #endregion

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            BafHost = false;
            BafMobs = false;
            Body.Health = Body.MaxHealth;
        }
        if (Body.IsMoving)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1 && !AggroTable.ContainsKey(player))
                    {
                        AddToAggroList(player, 10);
                    }
                }
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            if (BafHost == false)//baf all copies to pulled host
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is HostBrain && Body.PackageID != npc.PackageID)
                        {
                            GameLiving target = Body.TargetObject as GameLiving;
                            HostBrain brain = (HostBrain)npc.Brain;
                            if (target != null)
                            {
                                brain.AddToAggroList(target, 10);
                                npc.StartAttack(target);
                            }
                            BafHost = true;
                        }
                    }
                }
            }
            if (BafMobs == false)//baf linked mobs to boss
            {
                foreach (GameNpc npc2 in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc2 != null)
                    {
                        if (npc2.IsAlive && npc2.PackageID == "HostBaf")
                        {
                            AddAggroListTo(npc2.Brain as StandardMobBrain);
                            BafMobs = true;
                        }
                    }
                }
            }
        }
        else
        {
            HostPath();
        }
        base.Think();
    }
}
#endregion Host