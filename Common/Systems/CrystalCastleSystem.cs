using Microsoft.Xna.Framework;
using StructureHelper.API;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace CrystalCastle.Common.Systems
{
    public class CrystalCastleSystem : ModSystem
    {
        private const string StructurePath = "Structures/CrystalCastle";

        private const int SearchAttempts = 600;
        private const int MarginX = 140;
        private const int MarginYTop = 30;
        private const int MarginYBottom = 250;
        private const int SampleStep = 4;
        private const float RequiredHallowFrac = 0.25f;
        private const int AvoidPadding = 24;

        private bool _castlePlaced;

        public override void OnWorldLoad() => _castlePlaced = false;
        public override void OnWorldUnload() => _castlePlaced = false;

        public override void SaveWorldData(TagCompound tag)
        {
            if (_castlePlaced) tag["CrystalCastlePlaced"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            _castlePlaced = tag.ContainsKey("CrystalCastlePlaced") && tag.GetBool("CrystalCastlePlaced");
        }

        public override void NetSend(BinaryWriter writer) => writer.Write(_castlePlaced);
        public override void NetReceive(BinaryReader reader) => _castlePlaced = reader.ReadBoolean();

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (!_castlePlaced && Main.hardMode)
            {
                TryPlaceCrystalCastle();
            }
        }

        private void TryPlaceCrystalCastle()
        {
            Point16 size = Generator.GetStructureDimensions(StructurePath, Mod);
            int w = size.X;
            int h = size.Y;

            UnifiedRandom rand = WorldGen.genRand;

            int cavernStart = Math.Max((int)Main.rockLayer + MarginYTop, 0);
            int cavernEnd = Math.Min(Main.maxTilesY - MarginYBottom, Main.maxTilesY - h - 1);

            if (cavernEnd <= cavernStart || w <= 0 || h <= 0)
                return;

            int jungleMin = GenVars.jungleMinX;
            int jungleMax = GenVars.jungleMaxX;
            bool hasJungleSide = jungleMax > jungleMin;

            for (int attempt = 0; attempt < SearchAttempts; attempt++)
            {
                int tx;
                if (hasJungleSide && rand.NextBool(3))
                {
                    int width = jungleMax - jungleMin;
                    int start = jungleMin + (int)(width * 0.65);
                    int end = jungleMax - 40;
                    tx = rand.Next(Math.Max(start, MarginX), Math.Min(end, Main.maxTilesX - w - MarginX));
                }
                else
                {
                    tx = rand.Next(MarginX, Main.maxTilesX - w - MarginX);
                }

                int ty = rand.Next(cavernStart, cavernEnd - h);
                Point16 topLeft = new Point16(tx, ty);

                if (!Generator.IsInBounds(StructurePath, Mod, topLeft))
                    continue;

                Rectangle footprint = new Rectangle(tx, ty, w, h);
                Rectangle padded = new Rectangle(
                    Math.Max(0, tx - AvoidPadding),
                    Math.Max(0, ty - AvoidPadding),
                    Math.Min(w + 2 * AvoidPadding, Main.maxTilesX - tx + AvoidPadding),
                    Math.Min(h + 2 * AvoidPadding, Main.maxTilesY - ty + AvoidPadding)
                );

                if (!AreaLooksHallowEnough(footprint, RequiredHallowFrac, SampleStep))
                    continue;

                if (AreaHasImportantStructures(padded))
                    continue;

                Generator.GenerateStructure(StructurePath, topLeft, Mod);

                _castlePlaced = true;

                int cx = tx + w / 2;
                int cy = ty + h / 2;
                int side = Math.Max(w, h) / 2 + 10;
                NetMessage.SendTileSquare(-1, cx, cy, side);

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
                return;
            }
        }
        private static bool AreaLooksHallowEnough(Rectangle rect, float requiredFrac, int step)
        {
            int totalSamples = 0;
            int hallowCount = 0;

            int xEnd = Math.Min(rect.Right, Main.maxTilesX - 1);
            int yEnd = Math.Min(rect.Bottom, Main.maxTilesY - 1);

            for (int x = rect.Left; x < xEnd; x += Math.Max(1, step))
            {
                for (int y = rect.Top; y < yEnd; y += Math.Max(1, step))
                {
                    Tile t = Main.tile[x, y];
                    totalSamples++;
                    if (IsHallowTile(t)) hallowCount++;
                }
            }

            if (totalSamples == 0) return false;
            float frac = hallowCount / (float)totalSamples;
            return frac >= requiredFrac;
        }

        private static bool IsHallowTile(Tile t)
        {
            if (t == null) return false;
            ushort type = t.TileType;
            if (type == TileID.Pearlstone ||
                type == TileID.HallowedIce ||
                type == TileID.Pearlsand ||
                type == TileID.PearlstoneBrick)
                return true;

            ushort w = t.WallType;
            if (w == WallID.PearlstoneBrickUnsafe)
                return true;

            return false;
        }

        private static bool AreaHasImportantStructures(Rectangle rect)
        {
            for (int x = Math.Max(1, rect.Left); x < Math.Min(rect.Right, Main.maxTilesX - 1); x++)
            {
                for (int y = Math.Max(1, rect.Top); y < Math.Min(rect.Bottom, Main.maxTilesY - 1); y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t == null) continue;

                    if (t.LiquidAmount > 0 && t.LiquidType == LiquidID.Shimmer)
                        return true;

                    ushort type = t.TileType;
                    ushort wall = t.WallType;

                    if (type == TileID.Containers || type == TileID.Containers2)
                        return true;

                    if (type == TileID.DemonAltar)
                        return true;

                    if (type == TileID.BlueDungeonBrick || type == TileID.GreenDungeonBrick || type == TileID.PinkDungeonBrick)
                        return true;

                    if (type == TileID.LihzahrdBrick)
                        return true;

                    if (wall == WallID.SpiderUnsafe ||
                        wall == WallID.BlueDungeonUnsafe ||
                        wall == WallID.GreenDungeonUnsafe ||
                        wall == WallID.PinkDungeonUnsafe ||
                        wall == WallID.LihzahrdBrickUnsafe)
                        return true;

                    if (type == TileID.Marble || type == TileID.Granite)
                        return true;
                }
            }
            return false;
        }
    }
}
