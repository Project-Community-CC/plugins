using System;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using BlockID = System.UInt16;

namespace ProjectCommunity
{
    public sealed class DoubleBlocks : Plugin
    {
        public override string name { get { return "DoubleBlocks"; } }
        public override string creator { get { return "Akerre"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }

        public override void Load(bool startup)
        {
            OnBlockChangingEvent.Register(OnBlockChanging, Priority.Low);
        }
        public override void Unload(bool shutdown)
        {
            OnBlockChangingEvent.Unregister(OnBlockChanging);
        }

        public void OnBlockChanging(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing, ref bool cancel)
        {
            if (!placing || !p.AllowBuild) { return; }

            string placedBlock = Block.GetName(p, block);
            if (placedBlock.CaselessEnds("-B"))
            {
                string flowerName = placedBlock.Substring(0, placedBlock.Length - 2);
                block = Block.Parse(p, flowerName + "-T");
                y += 1;
                BlockID topBlock = p.level.FastGetBlock(x, y, z);
                if (topBlock == 0)
                {
                    p.level.UpdateBlock(p, x, y, z, block);
                }
            }
        }
    }
}