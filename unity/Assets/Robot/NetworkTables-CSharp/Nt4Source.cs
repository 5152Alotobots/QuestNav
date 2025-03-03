using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NetworkTables
{
    public class Nt4Source
    {
        public Nt4Client Client { get; private set; }

        private Dictionary<string, TopicValue<string>> _stringValues = new Dictionary<string, TopicValue<string>>();
        private Dictionary<string, TopicValue<long>> _longValues = new Dictionary<string, TopicValue<long>>();
        private Dictionary<string, TopicValue<double>> _doubleValues = new Dictionary<string, TopicValue<double>>();
        private Dictionary<string, TopicValue<double[]>> _doubleArrayValues = new Dictionary<string, TopicValue<double[]>>();

        private Dictionary<string, string> _queuedPublishes = new Dictionary<string, string>();
        private Dictionary<string, Nt4SubscriptionOptions> _queuedSubscribes = new Dictionary<string, Nt4SubscriptionOptions>();

        // Whether we've successfully connected and processed all queued operations
        private bool _initialConnectionEstablished = false;

        /// <summary>
        /// Create a new NT4Source which automatically creates a client and connects to the server.
        /// </summary>
        public Nt4Source(string appName = "Nt4Unity", string serverAddress = "127.0.0.1", int port = 5810)
        {
            try
            {
                Debug.Log($"[NT4Source] Creating new source with appName={appName}, serverAddress={serverAddress}, port={port}");
                Client = new Nt4Client(appName, serverAddress, port, OnOpen, OnNewTopicData);
                var result = Client.Connect();
                if (!result.success)
                {
                    Debug.LogError($"[NT4Source] Initial connection failed: {result.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error creating NT4Source: {ex.Message}");
                throw; // Re-throw to allow caller to handle
            }
        }
        
        public void PublishTopic(string topic, string type)
        {
            try
            {
                // Always queue publishes to handle reconnection scenarios
                if (!_queuedPublishes.ContainsKey(topic))
                {
                    _queuedPublishes.Add(topic, type);
                }
                else
                {
                    _queuedPublishes[topic] = type;
                }
                
                // If connected, publish immediately
                if (Client != null && Client.Connected())
                {
                    Client.PublishTopic(topic, type);
                    Debug.Log($"[NT4Source] Published topic: {topic} of type: {type}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error publishing topic {topic}: {ex.Message}");
            }
        }
        
        public void PublishValue(string topic, object value)
        {
            try
            {
                if (Client != null && Client.Connected())
                {
                    Client.PublishValue(topic, value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error publishing value for topic {topic}: {ex.Message}");
            }
        }
        
        public void Subscribe(string topic, double period = 0.1, bool all = false, bool topicsOnly = false, bool prefix = false)
        {
            try
            {
                // Always queue subscribes to handle reconnection scenarios
                if (!_queuedSubscribes.ContainsKey(topic))
                {
                    _queuedSubscribes.Add(topic, new Nt4SubscriptionOptions(period, all, topicsOnly, prefix));
                }
                else
                {
                    _queuedSubscribes[topic] = new Nt4SubscriptionOptions(period, all, topicsOnly, prefix);
                }
                
                // If connected, subscribe immediately
                if (Client != null && Client.Connected())
                {
                    Client.Subscribe(topic, period, all, topicsOnly, prefix);
                    Debug.Log($"[NT4Source] Subscribed to topic: {topic}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error subscribing to topic {topic}: {ex.Message}");
            }
        }

        private void OnOpen(object sender, EventArgs e)
        {
            try
            {
                Debug.Log("[NT4Source] Connection opened, processing queued operations...");
                
                // Process all queued publishes
                foreach(string topic in _queuedPublishes.Keys)
                {
                    Debug.Log($"[NT4Source] Publishing queued topic: {topic}");
                    Client.PublishTopic(topic, _queuedPublishes[topic]);
                }
                
                // Process all queued subscribes
                foreach(string topic in _queuedSubscribes.Keys)
                {
                    Debug.Log($"[NT4Source] Subscribing to queued topic: {topic}");
                    Client.Subscribe(topic, _queuedSubscribes[topic]);
                }
                
                _initialConnectionEstablished = true;
                Debug.Log("[NT4Source] Finished processing queued operations");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error in OnOpen handler: {ex.Message}");
            }
        }

        private void OnNewTopicData(Nt4Topic topic, long timestamp, object value)
        {
            try
            {
                switch (topic.Type)
                {
                    case "string":
                        AddString(topic.Name, timestamp, value);
                        break;
                    case "int":
                        AddInteger(topic.Name, timestamp, value);
                        break;
                    case "double":
                        AddDouble(topic.Name, timestamp, value);
                        break;
                    case "double[]":
                        AddDoubleArray(topic.Name, timestamp, value);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error handling topic data for {topic.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the latest string value of a topic
        /// </summary>
        /// <param name="key">The topic to get the value of</param>
        /// <returns>The latest string value of the topic, or empty string if it doesn't exist</returns>
        public string GetString(string key)
        {
            try
            {
                if (_stringValues.ContainsKey(key))
                {
                    return _stringValues[key].GetValue();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error getting string value for {key}: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Get the latest integer value of a topic
        /// </summary>
        /// <param name="key">The topic to get the value of</param>
        /// <returns>The latest integer value of the topic, or 0 if it doesn't exist</returns>
        public long GetLong(string key)
        {
            try
            {
                if (_longValues.ContainsKey(key))
                {
                    return _longValues[key].GetValue();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error getting long value for {key}: {ex.Message}");
            }

            return 0;
        }
        
        /// <summary>
        /// Get the latest double value of a topic
        /// </summary>
        /// <param name="key">The topic to get the value of</param>
        /// <returns>The latest double value of the topic, or 0 if it doesn't exist</returns>
        public double GetDouble(string key)
        {
            try
            {
                if (_doubleValues.ContainsKey(key))
                {
                    return _doubleValues[key].GetValue();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error getting double value for {key}: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Get the latest double array value of a topic
        /// </summary>
        /// <param name="key">The topic to get the value of</param>
        /// <returns>The latest double array of the topic, or empty array if it doesn't exist</returns>
        public double[] GetDoubleArray(string key)
        {
            try
            {
                if (_doubleArrayValues.ContainsKey(key))
                {
                    return _doubleArrayValues[key].GetValue();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error getting double array for {key}: {ex.Message}");
            }

            return new double[0];
        }

        private void AddString(string key, long timestamp, object value)
        {
            try
            {
                if (!_stringValues.ContainsKey(key))
                {
                    _stringValues.Add(key, new TopicValue<string>());
                }
                _stringValues[key].AddValue(timestamp, Convert.ToString(value));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error adding string value for {key}: {ex.Message}");
            }
        }

        private void AddInteger(string key, long timestamp, object value)
        {
            try
            {
                if (!_longValues.ContainsKey(key))
                {
                    _longValues.Add(key, new TopicValue<long>());
                }
                _longValues[key].AddValue(timestamp, Convert.ToInt64(value));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error adding integer value for {key}: {ex.Message}");
            }
        }
        
        private void AddDouble(string key, long timestamp, object value)
        {
            try
            {
                if (!_doubleValues.ContainsKey(key))
                {
                    _doubleValues.Add(key, new TopicValue<double>());
                }
                _doubleValues[key].AddValue(timestamp, Convert.ToDouble(value));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error adding double value for {key}: {ex.Message}");
            }
        }
        
        private void AddDoubleArray(string key, long timestamp, object value)
        {
            try
            {
                if (!_doubleArrayValues.ContainsKey(key))
                {
                    _doubleArrayValues.Add(key, new TopicValue<double[]>());
                }
                
                if (value is object[] arr)
                {
                    _doubleArrayValues[key].AddValue(timestamp, arr.Cast<double>().ToArray());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NT4Source] Error adding double array for {key}: {ex.Message}");
            }
        }
    }
}