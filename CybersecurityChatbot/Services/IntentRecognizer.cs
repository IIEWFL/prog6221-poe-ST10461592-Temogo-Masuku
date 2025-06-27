using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static CybersecurityChatbot.Services.IntentRecognizer;

namespace CybersecurityChatbot.Services
{
    public class IntentRecognizer
    {
        // Intent categories
        public enum UserIntent
        {
            CreateTask,
            ViewTasks,
            CompleteTask,
            DeleteTask,
            SetReminder,
            StartQuiz,
            QuickQuestion,
            ViewQuizStats,
            OpenTaskAssistant,
            OpenQuizGame,
            GetCybersecurityAdvice,
            GetHelp,
            Greeting,
            Unknown
        }

        // Intent patterns with confidence scores
        private readonly Dictionary<UserIntent, List<IntentPattern>> _intentPatterns;

        public IntentRecognizer()
        {
            _intentPatterns = InitializeIntentPatterns();
        }

        public IntentResult RecognizeIntent(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return new IntentResult(UserIntent.Unknown, 0.0, new Dictionary<string, string>());

            var preprocessedInput = PreprocessInput(userInput);
            var bestMatch = new IntentResult(UserIntent.Unknown, 0.0, new Dictionary<string, string>());

            foreach (var intentCategory in _intentPatterns)
            {
                foreach (var pattern in intentCategory.Value)
                {
                    var confidence = CalculateConfidence(preprocessedInput, pattern);
                    if (confidence > bestMatch.Confidence && confidence >= pattern.MinConfidence)
                    {
                        var entities = ExtractEntities(preprocessedInput, pattern);
                        bestMatch = new IntentResult(intentCategory.Key, confidence, entities);
                    }
                }
            }

            return bestMatch;
        }

        private string PreprocessInput(string input)
        {
            // Convert to lowercase and normalize
            input = input.ToLowerInvariant().Trim();

            // Handle contractions
            input = input.Replace("can't", "cannot");
            input = input.Replace("won't", "will not");
            input = input.Replace("don't", "do not");
            input = input.Replace("didn't", "did not");
            input = input.Replace("wouldn't", "would not");
            input = input.Replace("shouldn't", "should not");
            input = input.Replace("couldn't", "could not");
            input = input.Replace("i'm", "i am");
            input = input.Replace("you're", "you are");
            input = input.Replace("we're", "we are");
            input = input.Replace("they're", "they are");
            input = input.Replace("it's", "it is");
            input = input.Replace("that's", "that is");
            input = input.Replace("what's", "what is");
            input = input.Replace("where's", "where is");
            input = input.Replace("how's", "how is");
            input = input.Replace("i'll", "i will");
            input = input.Replace("you'll", "you will");
            input = input.Replace("we'll", "we will");
            input = input.Replace("they'll", "they will");
            input = input.Replace("i'd", "i would");
            input = input.Replace("you'd", "you would");
            input = input.Replace("he'd", "he would");
            input = input.Replace("she'd", "she would");
            input = input.Replace("we'd", "we would");
            input = input.Replace("they'd", "they would");
            input = input.Replace("i've", "i have");
            input = input.Replace("you've", "you have");
            input = input.Replace("we've", "we have");
            input = input.Replace("they've", "they have");

            // Remove extra whitespace and punctuation at the end
            input = Regex.Replace(input, @"[.!?]+$", "");
            input = Regex.Replace(input, @"\s+", " ");

            return input;
        }

        private double CalculateConfidence(string input, IntentPattern pattern)
        {
            double score = 0.0;
            int totalChecks = 0;

            // Check required keywords
            foreach (var keyword in pattern.RequiredKeywords)
            {
                totalChecks++;
                if (ContainsKeyword(input, keyword))
                    score += pattern.KeywordWeight;
            }

            // Check optional keywords (boost score)
            foreach (var keyword in pattern.OptionalKeywords)
            {
                if (ContainsKeyword(input, keyword))
                    score += pattern.OptionalKeywordBoost;
            }

            // Check patterns using regex
            foreach (var regexPattern in pattern.RegexPatterns)
            {
                totalChecks++;
                if (Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase))
                    score += pattern.PatternWeight;
            }

            // Check negative indicators (reduce score)
            foreach (var negative in pattern.NegativeIndicators)
            {
                if (ContainsKeyword(input, negative))
                    score -= pattern.NegativePenalty;
            }

            // Normalize score based on total checks
            if (totalChecks > 0)
                score = Math.Max(0, score / totalChecks);

            return Math.Min(1.0, score);
        }

        private bool ContainsKeyword(string input, string keyword)
        {
            // Use word boundaries to avoid partial matches
            var pattern = $@"\b{Regex.Escape(keyword.ToLowerInvariant())}\b";
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }

        private Dictionary<string, string> ExtractEntities(string input, IntentPattern pattern)
        {
            var entities = new Dictionary<string, string>();

            // Extract specific entities based on pattern
            foreach (var entityPattern in pattern.EntityPatterns)
            {
                var match = Regex.Match(input, entityPattern.Value, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    entities[entityPattern.Key] = match.Groups[1].Value.Trim();
                }
            }

            // Extract common entities
            ExtractCommonEntities(input, entities);

            return entities;
        }

        private void ExtractCommonEntities(string input, Dictionary<string, string> entities)
        {
            // Extract time expressions
            var timePatterns = new[]
            {
                @"(?:in\s+)?(\d+)\s+(day|days|week|weeks|month|months|hour|hours|minute|minutes)s?\b",
                @"\b(tomorrow|today|tonight|yesterday)\b",
                @"\b(next\s+(?:week|month|year))\b",
                @"\b(this\s+(?:week|month|year))\b"
            };

            foreach (var pattern in timePatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    entities["timeframe"] = match.Value.Trim();
                    break;
                }
            }

            // Extract cybersecurity topics
            var securityTopics = new[]
            {
                "password", "passwords", "2fa", "two-factor", "two factor", "authentication",
                "phishing", "malware", "virus", "antivirus", "firewall", "vpn",
                "encryption", "backup", "privacy", "security", "hacker", "hacking",
                "breach", "data protection", "network security", "safe browsing"
            };

            foreach (var topic in securityTopics)
            {
                if (ContainsKeyword(input, topic))
                {
                    entities["security_topic"] = topic;
                    break;
                }
            }

            // Extract task-related keywords
            var taskKeywords = new[]
            {
                "enable", "disable", "update", "change", "review", "check", "setup",
                "configure", "install", "uninstall", "activate", "deactivate"
            };

            foreach (var keyword in taskKeywords)
            {
                if (ContainsKeyword(input, keyword))
                {
                    entities["action"] = keyword;
                    break;
                }
            }
        }

        private Dictionary<UserIntent, List<IntentPattern>> InitializeIntentPatterns()
        {
            return new Dictionary<UserIntent, List<IntentPattern>>
            {
                [UserIntent.CreateTask] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "add", "task" },
                        OptionalKeywords = new[] { "create", "new", "reminder", "todo" },
                        RegexPatterns = new[] { @"(add|create|make|set up)\s+.*?(task|reminder|todo)" },
                        EntityPatterns = new Dictionary<string, string>
                        {
                            ["task_description"] = @"(?:add|create|make|set up)\s+(?:a\s+)?(?:task|reminder|todo)?\s+(?:to\s+)?(.+)",
                            ["reminder_time"] = @"(?:remind me|in|after)\s+(.+?)(?:\s|$)"
                        },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.4,
                        PatternWeight = 0.6
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "remind" },
                        OptionalKeywords = new[] { "me", "to", "about" },
                        RegexPatterns = new[] { @"(remind|reminder).*?(me|us)" },
                        EntityPatterns = new Dictionary<string, string>
                        {
                            ["task_description"] = @"remind\s+me\s+to\s+(.+?)(?:\s+in|\s+after|$)",
                            ["reminder_time"] = @"(?:in|after)\s+(.+?)(?:\s|$)"
                        },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.5,
                        PatternWeight = 0.5
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "schedule" },
                        OptionalKeywords = new[] { "task", "reminder", "appointment" },
                        RegexPatterns = new[] { @"schedule.*?(task|reminder|appointment)" },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.4,
                        PatternWeight = 0.6
                    }
                },

                [UserIntent.ViewTasks] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "show", "tasks" },
                        OptionalKeywords = new[] { "list", "my", "view", "display", "see" },
                        RegexPatterns = new[] { @"(show|list|view|display)\s+.*?(tasks|reminders|todos)" },
                        MinConfidence = 0.4,
                        KeywordWeight = 0.5,
                        PatternWeight = 0.5
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "what", "tasks" },
                        OptionalKeywords = new[] { "do", "i", "have", "my" },
                        RegexPatterns = new[] { @"what.*?tasks.*?(do i have|are there)" },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.4,
                        PatternWeight = 0.6
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "my", "reminders" },
                        OptionalKeywords = new[] { "show", "list", "view" },
                        RegexPatterns = new[] { @"(my|check)\s+reminders" },
                        MinConfidence = 0.4,
                        KeywordWeight = 0.5,
                        PatternWeight = 0.5
                    }
                },

                [UserIntent.CompleteTask] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "complete", "task" },
                        OptionalKeywords = new[] { "mark", "done", "finished", "finish" },
                        RegexPatterns = new[] { @"(complete|mark.*?complete|done|finished?)\s+.*?task" },
                        MinConfidence = 0.4,
                        KeywordWeight = 0.5,
                        PatternWeight = 0.5
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "done" },
                        OptionalKeywords = new[] { "with", "task", "reminder" },
                        RegexPatterns = new[] { @"(done|finished?)\s+with" },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.4,
                        PatternWeight = 0.6
                    }
                },

                [UserIntent.StartQuiz] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "start", "quiz" },
                        OptionalKeywords = new[] { "take", "begin", "play", "game" },
                        RegexPatterns = new[] { @"(start|take|begin|play)\s+.*?(quiz|test|game)" },
                        MinConfidence = 0.4,
                        KeywordWeight = 0.5,
                        PatternWeight = 0.5
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "quiz" },
                        OptionalKeywords = new[] { "take", "do", "try" },
                        RegexPatterns = new[] { @"(take|do|try)\s+.*?quiz" },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.4,
                        PatternWeight = 0.6
                    }
                },

                [UserIntent.QuickQuestion] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "quick", "question" },
                        OptionalKeywords = new[] { "random", "single", "one" },
                        RegexPatterns = new[] { @"(quick|random|single)\s+question" },
                        MinConfidence = 0.5,
                        KeywordWeight = 0.6,
                        PatternWeight = 0.4
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "question" },
                        OptionalKeywords = new[] { "ask", "me", "give", "random" },
                        RegexPatterns = new[] { @"(ask|give)\s+me\s+.*?question" },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.4,
                        PatternWeight = 0.6
                    }
                },

                [UserIntent.GetCybersecurityAdvice] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "what", "is" },
                        OptionalKeywords = new[] { "password", "phishing", "malware", "2fa", "vpn", "security" },
                        RegexPatterns = new[] { @"what\s+is\s+(password|phishing|malware|2fa|vpn|security)" },
                        MinConfidence = 0.4,
                        KeywordWeight = 0.3,
                        PatternWeight = 0.7
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "how" },
                        OptionalKeywords = new[] { "secure", "protect", "safe", "password", "account" },
                        RegexPatterns = new[] { @"how\s+(do i|can i|to)\s+(secure|protect|make.*?safe)" },
                        MinConfidence = 0.3,
                        KeywordWeight = 0.3,
                        PatternWeight = 0.7
                    }
                },

                [UserIntent.Greeting] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "hello" },
                        OptionalKeywords = new[] { "hi", "hey", "good", "morning", "afternoon", "evening" },
                        RegexPatterns = new[] { @"^(hello|hi|hey|good\s+(morning|afternoon|evening))" },
                        MinConfidence = 0.6,
                        KeywordWeight = 0.7,
                        PatternWeight = 0.3
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "hi" },
                        OptionalKeywords = new[] { "there", "hello" },
                        RegexPatterns = new[] { @"^(hi|hey)" },
                        MinConfidence = 0.5,
                        KeywordWeight = 0.8,
                        PatternWeight = 0.2
                    }
                },

                [UserIntent.GetHelp] = new List<IntentPattern>
                {
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "help" },
                        OptionalKeywords = new[] { "me", "need", "can", "how" },
                        RegexPatterns = new[] { @"(help|assistance|support)" },
                        MinConfidence = 0.4,
                        KeywordWeight = 0.6,
                        PatternWeight = 0.4
                    },
                    new IntentPattern
                    {
                        RequiredKeywords = new[] { "what", "can" },
                        OptionalKeywords = new[] { "you", "do", "help" },
                        RegexPatterns = new[] { @"what\s+can\s+you\s+do" },
                        MinConfidence = 0.5,
                        KeywordWeight = 0.4,
                        PatternWeight = 0.6
                    }
                }
            };
        }
    }

    public class IntentPattern
    {
        public string[] RequiredKeywords { get; set; } = Array.Empty<string>();
        public string[] OptionalKeywords { get; set; } = Array.Empty<string>();
        public string[] RegexPatterns { get; set; } = Array.Empty<string>();
        public string[] NegativeIndicators { get; set; } = Array.Empty<string>();
        public Dictionary<string, string> EntityPatterns { get; set; } = new Dictionary<string, string>();
        public double MinConfidence { get; set; } = 0.3;
        public double KeywordWeight { get; set; } = 0.4;
        public double PatternWeight { get; set; } = 0.6;
        public double OptionalKeywordBoost { get; set; } = 0.1;
        public double NegativePenalty { get; set; } = 0.3;
    }

    public class IntentResult
    {
        public UserIntent Intent { get; }
        public double Confidence { get; }
        public Dictionary<string, string> Entities { get; }

        public IntentResult(UserIntent intent, double confidence, Dictionary<string, string> entities)
        {
            Intent = intent;
            Confidence = confidence;
            Entities = entities ?? new Dictionary<string, string>();
        }

        public bool HasEntity(string entityName)
        {
            return Entities.ContainsKey(entityName) && !string.IsNullOrWhiteSpace(Entities[entityName]);
        }

        public string GetEntity(string entityName, string defaultValue = "")
        {
            return HasEntity(entityName) ? Entities[entityName] : defaultValue;
        }
    }
}