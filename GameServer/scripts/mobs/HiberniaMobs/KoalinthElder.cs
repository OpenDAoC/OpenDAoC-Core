using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class KoalinthElder : KoalinthNpc
	{
		public KoalinthElder() : base() { }

		protected override int TemplateId => 60162943;
		protected override StandardMobBrain CreateBrain() => new KoalinthElderBrain();
	}
}
namespace DOL.AI.Brain
{
	public class KoalinthElderBrain : KoalinthBrain
	{
		public KoalinthElderBrain() : base() { }

		protected override string BafPackageId => "KoalinthElderBaf";
		protected override Spell HasteDebuff => KoalinthElder_HasteDebuff;

		private static Spell KoalinthElder_HasteDebuff => CreateHasteDebuff("KoalinthElderHaste", 11971);
	}
}
