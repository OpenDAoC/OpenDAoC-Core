using System;
using Core.Base.Enums;

namespace Core.GS.Server;

/// <summary>
/// Denotes a class as a server rules handler for a given server type
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ServerRulesAttribute : Attribute
{
	protected EGameServerType m_serverType;

	public EGameServerType ServerType
	{
		get { return m_serverType; }
	}

	public ServerRulesAttribute(EGameServerType serverType)
	{
		m_serverType = serverType;
	}
}