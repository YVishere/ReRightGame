using UnityEngine;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections;
using System.Runtime.CompilerServices;
using System.IO;
using UnityEditor.PackageManager.Requests;

public class ServerSocketC : MonoBehaviour
{
    private Process pythonServerProcess;

    public static ServerSocketC Instance { get; private set; }

    private bool stopRetrying = false;
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start(){
        StartCoroutine(startSteps());       
    }

    private IEnumerator startSteps(int retries = 3)
    {
        startPythonServer();
        yield return new WaitForSeconds(5);
        yield return RequestDataFromServer("GetData");
        /////////////////////////////////////////////////////////////////////
        ///Debug statement to see how many connections can be made to server
        /////////////////////////////////////////////////////////////////////

        // for (int i = 0; i <= 10; i++){
        //     yield return new WaitForSeconds(5);
        //     Task requestTask = RequestDataFromServer("GetData");
        //     while (!requestTask.IsCompleted){
        //         yield return null;
        //     }
        //     if (requestTask.IsFaulted){
        //         UnityEngine.Debug.LogError("Error starting server: " + requestTask.Exception.Message);
        //     }
        // }    
    }
    
    // // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start(){
    //     RequestDataFromServer("GetData");
    // }

    void OnApplicationQuit(){
        stopRetrying = true;
        stopPythonServer();
    }

    void startPythonServer(){
        try{
            pythonServerProcess = new Process();
            pythonServerProcess.StartInfo.FileName = "python";
            
            // Pass the pipe name as argument for IPC authentication
            string pipeName = AuthManager.Instance.GetPipeName();
            pythonServerProcess.StartInfo.Arguments = $"ServerSocketPython.py --auth-pipe \"{pipeName}\"";

            //Somehow unity messes up same directory files so this line is important
            pythonServerProcess.StartInfo.WorkingDirectory = System.IO.Path.Combine(Application.dataPath, "Scripts/ServerFiles");

            pythonServerProcess.StartInfo.CreateNoWindow = true;
            pythonServerProcess.StartInfo.UseShellExecute = false;
            // pythonServerProcess.StartInfo.RedirectStandardOutput = true; // Redirect standard output

            pythonServerProcess.Start();

            // // Read the standard error output asynchronously
            // pythonServerProcess.BeginErrorReadLine();
            // pythonServerProcess.ErrorDataReceived += (sender, args) =>
            // {
            //     if (!string.IsNullOrEmpty(args.Data))
            //     {
            //         UnityEngine.Debug.LogError("Python server error: " + args.Data);
            //     }
            // };

            UnityEngine.Debug.Log("Python server started with IPC authentication");
        }
        catch (Exception e){
            UnityEngine.Debug.LogError("Error starting python server: " + e.Message);
        }
    }

    void stopPythonServer(){
        try{
            if (pythonServerProcess != null && !pythonServerProcess.HasExited){
                pythonServerProcess.Kill();
                UnityEngine.Debug.Log("Python server stopped");
            }
        }
        catch(Exception e){
            UnityEngine.Debug.LogError("Error stopping python server: " + e.Message);
        }
    }

    public async Task<TcpClient> connectToServer(int retries, bool doAgain = true){
        try{
            TcpClient client = new TcpClient();
            await client.ConnectAsync("localhost", 25001);
        
            // Configure keep-alive after successful connection
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            
            // Windows-specific keep-alive settings using IOControl
            byte[] inOptionValues = new byte[12];
            BitConverter.GetBytes(1).CopyTo(inOptionValues, 0);                // Enable keep-alive (1=on, 0=off)
            BitConverter.GetBytes(30000).CopyTo(inOptionValues, 4);            // Keep-alive time (30 seconds)
            BitConverter.GetBytes(5000).CopyTo(inOptionValues, 8);             // Keep-alive interval (5 seconds)
            
            client.Client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

            // Perform authentication handshake
            bool authSuccess = await AuthenticateConnection(client);
            if (!authSuccess)
            {
                UnityEngine.Debug.LogError("Authentication failed");
                client.Close();
                return null;
            }

            UnityEngine.Debug.Log("Connected to server with keep-alive enabled and authenticated");
            return client;
        }
        catch (Exception e){
            if (stopRetrying)
            {
                return null;
            }
            UnityEngine.Debug.LogError("Error connecting to server: " + e.Message);
            //Ideally you would need only a second retry but just in case lets go with 3
            if (retries > 0 && doAgain){
                UnityEngine.Debug.Log("Retrying connection..." + retries);
                OnApplicationQuit();
                StartCoroutine(startSteps(retries - 1));
            }
        }
        return null;
    }
    
    private async Task<bool> AuthenticateConnection(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            
            // Send authentication request
            string authRequest = "AUTH_REQUEST";
            byte[] authData = Encoding.ASCII.GetBytes(authRequest);
            await stream.WriteAsync(authData, 0, authData.Length);
            await stream.FlushAsync();
            
            UnityEngine.Debug.Log("Sent authentication request to server");
            
            // Add a small delay to ensure server processes the auth request
            await Task.Delay(100);
            
            // Receive authentication response
            byte[] responseBuffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            string authResponse = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead).Trim();
            
            UnityEngine.Debug.Log($"Received auth response: '{authResponse}' (length: {authResponse.Length})");
            
            if (authResponse.StartsWith("AUTH_SUCCESS"))
            {
                UnityEngine.Debug.Log("Server authentication successful");
                return true;
            }
            else
            {
                UnityEngine.Debug.LogError("Server authentication failed: " + authResponse);
                return false;
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Authentication error: " + e.Message);
            return false;
        }
    }

    private async Task RequestDataFromServer(string request){
        TcpClient client = await connectToServer(3);

        NetworkStream stream = client.GetStream();
        if (stream == null) return;

        byte[] data = Encoding.ASCII.GetBytes(request);
        stream.Write(data, 0, data.Length);
        UnityEngine.Debug.Log("Request sent to server");

        await ReceiveResponseFromServer(stream, client);

        stream.Close();

        closeConnection(client);
    }

    public async Task<string> NPCRequest(string request, TcpClient client, NetworkStream stream){
        if (stream == null) return "";

        // Generate authentication token for this request
        string sessionKey = AuthManager.Instance.GetSessionKey();
        string requestToken = AuthManager.Instance.GenerateRequestToken(request);
        string authenticatedRequest = $"TOKEN:{requestToken}|SESSION:{sessionKey}|{request}";

        byte[] data = Encoding.ASCII.GetBytes(authenticatedRequest);

        //Todo: send data in chunks, I am just sending the first 1024 bytes for simplicity
        //TCP websocket forcibly closes connection if datalength is greater than whatever i specified in python code
        UnityEngine.Debug.Log("Writing authenticated data to stream");
        await stream.WriteAsync(data, 0, Math.Min(data.Length, 1023));
        UnityEngine.Debug.Log("Authenticated request sent to server");

        String resp = await ReceiveResponseFromServer(stream, client);
        
        // Validate response
        if (!AuthManager.Instance.ValidateResponse(resp))
        {
            UnityEngine.Debug.LogError("Invalid response received from server");
            return "";
        }
        
        return resp;
    }

    public async Task<string> ReceiveResponseFromServer(NetworkStream stream, TcpClient client){
        if (stream == null) return "";

        byte[] data = new byte[1024];
        UnityEngine.Debug.Log("Waiting for response from server...");
        int bytes = await stream.ReadAsync(data, 0, data.Length);

        string response = Encoding.ASCII.GetString(data, 0, bytes);
        UnityEngine.Debug.Log("Response from server: " + response);

        return response;
    }

    void closeConnection(TcpClient client){
        if (client!= null){
            client.Close();
        }

        UnityEngine.Debug.Log("Connection closed");
    }
}

