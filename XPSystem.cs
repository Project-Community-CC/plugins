using System;
using System.Collections.Generic;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.SQL;
using MCGalaxy.Tasks;
using MCGalaxy;

namespace ProjectCommunity {

    public class XPSkillInfo
    {
        public string Colour;
        public virtual int XPRequiredForLevel(int level)
        {
            return level*100;
        }
        public XPSkillInfo(string colour="%e")
        {
            this.Colour = colour;
        }
    }

    public enum XPSkill
    {
        Fishing,
        Cooking,
        Foraging,
        Mining,
        Farming,
        Social
    }
    public class XPSystem : Plugin {
        public override string creator { get { return "morgana"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        public override string name { get { return "XPSystem"; } }

        public static XPSkillInfo defaultSkill = new XPSkillInfo();


        public static Dictionary<XPSkill, XPSkillInfo> Skills = new Dictionary<XPSkill, XPSkillInfo>()
        {
            {XPSkill.Fishing  ,     new XPSkillInfo("%h")},
            {XPSkill.Cooking  ,     new XPSkillInfo("%n")},
            {XPSkill.Foraging ,     new XPSkillInfo("%2")},
            {XPSkill.Mining   ,     new XPSkillInfo("%j")},
            {XPSkill.Farming  ,     new XPSkillInfo("%m")},
            {XPSkill.Social   ,     new XPSkillInfo("%d")}
        };

        public static string MsgLevelUp = "%eYour {col}{skill} %eskill leveled up to level %a{lvl}%e!";

        public CmdXP xpCmd = new CmdXP();

        public override void Load(bool startup) {
            foreach(var skill in Enum.GetValues(typeof(XPSkill)))
                Database.CreateTable(tblName((XPSkill)skill), PlayerXPTable);

            Command.Register(xpCmd);
        }
        

        public override void Unload(bool shutdown) {
            Command.Unregister(xpCmd);
        }

        private ColumnDesc[] PlayerXPTable = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("Level", ColumnType.Int32),
            new ColumnDesc("XP", ColumnType.Int32),
        };
        
        public static int GetXPRequiredLevelUp(XPSkill skill, int level)
        {
            if (Skills.ContainsKey(skill))
                return Skills[skill].XPRequiredForLevel(level);
            return defaultSkill.XPRequiredForLevel(level);
        }

        public static void AddXP(Player p, XPSkill skill, int xp)
        {
            int newXP =  GetXP(p, skill) + xp;
            SetXP(p, skill, newXP);
            CheckLevelUp(p, skill);
        }

        private static string tblName(XPSkill skill)
        {
            return "xp_" + Enum.GetName(typeof(XPSkill), skill).ToLower();
        }

        public static void SetXP(Player p, XPSkill skill, int xp) {
            string table = tblName(skill);
            
	        List<string[]> rows = Database.GetRows(table, "*", "WHERE Name=@0", p.name);

	        if (rows.Count == 0) {
	            Database.AddRow(table, "Name, Level, XP", p.name, 0, xp);
                return;
	        }

            Database.UpdateRows(table, "XP=@1", "WHERE NAME=@0", p.name, xp);
        }
        
        public static int GetXP(Player p, XPSkill skill)
        {
            List<string[]> rows = Database.GetRows(tblName(skill), "*", "WHERE Name=@0", p.name);

            return rows.Count > 0  ? int.Parse(rows[0][2]) : 0;
        }

        public static void SetLevel(Player p, XPSkill skill, int level) {
            string table =  tblName(skill);
            
	        List<string[]> rows = Database.GetRows(table, "*", "WHERE Name=@0", p.name);

	        if (rows.Count == 0) {
	            Database.AddRow(table, "Name, Level, XP", p.name, level, 0);
                return;
	        }

            Database.UpdateRows(table, "Level=@1", "WHERE NAME=@0", p.name, level);
        }

        public static int GetLevel(Player p, XPSkill skill)
        {
            List<string[]> rows = Database.GetRows(tblName(skill), "*", "WHERE Name=@0", p.name);

            return rows.Count > 0 ? int.Parse(rows[0][1]) : 0;
        }

        public static void CheckLevelUp(Player p, XPSkill skill)
        {
            int xp = GetXP(p, skill);

            int lvl = XPSystem.GetLevel(p, skill);
            int newlvl = lvl;

            while (GetXPRequiredLevelUp(skill, newlvl+1) <= xp)
                newlvl++;

            if (newlvl == lvl)
                return;
            
            SetLevel(p, skill, newlvl);

            string msg = MsgLevelUp.Replace("{skill}", Enum.GetName(typeof(XPSkill), skill))
                .Replace("{col}", Skills.ContainsKey(skill) ? Skills[skill].Colour : defaultSkill.Colour)
                .Replace("{lvl}", newlvl.ToString());

            p.Message(msg);
        }

        public class CmdXP : Command2
        {
            public override string name { get { return "XP"; } }
            public override string type { get { return "other"; } }
            public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
            public LevelPermission giveXPRank { get { return LevelPermission.Operator; } }

            public override void Help(Player p)
            {
                p.Message("%T/xp %S- Get info about current xp");
                p.Message("%T/xp [player] %S- Get info about current xp for a player");
                if (p.Rank < giveXPRank)
                    return;
                p.Message("%T/xp give [player] [skill] [XP] %S- Give XP for skill");
            }
            private void GiveXP(Player p, Player target, string skillarg, string xparg="")
            {
                int xp = 0;
               
                try
                {
                    xp = Convert.ToInt32(xparg);
                }
                catch (Exception)
                {
                    p.Message("&cInvalid XP amount.");
                    return;
                }
                XPSkill skill;
                if (!Enum.TryParse(skillarg, true, out skill))
                {
                    p.Message("&c\"%e" + skillarg + "%c\" is not a valid skill!");
                    return;
                }

                if (!XPSystem.Skills.ContainsKey(skill))
                {
                    p.Message("%cSkill \"%e" + skill + "%c\" doesn't exist!");
                    return;
                }

                XPSystem.AddXP(target, skill, xp);
                p.Message("%eYou gave " + target.ColoredName + " %a" + xp.ToString() + " " + XPSystem.Skills[skill].Colour + skill + "%e XP!");
                target.Message(p.ColoredName + "%e gave you %a" + xp.ToString() + " " + XPSystem.Skills[skill].Colour + skill + "%e XP!");
            }
            private void CmdGive(Player p, string[] args)
            {
                if (p.Rank < giveXPRank)
                {
                    p.Message("&cYou need to be "+ giveXPRank.ToString() + "%c+ give XP.");
                    return;
                }

                if (args.Length < 4)
                {
                    Help(p);
                    return;
                }

                Player target = PlayerInfo.FindMatches(p, args[1]);
                if (target == null)
                {
                    p.Message("%cCouldn't find player \"%e" + args[1] +"%c\".");
                    return;
                }
          
                GiveXP(p, target, args[2], args[3]);
            }
            public override void Use(Player p, string message)
            {
                string[] args = message.Split(' ');

                if (args.Length < 1 || args[0].Trim() == "")
                {
                    DisplayInfo(p,p);
                    return;
                }
                if (args[0].ToLower() == "give")
                {
                    CmdGive(p, args);
                    return;
                }

                Player target = PlayerInfo.FindMatches(p, args[0]);
                if (target == null)
                {
                    p.Message("%cCouldn't find player \"%e" + args[0] +"%c\".");
                    return;
                }

                DisplayInfo(p, target);
            }

            private void DisplayInfo(Player p, Player target)
            {
                p.Message("%eXP Info for " + target.ColoredName + "%e:");
                foreach(var pair in XPSystem.Skills)
                {
                    int level   = XPSystem.GetLevel(target, pair.Key);
                    int xp      = XPSystem.GetXP(target, pair.Key);
                    int xprequired = XPSystem.GetXPRequiredLevelUp(pair.Key, level+1);
                    p.Message("     " + pair.Value.Colour + Enum.GetName(typeof(XPSkill), pair.Key) + "%e: Level %a" + level.ToString());
                    p.Message("         %eXP until level %a" + (level+1).ToString() + "%e: %7"+ (xprequired-xp).ToString() + "%e.");
                }
            }
        }

    }
}