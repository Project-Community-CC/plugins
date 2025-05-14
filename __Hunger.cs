using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.SQL;
using MCGalaxy.Tasks;
using BlockID = System.UInt16;
using ProjectCommunity;

namespace ProjectCommunity
{
    public class Hunger : Plugin
    {
        public override string name { get { return "__Hunger"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "morgana"; } }

        public const int MaxHunger = 10;
        const int StarveInterval = 120;

        private SchedulerTask hungerTask;

        DateTime nextHungerStarve = DateTime.Now;

        private ColumnDesc[] PlayerHungerTable = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("Hunger", ColumnType.Int32),
        };

        public override void Load(bool startup)
        {
            Database.CreateTable("hunger", PlayerHungerTable);

            Server.MainScheduler.QueueRepeat(HungerTick, null, TimeSpan.FromMilliseconds(500));

            OnJoinedLevelEvent.Register(PlayerJoinedLevel, Priority.Normal);
        }

        public override void Unload(bool shutdown)
        {
            Server.MainScheduler.Cancel(hungerTask);

            OnJoinedLevelEvent.Unregister(PlayerJoinedLevel);
        }

        private void HungerTick(SchedulerTask task)
        {
            hungerTask = task;

            if (DateTime.Now < nextHungerStarve)
                return;

            nextHungerStarve = DateTime.Now.AddSeconds(StarveInterval);

            foreach(var pl in PlayerInfo.Online.Items)
                AddHunger(pl, -1);
        }

        private static void PlayerJoinedLevel(Player p, Level oldlvl, Level newlvl, ref bool announce)
        {
            GuiHunger(p);
        }

        public static void GuiHunger(Player p)
        {
            int hunger = GetHunger(p);
            int emptyhunger = MaxHunger-hunger;
            string hungerbar = "%f" + (new string('▬', hunger)) + "%0" + (new string('▬', emptyhunger));
            p.SendCpeMessage(CpeMessageType.BottomRight1, hungerbar);

            p.SendMapMotd();
        }

        public static int GetHunger(Player p)
        {
            List<string[]> rows = Database.GetRows("hunger", "*", "WHERE Name=@0", p.name);

            return rows.Count > 0  ? int.Parse(rows[0][1]) : MaxHunger;
        }

        public static void SetHunger(Player p, int amount)
        {
            List<string[]> rows = Database.GetRows("hunger", "*", "WHERE Name=@0", p.name);

            if (rows.Count == 0) 
	            Database.AddRow("hunger", "Name, Hunger", p.name, amount);
            else
                Database.UpdateRows("hunger", "Hunger=@1", "WHERE NAME=@0", p.name, amount);

            GuiHunger(p);
        }
        
        public static void AddHunger(Player p, int amount)
        {
            int oldHunger = GetHunger(p);
            int newHunger = oldHunger + amount;

            if (newHunger < 0)
                newHunger = 0;
            if (newHunger > MaxHunger)
                newHunger = MaxHunger;

            if (newHunger == oldHunger) return;

            SetHunger(p, newHunger);
        }
    }
}