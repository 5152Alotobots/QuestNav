using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using NetworkTables;
using UnityEngine;
using QuestNav.Core;
using QuestNav.Telemetry;

namespace QuestNav.Network
{
    /// <summary>
    /// Manages NetworkTables connections and communication
    /// </summary>
    public class NetworkTableManager : MonoBehaviour
    {
        #region Network Configuration
        /// <summary>
        /// Current IP address for connection
        /// </summary>
        private string ipAddress = "";
        
        /// <summary>
        /// Current team number
        /// </summary>
        private string teamNumber = QuestNavConstants.DEFAULT_TEAM_NUMBER;
        
        /// <summary>
        /// NetworkTables connection for FRC data communication
        /// </summary>
        private Nt4Source nt = null;
        #endregion
        
        #region Connection Management
        /// <summary>
        /// Reconnection delay variable for delaying failed connection attempts
        /// </summary>
        private float reconnectDelay = QuestNavConstants.DEFAULT_RECONNECT_DELAY;
        
        /// <summary>
        /// List of failed connection attempt candidates
        /// </summary>
        private Dictionary<string, float> failedCandidates = new Dictionary<string, float>();
        
        /// <summary>
        /// Coroutine for connection management
        /// </summary>
        private Coroutine _connectionCoroutine;
        
        /// <summary>
        /// Coroutine for heartbeat management
        /// </summary>
        private Coroutine _heartbeatCoroutine;
        #endregion
        
        #region Heartbeat System
        /// <summary>
        /// Heartbeat timer to track time since last heartbeat
        /// </summary>
        private float heartbeatTimer = 0f;
        
        /// <summary>
        /// Last heartbeat request ID sent
        /// </summary>
        private double lastHeartbeatRequestId = 0;
        
        /// <summary>
        /// Last heartbeat response ID received
        /// </summary>
        private double lastReceivedResponseId = 0;
        
        /// <summary>
        /// Connection status enum defining possible connection states
        /// </summary>
        public enum ConnectionStatus
        {
            Disconnected = 0,
            Connecting = 1,
            Connected = 2,
            Degraded = 3
        }
        
        /// <summary>
        /// Current connection status
        /// </summary>
        private ConnectionStatus currentStatus = ConnectionStatus.Disconnected;
        
        /// <summary>
        /// Get current connection status
        /// </summary>
        public ConnectionStatus Status => currentStatus;
        #endregion
        
        /// <summary>
        /// Unity MonoBehaviour lifecycle - Awake
        /// </summary>
        private void Awake()
        {
            // Reset connection and heartbeat state
            currentStatus = ConnectionStatus.Disconnected;
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
            heartbeatTimer = 0f;
        }
        
        /// <summary>
        /// Unity MonoBehaviour lifecycle - OnEnable
        /// </summary>
        private void OnEnable()
        {
            // Start heartbeat monitoring when component is enabled
            if (_heartbeatCoroutine == null && QuestNavConstants.REQUIRE_HEARTBEAT)
            {
                QueuedLogger.Log("[NetworkTableManager] Starting heartbeat coroutine");
                _heartbeatCoroutine = StartCoroutine(HeartbeatMonitoringCoroutine());
            }
        }
        
        /// <summary>
        /// Unity MonoBehaviour lifecycle - OnDisable
        /// </summary>
        private void OnDisable()
        {
            // Stop heartbeat monitoring when component is disabled
            if (_heartbeatCoroutine != null)
            {
                QueuedLogger.Log("[NetworkTableManager] Stopping heartbeat coroutine");
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
        }
        
        /// <summary>
        /// Initialize the NetworkTables manager
        /// </summary>
        public void Initialize()
        {
            teamNumber = PlayerPrefs.GetString("TeamNumber", QuestNavConstants.DEFAULT_TEAM_NUMBER);
            
            // Reset heartbeat counters
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
            heartbeatTimer = 0f;
            
            // Connect to robot
            ConnectToRobot();
        }
        
        /// <summary>
        /// Update team number and reconnect
        /// </summary>
        /// <param name="newTeamNumber">The new team number to use</param>
        public void UpdateTeamNumber(string newTeamNumber)
        {
            QueuedLogger.Log("[NetworkTableManager] Updating Team Number");
            teamNumber = newTeamNumber;
            PlayerPrefs.SetString("TeamNumber", teamNumber);
            PlayerPrefs.Save();
            
            // Clear cached IP and candidate failure data
            ipAddress = "";
            failedCandidates.Clear();
            
            // If a connection coroutine is already running, stop it
            if (_connectionCoroutine != null)
            {
                StopCoroutine(_connectionCoroutine);
                _connectionCoroutine = null;
            }
            
            // Disconnect existing connection, if any
            Disconnect();
            
            // Reset heartbeat tracking
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
            heartbeatTimer = 0f;
            
            // Restart the asynchronous connection process
            _connectionCoroutine = StartCoroutine(ConnectionCoroutineWrapper());
            
            // Update connection status
            UpdateConnectionStatus(ConnectionStatus.Disconnected);
        }
        
        /// <summary>
        /// Disconnects from the current NetworkTables server
        /// </summary>
        public void Disconnect()
        {
            if (nt != null)
            {
                if (nt.Client.Connected())
                {
                    nt.Client.Disconnect();
                }
                nt = null;
            }
            
            // Update connection status
            UpdateConnectionStatus(ConnectionStatus.Disconnected);
        }
        
        /// <summary>
        /// Gets the current team number
        /// </summary>
        public string GetTeamNumber()
        {
            return teamNumber;
        }
        
        /// <summary>
        /// Checks if connected to NetworkTables
        /// </summary>
        /// <returns>True if connected, false otherwise</returns>
        public bool IsConnected()
        {
            return nt != null && nt.Client.Connected();
        }
        
        /// <summary>
        /// Gets the current IP address
        /// </summary>
        public string GetIPAddress()
        {
            return ipAddress;
        }
        
        /// <summary>
        /// Publishes a topic to NetworkTables
        /// </summary>
        public void PublishTopic(string topic, string type)
        {
            if (IsConnected())
            {
                nt.PublishTopic(topic, type);
            }
        }
        
        /// <summary>
        /// Subscribes to a topic in NetworkTables
        /// </summary>
        public void Subscribe(string topic, double period = 0.1, bool topicsOnly = false, bool prefixed = false, bool sendAll = false)
        {
            if (IsConnected())
            {
                nt.Subscribe(topic, period, topicsOnly, prefixed, sendAll);
            }
        }
        
        /// <summary>
        /// Sets a value in NetworkTables
        /// </summary>
        public void PublishValue<T>(string topic, T value)
        {
            if (IsConnected())
            {
                try
                {
                    nt.PublishValue(topic, value);
                }
                catch (Exception ex)
                {
                    QueuedLogger.LogError($"[NetworkTableManager] Error publishing value to {topic}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Gets a value from NetworkTables with generic type support
        /// </summary>
        public T GetValue<T>(string topic)
        {
            if (!IsConnected()) return default(T);
            
            try
            {
                // Handle different types based on T
                if (typeof(T) == typeof(long)) 
                {
                    return (T)(object)nt.GetLong(topic);
                }
                if (typeof(T) == typeof(double))
                {
                    return (T)(object)nt.GetDouble(topic);
                }
                if (typeof(T) == typeof(double[]))
                {
                    return (T)(object)nt.GetDoubleArray(topic);
                }
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)nt.GetString(topic);
                }
                if (typeof(T) == typeof(float[]))
                {
                    // Convert double[] to float[]
                    double[] doubleArray = nt.GetDoubleArray(topic);
                    if (doubleArray == null) return default(T);
                    
                    float[] floatArray = new float[doubleArray.Length];
                    for (int i = 0; i < doubleArray.Length; i++)
                    {
                        floatArray[i] = (float)doubleArray[i];
                    }
                    return (T)(object)floatArray;
                }
                
                QueuedLogger.LogWarning($"[NetworkTableManager] Unsupported type {typeof(T).Name} requested for topic {topic}");
                return default(T);
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[NetworkTableManager] Error getting value from {topic}: {ex.Message}");
                return default(T);
            }
        }
        
        /// <summary>
        /// Initialize the NetworkTables topics
        /// </summary>
        public void PublishTopics()
        {
            if (!IsConnected()) return;
            
            QueuedLogger.Log("[NetworkTableManager] Publishing topics");
            
            // Publish command and telemetry topics
            PublishTopic(QuestNavConstants.Topics.COMMAND_MISO, "int");
            PublishTopic(QuestNavConstants.Topics.TELEMETRY_FRAME_COUNT, "int");
            PublishTopic(QuestNavConstants.Topics.TELEMETRY_TIMESTAMP, "double");
            PublishTopic(QuestNavConstants.Topics.TELEMETRY_POSITION, "float[]");
            PublishTopic(QuestNavConstants.Topics.TELEMETRY_QUATERNION, "float[]");
            PublishTopic(QuestNavConstants.Topics.TELEMETRY_EULER_ANGLES, "float[]");
            PublishTopic(QuestNavConstants.Topics.TELEMETRY_BATTERY_PERCENT, "double");
            PublishTopic(QuestNavConstants.Topics.CONNECTION_STATUS, "int");
            
            // Heartbeat topic only if required
            if (QuestNavConstants.REQUIRE_HEARTBEAT)
            {
                PublishTopic(QuestNavConstants.Topics.HEARTBEAT_REQUEST, "double");
                QueuedLogger.Log("[NetworkTableManager] Heartbeat request topic published");
            }
            
            // Subscribe to command topics
            Subscribe(QuestNavConstants.Topics.COMMAND_MOSI, 0.1, false, false, false);
            Subscribe(QuestNavConstants.Topics.INIT_POSITION, 0.1, false, false, false);
            Subscribe(QuestNavConstants.Topics.INIT_EULER_ANGLES, 0.1, false, false, false);
            Subscribe(QuestNavConstants.Topics.COMMAND_RESETPOSE, 0.1, false, false, false);
            
            // Heartbeat response topic only if required
            if (QuestNavConstants.REQUIRE_HEARTBEAT)
            {
                Subscribe(QuestNavConstants.Topics.HEARTBEAT_RESPONSE, 0.05, false, false, false);
                QueuedLogger.Log("[NetworkTableManager] Subscribed to heartbeat response topic");
            }
            
            // Publish initial connection status
            PublishValue(QuestNavConstants.Topics.CONNECTION_STATUS, (int)currentStatus);
        }
        
        /// <summary>
        /// Called when the app is going into standby mode
        /// </summary>
        public void HandleStandbyEnter()
        {
            QueuedLogger.Log("[NetworkTableManager] Preparing for standby mode");
            
            // Reset heartbeat tracking
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
            
            if (nt != null && nt.Client != null && nt.Client.Connected())
            {
                QueuedLogger.Log("[NetworkTableManager] Disconnecting from NetworkTables while entering standby mode");
                nt.Client.Disconnect();
            }
            else
            {
                QueuedLogger.Log("[NetworkTableManager] Already disconnected from NetworkTables while entering standby mode");
            }
            
            // Update connection status
            UpdateConnectionStatus(ConnectionStatus.Disconnected);
        }
        
        /// <summary>
        /// Called when the app is resuming from standby mode
        /// </summary>
        public void HandleStandbyExit()
        {
            QueuedLogger.Log("[NetworkTableManager] Resuming from standby mode");
            
            // Reset heartbeat tracking to force immediate check
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
            heartbeatTimer = QuestNavConstants.HEARTBEAT_REQUEST_INTERVAL; // Force immediate heartbeat
            
            if (nt == null || nt.Client == null || !nt.Client.Connected())
            {
                QueuedLogger.Log("[NetworkTableManager] Attempting to reconnect to NetworkTables after standby mode");
                ConnectToRobot();
            } 
            else 
            {
                QueuedLogger.LogWarning("[NetworkTableManager] Already connected to NetworkTables after standby mode! This should not be possible!");
                // Flag connection as potentially stale
                UpdateConnectionStatus(ConnectionStatus.Degraded);
                
                // Test the connection is truly alive
                if (!nt.Client.ValidateConnection())
                {
                    QueuedLogger.Log("[NetworkTableManager] WebSocket connection validation failed after standby, reconnecting");
                    Disconnect();
                    ConnectToRobot();
                }
            }
        }
        
        /// <summary>
        /// Updates the connection status and publishes it to NetworkTables
        /// </summary>
        /// <param name="newStatus">New connection status</param>
        private void UpdateConnectionStatus(ConnectionStatus newStatus)
        {
            if (newStatus != currentStatus)
            {
                QueuedLogger.Log($"[NetworkTableManager] Connection status changed: {currentStatus} → {newStatus}");
                currentStatus = newStatus;
                
                // Publish status if we have a connection
                if (IsConnected())
                {
                    PublishValue(QuestNavConstants.Topics.CONNECTION_STATUS, (int)currentStatus);
                }
            }
        }
        
        /// <summary>
        /// Manually sends a heartbeat request - useful for debugging
        /// </summary>
        public void SendHeartbeat()
        {
            if (!IsConnected() || !QuestNavConstants.REQUIRE_HEARTBEAT) return;
            
            lastHeartbeatRequestId++;
            PublishValue(QuestNavConstants.Topics.HEARTBEAT_REQUEST, lastHeartbeatRequestId);
            QueuedLogger.Log($"[NetworkTableManager] Manually sent heartbeat request ID: {lastHeartbeatRequestId}");
        }
        
        /// <summary>
        /// Updates the heartbeat system - can be called directly for more control
        /// </summary>
        public void UpdateHeartbeat()
        {
            if (!IsConnected() || !QuestNavConstants.REQUIRE_HEARTBEAT) return;
            
            // Update heartbeat timer
            heartbeatTimer += Time.deltaTime;
            
            // Time to send a heartbeat request?
            if (heartbeatTimer >= QuestNavConstants.HEARTBEAT_REQUEST_INTERVAL)
            {
                // Generate and send new heartbeat request with unique ID
                lastHeartbeatRequestId++;
                PublishValue(QuestNavConstants.Topics.HEARTBEAT_REQUEST, lastHeartbeatRequestId);
                heartbeatTimer = 0f;
                
                QueuedLogger.Log($"[NetworkTableManager] Sent heartbeat request ID: {lastHeartbeatRequestId}");
                
                // Check for responses to previous heartbeats
                double responseId = GetValue<double>(QuestNavConstants.Topics.HEARTBEAT_RESPONSE);
                
                // If we got a new response that matches what we sent previously
                if (responseId > lastReceivedResponseId && responseId <= lastHeartbeatRequestId)
                {
                    // Got a valid response
                    lastReceivedResponseId = responseId;
                    QueuedLogger.Log($"[NetworkTableManager] Received heartbeat response ID: {responseId}");
                    
                    // If we were in a degraded state but now getting responses, restore connected status
                    if (currentStatus != ConnectionStatus.Connected)
                    {
                        QueuedLogger.Log("[NetworkTableManager] Heartbeat responses resumed. Connection restored.");
                        UpdateConnectionStatus(ConnectionStatus.Connected);
                    }
                }
            }
        }
        
        /// <summary>
        /// Coroutine for monitoring heartbeat
        /// </summary>
        private IEnumerator HeartbeatMonitoringCoroutine()
        {
            QueuedLogger.Log("[NetworkTableManager] Heartbeat monitoring coroutine started");
            
            while (true)
            {
                // Skip heartbeat if not required or if disconnected
                if (!QuestNavConstants.REQUIRE_HEARTBEAT || !IsConnected())
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                
                // Update heartbeat timer
                heartbeatTimer += Time.deltaTime;
                
                // Time to send a heartbeat request?
                if (heartbeatTimer >= QuestNavConstants.HEARTBEAT_REQUEST_INTERVAL)
                {
                    // Generate and send new heartbeat request with unique ID
                    lastHeartbeatRequestId++;
                    PublishValue(QuestNavConstants.Topics.HEARTBEAT_REQUEST, lastHeartbeatRequestId);
                    heartbeatTimer = 0f;
                    
                    QueuedLogger.Log($"[NetworkTableManager] Sent heartbeat request ID: {lastHeartbeatRequestId}");
                    
                    // Check for responses to previous heartbeats
                    double responseId = GetValue<double>(QuestNavConstants.Topics.HEARTBEAT_RESPONSE);
                    
                    // If we got a new response that matches what we sent previously
                    if (responseId > lastReceivedResponseId && responseId <= lastHeartbeatRequestId)
                    {
                        // Got a valid response
                        lastReceivedResponseId = responseId;
                        QueuedLogger.Log($"[NetworkTableManager] Received heartbeat response ID: {responseId}");
                        
                        // If we were in a degraded state but now getting responses, restore connected status
                        if (currentStatus != ConnectionStatus.Connected)
                        {
                            QueuedLogger.Log("[NetworkTableManager] Heartbeat responses resumed. Connection restored.");
                            UpdateConnectionStatus(ConnectionStatus.Connected);
                        }
                    }
                    
                    // If we haven't received any responses yet, but we haven't been trying for long
                    // consider the connection in a connecting state
                    if (lastReceivedResponseId == 0 && lastHeartbeatRequestId < QuestNavConstants.HEARTBEAT_DEGRADED_THRESHOLD)
                    {
                        UpdateConnectionStatus(ConnectionStatus.Connecting);
                    }
                    // If we've sent several heartbeats but received no responses, consider the connection degraded
                    else if (lastReceivedResponseId == 0 && lastHeartbeatRequestId >= 5 && lastHeartbeatRequestId < 15)
                    {
                        UpdateConnectionStatus(ConnectionStatus.Degraded);
                    }
                    // If we've sent many heartbeats but received no responses, consider the connection lost
                    else if (lastReceivedResponseId == 0 && lastHeartbeatRequestId >= 15)
                    {
                        QueuedLogger.Log("[NetworkTableManager] Heartbeat responses missing for too long. Forcing reconnection...");
                        Disconnect();
                        ConnectToRobot();
                    }
                }
                
                yield return null; // Wait until next frame
            }
        }
        
        /// <summary>
        /// Begins the connection process
        /// </summary>
        private void ConnectToRobot()
        {
            _connectionCoroutine = StartCoroutine(ConnectionCoroutineWrapper());
        }
        
        /// <summary>
        /// A coroutine wrapper for asynchronous connection
        /// </summary>
        private IEnumerator ConnectionCoroutineWrapper()
        {
            var connectionTask = AttemptConnectionAsync();
            while (!connectionTask.IsCompleted)
            {
                yield return null;
            }
            
            // If the heartbeat coroutine isn't running and heartbeat is required, start it
            if (IsConnected() && _heartbeatCoroutine == null && QuestNavConstants.REQUIRE_HEARTBEAT)
            {
                QueuedLogger.Log("[NetworkTableManager] Starting heartbeat coroutine after successful connection");
                _heartbeatCoroutine = StartCoroutine(HeartbeatMonitoringCoroutine());
            }
        }
        
        /// <summary>
        /// Asynchronously attempts to connect to one of the candidate addresses
        /// </summary>
        private async Task AttemptConnectionAsync()
        {
            bool connectionEstablished = false;
            
            // Set initial connection status to disconnected
            UpdateConnectionStatus(ConnectionStatus.Disconnected);
            
            // If simulation mode is enabled, only try the simulation IP
            List<string> candidateAddresses = new List<string>();
            if (QuestNavConstants.USE_SIMULATION_MODE)
            {
                candidateAddresses.Add(QuestNavConstants.SIMULATION_IP_ADDRESS);
                QueuedLogger.Log("[NetworkTableManager] Using simulation mode with IP: " + QuestNavConstants.SIMULATION_IP_ADDRESS);
            }
            else
            {
                candidateAddresses.AddRange(new List<string>()
                {
                    GenerateIP(),
                    "172.22.11.2",
                    $"roboRIO-{teamNumber}-FRC.local",
                    $"roboRIO-{teamNumber}-FRC.lan",
                    $"roboRIO-{teamNumber}-FRC.frc-field.local"
                });
            }
            
            while (!connectionEstablished)
            {
                // Check if the network is reachable
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    QueuedLogger.LogWarning($"[NetworkTableManager] Network not reachable. Waiting {QuestNavConstants.UNREACHABLE_NETWORK_DELAY} seconds before reattempting.");
                    await Task.Delay(QuestNavConstants.UNREACHABLE_NETWORK_DELAY * 1000);
                    continue;
                }
                
                StringBuilder cycleLog = new StringBuilder();
                cycleLog.AppendLine("[NetworkTableManager] Connection attempt cycle:");
                
                // Update connection status to connecting
                UpdateConnectionStatus(ConnectionStatus.Connecting);
                
                foreach (string candidate in candidateAddresses)
                {
                    // Skip a candidate if it failed recently
                    if (failedCandidates.ContainsKey(candidate) && 
                        (Time.time - failedCandidates[candidate] < QuestNavConstants.CANDIDATE_FAILURE_COOLDOWN))
                    {
                        cycleLog.AppendLine($"Skipping candidate {candidate} (failed less than {QuestNavConstants.CANDIDATE_FAILURE_COOLDOWN} seconds ago).");
                        continue;
                    }
                    else
                    {
                        failedCandidates.Remove(candidate);
                    }
                    
                    string resolvedAddress = candidate;
                    try
                    {
                        // Skip DNS resolution for simulation IP address and use it directly
                        if (QuestNavConstants.USE_SIMULATION_MODE && candidate == QuestNavConstants.SIMULATION_IP_ADDRESS)
                        {
                            resolvedAddress = candidate;
                            cycleLog.AppendLine($"Using simulation IP directly: {resolvedAddress}");
                        }
                        else
                        {
                            // Use asynchronous DNS resolution for regular IPs
                            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(candidate);
                            IPAddress[] ipv4Addresses = hostEntry.AddressList
                                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                                .ToArray();
                            if (ipv4Addresses.Length > 0)
                            {
                                resolvedAddress = ipv4Addresses[0].ToString();
                            }
                            else
                            {
                                cycleLog.AppendLine($"DNS lookup returned no IPv4 for candidate '{candidate}'.");
                                failedCandidates[candidate] = Time.time;
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        cycleLog.AppendLine($"DNS resolution failed for candidate '{candidate}': {ex.Message}");
                        failedCandidates[candidate] = Time.time;
                        continue;
                    }
                    
                    cycleLog.AppendLine($"Attempting connection to {resolvedAddress}...");
                    
                    try
                    {
                        // Wrap the potentially blocking connection call in Task.Run
                        int port = QuestNavConstants.USE_SIMULATION_MODE ? QuestNavConstants.SIMULATION_PORT : QuestNavConstants.SERVER_PORT;
                        QueuedLogger.Log($"[NetworkTableManager] Attempting connection to {resolvedAddress} on port {port}...");
                        
                        var sink = await Task.Run(() =>
                        {
                            try
                            {
                                QueuedLogger.Log($"[NetworkTableManager] Creating NT4Source with app={QuestNavConstants.APP_NAME}, server={resolvedAddress}, port={port}");
                                return new Nt4Source(QuestNavConstants.APP_NAME, resolvedAddress, port);
                            }
                            catch (Exception innerEx)
                            {
                                QueuedLogger.LogError($"[NetworkTableManager] NT4Source creation error: {innerEx.Message}");
                                if (innerEx.InnerException != null)
                                {
                                    QueuedLogger.LogError($"[NetworkTableManager] Inner exception: {innerEx.InnerException.Message}");
                                }
                                return null;
                            }
                        });
                        
                        if (sink != null && sink.Client != null && sink.Client.Connected())
                        {
                            ipAddress = resolvedAddress; // Cache the working address
                            nt = sink;
                            cycleLog.AppendLine($"Connected successfully to {resolvedAddress}.");
                            connectionEstablished = true;
                            
                            // Reset heartbeat tracking
                            lastHeartbeatRequestId = 0;
                            lastReceivedResponseId = 0;
                            heartbeatTimer = 0f;
                            
                            // Update connection status
                            UpdateConnectionStatus(ConnectionStatus.Connected);
                            
                            break;
                        }
                        else
                        {
                            cycleLog.AppendLine($"Connection attempt to {resolvedAddress} did not succeed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        cycleLog.AppendLine($"Connection attempt failed for {resolvedAddress}: {ex.Message}");
                    }
                }
                
                // Handle a failed connection
                if (!connectionEstablished)
                {
                    cycleLog.AppendLine($"Could not establish a connection with any candidate addresses. Reattempting in {reconnectDelay} second(s)...");
                    QueuedLogger.Log(cycleLog.ToString(), QueuedLogger.LogLevel.Warning);
                    await Task.Delay((int)(reconnectDelay * 1000));
                    reconnectDelay = Mathf.Min(reconnectDelay * 2, QuestNavConstants.MAX_RECONNECT_DELAY);
                }
            }
            
            // Reset delay on success
            reconnectDelay = QuestNavConstants.DEFAULT_RECONNECT_DELAY;
            QueuedLogger.Log("[NetworkTableManager] Connection established. Publishing topics.");
            PublishTopics();
        }
        
        /// <summary>
        /// Generates the IP address for the roboRIO based on team number
        /// </summary>
        private string GenerateIP()
        {
            if (teamNumber.Length >= 1)
            {
                string tePart = teamNumber.Length > 2 ? teamNumber.Substring(0, teamNumber.Length - 2) : "0";
                string amPart = teamNumber.Length > 2 ? teamNumber.Substring(teamNumber.Length - 2) : teamNumber;
                return QuestNavConstants.SERVER_ADDRESS_FORMAT.Replace("TE", tePart).Replace("AM", amPart);
            }
            else
            {
                return QuestNavConstants.NetworkDefaults.FALLBACK_IP;
            }
        }
    }
}