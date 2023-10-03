using System;
using DOL.Events;
using DOL.GS.Keeps;

namespace DOL.GS.GameEvents
{
    public class RelicGuardManager
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Albion

        private static GuardFighterRK BenoCE = null;
        private static GuardFighterRK BenoCM = null;
        private static GuardFighterRK SursCE = null;
        private static GuardFighterRK SursCM = null;
        private static GuardFighterRK ErasCE = null;
        private static GuardFighterRK ErasCM = null;
        private static GuardFighterRK BercCE = null;
        private static GuardFighterRK BercCM = null;
        private static GuardFighterRK BoldCE = null;
        private static GuardFighterRK BoldCM = null;
        private static GuardFighterRK RenaCE = null;
        private static GuardFighterRK HurbuCM = null;

        #endregion Albion

        #region Midgard

        private static GuardFighterRK BledGF = null;
        private static GuardFighterRK BledMF = null;
        private static GuardFighterRK NottGF = null;
        private static GuardFighterRK NottMF = null;
        private static GuardFighterRK BlendGF = null;
        private static GuardFighterRK BlendMF = null;
        private static GuardFighterRK GlenGF = null;
        private static GuardFighterRK GlenMF = null;
        private static GuardFighterRK HlidGF = null;
        private static GuardFighterRK HlidMF = null;
        private static GuardFighterRK ArvaGF = null;
        private static GuardFighterRK FensMF = null;

        #endregion Midgard

        #region Hibernia

        private static GuardFighterRK CrauDL = null;
        private static GuardFighterRK CrauDD = null;
        private static GuardFighterRK AiliDL = null;
        private static GuardFighterRK ScatDL = null;
        private static GuardFighterRK BehnDL = null;
        private static GuardFighterRK BehnDD = null;
        private static GuardFighterRK BolgDL = null;
        private static GuardFighterRK BolgDD = null;
        private static GuardFighterRK CrimDL = null;
        private static GuardFighterRK CrimDD = null;
        private static GuardFighterRK nGedDL = null;
        private static GuardFighterRK nGedDD = null;

        #endregion Hibernia

        [GameServerStartedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs arguments)
        {
            InitGuards();
            Init();

            GameEventMgr.AddHandler(KeepEvent.KeepTaken, new DOLEventHandler(Notify));
            GameEventMgr.AddHandler(RelicPadEvent.RelicMounted, new DOLEventHandler(NotifyRelic));
            GameEventMgr.AddHandler(RelicPadEvent.RelicStolen, new DOLEventHandler(NotifyRelic));
            log.Info("Relic Guards System initialized");
        }

        public static void InitGuards()
        {
            RenaCE = new GuardFighterRK();
            RenaCE.X = 601801;
            RenaCE.Y = 428234;
            RenaCE.Z = 5645;
            RenaCE.Heading = 2055;
            RenaCE.CurrentRegionID = 1;
            RenaCE.Name = "Renaris Knight";
            RenaCE.TranslationId = "Chevalier.de.Caer.Renaris";
            foreach (AbstractArea area in RenaCE.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    RenaCE.Component = new GameKeepComponent();
                    RenaCE.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(RenaCE);

            HurbuCM = new GuardFighterRK();
            HurbuCM.X = 508047;
            HurbuCM.Y = 310852;
            HurbuCM.Z = 6832;
            HurbuCM.Heading = 3638;
            HurbuCM.CurrentRegionID = 1;
            HurbuCM.Name = "Hurbury Knight";
            HurbuCM.TranslationId = "Chevalier.de.Caer.Hurbury";
            foreach (AbstractArea area in HurbuCM.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    HurbuCM.Component = new GameKeepComponent();
                    HurbuCM.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(HurbuCM);

            BercCE = new GuardFighterRK();
            BercCE.X = 602200;
            BercCE.Y = 428233;
            BercCE.Z = 5539;
            BercCE.Heading = 2035;
            BercCE.CurrentRegionID = 1;
            BercCE.Name = "Berckstead Knight";
            BercCE.TranslationId = "Chevalier.de.Caer.Berckstead";
            foreach (AbstractArea area in BercCE.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BercCE.Component = new GameKeepComponent();
                    BercCE.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BercCE);

            BercCM = new GuardFighterRK();
            BercCM.X = 508642;
            BercCM.Y = 310348;
            BercCM.Z = 6832;
            BercCM.Heading = 3638;
            BercCM.CurrentRegionID = 1;
            BercCM.Name = "Berckstead Knight";
            BercCM.TranslationId = "Chevalier.de.Caer.Berckstead";
            foreach (AbstractArea area in BercCM.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BercCM.Component = new GameKeepComponent();
                    BercCM.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BercCM);

            SursCE = new GuardFighterRK();
            SursCE.X = 601527;
            SursCE.Y = 428233;
            SursCE.Z = 5628;
            SursCE.Heading = 2062;
            SursCE.CurrentRegionID = 1;
            SursCE.Name = "Sursbrooke Knight";
            SursCE.TranslationId = "Chevalier.de.Caer.Sursbrooke";
            foreach (AbstractArea area in SursCE.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    SursCE.Component = new GameKeepComponent();
                    SursCE.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(SursCE);

            SursCM = new GuardFighterRK();
            SursCM.X = 507894;
            SursCM.Y = 310323;
            SursCM.Z = 6832;
            SursCM.Heading = 3638;
            SursCM.CurrentRegionID = 1;
            SursCM.Name = "Sursbrooke Knight";
            SursCM.TranslationId = "Chevalier.de.Caer.Sursbrooke";
            foreach (AbstractArea area in SursCM.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    SursCM.Component = new GameKeepComponent();
                    SursCM.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(SursCM);

            BoldCE = new GuardFighterRK();
            BoldCE.X = 601769;
            BoldCE.Y = 427991;
            BoldCE.Z = 5544;
            BoldCE.Heading = 2055;
            BoldCE.CurrentRegionID = 1;
            BoldCE.Name = "Boldiam Knight";
            BoldCE.TranslationId = "Chevalier.de.Caer.Boldiam";
            foreach (AbstractArea area in BoldCE.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BoldCE.Component = new GameKeepComponent();
                    BoldCE.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BoldCE);

            BoldCM = new GuardFighterRK();
            BoldCM.X = 508354;
            BoldCM.Y = 310593;
            BoldCM.Z = 6832;
            BoldCM.Heading = 3638;
            BoldCM.CurrentRegionID = 1;
            BoldCM.Name = "Boldiam Knight";
            BoldCM.TranslationId = "Chevalier.de.Caer.Boldiam";
            foreach (AbstractArea area in BoldCM.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BoldCM.Component = new GameKeepComponent();
                    BoldCM.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BoldCM);

            ErasCE = new GuardFighterRK();
            ErasCE.X = 601096;
            ErasCE.Y = 428255;
            ErasCE.Z = 5606;
            ErasCE.Heading = 2035;
            ErasCE.CurrentRegionID = 1;
            ErasCE.Name = "Erasleigh Knight";
            ErasCE.TranslationId = "Chevalier.de.Caer.Erasleigh";
            foreach (AbstractArea area in ErasCE.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    ErasCE.Component = new GameKeepComponent();
                    ErasCE.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(ErasCE);

            ErasCM = new GuardFighterRK();
            ErasCM.X = 507705;
            ErasCM.Y = 311142;
            ErasCM.Z = 6832;
            ErasCM.Heading = 3638;
            ErasCM.CurrentRegionID = 1;
            ErasCM.Name = "Erasleigh Knight";
            ErasCM.TranslationId = "Chevalier.de.Caer.Erasleigh";
            foreach (AbstractArea area in ErasCM.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    ErasCM.Component = new GameKeepComponent();
                    ErasCM.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(ErasCM);

            BenoCE = new GuardFighterRK();
            BenoCE.X = 601595;
            BenoCE.Y = 427989;
            BenoCE.Z = 5547;
            BenoCE.Heading = 2055;
            BenoCE.CurrentRegionID = 1;
            BenoCE.Name = "Benowyc Knight";
            BenoCE.TranslationId = "Chevalier.de.Caer.Benowyc";
            foreach (AbstractArea area in BenoCE.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BenoCE.Component = new GameKeepComponent();
                    BenoCE.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BenoCE);

            BenoCM = new GuardFighterRK();
            BenoCM.X = 508253;
            BenoCM.Y = 310751;
            BenoCM.Z = 6832;
            BenoCM.Heading = 3640;
            BenoCM.CurrentRegionID = 1;
            BenoCM.Name = "Benowyc Knight";
            BenoCM.TranslationId = "Chevalier.de.Caer.Benowyc";
            foreach (AbstractArea area in BenoCM.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BenoCM.Component = new GameKeepComponent();
                    BenoCM.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BenoCM);

            FensMF = new GuardFighterRK();
            FensMF.X = 771263;
            FensMF.Y = 628729;
            FensMF.Z = 6992;
            FensMF.Heading = 3989;
            FensMF.CurrentRegionID = 100;
            FensMF.Name = "Fensalir Jarl";
            FensMF.TranslationId = "Jarl.de.Fensalir.Faste";
            foreach (AbstractArea area in FensMF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    FensMF.Component = new GameKeepComponent();
                    FensMF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(FensMF);

            ArvaGF = new GuardFighterRK();
            ArvaGF.X = 678770;
            ArvaGF.Y = 710252;
            ArvaGF.Z = 6912;
            ArvaGF.Heading = 2963;
            ArvaGF.CurrentRegionID = 100;
            ArvaGF.Name = "Arvakr Jarl";
            ArvaGF.TranslationId = "Jarl.de.Arvakr.Faste";
            foreach (AbstractArea area in ArvaGF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    ArvaGF.Component = new GameKeepComponent();
                    ArvaGF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(ArvaGF);

            GlenGF = new GuardFighterRK();
            GlenGF.X = 679248;
            GlenGF.Y = 710497;
            GlenGF.Z = 6836;
            GlenGF.Heading = 2950;
            GlenGF.CurrentRegionID = 100;
            GlenGF.Name = "Glenlock Jarl";
            GlenGF.TranslationId = "Jarl.de.Glenlock.Faste";
            foreach (AbstractArea area in GlenGF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    GlenGF.Component = new GameKeepComponent();
                    GlenGF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(GlenGF);

            GlenMF = new GuardFighterRK();
            GlenMF.X = 769816;
            GlenMF.Y = 627702;
            GlenMF.Z = 6992;
            GlenMF.Heading = 923;
            GlenMF.CurrentRegionID = 100;
            GlenMF.Name = "Glenlock Jarl";
            GlenMF.TranslationId = "Jarl.de.Glenlock.Faste";
            foreach (AbstractArea area in GlenMF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    GlenMF.Component = new GameKeepComponent();
                    GlenMF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(GlenMF);

            BlendGF = new GuardFighterRK();
            BlendGF.X = 679164;
            BlendGF.Y = 711149;
            BlendGF.Z = 6868;
            BlendGF.Heading = 3418;
            BlendGF.CurrentRegionID = 100;
            BlendGF.Name = "Blendrake Jarl";
            BlendGF.TranslationId = "Jarl.de.Blendrake.Faste";
            foreach (AbstractArea area in BlendGF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BlendGF.Component = new GameKeepComponent();
                    BlendGF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BlendGF);

            BlendMF = new GuardFighterRK();
            BlendMF.X = 770036;
            BlendMF.Y = 628191;
            BlendMF.Z = 6941;
            BlendMF.Heading = 503;
            BlendMF.CurrentRegionID = 100;
            BlendMF.Name = "Blendrake Jarl";
            BlendMF.TranslationId = "Jarl.de.Blendrake.Faste";
            foreach (AbstractArea area in BlendMF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BlendMF.Component = new GameKeepComponent();
                    BlendMF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BlendMF);

            HlidGF = new GuardFighterRK();
            HlidGF.X = 679128;
            HlidGF.Y = 709873;
            HlidGF.Z = 6787;
            HlidGF.Heading = 2950;
            HlidGF.CurrentRegionID = 100;
            HlidGF.Name = "Hlidskialf Jarl";
            HlidGF.TranslationId = "Jarl.de.Hlidskialf.Faste";
            foreach (AbstractArea area in HlidGF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    HlidGF.Component = new GameKeepComponent();
                    HlidGF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(HlidGF);

            HlidMF = new GuardFighterRK();
            HlidMF.X = 770615;
            HlidMF.Y = 628037;
            HlidMF.Z = 6992;
            HlidMF.Heading = 521;
            HlidMF.CurrentRegionID = 100;
            HlidMF.Name = "Hlidskialf Jarl";
            HlidMF.TranslationId = "Jarl.de.Hlidskialf.Faste";
            foreach (AbstractArea area in HlidMF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    HlidMF.Component = new GameKeepComponent();
                    HlidMF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(HlidMF);

            NottGF = new GuardFighterRK();
            NottGF.X = 678654;
            NottGF.Y = 709122;
            NottGF.Z = 6912;
            NottGF.Heading = 2511;
            NottGF.CurrentRegionID = 100;
            NottGF.Name = "Nottmoor Jarl";
            NottGF.TranslationId = "Jarl.de.Nottmoor.Faste";
            foreach (AbstractArea area in NottGF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    NottGF.Component = new GameKeepComponent();
                    NottGF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(NottGF);
            NottMF = new GuardFighterRK();
            NottMF.X = 770531;
            NottMF.Y = 628482;
            NottMF.Z = 6985;
            NottMF.Heading = 503;
            NottMF.CurrentRegionID = 100;
            NottMF.Name = "Nottmoor Jarl";
            NottMF.TranslationId = "Jarl.de.Nottmoor.Faste";
            foreach (AbstractArea area in NottMF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    NottMF.Component = new GameKeepComponent();
                    NottMF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(NottMF);

            BledGF = new GuardFighterRK();
            BledGF.X = 679121;
            BledGF.Y = 710193;
            BledGF.Z = 6873;
            BledGF.Heading = 2965;
            BledGF.CurrentRegionID = 100;
            BledGF.Name = "Bledmeer Jarl";
            BledGF.TranslationId = "Jarl.de.Bledmeer.Faste";
            foreach (AbstractArea area in BledGF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BledGF.Component = new GameKeepComponent();
                    BledGF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BledGF);

            BledMF = new GuardFighterRK();
            BledMF.X = 770308;
            BledMF.Y = 628344;
            BledMF.Z = 6969;
            BledMF.Heading = 513;
            BledMF.CurrentRegionID = 100;
            BledMF.Name = "Bledmeer Jarl";
            BledMF.TranslationId = "Jarl.de.Bledmeer.Faste";
            foreach (AbstractArea area in BledMF.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BledMF.Component = new GameKeepComponent();
                    BledMF.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BledMF);

            AiliDL = new GuardFighterRK();
            AiliDL.X = 348542;
            AiliDL.Y = 371526;
            AiliDL.Z = 4880;
            AiliDL.Heading = 2052;
            AiliDL.CurrentRegionID = 200;
            AiliDL.Name = "Ailinne Sentinel";
            AiliDL.TranslationId = "Sentinelle.de.Dun.Ailinne";
            foreach (AbstractArea area in AiliDL.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    AiliDL.Component = new GameKeepComponent();
                    AiliDL.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(AiliDL);

            ScatDL = new GuardFighterRK();
            ScatDL.X = 401953;
            ScatDL.Y = 464351;
            ScatDL.Z = 2888;
            ScatDL.Heading = 2567;
            ScatDL.CurrentRegionID = 200;
            ScatDL.Name = "Scathaig Sentinel";
            ScatDL.TranslationId = "Sentinelle.de.Dun.Scathaig";
            foreach (AbstractArea area in ScatDL.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    ScatDL.Component = new GameKeepComponent();
                    ScatDL.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(ScatDL);

            nGedDL = new GuardFighterRK();
            nGedDL.X = 348211;
            nGedDL.Y = 370945;
            nGedDL.Z = 4784;
            nGedDL.Heading = 2212;
            nGedDL.CurrentRegionID = 200;
            nGedDL.Name = "nGed Sentinel";
            nGedDL.TranslationId = "Sentinelle.de.Dun.nGed";
            foreach (AbstractArea area in nGedDL.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    nGedDL.Component = new GameKeepComponent();
                    nGedDL.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(nGedDL);

            nGedDD = new GuardFighterRK();
            nGedDD.X = 402193;
            nGedDD.Y = 463725;
            nGedDD.Z = 2854;
            nGedDD.Heading = 2565;
            nGedDD.CurrentRegionID = 200;
            nGedDD.Name = "nGed Sentinel";
            nGedDD.TranslationId = "Sentinelle.de.Dun.nGed";
            foreach (AbstractArea area in nGedDD.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    nGedDD.Component = new GameKeepComponent();
                    nGedDD.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(nGedDD);

            BolgDL = new GuardFighterRK();
            BolgDL.X = 347374;
            BolgDL.Y = 371175;
            BolgDL.Z = 4809;
            BolgDL.Heading = 1690;
            BolgDL.CurrentRegionID = 200;
            BolgDL.Name = "Bolg Sentinel";
            BolgDL.TranslationId = "Sentinelle.de.Dun.Bolg";
            foreach (AbstractArea area in BolgDL.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BolgDL.Component = new GameKeepComponent();
                    BolgDL.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BolgDL);

            BolgDD = new GuardFighterRK();
            BolgDD.X = 401172;
            BolgDD.Y = 463380;
            BolgDD.Z = 2884;
            BolgDD.Heading = 2239;
            BolgDD.CurrentRegionID = 200;
            BolgDD.Name = "Bolg Sentinel";
            BolgDD.TranslationId = "Sentinelle.de.Dun.Bolg";
            foreach (AbstractArea area in BolgDD.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BolgDD.Component = new GameKeepComponent();
                    BolgDD.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BolgDD);

            BehnDL = new GuardFighterRK();
            BehnDL.X = 348841;
            BehnDL.Y = 371000;
            BehnDL.Z = 4688;
            BehnDL.Heading = 2012;
            BehnDL.CurrentRegionID = 200;
            BehnDL.Name = "Behnn Sentinel";
            BehnDL.TranslationId = "Sentinelle.de.Dun.Behnn";
            foreach (AbstractArea area in BehnDL.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BehnDL.Component = new GameKeepComponent();
                    BehnDL.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BehnDL);

            BehnDD = new GuardFighterRK();
            BehnDD.X = 402899;
            BehnDD.Y = 465176;
            BehnDD.Z = 2848;
            BehnDD.Heading = 2978;
            BehnDD.CurrentRegionID = 200;
            BehnDD.Name = "Behnn Sentinel";
            BehnDD.TranslationId = "Sentinelle.de.Dun.Behnn";
            foreach (AbstractArea area in BehnDD.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    BehnDD.Component = new GameKeepComponent();
                    BehnDD.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(BehnDD);

            CrimDL = new GuardFighterRK();
            CrimDL.X = 349191;
            CrimDL.Y = 371458;
            CrimDL.Z = 4877;
            CrimDL.Heading = 2253;
            CrimDL.CurrentRegionID = 200;
            CrimDL.Name = "Crimthain Sentinel";
            CrimDL.TranslationId = "Sentinelle.de.Dun.Crimthain";
            foreach (AbstractArea area in CrimDL.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    CrimDL.Component = new GameKeepComponent();
                    CrimDL.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(CrimDL);

            CrimDD = new GuardFighterRK();
            CrimDD.X = 402337;
            CrimDD.Y = 463993;
            CrimDD.Z = 2862;
            CrimDD.Heading = 2565;
            CrimDD.CurrentRegionID = 200;
            CrimDD.Name = "Crimthain Sentinel";
            CrimDD.TranslationId = "Sentinelle.de.Dun.Crimthain";
            foreach (AbstractArea area in CrimDD.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    CrimDD.Component = new GameKeepComponent();
                    CrimDD.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(CrimDD);

            CrauDL = new GuardFighterRK();
            CrauDL.X = 348634;
            CrauDL.Y = 371189;
            CrauDL.Z = 4825;
            CrauDL.Heading = 2057;
            CrauDL.CurrentRegionID = 200;
            CrauDL.Name = "Crauchon Sentinel";
            CrauDL.TranslationId = "Sentinelle.de.Dun.Crauchon";
            foreach (AbstractArea area in CrauDL.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    CrauDL.Component = new GameKeepComponent();
                    CrauDL.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(CrauDL);

            CrauDD = new GuardFighterRK();
            CrauDD.X = 402670;
            CrauDD.Y = 464400;
            CrauDD.Z = 2888;
            CrauDD.Heading = 2621;
            CrauDD.CurrentRegionID = 200;
            CrauDD.Name = "Crauchon Sentinel";
            CrauDD.TranslationId = "Sentinelle.de.Dun.Crauchon";
            foreach (AbstractArea area in CrauDD.CurrentAreas)
            {
                if (area is KeepArea)
                {
                    AbstractGameKeep popkeep = (area as KeepArea).Keep;
                    CrauDD.Component = new GameKeepComponent();
                    CrauDD.Component.Keep = popkeep;
                    break;
                }
            }

            GuardTemplateMgr.RefreshTemplate(CrauDD);
        }

        private static int albAddedGuardsCount = 0;
        private static int midAddedGuardsCount = 0;
        private static int hibAddedGuardsCount = 0;
        private static bool firstrun = true;

        public static void Init()
        {
            int AlbrelicCount = 0;
            int MidrelicCount = 0;
            int HibrelicCount = 0;
            int Albguardpercentrelic = 100;
            int Midguardpercentrelic = 100;
            int Hibguardpercentrelic = 100;
            int albMaxGuards = 12;
            int midMaxGuards = 12;
            int hibMaxGuards = 12;
            foreach (GameRelic relic in RelicMgr.getNFRelics())
            {
                if (relic == null)
                    break;
                if (relic.Realm == eRealm.Albion)
                    AlbrelicCount++;
                if (relic.Realm == eRealm.Midgard)
                    MidrelicCount++;
                if (relic.Realm == eRealm.Hibernia)
                    HibrelicCount++;
            }

            if (AlbrelicCount > 2)
            {
                for (int i = 2; i < AlbrelicCount; i++)
                    Albguardpercentrelic -= 25;
            }

            if (MidrelicCount > 2)
            {
                for (int i = 2; i < MidrelicCount; i++)
                    Midguardpercentrelic -= 25;
            }

            if (HibrelicCount > 2)
            {
                for (int i = 2; i < HibrelicCount; i++)
                    Hibguardpercentrelic -= 25;
            }

            log.Debug("Albguardpercentrelic= " + Albguardpercentrelic + " Midguardpercentrelic= " +
                      Midguardpercentrelic + " Hibguardpercentrelic= " + Hibguardpercentrelic);
            albMaxGuards = albMaxGuards * Albguardpercentrelic / 100;
            midMaxGuards = midMaxGuards * Midguardpercentrelic / 100;
            hibMaxGuards = hibMaxGuards * Hibguardpercentrelic / 100;
            log.Debug("albMaxGuards= " + albMaxGuards);
            log.Debug("midMaxGuards= " + midMaxGuards);
            log.Debug("hibMaxGuards= " + hibMaxGuards);
            //int Albguardpercentkeep = 0;
            AbstractGameKeep keep = null;
            //foreach (AbstractGameKeep keep in GameServer.KeepManager.GetFrontierKeeps())
            //{

            #region Albion

            #region Renaris

            keep = GameServer.KeepManager.GetKeepByID(56);
            if (keep != null)
            {
                #region Renaris Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Renaris Knight", eRealm.Albion);

                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            RenaCE.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("1 Renaris Knight Excalibur Added");
                        }
                    }
                    else
                    {
                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (RenaCE.IsAlive && RenaCE.ObjectState == GameObject.eObjectState.Active ||
                                RenaCE.IsRespawning)
                            {
                                if (albAddedGuardsCount > albMaxGuards)
                                {
                                    if (RenaCE.IsRespawning)
                                        RenaCE.StopRespawn();
                                    else
                                        RenaCE.Delete();
                                    log.Info("2 Renaris Knight Excalibur Removed");
                                    albAddedGuardsCount--;
                                }
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!RenaCE.IsRespawning && RenaCE.ObjectState != GameObject.eObjectState.Active)
                            {
                                RenaCE.AddToWorld();
                                log.Info("3 Renaris Knight Excalibur Added");
                                albAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (RenaCE.IsAlive && RenaCE.ObjectState == GameObject.eObjectState.Active ||
                            RenaCE.IsRespawning)
                        {
                            if (RenaCE.IsRespawning)
                                RenaCE.StopRespawn();
                            else
                                RenaCE.Delete();
                            log.Info("4 Renaris Knight Excalibur Removed");
                            albAddedGuardsCount--;
                        }
                    }
                }

                #endregion Renaris Add
            }

            #endregion Renaris

            #region Hurbury

            keep = GameServer.KeepManager.GetKeepByID(55);
            if (keep != null)
            {
                #region Hurbury Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Hurbury Knight", eRealm.Albion);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            HurbuCM.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("5 Hurbury Knight Excalibur Added");
                        }
                    }
                    else
                    {
                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (HurbuCM.IsAlive && HurbuCM.ObjectState == GameObject.eObjectState.Active ||
                                HurbuCM.IsRespawning)
                            {
                                if (HurbuCM.IsRespawning)
                                    HurbuCM.StopRespawn();

                                HurbuCM.Delete();
                                log.Info("6 Hurbury Knight Myrddin Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!HurbuCM.IsRespawning && HurbuCM.ObjectState != GameObject.eObjectState.Active)
                            {
                                HurbuCM.AddToWorld();
                                log.Info("7 Hurbury Knight Excalibur Added");
                                albAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (HurbuCM.IsAlive && HurbuCM.ObjectState == GameObject.eObjectState.Active ||
                            HurbuCM.IsRespawning)
                        {
                            if (HurbuCM.IsRespawning)
                                HurbuCM.StopRespawn();

                            HurbuCM.Delete();
                            log.Info("8 Hurbury Knight Myrddin Removed");
                            albAddedGuardsCount--;
                        }
                    }
                }

                #endregion Hurbury Add
            }

            #endregion Hurbury

            #region Berckstead

            keep = GameServer.KeepManager.GetKeepByID(51);
            if (keep != null)
            {
                #region Berckstead Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Berckstead Knight", eRealm.Albion);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            BercCE.AddToWorld();
                            albAddedGuardsCount++;

                            log.Info("9 Berckstead Knight Excalibur Added");
                        }

                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            BercCM.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("10 Berckstead Knight Myrddin Added");
                        }
                    }
                    else
                    {
                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (BercCE.IsAlive && BercCE.ObjectState == GameObject.eObjectState.Active ||
                                BercCE.IsRespawning)
                            {
                                if (BercCE.IsRespawning)
                                    BercCE.StopRespawn();

                                BercCE.Delete();
                                log.Info("11 Berckstead Knight Excalibur Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!BercCE.IsRespawning && BercCE.ObjectState != GameObject.eObjectState.Active)
                            {
                                BercCE.AddToWorld();
                                log.Info("12 Berckstead Knight Excalibur Added");
                                albAddedGuardsCount++;
                            }

                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (BercCM.IsAlive && BercCM.ObjectState == GameObject.eObjectState.Active ||
                                BercCM.IsRespawning)
                            {
                                if (BercCM.IsRespawning)
                                    BercCM.StopRespawn();

                                BercCM.Delete();
                                log.Info("13 Berckstead Knight Myrddin Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!BercCM.IsRespawning && BercCM.ObjectState != GameObject.eObjectState.Active)
                            {
                                BercCM.AddToWorld();
                                log.Info("14 Berckstead Knight Myrddin Added");
                                albAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (BercCE.IsAlive && BercCE.ObjectState == GameObject.eObjectState.Active ||
                            BercCE.IsRespawning)
                        {
                            if (BercCE.IsRespawning)
                                BercCE.StopRespawn();

                            BercCE.Delete();
                            log.Info("15 Berckstead Knight Excalibur Removed");
                            albAddedGuardsCount--;
                        }

                        if (BercCM.IsAlive && BercCM.ObjectState == GameObject.eObjectState.Active ||
                            BercCM.IsRespawning)
                        {
                            if (BercCM.IsRespawning)
                                BercCM.StopRespawn();

                            BercCM.Delete();
                            log.Info("16 Berckstead Knight Myrddin Removed");
                            albAddedGuardsCount--;
                        }
                    }
                }

                #endregion Berckstead Add
            }

            #endregion Berckstead

            #region Sursbrook

            keep = GameServer.KeepManager.GetKeepByID(54);
            if (keep != null)
            {
                #region Sursbrook Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Sursbrooke Knight", eRealm.Albion);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            SursCE.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("17 Sursbrooke Knight Excalibur Added");
                        }

                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            SursCM.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("18 Sursbrooke Knight Myrddin Added");
                        }
                    }
                    else
                    {
                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (SursCE.IsAlive && SursCE.ObjectState == GameObject.eObjectState.Active ||
                                SursCE.IsRespawning)
                            {
                                if (SursCE.IsRespawning)
                                    SursCE.StopRespawn();

                                SursCE.Delete();
                                log.Info("19 Sursbrooke Knight Excalibur Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!SursCE.IsRespawning && SursCE.ObjectState != GameObject.eObjectState.Active)
                            {
                                SursCE.AddToWorld();
                                log.Info("20 Sursbrooke Knight Excalibur Added");
                                albAddedGuardsCount++;
                            }

                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (SursCM.IsAlive && SursCM.ObjectState == GameObject.eObjectState.Active ||
                                SursCM.IsRespawning)
                            {
                                if (SursCM.IsRespawning)
                                    SursCM.StopRespawn();

                                SursCM.Delete();
                                log.Info("21 Sursbrooke Knight Myrddin Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!SursCM.IsRespawning && SursCM.ObjectState != GameObject.eObjectState.Active)
                            {
                                SursCM.AddToWorld();
                                log.Info("22 Sursbrooke Knight Myrddin Added");
                                albAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (SursCE.IsAlive && SursCE.ObjectState == GameObject.eObjectState.Active ||
                            SursCE.IsRespawning)
                        {
                            if (SursCE.IsRespawning)
                                SursCE.StopRespawn();

                            SursCE.Delete();
                            log.Info("23 Sursbrooke Knight Excalibur Removed");
                            albAddedGuardsCount--;
                        }

                        if (SursCM.IsAlive && SursCM.ObjectState == GameObject.eObjectState.Active ||
                            SursCM.IsRespawning)
                        {
                            if (SursCM.IsRespawning)
                                SursCM.StopRespawn();

                            SursCM.Delete();
                            log.Info("24 Sursbrooke Knight Myrddin Removed");
                            albAddedGuardsCount--;
                        }
                    }
                }

                #endregion Sursbrook Add
            }

            #endregion Sursbrook

            #region Boldiam

            keep = GameServer.KeepManager.GetKeepByID(53);
            if (keep != null)
            {
                #region Boldiam Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Boldiam Knight", eRealm.Albion);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            BoldCE.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("25 Boldiam Knight Excalibur Added");
                        }

                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            BoldCM.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("26 Boldiam Knight Myrddin Added");
                        }
                    }
                    else
                    {
                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (BoldCE.IsAlive && SursCM.ObjectState == GameObject.eObjectState.Active ||
                                BoldCE.IsRespawning)
                            {
                                if (BoldCE.IsRespawning)
                                    BoldCE.StopRespawn();

                                BoldCE.Delete();
                                log.Info("27 Boldiam Knight Excalibur Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!BoldCE.IsRespawning && BoldCE.ObjectState != GameObject.eObjectState.Active)
                            {
                                BoldCE.AddToWorld();
                                log.Info("28 Boldiam Knight Excalibur Added");
                                albAddedGuardsCount++;
                            }

                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (BoldCM.IsAlive && BoldCM.ObjectState == GameObject.eObjectState.Active ||
                                BoldCM.IsRespawning)
                            {
                                if (BoldCM.IsRespawning)
                                    BoldCM.StopRespawn();

                                BoldCM.Delete();
                                log.Info("29 Boldiam Knight Myrddin Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!BoldCM.IsRespawning && BoldCM.ObjectState != GameObject.eObjectState.Active)
                            {
                                BoldCM.AddToWorld();
                                log.Info("30 Boldiam Knight Myrddin Added");
                                albAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (BoldCE.IsAlive && BoldCE.ObjectState == GameObject.eObjectState.Active ||
                            BoldCE.IsRespawning)
                        {
                            if (BoldCE.IsRespawning)
                                BoldCE.StopRespawn();

                            BoldCE.Delete();
                            log.Info("31 Boldiam Knight Excalibur Removed");
                            albAddedGuardsCount--;
                        }

                        if (BoldCM.IsAlive && BoldCM.ObjectState == GameObject.eObjectState.Active ||
                            BoldCM.IsRespawning)
                        {
                            if (BoldCM.IsRespawning)
                                BoldCM.StopRespawn();

                            BoldCM.Delete();
                            log.Info("32 Boldiam Knight Myrddin Removed");
                            albAddedGuardsCount--;
                        }
                    }
                }

                #endregion Boldiam Add
            }

            #endregion Boldiam

            #region Erasleigh

            keep = GameServer.KeepManager.GetKeepByID(52);
            if (keep != null)
            {
                #region Erasleigh Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Erasleigh Knight", eRealm.Albion);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            ErasCE.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("33 Erasleigh Knight Excalibur Added");
                        }

                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            ErasCM.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("34 Erasleigh Knight Myrddin Added");
                        }
                    }
                    else
                    {
                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (ErasCE.IsAlive && ErasCE.ObjectState == GameObject.eObjectState.Active ||
                                ErasCE.IsRespawning)
                            {
                                if (ErasCE.IsRespawning)
                                    ErasCE.StopRespawn();

                                ErasCE.Delete();
                                log.Info("35 Erasleigh Knight Excalibur Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!ErasCE.IsRespawning && ErasCE.ObjectState != GameObject.eObjectState.Active)
                            {
                                ErasCE.AddToWorld();
                                log.Info("36 Erasleigh Knight Excalibur Added");
                                albAddedGuardsCount++;
                            }

                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (ErasCM.IsAlive && ErasCM.ObjectState == GameObject.eObjectState.Active ||
                                ErasCM.IsRespawning)
                            {
                                if (ErasCM.IsRespawning)
                                    ErasCM.StopRespawn();

                                ErasCM.Delete();
                                log.Info("37 Erasleigh Knight Myrddin Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!ErasCM.IsRespawning && ErasCM.ObjectState != GameObject.eObjectState.Active)
                            {
                                ErasCM.AddToWorld();
                                log.Info("38 Erasleigh Knight Myrddin Added");
                                albAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (ErasCE.IsAlive && ErasCE.ObjectState == GameObject.eObjectState.Active ||
                            ErasCE.IsRespawning)
                        {
                            if (ErasCE.IsRespawning)
                                ErasCE.StopRespawn();

                            ErasCE.Delete();
                            log.Info("39 Erasleigh Knight Excalibur Removed");
                            albAddedGuardsCount--;
                        }

                        if (ErasCM.IsAlive && ErasCM.ObjectState == GameObject.eObjectState.Active ||
                            ErasCM.IsRespawning)
                        {
                            if (ErasCM.IsRespawning)
                                ErasCM.StopRespawn();

                            ErasCM.Delete();
                            log.Info("40 Erasleigh Knight Myrddin Removed");
                            albAddedGuardsCount--;
                        }
                    }
                }

                #endregion Erasleigh Add
            }

            #endregion Erasleigh

            #region Benowyc

            keep = GameServer.KeepManager.GetKeepByID(50);
            if (keep != null)
            {
                #region Benowyc Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Benowyc Knight", eRealm.Albion);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            BenoCE.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("41 Benowyc Knight Excalibur Added");
                        }

                        if (albAddedGuardsCount < albMaxGuards)
                        {
                            BenoCM.AddToWorld();
                            albAddedGuardsCount++;
                            log.Info("42 Benowyc Knight Myrddin Added");
                        }
                    }
                    else
                    {
                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (BenoCE.IsAlive && BenoCE.ObjectState == GameObject.eObjectState.Active ||
                                BenoCE.IsRespawning)
                            {
                                if (BenoCE.IsRespawning)
                                    BenoCE.StopRespawn();

                                BenoCE.Delete();
                                log.Info("43 Benowyc Knight Excalibur Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!BenoCE.IsRespawning && BenoCE.ObjectState != GameObject.eObjectState.Active)
                            {
                                BenoCE.AddToWorld();
                                log.Info("44 Benowyc Knight Excalibur Added");
                                albAddedGuardsCount++;
                            }

                        if (albAddedGuardsCount > albMaxGuards)
                        {
                            if (BenoCM.IsAlive && BenoCM.ObjectState == GameObject.eObjectState.Active ||
                                BenoCM.IsRespawning)
                            {
                                if (BenoCM.IsRespawning)
                                    BenoCM.StopRespawn();

                                BenoCM.Delete();
                                log.Info("45 Benowyc Knight Myrddin Removed");
                                albAddedGuardsCount--;
                            }
                        }
                        else if (albAddedGuardsCount < albMaxGuards)
                            if (!BenoCM.IsRespawning && BenoCM.ObjectState != GameObject.eObjectState.Active)
                            {
                                BenoCM.AddToWorld();
                                log.Info("46 Benowyc Knight Myrddin Added");
                                albAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (BenoCE.IsAlive && BenoCE.ObjectState == GameObject.eObjectState.Active ||
                            BenoCE.IsRespawning)
                        {
                            if (BenoCE.IsRespawning)
                                BenoCE.StopRespawn();

                            BenoCE.Delete();
                            log.Info("47 Benowyc Knight Excalibur Removed");
                            albAddedGuardsCount--;
                        }

                        if (BenoCM.IsAlive && BenoCM.ObjectState == GameObject.eObjectState.Active ||
                            BenoCM.IsRespawning)
                        {
                            if (BenoCM.IsRespawning)
                                BenoCM.StopRespawn();

                            BenoCM.Delete();
                            log.Info("48 Benowyc Knight Myrddin Removed");
                            albAddedGuardsCount--;
                        }
                    }
                }

                #endregion Benowyc Add
            }

            #endregion Benowyc

            #endregion Albion

            #region Midgard

            #region Fensalir

            keep = GameServer.KeepManager.GetKeepByID(80);
            if (keep != null)
            {
                #region Fensalir Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Fensalir Jarl", eRealm.Midgard);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            FensMF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("49 Fensalir Jarl Mjollner Added");
                        }
                    }
                    else
                    {
                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (FensMF.IsAlive && FensMF.ObjectState == GameObject.eObjectState.Active ||
                                FensMF.IsRespawning)
                            {
                                if (FensMF.IsRespawning)
                                    FensMF.StopRespawn();

                                FensMF.Delete();
                                log.Info("50 Fensalir Knight Mjollner Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!FensMF.IsRespawning && FensMF.ObjectState != GameObject.eObjectState.Active)
                            {
                                FensMF.AddToWorld();
                                log.Info("51 Fensalir Jarl Mjollner Added");
                                midAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (FensMF.IsAlive && FensMF.ObjectState == GameObject.eObjectState.Active ||
                            FensMF.IsRespawning)
                        {
                            if (FensMF.IsRespawning)
                                FensMF.StopRespawn();

                            FensMF.Delete();
                            midAddedGuardsCount--;
                            log.Info("52 Fensalir Knight Mjollner Removed");
                        }
                    }
                }

                #endregion Fensalir Add
            }

            #endregion Fensalir

            #region Arvakr

            keep = GameServer.KeepManager.GetKeepByID(81);
            if (keep != null)
            {
                #region Arvakr Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Arvakr Jarl", eRealm.Midgard);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            ArvaGF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("53 Arvakr Jarl Grallahorn Added");
                        }
                    }
                    else
                    {
                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (ArvaGF.IsAlive && FensMF.ObjectState == GameObject.eObjectState.Active ||
                                ArvaGF.IsRespawning)
                            {
                                if (ArvaGF.IsRespawning)
                                    ArvaGF.StopRespawn();

                                ArvaGF.Delete();
                                log.Info("54 Arvakr Knight Grallahorn Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!ArvaGF.IsRespawning && ArvaGF.ObjectState != GameObject.eObjectState.Active)
                            {
                                ArvaGF.AddToWorld();
                                log.Info("55 Arvakr Jarl Grallahorn Added");
                                midAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (ArvaGF.IsAlive && ArvaGF.ObjectState == GameObject.eObjectState.Active ||
                            ArvaGF.IsRespawning)
                        {
                            if (ArvaGF.IsRespawning)
                                ArvaGF.StopRespawn();

                            ArvaGF.Delete();
                            log.Info("56 Arvakr Knight Grallahorn Removed");
                            midAddedGuardsCount--;
                        }
                    }
                }

                #endregion Arvakr Add
            }

            #endregion Arvakr

            #region Glenlock

            keep = GameServer.KeepManager.GetKeepByID(79);
            if (keep != null)
            {
                #region Glenlock Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Glenlock Jarl", eRealm.Midgard);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            GlenGF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("57 Glenlock Jarl Grallahorn Added");
                        }

                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            GlenMF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("58 Glenlock Jarl Mjollner Added");
                        }
                    }
                    else
                    {
                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (GlenGF.IsAlive && GlenGF.ObjectState == GameObject.eObjectState.Active ||
                                GlenGF.IsRespawning)
                            {
                                if (GlenGF.IsRespawning)
                                    GlenGF.StopRespawn();

                                GlenGF.Delete();
                                log.Info("59 Glenlock Knight Grallahorn Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!GlenGF.IsRespawning && GlenGF.ObjectState != GameObject.eObjectState.Active)
                            {
                                GlenGF.AddToWorld();
                                log.Info("60 Glenlock Jarl Grallahorn Added");
                                midAddedGuardsCount++;
                            }

                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (GlenMF.IsAlive && GlenMF.ObjectState == GameObject.eObjectState.Active ||
                                GlenMF.IsRespawning)
                            {
                                if (GlenMF.IsRespawning)
                                    GlenMF.StopRespawn();

                                GlenMF.Delete();
                                log.Info("61 Glenlock Knight Mjollner Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!GlenMF.IsRespawning && GlenMF.ObjectState != GameObject.eObjectState.Active)
                            {
                                GlenMF.AddToWorld();
                                log.Info("62 Glenlock Jarl Mjollner Added");
                                midAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (GlenGF.IsAlive && GlenGF.ObjectState == GameObject.eObjectState.Active ||
                            GlenGF.IsRespawning)
                        {
                            if (GlenGF.IsRespawning)
                                GlenGF.StopRespawn();

                            GlenGF.Delete();
                            log.Info("63 Glenlock Knight Grallahorn Removed");
                            midAddedGuardsCount--;
                        }

                        if (GlenMF.IsAlive && GlenMF.ObjectState == GameObject.eObjectState.Active ||
                            GlenMF.IsRespawning)
                        {
                            if (GlenMF.IsRespawning)
                                GlenMF.StopRespawn();

                            GlenMF.Delete();
                            log.Info("64 Glenlock Knight Mjollner Removed");
                            midAddedGuardsCount--;
                        }
                    }
                }

                #endregion Glenlock Add
            }

            #endregion Glenlock

            #region Blendrake

            keep = GameServer.KeepManager.GetKeepByID(78);
            if (keep != null)
            {
                #region Blendrake Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Blendrake Jarl", eRealm.Midgard);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            BlendGF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("65 Blendrake Jarl Grallahorn Added");
                        }

                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            BlendMF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("66 Blendrake Jarl Mjollner Added");
                        }
                    }
                    else
                    {
                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (BlendGF.IsAlive && BlendGF.ObjectState == GameObject.eObjectState.Active ||
                                BlendGF.IsRespawning)
                            {
                                if (BlendGF.IsRespawning)
                                    BlendGF.StopRespawn();

                                BlendGF.Delete();
                                log.Info("67 Blendrake Knight Grallahorn Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!BlendGF.IsRespawning && BlendGF.ObjectState != GameObject.eObjectState.Active)
                            {
                                BlendGF.AddToWorld();
                                log.Info("68 Blendrake Jarl Grallahorn Added");
                                midAddedGuardsCount++;
                            }

                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (BlendMF.IsAlive && BlendMF.ObjectState == GameObject.eObjectState.Active ||
                                BlendMF.IsRespawning)
                            {
                                if (BlendMF.IsRespawning)
                                    BlendMF.StopRespawn();

                                BlendMF.Delete();
                                log.Info("69 Blendrake Knight Mjollner Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!BlendMF.IsRespawning && BlendMF.ObjectState != GameObject.eObjectState.Active)
                            {
                                BlendMF.AddToWorld();
                                log.Info("70 Blendrake Jarl Mjollner Added");
                                midAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (BlendGF.IsAlive && BlendGF.ObjectState == GameObject.eObjectState.Active ||
                            BlendGF.IsRespawning)
                        {
                            if (BlendGF.IsRespawning)
                                BlendGF.StopRespawn();

                            BlendGF.Delete();
                            log.Info("71 Blendrake Knight Grallahorn Removed");
                            midAddedGuardsCount--;
                        }

                        if (BlendMF.IsAlive && BlendMF.ObjectState == GameObject.eObjectState.Active ||
                            BlendMF.IsRespawning)
                        {
                            if (BlendMF.IsRespawning)
                                BlendMF.StopRespawn();

                            BlendMF.Delete();
                            log.Info("72 Blendrake Knight Mjollner Removed");
                            midAddedGuardsCount--;
                        }
                    }
                }

                #endregion Blendrake Add
            }

            #endregion Blendrake

            #region Hlidskialf

            keep = GameServer.KeepManager.GetKeepByID(77);
            if (keep != null)
            {
                #region Hlidskialf Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Hlidskialf Jarl", eRealm.Midgard);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            HlidGF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("73 Hlidskialf Jarl Grallahorn Added");
                        }

                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            HlidMF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("74 Hlidskialf Jarl Mjollner Added");
                        }
                    }
                    else
                    {
                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (HlidGF.IsAlive && HlidGF.ObjectState == GameObject.eObjectState.Active ||
                                HlidGF.IsRespawning)
                            {
                                if (HlidGF.IsRespawning)
                                    HlidGF.StopRespawn();

                                HlidGF.Delete();
                                log.Info("75 Hlidskialf Knight Grallahorn Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!HlidGF.IsRespawning && HlidGF.ObjectState != GameObject.eObjectState.Active)
                            {
                                HlidGF.AddToWorld();
                                log.Info("76 Hlidskialf Jarl Grallahorn Added");
                                midAddedGuardsCount++;
                            }

                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (HlidMF.IsAlive && HlidMF.ObjectState == GameObject.eObjectState.Active ||
                                HlidMF.IsRespawning)
                            {
                                if (HlidMF.IsRespawning)
                                    HlidMF.StopRespawn();

                                HlidMF.Delete();
                                log.Info("77 Hlidskialf Knight Mjollner Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!HlidMF.IsRespawning && HlidMF.ObjectState != GameObject.eObjectState.Active)
                            {
                                HlidMF.AddToWorld();
                                log.Info("78 Hlidskialf Jarl Mjollner Added");
                                midAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (HlidGF.IsAlive && HlidGF.ObjectState == GameObject.eObjectState.Active ||
                            HlidGF.IsRespawning)
                        {
                            if (HlidGF.IsRespawning)
                                HlidGF.StopRespawn();

                            HlidGF.Delete();
                            log.Info("79 Hlidskialf Knight Grallahorn Removed");
                            midAddedGuardsCount--;
                        }

                        if (HlidMF.IsAlive && HlidMF.ObjectState == GameObject.eObjectState.Active ||
                            HlidMF.IsRespawning)
                        {
                            if (HlidMF.IsRespawning)
                                HlidMF.StopRespawn();

                            HlidMF.Delete();
                            log.Info("80 Hlidskialf Knight Mjollner Removed");
                            midAddedGuardsCount--;
                        }
                    }
                }

                #endregion Hlidskialf Add
            }

            #endregion Hlidskialf

            #region Nottmoor

            keep = GameServer.KeepManager.GetKeepByID(76);
            if (keep != null)
            {
                #region Nottmoor Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Nottmoor Jarl", eRealm.Midgard);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            NottGF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("81 Nottmoor Jarl Grallahorn Added");
                        }

                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            NottMF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("82 Nottmoor Jarl Mjollner Added");
                        }
                    }
                    else
                    {
                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (NottGF.IsAlive && NottGF.ObjectState == GameObject.eObjectState.Active ||
                                NottGF.IsRespawning)
                            {
                                if (NottGF.IsRespawning)
                                    NottGF.StopRespawn();

                                NottGF.Delete();
                                log.Info("83 Nottmoor Knight Grallahorn Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!NottGF.IsRespawning && NottGF.ObjectState != GameObject.eObjectState.Active)
                            {
                                NottGF.AddToWorld();
                                log.Info("84Nottmoor Jarl Grallahorn Added");
                                midAddedGuardsCount++;
                            }

                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (NottMF.IsAlive && NottMF.ObjectState == GameObject.eObjectState.Active ||
                                NottMF.IsRespawning)
                            {
                                if (NottMF.IsRespawning)
                                    NottMF.StopRespawn();

                                NottMF.Delete();
                                log.Info("85 Nottmoor Knight Mjollner Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!NottMF.IsRespawning && NottMF.ObjectState != GameObject.eObjectState.Active)
                            {
                                NottMF.AddToWorld();
                                log.Info(" 86 Nottmoor Jarl Mjollner Added");
                                midAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (NottGF.IsAlive && NottGF.ObjectState == GameObject.eObjectState.Active ||
                            NottGF.IsRespawning)
                        {
                            if (NottGF.IsRespawning)
                                NottGF.StopRespawn();

                            NottGF.Delete();
                            log.Info("87 Nottmoor Knight Grallahorn Removed");
                            midAddedGuardsCount--;
                        }

                        if (NottMF.IsAlive && NottMF.ObjectState == GameObject.eObjectState.Active ||
                            NottMF.IsRespawning)
                        {
                            if (NottMF.IsRespawning)
                                NottMF.StopRespawn();

                            NottMF.Delete();
                            log.Info("88 Nottmoor Knight Mjollner Removed");
                            midAddedGuardsCount--;
                        }
                    }
                }

                #endregion Nottmoor Add
            }

            #endregion Nottmoor

            #region Bledmeer

            keep = GameServer.KeepManager.GetKeepByID(75);
            if (keep != null)
            {
                #region Bledmeer Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Bledmeer Jarl", eRealm.Midgard);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            BledGF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("89 Bledmeer Jarl Grallahorn Added");
                        }

                        if (midAddedGuardsCount < midMaxGuards)
                        {
                            BledMF.AddToWorld();
                            midAddedGuardsCount++;
                            log.Info("90 Bledmeer Jarl Mjollner Added");
                        }
                    }
                    else
                    {
                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (BledGF.IsAlive && BledGF.ObjectState == GameObject.eObjectState.Active ||
                                BledGF.IsRespawning)
                            {
                                if (BledGF.IsRespawning)
                                    BledGF.StopRespawn();

                                BledGF.Delete();
                                log.Info("91 Bledmeer Knight Grallahorn Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!BledGF.IsRespawning && BledGF.ObjectState != GameObject.eObjectState.Active)
                            {
                                BledGF.AddToWorld();
                                log.Info("92 Bledmeer Jarl Grallahorn Added");
                                midAddedGuardsCount++;
                            }

                        if (midAddedGuardsCount > midMaxGuards)
                        {
                            if (BledMF.IsAlive && BledMF.ObjectState == GameObject.eObjectState.Active ||
                                BledMF.IsRespawning)
                            {
                                if (BledMF.IsRespawning)
                                    BledMF.StopRespawn();

                                BledMF.Delete();
                                log.Info("93 Bledmeer Knight Mjollner Removed");
                                midAddedGuardsCount--;
                            }
                        }
                        else if (midAddedGuardsCount < midMaxGuards)
                            if (!BledMF.IsRespawning && BledMF.ObjectState != GameObject.eObjectState.Active)
                            {
                                BledMF.AddToWorld();
                                log.Info("94Bledmeer Jarl Mjollner Added");
                                midAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (BledGF.IsAlive && BledGF.ObjectState == GameObject.eObjectState.Active ||
                            BledGF.IsRespawning)
                        {
                            if (BledGF.IsRespawning)
                                BledGF.StopRespawn();

                            BledGF.Delete();
                            log.Info("95 Bledmeer Knight Grallahorn Removed");
                            midAddedGuardsCount--;
                        }

                        if (BledMF.IsAlive && BledMF.ObjectState == GameObject.eObjectState.Active ||
                            BledMF.IsRespawning)
                        {
                            if (BledMF.IsRespawning)
                                BledMF.StopRespawn();

                            BledMF.Delete();
                            log.Info("96Bledmeer Knight Mjollner Removed");
                            midAddedGuardsCount--;
                        }
                    }
                }

                #endregion Bledmeer Add
            }

            #endregion Bledmeer

            #endregion Midgard

            #region Hibernia

            #region Ailinne

            keep = GameServer.KeepManager.GetKeepByID(106);
            if (keep != null)
            {
                #region Ailinne Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Ailinne Sentinel", eRealm.Hibernia);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            AiliDL.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("97 Ailinne Sentinel Lamfhota Added");
                        }
                    }
                    else
                    {
                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (AiliDL.IsAlive && AiliDL.ObjectState == GameObject.eObjectState.Active ||
                                AiliDL.IsRespawning)
                            {
                                if (AiliDL.IsRespawning)
                                    AiliDL.StopRespawn();

                                AiliDL.Delete();
                                log.Info("98 Ailinne Sentinel Lamfhota Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!AiliDL.IsRespawning && AiliDL.ObjectState != GameObject.eObjectState.Active)
                            {
                                AiliDL.AddToWorld();
                                log.Info("99 Ailinne Sentinel Lamfhota Added");
                                hibAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (AiliDL.IsAlive && AiliDL.ObjectState == GameObject.eObjectState.Active ||
                            AiliDL.IsRespawning)
                        {
                            if (AiliDL.IsRespawning)
                                AiliDL.StopRespawn();

                            AiliDL.Delete();
                            log.Info("100 Ailinne Sentinel Lamfhota Removed");
                            hibAddedGuardsCount--;
                        }
                    }
                }

                #endregion Ailinne Add
            }

            #endregion Ailinne

            #region Scathaig

            keep = GameServer.KeepManager.GetKeepByID(105);
            if (keep != null)
            {
                #region Scathaig Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Scathaig Sentinel", eRealm.Hibernia);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            ScatDL.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("101 Scathaig Sentinel Dagda Added");
                        }
                    }
                    else
                    {
                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (ScatDL.IsAlive && ScatDL.ObjectState == GameObject.eObjectState.Active ||
                                ScatDL.IsRespawning)
                            {
                                if (ScatDL.IsRespawning)
                                    ScatDL.StopRespawn();

                                ScatDL.Delete();
                                log.Info("102 Scathaig Sentinel Dagda Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!ScatDL.IsRespawning && ScatDL.ObjectState != GameObject.eObjectState.Active)
                            {
                                ScatDL.AddToWorld();
                                log.Info("103 Scathaig Sentinel Dagda Added");
                                hibAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (ScatDL.IsAlive && ScatDL.ObjectState == GameObject.eObjectState.Active ||
                            ScatDL.IsRespawning)
                        {
                            if (ScatDL.IsRespawning)
                                ScatDL.StopRespawn();

                            ScatDL.Delete();
                            log.Info("104 Scathaig Sentinel Dagda Removed");
                            hibAddedGuardsCount--;
                        }
                    }
                }

                #endregion Scathaig Add
            }

            #endregion Scathaig

            #region nGed

            keep = GameServer.KeepManager.GetKeepByID(103);
            if (keep != null)
            {
                #region nGed Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("nGed Sentinel", eRealm.Hibernia);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            nGedDL.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("105 nGed Sentinel Lamfhota Added");
                        }

                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            nGedDD.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("106 nGed Sentinel Dagda Added");
                        }
                    }
                    else
                    {
                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (nGedDL.IsAlive && nGedDL.ObjectState == GameObject.eObjectState.Active ||
                                nGedDL.IsRespawning)
                            {
                                if (nGedDL.IsRespawning)
                                    nGedDL.StopRespawn();

                                nGedDL.Delete();
                                log.Info("107 nGed Sentinel Lamfhota Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!nGedDL.IsRespawning && nGedDL.ObjectState != GameObject.eObjectState.Active)
                            {
                                nGedDL.AddToWorld();
                                log.Info("108 nGed Sentinel Lamfhota Added");
                                hibAddedGuardsCount++;
                            }

                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (nGedDD.IsAlive && nGedDD.ObjectState == GameObject.eObjectState.Active ||
                                nGedDD.IsRespawning)
                            {
                                if (nGedDD.IsRespawning)
                                    nGedDD.StopRespawn();

                                nGedDD.Delete();
                                log.Info("109 nGed Sentinel Dagda Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!nGedDD.IsRespawning && nGedDD.ObjectState != GameObject.eObjectState.Active)
                            {
                                nGedDD.AddToWorld();
                                log.Info("110 nGed Sentinel Dagda Added");
                                hibAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (nGedDL.IsAlive && nGedDL.ObjectState == GameObject.eObjectState.Active ||
                            nGedDL.IsRespawning)
                        {
                            if (nGedDL.IsRespawning)
                                nGedDL.StopRespawn();

                            nGedDL.Delete();
                            log.Info("111 nGed Sentinel Lamfhota Removed");
                            hibAddedGuardsCount--;
                        }

                        if (nGedDD.IsAlive && nGedDD.ObjectState == GameObject.eObjectState.Active ||
                            nGedDD.IsRespawning)
                        {
                            if (nGedDD.IsRespawning)
                                nGedDD.StopRespawn();

                            nGedDD.Delete();
                            log.Info("112 nGed Sentinel Dagda Removed");
                            hibAddedGuardsCount--;
                        }
                    }
                }

                #endregion nGed Add
            }

            #endregion nGed

            #region Bolg

            keep = GameServer.KeepManager.GetKeepByID(102);
            if (keep != null)
            {
                #region Bolg Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Bolg Sentinel", eRealm.Hibernia);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            BolgDL.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("113 Bolg Sentinel Lamfhota Added");
                        }

                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            BolgDD.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("114 Bolg Sentinel Dagda Added");
                        }
                    }
                    else
                    {
                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (BolgDL.IsAlive && BolgDL.ObjectState == GameObject.eObjectState.Active ||
                                BolgDL.IsRespawning)
                            {
                                if (BolgDL.IsRespawning)
                                    BolgDL.StopRespawn();

                                BolgDL.Delete();
                                log.Info("115 Bolg Sentinel Lamfhota Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!BolgDL.IsRespawning && BolgDL.ObjectState != GameObject.eObjectState.Active)
                            {
                                BolgDL.AddToWorld();
                                log.Info("116 Bolg Sentinel Lamfhota Added");
                                hibAddedGuardsCount++;
                            }

                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (BolgDD.IsAlive && BolgDD.ObjectState == GameObject.eObjectState.Active ||
                                BolgDD.IsRespawning)
                            {
                                if (BolgDD.IsRespawning)
                                    BolgDD.StopRespawn();

                                BolgDD.Delete();
                                log.Info("117 Bolg Sentinel Dagda Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!BolgDD.IsRespawning && BolgDD.ObjectState != GameObject.eObjectState.Active)
                            {
                                BolgDD.AddToWorld();
                                log.Info("118 Bolg Sentinel Dagda Added");
                                hibAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (BolgDL.IsAlive && BolgDL.ObjectState == GameObject.eObjectState.Active ||
                            BolgDL.IsRespawning)
                        {
                            if (BolgDL.IsRespawning)
                                BolgDL.StopRespawn();

                            BolgDL.Delete();
                            log.Info("119 Bolg Sentinel Lamfhota Removed");
                            hibAddedGuardsCount--;
                        }

                        if (BolgDD.IsAlive && BolgDD.ObjectState == GameObject.eObjectState.Active ||
                            BolgDD.IsRespawning)
                        {
                            if (BolgDD.IsRespawning)
                                BolgDD.StopRespawn();

                            BolgDD.Delete();
                            log.Info("120 Bolg Sentinel Dagda Removed");
                            hibAddedGuardsCount--;
                        }
                    }
                }

                #endregion Bolg Add
            }

            #endregion Bolg

            #region Behnn

            keep = GameServer.KeepManager.GetKeepByID(104);
            if (keep != null)
            {
                #region Behnn Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Behnn Sentinel", eRealm.Hibernia);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            BehnDL.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("121 Da Behnn Sentinel Lamfhota Added");
                        }

                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            BehnDD.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("122 Da Behnn Sentinel Dagda Added");
                        }
                    }
                    else
                    {
                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (BehnDL.IsAlive && BehnDL.ObjectState == GameObject.eObjectState.Active ||
                                BehnDL.IsRespawning)
                            {
                                if (BehnDL.IsRespawning)
                                    BehnDL.StopRespawn();

                                BehnDL.Delete();
                                log.Info("123 Da Behnn Sentinel Lamfhota Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!BehnDL.IsRespawning && BehnDL.ObjectState != GameObject.eObjectState.Active)
                            {
                                BehnDL.AddToWorld();
                                log.Info("124 Da Behnn Sentinel Lamfhota Added");
                                hibAddedGuardsCount++;
                            }

                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (BehnDD.IsAlive && BehnDD.ObjectState == GameObject.eObjectState.Active ||
                                BehnDD.IsRespawning)
                            {
                                if (BehnDD.IsRespawning)
                                    BehnDD.StopRespawn();

                                BehnDD.Delete();
                                log.Info("125 Da Behnn Sentinel Dagda Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!BehnDD.IsRespawning && BehnDD.ObjectState != GameObject.eObjectState.Active)
                            {
                                BehnDD.AddToWorld();
                                log.Info("126 Da Behnn Sentinel Dagda Added");
                                hibAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (BehnDL.IsAlive && BehnDL.ObjectState == GameObject.eObjectState.Active ||
                            BehnDL.IsRespawning)
                        {
                            if (BehnDL.IsRespawning)
                                BehnDL.StopRespawn();

                            BehnDL.Delete();
                            log.Info("127 Da Behnn Sentinel Lamfhota Removed");
                            hibAddedGuardsCount--;
                        }

                        if (BehnDD.IsAlive && BehnDD.ObjectState == GameObject.eObjectState.Active ||
                            BehnDD.IsRespawning)
                        {
                            if (BehnDD.IsRespawning)
                                BehnDD.StopRespawn();

                            BehnDD.Delete();
                            log.Info("128 Da Behnn Sentinel Dagda Removed");
                            hibAddedGuardsCount--;
                        }
                    }
                }

                #endregion Behnn Add
            }

            #endregion Behnn

            #region Crimthain

            keep = GameServer.KeepManager.GetKeepByID(101);
            if (keep != null)
            {
                #region Crimthain Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Crimthain Sentinel", eRealm.Hibernia);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            CrimDL.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("129 Crimthain Sentinel Lamfhota Added");
                        }

                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            CrimDD.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("130 Crimthain Sentinel Dagda Added");
                        }
                    }
                    else
                    {
                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (CrimDL.IsAlive && CrimDL.ObjectState == GameObject.eObjectState.Active ||
                                CrimDL.IsRespawning)
                            {
                                if (CrimDL.IsRespawning)
                                    CrimDL.StopRespawn();

                                CrimDL.Delete();
                                log.Info("131 Crimthain Sentinel Lamfhota Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!CrimDL.IsRespawning && CrimDL.ObjectState != GameObject.eObjectState.Active)
                            {
                                CrimDL.AddToWorld();
                                log.Info("132 Crimthain Sentinel Lamfhota Added");
                                hibAddedGuardsCount++;
                            }

                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (CrimDD.IsAlive && CrimDD.ObjectState == GameObject.eObjectState.Active ||
                                CrimDD.IsRespawning)
                            {
                                if (CrimDD.IsRespawning)
                                    CrimDD.StopRespawn();

                                CrimDD.Delete();
                                log.Info("133 Crimthain Sentinel Dagda Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!CrimDD.IsRespawning && CrimDD.ObjectState != GameObject.eObjectState.Active)
                            {
                                CrimDD.AddToWorld();
                                log.Info("134 Crimthain Sentinel Dagda Added");
                                hibAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (CrimDL.IsAlive && CrimDL.ObjectState == GameObject.eObjectState.Active ||
                            CrimDL.IsRespawning)
                        {
                            if (CrimDL.IsRespawning)
                                CrimDL.StopRespawn();

                            CrimDL.Delete();
                            log.Info("135 Crimthain Sentinel Lamfhota Removed");
                            hibAddedGuardsCount--;
                        }

                        if (CrimDD.IsAlive && CrimDD.ObjectState == GameObject.eObjectState.Active ||
                            CrimDD.IsRespawning)
                        {
                            if (CrimDD.IsRespawning)
                                CrimDD.StopRespawn();

                            CrimDD.Delete();
                            log.Info("136 Crimthain Sentinel Dagda Removed");
                            hibAddedGuardsCount--;
                        }
                    }
                }

                #endregion Crimthain Add
            }

            #endregion Crimthain

            #region Crauchon

            keep = GameServer.KeepManager.GetKeepByID(100);
            if (keep != null)
            {
                #region Crauchon Add

                GameNPC[] npcs = WorldMgr.GetNPCsByName("Crauchon Sentinel", eRealm.Hibernia);
                if (keep.Realm == keep.OriginalRealm)
                {
                    if (npcs.Length == 0)
                    {
                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            CrauDL.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("137 Crauchon Sentinel Lamfhota Added");
                        }

                        if (hibAddedGuardsCount < hibMaxGuards)
                        {
                            CrauDD.AddToWorld();
                            hibAddedGuardsCount++;
                            log.Info("138 Crauchon Sentinel Dagda Added");
                        }
                    }
                    else
                    {
                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (CrauDL.IsAlive && CrauDL.ObjectState == GameObject.eObjectState.Active ||
                                CrauDL.IsRespawning)
                            {
                                if (CrauDL.IsRespawning)
                                    CrauDL.StopRespawn();

                                CrauDL.Delete();
                                log.Info("139 Crauchon Sentinel Lamfhota Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!CrauDL.IsRespawning && CrauDL.ObjectState != GameObject.eObjectState.Active)
                            {
                                CrauDL.AddToWorld();
                                log.Info("140 Crauchon Sentinel Lamfhota Added");
                                hibAddedGuardsCount++;
                            }

                        if (hibAddedGuardsCount > hibMaxGuards)
                        {
                            if (CrauDD.IsAlive && CrauDD.ObjectState == GameObject.eObjectState.Active ||
                                CrauDD.IsRespawning)
                            {
                                if (CrauDD.IsRespawning)
                                    CrauDD.StopRespawn();

                                CrauDD.Delete();
                                log.Info("141 Crauchon Sentinel Dagda Removed");
                                hibAddedGuardsCount--;
                            }
                        }
                        else if (hibAddedGuardsCount < hibMaxGuards)
                            if (!CrauDD.IsRespawning && CrauDD.ObjectState != GameObject.eObjectState.Active)
                            {
                                CrauDD.AddToWorld();
                                log.Info("142 Crauchon Sentinel Dagda Added");
                                hibAddedGuardsCount++;
                            }
                    }
                }
                else
                {
                    if (!firstrun)
                    {
                        if (CrauDL.IsAlive && CrauDL.ObjectState == GameObject.eObjectState.Active ||
                            CrauDL.IsRespawning)
                        {
                            if (CrauDL.IsRespawning)
                                CrauDL.StopRespawn();

                            CrauDL.Delete();
                            log.Info("143 Crauchon Sentinel Lamfhota Removed");
                            hibAddedGuardsCount--;
                        }

                        if (CrauDD.IsAlive && CrauDD.ObjectState == GameObject.eObjectState.Active ||
                            CrauDD.IsRespawning)
                        {
                            if (CrauDD.IsRespawning)
                                CrauDD.StopRespawn();

                            CrauDD.Delete();
                            log.Info("144 Crauchon Sentinel Dagda Removed");
                            hibAddedGuardsCount--;
                        }
                    }
                }

                #endregion Crauchon Add
            }

            #endregion Crauchon

            #endregion Hibernia

            log.Debug("albAddedGuardsCount= " + albAddedGuardsCount);
            log.Debug("midAddedGuardsCount= " + midAddedGuardsCount);
            log.Debug("hibAddedGuardsCount= " + hibAddedGuardsCount);
            firstrun = false;
        }

        public static void NotifyRelic(DOLEvent e, object sender, EventArgs args)
        {
            if (e == RelicPadEvent.RelicStolen || e == RelicPadEvent.RelicMounted)
            {
                RelicPadEventArgs relicEvent = args as RelicPadEventArgs;

                Init();
            }
        }

        public static void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (e == KeepEvent.KeepTaken)
            {
                KeepEventArgs keepEvent = args as KeepEventArgs;
                if (keepEvent.Keep.CurrentZone.IsOF == false)
                {
                    return;
                }

                Init(); // Keep(keepEvent.Keep.KeepID);
            }
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnload(DOLEvent e, object sender, EventArgs args)
        {
            #region Albion

            if (BenoCE != null)
                BenoCE.Delete();
            if (BenoCM != null)
                BenoCM.Delete();

            if (SursCE != null)
                SursCE.Delete();
            if (SursCM != null)
                SursCM.Delete();

            if (ErasCE != null)
                ErasCE.Delete();
            if (ErasCM != null)
                ErasCM.Delete();

            if (BercCE != null)
                BercCE.Delete();
            if (BercCM != null)
                BercCM.Delete();

            if (BoldCE != null)
                BoldCE.Delete();
            if (BoldCM != null)
                BoldCM.Delete();

            if (RenaCE != null)
                RenaCE.Delete();

            if (HurbuCM != null)
                HurbuCM.Delete();

            #endregion Albion

            #region Midgard

            if (BledGF != null)
                BledGF.Delete();
            if (BledMF != null)
                BledMF.Delete();

            if (NottGF != null)
                NottGF.Delete();
            if (NottMF != null)
                NottMF.Delete();

            if (BlendGF != null)
                BlendGF.Delete();
            if (BlendMF != null)
                BlendMF.Delete();

            if (GlenGF != null)
                GlenGF.Delete();
            if (GlenMF != null)
                GlenMF.Delete();

            if (HlidGF != null)
                HlidGF.Delete();
            if (HlidMF != null)
                HlidMF.Delete();

            if (ArvaGF != null)
                ArvaGF.Delete();

            if (FensMF != null)
                FensMF.Delete();

            #endregion Midgard

            #region Hibernia

            if (CrauDL != null)
                CrauDL.Delete();
            if (CrauDD != null)
                CrauDD.Delete();

            if (BehnDL != null)
                BehnDL.Delete();
            if (BehnDD != null)
                BehnDD.Delete();

            if (BolgDL != null)
                BolgDL.Delete();
            if (BolgDD != null)
                BolgDD.Delete();

            if (CrimDL != null)
                CrimDL.Delete();
            if (CrimDD != null)
                CrimDD.Delete();

            if (nGedDL != null)
                nGedDL.Delete();
            if (nGedDD != null)
                nGedDD.Delete();

            if (AiliDL != null)
                AiliDL.Delete();

            if (ScatDL != null)
                ScatDL.Delete();

            #endregion Hibernia
        }
    }
}