// TODO: Don't use commands for starting/updating quests
//reference System.Xml.dll
using MCGalaxy;
using MCGalaxy.Maths;
using MCGalaxy.SQL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ProjectCommunity
{
    public class Quests : Plugin
    {
        public override string name { get { return "_Quests"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            string path = "./plugins/Quests/quests.xml";
            if (!File.Exists(path))
            {
                Logger.Log(LogType.Error, "Quest XML not found: " + path);
                return;
            }

            QuestManager.LoadQuests(path);
            Logger.Log(LogType.SystemActivity, "Loaded " + QuestManager.Quests.Count + " quests.");

            Database.CreateTable("Quests", QuestProgressTable);

            Command.Register(new CmdListQuests());
            Command.Register(new CmdStartQuest());
            Command.Register(new CmdUpdateQuest());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("ListQuests"));
            Command.Unregister(Command.Find("StartQuest"));
            Command.Unregister(Command.Find("UpdateQuest"));
        }

        private ColumnDesc[] QuestProgressTable = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("QuestName", ColumnType.VarChar, 64),
            new ColumnDesc("Completed", ColumnType.Int32),
            new ColumnDesc("ProgressIndex", ColumnType.Int32),
        };
    }

    public class Quest
    {
        public int Level;
        public string Name;
        public string Description;
        public bool ObjectivesInOrder;
        public List<string> Objectives;
        public List<Vec3U16> ObjectiveCoords;

        public Quest()
        {
            Objectives = new List<string>();
            ObjectiveCoords = new List<Vec3U16>();
        }
    }

    public static class QuestManager
    {
        public static List<Quest> Quests = new List<Quest>();

        public static void LoadQuests(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlNodeList questNodes = doc.SelectNodes("/Quests/Quest");
            foreach (XmlNode questNode in questNodes)
            {
                Quest quest = new Quest();
                quest.Level = int.Parse(questNode.Attributes["level"].Value);
                quest.Name = questNode.Attributes["name"].Value;
                quest.Description = questNode["Description"].InnerText.Trim();

                XmlNode objectivesWrapper = questNode["Objectives"];
                if (objectivesWrapper == null)
                {
                    Logger.Log(LogType.Warning, "Quest has no objectives: " + quest.Name);
                    continue;
                }

                quest.ObjectivesInOrder = bool.Parse(objectivesWrapper.Attributes["ordered"].Value);

                // Handle nested Objectives (if incorrectly structured)
                XmlNodeList objectiveNodes = objectivesWrapper.SelectNodes(".//Objective");

                foreach (XmlNode objNode in objectiveNodes)
                {
                    string objText = objNode.InnerText.Trim();
                    quest.Objectives.Add(objText);

                    XmlNode locationNode = objNode["Location"];
                    if (locationNode != null && locationNode.Attributes["x"] != null)
                    {
                        int x = int.Parse(locationNode.Attributes["x"].Value);
                        int y = int.Parse(locationNode.Attributes["y"].Value);
                        int z = int.Parse(locationNode.Attributes["z"].Value);
                        quest.ObjectiveCoords.Add(new Vec3U16((ushort)x, (ushort)y, (ushort)z));
                    }
                    else
                    {
                        quest.ObjectiveCoords.Add(new Vec3U16(0, 0, 0));
                    }
                }

                Quests.Add(quest);
            }
        }
    }

    public static class QuestProgressManager
    {
        public static void StartQuest(Player p, string questName)
        {
            Quest quest = QuestManager.Quests.Find(q => q.Name == questName);
            if (quest == null)
            {
                p.Message("%cQuest not found.");
                return;
            }

            List<string[]> rows = Database.GetRows("Quests", "*", "WHERE Name=@0 AND QuestName=@1", p.name, questName);
            if (rows.Count > 0)
            {
                p.Message("%cYou have already started this quest.");
                return;
            }

            Database.AddRow("Quests", "Name, QuestName, Completed, ProgressIndex", p.name, questName, 0, 0);
            p.Message("%aStarted quest: &b" + questName);
        }

        public static void UpdateProgress(Player p, string questName)
        {
            Quest quest = QuestManager.Quests.Find(q => q.Name == questName);
            if (quest == null)
            {
                p.Message("%cQuest not found.");
                return;
            }

            List<string[]> rows = Database.GetRows("Quests", "*", "WHERE Name=@0 AND QuestName=@1", p.name, questName);
            if (rows.Count == 0)
            {
                p.Message("%cYou haven't started that quest yet.");
                return;
            }

            int index = int.Parse(rows[0][3]);
            if (int.Parse(rows[0][2]) == 1)
            {
                p.Message("%cQuest already completed.");
                return;
            }

            if (quest.ObjectivesInOrder)
            {
                index++;
            }
            else
            {
                index = quest.Objectives.Count;
            }

            int complete = index >= quest.Objectives.Count ? 1 : 0;
            Database.UpdateRows("Quests", "ProgressIndex=@2, Completed=@3", "WHERE Name=@0 AND QuestName=@1",
                p.name, questName, index, complete);

            p.Message(complete == 1 ? "%aQuest completed!" : "%eProgress updated: " + index + "/" + quest.Objectives.Count);
        }

        public static List<string[]> GetQuests(Player p, string filter)
        {
            if (filter == "incomplete")
                return Database.GetRows("Quests", "*", "WHERE Name=@0 AND Completed=0", p.name);
            else if (filter == "completed")
                return Database.GetRows("Quests", "*", "WHERE Name=@0 AND Completed=1", p.name);
            else if (filter == "all")
                return Database.GetRows("Quests", "*", "WHERE Name=@0", p.name);
            return new List<string[]>();
        }
    }


    public class CmdListQuests : Command2
    {
        public override string name { get { return "ListQuests"; } }
        public override string shortcut { get { return "lq"; } }
        public override string type { get { return "information"; } }

        public override void Use(Player p, string message)
        {
            string filter = message.ToLower();
            if (filter == "")
            {
                Help(p);
                return;
            }

            if (filter == "all")
            {
                foreach (Quest quest in QuestManager.Quests)
                {
                    List<string[]> rows = Database.GetRows("Quests", "*", "WHERE Name=@0 AND QuestName=@1", p.name, quest.Name);
                    int progress = 0;
                    int completed = 0;

                    if (rows.Count > 0)
                    {
                        completed = int.Parse(rows[0][2]);
                        progress = int.Parse(rows[0][3]);
                    }

                    string status = "&8Not Started";
                    if (completed == 1) status = "&aCompleted";
                    else if (progress > 0) status = "&eIn Progress";

                    p.Message("&b" + quest.Name + " &f[" + status + "&f] " + progress + "/" + quest.Objectives.Count);
                    p.Message("&7" + quest.Description);

                    for (int i = 0; i < quest.Objectives.Count; i++)
                    {
                        string prefix = quest.ObjectivesInOrder ? (i + 1).ToString() + ". " : "- ";
                        string color = i < progress ? "&a" : "&f";
                        p.Message(color + prefix + quest.Objectives[i]);
                    }
                }
            }

            else
            {
                List<string[]> rows = QuestProgressManager.GetQuests(p, filter);
                if (rows.Count == 0)
                {
                    p.Message("%cNo quests found with filter: " + filter);
                    return;
                }

                foreach (string[] row in rows)
                {
                    string name = row[1];
                    int completed = int.Parse(row[2]);
                    int progress = int.Parse(row[3]);

                    Quest quest = QuestManager.Quests.Find(q => q.Name == name);
                    if (quest == null)
                    {
                        p.Message("%cQuest data not found for: " + name);
                        continue;
                    }

                    string status = completed == 1 ? "&aCompleted" : "&eIn Progress";
                    p.Message("&b" + quest.Name + " &f[" + status + "&f] " + progress + "/" + quest.Objectives.Count);
                    p.Message("&7" + quest.Description);

                    for (int i = 0; i < quest.Objectives.Count; i++)
                    {
                        string prefix = quest.ObjectivesInOrder ? (i + 1).ToString() + ". " : "- ";
                        string color = i < progress ? "&a" : "&f";
                        p.Message(color + prefix + quest.Objectives[i]);
                    }
                }
            }
        }

        public override void Help(Player p)
        {
            p.Message("/ListQuests [all|complete|incomplete] - View your quest progress.");
        }
    }

    public class CmdStartQuest : Command2
    {
        public override string name { get { return "StartQuest"; } }
        public override string type { get { return "game"; } }

        public override void Use(Player p, string message)
        {
            if (message.Length == 0)
            {
                p.Message("%cUsage: /StartQuest [QuestName]");
                return;
            }

            QuestProgressManager.StartQuest(p, message);
        }

        public override void Help(Player p)
        {
            p.Message("/StartQuest [QuestName] - Begins a quest.");
        }
    }

    public class CmdUpdateQuest : Command2
    {
        public override string name { get { return "UpdateQuest"; } }
        public override string type { get { return "game"; } }

        public override void Use(Player p, string message)
        {
            if (message.Length == 0)
            {
                p.Message("%cUsage: /UpdateQuest [QuestName]");
                return;
            }

            QuestProgressManager.UpdateProgress(p, message);
        }

        public override void Help(Player p)
        {
            p.Message("/UpdateQuest [QuestName] - Updates your progress for a quest.");
        }
    }

}