//reference System.Core.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;
using MCGalaxy.SQL;

namespace ProjectCommunity
{
    public class Plots : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "Plots"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdFarm());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Farm"));
        }
    }

    public sealed class CmdFarm : Command2
    {
        public override string name { get { return "Farm"; } }
        public override string type { get { return CommandTypes.World; } }

        void HandleCreate(Player p)
        {
            if (LevelInfo.MapExists("farm-" + p.name.ToLower()))
            {
                p.Message("You already have a farm. Type %b/Farm go %Sto go to it.");
                return;
            }

            else
            {
                string dst = "farm-" + p.name.ToLower();
                string src = "farm";

                src = Matcher.FindMaps(Player.Console, src);
                if (src == null) return;

                try
                {
                    if (!LevelActions.Copy(Player.Console, src, dst)) return;
                }

                catch (IOException)
                {
                    p.Message("%cThere was a problem when executing that command.");
                    return;
                }

                LevelActions.Load(Player.Console, dst, false);

                Level lvl = Matcher.FindLevels(Player.Console, dst);
                if (lvl == null) return;

                Command.Find("Goto").Use(p, dst);

                // Build-whitelist them in all of the zones
                for (int i = 0; i < 8; i++)
                {
                    Zone zone = null;
                    if (i == 0) zone = Matcher.FindZones(Player.Console, lvl, "build");
                    else if (i == 1) continue;
                    else zone = Matcher.FindZones(Player.Console, lvl, "build" + i);
                    if (zone == null) continue;
                    zone.Access.Whitelist(Player.Console, LevelPermission.Owner, lvl, p.name);
                }

                p.Message("%aWorld created successfully. Type %b/Farm go %ato visit.");
            }
        }

        void HandleGo(Player p, string[] args)
        {
            if (args.Length == 1)
            {
                if (LevelInfo.MapExists("farm-" + p.name.ToLower()))
                    PlayerActions.ChangeMap(p, "farm-" + p.name.ToLower());

                else p.Message("You do not have a farm. You can make one with %b/Farm create%S.");
            }

            else
            {
                if (LevelInfo.MapExists("farm-" + args[1].ToLower()))
                    PlayerActions.ChangeMap(p, "farm-" + args[1].ToLower());

                else p.Message("That user does not have a farm.");
            }
        }

        public override void Use(Player p, string message)
        {
            string[] args = message.SplitSpaces();

            if (message.Length == 0)
            {
                Help(p);
                return;
            }

            if (args[0].CaselessEq("create")) HandleCreate(p);
            else if (args[0].CaselessEq("go")) HandleGo(p, args);

            else Help(p);
        }

        public override void Help(Player p)
        {
            p.Message("%T/Farm create %H- Creates a new farm.");
            p.Message("%T/Farm go <name> %H- Visits your farm.");
            p.Message("%HIf <name> is given, you will visit their farm instead.");
        }
    }
}