using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickLook.Plugin.ArchiveViewer
{
    public class ArchiveFileEntry : IComparable<ArchiveFileEntry>
    {
        private readonly ArchiveFileEntry _parent;
        private int _cachedDepth = -1;
        private int _cachedLevel = -1;

        public ArchiveFileEntry(string name, bool isFolder, ArchiveFileEntry parent = null)
        {
            Name = name;
            IsFolder = isFolder;

            _parent = parent;
            _parent?.Children.Add(this, false);
        }

        public SortedList<ArchiveFileEntry, bool> Children { get; set; } = new SortedList<ArchiveFileEntry, bool>();

        public string Name { get; set; }
        public bool Encrypted { get; set; }
        public bool IsFolder { get; set; }
        public ulong Size { get; set; }
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        ///     Returns the maximum depth of all siblings
        /// </summary>
        public int Level
        {
            get
            {
                if (_cachedLevel != -1)
                    return _cachedLevel;

                if (_parent == null)
                    _cachedLevel = GetDepth();
                else
                    _cachedLevel = _parent.Level - 1;

                return _cachedLevel;
            }
        }

        public int CompareTo(ArchiveFileEntry other)
        {
            if (IsFolder == other.IsFolder)
                return string.Compare(Name, other.Name, StringComparison.CurrentCulture);

            if (IsFolder)
                return -1;

            return 1;
        }

        /// <summary>
        ///     Returns the number of nodes in the longest path to a leaf
        /// </summary>
        private int GetDepth()
        {
            if (_cachedDepth != -1)
                return _cachedDepth;

            var max = Children.Keys.Count == 0 ? 0 : Children.Keys.Max(r => r.GetDepth());
            _cachedDepth = max + 1;
            return _cachedDepth;
        }

        public override string ToString()
        {
            if (IsFolder)
                return $"{Name}";

            var en = Encrypted ? "*" : "";
            return $"{Name}{en},{IsFolder},{Size},{ModifiedDate}";
        }
    }
}