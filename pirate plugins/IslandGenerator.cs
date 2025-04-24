using System;
using LibNoise;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Generator;
using MCGalaxy.Generator.Foliage;
using MCGalaxy.Maths;
using BlockID = System.UInt16;

namespace MCGalaxy
{
    public class PerlinIslandGenerator : Plugin
    {
        public override string name { get { return "IslandGenerator"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        public override string creator { get { return "Venk"; } }

        MapGen generator;
        static Tree palm;

        public override void Load(bool startup)
        {
            string help = "&HGenerates a beach with sand, water, and inland terrain with dropoff.";
            generator = new MapGen() { Theme = "Perlin2", GenFunc = GenerateIsland, Desc = help, Type = GenType.Advanced };
            MapGen.Generators.Add(generator);

            Command.Register(new CmdPerlin());

            palm = new BetterPalmTree();
            Tree.TreeTypes.Add("BetterPalm", () => palm);
        }

        public override void Unload(bool shutdown)
        {
            MapGen.Generators.Remove(generator);

            Command.Unregister(Command.Find("Perlin"));
            Tree.TreeTypes.Remove("BetterPalm");
        }

        static bool GenerateIsland(Player p, Level lvl, MapGenArgs args)
        {
            Perlin2 module = new Perlin2();
            return Gen2D(p, lvl, module, args);
        }

        static bool Gen2D(Player p, Level lvl, IModule module, MapGenArgs args)
        {
            int width = lvl.Width, length = lvl.Length, half = lvl.Height / 2;
            int waterHeight = half - 1;
            module.Frequency = 1 / 100.0;

            if (!args.ParseArgs(p)) return false;
            module.Seed = args.Seed;
            MapGenBiome biome = MapGenBiome.Get(args.Biome);

            int centerX = width / 2;
            int centerZ = length / 2;
            double maxDistance = Math.Sqrt(centerX * centerX + centerZ * centerZ);

            for (int z = 0; z < length; ++z)
                for (int x = 0; x < width; ++x)
                {
                    double noise = module.GetValue(x, 10, z);
                    double distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    double dropoff = 1.2 - (distance / (maxDistance * 1.5));
                    dropoff = Math.Max(0, dropoff);

                    int dirtHeight = (int)Math.Floor((noise * 10 + half) * dropoff);

                    if (dirtHeight < waterHeight)
                    {
                        for (int y = waterHeight; y >= dirtHeight; y--)
                        {
                            lvl.SetTile((ushort)x, (ushort)y, (ushort)z, biome.Water);
                        }
                    }
                    else
                    {
                        int sandHeight = (int)Math.Floor((noise * 120 + half) * dropoff);
                        byte topBlock = dirtHeight < sandHeight ? biome.Surface : biome.BeachSandy;
                        lvl.SetTile((ushort)x, (ushort)dirtHeight, (ushort)z, topBlock);
                    }

                    for (int y = dirtHeight - 1; y >= 0; y--)
                    {
                        byte block = (y > dirtHeight * 3 / 4) ? biome.Ground : biome.Cliff;
                        lvl.SetTile((ushort)x, (ushort)y, (ushort)z, block);
                    }
                }

            GenerateTrees(p, lvl);
            return true;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        static void GenerateTrees(Player p, Level lvl)
        {
            Random rnd = new Random();

            double islandSizeFactor = Math.Sqrt(lvl.Width * lvl.Length) / 70.0;

            int minSpacing = (int)(15 + (50 / islandSizeFactor));
            int maxSpacing = (int)(40 + (100 / islandSizeFactor));

            minSpacing = Clamp(minSpacing, 5, 20);
            maxSpacing = Clamp(maxSpacing, 10, 80);

            for (int z = 5; z < lvl.Length - 5; z += rnd.Next(minSpacing, maxSpacing))
            {
                for (int x = 5; x < lvl.Width - 5; x += rnd.Next(minSpacing, maxSpacing))
                {
                    int highestY = 0;
                    for (int y = lvl.Height - 1; y > 0; y--)
                    {
                        if (lvl.GetBlock((ushort)x, (ushort)y, (ushort)z) != Block.Air)
                        {
                            highestY = y;
                            break;
                        }
                    }

                    BlockID block = lvl.GetBlock((ushort)x, (ushort)highestY, (ushort)z);

                    if (block == Block.Sand || block == Block.Grass)
                    {
                        palm.SetData(rnd, palm.DefaultSize(rnd));
                        palm.Generate((ushort)x, (ushort)(highestY + 1), (ushort)z, (xT, yT, zT, bT) =>
                        {
                            if (!lvl.IsAirAt(xT, yT, zT)) return;
                            lvl.Blockchange(xT, yT, zT, (ushort)bT);
                        });
                    }
                }
            }
        }

    }

    public sealed class Perlin2 : IModule
    {
        public double Persistence;
        public int OctaveCount;
        public double Lacunarity;

        public Perlin2()
        {
            Lacunarity = 1.8;
            OctaveCount = 6;
            Persistence = 0.6;
        }

        public override double GetValue(double x, double y, double z)
        {
            double value = 0.0;
            double signal = 0.0;
            double curPersistence = 1.0;

            x *= Frequency;
            y *= Frequency;
            z *= Frequency;

            for (int octave = 0; octave < OctaveCount; octave++)
            {
                signal = GradientNoise.GradientCoherentNoise(x, y, z, Seed + octave);
                value += signal * curPersistence;

                x *= Lacunarity;
                y *= Lacunarity;
                z *= Lacunarity;
                curPersistence *= Persistence;
            }

            return value;
        }
    }

    public class CmdPerlin : Command2
    {
        public override string name { get { return "Perlin"; } }
        public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message)
        {
            string[] args = message.SplitSpaces();
            if (args.Length < 3)
            {
                Help(p);
                return;
            }

            double lacunarity;
            if (!double.TryParse(args[0], out lacunarity) || lacunarity <= 0)
            {
                p.Message("&cInvalid lacunarity. Must be a positive number.");
                return;
            }

            int octaveCount;
            if (!int.TryParse(args[1], out octaveCount) || octaveCount < 1 || octaveCount > 10)
            {
                p.Message("&cInvalid octave count. Must be between 1 and 10.");
                return;
            }

            double persistence;
            if (!double.TryParse(args[2], out persistence) || persistence < 0 || persistence > 1)
            {
                p.Message("&cInvalid persistence. Must be between 0 and 1.");
                return;
            }

            Perlin2 module = new Perlin2
            {
                Lacunarity = lacunarity,
                OctaveCount = octaveCount,
                Persistence = persistence
            };

            p.Message("&aUpdated Perlin2 values: Lacunarity=" + lacunarity + ", OctaveCount=" + octaveCount + ", Persistence=" + persistence);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Perlin [lacunarity] [octave count] [persistence] &S- Updates island generator values.");
            p.Message("&TExample: /Gen 2.0 6 0.5");
        }
    }

    public sealed class BetterPalmTree : Tree
    {
        public override long EstimateBlocksAffected() { return height + 12; }

        public override int DefaultSize(Random rnd) { return rnd.Next(8, 20); } // Slightly taller range

        public override void SetData(Random rnd, int value)
        {
            height = value;
            size = 2;
            this.rnd = rnd;
        }

        public override void Generate(ushort x, ushort y, ushort z, TreeOutput output)
        {
            int bendDirection = rnd.Next(4); // 0 = +X, 1 = -X, 2 = +Z, 3 = -Z
            float bendFactor = rnd.Next(2, 4) / 10.0f; // Smaller bending factor (less aggressive)
            float accumulatedBend = 0;

            ushort currentX = x, currentZ = z;
            for (int dy = 0; dy <= height; dy++)
            {
                output(currentX, (ushort)(y + dy), currentZ, Block.Log);

                if (dy > height / 7 && dy % 3 == 0)
                {
                    accumulatedBend += bendFactor;
                    if (bendDirection == 0) currentX += (ushort)Math.Round(accumulatedBend);
                    else if (bendDirection == 1) currentX -= (ushort)Math.Round(accumulatedBend);
                    else if (bendDirection == 2) currentZ += (ushort)Math.Round(accumulatedBend);
                    else if (bendDirection == 3) currentZ -= (ushort)Math.Round(accumulatedBend);
                }
            }

            // Generate a radial leaf pattern
            int leafRadius = 4;
            for (int dz = -leafRadius; dz <= leafRadius; dz++)
            {
                for (int dx = -leafRadius; dx <= leafRadius; dx++)
                {
                    if (rnd.Next(2) == 0) continue; // Adds gaps for realism
                    output((ushort)(currentX + dx), (ushort)(y + height), (ushort)(currentZ + dz), Block.Leaves);
                }
            }

            // Fuller canopy
            for (int i = 0; i < 14; i++)
            {
                int offsetX = rnd.Next(-2, 3);
                int offsetZ = rnd.Next(-2, 3);
                output((ushort)(currentX + offsetX), (ushort)(y + height - 1), (ushort)(currentZ + offsetZ), Block.Leaves);
            }

            for (int i = 0; i < 5; i++)
            {
                int offsetX = rnd.Next(-2, 3);
                int offsetZ = rnd.Next(-2, 3);
                output((ushort)(currentX + offsetX), (ushort)(y + height - 2), (ushort)(currentZ + offsetZ), Block.Leaves);
            }

            for (int i = 0; i < 5; i++)
            {
                int offsetX = rnd.Next(-2, 3);
                int offsetZ = rnd.Next(-2, 3);
                output((ushort)(currentX + offsetX), (ushort)(y + height + 1), (ushort)(currentZ + offsetZ), Block.Green);
            }

            // What's a palm tree without coconuts?
            if (rnd.Next(3) == 0)
            {
                output(currentX, (ushort)(y + height - 1), currentZ, Block.Brown);
            }
        }
    }
}
