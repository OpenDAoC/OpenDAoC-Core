using System;
using System.Reflection;
using DOL.Logging;

namespace DOL.GS
{
    /// <summary>
    /// Eine spezialisierte Klasse für Relic Pads auf den NF Temples.
    /// Erbt von GameRelicPad, setzt aber das Modell auf 2649, um ein sichtbares Objekt zu vermeiden.
    /// </summary>
    public class GameTempleRelicPad : GameRelicPad
    {
        // Wir brauchen keinen eigenen Logger, da der Basis-Logger verwendet werden kann.

        public GameTempleRelicPad() : base() { }

        // Überschreibt das Model Property, um sicherzustellen, dass es unsichtbar ist.
        // Das Standard-Model von GameRelicPad (2655) wird hiermit auf 0 überschrieben.
        public override ushort Model
        {
            get => 2649; // Kein sichtbares 3D-Objekt
            set => base.Model = value;
        }

        // Alle anderen Logiken (Realm, PadType, MountedRelics, AddToWorld, OnPlayerEnter)
        // werden von der Basisklasse GameRelicPad korrekt geerbt.
    }
}