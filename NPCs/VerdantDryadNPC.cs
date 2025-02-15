﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Verdant.Items.Verdant.Blocks.Misc;
using Verdant.Items.Verdant.Blocks.Plants;
using Verdant.Items.Verdant.Misc;

namespace Verdant.NPCs;

class VerdantDryadNPC : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type == NPCID.Dryad;

    public override void SetupShop(int type, Chest shop, ref int nextSlot)
    {
        if (type != NPCID.Dryad)
            return;

        if (!ModContent.GetInstance<VerdantSystem>().microcosmUsed)
            shop.item[nextSlot++].SetDefaults(ModContent.ItemType<Microcosm>());

        if (Main.LocalPlayer.GetModPlayer<VerdantPlayer>().ZoneVerdant && ModContent.GetInstance<VerdantSystem>().apotheosisEvilDown)
            shop.item[nextSlot++].SetDefaults(ModContent.ItemType<LightbulbSeeds>());

        if (Main.LocalPlayer.GetModPlayer<VerdantPlayer>().ZoneVerdant)
        {
            shop.item[nextSlot++].SetDefaults(ModContent.ItemType<ApotheoticPaintingItem>());
            shop.item[nextSlot++].SetDefaults(ModContent.ItemType<LightbulbPaintingItem>());
        }
    }
}
