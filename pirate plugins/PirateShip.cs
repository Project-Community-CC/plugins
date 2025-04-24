using System;
using System.Collections.Generic;
using MCGalaxy.Bots;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.SQL;
using MCGalaxy.Tasks;

namespace MCGalaxy
{
    public class PirateShip : Plugin
    {
        public override string name { get { return "PirateShip"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string creator { get { return "Venk"; } }
        public override bool LoadAtStartup { get { return true; } }

        public static Dictionary<string, Position> shipPositions = new Dictionary<string, Position>();

        public override void Load(bool startup)
        {
            Command.Register(new CmdShip());
            OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.Low);
            OnPlayerDisconnectEvent.Register(HandlePlayerDisconnect, Priority.Low);
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Ship"));
            OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
            OnPlayerDisconnectEvent.Unregister(HandlePlayerDisconnect);

            foreach (Player p in PlayerInfo.Online.Items)
            {
                if (!p.Extras.GetBoolean("DRIVING_SHIP")) continue;
                p.Extras["DRIVING_SHIP"] = false;

                PlayerBot oldBot = FindBots(p, p.level, "ship_" + p.name);
                if (oldBot != null) PlayerBot.Remove(oldBot);
            }
        }

        private void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            if (prevLevel == null) return;
            if (prevLevel == Server.mainLevel)
            {
                List<string[]> rows = Database.GetRows("Location", "*", "WHERE Name=@0", p.name);
                if (rows.Count == 0) return;

                int data = int.Parse(rows[0][7]);
                bool hasShip = data == 1;
                if (hasShip)
                {
                    Command.Find("Ship").Use(p, "");
                    p.Message("spawn ship");
                }
                return;
            }

            if (!p.Extras.GetBoolean("DRIVING_SHIP"))
            {
                if (shipPositions.ContainsKey(p.name))
                {
                    PlayerBot oldBot = FindBots(p, prevLevel, "ship_" + p.name);
                    if (oldBot != null) PlayerBot.Remove(oldBot);

                    p.Message("&SYour ship has sunk due to changing levels. Use /Ship again to spawn it again.");
                    shipPositions.Remove(p.name);

                }
                return;
            }

            if (p.level.name.CaselessStarts("map"))
            {
                PlayerBot oldBot = FindBots(p, prevLevel, "ship_" + p.name);
                if (oldBot != null) PlayerBot.Remove(oldBot);

                PlayerBot newBot = new PlayerBot(p.name, level);
                newBot.SetInitialPos(p.Pos);
                newBot.SetYawPitch(p.Rot.RotY, p.Rot.HeadX);
                PlayerBot.Add(newBot);

                newBot.name = "ship_" + p.name;
                newBot.DisplayName = "empty";
                newBot.UpdateModel("pirate_ship|5");
                newBot.SkinName = "https://i.imgur.com/bWvNaIC.png";
                newBot.GlobalDespawn();
                newBot.GlobalSpawn();
                BotsFile.Save(p.level);

                SchedulerTask[] tasks = p.CriticalTasks.Items;
                for (int i = 0; i < tasks.Length; i++)
                {
                    SchedulerTask task = tasks[i];
                    FlyState state = task.State as FlyState;
                    if (state != null)
                    {
                        state.bot = newBot; // Update bot reference
                        break;
                    }
                }
            }
        }

        private void HandlePlayerDisconnect(Player p, string reason)
        {
            PlayerBot bot = FindBots(p, p.level, "ship_" + p.name);
            if (bot != null) PlayerBot.Remove(bot);

            if (shipPositions.ContainsKey(p.name))
            {
                List<string[]> rows = Database.GetRows("Location", "*", "WHERE Name=@0", p.name);
                if (rows.Count == 0) return;

                if (p.Extras.GetBoolean("DRIVING_SHIP")) Database.UpdateRows("Location", "HasShip=@1", "WHERE NAME=@0", p.name, 1);
                shipPositions.Remove(p.name);
                return;
            }

            Database.UpdateRows("Location", "HasShip=@1", "WHERE NAME=@0", p.name, 0);
        }

        public static PlayerBot FindBots(Player p, Level lvl, string name)
        {
            int matches;
            return Matcher.Find(p, name, out matches, lvl.Bots.Items,
                        null, b => b.name, "bots");
        }
    }

    class FlyState
    {
        public Player player;
        public PlayerBot bot;
        public Position oldPos = default(Position);
        public List<Vec3U16> lastGlass = new List<Vec3U16>();
        public List<Vec3U16> glassCoords = new List<Vec3U16>();
    }

    public sealed class CmdShip : Command2
    {
        public override string name { get { return "Ship"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (!Hacks.CanUseFly(p))
            {
                p.Message("You cannot use &T/Ship &Son this map.");
                p.Extras["DRIVING_SHIP"] = false;
                return;
            }

            p.Extras["DRIVING_SHIP"] = !p.Extras.GetBoolean("DRIVING_SHIP");
            if (!p.Extras.GetBoolean("DRIVING_SHIP")) return;

            PlayerBot bot = null;

            if (!PirateShip.shipPositions.ContainsKey(p.name))
            {
                // Spawn the pirate ship if it isn't already spawned
                bot = new PlayerBot(p.name, p.level);
                bot.SetInitialPos(p.Pos);
                bot.SetYawPitch(p.Rot.RotY, p.Rot.HeadX);
                PlayerBot.Add(bot);
            }

            else
            {
                // Use the existing bot if it exists
                PlayerBot oldBot = PirateShip.FindBots(p, p.level, "ship_" + p.name);
                if (oldBot != null)
                {
                    if (!IsPlayerNearBot(p, oldBot, 7))
                    {
                        p.Message("&cYou are too far away from your ship to control it.");
                        p.Extras["DRIVING_SHIP"] = false;
                        return;
                    }

                    Position shipPos = new Position(oldBot.Pos.X, oldBot.Pos.Y + 60, oldBot.Pos.Z);
                    bot = oldBot;
                    p.SendPosition(shipPos, oldBot.Rot);
                }
            }

            p.Message("You are now driving the ship.");
            p.Session.SendMotd(p.GetMotd() + " jumpheight=0");

            // Spawn the player above the edge level if they are in the water
            if ((p.Pos.Y / 32) < p.level.GetEdgeLevel())
            {
                Position shipPos = new Position(p.Pos.X, (p.level.GetEdgeLevel() * 32) + 128, p.Pos.Z);
                p.SendPosition(shipPos, p.Rot);
            }

            FlyState state = new FlyState();
            state.player = p;
            state.player.UpdateModel("height_adjust|1");

            state.bot = bot;
            state.bot.name = "ship_" + p.name;
            state.bot.DisplayName = "empty";
            state.bot.UpdateModel("pirate_ship|5");
            state.bot.SkinName = "https://i.imgur.com/bWvNaIC.png";
            state.bot.GlobalDespawn();
            state.bot.GlobalSpawn();
            BotsFile.Save(p.level);

            SchedulerTask task = new SchedulerTask(FlyCallback, state, TimeSpan.Zero, true);
            p.CriticalTasks.Add(task);
        }

        private bool IsPlayerNearBot(Player p, PlayerBot bot, int maxDistance)
        {
            int dx = p.Pos.BlockX - bot.Pos.BlockX;
            int dy = p.Pos.BlockY - bot.Pos.BlockY;
            int dz = p.Pos.BlockZ - bot.Pos.BlockZ;

            return (dx * dx + dy * dy + dz * dz) <= (maxDistance * maxDistance);
        }

        private bool IsOtherPlayerOnShip(Player pl, Player driver, int maxDistance)
        {
            int dx = driver.Pos.BlockX - pl.Pos.BlockX;
            int dy = driver.Pos.BlockY - pl.Pos.BlockY;
            int dz = driver.Pos.BlockZ - pl.Pos.BlockZ;

            return (dx * dx + dy * dy + dz * dz) <= (maxDistance * maxDistance);
        }

        private void FlyCallback(SchedulerTask task)
        {
            FlyState state = (FlyState)task.State;
            Player p = state.player;
            if (p.Extras.GetBoolean("DRIVING_SHIP")) { DoFly(state); return; }

            // If no longer driving ship

            /*foreach (Vec3U16 pos in state.lastGlass) {
                //p.SendBlockchange(pos.X, pos.Y, pos.Z, Block.Water);
                p.level.BroadcastRevert(pos.X, pos.Y, pos.Z);
            } */

            //PlayerBot.Remove(state.bot);
            p.UpdateModel("human|1");
            p.Session.SendMotd(p.GetMotd());
            p.Message("You are no longer driving the ship.");
            task.Repeating = false;

            List<string[]> rows = Database.GetRows("Location", "*", "WHERE Name=@0", p.name);
            if (rows.Count == 0) return;
            Database.UpdateRows("Location", "HasShip=@1", "WHERE NAME=@0", p.name, 0);
        }

        private bool IsWaterAt(Level lvl, ushort x, ushort y, ushort z)
        {
            if (x >= lvl.Width || y >= lvl.Height || z >= lvl.Length || lvl.blocks == null) return false;
            return lvl.blocks[x + lvl.Width * (z + y * lvl.Length)] == Block.Water;
        }

        private void DoFly(FlyState state)
        {
            Player p = state.player;
            if (p.Pos == state.oldPos) return;

            Position shipPos = new Position(p.Pos.X, p.Pos.Y - 60, p.Pos.Z);
            state.bot.Pos = shipPos;
            state.bot.Rot = p.Rot;

            PirateShip.shipPositions[p.name] = shipPos;

            int x = p.Pos.BlockX, z = p.Pos.BlockZ;
            int y = p.level.GetEdgeLevel();

            for (int yy = y; yy <= y; yy++)
                for (int zz = z - 3; zz <= z + 3; zz++)
                    for (int xx = x - 3; xx <= x + 3; xx++)
                    {
                        Vec3U16 pos;
                        pos.X = (ushort)xx; pos.Y = (ushort)yy; pos.Z = (ushort)zz;
                        if (IsWaterAt(p.level, pos.X, pos.Y, pos.Z) || p.level.IsAirAt(pos.X, pos.Y, pos.Z)) state.glassCoords.Add(pos);
                    }

            foreach (Vec3U16 P in state.glassCoords)
            {
                if (state.lastGlass.Contains(P)) continue;
                state.lastGlass.Add(P);
                //p.SendBlockchange(P.X, P.Y, P.Z, Block.FromRaw(253));
                p.level.BroadcastChange(P.X, P.Y, P.Z, Block.FromRaw(253));
            }

            for (int i = 0; i < state.lastGlass.Count; i++)
            {
                Vec3U16 P = state.lastGlass[i];
                if (state.glassCoords.Contains(P)) continue;

                p.level.BroadcastRevert(P.X, P.Y, P.Z);

                state.lastGlass.RemoveAt(i); i--;
            }

            /*foreach (Player pl in p.level.getPlayers())
            {
                if (pl == p) continue;
                if (!IsOtherPlayerOnShip(pl, p, 7)) continue;

                // Apply the same movements and rotations as the driver to the player on the ship
                int dx = state.oldPos.X - p.Pos.X;
                int dy = state.oldPos.Y - p.Pos.Y;
                int dz = state.oldPos.Z - p.Pos.Z;

                pl.Message("change: " + dx + " " + dy + " " + dz);

                // Move the player along with the ship
                Position newPlPos = new Position(pl.Pos.X + -dx, pl.Pos.Y + -dy, pl.Pos.Z + dz);
                pl.SendPosition(newPlPos, p.Rot);
            }*/

            foreach (Player pl in p.level.getPlayers())
            {
                if (pl == p) continue;
                if (!IsOtherPlayerOnShip(pl, p, 2)) continue;

                //float rad = Utils.Deg2Rad(p.Rot.RotY);

                double rad = p.Rot.RotY * (Math.PI / 180);
                float cos = (float)Math.Cos(rad);
                float sin = (float)Math.Sin(rad);

                int relX = pl.Pos.BlockX - p.Pos.BlockX;
                int relZ = pl.Pos.BlockZ - p.Pos.BlockZ;

                int newX = (int)(relX * cos - relZ * sin);
                int newZ = (int)(relX * sin + relZ * cos);

                Position newPlPos = new Position(
                    p.Pos.X + newX,
                    pl.Pos.Y,
                    p.Pos.Z + newZ
                );

                pl.SendPosition(newPlPos, p.Rot);
            }


            state.oldPos = p.Pos;
            state.glassCoords.Clear();
        }

        public override void Help(Player p)
        {
            string name = Group.GetColoredName(LevelPermission.Operator);
            p.Message("&T/Ship");
            p.Message("&HCreates a glass platform underneath you that moves with you.");
            p.Message("&H  May not work if you have high latency.");
            p.Message("&H  Cannot be used on maps which have -hax in their motd. " +
                           "(unless you are {0}&H+ and the motd has +ophax)", name);
        }
    }
}
