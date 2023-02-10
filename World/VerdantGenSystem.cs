﻿using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.WorldBuilding;
using Verdant.Noise;
using Verdant.Walls;
using Verdant.Tiles.Verdant.Decor;
using Verdant.Tiles.Verdant.Basic;
using Verdant.Tiles.Verdant.Trees;
using Verdant.Tiles.Verdant.Mounted;
using Verdant.Tiles.Verdant.Basic.Blocks;
using Verdant.Tiles.Verdant.Basic.Plants;
using Verdant.Tiles;
using Terraria.IO;
using System;
using Verdant.Tiles.Verdant.Basic.Puff;

namespace Verdant.World
{
    ///Handles specific Verdant biome gen.
    public class VerdantGenSystem : ModSystem
    {
        public static float WorldSize { get => Main.maxTilesX / 4200f; }

        private static int[] TileTypes => new int[] { ModContent.TileType<VerdantGrassLeaves>(), ModContent.TileType<LushSoil>(), TileID.ChlorophyteBrick, ModContent.TileType<VerdantLightbulb>(), ModContent.TileType<LivingLushWood>() };
        private static int[] WallTypes  => new int[] { ModContent.WallType<VerdantLeafWall_Unsafe>(), ModContent.WallType<LushSoilWall_Unsafe>(), ModContent.WallType<LivingLushWoodWall_Unsafe>() };

        internal static Rectangle VerdantArea = new(0, 0, 0, 0);

        private readonly List<GenCircle> VerdantCircles = new();

        internal Point? apotheosisLocation = null;

        public void VerdantGeneration(GenerationProgress p, GameConfiguration config)
        {
            p.Message = "Growing plants...";

            Mod.Logger.Info("World Seed: " + WorldGen._genRandSeed);
            Mod.Logger.Info("Noise Seed: " + VerdantSystem.genNoise.Seed);

            static int GetCenterX()
            {
                int x = WorldGen.genRand.Next(Main.maxTilesX / 4, (int)(Main.maxTilesX / 1.25f));

                while (Math.Abs(x - (Main.maxTilesX / 2)) < 80)
                    x = WorldGen.genRand.Next(Main.maxTilesX / 4, (int)(Main.maxTilesX / 1.25f));
                return x;
            }

            Point center = new(GetCenterX(), WorldGen.genRand.Next((int)(Main.maxTilesY / 2.1f), (int)(Main.maxTilesY / 1.75f)));

            int FluffX = (int)(220 * WorldSize);
            int FluffY = (int)(130 * WorldSize);

            int total = 0;
            while (true) //Find valid position for biome
            {
            reset:
                center = new Point(GetCenterX(), WorldGen.genRand.Next((int)(Main.maxTilesY / 2.5f), (int)(Main.maxTilesY / 1.75f)));
                total = 0;
                if (WorldGen.UndergroundDesertLocation.Contains(center.X - FluffX, center.Y - FluffY) || WorldGen.UndergroundDesertLocation.Contains(center.X - FluffX, center.Y + FluffY)
                    || WorldGen.UndergroundDesertLocation.Contains(center.X + FluffX, center.Y - FluffY) || WorldGen.UndergroundDesertLocation.Contains(center.X + FluffX, center.Y + FluffY)
                    || WorldGen.UndergroundDesertLocation.Contains(center))
                    continue;
                for (int i = center.X - (int)(FluffX * 1.2f); i < center.X + (FluffX * 1.2f); ++i) //Assume width
                {
                    for (int j = center.Y - 140; j < center.Y + 140; ++j) //Assume height
                    {
                        List<int> invalidTypes = new() { TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick, TileID.LihzahrdBrick, TileID.IceBlock, TileID.SnowBlock }; //Vanilla blacklist

                        if (ModLoader.TryGetMod("SpiritMod", out Mod spirit)) //Spirit blacklist
                            invalidTypes.Add(spirit.Find<ModTile>("BriarGrass").Type);
                        if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)) //Calamity blacklist
                            invalidTypes.Add(calamity.Find<ModTile>("Navystone").Type);

                        if ((Framing.GetTileSafely(i, j).HasTile && invalidTypes.Any(x => Framing.GetTileSafely(i, j).TileType == x)))
                            total++;

                        if (total > 40)
                            goto reset;
                    }
                }

                VerdantArea = new Rectangle(center.X, center.Y, 1, 1);
                break;
            }

            GenerateCircles();
            CleanForCaves();
            AddStone();

            for (int i = VerdantArea.Left; i < VerdantArea.Right; ++i) //Smooth out the biome!
                for (int j = VerdantArea.Top; j < VerdantArea.Bottom; ++j)
                    if (WorldGen.genRand.Next(7) <= 3)
                        Tile.SmoothSlope(i, j, false);

            p.Message = "Growing vines...";
            Vines();
            p.Message = "Growing flowers...";
            AddPlants();
            p.Message = "Watering plants...";
            AddWater();
            AddWaterfalls();
            p.Message = "Growing surface...";
            AddSurfaceVerdant();
        }

        private static void AddWaterfalls()
        {
            for (int i = 0; i < 50 * WorldSize; ++i)
            {
                int x = Main.rand.Next(VerdantArea.Left, VerdantArea.Right);
                int y = Main.rand.Next(VerdantArea.Top, VerdantArea.Bottom);
                Tile tile = Main.tile[x, y];

                if (!WorldGen.SolidTile(tile))
                    DigFall(x, y, 2);
                else
                    --i;
            }
        }

        private static void DigFall(int x, int y, int minHeight)
        {
            bool CanPlaceAt(int i, int j, int dir) => WorldGen.SolidTile(i, j) && WorldGen.SolidTile(i + dir, j) && WorldGen.SolidTile(i + dir + dir, j - 1);

            void PlaceAt(int i, int j, int dir)
            {
                Tile liquidTile = Main.tile[i + dir, j - 1];
                liquidTile.ClearTile();
                liquidTile.LiquidAmount = 255;
                liquidTile.LiquidType = LiquidID.Water;

                Tile halfTile = Main.tile[i, j - 1];
                halfTile.IsHalfBlock = true;

                if (halfTile.TileType == TileID.Silt || liquidTile.TileType == TileID.Sand)
                    liquidTile.TileType = TileID.Stone;

                Tile silt = Main.tile[i + dir + dir, j - 1];
                if (silt.TileType == TileID.Silt || silt.TileType == TileID.Sand)
                    silt.TileType = TileID.Stone;

                silt = Main.tile[i + dir, j];
                if (silt.TileType == TileID.Silt || silt.TileType == TileID.Sand)
                    silt.TileType = TileID.Stone;

                int adjY = 0;
                while (Main.tile[i - dir, j - 1 + adjY].HasTile)
                    Main.tile[i - dir, j - 1 + adjY++].ClearTile();
            }

            const int MaxHeight = 16;

            for (int j = y; j > y - MaxHeight; --j)
            {
                if (Math.Abs(j - y) < minHeight)
                    continue;

                if (CanPlaceAt(x, j, -1))
                {
                    PlaceAt(x, j, -1);
                    return;
                }
                else if (CanPlaceAt(x, j, 1))
                {
                    PlaceAt(x, j, 1);
                    return;
                }
            }
        }

        public static void AddSurfaceVerdant()
        {
            int offset = 0;
        retry:
            int x = (VerdantArea.Center.X + (WorldGen.genRand.NextBool() ? -offset : offset));
            int top = Helper.FindDown(new Vector2(x, 200) * 16);
            Point16 size = Point16.Zero;

            if (!StructureHelper.Generator.GetDimensions("World/Structures/SurfaceTree", VerdantMod.Instance, ref size))
                return;

            if (top <= Main.worldSurface * 0.36f)
            {
                offset += offset + WorldGen.genRand.Next(10, 20);
                goto retry;
            }

            Point16 spawnPos = new(x, top - size.Y + 12);

            if (Helper.TileRectangle(spawnPos.X, spawnPos.Y, size.X, size.Y, TileID.Cloud, TileID.RainCloud) > 0)
            {
                offset += offset + WorldGen.genRand.Next(10, 20);
                goto retry;
            }

            int tryRepeats = 0;
            while (tryRepeats < 20 && Helper.AnyTileRectangle(spawnPos.X - 6, spawnPos.Y + tryRepeats++, size.X, size.Y + 36) < 20) { }
            
            if (tryRepeats >= 20)
            {
                offset += offset + WorldGen.genRand.Next(10, 20);
                goto retry;
            }

            StructureHelper.Generator.GenerateStructure("World/Structures/SurfaceTree", spawnPos + new Point16(0, tryRepeats), VerdantMod.Instance);
        }

        public void VerdantCleanup(GenerationProgress p, GameConfiguration config)
        {
            p.Message = "Trimming plants...";

            AddFlowerStructures();
            PlaceApotheosis();

            for (int i = VerdantArea.Right; i > VerdantArea.X; --i)
            {
                for (int j = VerdantArea.Bottom; j > VerdantArea.Y; --j)
                {
                    Tile tile = Main.tile[i, j];
                    tile.LiquidType = LiquidID.Water;

                    Tile t = Framing.GetTileSafely(i, j);
                    int[] vineAnchors = new int[] { ModContent.TileType<VerdantVine>(), ModContent.TileType<VerdantGrassLeaves>(), ModContent.TileType<VerdantLeaves>() };
                    if (t.TileType == ModContent.TileType<VerdantVine>() && !vineAnchors.Contains(Framing.GetTileSafely(i, j - 1).TileType))
                        WorldGen.KillTile(i, j);
                }
            }
        }

        readonly static int[] InvalidTypes = new int[] { TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick, TileID.LihzahrdBrick };
        readonly static int[] InvalidWalls = new int[] { WallID.BlueDungeonSlabUnsafe, WallID.BlueDungeonUnsafe, WallID.BlueDungeonTileUnsafe, WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonTileUnsafe,
                WallID.GreenDungeonUnsafe, WallID.PinkDungeonUnsafe, WallID.PinkDungeonTileUnsafe, WallID.PinkDungeonSlabUnsafe };

        private static void PlaceApotheosis()
        {
            Point apothPos = new(VerdantArea.Center.X - 10, VerdantArea.Center.Y - 4);
            int side = WorldGen.genRand.NextBool(2) ? -1 : 1;
            bool triedOneSide = false;

        redo:
            for (int i = 0; i < 20; ++i)
            {
                for (int j = 0; j < 18; ++j)
                {
                    Tile t = Framing.GetTileSafely(apothPos.X + i, apothPos.Y + j);
                    if (t.HasTile && (InvalidTypes.Contains(t.TileType) || InvalidWalls.Contains(t.WallType))) 
                    {
                        apothPos.X += WorldGen.genRand.Next(20, 27) * side;
                        if (!VerdantArea.Contains(apothPos) && !triedOneSide)
                        {
                            triedOneSide = true;
                            apothPos = new Point(VerdantArea.Center.X - 30, VerdantArea.Center.Y - 4);
                            side *= -1;
                        }
                        goto redo; //sorry but i had to
                    }
                }
            }
            StructureHelper.Generator.GenerateStructure("World/Structures/Apotheosis", new Point16(apothPos.X, apothPos.Y), VerdantMod.Instance);
        }

        private void AddFlowerStructures()
        {
            Point[] offsets = new Point[3] { new Point(7, -1), new Point(3, 0), new Point(3, 0) }; //ruler in-game is ONE HIGHER on both planes

            var list = InvalidTypes.ToList();
            list.Add(ModContent.TileType<Apotheosis>());
            int[] invalids = list.ToArray();

            int[] valids = new int[] { ModContent.TileType<VerdantGrassLeaves>(), ModContent.TileType<LushSoil>() };

            List<Vector2> positions = new() { new Vector2(VerdantArea.Center.X - 10, VerdantArea.Center.Y - 4) }; //So I don't overlap with the Apotheosis
            int attempts = 0;

            for (int i = 0; i < 9 * WorldSize; ++i)
            {
                int index = Main.rand.Next(offsets.Length);
                Point16 pos = new(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));

                bool notNear = !positions.Any(x => Vector2.DistanceSquared(x, pos.ToVector2()) < 20 * 20);

                if (notNear && Helper.TileRectangle(pos.X, pos.Y, 20, 10, valids) > 4 && Helper.TileRectangle(pos.X, pos.Y, 20, 10, invalids) <= 0 && Helper.NoTileRectangle(pos.X, pos.Y, 20, 10) > 40)
                {
                    StructureHelper.Generator.GenerateMultistructureSpecific("World/Structures/Flowers", pos, Mod, index);
                    positions.Add(pos.ToVector2());
                }
                else
                {
                    i--;

                    if (attempts++ > 500)
                        return;
                    continue;
                }
            }
        }

        private static void AddStone()
        {
            for (int i = 0; i < 50 * WorldSize; ++i) //Stones
            {
                Point p = new(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                while (!TileHelper.ActiveType(p.X, p.Y, ModContent.TileType<LushSoil>()))
                    p = new Point(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                WorldGen.TileRunner(p.X, p.Y, WorldGen.genRand.NextFloat(7, 15), WorldGen.genRand.Next(5, 15), TileID.Stone, false, 0, 0, false, true);
            }

            for (int i = 0; i < 12 * WorldSize; ++i) //Ores
            {
                Point p = new(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                while (!TileHelper.ActiveType(p.X, p.Y, ModContent.TileType<LushSoil>()))
                    p = new Point(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                WorldGen.TileRunner(p.X, p.Y, WorldGen.genRand.NextFloat(2, 8), WorldGen.genRand.Next(5, 15), TileID.Gold, false, 0, 0, false, true);

                p = new Point(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                while (!TileHelper.ActiveType(p.X, p.Y, ModContent.TileType<LushSoil>()))
                    p = new Point(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                WorldGen.TileRunner(p.X, p.Y, WorldGen.genRand.NextFloat(2, 8), WorldGen.genRand.Next(5, 15), TileID.Platinum, false, 0, 0, false, true);
            }
        }

        private static void AddWater()
        {
            for (int i = 0; i < 30 * WorldSize; ++i)
            {
                Point p = new(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                for (int j = -14; j < 14; ++j)
                {
                    for (int k = -14; k < 14; ++k)
                    {
                        Tile tile = Main.tile[p.X + j, p.Y + k];
                        tile.LiquidAmount = 255;
                        tile.LiquidType = 0;
                    }
                }
            }
        }

        private static void Vines()
        {
            for (int i = 0; i < 240 * WorldSize; ++i)
            {
                Point rP = new(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                Point adj = TileHelper.GetRandomOpenAdjacent(rP.X, rP.Y);
                while (adj == new Point(-2, -2) || adj == new Point(0, -1) || adj == new Point(0, 1))
                {
                    rP = new Point(WorldGen.genRand.Next(VerdantArea.X, VerdantArea.Right), WorldGen.genRand.Next(VerdantArea.Y, VerdantArea.Bottom));
                    adj = TileHelper.GetRandomOpenAdjacent(rP.X, rP.Y);
                }
                Tile tile = Framing.GetTileSafely(rP.X, rP.Y);

                if (tile.TileType == ModContent.TileType<VerdantGrassLeaves>() || tile.TileType == ModContent.TileType<LushSoil>() || tile.TileType == ModContent.TileType<LivingLushWood>())
                {
                    Point adjPos = rP.Add(adj);
                    Point end = adjPos.Add(adj);

                    while (true)
                    {
                        end = end.Add(adj);
                        if (TileHelper.SolidTile(end.X - adj.X, end.Y - adj.Y))
                            break;
                    }
                    
                    int midPointY = ((adjPos.Y + end.Y) / 2) + WorldGen.genRand.Next(10, 20);
                    int thickness = WorldGen.genRand.Next(1, 4);

                    if (WorldGen.genRand.Next(3) > 0)
                    {
                        for (int k = 0; k < thickness; ++k)
                            GenHelper.GenBezierDirectWall(new double[] {
                                adjPos.X, adjPos.Y - k,
                                ((adjPos.X + end.X) / 2), midPointY - k,
                                end.X, end.Y - k,
                            }, 200, ModContent.WallType<VerdantVineWall_Unsafe>(), true, 1);
                    }
                    else
                    {
                        for (int k = 0; k < thickness; ++k)
                            GenHelper.GenBezierDirect(new double[] {
                                adjPos.X, adjPos.Y - k,
                                ((adjPos.X + end.X) / 2), midPointY - k,
                                end.X, end.Y - k,
                            }, 200, ModContent.TileType<VerdantLeaves>(), false, 1);
                    }
                }
                else if (WorldGen.genRand.Next(4) > 0)
                    i--;
            }
        }

        private static void AddPlants()
        {
            LoopTrees();

            for (int i = VerdantArea.X; i < VerdantArea.Right; ++i)
            {
                for (int j = VerdantArea.Y; j < VerdantArea.Bottom; ++j)
                {
                    bool puff = VerdantGrassLeaves.CheckPuff(i, j, 1.25f);

                    if (TileHelper.ActiveType(i, j, ModContent.TileType<VerdantGrassLeaves>()))
                    {
                        //Vines
                        if (!Framing.GetTileSafely(i, j + 1).HasTile && !Framing.GetTileSafely(i, j + 1).BottomSlope && WorldGen.genRand.Next(5) <= 2)
                        {
                            int length = WorldGen.genRand.Next(4, 20);
                            bool strong = WorldGen.genRand.NextBool(10);

                            int type = strong ? ModContent.TileType<VerdantStrongVine>() : ModContent.TileType<VerdantVine>();
                            if (puff)
                                type = ModContent.TileType<PuffVine>();

                            for (int l = 1; l < length; ++l)
                            {
                                if (Framing.GetTileSafely(i, j + l + 1).HasTile)
                                    break;

                                WorldGen.KillTile(i, j + l, false, false, true);
                                WorldGen.PlaceTile(i, j + l, type, true, true);
                                Framing.GetTileSafely(i, j + l).TileType = (ushort)type;

                                if (strong)
                                    Framing.GetTileSafely(i, j + l).TileFrameY = (short)(Main.rand.Next(4) * 18);
                            }
                            continue;
                        }
                    }

                    //lightbulb
                    bool doPlace = Helper.AreaClear(i, j - 2, 2, 2) && TileHelper.ActiveTypeNoTopSlope(i, j, ModContent.TileType<VerdantGrassLeaves>()) && TileHelper.ActiveTypeNoTopSlope(i + 1, j, ModContent.TileType<VerdantGrassLeaves>());
                    if (doPlace && WorldGen.genRand.NextBool(11))
                    {
                        WorldGen.PlaceTile(i, j - 2, ModContent.TileType<VerdantLightbulb>(), true, false, -1, WorldGen.genRand.Next(3));
                        continue;
                    }

                    //weeping bud
                    doPlace = Helper.AreaClear(i - 1, j + 1, 3, 2) && TileHelper.ActiveTypeNoBottomSlope(i, j, ModContent.TileType<VerdantGrassLeaves>());
                    if (doPlace && WorldGen.genRand.NextBool(32))
                    {
                        WorldGen.PlaceTile(i, j + 1, ModContent.TileType<WaterPlant>(), true, true);
                        continue;
                    }

                    //beehive
                    doPlace = Helper.AreaClear(i, j - 2, 2, 2) && TileHelper.ActiveTypeNoTopSlope(i, j, ModContent.TileType<VerdantGrassLeaves>()) && TileHelper.ActiveTypeNoTopSlope(i + 1, j, ModContent.TileType<VerdantGrassLeaves>());
                    if (doPlace && WorldGen.genRand.NextBool(40))
                    {
                        WorldGen.PlaceTile(i, j - 2, ModContent.TileType<Beehive>(), true, false);
                        continue;
                    }

                    if (puff && PuffDecor(i, j))
                        continue;
                    else if (!puff && NormalDecor(i, j))
                        continue;

                    //flower wall 2x2
                    doPlace = Helper.AreaClear(i, j, 2, 2) && Helper.WalledSquare(i, j, 2, 2) && Helper.WalledSquareType(i, j, 2, 2, WallTypes[0]);
                    if (doPlace && WorldGen.genRand.NextBool(42))
                    {
                        int type = WorldGen.genRand.NextBool(13) ? ModContent.TileType<MountedLightbulb_2x2>() : ModContent.TileType<Flower_2x2>();
                        GenHelper.PlaceMultitile(new Point(i, j), type, WorldGen.genRand.Next(type == ModContent.TileType<MountedLightbulb_2x2>() ? 2 : 4));
                        continue;
                    }

                    //flower wall 3x3
                    doPlace = Helper.AreaClear(i, j, 3, 3) && Helper.WalledSquare(i, j, 3, 3) && Helper.WalledSquareType(i, j, 3, 3, WallTypes[0]);
                    if (doPlace && WorldGen.genRand.NextBool(68))
                    {
                        GenHelper.PlaceMultitile(new Point(i, j), ModContent.TileType<Flower_3x3>(), WorldGen.genRand.Next(2));
                        continue;
                    }
                }
            }
        }

        private static void LoopTrees()
        {
            for (int i = VerdantArea.X; i < VerdantArea.Right; ++i)
            {
                for (int j = VerdantArea.Y; j < VerdantArea.Bottom; ++j) //Loop explicitly for trees & puffs so they get all the spawns they need
                {
                    //Trees
                    bool doPlace = true;

                    for (int k = -1; k < 2; ++k)
                    {
                        bool anyConditions = !TileHelper.ActiveTypeNoTopSlope(i + k, j, TileTypes[0]) || !WorldGen.TileEmpty(i + k, j - 1);
                        if (anyConditions)
                        {
                            doPlace = false;
                            break;
                        }
                    }

                    if (!WorldGen.TileEmpty(i, j - 2))
                        doPlace = false;

                    if (doPlace && WorldGen.genRand.NextBool(24))
                        VerdantTree.Spawn(i, j - 1, -1, WorldGen.genRand, 6, 12, false, -1, false);

                    //Puffs
                    doPlace = Helper.AreaClear(i, j, 2, 3) && TileHelper.ActiveTypeNoTopSlope(i, j + 3, ModContent.TileType<VerdantGrassLeaves>()) &&
                        TileHelper.ActiveTypeNoTopSlope(i + 1, j + 3, ModContent.TileType<VerdantGrassLeaves>());
                    if (doPlace && WorldGen.genRand.NextBool(60))
                    {
                        WorldGen.PlaceObject(i, j + 1, ModContent.TileType<BigPuff>(), true);

                        int pickipuffs = WorldGen.genRand.Next(1, 4);
                        for (int k = 0; k < pickipuffs; ++k)
                        {
                            int x = i - WorldGen.genRand.Next(-10, 11);
                            int y = Helper.FindUpWithType(new Point(x, j), ModContent.TileType<VerdantGrassLeaves>());

                            if (y != -1)
                                ModContent.GetInstance<Tiles.TileEntities.Puff.Pickipuff>().Place(x, y);
                        }
                    }
                }
            }
        }

        private static bool NormalDecor(int i, int j)
        {
            //decor 1x1
            if (!Framing.GetTileSafely(i, j - 1).HasTile && TileHelper.ActiveTypeNoTopSlope(i, j, ModContent.TileType<VerdantGrassLeaves>()) && WorldGen.genRand.Next(5) >= 1)
            {
                int type = Main.rand.NextBool(2) ? ModContent.TileType<VerdantDecor1x1>() : ModContent.TileType<VerdantDecor1x1NoCut>();
                WorldGen.PlaceTile(i, j - 1, type, true, true, style: WorldGen.genRand.Next(7));
                return true;
            }

            //ground decor 2x1
            bool doPlace = !Framing.GetTileSafely(i, j - 1).HasTile && TileHelper.ActiveTypeNoTopSlope(i, j, ModContent.TileType<VerdantGrassLeaves>()) &&
                !Framing.GetTileSafely(i + 1, j - 1).HasTile && TileHelper.ActiveTypeNoTopSlope(i + 1, j, ModContent.TileType<VerdantGrassLeaves>());
            if (doPlace && WorldGen.genRand.NextBool(2))
            {
                GenHelper.PlaceMultitile(new Point(i, j - 1), ModContent.TileType<VerdantDecor2x1>(), WorldGen.genRand.Next(7));
                return true;
            }
            return false;
        }

        private static bool PuffDecor(int i, int j)
        {
            //decor 1x1
            if (!Framing.GetTileSafely(i, j - 1).HasTile && TileHelper.ActiveTypeNoTopSlope(i, j, ModContent.TileType<VerdantGrassLeaves>()) && WorldGen.genRand.Next(8) >= 1)
            {
                WorldGen.PlaceTile(i, j - 1, ModContent.TileType<PuffDecor1x1>(), true, false, -1, WorldGen.genRand.Next(7));
                return true;
            }
            return false;
        }

        private void GenerateCircles()
        {
            float repeats = 8 * WorldSize;

            VerdantArea = new Rectangle(VerdantArea.Center.X - (int)(3 * WorldSize * WorldGen.genRand.Next(75, 85)) - 20, VerdantArea.Center.Y - 100, (int)(8 * WorldSize * WorldGen.genRand.Next(75, 85)), 200);
            VerdantArea.Location = new Point(VerdantArea.Location.X - 40, VerdantArea.Location.Y - 40);
            VerdantArea.Width += 80;
            VerdantArea.Height += 80;

            VerdantCircles.Clear();
            for (int i = 0; i < repeats; ++i)
            {
                int x = (int)MathHelper.Lerp(VerdantArea.X + 50, VerdantArea.Right - 50,  i / repeats);
                int y = VerdantArea.Center.Y - WorldGen.genRand.Next(-20, 20);

                VerdantCircles.Add(new GenCircle((int)(WorldGen.genRand.Next(50, 80) * WorldSize), new Point16(x, y)));
            }

            for (int i = 0; i < VerdantCircles.Count; ++i)
                VerdantCircles[i].FindTiles();
        }

        private void CleanForCaves()
        {
            const float Buffer = 3f;

            //Caves
            VerdantSystem.genNoise = new FastNoise(WorldGen._genRandSeed);
            VerdantSystem.genNoise.Seed = WorldGen._genRandSeed;
            VerdantSystem.genNoise.Frequency = 0.05f;
            VerdantSystem.genNoise.NoiseType = FastNoise.NoiseTypes.CubicFractal; //Sets noise to proper type
            VerdantSystem.genNoise.FractalType = FastNoise.FractalTypes.Billow;

            int startX = VerdantArea.Center.X - (int)(Main.maxTilesX / Buffer);
            int endX = VerdantArea.Center.X + (int)(Main.maxTilesX / Buffer);

            int startY = VerdantArea.Center.Y - (int)(Main.maxTilesY / (Buffer * 2));
            int endY = VerdantArea.Center.Y + (int)(Main.maxTilesY / (Buffer * 2));

            List<Point16> aggregateTiles = new();

            foreach (var item in VerdantCircles)
                aggregateTiles.AddRange(item.tiles);

            aggregateTiles = aggregateTiles.Distinct().ToList();

            GetVerdantArea(aggregateTiles);

            foreach (var point in aggregateTiles)
            {
                Tile t = Framing.GetTileSafely(point.X, point.Y);
                float n = VerdantSystem.genNoise.GetNoise(point.X, point.Y);
                t.ClearEverything();
                if (n < -0.67f) { }
                else if (n < -0.57f)
                    WorldGen.PlaceTile(point.X, point.Y, TileTypes[0]);
                else
                    WorldGen.PlaceTile(point.X, point.Y, TileTypes[1]);

                if (n < -0.85f)
                    WorldGen.KillWall(point.X, point.Y, false);
                else if (n < -0.52f)
                    WorldGen.PlaceWall(point.X, point.Y, WallTypes[0]);
                else
                    WorldGen.PlaceWall(point.X, point.Y, WallTypes[1]);
            }

            VerdantSystem.genNoise.Seed = WorldGen._genRandSeed;
            VerdantSystem.genNoise.Frequency = 0.014f;
            VerdantSystem.genNoise.NoiseType = FastNoise.NoiseTypes.ValueFractal;
            VerdantSystem.genNoise.FractalType = FastNoise.FractalTypes.Billow;
            VerdantSystem.genNoise.InterpolationMethod = FastNoise.Interp.Quintic;

            foreach (var point in aggregateTiles)
            {
                Tile t = Framing.GetTileSafely(point.X, point.Y);
                float n = VerdantSystem.genNoise.GetNoise(point.X, point.Y);
                if (t.WallType == WallTypes[0] && n < -0.4f)
                    GenHelper.ReplaceWall(point, WallTypes[2]);

                if (n < -0.72f && TileTypes.Any(x => x == t.TileType) && t.TileType != TileTypes[0] && t.HasTile)
                    GenHelper.ReplaceTile(point, TileTypes[4]);
            }
        }

        private void GetVerdantArea(List<Point16> aggregateTiles)
        {
            int left = Main.maxTilesX;
            int right = 0;
            int top = Main.maxTilesY;
            int bottom = 0;

            foreach (var point in aggregateTiles)
            {
                if (point.X < left)
                    left = point.X;

                if (point.X > right)
                    right = point.X;

                if (point.Y < top)
                    top = point.Y;

                if (point.Y > bottom)
                    bottom = point.Y;
            }

            VerdantArea = new Rectangle(left, top, right - left, bottom - top);
        }

        public override void PostWorldGen() //Final cleanup
        {
            for (int i = VerdantArea.Right; i > VerdantArea.X; --i)
            {
                for (int j = VerdantArea.Bottom; j > VerdantArea.Y; --j)
                {
                    if (TileHelper.ActiveType(i, j, ModContent.TileType<VerdantLillie>()) && Framing.GetTileSafely(i, j).LiquidAmount < 155)
                        WorldGen.KillTile(i, j, false, false, true);

                    if (TileHelper.ActiveType(i, j, ModContent.TileType<VerdantTree>()) && !TileHelper.ActiveType(i, j + 1, ModContent.TileType<VerdantTree>()) && !TileHelper.ActiveType(i, j + 1, ModContent.TileType<VerdantGrassLeaves>()))
                        WorldGen.KillTile(i, j, false, false, true);
                }
            }

            for (int i = VerdantArea.X; i < VerdantArea.Right; ++i)
            {
                for (int j = VerdantArea.Y; j < VerdantArea.Bottom; ++j)
                {
                    if (TileHelper.ActiveType(i, j, ModContent.TileType<VerdantStrongVine>()) && !TileHelper.ActiveType(i, j - 1, ModContent.TileType<VerdantStrongVine>()) && !TileHelper.ActiveType(i, j - 1, ModContent.TileType<VerdantGrassLeaves>()))
                        WorldGen.KillTile(i, j, false, false, true);
                }
            }
        }
    }
}