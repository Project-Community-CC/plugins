//pluginref __hunger.dll
//pluginref __itemsystem.dll
//pluginref __constants.dll
using ProjectCommunity.Items.Food;
using ProjectCommunity;
using MCGalaxy;

namespace ProjectCommunity.Items.Food
{
    public class FoodBase : ItemBaseConsumeable
    {
        public int HungerReplenishAmount = 1;
        public override bool OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
        {
            if (HungerReplenishAmount == 0) // Hunger disabled
                return false;

            if (Hunger.GetHunger(p) >= Hunger.MaxHunger)
                return false;

            Hunger.AddHunger(p, HungerReplenishAmount);
            return true;
        }
        public FoodBase(int replenishment=1) : base()
        {
            this.HungerReplenishAmount=replenishment;
        }
    }
}
namespace ProjectCommunity
{
    public class Food : Plugin
    {
        public override string name { get { return "_Food"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "morgana"; } }


        public override void Load(bool startup)
        {
            ItemSystem.RegisterItem(39, new FoodBase(2)); // Temp mushroom test

            // Farming system uses these as seeds,
            // so we register them as seeditems in farmingcrops 
            // which are derived from foodbase and can be optionally made to be edible
            //ItemSystem.RegisterItem(BlockConstants.Carrot, new FoodBase(2));
            //ItemSystem.RegisterItem(BlockConstants.Beet, new FoodBase(2));
            //ItemSystem.RegisterItem(BlockConstants.Potato, new FoodBase(2));

            ItemSystem.RegisterItem(BlockConstants.BakedPotato, new FoodBase(4));
            ItemSystem.RegisterItem(BlockConstants.Cake, new FoodBase(4));
            ItemSystem.RegisterItem(BlockConstants.Bread, new FoodBase(3));
            ItemSystem.RegisterItem(BlockConstants.Chicken, new FoodBase(2));
            ItemSystem.RegisterItem(BlockConstants.Steak, new FoodBase(2));
            ItemSystem.RegisterItem(BlockConstants.Fish, new FoodBase(4));
            ItemSystem.RegisterItem(BlockConstants.Cookie, new FoodBase(3));
            ItemSystem.RegisterItem(BlockConstants.Grape, new FoodBase(1));
            ItemSystem.RegisterItem(BlockConstants.Soup, new FoodBase(4));

        }

        public override void Unload(bool shutdown)
        {
            ItemSystem.UnregisterItem(39);

            //ItemSystem.UnregisterItem(BlockConstants.Carrot);
            //ItemSystem.UnregisterItem(BlockConstants.Beet);
            //ItemSystem.UnregisterItem(BlockConstants.Potato);

            ItemSystem.UnregisterItem(BlockConstants.BakedPotato);
            ItemSystem.UnregisterItem(BlockConstants.Cake);
            ItemSystem.UnregisterItem(BlockConstants.Bread);
            ItemSystem.UnregisterItem(BlockConstants.Chicken);
            ItemSystem.UnregisterItem(BlockConstants.Steak);
            ItemSystem.UnregisterItem(BlockConstants.Fish);
            ItemSystem.UnregisterItem(BlockConstants.Cookie);
            ItemSystem.UnregisterItem(BlockConstants.Grape);
            ItemSystem.UnregisterItem(BlockConstants.Soup);
        }
    }
}