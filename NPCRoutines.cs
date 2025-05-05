//reference System.dll
//pluginref DayNightCycle.dll

// Note: To make bots follow a routine, you must first do /botai add routine routine, then /botset [bot] routine.

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

        public override void Load(bool startup)
        {
            ins = new RoutineInstruction();
            BotInstruction.Instructions.Add(ins);

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

        private void TryAddBot(Level lvl, NPCState state)
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
            PlayerBot.Add(bot);
        }

        private PlayerBot FindBot(Level lvl, string name)
        {
            PlayerBot[] bots = lvl.Bots.Items;
            foreach (PlayerBot bot in bots)
                if (bot.name.CaselessEq(name)) return bot;

            return null;
        }

        private void HandleLevelLoaded(Level lvl)
        {
            foreach (var state in npcStates.Values)
            {
                if (!state.LevelName.CaselessEq(lvl.name)) continue;

                PlayerBot bot = FindBot(lvl, state.Name);
                if (bot == null)
                { // Bot doesn't exist, let's add a new one at the NPC's position
                    TryAddBot(lvl, state);
                }
                else
                { // Bot exists, let's move it to the NPC's position
                    Position newPos = new Position((state.Position.X * 32) + 16, (state.Position.Y * 32) + 52, (state.Position.Z * 32) + 16);
                    bot.Pos = newPos;
                    bot.TargetPos = bot.Pos;
                    bot.movement = false;
                }
            }
        }

        private void HandleDayNightCycleTick(int time, Season season, DayOfWeek day)
        {
            /* In the future, we can look into seasonal variations in routine. 
             * However, this would require (4 seasons * 7 days * number of NPCs) unique routines,
             * which might be asking too much for a hobby project.
             * 
             * For now, let's just have one seasonal routine (7 days * number of NPCs).
            */

            /*
            switch (season)
            {
                case DayOfWeek.Spring:
                    SpringBehaviour(time, day);
                    break;
                case DayOfWeek.Summer:
                    SummerBehaviour(time, day);
                    break;
                case DayOfWeek.Autumn:
                    AutumnBehaviour(time, day);
                    break;
                case DayOfWeek.Winter:
                    WinterBehaviour(time, day);
                    break;
                default:
                    SpringBehaviour(time, day);
                    break;
            }*/

            SpringBehaviour(time, day);
        }

        private void SpringBehaviour(int time, DayOfWeek day)
        {
            int totalMinutes = ((time + 6000) % 24000) * 60 / 1000;
            int hour = totalMinutes / 60;
            int minute = totalMinutes % 60;

            // TODO: Neaten this area somehow

            if (day == DayOfWeek.Sunday && hour == 6 && minute == 00)
                UpdateNPCState("Pops", "grocery_store", new Position(50, 33, 22), "Sleeping in bed");

            if (day == DayOfWeek.Sunday && hour == 6 && minute == 04)
                UpdateNPCState("Pops", "grocery_store", new Position(50, 32, 20), "Walking to counter", new List<Position> { new Position(34, 32, 20), new Position(34, 32, 30) });

            if (day == DayOfWeek.Sunday && hour == 6 && minute == 25)
                UpdateNPCState("Pops", "grocery_store", new Position(34, 32, 30), "Standing behind counter");

            if (day == DayOfWeek.Sunday && hour == 12 && minute == 45)
                UpdateNPCState("Pops", "grocery_store", new Position(50, 33, 22), "Going to bed");

            if (day == DayOfWeek.Monday && hour == 3 && minute == 00)
                UpdateNPCState("Pops", "grocery_store", new Position(50, 33, 23), "Waking up");
        }

        private void SummerBehaviour(int time, DayOfWeek day)
        {
        }

        private void AutumnBehaviour(int time, DayOfWeek day)
        {
        }

        private void WinterBehaviour(int time, DayOfWeek day)
        {
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

        public static Dictionary<string, NPCState> npcStates = new Dictionary<string, NPCState>();

        public static Dictionary<string, Queue<Position>> botPaths = new Dictionary<string, Queue<Position>>();

        public void UpdateNPCState(string npcName, string levelName, Position position, string taskDescription = null, List<Position> path = null)
        {
            NPCState state = new NPCState(npcName, levelName, position, taskDescription);
            npcStates[npcName] = state;

            // If the NPC's level is loaded, move the NPC to their new position
            Level lvl = LevelInfo.FindExact(levelName);
            if (lvl == null) return;

            PlayerBot bot = FindBot(lvl, state.Name);
            if (bot == null)
            { // Bot doesn't exist, let's add a new one at the NPC's position
                TryAddBot(lvl, state);
            }
            else
            { // Bot exists, let's move it to the NPC's position
                Position newPos = new Position((state.Position.X * 32) + 16, (state.Position.Y * 32) + 52, (state.Position.Z * 32) + 16);
                bot.Pos = newPos;
                bot.TargetPos = bot.Pos;

                // If a target position was specified, force the bot to start moving toward its target position
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

            // Check if the bot has a path and if it's valid (queue of positions)
            Queue<Position> pathQueue;
            if (!NPCRoutines.botPaths.ContainsKey(bot.name) || NPCRoutines.botPaths[bot.name] == null || NPCRoutines.botPaths[bot.name].Count == 0)
                return true; // No path to move, instruction is complete

            pathQueue = NPCRoutines.botPaths[bot.name];
            Position target = pathQueue.Peek();

            // Move bot towards the target
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

                // If there are still more targets to go, set the next target
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
                    return true; // Finished with the instruction
                }
            }

            bot.Pos = pos;
            return false; // Still moving, continue the instruction
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