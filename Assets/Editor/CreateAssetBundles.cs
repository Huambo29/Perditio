using System.IO;
using UnityEditor;

public static class CreateAssetBundles
{

    public static string AssetBundleDirectory = "Assets/AssetBundles";

    [MenuItem("Tools/Assets/Build AssetBundles")]
    public static void BuildAllAssetBundles()
    {
        BuildBundles(true);
    }

    [MenuItem("Tools/Assets/Build AssetBundles (Uncompressed)")]
    public static void BuildAllAssetBundlesUncompressed()
    {
        BuildBundles(false);
    }

    private static void BuildBundles(bool compressed)
    {
        if (!Directory.Exists(AssetBundleDirectory))
        {
            Directory.CreateDirectory(AssetBundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(AssetBundleDirectory, compressed ? BuildAssetBundleOptions.None : BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);
    }

}
