//Apache2, 2016-present, WinterDev
using System.Collections.Generic;
namespace Typography.OpenFont
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/featurelist

    public sealed class FeatureInfo
    {
        public readonly string fullname;
        public readonly string shortname;
        public FeatureInfo(string fullname, string shortname)
        {
            this.fullname = fullname;
            this.shortname = shortname;
        }
    }
    public static class Features
    {
        static readonly Dictionary<string, FeatureInfo> s_features = new Dictionary<string, FeatureInfo>();

        // 
        public static readonly FeatureInfo
            aalt = _("aalt", "Access All Alternates"),
            abvf = _("abvf", "Above-base Forms"),
            abvm = _("abvm", "Above-base Mark Positioning"),
            abvs = _("abvs", "Above-base Substitutions"),
            afrc = _("afrc", "Alternative Fractions"),
            akhn = _("akhn", "Akhands"),
            //
            blwf = _("blwf", "Below-base Forms"),
            blwm = _("blwm", "Below-base Mark Positioning"),
            blws = _("blws", "Below-base Substitutions"),
            //
            calt = _("calt", "Access All Alternates"),
            case_ = _("case", "Above-base Forms"),
            ccmp = _("ccmp", "Glyph Composition / Decomposition"),
            cfar = _("cfar", "Conjunct Form After Ro"),
            cjct = _("cjct", "Conjunct Forms"),
            clig = _("clig", "Contextual Ligatures"),
            cpct = _("cpct", "Centered CJK Punctuation"),
            cpsp = _("cpsp", "Capital Spacing"),
            cswh = _("cswh", "Contextual Swash"),
            curs = _("curs", "Cursive Positioning"),
            c2pc = _("c2pc", "Petite Capitals From Capitals"),
            c2sc = _("c2sc", "Small Capitals From Capitals"),
            //
            dist = _("dist", "Distances"),
            dlig = _("dlig", "Discretionary Ligatures"),
            dnom = _("dnom", "Denominators"),
            dtls = _("dtls", "Dotless Forms"),
            //
            expt = _("expt", "Expert Forms"),
            //
            falt = _("falt", "Final Glyph on Line Alternates"),
            fin2 = _("fin2", "Terminal Forms #2"),
            fin3 = _("fin3", "Terminal Forms #3"),
            fina = _("fina", "Terminal Forms"),
            flac = _("flac", "Flattened accent forms"),
            frac = _("frac", "Fractions"),
            fwid = _("fwid", "Full Widths"),
            //
            half = _("half", "Half Forms"),
            haln = _("haln", "Halant Forms"),
            halt = _("halt", "Alternate Half Widths"),
            hist = _("hist", "Historical Forms"),
            hkna = _("hkna", "Horizontal Kana Alternates"),
            hlig = _("hlig", "Historical Ligatures"),
            hngl = _("hngl", "Hangul"),
            hojo = _("hojo", "Hojo Kanji Forms (JIS X 0212-1990 Kanji Forms)"),
            hwid = _("hwid", "Half Widths"),
            //
            init = _("init", "Initial Forms"),
            isol = _("isol", "Isolated Forms"),
            ital = _("ital", "Italics"),

            jalt = _("jalt", "Justification Alternates"),
            jp78 = _("jp78", "JIS78 Forms"),
            jp83 = _("jp83", "JIS83 Forms"),
            jp90 = _("jp90", "JIS90 Forms"),
            jp04 = _("jp04", "JIS2004 Forms"),
            //
            kern = _("kern", "Kerning"),
            // 
            lfbd = _("lfbd", "Left Bounds"),
            liga = _("liga", "Standard Ligatures"),
            ljmo = _("ljmo", "Leading Jamo Forms"),
            lnum = _("lnum", "Lining Figures"),
            locl = _("locl", "Localized Forms"),
            ltra = _("ltra", "Left-to-right alternates"),
            ltrm = _("ltrm", "Left-to-right mirrored forms"),
            //
            mark = _("mark", "Mark Positioning"),
            med2 = _("med2", "Medial Forms #2"),
            medi = _("medi", "Medial Forms"),
            mgrk = _("mgrk", "Mathematical Greek"),
            mkmk = _("mkmk", "Mark to Mark Positioning"),
            mset = _("mset", "Mark Positioning via Substitution"),
            //
            nalt = _("nalt", "Alternate Annotation Forms"),
            nlck = _("nlck", "NLC Kanji Forms"),
            nukt = _("nukt", "Nukta Forms"),
            numr = _("numr", "Numerators"),
            //
            onum = _("onum", "Oldstyle Figures"),
            opbd = _("opbd", "Optical Bounds"),
            ordn = _("ordn", "Ordinals"),
            ornm = _("ornm", "Ornaments"),
            //
            palt = _("palt", "Proportional Alternate Widths"),
            pcap = _("pcap", "Petite Capitals"),
            pkna = _("pkna", "Proportional Kana"),
            pnum = _("pnum", "Proportional Figures"),
            pref = _("pref", "Pre-Base Forms"),
            pres = _("pres", "Pre-base Substitutions"),
            pstf = _("pstf", "Post-base Forms"),
            psts = _("psts", "Post-base Substitutions"),
            pwid = _("pwid", "Proportional Widths"),
            //
            qwid = _("qwid", "Quarter Widths"),
            //
            rand = _("rand", "Randomize"),
            rclt = _("rclt", "Required Contextual Alternates"),
            rkrf = _("rkrf", "Rakar Forms"),
            rlig = _("rlig", "Required Ligatures"),
            rphf = _("rphf", "Reph Forms"),
            rtbd = _("rtbd", "Right Bounds"),
            rtla = _("rtla", "Right-to-left alternates"),
            rtlm = _("rtlm", "Right-to-left mirrored forms"),
            ruby = _("ruby", "Ruby Notation Forms"),
            rvrn = _("rvrn", "Required Variation Alternates"),
            // 
            salt = _("salt", "Stylistic Alternates"),
            sinf = _("sinf", "Scientific Inferiors"),
            size = _("size", "Optical size"),
            smcp = _("smcp", "Small Capitals"),
            smpl = _("smpl", "Simplified Forms"),

            ssty = _("ssty", "Math script style alternates"),
            stch = _("stch", "Stretching Glyph Decomposition"),
            subs = _("subs", "Subscript"),
            sups = _("sups", "Superscript"),
            swsh = _("swsh", "Swash"),
            //
            titl = _("titl", "Titling"),
            tjmo = _("tjmo", "Trailing Jamo Forms"),
            tnam = _("tnam", "Traditional Name Forms"),
            tnum = _("tnum", "Tabular Figures"),
            trad = _("trad", "Traditional Forms"),
            twid = _("twid", "Third Widths"),
            //
            unic = _("unic", "Unicase"),
            //
            valt = _("valt", "Alternate Vertical Metrics"),
            vatu = _("vatu", "Vattu Variants"),
            vert = _("vert", "Vertical Writing"),
            vhal = _("vhal", "Alternate Vertical Half Metrics"),
            vjmo = _("vjmo", "Vowel Jamo Forms"),
            vkna = _("vkna", "Vertical Kana Alternates"),
            vkrn = _("vkrn", "Vertical Kerning"),
            vpal = _("vpal", "Proportional Alternate Vertical Metrics"),
            vrt2 = _("vrt2", "Vertical Alternates and Rotation"),
            vrtr = _("vrtr", "Vertical Alternates for Rotation")
            ;

        static Features()
        {

            //
            for (int i = 1; i < 9; ++i)
            {
                _("cv0" + i, "Character Variants" + i);
            }
            for (int i = 10; i < 100; ++i)
            {
                _("cv" + i, "Character Variants" + i);
            }
            //
            for (int i = 1; i < 9; ++i)
            {
                _("ss0" + i, "Stylistic Set " + i);
            }
            for (int i = 10; i < 21; ++i)
            {
                _("ss" + i, "Stylistic Set " + i);
            }
        }

        static FeatureInfo _(string shortname, string fullname)
        {
            var featureInfo = new FeatureInfo(fullname, shortname);
            s_features.Add(shortname, featureInfo);
            return featureInfo;
        }
    }
}