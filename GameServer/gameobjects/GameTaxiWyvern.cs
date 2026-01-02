using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Movement;

namespace DOL.GS
{
	/// <summary>
	/// Eine fliegende Taxi-Einheit. Erbt von GameTaxiBoat.
	/// </summary>
	public class GameTaxiWyvern : GameTaxiBoat 
	{
        // ERSETZEN SIE HIER DIE ID 2138 DURCH DIE WYVERN-MODEL-ID, DIE ZUVOR FUNKTIONIERT HAT!
        private const int WYVERN_MODEL_ID = 765; 

        // Der Konstruktor, der die Wyvern korrekt initialisiert
        public GameTaxiWyvern() : base()
		{
            // Casting der int-Konstante auf ushort (behebt CS0266)
			this.Model = (ushort)WYVERN_MODEL_ID; 
			this.Name = "Wyvern";
            this.Size = 80; 
            
            BlankBrain brain = new BlankBrain();
			SetOwnBrain(brain);
            
			this.MaxSpeedBase = 2000; 
			this.FixedSpeed = true;
		}
        
        public override int MAX_PASSENGERS => 1; 

        public override int SLOT_OFFSET => 0;
	}
}