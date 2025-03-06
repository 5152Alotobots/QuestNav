namespace QuestNav.Core
{
    /// <summary>
    /// Global constants for the QuestNav system
    /// </summary>
    public static class QuestNavConstants
    {
        #region NetworkTables Configuration
        /// <summary>
        /// Application name for NetworkTables connection
        /// </summary>
        public const string APP_NAME = "QuestNav";
        
        /// <summary>
        /// Server address format for NetworkTables connection
        /// </summary>
        public const string SERVER_ADDRESS_FORMAT = "10.TE.AM.2";
        
        /// <summary>
        /// NetworkTables server port
        /// </summary>
        public const int SERVER_PORT = 5810;
        
        /// <summary>
        /// Default team number
        /// </summary>
        public const string DEFAULT_TEAM_NUMBER = "9999";
        #endregion
        
        #region NetworkTables Topic Paths
        public static class Topics
        {
            private const string BASE_PATH = "/questnav";
            
            // Command Topics
            public const string COMMAND_MOSI = BASE_PATH + "/mosi";
            public const string COMMAND_MISO = BASE_PATH + "/miso";
            public const string COMMAND_RESETPOSE = BASE_PATH + "/resetpose";
            
            // Telemetry Topics
            public const string TELEMETRY_FRAME_COUNT = BASE_PATH + "/frameCount";
            public const string TELEMETRY_TIMESTAMP = BASE_PATH + "/timestamp";
            public const string TELEMETRY_POSITION = BASE_PATH + "/position";
            public const string TELEMETRY_QUATERNION = BASE_PATH + "/quaternion";
            public const string TELEMETRY_EULER_ANGLES = BASE_PATH + "/eulerAngles";
            public const string TELEMETRY_BATTERY_PERCENT = BASE_PATH + "/batteryPercent";
            
            // Initialization Topics
            public const string INIT_POSITION = BASE_PATH + "/init/position";
            public const string INIT_EULER_ANGLES = BASE_PATH + "/init/eulerAngles";
        }
        #endregion
        
        #region Command IDs
        public static class Commands
        {
            // Command from robot (MOSI)
            public const long HEADING_RESET = 1;
            public const long POSE_RESET = 2;
            public const long PING = 3;
            
            // Response codes (MISO)
            public const long IDLE = 0;
            public const long PING_RESPONSE = 97;
            public const long POSE_RESET_COMPLETE = 98;
            public const long HEADING_RESET_COMPLETE = 99;
        }
        #endregion
        
        #region Field Dimensions
        /// <summary>
        /// Field limit constants
        /// </summary>
        public static class FieldLimits
        {
            /// <summary>
            /// Minimum X coordinate on field (meters)
            /// </summary>
            public const float MIN_FIELD_X = 0f;
            
            /// <summary>
            /// Minimum Y coordinate on field (meters)
            /// </summary>
            public const float MIN_FIELD_Y = 0f;

            public const float FIELD_LENGTH = 16.54f;  // FRC field length in meters
            public const float FIELD_WIDTH = 8.02f;    // FRC field width in meters
        }
        
        #endregion
        
        #region Connection Configuration
        /// <summary>
        /// Default reconnect delay for failed connection attempts
        /// </summary>
        public const float DEFAULT_RECONNECT_DELAY = 3.0f;
        
        /// <summary>
        /// Maximum reconnection delay
        /// </summary>
        public const float MAX_RECONNECT_DELAY = 10.0f;
        
        /// <summary>
        /// Cooldown before retrying failed connection candidates
        /// </summary>
        public const float CANDIDATE_FAILURE_COOLDOWN = 10.0f;
        
        /// <summary>
        /// Delay to wait when network is unreachable
        /// </summary>
        public const int UNREACHABLE_NETWORK_DELAY = 5;

        #endregion

        #region Thresholds and Limits
        /// <summary>
        /// Thresholds for various operations
        /// </summary>
        public static class Thresholds
        {
            /// <summary>
            /// Position error threshold for pose reset (1cm)
            /// </summary>
            public const float POSITION_ERROR_THRESHOLD = 0.01f;
            
            /// <summary>
            /// Maximum retries for command operations
            /// </summary>
            public const int MAX_COMMAND_RETRIES = 3;
            
            /// <summary>
            /// Delay between command retries in milliseconds
            /// </summary>
            public const float COMMAND_RETRY_DELAY_MS = 50f;
            
            /// <summary>
            /// Default display refresh rate for Quest HMD in Hz
            /// </summary>
            public const float DEFAULT_DISPLAY_FREQUENCY = 120.0f;
            
            /// <summary>
            /// Number of frames between UI updates
            /// </summary>
            public const int UI_UPDATE_INTERVAL = 120;
        }

        /// <summary>
        /// Network-related defaults
        /// </summary>
        public static class NetworkDefaults
        {
            /// <summary>
            /// Fallback IP address when team number format is invalid
            /// </summary>
            public const string FALLBACK_IP = "10.51.52.2";
        }
        #endregion
    }
}