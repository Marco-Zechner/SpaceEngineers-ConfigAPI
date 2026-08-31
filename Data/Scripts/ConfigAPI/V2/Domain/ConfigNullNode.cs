namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public sealed class ConfigNullNode : ConfigNode
    {
        public static readonly ConfigNullNode Instance = new ConfigNullNode();

        private ConfigNullNode()
        {
        }

        protected override bool EqualsNode(ConfigNode other)
        {
            return other is ConfigNullNode;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
