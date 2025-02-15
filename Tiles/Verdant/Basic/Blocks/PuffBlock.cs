﻿using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Verdant.Items.Verdant.Blocks;
using Verdant.Items.Verdant.Blocks.Puff;

namespace Verdant.Tiles.Verdant.Basic.Blocks;

internal class PuffBlock : ModTile
{
    public override void SetStaticDefaults()
    {
        QuickTile.SetAll(this, 0, DustID.PinkStarfish, SoundID.NPCHit11, new Color(255, 112, 202), ModContent.ItemType<PuffBlockItem>(), "", true, false);
        QuickTile.MergeWith(Type, TileID.Dirt, TileID.Mud, ModContent.TileType<VerdantGrassLeaves>(), ModContent.TileType<VerdantPinkPetal>(), ModContent.TileType<VerdantRedPetal>());
    }
}