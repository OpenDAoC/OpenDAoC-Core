using System;
using Core.GS.Scripts;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the Execute Command Event.
/// </summary>
public class ExecuteCommandEventArgs : EventArgs
{

	private GamePlayer source;
	private ScriptMgr.GameCommand command;
	private string[] pars;

	/// <summary>
	/// Constructs a new ExecuteCommandEventArgs
	/// </summary>
	/// <param name="source">the source that executed the command</param>
	/// <param name="command">the command which was executed.</param>
	/// <param name="pars">the pars given!</param>
	public ExecuteCommandEventArgs(GamePlayer source, ScriptMgr.GameCommand command, string[] pars)
	{
		this.source = source;
		this.command = command;
		this.pars = pars;
	}

	/// <summary>
	/// Gets the GamePlayer source who executed the command.
	/// </summary>
	public GamePlayer Source
	{
		get { return source; }
	}
	
	/// <summary>
	/// Gets the Command which was executed by the source player !
	/// </summary>
	public ScriptMgr.GameCommand Command
	{
		get { return command; }
	}

	/// <summary>
	/// Gets the Command Parameters!
	/// </summary>
	public string[] Parameters
	{
		get { return pars; }
	}
}