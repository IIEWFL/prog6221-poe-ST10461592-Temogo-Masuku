using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CybersecurityChatbot.Services;

namespace CybersecurityChatbot
{
    public partial class MainWindow : Window
    {
        // Core Variables
        private static Random random = new Random();
        private static string currentTopic = "";
        private static string[] currentTopicKeywords = null;
        private List<CyberSecurityTask> tasks = new List<CyberSecurityTask>();
        private DispatcherTimer reminderTimer;
        private bool waitingForReminderResponse = false;
        private CyberSecurityTask pendingTaskForReminder = null;
        private List<QuizQuestion> quizQuestions;
        private int currentQuestionIndex = 0;
        private int correctAnswers = 0;
        private bool quizInProgress = false;
        private EnhancedNLPManager _nlpManager;

        // PREDEFINED TASKS DICTIONARY
        private Dictionary<string, string> predefinedTasks = new Dictionary<string, string>
        {
            {"Enable Two-Factor Authentication", "Set up 2FA on all important accounts (email, banking, social media)"},
            {"Update All Passwords", "Create strong, unique passwords for all accounts using a password manager"},
            {"Review Privacy Settings", "Check and update privacy settings on social media and online accounts"},
            {"Install Security Updates", "Update operating system, browsers, and software with latest security patches"},
            {"Backup Important Data", "Create secure backups of important files and documents"},
            {"Review Account Activity", "Check recent login activity and transactions on all accounts"},
            {"Set Up Password Manager", "Install and configure a reputable password manager"},
            {"Enable Automatic Updates", "Configure automatic security updates for operating system and software"},
            {"Review App Permissions", "Audit permissions granted to mobile apps and browser extensions"},
            {"Secure Home Network", "Change default router passwords and enable WPA3 encryption"}
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            InitializeChatbot();
            SetupTaskReminders();
            ReminderDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            InitializeQuiz();
            InitializeNLP();
        }

        private void InitializeNLP()
        {
            try
            {
                var taskIntegration = new MockTaskIntegration(this);
                var quizIntegration = new MockQuizIntegration(this);
                _nlpManager = new EnhancedNLPManager(taskIntegration, quizIntegration);
            }
            catch (Exception ex)
            {
                _nlpManager = null;
                AppendToChatDisplay($"Warning: NLP features unavailable: {ex.Message}", Colors.Orange, false);
            }
        }

        // Activity Log Integration Method
        public void LogActivityAction(string actionType, string description, string details = "")
        {
            try
            {
                if (_nlpManager != null)
                {
                    // Access the activity log manager through reflection
                    var activityLogField = typeof(EnhancedNLPManager).GetField("_activityLogManager",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (activityLogField != null)
                    {
                        var activityLogManager = activityLogField.GetValue(_nlpManager);
                        var logMethod = activityLogManager?.GetType().GetMethod("LogAction");
                        logMethod?.Invoke(activityLogManager, new object[] { actionType, description, details });
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail if logging is not available
            }
        }

        private void InitializeChatbot()
        {
            PlaySound();
            AppendToChatDisplay("CYBERSECURITY LEARNING ASSISTANT", Colors.Blue, true);
            AppendToChatDisplay("════════════════════════════════════════════════════════════════", Colors.Gray, false);

            string userName = AskForName();
            AppendToChatDisplay($"Hello, {userName}! Welcome to the Cybersecurity Learning Assistant.", Colors.Green, true);
            AppendToChatDisplay("", Colors.Black, false);
            ShowInitialInstructions();

            // Log the chatbot initialization
            LogActivityAction("System", "User session started", $"User: {userName}");
        }

        private void ShowInitialInstructions()
        {
            var instructions = new[]
            {
                ("What cybersecurity topic would you like to learn about?", Colors.Green, false),
                ("You can ask about topics like:", Colors.Green, false),
                ("• Password safety", Colors.Black, false),
                ("• Phishing", Colors.Black, false),
                ("• Safe browsing", Colors.Black, false),
                ("• Malware", Colors.Black, false),
                ("", Colors.Black, false),
                ("Enhanced Natural Language Understanding Available!", Colors.Purple, true),
                ("Try natural requests like:", Colors.Purple, false),
                ("• 'Can you remind me to update my password next week?'", Colors.Blue, false),
                ("• 'I want to test my knowledge about phishing'", Colors.Blue, false),
                ("• 'Help me understand two-factor authentication'", Colors.Blue, false),
                ("• 'Show me what security tasks I have'", Colors.Blue, false),
                ("• 'Show activity log' to see what we've been working on", Colors.Blue, false),
                ("", Colors.Black, false),
                ("Type 'help' to see available topics or 'exit' to quit.", Colors.Gray, false),
                ("Try 'quiz' to test your cybersecurity knowledge!", Colors.Purple, true)
            };

            foreach (var (text, color, bold) in instructions)
                AppendToChatDisplay(text, color, bold);
        }

        #region Event Handlers
        private void UserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ProcessUserInput();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) => ProcessUserInput();
        #endregion

        #region Input Processing
        private void ProcessUserInput()
        {
            string userInput = UserInput.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            AppendToChatDisplay($"You: {userInput}", Colors.Blue, false);
            UserInput.Clear();

            if (waitingForReminderResponse)
            {
                HandleReminderResponse(userInput, userInput.ToLower());
                return;
            }

            // Try NLP first, then fallback to original logic
            if (_nlpManager != null && TryNLPProcessing(userInput)) return;
            ProcessUserInputOriginal(userInput, userInput.ToLower());
        }

        private bool TryNLPProcessing(string userInput)
        {
            try
            {
                var lowerInput = userInput.ToLower();
                if (new[] { "exit", "help", "clear", "reset" }.Contains(lowerInput))
                    return false;

                string response = _nlpManager.ProcessUserInput(userInput);
                if (!string.IsNullOrEmpty(response))
                {
                    AppendToChatDisplay(response, Colors.Black, false);
                    AppendToChatDisplay("", Colors.Black, false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppendToChatDisplay($"Warning: NLP processing error: {ex.Message}", Colors.Orange, false);
            }
            return false;
        }

        private void ProcessUserInputOriginal(string userInput, string lowerInput)
        {
            var commands = new Dictionary<Func<string, bool>, Action>
            {
                [input => input == "exit"] = () => {
                    LogActivityAction("System", "User session ended", "Exit command used");
                    AppendToChatDisplay("Goodbye! Stay safe online!", Colors.Red, true);
                },
                [input => input == "help"] = DisplayAvailableTopics,
                [input => input == "tasks" || input == "task manager"] = () => SwitchTab(1, "Task Manager"),
                [input => input == "quiz" || input == "start quiz" || input == "security quiz"] = () => SwitchTab(2, "Security Quiz"),
                [input => input.Contains("add task") || input.Contains("create task")] = () => HandleTaskCreationFromChat(userInput),
                [input => input.Contains("remind me") || input.Contains("reminder")] = () => HandleReminderFromChat(userInput),
                [input => input == "clear" || input == "reset"] = ResetConversation
            };

            foreach (var kvp in commands)
            {
                if (kvp.Key(lowerInput))
                {
                    kvp.Value();
                    return;
                }
            }

            ProcessUserInputLogic(lowerInput);
        }

        private void SwitchTab(int index, string tabName)
        {
            MainTabControl.SelectedIndex = index;
            AppendToChatDisplay($"Switched to {tabName} tab!", Colors.Green, false);

            // Log tab switching
            LogActivityAction("Navigation", $"Switched to {tabName} tab", "Via chat command");
        }

        private void ResetConversation()
        {
            currentTopic = "";
            currentTopicKeywords = null;
            AppendToChatDisplay("Conversation reset! What cybersecurity topic would you like to discuss?", Colors.Orange, true);

            // Log conversation reset
            LogActivityAction("System", "Conversation reset", "User requested reset");
        }
        #endregion

        #region Task Management
        public void HandleTaskCreationFromChat(string message)
        {
            var (title, description) = ExtractTaskInfo(message);
            var newTask = new CyberSecurityTask { Title = title, Description = description };

            tasks.Add(newTask);
            RefreshTaskList();

            AppendToChatDisplay($"Task added: \"{description}\"", Colors.Green, false);
            AppendToChatDisplay("", Colors.Black, false);
            AppendToChatDisplay("Would you like a reminder?", Colors.Blue, false);

            waitingForReminderResponse = true;
            pendingTaskForReminder = newTask;

            // Log task creation from chat
            LogActivityAction("Task", $"Task created via chat: '{title}'", description);
        }

        private (string title, string description) ExtractTaskInfo(string message)
        {
            var taskMappings = new Dictionary<string, (string, string)>
            {
                ["privacy"] = ("Review Privacy Settings", "Review account privacy settings to ensure your data is protected."),
                ["2fa"] = ("Enable Two-Factor Authentication", "Set up 2FA on all important accounts (email, banking, social media)"),
                ["password"] = ("Update All Passwords", "Create strong, unique passwords for all accounts using a password manager"),
                ["backup"] = ("Backup Important Data", "Create secure backups of important files and documents")
            };

            string[] parts = message.ToLower().Split(new[] { "add task", "create task" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                string taskInfo = parts[1].Trim().Replace(" - ", " ").Replace("-", " ");

                var match = taskMappings.FirstOrDefault(kvp => taskInfo.Contains(kvp.Key));
                if (!match.Equals(default(KeyValuePair<string, (string, string)>)))
                    return match.Value;

                if (taskInfo.Length > 5)
                {
                    string title = char.ToUpper(taskInfo[0]) + taskInfo.Substring(1);
                    return (title, $"Complete the following cybersecurity task: {title}");
                }
            }

            return ("Enable Two-Factor Authentication", "Set up 2FA on all important accounts (email, banking, social media)");
        }

        private void HandleReminderResponse(string userInput, string lowerInput)
        {
            var timeExpressions = new Dictionary<string, int>
            {
                ["tomorrow"] = 1,
                ["3 days"] = 3,
                ["week"] = 7
            };

            if (lowerInput.Contains("yes") || lowerInput.Contains("remind me"))
            {
                var timeMatch = timeExpressions.FirstOrDefault(kvp => lowerInput.Contains(kvp.Key));
                if (!timeMatch.Equals(default(KeyValuePair<string, int>)))
                {
                    pendingTaskForReminder.ReminderDate = DateTime.Now.AddDays(timeMatch.Value);
                    AppendToChatDisplay($"Got it! I'll remind you in {timeMatch.Key}.", Colors.Green, false);

                    // Log reminder setting
                    LogActivityAction("Reminder", $"Reminder set for task: '{pendingTaskForReminder.Title}'",
                        $"Due in {timeMatch.Value} days");
                }
                else
                {
                    AppendToChatDisplay("When would you like to be reminded? (e.g., 'in 3 days', 'in 1 week', 'tomorrow')", Colors.Blue, false);
                    return;
                }
            }
            else if (lowerInput.Contains("no") || lowerInput.Contains("not") || lowerInput.Contains("skip"))
            {
                AppendToChatDisplay("No problem! Task created without a reminder.", Colors.Green, false);
                LogActivityAction("Task", "Task created without reminder", pendingTaskForReminder.Title);
            }
            else
            {
                var directTimeMatch = timeExpressions.FirstOrDefault(kvp => lowerInput.Contains(kvp.Key));
                if (!directTimeMatch.Equals(default(KeyValuePair<string, int>)))
                {
                    pendingTaskForReminder.ReminderDate = DateTime.Now.AddDays(directTimeMatch.Value);
                    AppendToChatDisplay($"Got it! I'll remind you in {directTimeMatch.Key}.", Colors.Green, false);

                    // Log direct reminder setting
                    LogActivityAction("Reminder", $"Reminder set for task: '{pendingTaskForReminder.Title}'",
                        $"Due in {directTimeMatch.Value} days");
                }
                else
                {
                    AppendToChatDisplay("I didn't understand. Would you like a reminder? (Yes/No)", Colors.Orange, false);
                    return;
                }
            }

            RefreshTaskList();
            waitingForReminderResponse = false;
            pendingTaskForReminder = null;
            AppendToChatDisplay("", Colors.Black, false);
        }

        private void HandleReminderFromChat(string message)
        {
            var timeMap = new Dictionary<string, int> { ["tomorrow"] = 1, ["week"] = 7, ["month"] = 30, ["3 days"] = 3 };
            DateTime reminderDate = DateTime.Now.AddDays(7);

            var timeMatch = timeMap.FirstOrDefault(kvp => message.Contains(kvp.Key));
            if (!timeMatch.Equals(default(KeyValuePair<string, int>)))
                reminderDate = DateTime.Now.AddDays(timeMatch.Value);

            string reminderText = ExtractReminderText(message);
            var reminderTask = new CyberSecurityTask
            {
                Title = $"Reminder: {reminderText}",
                Description = "Scheduled reminder created from chat",
                ReminderDate = reminderDate
            };

            tasks.Add(reminderTask);
            RefreshTaskList();
            AppendToChatDisplay($"Set reminder: '{reminderText}' for {reminderDate.ToShortDateString()}", Colors.Green, true);

            // Log reminder creation
            LogActivityAction("Reminder", $"Standalone reminder created: '{reminderText}'",
                $"Due: {reminderDate.ToShortDateString()}");
        }

        private string ExtractReminderText(string message)
        {
            string[] parts = message.ToLower().Split(new[] { "remind me to", "reminder to", "remind me" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                string text = parts[1].Split(new[] { " in ", " tomorrow", " next" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                return text.Length > 0 ? char.ToUpper(text[0]) + text.Substring(1) : "Review cybersecurity settings";
            }
            return "Review cybersecurity settings";
        }
        #endregion

        #region Chatbot Logic
        private void ProcessUserInputLogic(string input)
        {
            string detectedSentiment = DetectSentiment(input);
            if (detectedSentiment != "neutral") RespondToSentiment(detectedSentiment);

            bool isFollowUp = CybersecurityData.FollowUpKeywords.Any(keyword => input.Contains(keyword.ToLower()));
            bool isContinuation = CybersecurityData.ContinuationKeywords.Any(keyword => input.Contains(keyword.ToLower()));

            if ((isFollowUp || isContinuation) && !string.IsNullOrEmpty(currentTopic))
            {
                HandleFollowUpQuestion(input, isFollowUp, isContinuation, detectedSentiment);
                return;
            }

            bool foundMatch = ProcessKeywordResponses(input, detectedSentiment);
            if (!foundMatch) HandleNoMatch(detectedSentiment);
            else AddFollowUpSuggestions(detectedSentiment);
        }

        private bool ProcessKeywordResponses(string input, string sentiment)
        {
            foreach (var kvp in CybersecurityData.KeywordResponses)
            {
                if (kvp.Key.Any(keyword => input.Contains(keyword.ToLower())))
                {
                    currentTopic = GetTopicName(kvp.Key);
                    currentTopicKeywords = kvp.Key;

                    AppendToChatDisplay($"I detected you're asking about: {currentTopic}", Colors.Purple, true);
                    AppendToChatDisplay(kvp.Value[random.Next(kvp.Value.Length)], Colors.Black, false);

                    // Log educational topic interaction
                    LogActivityAction("Education", $"Provided information about: {currentTopic}",
                        $"Sentiment: {sentiment}, Keywords: {string.Join(", ", kvp.Key)}");

                    if (sentiment == "worried")
                        AppendToChatDisplay("Remember: Taking steps to learn puts you ahead of most people online!", Colors.Green, false);
                    else if (sentiment == "frustrated")
                        AppendToChatDisplay("Don't hesitate to ask for simpler explanations!", Colors.Green, false);

                    AppendToChatDisplay("", Colors.Black, false);
                    return true;
                }
            }
            return false;
        }

        private void HandleFollowUpQuestion(string input, bool isFollowUp, bool isContinuation, string sentiment)
        {
            AppendToChatDisplay($"Continuing our discussion about {currentTopic}...", Colors.Purple, true);
            AppendToChatDisplay("", Colors.Black, false);

            // Log follow-up interaction
            LogActivityAction("Education", $"Follow-up question about: {currentTopic}",
                $"Type: {(isFollowUp ? "Follow-up" : "Continuation")}");

            if (isFollowUp)
            {
                if (CybersecurityData.DetailedExplanations.ContainsKey(currentTopic))
                {
                    string[] explanations = CybersecurityData.DetailedExplanations[currentTopic];
                    string randomExplanation = explanations[random.Next(explanations.Length)];

                    if (sentiment == "frustrated")
                        AppendToChatDisplay("Let me explain this in simpler terms:", Colors.Orange, true);

                    AppendToChatDisplay(randomExplanation, Colors.Black, false);
                }
                else
                {
                    AppendToChatDisplay($"Let me provide more details about {currentTopic}...", Colors.Black, false);
                    ProvideTopicDefinition();
                }
            }
            else if (isContinuation)
            {
                ProvideTopicDefinition();
            }

            AppendToChatDisplay("", Colors.Black, false);
            AddFollowUpSuggestions(sentiment);
        }

        private void ProvideTopicDefinition()
        {
            if (currentTopicKeywords != null && CybersecurityData.KeywordResponses.ContainsKey(currentTopicKeywords))
            {
                string[] responses = CybersecurityData.KeywordResponses[currentTopicKeywords];
                string randomResponse = responses[random.Next(responses.Length)];
                AppendToChatDisplay(randomResponse, Colors.Black, false);
            }
        }

        private void HandleNoMatch(string sentiment)
        {
            AppendToChatDisplay("I couldn't identify a specific cybersecurity topic in your message.", Colors.Orange, false);
            if (!string.IsNullOrEmpty(currentTopic))
            {
                AppendToChatDisplay($"We were discussing {currentTopic}. Would you like to continue with that topic or ask about something else?", Colors.Orange, false);
            }
            else
            {
                if (sentiment == "worried" || sentiment == "seeking_help")
                    AppendToChatDisplay("I'm here to help you feel more confident about online safety. Try asking about topics like 'password safety', 'phishing', or 'safe browsing'.", Colors.Green, false);
                else
                    AppendToChatDisplay("Try asking about topics like 'password safety', 'phishing', or 'safe browsing'.", Colors.Green, false);
            }
            AppendToChatDisplay("Type 'help' to see all available topics.", Colors.Gray, false);
        }

        private void AddFollowUpSuggestions(string sentiment)
        {
            if (sentiment == "frustrated")
                AppendToChatDisplay("If anything wasn't clear, ask 'can you explain that more simply?' or 'break that down for me'!", Colors.Green, false);
            else if (sentiment == "worried")
                AppendToChatDisplay("Want practical tips to feel more secure? Ask 'what should I do?' or 'how can I protect myself?'!", Colors.Green, false);
            else
                AppendToChatDisplay("Want to know more? Ask 'tell me more', 'explain how', or 'give me more info'!", Colors.Green, false);
        }

        private string GetTopicName(string[] keywords)
        {
            switch (keywords[0].ToLower())
            {
                case "password":
                    return "Password Security";
                case "phishing":
                    return "Phishing Protection";
                case "safe browsing":
                    return "Safe Browsing";
                case "malware":
                    return "Malware Protection";
                default:
                    return "Cybersecurity Topic";
            }
        }

        private string DetectSentiment(string input)
        {
            return CybersecurityData.SentimentKeywords.FirstOrDefault(kvp =>
                kvp.Key.Any(keyword => input.Contains(keyword.ToLower()))).Value ?? "neutral";
        }

        private void RespondToSentiment(string sentiment)
        {
            if (CybersecurityData.SentimentResponses.ContainsKey(sentiment))
            {
                var responses = CybersecurityData.SentimentResponses[sentiment];
                AppendToChatDisplay($"Note: {responses[random.Next(responses.Length)]}", Colors.Purple, false);
                AppendToChatDisplay("", Colors.Black, false);
            }
        }

        private void DisplayAvailableTopics()
        {
            LogActivityAction("Help", "User requested help menu");

            AppendToChatDisplay("Available Cybersecurity Topics:", Colors.Purple, true);
            AppendToChatDisplay("════════════════════════════════════", Colors.Gray, false);
            AppendToChatDisplay("Password Security", Colors.Black, false);
            AppendToChatDisplay("Phishing Protection", Colors.Black, false);
            AppendToChatDisplay("Safe Browsing", Colors.Black, false);
            AppendToChatDisplay("Malware Protection", Colors.Black, false);
            AppendToChatDisplay("", Colors.Black, false);
            AppendToChatDisplay("Commands:", Colors.Green, true);
            AppendToChatDisplay("• 'help' - Show this help menu", Colors.Black, false);
            AppendToChatDisplay("• 'tasks' - Open task manager", Colors.Black, false);
            AppendToChatDisplay("• 'quiz' - Take the cybersecurity knowledge quiz", Colors.Black, false);
            AppendToChatDisplay("• 'add task [description]' - Create a new task", Colors.Black, false);
            AppendToChatDisplay("• 'remind me to [action] in [timeframe]' - Set a reminder", Colors.Black, false);
            AppendToChatDisplay("• 'show activity log' - View recent activity", Colors.Black, false);
            AppendToChatDisplay("• 'clear' - Reset conversation", Colors.Black, false);
            AppendToChatDisplay("• 'exit' - Quit application", Colors.Black, false);
            AppendToChatDisplay("", Colors.Black, false);
            AppendToChatDisplay("Enhanced NLP: You can now use natural language!", Colors.Purple, true);
            AppendToChatDisplay("Try: 'Can you remind me to update my password next week?'", Colors.Blue, false);
        }
        #endregion

        #region UI Helper Methods
        private void AppendToChatDisplay(string text, Color color, bool bold)
        {
            var paragraph = new Paragraph();
            var run = new Run(text) { Foreground = new SolidColorBrush(color) };
            if (bold) run.FontWeight = FontWeights.Bold;
            paragraph.Inlines.Add(run);
            ChatDisplay.Document.Blocks.Add(paragraph);
            ChatDisplay.ScrollToEnd();
        }

        private void PlaySound()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("CybersecurityChatbot.Chatbot_recording.wav"))
                {
                    if (stream != null)
                    {
                        string tempFile = Path.GetTempFileName();
                        string wavTempFile = Path.ChangeExtension(tempFile, ".wav");
                        using (FileStream fileStream = File.Create(wavTempFile))
                            stream.CopyTo(fileStream);
                        new SoundPlayer(wavTempFile).Play();
                        System.Threading.Tasks.Task.Delay(5000).ContinueWith(t =>
                        {
                            try { File.Delete(wavTempFile); File.Delete(tempFile); } catch { }
                        });
                    }
                    else SystemSounds.Asterisk.Play();
                }
            }
            catch { SystemSounds.Asterisk.Play(); }
        }

        private string AskForName()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter your name:", "Welcome", "User");
            while (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 20)
            {
                string prompt = string.IsNullOrEmpty(name) ? "Name cannot be empty." : "Name must be 3-20 characters.";
                name = Microsoft.VisualBasic.Interaction.InputBox($"{prompt} Please enter your name:", "Name Required", "User");
                if (name == "") return "User";
            }
            return name;
        }
        #endregion

        #region Task UI Events
        private void PredefinedTasksComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PredefinedTasksComboBox.SelectedIndex > 0)
            {
                string selectedTask = ((ComboBoxItem)PredefinedTasksComboBox.SelectedItem).Content.ToString();
                TaskTitleTextBox.Text = selectedTask;
                if (predefinedTasks.ContainsKey(selectedTask))
                {
                    TaskDescriptionTextBox.Text = predefinedTasks[selectedTask];
                }

                // Log predefined task selection
                LogActivityAction("Task", $"Predefined task selected: '{selectedTask}'", "Via dropdown menu");
            }
        }

        private void EnableReminderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ReminderDatePicker.IsEnabled = true;
        }

        private void EnableReminderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ReminderDatePicker.IsEnabled = false;
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskTitleTextBox.Text))
            {
                MessageBox.Show("Please enter a task title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newTask = new CyberSecurityTask
            {
                Title = TaskTitleTextBox.Text.Trim(),
                Description = TaskDescriptionTextBox.Text.Trim(),
                ReminderDate = EnableReminderCheckBox.IsChecked == true ? ReminderDatePicker.SelectedDate : null
            };

            tasks.Add(newTask);
            RefreshTaskList();
            ClearTaskInputs();

            // Log task creation via UI
            LogActivityAction("Task", $"Task added via UI: '{newTask.Title}'",
                newTask.ReminderDate.HasValue ? $"Reminder: {newTask.ReminderDate.Value.ToShortDateString()}" : "No reminder");

            MessageBox.Show($"Task '{newTask.Title}' added successfully!", "Task Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem == null)
            {
                MessageBox.Show("Please select a task to complete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedTask = (TaskListViewItem)TaskListView.SelectedItem;
            selectedTask.Task.IsCompleted = true;
            RefreshTaskList();

            // Log task completion
            LogActivityAction("Task", $"Task completed via UI: '{selectedTask.Task.Title}'",
                "Marked as completed using Complete button");

            MessageBox.Show($"Task '{selectedTask.Task.Title}' marked as completed!", "Task Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem == null)
            {
                MessageBox.Show("Please select a task to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this task?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (MessageBox.Show("Are you sure you want to delete this task?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var selectedTask = (TaskListViewItem)TaskListView.SelectedItem;

                    // Log task deletion before removing
                    LogActivityAction("Task", $"Task deleted via UI: '{selectedTask.Task.Title}'",
                        "User confirmed deletion");

                    tasks.Remove(selectedTask.Task);
                    RefreshTaskList();
                    MessageBox.Show("Task deleted successfully.", "Task Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void RefreshTaskList()
        {
            TaskListView.Items.Clear();
            foreach (var task in tasks.OrderByDescending(t => t.CreatedDate))
            {
                TaskListView.Items.Add(new TaskListViewItem
                {
                    Task = task,
                    Status = task.IsCompleted ? "Done" : "Pending",
                    Title = task.Title,
                    Description = task.Description.Length > 50 ? task.Description.Substring(0, 47) + "..." : task.Description,
                    Reminder = task.ReminderDate?.ToShortDateString() ?? "None",
                    Created = task.CreatedDate.ToShortDateString()
                });
            }
            UpdateTaskStatistics();
        }

        private void UpdateTaskStatistics()
        {
            int total = tasks.Count, completed = tasks.Count(t => t.IsCompleted);
            int pending = total - completed, upcoming = tasks.Count(t => !t.IsCompleted && t.ReminderDate.HasValue && t.ReminderDate.Value <= DateTime.Now.AddDays(7));
            TaskStatsTextBlock.Text = $"Tasks: {total} Total | {completed} Completed | {pending} Pending | {upcoming} Upcoming";
        }

        private void ClearTaskInputs()
        {
            TaskTitleTextBox.Clear();
            TaskDescriptionTextBox.Clear();
            EnableReminderCheckBox.IsChecked = false;
            ReminderDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            PredefinedTasksComboBox.SelectedIndex = 0;
        }

        private void SetupTaskReminders()
        {
            reminderTimer = new DispatcherTimer();
            reminderTimer.Interval = TimeSpan.FromMinutes(1);
            reminderTimer.Tick += ReminderTimer_Tick;
            reminderTimer.Start();
        }

        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            var dueReminders = tasks.Where(t =>
                !t.IsCompleted &&
                t.ReminderDate.HasValue &&
                t.ReminderDate.Value <= DateTime.Now &&
                t.ReminderDate.Value > DateTime.Now.AddMinutes(-1)).ToList();

            foreach (var task in dueReminders)
            {
                // Log reminder notification
                LogActivityAction("Reminder", $"Reminder notification shown: '{task.Title}'",
                    $"Due date: {task.ReminderDate.Value.ToShortDateString()}");

                MessageBox.Show($"Reminder: {task.Title}\n\n{task.Description}\n\nDon't forget to complete this cybersecurity task!",
                    "Security Task Reminder", MessageBoxButton.OK, MessageBoxImage.Information);

                if (MainTabControl.SelectedIndex == 0)
                {
                    AppendToChatDisplay($"REMINDER: {task.Title}", Colors.Red, true);
                    AppendToChatDisplay($"Task: {task.Description}", Colors.Black, false);
                    AppendToChatDisplay("", Colors.Black, false);
                }
            }
        }
        #endregion

        #region Quiz Implementation
        private void InitializeQuiz()
        {
            quizQuestions = QuizData.GetQuestions();
        }

        private void StartQuizButton_Click(object sender, RoutedEventArgs e)
        {
            // Log quiz start via UI
            LogActivityAction("Quiz", "Quiz started via UI", "User clicked Start Quiz button");
            StartQuiz();
        }

        private void NextQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNextQuestion();
        }

        private void RetakeQuizButton_Click(object sender, RoutedEventArgs e)
        {
            // Log quiz retake
            LogActivityAction("Quiz", "Quiz retake requested", "User clicked Retake Quiz button");
            ResetQuiz();
            StartQuiz();
        }

        private void BackToChatButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 0;
            // Log navigation back to chat
            LogActivityAction("Navigation", "Returned to chat from quiz", "User clicked Back to Chat button");
        }

        private void StartQuiz()
        {
            if (quizQuestions == null)
                InitializeQuiz();

            quizInProgress = true;
            currentQuestionIndex = 0;
            correctAnswers = 0;

            ShuffleQuestions();

            WelcomePanel.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Collapsed;
            QuestionPanel.Visibility = Visibility.Visible;

            ShowCurrentQuestion();

            // Log quiz initialization
            LogActivityAction("Quiz", "Quiz session started", $"Total questions: {quizQuestions.Count}");
        }

        private void ShuffleQuestions()
        {
            Random rng = new Random();
            int n = quizQuestions.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var temp = quizQuestions[k];
                quizQuestions[k] = quizQuestions[n];
                quizQuestions[n] = temp;
            }
        }

        private void ShowCurrentQuestion()
        {
            if (currentQuestionIndex >= quizQuestions.Count)
            {
                ShowResults();
                return;
            }

            var question = quizQuestions[currentQuestionIndex];

            QuestionCounterTextBlock.Text = $"Question {currentQuestionIndex + 1} of {quizQuestions.Count}";
            QuizProgressBar.Value = currentQuestionIndex;

            QuestionTextBlock.Text = question.Question;

            AnswerOptionsPanel.Children.Clear();

            for (int i = 0; i < question.Options.Count; i++)
            {
                Button answerButton = new Button
                {
                    Content = $"{(char)('A' + i)}. {question.Options[i]}",
                    FontSize = 14,
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 5, 0, 5),
                    Background = new SolidColorBrush(Colors.LightBlue),
                    BorderBrush = new SolidColorBrush(Colors.SteelBlue),
                    BorderThickness = new Thickness(2, 2, 2, 2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Tag = i
                };
                answerButton.Click += AnswerButton_Click;
                AnswerOptionsPanel.Children.Add(answerButton);
            }

            FeedbackBorder.Visibility = Visibility.Collapsed;
            NextQuestionButton.Visibility = Visibility.Collapsed;

            QuizStatusTextBlock.Text = $"Category: {question.Category} | Think carefully before answering!";

            // Log question display
            LogActivityAction("Quiz", $"Question {currentQuestionIndex + 1} displayed",
                $"Category: {question.Category}, Type: {question.Type}");
        }

        private void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            int selectedAnswer = (int)clickedButton.Tag;
            var question = quizQuestions[currentQuestionIndex];

            bool isCorrect = selectedAnswer == question.CorrectAnswerIndex;

            foreach (Button button in AnswerOptionsPanel.Children.OfType<Button>())
            {
                button.IsEnabled = false;

                int buttonIndex = (int)button.Tag;
                if (buttonIndex == question.CorrectAnswerIndex)
                {
                    button.Background = new SolidColorBrush(Colors.LightGreen);
                    button.BorderBrush = new SolidColorBrush(Colors.Green);
                }
                else if (buttonIndex == selectedAnswer && !isCorrect)
                {
                    button.Background = new SolidColorBrush(Colors.LightCoral);
                    button.BorderBrush = new SolidColorBrush(Colors.Red);
                }
            }

            if (isCorrect)
                correctAnswers++;

            // Log answer selection
            LogActivityAction("Quiz", $"Question {currentQuestionIndex + 1} answered",
                $"Selected: {(char)('A' + selectedAnswer)}, Correct: {isCorrect}, Category: {question.Category}");

            ShowAnswerFeedback(isCorrect, question.Explanation);

            if (currentQuestionIndex < quizQuestions.Count - 1)
            {
                NextQuestionButton.Content = "Next Question";
                NextQuestionButton.Visibility = Visibility.Visible;
            }
            else
            {
                NextQuestionButton.Content = "See Results";
                NextQuestionButton.Visibility = Visibility.Visible;
            }
        }

        private void ShowAnswerFeedback(bool isCorrect, string explanation)
        {
            FeedbackHeaderTextBlock.Text = isCorrect ? "Correct!" : "Incorrect";
            FeedbackHeaderTextBlock.Foreground = new SolidColorBrush(isCorrect ? Colors.Green : Colors.Red);

            FeedbackTextBlock.Text = explanation;

            FeedbackBorder.Background = new SolidColorBrush(isCorrect ? Colors.LightGreen : Colors.LightPink);
            FeedbackBorder.BorderBrush = new SolidColorBrush(isCorrect ? Colors.Green : Colors.Red);
            FeedbackBorder.Visibility = Visibility.Visible;
        }

        private void ShowNextQuestion()
        {
            currentQuestionIndex++;

            if (currentQuestionIndex >= quizQuestions.Count)
            {
                ShowResults();
            }
            else
            {
                ShowCurrentQuestion();
            }
        }

        private void ShowResults()
        {
            quizInProgress = false;

            QuestionPanel.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;

            int percentage = (int)Math.Round((double)correctAnswers / quizQuestions.Count * 100);

            ScoreTextBlock.Text = $"Your Score: {correctAnswers}/{quizQuestions.Count}";
            ScorePercentageTextBlock.Text = $"{percentage}%";

            if (percentage >= 90)
            {
                ScorePercentageTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            }
            else if (percentage >= 70)
            {
                ScorePercentageTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                ScorePercentageTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }

            string feedback = GetScoreFeedback(percentage);
            ScoreFeedbackTextBlock.Text = feedback;

            QuizStatusTextBlock.Text = "Quiz completed! Review your results and consider retaking to improve your score.";

            // Log quiz completion with detailed results
            LogActivityAction("Quiz", "Quiz completed",
                $"Score: {correctAnswers}/{quizQuestions.Count} ({percentage}%), Level: {GetPerformanceLevel(percentage)}");
        }

        private string GetPerformanceLevel(int percentage)
        {
            if (percentage >= 90) return "Expert";
            if (percentage >= 80) return "Advanced";
            if (percentage >= 70) return "Intermediate";
            if (percentage >= 60) return "Beginner";
            return "Needs Improvement";
        }

        private string GetScoreFeedback(int percentage)
        {
            if (percentage == 100)
            {
                return "Perfect! You're a cybersecurity expert! Your knowledge will keep you safe online. Consider sharing these tips with friends and family.";
            }
            else if (percentage >= 90)
            {
                return "Excellent! You have strong cybersecurity awareness. You're well-prepared to protect yourself online. Just review the questions you missed.";
            }
            else if (percentage >= 80)
            {
                return "Great job! You have good cybersecurity knowledge. Consider reviewing the areas where you missed questions to strengthen your defenses.";
            }
            else if (percentage >= 70)
            {
                return "Good work! You have solid basic knowledge. Focus on learning more about the topics you missed to improve your online safety.";
            }
            else if (percentage >= 60)
            {
                return "Keep learning! You have some cybersecurity awareness, but there's room for improvement. Review the explanations and consider taking our chat lessons.";
            }
            else if (percentage >= 50)
            {
                return "You're on the right track, but need more practice! Cybersecurity is crucial - spend time learning about password safety, phishing, and safe browsing.";
            }
            else
            {
                return "Important: Your cybersecurity knowledge needs attention! This puts you at risk online. Please review all the explanations and consider learning more through our chat feature.";
            }
        }

        private void ResetQuiz()
        {
            currentQuestionIndex = 0;
            correctAnswers = 0;
            quizInProgress = false;

            WelcomePanel.Visibility = Visibility.Visible;
            QuestionPanel.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Collapsed;

            QuizProgressBar.Value = 0;

            QuizStatusTextBlock.Text = "Tip: Read each question carefully and think about real-world scenarios!";
        }
        #endregion
    }

    // EXTRACTED DATA CLASSES TO REDUCE CLUTTER
    public static class CybersecurityData
    {
        public static readonly string[] FollowUpKeywords = { "tell me more", "explain", "can you elaborate" };
        public static readonly string[] ContinuationKeywords = { "more info", "continue", "keep going", "what else" };

        public static readonly Dictionary<string[], string> SentimentKeywords = new Dictionary<string[], string>
        {
            [new[] { "worried", "concerned", "overwhelmed" }] = "worried",
            [new[] { "curious", "interested", "want to know" }] = "curious",
            [new[] { "frustrated", "confused", "don't understand" }] = "frustrated",
            [new[] { "confident", "prepared", "understand" }] = "confident",
            [new[] { "help", "lost", "don't know" }] = "seeking_help"
        };

        public static readonly Dictionary<string, string[]> SentimentResponses = new Dictionary<string, string[]>
        {
            ["worried"] = new[] {
                "It's completely understandable to feel that way. Cybersecurity threats can seem overwhelming, but knowledge is your best defense.",
                "Your concern shows you're taking online safety seriously, which is great! Let me help ease your worries with some practical guidance."
            },
            ["curious"] = new[] {
                "I love your curiosity! Learning about cybersecurity is one of the best ways to protect yourself online.",
                "Great question! Your interest in understanding these topics will help keep you safer online."
            },
            ["frustrated"] = new[] {
                "I understand this can be confusing. Let me break it down into simpler terms to make it clearer.",
                "Don't worry if this seems complicated - cybersecurity can be complex, but I'll explain it step by step."
            },
            ["confident"] = new[] {
                "That's great to hear! Your confidence in understanding these concepts will serve you well.",
                "Wonderful! It sounds like you're building a strong foundation in cybersecurity awareness."
            },
            ["seeking_help"] = new[] {
                "I'm here to help! Let's work through this together step by step.",
                "No problem at all - that's exactly what I'm here for. Let me guide you through this."
            }
        };

        public static readonly Dictionary<string[], string[]> KeywordResponses = new Dictionary<string[], string[]>
        {
            [new[] { "password", "passwords", "password safety" }] = new[] {
                "Password Security Definition: Password security refers to the practices and measures used to create, manage, and protect passwords to prevent unauthorized access to accounts and systems. It involves using strong, unique passwords and implementing additional security measures.",
                "What is Password Security: Password security is the foundation of digital protection, encompassing the creation of complex passwords that are difficult to guess or crack, using different passwords for different accounts, and employing tools like password managers."
            },
            [new[] { "phishing", "phishing email", "phishing attack" }] = new[] {
                "Phishing Definition: Phishing is a cybercrime technique where attackers impersonate legitimate organizations through fraudulent emails, websites, or messages to trick victims into revealing sensitive information like passwords, credit card numbers, or personal data.",
                "What is Phishing: Phishing is a social engineering attack method where cybercriminals create fake communications that appear to come from trusted sources to steal sensitive information, install malware, or gain unauthorized access to systems."
            },
            [new[] { "safe browsing", "internet safety", "online safety" }] = new[] {
                "Safe Browsing Definition: Safe browsing refers to the practices and technologies used to protect users from online threats while navigating the internet, including avoiding malicious websites, protecting personal information, and maintaining secure connections.",
                "What is Safe Browsing: Safe browsing is a set of security practices and tools designed to protect internet users from web-based threats such as malicious websites, unsafe downloads, and privacy violations while maintaining a secure online experience."
            },
            [new[] { "malware", "trojan", "ransomware" }] = new[] {
                "Malware Definition: Malware (malicious software) is any software intentionally designed to cause damage, steal information, gain unauthorized access, or disrupt computer systems, including viruses, trojans, spyware, and ransomware.",
                "What is Malware: Malware refers to various types of harmful software created by cybercriminals to infiltrate, damage, or gain unauthorized control over computer systems, networks, and devices for malicious purposes."
            }
        };

        public static readonly Dictionary<string, string[]> DetailedExplanations = new Dictionary<string, string[]>
        {
            ["Password Security"] = new[] {
                "Password Security Components: Password security involves three main elements: password strength (complexity and length), password uniqueness (different passwords for different accounts), and password protection (secure storage and two-factor authentication).",
                "Why Password Security Matters: Passwords are the primary barrier between your personal information and cybercriminals. Weak passwords can be cracked in seconds, while strong passwords combined with security measures can protect your digital identity for years."
            },
            ["Phishing Protection"] = new[] {
                "Types of Phishing: Phishing comes in many forms including email phishing (fake emails), spear phishing (targeted attacks), whaling (targeting executives), smishing (SMS phishing), and vishing (voice/phone phishing).",
                "How Phishing Works: Attackers research targets, create convincing fake communications, establish urgency or fear, trick victims into clicking links or sharing information, then use the stolen data for financial gain or further attacks."
            },
            ["Safe Browsing"] = new[] {
                "Web Threat Types: Online threats include malicious websites hosting malware, fake websites designed for phishing, drive-by downloads that install software without consent, and compromised legitimate sites serving malicious content.",
                "Browser Security Features: Modern browsers include built-in protection like phishing and malware detection, secure connection warnings, popup blockers, and automatic security updates to protect users from web-based threats."
            },
            ["Malware Protection"] = new[] {
                "Malware Categories: Malware includes viruses (self-replicating code), trojans (disguised malicious software), ransomware (encrypts files for money), spyware (secretly monitors activity), and rootkits (hide other malware).",
                "Infection Methods: Malware spreads through email attachments, infected downloads, malicious websites, USB drives, network vulnerabilities, and social engineering tactics that trick users into installation."
            }
        };
    }

    public static class QuizData
    {
        public static List<QuizQuestion> GetQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What makes a password strong and secure?",
                    Options = new List<string> { "Using your birthday", "At least 12 characters with mixed letters, numbers, and symbols", "Using common words like 'password'", "Using the same password everywhere" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Strong passwords should be at least 12 characters long and include a mix of uppercase, lowercase, numbers, and special characters. Avoid personal information and common words.",
                    Category = "Password Safety",
                    Type = QuestionType.MultipleChoice
                },
                new QuizQuestion
                {
                    Question = "True or False: It's safe to use the same password for multiple accounts if it's a strong password.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "False! Even strong passwords should be unique for each account. If one account is compromised, all your other accounts remain safe with unique passwords.",
                    Category = "Password Safety",
                    Type = QuestionType.TrueFalse
                },
                new QuizQuestion
                {
                    Question = "Which of these is a common sign of a phishing email?",
                    Options = new List<string> { "Personalized greeting with your full name", "Urgent language like 'Act now or lose access!'", "Professional company logo", "Detailed contact information" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Phishing emails often create false urgency to pressure you into quick action without thinking. Legitimate companies rarely threaten immediate account closure.",
                    Category = "Phishing",
                    Type = QuestionType.MultipleChoice
                },
                new QuizQuestion
                {
                    Question = "What should you do if you receive an unexpected email asking you to verify your bank account?",
                    Options = new List<string> { "Click the link immediately", "Reply with your account details", "Contact your bank directly using official contact methods", "Forward it to friends" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Never click links in suspicious emails. Always contact your bank directly using phone numbers or websites you know are legitimate.",
                    Category = "Phishing",
                    Type = QuestionType.MultipleChoice
                },
                new QuizQuestion
                {
                    Question = "True or False: HTTPS websites (with the padlock icon) are always 100% safe to use.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "False! While HTTPS encrypts data transmission, malicious websites can also use HTTPS. Always verify the website's legitimacy and check for spelling errors in the URL.",
                    Category = "Safe Browsing",
                    Type = QuestionType.TrueFalse
                },
                new QuizQuestion
                {
                    Question = "When shopping online, which is the safest way to make payments?",
                    Options = new List<string> { "Debit card on any website", "Wire transfer", "Credit card or secure payment services like PayPal", "Cryptocurrency only" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Credit cards and secure payment services offer better fraud protection and dispute resolution compared to debit cards or wire transfers.",
                    Category = "Safe Browsing",
                    Type = QuestionType.MultipleChoice
                },
                new QuizQuestion
                {
                    Question = "What is social engineering in cybersecurity?",
                    Options = new List<string> { "Building social networks", "Manipulating people to reveal confidential information", "Engineering social media platforms", "Creating user-friendly interfaces" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Social engineering attacks manipulate human psychology rather than technical vulnerabilities to trick people into revealing sensitive information or performing actions that compromise security.",
                    Category = "Social Engineering",
                    Type = QuestionType.MultipleChoice
                },
                new QuizQuestion
                {
                    Question = "A stranger calls claiming to be from IT support and asks for your password to 'fix your computer.' What should you do?",
                    Options = new List<string> { "Give them the password immediately", "Ask for their employee ID first", "Hang up and contact your IT department directly", "Give them a fake password" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Legitimate IT support will never ask for your password over the phone. Always verify requests through official channels before sharing any information.",
                    Category = "Social Engineering",
                    Type = QuestionType.MultipleChoice
                },
                new QuizQuestion
                {
                    Question = "True or False: Public Wi-Fi networks are safe for online banking and shopping.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "False! Public Wi-Fi networks are often unsecured and can be monitored by attackers. Avoid sensitive activities on public Wi-Fi or use a VPN for protection.",
                    Category = "Safe Browsing",
                    Type = QuestionType.TrueFalse
                },
                new QuizQuestion
                {
                    Question = "What is the best practice for software updates?",
                    Options = new List<string> { "Never update to avoid bugs", "Only update once a year", "Install updates promptly, especially security updates", "Wait for others to test updates first" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Security updates patch vulnerabilities that attackers could exploit. Installing them promptly is crucial for maintaining your device's security.",
                    Category = "General Security",
                    Type = QuestionType.MultipleChoice
                }
            };
        }
    }

    // MOCK INTEGRATION CLASSES FOR NLP
    public class MockTaskIntegration
    {
        private MainWindow _mainWindow;

        public MockTaskIntegration(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public string ProcessTaskCommand(string userInput)
        {
            var lowerInput = userInput.ToLower();

            if (lowerInput.Contains("add task") || lowerInput.Contains("create task") ||
                lowerInput.Contains("remind me"))
            {
                _mainWindow.HandleTaskCreationFromChat(userInput);
                return ""; // Let existing method handle response
            }

            if (lowerInput.Contains("show") && lowerInput.Contains("task"))
            {
                _mainWindow.MainTabControl.SelectedIndex = 1;
                return "Switched to Task Manager! Here you can view and manage all your cybersecurity tasks.";
            }

            return null; // Not a task command
        }

        public string GetTaskAssistantHelp()
        {
            return "Task Management Help:\n\n" +
                   "• 'Add task to enable 2FA' - Create security tasks\n" +
                   "• 'Remind me to update passwords in 7 days'\n" +
                   "• 'Show my tasks' - View task manager\n" +
                   "• Switch to the Task Manager tab for full management";
        }
    }

    public class MockQuizIntegration
    {
        private MainWindow _mainWindow;

        public MockQuizIntegration(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public string ProcessQuizCommand(string userInput)
        {
            var lowerInput = userInput.ToLower();

            if (lowerInput.Contains("quiz") || lowerInput.Contains("question") ||
                lowerInput.Contains("test"))
            {
                _mainWindow.MainTabControl.SelectedIndex = 2;
                return "Starting the Cybersecurity Quiz! Test your knowledge with our interactive quiz featuring 10 questions covering password safety, phishing, safe browsing, and more!";
            }

            return null; // Not a quiz command
        }

        public string GetQuizHelp()
        {
            return "Quiz Help:\n\n" +
                   "• 'Start quiz' - Take the cybersecurity knowledge test\n" +
                   "• 'Quiz' - Launch the interactive quiz\n" +
                   "• Switch to the Security Quiz tab for the full experience";
        }

        public bool IsInQuizMode => false;
        public void ExitQuizMode() { }
    }

    // MODEL CLASSES
    public class CyberSecurityTask
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; }

        public CyberSecurityTask()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            IsCompleted = false;
        }
    }

    public class TaskListViewItem
    {
        public CyberSecurityTask Task { get; set; }
        public string Status { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Reminder { get; set; }
        public string Created { get; set; }
    }

    public class QuizQuestion
    {
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public string Explanation { get; set; }
        public string Category { get; set; }
        public QuestionType Type { get; set; }

        public QuizQuestion()
        {
            Options = new List<string>();
        }
    }

    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse
    }
}