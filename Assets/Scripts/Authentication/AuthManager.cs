using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System; // Added for Environment, Exception, and DateTime
using System.Threading.Tasks;
using System.Threading;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }
    
    // Generate unique API key per game session
    private string gameSessionKey;
    private string dynamicSecret;
    private string pipeName;
    private NamedPipeServerStream authPipeServer;
    private CancellationTokenSource cancellationTokenSource;
    private Task authTask;
    
    private void Awake()
    {
        Instance = this;
        GenerateDynamicSecret();
        GenerateSessionKey();
        SetupAuthenticationPipe();
        
        // CRITICAL: Register for domain reload cleanup
        #if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeDomainReload;
        #endif
    }
    
    #if UNITY_EDITOR
    private void OnBeforeDomainReload()
    {
        Debug.Log("AuthManager: Domain reload detected - cleaning up immediately");
        CleanupIPC();
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeDomainReload;
    }
    #endif
    
    private void GenerateDynamicSecret()
    {
        // Generate a unique secret per game session that's not stored anywhere on disk
        string processInfo = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
        string machineInfo = Environment.MachineName;
        string userInfo = Environment.UserName;
        string timestamp = DateTime.Now.Ticks.ToString();
        string unityInstanceId = System.Guid.NewGuid().ToString();
        
        // Combine multiple system identifiers to create a unique secret
        string rawSecret = processInfo + "_" + machineInfo + "_" + userInfo + "_" + timestamp + "_" + unityInstanceId;
        dynamicSecret = GenerateHash(rawSecret);
        
        // Generate unique pipe name with additional entropy to prevent collisions
        string processId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
        string randomSuffix = System.Guid.NewGuid().ToString("N")[..8]; // Short random suffix
        pipeName = $"ReRightAuth_{processId}_{DateTime.Now.Ticks}_{randomSuffix}";
        
        Debug.Log("Generated dynamic secret and pipe name for IPC authentication");
    }
    
    private void SetupAuthenticationPipe()
    {
        try
        {
            cancellationTokenSource = new CancellationTokenSource();
            
            // Create pipe with timeout to prevent hanging
            authPipeServer = new NamedPipeServerStream(
                pipeName, 
                PipeDirection.InOut, 
                1, 
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous // Use async pipes
            );
            
            Debug.Log($"Authentication pipe server created: {pipeName}");
            
            // Start listening for authentication requests in background with proper cleanup
            authTask = Task.Run(async () => await HandleAuthenticationRequests(), cancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to setup authentication pipe: " + e.Message);
        }
    }
    
    private async Task HandleAuthenticationRequests()
    {
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Debug.Log("Waiting for authentication connection on pipe...");
                
                // Use timeout to prevent indefinite hanging during domain reload
                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token))
                {
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout
                    
                    try
                    {
                        await authPipeServer.WaitForConnectionAsync(timeoutCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Timeout occurred, continue loop to check main cancellation token
                        continue;
                    }
                }
                
                // Read authentication request
                byte[] buffer = new byte[1024];
                int bytesRead = await authPipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Debug.Log("Received authentication request via IPC");
                
                // Send dynamic secret as response
                byte[] secretBytes = Encoding.UTF8.GetBytes(dynamicSecret);
                await authPipeServer.WriteAsync(secretBytes, 0, secretBytes.Length, cancellationTokenSource.Token);
                await authPipeServer.FlushAsync(cancellationTokenSource.Token);
                
                Debug.Log("Sent authentication secret via IPC");
                
                // Disconnect and wait for next connection
                authPipeServer.Disconnect();
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Authentication pipe handling cancelled - this is normal during shutdown");
        }
        catch (Exception e)
        {
            Debug.LogError("Error in authentication pipe handling: " + e.Message);
        }
        finally
        {
            Debug.Log("Authentication pipe task completed");
        }
    }
    
    public string GetPipeName()
    {
        return pipeName;
    }
    
    public string GetDynamicSecret()
    {
        // Backup method to get secret directly if pipes fail
        return dynamicSecret;
    }
    
    private void GenerateSessionKey()
    {
        // Generate unique key combining dynamic secret with additional entropy
        string rawKey = System.Guid.NewGuid().ToString() + "_" + DateTime.Now.Ticks + "_" + dynamicSecret;
        gameSessionKey = GenerateHash(rawKey);
        Debug.Log("Generated session key for authentication");
    }
    
    public string GetSessionKey()
    {
        return gameSessionKey;
    }
    
    public string GenerateRequestToken(string request)
    {
        // Create HMAC for request validation using dynamic secret
        string message = gameSessionKey + request + DateTime.Now.ToString("yyyyMMddHH");
        return GenerateHash(message + dynamicSecret);
    }
    
    public bool ValidateToken(string token, string originalRequest)
    {
        string expectedToken = GenerateRequestToken(originalRequest);
        return token == expectedToken;
    }
    
    private string GenerateHash(string input)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            
            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
    
    public bool ValidateResponse(string response)
    {
        // Add response validation if needed
        return !string.IsNullOrEmpty(response) && response != "AUTH_FAILED" && response != "INVALID_TOKEN";
    }
    
    private void OnApplicationQuit()
    {
        // Clean up IPC resources
        CleanupIPC();
    }
    
    private void OnDestroy()
    {
        // Clean up IPC resources
        CleanupIPC();
    }
    
    public static void ForceCleanupAllInstances()
    {
        // Emergency cleanup method that can be called statically
        Debug.Log("AuthManager: Force cleanup all instances");
        
        if (Instance != null)
        {
            Instance.CleanupIPC();
        }
        
        // Additional cleanup for any orphaned pipes
        try
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during force cleanup: {e.Message}");
        }
    }
    
    private void CleanupIPC()
    {
        Debug.Log("AuthManager: Starting IPC cleanup...");
        
        try
        {
            // Cancel background authentication task immediately
            if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                Debug.Log("AuthManager: Cancellation token triggered");
            }
            
            // Wait for auth task to complete with timeout
            if (authTask != null && !authTask.IsCompleted)
            {
                Debug.Log("AuthManager: Waiting for auth task completion...");
                if (!authTask.Wait(1000)) // 1 second timeout
                {
                    Debug.LogWarning("AuthManager: Auth task did not complete within timeout");
                }
            }
            
            // Close pipe server
            if (authPipeServer != null)
            {
                try
                {
                    if (authPipeServer.IsConnected)
                    {
                        authPipeServer.Disconnect();
                        Debug.Log("AuthManager: Pipe disconnected");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"AuthManager: Error disconnecting pipe: {e.Message}");
                }
                
                authPipeServer.Dispose();
                authPipeServer = null;
                Debug.Log("AuthManager: Pipe server disposed");
            }
            
            // Dispose cancellation token
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
                Debug.Log("AuthManager: Cancellation token disposed");
            }
            
            Debug.Log("AuthManager: IPC cleanup completed successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("AuthManager: Error during IPC cleanup: " + e.Message);
        }
    }
}
