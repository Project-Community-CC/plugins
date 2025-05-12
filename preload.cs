using System;
using System.Collections.Generic;

namespace MCGalaxy {

    public class PluginReload : Plugin {
        public override string creator { get { return "morgana"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        public override string name { get { return "preload"; } }

        public CmdPluginReload pluginReloadCmd = new CmdPluginReload();
        public CmdPluginRecompile pluginRecompileCmd = new CmdPluginRecompile();

        public override void Load(bool startup) {
            Command.Register(pluginReloadCmd);
            Command.Register(pluginRecompileCmd);
        }
        
        public override void Unload(bool shutdown) {
            Command.Unregister(pluginReloadCmd);
            Command.Unregister(pluginRecompileCmd);
        }

        public class CmdPluginReload : Command2
        {
            public override string name { get { return "preload"; } }
            public override string type { get { return "other"; } }
            public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }

            public override void Help(Player p)
            {
                p.Message("%T/preload pluginname %S- reload plugin name");
            }
        
            public override void Use(Player p, string message)
            {
                string[] args = message.Split(' ');
                
                if (args.Length < 1)
                {
                    Help(p);
                    return;
                }

                Command.Find("Plugin").Use(p, "unload " + args[0]);
                Command.Find("Plugin").Use(p, "load " + args[0]);
            }
        }

        public class CmdPluginRecompile : Command2
        {
            public override string name { get { return "precompile"; } }
            public override string type { get { return "other"; } }
            public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }

            public override void Help(Player p)
            {
                p.Message("%T/precompile pluginname %S- recompile and reload plugin name");
            }
        
            public override void Use(Player p, string message)
            {
                string[] args = message.Split(' ');
                
                if (args.Length < 1)
                {
                    Help(p);
                    return;
                }

                Command.Find("Compile").Use(p, "plugin " + args[0]);
                Command.Find("Plugin").Use(p, "unload " + args[0]);
                Command.Find("Plugin").Use(p, "load " + args[0]);
            }
        }
    }
}