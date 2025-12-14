using System.Collections.Generic;

namespace MarcoZechner.Logging
{
    public sealed class LogConfig<TTopic> where TTopic : struct
    {
        // Severity routing (fixed by policy)
        public LogOutput WarningOutput = LogOutput.File;
        public LogOutput ErrorOutput = LogOutput.FileAndChat;

        // If true, warnings/errors bypass topic enablement (recommended)
        public bool AlwaysLogWarnings = true;
        public bool AlwaysLogErrors = true;

        // Default rule for Trace/Debug/Info if topic not configured
        public TopicRule DefaultRule = new TopicRule(
            enabled: false,
            maxDetail: 0,
            output: LogOutput.File
        );

        private readonly Dictionary<TTopic, TopicRule> _rules = new Dictionary<TTopic, TopicRule>();

        public TopicRule GetRule(TTopic topic)
        {
            TopicRule r;
            if (_rules.TryGetValue(topic, out r))
                return r;
            return DefaultRule;
        }

        public void SetRule(TTopic topic, bool enabled, int maxDetail, LogOutput output)
        {
            _rules[topic] = new TopicRule(enabled, maxDetail, output);
        }

        public void DisableTopic(TTopic topic)
        {
            _rules[topic] = new TopicRule(false, 0, LogOutput.File);
        }
    }
}