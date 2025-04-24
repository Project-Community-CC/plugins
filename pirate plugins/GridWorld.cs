using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Levels.IO;
using MCGalaxy.Maths;

namespace MCGalaxy
{
    public class GridWorld : Plugin
    {
        public override string name { get { return "GridWorld"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string creator { get { return "Venk"; } }
        public override bool LoadAtStartup { get { return true; } }

        public override void Load(bool startup)
        {
            OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.Low);
            OnPlayerMoveEvent.Register(HandlePlayerMove, Priority.Normal);

            Command.Register(new CmdGridWorld());
        }

        public override void Unload(bool shutdown)
        {
            OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
            OnPlayerMoveEvent.Unregister(HandlePlayerMove);

            Command.Unregister(Command.Find("GridWorld"));
        }

        private void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            if (!p.Extras.GetBoolean("GRID_WORLD_CHANGING_WORLD")) return;
            if (!p.level.name.CaselessStarts("map")) return;

            p.Extras["GRID_WORLD_CHANGING_WORLD"] = false;

            int x = p.Extras.GetInt("GRID_WORLD_SPAWN_X");
            int y = p.Extras.GetInt("GRID_WORLD_SPAWN_Y");
            int z = p.Extras.GetInt("GRID_WORLD_SPAWN_Z");
            byte yaw = (byte)p.Extras.Get("GRID_WORLD_SPAWN_YAW");
            byte pitch = (byte)p.Extras.Get("GRID_WORLD_SPAWN_PITCH");

            Vec3F32 dir = DirUtils.GetDirVector(yaw, pitch);
            Orientation rot = p.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            Position pos = Position.FromFeetBlockCoords(x, y, z);
            p.SendPosition(pos, rot);
            p.Message("&a" + x + " " + y + " " + z);
        }

        void HandlePlayerMove(Player p, Position next, byte rotX, byte rotY, ref bool cancel)
        {
            if (p.Loading) return;
            if (!p.level.name.CaselessStarts("map")) return;

            string coords = p.level.name.Replace("map", "");
            string[] parts = coords.Split(',');

            int currentMapX, currentMapY;
            if (!int.TryParse(parts[0], out currentMapX) || !int.TryParse(parts[1], out currentMapY)) return;

            int x = p.Pos.X / 32;
            int z = p.Pos.Z / 32;

            string offsetDirection = "none";
            string newMapName = null;

            if (x == 0 && (currentMapX - 1) >= 0)
            {
                newMapName = "map" + (currentMapX - 1) + "," + currentMapY;
                offsetDirection = "north";
            }

            else if (z == (p.level.Length - 1) && (currentMapY - 1) >= 0)
            {
                newMapName = "map" + currentMapX + "," + (currentMapY + 1);
                offsetDirection = "east";
            }

            else if (x == (p.level.Width - 1) && (currentMapX + 1) <= 16)
            {
                newMapName = "map" + (currentMapX + 1) + "," + currentMapY;
                offsetDirection = "south";
            }

            else if (z == 0 && (currentMapY + 1) <= 16)
            {
                newMapName = "map" + currentMapX + "," + (currentMapY - 1);
                offsetDirection = "west";
            }

            else return;

            string map = Matcher.FindMaps(p, newMapName);
            if (map == null) return;

            string path = LevelInfo.MapPath(map);
            Vec3U16 dims = IMapImporter.GetFor(path).ReadDimensions(path);
            MapInfo info = new MapInfo();
            info.FromMap(map);

            int width = dims.X;
            int length = dims.Z;

            int offsetDistance = 5; // How many blocks to spawn from the map border
            int dx = x, dz = z;

            if (offsetDirection == "north") dx = width - offsetDistance;
            if (offsetDirection == "east") dz = 0 + offsetDistance;
            if (offsetDirection == "south") dx = 0 + offsetDistance;
            if (offsetDirection == "west") dz = length - offsetDistance;

            p.Extras["GRID_WORLD_SPAWN_X"] = dx;
            p.Extras["GRID_WORLD_SPAWN_Y"] = info.Get(EnvProp.EdgeLevel) + 1;
            p.Extras["GRID_WORLD_SPAWN_Z"] = dz;

            p.Extras["GRID_WORLD_SPAWN_YAW"] = p.Rot.RotY;
            p.Extras["GRID_WORLD_SPAWN_PITCH"] = p.Rot.HeadX;

            p.Loading = true;
            cancel = true;

            p.Extras["GRID_WORLD_CHANGING_WORLD"] = true;
            PlayerActions.ChangeMap(p, newMapName);
            p.Message("  &2Now entering: &7" + newMapName);

            p.Message("&d" + dx + " " + (info.Get(EnvProp.EdgeLevel) + 1) + " " + dz);
        }

        class MapInfo
        {
            public ushort Width, Height, Length;
            public string Name, MapName;
            public AccessController Visit, Build;
            public LevelConfig Config;

            public void FromMap(string map)
            {
                this.Name = map; MapName = map;
                string path = LevelInfo.MapPath(map);
                Vec3U16 dims = IMapImporter.GetFor(path).ReadDimensions(path);

                Width = dims.X; Height = dims.Y; Length = dims.Z;

                path = LevelInfo.PropsPath(map);
                LevelConfig cfg = new LevelConfig();
                cfg.Load(path);

                Config = cfg;
                Visit = new LevelAccessController(cfg, map, true);
                Build = new LevelAccessController(cfg, map, false);
            }

            public int Get(EnvProp i)
            {
                int value = Config.GetEnvProp(i);
                bool block = i == EnvProp.EdgeBlock || i == EnvProp.SidesBlock;
                int default_ = block ? Block.Invalid : EnvConfig.ENV_USE_DEFAULT;
                return value != default_ ? value : EnvConfig.DefaultEnvProp(i, Height);
            }
        }
    }

    public class CmdGridWorld : Command2
    {
        public override string name { get { return "GridWorld"; } }
        public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }

        public override void Use(Player p, string message)
        {
            if (message.Length == 0)
            {
                Help(p);
                return;
            }

            string[] args = message.SplitSpaces();

            if (args[0].CaselessEq("generate") && args.Length >= 2)
            {
                string sourceLevel = args[1];
                if (LevelInfo.FindExact(sourceLevel) == null)
                {
                    p.Message("&cInvalid source level.");
                    return;
                }

                for (int x = 0; x < 26; x++)
                {
                    for (int y = 0; y < 26; y++)
                    {
                        string mapName = "map" + x + "," + y;
                        Command.Find("CopyLvl").Use(p, sourceLevel + " " + mapName);
                    }
                }

                p.Message("Generated grid world.");
            }

            else if (args[0].CaselessEq("delete"))
            {
                for (int x = 0; x < 26; x++)
                {
                    for (int y = 0; y < 26; y++)
                    {
                        string mapName = "map" + x + "," + y;
                        Command.Find("DeleteLvl").Use(p, mapName);
                    }
                }

                p.Message("Deleted grid world.");
            }

            else if (args[0].CaselessEq("map"))
            {
                if (!p.level.name.CaselessStarts("map"))
                {
                    p.Message("&cYou are not in a grid world.");
                    return;
                }

                string coords = p.level.name.Replace("map", "");
                string[] parts = coords.Split(',');

                int currentMapX, currentMapY;
                if (!int.TryParse(parts[0], out currentMapX) || !int.TryParse(parts[1], out currentMapY)) return;

                p.Message("   &70 1 2 3 4 5 6 7 8 9");

                for (int y = 0; y < 26; y++)
                {
                    string row = y < 10 ? "&7" + y + " " : "&9   ";

                    for (int x = 0; x < 26; x++)
                    {
                        if (x == currentMapX && y == currentMapY)
                        {
                            row += "&f☻"; // Player position
                        }
                        else
                        {
                            row += "&9█";
                        }
                    }

                    p.Message(row);
                }

            }

            else
            {
                Help(p);
                return;
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/GridWorld generate [source level] &S- Turns [source level] into several grid world copies.");
            p.Message("&T/GridWorld delete &S- Deletes the grid world.");
            p.Message("&T/GridWorld map &S- Shows where you are in the grid world.");
        }
    }
}
