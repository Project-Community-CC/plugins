//pluginref __Constants.dll
//pluginref _ItemSystem.dll
//pluginref DayNightCycle.dll

// TODO: Farming doesn't work if the player isn't in the level at the time of new day

using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.SQL;
using BlockID = System.UInt16;
using DayOfWeek = ProjectCommunity.DayOfWeek;
using Season = ProjectCommunity.Season;
using ProjectCommunity.Items;

namespace ProjectCommunity
{
    public class Farming : Plugin
    {
        public class WateringCanTool : ItemBase
        {
            public override void OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
            {
                BlockID floorBlock = p.level.GetBlock(x, y, z);

                if (floorBlock != BlockConstants.Dry_Farmland && floorBlock != BlockConstants.Wet_Farmland) return;

                p.level.UpdateBlock(p, x, y, z, BlockConstants.Wet_Farmland);
            }
            public WateringCanTool() : base()
            {

            }
        }

        public class HoeTool : ItemBase
        {
            public override void OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
            {
                BlockID floorBlock = p.level.GetBlock(x, y, z);

                if (floorBlock != Block.Grass && floorBlock != Block.Dirt) return; // Only till dirt/grass/farmland

                p.level.UpdateBlock(p, x, y, z, BlockConstants.Dry_Farmland);
            }
            public HoeTool() : base()
            {

            }
        }

        public override string name { get { return "Farming"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            OnBlockChangingEvent.Register(HandleBlockChanged, Priority.Low);
            OnLevelDeletedEvent.Register(HandleLevelDeleted, Priority.Low);
            OnNewDayEvent.Register(HandleNewDay, Priority.Low);

            ItemSystem.RegisterItem(BlockConstants.Watering_Can_Tool, new WateringCanTool());
            ItemSystem.RegisterItem(BlockConstants.Hoe_Tool         , new HoeTool());
        }

        public override void Unload(bool shutdown)
        {
            OnBlockChangingEvent.Unregister(HandleBlockChanged);
            OnLevelDeletedEvent.Unregister(HandleLevelDeleted);
            OnNewDayEvent.Unregister(HandleNewDay);

            ItemSystem.UnregisterItem(BlockConstants.Watering_Can_Tool);
            ItemSystem.UnregisterItem(BlockConstants.Hoe_Tool);
        }

        private void HandleLevelDeleted(string map)
        {
            Database.DeleteTable("Crops_" + map);
        }

        private ColumnDesc[] cropsTable = new ColumnDesc[] {
            new ColumnDesc("X", ColumnType.UInt16),
            new ColumnDesc("Y", ColumnType.UInt16),
            new ColumnDesc("Z", ColumnType.UInt16),
            new ColumnDesc("Type", ColumnType.Char, 255),
            new ColumnDesc("Stage", ColumnType.UInt16),
        };

        private void HandleBlockChanged(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing, ref bool cancel)
        {
            // Override default hoe and watering can behaviour
            if (!placing)
            {
                BlockID clickedBlock = p.level.GetBlock(x, y, z);
                if (clickedBlock == BlockConstants.Carrot || clickedBlock == BlockConstants.Beet || clickedBlock == BlockConstants.Potato || clickedBlock == BlockConstants.Wheat)
                {
                    Database.DeleteRows("Crops_" + p.level.name, "WHERE X=@0 AND Y=@1 AND Z=@2", x, y, z);
                    return;
                }

                // TODO: Add to inventory
            }

            if (IsSeed(block))
            {
                p.RevertBlock(x, y, z);
                cancel = true;
                TryPlantSeed(p, block, x, y, z, placing);
            }
        }

        private void HandleNewDay(Season season, DayOfWeek day)
        {
            foreach (Player pl in PlayerInfo.Online.Items)
            {
                pl.Message("&eA new day has started!");

                Level lvl = LevelInfo.FindExact("farm-" + pl.name);
                if (lvl == null) continue;

                GrowCrops(lvl);
            }
        }

        private List<Vec3U16> GetAllCropLocations(string map)
        {
            List<Vec3U16> coords = new List<Vec3U16>();
            if (!Database.TableExists("Crops_" + map)) return coords;

            Database.ReadRows("Crops_" + map, "X,Y,Z",
                                record => coords.Add(ParseCoords(record)));
            return coords;
        }

        private Vec3U16 ParseCoords(ISqlRecord record)
        {
            Vec3U16 pos;
            pos.X = (ushort)record.GetInt32(0);
            pos.Y = (ushort)record.GetInt32(1);
            pos.Z = (ushort)record.GetInt32(2);
            return pos;
        }

        private Dictionary<string, int> cropMaxStages = new Dictionary<string, int> {
            { "carrot", 11 },
            { "beet", 5 },
            { "potato", 6 },
            { "wheat", 4 }
        };

        private Dictionary<string, Dictionary<int, BlockID>> cropBlocks = new Dictionary<string, Dictionary<int, BlockID>> {
            {
                "carrot", new Dictionary<int, BlockID> {
                    { 1, Block.FromRaw(206) },
                    { 3, Block.FromRaw(205) },
                    { 6, Block.FromRaw(199) },
                    { 9, Block.FromRaw(198) }
                }
            },
            {
                "beet", new Dictionary<int, BlockID> {
                    { 1, Block.FromRaw(206) },
                    { 2, Block.FromRaw(205) },
                    { 3, Block.FromRaw(199) },
                    { 5, Block.FromRaw(203) }
                }
            },
            {
                "potato", new Dictionary<int, BlockID> {
                    { 1, Block.FromRaw(206) },
                    { 2, Block.FromRaw(205) },
                    { 4, Block.FromRaw(199) },
                    { 6, Block.FromRaw(208) }
                }
            },
            {
                "wheat", new Dictionary<int, BlockID> {
                    { 1, Block.FromRaw(216) },
                    { 2, Block.FromRaw(215) },
                    { 3, Block.FromRaw(214) },
                    { 4, Block.FromRaw(213) }
                }
            }
        };

        void TryPlantSeed(Player p, BlockID block, ushort x, ushort y, ushort z, bool placing)
        {
            BlockID floorBlock = p.level.GetBlock(x, (ushort)(y - 1), z);
            if (!placing) floorBlock = p.level.GetBlock(x, y, z); // If deleting, then the deleted block /is/ the floor block
            if (floorBlock != BlockConstants.Dry_Farmland && floorBlock != BlockConstants.Wet_Farmland)
                return;

            ushort plantY = placing ? y : (ushort)(y + 1);
            List<string[]> rows = Database.GetRows("Crops_" + p.level.name, "*",
                "WHERE X=" + x + " AND Y=" + plantY + " AND Z=" + z, "");
            if (rows.Count > 0)
            {
                p.Message("&CThere is already a crop here.");
                return;
            }

            Database.CreateTable("Crops_" + p.level.name, cropsTable);
            string type = GetSeedType(block);
            object[] args = new object[] { x, plantY, z, type, 0 };

            int changed = Database.UpdateRows("Crops_" + p.level.name, "Stage=@3",
                "WHERE X=@0 AND Y=@1 AND Z=@2", args);
            if (changed == 0)
            {
                Database.AddRow("Crops_" + p.level.name, "X,Y,Z, Type, Stage", args);
            }

            p.Message("&aPlanted " + type + "!");
        }

        private string GetSeedType(BlockID block)
        {
            if (block == BlockConstants.Carrot_Seed) return "carrot";
            if (block == BlockConstants.Beet_Seed) return "beet";
            if (block == BlockConstants.Potato_Seed) return "potato";
            if (block == BlockConstants.Wheat_Seed) return "wheat";
            return "unknown";
        }

        private bool IsSeed(BlockID block)
        {
            return block == BlockConstants.Carrot_Seed ||
                   block == BlockConstants.Beet_Seed ||
                   block == BlockConstants.Potato_Seed ||
                   block == BlockConstants.Wheat_Seed;
        }

        private void GrowCrops(Level lvl)
        {
            List<Vec3U16> coords = GetAllCropLocations(lvl.name);

            foreach (Vec3U16 pos in coords)
            {
                BlockID floorBlock = lvl.FastGetBlock(pos.X, (ushort)(pos.Y - 1), pos.Z);
                if (floorBlock != BlockConstants.Wet_Farmland) continue; // We only care about crops on watered farmland

                lvl.UpdateBlock(Player.Console, pos.X, (ushort)(pos.Y - 1), pos.Z, BlockConstants.Dry_Farmland); // Change wet farmland back to dry farmland

                List<string[]> rows = Database.GetRows("Crops_" + lvl.name, "Stage, Type", "WHERE X=" + pos.X + " AND Y=" + pos.Y + " AND Z=" + pos.Z, "");

                if (rows.Count > 0)
                {
                    int stage = 0;
                    bool success = int.TryParse(rows[0][0], out stage);
                    if (!success) continue;

                    string type = rows[0][1].ToLower();
                    if (!cropMaxStages.ContainsKey(type)) continue;

                    int maxStage = cropMaxStages[type];
                    if (stage >= maxStage) continue; // Stop growing since already fully grown

                    stage += 1;
                    Database.UpdateRows("Crops_" + lvl.name, "Stage=@3", "WHERE X=@0 AND Y=@1 AND Z=@2", pos.X, pos.Y, pos.Z, stage);

                    if (cropBlocks.ContainsKey(type) && cropBlocks[type].ContainsKey(stage))
                    {
                        BlockID newBlock = cropBlocks[type][stage];
                        lvl.UpdateBlock(Player.Console, pos.X, pos.Y, pos.Z, newBlock);
                    }

                    continue;
                }

                // TODO: Don't use UpdateBlock since it requires Player.Console
                // TODO: Sprinkler functionality for automatic watering
            }

            // Get a list of all crops in the level
            lvl.Message("&eCrops have grown in &b" + lvl.name + "&e!");
        }
    }
}