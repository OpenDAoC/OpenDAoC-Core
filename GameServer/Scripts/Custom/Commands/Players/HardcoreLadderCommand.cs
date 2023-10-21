using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
        "&hcladder",
        EPrivLevel.Player,
        "Displays the Hardcore Ladder.",
        "/hcladder")]
    public class HardcoreLadderCommand : ACommandHandler, ICommandHandler
    {
        public class HCCharacter : IComparable<HCCharacter>
        {
            public string CharacterName { get; set; }
        
            public int CharacterLevel { get; set; }
        
            public string CharacterClass { get; set; }
            
            public bool isSolo { get; set; }
            
            public override string ToString()
            {
                if (isSolo)
                    return string.Format("{0} the level {1} {2} <solo>", CharacterName, CharacterLevel, CharacterClass);
                
                return string.Format("{0} the level {1} {2}", CharacterName, CharacterLevel, CharacterClass);
                
            }

            public int CompareTo(HCCharacter compareLevel)
            {
                // A null value means that this object is greater.
                if (compareLevel == null)
                    return 1;
                
                return CharacterLevel.CompareTo(compareLevel.CharacterLevel);
            }
        }
        
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "hardcoreladder"))
                return;
            
            IList<string> textList = GetHardcoreLadder();
            client.Out.SendCustomTextWindow("Hardcore Ladder", textList);
            return;

            IList<string> GetHardcoreLadder()
            
        {
            IList<string> output = new List<string>();
            IList<HCCharacter> hcCharacters = new List<HCCharacter>();
            IList<DbCoreCharacter> characters = GameServer.Database.SelectObjects<DbCoreCharacter>(DB.Column("HCFlag").IsEqualTo(1)).OrderByDescending(x => x.Level).Take(50).ToList();
            
            output.Add("Top 50 Hardcore characters:\n");
            
            foreach (DbCoreCharacter c in characters)
            {
                if (c == null)
                    continue;

                string className = ((EPlayerClass)c.Class).ToString();
                bool isSolo = false;
                
                const string customKey = "grouped_char";
                var hasGrouped = CoreDb<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(c.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

                if (hasGrouped == null || c.NoHelp)
                {
                    isSolo = true;
                }
                
                hcCharacters.Add(new HCCharacter() {CharacterName = c.Name, CharacterLevel = c.Level, CharacterClass = className, isSolo = isSolo});

            }

            int position = 0;
            
            foreach (HCCharacter hcCharacter in hcCharacters)
            {
                position++;
                output.Add(position + ". " + hcCharacter);
            }

            return output;
        }
        }
    }