using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Encoding = Portable.Text.Encoding;

namespace Mono.Web.Util
{

    public class HttpEncoder
    {

        private static HttpEncoder currentEncoder;
        private static Lazy<HttpEncoder> currentEncoderLazy;
        private static Lazy<HttpEncoder> defaultEncoder;
        private static SortedDictionary<string, char> entities;
        private static object entitiesLock = new object();
        private static char[] hexChars = "0123456789abcdef".ToCharArray();

        static HttpEncoder()
        {
            defaultEncoder = new Lazy<HttpEncoder>(() => new HttpEncoder());
            currentEncoderLazy = new Lazy<HttpEncoder>(GetCustomEncoderFromConfig);
        }

        private static string EncodeHeaderString(string input)
        {
            StringBuilder sb = null;
            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                if (((ch < ' ') && (ch != '\t')) || (ch == '\x007f'))
                {
                    StringBuilderAppend(string.Format("%{0:x2}", (int) ch), ref sb);
                }
            }
            if (sb != null)
            {
                return sb.ToString();
            }
            return input;
        }

        private static HttpEncoder GetCustomEncoderFromConfig()
        {
            return defaultEncoder.Value;
        }

        protected internal virtual void HeaderNameValueEncode(string headerName, string headerValue, out string encodedHeaderName, out string encodedHeaderValue)
        {
            if (string.IsNullOrEmpty(headerName))
            {
                encodedHeaderName = headerName;
            }
            else
            {
                encodedHeaderName = EncodeHeaderString(headerName);
            }
            if (string.IsNullOrEmpty(headerValue))
            {
                encodedHeaderValue = headerValue;
            }
            else
            {
                encodedHeaderValue = EncodeHeaderString(headerValue);
            }
        }

        internal static string HtmlAttributeEncode(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            bool flag = false;
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '&':
                    case '"':
                    case '<':
                    case '\'':
                        flag = true;
                        goto Label_0041;
                }
            }
        Label_0041:
            if (!flag)
            {
                return s;
            }
            StringBuilder builder = new StringBuilder();
            int length = s.Length;
            for (int j = 0; j < length; j++)
            {
                switch (s[j])
                {
                    case '&':
                        builder.Append("&amp;");
                        break;

                    case '\'':
                        builder.Append("&#39;");
                        break;

                    case '<':
                        builder.Append("&lt;");
                        break;

                    case '"':
                        builder.Append("&quot;");
                        break;

                    default:
                        builder.Append(s[j]);
                        break;
                }
            }
            return builder.ToString();
        }

        protected internal virtual void HtmlAttributeEncode(string value, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            if (!string.IsNullOrEmpty(value))
            {
                output.Write(HtmlAttributeEncode(value));
            }
        }

        internal static string HtmlDecode(string s)
        {
            if (s == null)
            {
                return null;
            }
            if (s.Length == 0)
            {
                return string.Empty;
            }
            if (s.IndexOf('&') == -1)
            {
                return s;
            }
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            StringBuilder builder3 = new StringBuilder();
            int length = s.Length;
            int num2 = 0;
            int num3 = 0;
            bool flag = false;
            bool flag2 = false;
            for (int i = 0; i < length; i++)
            {
                char ch = s[i];
                if (num2 == 0)
                {
                    if (ch == '&')
                    {
                        builder2.Append(ch);
                        builder.Append(ch);
                        num2 = 1;
                    }
                    else
                    {
                        builder3.Append(ch);
                    }
                }
                else if (ch == '&')
                {
                    num2 = 1;
                    if (flag2)
                    {
                        builder2.Append(num3.ToString(Helpers.InvariantCulture));
                        flag2 = false;
                    }
                    builder3.Append(builder2.ToString());
                    builder2.Length = 0;
                    builder2.Append('&');
                }
                else
                {
                    switch (num2)
                    {
                        case 1:
                            if (ch == ';')
                            {
                                num2 = 0;
                                builder3.Append(builder2.ToString());
                                builder3.Append(ch);
                                builder2.Length = 0;
                            }
                            else
                            {
                                num3 = 0;
                                flag = false;
                                if (ch != '#')
                                {
                                    num2 = 2;
                                }
                                else
                                {
                                    num2 = 3;
                                }
                                builder2.Append(ch);
                                builder.Append(ch);
                            }
                            break;

                        case 2:
                            builder2.Append(ch);
                            if (ch == ';')
                            {
                                string str = builder2.ToString();
                                if ((str.Length > 1) && Entities.ContainsKey(str.Substring(1, str.Length - 2)))
                                {
                                    str = Entities[str.Substring(1, str.Length - 2)].ToString();
                                }
                                builder3.Append(str);
                                num2 = 0;
                                builder2.Length = 0;
                                builder.Length = 0;
                            }
                            break;

                        case 3:
                            if (ch == ';')
                            {
                                if (num3 == 0)
                                {
                                    builder3.Append(builder.ToString() + ";");
                                }
                                else if (num3 > 0xffff)
                                {
                                    builder3.Append("&#");
                                    builder3.Append(num3.ToString(Helpers.InvariantCulture));
                                    builder3.Append(";");
                                }
                                else
                                {
                                    builder3.Append((char) num3);
                                }
                                num2 = 0;
                                builder2.Length = 0;
                                builder.Length = 0;
                                flag2 = false;
                            }
                            else if (flag && IsHexDigit(ch))
                            {
                                num3 = (num3 * 0x10) + FromHex(ch);
                                flag2 = true;
                                builder.Append(ch);
                            }
                            else if (char.IsDigit(ch))
                            {
                                num3 = (num3 * 10) + (ch - '0');
                                flag2 = true;
                                builder.Append(ch);
                            }
                            else if ((num3 == 0) && ((ch == 'x') || (ch == 'X')))
                            {
                                flag = true;
                                builder.Append(ch);
                            }
                            else
                            {
                                num2 = 2;
                                if (flag2)
                                {
                                    builder2.Append(num3.ToString(Helpers.InvariantCulture));
                                    flag2 = false;
                                }
                                builder2.Append(ch);
                            }
                            break;
                    }
                }
            }
            if (builder2.Length > 0)
            {
                builder3.Append(builder2.ToString());
            }
            else if (flag2)
            {
                builder3.Append(num3.ToString(Helpers.InvariantCulture));
            }
            return builder3.ToString();
        }

        protected internal virtual void HtmlDecode(string value, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.Write(HtmlDecode(value));
        }

        internal static string HtmlEncode(string s)
        {
            if (s == null)
            {
                return null;
            }
            if (s.Length == 0)
            {
                return string.Empty;
            }
            bool flag = false;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if ((((ch == '&') || (ch == '"')) || ((ch == '<') || (ch == '>'))) || ((ch > '\x009f') || (ch == '\'')))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                return s;
            }
            StringBuilder builder = new StringBuilder();
            int length = s.Length;
            for (int j = 0; j < length; j++)
            {
                switch (s[j])
                {
                    case '&':
                    {
                        builder.Append("&amp;");
                        continue;
                    }
                    case '\'':
                    {
                        builder.Append("&#39;");
                        continue;
                    }
                    case '"':
                    {
                        builder.Append("&quot;");
                        continue;
                    }
                    case '<':
                    {
                        builder.Append("&lt;");
                        continue;
                    }
                    case '>':
                    {
                        builder.Append("&gt;");
                        continue;
                    }
                    case (char)0xff1c:
                    {
                        builder.Append("&#65308;");
                        continue;
                    }
                    case (char)0xff1e:
                    {
                        builder.Append("&#65310;");
                        continue;
                    }
                }
                char ch2 = s[j];
                if ((ch2 > '\x009f') && (ch2 < 'Ā'))
                {
                    builder.Append("&#");
                    builder.Append(((int) ch2).ToString(Helpers.InvariantCulture));
                    builder.Append(";");
                }
                else
                {
                    builder.Append(ch2);
                }
            }
            return builder.ToString();
        }

        protected internal virtual void HtmlEncode(string value, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            output.Write(HtmlEncode(value));
        }

        private static void InitEntities()
        {
            entities = new SortedDictionary<string, char>(StringComparer.Ordinal);
            entities.Add("nbsp", '\x00a0');
            entities.Add("iexcl", '\x00a1');
            entities.Add("cent", '\x00a2');
            entities.Add("pound", '\x00a3');
            entities.Add("curren", '\x00a4');
            entities.Add("yen", '\x00a5');
            entities.Add("brvbar", '\x00a6');
            entities.Add("sect", '\x00a7');
            entities.Add("uml", '\x00a8');
            entities.Add("copy", '\x00a9');
            entities.Add("ordf", '\x00aa');
            entities.Add("laquo", '\x00ab');
            entities.Add("not", '\x00ac');
            entities.Add("shy", '\x00ad');
            entities.Add("reg", '\x00ae');
            entities.Add("macr", '\x00af');
            entities.Add("deg", '\x00b0');
            entities.Add("plusmn", '\x00b1');
            entities.Add("sup2", '\x00b2');
            entities.Add("sup3", '\x00b3');
            entities.Add("acute", '\x00b4');
            entities.Add("micro", '\x00b5');
            entities.Add("para", '\x00b6');
            entities.Add("middot", '\x00b7');
            entities.Add("cedil", '\x00b8');
            entities.Add("sup1", '\x00b9');
            entities.Add("ordm", '\x00ba');
            entities.Add("raquo", '\x00bb');
            entities.Add("frac14", '\x00bc');
            entities.Add("frac12", '\x00bd');
            entities.Add("frac34", '\x00be');
            entities.Add("iquest", '\x00bf');
            entities.Add("Agrave", '\x00c0');
            entities.Add("Aacute", '\x00c1');
            entities.Add("Acirc", '\x00c2');
            entities.Add("Atilde", '\x00c3');
            entities.Add("Auml", '\x00c4');
            entities.Add("Aring", '\x00c5');
            entities.Add("AElig", '\x00c6');
            entities.Add("Ccedil", '\x00c7');
            entities.Add("Egrave", '\x00c8');
            entities.Add("Eacute", '\x00c9');
            entities.Add("Ecirc", '\x00ca');
            entities.Add("Euml", '\x00cb');
            entities.Add("Igrave", '\x00cc');
            entities.Add("Iacute", '\x00cd');
            entities.Add("Icirc", '\x00ce');
            entities.Add("Iuml", '\x00cf');
            entities.Add("ETH", '\x00d0');
            entities.Add("Ntilde", '\x00d1');
            entities.Add("Ograve", '\x00d2');
            entities.Add("Oacute", '\x00d3');
            entities.Add("Ocirc", '\x00d4');
            entities.Add("Otilde", '\x00d5');
            entities.Add("Ouml", '\x00d6');
            entities.Add("times", '\x00d7');
            entities.Add("Oslash", '\x00d8');
            entities.Add("Ugrave", '\x00d9');
            entities.Add("Uacute", '\x00da');
            entities.Add("Ucirc", '\x00db');
            entities.Add("Uuml", '\x00dc');
            entities.Add("Yacute", '\x00dd');
            entities.Add("THORN", '\x00de');
            entities.Add("szlig", '\x00df');
            entities.Add("agrave", '\x00e0');
            entities.Add("aacute", '\x00e1');
            entities.Add("acirc", '\x00e2');
            entities.Add("atilde", '\x00e3');
            entities.Add("auml", '\x00e4');
            entities.Add("aring", '\x00e5');
            entities.Add("aelig", '\x00e6');
            entities.Add("ccedil", '\x00e7');
            entities.Add("egrave", '\x00e8');
            entities.Add("eacute", '\x00e9');
            entities.Add("ecirc", '\x00ea');
            entities.Add("euml", '\x00eb');
            entities.Add("igrave", '\x00ec');
            entities.Add("iacute", '\x00ed');
            entities.Add("icirc", '\x00ee');
            entities.Add("iuml", '\x00ef');
            entities.Add("eth", '\x00f0');
            entities.Add("ntilde", '\x00f1');
            entities.Add("ograve", '\x00f2');
            entities.Add("oacute", '\x00f3');
            entities.Add("ocirc", '\x00f4');
            entities.Add("otilde", '\x00f5');
            entities.Add("ouml", '\x00f6');
            entities.Add("divide", '\x00f7');
            entities.Add("oslash", '\x00f8');
            entities.Add("ugrave", '\x00f9');
            entities.Add("uacute", '\x00fa');
            entities.Add("ucirc", '\x00fb');
            entities.Add("uuml", '\x00fc');
            entities.Add("yacute", '\x00fd');
            entities.Add("thorn", '\x00fe');
            entities.Add("yuml", '\x00ff');
            entities.Add("fnof", 'ƒ');
            entities.Add("Alpha", 'Α');
            entities.Add("Beta", 'Β');
            entities.Add("Gamma", 'Γ');
            entities.Add("Delta", 'Δ');
            entities.Add("Epsilon", 'Ε');
            entities.Add("Zeta", 'Ζ');
            entities.Add("Eta", 'Η');
            entities.Add("Theta", 'Θ');
            entities.Add("Iota", 'Ι');
            entities.Add("Kappa", 'Κ');
            entities.Add("Lambda", 'Λ');
            entities.Add("Mu", 'Μ');
            entities.Add("Nu", 'Ν');
            entities.Add("Xi", 'Ξ');
            entities.Add("Omicron", 'Ο');
            entities.Add("Pi", 'Π');
            entities.Add("Rho", 'Ρ');
            entities.Add("Sigma", 'Σ');
            entities.Add("Tau", 'Τ');
            entities.Add("Upsilon", 'Υ');
            entities.Add("Phi", 'Φ');
            entities.Add("Chi", 'Χ');
            entities.Add("Psi", 'Ψ');
            entities.Add("Omega", 'Ω');
            entities.Add("alpha", 'α');
            entities.Add("beta", 'β');
            entities.Add("gamma", 'γ');
            entities.Add("delta", 'δ');
            entities.Add("epsilon", 'ε');
            entities.Add("zeta", 'ζ');
            entities.Add("eta", 'η');
            entities.Add("theta", 'θ');
            entities.Add("iota", 'ι');
            entities.Add("kappa", 'κ');
            entities.Add("lambda", 'λ');
            entities.Add("mu", 'μ');
            entities.Add("nu", 'ν');
            entities.Add("xi", 'ξ');
            entities.Add("omicron", 'ο');
            entities.Add("pi", 'π');
            entities.Add("rho", 'ρ');
            entities.Add("sigmaf", 'ς');
            entities.Add("sigma", 'σ');
            entities.Add("tau", 'τ');
            entities.Add("upsilon", 'υ');
            entities.Add("phi", 'φ');
            entities.Add("chi", 'χ');
            entities.Add("psi", 'ψ');
            entities.Add("omega", 'ω');
            entities.Add("thetasym", 'ϑ');
            entities.Add("upsih", 'ϒ');
            entities.Add("piv", 'ϖ');
            entities.Add("bull", '•');
            entities.Add("hellip", '…');
            entities.Add("prime", '′');
            entities.Add("Prime", '″');
            entities.Add("oline", '‾');
            entities.Add("frasl", '⁄');
            entities.Add("weierp", '℘');
            entities.Add("image", 'ℑ');
            entities.Add("real", 'ℜ');
            entities.Add("trade", '™');
            entities.Add("alefsym", 'ℵ');
            entities.Add("larr", '←');
            entities.Add("uarr", '↑');
            entities.Add("rarr", '→');
            entities.Add("darr", '↓');
            entities.Add("harr", '↔');
            entities.Add("crarr", '↵');
            entities.Add("lArr", '⇐');
            entities.Add("uArr", '⇑');
            entities.Add("rArr", '⇒');
            entities.Add("dArr", '⇓');
            entities.Add("hArr", '⇔');
            entities.Add("forall", '∀');
            entities.Add("part", '∂');
            entities.Add("exist", '∃');
            entities.Add("empty", '∅');
            entities.Add("nabla", '∇');
            entities.Add("isin", '∈');
            entities.Add("notin", '∉');
            entities.Add("ni", '∋');
            entities.Add("prod", '∏');
            entities.Add("sum", '∑');
            entities.Add("minus", '−');
            entities.Add("lowast", '∗');
            entities.Add("radic", '√');
            entities.Add("prop", '∝');
            entities.Add("infin", '∞');
            entities.Add("ang", '∠');
            entities.Add("and", '∧');
            entities.Add("or", '∨');
            entities.Add("cap", '∩');
            entities.Add("cup", '∪');
            entities.Add("int", '∫');
            entities.Add("there4", '∴');
            entities.Add("sim", '∼');
            entities.Add("cong", '≅');
            entities.Add("asymp", '≈');
            entities.Add("ne", '≠');
            entities.Add("equiv", '≡');
            entities.Add("le", '≤');
            entities.Add("ge", '≥');
            entities.Add("sub", '⊂');
            entities.Add("sup", '⊃');
            entities.Add("nsub", '⊄');
            entities.Add("sube", '⊆');
            entities.Add("supe", '⊇');
            entities.Add("oplus", '⊕');
            entities.Add("otimes", '⊗');
            entities.Add("perp", '⊥');
            entities.Add("sdot", '⋅');
            entities.Add("lceil", '⌈');
            entities.Add("rceil", '⌉');
            entities.Add("lfloor", '⌊');
            entities.Add("rfloor", '⌋');
            entities.Add("lang", '〈');
            entities.Add("rang", '〉');
            entities.Add("loz", '◊');
            entities.Add("spades", '♠');
            entities.Add("clubs", '♣');
            entities.Add("hearts", '♥');
            entities.Add("diams", '♦');
            entities.Add("quot", '"');
            entities.Add("amp", '&');
            entities.Add("lt", '<');
            entities.Add("gt", '>');
            entities.Add("OElig", 'Œ');
            entities.Add("oelig", 'œ');
            entities.Add("Scaron", 'Š');
            entities.Add("scaron", 'š');
            entities.Add("Yuml", 'Ÿ');
            entities.Add("circ", 'ˆ');
            entities.Add("tilde", '˜');
            entities.Add("ensp", ' ');
            entities.Add("emsp", ' ');
            entities.Add("thinsp", ' ');
            entities.Add("zwnj", '‌');
            entities.Add("zwj", '‍');
            entities.Add("lrm", '‎');
            entities.Add("rlm", '‏');
            entities.Add("ndash", '–');
            entities.Add("mdash", '—');
            entities.Add("lsquo", '‘');
            entities.Add("rsquo", '’');
            entities.Add("sbquo", '‚');
            entities.Add("ldquo", '“');
            entities.Add("rdquo", '”');
            entities.Add("bdquo", '„');
            entities.Add("dagger", '†');
            entities.Add("Dagger", '‡');
            entities.Add("permil", '‰');
            entities.Add("lsaquo", '‹');
            entities.Add("rsaquo", '›');
            entities.Add("euro", '€');
        }

        internal static bool NotEncoded(char c)
        {
            if ((((c != '!') && (c != '(')) && ((c != ')') && (c != '*'))) && ((c != '-') && (c != '.')))
            {
                return (c == '_');
            }
            return true;
        }

        private static void StringBuilderAppend(string s, ref StringBuilder sb)
        {
            if (sb == null)
            {
                sb = new StringBuilder(s);
            }
            else
            {
                sb.Append(s);
            }
        }

        protected internal virtual byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            return UrlEncodeToBytes(bytes, offset, count);
        }

        internal static void UrlEncodeChar(char c, Stream result, bool isUnicode)
        {
            if (c > '\x00ff')
            {
                int num2 = c;
                result.WriteByte(0x25);
                result.WriteByte(0x75);
                int index = num2 >> 12;
                result.WriteByte((byte) hexChars[index]);
                index = (num2 >> 8) & 15;
                result.WriteByte((byte) hexChars[index]);
                index = (num2 >> 4) & 15;
                result.WriteByte((byte) hexChars[index]);
                index = num2 & 15;
                result.WriteByte((byte) hexChars[index]);
            }
            else if ((c > ' ') && NotEncoded(c))
            {
                result.WriteByte((byte) c);
            }
            else if (c == ' ')
            {
                result.WriteByte(0x2b);
            }
            else if ((((c < '0') || ((c < 'A') && (c > '9'))) || ((c > 'Z') && (c < 'a'))) || (c > 'z'))
            {
                if (isUnicode && (c > '\x007f'))
                {
                    result.WriteByte(0x25);
                    result.WriteByte(0x75);
                    result.WriteByte(0x30);
                    result.WriteByte(0x30);
                }
                else
                {
                    result.WriteByte(0x25);
                }
                int num3 = c >> 4;
                result.WriteByte((byte) hexChars[num3]);
                num3 = c & '\x000f';
                result.WriteByte((byte) hexChars[num3]);
            }
            else
            {
                result.WriteByte((byte) c);
            }
        }

        internal static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            int length = bytes.Length;
            if (length == 0)
            {
                return new byte[0];
            }
            if ((offset < 0) || (offset >= length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || (count > (length - offset)))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            MemoryStream result = new MemoryStream(count);
            int num2 = offset + count;
            for (int i = offset; i < num2; i++)
            {
                UrlEncodeChar((char) bytes[i], result, false);
            }
            return result.ToArray();
        }

        protected internal virtual string UrlPathEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            MemoryStream result = new MemoryStream();
            int length = value.Length;
            for (int i = 0; i < length; i++)
            {
                UrlPathEncodeChar(value[i], result);
            }
            return Encoding.ASCII.GetString(result.ToArray());
        }

        internal static void UrlPathEncodeChar(char c, Stream result)
        {
            if ((c < '!') || (c > '~'))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
                for (int i = 0; i < bytes.Length; i++)
                {
                    result.WriteByte(0x25);
                    int index = bytes[i] >> 4;
                    result.WriteByte((byte) hexChars[index]);
                    index = bytes[i] & 15;
                    result.WriteByte((byte) hexChars[index]);
                }
            }
            else if (c == ' ')
            {
                result.WriteByte(0x25);
                result.WriteByte(50);
                result.WriteByte(0x30);
            }
            else
            {
                result.WriteByte((byte) c);
            }
        }

        public static HttpEncoder Current
        {
            get
            {
                if (currentEncoder == null)
                {
                    currentEncoder = currentEncoderLazy.Value;
                }
                return currentEncoder;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                currentEncoder = value;
            }
        }

        public static HttpEncoder Default
        {
            get
            {
                return defaultEncoder.Value;
            }
        }

        private static IDictionary<string, char> Entities
        {
            get
            {
                lock (entitiesLock)
                {
                    if (entities == null)
                    {
                        InitEntities();
                    }
                    return entities;
                }
            }
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static byte FromHex(char c)
        {
            return (byte)(char.ToLower(c) - '0');
        }

    }

}