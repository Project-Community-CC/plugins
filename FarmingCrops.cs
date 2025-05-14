//pluginref __constants.dll
//pluginref ___util.dll
//pluginref farming.dll
using ProjectCommunity;
using ProjectCommunity.Util;
using System.Collections.Generic;
using MCGalaxy;

namespace ProjectCommunity
{
    public class FarmingCrops : Plugin
    {
        public override string name { get { return "FarmingCrops"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "morgana"; } }

        public override void Load(bool startup)
        {
            Farming.RegisterSeed(BlockConstants.Carrot_Seed, "carrot",2);
            Farming.RegisterCrop("carrot", new CropBase(
                7, 
                new ushort[]{
                    Block.FromRaw(279),
                    Block.FromRaw(201),
                    Block.FromRaw(198)
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Carrot, 100, 2),
                    new Loot(BlockConstants.Carrot_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Beet_Seed, "beet", 2);
            Farming.RegisterCrop("beet", new CropBase(
                4, 
                new ushort[]{
                    Block.FromRaw(206),
                 //   Block.FromRaw(199),
                    Block.FromRaw(203)
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Beet, 100, 2),
                    new Loot(BlockConstants.Beet_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Potato_Seed, "potato",2);
            Farming.RegisterCrop("potato", new CropBase(
                6, 
                new ushort[]{
                    Block.FromRaw(279),
                    Block.FromRaw(211),
                    Block.FromRaw(208)
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Potato, 100, 2),
                    new Loot(BlockConstants.Potato_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Wheat_Seed, "wheat");
            Farming.RegisterCrop("wheat", new CropBase(
                4, // Has to be 4 rather han 3 to account for blocks
                new ushort[]{
                    Block.FromRaw(216),
                    Block.FromRaw(215),
                    Block.FromRaw(214),
                    Block.FromRaw(213)
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Wheat, 100, 2),
                    new Loot(BlockConstants.Wheat_Seed, 100, 3)
                }
            ));


            Farming.RegisterSeed(BlockConstants.Onion_Seed, "onion", 2);
            Farming.RegisterCrop("onion", new CropBase(
                5,
                new ushort[]{
                    Block.FromRaw(287),
                    Block.FromRaw(286),
                    Block.FromRaw(285),
                    Block.FromRaw(284)
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Onion, 100, 2),
                    new Loot(BlockConstants.Onion_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Tomato_Seed, "tomato", 3);
            Farming.RegisterCrop("tomato", new CropBase(
                8,
                new ushort[]{
                    Block.FromRaw(283),
                    Block.FromRaw(282),
                    Block.FromRaw(281),
                    Block.FromRaw(280)
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Tomato, 100, 2),
                    new Loot(BlockConstants.Tomato_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Lettuce_Seed, "lettuce", 4);
            Farming.RegisterCrop("lettuce", new CropBase(
                4,
                new ushort[]{
                    Block.FromRaw(273),
                    Block.FromRaw(279),
                    Block.FromRaw(278),
                    Block.FromRaw(277),
                    Block.FromRaw(276)
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Lettuce, 100, 2),
                    new Loot(BlockConstants.Lettuce_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Berry_Seed, "wildberry", 2);
            Farming.RegisterCrop("wildberry", new CropBase(
                5,
                new ushort[]{
                    Block.FromRaw(301),
                    Block.FromRaw(176),
                    Block.FromRaw(177),
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Berry, 100, 2),
                    new Loot(BlockConstants.Berry_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Cauliflower_Seed, "cauliflower", 3);
            Farming.RegisterCrop("cauliflower", new CropBase(
                5,
                new ushort[]{
                    Block.FromRaw(297),
                    Block.FromRaw(296),
                    Block.FromRaw(295),
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Cauliflower, 100, 2),
                    new Loot(BlockConstants.Cauliflower_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Zuccini_Seed, "zuccini", 4);
            Farming.RegisterCrop("zuccini", new CropBase(
                5,
                new ushort[]{
                    Block.FromRaw(300),
                    Block.FromRaw(299),
                    Block.FromRaw(298),
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Zuccini, 100, 2),
                    new Loot(BlockConstants.Zuccini_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Pineapple_Seed, "pineapple", 2);
            Farming.RegisterCrop("pineapple", new CropBase(
                8,
                new ushort[]{
                    Block.FromRaw(294),
                    Block.FromRaw(293),
                    Block.FromRaw(292),
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Pineapple, 100, 2),
                    new Loot(BlockConstants.Pineapple_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Melon_Seed, "melon", placeable:true);
            Farming.RegisterCrop("melon", new CropBase(
                13,
                new ushort[]{
                    Block.FromRaw(279),
                    Block.FromRaw(219),
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Melon, 100, 2),
                    new Loot(BlockConstants.Melon_Seed, 100, 3)
                }
            ));

            Farming.RegisterSeed(BlockConstants.Pumpkin_Seed, "pumpkin", placeable:true);
            Farming.RegisterCrop("pumpkin", new CropBase(
                12,
                new ushort[]{
                    Block.FromRaw(279),
                    Block.FromRaw(220),
                },
                new List<Loot>(){
                    new Loot(BlockConstants.Pumpkin, 100, 2),
                    new Loot(BlockConstants.Pumpkin_Seed, 100, 3)
                }
            ));
        }

        public override void Unload(bool shutdown)
        {

        }
    }
}