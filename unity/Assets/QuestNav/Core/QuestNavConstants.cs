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
        public const float FIELD_LENGTH = 16.54f;  // FRC field length in meters
        public const float FIELD_WIDTH = 8.02f;    // FRC field width in meters
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
    }
}