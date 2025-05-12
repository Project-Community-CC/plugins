using System;
using System.Collections.Generic;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events;
using MCGalaxy.Maths;
using MCGalaxy.Tasks;
using MCGalaxy.Network;
using MCGalaxy;
using ProjectCommunity.Items;

namespace ProjectCommunity {
    public class ItemSystem : Plugin {
        public override string creator { get { return "morgana"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        public override string name { get { return "_ItemSystem"; } }

        public static Dictionary<ushort, ItemBase> Items = new Dictionary<ushort, ItemBase>();

        public override void Load(bool startup) {
            OnPlayerClickEvent.Register(PlayerClick, MCGalaxy.Priority.High);
            OnBlockChangingEvent.Register(PlayerChangingBlock, MCGalaxy.Priority.High);
            OnSentMapEvent.Register(PlayerSentMap, MCGalaxy.Priority.Normal);
        }
        

        public override void Unload(bool shutdown) {
            OnPlayerClickEvent.Unregister(PlayerClick);
            OnBlockChangingEvent.Unregister(PlayerChangingBlock);
            OnSentMapEvent.Unregister(PlayerSentMap);
        }

        public static void RegisterItem(ushort BlockId, ItemBase item)
        {
            item.BlockId = BlockId;

            if (!Items.ContainsKey(BlockId))
            {
                Items.Add(BlockId, item);
                return;
            }

            Items[BlockId] = item;
        }

        public static void UnregisterItem(ushort BlockId)
        {
            if (!Items.ContainsKey(BlockId))
                return;
            Items.Remove(BlockId);
        }

        private static ushort GetHeldBlock(Player player)
        {
            ushort heldBlock = player.GetHeldBlock();
            
            /*if (!Inventory.Has(heldBlock))
                return 0;*/

            return heldBlock;
        }

        private static void PlayerClick(Player player, MouseButton button, MouseAction act, ushort yaw, ushort pitch, byte entity, ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            if (act != MouseAction.Released) return;
            ushort heldblock = GetHeldBlock(player);

            if (!Items.ContainsKey(heldblock))
                return;
            
            Items[heldblock].PlayerRightClicked(player,button,act,yaw,pitch,entity,x,y,z,face);
        }

        private static void PlayerChangingBlock(MCGalaxy.Player player, ushort x, ushort y, ushort z, ushort block, bool placing, ref bool cancel)
        {
            ushort heldblock = GetHeldBlock(player);

            if (!Items.ContainsKey(heldblock))
                return;

            Items[heldblock].PlayerBlockPlaced(player, x,y,z,block,placing, ref cancel);
        }

        private static void PlayerSentMap(Player player, Level prevLevel, Level nextLevel)
        {
            SendBreakPlaceEnablePackets(player);
        }

        private static void SendBreakPlaceEnablePackets(Player player)
        {
            var p = player;

            bool extBlocks = p.Session.hasExtBlocks;
            int count = Items.Count;  //p.Session.MaxRawBlock + 1;
            int size = extBlocks ? 5 : 4;
            byte[] bulk = new byte[count * size];

            ushort x = 0;
            foreach(var pair in Items) // (ushort i = 0; i < count; i++)
            {
                ushort i = pair.Key;
                //if (!Items.ContainsKey(i)) continue;

                bool canPlace = p.Game.Referee || Items[i].CanPlaceWith;
                bool canBreak = p.Game.Referee || Items[i].CanBreakWith;

                Packet.WriteBlockPermission((ushort)i, canPlace, canBreak, p.Session.hasExtBlocks, bulk, x * size);
                x++;
            }
            p.Send(bulk);
        }
    }
}

namespace ProjectCommunity.Items {
    public class ItemBase
    {
        public ushort BlockId = 0;
        public bool CanPlaceWith=false;
        public bool CanBreakWith=false;

        public virtual void OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
        {

        }

        public virtual bool CanUse(Player p)
        {
            return true;
        }

        public void PlayerRightClicked(Player player, MouseButton button, MouseAction act, ushort yaw, ushort pitch, byte entity, ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            if (CanUse(player))
                OnUse(player,x,y,z,entity);
        }
        public void PlayerBlockPlaced(Player p, ushort x, ushort y, ushort z, ushort block, bool placing, ref bool cancel)
        {
            if (!CanPlaceWith && (block == this.BlockId))
            {
                cancel = true;
                p.RevertBlock(x,y,z);
            }

            if (!CanBreakWith && (!placing || block==0))
            {
                cancel = true;
                p.RevertBlock(x,y,z);
            }

        }

        public ItemBase()
        {
        }
    }

    public class ItemBaseConsumeable : ItemBase 
    {
        /*public override bool CanUse(Player p)
        {
            return (Inventory.GetItemQuantity(p, this.BlockId) > 0);
        }*/

       
        public ItemBaseConsumeable()
        {
        }
    }
}
