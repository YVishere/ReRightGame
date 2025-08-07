import sys
import os
import socket
import threading
import argparse
import win32pipe
import win32file
import pywintypes

# Add the PythonFiles directory to sys.path
python_files_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../../PythonFiles'))
sys.path.append(python_files_dir)

try:
    import llamaModelFile as lmf
except Exception as e:
    print(e)
    print("Error: llamaModelFile module not found in", python_files_dir)
    raise ImportError("llamaModelFile module not found in", python_files_dir)

# Parse command line arguments for IPC authentication
parser = argparse.ArgumentParser(description='LLaMA Server with IPC Authentication')
parser.add_argument('--auth-pipe', type=str, help='Named pipe for authentication')
args = parser.parse_args()

class IPCAuthValidator:
    def __init__(self, pipe_name=None):
        self.pipe_name = pipe_name
        self.auth_secret = None
        self.authenticated_clients = set()
        
        if pipe_name:
            self.retrieve_auth_secret()
    
    def retrieve_auth_secret(self):
        """Retrieve authentication secret from named pipe"""
        try:
            pipe_path = f"\\\\.\\pipe\\{self.pipe_name}"
            print(f"Connecting to authentication pipe: {pipe_path}")
            
            # Connect to the named pipe
            handle = win32file.CreateFile(
                pipe_path,
                win32file.GENERIC_READ | win32file.GENERIC_WRITE,
                0,
                None,
                win32file.OPEN_EXISTING,
                0,
                None
            )
            
            # Send authentication request
            auth_request = b"AUTH_REQUEST"
            win32file.WriteFile(handle, auth_request)
            
            # Read authentication secret
            result, secret_data = win32file.ReadFile(handle, 1024)
            self.auth_secret = secret_data.decode('utf-8')
            
            win32file.CloseHandle(handle)
            print("Successfully retrieved authentication secret via IPC")
            
        except pywintypes.error as e:
            print(f"Failed to connect to authentication pipe: {e}")
            self.auth_secret = None
        except Exception as e:
            print(f"Error during IPC authentication: {e}")
            self.auth_secret = None
    
    def authenticate_client(self, client_address):
        """Authenticate a client connection"""
        if not self.auth_secret:
            print("No authentication secret available, allowing connection")
            return True
        
        # For now, just mark client as authenticated
        # In a real implementation, you'd validate tokens
        self.authenticated_clients.add(client_address)
        return True
    
    def validate_request(self, data, client_address):
        """Validate an authenticated request"""
        if not self.auth_secret:
            return data  # No authentication required
        
        if client_address not in self.authenticated_clients:
            return None  # Client not authenticated
        
        # Parse token-based request
        if data.startswith("TOKEN:"):
            parts = data.split("|", 2)
            if len(parts) == 3:
                token_part = parts[0][6:]  # Remove "TOKEN:" prefix
                session_part = parts[1][8:]  # Remove "SESSION:" prefix
                actual_request = parts[2]
                
                # In a real implementation, validate the token here
                # For now, just return the actual request
                print(f"Validated authenticated request from {client_address}")
                return actual_request
        
        # If not a token request, treat as regular request
        return data

# Initialize authentication validator
auth_validator = IPCAuthValidator(args.auth_pipe)

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
# Allow socket reuse to prevent "Address already in use" errors
server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server_socket.bind(('localhost', 25001))
server_socket.listen(5)  # Allow up to 5 pending connections

ob = lmf.llamaModel()

active_connections = {}
model_lock = threading.Lock()

print("Server is listening for incoming connections with IPC authentication")

def handle_client(connection, client_address):
    
    try:
        authenticated = False
        
        while True:  # Keep processing requests on this connection
            try:
                data = connection.recv(1024).decode()
                if not data:  # Empty data means client disconnected
                    print(f"Client {client_address} disconnected")
                    break
                
                # Handle authentication request first
                if not authenticated and data.strip() == "AUTH_REQUEST":
                    if auth_validator.authenticate_client(client_address):
                        connection.sendall(b"AUTH_SUCCESS")
                        authenticated = True
                        print(f"Client {client_address} authenticated successfully")
                    else:
                        connection.sendall(b"AUTH_FAILED")
                        print(f"Authentication failed for {client_address}")
                        break
                    continue
                
                # If not authenticated and not an auth request, reject
                if not authenticated:
                    print(f"Unauthenticated request from {client_address}: {data}")
                    connection.sendall(b"AUTH_REQUIRED")
                    break
                
                # Validate and process authenticated request
                validated_data = auth_validator.validate_request(data, client_address)
                if validated_data is None:
                    print(f"Invalid request from {client_address}")
                    connection.sendall(b"INVALID_TOKEN")
                    continue
                    
                print("Received authenticated data: ", validated_data)
                
                with model_lock:
                    if validated_data == "GetData":
                        response = ob.invoke("You are the server, say hello and something random")
                    elif validated_data == "Stop":
                        response = "Server stopping..."
                        connection.sendall(response.encode())
                        break  # Exit the loop to close this connection
                    elif validated_data.find("Invoke:::") != -1:
                        # Your existing Invoke processing
                        split = validated_data.split(":::")
                        sendText = split[1]
                        
                        if validated_data.find("Context:::") != -1:
                            context = split[3]
                        else:
                            context = None
                        
                        response = ob.invoke(sendText, context)
                    else:
                        response = "Invalid request"
                
                connection.sendall(response.encode())
                print("Response sent: ", response)
                
            except socket.timeout:
                # Optional: Handle timeouts
                continue
            except Exception as e:
                print(f"Error handling request: {e}")
                break
    finally:
        # Clean up when the connection is done
        print(f"Closing connection from {client_address}")
        connection.close()
        if client_address in active_connections:
            del active_connections[client_address]

try:
    while True:
        connection, client_address = server_socket.accept()
        
        # Check if client already has an active connection
        if client_address in active_connections:
            print(f"Connection already exists for {client_address}, closing old connection")
            active_connections[client_address].close()
            del active_connections[client_address]
        
        # Add new connection
        active_connections[client_address] = connection
        print("Connection from", client_address)
        client_thread = threading.Thread(target=handle_client, args=(connection, client_address))
        client_thread.start()
except KeyboardInterrupt:
    print("Server shutting down...")
    # Close all active connections
    for client_addr, conn in active_connections.items():
        try:
            conn.close()
            print(f"Closed connection to {client_addr}")
        except:
            pass
    active_connections.clear()
    server_socket.close()
    print("Server stopped")