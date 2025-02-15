﻿using Terraria.ID;
using Terraria.ModLoader;
using Verdant.Items.Verdant.Materials;
using Verdant.Walls;

namespace Verdant.Items.Verdant.Blocks.Walls
{
    public class VerdantVineWallItem : ModItem
    {
        public override void SetStaticDefaults() => QuickItem.SetStatic(this, "Lush Vine Wall", "");
        public override void SetDefaults() => QuickItem.SetWall(this, 32, 32, ModContent.WallType<VerdantVineWall>());
        public override void AddRecipes() => QuickItem.AddRecipe(this, TileID.WorkBenches, 4, (ModContent.ItemType<LushLeaf>(), 1));
    }
}