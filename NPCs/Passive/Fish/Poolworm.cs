﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Verdant.Items.Verdant.Critter.Fish;

namespace Verdant.NPCs.Passive.Fish
{
    public class Poolworm : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcCatchable[Type] = true;
            Main.npcFrameCount[Type] = 3;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 12;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 5;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.dontTakeDamage = false;
            NPC.value = 0f;
            NPC.aiStyle = 16;
            NPC.dontCountMe = true;
            NPC.catchItem = (short)ModContent.ItemType<PoolwormItem>();

            AIType = NPCID.Goldfish;
            SpawnModBiomes = new int[1] { ModContent.GetInstance<Scenes.VerdantBiome>().Type };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                new FlavorTextBestiaryInfoElement("The smallest aquatic critter in the Verdant. Makes for great bait."),
            });
        }

        public override bool PreAI()
        {
            if (NPC.velocity.X > 0) 
                NPC.spriteDirection = 1;
            else 
                NPC.spriteDirection = -1;

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            int frame = (int)(NPC.frameCounter % 48 / 12);

            if (frame == 2)
                frame = 0;
            else if (frame == 3)
                frame = 2;

            NPC.frame.Y = frame * frameHeight;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 6; ++i)
                    Dust.NewDust(NPC.Center, 26, 18, DustID.Grass, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3));
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => ((spawnInfo.Player.GetModPlayer<VerdantPlayer>().ZoneVerdant && spawnInfo.Water) ? 1.75f : 0f) * (spawnInfo.PlayerInTown ? 1.25f : 1f);
    }
}