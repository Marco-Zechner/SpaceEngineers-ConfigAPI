using System;
using System.Globalization;

namespace mz.SemanticVersioning
{
    /// <summary>
    /// Represents a semantic version number with major, minor, and patch components.
    /// MAJOR: Incremented for incompatible API changes.
    /// MINOR: Incremented for added functionality in a <<< backwards-compatible >>> manner.
    /// PATCH: Incremented for <<< backwards-compatible >>> bug fixes.
    /// </summary>
    public class SemanticVersion
    {
        /// <summary>
        /// Gets the major version number.
        /// Indicates incompatible API changes.
        /// If Major is incremented, Minor and Patch should be reset to 0.
        /// If Major is 0, the API is considered <<< unstable >>>.
        /// </summary>
        public int Major { get; }
        /// <summary>
        /// Gets the minor version number.
        /// Indicates added functionality in a <<< backwards-compatible >>> manner.
        /// If Minor is incremented, Patch should be reset to 0.
        /// </summary>
        public int Minor { get; }
        /// <summary>
        /// Gets the patch version number.
        /// Indicates <<< backwards-compatible >>> bug fixes.
        /// </summary>
        public int Patch { get; }

        public SemanticVersion(int major, int minor, int patch)
        {
            if (major < 0 || minor < 0 || patch < 0)
                throw new FormatException("Version numbers must be non-negative integers.");
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public static implicit operator SemanticVersion(string s) => Parse(s);

        public static implicit operator string(SemanticVersion version) => version.ToString();

        public static SemanticVersion Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return new SemanticVersion(0, 0, 0);

            var parts = s.Split('.');
            int major = 0, minor = 0, patch = 0;

            if (parts.Length > 0)
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out major);
            if (parts.Length > 1)
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minor);
            if (parts.Length > 2)
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out patch);

            return new SemanticVersion(major, minor, patch);
        }

        public static bool TryParse(string s, out SemanticVersion v)
        {
            try
            {
                v = Parse(s);
                return true;
            }
            catch
            {
                v = new SemanticVersion(0, 0, 0);
                return false;
            }
        }

        public int CompareTo(SemanticVersion other)
        {
            int m = Major.CompareTo(other.Major);
            if (m != 0) return m;
            int n = Minor.CompareTo(other.Minor);
            if (n != 0) return n;
            return Patch.CompareTo(other.Patch);
        }

        #region Comparison Operators
        public static bool operator >(SemanticVersion left, SemanticVersion right)
        {
            if (left.Major != right.Major)
                return left.Major > right.Major;
            if (left.Minor != right.Minor)
                return left.Minor > right.Minor;
            return left.Patch > right.Patch;
        }

        public static bool operator <(SemanticVersion left, SemanticVersion right)
        {
            if (left.Major != right.Major)
                return left.Major < right.Major;
            if (left.Minor != right.Minor)
                return left.Minor < right.Minor;
            return left.Patch < right.Patch;
        }

        public static bool operator >=(SemanticVersion left, SemanticVersion right)
        {
            return left > right || left == right;
        }

        public static bool operator <=(SemanticVersion left, SemanticVersion right)
        {
            return left < right || left == right;
        }

        public static bool operator ==(SemanticVersion left, SemanticVersion right)
        {
            return left.Major == right.Major && left.Minor == right.Minor && left.Patch == right.Patch;
        }

        public static bool operator !=(SemanticVersion left, SemanticVersion right)
        {
            return !(left == right);
        }
        #endregion

        #region Overrides
        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public override bool Equals(object obj)
        {
            if (obj is SemanticVersion)
            {
                return this == (SemanticVersion)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        #endregion
    }
}