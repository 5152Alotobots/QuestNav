using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using WebSocketSharp;
using Random = System.Random;

namespace NetworkTables
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

        // Track connection state
        private bool _connectionActive = false;
        private int _messageCounter = 0;
        private int _pingCounter = 0;
        private const int MAX_PING_WITHOUT_RESPONSE = 3;

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
            _connectionActive = false;
            _messageCounter = 0;
            _pingCounter = 0;
        }

        /// <summary>
        /// Connect to the NetworkTables server.
        /// </summary>
        public (bool success, string errorMessage) Connect()
        {
            try
            {
                // Clean up any existing connection first
                if (_ws != null)
                {
                    try
                    {
                        // Disconnect and clean up previous connection
                        if (_ws.ReadyState == WebSocketState.Open || _ws.ReadyState == WebSocketState.Connecting)
                        {
                            _ws.Close();
                        }
                        
                        // Ensure event handlers are removed to prevent memory leaks
                        _ws.OnOpen -= OnOpen;
                        _ws.OnMessage -= OnMessage;
                        _ws.OnError -= OnError;
                        _ws.OnClose -= OnClose;
                        
                        _ws = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[NT4] Error cleaning up existing WebSocket: {ex.Message}");
                    }
                }

                // Reset connection state
                _connectionActive = false;
                _messageCounter = 0;
                _pingCounter = 0;
                _serverTopics.Clear();
                _publishedTopics.Clear();

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
                    
                    // Ensure event handlers are removed
                    _ws.OnOpen -= OnOpen;
                    _ws.OnMessage -= OnMessage;
                    _ws.OnError -= OnError;
                    _ws.OnClose -= OnClose;
                    
                    _ws = null;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NT4] Error during disconnect: {ex.Message}");
                }
            }
            
            // Clear topics and subscriptions
            _publishedTopics.Clear();
            _subscriptions.Clear();
            _serverTopics.Clear();
            
            _connectionActive = false;
            _messageCounter = 0;
            _pingCounter = 0;
        }
        
        /// <summary>
        /// Validate if the connection is still active by sending a timestamp ping
        /// </summary>
        /// <returns>True if the connection appears valid</returns>
        public bool ValidateConnection()
        {
            // Basic connection check
            if (_ws == null || _ws.ReadyState != WebSocketState.Open)
            {
                return false;
            }
            
            // Send a timestamp to verify connection
            try
            {
                WsSendTimestamp();
                _pingCounter++;
                
                // If we've sent several pings without receiving any messages, connection is stale
                if (_pingCounter > MAX_PING_WITHOUT_RESPONSE)
                {
                    Debug.LogWarning("[NT4] Connection appears stale - no responses to pings");
                    _connectionActive = false;
                    return false;
                }
                
                return _connectionActive;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NT4] Connection validation failed: {ex.Message}");
                _connectionActive = false;
                return false;
            }
        }
        
        // Event handlers
        private void OnOpen(object sender, EventArgs e)
        {
            Debug.Log("[NT4] Connected with identity " + _appName);
            _connectionActive = true;
            _messageCounter = 0;
            _pingCounter = 0;
            WsSendTimestamp();
            
            // Call the user-provided event handler
            _onOpen?.Invoke(this, e);
        }

        private void OnMessage(object message, MessageEventArgs args)
        {
            // Update connection status whenever we receive a message
            _connectionActive = true;
            _messageCounter++;
            _pingCounter = 0; // Reset ping counter as we got a message

            if (args.Data != null)
            {
                try
                {
                    // Attempt to decode the message as JSON array
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
            _connectionActive = false;
        }
        
        private void OnClose(object sender, CloseEventArgs e)
        {
            Debug.Log("[NT4] Disconnected: " + e.Reason);
            _connectionActive = false;
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
        /// Checks if the client is connected
        /// </summary>
        /// <returns>True if connected, False otherwise</returns>
        public bool Connected()
        {
            return _ws != null && _ws.ReadyState == WebSocketState.Open && _connectionActive;
        }
    }
}