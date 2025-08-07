using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class DomainReloadHelper
{
    static DomainReloadHelper()
    {
        // Register for domain reload events
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeDomainReload;
        
        Debug.Log("DomainReloadHelper: Registered for domain reload events");
    }
    
    private static void OnBeforeDomainReload()
    {
        Debug.Log("DomainReloadHelper: Domain reload starting - forcing cleanup");
        
        // Force cleanup of AuthManager
        AuthManager.ForceCleanupAllInstances();
        
        // Force cleanup of other singletons if needed
        if (Hasher.Instance != null)
        {
            Hasher.Instance.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);
        }
        
        if (ServerSocketC.Instance != null)
        {
            ServerSocketC.Instance.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);
        }
        
        Debug.Log("DomainReloadHelper: Cleanup completed");
    }
    
    [MenuItem("Tools/Force Cleanup Before Domain Reload")]
    public static void ForceCleanup()
    {
        OnBeforeDomainReload();
    }
}
#endif
