//reference System.dll
//reference System.Core.dll

// TODO:
// - Neaten code
// - Add mineable rocks/geodes
// - Spawn holes under one of the rocks so the player can descend to the next level
// - Maybe some decorative vines/cobwebs on the roof
// - Mossy stone brick noise/cloudy brush on the walls/roof for decoration
// - Dungeon presets: normal, ice, desert, fire?
// - Automatic dungeon generation with random values
// - Scaled sizes: lvl 1 = small, lvl 50 = medium, lvl 100 = big

using System;
using System.Collections.Generic;
using System.Linq;

using MCGalaxy;
using MCGalaxy.Network;

using BlockID = System.UInt16;

namespace Core
{
    public class Dungeon : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override string name { get { return "Dungeon"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdDungeon());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Dungeon"));
        }
    }

    public sealed class CmdDungeon : Command2
    {
        public override string name { get { return "Dungeon"; } }
        public override string type { get { return "information"; } }

        public override void Use(Player p, string message)
        {
            string[] args = message.SplitSpaces();

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

            CorridorFirstDungeonGenerator generator2 = new CorridorFirstDungeonGenerator(p.level, corridorLength, corridorWidth, corridorCount, roomPercent);
            generator2.RunProceduralGeneration();
        }

        public override void Help(Player p)
        {
            p.Message("/dungeon iter length random? corLength corWidth cor# room%");
        }

        public static Position startPosition;
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
        public int corridorLength = 14;
        public int corridorWidth = 5;
        public int corridorCount = 5;
        public float roomPercent = 0.5f; // 0.1 - 1
        public Level lvl = null;

        public CorridorFirstDungeonGenerator(Level _lvl, int _corridorLength, int _corridorWidth, int _corridorCount, float _roomPercent)
        {
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

            TilemapVisualizer.Clear(lvl);
            TilemapVisualizer.PaintFloorTiles(lvl, floorPositions);
            TilemapVisualizer.CreateWalls(lvl, floorPositions);
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
        private static BlockID floorBlock = Block.FromRaw(1);
        //private static BlockID wallBlock = Block.FromRaw(12);

        private static Random random = new Random();

        private static BufferedBlockSender bulk = new BufferedBlockSender();

        public static void PaintFloorTiles(Level lvl, IEnumerable<Position> floorPositions)
        {
            PaintTiles(lvl, floorPositions);
            SendBulkBlockUpdate(lvl);
        }

        private static void PaintTiles(Level lvl, IEnumerable<Position> positions)
        {
            foreach (var position in positions)
            {
                PaintSingleTile(lvl, position);
            }
        }

        private static void PaintSingleTile(Level lvl, Position position)
        {
            int index = lvl.PosToInt((ushort)position.X, (ushort)position.Y, (ushort)position.Z);
            int roofIndex = lvl.PosToInt((ushort)position.X, (ushort)(position.Y + 7), (ushort)position.Z);
            bulk.Add(index, floorBlock);
            bulk.Add(roofIndex, Block.FromRaw(65));
            //lvl.BroadcastChange((ushort)position.X, (ushort)position.Y, (ushort)position.Z, floorBlock);
        }

        public static void PaintSingleBasicWall(Level lvl, Position position, BlockID block)
        {
            int index = lvl.PosToInt((ushort)position.X, (ushort)(position.Y + 1), (ushort)position.Z);
            bulk.Add(index, block);
            //lvl.BroadcastChange((ushort)position.X, (ushort)(position.Y + 1), (ushort)position.Z, wallBlock);
        }

        public static void CreateWalls(Level lvl, HashSet<Position> floorPositions)
        {
            var basicWallPositions = FindWallsInDirections(floorPositions, Direction2D.cardinalDirectionsList);

            HashSet<Position> combinedSet = new HashSet<Position>(floorPositions);
            combinedSet.UnionWith(basicWallPositions);

            var floorAndWallPositions = FindWallsInDirections2(combinedSet, Direction2D.cardinalDirectionsList);

            foreach (var position in basicWallPositions)
            {
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y - 1, position.Z), Block.FromRaw(13));
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 5, position.Z), Block.FromRaw(13));
            }

            foreach (var position in floorAndWallPositions)
            {
                TilemapVisualizer.PaintSingleBasicWall(lvl, position, Block.FromRaw(65));
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 1, position.Z), Block.FromRaw(65));
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 2, position.Z), Block.FromRaw(65));
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 3, position.Z), Block.FromRaw(65));
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 4, position.Z), Block.FromRaw(65));
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 5, position.Z), Block.FromRaw(65));
            }

            /*var stroke = FindWallsInDirections2(floorAndWallPositions, Direction2D.cardinalDirectionsList);
            foreach (var position in stroke)
            {
                TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 5, position.Z), Block.Green);
            }*/

            HashSet<Position> lastSet = new HashSet<Position>(floorAndWallPositions);

            for (int i = 0; i < 2; i++)
            {
                HashSet<Position> newSet = FindWallsInDirections2(lastSet, Direction2D.cardinalDirectionsList);
                lastSet.UnionWith(newSet);

                foreach (var position in lastSet)
                {
                    //TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 4, position.Z), Block.FromRaw(33));

                    /*if (random.NextDouble() < 0.02) // Adjust the probability threshold as needed
                    {
                        TilemapVisualizer.PaintSingleBasicWall(lvl, new Position(position.X, position.Y + 5, position.Z), Block.Pink);
                    }*/
                }
            }

            SendBulkBlockUpdate(lvl);
        }

        private static HashSet<Position> FindWallsInDirections(HashSet<Position> floorPositions, List<Position> directionList)
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

        private static HashSet<Position> FindWallsInDirections2(HashSet<Position> floorAndWallPositions, List<Position> directionList)
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

        public static void Clear(Level lvl)
        {
            for (ushort x = 0; x < lvl.Length; x++)
                for (ushort y = 0; y <= 7; y++)
                    for (ushort z = 0; z < lvl.Width; z++)
                    {
                        //lvl.BroadcastChange(x, (ushort)(1 + y), z, Block.Air);
                        int index = lvl.PosToInt((ushort)x, (ushort)(1 + y), (ushort)z);
                        bulk.Add(index, Block.Air);
                    }

            SendBulkBlockUpdate(lvl);
        }

        private static void SendBulkBlockUpdate(Level lvl)
        {
            bulk.level = lvl;
            bulk.Flush();
        }
    }
}