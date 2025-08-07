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
    
    private void Awake()
    {
        Instance = this;
        GenerateDynamicSecret();
        GenerateSessionKey();
        SetupAuthenticationPipe();
    }
    
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
        
        // Generate unique pipe name based on process ID and timestamp
        pipeName = "ReRightAuth_" + System.Diagnostics.Process.GetCurrentProcess().Id + "_" + DateTime.Now.Ticks;
        
        Debug.Log("Generated dynamic secret and pipe name for IPC authentication");
    }
    
    private async void SetupAuthenticationPipe()
    {
        try
        {
            cancellationTokenSource = new CancellationTokenSource();
            authPipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
            
            Debug.Log($"Authentication pipe server created: {pipeName}");
            
            // Start listening for authentication requests in background
            _ = Task.Run(async () => await HandleAuthenticationRequests(), cancellationTokenSource.Token);
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
                Debug.Log("Waiting for authentication connection on pipe...");
                await authPipeServer.WaitForConnectionAsync(cancellationTokenSource.Token);
                
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
            Debug.Log("Authentication pipe handling cancelled");
        }
        catch (Exception e)
        {
            Debug.LogError("Error in authentication pipe handling: " + e.Message);
        }
    }
    
    public string GetPipeName()
    {
        return pipeName;
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
    
    private void CleanupIPC()
    {
        try
        {
            // Cancel background authentication task
            cancellationTokenSource?.Cancel();
            
            // Close pipe server
            if (authPipeServer != null)
            {
                if (authPipeServer.IsConnected)
                {
                    authPipeServer.Disconnect();
                }
                authPipeServer.Dispose();
                Debug.Log("Authentication pipe server disposed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error during IPC cleanup: " + e.Message);
        }
    }
}
