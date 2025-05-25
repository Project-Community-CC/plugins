//pluginref DayNightCycle.dll

using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace ProjectCommunity {

    public class SleepSystem : Plugin {
        public override string creator { get { return "morgana"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        public override string name { get { return "sleep"; } }

        public CmdSleep pluginSleepCmd = new CmdSleep();

        public static List<Player> SleepingPlayers = new List<Player>();

        public static DateTime voteCooldown = DateTime.Now;

        private static SchedulerTask sleepCheckTask;

        public override void Load(bool startup) {
            Command.Register(pluginSleepCmd);

            SleepingPlayers.Clear();

            OnPlayerDisconnectEvent.Register(PlayerDisconnect, MCGalaxy.Priority.Normal);

            Server.MainScheduler.QueueRepeat(DoSleepCheck, null, TimeSpan.FromMilliseconds(1000));
        }
        
        public override void Unload(bool shutdown) {
            Command.Unregister(pluginSleepCmd);

            SleepingPlayers.Clear();

            OnPlayerDisconnectEvent.Unregister(PlayerDisconnect);

            Server.MainScheduler.Cancel(sleepCheckTask);
        }

        private static void PlayerDisconnect(Player player, string reason)
        {
            PlayerUnsleep(player);
        }
        private void DoSleepCheck(SchedulerTask task)
        {
            sleepCheckTask = task;

            if (SleepingPlayers.Count == 0)
                return;

            if (PlayerInfo.Online.Count == 0)
            {
                SleepingPlayers.Clear();
                return;
            }

            int timeOfDay = DayNightCycle.timeOfDay;

            if (!(DayNightCycle.IsTimeInRange(timeOfDay, 19500, 24000) || DayNightCycle.IsTimeInRange(timeOfDay, 0, 5000)))
            {
                SleepingPlayers.Clear();
                return;
            }

            if (SleepingPlayers.Count < PlayerInfo.Online.Count)
                return;
   
            SkipNight();
        }
        public static void PlayerSleep(Player pl)
        {
            int timeOfDay = DayNightCycle.timeOfDay;
            if (!(DayNightCycle.IsTimeInRange(timeOfDay, 19500, 24000) || DayNightCycle.IsTimeInRange(timeOfDay, 0, 5000)))
            {
                pl.Message("%cIt is not night time yet!");
                return;
            }

            if (SleepSystem.SleepingPlayers.Contains(pl))
            {
                pl.Message("%cYou are already sleeping!");
                return;
            }
            
            SleepingPlayers.Add(pl);

            string message = pl.ColoredName + " %ewants to sleep! %7(%a" + SleepingPlayers.Count + "%7/%a" + PlayerInfo.Online.Count+"%7)";
            foreach(var p in PlayerInfo.Online.Items)
                p.Message(message);
        }

        public static void PlayerUnsleep(Player pl)
        {
            if (!SleepingPlayers.Contains(pl))
                return;

            SleepingPlayers.Remove(pl);

            string message = pl.ColoredName + " %cno longer wants to sleep! %7(%a" + SleepingPlayers.Count + "%7/%a" + PlayerInfo.Online.Count+"%7)";
            foreach(var p in PlayerInfo.Online.Items)
                p.Message(message);
        }

        public static void SkipNight()
        {
            SleepingPlayers.Clear();

            Command.Find("settime").Use(Player.Console, "6:00");
            string message = "%eGood morning! %aThe night has been skipped!";
            foreach(var p in PlayerInfo.Online.Items)
                p.Message(message);
        }
        public class CmdSleep : Command2
        {
            public override string name { get { return "sleep"; } }
            public override string type { get { return "other"; } }
            public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

            public override void Help(Player p)
            {
                p.Message("%T/sleep %S- start a vote to skip to day");
            }
        
            public override void Use(Player p, string message)
            {
                SleepSystem.PlayerSleep(p);
            }
        }
    }
}