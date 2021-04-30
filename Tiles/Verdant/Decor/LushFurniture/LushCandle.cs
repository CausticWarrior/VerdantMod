﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Verdant.Tiles.Verdant.Decor.LushFurniture
{
    internal class LushCandle : ModTile
    {
        public override void SetDefaults()
        {
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileWaterDeath[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
            TileObjectData.newTile.WaterDeath = true;
            TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.LavaPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(253, 221, 3), CreateMapEntryName());
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            base.KillTile(i, j, ref fail, ref effectOnly, ref noItem);
        }

        public override void HitWire(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            int topY = j - tile.frameY / 18 % 3;
            short frameAdjustment = (short)(tile.frameX > 0 ? -18 : 18);
            Main.tile[i, topY].frameX += frameAdjustment;
            Main.tile[i, topY + 1].frameX += frameAdjustment;
            Main.tile[i, topY + 2].frameX += frameAdjustment;
            Wiring.SkipWire(i, topY);
            Wiring.SkipWire(i, topY + 1);
            Wiring.SkipWire(i, topY + 2);
            NetMessage.SendTileSquare(-1, i, topY + 1, 3, TileChangeType.None);
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects spriteEffects) => spriteEffects = i % 2 == 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Framing.GetTileSafely(i, j).frameX == 0)
            {
                r = 1f;
                g = 0.75f;
                b = 1f;
            }
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref Color drawColor, ref int nextSpecialDrawIndex)
        {
            if (!Main.gamePaused && Main.instance.IsActive && (!Lighting.UpdateEveryFrame || Main.rand.NextBool(4)))
            {
                Tile tile = Main.tile[i, j];
                if (Main.rand.NextBool(40) && tile.frameX == 0 && tile.frameY / 18 % 3 == 0)
                {
                    int dust = Dust.NewDust(new Vector2(i * 16 + 4, j * 16 + 2), 4, 4, DustID.Fire, 0f, 0f, 100, default, 1f);
                    if (Main.rand.Next(3) != 0)
                        Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.3f;
                    Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 1.5f;
                }
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (Framing.GetTileSafely(i, j).frameX != 0)
                return;

            SpriteEffects effects = SpriteEffects.None;
            if (i % 2 == 1)
                effects = SpriteEffects.FlipHorizontally;

            Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
            if (Main.drawToScreen)
                zero = Vector2.Zero;

            Tile tile = Main.tile[i, j];
            int width = 16;
            int offsetY = 0;
            int height = 16;
            int offsetX = i % 2 == 0 ? 3 : -3;
            TileLoader.SetDrawPositions(i, j, ref width, ref offsetY, ref height);
            var flameTexture = Main.FlameTexture[0];// mod.GetTexture("Tiles/ExampleLamp_Flame"); // We could also reuse Main.FlameTexture[] textures, but using our own texture is nice.

            ulong seed = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (uint)i);
            // We can support different flames for different styles here: int style = Main.tile[j, i].frameY / 54;
            for (int c = 0; c < 7; c++)
            {
                float shakeX = Utils.RandomInt(ref seed, -10, 11) * 0.15f;
                float shakeY = Utils.RandomInt(ref seed, -10, 1) * 0.35f;
                Vector2 pos = new Vector2(i * 16 - (int)Main.screenPosition.X - (width - 16f) / 2f + shakeX, j * 16 - (int)Main.screenPosition.Y + offsetY + shakeY) + zero;
                Main.spriteBatch.Draw(flameTexture, pos + new Vector2(-offsetX, 2), new Rectangle(tile.frameX, tile.frameY, 16, 16), new Color(100, 100, 100, 0), 0f, default, 1f, effects, 0f);
            }
        }
    }
}
