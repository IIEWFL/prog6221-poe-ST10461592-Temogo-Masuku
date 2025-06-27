// File: Services/EnhancedNLPManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CybersecurityChatbot.Services;
using static CybersecurityChatbot.Services.IntentRecognizer;

namespace CybersecurityChatbot.Services
{
    /// <summary>
    /// Simple NLP Manager with activity logging for college students
    /// Handles user conversations and tracks what users do
    /// </summary>
    public class EnhancedNLPManager
    {
        private readonly IntentRecognizer _intentRecognizer;
        private readonly MockTaskIntegration _taskIntegration;
        private readonly MockQuizIntegration _quizIntegration;

        // Activity log manager - accessed by MainWindow using reflection
        internal readonly ActivityLogManager _activityLogManager;

        // Simple conversation tracking
        private UserIntent? _lastIntent;
        private Dictionary<string, string> _conversationContext;

        public EnhancedNLPManager(MockTaskIntegration taskIntegration, MockQuizIntegration quizIntegration)
        {
            _intentRecognizer = new IntentRecognizer();
            _taskIntegration = taskIntegration;
            _quizIntegration = quizIntegration;
            _conversationContext = new Dictionary<string, string>();

            // Initialize activity logging
            _activityLogManager = new ActivityLogManager();
            _activityLogManager.LogAction("System", "NLP Manager started");
        }

        /// <summary>
        /// Main method to process what the user types
        /// </summary>
        public string ProcessUserInput(string userInput)
        {
            try
            {
                // Check if user input is empty
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    _activityLogManager.LogAction("User Input", "Empty message received");
                    return "I didn't catch that. Could you please try again?";
                }

                // Log what the user said (keep it short for privacy)
                var shortInput = userInput.Length > 50 ? userInput.Substring(0, 47) + "..." : userInput;
                _activityLogManager.LogAction("User Input", $"User said: '{shortInput}'");

                // Check if they want to see the activity log
                if (IsActivityLogRequest(userInput))
                {
                    return HandleActivityLogRequest(userInput);
                }

                // Figure out what the user wants to do
                var intentResult = _intentRecognizer.RecognizeIntent(userInput);
                _activityLogManager.LogAction("NLP", $"Recognized intent: {intentResult.Intent}");

                // If we're not sure what they want, try to help anyway
                if (intentResult.Confidence < 0.3)
                {
                    _activityLogManager.LogAction("NLP", "Low confidence - providing help");
                    return HandleUnclearInput(userInput);
                }

                // Process what we think they want
                var response = ProcessUserIntent(userInput, intentResult);
                _activityLogManager.LogAction("NLP", "Response provided to user");

                return response;
            }
            catch (Exception ex)
            {
                _activityLogManager.LogAction("Error", "Something went wrong", ex.Message);
                return "I had trouble understanding that. Could you try asking in a different way?";
            }
        }

        /// <summary>
        /// Check if user wants to see the activity log
        /// </summary>
        private bool IsActivityLogRequest(string userInput)
        {
            var input = userInput.ToLower();
            return input.Contains("show activity") ||
                   input.Contains("activity log") ||
                   input.Contains("what have you done") ||
                   input.Contains("show log");
        }

        /// <summary>
        /// Handle activity log requests
        /// </summary>
        private string HandleActivityLogRequest(string userInput)
        {
            var input = userInput.ToLower();
            _activityLogManager.LogAction("Activity Log", "User requested activity log");

            if (input.Contains("more") || input.Contains("all") || input.Contains("full"))
            {
                return _activityLogManager.GetFullActivityLog();
            }
            else if (input.Contains("stats"))
            {
                return _activityLogManager.GetActivityStatistics();
            }
            else
            {
                return _activityLogManager.GetActivityLogSummary();
            }
        }

        /// <summary>
        /// Process different types of user requests
        /// </summary>
        private string ProcessUserIntent(string userInput, IntentResult intentResult)
        {
            switch (intentResult.Intent)
            {
                case UserIntent.CreateTask:
                    return HandleTaskCreation(userInput, intentResult);
                case UserIntent.ViewTasks:
                    return HandleViewTasks();
                case UserIntent.StartQuiz:
                    return HandleStartQuiz(userInput, intentResult);
                case UserIntent.GetCybersecurityAdvice:
                    return HandleSecurityAdvice(userInput, intentResult);
                case UserIntent.Greeting:
                    return HandleGreeting();
                case UserIntent.GetHelp:
                    return HandleHelp();
                default:
                    return HandleOtherRequests(userInput);
            }
        }

        /// <summary>
        /// Help users create tasks
        /// </summary>
        private string HandleTaskCreation(string userInput, IntentResult intentResult)
        {
            var topic = intentResult.GetEntity("security_topic");
            var timeframe = intentResult.GetEntity("timeframe");

            _activityLogManager.LogAction("Task", "Task creation requested",
                $"Topic: {topic}, Time: {timeframe}");

            // Let the existing task system handle it
            var response = _taskIntegration.ProcessTaskCommand(userInput);
            if (!string.IsNullOrEmpty(response))
                return response;

            return "I'll help you create a security task! What would you like to be reminded about?";
        }

        /// <summary>
        /// Show user their tasks
        /// </summary>
        private string HandleViewTasks()
        {
            _activityLogManager.LogAction("Task", "User wants to view tasks");
            return "Let me show you your tasks! Switch to the Task Manager tab to see all your security tasks.";
        }

        /// <summary>
        /// Help users start the quiz
        /// </summary>
        private string HandleStartQuiz(string userInput, IntentResult intentResult)
        {
            var topic = intentResult.GetEntity("security_topic");
            _activityLogManager.LogAction("Quiz", "Quiz requested", $"Topic: {topic}");

            var response = _quizIntegration.ProcessQuizCommand(userInput);
            if (!string.IsNullOrEmpty(response))
                return response;

            return "Ready to test your cybersecurity knowledge? Switch to the Quiz tab to start!";
        }

        /// <summary>
        /// Provide security advice
        /// </summary>
        private string HandleSecurityAdvice(string userInput, IntentResult intentResult)
        {
            var topic = intentResult.GetEntity("security_topic");
            _activityLogManager.LogAction("Education", "Security advice requested", $"Topic: {topic}");

            return GetSecurityAdvice(topic, userInput);
        }

        /// <summary>
        /// Respond to greetings
        /// </summary>
        private string HandleGreeting()
        {
            _activityLogManager.LogAction("Interaction", "User said hello");

            var greetings = new[]
            {
                "Hello! I'm here to help you learn about cybersecurity. What would you like to know?",
                "Hi there! Ready to improve your online security? I can help with tasks, quizzes, and advice.",
                "Hey! Great to see you're thinking about cybersecurity. How can I help you today?"
            };

            var random = new Random();
            var response = greetings[random.Next(greetings.Length)];

            response += "\n\nTry saying things like:\n";
            response += "• 'Help me create a task'\n";
            response += "• 'Start a quiz'\n";
            response += "• 'What is phishing?'\n";
            response += "• 'Show activity log'";

            return response;
        }

        /// <summary>
        /// Provide help information
        /// </summary>
        private string HandleHelp()
        {
            _activityLogManager.LogAction("Help", "User requested help");

            return "I can help you with cybersecurity! Here's what I can do:\n\n" +
                   "Learning:\n" +
                   "• Answer questions about security topics\n" +
                   "• Explain concepts like passwords and phishing\n\n" +
                   "Tasks:\n" +
                   "• Help you create security reminders\n" +
                   "• Manage your cybersecurity to-do list\n\n" +
                   "Quizzes:\n" +
                   "• Test your security knowledge\n" +
                   "• Learn through interactive questions\n\n" +
                   "Activity Tracking:\n" +
                   "• 'Show activity log' to see what we've done\n\n" +
                   "Just tell me what you need help with!";
        }

        /// <summary>
        /// Handle other types of requests
        /// </summary>
        private string HandleOtherRequests(string userInput)
        {
            _activityLogManager.LogAction("NLP", "Other request processed", userInput);
            return "I'm here to help with cybersecurity! Try asking about passwords, phishing, creating tasks, or taking the quiz.";
        }

        /// <summary>
        /// Help when we're not sure what the user wants
        /// </summary>
        private string HandleUnclearInput(string userInput)
        {
            var suggestions = new List<string>();

            // Look for keywords to give helpful suggestions
            if (userInput.ToLower().Contains("task"))
                suggestions.Add("• Say 'help me create a task' to add security reminders");

            if (userInput.ToLower().Contains("quiz"))
                suggestions.Add("• Say 'start quiz' to test your knowledge");

            if (userInput.ToLower().Contains("password") || userInput.ToLower().Contains("phishing"))
                suggestions.Add("• Ask 'What is [topic]?' to learn about security");

            _activityLogManager.LogAction("Help", "Provided suggestions for unclear input");

            var response = "I'm not sure exactly what you're looking for, but I'm here to help!\n\n";

            if (suggestions.Any())
            {
                response += "Based on what you said, you might want to:\n" + string.Join("\n", suggestions) + "\n\n";
            }

            response += "Here are some things you can try:\n" +
                       "• 'What is phishing?' - Learn about security topics\n" +
                       "• 'Help me create a task' - Set up security reminders\n" +
                       "• 'Start quiz' - Test your knowledge\n" +
                       "• 'Show activity log' - See what we've been doing\n\n" +
                       "What would you like to do?";

            return response;
        }

        /// <summary>
        /// Provide simple security advice based on topic
        /// </summary>
        private string GetSecurityAdvice(string topic, string userInput)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return "I'd be happy to help with cybersecurity advice! What specific topic are you interested in? For example: passwords, phishing, malware, or VPNs.";
            }

            switch (topic.ToLower())
            {
                case "password":
                case "passwords":
                    return "Password Tips:\n\n" +
                           "• Use at least 12 characters\n" +
                           "• Mix uppercase, lowercase, numbers, and symbols\n" +
                           "• Use different passwords for each account\n" +
                           "• Consider using a password manager\n" +
                           "• Enable two-factor authentication when possible";

                case "phishing":
                    return "Phishing Protection:\n\n" +
                           "• Be suspicious of urgent emails asking for personal info\n" +
                           "• Check sender addresses carefully\n" +
                           "• Don't click links in suspicious emails\n" +
                           "• When in doubt, contact the company directly\n" +
                           "• Look for spelling and grammar mistakes";

                case "malware":
                case "virus":
                    return "Malware Protection:\n\n" +
                           "• Keep your software updated\n" +
                           "• Use antivirus software\n" +
                           "• Don't download suspicious files\n" +
                           "• Be careful with email attachments\n" +
                           "• Backup your important files regularly";

                case "vpn":
                    return "VPN Basics:\n\n" +
                           "• VPNs encrypt your internet connection\n" +
                           "• Great for public Wi-Fi security\n" +
                           "• Choose reputable VPN providers\n" +
                           "• Look for no-logs policies\n" +
                           "• Can help protect your privacy online";

                default:
                    return $"Here are some general tips about {topic}:\n\n" +
                           "• Stay informed about latest threats\n" +
                           "• Keep software updated\n" +
                           "• Use strong, unique passwords\n" +
                           "• Be cautious with personal information\n" +
                           "• When in doubt, ask for help!";
            }
        }
    }
}