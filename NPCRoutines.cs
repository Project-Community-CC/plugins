//reference System.dll
//pluginref DayNightCycle.dll

// Note: To make bots follow a routine, you must first do /botai add routine routine, then /botset [bot] routine.
// TODO: Move routines into XML files

using System;
using System.Collections.Generic;
using System.Globalization;
using MCGalaxy;
using MCGalaxy.Bots;
using MCGalaxy.Events.LevelEvents;
using DayOfWeek = ProjectCommunity.DayOfWeek;
using Season = ProjectCommunity.Season;

namespace ProjectCommunity
{
    public class NPCRoutines : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string name { get { return "NPCRoutines"; } }

        BotInstruction ins;

        public static List<ScheduledNPC> registeredNPCs = new List<ScheduledNPC>();

        public override void Load(bool startup)
        {
            ins = new RoutineInstruction();
            BotInstruction.Instructions.Add(ins);

            // Register NPCs
            registeredNPCs.Add(new PopsNPC());
            registeredNPCs.Add(new DaisyNPC());

            OnDayNightCycleTickEvent.Register(HandleDayNightCycleTick, Priority.Normal);
            OnLevelLoadedEvent.Register(HandleLevelLoaded, Priority.Normal);

            Command.Register(new CmdWhereNPC());
        }

        public override void Unload(bool shutdown)
        {
            OnDayNightCycleTickEvent.Unregister(HandleDayNightCycleTick);
            OnLevelLoadedEvent.Unregister(HandleLevelLoaded);

            BotInstruction.Instructions.Remove(ins);

            Command.Unregister(Command.Find("WhereNPC"));
        }

        private static void TryAddBot(Level lvl, NPCState state)
        {
            PlayerBot bot = new PlayerBot(state.Name, lvl);

            if (lvl.Bots.Count >= Server.Config.MaxBotsPerLevel)
            {
                Console.WriteLine("Could not spawn NPC as reached over maximum number of bots allowed in " + lvl.name + ".");
                return;
            }

            Position startPos = new Position((state.Position.X * 32) + 16, (state.Position.Y * 32) + 52, (state.Position.Z * 32) + 16);
            bot.SetInitialPos(startPos);
            bot.SetYawPitch(0, 0); // TODO: Store orientation
            bot.TargetPos = startPos;
            bot.DisplayName = "";
            if (!ScriptFile.Parse(Player.Console, bot, "routine")) return;
            BotsFile.Save(bot.level);
            // bot.SkinName = ""; // TODO: Skins
            PlayerBot.Add(bot);
        }

        private static PlayerBot FindBot(Level lvl, string name)
        {
            PlayerBot[] bots = lvl.Bots.Items;
            foreach (PlayerBot bot in bots)
                if (bot.name.CaselessEq(name)) return bot;

            return null;
        }

        private void HandleLevelLoaded(Level lvl)
        {
            foreach (var npc in registeredNPCs)
            {
                if (!npc.LevelName.CaselessEq(lvl.name)) continue;

                PlayerBot bot = FindBot(lvl, npc.Name);
                if (bot == null)
                {
                    UpdateNPCState(npc.Name, npc.LevelName, npc.CurrentPosition, npc.Description);
                }
                else
                {
                    Position pos = npc.CurrentPosition;
                    Position newPos = new Position((pos.X * 32) + 16, (pos.Y * 32) + 52, (pos.Z * 32) + 16);
                    bot.Pos = newPos;
                    bot.TargetPos = bot.Pos;
                    bot.movement = false;
                }
            }
        }


        private void HandleDayNightCycleTick(int time, Season season, DayOfWeek day)
        {
            int totalMinutes = ((time) % 24000) * 60 / 1000;
            int hour = totalMinutes / 60;
            int minute = totalMinutes % 60;

            foreach (var npc in registeredNPCs)
                npc.OnTimeChanged(hour, minute, day, season);
        }

        public class NPCState
        {
            public string Name;
            public string LevelName;
            public Position Position;
            public string TaskDescription;

            public NPCState(string name, string levelName, Position position, string taskDescription = null)
            {
                Name = name;
                LevelName = levelName;
                Position = position;
                TaskDescription = taskDescription;
            }
        }

        public abstract class ScheduledNPC
        {
            public string Name { get; set; }
            public string LevelName { get; set; }
            public Position CurrentPosition { get; set; }
            public string Description { get; set; }
            public string Skin { get; set; }
            public PlayerBot BotInstance { get; set; }

            protected ScheduledNPC(string name, string levelName, Position startPos, string skin = null)
            {
                Name = name;
                LevelName = levelName;
                CurrentPosition = startPos;
                Skin = skin;
            }

            public virtual void OnTimeChanged(int hour, int minute, DayOfWeek day, Season season) { }

            protected void MoveTo(Position pos, string taskDesc = null, List<Position> path = null)
            {
                Description = taskDesc;
                CurrentPosition = pos;
                NPCRoutines.UpdateNPCState(Name, LevelName, pos, taskDesc, path);
            }
        }

        public static List<DayOfWeek> weekdays = new List<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        public class DaisyNPC : ScheduledNPC
        {
            public DaisyNPC() : base("Daisy", "town", new Position(190, 54, 167), "https://i.imgur.com/rS2ktJi.png") { }

            public override void OnTimeChanged(int hour, int minute, DayOfWeek day, Season season)
            {
                if (weekdays.Contains(day) && hour == 6 && minute == 00)
                    MoveTo(new Position(190, 54, 167), "Leaving the house", new List<Position> {
                        new Position(190, 54, 167),
                        new Position(188, 54, 170),
                        new Position(187, 54, 173),
                        new Position(190, 54, 177),
                        new Position(194, 54, 181),
                        new Position(198, 54, 186),
                        new Position(198, 54, 196),
                        new Position(197, 54, 200),
                        new Position(198, 54, 204),
                        new Position(202, 54, 208),
                        new Position(204, 54, 213),
                        new Position(208, 54, 216),
                        new Position(212, 54, 216),
                        new Position(216, 54, 220),
                        new Position(222, 54, 220),
                        new Position(227, 54, 224),
                        new Position(233, 54, 229),
                        new Position(237, 54, 230),
                        new Position(241, 54, 231),
                        new Position(245, 54, 231),
                        new Position(247, 54, 234),
                        new Position(248, 54, 238),
                        new Position(248, 54, 242),
                        new Position(246, 54, 244),
                        new Position(246, 55, 246),
                        new Position(245, 55, 248)
                    });

                else if (weekdays.Contains(day) && hour == 6 && minute == 55)
                    MoveTo(new Position(245, 55, 248), "Tending to crops");

                else if (weekdays.Contains(day) && hour == 7 && minute == 15)
                    MoveTo(new Position(245, 55, 248), "Tending to crops", new List<Position> {
                        new Position(247, 55, 248),
                        new Position(249, 55, 249),
                        new Position(251, 55, 250),
                        new Position(253, 55, 250),
                        new Position(254, 55, 248),
                        new Position(255, 55, 248),
                        new Position(259, 55, 248),
                        new Position(260, 55, 250),
                        new Position(262, 55, 251),
                        new Position(263, 55, 253),
                        new Position(264, 55, 253)
                    });

                else if (weekdays.Contains(day) && hour == 7 && minute == 46)
                    MoveTo(new Position(264, 55, 253), "Tending to crops", new List<Position> {
                        new Position(263, 55, 254),
                        new Position(261, 55, 254),
                        new Position(259, 55, 252),
                        new Position(257, 55, 253),
                        new Position(257, 55, 255),
                        new Position(257, 56, 262),
                        new Position(256, 56, 264)
                    });

                else if (weekdays.Contains(day) && hour == 8 && minute == 18)
                    MoveTo(new Position(256, 56, 264), "Tending to crops", new List<Position> {
                        new Position(259, 56, 265),
                        new Position(265, 56, 265),
                        new Position(265, 56, 272)
                    });

                else if (weekdays.Contains(day) && hour == 8 && minute == 48)
                    MoveTo(new Position(265, 56, 272), "Tending to crops", new List<Position> {
                        new Position(267, 56, 273),
                        new Position(270, 56, 274),
                        new Position(272, 55, 275),
                        new Position(276, 55, 275),
                        new Position(276, 55, 278),
                        new Position(277, 55, 281)
                    });

                else if (weekdays.Contains(day) && hour == 9 && minute == 18)
                    MoveTo(new Position(277, 55, 281), "Tending to crops", new List<Position> {
                        new Position(279, 55, 281),
                        new Position(282, 54, 281),
                        new Position(283, 54, 283),
                        new Position(286, 54, 283),
                        new Position(286, 54, 288),
                        new Position(287, 54, 290),
                        new Position(288, 54, 292)
                    });

                else if (weekdays.Contains(day) && hour == 9 && minute == 40)
                    MoveTo(new Position(288, 54, 292), "Tending to crops", new List<Position> {
                        new Position(286, 54, 294),
                        new Position(283, 54, 297),
                        new Position(280, 54, 299),
                        new Position(278, 54, 301),
                        new Position(277, 54, 303),
                        new Position(275, 54, 305)
                    });

                else if (weekdays.Contains(day) && hour == 10 && minute == 10)
                    MoveTo(new Position(275, 54, 305), "Tending to crops", new List<Position> {
                        new Position(273, 54, 304),
                        new Position(270, 54, 302),
                        new Position(268, 54, 300),
                        new Position(267, 54, 297),
                        new Position(265, 55, 296)
                    });

                else if (weekdays.Contains(day) && hour == 10 && minute == 39)
                    MoveTo(new Position(265, 55, 296), "Tending to crops", new List<Position> {
                        new Position(263, 55, 296),
                        new Position(259, 55, 298),
                        new Position(256, 55, 299),
                        new Position(251, 55, 299),
                        new Position(249, 56, 297),
                        new Position(246, 56, 297)
                    });

                else if (weekdays.Contains(day) && hour == 11 && minute == 09)
                    MoveTo(new Position(246, 56, 297), "Tending to crops", new List<Position> {
                        new Position(246, 56, 294),
                        new Position(246, 56, 289),
                        new Position(245, 56, 286),
                        new Position(243, 56, 284)
                    });

                else if (weekdays.Contains(day) && hour == 11 && minute == 37)
                    MoveTo(new Position(243, 56, 284), "Tending to crops", new List<Position> {
                        new Position(242, 56, 282),
                        new Position(240, 56, 280),
                        new Position(237, 56, 280),
                        new Position(236, 56, 276),
                        new Position(238, 56, 274),
                        new Position(238, 56, 272)
                    });

                else if (weekdays.Contains(day) && hour == 12 && minute == 09)
                    MoveTo(new Position(238, 56, 272), "Tending to crops", new List<Position> {
                        new Position(234, 56, 272),
                        new Position(233, 56, 275),
                        new Position(233, 56, 278),
                        new Position(231, 56, 281),
                        new Position(230, 56, 282),
                        new Position(230, 56, 286),
                        new Position(227, 56, 288),
                        new Position(227, 56, 293),
                        new Position(225, 56, 293)
                    });

                else if (weekdays.Contains(day) && hour == 12 && minute == 45)
                    MoveTo(new Position(225, 56, 293), "Tending to crops", new List<Position> {
                        new Position(222, 56, 293),
                        new Position(219, 56, 292),
                        new Position(217, 56, 290),
                        new Position(217, 56, 288),
                        new Position(214, 56, 288),
                        new Position(214, 56, 284),
                        new Position(212, 56, 282),
                        new Position(211, 56, 281)
                    });

                else if (weekdays.Contains(day) && hour == 13 && minute == 18)
                    MoveTo(new Position(211, 56, 281), "Tending to crops", new List<Position> {
                        new Position(209, 56, 282),
                        new Position(206, 55, 283),
                        new Position(203, 55, 283),
                        new Position(200, 54, 283),
                        new Position(198, 54, 284),
                        new Position(196, 54, 286),
                        new Position(194, 54, 288),
                        new Position(191, 54, 290),
                    });
            }
        }

        public class PopsNPC : ScheduledNPC
        {
            public PopsNPC() : base("Pops", "town", new Position(394, 13, 487), "https://i.imgur.com/rS2ktJi.png") { }

            public override void OnTimeChanged(int hour, int minute, DayOfWeek day, Season season)
            {
                if (day == DayOfWeek.Sunday && hour == 6 && minute == 0)
                    MoveTo(new Position(394, 13, 487), "Sleeping in bed");

                else if (day == DayOfWeek.Sunday && hour == 6 && minute == 4)
                    MoveTo(new Position(50, 32, 20), "Walking to counter", new List<Position> {
                        new Position(34, 32, 20),
                        new Position(34, 32, 30)
                    });

                else if (day == DayOfWeek.Sunday && hour == 6 && minute == 25)
                    MoveTo(new Position(34, 32, 30), "Standing behind counter");

                else if (day == DayOfWeek.Sunday && hour == 12 && minute == 45)
                    MoveTo(new Position(50, 33, 22), "Going to bed");

                else if (weekdays.Contains(day) && hour == 6 && minute == 00)
                    MoveTo(new Position(394, 13, 487), "Waking up", new List<Position> {
                        new Position(396, 13, 487),
                        new Position(398, 12, 487),
                        new Position(398, 12, 478),
                        new Position(400, 12, 478),
                        new Position(401, 12, 478),
                        new Position(401, 12, 476),
                        new Position(401, 12, 475),
                        new Position(401, 8, 470),
                        new Position(396, 8, 470),
                        new Position(396, 8, 473),
                        new Position(392, 8, 473),
                        new Position(392, 1, 465),
                        new Position(395, 1, 465),
                        new Position(397, 1, 465),
                        new Position(397, 1, 471),
                        new Position(395, 1, 471),
                        new Position(395, 1, 472),
                    });

                else if (weekdays.Contains(day) && hour == 6 && minute == 27)
                    MoveTo(new Position(395, 1, 472), "Making breakfast");

                else if (weekdays.Contains(day) && hour == 6 && minute == 40)
                    MoveTo(new Position(395, 1, 472), "Making breakfast", new List<Position> {
                        new Position(395, 1, 471),
                        new Position(397, 1, 471),
                        new Position(398, 1, 470),
                        new Position(400, 2, 470),
                    });

                else if (weekdays.Contains(day) && hour == 6 && minute == 45)
                    MoveTo(new Position(400, 2, 470), "Eating breakfast");

                else if (weekdays.Contains(day) && hour == 7 && minute == 00)
                    MoveTo(new Position(400, 2, 470), "Eating breakfast", new List<Position> {
                        new Position(398, 1, 470),
                        new Position(398, 1, 470),
                        new Position(397, 1, 471),
                        new Position(394, 1, 471),
                    });

                else if (weekdays.Contains(day) && hour == 7 && minute == 04)
                    MoveTo(new Position(394, 1, 471), "Washing dishes");

                else if (weekdays.Contains(day) && hour == 7 && minute == 15)
                    MoveTo(new Position(394, 1, 471), "Walking to work", new List<Position> {
                        new Position(398, 1, 471),
                        new Position(398, 1, 472),
                        new Position(400, 1, 472),
                        new Position(400, 2, 485),
                        new Position(395, 2, 485),
                        new Position(395, 2, 481),
                    });

                else if (weekdays.Contains(day) && hour == 7 && minute == 30)
                    MoveTo(new Position(395, 2, 481), "Working at Pops'");
            }
        }

        public static Dictionary<string, NPCState> npcStates = new Dictionary<string, NPCState>();

        public static Dictionary<string, Queue<Position>> botPaths = new Dictionary<string, Queue<Position>>();

        public static void UpdateNPCState(string npcName, string levelName, Position position, string taskDescription = null, List<Position> path = null)
        {
            NPCState state = new NPCState(npcName, levelName, position, taskDescription);
            npcStates[npcName] = state;

            Level lvl = LevelInfo.FindExact(levelName);
            if (lvl == null) return;

            PlayerBot bot = FindBot(lvl, state.Name);
            if (bot == null)
            { // Bot doesn't exist, let's add a new one at the NPC's position
                TryAddBot(lvl, state);
            }
            else
            { // Bot exists, let's move it to the NPC's position
                Position newPos = new Position((state.Position.X * 32) + 16, (state.Position.Y * 32) + 60, (state.Position.Z * 32) + 16);
                bot.Pos = newPos;
                bot.TargetPos = bot.Pos;

                if (path != null && path.Count > 0)
                {
                    Queue<Position> q = new Queue<Position>();
                    foreach (var pos in path)
                    {
                        Position converted = new Position((pos.X * 32) + 16, (pos.Y * 32) + 52, (pos.Z * 32) + 16);
                        q.Enqueue(converted);
                    }
                    botPaths[state.Name] = q;

                    // Set first target
                    bot.TargetPos = q.Peek();
                    bot.FaceTowards(bot.Pos, bot.TargetPos);
                    bot.movement = true;
                }
                else
                {
                    bot.TargetPos = bot.Pos;
                    bot.movement = false;
                }
            }
        }
    }

    sealed class RoutineInstruction : BotInstruction
    {
        public RoutineInstruction() { Name = "Routine"; }

        public override bool Execute(PlayerBot bot, InstructionData data)
        {
            Position pos = bot.Pos;
            bot.movementSpeed = (int)Math.Round(4m * 100 / 100m);

            // Check if the bot has a path and if it's valid (queue of positions)
            Queue<Position> pathQueue;
            if (!NPCRoutines.botPaths.ContainsKey(bot.name) || NPCRoutines.botPaths[bot.name] == null || NPCRoutines.botPaths[bot.name].Count == 0)
                return true; // No path to move, instruction is complete

            pathQueue = NPCRoutines.botPaths[bot.name];
            Position target = pathQueue.Peek();

            int dx = Math.Sign(target.X - pos.X);
            int dy = Math.Sign(target.Y - pos.Y);
            int dz = Math.Sign(target.Z - pos.Z);

            pos.X += (short)(dx * 4);
            pos.Y += (short)(dy * 4);
            pos.Z += (short)(dz * 4);

            // Check if we've reached the target position
            if (Math.Abs(target.X - pos.X) < 4 && Math.Abs(target.Y - pos.Y) < 4 && Math.Abs(target.Z - pos.Z) < 4)
            {
                pos = target;
                pathQueue.Dequeue();

                if (pathQueue.Count > 0)
                {
                    bot.TargetPos = pathQueue.Peek();
                    bot.FaceTowards(bot.Pos, bot.TargetPos);
                }
                else
                {
                    // No more targets, stop movement
                    bot.movement = false;
                    NPCRoutines.botPaths.Remove(bot.name);
                    return true;
                }
            }

            bot.Pos = pos;
            return false;
        }

        public override string[] Help { get { return help; } }
        static string[] help = new string[] {
            "&T/BotAI add [name] routine",
            "&HForces the bot to always move towards its target position (bot.TargetPos).",
        };
    }

    public class CmdWhereNPC : Command2
    {
        public override string name { get { return "WhereNPC"; } }
        public override string type { get { return "information"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message)
        {
            string[] args = message.SplitSpaces();
            if (message.Length == 0)
            {
                Help(p);
                return;
            }

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string npcName = args[0];
            npcName = textInfo.ToTitleCase(npcName);

            if (!NPCRoutines.npcStates.ContainsKey(npcName))
            {
                p.Message("%cNo data found for NPC '" + npcName + "'.");
                return;
            }

            NPCRoutines.NPCState state = NPCRoutines.npcStates[npcName];
            string task = state.TaskDescription ?? "nothing";
            p.Message("&b" + state.Name + " &Sin level &a" + state.LevelName +
                      "&S at position &e" + state.Position.X + "," + state.Position.Y + "," + state.Position.Z +
                      "&S doing: &d" + task);
        }

        public override void Help(Player p)
        {
            p.Message("&T/WhereNPC <name> &S- Shows the current location and activity of the specified NPC.");
        }
    }
}