using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Alchemist.Domain.Player
{
    /// <summary>
    /// 최소 JSON 파서. object -> Dictionary&lt;string,object&gt;, array -> List&lt;object&gt;,
    /// number -> long/double, string -> string, true/false -> bool, null -> null.
    /// WHY: UnityEngine.JsonUtility 의 Dictionary/중첩 제약 회피 + 테스트 환경 독립.
    /// WHY: 외부 의존 없이 SaveService 결정적 직렬화 라운드트립 보장.
    /// </summary>
    internal static class MiniJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new FormatException("empty");
            int idx = 0;
            SkipWs(json, ref idx);
            object root = ReadValue(json, ref idx);
            SkipWs(json, ref idx);
            if (idx != json.Length) throw new FormatException("trailing garbage");
            return root;
        }

        private static object ReadValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("eof");
            char c = s[i];
            if (c == '{') return ReadObject(s, ref i);
            if (c == '[') return ReadArray(s, ref i);
            if (c == '"') return ReadString(s, ref i);
            if (c == 't' || c == 'f') return ReadBool(s, ref i);
            if (c == 'n') { ReadLiteral(s, ref i, "null"); return null; }
            return ReadNumber(s, ref i);
        }

        private static Dictionary<string, object> ReadObject(string s, ref int i)
        {
            var dict = new Dictionary<string, object>();
            i++; // '{'
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return dict; }
            while (i < s.Length)
            {
                SkipWs(s, ref i);
                string key = ReadString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("expected ':'");
                i++;
                var val = ReadValue(s, ref i);
                dict[key] = val;
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("unterminated object");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return dict; }
                throw new FormatException("expected ',' or '}'");
            }
            throw new FormatException("unterminated object");
        }

        private static List<object> ReadArray(string s, ref int i)
        {
            var list = new List<object>();
            i++; // '['
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (i < s.Length)
            {
                list.Add(ReadValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("unterminated array");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return list; }
                throw new FormatException("expected ',' or ']'");
            }
            throw new FormatException("unterminated array");
        }

        private static string ReadString(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("expected string");
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (i >= s.Length) throw new FormatException("bad escape");
                    char esc = s[i++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw new FormatException("bad u escape");
                            string hex = s.Substring(i, 4);
                            sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        default: throw new FormatException("unknown escape");
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw new FormatException("unterminated string");
        }

        private static object ReadBool(string s, ref int i)
        {
            if (s[i] == 't') { ReadLiteral(s, ref i, "true"); return true; }
            ReadLiteral(s, ref i, "false"); return false;
        }

        private static void ReadLiteral(string s, ref int i, string lit)
        {
            if (i + lit.Length > s.Length) throw new FormatException("literal overflow");
            for (int k = 0; k < lit.Length; k++)
            {
                if (s[i + k] != lit[k]) throw new FormatException("expected " + lit);
            }
            i += lit.Length;
        }

        private static object ReadNumber(string s, ref int i)
        {
            int start = i;
            if (s[i] == '-') i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-'))
            {
                i++;
            }
            string token = s.Substring(start, i - start);
            if (token.IndexOf('.') < 0 && token.IndexOf('e') < 0 && token.IndexOf('E') < 0)
            {
                if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
            }
            return double.Parse(token, CultureInfo.InvariantCulture);
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') i++;
                else break;
            }
        }
    }
}
