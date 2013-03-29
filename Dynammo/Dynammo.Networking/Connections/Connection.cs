/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Dynammo.Common;

namespace Dynammo.Networking
{

    /// <summary>
    ///     Stores state information used to asyncronously await replies from packet sends.
    /// </summary>
    internal sealed class ConnectionReplyWaitContext
    {
        public Packet          SendPacket;
        public Packet          ReplyPacket;
        public AutoResetEvent  Event = new AutoResetEvent(false);
    }

    /// <summary>
    ///     Provides a high level interface for making and maintaining connections to 
    ///     different parts of the network using our special protocol.
    /// </summary>
    public sealed class Connection
    {
        #region Members

        // Static members.
        //private static ObjectPool<SocketAsyncEventArgs> g_socketAsyncEventArgsPool = new ObjectPool<SocketAsyncEventArgs>(10000);

        // Connection state.
        private object              m_socket_lock = new object();
        private Socket              m_socket;

        private IPEndPoint          m_endPoint;
        private bool                m_connecting;
        private bool                m_connected;
        private bool                m_listening;
        private bool                m_reconnecting;
        private Connection          m_listenConnection;
        private StreamEncryptor     m_encryptor;
        private StreamCompressor    m_compressor;
        private StreamDeltaEncoder  m_deltaEncoder;
        private int                 m_disconnectTimer = Environment.TickCount;

        private byte[]              m_guid = HardwareHelper.GenerateGUID();
        private byte[]              m_hardwareFingerprint = { };
        private string              m_computerName = "";
        private string              m_computerUserName = "";
        private bool                m_connectionEstablished = false;

        private int                 m_connectionCreateTime = Environment.TickCount;

        private object              m_metaData = null;

        // Data recieving state.
        private bool                m_readingMessagePayload;
        private byte[]              m_recieveBuffer;
        private Packet              m_recievePacket;

        private object              m_messageQueue_lock = new object();
        private Queue<Packet>       m_messageQueue      = new Queue<Packet>();

        // Packet reply awaiting variables.
        private object                           m_replyWaitContexts_lock = new object();
        private List<ConnectionReplyWaitContext> m_replyWaitContexts      = new List<ConnectionReplyWaitContext>();

        // Peer connections.
        private object              m_peers_lock            = new object();
        private List<Connection>    m_peers                 = new List<Connection>();

        private List<Connection>    m_connected_peers       = new List<Connection>();
        private List<Connection>    m_disconnected_peers    = new List<Connection>();

        // Transfer tracking.
        private int                 m_sentBytes;
        private int                 m_queuedSendBytes;
        private int                 m_recievedBytes;
        private int                 m_deltaSentBytes;
        private int                 m_deltaRecievedBytes;
        private int                 m_sendPacketIDCounter;
        private int                 m_recvPacketIDCounter;

        // Ping tracking.
        private int                 m_timeSinceLastPing;
        private int                 m_timeSinceLastPong;
        private bool                m_waitingForPong;
        private int                 m_ping;

        #endregion
        #region Properties

        /// <summary>
        ///     Gets a list of the current peers connected to us (if/when we are listening).
        /// </summary>
        public List<Connection> Peers
        {
            get 
            {

                // We always create a copy when iterating over peers to prevent people
                // fiddling with the underlying collection - also because the internal list can 
                // be modified at any time. 
                List<Connection> peersCopy;

                lock (m_peers_lock)
                {
                    peersCopy = m_peers.ToList<Connection>();
                }

                return peersCopy; 

            }
        }

        /// <summary>
        ///     Gets if this connection is connected or not.
        /// </summary>
        public bool Connected
        {
            get 
            {
                lock (m_socket_lock)
                {
                    return m_connected;
                }
            }
        }

        /// <summary>
        ///     Gets if this connection is connected or not.
        /// </summary>
        public bool ConnectionEstablished
        {
            get 
            {
                lock (m_socket_lock)
                {
                    return m_connectionEstablished;
                }
            }
        }

        /// <summary>
        ///     Returns the last point at which we were connected.
        /// </summary>
        public int DisconnectTimer
        {
            get
            {
                return m_disconnectTimer;
            }
        }

        /// <summary>
        ///     Gets if this connection is connecting or not.
        /// </summary>
        public bool Connecting
        {
            get 
            {
                lock (m_socket_lock)
                {
                    return m_connecting;
                }
            }
        }

        /// <summary>
        ///     Gets if this connection is reconnecting or not.
        /// </summary>
        public bool Reconnecting
        {
            get
            {
                lock (m_socket_lock)
                {
                    return m_reconnecting;
                }
            }
        }

        /// <summary>
        ///     Gets if this connection is listening or not.
        /// </summary>
        public bool Listening
        {
            get { return m_listening; }
        }

        /// <summary>
        ///     Gets the number of bytes sent via this connection.
        /// </summary>
        public int SentBytes
        {
            get { return m_sentBytes; }
        }

        /// <summary>
        ///     Gets the number of bytes queued to be sent over this connection.
        /// </summary>
        public int QueuedSendBytes
        {
            get { return m_queuedSendBytes; }
        }

        /// <summary>
        ///     Gets the number of bytes recieved via this connection.
        /// </summary>
        public int RecievedBytes
        {
            get { return m_recievedBytes; }
        }

        /// <summary>
        ///     Gets the number of bytes sent via this connection since last call.
        /// </summary>
        public int DeltaBandwidthIn
        {
            get 
            {
                int final = m_recievedBytes - m_deltaRecievedBytes;
                m_deltaRecievedBytes = m_recievedBytes;

                lock (m_peers_lock)
                {
                    foreach (Connection p in m_peers)
                    {
                        final += p.DeltaBandwidthIn;
                    }
                }

                return final;
            }
        }

        /// <summary>
        ///     Gets the number of bytes recieved via this connection since last call.
        /// </summary>
        public int DeltaBandwidthOut
        {
            get
            {
                int final = m_sentBytes - m_deltaSentBytes;
                m_deltaSentBytes = m_sentBytes;

                lock (m_peers_lock)
                {
                    foreach (Connection p in m_peers)
                    {
                        final += p.DeltaBandwidthOut;
                    }
                }

                return final;
            }
        }

        /// <summary>
        ///     Gets the end-point of this connection.
        /// </summary>
        public System.Net.IPEndPoint ConnectionEndPoint
        {
            get { return m_endPoint; }
        }

        /// <summary>
        ///     Gets an IP address this connection is connected to. If this is a local address
        ///     then the LAN IP of this machine will be returned.
        /// </summary>
        public string IPAddress
        {
            get
            {
                if (m_endPoint != null)
                {
                    string ip = m_endPoint.Address.ToString();
                    if (ip == "172.0.0.1" || ip == "localhost" || ip == "::1")
                    {
                        return HardwareHelper.GetLocalIPAddress();
                    }
                    return ip;
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        ///     Gets port this connection is connected to.
        /// </summary>
        public int Port
        {
            get
            {
                if (m_endPoint != null)
                {
                    return m_endPoint.Port;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the meta data associated with this connection.
        /// </summary>
        public object MetaData
        {
            get { return m_metaData; }
            set { m_metaData = value; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Generates an encryption key based on the contents of a connection packet.
        /// </summary>
        /// <returns>New encryption key.</returns>
        private string GenerateEncryptionKey()
        {
            string unhashed = StringHelper.ByteArrayToString(m_guid);
            unhashed += StringHelper.ByteArrayToString(m_hardwareFingerprint);

            // Hash the entire string.
            SHA1 hasher = new SHA1CryptoServiceProvider();
            byte[] hash = hasher.ComputeHash(StringHelper.StringToByteArray(unhashed));

            return StringHelper.ByteArrayToString(hash);
        }

        /// <summary>
        ///     Processes a packet if its an internal packet.
        /// </summary>
        /// <param name="packet">Packet to process.</param>
        /// <returns>True if the packet was processes, otherwise false.</returns>
        private bool ProcessInternalPacket(Packet packet)
        {
            // Responds to a ping request from a client.
            if (packet is PingPacket)
            {
                PongPacket pong = new PongPacket();
                SendPacket(pong, packet);

                return true;
            }

            // Registers the response from a ping packet we have sent.
            else if (packet is PongPacket)
            {
                m_ping              = Environment.TickCount - m_timeSinceLastPing;
                m_timeSinceLastPong = Environment.TickCount;
                m_waitingForPong    = false;

                return true;
            }

            // Provides connections details regarding their connection.
            else if (packet is ConnectPacket)
            {
                if (m_listenConnection == null)
                {
                    throw new InvalidOperationException("Remote host sent a connect packet to a non-listening connection.");
                }

                ConnectPacket p = (ConnectPacket)packet;

                m_hardwareFingerprint = p.HardwareFingerprint;
                m_guid                = p.ConnectionGUID;
                m_computerUserName    = p.ComputerUserName;
                m_computerName        = p.ComputerName;

                Logger.Info("Connection from {0} established connection.", LoggerVerboseLevel.High, m_endPoint.Serialize().ToString());
                Logger.Info("\tHardware Fingerprint: {0}", LoggerVerboseLevel.High, StringHelper.ByteArrayToHexString(m_hardwareFingerprint));
                Logger.Info("\tGUID: {0}", LoggerVerboseLevel.High, StringHelper.ByteArrayToHexString(m_guid));
                Logger.Info("\tComputer Name: {0}", LoggerVerboseLevel.High, m_computerName);
                Logger.Info("\tComputer User Name: {0}", LoggerVerboseLevel.High, m_computerUserName);

                // Send an AOK message :3.
                ConnectReplyPacket reply = new ConnectReplyPacket();
                SendPacket(reply, packet);

                lock (m_socket_lock)
                {
                    m_connectionEstablished = true;
                    lock (m_listenConnection.m_peers_lock)
                    {
                        m_listenConnection.m_connected_peers.Add(this);
                    }
                    m_encryptor = new StreamEncryptor(GenerateEncryptionKey());
                }

                return true;
            }

            // Finishes establishing a connection.
            else if (packet is ConnectReplyPacket)
            {
                // Generate new encryption keys.
                lock (m_socket_lock)
                {
                    m_encryptor = new StreamEncryptor(GenerateEncryptionKey());
                }

                m_connectionEstablished = true;
            }

            // Disconnects the user from the server gracefully.
            else if (packet is DisconnectPacket)
            {
                DisconnectAsync(true).Wait();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Processes a packet that has just been recieved, if its internal it will do as required, if not it
        ///     will queue it and wait for the host application to deal with it.
        /// </summary>
        /// <param name="packet">Packet to process.</param>
        private void ProcessPacket(Packet packet)
        {
            lock (m_socket_lock)
            {
                // Increment the recieve packet ID.
                m_recvPacketIDCounter++;
                if (m_recvPacketIDCounter >= ushort.MaxValue)
                {
                    m_recvPacketIDCounter = 0;
                }

                // Work out ID of this packet.
                packet.PacketID = m_recvPacketIDCounter;
            }

            // Is someone waiting for this reply?
            ConnectionReplyWaitContext replyContext = null;
            lock (m_replyWaitContexts_lock)
            {
                foreach (ConnectionReplyWaitContext context in m_replyWaitContexts)
                {
                    if (context.SendPacket.PacketID == packet.ReplyToPacketID)
                    {
                        replyContext = context;
                        break;
                    }
                }
            }

            // Process system packet.
            if (ProcessInternalPacket(packet) == false)
            {
                if (m_connectionEstablished == true)
                {
                    // Not internal, leave it to host to deal with.
                    if (replyContext == null)
                    {
                        lock (m_messageQueue_lock)
                        {
                            m_messageQueue.Enqueue(packet);
                        }
                    }
                }
                else
                {
                    // TODO: Store packets till connection is established!
                    Logger.Error("Recieved non-connection based packet before connection was established.", LoggerVerboseLevel.Normal);
                }
            }

            if (replyContext != null)
            {
                replyContext.ReplyPacket = packet;
                replyContext.Event.Set();
            }
        }

        /// <summary>
        ///     Invoked when a socket connection operation completes.
        /// </summary>
        /// <param name="sender">Object that invoked this event.</param>
        /// <param name="e">Arguments describing why this event was invoked.</param>
        private async void SocketConnect_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= SocketConnect_Completed;
            //g_socketAsyncEventArgsPool.FreeObject(e);
            e.Dispose();

            if (m_connected == true)
            {
                return;
            }

            if (e.SocketError == SocketError.Success)
            {
                lock (m_socket_lock)
                {
                    m_connected = true;
                    m_connecting = false;
                    m_connectionCreateTime = Environment.TickCount;
                    m_connectionEstablished = false;
                }

                // Begin reading from the stream.
                StartRead();

                // Begin polling for information.
                StartPoll();

                // Send a connection packet.
                ConnectPacket connectPacket         = new ConnectPacket();
                connectPacket.ConnectionGUID        = m_guid;
                connectPacket.HardwareFingerprint   = HardwareHelper.GenerateFingerprint();
                connectPacket.ComputerUserName      = Environment.UserName;
                connectPacket.ComputerName          = Environment.MachineName;

                m_hardwareFingerprint               = connectPacket.HardwareFingerprint;
                m_computerUserName                  = connectPacket.ComputerUserName;
                m_computerName                      = connectPacket.ComputerName;

                // Wait for a reply
                ConnectReplyPacket connectReplyPacket = (await SendPacketAndWaitAsync(connectPacket)) as ConnectReplyPacket;
                if (connectReplyPacket == null)
                {
                    await DisconnectAsync(true);
                }
            }
            else
            {
                lock (m_socket_lock)
                {
                    m_connected = false;
                    m_connecting = false;
                }
            }
        }

        /// <summary>
        ///     Poll the connections.  This just checks on the connection
        ///     every so ofen to perform routine maintenance (sending ping packets, etc).
        /// </summary>
        private bool Poll()
        {
            // Disconnected? :(
            if (m_connected == true || m_connecting == true || m_reconnecting == true)
            {
                m_disconnectTimer = Environment.TickCount;
            }

            // Update connected state.
            lock (m_socket_lock)
            {
                if (m_connected == true && m_socket != null)
                {
                    if (m_socket.Connected == false)
                    {
                        DisposeSocket(true);
                        return false;
                    }
                }
            }

            // Send a ping!
            if (m_waitingForPong == false && m_connectionEstablished == true)
            {
                if (Environment.TickCount - m_timeSinceLastPong > Settings.CONNECTION_PING_INTERVAL || m_timeSinceLastPong == 0)
                {
                    PingPacket packet = new PingPacket();
                    SendPacket(packet);

                    m_timeSinceLastPing = Environment.TickCount;
                    m_waitingForPong = true;
                }
            }

            // Has ping reply timed out?
            else if (m_connectionEstablished == true)
            {
                if (m_waitingForPong == true && Environment.TickCount - m_timeSinceLastPing > Settings.CONNECTION_PING_TIMEOUT_INTERVAL)
                {
                    Logger.Error("Failed to recieve ping within timeout from peer at end point {0}.", LoggerVerboseLevel.Normal, m_endPoint.ToString());
                    DisposeSocket(true);
                    return false;
                }
            }

            // Have we not established connection yet?
            else if (m_connectionEstablished == false)
            {
                if (Environment.TickCount - m_connectionCreateTime > Settings.CONNECTION_ESTABLISH_TIMEOUT_INTERVAL)
                {
                    Logger.Error("Failed to recieve connection packet within timeout from peer at end point {0}.", LoggerVerboseLevel.Normal, m_endPoint.ToString());
                    DisposeSocket(true);
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        ///     Begins the process of polling the connection. This just checks on the connection
        ///     every so ofen to perform routine maintenance (sending ping packets, etc).
        /// </summary>
        private void StartPoll()
        {
            Task.Run(async () => {

                // Wait until we run again.
                await Task.Delay(Settings.CONNECTION_POLL_INTERVAL);

                // Ignore, we aren't connected anymore :S.
                if (m_connected == false)
                {
                    return;
                }

                // Poll!
                if (Poll() == true && m_connected == true)
                {
                    // Start polling again.
                    StartPoll();
                }

            });
        }

        /// <summary>
        ///     Invoked when a socket send has completed.
        /// </summary>
        /// <param name="sender">Object that invoked this event.</param>
        /// <param name="e">Arguments describing why this event was invoked.</param>
        void SocketIO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // Did we error out? If so, bail out.
            if (e.SocketError != SocketError.Success)
            {
                Logger.Error("Recieved socket error ({0}) during IO, closing socket.", LoggerVerboseLevel.High, e.SocketError.ToString());

                e.Completed -= SocketIO_Completed;
                //g_socketAsyncEventArgsPool.FreeObject(e);
                e.Dispose();                
                
                DisposeSocket(true);
                return;
            }

            // Client has almost certainly disconnected in this situation.
            if (e.BytesTransferred <= 0 || m_socket == null || m_socket.Connected == false)
            {
                if (m_socket != null)// && m_socket.Connected == false)
                {
                    Logger.Error("Socket appears to have closed during IO.", LoggerVerboseLevel.High);
                }

                e.Completed -= SocketIO_Completed;
                //g_socketAsyncEventArgsPool.FreeObject(e);
                e.Dispose();

                DisposeSocket(true);
                return;
            }

            // Track bandwidth stats.
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    m_sentBytes += e.BytesTransferred;
                    m_queuedSendBytes -= e.BytesTransferred;
                    
                    e.Completed -= SocketIO_Completed;
                    e.Dispose();    
                    //g_socketAsyncEventArgsPool.FreeObject(e);

                    break;

                case SocketAsyncOperation.Receive:
                    m_recievedBytes += e.BytesTransferred;

                    // What part of the message are we recieving?
                    if (m_readingMessagePayload == true)
                    {
                        if (m_recievePacket.RecievePayload(m_recieveBuffer, m_encryptor, m_compressor, m_deltaEncoder) == true)
                        {
                            Logger.Info("Recieved pack et of type '{0}' of size '{1}' from '{2}'.", LoggerVerboseLevel.Highest, m_recievePacket.GetType().Name, Packet.HEADER_SIZE + m_recieveBuffer.Length, m_endPoint.Serialize().ToString());
                            ProcessPacket(m_recievePacket);
                        }
                        else
                        {
                            Logger.Error("Recieved invalid payload for packet of type '{0}' of size '{1}', dropping packet.", LoggerVerboseLevel.Normal, m_recievePacket.GetType().Name, m_recieveBuffer.Length);
                        }
                        m_readingMessagePayload = false;
                    }
                    else
                    {
                        m_recievePacket = Packet.FromHeader(m_recieveBuffer, m_encryptor, m_compressor, m_deltaEncoder);
                        if (m_recievePacket == null)
                        {
                            Logger.Error("Recieved invalid header for packet of size '{0}', dropping packet.", LoggerVerboseLevel.Normal, m_recieveBuffer.Length);
                        }
                        else
                        {
                            if (m_recievePacket.PayloadSize > 0)
                            {
                                m_readingMessagePayload = true;
                            }
                            else
                            {
                               Logger.Info("Recieved packet of type '{0}' of size '{1}' from '{2}'.", LoggerVerboseLevel.Highest, m_recievePacket.GetType().Name, m_recieveBuffer.Length, m_endPoint.Serialize().ToString());
                                ProcessPacket(m_recievePacket);
                            }
                        }
                    }

                    e.Completed -= SocketIO_Completed;
                    //g_socketAsyncEventArgsPool.FreeObject(e);
                    e.Dispose();

                    // Continue reading.
                    StartRead();

                    break;
            }
        }

        /// <summary>
        ///     Begins the process of reading from the stream.
        /// </summary>
        private void StartRead()
        {
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();//g_socketAsyncEventArgsPool.NewObject();
            
            // Read message header.
            if (m_readingMessagePayload == false)
            {
                m_recieveBuffer = new byte[Packet.HEADER_SIZE];
            }

            // Read message body.
            else
            {
                m_recieveBuffer = new byte[m_recievePacket.PayloadSize];
            }

            e.Completed += SocketIO_Completed;
            e.SetBuffer(m_recieveBuffer, 0, m_recieveBuffer.Length);

            // Begin recieving!
            lock (m_socket_lock)
            {
                if (m_socket == null)
                {
                    return;
                }

                if (!m_socket.ReceiveAsync(e))
                {
                    SocketIO_Completed(this, e);
                }
            }
        }

        /// <summary>
        ///     Invoked when a socket accept has completed.
        /// </summary>
        /// <param name="sender">Object that invoked this event.</param>
        /// <param name="e">Arguments describing why this event was invoked.</param>
        void SocketAccept_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= SocketAccept_Completed;
            //g_socketAsyncEventArgsPool.FreeObject(e);
            e.Dispose();

            // Did we error out? If so, bail out.
            if (e.SocketError != SocketError.Success)
            {
                Logger.Error("Recieved socket error ({0}) during socket accept, closing socket.", LoggerVerboseLevel.High, e.SocketError.ToString());

                DisposeSocket(true);
                return;
            }

            // Create a new connection to represent this one.
            Connection connection = new Connection();
            connection.SetupAsPeer(this, e.AcceptSocket);

            // Peer connected!
            PeerConnected(connection);

            // Accept the next connection.
            StartAccept();
        }

        /// <summary>
        ///     Starts a socket accept operation.
        /// </summary>
        private void StartAccept()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();//g_socketAsyncEventArgsPool.NewObject();//;new SocketAsyncEventArgs();
            args.UserToken = this;
            args.Completed += SocketAccept_Completed;
            args.AcceptSocket = null;
            args.SetBuffer(null, 0, 0);

            lock (m_socket_lock)
            {
                if (m_socket == null)
                {
                    return;
                }

                if (!m_socket.AcceptAsync(args) == true)
                {
                    SocketAccept_Completed(this, args);
                }
            }
        }

        /// <summary>
        ///     Sets up this connection as a peer of a listen connection.
        /// </summary>
        private void SetupAsPeer(Connection listenConnection, Socket socket)
        {
            lock (m_socket_lock)
            {
                m_listenConnection = listenConnection;
                m_socket = socket;
                m_connected = true;
                m_endPoint = (System.Net.IPEndPoint)socket.RemoteEndPoint;

                m_encryptor = new StreamEncryptor("");
                m_compressor = new StreamCompressor();
                m_deltaEncoder = new StreamDeltaEncoder();
            }

           // Begin reading from the server.
           StartRead();

           // Start polling connection.
           StartPoll();
        }

        /// <summary>
        ///     Disposes of the socket if it exists.
        /// </summary>
        /// <param name="byError">If this disconnect was caused by an error, then we will try and reconnect.</param>
        /// <param name="timeout">Amount of time in milliseconds to wait until socket is closed. 0 closes socket instantly.</param>
        /// <param name="reconnectDelay">Used to determine at what point we reconnect.</param>
        private void DisposeSocket(bool byError, int timeout = 1000)
        {
            // Disconnect from the socket.
            lock (m_socket_lock)
            {
                if (m_socket != null)
                {
                    try
                    {
                        if (m_socket.Connected == true)
                        {
                            m_socket.Disconnect(false);
                        }
                        m_socket.Close(timeout);
                        m_socket = null;
                    }
                    catch (SocketException)
                    {
                        Logger.Warning("Recieved socket exception whilst disposing of old socket.", LoggerVerboseLevel.Highest);
                    }
                    catch (ObjectDisposedException)
                    {
                        Logger.Warning("Recieved socket exception whilst disposing of old socket.", LoggerVerboseLevel.Highest);
                    }
                    catch (NullReferenceException)
                    {
                        Logger.Warning("Recieved socket exception whilst disposing of old socket.", LoggerVerboseLevel.Highest);
                    }
                }
            }

            // Disconnect all peers.
            lock (m_peers_lock)
            {
                try
                {
                    foreach (Connection connection in m_peers)
                    {
                        connection.DisposeSocket(false, timeout); // BUG: Lock loop, this calls DisposeSocket, which in turn calls listen_socket (this connection) .PeerDisconnect, which in turn calls this function!
                        m_disconnected_peers.Add(connection);
                    }
                    m_peers.Clear();
                }
                catch (InvalidOperationException ex)
                {
                    // Hack: This needs fixing, its just to prevent a bug with the lock loop that causes 
                    //       a "collection modified" error.
                }
            }

            // Reset state.
            lock (m_socket_lock)
            {
                m_connected = false;
                m_connecting = false;
                m_listening = false;
                m_readingMessagePayload = false;

                m_hardwareFingerprint = new byte[0];
                m_computerName = "";
                m_computerUserName = "";
                m_connectionEstablished = false;

                m_sentBytes = 0;
                m_recievedBytes = 0;
                m_queuedSendBytes = 0;
                m_sendPacketIDCounter = 0;
                m_recvPacketIDCounter = 0;

                m_timeSinceLastPing = 0;
                m_timeSinceLastPong = 0;
                m_waitingForPong = false;
                m_ping = 0;

                m_encryptor = null;
                m_deltaEncoder = null;
                m_compressor = null;
            }

            lock (m_messageQueue_lock)
            {
                m_messageQueue.Clear();
            }

            // If we have a listen socket, remove us from their peer list.
            if (m_listenConnection != null)
            {
                m_listenConnection.PeerDisconnected(this);
            }

            // Clear out the packet wait list.
            lock (m_replyWaitContexts_lock)
            {
                foreach (ConnectionReplyWaitContext context in m_replyWaitContexts)
                {
                    context.Event.Set();
                }
                m_replyWaitContexts.Clear();
            }

            // Reconnect to the server as it appears
            // that we have been disconnected!
            //if (byError == true)
            //{
            //    PerformReconnect();
            //}
        }

        /// <summary>
        ///     Invoked when a peer connects to us.
        /// </summary>
        /// <param name="connection">Peer that connected to us.</param>
        private void PeerConnected(Connection connection)
        {
            lock (m_peers_lock)
            {
               if (m_peers.Contains(connection))
               {
                   return;
               }

                m_peers.Add(connection);

                // Log the connection.
                Logger.Info("New peer connected from {0}, {1} peers now connected.", LoggerVerboseLevel.High, connection.ConnectionEndPoint.Serialize().ToString(), m_peers.Count);
            }
        }

        /// <summary>
        ///     Invoked when a peer disconnects to us.
        /// </summary>
        /// <param name="connection">Peer that disconnected from us.</param>
        private void PeerDisconnected(Connection connection)
        {
            lock (m_peers_lock)
            {
                if (!m_peers.Contains(connection))
                {
                    return;
                }

                m_peers.Remove(connection);
                m_disconnected_peers.Add(connection);

                // Log the connection.
                Logger.Info("Peer disconnected from {0}, {1} peers now connected.", LoggerVerboseLevel.High, connection.ConnectionEndPoint.Serialize().ToString(), m_peers.Count);
            }
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Deconstructs all connection resources.
        /// </summary>
        ~Connection()
        {
            lock (m_socket_lock)
            {
                if (m_socket != null)
                {
                    try
                    {
                        m_socket.Disconnect(false);
                        m_socket.Close(1000); // No waiting, abort!
                        m_socket = null;
                    }
                    catch (SocketException)
                    {
                        Logger.Warning("Recieved socket exception whilst disposing of old socket.", LoggerVerboseLevel.Highest);
                    }
                    catch (ObjectDisposedException)
                    {
                        Logger.Warning("Recieved socket exception whilst disposing of old socket.", LoggerVerboseLevel.Highest);
                    }
                }
            }
        }

        /// <summary>
        ///     Reconnects to the server if we have been booted for some reason.
        /// </summary>
        public async Task<bool> ReconnectAsync()
        {
            lock (m_socket_lock)
            {
                // If we are already reconnecting then ignore.
                if (m_reconnecting == true)
                {
                    return false;
                }
                m_reconnecting = true;
            }

            // Dispose of old socket.
            DisposeSocket(false);

            Logger.Error("Attempting to perform socket reconnection ...", LoggerVerboseLevel.Normal);

            // Keep attempting to reconnect.
            long reconnect_timeout = Environment.TickCount + Settings.CONNECTION_RECONNECT_DURATION;
            int exponential_backoff = Settings.CONNECTION_RECONNECT_BACKOFF_START;

            while (Environment.TickCount < reconnect_timeout)
            {
                bool success = await ConnectAsync(m_endPoint.Address.ToString(), (ushort)m_endPoint.Port);
                if (m_connected == true && success == true)
                {
                    break;
                }

                // Exponentially backoff so we don't flood the server with connections.
                await Task.Delay(exponential_backoff);
                exponential_backoff = Math.Min(Settings.CONNECTION_RECONNECT_BACKOFF_MAX, exponential_backoff * Settings.CONNECTION_RECONNECT_BACKOFF_MULTIPLIER);

                Logger.Error("Reconnection attempt failed, attempting again in {0}ms.", LoggerVerboseLevel.Normal, exponential_backoff);
            }

            if (m_connected == false)
            {
                Logger.Error("Failed to reconnect to host after disconnect.", LoggerVerboseLevel.Normal);
            }

            lock (m_socket_lock)
            {
                m_reconnecting = false;
            }

            return m_connected;
        }

        /// <summary>
        ///     Relistens the server we have been booted for some reason.
        /// </summary>
        public async Task<bool> RelistenAsync()
        {
            lock (m_socket_lock)
            {
                // If we are already reconnecting then ignore.
                if (m_reconnecting == true)
                {
                    return false;
                }
                m_reconnecting = true;
            }

            Logger.Error("Attempting to start listening again ...", LoggerVerboseLevel.Normal);

            // Keep attempting to reconnect.
            long reconnect_timeout = Environment.TickCount + Settings.CONNECTION_RECONNECT_DURATION;
            int exponential_backoff = Settings.CONNECTION_RECONNECT_BACKOFF_START;

            while (Environment.TickCount < reconnect_timeout)
            {
                bool success = await ListenAsync((ushort)m_endPoint.Port, m_endPoint.Address.ToString());
                if (m_listening == true && success == true)
                {
                    break;
                }

                // Exponentially backoff so we don't flood the server with connections.
                await Task.Delay(exponential_backoff);
                exponential_backoff = Math.Min(Settings.CONNECTION_RECONNECT_BACKOFF_MAX, exponential_backoff * Settings.CONNECTION_RECONNECT_BACKOFF_MULTIPLIER);

                Logger.Error("Relisten attempt failed, attempting again in {0}ms.", LoggerVerboseLevel.Normal, exponential_backoff);
            }

            if (m_connected == false)
            {
                Logger.Error("Failed to relisten to host after disconnect.", LoggerVerboseLevel.Normal);
            }

            lock (m_socket_lock)
            {
                m_reconnecting = false;
            }

            return m_connected;
        }

        /// <summary>
        ///     Connects to a service running on another host.
        /// </summary>
        /// <param name="ip">IP address of the server to connect to.</param>
        /// <param name="port">Port number that service is running on.</param>
        public async Task<bool> ConnectAsync(string ip, UInt16 port)
        {
            // Close down old socket.
            DisposeSocket(false);

            m_connecting            = true;
            m_connectionEstablished = false;
            m_encryptor             = new StreamEncryptor("");
            m_compressor            = new StreamCompressor();
            m_deltaEncoder          = new StreamDeltaEncoder();

            // Local address?
            if (ip == "127.0.0.1" || ip == "::1" || ip == HardwareHelper.GetLocalIPAddress())
            {
                ip = "localhost";
            }

            // Create new socket!
            System.Net.IPAddress[] hosts = null;
            try
            {
                hosts = await Dns.GetHostAddressesAsync(ip);
            }
            catch (System.Net.Sockets.SocketException)
            {
                hosts = null;
            }
            if (hosts == null || hosts.Length == 0)
            {
                Logger.Error("Failed to connect to host - could not find address for {0}.", LoggerVerboseLevel.Normal, ip);
                m_connecting = false;
                return false;
            }

            System.Net.IPAddress host = hosts[0];
            try
            {
                bool result = false;
                SocketAsyncEventArgs args;

                lock (m_socket_lock)
                {
                    m_endPoint                  = new IPEndPoint(host, port);
                    m_socket                    = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    m_socket.SendTimeout        = Settings.CONNECTION_SEND_TIMEOUT;
                    m_socket.ReceiveTimeout     = Settings.CONNECTION_RECIEVE_TIMEOUT;
               
                    // Get connecting!
                    args                        = new SocketAsyncEventArgs();//g_socketAsyncEventArgsPool.NewObject();//new SocketAsyncEventArgs();
                    args.RemoteEndPoint         = m_endPoint;
                    args.UserToken              = this;
                    args.Completed              += SocketConnect_Completed;

                    Logger.Info("Attempting to connect to {0}:{1}.", LoggerVerboseLevel.High, ip, port);

                    result = m_socket.ConnectAsync(args);
                }

                if (result == false)
                {
                    SocketConnect_Completed(this, args);
                }

                // Wait until we have finished connecting to the host.
                while (m_connecting == true || (m_connected == true && m_connectionEstablished == false))
                {
                    await Task.Delay(10);
                }

                if (m_connected == false)
                {
                    Logger.Warning("Failed to connect to {0}:{1}.", LoggerVerboseLevel.High, ip, port);
                }
            }
            catch (SocketException)
            {
                Logger.Error("Encountered socket exception while attempting to connect to host.", LoggerVerboseLevel.Normal);
                DisposeSocket(false);
            }

            // And we are done!
            return m_connected;
        }


        /// <summary>
        ///     Connects to the service and updates over time.
        /// </summary>
        /// <param name="ip">IP address of the server to connect to.</param>
        /// <param name="port">Port number that service is running on.</param>
        public bool ConnectOverTime(string ip, UInt16 port)
        {
            // Close down old socket.
            DisposeSocket(false);

            m_connecting = true;
            m_connectionEstablished = false;
            m_encryptor = new StreamEncryptor("");
            m_compressor = new StreamCompressor();
            m_deltaEncoder = new StreamDeltaEncoder();

            // Local address?
            if (ip == "127.0.0.1" || ip == "::1" || ip == HardwareHelper.GetLocalIPAddress())
            {
                ip = "localhost";
            }

            // Create new socket!
            System.Net.IPAddress[] hosts = null;
            try
            {
                hosts = Dns.GetHostAddresses(ip);
            }
            catch (System.Net.Sockets.SocketException)
            {
                hosts = null;
            }
            if (hosts == null || hosts.Length == 0)
            {
                Logger.Error("Failed to connect to host - could not find address for {0}.", LoggerVerboseLevel.Normal, ip);
                m_connecting = false;
                return false;
            }

            System.Net.IPAddress host = hosts[0];
            try
            {
                bool result = false;
                SocketAsyncEventArgs args;

                lock (m_socket_lock)
                {
                    m_endPoint = new IPEndPoint(host, port);
                    m_socket = new Socket(host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    m_socket.SendTimeout = Settings.CONNECTION_SEND_TIMEOUT;
                    m_socket.ReceiveTimeout = Settings.CONNECTION_RECIEVE_TIMEOUT;

                    // Get connecting!
                    args = new SocketAsyncEventArgs();//g_socketAsyncEventArgsPool.NewObject();//new SocketAsyncEventArgs();
                    args.RemoteEndPoint = m_endPoint;
                    args.UserToken = this;
                    args.Completed += SocketConnect_Completed;

                    Logger.Info("Attempting to connect to {0}:{1}.", LoggerVerboseLevel.High, ip, port);

                    result = m_socket.ConnectAsync(args);
                }

                if (result == false)
                {
                    SocketConnect_Completed(this, args);
                }
            }
            catch (SocketException)
            {
                Logger.Error("Encountered socket exception while attempting to connect to host.", LoggerVerboseLevel.Normal);
                DisposeSocket(false);
            }

            // And we are done!
            return m_connected;
        }


        /// <summary>
        ///     Listens for connections from other clients/peers on a given port.
        /// </summary>
        /// <param name="port">Port number to listen on.</param>
        /// <param name="ip">Optional: Can be used to set the IP to listen on on multihomed computers.</param>
        public async Task<bool> ListenAsync(UInt16 port, string ip = "")
        {
            // Close down old socket.
            DisposeSocket(false);
            m_connecting = true;
            m_listening = false;

            // Use machine name if IP is not given.
            if (ip == "")
            {
                ip = Environment.MachineName;
            }

            // Create new socket!
            System.Net.IPAddress[] hosts = await Dns.GetHostAddressesAsync(ip);
            if (hosts == null || hosts.Length == 0)
            {
                Logger.Error("Failed to start listening - could not find ip address for host '{0}'.", LoggerVerboseLevel.Normal, ip);
                m_connecting = false;
                return false;
            }

            try
            {
                m_endPoint              = new IPEndPoint(hosts[0], port);

                lock (m_socket_lock)
                {
                    m_socket = new Socket(hosts[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    m_socket.SendTimeout = Settings.CONNECTION_SEND_TIMEOUT;
                    m_socket.ReceiveTimeout = Settings.CONNECTION_RECIEVE_TIMEOUT;
                    m_socket.NoDelay = true;

                    // Start listening on the socket.
                    m_socket.Bind(m_endPoint);

                    // Get the end-point from the socket (because if we bind to port 0 or do anything special like that
                    //                                    then we want to get the allocated end point).
                    m_endPoint = (IPEndPoint)m_socket.LocalEndPoint;

                    // It seems MaxConnections does not work correctly on sockets :S
                    //m_socket.Listen((int)m_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.MaxConnections));

                    m_socket.Listen(Settings.CONNECTION_LISTEN_BACKLOG);
                }

                m_listening = true;

                // Start accepting socket connections.
                StartAccept();
            }
            catch (System.Security.SecurityException)
            {
                Logger.Error("Encountered security exception while attempting to listen on socket.", LoggerVerboseLevel.Normal);
                DisposeSocket(false);
            }
            catch (SocketException)
            {
                Logger.Error("Encountered socket exception while attempting to listen on socket.", LoggerVerboseLevel.Normal);
                DisposeSocket(false);
            }

            // And we are done!
            return m_listening;
        }

        /// <summary>
        ///     Disconnects from the host.
        /// </summary>
        /// <returns>True if the host was force disconnected.</returns>
        /// <param name="force">If set to true, the connection will be immediately terminated, otherwise it will ask the server to terminate it.</param>
        public async Task<bool> DisconnectAsync(bool force = false)
        {
            if (force == true)
            {
                Logger.Info("Forcing disconnect of socket.", LoggerVerboseLevel.High);

                DisposeSocket(false);
                return true;
            }
            else
            {
                // Disconnect peers.
                foreach (Connection conn in m_peers.ToList())
                {
                    await conn.DisconnectAsync(false);
                }
                
                DisconnectPacket packet = new DisconnectPacket();
                SendPacket(packet);

                // Wait until we are disconnected!
                long timeout = System.Environment.TickCount + Settings.CONNECTION_DISCONNECT_TIMEOUT;
                while (m_connected == true)
                {
                    // Force close the socket.
                    if (System.Environment.TickCount >= timeout)
                    {
                        Logger.Info("Forcing disconnect of socket.", LoggerVerboseLevel.High);

                        DisposeSocket(false);
                        return true;
                    }

                    await Task.Delay(10);
                }

                DisposeSocket(false);
                return true;
            }

            Logger.Info("Socket disconnected successfully.", LoggerVerboseLevel.High);

            return false;
        }

        /// <summary>
        ///     Sends a packet to the host on the other end of the connection.
        /// </summary>
        /// <param name="packet">Packet ot send.</param>
        /// <param name="inReplyTo">Packet that the packet being sent is in reply to.</param>
        public void SendPacket(Packet packet, Packet inReplyTo = null)
        {
            // If we are a listen-server then send messages to all child peers.
            if (m_listening == true)
            {
                lock (m_peers_lock)
                {
                    foreach (Connection connection in m_peers)
                    {
                        connection.SendPacket(packet, inReplyTo);
                    }
                }
            }

            // Send as a peer.
            else
            {
                if (m_socket == null || m_connected == false || (m_connectionEstablished == false && (packet as ConnectPacket) == null && (packet as ConnectReplyPacket) == null))
                {
                   // throw new InvalidOperationException("Attempt to send packet to unconnected socket!");
                }

                lock (m_socket_lock)
                {
                    if (m_socket != null && m_connected == true)
                    {
                        // Increment the send packet counter.
                        m_sendPacketIDCounter++;
                        if (m_sendPacketIDCounter >= ushort.MaxValue)
                        {
                            m_sendPacketIDCounter = 0;
                        }

                        // Give the packet a new unique ID.
                        packet.PacketID = m_sendPacketIDCounter;

                        // Convert packet to a buffer!
                        byte[] buffer = packet.ToBuffer(inReplyTo, m_encryptor, m_compressor, m_deltaEncoder);

                        //Logger.Info("Sending packet of type '{0}' of size '{1}' to '{2}'.", LoggerVerboseLevel.Highest, packet.GetType().Name, buffer.Length, m_endPoint.Serialize().ToString());

                        try
                        {
                            SocketAsyncEventArgs e = new SocketAsyncEventArgs();//g_socketAsyncEventArgsPool.NewObject();//new SocketAsyncEventArgs();
                            e.Completed += SocketIO_Completed;
                            e.UserToken = this;
                            e.SetBuffer(buffer, 0, buffer.Length);

                            m_queuedSendBytes += buffer.Length;

                            if (!m_socket.SendAsync(e))
                            {
                                SocketIO_Completed(this, e);
                            }
                        }
                        catch (SocketException)
                        {
                            // Most likely we've been disconnect, we can ignore this as the disconnect code will deal with reconnections.
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Sends a packet to the host on the other end of the connection and waits asynchronously for a reply.
        /// </summary>
        /// <returns>The packet recieved in reply, or null if we failed to recieve a reply.</returns>
        /// <param name="packet">Packet ot send.</param>
        /// <param name="inReplyTo">Packet that the packet being sent is in reply to.</param>
        /// <param name="timeout">If a packet is not recieved within this time (in milliseconds), the function will return null.</param>
        public async Task<Packet> SendPacketAndWaitAsync(Packet packet, Packet inReplyTo = null, int timeout = 60000)
        {
            // Log that we are currently waiting for a reply to this packet.
            ConnectionReplyWaitContext context = new ConnectionReplyWaitContext();
            context.SendPacket  = packet;
            context.ReplyPacket = null;

            lock (m_replyWaitContexts_lock)
            {
                m_replyWaitContexts.Add(context);
            }

            // Wait for a reply.
            Packet reply = await Task<Packet>.Run(() => 
            {
                // Send the packet.
                SendPacket(packet, inReplyTo);
                
                // Await the reply.
                Packet replyPacket = null;
                if (context.Event.WaitOne(timeout) == true)
                {
                    replyPacket = context.ReplyPacket;
                }

                // Remove the context.
                lock (m_replyWaitContexts_lock)
                {
                    if (m_replyWaitContexts.Contains(context))
                    {
                        m_replyWaitContexts.Remove(context);
                    }
                }

                // Return the reply!
                return context.ReplyPacket;
            });

            return reply;
        }

        /// <summary>
        ///     Retrieves the next packet from the current queue.
        /// </summary>
        /// <returns>Next packet in queue, or null if none are available.</returns>
        public Packet DequeuePacket()
        {
            Packet packet = null;
            lock (m_messageQueue_lock)
            {
                packet = m_messageQueue.Count <= 0 ? null : m_messageQueue.Dequeue();
            }
            return packet;
        }

        /// <summary>
        ///     Gets a list of all peers that have connected since the function was last called.
        /// </summary>
        /// <returns>List of peers that have connected since the last call.</returns>
        public List<Connection> GetConnectedPeers()
        {
            List<Connection> list = null;
            lock (m_peers_lock)
            {
                list = new List<Connection>(m_connected_peers);
                m_connected_peers.Clear();
            }
            return list;
        }

        /// <summary>
        ///     Gets a list of all peers that have disconnected since the function was last called.
        /// </summary>
        /// <returns>List of peers that have disconnected since the last call.</returns>
        public List<Connection> GetDisconnectedPeers()
        {
            List<Connection> list = null;
            lock (m_peers_lock)
            {
                list = new List<Connection>(m_disconnected_peers);
                m_disconnected_peers.Clear();
            }
            return list;
        }

        #endregion
    }

}
