using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil;

/// <summary>
/// Networking Controller for Snake game
///
/// This program is a simple Networking program that starts the server
/// as well as client connection. Its purpose is to communicate with client & server
/// ensure connecttion & sending messages.
///
/// Author: Monthon Paul
///
/// Version: November 11, 2022
/// </summary>
public static class Networking {
	/////////////////////////////////////////////////////////////////////////////////////////
	// Server-Side Code
	/////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
	/// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
	/// AcceptNewClient will continue the event-loop.
	/// </summary>
	/// <param name="toCall">The method to call when a new connection is made</param>
	/// <param name="port">The the port to listen on</param>
	public static TcpListener StartServer(Action<SocketState> toCall, int port) {
		TcpListener listener = new(IPAddress.Any, port);
		try {
			// Start the server with TCP
			listener.Start();
			// add a tuple of objects for the server to run
			Tuple<TcpListener, Action<SocketState>> StateListener = new(listener, toCall);
			// Begin accepting a client (starts an event loop)
			listener.BeginAcceptSocket(AcceptNewClient, StateListener);

			return listener;
		} catch (Exception) {
			return new(IPAddress.Any, port);
		}
	}

	/// <summary>
	/// To be used as the callback for accepting a new client that was initiated by StartServer, and 
	/// continues an event-loop to accept additional clients.
	///
	/// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
	/// OnNetworkAction should be set to the delegate that was passed to StartServer.
	/// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
	/// 
	/// If anything goes wrong during the connection process (such as the server being stopped externally), 
	/// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccurred flag set to true 
	/// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
	/// an error occurs.
	///
	/// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
	/// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
	/// </summary>
	/// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
	/// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
	private static void AcceptNewClient(IAsyncResult ar) {
		// Casting with AsyncResults
		Tuple<TcpListener, Action<SocketState>> StateListener = (Tuple<TcpListener, Action<SocketState>>) ar.AsyncState!;
		try {
			// create a new Socket for Client
			Socket newClient = StateListener.Item1.EndAcceptSocket(ar);

			// This disables Nagle's algorithm
			newClient.NoDelay = true;

			// Make a SocketState to add in delegate
			SocketState state = new(StateListener.Item2, newClient);
			state.OnNetworkAction(state);

			//Begins the acceptence
			StateListener.Item1.BeginAcceptSocket(AcceptNewClient, StateListener);
		} catch (Exception) {
			//Create a SocketState, with seting Error Occur & message
			SocketState ErrorState = new(StateListener.Item2, "Connection Interupted");

			//Invoke OnNetworkAction with error flag true
			ErrorState.OnNetworkAction(ErrorState);
		}
	}

	/// <summary>
	/// Stops the given TcpListener.
	/// </summary>
	public static void StopServer(TcpListener listener) {
		try {
			listener.Stop();
		} catch (Exception e) {
			// Anything that goes wrong, display exception message
			Console.WriteLine(e.Message);
		}
	}

	/////////////////////////////////////////////////////////////////////////////////////////
	// Client-Side Code
	/////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Begins the asynchronous process of connecting to a server via BeginConnect, 
	/// and using ConnectedCallback as the method to finalize the connection once it's made.
	/// 
	/// If anything goes wrong during the connection process, toCall should be invoked 
	/// with a new SocketState with its ErrorOccurred flag set to true and an appropriate message 
	/// placed in its ErrorMessage field. Depending on when the error occurs, this should happen either
	/// in this method or in ConnectedCallback.
	///
	/// This connection process should timeout and produce an error (as discussed above) 
	/// if a connection can't be established within 3 seconds of starting BeginConnect.
	/// 
	/// </summary>
	/// <param name="toCall">The action to take once the connection is open or an error occurs</param>
	/// <param name="hostName">The server to connect to</param>
	/// <param name="port">The port on which the server is listening</param>
	public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port) {
		// Generate a Error SocketState
		SocketState ErrorState = new(x => { }, "Filler Error msg");

		// Establish the remote endpoint for the socket.
		IPHostEntry ipHostInfo;
		IPAddress ipAddress = IPAddress.None;

		// Determine if the server address is a URL or an IP
		try {
			ipHostInfo = Dns.GetHostEntry(hostName);
			bool foundIPV4 = false;
			foreach (IPAddress addr in ipHostInfo.AddressList)
				if (addr.AddressFamily != AddressFamily.InterNetworkV6) {
					foundIPV4 = true;
					ipAddress = addr;
					break;
				}
			// Didn't find any IPV4 addresses
			if (!foundIPV4) {
				ErrorState.ErrorMessage = "Invalid address: " + hostName;
				toCall(ErrorState);
				ErrorState.OnNetworkAction(ErrorState);
				return;
			}
		} catch (Exception) {
			// see if host name is a valid ipaddress
			try {
				ipAddress = IPAddress.Parse(hostName);
			} catch (Exception) {
				ErrorState.ErrorMessage = "None valid IP address";
				toCall(ErrorState);
				ErrorState.OnNetworkAction(ErrorState);
				return;
			}
		}

		// Create a TCP/IP socket.
		Socket socket = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

		// This disables Nagle's algorithm (google if curious!)
		// Nagle's algorithm can cause problems for a latency-sensitive 
		// game like ours will be 
		socket.NoDelay = true;

		//Try to connection
		SocketState state = new SocketState(toCall, socket);
		try {
			//Run a Async Result to signal timeout, end the connection if can't be established within 3 seconds
			IAsyncResult asyncResult = state.TheSocket.BeginConnect(ipAddress, port, ConnectedCallback, state);
			if (!asyncResult.AsyncWaitHandle.WaitOne(3000, true)) {
				// if timeout happen, close and throw
				state.TheSocket.Close();
				throw new Exception();
			}
		} catch (Exception) {
			// Generate and ErrorState
			ErrorState.ErrorMessage = "Failed to connect server. Timeout Happen";
			toCall(ErrorState);
			ErrorState.OnNetworkAction(ErrorState);
		}
	}

	/// <summary>
	/// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
	///
	/// Uses EndConnect to finalize the connection.
	/// 
	/// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
	/// either this method or ConnectToServer should indicate the error appropriately.
	/// 
	/// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
	/// with a new SocketState representing the new connection.
	/// 
	/// </summary>
	/// <param name="ar">The object asynchronously passed via BeginConnect</param>
	private static void ConnectedCallback(IAsyncResult ar) {
		SocketState state = (SocketState) ar.AsyncState!;
		try {
			//Finshing up the connection process of the client
			state.TheSocket.EndConnect(ar);
			// This disables Nagle's algorithm
			state.TheSocket.NoDelay = true;
			state.OnNetworkAction(state);

		} catch (Exception) {
			// modify the state to change to an error state
			state.ErrorOccurred = true;
			state.ErrorMessage = "Connection Error";

			//Invoke OnNetworkAction with error flag true
			state.OnNetworkAction(state);
		}
	}


	/////////////////////////////////////////////////////////////////////////////////////////
	// Server and Client Common Code
	/////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
	/// as the callback to finalize the receive and store data once it has arrived.
	/// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
	/// 
	/// If anything goes wrong during the receive process, the SocketState's ErrorOccurred flag should 
	/// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
	/// OnNetworkAction should be invoked. Depending on when the error occurs, this should happen either
	/// in this method or in ReceiveCallback.
	/// </summary>
	/// <param name="state">The SocketState to begin receiving</param>
	public static void GetData(SocketState state) {
		try {
			// Start a receive loop
			state.TheSocket.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, ReceiveCallback, state);
		} catch (Exception e) {
			// Modify the state to change to an error state
			state.ErrorOccurred = true;
			state.ErrorMessage = e.Message;

			//Invoke OnNetworkAction with error flag true
			state.OnNetworkAction(state);
		}
	}

	/// <summary>
	/// To be used as the callback for finalizing a receive operation that was initiated by GetData.
	/// 
	/// Uses EndReceive to finalize the receive.
	///
	/// As stated in the GetData documentation, if an error occurs during the receive process,
	/// either this method or GetData should indicate the error appropriately.
	/// 
	/// If data is successfully received:
	///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
	///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
	///      string builder.
	///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
	/// </summary>
	/// <param name="ar"> 
	/// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
	/// </param>
	private static void ReceiveCallback(IAsyncResult ar) {
		SocketState state = (SocketState) ar.AsyncState!;
		//Begins the finalization of receiving the data
		try {
			// Receieve the num of bytes of data
			int numbytes = state.TheSocket.EndReceive(ar);
			// Check if there is any message sent, Otherwise throw
			if (numbytes != 0) {
				//Lock to prevent multiple cases trying to receive the data
				lock (state.data) {
					// Buffer the data received (we may not have a full message yet)
					state.data.Append(Encoding.UTF8.GetString(state.buffer, 0, numbytes));
				}
				state.OnNetworkAction(state);
			} else {
				throw new Exception();
			}
		} catch (Exception) {
			// Modify the state to change to an error state
			state.ErrorOccurred = true;
			state.ErrorMessage = "Message was not received";
			//Invoke OnNetworkAction with error flag true
			state.OnNetworkAction(state);
		}
	}

	/// <summary>
	/// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
	/// 
	/// If the socket is closed, does not attempt to send.
	/// 
	/// If a send fails for any reason, this method ensures that the Socket is closed before returning.
	/// </summary>
	/// <param name="socket">The socket on which to send the data</param>
	/// <param name="data">The string to send</param>
	/// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
	public static bool Send(Socket socket, string data) {
		// Check if any Socket is connected
		if (!socket.Connected) {
			return false;
		}
		//Begins the attempt to send the data
		try {
			// Process Message by UTF8, to start BeginSend
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, socket);
			return true;
		} catch (Exception) {
			try {
				//When there is an error, shutdown both sockets and close,
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			} catch (Exception) { }
			return false;
		}
	}

	/// <summary>
	/// To be used as the callback for finalizing a send operation that was initiated by Send.
	///
	/// Uses EndSend to finalize the send.
	/// 
	/// This method must not throw, even if an error occurred during the Send operation.
	/// </summary>
	/// <param name="ar">
	/// This is the Socket (not SocketState) that is stored with the callback when
	/// the initial BeginSend is called.
	/// </param>
	private static void SendCallback(IAsyncResult ar) {
		Socket socket = (Socket) ar.AsyncState!;
		// Finalize send operation
		if (socket.Connected) {
			socket.EndSend(ar);
		}
	}


	/// <summary>
	/// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
	/// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
	/// 
	/// If the socket is closed, does not attempt to send.
	/// 
	/// If a send fails for any reason, this method ensures that the Socket is closed before returning.
	/// </summary>
	/// <param name="socket">The socket on which to send the data</param>
	/// <param name="data">The string to send</param>
	/// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
	public static bool SendAndClose(Socket socket, string data) {
		// Check if any Socket is connected
		if (!socket.Connected) {
			return false;
		}
		//Begins the attempt to send the data
		try {
			// Process Message by UTF8, to start BeginSend
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendAndCloseCallback, socket);
			return true;
		} catch (Exception) {
			try {
				//When there is an error, shutdown both sockets and close,
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			} catch (Exception) { }
			return false;
		}
	}

	/// <summary>
	/// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
	///
	/// Uses EndSend to finalize the send, then closes the socket.
	/// 
	/// This method must not throw, even if an error occurred during the Send operation.
	/// 
	/// This method ensures that the socket is closed before returning.
	/// </summary>
	/// <param name="ar">
	/// This is the Socket (not SocketState) that is stored with the callback when
	/// the initial BeginSend is called.
	/// </param>
	private static void SendAndCloseCallback(IAsyncResult ar) {
		Socket socket = (Socket) ar.AsyncState!;
		// Finalize send operation as well as close the socket
		if (socket.Connected) {
			socket.EndSend(ar);
			socket.Close();
		}
	}
}
