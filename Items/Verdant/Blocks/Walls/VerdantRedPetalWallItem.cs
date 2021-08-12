﻿using Terraria.ID;
using Terraria.ModLoader;
using Verdant.Items.Verdant.Materials;
using Verdant.Walls.Verdant;
using static Terraria.ModLoader.ModContent;

namespace Verdant.Items.Verdant.Blocks.Walls
{
    public class VerdantRedPetalWallItem : ModItem
    {
        public override void SetStaticDefaults() => QuickItem.SetStatic(this, "Red Petal Wall", "");
        public override void SetDefaults() => QuickItem.SetWall(this, 16, 16, WallType<VerdantRedPetalWall>());
        public override void AddRecipes() => QuickItem.AddRecipe(this, mod, TileID.WorkBenches, 1, (ItemType<RedPetal>(), 4));
    }
}
