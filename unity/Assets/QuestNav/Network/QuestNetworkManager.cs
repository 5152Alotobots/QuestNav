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
        private float lastReceiveTime = 0f;
    
        // Team number management
        private string teamNumber;
        private bool useAddress = true;
        private string ipAddress = "";
    
        /// <summary>
        /// Reference to the NetworkTables connection
        /// </summary>
        public Nt4Source NetworkTables => frcDataSink;
    
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
            lastReceiveTime = Time.time;
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
        /// Updates the connection state, should be called from Update or LateUpdate
        /// </summary>
        /// <returns>Whether the connection is active</returns>
        public bool UpdateConnectionState()
        {
            if (frcDataSink != null && frcDataSink.Client != null && frcDataSink.Client.Connected())
            {
                // Reset reconnection state if we're connected
                if (isReconnecting)
                {
                    isReconnecting = false;
                    Debug.Log("[QuestNetworkManager] Connection restored");
                }
            
                // Force reconnection if we haven't received data in a while
                if (Time.time - lastReceiveTime > QuestNavConstants.DATA_TIMEOUT)
                {
                    Debug.Log("[QuestNetworkManager] Connection appears stale. Forcing reconnection...");
                    HandleDisconnectedState();
                    return false;
                }
            
                // Ensure topics are properly published if needed
                if (!topicsPublished)
                {
                    PublishTopics();
                }
            
                return true;
            }
            else
            {
                // Handle reconnection if not connected
                topicsPublished = false;
                HandleDisconnectedState();
                return false;
            }
        }
    
        /// <summary>
        /// Updates the last receive time, should be called whenever data is received
        /// </summary>
        public void UpdateReceiveTime()
        {
            lastReceiveTime = Time.time;
        }
    
        /// <summary>
        /// Establishes connection to the robot using either IP or DNS
        /// </summary>
        private void ConnectToRobot()
        {
            string attemptedAddress = "Unknown";
        
            try
            {
                if (useAddress)
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
            
                Debug.Log($"[QuestNetworkManager] Attempting to connect to the RoboRIO at {ipAddress} (via {attemptedAddress}).");
                frcDataSink = new Nt4Source(QuestNavConstants.APP_NAME, ipAddress, QuestNavConstants.SERVER_PORT);
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
            
                // More thorough cleanup of the old connection
                if (frcDataSink != null && frcDataSink.Client != null)
                {
                    try
                    {
                        frcDataSink.Client.Disconnect();
                        // Force garbage collection to clean up socket resources
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[QuestNetworkManager] Error during connection cleanup: {ex.Message}");
                    }
                
                    // Set to null to ensure complete recreation
                    frcDataSink = null;
                }
            }
        
            // Handle reconnection with fixed short delay
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer >= QuestNavConstants.RECONNECT_DELAY)
            {
                reconnectAttempts++;
                Debug.Log($"[QuestNetworkManager] Reconnection attempt {reconnectAttempts}");
            
                // Toggle between connection methods to increase chances of success
                useAddress = !useAddress;
            
                // Create a fresh client instance
                ConnectToRobot();
            
                reconnectTimer = 0f;
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
            
                // Subscribed by Quest (published by Robot)
                frcDataSink.Subscribe(QuestNavConstants.Topics.MOSI, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.INIT_POSITION, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.INIT_EULER, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.RESET_POSE, 0.1, false, false, false);
                frcDataSink.Subscribe(QuestNavConstants.Topics.UPDATE_TRANSFORM, 0.1, false, false, false);
            
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
        /// Generates the DNS address for the roboRIO based on team number
        /// </summary>
        /// <returns>The formatted DNS address string</returns>
        private string GetDNS()
        {
            return QuestNavConstants.SERVER_DNS_FORMAT.Replace("####", teamNumber);
        }

        /// <summary>
        /// Generates the IP address for the roboRIO based on team number
        /// </summary>
        /// <returns>The formatted IP address string</returns>
        private string GetIP()
        {
            string tePart = teamNumber.Length > 2 ? teamNumber.Substring(0, teamNumber.Length - 2) : "0";
            string amPart = teamNumber.Length > 2 ? teamNumber.Substring(teamNumber.Length - 2) : teamNumber;
            return QuestNavConstants.SERVER_ADDRESS_FORMAT.Replace("TE", tePart).Replace("AM", amPart);
        }
    }
}