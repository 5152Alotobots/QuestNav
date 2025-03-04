using System;
using QuestNav.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuestNav.UI
{
    /// <summary>
    /// Handles all UI-related functionality for the Quest navigation app
    /// </summary>
    public class QuestUIManager : MonoBehaviour
    {
        /// <summary>
        /// Input field for team number entry
        /// </summary>
        [SerializeField]
        private TMP_InputField teamNumberInput;

        /// <summary>
        /// Button to update team number
        /// </summary>
        [SerializeField]
        private Button updateTeamButton;

        /// <summary>
        /// Event fired when team number is updated
        /// </summary>
        public event Action<string> OnTeamNumberUpdated;

        /// <summary>
        /// Current team number 
        /// </summary>
        private string teamNumber;

        /// <summary>
        /// Initialize the UI components
        /// </summary>
        public void Initialize()
        {
            // Load saved team number or use default
            teamNumber = PlayerPrefs.GetString("TeamNumber", QuestNavConstants.DEFAULT_TEAM);
            SetInputBoxPlaceholder(teamNumber);
        
            // Set up event listeners
            updateTeamButton.onClick.AddListener(HandleTeamNumberUpdate);
            teamNumberInput.onSelect.AddListener(OnInputFieldSelected);
        
            // Focus the input field
            teamNumberInput.Select();
        }

        /// <summary>
        /// Updates the team number based on user input
        /// </summary>
        private void HandleTeamNumberUpdate()
        {
            Debug.Log("[QuestUIManager] Updating Team Number");
        
            // Only update if the input is not empty
            if (!string.IsNullOrEmpty(teamNumberInput.text))
            {
                teamNumber = teamNumberInput.text;
            
                // Save to player prefs for persistence
                PlayerPrefs.SetString("TeamNumber", teamNumber);
                PlayerPrefs.Save();
            
                // Update UI
                SetInputBoxPlaceholder(teamNumber);
            
                // Notify listeners
                OnTeamNumberUpdated?.Invoke(teamNumber);
            }
        }

        /// <summary>
        /// Sets the input box placeholder text with the current team number
        /// </summary>
        private void SetInputBoxPlaceholder(string team)
        {
            teamNumberInput.text = "";
            TextMeshProUGUI placeholderText = teamNumberInput.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                placeholderText.text = "Current: " + team;
            }
            else
            {
                Debug.LogError("[QuestUIManager] Placeholder is not assigned or not a TextMeshProUGUI component.");
            }
        }

        /// <summary>
        /// Event handler for when the input field is selected
        /// </summary>
        private void OnInputFieldSelected(string text)
        {
            Debug.Log("[QuestUIManager] Input Field Selected");
        }
    
        /// <summary>
        /// Gets the current team number
        /// </summary>
        public string GetTeamNumber()
        {
            return teamNumber;
        }
    }
}