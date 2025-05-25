/*
    Note: 1 in-game day lasts 20 minutes real-time.

    Times of day:
        1. Dawn – 5:00-6:30 (texture pack changes to normal)
        2. Sunrise – 6:30-7:30
        3. Day – 7:30-17:30
        4. Sunset – 17:30-18:30
        5. Dusk – 18:30-19:30
        6. Night – 19:30-20:30 (texture pack changes to night)
        7. Midnight – 22:30-2:00
        8. Night 2:00-5:00
        (goes back to dawn)
*/

using System;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Events;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace ProjectCommunity
{
    public class DayNightCycle : Plugin
    {
        public override string name { get { return "DayNightCycle"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.0"; } }
        public override string creator { get { return "Venk"; } }

        public static int timeOfDay = 0;
        public static DayOfWeek currentDay = DayOfWeek.Sunday;
        public static int dayNumber = 0;

        public static Season currentSeason = Season.Spring;

        public static SchedulerTask Task;

        public static string NormalTexturePackUrl = "https://dl.dropbox.com/scl/fi/llemo0m2a7usfi5fl3xuh/Hafen.zip?rlkey=2cossqjwrlzh24kp5teeeyn0k";
        public static string NightTexturePackUrl = "https://dl.dropbox.com/scl/fi/8ucawyyh83uqw2vvhxhrc/HafenNight.zip?rlkey=v7zpnipbh0kmk2nlnkem8nzci";

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

        private bool IsTimeInRange(int timeOfDay, int startTicks, int endTicks)
        {
            // Normalize timeOfDay to be within 0-23999
            if (timeOfDay < 0) timeOfDay += 24000;
            if (timeOfDay >= 24000) timeOfDay -= 24000;

            return timeOfDay >= startTicks && timeOfDay <= endTicks;
        }

        private string GetSkyColor(int timeOfDay)
        {
            // Dawn: 5:00 - 6:30
            if (IsTimeInRange(timeOfDay, 5000, 6500))
                return "#836668";

            // Sunrise: 6:30 - 7:30
            if (IsTimeInRange(timeOfDay, 6500, 7500))
                return "#836668";

            // Day: 7:30 - 17:30
            if (IsTimeInRange(timeOfDay, 7500, 17500))
                return "#6BB3FF";

            // Sunset: 17:30 - 18:30
            if (IsTimeInRange(timeOfDay, 17500, 18500))
                return "#836668";

            // Dusk: 18:30 - 19:30
            if (IsTimeInRange(timeOfDay, 18500, 19500))
                return "#836668";

            // Night: 19:30 - 22:30
            if (IsTimeInRange(timeOfDay, 19500, 22500))
                return "#00407B";

            // Midnight: 22:30 - 2:00
            if (IsTimeInRange(timeOfDay, 22500, 24000) || IsTimeInRange(timeOfDay, 0, 2000))
                return "#070A23";

            // Night: 2:00 - 5:00
            if (IsTimeInRange(timeOfDay, 2000, 5000))
                return "#00407B";

            return "#6BB3FF";
        }

        private string GetFogColor(int timeOfDay)
        {
            // Dawn: 5:00 - 6:30
            if (IsTimeInRange(timeOfDay, 5000, 6500))
                return "#D36538";

            // Sunrise: 6:30 - 7:30
            if (IsTimeInRange(timeOfDay, 6300, 7500))
                return "#FFA322";

            // Day: 7:30 - 17:30
            if (IsTimeInRange(timeOfDay, 7500, 17500))
                return "#FFFFFF";

            // Sunset: 17:30 - 18:30
            if (IsTimeInRange(timeOfDay, 17500, 18500))
                return "#FFA322";

            // Dusk: 18:30 - 19:30
            if (IsTimeInRange(timeOfDay, 18500, 19500))
                return "#D36538";

            // Night: 19:30 - 22:30
            if (IsTimeInRange(timeOfDay, 19500, 22500))
                return "#00407B";

            // Midnight: 22:30 - 2:00
            if (IsTimeInRange(timeOfDay, 22500, 24000) || IsTimeInRange(timeOfDay, 0, 2000))
                return "#131947";

            // Night: 2:00 - 5:00
            if (IsTimeInRange(timeOfDay, 2000, 5000))
                return "#00407B";

            return "#FFFFFF";
        }

        private string GetCloudColor(int timeOfDay)
        {
            // Dawn: 5:00 - 6:30
            if (IsTimeInRange(timeOfDay, 5000, 6500))
                return "#836668";

            // Sunrise: 6:30 - 7:30
            if (IsTimeInRange(timeOfDay, 6500, 7500))
                return "#9A6551";

            // Day: 7:30 - 17:30
            if (IsTimeInRange(timeOfDay, 7500, 17500))
                return "#DBFEFF";

            // Sunset: 17:30 - 18:30
            if (IsTimeInRange(timeOfDay, 17500, 18500))
                return "#9A6551";

            // Dusk: 18:30 - 19:30
            if (IsTimeInRange(timeOfDay, 18500, 19500))
                return "#836668";

            // Night: 19:30 - 22:30
            if (IsTimeInRange(timeOfDay, 19500, 22500))
                return "#065784";

            // Midnight: 22:30 - 2:00
            if (IsTimeInRange(timeOfDay, 22500, 24000) || IsTimeInRange(timeOfDay, 0, 2000))
                return "#1E223A";

            // Night: 2:00 - 5:00
            if (IsTimeInRange(timeOfDay, 2000, 5000))
                return "#065784";

            return "#DBFEFF";
        }

        private string GetShadowColor(int timeOfDay)
        {
            // Dawn: 5:00 - 6:30
            if (IsTimeInRange(timeOfDay, 5000, 6500))
                return "#30304B";

            // Sunrise: 6:30 - 7:30
            if (IsTimeInRange(timeOfDay, 6500, 7500))
                return "#46444C";

            // Day: 7:30 - 17:30
            if (IsTimeInRange(timeOfDay, 7500, 17500))
                return "#88888A";

            // Sunset: 17:30 - 18:30
            if (IsTimeInRange(timeOfDay, 17500, 18500))
                return "#46444C";

            // Dusk: 18:30 - 19:30
            if (IsTimeInRange(timeOfDay, 18500, 19500))
                return "#30304B";

            // Night: 19:30 - 22:30
            if (IsTimeInRange(timeOfDay, 19500, 22500))
                return "#293654";

            // Midnight: 22:30 - 2:00
            if (IsTimeInRange(timeOfDay, 22500, 24000) || IsTimeInRange(timeOfDay, 0, 2000))
                return "#0F0F19";

            // Night: 2:00 - 5:00
            if (IsTimeInRange(timeOfDay, 2000, 5000))
                return "#293654";

            return "#88888A";
        }


        private string GetSunlightColor(int timeOfDay)
        {
            // Dawn: 5:00 - 6:30
            if (IsTimeInRange(timeOfDay, 5000, 6500))
                return "#525163";

            // Sunrise: 6:30 - 7:30
            if (IsTimeInRange(timeOfDay, 6500, 7500))
                return "#7F6C60";

            // Day: 7:30 - 17:30
            if (IsTimeInRange(timeOfDay, 7500, 17500))
                return "#FFFFFF";

            // Sunset: 17:30 - 18:30
            if (IsTimeInRange(timeOfDay, 17500, 18500))
                return "#7F6C60";

            // Dusk: 18:30 - 19:30
            if (IsTimeInRange(timeOfDay, 18500, 19500))
                return "#525163";

            // Night: 19:30 - 22:30
            if (IsTimeInRange(timeOfDay, 19500, 22500))
                return "#2F3E60";

            // Midnight: 22:30 - 2:00
            if (IsTimeInRange(timeOfDay, 22500, 24000) || IsTimeInRange(timeOfDay, 0, 2000))
                return "#181828";

            // Night: 2:00 - 5:00
            if (IsTimeInRange(timeOfDay, 2000, 5000))
                return "#2F3E60";

            return "#FFFFFF";
        }

        private string GetTimeEmoji()
        {
            if (IsTimeInRange(timeOfDay, 5000, 7500) || IsTimeInRange(timeOfDay, 17500, 19500))
                return "◘"; // Sunrise/Sunset
            if (IsTimeInRange(timeOfDay, 7500, 17500))
                return "♠"; // Day
            return "•";     // Night
        }

        private string GetGameTimeString(int ticks)
        {
            // Convert ticks to hours and minutes
            int inGameTime = (ticks * 24) / 24000; // Convert ticks to in-game hours
            int totalMinutes = (ticks * 24 * 60) / 24000; // Convert ticks to in-game minutes

            int hour = totalMinutes / 60;
            int minute = totalMinutes % 60;

            string period = "AM";
            if (hour >= 12)
            {
                period = "PM";
                if (hour > 12) hour -= 12;
            }
            if (hour == 0) hour = 12;

            string timeStr = string.Format("{0}:{1:D2}{2}", hour, minute, period);
            string emoji = GetTimeEmoji();

            return string.Format("{0} {1}, {2} (Season: {3})", emoji, currentDay, timeStr, currentSeason);
        }

        private void DoDayNightCycle(SchedulerTask task)
        {
            OnDayNightCycleTickEvent.Call(timeOfDay, currentSeason, currentDay);

            if (timeOfDay >= 23999)
            {
                timeOfDay = 0;
                dayNumber++;
                currentDay = (DayOfWeek)(((int)currentDay + 1) % 7);

                if (dayNumber >= 28)
                {
                    dayNumber = 0;
                    currentSeason = (Season)(((int)currentSeason + 1) % 4);
                }

                OnNewDayEvent.Call(currentSeason, currentDay);
            }

            else timeOfDay += 20;

            ChangeEnvironment();
            Task = task;
        }

        private bool isNightTime = false;

        public void ChangeEnvironment()
        {
            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (!pl.level.Config.MOTD.Contains("daynightcycle=true")) continue;
                pl.SendCpeMessage(CpeMessageType.Status3, GetGameTimeString(timeOfDay) + " ");

                ColorDesc sky = default(ColorDesc);
                if (!CommandParser.GetHex(pl, GetSkyColor(timeOfDay), ref sky)) return;

                ColorDesc cloud = default(ColorDesc);
                if (!CommandParser.GetHex(pl, GetCloudColor(timeOfDay), ref cloud)) return;

                ColorDesc fog = default(ColorDesc);
                if (!CommandParser.GetHex(pl, GetFogColor(timeOfDay), ref fog)) return;

                ColorDesc shadow = default(ColorDesc);
                if (!CommandParser.GetHex(pl, GetShadowColor(timeOfDay), ref shadow)) return;

                ColorDesc sun = default(ColorDesc);
                if (!CommandParser.GetHex(pl, GetSunlightColor(timeOfDay), ref sun)) return;

                pl.Send(Packet.EnvColor(0, sky.R, sky.G, sky.B));
                pl.Send(Packet.EnvColor(1, cloud.R, cloud.G, cloud.B));
                pl.Send(Packet.EnvColor(2, fog.R, fog.G, fog.B));
                pl.Send(Packet.EnvColor(3, shadow.R, shadow.G, shadow.B));
                pl.Send(Packet.EnvColor(4, sun.R, sun.G, sun.B));

                // No skybox tint for night time as the night skybox is already tinted
                if (IsTimeInRange(timeOfDay, 19500, 22500) || IsTimeInRange(timeOfDay, 2000, 5000))
                    pl.Send(Packet.EnvColor(5, 256, 256, 256));
                else pl.Send(Packet.EnvColor(5, sun.R, sun.G, sun.B));
            }

            // When it's night time, change the server's texture pack and reload for all players
            if (!isNightTime && (IsTimeInRange(timeOfDay, 19500, 22500) || IsTimeInRange(timeOfDay, 22500, 24000) || IsTimeInRange(timeOfDay, 0, 2000) || IsTimeInRange(timeOfDay, 2000, 5000)))
            {
                isNightTime = true;
                Server.Config.DefaultTexture = NightTexturePackUrl;
                MCGalaxy.SrvProperties.Save();

                foreach (Player pl in players)
                    pl.SendCurrentTextures();


            }
            else if (isNightTime && (IsTimeInRange(timeOfDay, 5000, 6500) || IsTimeInRange(timeOfDay, 6500, 7500) || IsTimeInRange(timeOfDay, 7500, 17500)))
            {
                isNightTime = false;
                Server.Config.DefaultTexture = NormalTexturePackUrl;
                MCGalaxy.SrvProperties.Save();

                foreach (Player pl in players)
                    pl.SendCurrentTextures();
            }
        }
    }

    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    public enum DayOfWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }

    public delegate void OnNewDay(Season season, DayOfWeek day);

    public sealed class OnNewDayEvent : IEvent<OnNewDay>
    {
        public static void Call(Season season, DayOfWeek day)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(season, day));
        }
    }

    public delegate void OnDayNightCycleTick(int time, Season season, DayOfWeek day);

    public sealed class OnDayNightCycleTickEvent : IEvent<OnDayNightCycleTick>
    {
        public static void Call(int time, Season season, DayOfWeek day)
        {
            if (handlers.Count == 0) return;
            CallCommon(pl => pl(time, season, day));
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

            if (args.Length > 0)
            {
                int timeOfDay = 0;
                if (int.TryParse(args[0], out timeOfDay))
                {
                    if (timeOfDay < 0 || timeOfDay >= 24000)
                    {
                        p.Message("%CInvalid tick value. Must be between 0 and 23999.");
                        return;
                    }
                }
                else
                {
                    string[] timeParts = args[0].Split(':');
                    int hours, minutes;
                    if (timeParts.Length == 2 && int.TryParse(timeParts[0], out hours) && int.TryParse(timeParts[1], out minutes))
                    {
                        if (hours < 0 || hours >= 24 || minutes < 0 || minutes >= 60)
                        {
                            p.Message("%CInvalid time format. Hours must be between 0 and 23, and minutes must be between 0 and 59.");
                            return;
                        }

                        timeOfDay = hours * 1000 + (int)(minutes * 16.6667);
                    }
                    else
                    {
                        p.Message("%CInvalid time format. Use either ticks (e.g., 3000) or H:MM format (e.g., 7:15).");
                        return;
                    }
                }

                DayNightCycle.timeOfDay = timeOfDay;
                p.Message("%STime set to: %b" + DayNightCycle.timeOfDay + "%S.");
            }
        }

        public override void Help(Player p)
        {
            p.Message("%T/SetTime [tick or H:MM] - %HSets the day-night cycle time to either a tick value or a time in H:MM format.");
        }
    }
}