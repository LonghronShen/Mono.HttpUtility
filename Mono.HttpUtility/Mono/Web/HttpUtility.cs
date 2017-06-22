using Mono.Web.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

//using Encoding = Portable.Text.Encoding;

namespace Mono.Web
{

    public sealed class HttpUtility
    {

        private static int GetChar(string str, int offset, int length)
        {
            int num = 0;
            int num2 = length + offset;
            for (int i = offset; i < num2; i++)
            {
                char ch = str[i];
                if (ch > '\x007f')
                {
                    return -1;
                }
                int @int = GetInt((byte) ch);
                if (@int == -1)
                {
                    return -1;
                }
                num = (num << 4) + @int;
            }
            return num;
        }

        private static int GetChar(byte[] bytes, int offset, int length)
        {
            int num = 0;
            int num2 = length + offset;
            for (int i = offset; i < num2; i++)
            {
                int @int = GetInt(bytes[i]);
                if (@int == -1)
                {
                    return -1;
                }
                num = (num << 4) + @int;
            }
            return num;
        }

        private static char[] GetChars(MemoryStream b, Encoding e)
        {
            var array = b.ToArray();
            return e.GetChars(array, 0, (int) b.Length);
        }

        private static int GetInt(byte b)
        {
            char ch = (char) b;
            if ((ch >= '0') && (ch <= '9'))
            {
                return (ch - '0');
            }
            if ((ch >= 'a') && (ch <= 'f'))
            {
                return ((ch - 'a') + 10);
            }
            if ((ch >= 'A') && (ch <= 'F'))
            {
                return ((ch - 'A') + 10);
            }
            return -1;
        }

        public static string HtmlAttributeEncode(string s)
        {
            if (s == null)
            {
                return null;
            }
            using (StringWriter writer = new StringWriter())
            {
                HttpEncoder.Current.HtmlAttributeEncode(s, writer);
                return writer.ToString();
            }
        }

        public static void HtmlAttributeEncode(string s, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            HttpEncoder.Current.HtmlAttributeEncode(s, output);
        }

        public static string HtmlDecode(string s)
        {
            if (s == null)
            {
                return null;
            }
            using (StringWriter writer = new StringWriter())
            {
                HttpEncoder.Current.HtmlDecode(s, writer);
                return writer.ToString();
            }
        }

        public static void HtmlDecode(string s, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            if (!string.IsNullOrEmpty(s))
            {
                HttpEncoder.Current.HtmlDecode(s, output);
            }
        }

        public static string HtmlEncode(object value)
        {
            if (value == null)
            {
                return null;
            }
            return HtmlEncode(value.ToString());
        }

        public static string HtmlEncode(string s)
        {
            if (s == null)
            {
                return null;
            }
            using (StringWriter writer = new StringWriter())
            {
                HttpEncoder.Current.HtmlEncode(s, writer);
                return writer.ToString();
            }
        }

        public static void HtmlEncode(string s, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            if (!string.IsNullOrEmpty(s))
            {
                HttpEncoder.Current.HtmlEncode(s, output);
            }
        }

        public static string JavaScriptStringEncode(string value)
        {
            return JavaScriptStringEncode(value, false);
        }

        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes)
        {
            char ch;
            if (string.IsNullOrEmpty(value))
            {
                if (!addDoubleQuotes)
                {
                    return string.Empty;
                }
                return "\"\"";
            }
            int length = value.Length;
            bool flag = false;
            for (int i = 0; i < length; i++)
            {
                ch = value[i];
                if (((ch >= '\0') && (ch <= '\x001f')) || (((ch == '"') || (ch == '\'')) || (((ch == '<') || (ch == '>')) || (ch == '\\'))))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                if (!addDoubleQuotes)
                {
                    return value;
                }
                return ("\"" + value + "\"");
            }
            StringBuilder builder = new StringBuilder();
            if (addDoubleQuotes)
            {
                builder.Append('"');
            }
            for (int j = 0; j < length; j++)
            {
                ch = value[j];
                if ((((ch >= '\0') && (ch <= '\a')) || ((ch == '\v') || ((ch >= '\x000e') && (ch <= '\x001f')))) || (((ch == '\'') || (ch == '<')) || (ch == '>')))
                {
                    builder.AppendFormat(@"\u{0:x4}", (int) ch);
                    continue;
                }
                switch (ch)
                {
                    case '\b':
                    {
                        builder.Append(@"\b");
                        continue;
                    }
                    case '\t':
                    {
                        builder.Append(@"\t");
                        continue;
                    }
                    case '\n':
                    {
                        builder.Append(@"\n");
                        continue;
                    }
                    case '\f':
                    {
                        builder.Append(@"\f");
                        continue;
                    }
                    case '\r':
                    {
                        builder.Append(@"\r");
                        continue;
                    }
                    case '"':
                    {
                        builder.Append("\\\"");
                        continue;
                    }
                    case '\\':
                    {
                        builder.Append(@"\\");
                        continue;
                    }
                }
                builder.Append(ch);
            }
            if (addDoubleQuotes)
            {
                builder.Append('"');
            }
            return builder.ToString();
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }

        public static NameValueCollection ParseQueryString(string query, Encoding encoding)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
            if ((query.Length == 0) || ((query.Length == 1) && (query[0] == '?')))
            {
                return new HttpQSCollection();
            }
            if (query[0] == '?')
            {
                query = query.Substring(1);
            }
            NameValueCollection result = new HttpQSCollection();
            ParseQueryString(query, encoding, result);
            return result;
        }

        internal static void ParseQueryString(string query, Encoding encoding, NameValueCollection result)
        {
            if (query.Length != 0)
            {
                string str = HtmlDecode(query);
                int length = str.Length;
                int startIndex = 0;
                bool flag = true;
                while (startIndex <= length)
                {
                    string str2;
                    int num3 = -1;
                    int num4 = -1;
                    for (int i = startIndex; i < length; i++)
                    {
                        if ((num3 == -1) && (str[i] == '='))
                        {
                            num3 = i + 1;
                        }
                        else if (str[i] == '&')
                        {
                            num4 = i;
                            break;
                        }
                    }
                    if (flag)
                    {
                        flag = false;
                        if (str[startIndex] == '?')
                        {
                            startIndex++;
                        }
                    }
                    if (num3 == -1)
                    {
                        str2 = null;
                        num3 = startIndex;
                    }
                    else
                    {
                        str2 = UrlDecode(str.Substring(startIndex, (num3 - startIndex) - 1), encoding);
                    }
                    if (num4 < 0)
                    {
                        startIndex = -1;
                        num4 = str.Length;
                    }
                    else
                    {
                        startIndex = num4 + 1;
                    }
                    string str3 = UrlDecode(str.Substring(num3, num4 - num3), encoding);
                    result.Add(str2, str3);
                    if (startIndex == -1)
                    {
                        return;
                    }
                }
            }
        }

        public static string UrlDecode(string str)
        {
            return UrlDecode(str, Encoding.UTF8);
        }

        public static string UrlDecode(byte[] bytes, Encoding e)
        {
            if (bytes == null)
            {
                return null;
            }
            return UrlDecode(bytes, 0, bytes.Length, e);
        }

        public static string UrlDecode(string s, Encoding e)
        {
            if (s == null)
            {
                return null;
            }
            if ((s.IndexOf('%') == -1) && (s.IndexOf('+') == -1))
            {
                return s;
            }
            if (e == null)
            {
                e = Encoding.UTF8;
            }
            long length = s.Length;
            List<byte> buf = new List<byte>();
            for (int i = 0; i < length; i++)
            {
                char ch = s[i];
                if (((ch == '%') && ((i + 2) < length)) && (s[i + 1] != '%'))
                {
                    int num2;
                    if ((s[i + 1] == 'u') && ((i + 5) < length))
                    {
                        num2 = GetChar(s, i + 2, 4);
                        if (num2 != -1)
                        {
                            WriteCharBytes(buf, (char) num2, e);
                            i += 5;
                        }
                        else
                        {
                            WriteCharBytes(buf, '%', e);
                        }
                    }
                    else
                    {
                        num2 = GetChar(s, i + 1, 2);
                        if (num2 != -1)
                        {
                            WriteCharBytes(buf, (char) num2, e);
                            i += 2;
                        }
                        else
                        {
                            WriteCharBytes(buf, '%', e);
                        }
                    }
                }
                else if (ch == '+')
                {
                    WriteCharBytes(buf, ' ', e);
                }
                else
                {
                    WriteCharBytes(buf, ch, e);
                }
            }
            byte[] bytes = buf.ToArray();
            buf = null;
            return e.GetString(bytes);
        }

        public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e)
        {
            if (bytes == null)
            {
                return null;
            }
            if (count == 0)
            {
                return string.Empty;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            StringBuilder builder = new StringBuilder();
            MemoryStream b = new MemoryStream();
            int num = count + offset;
            for (int i = offset; i < num; i++)
            {
                if (((bytes[i] == 0x25) && ((i + 2) < count)) && (bytes[i + 1] != 0x25))
                {
                    int num2;
                    if ((bytes[i + 1] == 0x75) && ((i + 5) < num))
                    {
                        if (b.Length > 0L)
                        {
                            builder.Append(GetChars(b, e));
                            b.SetLength(0L);
                        }
                        num2 = GetChar(bytes, i + 2, 4);
                        if (num2 == -1)
                        {
                            goto Label_00EE;
                        }
                        builder.Append((char) num2);
                        i += 5;
                        continue;
                    }
                    num2 = GetChar(bytes, i + 1, 2);
                    if (num2 != -1)
                    {
                        b.WriteByte((byte) num2);
                        i += 2;
                        continue;
                    }
                }
            Label_00EE:
                if (b.Length > 0L)
                {
                    builder.Append(GetChars(b, e));
                    b.SetLength(0L);
                }
                if (bytes[i] == 0x2b)
                {
                    builder.Append(' ');
                }
                else
                {
                    builder.Append((char) bytes[i]);
                }
            }
            if (b.Length > 0L)
            {
                builder.Append(GetChars(b, e));
            }
            b = null;
            return builder.ToString();
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            return UrlDecodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlDecodeToBytes(string str)
        {
            return UrlDecodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlDecodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            return UrlDecodeToBytes(e.GetBytes(str));
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                return null;
            }
            if (count == 0)
            {
                return new byte[0];
            }
            int length = bytes.Length;
            if ((offset < 0) || (offset >= length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || (offset > (length - count)))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            MemoryStream stream = new MemoryStream();
            int num2 = offset + count;
            for (int i = offset; i < num2; i++)
            {
                char ch = (char) bytes[i];
                if (ch == '+')
                {
                    ch = ' ';
                }
                else if ((ch == '%') && (i < (num2 - 2)))
                {
                    int num4 = GetChar(bytes, i + 1, 2);
                    if (num4 != -1)
                    {
                        ch = (char) num4;
                        i += 2;
                    }
                }
                stream.WriteByte((byte) ch);
            }
            return stream.ToArray();
        }

        public static string UrlEncode(string str)
        {
            return UrlEncode(str, Encoding.UTF8);
        }

        public static string UrlEncode(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            if (bytes.Length == 0)
            {
                return string.Empty;
            }
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, 0, bytes.Length));
        }

        public static string UrlEncode(string s, Encoding Enc)
        {
            if (s == null)
            {
                return null;
            }
            if (s == string.Empty)
            {
                return string.Empty;
            }
            bool flag = false;
            int length = s.Length;
            for (int i = 0; i < length; i++)
            {
                char c = s[i];
                if (((((c < '0') || ((c < 'A') && (c > '9'))) || ((c > 'Z') && (c < 'a'))) || (c > 'z')) && !HttpEncoder.NotEncoded(c))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                return s;
            }
            byte[] bytes = new byte[Enc.GetMaxByteCount(s.Length)];
            int count = Enc.GetBytes(s, 0, s.Length, bytes, 0);
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, 0, count));
        }

        public static string UrlEncode(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                return null;
            }
            if (bytes.Length == 0)
            {
                return string.Empty;
            }
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
        }

        public static byte[] UrlEncodeToBytes(string str)
        {
            return UrlEncodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            if (bytes.Length == 0)
            {
                return new byte[0];
            }
            return UrlEncodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            if (str.Length == 0)
            {
                return new byte[0];
            }
            byte[] bytes = e.GetBytes(str);
            return UrlEncodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                return null;
            }
            return HttpEncoder.Current.UrlEncode(bytes, offset, count);
        }

        public static string UrlEncodeUnicode(string str)
        {
            if (str == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(UrlEncodeUnicodeToBytes(str));
        }

        public static byte[] UrlEncodeUnicodeToBytes(string str)
        {
            if (str == null)
            {
                return null;
            }
            if (str.Length == 0)
            {
                return new byte[0];
            }
            MemoryStream result = new MemoryStream(str.Length);
            foreach (char ch in str)
            {
                HttpEncoder.UrlEncodeChar(ch, result, true);
            }
            return result.ToArray();
        }

        public static string UrlPathEncode(string s)
        {
            return HttpEncoder.Current.UrlPathEncode(s);
        }

        private static void WriteCharBytes(IList buf, char ch, Encoding e)
        {
            if (ch > '\x00ff')
            {
                foreach (byte num in e.GetBytes(new char[] { ch }))
                {
                    buf.Add(num);
                }
            }
            else
            {
                buf.Add((byte) ch);
            }
        }

        private sealed class HttpQSCollection
            : NameValueCollection
        {

            public override string ToString()
            {
                int count = this.Count;
                if (count == 0)
                {
                    return "";
                }
                StringBuilder builder = new StringBuilder();
                string[] allKeys = this.AllKeys;
                for (int i = 0; i < count; i++)
                {
                    builder.AppendFormat("{0}={1}&", allKeys[i], base[allKeys[i]]);
                }
                if (builder.Length > 0)
                {
                    builder.Length--;
                }
                return builder.ToString();
            }

        }

    }

}