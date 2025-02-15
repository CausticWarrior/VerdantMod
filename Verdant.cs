using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Verdant.Systems.Foreground;
using Microsoft.Xna.Framework;
using Terraria.Graphics.Effects;
using Verdant.Backgrounds.BGItem;
using Verdant.Tiles.Verdant.Trees;
using Verdant.Tiles.Verdant.Basic.Plants;
using Verdant.World.Biome;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Shaders;
using Verdant.Effects;
using Verdant.Tiles.Verdant;
using System;
using System.IO;
using Verdant.Systems.ModCompat;

namespace Verdant
{
    public partial class VerdantMod : Mod
	{
        public static VerdantMod Instance;

        public VerdantMod() 
        {
            Instance = this;
        }

        public override void Load()
        {
            SkyManager.Instance["Verdant:Verdant"] = new VerdantSky();

            if (!Main.dedServ)
            {
                Ref<Effect> filterRef = new Ref<Effect>(Assets.Request<Effect>("Effects/Screen/SteamEffect", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);
                Filters.Scene[EffectIDs.BiomeSteam] = new Filter(new ScreenShaderData(filterRef, "Steam"), EffectPriority.VeryHigh);
                Filters.Scene[EffectIDs.BiomeSteam].Load();
            }

            MonoModChanges();
            NewBeginningsCompatibility.AddOrigin();
        }

        public override void PostSetupContent()
        {
            NetEasy.NetEasy.Register(this);

            Flowers.Load(this);
        }

        public override void Unload()
        {
            ForegroundManager.Unload();

            Instance = null;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            NetEasy.NetEasy.HandleModule(reader, whoAmI);
        }

        private void MonoModChanges()
        {
            ForegroundManager.Hooks();
            On.Terraria.Main.DrawBackgroundBlackFill += Main_DrawBackgroundBlackFill; //BackgroundItemManager Draw hook
            On.Terraria.WorldGen.GrowTree += WorldGen_GrowTree; //So that GrowTree works along with other mods
            On.Terraria.Main.Update += Main_Update; //Used for BackgroundItemManager Update
            On.Terraria.Main.DrawWater += Main_DrawWater;

            On.Terraria.Player.QuickMount += VinePulleyPlayer.Player_QuickMount;
            On.Terraria.Player.Teleport += VinePulleyPlayer.Player_Teleport;

            if (ModContent.GetInstance<VerdantClientConfig>().Waterfalls)
                IL.Terraria.WaterfallManager.FindWaterfalls += WaterfallManager_FindWaterfalls;
        }

        public override object Call(params object[] args)
        {
            if (args[0] is not string message)
                throw new ArgumentException("[Verdant] First argument of Call must be a string! Check the GitHub for more info.");

            message = message.ToLower();

            if (message == "inverdant")
                return CallMethods.InVerdant(args);
            else if (message == "nearapotheosis")
                return CallMethods.NearApotheosis(args);
            else if (message == "setverdantarea")
            {
                CallMethods.SetVerdantArea(args);
                return null;
            }

            throw new ArgumentException("[Verdant] Call didn't recieve a valid message! Valid messages are:\nInVerdant NearApotheosis");
        }
    }
}