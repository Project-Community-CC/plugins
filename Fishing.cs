using System;
using System.Collections.Generic;
using MCGalaxy;

using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace Core
{
    public class Fishing : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.2.8"; } }
        public override string name { get { return "Fishing"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdFish());
            OnPlayerClickEvent.Register(HandlePlayerClick, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Fish"));
            OnPlayerClickEvent.Unregister(HandlePlayerClick);
        }

        private void HandlePlayerClick(Player p, MouseButton button, MouseAction action, ushort yaw, ushort pitch, byte entity, ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            if (action == MouseAction.Pressed)
            {
                p.Extras["FISHING_DOWN"] = true;
            }
            else if (action == MouseAction.Released)
            {
                p.Extras["FISHING_DOWN"] = false;
            }
        }

        public class CmdFish : Command2
        {
            public override string name { get { return "Fish"; } }
            public override string type { get { return "other"; } }
            public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
            public override bool UseableWhenFrozen { get { return true; } }

            public override void Help(Player p)
            {
                p.Message("%T/Fish %S- Go fishing!");
            }

            public override void Use(Player p, string message)
            {
                bool fishing = !p.Extras.GetBoolean("FISHING");

                string[] args = message.SplitSpaces();

                if (args.Length > 1)
                {
                    if (args[0].CaselessEq("bait")) p.Extras["FISHING_BAIT"] = args[1].ToLower();
                }

                if (fishing)
                {
                    p.Send(Packet.Motd(p, "-hax -push horspeed=0 jumps=0 +thirdperson"));
                    p.Extras["MOTD"] = "-hax -push horspeed=0 jumps=0 +thirdperson";
                    p.frozen = true;
                    p.UpdateModel("fishing");

                    FishingData data = new FishingData(p);

                    data.fishingBar.SetMovementProbability();
                    data.ApplyBaitEffects();

                    SchedulerTask task = new SchedulerTask(FishingCallback, data, TimeSpan.FromMilliseconds(100), true);
                    p.CriticalTasks.Add(task);
                }
                else
                {
                    p.SendMapMotd();
                    p.frozen = false;
                    p.UpdateModel("human");
                }

                p.Extras["FISHING"] = fishing;
            }


            void FishingCallback(SchedulerTask task)
            {
                FishingData data = (FishingData)task.State;

                if (data.fishingBar.Progress == 0)
                {
                    data.player.Message("You let the fish get away!");
                    data.player.SendMapMotd();
                    data.player.frozen = false;
                    data.player.UpdateModel("human");
                    data.player.Extras["FISHING"] = false;
                    task.Repeating = false;
                    data.player.SendCpeMessage(CpeMessageType.BigAnnouncement, "");
                    data.player.SendCpeMessage(CpeMessageType.SmallAnnouncement, "");
                    return;
                }

                if (data.fishingBar.Progress == 100)
                {
                    data.player.Message("You caught the fish!");
                    data.player.SendMapMotd();
                    data.player.frozen = false;
                    data.player.UpdateModel("human");
                    data.player.Extras["FISHING"] = false;
                    task.Repeating = false;
                    data.player.SendCpeMessage(CpeMessageType.BigAnnouncement, "");
                    data.player.SendCpeMessage(CpeMessageType.SmallAnnouncement, "");
                    return;
                }

                Random random = new Random();

                if (data.fishingBar.FishPosition == data.fishingBar.TargetPosition)
                {
                    if (random.NextDouble() <= data.fishingBar.MovementProbability)
                    {
                        data.fishingBar.SetTargetPosition();
                    }
                }

                data.fishingBar.MoveFish();

                data.fishingBar.UpdateProgress();

                data.player.SendCpeMessage(CpeMessageType.BigAnnouncement, data.fishingBar.GetDisplay());
                data.player.SendCpeMessage(CpeMessageType.SmallAnnouncement, "(" + data.fishingBar.Progress + "%%f)");

                if (!data.player.Extras.GetBoolean("FISHING"))
                {
                    task.Repeating = false;
                    data.player.Message("cancel fishing");
                    return;
                }

                if (data.player.Extras.GetBoolean("FISHING_DOWN"))
                {
                    data.fishingBar.MoveNetLeft();
                }
                else
                {
                    data.fishingBar.MoveNetRight();
                }
            }
        }

        public class Fish
        {
            public string Name { get; set; }
            public FishMovementPattern MovementPattern { get; set; }
            public int Difficulty { get; set; }
            public double Rarity { get; set; }

            public Fish(string name, FishMovementPattern movementPattern, int difficulty, double rarity)
            {
                Name = name;
                MovementPattern = movementPattern;
                Difficulty = difficulty;
                Rarity = rarity;
            }
        }

        public static class BaitCatalog
        {
            public static readonly Dictionary<string, double> BaitEffects = new Dictionary<string, double>
            {
                { "sinker", 1.25 },
                { "floater", 1.25 },
                { "dart", 1.25 },
                { "stabilizer", 2.5 },
                { "restrictor", 0.3 }
            };
        }

        public static class FishCatalog
        {
            public static readonly List<Fish> FishList = new List<Fish>
            {

                new Fish("Great White Shark", FishMovementPattern.Dart, 90, 0.5),
                new Fish("Killer Whale", FishMovementPattern.Floater, 100, 0.5),
                new Fish("Goliath Grouper", FishMovementPattern.Sinker, 100, 0.5),
                new Fish("Black Seadevil", FishMovementPattern.Mixed, 100, 0.5),

                new Fish("Bass", FishMovementPattern.Smooth, 35, 6.0),
                new Fish("Trout", FishMovementPattern.Mixed, 20, 7.0),
                new Fish("Salmon", FishMovementPattern.Smooth, 45, 5.5),
                new Fish("Catfish", FishMovementPattern.Sinker, 25, 6.5),
                new Fish("Pike", FishMovementPattern.Dart, 50, 4.5),
                new Fish("Bluegill", FishMovementPattern.Smooth, 15, 8.0),
                new Fish("Walleye", FishMovementPattern.Mixed, 40, 5.0),
                new Fish("Perch", FishMovementPattern.Smooth, 30, 7.5),
                new Fish("Muskellunge", FishMovementPattern.Dart, 60, 3.5),
                new Fish("Carp", FishMovementPattern.Sinker, 30, 6.0),
                new Fish("Striped Bass", FishMovementPattern.Mixed, 50, 4.0),
                new Fish("Swordfish", FishMovementPattern.Dart, 70, 2.5),
                new Fish("Marlin", FishMovementPattern.Dart, 80, 2.0),
                new Fish("Halibut", FishMovementPattern.Sinker, 45, 4.5),
                new Fish("Cod", FishMovementPattern.Sinker, 35, 5.5),
                new Fish("Snapper", FishMovementPattern.Mixed, 25, 6.0),
                new Fish("Tuna", FishMovementPattern.Dart, 65, 3.0),
                new Fish("Redfish", FishMovementPattern.Smooth, 40, 5.5),
                new Fish("Barracuda", FishMovementPattern.Dart, 55, 3.5),
                new Fish("Amberjack", FishMovementPattern.Mixed, 60, 3.0),
                new Fish("Mahi-Mahi", FishMovementPattern.Dart, 50, 4.0),
                new Fish("Flounder", FishMovementPattern.Sinker, 30, 6.0),
                new Fish("Sea Bass", FishMovementPattern.Smooth, 35, 6.0),
                new Fish("Largemouth Bass", FishMovementPattern.Smooth, 30, 6.0),
                new Fish("Smallmouth Bass", FishMovementPattern.Smooth, 25, 7.0),
                new Fish("Northern Pike", FishMovementPattern.Dart, 55, 3.5),
                new Fish("Rainbow Trout", FishMovementPattern.Mixed, 20, 7.5),
                new Fish("Brook Trout", FishMovementPattern.Smooth, 15, 8.0),
                new Fish("Golden Trout", FishMovementPattern.Mixed, 45, 4.0),
                new Fish("White Bass", FishMovementPattern.Mixed, 40, 5.0),
                new Fish("Gar", FishMovementPattern.Mixed, 50, 4.0),
                new Fish("Lemon Shark", FishMovementPattern.Dart, 70, 2.5),
                new Fish("Nile Perch", FishMovementPattern.Sinker, 50, 4.0),
                new Fish("Grouper", FishMovementPattern.Sinker, 45, 4.5),
                new Fish("Lingcod", FishMovementPattern.Sinker, 40, 5.0),
                new Fish("Mackerel", FishMovementPattern.Smooth, 20, 7.5),
                new Fish("Pollock", FishMovementPattern.Sinker, 30, 6.0),
                new Fish("Cobia", FishMovementPattern.Dart, 65, 3.0),
                new Fish("Goliath Grouper", FishMovementPattern.Sinker, 70, 2.5),
                new Fish("Shark", FishMovementPattern.Dart, 80, 2.0),
                new Fish("Swordfish", FishMovementPattern.Dart, 75, 2.5),
                new Fish("Whiting", FishMovementPattern.Smooth, 20, 7.5),
                new Fish("Eel", FishMovementPattern.Smooth, 35, 6.0),
                new Fish("Pompano", FishMovementPattern.Mixed, 25, 6.5),
                new Fish("Red Drum", FishMovementPattern.Mixed, 35, 6.0),
                new Fish("Yellowfin Tuna", FishMovementPattern.Dart, 75, 2.5),
                new Fish("Garfish", FishMovementPattern.Mixed, 30, 6.0),
                new Fish("Blacktip Reef Shark", FishMovementPattern.Dart, 70, 2.5),
                new Fish("Great Barracuda", FishMovementPattern.Dart, 65, 3.0),
            };
        }

        public class RandomSelector
        {
            private static Random _random = new Random();

            public static T Select<T>(Dictionary<T, double> options)
            {
                double totalRarity = 0;
                foreach (var rarity in options.Values)
                {
                    totalRarity += rarity;
                }

                double randomValue = _random.NextDouble() * totalRarity;
                double cumulativeRarity = 0;

                foreach (var option in options)
                {
                    cumulativeRarity += option.Value;
                    if (randomValue < cumulativeRarity)
                    {
                        return option.Key;
                    }
                }

                throw new InvalidOperationException("Selection failed.");
            }
        }


        public class FishingData
        {
            public Player player;
            public FishingBar fishingBar;
            public Fish fish;

            public FishingData(Player player)
            {
                this.player = player;
                this.fish = GetRandomFish(player);

                this.fishingBar = new FishingBar
                {
                    MovementPattern = fish.MovementPattern,
                    MovementRange = (int)Math.Round((double)fish.Difficulty / 10),
                    Difficulty = fish.Difficulty
                };

                player.Message("fish " + fish.Name + " dif " + fish.Difficulty + " type " + fish.MovementPattern + " range " + (int)Math.Round((double)fish.Difficulty / 10));
            }

            public void ApplyBaitEffects()
            {
                string baitType = player.Extras.GetString("FISHING_BAIT");
                if (string.IsNullOrEmpty(baitType)) baitType = "normal";
                player.Message(baitType + " type");

                double baitEffect;
                if (BaitCatalog.BaitEffects.TryGetValue(baitType, out baitEffect))
                {
                    if (baitType == "stabilizer")
                    {
                        player.Message(this.fishingBar.MovementProbability + " old");
                        this.fishingBar.MovementProbability /= (float)baitEffect;
                        player.Message(this.fishingBar.MovementProbability + " new");
                    }

                    else if (baitType == "restrictor")
                    {
                        player.Message(this.fishingBar.MovementRange + " old");
                        this.fishingBar.MovementRange = Math.Max(this.fishingBar.MovementRange - 3, 1);
                        player.Message(this.fishingBar.MovementRange + " new");
                    }
                }
            }

            private FishMovementPattern GetRandomMovementPattern()
            {
                var patterns = Enum.GetValues(typeof(FishMovementPattern));
                Random random = new Random();
                return (FishMovementPattern)patterns.GetValue(random.Next(patterns.Length));
            }

            private Fish GetRandomFish()
            {
                var fishRarities = new Dictionary<Fish, double>();
                foreach (var fish in FishCatalog.FishList)
                {
                    fishRarities[fish] = fish.Rarity;
                }

                return RandomSelector.Select(fishRarities);
            }

            private Fish GetRandomFish(Player player)
            {
                var fishRarities = new Dictionary<Fish, double>();

                string baitType = player.Extras.GetString("FISHING_BAIT");
                if (string.IsNullOrEmpty(baitType)) baitType = "normal";

                foreach (var fish in FishCatalog.FishList)
                {
                    double rarity = fish.Rarity;

                    // If the player is using a specific bait, modify the odds for fish that like it.
                    double baitEffect;
                    if (BaitCatalog.BaitEffects.TryGetValue(baitType, out baitEffect))
                    {
                        if (fish.MovementPattern.ToString().CaselessEq(baitType))
                        {
                            rarity *= baitEffect;
                        }
                    }

                    fishRarities[fish] = rarity;
                }

                return RandomSelector.Select(fishRarities);
            }

        }


        public enum FishMovementPattern
        {
            Mixed,
            Smooth,
            Sinker,
            Floater,
            Dart
        }

        public class FishingBar
        {
            public int BarLength = 20; // Length of the fishing bar.
            public int FishPosition = 17; // Current position of the fish (0 to BarLength-1).
            public int TargetPosition = 17; // Target position for the fish.
            public int NetStart = 9; // Position of the leftmost "o" on the bar.
            public int NetEnd = 15; // Position of the rightmost "o" on the bar.
            public int NetSize = 9; // Number of net spaces the fish must be in to gain progress.
            public int Progress = 33; // Start at 33% to give players a chance. 0 = fail, 100 = catch.
            public float MovementProbability = 0.15f; // Probability (0.0 to 1.0) of the fish moving to a new target position.
            public int MovementRange = 3; // The max distance the fish's target position can be from itself.
            public int Difficulty = 25; // How difficult the fish is to catch. Influences range and probability of moving.
            public FishMovementPattern MovementPattern = FishMovementPattern.Mixed; // The type of movement pattern.

            private Random random = new Random();

            public bool IsFishInNet()
            {
                return FishPosition >= NetStart && FishPosition <= NetEnd;
            }

            public void UpdateProgress()
            {
                if (IsFishInNet())
                {
                    Progress += 1;
                }

                else
                {
                    Progress -= 1;
                }

                Progress = Math.Max(0, Math.Min(100, Progress));
            }

            public void SetTargetPosition()
            {
                //int amplitude = GetMovementAmplitude();
                int amplitude = MovementRange;

                int minPotentialTarget = Math.Max(FishPosition - amplitude, 0);
                int maxPotentialTarget = Math.Min(FishPosition + amplitude, BarLength);

                TargetPosition = random.Next(minPotentialTarget, maxPotentialTarget);
            }

            public void MoveFish()
            {
                int step = GetMovementStep();

                if (FishPosition < TargetPosition)
                    FishPosition = Math.Min(FishPosition + step, TargetPosition);

                else if (FishPosition > TargetPosition)
                    FishPosition = Math.Max(FishPosition - step, TargetPosition);
            }

            private int GetMovementStep()
            {
                switch (MovementPattern)
                {
                    case FishMovementPattern.Smooth:
                        return 1;

                    case FishMovementPattern.Mixed:
                        return 2;

                    case FishMovementPattern.Sinker:
                        if (TargetPosition > FishPosition)
                        {
                            return 3; // Faster rightward movement.
                        }

                        return 1;

                    case FishMovementPattern.Floater:
                        if (TargetPosition < FishPosition)
                        {
                            return 3; // Faster leftward movement.
                        }

                        return 1;

                    case FishMovementPattern.Dart:
                        return 3; // Ate-too-much-sugar movement.

                    default:
                        return 1;
                }
            }


            public void SetMovementProbability()
            {
                MovementProbability = Difficulty / 100f;

                Console.WriteLine("diff " + Difficulty + " prob " + MovementProbability);

                switch (MovementPattern)
                {
                    case FishMovementPattern.Dart:
                        MovementProbability *= 1.15f;
                        break;
                    case FishMovementPattern.Smooth:
                        MovementProbability /= 1.35f;
                        break;
                }

                Console.WriteLine("after " + MovementProbability);
            }

            private int GetMovementAmplitude()
            {
                switch (MovementPattern)
                {
                    case FishMovementPattern.Dart:
                        return 8;
                    case FishMovementPattern.Smooth:
                        return 4;
                    case FishMovementPattern.Sinker:
                        return 12;
                    case FishMovementPattern.Floater:
                        return 12;
                    default:
                        return 6;
                }
            }

            public string GetDisplay()
            {
                string[] bar = new string[BarLength];

                for (int i = 0; i < BarLength; i++)
                {
                    bar[i] = "&7-";
                }

                for (int i = NetStart; i <= NetEnd; i++)
                {
                    bar[i] = "&ao";
                }

                if (FishPosition >= 0 && FishPosition < BarLength)
                {
                    bar[FishPosition] = "&f▬";
                }

                //if (FishPosition != TargetPosition) bar[TargetPosition] = 'G';

                return string.Join("", bar);
            }

            public void MoveNetLeft()
            {
                if (NetStart > 0)
                {
                    NetStart--;
                    NetEnd--;
                }
            }

            public void MoveNetRight()
            {
                if (NetEnd < BarLength - 1)
                {
                    NetStart++;
                    NetEnd++;
                }
            }
        }

    }
}
