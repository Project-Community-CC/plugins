//pluginref _hunger.dll
//pluginref __itemsystem.dll
//pluginref __constants.dll
using ProjectCommunity.Items.Food;
using ProjectCommunity;
using MCGalaxy;

namespace ProjectCommunity.Items.Food
{
    public class FoodBase : ItemBaseConsumeable
    {
        int HungerReplenishAmount = 1;
        public override void OnUse(Player p, ushort x, ushort y, ushort z, byte entity)
        {
            Hunger.AddHunger(p, HungerReplenishAmount);
        }
        public override bool CanUse(Player p)
        {
            return base.CanUse(p) && Hunger.GetHunger(p) < Hunger.MaxHunger;
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
        public override string name { get { return "Food"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "morgana"; } }


        public override void Load(bool startup)
        {
            ItemSystem.RegisterItem(39, new FoodBase(2)); // Temp mushroom test

            ItemSystem.RegisterItem(BlockConstants.Carrot, new FoodBase(2));
            ItemSystem.RegisterItem(BlockConstants.Beet, new FoodBase(2));
            ItemSystem.RegisterItem(BlockConstants.Potato, new FoodBase(2));

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

            ItemSystem.UnregisterItem(BlockConstants.Carrot);
            ItemSystem.UnregisterItem(BlockConstants.Beet);
            ItemSystem.UnregisterItem(BlockConstants.Potato);

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