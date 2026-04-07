using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 在运行时为 TMP 注入一个系统字体动态回退，降低中文缺字告警。
/// </summary>
public static class TmpFontFallbackBootstrap
{
    private static readonly string[] CandidateFontNames =
    {
        "Source Han Sans Old Style",
        "Noto Sans CJK SC",
        "Microsoft YaHei",
        "Hiragino Sans GB",
        "Arial Unicode MS",
        "SimHei"
    };

    private const string CoverageProbe = "中文有帮助忙关闭商店伤人";

    private static TMP_FontAsset runtimeFallbackFont;
    private static bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (installed)
        {
            return;
        }

        installed = true;

        if (TMP_Settings.instance == null)
        {
            return;
        }

        if (TryCreateRuntimeFallback(out TMP_FontAsset fallback) == false)
        {
            Debug.LogWarning("[TmpFontFallbackBootstrap] 未能创建运行时 CJK 字体回退，中文可能出现缺字。");
            return;
        }

        runtimeFallbackFont = fallback;

        List<TMP_FontAsset> settingsFallbacks = TMP_Settings.fallbackFontAssets;
        if (settingsFallbacks != null && settingsFallbacks.Contains(runtimeFallbackFont) == false)
        {
            settingsFallbacks.Add(runtimeFallbackFont);
        }

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
        {
            defaultFont.isMultiAtlasTexturesEnabled = true;
            List<TMP_FontAsset> defaultFallbacks = defaultFont.fallbackFontAssetTable;
            if (defaultFallbacks != null && defaultFallbacks.Contains(runtimeFallbackFont) == false)
            {
                defaultFallbacks.Add(runtimeFallbackFont);
            }
        }

        Debug.Log($"[TmpFontFallbackBootstrap] 已安装运行时字体回退: {runtimeFallbackFont.name}");
    }

    private static bool TryCreateRuntimeFallback(out TMP_FontAsset fallback)
    {
        fallback = null;

        for (int i = 0; i < CandidateFontNames.Length; i++)
        {
            if (TryCreateFromFontName(CandidateFontNames[i], out fallback))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryCreateFromFontName(string fontName, out TMP_FontAsset fallback)
    {
        fallback = null;
        Font osFont = Font.CreateDynamicFontFromOSFont(fontName, 90);
        if (osFont == null)
        {
            return false;
        }

        fallback = CreateRuntimeFontAsset(osFont);
        if (fallback == null)
        {
            return false;
        }

        if (!HasCjkCoverage(fallback))
        {
            UnityEngine.Object.Destroy(fallback);
            fallback = null;
            return false;
        }

        fallback.name = $"TMP Runtime CJK Fallback ({fontName})";
        fallback.hideFlags = HideFlags.DontSave;
        fallback.isMultiAtlasTexturesEnabled = true;
        return true;
    }

    private static TMP_FontAsset CreateRuntimeFontAsset(Font sourceFont)
    {
        try
        {
            return TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TmpFontFallbackBootstrap] 创建运行时回退字体失败: {sourceFont.name}, {ex.Message}");
            return null;
        }
    }

    private static bool HasCjkCoverage(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return false;
        }

        fontAsset.isMultiAtlasTexturesEnabled = true;
        if (fontAsset.TryAddCharacters(CoverageProbe, out string missing))
        {
            return true;
        }

        return string.IsNullOrEmpty(missing);
    }
}
