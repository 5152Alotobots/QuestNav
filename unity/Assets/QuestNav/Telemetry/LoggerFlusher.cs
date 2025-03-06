using UnityEngine;

namespace QuestNav.Telemetry
{
    /// <summary>
    /// MonoBehaviour that periodically flushes the queued logs
    /// </summary>
    public class LoggerFlusher : MonoBehaviour
    {
        void Update()
        {
            QueuedLogger.Flush();
        }
    }
}