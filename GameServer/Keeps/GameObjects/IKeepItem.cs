using Core.Database;
using Core.Database.Tables;

namespace Core.GS.Keeps
{
	public interface IKeepItem
	{
		ushort CurrentRegionID { get;set;}
		int X { get;set;}
		int Y { get;set;}
		int Z { get;set;}
		ushort Heading { get;set;}
		string TemplateID { get;}
		GameKeepComponent Component { get; set;}
		DbKeepPosition Position { get;set;}
		void LoadFromPosition(DbKeepPosition position, GameKeepComponent component);
		void MoveToPosition(DbKeepPosition position);
	}
}