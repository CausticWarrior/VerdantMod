﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Verdant.Projectiles.Verdant.Minion
{
    class VerdantSnailMinion : ModProjectile
    {
        ref float MovementState => ref projectile.ai[0];
        ref float Timer => ref projectile.ai[1];
        public int Target = -1;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Snale");
            Main.projFrames[projectile.type] = 5;
            Main.projPet[projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[projectile.type] = true;
            //ProjectileID.Sets.TrailCacheLength[projectile.type] = 9;
            //ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.CloneDefaults(ProjectileID.OneEyedPirate);
            projectile.aiStyle = -1;
            projectile.width = 30;
            projectile.height = 22;
            projectile.tileCollide = true;
            projectile.ignoreWater = false;
            projectile.minionSlots = 0.75f;
            projectile.minion = true;
            projectile.hostile = false;
            projectile.friendly = true;
            projectile.penetrate = -1;

            aiType = 0;
        }

        public override bool MinionContactDamage() => true;

        public override bool? CanCutTiles() => false;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (MovementState == 4)
                projectile.velocity = -projectile.velocity.RotatedBy(Main.rand.Next(0, 16) * 0.005f) * 0.94f;
            return false;
        }

        public const int AnimSpeedMult = 14;
        public const int AnimSpeedMultHasty = 6;
        public const int DistanceUntilReturn = 700;

        public override void AI()
        {
            Player p = Main.player[projectile.owner];
            Timer++;

            projectile.friendly = MovementState == 4;

            if (MovementState == 0) //Spawn
            {
                projectile.spriteDirection = Main.rand.NextBool(2) ? -1 : 1;
                MovementState = 1;
                Timer--;
                Target = -1;
            }
            if (MovementState == 1) //Literally vibing too hard
            {
                projectile.tileCollide = true;
                if (Timer == AnimSpeedMult)
                    projectile.frame = 1;
                if (Timer == 2 * AnimSpeedMult)
                {
                    projectile.frame = 0;
                    projectile.velocity.X = 0.25f * projectile.spriteDirection;
                }
                if (Timer == 3 * AnimSpeedMult)
                {
                    projectile.frame = 2;
                    if (projectile.velocity.X == 0)
                        projectile.spriteDirection *= -1;
                }
                if (Timer == 4 * AnimSpeedMult)
                {
                    projectile.velocity.X = 0f;
                    projectile.frame = 0;
                    Timer = 0;
                }

                if (Vector2.Distance(p.position, projectile.position) > DistanceUntilReturn)
                {
                    MovementState = 2;
                    Timer = 0;
                }

                // --------------------- GET TARGET ----------------------
                if (Target == -1)
                {
                    int hasTarget = -1;
                    for (int i = 0; i < Main.npc.Length; ++i)
                    {
                        float dist = Vector2.Distance(Main.npc[i].position, projectile.position);
                        if (Main.npc[i].active && !Main.npc[i].friendly && dist < 500 && Collision.CanHitLine(projectile.position, projectile.width, projectile.height, Main.npc[i].position, Main.npc[i].width, Main.npc[i].height) &&
                            (hasTarget == -1 || (hasTarget != -1 && Vector2.Distance(Main.npc[hasTarget].position, projectile.position) < dist)))
                            hasTarget = i;
                    }

                    if (hasTarget != -1)
                    {
                        Target = hasTarget;
                        MovementState = 4;
                        Timer = 0;
                        projectile.frame = 0;
                        projectile.velocity *= 0f;
                    }
                }
                else
                {
                    if (Target < -1)
                        Target++;
                }

                //Gravity
                projectile.velocity.Y += 0.2f;
            }
            else if (MovementState == 2) //Catch up to player
            {
                projectile.tileCollide = false;
                projectile.spriteDirection = p.position.X < projectile.position.X ? -1 : 1;
                if (Timer == AnimSpeedMultHasty)
                    projectile.frame = 2;
                if (Timer == AnimSpeedMultHasty * 2)
                    projectile.frame = 3;
                if (Timer == AnimSpeedMultHasty * 3)
                    projectile.frame = 4;
                if (Timer > AnimSpeedMultHasty * 3)
                {
                    float adjTimer = Timer - (AnimSpeedMultHasty * 3);
                    float mult = 1f;
                    if (adjTimer <= 60)
                        mult = adjTimer / 60f;
                    projectile.velocity = Vector2.Normalize(p.position - projectile.position) * 7 * mult;
                    projectile.rotation += 0.4f * mult;

                    if (Vector2.Distance(p.position, projectile.position) < DistanceUntilReturn * 0.6f)
                    {
                        MovementState = 3;
                        Timer = 0;
                    }
                }
            }
            else if (MovementState == 3) //Deccelerate
            {
                projectile.tileCollide = true;
                float mult = 1 - (Timer / 240f);
                projectile.velocity.X *= mult;
                projectile.velocity.Y += 0.2f;
                projectile.rotation += 0.4f * mult;

                if (Timer > 60)
                {
                    Timer = 0;
                    MovementState = 1;
                    projectile.rotation = 0;
                    projectile.spriteDirection = Main.rand.NextBool(2) ? -1 : 1;
                }
            }
            else if (MovementState == 4) //ENEMY DETECTED
            {
                projectile.tileCollide = true;

                if (Timer == AnimSpeedMultHasty)
                    projectile.frame = 2;
                if (Timer == AnimSpeedMultHasty * 2)
                    projectile.frame = 3;
                if (Timer == AnimSpeedMultHasty * 3)
                    projectile.frame = 4;
                if (Timer > AnimSpeedMultHasty * 3)
                {
                    if (Target != -2 && Main.npc[Target].active)
                        projectile.velocity = Vector2.Normalize(Main.npc[Target].position - projectile.position) * 7;
                    if (Timer >= AnimSpeedMultHasty * 20 || Target == -2 || !Main.npc[Target].active || projectile.velocity.Length() < 0.15f)
                    {
                        projectile.velocity.Y += 0.2f;
                        projectile.velocity.X *= 0.9999f;
                        if (Timer >= AnimSpeedMultHasty * 17)
                        {
                            Target = -80;
                            MovementState = 3;
                            Timer = 0;
                        }
                    }
                    else
                        projectile.rotation += 0.4f;
                }
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.Slimed, 20);
            projectile.velocity = projectile.velocity.RotatedBy(Main.rand.Next(-70, 71) * 0.01f) * -1;

            if (Timer > AnimSpeedMultHasty * 2 && MovementState == 4)
            {
                Target = -2;
                Timer = AnimSpeedMultHasty * 4;
            }
        }
    }
}
