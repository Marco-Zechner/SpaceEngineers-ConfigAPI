using System;

namespace MarcoZechner.ConfigAPI.V2.Domain
{
    public abstract class ConfigNode : IEquatable<ConfigNode>
    {
        public bool Equals(ConfigNode other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return EqualsNode(other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConfigNode);
        }

        public abstract override int GetHashCode();

        protected abstract bool EqualsNode(ConfigNode other);
    }
}
