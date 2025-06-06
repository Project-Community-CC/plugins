//reference System.dll
//reference System.Core.dll

// TODO:
// - Neaten code
// - Add mineable rocks/geodes
// - Add decorative vines/cobwebs on the roof
// - Stalagmites/stalactites
// - Mossy stone brick noise/cloudy brush on the walls/roof for decoration?
// - Dungeon presets: normal, ice, desert, fire?
// - Monsters?

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

using BlockID = System.UInt16;

namespace ProjectCommunity
{
    public class Dungeon : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "Dungeon"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdDungeon());
            OnPlayerClickEvent.Register(HandleBlockClick, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Dungeon"));
            OnPlayerClickEvent.Unregister(HandleBlockClick);
        }

        void HandleBlockClick(Player p, MouseButton button, MouseAction action, ushort yaw, ushort pitch, byte entity, ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            if (!p.level.name.CaselessEq("dungeon")) return;

            if (p.Extras.Contains("DUNGEON_EXIT_LADDER_POSITION"))
            {
                string[] exitLadderPosition = p.Extras.GetString("DUNGEON_EXIT_LADDER_POSITION").Split(';');
                int ladderX = int.Parse(exitLadderPosition[0]);
                // No need for Y here
                int ladderZ = int.Parse(exitLadderPosition[2]);

                if (x == ladderX && z == ladderZ)
                {
                    p.Message("Clicked on exit ladder.");
                    p.Extras.Remove("DUNGEON_LEVEL");
                    Command.Find("Main").Use(p, "");
                }
            }

            if (p.Extras.Contains("DUNGEON_DESCEND_LADDER_POSITION"))
            {
                string[] descendLadderPosition = p.Extras.GetString("DUNGEON_DESCEND_LADDER_POSITION").Split(';');
                int ladderX = int.Parse(descendLadderPosition[0]);
                int ladderY = int.Parse(descendLadderPosition[1]);
                int ladderZ = int.Parse(descendLadderPosition[2]);

                if (x == ladderX && y == ladderY && z == ladderZ)
                {
                    p.Message("Clicked on descend ladder.");
                    int level = p.Extras.Contains("DUNGEON_LEVEL") ? (int)p.Extras["DUNGEON_LEVEL"] : 1;
                    level++;
                    p.Extras["DUNGEON_LEVEL"] = level;
                    p.Message("Descending to dungeon level " + level + "...");

                    CmdDungeon.GenerateDungeonForPlayer(p, level);
                }
            }
        }

        public class FadeData
        {
            public Player p;
            public bool isFadingFrom;
            public float transparency;

            public FadeData(Player _p, bool _isFadingFrom, float _transparency)
            {
                p = _p;
                isFadingFrom = _isFadingFrom;
                transparency = _transparency;
            }
        }

        private static void FadeCallback(SchedulerTask task)
        {
            FadeData args = (FadeData)task.State;

            if (args.transparency <= 0 && args.isFadingFrom)
            {
                task.Repeating = false;
                return;
            }

            if (args.transparency >= 1 && !args.isFadingFrom)
            {
                task.Repeating = false;
                return;
            }

            if (args.isFadingFrom) args.transparency -= 0.05f;
            else args.transparency += 0.05f;

            CinematicGui gui = new CinematicGui();
            gui.hideCrosshair = false;
            gui.hideHand = false;
            gui.hideHotbar = false;
            gui.barSize = 1;
            gui.barColor = new ColorDesc(0, 0, 0);
            gui.barColor.A = (byte)(255 * args.transparency);

            if (!args.p.Session.SendCinematicGui(gui)) task.Repeating = false;
        }

        public static void FadeToBlack(Player p, Action callback)
        {
            FadeData data = new FadeData(p, false, 0f);
            SchedulerTask task = new SchedulerTask(t =>
            {
                FadeData args = (FadeData)t.State;

                if (args.transparency >= 1)
                {
                    t.Repeating = false;
                    if (callback != null) callback();
                    return;
                }

                args.transparency += 0.05f;

                CinematicGui gui = new CinematicGui();
                gui.hideCrosshair = false;
                gui.hideHand = false;
                gui.hideHotbar = false;
                gui.barSize = 1;
                gui.barColor = new ColorDesc(0, 0, 0);
                gui.barColor.A = (byte)(255 * args.transparency);

                if (!args.p.Session.SendCinematicGui(gui)) t.Repeating = false;
            }, data, TimeSpan.FromMilliseconds(20), true);

            p.CriticalTasks.Add(task);
        }


        public static void FadeFromBlack(Player p)
        {
            FadeData data = new FadeData(p, true, 1f); // isFadingFrom = true, starting from full black

            SchedulerTask task = new SchedulerTask(t =>
            {
                FadeData args = (FadeData)t.State;

                if (args.transparency <= 0)
                {
                    t.Repeating = false;
                    return;
                }

                args.transparency -= 0.05f;

                CinematicGui gui = new CinematicGui();
                gui.hideCrosshair = false;
                gui.hideHand = false;
                gui.hideHotbar = false;
                gui.barSize = 1;
                gui.barColor = new ColorDesc(0, 0, 0);
                gui.barColor.A = (byte)(255 * args.transparency);

                if (!args.p.Session.SendCinematicGui(gui)) t.Repeating = false;
            }, data, TimeSpan.FromMilliseconds(20), true);

            p.CriticalTasks.Add(task);
        }
    }

    public sealed class CmdDungeon : Command2
    {
        public override string name { get { return "Dungeon"; } }
        public override string type { get { return "information"; } }

        public override void Use(Player p, string message)
        {
            p.Extras["DUNGEON_LEVEL"] = 1;
            GenerateDungeonForPlayer(p, 1);

            /*string[] args = message.SplitSpaces();

            if (args.Length < 7)
            {
                Help(p);
                return;
            }

            int iterations = int.Parse(args[0]);
            int length = int.Parse(args[1]);
            bool random = bool.Parse(args[2]);
            int corridorLength = int.Parse(args[3]);
            int corridorWidth = int.Parse(args[4]);
            int corridorCount = int.Parse(args[5]);
            float roomPercent = float.Parse(args[6]);

            startPosition = new Position(p.level.Length / 2, 1, p.level.Width / 2);

            SimpleRandomWalkDungeonGenerator generator = new SimpleRandomWalkDungeonGenerator(p.level, iterations, length, random);
            //generator.RunProceduralGeneration();

            CorridorFirstDungeonGenerator generator2 = new CorridorFirstDungeonGenerator(p, p.level, corridorLength, corridorWidth, corridorCount, roomPercent);
            generator2.RunProceduralGeneration();*/
        }

        public override void Help(Player p)
        {
            p.Message("/dungeon iter length random? corLength corWidth cor# room%");
        }

        public static Position startPosition;

        public struct DungeonSettings
        {
            public int Iterations, Length, CorridorLength, CorridorWidth, CorridorCount;
            public float RoomPercent;
            public bool Random;

            public DungeonSettings(int i, int l, bool r, int cl, int cw, int cc, float rp)
            {
                Iterations = i; Length = l; Random = r;
                CorridorLength = cl; CorridorWidth = cw; CorridorCount = cc; RoomPercent = rp;
            }
        }

        private static DungeonSettings GetSettingsForLevel(int level)
        {
            if (level <= 5) return new DungeonSettings(15, 30, true, 5, 4, 2, 100); // tiny
            if (level <= 10) return new DungeonSettings(15, 30, true, 10, 4, 9, 80); // small
            if (level <= 20) return new DungeonSettings(15, 30, true, 12, 6, 9, 8);  // medium
            return new DungeonSettings(15, 30, true, 24, 8, 9, 8);                   // large
        }

        private static void test(Player p, int level)
        {
            DungeonSettings s = GetSettingsForLevel(level);
            CmdDungeon.startPosition = new Position(p.level.Length / 2, 1, p.level.Width / 2);

            var generator = new CorridorFirstDungeonGenerator(
                p, p.level, s.CorridorLength, s.CorridorWidth, s.CorridorCount, s.RoomPercent);
            generator.RunProceduralGeneration();
        }

        public static void GenerateDungeonForPlayer(Player p, int level)
        {
            Dungeon.FadeToBlack(p, delegate
            {
                test(p, level);
            });
        }
    }

    public class ProceduralGenerationAlgorithms
    {
        public static HashSet<Position> SimpleRandomWalk(Position startPosition, int walkLength)
        {
            HashSet<Position> path = new HashSet<Position>();
            path.Add(startPosition);
            var previousPosition = startPosition;

            for (int i = 0; i < walkLength; i++)
            {
                Position direction = Direction2D.GetRandomCardinalDirection();
                Position newPosition = new Position(previousPosition.X + direction.X, previousPosition.Y + direction.Y, previousPosition.Z + direction.Z);
                path.Add(newPosition);
                previousPosition = newPosition;
            }

            return path;
        }

        public static List<Position> RandomWalkCorridor(Position startPosition, int corridorLength)
        {
            List<Position> corridor = new List<Position>();
            var direction = Direction2D.GetRandomCardinalDirection();
            var currentPosition = startPosition;
            corridor.Add(currentPosition);

            for (int i = 0; i < corridorLength; i++)
            {
                currentPosition = new Position(currentPosition.X + direction.X, currentPosition.Y + direction.Y, currentPosition.Z + direction.Z);
                corridor.Add(currentPosition);
            }

            return corridor;
        }
    }

    public class CorridorFirstDungeonGenerator
    {
        public Player player = null;
        public int corridorLength = 14;
        public int corridorWidth = 5;
        public int corridorCount = 5;
        public float roomPercent = 0.5f; // 0.1 - 1
        public Level lvl = null;

        public CorridorFirstDungeonGenerator(Player _player, Level _lvl, int _corridorLength, int _corridorWidth, int _corridorCount, float _roomPercent)
        {
            player = _player;
            corridorLength = _corridorLength;
            corridorWidth = _corridorWidth;
            corridorCount = _corridorCount;
            roomPercent = _roomPercent;
            lvl = _lvl;
        }

        public void RunProceduralGeneration()
        {
            CorridorFirstGeneration();
        }

        private void CorridorFirstGeneration()
        {
            HashSet<Position> floorPositions = new HashSet<Position>();
            HashSet<Position> potentialRoomPositions = new HashSet<Position>();

            List<List<Position>> corridors = CreateCorridors(floorPositions, potentialRoomPositions);

            HashSet<Position> roomPositions = CreateRooms(potentialRoomPositions);

            List<Position> deadEnds = FindAllDeadEnds(floorPositions);

            CreateRoomsAtDeadEnd(deadEnds, roomPositions);

            floorPositions.UnionWith(roomPositions);

            for (int i = 0; i < corridors.Count; i++)
            {
                //corridors[i] = IncreaseCorridorSizeByOne(corridors[i]);
                corridors[i] = IncreaseCorridorBrush3by3(corridors[i]);
                //corridors[i] = IncreaseCorridorBrush3by3(corridors[i]);
                //corridors[i] = IncreaseCorridorBrush3by3(corridors[i]);
                floorPositions.UnionWith(corridors[i]);
            }

            TilemapVisualizer visualiser = new TilemapVisualizer(player);
            visualiser.Clear();
            visualiser.PaintFloorTiles(floorPositions);
            visualiser.CreateWalls(floorPositions);
            visualiser.ScatterRocks(floorPositions);

            Position spawnPos = new Position((visualiser.spawnPosition.X * 32) + 16, (visualiser.spawnPosition.Y * 32) + 128, (visualiser.spawnPosition.Z * 32) + 16);
            player.SendPosition(spawnPos, player.Rot);

            Thread.Sleep(2300); // TODO: Make Dungeon.FadeFromBlack(player); wait until everything has been finished.
                                // Without this Thread.Sleep(2300), there is no smooth FadeFromBlack gradiant
            Dungeon.FadeFromBlack(player);
        }

        public List<Position> IncreaseCorridorBrush3by3(List<Position> corridor)
        {
            List<Position> newCorridor = new List<Position>();

            for (int i = 1; i < corridor.Count; i++)
            {
                for (int x = -1; x < (corridorWidth - 1); x++)
                {
                    for (int z = -1; z < (corridorWidth - 1); z++)
                    {

                        newCorridor.Add(new Position(corridor[i - 1].X + x, corridor[i - 1].Y + 0, corridor[i - 1].Z + z));
                    }
                }
            }

            return newCorridor;
        }

        public List<Position> IncreaseCorridorSizeByOne(List<Position> corridor)
        {
            List<Position> newCorridor = new List<Position>();
            Position previousDirection = new Position(0, 0, 0);

            for (int i = 1; i < corridor.Count; i++)
            {
                Position directionFromCell = new Position(corridor[i].X - corridor[i - 1].X,
                    corridor[i].Y - corridor[i - 1].Y,
                    corridor[i].Z - corridor[i - 1].Z);

                if (previousDirection != new Position(0, 0, 0) &&
                    directionFromCell != previousDirection)
                {
                    // Handle corner
                    for (int x = -1; x < 2; x++)
                    {
                        for (int z = -1; z < 2; z++)
                        {

                            newCorridor.Add(new Position(corridor[i - 1].X + x, corridor[i - 1].Y + 0, corridor[i - 1].Z + z));
                        }
                    }

                    previousDirection = directionFromCell;
                }

                else
                {
                    // Add a single cell in the direction + 90 degrees
                    Position newCorridorTileOffset = GetDirection90From(directionFromCell);
                    newCorridor.Add(corridor[i - 1]);
                    newCorridor.Add(new Position(corridor[i - 1].X + newCorridorTileOffset.X,
                        corridor[i - 1].Y + newCorridorTileOffset.Y,
                        corridor[i - 1].Z + newCorridorTileOffset.Z));

                    previousDirection = directionFromCell;
                }
            }

            return newCorridor;
        }

        private Position GetDirection90From(Position direction)
        {
            Position down = new Position(0, 0, -1);
            Position up = new Position(0, 0, 1);
            Position left = new Position(1, 0, 0);
            Position right = new Position(-1, 0, 0);

            if (direction == up) return right;
            if (direction == right) return down;
            if (direction == down) return left;
            if (direction == left) return up;

            return new Position(0, 0, 0);
        }

        private void CreateRoomsAtDeadEnd(List<Position> deadEnds, HashSet<Position> roomFloors)
        {
            foreach (var position in deadEnds)
            {
                if (!roomFloors.Contains(position))
                {
                    var room = SimpleRandomWalkDungeonGenerator.RunRandomWalk(position);
                    roomFloors.UnionWith(room);
                }
            }
        }

        private List<Position> FindAllDeadEnds(HashSet<Position> floorPositions)
        {
            List<Position> deadEnds = new List<Position>();

            foreach (Position position in floorPositions)
            {
                int neighboursCount = 0;
                foreach (var direction in Direction2D.cardinalDirectionsList)
                {
                    Position newPosition = new Position(position.X + direction.X, position.Y + direction.Y, position.Z + direction.Z);
                    if (floorPositions.Contains(newPosition))
                        neighboursCount++;
                }

                if (neighboursCount == 1)
                    deadEnds.Add(position);
            }

            return deadEnds;
        }

        private HashSet<Position> CreateRooms(HashSet<Position> potentialRoomPositions)
        {
            HashSet<Position> roomPositions = new HashSet<Position>();
            int roomsToCreateCount = (int)(potentialRoomPositions.Count * roomPercent);

            List<Position> roomsToCreate = potentialRoomPositions.OrderBy(x => Guid.NewGuid()).Take(roomsToCreateCount).ToList();

            foreach (var roomPosition in roomsToCreate)
            {
                var roomFloor = SimpleRandomWalkDungeonGenerator.RunRandomWalk(roomPosition);
                roomPositions.UnionWith(roomFloor);
            }

            return roomPositions;
        }

        private List<List<Position>> CreateCorridors(HashSet<Position> floorPositions, HashSet<Position> potentialRoomPositions)
        {
            var currentPosition = CmdDungeon.startPosition;
            potentialRoomPositions.Add(currentPosition);
            List<List<Position>> corridors = new List<List<Position>>();

            for (int i = 0; i < corridorCount; i++)
            {
                var corridor = ProceduralGenerationAlgorithms.RandomWalkCorridor(currentPosition, corridorLength);
                corridors.Add(corridor);
                currentPosition = corridor[corridor.Count - 1];
                potentialRoomPositions.Add(currentPosition);
                floorPositions.UnionWith(corridor);
            }

            return corridors;
        }
    }

    public static class Direction2D
    {
        private static readonly Random random = new Random();

        public static List<Position> cardinalDirectionsList = new List<Position>
        {
            new Position(0, 0, 1), // Up
            new Position(1, 0, 0), // Right
            new Position(0, 0, -1), // Down
            new Position(-1, 0, 0), // Up
        };

        public static Position GetRandomCardinalDirection()
        {
            return cardinalDirectionsList[random.Next(0, cardinalDirectionsList.Count)];
        }
    }

    public class SimpleRandomWalkDungeonGenerator
    {
        public static int iterations = 20;
        public static int walkLength = 20;
        public static bool startRandomlyEachIteration = true;
        public static Level lvl;

        public SimpleRandomWalkDungeonGenerator(Level _lvl, int _iterations, int _walkLength, bool _startRandomlyEachIteration)
        {
            iterations = _iterations;
            walkLength = _walkLength;
            startRandomlyEachIteration = _startRandomlyEachIteration;
            lvl = _lvl;
        }

        public virtual void RunProceduralGeneration()
        {
            HashSet<Position> floorPositions = RunRandomWalk(CmdDungeon.startPosition);

            //TilemapVisualizer.Clear();
            //TilemapVisualizer.PaintFloorTiles(floorPositions);
            //TilemapVisualizer.CreateWalls(floorPositions);
        }

        public static HashSet<Position> RunRandomWalk(Position position)
        {
            var currentPosition = position;
            HashSet<Position> floorPositions = new HashSet<Position>();

            Random random = new Random();
            for (int i = 0; i < iterations; i++)
            {
                var path = ProceduralGenerationAlgorithms.SimpleRandomWalk(currentPosition, walkLength);
                floorPositions.UnionWith(path);

                if (startRandomlyEachIteration)
                {
                    currentPosition = floorPositions.ElementAt(random.Next(0, floorPositions.Count));
                }
            }

            return floorPositions;
        }
    }

    public class TilemapVisualizer
    {
        public Player player = null;
        private BlockID floorBlock = Block.Stone;

        private Random random = new Random();

        private BufferedBlockSender bulk;
        public Position spawnPosition = new Position(0, 0, 0);
        private Level lvl = null;

        public TilemapVisualizer(Player _player)
        {
            player = _player;
            lvl = _player.level;
            bulk = new BufferedBlockSender(_player);
        }

        public void PaintFloorTiles(IEnumerable<Position> floorPositions)
        {
            PaintTiles(floorPositions);
            SendBulkBlockUpdate();
        }

        private void PaintTiles(IEnumerable<Position> positions)
        {
            foreach (var position in positions)
            {
                PaintSingleTile(position);
            }
        }

        private void PaintSingleTile(Position position)
        {
            int index = lvl.PosToInt((ushort)position.X, (ushort)position.Y, (ushort)position.Z);
            int roofIndex = lvl.PosToInt((ushort)position.X, (ushort)(position.Y + 7), (ushort)position.Z);
            bulk.Add(index, floorBlock);
            bulk.Add(roofIndex, Block.Gravel);
            //lvl.BroadcastChange((ushort)position.X, (ushort)position.Y, (ushort)position.Z, floorBlock);
        }

        public void PaintSingleBasicWall(Position position, BlockID block)
        {
            int index = lvl.PosToInt((ushort)position.X, (ushort)(position.Y + 1), (ushort)position.Z);
            bulk.Add(index, block);
            //lvl.BroadcastChange((ushort)position.X, (ushort)(position.Y + 1), (ushort)position.Z, wallBlock);
        }

        public void CreateWalls(HashSet<Position> floorPositions)
        {
            var basicWallPositions = FindWallsInDirections(floorPositions, Direction2D.cardinalDirectionsList);

            HashSet<Position> combinedSet = new HashSet<Position>(floorPositions);
            combinedSet.UnionWith(basicWallPositions);

            var floorAndWallPositions = FindWallsInDirections2(combinedSet, Direction2D.cardinalDirectionsList);

            foreach (var position in basicWallPositions)
            {
                PaintSingleBasicWall(new Position(position.X, position.Y - 1, position.Z), Block.Gravel);
                PaintSingleBasicWall(new Position(position.X, position.Y + 5, position.Z), Block.Gravel);
            }

            foreach (var position in floorAndWallPositions)
            {
                PaintSingleBasicWall(position, Block.Gravel);
                PaintSingleBasicWall(new Position(position.X, position.Y + 1, position.Z), Block.Gravel);
                PaintSingleBasicWall(new Position(position.X, position.Y + 2, position.Z), Block.Gravel);
                PaintSingleBasicWall(new Position(position.X, position.Y + 3, position.Z), Block.Gravel);
                PaintSingleBasicWall(new Position(position.X, position.Y + 4, position.Z), Block.Gravel);
                PaintSingleBasicWall(new Position(position.X, position.Y + 5, position.Z), Block.Gravel);
            }

            /*var stroke = FindWallsInDirections2(floorAndWallPositions, Direction2D.cardinalDirectionsList);
            foreach (var position in stroke)
            {
                PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 5, position.Z), Block.Green);
            }*/

            HashSet<Position> lastSet = new HashSet<Position>(floorAndWallPositions);

            for (int i = 0; i < 2; i++)
            {
                HashSet<Position> newSet = FindWallsInDirections2(lastSet, Direction2D.cardinalDirectionsList);
                lastSet.UnionWith(newSet);

                foreach (var position in lastSet)
                {
                    //PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 4, position.Z), Block.FromRaw(33));

                    /*if (random.NextDouble() < 0.02) // Adjust the probability threshold as needed
                    {
                        PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 5, position.Z), Block.Pink);
                    }*/
                }
            }

            SendBulkBlockUpdate();
        }

        private HashSet<Position> FindWallsInDirections(HashSet<Position> floorPositions, List<Position> directionList)
        {
            HashSet<Position> wallPositions = new HashSet<Position>();

            foreach (var position in floorPositions)
            {
                foreach (var direction in directionList)
                {
                    Position neighbourPosition = new Position(position.X + direction.X, position.Y + direction.Y, position.Z + direction.Z);

                    if (!floorPositions.Contains(neighbourPosition))
                    {
                        wallPositions.Add(neighbourPosition);
                    }
                }
            }

            return wallPositions;
        }

        private HashSet<Position> FindWallsInDirections2(HashSet<Position> floorAndWallPositions, List<Position> directionList)
        {
            HashSet<Position> newWallPositions = new HashSet<Position>();

            foreach (var position in floorAndWallPositions)
            {
                foreach (var direction in directionList)
                {
                    Position neighbourPosition = new Position(position.X + direction.X, position.Y + direction.Y, position.Z + direction.Z);

                    if (!floorAndWallPositions.Contains(neighbourPosition))
                    {
                        newWallPositions.Add(neighbourPosition);
                    }
                }
            }

            return newWallPositions;
        }

        public void Clear()
        {
            for (ushort x = 0; x < lvl.Length; x++)
                for (ushort y = 0; y <= 7; y++)
                    for (ushort z = 0; z < lvl.Width; z++)
                    {
                        //lvl.BroadcastChange(x, (ushort)(1 + y), z, Block.Air);
                        int index = lvl.PosToInt((ushort)x, (ushort)(1 + y), (ushort)z);
                        bulk.Add(index, Block.Air);
                    }

            SendBulkBlockUpdate();
        }

        public void ScatterRocks(HashSet<Position> floorPositions)
        {
            BlockID[] rockTypes = new BlockID[] { Block.Red, Block.Orange, Block.Yellow };
            Random rnd = new Random();

            List<Position> rockPositions = new List<Position>();
            List<Position> spawnPositions = new List<Position>();

            foreach (var pos in floorPositions)
            {
                if (rnd.NextDouble() < 0.1) // 10% chance to place a rock
                {
                    BlockID rock = rockTypes[rnd.Next(rockTypes.Length)];
                    int rockIndex = lvl.PosToInt((ushort)pos.X, (ushort)(pos.Y + 1), (ushort)pos.Z);
                    bulk.Add(rockIndex, rock);
                    rockPositions.Add(pos);
                }
            }

            if (floorPositions.Count > 0)
            {
                spawnPosition = floorPositions.ElementAt(rnd.Next(floorPositions.Count)); // Generate random spawn position

                if (IsPositionEmpty(spawnPosition))
                {
                    // Place the exit ladder next to the spawn
                    Position ladderPos = GetNextEmptyPosition(spawnPosition);
                    player.Extras["DUNGEON_EXIT_LADDER_POSITION"] = ladderPos.X + ";" + ladderPos.Y + ";" + ladderPos.Z;
                    if (ladderPos != null)
                    {
                        for (int i = 1; i < 8; i++)
                        {
                            int ladderIndex = lvl.PosToInt((ushort)ladderPos.X, (ushort)(ladderPos.Y + i), (ushort)ladderPos.Z);
                            if (i == 7) bulk.Add(ladderIndex, Block.FromRaw(106)); // Hole above the exit ladder appears as a black block
                            else bulk.Add(ladderIndex, Block.FromRaw(159)); // Exit ladder blocks
                        }
                    }

                    spawnPositions.Add(spawnPosition);
                }
            }

            // Place a ladder under a random rock
            if (rockPositions.Count > 0)
            {
                Position ladderPos = rockPositions[rnd.Next(rockPositions.Count)];
                int ladderIndex = lvl.PosToInt((ushort)ladderPos.X, (ushort)(ladderPos.Y), (ushort)ladderPos.Z);
                int holeIndex = lvl.PosToInt((ushort)ladderPos.X, (ushort)(ladderPos.Y - 1), (ushort)ladderPos.Z);

                bulk.Add(ladderIndex, Block.FromRaw(159)); // Ladder block
                bulk.Add(holeIndex, Block.FromRaw(106)); // Block hole
                player.Extras["DUNGEON_DESCEND_LADDER_POSITION"] = ladderPos.X + ";" + ladderPos.Y + ";" + ladderPos.Z;
            }

            SendBulkBlockUpdate();
        }

        private bool IsPositionEmpty(Position pos)
        {
            int index = lvl.PosToInt((ushort)pos.X, (ushort)(pos.Y + 1), (ushort)pos.Z);
            return lvl.FastGetBlock(index) == Block.Air;
        }

        private Position GetNextEmptyPosition(Position pos)
        {
            // Try all cardinal directions around the position for an empty space
            foreach (var direction in Direction2D.cardinalDirectionsList)
            {
                Position newPos = new Position(pos.X + direction.X, pos.Y, pos.Z + direction.Z);
                if (IsPositionEmpty(newPos))
                {
                    return newPos;
                }
            }

            // No valid positions
            return new Position(-1, -1, -1);
        }

        private void SendBulkBlockUpdate()
        {
            bulk.level = lvl;
            bulk.Flush();
        }
    }
}