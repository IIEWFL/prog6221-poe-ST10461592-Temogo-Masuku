using System;
using System.Collections.Generic;
using System.Linq;

namespace CybersecurityChatbot.Services
{
    /// <summary>
    /// Simple activity log manager for tracking user actions
    /// </summary>
    public class ActivityLogManager
    {
        private readonly List<ActivityLogEntry> _activityLog;
        private const int MaxEntries = 50; // Keep last 50 activities

        public ActivityLogManager()
        {
            _activityLog = new List<ActivityLogEntry>();
            LogAction("System", "Application started");
        }

        /// <summary>
        /// Add a new activity to the log
        /// </summary>
        public void LogAction(string actionType, string description, string details = "")
        {
            try
            {
                var entry = new ActivityLogEntry(actionType, description, details);
                _activityLog.Add(entry);

                // Remove old entries to save memory
                if (_activityLog.Count > MaxEntries)
                {
                    _activityLog.RemoveAt(0);
                }
            }
            catch
            {
                // Ignore errors to prevent crashes
            }
        }

        /// <summary>
        /// Get recent activities for display in chat
        /// </summary>
        public string GetActivityLogSummary(int maxEntries = 10)
        {
            if (_activityLog.Count == 0)
            {
                return "No recent activity to display.";
            }

            // Get the most recent entries
            var recentEntries = _activityLog
                .Skip(Math.Max(0, _activityLog.Count - maxEntries))
                .ToList();

            var summary = "Here's a summary of recent actions:\n\n";

            for (int i = 0; i < recentEntries.Count; i++)
            {
                summary += $"{i + 1}. {recentEntries[i]}\n";
            }

            // Show if there are more entries
            if (_activityLog.Count > maxEntries)
            {
                var moreCount = _activityLog.Count - maxEntries;
                summary += $"\n... and {moreCount} more actions.";
                summary += "\nSay 'show more activity' to see everything.";
            }

            return summary;
        }

        /// <summary>
        /// Get all activities for complete history
        /// </summary>
        public string GetFullActivityLog()
        {
            if (_activityLog.Count == 0)
            {
                return "No activity to display.";
            }

            var summary = "Complete Activity Log:\n\n";

            for (int i = 0; i < _activityLog.Count; i++)
            {
                summary += $"{i + 1}. {_activityLog[i]}\n";
            }

            summary += $"\nTotal Actions: {_activityLog.Count}";
            return summary;
        }

        /// <summary>
        /// Count how many actions of a specific type
        /// </summary>
        public int GetActionCount(string actionType = null)
        {
            if (string.IsNullOrEmpty(actionType))
            {
                return _activityLog.Count;
            }

            return _activityLog.Count(entry =>
                entry.ActionType.Equals(actionType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get simple statistics
        /// </summary>
        public string GetActivityStatistics()
        {
            if (_activityLog.Count == 0)
            {
                return "No activity data available.";
            }

            var taskCount = GetActionCount("Task");
            var quizCount = GetActionCount("Quiz");
            var educationCount = GetActionCount("Education");

            var stats = "Activity Statistics:\n\n";
            stats += $"Total Actions: {_activityLog.Count}\n";
            stats += $"Tasks: {taskCount}\n";
            stats += $"Quizzes: {quizCount}\n";
            stats += $"Learning: {educationCount}\n";

            return stats;
        }

        /// <summary>
        /// Get recent actions for programming use
        /// </summary>
        public List<ActivityLogEntry> GetRecentActions(int count = 5)
        {
            return _activityLog
                .Skip(Math.Max(0, _activityLog.Count - count))
                .ToList();
        }
    }
}