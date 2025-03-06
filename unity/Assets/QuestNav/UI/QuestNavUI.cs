using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using QuestNav.Network;
using QuestNav.Telemetry;
using QuestNav.Core;

namespace QuestNav.UI
{
    /// <summary>
    /// Manages UI components and user interactions
    /// </summary>
    public class QuestNavUI : MonoBehaviour
    {
        #region UI References
        /// <summary>
        /// Input field for team number entry
        /// </summary>
        public TMP_InputField teamInput;
        
        /// <summary>
        /// IP address text
        /// </summary>
        public TMP_Text ipAddressText;
        
        /// <summary>
        /// Button to update team number
        /// </summary>
        public Button teamUpdateButton;
        
        /// <summary>
        /// Connection status text
        /// </summary>
        public TMP_Text connectionStatusText;
        #endregion
        
        /// <summary>
        /// Reference to the network manager
        /// </summary>
        private NetworkTableManager networkTableManager;
        
        /// <summary>
        /// Holds the detected local IP address of the HMD
        /// </summary>
        private string myAddressLocal = "0.0.0.0";
        
        /// <summary>
        /// Initialize the UI manager
        /// </summary>
        /// <param name="networkManager">Reference to NetworkTableManager</param>
        public void Initialize(NetworkTableManager networkManager)
        {
            this.networkTableManager = networkManager;
            
            // Setup team number UI
            string savedTeamNumber = networkManager.GetTeamNumber();
            SetInputBox(savedTeamNumber);
            teamInput.Select();
            
            // Setup button listeners
            teamUpdateButton.onClick.AddListener(UpdateTeamNumber);
            teamInput.onSelect.AddListener(OnInputFieldSelected);
            
            // Update the IP address display
            UpdateIPAddressText();
            
            QueuedLogger.Log("[QuestNavUI] Initialized");
        }
        
        /// <summary>
        /// Updates the team number based on user input
        /// </summary>
        public void UpdateTeamNumber()
        {
            QueuedLogger.Log("[QuestNavUI] Updating Team Number");
            string newTeamNumber = teamInput.text;
            
            // Update in the NetworkTableManager will trigger reconnection
            networkTableManager.UpdateTeamNumber(newTeamNumber);
            
            // Update UI to show the new team number
            SetInputBox(newTeamNumber);
        }
        
        /// <summary>
        /// Updates the default IP address shown in the UI with the current HMD IP address
        /// </summary>
        public void UpdateStatusDisplay()
        {
            UpdateIPAddressText();
            UpdateConnectionStatus();
        }
        
        /// <summary>
        /// Updates the IP address display
        /// </summary>
        private void UpdateIPAddressText()
        {
            TextMeshProUGUI ipText = ipAddressText as TextMeshProUGUI;
            
            if (QuestNavConstants.USE_SIMULATION_MODE)
            {
                // Show simulation mode in UI
                ipText.text = "SIM MODE: " + QuestNavConstants.SIMULATION_IP_ADDRESS + ":" + QuestNavConstants.SIMULATION_PORT;
                ipText.color = Color.yellow;
                return;
            }
            
            // Normal mode - show local IP
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    myAddressLocal = ip.ToString();
                    if (myAddressLocal == "127.0.0.1")
                    {
                        ipText.text = "No Adapter Found";
                    }
                    else
                    {
                        ipText.text = myAddressLocal;
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// Updates the connection status display
        /// </summary>
        private void UpdateConnectionStatus()
        {
            if (connectionStatusText != null)
            {
                if (networkTableManager.IsConnected())
                {
                    connectionStatusText.text = "Connected: " + networkTableManager.GetIPAddress();
                    connectionStatusText.color = Color.green;
                }
                else
                {
                    connectionStatusText.text = "Disconnected";
                    connectionStatusText.color = Color.red;
                }
            }
        }
        
        /// <summary>
        /// Updates the input box placeholder text with the current team number
        /// </summary>
        /// <param name="team">The team number to display</param>
        private void SetInputBox(string team)
        {
            teamInput.text = "";
            TextMeshProUGUI placeholderText = teamInput.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                placeholderText.text = "Current: " + team;
            }
            else
            {
                QueuedLogger.LogError("Placeholder is not assigned or not a TextMeshProUGUI component.");
            }
        }
        
        /// <summary>
        /// Event handler for when the input field is selected
        /// </summary>
        /// <param name="text">The current text in the input field</param>
        private void OnInputFieldSelected(string text)
        {
            QueuedLogger.Log("[QuestNavUI] Input Selected");
        }
    }
}