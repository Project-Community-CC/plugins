//reference System.Core.dll
//pluginref _Quests.dll
// TODO: Sometimes the compass doesn't show ! for objectives?

using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectCommunity
{
    public sealed class Compass : Plugin
    {
        public override string name { get { return "Compass"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.3"; } }
        public override string creator { get { return "123DontMessWitMe, Venk"; } }

        SchedulerTask compassTask;

        const string Cardinals =
        "|       N       |       E       |       S       |       W       " +
        "|       N       |       E       |       S       |       W       " +
        "|       N       |       E       |       S       |       W       ";

        private int cpeTickCounter = 0; // Non-standard clients experience lag when sending too many CPE packets. We'll use this value to send every 5 ticks instead of every tick.

        private List<string> nonStandardClients = new List<string>()
        {
            "Web", "Mobile", "3DS", "Android"
        };

        public override void Load(bool auto)
        {
            Server.MainScheduler.QueueRepeat(CompassTick, null, TimeSpan.Zero);
        }

        public override void Unload(bool auto)
        {
            Server.MainScheduler.Cancel(compassTask);
        }

        private void CompassTick(SchedulerTask task)
        {
            compassTask = task;

            foreach (Player p in PlayerInfo.Online.Items)
            {
                if (!p.Supports(CpeExt.MessageTypes)) return;

                string app = p.Session.ClientName();
                bool isNonStandardClient = nonStandardClients.Any(tag => app.Contains(tag));

                if (isNonStandardClient && cpeTickCounter % 100 != 0)
                    continue; // Skip this tick for non-standard clients

                Vec3S32 playerPos = new Vec3S32(p.Pos.X, p.Pos.Y, p.Pos.Z);
                Vec3S32? target = QuestProgressManager.GetActiveObjectiveTarget(p);

                if (target.HasValue)
                {
                    p.SendCpeMessage(CpeMessageType.Status2, GetCompassString(p.Rot.RotY, playerPos, target.Value));
                }
                else
                {
                    p.SendCpeMessage(CpeMessageType.Status2, GetCompassString(p.Rot.RotY, playerPos, new Vec3S32(-1, -1, -1)));
                }
            }
        }

        private PlayerBot FindBots(Player p, Level lvl, string name)
        {
            int matches;
            return Matcher.Find(p, name, out matches, lvl.Bots.Items,
                        null, b => b.name, "bots");
        }

        private string GetCompassString(int yaw, Vec3S32 playerPos, Vec3S32 targetPos)
        {
            int yawOffset = 32;
            int adjustedYaw = (yaw + yawOffset) % 256;
            int centerOffset = (int)(adjustedYaw / 256f * 64f) + 64;

            int dx = targetPos.X - playerPos.X;
            int dz = targetPos.Z - playerPos.Z;
            float angle = (float)Math.Atan2(dx, -dz);
            if (angle < 0) angle += (float)(2 * Math.PI);
            float targetYaw = angle / (2f * (float)Math.PI) * 256f;
            int targetOffset = (int)((targetYaw + yawOffset) / 256f * 64f) + 64;

            string raw = Cardinals.Substring(centerOffset - 20, 41);
            int relativeTarget = targetOffset - centerOffset + 20;
            int facingHeadingIndex = 20; // Middle of compass is where player is facing

            System.Text.StringBuilder compass = new System.Text.StringBuilder();

            for (int i = 0; i < raw.Length; i++)
            {
                char ch = raw[i];

                if (i == relativeTarget && targetPos != new Vec3S32(-1, -1, -1))
                    compass.Append("&e!&7"); // Highlight quest objective location

                else if (Math.Abs(i - facingHeadingIndex) <= 7 && ch != ' ' && ch != '|')
                    compass.Append("&f").Append(ch).Append("&7");

                else if (ch == '|')
                    compass.Append("|");

                else if (ch == ' ')
                    compass.Append(" ");

                else
                    compass.Append(ch);
            }

            return "&S[&7" + compass.ToString() + "&S]";
        }
    }
}