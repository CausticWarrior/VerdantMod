﻿using Terraria;
using Terraria.ModLoader;

namespace Verdant.Items.Verdant.Critter
{
    [Sacrifice(5)]
    class RedGrassSnail : ModItem
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Red Petal Snail");
        public override void SetDefaults() => QuickItem.SetCritter(this, 34, 34, ModContent.NPCType<NPCs.Passive.VerdantRedGrassSnail>(), 1, 15);
        public override bool CanUseItem(Player player) => QuickItem.CanCritterSpawnCheck();
    }
}
