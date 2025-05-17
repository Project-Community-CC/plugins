using BlockID = System.UInt16;

namespace MCGalaxy
{
    public class __Constants : Plugin
    {
        public override string name { get { return "__Constants"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "Venk"; } }

        public override void Load(bool startup)
        {
            // There is a strange 'Field not found' bug in MCGalaxy due to referencing old plugin .dll files.
            // Make sure to add all dependent plugins here so they are recompiled when this plugin is loaded.
            // You should also /restart when no players are online.
            if (!startup) Command.Find("Compload").Use(Player.Console, "plugin Farming");
        }

        public override void Unload(bool shootdown)
        {
        }
    }

    public class BlockConstants
    {
        // Tools
        public static BlockID Hoe_Tool = Block.FromRaw(55);
        public static BlockID Watering_Can_Tool = Block.FromRaw(56);
        public static BlockID Pickaxe_Tool = Block.FromRaw(57);
        public static BlockID Axe_Tool = Block.FromRaw(58);

        // Farming blocks
        public static BlockID Dry_Farmland = Block.FromRaw(164);
        public static BlockID Wet_Farmland = Block.FromRaw(165);

        // Seeds
        public static BlockID Carrot_Seed = Block.FromRaw(197);
        public static BlockID Beet_Seed = Block.FromRaw(202);
        public static BlockID Potato_Seed = Block.FromRaw(207);
        public static BlockID Wheat_Seed = Block.FromRaw(212);
        public static BlockID Onion_Seed = Block.FromRaw(204);
        public static BlockID Tomato_Seed = Block.FromRaw(200);
        public static BlockID Lettuce_Seed = Block.FromRaw(199);
        public static BlockID Berry_Seed = Block.FromRaw(233);
        public static BlockID Cauliflower_Seed = Block.FromRaw(210);
        public static BlockID Zuccini_Seed = Block.FromRaw(272);
        public static BlockID Pineapple_Seed = Block.FromRaw(209);
        public static BlockID Pumpkin_Seed = Block.FromRaw(220);
        public static BlockID Melon_Seed = Block.FromRaw(219);

        // Crop blocks
        public static BlockID Carrot = Block.FromRaw(197);
        public static BlockID Beet = Block.FromRaw(202);
        public static BlockID Potato = Block.FromRaw(207);
        public static BlockID Wheat = Block.FromRaw(212);
        public static BlockID Onion = Block.FromRaw(204);
        public static BlockID Tomato = Block.FromRaw(200);
        public static BlockID Lettuce = Block.FromRaw(199);
        public static BlockID Berry = Block.FromRaw(233);
        public static BlockID Cauliflower = Block.FromRaw(210);
        public static BlockID Zuccini = Block.FromRaw(272);
        public static BlockID Pineapple = Block.FromRaw(209);
        public static BlockID Pumpkin = Block.FromRaw(220);
        public static BlockID Melon = Block.FromRaw(219);
        // Food blocks
        public static BlockID BakedPotato = Block.FromRaw(308);
        public static BlockID Cake = Block.FromRaw(309);
        public static BlockID Bread = Block.FromRaw(312);
        public static BlockID Chicken = Block.FromRaw(313);
        public static BlockID Steak = Block.FromRaw(314);
        public static BlockID Fish = Block.FromRaw(315);
        public static BlockID Cookie = Block.FromRaw(316);
        public static BlockID Grape = Block.FromRaw(317);
        public static BlockID Soup = Block.FromRaw(318);
    }
}