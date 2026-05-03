using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Data
{
    // 加载 Shader
    [StaticConstructorOnStartup]
    public static class ShaderData
    {
        private const string ModPackageId = "mincho.the.candy.wars";
        private const string InnerOutlineShaderPath = "assets/mcw/inneroutline.shader";

        private static readonly Dictionary<string, Shader> CachedShaders = new Dictionary<string, Shader>();

        //颜色变化处发光shader
        public static readonly Shader? InnerOutline;

        static ShaderData()
        {
            InnerOutline = LoadShader(InnerOutlineShaderPath);
        }

        public static Shader? GetShader(string shaderPath)
        {
            if (shaderPath.NullOrEmpty())
            {
                Log.Error("Tried to get a shader with an empty shader path.");
                return null;
            }

            if (CachedShaders.TryGetValue(shaderPath, out var cachedShader))
            {
                return cachedShader;
            }

            return LoadShader(shaderPath);
        }

        private static Shader? LoadShader(string shaderPath)
        {
            var mod = ResolveCurrentModContentPack();
            if (mod == null)
            {
                Log.Error($"Could not resolve mod content pack with packageId '{ModPackageId}' when loading shader '{shaderPath}'.");
                return null;
            }

            foreach (var bundle in mod.assetBundles.loadedAssetBundles)
            {
                if (bundle == null)
                {
                    continue;
                }

                var shader = bundle.LoadAsset<Shader>(shaderPath);
                if (shader == null)
                {
                    continue;
                }

                CachedShaders[shaderPath] = shader;
                return shader;
            }

            Log.Error($"Could not load shader '{shaderPath}' from any auto-loaded asset bundle in mod '{mod.PackageIdPlayerFacing}'.");
            return null;
        }

        private static ModContentPack? ResolveCurrentModContentPack()
        {
            foreach (var runningMod in LoadedModManager.RunningModsListForReading)
            {
                if (string.Equals(runningMod.PackageId, ModPackageId, StringComparison.OrdinalIgnoreCase))
                {
                    return runningMod;
                }
            }

            return null;
        }
    }
}
