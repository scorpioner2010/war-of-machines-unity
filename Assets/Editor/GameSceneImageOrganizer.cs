using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class GameSceneImageOrganizer
    {
        private const string SceneRoot = "Assets/Game/Scenes";
        private const string TargetRoot = "Assets/Game/Images";

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".tga",
            ".psd",
            ".tif",
            ".tiff",
            ".bmp",
            ".exr",
            ".hdr"
        };

        private static readonly Dictionary<string, string> ExactDestinations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Assets/Art/Model/House/texture_pbr_20250901_upscayl_4x_high-fidelity-4x.png", "Assets/Game/Images/Environment/Models/Environment_House_Albedo.png" },
            { "Assets/FishNet/Demos/ColliderRollback/Textures/Crosshair.png", "Assets/Game/Images/UI/HUD/Hud_Crosshair_FishNet.png" },
            { "Assets/FishNet/Demos/HashGrid/Textures/1x1 Pixel.png", "Assets/Game/Images/Utility/Texture_WhitePixel.png" },
            { "Assets/Game/adamant.png", "Assets/Game/Images/UI/Icons/Icon_Adamant.png" },
            { "Assets/Game/aim.png", "Assets/Game/Images/UI/HUD/Hud_Aim_Default.png" },
            { "Assets/Game/aim-B.png", "Assets/Game/Images/UI/HUD/Hud_Aim_Bracket.png" },
            { "Assets/Game/background.png", "Assets/Game/Images/UI/Backgrounds/Background_Menu.png" },
            { "Assets/Game/bolts.png", "Assets/Game/Images/UI/Icons/Icon_Bolts.png" },
            { "Assets/Game/button 1.png", "Assets/Game/Images/UI/Buttons/Button_Secondary.png" },
            { "Assets/Game/button.png", "Assets/Game/Images/UI/Buttons/Button_Default.png" },
            { "Assets/Game/exp.png", "Assets/Game/Images/UI/Icons/Icon_Experience.png" },
            { "Assets/Game/freeXP.png", "Assets/Game/Images/UI/Icons/Icon_FreeXP.png" },
            { "Assets/Game/GameResources/LightSettings/dreamlike_sky_sunny_cloud 2.jpg", "Assets/Game/Images/Environment/Sky/Sky_Dreamlike_Sunny_Clouds.jpg" },
            { "Assets/Game/GameResources/Models/t1-hunter/texture/t1_thicknessmap.png", "Assets/Game/Images/Vehicles/T1Hunter/Vehicle_T1Hunter_Thickness.png" },
            { "Assets/Game/GameResources/Models/t2/texture/Track.psd", "Assets/Game/Images/Vehicles/T2/Vehicle_T2_Track.psd" },
            { "Assets/Game/GameResources/Models/t2/texture/Wheel.psd", "Assets/Game/Images/Vehicles/T2/Vehicle_T2_Wheel.psd" },
            { "Assets/Game/GameResources/Sprites/Backgrounds/33.png", "Assets/Game/Images/UI/Backgrounds/Background_Battlefield_03.png" },
            { "Assets/Game/GameResources/Sprites/Backgrounds/bg3.png", "Assets/Game/Images/UI/Backgrounds/Background_Battlefield_Alt03.png" },
            { "Assets/Game/GameResources/Sprites/Backgrounds/bg5.psd", "Assets/Game/Images/UI/Backgrounds/Background_Battlefield_Source05.psd" },
            { "Assets/Game/GameResources/Sprites/box.psd", "Assets/Game/Images/UI/Frames/Frame_Box.psd" },
            { "Assets/Game/GameResources/Sprites/box2.psd", "Assets/Game/Images/UI/Frames/Frame_Box_Alt.psd" },
            { "Assets/Game/GameResources/Sprites/cross.psd", "Assets/Game/Images/UI/HUD/Hud_Crosshair_Cross.psd" },
            { "Assets/Game/GameResources/Sprites/damage.png", "Assets/Game/Images/UI/HUD/Hud_Damage_Indicator.png" },
            { "Assets/Game/GameResources/Sprites/LoadingImage.png", "Assets/Game/Images/UI/Backgrounds/Background_Loading.png" },
            { "Assets/Game/GameResources/Sprites/LoginBack.png", "Assets/Game/Images/UI/Backgrounds/Background_Login.png" },
            { "Assets/Game/GameResources/Sprites/LoginBack4.png", "Assets/Game/Images/UI/Backgrounds/Background_Login_Alt.png" },
            { "Assets/Game/GameResources/SpritesUI/Button_Circle_01_Yellow.Png", "Assets/Game/Images/UI/Buttons/Button_Circle_Yellow.png" },
            { "Assets/Game/GameResources/SpritesUI/Button_Rectangle_01_Convex_Dark.Png", "Assets/Game/Images/UI/Buttons/Button_Rectangle_Dark.png" },
            { "Assets/Game/mmr.png", "Assets/Game/Images/UI/Icons/Icon_MMR.png" },
            { "Assets/Game/settings.png", "Assets/Game/Images/UI/Icons/Icon_Settings.png" },
            { "Assets/Game/t1.png", "Assets/Game/Images/Vehicles/T1Hunter/Vehicle_T1Hunter_Albedo.png" },
            { "Assets/Game/t1-N.png", "Assets/Game/Images/Vehicles/T1Hunter/Vehicle_T1Hunter_Normal.png" },
            { "Assets/Game/t2.png", "Assets/Game/Images/Vehicles/T2/Vehicle_T2_Albedo.png" },
            { "Assets/Game/t2-N.png", "Assets/Game/Images/Vehicles/T2/Vehicle_T2_Normal.png" },
            { "Assets/JMO Assets/Cartoon FX (legacy)/Textures/CFX_T_Anim4_Triangle1.png", "Assets/Game/Images/Effects/Particles/Effect_Triangle_Frame.png" },
            { "Assets/JMO Assets/Cartoon FX (legacy)/Textures/CFX2_T_Glow.png", "Assets/Game/Images/Effects/Particles/Effect_Glow.png" },
            { "Assets/JMO Assets/Cartoon FX (legacy)/Textures/CFX3_T_FireBulk.png", "Assets/Game/Images/Effects/Particles/Effect_Fire_Bulk.png" },
            { "Assets/JMO Assets/Cartoon FX (legacy)/Textures/CFX3_T_FireSparkle.png", "Assets/Game/Images/Effects/Particles/Effect_Fire_Sparkle.png" },
            { "Assets/JMO Assets/Cartoon FX (legacy)/Textures/CFX3_T_GlowDot.png", "Assets/Game/Images/Effects/Particles/Effect_Glow_Dot.png" },
            { "Assets/JMO Assets/Cartoon FX (legacy)/Textures/CFX3_T_SmokePuff.png", "Assets/Game/Images/Effects/Particles/Effect_Smoke_Puff.png" },
            { "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Fonts/Alata-Regular-Outline 50 SDF Atlas.png", "Assets/Game/Images/UI/Fonts/Font_Alata_Outline_SDF_Atlas.png" },
            { "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Circle_02_Gray.png", "Assets/Game/Images/UI/Buttons/Button_Circle_Gray.png" },
            { "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/SlotFrame_Circle_White_FocusBg.png", "Assets/Game/Images/UI/Frames/Frame_Slot_Circle_Focus.png" },
            { "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Icon_PictoIcons/Original/function_icon_arrow_bottom.png", "Assets/Game/Images/UI/Icons/Icon_Arrow_Down.png" },
            { "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/IconMisc/Icon_Check_01_m.png", "Assets/Game/Images/UI/Icons/Icon_Checkmark.png" },
            { "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/UI_Etc/Toggle_Check_White_InnerBorder.png", "Assets/Game/Images/UI/Controls/Control_Toggle_Check.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Static/CliffRockTTFEL_A_Albedo.png", "Assets/Game/Images/Environment/Terrain/Terrain_CliffRock_Albedo.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Static/CliffRockTTFEL_A_MAG.png", "Assets/Game/Images/Environment/Terrain/Terrain_CliffRock_Mask.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Static/CliffRockTTFEL_A_Normal.png", "Assets/Game/Images/Environment/Terrain/Terrain_CliffRock_Normal.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Static/RocksTTFEL_Albedo.png", "Assets/Game/Images/Environment/Terrain/Terrain_Rocks_Albedo.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Static/RocksTTFEL_MAG.png", "Assets/Game/Images/Environment/Terrain/Terrain_Rocks_Mask.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Static/RocksTTFEL_Normal.png", "Assets/Game/Images/Environment/Terrain/Terrain_Rocks_Normal.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Tiles Nature/GrassGCATA_MAG.png", "Assets/Game/Images/Environment/Terrain/Terrain_Grass_Mask.png" },
            { "Assets/Toby Fredson/The Toby Foliage Engine/(TTFE)_Demo/Textures/Textures_Tiles Nature/GrassGCATA_Normal.png", "Assets/Game/Images/Environment/Terrain/Terrain_Grass_Normal.png" }
        };

        [MenuItem("Tools/Game/Organize Scene Images")]
        public static void OrganizeGameSceneImages()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SceneRoot });
            if (sceneGuids.Length == 0)
            {
                Debug.LogWarning("No scenes found under " + SceneRoot + ".");
                return;
            }

            List<string> scenes = new List<string>(sceneGuids.Length);
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                if (string.IsNullOrWhiteSpace(scenePath) == false)
                {
                    scenes.Add(scenePath);
                }
            }

            string[] dependencies = AssetDatabase.GetDependencies(scenes.ToArray(), true);
            Array.Sort(dependencies, StringComparer.OrdinalIgnoreCase);

            EnsureFolder(TargetRoot);

            int moved = 0;
            int skipped = 0;
            int failed = 0;

            for (int i = 0; i < dependencies.Length; i++)
            {
                string dependency = NormalizePath(dependencies[i]);
                if (IsImageAsset(dependency) == false)
                {
                    continue;
                }

                if (ShouldSkipDependency(dependency))
                {
                    skipped++;
                    continue;
                }

                if (IsUnderTargetRoot(dependency))
                {
                    skipped++;
                    continue;
                }

                string destination = BuildDestinationPath(dependency);
                destination = MakeUniqueDestination(destination);

                string destinationFolder = NormalizePath(Path.GetDirectoryName(destination));
                EnsureFolder(destinationFolder);

                string error = AssetDatabase.MoveAsset(dependency, destination);
                if (string.IsNullOrEmpty(error))
                {
                    moved++;
                    Debug.Log("Moved scene image: " + dependency + " -> " + destination);
                }
                else
                {
                    failed++;
                    Debug.LogError("Failed to move scene image: " + dependency + " -> " + destination + ". " + error);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Game scene image organization complete. Moved: " + moved + ", skipped: " + skipped + ", failed: " + failed + ".");
        }

        private static bool IsImageAsset(string assetPath)
        {
            string extension = Path.GetExtension(assetPath);
            return ImageExtensions.Contains(extension);
        }

        private static bool IsUnderTargetRoot(string assetPath)
        {
            return assetPath.StartsWith(TargetRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldSkipDependency(string assetPath)
        {
            if (assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) == false)
            {
                return true;
            }

            if (assetPath.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (assetPath.IndexOf("/Gizmos/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (assetPath.IndexOf("Editor Resources", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private static string BuildDestinationPath(string assetPath)
        {
            if (ExactDestinations.TryGetValue(assetPath, out string exactDestination))
            {
                return exactDestination;
            }

            string category = GetCategory(assetPath);
            string extension = Path.GetExtension(assetPath).ToLowerInvariant();
            string fileName = "Image_" + SanitizeName(Path.GetFileNameWithoutExtension(assetPath)) + extension;
            return TargetRoot + "/" + category + "/" + fileName;
        }

        private static string GetCategory(string assetPath)
        {
            string lower = assetPath.ToLowerInvariant();
            if (lower.Contains("/cartoon fx") || lower.Contains("/effects/") || lower.Contains("cfx"))
            {
                return "Effects/Particles";
            }

            if (lower.Contains("/model/") || lower.Contains("/models/") || lower.Contains("t1") || lower.Contains("t2") || lower.Contains("vehicle"))
            {
                return "Vehicles/Misc";
            }

            if (lower.Contains("sky") || lower.Contains("grass") || lower.Contains("rock") || lower.Contains("terrain") || lower.Contains("house") || lower.Contains("foliage"))
            {
                return "Environment/Misc";
            }

            if (lower.Contains("button"))
            {
                return "UI/Buttons";
            }

            if (lower.Contains("icon") || lower.Contains("adamant") || lower.Contains("bolt") || lower.Contains("exp") || lower.Contains("mmr") || lower.Contains("setting"))
            {
                return "UI/Icons";
            }

            if (lower.Contains("background") || lower.Contains("login") || lower.Contains("loading"))
            {
                return "UI/Backgrounds";
            }

            if (lower.Contains("crosshair") || lower.Contains("aim") || lower.Contains("damage"))
            {
                return "UI/HUD";
            }

            if (lower.Contains("font") || lower.Contains("atlas"))
            {
                return "UI/Fonts";
            }

            return "Misc";
        }

        private static string MakeUniqueDestination(string destination)
        {
            destination = NormalizePath(destination);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination) == null)
            {
                return destination;
            }

            string folder = NormalizePath(Path.GetDirectoryName(destination));
            string name = Path.GetFileNameWithoutExtension(destination);
            string extension = Path.GetExtension(destination);

            for (int i = 2; i < 1000; i++)
            {
                string candidate = folder + "/" + name + "_" + i.ToString("00") + extension;
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidate) == null)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Unable to find a unique destination path for " + destination + ".");
        }

        private static void EnsureFolder(string folderPath)
        {
            folderPath = NormalizePath(folderPath);
            if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (AssetDatabase.IsValidFolder(next) == false)
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/').TrimEnd('/');
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unnamed";
            }

            char[] chars = value.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsLetterOrDigit(c) == false)
                {
                    chars[i] = '_';
                }
            }

            string sanitized = new string(chars);
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            return sanitized.Trim('_');
        }
    }
}
