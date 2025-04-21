using System;
using MCGalaxy.Commands;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace MCGalaxy
{
    public class DayNightCycle : Plugin
    {
        public override string name { get { return "DayNightCycle"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.0"; } }
        public override string creator { get { return "Venk"; } }

        public static int timeOfDay = 0;
        public static SchedulerTask Task;

        public override void Load(bool startup)
        {
            Command.Register(new CmdSetTime());
            Server.MainScheduler.QueueRepeat(DoDayNightCycle, null, TimeSpan.FromMilliseconds(1000));
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("SetTime"));
            Server.MainScheduler.Cancel(Task);
        }

        string TickToSky(int timeOfDay)
        {
            if (timeOfDay >= 0 && timeOfDay < 1000) return "#709DED";
            if (timeOfDay >= 1000 && timeOfDay < 11834) return "#78A9FF";
            if (timeOfDay >= 11834 && timeOfDay < 12542) return "#709DED";
            if (timeOfDay >= 12542 && timeOfDay < 12610) return "#4C6BA2";
            if (timeOfDay >= 12610 && timeOfDay < 12786) return "#486599";
            if (timeOfDay >= 12786 && timeOfDay < 13000) return "#3C547F";
            if (timeOfDay >= 13000 && timeOfDay < 13188) return "#2D4061";
            if (timeOfDay >= 13188 && timeOfDay < 17843) return "#212E46";
            if (timeOfDay >= 17843 && timeOfDay < 22300) return "#000000";
            if (timeOfDay >= 22300 && timeOfDay < 23000) return "#212E46";
            if (timeOfDay >= 23000 && timeOfDay < 23216) return "#304365";
            if (timeOfDay >= 23216 && timeOfDay < 23460) return "#3C547F";
            if (timeOfDay >= 23460 && timeOfDay < 23961) return "#4C6BA2";
            if (timeOfDay >= 23961 && timeOfDay < 23992) return "#6F9CEC";
            return "#709DED";
        }

        string TickToCloud(int timeOfDay)
        {
            if (timeOfDay >= 0 && timeOfDay < 1000) return "#FFFFFF";
            if (timeOfDay >= 1000 && timeOfDay < 11834) return "#FFFFFF";
            if (timeOfDay >= 11834 && timeOfDay < 12542) return "#FFFFFF";
            if (timeOfDay >= 12542 && timeOfDay < 12610) return "#926864";
            if (timeOfDay >= 12610 && timeOfDay < 12786) return "#926864";
            if (timeOfDay >= 12786 && timeOfDay < 13000) return "#926864";
            if (timeOfDay >= 13000 && timeOfDay < 13188) return "#2D4061";
            if (timeOfDay >= 13188 && timeOfDay < 17843) return "#212E46";
            if (timeOfDay >= 17843 && timeOfDay < 22300) return "#000000";
            if (timeOfDay >= 22300 && timeOfDay < 23000) return "#212E46";
            if (timeOfDay >= 23000 && timeOfDay < 23216) return "#D15F36";
            if (timeOfDay >= 23216 && timeOfDay < 23460) return "#D15F36";
            if (timeOfDay >= 23460 && timeOfDay < 23961) return "#D15F36";
            if (timeOfDay >= 23961 && timeOfDay < 23992) return "#D15F36";
            return "#FFFFFF";
        }

        string TickToFog(int timeOfDay)
        {
            if (timeOfDay >= 0 && timeOfDay < 1000) return "#FFFFFF";
            if (timeOfDay >= 1000 && timeOfDay < 11834) return "#FFFFFF";
            if (timeOfDay >= 11834 && timeOfDay < 12542) return "#FFFFFF";
            if (timeOfDay >= 12542 && timeOfDay < 12610) return "#D15F36";
            if (timeOfDay >= 12610 && timeOfDay < 12786) return "#D15F36";
            if (timeOfDay >= 12786 && timeOfDay < 13000) return "#D15F36";
            if (timeOfDay >= 13000 && timeOfDay < 13188) return "#264559";
            if (timeOfDay >= 13188 && timeOfDay < 17843) return "#264559";
            if (timeOfDay >= 17843 && timeOfDay < 22300) return "#264559";
            if (timeOfDay >= 22300 && timeOfDay < 23000) return "#264559";
            if (timeOfDay >= 23000 && timeOfDay < 23216) return "#D15F36";
            if (timeOfDay >= 23216 && timeOfDay < 23460) return "#D15F36";
            if (timeOfDay >= 23460 && timeOfDay < 23961) return "#D15F36";
            if (timeOfDay >= 23961 && timeOfDay < 23992) return "#D15F36";
            return "#FFFFFF";
        }

        string TickToSun(int timeOfDay)
        {
            if (timeOfDay >= 0 && timeOfDay < 1000) return "#FFFFFF";
            if (timeOfDay >= 1000 && timeOfDay < 11834) return "#FFFFFF";
            if (timeOfDay >= 11834 && timeOfDay < 12542) return "#FFFFFF";
            if (timeOfDay >= 12542 && timeOfDay < 12610) return "#FFD580";
            if (timeOfDay >= 12610 && timeOfDay < 12786) return "#FFAA55";
            if (timeOfDay >= 12786 && timeOfDay < 13000) return "#CC8844";
            if (timeOfDay >= 13000 && timeOfDay < 13188) return "#886644";
            if (timeOfDay >= 13188 && timeOfDay < 17843) return "#444444";
            if (timeOfDay >= 17843 && timeOfDay < 22300) return "#222222";
            if (timeOfDay >= 22300 && timeOfDay < 23000) return "#444444";
            if (timeOfDay >= 23000 && timeOfDay < 23216) return "#886644";
            if (timeOfDay >= 23216 && timeOfDay < 23460) return "#FFAA55";
            if (timeOfDay >= 23460 && timeOfDay < 23961) return "#FFD580";
            if (timeOfDay >= 23961 && timeOfDay < 23992) return "#FFFFFF";
            return "#FFFFFF";
        }

        string TickToShadow(int timeOfDay)
        {
            if (timeOfDay >= 0 && timeOfDay < 1000) return "#BBBBBB";
            if (timeOfDay >= 1000 && timeOfDay < 11834) return "#BBBBBB";
            if (timeOfDay >= 11834 && timeOfDay < 12542) return "#BBBBBB";
            if (timeOfDay >= 12542 && timeOfDay < 12610) return "#CCB377";
            if (timeOfDay >= 12610 && timeOfDay < 12786) return "#BB7733";
            if (timeOfDay >= 12786 && timeOfDay < 13000) return "#996622";
            if (timeOfDay >= 13000 && timeOfDay < 13188) return "#664411";

            // Night: shadow = sun (no sun = no shadow difference)
            if (timeOfDay >= 13188 && timeOfDay < 17843) return "#444444";
            if (timeOfDay >= 17843 && timeOfDay < 22300) return "#222222";
            if (timeOfDay >= 22300 && timeOfDay < 23000) return "#444444";
            if (timeOfDay >= 23000 && timeOfDay < 23216) return "#664411";
            if (timeOfDay >= 23216 && timeOfDay < 23460) return "#BB7733";
            if (timeOfDay >= 23460 && timeOfDay < 23961) return "#CCB377";
            if (timeOfDay >= 23961 && timeOfDay < 23992) return "#BBBBBB";

            return "#BBBBBB";
        }

        string GetTimeEmoji(int ticks)
        {
            if ((ticks >= 12542 && ticks < 13188) || (ticks >= 23000 && ticks < 23460))
                return "◘"; // Sunrise/Sunset
            if ((ticks >= 1000 && ticks < 12542) || (ticks >= 23460 || ticks < 1000))
                return "♠"; // Day
            return "•";     // Night
        }

        string GetGameTimeString(int ticks)
        {
            ticks = ticks % 24000;

            int totalMinutes = ((ticks + 6000) % 24000) * 60 / 1000;
            int hour = totalMinutes / 60;
            int minute = totalMinutes % 60;

            string period = "AM";
            if (hour >= 12)
            {
                period = "PM";
                if (hour > 12) hour -= 12;
            }

            if (hour == 0) hour = 12;

            string timeStr = hour + ":" + minute.ToString("D2") + period;
            string emoji = GetTimeEmoji(ticks);

            return emoji + " " + timeStr;
        }

        void DoDayNightCycle(SchedulerTask task)
        {
            if (timeOfDay >= 23999) timeOfDay = 0;
            else timeOfDay += 20;

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (!pl.level.Config.MOTD.Contains("daynightcycle=true")) continue;
                pl.SendCpeMessage(CpeMessageType.Status3, GetGameTimeString(timeOfDay) + " ");
                ColorDesc sky = default(ColorDesc);
                if (!CommandParser.GetHex(pl, TickToSky(timeOfDay), ref sky)) return;

                ColorDesc cloud = default(ColorDesc);
                if (!CommandParser.GetHex(pl, TickToCloud(timeOfDay), ref cloud)) return;

                ColorDesc fog = default(ColorDesc);
                if (!CommandParser.GetHex(pl, TickToFog(timeOfDay), ref fog)) return;

                ColorDesc shadow = default(ColorDesc);
                if (!CommandParser.GetHex(pl, TickToShadow(timeOfDay), ref shadow)) return;

                ColorDesc sun = default(ColorDesc);
                if (!CommandParser.GetHex(pl, TickToSun(timeOfDay), ref sun)) return;

                pl.Send(Packet.EnvColor(0, sky.R, sky.G, sky.B));
                pl.Send(Packet.EnvColor(1, cloud.R, cloud.G, cloud.B));
                pl.Send(Packet.EnvColor(2, fog.R, fog.G, fog.B));
                pl.Send(Packet.EnvColor(3, shadow.R, shadow.G, shadow.B));
                pl.Send(Packet.EnvColor(4, sun.R, sun.G, sun.B));
                pl.Send(Packet.EnvColor(5, sun.R, sun.G, sun.B));
            }

            Task = task;
        }
    }

    public sealed class CmdSetTime : Command2
    {
        public override string name { get { return "SetTime"; } }
        public override string shortcut { get { return "timeset"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();

            DayNightCycle.timeOfDay = int.Parse(args[0]);
            p.Message("%STime set to: %b" + DayNightCycle.timeOfDay + "%S.");
        }

        public override void Help(Player p)
        {
            p.Message("%T/SetTime [tick] - %HSets the day-night cycle time to [tick].");
        }
    }
}