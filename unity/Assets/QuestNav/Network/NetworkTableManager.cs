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
        #endregion
        
        /// <summary>
        /// Initialize the NetworkTables manager
        /// </summary>
        public void Initialize()
        {
            teamNumber = PlayerPrefs.GetString("TeamNumber", QuestNavConstants.DEFAULT_TEAM_NUMBER);
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
            
            // Restart the asynchronous connection process
            _connectionCoroutine = StartCoroutine(ConnectionCoroutineWrapper());
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
                nt.PublishValue(topic, value);
            }
        }
        
        /// <summary>
        /// Gets a value from NetworkTables with generic type support
        /// </summary>
        public T GetValue<T>(string topic)
        {
            if (!IsConnected()) return default(T);
            
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
            
            // Subscribe to command topics
            Subscribe(QuestNavConstants.Topics.COMMAND_MOSI, 0.1, false, false, false);
            Subscribe(QuestNavConstants.Topics.INIT_POSITION, 0.1, false, false, false);
            Subscribe(QuestNavConstants.Topics.INIT_EULER_ANGLES, 0.1, false, false, false);
            Subscribe(QuestNavConstants.Topics.COMMAND_RESETPOSE, 0.1, false, false, false);
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
        }
        
        /// <summary>
        /// Asynchronously attempts to connect to one of the candidate addresses
        /// </summary>
        private async Task AttemptConnectionAsync()
        {
            bool connectionEstablished = false;
            
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
                    "10.0.0.113"
                    // GenerateIP(),
                    // "172.22.11.2",
                    // $"roboRIO-{teamNumber}-FRC.local",
                    // $"roboRIO-{teamNumber}-FRC.lan",
                    // $"roboRIO-{teamNumber}-FRC.frc-field.local"
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