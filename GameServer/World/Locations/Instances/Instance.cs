using System;
using System.Reflection;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// The Instance is an implementation of BaseInstance that contains additional functionality to load
	/// a template from InstanceXElement.
	/// </summary>
	public class Instance : BaseInstance
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Creates an instance object. This shouldn't be used directly - Please use WorldMgr.CreateInstance
		/// to create an instance.
		/// </summary>
		public Instance(ushort ID, RegionData data) :base(ID, data)
		{
		}

		~Instance()
		{
			log.Debug("Instance destructor called for " + Description);
		}

		#region Entrance

		protected GameLocation m_entranceLocation = null;

		/// <summary>
		/// Returns the entrance location into this instance.
		/// </summary>
		public GameLocation InstanceEntranceLocation
		{ get { return m_entranceLocation; } }

		#endregion

		#region LoadFromDatabase

		/// <summary>
		/// Loads elements relating to the given instance keyname from the database and populates the instance.
		/// </summary>
		/// <param name="instanceName"></param>
		public virtual void LoadFromDatabase(string instanceName)
		{
			var objects = DOLDB<DbInstanceXElement>.SelectObjects(DB.Column("InstanceID").IsEqualTo(instanceName));

			if (objects.Count == 0)
				return;

			int count = 0;

			//Now we have a list of DBElements, lets create the various entries
			//associated with them and populate the instance.
			foreach (DbInstanceXElement entry in objects)
			{
				if (entry == null)
					continue; //an odd error, but experience knows best.

				GameObject obj = null;
				string theType = "DOL.GS.GameNPC";

				//Switch the classtype to see what we are making.
				switch (entry.ClassType)
				{
					case "entrance":
						{
							//create the entrance, then move to the next.
							m_entranceLocation = new GameLocation(instanceName + "entranceRegion" + ID, ID, entry.X, entry.Y, entry.Z, entry.Heading);
							//move to the next entry, nothing more to do here...
							continue;
						}
						case "region": continue; //This is used to save the regionID as NPCTemplate.
						case "DOL.GS.GameNPC": break;
						default: theType = entry.ClassType; break;
				}

				//Now we have the classtype to create, create it thus!
				//This is required to ensure we check scripts for the space aswell, such as quests!
				foreach (Assembly asm in ScriptMgr.GameServerScripts)
				{
					obj = (GameObject)(asm.CreateInstance(theType, false));
					if (obj != null)
						break;
				}

				
				if (obj == null)
					continue;


				//We now have an object that isnt null. Lets place it at the location, in this region.

				obj.X = entry.X;
				obj.Y = entry.Y;
				obj.Z = entry.Z;
				obj.Heading = entry.Heading;
				obj.CurrentRegionID = ID;

				//If its an npc, load from the npc template about now.
				if ((GameNPC)obj != null && !string.IsNullOrEmpty(entry.NPCTemplate))
				{
					var listTemplate = Util.SplitCSV(entry.NPCTemplate, true);
					int template = 0;
					
					if (int.TryParse(listTemplate[Util.Random(listTemplate.Count-1)], out template) && template > 0)
					{	
						INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(template);
						//we only want to load the template if one actually exists, or there could be trouble!
						if (npcTemplate != null)
						{
							((GameNPC)obj).LoadTemplate(npcTemplate);
						}
					}
				}
				//Finally, add it to the world!
				obj.AddToWorld();

				//Keep track of numbers.
				count++;
			}

			log.Info("Successfully loaded a db entry to " + Description + " - Region ID " + ID + ". Loaded Entities: " + count);
		}

		#endregion


		/// <summary>
		/// This method returns an int representative of an average level for the instance.
		/// Instances do not scale with level by default, but specific instances like TaskDungeonMission can
		/// use this for an accurate representation of level.
		/// </summary>
		/// <returns></returns>
		public int GetInstanceLevel()
		{
			if (Objects == null)
				return 0;
			double level = 0;
			double count = 0;
			foreach (GameObject obj in Objects)
			{
				if (obj == null)
					continue;

				GamePlayer player = obj as GamePlayer;
				if (player == null)
					continue;

				//Dinberg: Guess work for now!
				//I'll guess an appropriate formulae.
				//100 + 7 times number of players divided by 100, multiplied by E(level)

				//Where E(level) = average level.
				count++;
				level += player.Level;
			}
			level = Math.Max(1,(level / count)); //double needed needed for lower levels...

			level *= ((100 + 7 * count) / 100);
			return (int)level;
		}
	}
}
