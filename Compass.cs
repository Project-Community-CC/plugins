using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;

namespace ProjectCommunity
{
    public sealed class Compass : Plugin
    {
        public override string name { get { return "Compass"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.3"; } }
        public override string creator { get { return "123DontMessWitMe"; } }
        public override int build { get { return 1; } }

        const string Cardinals =
        ".NW......N.......NE......E.......SE......S.......SW......W......." +
        "NW......N.......NE......E.......SE......S.......SW......W......." +
        "NW......N.......NE......E.......SE......S.......SW......W.......";

        public override void Load(bool auto)
        {
            OnPlayerMoveEvent.Register(OnPlayerMoveCall, Priority.Normal);
        }

        public override void Unload(bool auto)
        {
            OnPlayerMoveEvent.Unregister(OnPlayerMoveCall);
        }

        public void OnPlayerMoveCall(Player p, Position next, byte yaw, byte pitch, ref bool cancel)
        {
            if (!p.Supports(CpeExt.MessageTypes) || p.Extras.GetInt("COMPASS_VALUE") == yaw) return;
            p.Extras["COMPASS_VALUE"] = yaw;
            p.SendCpeMessage(CpeMessageType.Status2, GetCompassString(yaw)); // TODO: Add TopAnnouncement MessageType into the client
        }

        public static string GetCompassString(int yaw)
        {
            int centerOffset = (int)(yaw / 256f * 64f) + 64; // 64 is to center into repeated string
            return string.Format("&S[&F{0}&C{1}&F{2}&S]",
                Cardinals.Substring(centerOffset - 18, 17),
                Cardinals.Substring(centerOffset - 1, 3),
                Cardinals.Substring(centerOffset + 2, 17));
        }
    }
}