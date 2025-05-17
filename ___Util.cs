using BlockID = System.UInt16;
using System.Collections.Generic;
using System;

namespace MCGalaxy
{
    public class UtilPlugin : Plugin
    {
        public override string name { get { return "___Util"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "morgana"; } }

        public override void Load(bool startup)
        {
        }
        public override void Unload(bool shootdown)
        {
        }
    }
}

namespace ProjectCommunity.Util
{
    public class Loot
    {
        public int      MinAmount=1;
        public int      MaxAmount=1;
        public int      Amount {get {return rnd.Next(MinAmount, MaxAmount);}}
        public ushort   Value;
        public int       Weight;

        public Loot(ushort Value, int Chance, int MaxAmount=1, int MinAmount=1)
        {
            this.Value = Value;
            this.Weight = Chance;
            this.MaxAmount = MaxAmount;
            this.MinAmount = MinAmount;
        }
        
        static System.Random rnd = new System.Random();

        public static Loot GetRandomLoot(List<Loot> weightedList)
        {
            int totalWeight = 0;

            for (int i=1;i<weightedList.Count;i++)
                totalWeight += weightedList[i].Weight;
        
            int rand = rnd.Next(0,totalWeight);

            foreach(var item in weightedList)
            {
                if (rand < item.Weight)
                    return item;
                
                rand -= item.Weight;
            }

            return weightedList[rnd.Next(weightedList.Count)];
        }

        public static List<Loot> GetRandomLootList(List<Loot> lootlist, int amount=-1)
        {
            if (amount == -1)
                amount = rnd.Next(1,lootlist.Count);
            
            if (amount < lootlist.Count)
                return lootlist;
            
                     
            List<Loot> newList = new List<Loot>();
      
            while (amount > 0)
            {
                var newLoot = GetRandomLoot(lootlist);
                if (!newList.Contains(newLoot))
                {
                    newList.Add(newLoot);
                    amount--;
                }
            }

            return newList;
        }
    }
}