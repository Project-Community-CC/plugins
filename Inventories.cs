// Adding: Inventory.AddItemToInventory(p, blockID, amount);
// Removing: Inventory.RemoveItemFromInventory(p, blockID, amount);
// TODO: Chest inventories

using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Network;
using MCGalaxy.SQL;
using BlockID = System.UInt16;

namespace ProjectCommunity
{
    public class Inventory : Plugin
    {
        public override string name { get { return "Inventories"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            Database.CreateTable("Inventories", inventoriesTable);
            Command.Register(new CmdInventory());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Inventory"));
        }

        private ColumnDesc[] inventoriesTable = new ColumnDesc[] {
            new ColumnDesc("PlayerName", ColumnType.VarChar, 20),
            new ColumnDesc("Slot", ColumnType.Int32),
            new ColumnDesc("BlockID", ColumnType.UInt16),
            new ColumnDesc("Quantity", ColumnType.Int32)
        };

        public void AddItemToInventory(Player p, BlockID block, int quantity)
        {
            int id = block;
            if (id >= 66) id -= 256; // Need to convert block if ID is over 66

            List<string[]> rows = Database.GetRows("Inventories", "*", "WHERE PlayerName=@0", p.truename);

            Dictionary<int, string[]> slotData = new Dictionary<int, string[]>();

            foreach (string[] row in rows)
            {
                int slot;
                if (int.TryParse(row[1], out slot))
                {
                    slotData[slot] = row;

                    ushort existingBlockID = (ushort)int.Parse(row[2]);
                    int existingQuantity = int.Parse(row[3]);

                    if (existingBlockID == id)
                    {
                        int newQuantity = existingQuantity + quantity;

                        Database.UpdateRows(
                            "Inventories",
                            "Quantity=@0",
                            "WHERE PlayerName=@1 AND Slot=@2",
                            newQuantity, p.truename, slot
                        );

                        return;
                    }
                }
            }

            for (int i = 1; i <= 30; i++)
            {
                if (!slotData.ContainsKey(i))
                {
                    Database.AddRow("Inventories", "PlayerName, Slot, BlockID, Quantity", p.truename, i, id, quantity);
                    return;
                }
            }

            p.Message("&cYour inventory is full.");
        }

        public void RemoveItemFromInventory(Player p, ushort blockID, int quantity)
        {
            int id = blockID;
            if (id >= 66) id -= 256; // Need to convert block if ID is over 66

            List<string[]> rows = Database.GetRows("Inventories", "*", "WHERE PlayerName=@0", p.truename);
            if (rows.Count == 0)
            {
                p.Message("&cYou do not have anything in your inventory.");
                return;
            }

            bool itemFound = false;

            foreach (string[] row in rows)
            {
                int slot;
                if (int.TryParse(row[1], out slot))
                {
                    ushort existingBlockID = (ushort)int.Parse(row[2]);
                    int existingQuantity = int.Parse(row[3]);

                    if (existingBlockID == id)
                    {
                        itemFound = true;

                        if (existingQuantity >= quantity)
                        {
                            int newQuantity = existingQuantity - quantity;

                            if (newQuantity > 0)
                            {
                                // Update the new amount in the database
                                Database.UpdateRows(
                                    "Inventories",
                                    "Quantity=@0",
                                    "WHERE PlayerName=@1 AND Slot=@2",
                                    newQuantity, p.truename, slot
                                );
                            }
                            else
                            {
                                Database.DeleteRows("Inventories", "WHERE PlayerName=@0 AND Slot=@1", p.truename, slot);
                            }
                            return;
                        }

                        else
                        {
                            p.Message("&cYou do not have enough of block ID " + id + " to remove.");
                            return;
                        }
                    }
                }
            }

            if (!itemFound)
            {
                p.Message("&cYou do not have any of block ID " + id + " in your inventory.");
                return;
            }
        }

        public int GetItemQuantity(Player p, ushort blockID)
        {
            List<string[]> rows = Database.GetRows("Inventories", "*", "WHERE PlayerName=@0", p.truename);
            if (rows.Count == 0)
            {
                p.Message("&cYou do not have anything in your inventory.");
                return 0;
            }

            return 0;
        }
    }

    public class CmdInventory : Command2
    {
        public override string name { get { return "Inventory"; } }
        public override string type { get { return "information"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message)
        {
            // Clear the existing block menu so we can show inventory blocks
            for (int i = 0; i <= 767; i++)
            {
                p.Send(Packet.SetInventoryOrder(Block.Air, (BlockID)i, p.Session.hasExtBlocks));
            }

            List<string[]> rows = Database.GetRows("Inventories", "*", "WHERE PlayerName=@0", p.truename);
            foreach (string[] row in rows)
            {
                int slot;
                ushort block;

                if (int.TryParse(row[1], out slot) && ushort.TryParse(row[2], out block))
                {
                    if (slot >= 1 && slot <= 30)
                    {
                        p.Send(Packet.SetInventoryOrder(block, (BlockID)slot, p.Session.hasExtBlocks));
                    }
                }
            }

            p.Send(Packet.ToggleBlockList(false)); // Open player inventory
        }

        public override void Help(Player p)
        {
            p.Message("&T/Inventory &H- Opens the inventory.");
        }
    }
}