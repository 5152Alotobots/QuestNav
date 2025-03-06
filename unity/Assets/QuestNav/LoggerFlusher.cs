using UnityEngine;

public class LoggerFlusher : MonoBehaviour
{
    void Update()
    {
        QueuedLogger.Flush();
    }
}