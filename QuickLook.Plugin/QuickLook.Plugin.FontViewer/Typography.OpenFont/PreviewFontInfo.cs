//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev


using Typography.OpenFont.Tables;

namespace Typography.OpenFont
{
    public class PreviewFontInfo
    {
        public readonly string Name;
        public readonly string SubFamilyName;
        public readonly Extensions.TranslatedOS2FontStyle OS2TranslatedStyle;
        public readonly Extensions.OS2FsSelection OS2FsSelection;

        readonly PreviewFontInfo[] _ttcfMembers;

        public Languages Languages { get; }
        public NameEntry NameEntry { get; }
        public OS2Table OS2Table { get; }

        internal PreviewFontInfo(
            NameEntry nameEntry,
            OS2Table os2Table,
            Languages langs)
        {
            NameEntry = nameEntry;
            OS2Table = os2Table;
            Languages = langs;

            Name = nameEntry.FontName;
            SubFamilyName = nameEntry.FontSubFamily;
            OS2TranslatedStyle = Extensions.TypefaceExtensions.TranslateOS2FontStyle(os2Table);
            OS2FsSelection = Extensions.TypefaceExtensions.TranslateOS2FsSelection(os2Table);
        }
        internal PreviewFontInfo(string fontName, PreviewFontInfo[] ttcfMembers)
        {
            Name = fontName;
            SubFamilyName = "";
            _ttcfMembers = ttcfMembers;
            Languages = new Languages();
        }

        public string TypographicFamilyName => (NameEntry?.TypographicFamilyName) ?? string.Empty;
        public string TypographicSubFamilyName => (NameEntry?.TypographyicSubfamilyName) ?? string.Empty;
        public string PostScriptName => (NameEntry?.PostScriptName) ?? string.Empty;
        public string UniqueFontIden => (NameEntry?.UniqueFontIden) ?? string.Empty;
        public string VersionString => (NameEntry?.VersionString) ?? string.Empty;
        public ushort WeightClass => (OS2Table != null) ? OS2Table.usWeightClass : ushort.MinValue;
        public ushort WidthClass => (OS2Table != null) ? OS2Table.usWidthClass : ushort.MinValue;


        public int ActualStreamOffset { get; internal set; }
        public bool IsWebFont { get; internal set; }
        public bool IsFontCollection => _ttcfMembers != null;
        /// <summary>
        /// get font collection's member count
        /// </summary>
        public int MemberCount => _ttcfMembers.Length;
        /// <summary>
        /// get font collection's member
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public PreviewFontInfo GetMember(int index) => _ttcfMembers[index];
#if DEBUG
        public override string ToString()
        {
            return (IsFontCollection) ? Name : Name + ", " + SubFamilyName + ", " + OS2TranslatedStyle;
        }
#endif
    }


}