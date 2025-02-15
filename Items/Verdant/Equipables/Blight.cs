﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Verdant.Items.Verdant.Equipables
{
    class Blight : ModItem
    {
        public override bool IsLoadingEnabled(Mod mod) => false;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blightlight");
            Tooltip.SetDefault("WIP - WILL NOT BE FINISHED BEFORE RELEASE\nAn old growth that suffered inside of a chest\nSpreads glowing infection to nearby enemies");
        }

        public override void SetDefaults()
        {
            QuickItem.SetLightPet(this, 38, 32);
        }

        public override bool CanUseItem(Player player) => player.miscEquips[1].IsAir;
    }
}
