using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using QuestNav.Core;
using QuestNav.Network.NetworkTables_CSharp;
using UnityEngine;

namespace QuestNav.Network
{
    /// <summary>
    /// Handles all network-related functionality for the Quest navigation system
    /// </summary>
    public class QuestNetworkManager
    {
        // Connection state fields
        private Nt4Source frcDataSink = null;
        private bool isReconnecting = false;
        private float reconnectTimer = 0f;
        private int reconnectAttempts = 0;
        private bool topicsPublished = false;
    
        // Team number management
        private string teamNumber;
        private bool useAddress = true;
        private string ipAddress = "";
        
        // Heartbeat tracking
        private float heartbeatTimer = 0f;
        private double lastHeartbeatRequestId = 0;
        private double lastReceivedResponseId = 0;
        
        // Standby recovery tracking
        private bool isRecoveringFromStandby = false;
        
        // Focus loss handling
        private bool hasLostFocus = false;
        private float focusRecoveryAttempts = 0;
        private float focusRecoveryTimer = 0f;
        private const float FOCUS_RECOVERY_INTERVAL = 0.5f; // More frequent checks during focus recovery
        private const int MAX_FOCUS_RECOVERY_ATTEMPTS = 5;
        
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
        
        private ConnectionStatus currentStatus = ConnectionStatus.Disconnected;
        
        /// <summary>
        /// Get current connection status
        /// </summary>
        public ConnectionStatus Status => currentStatus;
        
        /// <summary>
        /// Reference to the NetworkTables connection
        /// </summary>
        public Nt4Source NetworkTables => frcDataSink;
        
        /// <summary>
        /// Whether the connection is considered active
        /// </summary>
        public bool IsConnected => currentStatus == ConnectionStatus.Connected || 
                                  currentStatus == ConnectionStatus.Degraded;
    
        /// <summary>
        /// Disconnects from the robot and cleans up resources
        /// </summary>
        public void Disconnect()
        {
            if (frcDataSink != null && frcDataSink.Client != null)
            {
                try
                {
                    frcDataSink.Client.Disconnect();
                    Debug.Log("[QuestNetworkManager] Disconnected from robot");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[QuestNetworkManager] Error disconnecting: {ex.Message}");
                }
            }
            
            UpdateConnectionStatus(ConnectionStatus.Disconnected);
        }
    
        /// <summary>
        /// Whether the topics have been published
        /// </summary>
        public bool TopicsPublished => topicsPublished;
    
        /// <summary>
        /// Creates a new QuestNetworkManager with the specified team number
        /// </summary>
        /// <param name="teamNumber">FRC team number</param>
        public QuestNetworkManager(string teamNumber)
        {
            this.teamNumber = teamNumber;
            
            if (QuestNavConstants.USE_SIMULATION_MODE)
            {
                Debug.Log("[QuestNetworkManager] Initializing in simulation mode using localhost");
            }
            
            ConnectToRobot();
        }
    
        /// <summary>
        /// Updates the team number and triggers a reconnection
        /// </summary>
        /// <param name="newTeamNumber">New FRC team number</param>
        public void UpdateTeamNumber(string newTeamNumber)
        {
            Debug.Log($"[QuestNetworkManager] Updating team number to {newTeamNumber}");
            teamNumber = newTeamNumber;
            HandleDisconnectedState();
        }
        
        /// <summary>
        /// Updates the connection status and publishes it to NetworkTables
        /// </summary>
        /// <param name="newStatus">New connection status</param>
        private void UpdateConnectionStatus(ConnectionStatus newStatus)
        {
            if (newStatus != currentStatus)
            {
                Debug.Log($"[QuestNetworkManager] Connection status changed: {currentStatus} → {newStatus}");
                currentStatus = newStatus;
                
                // Publish status if we have a connection
                if (frcDataSink != null && frcDataSink.Client != null && frcDataSink.Client.Connected())
                {
                    frcDataSink.PublishValue(QuestNavConstants.Topics.CONNECTION_STATUS, (int)currentStatus);
                }
            }
        }
    
        /// <summary>
        /// Updates the connection state, should be called from Update or LateUpdate
        /// </summary>
        /// <returns>Whether the connection is active</returns>
        public bool UpdateConnectionState()
        {
            // Update heartbeat timer
            heartbeatTimer += Time.deltaTime;
            
            // If we're in focus recovery mode, handle it separately
            if (hasLostFocus)
            {
                return HandleFocusRecovery();
            }
            
            // Check if we have NT connection
            bool ntConnected = frcDataSink != null && frcDataSink.Client != null && frcDataSink.Client.Connected();
            
            if (!ntConnected)
            {
                // Handle reconnection if not connected
                topicsPublished = false;
                UpdateConnectionStatus(ConnectionStatus.Disconnected);
                HandleDisconnectedState();
                return false;
            }
            
            // Ensure topics are properly published if needed
            if (!topicsPublished)
            {
                PublishTopics();
                UpdateConnectionStatus(ConnectionStatus.Connecting);
            }
            
            // If heartbeat system is disabled, consider connection active as long as NT is connected
            if (!QuestNavConstants.REQUIRE_HEARTBEAT)
            {
                if (currentStatus != ConnectionStatus.Connected)
                {
                    Debug.Log("[QuestNetworkManager] Heartbeat system disabled, considering connection active.");
                    UpdateConnectionStatus(ConnectionStatus.Connected);
                }
                return true;
            }
            
            // Time to send a heartbeat request?
            if (heartbeatTimer >= QuestNavConstants.HEARTBEAT_REQUEST_INTERVAL)
            {
                // Generate and send new heartbeat request with unique ID
                lastHeartbeatRequestId++;
                frcDataSink.PublishValue(QuestNavConstants.Topics.HEARTBEAT_REQUEST, lastHeartbeatRequestId);
                heartbeatTimer = 0f;
                
                // Check for responses to previous heartbeats
                double responseId = frcDataSink.GetDouble(QuestNavConstants.Topics.HEARTBEAT_RESPONSE);
                
                // If we got a new response that matches what we sent previously
                if (responseId > lastReceivedResponseId && responseId <= lastHeartbeatRequestId)
                {
                    // Got a valid response
                    lastReceivedResponseId = responseId;
                    
                    // If we were in a degraded state but now getting responses, restore connected status
                    if (currentStatus != ConnectionStatus.Connected)
                    {
                        Debug.Log("[QuestNetworkManager] Heartbeat responses resumed. Connection restored.");
                        UpdateConnectionStatus(ConnectionStatus.Connected);
                    }
                }
                
                // If we haven't received any responses yet, but we haven't been trying for long
                // consider the connection in a connecting state
                if (lastReceivedResponseId == 0 && lastHeartbeatRequestId < 5)
                {
                    UpdateConnectionStatus(ConnectionStatus.Connecting);
                }
                // If we are recovering from standby, be more aggressive with reconnection
                else if (isRecoveringFromStandby && lastReceivedResponseId == 0 && lastHeartbeatRequestId >= 2)
                {
                    Debug.Log("[QuestNetworkManager] No heartbeat responses after standby. Forcing reconnection...");
                    isRecoveringFromStandby = false;  // Reset recovery flag
                    HandleDisconnectedState();
                    return false;
                }
                // If we've sent several heartbeats but received no responses, consider the connection degraded
                else if (lastReceivedResponseId == 0 && lastHeartbeatRequestId >= 5 && lastHeartbeatRequestId < 15)
                {
                    UpdateConnectionStatus(ConnectionStatus.Degraded);
                }
                // If we've sent many heartbeats but received no responses, consider the connection lost
                else if (lastReceivedResponseId == 0 && lastHeartbeatRequestId >= 15)
                {
                    Debug.Log("[QuestNetworkManager] Heartbeat responses missing for too long. Forcing reconnection...");
                    HandleDisconnectedState();
                    return false;
                }
            }
            
            // Connection is active if WebSocket is open and connected
            return ntConnected;
        }
    
        /// <summary>
        /// Updates the last receive time, called whenever data is received
        /// </summary>
        public void UpdateReceiveTime()
        {
            // No longer using time-based monitoring, this is a no-op now
        }
        
        /// <summary>
        /// Called when the app is going into standby mode
        /// </summary>
        public void HandleStandbyEnter()
        {
            Debug.Log("[QuestNetworkManager] Preparing for standby mode");
            // Mark that heartbeat monitoring needs to be reset on resume
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
        }

        /// <summary>
        /// Called when the app is resuming from standby mode
        /// </summary>
        public void HandleStandbyExit()
        {
            Debug.Log("[QuestNetworkManager] Recovering from standby mode");
            
            // Mark that we're in recovery mode
            isRecoveringFromStandby = true;
            
            // Reset heartbeat tracking to force immediate check
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
            heartbeatTimer = QuestNavConstants.HEARTBEAT_REQUEST_INTERVAL; // Force immediate heartbeat
            
            // Enter focus recovery mode with more aggressive connection checking
            EnterFocusRecoveryMode();
            
            // Flag connection as potentially stale
            UpdateConnectionStatus(ConnectionStatus.Degraded);
            
            // Force WebSocket state check - helps detect stale connections faster
            if (frcDataSink != null && frcDataSink.Client != null)
            {
                if (!frcDataSink.Client.Connected())
                {
                    Debug.Log("[QuestNetworkManager] WebSocket connection stale after standby, reconnecting immediately");
                    HandleDisconnectedState();
                }
                else
                {
                    // Test the connection is truly alive with a ping
                    if (!frcDataSink.Client.ValidateConnection())
                    {
                        Debug.Log("[QuestNetworkManager] WebSocket connection validation failed after standby, reconnecting");
                        HandleDisconnectedState();
                    }
                }
            }
        }
        
        /// <summary>
        /// Enters focus recovery mode with accelerated connection checks
        /// </summary>
        private void EnterFocusRecoveryMode()
        {
            Debug.Log("[QuestNetworkManager] Entering focus recovery mode");
            hasLostFocus = true;
            focusRecoveryAttempts = 0;
            focusRecoveryTimer = 0f;
        }
        
        /// <summary>
        /// Handles more aggressive connection recovery after focus loss
        /// </summary>
        private bool HandleFocusRecovery()
        {
            // Update recovery timer
            focusRecoveryTimer += Time.deltaTime;
            
            // Check connection more frequently during focus recovery
            if (focusRecoveryTimer >= FOCUS_RECOVERY_INTERVAL)
            {
                focusRecoveryTimer = 0f;
                focusRecoveryAttempts++;
                
                Debug.Log($"[QuestNetworkManager] Focus recovery attempt {focusRecoveryAttempts}/{MAX_FOCUS_RECOVERY_ATTEMPTS}");
                
                // Check if we have a valid connection
                bool ntConnected = frcDataSink != null && 
                                  frcDataSink.Client != null && 
                                  frcDataSink.Client.Connected() && 
                                  frcDataSink.Client.ValidateConnection();
                
                if (ntConnected)
                {
                    // Validate connection with a ping
                    if (frcDataSink.Client.ValidateConnection())
                    {
                        Debug.Log("[QuestNetworkManager] Focus recovery successful, connection validated");
                        hasLostFocus = false;
                        isRecoveringFromStandby = false;
                        
                        // Ensure topics are published
                        if (!topicsPublished)
                        {
                            PublishTopics();
                        }
                        
                        UpdateConnectionStatus(ConnectionStatus.Connected);
                        return true;
                    }
                }
                
                // Force a reconnection if too many attempts have failed
                if (focusRecoveryAttempts >= MAX_FOCUS_RECOVERY_ATTEMPTS)
                {
                    Debug.Log("[QuestNetworkManager] Max focus recovery attempts reached, forcing reconnection");
                    hasLostFocus = false;
                    ForceReconnect();
                    return false;
                }
                
                // Check if we need to force a reconnection
                if (!ntConnected && focusRecoveryAttempts > 2)
                {
                    Debug.Log("[QuestNetworkManager] Focus recovery detected connection issue, initiating reconnection");
                    HandleDisconnectedState();
                }
            }
            
            // Continue treating connection as degraded during recovery
            return false;
        }
    
        /// <summary>
        /// Establishes connection to the robot using either IP or DNS
        /// </summary>
        private void ConnectToRobot()
        {
            string attemptedAddress = "Unknown";
        
            try
            {
                // In simulation mode, always use localhost
                if (QuestNavConstants.USE_SIMULATION_MODE)
                {
                    ipAddress = QuestNavConstants.SIM_SERVER_ADDRESS;
                    attemptedAddress = ipAddress;
                }
                else if (useAddress)
                {
                    ipAddress = GetIP();
                    attemptedAddress = ipAddress;
                    useAddress = false;
                }
                else
                {
                    try
                    {
                        string dnsName = GetDNS();
                        attemptedAddress = dnsName;
                        IPHostEntry hostEntry = Dns.GetHostEntry(dnsName);
                        IPAddress[] ipv4Addresses = hostEntry.AddressList
                            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                            .ToArray();
                       
                        if (ipv4Addresses.Length > 0)
                        {
                            ipAddress = ipv4Addresses[0].ToString();
                        }
                        else
                        {
                            Debug.LogWarning($"[QuestNetworkManager] No IPv4 addresses found for {dnsName}, falling back to IP-based connection");
                            ipAddress = GetIP();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[QuestNetworkManager] Error resolving DNS name: {ex.Message}, falling back to IP-based connection");
                        ipAddress = GetIP();
                    }

                    useAddress = true;
                }
            
                string connectionTarget = QuestNavConstants.USE_SIMULATION_MODE ? "simulation" : "RoboRIO";
                Debug.Log($"[QuestNetworkManager] Attempting to connect to the {connectionTarget} at {ipAddress} (via {attemptedAddress}).");
                frcDataSink = new Nt4Source(QuestNavConstants.APP_NAME, ipAddress, QuestNavConstants.SERVER_PORT);
                
                // We'll rely on our heartbeat-based detection since direct connection events 
                // aren't available in this NetworkTables client implementation
                if (frcDataSink != null && frcDataSink.Client != null)
                {
                    Debug.Log("[QuestNetworkManager] NetworkTables client initialized, starting heartbeat monitoring");
                    // The actual connection monitoring happens in UpdateConnectionState()
                }
                
                PublishTopics();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestNetworkManager] Failed to connect to {attemptedAddress}: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles rapid reconnection strategy when connection is lost
        /// </summary>
        private void HandleDisconnectedState()
        {
            if (!isReconnecting)
            {
                Debug.Log("[QuestNetworkManager] Robot disconnected. Starting rapid reconnection process...");
                isReconnecting = true;
                reconnectTimer = 0f;
                reconnectAttempts = 0;
                lastHeartbeatRequestId = 0;
                lastReceivedResponseId = 0;
            
                // More thorough cleanup of the old connection
                if (frcDataSink != null && frcDataSink.Client != null)
                {
                    try
                    {
                        frcDataSink.Client.Disconnect();
                        // Force garbage collection to clean up socket resources
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        
                        // Wait a small amount to allow system resources to be freed
                        System.Threading.Thread.Sleep(50);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[QuestNetworkManager] Error during connection cleanup: {ex.Message}");
                    }
                
                    // Set to null to ensure complete recreation
                    frcDataSink = null;
                }
                
                UpdateConnectionStatus(ConnectionStatus.Disconnected);
            }
        
            // Handle reconnection with fixed short delay
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer >= QuestNavConstants.RECONNECT_DELAY)
            {
                reconnectAttempts++;
                Debug.Log($"[QuestNetworkManager] Reconnection attempt {reconnectAttempts}");
            
                // Toggle between connection methods to increase chances of success
                // But only if not in simulation mode
                if (!QuestNavConstants.USE_SIMULATION_MODE)
                {
                    useAddress = !useAddress;
                }
            
                // Create a fresh client instance
                ConnectToRobot();
            
                reconnectTimer = 0f;
                
                // If we've tried too many times, slow down to avoid overwhelming the network
                if (reconnectAttempts > 10 && !isRecoveringFromStandby && !hasLostFocus) 
                {
                    reconnectTimer = -1.0f;  // Add a longer delay
                }
                
                // After some reconnection attempts, if we're still in focus recovery mode, exit it
                if (hasLostFocus && reconnectAttempts >= MAX_FOCUS_RECOVERY_ATTEMPTS)
                {
                    Debug.Log("[QuestNetworkManager] Exiting focus recovery mode after multiple reconnection attempts");
                    hasLostFocus = false;
                }
            }
        }

        /// <summary>
        /// Publishes and subscribes to required NetworkTables topics
        /// </summary>
        private void PublishTopics()
        {
            try
            {
                if (frcDataSink == null) return;
            
                // Published by Quest
                frcDataSink.PublishTopic(QuestNavConstants.Topics.MISO, "int");
                frcDataSink.PublishTopic(QuestNavConstants.Topics.FRAME_COUNT, "int");
                frcDataSink.PublishTopic(QuestNavConstants.Topics.TIMESTAMP, "double");
                frcDataSink.PublishTopic(QuestNavConstants.Topics.POSITION, "float[]");
                frcDataSink.PublishTopic(QuestNavConstants.Topics.QUATERNION, "float[]");
                frcDataSink.PublishTopic(QuestNavConstants.Topics.EULER_ANGLES, "float[]");
                frcDataSink.PublishTopic(QuestNavConstants.Topics.BATTERY, "double");
                frcDataSink.PublishTopic(QuestNavConstants.Topics.CONNECTION_STATUS, "int");
                
                // Heartbeat topic only if required
                if (QuestNavConstants.REQUIRE_HEARTBEAT)
                {
                    frcDataSink.PublishTopic(QuestNavConstants.Topics.HEARTBEAT_REQUEST, "double");
                }
            
                // Subscribed by Quest (published by Robot)
                frcDataSink.Subscribe(QuestNavConstants.Topics.MOSI, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.INIT_POSITION, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.INIT_EULER, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.RESET_POSE, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.UPDATE_TRANSFORM, 0.1, false, false, false);
                
                // Heartbeat response topic only if required
                if (QuestNavConstants.REQUIRE_HEARTBEAT)
                {
                    frcDataSink.Subscribe(QuestNavConstants.Topics.HEARTBEAT_RESPONSE, 0.05, false, false, false);
                }
            
                topicsPublished = true;
                Debug.Log("[QuestNetworkManager] Topics published successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestNetworkManager] Error publishing topics: {ex.Message}");
                topicsPublished = false;
            }
        }
        
        /// <summary>
        /// Forces a reconnection attempt regardless of current state
        /// </summary>
        public void ForceReconnect()
        {
            Debug.Log("[QuestNetworkManager] Forcing reconnection...");
            
            // Disconnect and clean up resources
            Disconnect();
            
            // Reset connection state
            isReconnecting = false;
            topicsPublished = false;
            lastHeartbeatRequestId = 0;
            lastReceivedResponseId = 0;
            
            // Reset focus recovery state
            hasLostFocus = false;
            focusRecoveryAttempts = 0;
            isRecoveringFromStandby = false;
            
            // Immediate reconnection attempt
            reconnectTimer = QuestNavConstants.RECONNECT_DELAY;
            reconnectAttempts = 0;
            
            // Trigger reconnection process
            HandleDisconnectedState();
        }
    
        /// <summary>
        /// Generates the DNS address for the roboRIO based on team number
        /// </summary>
        /// <returns>The formatted DNS address string</returns>
        private string GetDNS()
        {
            return QuestNavConstants.SERVER_DNS_FORMAT.Replace("####", teamNumber);
        }
        
        /// <summary>
        /// Performs a validation of the current connection
        /// </summary>
        /// <returns>True if connection is considered valid</returns>
        public bool ValidateConnection()
        {
            if (frcDataSink == null || frcDataSink.Client == null)
            {
                return false;
            }
            
            return frcDataSink.Client.ValidateConnection();
        }

        /// <summary>
        /// Generates the IP address for the roboRIO based on team number
        /// </summary>
        /// <returns>The formatted IP address string</returns>
        private string GetIP()
        {
            // For simulation mode, return localhost
            if (QuestNavConstants.USE_SIMULATION_MODE)
            {
                return QuestNavConstants.SIM_SERVER_ADDRESS;
            }
            
            // Otherwise, format the IP based on team number
            string tePart = teamNumber.Length > 2 ? teamNumber.Substring(0, teamNumber.Length - 2) : "0";
            string amPart = teamNumber.Length > 2 ? teamNumber.Substring(teamNumber.Length - 2) : teamNumber;
            return QuestNavConstants.SERVER_ADDRESS_FORMAT.Replace("TE", tePart).Replace("AM", amPart);
        }
        
        /// <summary>
        /// Gets if we're currently in focus recovery mode
        /// </summary>
        public bool IsInFocusRecovery => hasLostFocus;
    }
}