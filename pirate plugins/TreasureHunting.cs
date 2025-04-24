//pluginref __item.dll
using System;
using System.Collections.Generic;
using System.Threading;

using MCGalaxy;
using MCGalaxy.Bots;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.Tasks;
using BlockID = System.UInt16;
using NA2;

namespace MCGalaxy
{
    public class TreasureHunting : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        public override string name { get { return "TreasureHunting"; } }

        public static SchedulerTask task;

        private Dictionary<Level, List<Tuple<ushort, ushort, ushort, BlockID>>> removedBlocks = new Dictionary<Level, List<Tuple<ushort, ushort, ushort, BlockID>>>();

        public override void Load(bool startup)
        {
            OnBlockChangingEvent.Register(HandleBlockChanged, Priority.Low);
            OnLevelUnloadEvent.Register(HandleLevelUnloaded, Priority.Normal);
            OnPlayerClickEvent.Register(HandlePlayerClick, Priority.Normal);
            task = Server.MainScheduler.QueueRepeat(CheckDirection, null, TimeSpan.FromMinutes(5));
        }

        public override void Unload(bool shutdown)
        {
            OnBlockChangingEvent.Unregister(HandleBlockChanged);
            OnLevelUnloadEvent.Unregister(HandleLevelUnloaded);
            OnPlayerClickEvent.Unregister(HandlePlayerClick);
            Server.MainScheduler.Cancel(task);
        }

        private void HandleLevelUnloaded(Level lvl, ref bool cancel)
        {
            if (removedBlocks.ContainsKey(lvl))
            {
                foreach (var block in removedBlocks[lvl])
                {
                    ushort x = block.Item1, y = block.Item2, z = block.Item3;
                    BlockID originalBlock = block.Item4;

                    lvl.UpdateBlock(Player.Console, x, y, z, originalBlock);
                }

                removedBlocks.Remove(lvl);
            }
        }

        private PlayerBot GetClickedBot(Player p, byte entity)
        {
            PlayerBot[] bots = p.level.Bots.Items;
            for (int i = 0; i < bots.Length; i++)
            {
                if (bots[i].EntityID != entity) continue;

                Vec3F32 delta = p.Pos.ToVec3F32() - bots[i].Pos.ToVec3F32();
                float reachSq = p.ReachDistance * p.ReachDistance;
                if (delta.LengthSquared > (reachSq + 1)) return null;

                return bots[i];
            }
            return null;
        }

        class TreasureTickState
        {
            public Player player;
            public PlayerBot bot;
        }

        private void HandlePlayerClick(Player p, MouseButton button, MouseAction action, ushort yaw, ushort pitch, byte entity, ushort x, ushort y, ushort z, TargetBlockFace face)
        {
            if (action != MouseAction.Pressed || button != MouseButton.Right) return;

            // If holding a chest, put it down at the clicked position
            if (p.Extras.Contains("PICKED_UP_CHEST_BOT"))
            {
                if (x == 65535 || y == 65535 || z == 65535) return; // Invalid block position

                int botID = p.Extras.GetInt("PICKED_UP_CHEST_BOT");
                PlayerBot[] bots = p.level.Bots.Items;
                for (int i = 0; i < bots.Length; i++)
                {
                    if (bots[i].EntityID == botID)
                    {
                        bots[i].Pos = new Position((x * 32) + 16, (y * 32) + 84, (z * 32) + 16);
                        bots[i].Rot = p.Rot;
                        break;
                    }
                }

                p.Extras.Remove("PICKED_UP_CHEST_BOT");
                return;
            }

            // Otherwise, pick up the bot
            PlayerBot botToPickUp = GetClickedBot(p, entity);
            if (botToPickUp == null) return;

            TreasureTickState state = new TreasureTickState();
            state.player = p;
            state.bot = botToPickUp;
            p.Extras["PICKED_UP_CHEST_BOT"] = botToPickUp.EntityID; // Store the bot’s EntityID so we can retrieve it when putting it back down again

            SchedulerTask task = new SchedulerTask(SpawnChestAbovePlayerHead, state, TimeSpan.Zero, true);
            p.CriticalTasks.Add(task);
        }

        private void SpawnChestAbovePlayerHead(SchedulerTask task)
        {
            TreasureTickState state = (TreasureTickState)task.State;
            Player p = state.player;
            if (state.player.Extras.Contains("PICKED_UP_CHEST_BOT"))
            {
                Position pos = new Position(p.Pos.X, p.Pos.Y + 72, p.Pos.Z);
                state.bot.Pos = pos;
                state.bot.Rot = p.Rot;
                return;
            }

            task.Repeating = false;
        }

        private void HandleBlockChanged(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing, ref bool cancel)
        {
            if (!placing)
            {
                Level lvl = p.Level;
                BlockID originalBlock = lvl.FastGetBlock(x, y, z); // Get the original block type

                if (originalBlock != Block.Sand && originalBlock != Block.Dirt && originalBlock != Block.Grass)
                {
                    p.RevertBlock(x, y, z);
                    cancel = true;
                    return;
                }

                Item item = Item.MakeInstance(p, "SHOVEL");
                if (item == null) { return; }

                if (!item.OwnedBy(p.name) || item.isVar)
                {
                    p.RevertBlock(x, y, z);
                    cancel = true;
                    p.Message("&cYou need a shovel to dig.");
                    return;
                }

                if (!removedBlocks.ContainsKey(lvl))
                {
                    removedBlocks[lvl] = new List<Tuple<ushort, ushort, ushort, BlockID>>();
                }

                removedBlocks[lvl].Add(Tuple.Create(x, y, z, originalBlock));
            }
        }

        private List<Tuple<int, int>> treasureLevels = new List<Tuple<int, int>>
        {
            Tuple.Create(5, 4),
            Tuple.Create(8, 1)
        };

        private List<Vec3U16> sandBlocks = new List<Vec3U16>();
        bool spawned = false;

        void FindSandBlocks(Level lvl)
        {
            if (sandBlocks != null && sandBlocks.Count > 0) sandBlocks.Clear(); // Reset in case of reload
            int waterLevel = lvl.GetEdgeLevel();

            for (int y = (waterLevel); y <= (waterLevel + 3); y++)
                for (int z = 0; z <= lvl.Length - 1; z++)
                    for (int x = 0; x <= lvl.Width - 1; x++)
                    {
                        if (lvl.FastGetBlock((ushort)x, (ushort)y, (ushort)z) == Block.Sand)
                            sandBlocks.Add(new Vec3U16((ushort)x, (ushort)y, (ushort)z));
                    }
        }

        private string lastMap = null;

        void CheckDirection(SchedulerTask task)
        {
            // Clear any unfound chests from the last map
            if (lastMap != null)
            {
                bool loadedLastLevel = false;
                Level lastLevel = LevelInfo.FindExact(lastMap);
                if (lastLevel == null)
                {
                    loadedLastLevel = true;
                    LevelActions.Load(Player.Console, lastMap, false);
                    lastLevel = LevelInfo.FindExact(lastMap);
                }

                PlayerBot[] bots = lastLevel.Bots.Items;
                foreach (PlayerBot oldBot in bots)
                {
                    if (oldBot.name.CaselessStarts("chest_")) PlayerBot.Remove(oldBot);
                }

                if (loadedLastLevel) lastLevel.Unload(true, false); // Unload the last level since no players were on it
            }

            // Choose a random island to bury treasure on
            Random random = new Random();
            Tuple<int, int> mapCoords = treasureLevels[random.Next(treasureLevels.Count)];
            string map = "map" + mapCoords.Item1 + "," + mapCoords.Item2;
            lastMap = map;

            bool loadedLevel = false;

            // If the level isn't already loaded, load it so we can spawn the treasure chest bot
            Level lvl = LevelInfo.FindExact(map);
            if (lvl == null)
            {
                loadedLevel = true;
                LevelActions.Load(Player.Console, map, false);
                lvl = LevelInfo.FindExact(map);
            }

            FindSandBlocks(lvl);

            if (sandBlocks == null || sandBlocks.Count == 0)
            {
                Logger.Log(LogType.Warning, "No sand blocks available on " + map + ".");
                return;
            }

            Vec3U16 treasureLocation = sandBlocks[random.Next(sandBlocks.Count)];

            PlayerBot bot = new PlayerBot("chest_" + treasureLocation.X + "." + treasureLocation.Y + "." + treasureLocation.Z, lvl);
            bot.SetInitialPos(new Position((ushort)((treasureLocation.X * 32) + 16), (ushort)(((treasureLocation.Y * 32) + 51) - 96), (ushort)((treasureLocation.Z * 32) + 16)));
            bot.SetYawPitch(0, 0);
            bot.DisplayName = "empty";
            bot.ClickedOnText = "hi";
            bot.UpdateModel("chest");
            bot.SkinName = "https://i.imgur.com/KTcPZb8.png";
            PlayerBot.Add(bot);
            bot.GlobalDespawn();
            bot.GlobalSpawn();
            BotsFile.Save(lvl);

            Console.WriteLine("treasure spawned at " + map + ": " + treasureLocation.X + " " + treasureLocation.Y + " " + treasureLocation.Z);
            spawned = true;

            if (loadedLevel) lvl.Unload(true, false); // Unload the level since no players were on it
        }
    }
}