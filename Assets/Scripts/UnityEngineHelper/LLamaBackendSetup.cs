using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
// Runs automatically on every domain reload to fix LLamaSharp multi-backend plugin conflicts.
//
// Problem: NuGetForUnity installs LLamaSharp.Backend.Cpu as a transitive dependency of
// LLamaSharp.Backend.Cuda12. The Cpu package ships identical DLL names (ggml.dll, llama.dll,
// etc.) across four AVX subdirectories (avx512/avx2/avx/noavx). Unity's plugin system marks
// all of them as Editor-compatible, raising "Multiple plugins with the same name" errors.
//
// Solution: Disable all Cpu backend DLLs as Unity plugins. LLamaSharp's DefaultNativeLibrary-
// SelectingPolicy handles AVX-level selection internally at runtime — Unity's plugin loader
// must not pre-load them.
//
// Additionally copies ggml-cpu.dll into the active backend's native folder so the OS can
// resolve it as a DLL dependency when llama.dll is loaded (required even for the CUDA path).
//
// This fix persists across NuGet restores as long as the generated .meta files are committed
// to version control. Run "Tools/Fix LLamaSharp Backend Plugins" manually after any restore
// that regenerates the .meta files.

[InitializeOnLoad]
public static class LLamaBackendSetup
{
    private const string GgmlCpuDllName = "ggml-cpu.dll";

    static LLamaBackendSetup()
    {
        // Defer so the AssetDatabase is fully ready before we touch importers
        EditorApplication.delayCall += FixBackendPlugins;

        Debug.Log("LlamaBackendSetup.cs: Registered for domain reload events");
    }

    [MenuItem("Tools/Fix LLamaSharp Backend Plugins")]
    public static void FixBackendPlugins()
    {
        string packagesPath = Path.Combine(Application.dataPath, "Packages");
        if (!Directory.Exists(packagesPath))
            return;

        bool changed = false;
        string dataRoot = Application.dataPath.Replace("\\", "/");

        // Convert an absolute file path to an "Assets/..." project-relative path
        string ToAssetPath(string abs) =>
            "Assets" + abs.Replace("\\", "/").Substring(dataRoot.Length);

        // -------------------------------------------------------------------------
        // Step 1: Disable all Cpu backend DLLs in Unity's plugin system
        // -------------------------------------------------------------------------
        string[] cpuDirs = Directory.GetDirectories(
            packagesPath, "LLamaSharp.Backend.Cpu*", SearchOption.TopDirectoryOnly);

        foreach (string cpuDir in cpuDirs)
        {
            foreach (string dll in Directory.GetFiles(cpuDir, "*.dll", SearchOption.AllDirectories))
            {
                string assetPath = ToAssetPath(dll);
                if (AssetImporter.GetAtPath(assetPath) is not PluginImporter importer)
                    continue;
                if (!importer.GetCompatibleWithEditor() && !importer.GetCompatibleWithAnyPlatform())
                    continue;

                importer.SetCompatibleWithEditor(false);
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SaveAndReimport();
                Debug.Log($"LLamaBackendSetup: Disabled plugin {assetPath}");
                changed = true;
            }
        }

        // -------------------------------------------------------------------------
        // Step 2: Ensure ggml-cpu.dll is present alongside the active backend's
        //         llama.dll so the OS can resolve it as a load-time dependency
        // -------------------------------------------------------------------------
        // Search all Cuda12 packages for llama.dll. No platform filter needed —
        // on Windows only the .Windows sub-package contains .dll files; the Linux
        // sub-package contains .so files which won't match the llama.dll search.
        string[] cuda12Dirs = Directory.GetDirectories(
            packagesPath, "LLamaSharp.Backend.Cuda12*", SearchOption.TopDirectoryOnly);

        foreach (string cuda12Dir in cuda12Dirs)
        {
            foreach (string llamaDll in Directory.GetFiles(cuda12Dir, "llama.dll", SearchOption.AllDirectories))
            {
                string nativeDir  = Path.GetDirectoryName(llamaDll)!;
                string destPath   = Path.Combine(nativeDir, GgmlCpuDllName);
                if (File.Exists(destPath))
                    continue;

                // Prefer the highest available AVX variant from the Cpu backend
                string? src = cpuDirs
                    .SelectMany(d => Directory.GetFiles(d, GgmlCpuDllName, SearchOption.AllDirectories))
                    .OrderByDescending(p =>
                        p.Contains("avx512") ? 3 :
                        p.Contains("avx2")   ? 2 :
                        p.Contains("avx")    ? 1 : 0)
                    .FirstOrDefault();

                if (src == null)
                {
                    Debug.LogWarning(
                        $"LLamaBackendSetup: {GgmlCpuDllName} not found in any Backend.Cpu folder. " +
                        "LLamaSharp may fail to initialise — copy it manually into the active backend's native folder.");
                    continue;
                }

                File.Copy(src, destPath);
                AssetDatabase.ImportAsset(ToAssetPath(destPath));
                Debug.Log($"LLamaBackendSetup: Copied {GgmlCpuDllName} \u2192 {ToAssetPath(destPath)}");
                changed = true;
            }
        }

        if (changed)
            Debug.Log("LLamaBackendSetup: Backend plugin configuration updated.");
        else
            Debug.Log("LLamaBackendSetup: Backend plugins already configured correctly, no changes needed.");
    }
}
#endif
