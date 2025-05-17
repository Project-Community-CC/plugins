//pluginref __Constants.dll
//pluginref ___Util.dll;
//pluginref __itemsystem.dll
//pluginref __Inventory.dll
//pluginref _Food.dll
//pluginref ___XPSystem.dll
//pluginref DayNightCycle.dll
//reference System.dll
//reference System.Core.dll

// TODO: Farming doesn't work if the player isn't in the level at the time of new day
using System;
using System.Linq;
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
using ProjectCommunity.Items.Tools;
using ProjectCommunity.Items.Food;
using ProjectCommunity.Util;

namespace ProjectCommunity
{   
    public class Farming : Plugin
    {
        public override string name { get { return "Farming"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "Venk, Morgana"; } }

        public static Dictionary<string, CropBase> Crops = new Dictionary<string, CropBase>();


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

        public static void RegisterSeed(BlockID itemId, string cropId, int hungerReplenish=0, bool placeable=false) // hungerreplenish enables making it a food item if > 0
        {
            ItemSystem.RegisterItem(itemId, new SeedItem(cropId, hungerReplenish){CanPlaceWith = placeable});
        }

        public static void UnregisterSeed(BlockID itemId)
        {
            ItemSystem.UnregisterItem(itemId);
        }

        public static void RegisterCrop(string cropid, CropBase crop)
        {
            crop.Crop = cropid;

            if (!Crops.ContainsKey(cropid))
            {
                Crops.Add(cropid, crop);
                return;
            }

            Crops[cropid] = crop;
        }

        public static void UnregisterCrop(string cropid)
        {
            if (Crops.ContainsKey(cropid))
                Crops.Remove(cropid);
        }

        private void HandleLevelDeleted(string map)
        {
            Database.DeleteTable("Crops_" + map);
        }

        internal static ColumnDesc[] cropsTable = new ColumnDesc[] {
            new ColumnDesc("X", ColumnType.UInt16),
            new ColumnDesc("Y", ColumnType.UInt16),
            new ColumnDesc("Z", ColumnType.UInt16),
            new ColumnDesc("Type", ColumnType.Char, 255),
            new ColumnDesc("Stage", ColumnType.UInt16),
        };

        private void HandleBlockChanged(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing, ref bool cancel)
        {
            if (placing)
                return;
            
            var crop = PlacedCrop.Get(p.level, x,y,z);

            if (crop == null)
                return;

            if (crop.Crop != null && crop.stage >= crop.maxstage)
                crop.Crop.Harvest(p);
            
            crop.Destroy();
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


        private void GrowCrops(Level lvl)
        {
            List<Vec3U16> coords = GetAllCropLocations(lvl.name);

            foreach (Vec3U16 pos in coords) // Get a list of all crops in the level
            {
                BlockID floorBlock = lvl.FastGetBlock(pos.X, (ushort)(pos.Y - 1), pos.Z);
                if (floorBlock != BlockConstants.Wet_Farmland) continue; // We only care about crops on watered farmland

                lvl.UpdateBlock(Player.Console, pos.X, (ushort)(pos.Y - 1), pos.Z, BlockConstants.Dry_Farmland); // Change wet farmland back to dry farmland

                PlacedCrop placedCrop = PlacedCrop.Get(lvl, pos.X, pos.Y, pos.Z);

                if (placedCrop == null) continue;
                if (placedCrop.stage >= placedCrop.maxstage) continue;

                placedCrop.Grow(1);
              
                placedCrop.Save();
                // TODO: Don't use UpdateBlock since it requires Player.Console
                // TODO: Sprinkler functionality for automatic watering
            }

            // Get a list of all crops in the level
            lvl.Message("&eCrops have grown in &b" + lvl.name + "&e!");
        }
    }

    public class CropBase
    {
        public string Crop;
        public int growTime;

        public Dictionary<int, BlockID> CropBlocks;
        public List<Loot> CropYeilds; 

        public CropBase(int growTime, ushort[] cropBlocks, List<Loot> cropYeilds)
        {
            this.growTime = growTime;

            this.CropYeilds = cropYeilds;
            this.CropBlocks = new Dictionary<int, BlockID>();

            for (int i=0; i< cropBlocks.Length; i++)
            {
                int stage = (int)((((float)i/(float)(cropBlocks.Length-1))*(float)growTime));
                while (this.CropBlocks.ContainsKey(stage))
                    stage++;
                this.CropBlocks.Add(stage, cropBlocks[i]);
            } 
        }

        public ushort GetCropBlock(int stage)
        {
            return CropBlocks.Where(x => x.Key <= stage)
                        .OrderByDescending(x => x.Key)
                        .First().Value;
        }

        public virtual void Harvest(Player p)
        {
            List<Loot> loots = Loot.GetRandomLootList(CropYeilds);
            p.Message("Harvested");
            foreach(var loot in loots)
            {
                p.Message(loot.Amount.ToString() + "x " + loot.Value.ToString());
                bool success = Inventory.AddItemToInventory(p, loot.Value, loot.Amount); // loot.Amount is a random between the loots Min&Max Value
            }
               
        }
    }

    public class PlacedCrop
    {
        public Level level;
        public ushort x;
        public ushort y;
        public ushort z;

        public int stage;
        public int maxstage {get {return this.Crop != null ? Crop.growTime : 1;}}

        public string cropType;
        public CropBase Crop;

        public PlacedCrop(Level lvl, ushort x, ushort y, ushort z, string type, int stage)
        {
            this.level = lvl;
            this.x = x;
            this.y = y;
            this.z = z;
            this.cropType = type;
            this.stage = stage;
            this.Crop = Farming.Crops.ContainsKey(type) ? Farming.Crops[type] : null;   
        }
        public void Save()
        {
            Database.CreateTable("Crops_" + level.name, Farming.cropsTable);

            int changed = Database.UpdateRows("Crops_" + level.name, "Stage=@3",
                "WHERE X=@0 AND Y=@1 AND Z=@2", x,y,z,stage);
            
            if (changed == 0)
                Database.AddRow("Crops_" + level.name, "X,Y,Z, Type, Stage", x, y, z, cropType, stage);
        }

        public void Destroy()
        {
            Database.DeleteRows("Crops_" + level.name, "WHERE X=@0 AND Y=@1 AND Z=@2", x, y, z);
        }

        public void Grow(int amount=1)
        {
            if (this.stage >= this.maxstage)
                return;

            this.stage += amount;
            
            if (this.stage > this.maxstage)
                this.stage = this.maxstage;

            //Player.Console.Message("Crop grew to stage " + this.stage.ToString());

            if (this.Crop == null)
                return;
            BlockID newBlock = this.Crop.GetCropBlock(this.stage);
            level.UpdateBlock(Player.Console, x, y, z, newBlock);
        }

        public static PlacedCrop Get(Level lvl, ushort x, ushort y, ushort z)
        {
            if (!Database.TableExists("Crops_" + lvl.name)) return null;

            List<string[]> rows = Database.GetRows("Crops_" + lvl.name, "Stage, Type", "WHERE X=" + x + " AND Y=" + y + " AND Z=" + z, "");

            if (rows.Count == 0)
                return null;
            
            int stage = 0;
            bool success = int.TryParse(rows[0][0], out stage);
            if (!success) stage=0;

            string type = rows[0][1].ToLower();

            return new PlacedCrop(lvl, x,y,z, type, stage);
        }
    }
}
namespace ProjectCommunity.Items
{
    public class SeedItem : FoodBase
    {
        public string Crop;
        
        public override bool OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
        {            
            ushort floorBlock = p.level.GetBlock(x, y, z);
          
            if (floorBlock != BlockConstants.Dry_Farmland && floorBlock != BlockConstants.Wet_Farmland)
                return base.OnUse(p,x,y,z,entity);
            
            ushort plantY = (ushort)(y + 1);
            string query = "WHERE X=" + x + " AND Y=" + plantY + " AND Z=" + z;
           
            if (Database.TableExists("Crops_" + p.level.name) && Database.GetRows("Crops_" + p.level.name, "*", query,"").Count > 0)
            {
                p.Message("&CThere is already a crop here.");
                return false;
            }

            var crop = new PlacedCrop(p.level, x, plantY, z, Crop, 0);
            crop.Grow(0); // Just for placing the block down;
            crop.Save();

            p.Message("&aPlanted " + this.Crop + "!");
            return true;
            
        }
        public SeedItem(string crop, int replenishment=0) : base(replenishment)
        {
            Crop = crop;
            this.HungerReplenishAmount = replenishment;
        }
    }
}


namespace ProjectCommunity.Items.Tools
{
    public class WateringCanTool : ItemBase
    {
        public override bool OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
        {
            BlockID floorBlock = p.level.GetBlock(x, y, z);

            if (floorBlock != BlockConstants.Dry_Farmland && floorBlock != BlockConstants.Wet_Farmland) 
                return false;

            p.level.UpdateBlock(p, x, y, z, BlockConstants.Wet_Farmland);

            return true;
        }
        public WateringCanTool() : base()
        {

        }
    }

    public class HoeTool : ItemBase
    {
        public override bool OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
        {
            BlockID floorBlock = p.level.GetBlock(x, y, z);

            if (floorBlock != Block.Grass && floorBlock != Block.Dirt) 
                return false; // Only till dirt/grass/farmland

            p.level.UpdateBlock(p, x, y, z, BlockConstants.Dry_Farmland);
            return true;
        }
        public HoeTool() : base()
        {

        }
    }
}