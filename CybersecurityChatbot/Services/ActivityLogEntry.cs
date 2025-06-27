using System;

namespace CybersecurityChatbot.Services
{
    /// <summary>
    /// Simple class to store one activity log entry
    /// </summary>
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }

        public ActivityLogEntry(string actionType, string description, string details = "")
        {
            Timestamp = DateTime.Now;
            ActionType = actionType;
            Description = description;
            Details = details;
        }

        /// <summary>
        /// Simple display format for the activity log
        /// </summary>
        public override string ToString()
        {
            var timeString = Timestamp.ToString("HH:mm:ss");
            if (string.IsNullOrEmpty(Details))
            {
                return $"{timeString} - {Description}";
            }
            else
            {
                return $"{timeString} - {Description} ({Details})";
            }
        }
    }
}