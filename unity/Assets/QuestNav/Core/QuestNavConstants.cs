namespace QuestNav.Core
{
    /// <summary>
    /// Constants used throughout the Quest Navigation system
    /// </summary>
    public static class QuestNavConstants
    {
        // Field dimensions
        public const float FIELD_LENGTH = 16.54f;  // FRC field length in meters (X-axis)
        public const float FIELD_WIDTH = 8.02f;    // FRC field width in meters (Y-axis)
    
        // Network related constants
        public const float RECONNECT_DELAY = 0.25f; // Delay between reconnection attempts
        public const int SERVER_PORT = 5810;        // NetworkTables server port
        
        // Heartbeat constants
        public const bool REQUIRE_HEARTBEAT = true;  // Set to false to disable heartbeat checks
        public const float HEARTBEAT_REQUEST_INTERVAL = 0.2f;  // 200ms between heartbeat requests
    
        // Application identity
        public const string APP_NAME = "QuestNav";
        public const string DEFAULT_TEAM = "5152";
    
        // Server address formats
        public const string SERVER_ADDRESS_FORMAT = "10.TE.AM.2";
        public const string SERVER_DNS_FORMAT = "roboRIO-####-FRC.local";
        public const string SIM_SERVER_ADDRESS = "10.0.0.113";  // Simulation server address (localhost)
    
        // Simulation mode flag
        public const bool USE_SIMULATION_MODE = true;  // Set to true to enable simulation mode
    
        // Command codes (Robot → Quest)
        public const int CMD_NONE = 0;
        public const int CMD_HEADING_RESET = 1;
        public const int CMD_POSE_RESET = 2;
        public const int CMD_PING = 3;
        public const int CMD_TRANSFORM_UPDATE = 4;
        public const int CMD_DISCONNECT = 5;
    
        // Response codes (Quest → Robot)
        public const int RESP_NONE = 0;
        public const int RESP_ERROR = -1;             // Generic error
        public const int RESP_DISCONNECT = -2;        // Disconnecting notification
        public const int RESP_TRANSFORM_SUCCESS = 96;
        public const int RESP_PING_RESPONSE = 97;
        public const int RESP_POSE_RESET_SUCCESS = 98;
        public const int RESP_HEADING_RESET_SUCCESS = 99;
        
        // Network and connection constants
        public const float CONNECTION_SLOW_RETRY_DELAY = 1.0f;     // Extended delay for slower reconnection retry
        public const int MAX_QUICK_RECONNECT_ATTEMPTS = 10;        // Number of attempts before slowing down reconnection
        public const int MESSAGE_SEND_DELAY_MS = 100;              // Delay to ensure messages are sent before disconnecting
        public const int SOCKET_CLEANUP_DELAY_MS = 50;             // Delay to allow system resources to be freed

// Heartbeat monitoring constants
        public const int HEARTBEAT_DEGRADED_THRESHOLD = 5;         // Requests without response before degraded state
        public const int HEARTBEAT_DISCONNECT_THRESHOLD = 15;      // Requests without response before disconnection
        
        // NetworkTables paths
        public static class Topics
        {
            private const string BASE_PATH = "/questnav/";
        
            // Published by Quest
            public const string MISO = BASE_PATH + "miso";                 // Response code
            public const string FRAME_COUNT = BASE_PATH + "frameCount";    // Current frame count
            public const string TIMESTAMP = BASE_PATH + "timestamp";       // Current timestamp
            public const string POSITION = BASE_PATH + "position";         // Position vector [x,y,z]
            public const string QUATERNION = BASE_PATH + "quaternion";     // Rotation quaternion [x,y,z,w]
            public const string EULER_ANGLES = BASE_PATH + "eulerAngles";  // Euler angles [x,y,z]
            public const string BATTERY = BASE_PATH + "batteryPercent";    // Battery percentage
            public const string HEARTBEAT_REQUEST = BASE_PATH + "heartbeatReq";  // Quest heartbeat request
            public const string CONNECTION_STATUS = BASE_PATH + "connectionStatus"; // Connection status
        
            // Published by Robot
            public const string MOSI = BASE_PATH + "mosi";                      // Command code
            public const string INIT_POSITION = BASE_PATH + "init/position";    // Initial position
            public const string INIT_EULER = BASE_PATH + "init/eulerAngles";    // Initial rotation
            public const string RESET_POSE = BASE_PATH + "resetpose";           // Reset pose data
            public const string UPDATE_TRANSFORM = BASE_PATH + "updateTransform"; // Transform update
            public const string HEARTBEAT_RESPONSE = BASE_PATH + "heartbeatResp"; // Robot heartbeat response
        }
    
        // Pose reset constants
        public const int MAX_POSE_RESET_RETRIES = 3;      // Maximum reset attempts
        public const float POSE_RESET_RETRY_DELAY = 50f;  // Delay between retries (ms)
        public const float POSITION_ERROR_THRESHOLD = 0.01f; // Position error threshold (m)
    }
}