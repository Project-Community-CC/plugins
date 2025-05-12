//pluginref _hunger.dll
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;
namespace ProjectCommunity
{
    public class Sprint : Plugin
    {
        public override string name { get { return "Sprint"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.3"; } }
        public override string creator { get { return "morgana"; } }


        public const float StarvingWalkSpeed = 0.8f;
        public const float SprintingWalkSpeed = 1.47f;

        public override void Load(bool startup)
        {
            OnSendingMotdEvent.Register(PlayerSendingMotd, Priority.Normal);
        }

        public override void Unload(bool shutdown)
        {
            OnSendingMotdEvent.Unregister(PlayerSendingMotd);
        }

        private static float GetMaxSpeed(Player p)
        {
            if (Hunger.GetHunger(p) <= 0)
                return StarvingWalkSpeed;

            return SprintingWalkSpeed;
        }

        private static void PlayerSendingMotd(Player p, ref string motd)
        {
            if (p.Game.Referee)
                return;

            motd += " -speed maxspeed=" + GetMaxSpeed(p).ToString("0.##");
        }
      
    }
}