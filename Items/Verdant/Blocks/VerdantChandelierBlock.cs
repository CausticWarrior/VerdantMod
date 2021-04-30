﻿using System;
using Terraria.ID;
using Terraria.ModLoader;
using Verdant.Items.Verdant.Materials;
using static Terraria.ModLoader.ModContent;

namespace Verdant.Items.Verdant.Blocks
{
    public class VerdantChandelierBlock : ModItem
    {
        public override void SetStaticDefaults() => QuickItem.SetStatic(this, "Lush Chandelier");
        public override void SetDefaults() => QuickItem.SetBlock(this, 32, 42, TileType<Tiles.Verdant.Decor.VerdantFurniture.VerdantChandelier>());
        public override void AddRecipes() => QuickItem.AddRecipe(this, mod, TileID.WorkBenches, 1, (ItemType<RedPetal>(), 6), (ItemType<VerdantStrongVineMaterial>(), 4), (ItemType<Lightbulb>(), 3));
    }
}