using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using WebSocketSharp;
using Random = System.Random;

namespace QuestNav.Network.NetworkTables_CSharp
{
    public class Nt4Client
    {
        /// <summary>
        /// Convert a type string to its corresponding integer.
        /// </summary>
        public static readonly Dictionary<string, int> TypeStrIdxLookup = new Dictionary<string, int>
        {
            { "boolean", 0 },
            { "double", 1 },
            { "int", 2 },
            { "string", 3 },
            { "json", 4 },
            { "raw", 5 },
            { "rpc", 5 },
            {"msgpack", 5},
            {"protobuf", 5},
            {"boolean[]", 16},
            {"double[]", 17},
            {"int[]", 18},
            {"float[]", 19},
            {"string[]", 20},
        };
        
        private readonly string _appName;
        private readonly string _serverAddress;
        
        private WebSocket _ws;
        private long? _serverTimeOffsetUs;
        private long _networkLatencyUs;

        // Track the last successful communication time for connection health monitoring
        private float _lastSuccessfulCommunication = 0f;
        private const float COMMUNICATION_TIMEOUT = 3.0f; // 3 seconds without comms = connection issue

        private readonly Dictionary<int, Nt4Subscription> _subscriptions = new Dictionary<int, Nt4Subscription>();
        private readonly Dictionary<string, Nt4Topic> _publishedTopics = new Dictionary<string, Nt4Topic>();
        private readonly Dictionary<string, Nt4Topic> _serverTopics = new Dictionary<string, Nt4Topic>();

        private readonly Action<Nt4Topic, long, object> _onNewTopicData;
        private readonly EventHandler _onOpen;

        /// <summary>
        /// Create a new NetworkTables client without connecting.
        /// </summary>
        /// <param name="appName">The identity to connect with</param>
        /// <param name="serverBaseAddress">The base IP address to connect to</param>
        /// <param name="serverPort">The server port to connect to</param>
        /// <param name="onOpen">On Open event handler</param>
        /// <param name="onNewTopicData">On New Topic Data event handler</param>
        public Nt4Client(string appName, string serverBaseAddress, int serverPort = 5810, EventHandler onOpen = null, Action<Nt4Topic, long, object> onNewTopicData = null)
        {
            _serverAddress = "ws://" + serverBaseAddress + ":" + serverPort + "/nt/" + appName;
            _appName = appName;
            _onNewTopicData = onNewTopicData;
            _onOpen = onOpen;
            _lastSuccessfulCommunication = Time.time; // Initialize with current time
        }

        /// <summary>
        /// Connect to the NetworkTables server.
        /// </summary>
        public (bool success, string errorMessage) Connect()
        {
            try
            {
                // Check if already connected or connecting
                if (_ws != null && (_ws.ReadyState == WebSocketState.Open || _ws.ReadyState == WebSocketState.Connecting))
                {
                    return (true, null); // Already connected or connecting
                }

                // Clean up any existing connection first
                if (_ws != null)
                {
                    try
                    {
                        _ws.Close();
                        _ws = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[NT4] Error closing existing WebSocket: {ex.Message}");
                    }
                }

                // Create a new connection
                _ws = new WebSocket(_serverAddress);
                _ws.OnOpen += OnOpen;
                _ws.OnMessage += OnMessage;
                _ws.OnError += OnError;
                _ws.OnClose += OnClose;

                Debug.Log($"[NT4] Attempting to connect to {_serverAddress}");
                _ws.Connect();
                return (true, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Connection error: {ex.Message}");
                return (false, ex.Message);
            }
        }
        
        /// <summary>
        /// Disconnect from the NetworkTables server.
        /// </summary>
        public void Disconnect()
        {
            if (_ws != null)
            {
                try
                {
                    if (_ws.ReadyState == WebSocketState.Open || _ws.ReadyState == WebSocketState.Connecting)
                    {
                        _ws.Close();
                    }
                    _ws = null;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NT4] Error during disconnect: {ex.Message}");
                }
            }
        }
        
        // Event handlers
        private void OnOpen(object sender, EventArgs e)
        {
            Debug.Log("[NT4] Connected with identity " + _appName);
            _lastSuccessfulCommunication = Time.time; // Update communication time
            WsSendTimestamp();
            
            // Call the user-provided event handler
            _onOpen?.Invoke(this, e);
        }

        private void OnMessage(object message, MessageEventArgs args)
        {
            // Update communication timestamp whenever we receive a message
            _lastSuccessfulCommunication = Time.time;

            if (args.Data != null)
            {
                // Attempt to decode the message as JSON array
                try
                {
                    object[] msg = JsonConvert.DeserializeObject<object[]>(args.Data);
                    if (msg == null)
                    {
                        Debug.LogError("[NT4] Failed to decode JSON message: " + args.Data);
                        return;
                    }
                    // Iterate through the messages
                    foreach (object obj in msg)
                    {
                        String objStr = obj.ToString();
                        // Attempt to decode the message as a JSON object
                        Dictionary<string, object> msgObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(objStr);
                        if (msgObj == null)
                        {
                            Debug.LogError("[NT4] Failed to decode JSON message: " + obj);
                            continue;
                        }
                        // Handle the message
                        HandleJsonMessage(msgObj);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NT4] Error processing JSON message: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    object[] msg = MessagePackSerializer.Deserialize<object[]>(args.RawData);
                    if (msg == null)
                    {
                        Debug.LogError("[NT4] Failed to decode MSGPack message");
                        return;
                    }
                    HandleMsgPackMessage(msg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NT4] Error processing binary message: {ex.Message}");
                }
            }
        }
        
        private void OnError(object sender, ErrorEventArgs e)
        {
            Debug.LogError("[NT4] Error: " + e.Message + " " + e.Exception);
        }
        
        private void OnClose(object sender, CloseEventArgs e)
        {
            Debug.Log("[NT4] Disconnected: " + e.Reason);
        }
        
        /// <summary>
        /// Publish a topic to the server.
        /// </summary>
        /// <param name="key">The topic to publish</param>
        /// <param name="type">The type of topic</param>
        public void PublishTopic(string key, string type)
        {
            try
            {
                Nt4Topic topic = new Nt4Topic(GetNewUid(), key, type, new Dictionary<string, object>());
                if (!Connected() || _publishedTopics.ContainsKey(key)) return;
                _publishedTopics.Add(key, topic);
                WsPublishTopic(topic);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error publishing topic {key}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish a topic to the server.
        /// </summary>
        /// <param name="key">The topic to publish</param>
        /// <param name="type">The type of topic</param>
        /// <param name="properties">Properties of the topic</param>
        public void PublishTopic(string key, string type, Dictionary<string, object> properties)
        {
            try
            {
                Nt4Topic topic = new Nt4Topic(GetNewUid(), key, type, properties);
                if (!Connected() || _publishedTopics.ContainsKey(key)) return;
                _publishedTopics.Add(key, topic);
                WsPublishTopic(topic);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error publishing topic {key} with properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Unpublish a topic from the server.
        /// </summary>
        /// <param name="key">The topic to unpublish</param>
        public void UnpublishTopic(string key)
        {
            try
            {
                if (!Connected()) return;
                if (!_publishedTopics.ContainsKey(key))
                {
                    Debug.LogWarning("[NT4] Attempted to unpublish topic that was not published: " + key);
                    return;
                }
                Nt4Topic topic = _publishedTopics[key];
                _publishedTopics.Remove(key);
                WsUnpublishTopic(topic);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error unpublishing topic {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Publish a value to the server.
        /// </summary>
        /// <param name="key">The topic to publish the value under</param>
        /// <param name="value">The new value to publish</param>
        public void PublishValue(string key, object value)
        {
            try
            {
                long timestamp = GetServerTimeUs() ?? 0;
                if (!Connected()) return;
                if (!_publishedTopics.ContainsKey(key))
                {
                    Debug.LogWarning("[NT4] Attempted to publish value for topic that was not published: " + key);
                    return;
                }
                Nt4Topic topic = _publishedTopics[key];
                WsSendBinary(MessagePackSerializer.Serialize(new[] { topic.Uid, timestamp, TypeStrIdxLookup[topic.Type], value }));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error publishing value for topic {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to a topic from the server.
        /// </summary>
        /// <param name="key">The topic to subscribe to</param>
        /// <param name="periodic">Update rate in seconds</param>
        /// <param name="all">Whether or not to send all values or just the current ones</param>
        /// <param name="topicsOnly">Whether or not to read only topic announcements, not values.</param>
        /// <param name="prefix">Whether or not to include all sub-values</param>
        /// <returns>The ID of the subscription (for unsubscribing), -1 if not connected</returns>
        public int Subscribe(string key, double periodic = 0.1, bool all = false, bool topicsOnly = false, bool prefix = false)
        {
            try
            {
                if(!Connected()) return -1;
                Nt4SubscriptionOptions opts = new Nt4SubscriptionOptions(periodic, all, topicsOnly, prefix);
                Nt4Subscription sub = new Nt4Subscription(GetNewUid(), new[]{key}, opts);
                WsSubscribe(sub);
                _subscriptions.Add(sub.Uid, sub);
                return sub.Uid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error subscribing to topic {key}: {ex.Message}");
                return -1;
            }
        }
        
        /// <summary>
        /// Subscribe to a topic from the server.
        /// </summary>
        /// <param name="key">The topic to subscribe to</param>
        /// <param name="opts">Options for the subscription</param>
        /// <returns>The ID of the subscription (for unsubscribing), -1 if not connected</returns>
        public int Subscribe(string key, Nt4SubscriptionOptions opts)
        {
            try
            {
                if(!Connected()) return -1;
                Nt4Subscription sub = new Nt4Subscription(GetNewUid(), new[]{key}, opts);
                WsSubscribe(sub);
                _subscriptions.Add(sub.Uid, sub);
                return sub.Uid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error subscribing to topic {key} with options: {ex.Message}");
                return -1;
            }
        }
        
        /// <summary>
        /// Subscribe to a topic from the server.
        /// </summary>
        /// <param name="keys">A list of topics to subscribe to</param>
        /// <param name="periodic">Update rate in seconds</param>
        /// <param name="all">Whether or not to send all values or just the current ones</param>
        /// <param name="topicsOnly">Whether or not to read only topic announcements, not values.</param>
        /// <param name="prefix">Whether or not to include all sub-values</param>
        /// <returns>The ID of the subscription (for unsubscribing)</returns>
        public int Subscribe(string[] keys, double periodic = 0.1, bool all = false, bool topicsOnly = false, bool prefix = false)
        {
            try
            {
                if(!Connected()) return -1;
                Nt4SubscriptionOptions opts = new Nt4SubscriptionOptions(periodic, all, topicsOnly, prefix);
                Nt4Subscription sub = new Nt4Subscription(GetNewUid(), keys, opts);
                WsSubscribe(sub);
                _subscriptions.Add(sub.Uid, sub);
                return sub.Uid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error subscribing to multiple topics: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Unsubscribe from a subscription.
        /// </summary>
        /// <param name="uid">The ID of the subscription</param>
        public void Unsubscribe(int uid)
        {
            try
            {
                if(!Connected()) return;
                if (!_subscriptions.ContainsKey(uid))
                {
                    Debug.LogWarning("[NT4] Attempted to unsubscribe from a subscription that does not exist: " + uid);
                    return;
                }
                Nt4Subscription sub = _subscriptions[uid];
                _subscriptions.Remove(uid);
                WsUnsubscribe(sub);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error unsubscribing from {uid}: {ex.Message}");
            }
        }
        
        // ws utility functions
        private void WsSendJson(string method, Dictionary<string, object> paramsObj)
        {
            try
            {
                if (!Connected()) return;
                Dictionary<string, object> msg = new Dictionary<string, object>
                {
                    { "method", method },
                    { "params", paramsObj }
                };
                // convert msg to a json array, not object, containing only msg
                _ws.SendAsync(JsonConvert.SerializeObject(new object[] {msg}), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error sending JSON: {ex.Message}");
            }
        }
        
        void WsSendBinary(byte[] data)
        {
            try
            {
                if (!Connected())
                {
                    Debug.LogWarning("WebSocket is not open. Cannot send data.");
                    return;
                }
                _ws.SendAsync(data, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error sending binary data: {ex.Message}");
            }
        }

        private void WsPublishTopic(Nt4Topic topic)
        {
            WsSendJson("publish", topic.ToPublishObj());
        }
        
        private void WsUnpublishTopic(Nt4Topic topic)
        {
            WsSendJson("unpublish", topic.ToUnpublishObj());
        }

        private void WsSubscribe(Nt4Subscription subscription)
        {
            WsSendJson("subscribe", subscription.ToSubscribeObj());
        }
        
        private void WsUnsubscribe(Nt4Subscription subscription)
        {
            WsSendJson("unsubscribe", subscription.ToUnsubscribeObj());
        }
        
        private void WsSendTimestamp()
        {
            long timestamp = GetClientTimeUs();
            // Send the timestamp (convert using MessagePack) in the format: -1, 0, type, timestamp
            WsSendBinary(MessagePackSerializer.Serialize(new object[] {-1, 0, TypeStrIdxLookup["int"], timestamp}));
        }
        
        private void WsHandleReceiveTimestamp(long serverTimestamp, long clientTimestamp) {
            long rxTime = GetClientTimeUs();

            // Recalculate server/client offset based on round trip time
            long rtt = rxTime - clientTimestamp;
            _networkLatencyUs = rtt / 2L;
            long serverTimeAtRx = serverTimestamp + _networkLatencyUs;
            _serverTimeOffsetUs = serverTimeAtRx - rxTime;

            Debug.Log(
                "[NT4] New server time: " +
                (GetServerTimeUs() / 1000000.0) +
                "s with " +
                (_networkLatencyUs / 1000.0) +
                "ms latency"
            );
        }
        
        // General Utility
        private static long GetClientTimeUs()
        {
            long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            // Convert to us
            return timestamp * 1000;
        }
        
        private long? GetServerTimeUs() {
            if (_serverTimeOffsetUs == null) return null;
            return GetClientTimeUs() + _serverTimeOffsetUs;
        }
        private void HandleJsonMessage(Dictionary<string, object> msg)
        {
            try
            {
                if(!msg.ContainsKey("method") || !msg.ContainsKey("params")) return;
                string method = msg["method"] as string;
                if (method == null) return;
                JObject parameters = msg["params"] as JObject;
                if (parameters == null) return;
                Dictionary<string, object> parametersDict = parameters.ToObject<Dictionary<string, object>>();
                if (method == "announce")
                {
                    Nt4Topic topic = new Nt4Topic(parametersDict);
                    _serverTopics[topic.Name] = topic; // Use indexer to handle duplicates
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error handling JSON message: {ex.Message}");
            }
        }

        private void HandleMsgPackMessage(object[] msg)
        {
            try
            {
                int topicId = Convert.ToInt32(msg[0]);
                long timestampUs = Convert.ToInt64(msg[1]);
                object value = msg[3];
                
                if (topicId >= 0)
                {
                    Nt4Topic topic = null;
                    // Check to see if the topic ID matches any of the server topics
                    foreach (Nt4Topic serverTopic in _serverTopics.Values)
                    {
                        if (serverTopic.Uid == topicId)
                        {
                            topic = serverTopic;
                            break;
                        }
                    }
                    // If the topic is not found, return
                    if (topic == null) return;
                    _onNewTopicData?.Invoke(topic, timestampUs, value);
                } else if (topicId == -1)
                {
                    // Handle receive timestamp
                    WsHandleReceiveTimestamp(timestampUs, Convert.ToInt64(value));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4] Error handling MSGPack message: {ex.Message}");
            }
        }

        private static int GetNewUid()
        {   
            // Return a random int
            return new Random().Next(0, 10000000);
        }

        /// <summary>
        /// Checks if the client is connected and has had recent communication
        /// </summary>
        /// <returns>True if connected and healthy, False otherwise</returns>
        public bool Connected()
        {
            // Check if WebSocket is connected
            bool wsConnected = _ws != null && _ws.ReadyState == WebSocketState.Open;
            
            // Check if we've received any communication recently
            bool commActive = (Time.time - _lastSuccessfulCommunication) < COMMUNICATION_TIMEOUT;
            
            return wsConnected && commActive;
        }
    }
}