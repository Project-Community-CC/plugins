using System;
using System.Collections.Generic;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Maths;
using MCGalaxy.SQL;
using MCGalaxy.Tasks;

namespace MCGalaxy {
    public class LastLocation : Plugin {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        public override string name { get { return "LastLocation"; } }

        private SchedulerTask task;

        public override void Load(bool startup) {
            OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.Low);
            OnPlayerFinishConnectingEvent.Register(HandlePlayerFinishConnecting, Priority.Low);
            task = Server.MainScheduler.QueueRepeat(UpdatePosition, null, TimeSpan.FromSeconds(1));
            Database.CreateTable("Location", LocationTable);
        }

        public override void Unload(bool shutdown) {
            OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
            OnPlayerFinishConnectingEvent.Unregister(HandlePlayerFinishConnecting);
            Server.MainScheduler.Cancel(task);
        }

        private ColumnDesc[] LocationTable = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("World", ColumnType.VarChar, 255),
            new ColumnDesc("X", ColumnType.Int32),
            new ColumnDesc("Y", ColumnType.Int32),
            new ColumnDesc("Z", ColumnType.Int32),
            new ColumnDesc("Yaw", ColumnType.Int32),
            new ColumnDesc("Pitch", ColumnType.Int32),
            new ColumnDesc("HasShip", ColumnType.UInt8),
        };
        
        private void UpdatePosition(SchedulerTask task) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
                SetPosition(p);
            }
        }
        
        private void SetPosition(Player p) {
            int x = p.Pos.X / 32;
	        int y = (p.Pos.Y - Entities.CharacterHeight) / 32;
	        int z = p.Pos.Z / 32;
	            
	        List<string[]> rows = Database.GetRows("Location", "*", "WHERE Name=@0", p.name);
	
	        if (rows.Count == 0) {
	            Database.AddRow("Location", "Name, World, X, Y, Z, Yaw, Pitch, HasShip", p.name, p.level.name, x, y, z, p.Rot.RotY, p.Rot.HeadX, 0);
	        }
	                        
	        else {
	            Database.UpdateRows("Location", "World=@1", "WHERE NAME=@0", p.name, p.level.name);
	            Database.UpdateRows("Location", "X=@1", "WHERE NAME=@0", p.name, x);
	            Database.UpdateRows("Location", "Y=@1", "WHERE NAME=@0", p.name, y);
	            Database.UpdateRows("Location", "Z=@1", "WHERE NAME=@0", p.name, z);
	            Database.UpdateRows("Location", "Yaw=@1", "WHERE NAME=@0", p.name, p.Rot.RotY);
	            Database.UpdateRows("Location", "Pitch=@1", "WHERE NAME=@0", p.name, p.Rot.HeadX);
	        }
        }

        private void HandlePlayerFinishConnecting(Player p)
        {
            List<string[]> rows = Database.GetRows("Location", "*", "WHERE Name=@0", p.name);
	        if (rows.Count == 0) return;

            string world = rows[0][1];
            p.Extras["SEND_TO_LAST_LOCATION"] = true;
            
            if (p.level.name != world) PlayerActions.ChangeMap(p, world);

            
        }

        private void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            if (p.Extras.GetBoolean("GRID_WORLD_CHANGING_WORLD")) return;

            List<string[]> rows = Database.GetRows("Location", "*", "WHERE Name=@0", p.name);
            if (rows.Count == 0) return;

            int x = int.Parse(rows[0][2]);
            int y = int.Parse(rows[0][3]);
            int z = int.Parse(rows[0][4]);
            byte yaw = byte.Parse(rows[0][5]);
            byte pitch = byte.Parse(rows[0][6]);

            Vec3F32 dir = DirUtils.GetDirVector(yaw, pitch);
            Orientation rot = p.Rot;
            DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);

            Position pos = Position.FromFeetBlockCoords(x, y, z);
            p.SendPosition(pos, rot);
            p.Extras["SEND_TO_LAST_LOCATION"] = false;
        }
    }
}