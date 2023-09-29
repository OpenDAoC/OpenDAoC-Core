using System.Collections;

namespace DOL.DOLServer
{
	/// <summary>
	/// Defines the interface for server actions
	/// </summary>
	public interface IAction
	{
		/// <summary>
		/// returns the name of this action
		/// </summary>
		string Name { get; }
		/// <summary>
		/// returns the syntax of this action
		/// </summary>
		string Syntax { get; }
		/// <summary>
		/// returns the description of this action
		/// </summary>
		string Description { get; }
		/// <summary>
		/// This method is called when the action should be
		/// executed
		/// </summary>
		/// <param name="parameters">The parsed command line parameters</param>
		void OnAction(Hashtable parameters);
	}
}