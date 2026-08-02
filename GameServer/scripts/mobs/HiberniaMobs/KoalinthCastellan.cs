using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class KoalinthCastellan : KoalinthNpc
	{
		public KoalinthCastellan() : base() { }

		protected override int TemplateId => 60162941;
		protected override StandardMobBrain CreateBrain() => new KoalinthCastellanBrain();
	}
}
namespace DOL.AI.Brain
{
	public class KoalinthCastellanBrain : KoalinthBrain
	{
		public KoalinthCastellanBrain() : base() { }

		protected override string BafPackageId => "KoalinthCastellanBaf";
		protected override Spell HasteDebuff => KoalinthCastellan_HasteDebuff;

		private static Spell KoalinthCastellan_HasteDebuff => CreateHasteDebuff("KoalinthCastellanHaste", 11972);
	}
}
