import sys
import os
import socket
import threading

# Add the PythonFiles directory to sys.path
python_files_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../../PythonFiles'))
sys.path.append(python_files_dir)

try:
    import llamaModelFile as lmf
except Exception as e:
    print(e.msg)
    print("Error: llamaModelFile module not found in", python_files_dir)
    raise ImportError("llamaModelFile module not found in", python_files_dir)

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
# Allow socket reuse to prevent "Address already in use" errors
server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server_socket.bind(('localhost', 25001))
server_socket.listen(5)  # Allow up to 5 pending connections

ob = lmf.llamaModel()

active_connections = {}
model_lock = threading.Lock()

print("Server is listening for incoming connections")

def handle_client(connection, client_address):
    
    try:
        while True:  # Keep processing requests on this connection
            try:
                data = connection.recv(1024).decode()
                if not data:  # Empty data means client disconnected
                    print(f"Client {client_address} disconnected")
                    break
                    
                print("Received data: ", data)
                
                with model_lock:
                    if data == "GetData":
                        response = ob.invoke("You are the server, say hello and something random")
                    elif data == "Stop":
                        response = "Server stopping..."
                        connection.sendall(response.encode())
                        break  # Exit the loop to close this connection
                    elif data.find("Invoke:::") != -1:
                        # Your existing Invoke processing
                        split = data.split(":::")
                        sendText = split[1]
                        
                        if data.find("Context:::") != -1:
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