namespace MarcoZechner.Logging
{
    public sealed class TopicRule
    {
        public bool Enabled;
        public int MaxDetail;     // applies to Debug/Info; Trace ignores detail at call site
        public LogOutput Output;  // applies to Trace/Debug/Info (warnings/errors have global policy)

        public TopicRule(bool enabled, int maxDetail, LogOutput output)
        {
            Enabled = enabled;
            MaxDetail = maxDetail;
            Output = output;
        }
    }
}