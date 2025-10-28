using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DellCPPL
{
    internal static class RiffAep
    {
        public static byte[] RemoveCpplAuto(byte[] src, string ext, out string report, out bool changed)
        {
            report = "";
            changed = false;

            // 先頭空白スキップして判定
            int i = 0;
            while (i < src.Length && (src[i] == 0x20 || src[i] == 0x09 || src[i] == 0x0D || src[i] == 0x0A)) i++;
            bool looksXml = i < src.Length && src[i] == (byte)'<';
            bool looksRiff = src.Length >= 12 &&
                             ((src[0] == (byte)'R' && src[1] == (byte)'I' && src[2] == (byte)'F' && src[3] == (byte)'F') ||
                              (src[0] == (byte)'R' && src[1] == (byte)'I' && src[2] == (byte)'F' && src[3] == (byte)'X'));

            bool isAepx = ext != null && ext.Equals(".aepx", StringComparison.OrdinalIgnoreCase);
            bool isAep = ext != null && ext.Equals(".aep", StringComparison.OrdinalIgnoreCase);

            if (isAepx || looksXml)
                return RemoveFromXml(src, out report, out changed);

            if (isAep || looksRiff)
            {
                byte[] out1 = RemoveFromRiff(src, out report, out changed);
                if (changed) return out1;

                // 解析で見つからない時はパターン法を試す
                byte[] out2 = RemoveByPatternLikeNote(src, out string rep2, out bool ch2);
                report = report + (report.Length > 0 ? "\n" : "") + rep2;
                if (ch2) { changed = true; return out2; }
                return null;
            }

            report = "未知フォーマット。aep/aepxのみ対応。";
            changed = false;
            return null;
        }

        // ===== XML(.aepx): <CPPl>/<CPPI> を大小無視で除去 =====
        private static byte[] RemoveFromXml(byte[] src, out string report, out bool changed)
        {
            int bomLen;
            Encoding enc = DetectTextEncoding(src, out bomLen);
            string text = enc.GetString(src, bomLen, src.Length - bomLen);

            Regex reBlocks = new Regex(@"<(CPPl|CPPI)\b[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Regex reEmpties = new Regex(@"<(CPPl|CPPI)\b[^>]*/>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            int beforeLen = text.Length;
            string t1 = reBlocks.Replace(text, "");
            string t2 = reEmpties.Replace(t1, "");
            changed = beforeLen != t2.Length;

            if (!changed)
            {
                report = "XML: <CPPl>/<CPPI> は見つかりませんでした。";
                return null;
            }

            report = "XML: <CPPl>/<CPPI> 要素を削除。\nテキスト長変化: " + beforeLen + " -> " + t2.Length;

            using (var ms = new MemoryStream())
            {
                byte[] pre = enc.GetPreamble();
                if (pre != null && pre.Length > 0) ms.Write(pre, 0, pre.Length);
                byte[] outBytes = enc.GetBytes(t2);
                ms.Write(outBytes, 0, outBytes.Length);
                return ms.ToArray();
            }
        }

        private static Encoding DetectTextEncoding(byte[] src, out int bomLen)
        {
            bomLen = 0;
            if (src.Length >= 3 && src[0] == 0xEF && src[1] == 0xBB && src[2] == 0xBF) { bomLen = 3; return new UTF8Encoding(true); }
            if (src.Length >= 2 && src[0] == 0xFF && src[1] == 0xFE) { bomLen = 2; return new UnicodeEncoding(false, true); } // UTF-16LE
            if (src.Length >= 2 && src[0] == 0xFE && src[1] == 0xFF) { bomLen = 2; return new UnicodeEncoding(true, true); }  // UTF-16BE
            return new UTF8Encoding(false);
        }

        // ===== バイナリ(.aep) 正規解析 =====
        private class Chunk
        {
            public string Id = "";
            public uint Size;
            public string TypeOrForm;    // LIST/RIFF/RIFX の type
            public List<Chunk> Children; // コンテナのみ
            public byte[] Data;          // 非コンテナのみ
        }

        private static uint ReadU32(byte[] s, int o, bool be)
        {
            if (be) return (uint)(s[o] << 24 | s[o + 1] << 16 | s[o + 2] << 8 | s[o + 3]);
            return (uint)(s[o + 3] << 24 | s[o + 2] << 16 | s[o + 1] << 8 | s[o]);
        }

        private static void WriteU32(byte[] s, int o, uint v, bool be)
        {
            if (be) { s[o] = (byte)(v >> 24); s[o + 1] = (byte)(v >> 16); s[o + 2] = (byte)(v >> 8); s[o + 3] = (byte)v; }
            else { s[o + 3] = (byte)(v >> 24); s[o + 2] = (byte)(v >> 16); s[o + 1] = (byte)(v >> 8); s[o] = (byte)v; }
        }

        private static string FourCC(byte[] b, int o) { return Encoding.ASCII.GetString(b, o, 4); }

        private static byte[] RemoveFromRiff(byte[] src, out string report, out bool changed)
        {
            changed = false;
            report = "";

            if (src.Length < 12) { report = "バイナリ: 先頭長不足。"; return null; }

            string rootId = FourCC(src, 0);
            if (rootId != "RIFX" && rootId != "RIFF") { report = "バイナリ: RIFF/RIFX ではありません。"; return null; }
            bool be = rootId == "RIFX";

            int consumed;
            Chunk root = ParseChunk(src, 0, src.Length, be, out consumed, true);
            if (root == null) { report = "バイナリ: 解析失敗。"; return null; }

            int beforeLen = src.Length;
            int removedCount = 0;
            int removedBytesSum = 0;

            RemoveCpplRecursive(root, ref removedCount, ref removedBytesSum);

            if (removedCount == 0)
            {
                report = "バイナリ: 解析では CPPl は見つからず。";
                return null;
            }

            byte[] rebuilt = BuildChunk(root, be);
            int afterLen = rebuilt.Length;

            changed = true;
            report = "バイナリ: 解析で削除した CPPl LIST 数: " + removedCount + "\n" +
                     "理論削減量合計: " + removedBytesSum + " bytes\n" +
                     "実サイズ差: " + (beforeLen - afterLen) + " bytes";
            return rebuilt;
        }

        private static Chunk ParseChunk(byte[] buf, int offset, int limit, bool be, out int consumed, bool isRoot)
        {
            consumed = 0;
            if (offset + 8 > limit) return null;

            string id = FourCC(buf, offset);
            uint size = ReadU32(buf, offset + 4, be);
            int pos = offset + 8;

            if (id == "RIFF" || id == "RIFX" || id == "LIST")
            {
                if (pos + 4 > limit) return null;
                string type = FourCC(buf, pos);
                pos += 4;

                int contentBytes = checked((int)size) - 4;
                if (pos + contentBytes > limit) return null;

                var children = new List<Chunk>();
                int end = pos + contentBytes;
                int cur = pos;

                while (cur + 8 <= end)
                {
                    int ccons;
                    Chunk child = ParseChunk(buf, cur, end, be, out ccons, false);
                    if (child == null) break;
                    children.Add(child);
                    cur += ccons;
                    if ((ccons % 2) != 0) cur++; // パディング
                }

                consumed = (end - offset) + 8;
                return new Chunk { Id = id, Size = size, TypeOrForm = type, Children = children };
            }
            else
            {
                if (pos + size > limit) return null;
                var data = new byte[size];
                Buffer.BlockCopy(buf, pos, data, 0, (int)size);
                consumed = checked((int)(8 + size));
                return new Chunk { Id = id, Size = size, Data = data, TypeOrForm = null, Children = null };
            }
        }

        private static void RemoveCpplRecursive(Chunk node, ref int removedCount, ref int removedBytesSum)
        {
            if (node.Children == null) return;

            var kept = new List<Chunk>(node.Children.Count);
            foreach (var c in node.Children)
            {
                if (c.Id == "LIST" && c.TypeOrForm != null &&
                    string.Equals(c.TypeOrForm, "CPPl", StringComparison.OrdinalIgnoreCase))
                {
                    int total = checked((int)(8 + c.Size));
                    removedBytesSum += total;
                    removedCount++;
                    continue; // DROP
                }
                RemoveCpplRecursive(c, ref removedCount, ref removedBytesSum);
                kept.Add(c);
            }
            node.Children = kept;
        }

        private static byte[] BuildChunk(Chunk ch, bool be)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.ASCII))
            {
                Action<string> W4 = fourcc => bw.Write(Encoding.ASCII.GetBytes(fourcc));

                if (ch.Id == "RIFF" || ch.Id == "RIFX" || ch.Id == "LIST")
                {
                    long start = ms.Position;
                    W4(ch.Id);
                    bw.Write(new byte[4]); // size placeholder
                    W4(ch.TypeOrForm != null ? ch.TypeOrForm : "    ");

                    long childrenStart = ms.Position;

                    if (ch.Children != null)
                    {
                        foreach (var child in ch.Children)
                        {
                            byte[] bytes = BuildChunk(child, be);
                            bw.Write(bytes);
                            if ((bytes.Length % 2) != 0) bw.Write((byte)0x00);
                        }
                    }

                    long end = ms.Position;
                    uint size = checked((uint)(end - childrenStart + 4));
                    ms.Position = start + 4;
                    var sz = new byte[4];
                    WriteU32(sz, 0, size, be);
                    bw.Write(sz, 0, 4);
                    ms.Position = end;

                    bw.Flush();
                    return ms.ToArray();
                }
                else
                {
                    W4(ch.Id);
                    var sz = new byte[4];
                    WriteU32(sz, 0, (uint)(ch.Data != null ? ch.Data.Length : 0), be);
                    bw.Write(sz, 0, 4);
                    if (ch.Data != null && ch.Data.Length > 0) bw.Write(ch.Data);
                    bw.Flush();
                    return ms.ToArray();
                }
            }
        }

        // ===== パターン法（あなたのPythonに準拠）
        private static byte[] RemoveByPatternLikeNote(byte[] data, out string report, out bool changed)
        {
            changed = false;
            report = "パターン法: ";

            if (data.Length < 200) { report += "データ短すぎ。"; return null; }

            // RIFX/RIFF判定とエンディアン
            string rifx = FourCC(data, 0);
            bool be = rifx == "RIFX";
            if (!be && rifx != "RIFF") { report += "RIFF/RIFXではない。"; return null; }

            // ヘッダ読み出し
            uint riffSize = ReadU32(data, 4, be);
            string form = FourCC(data, 8);

            int cppl = IndexOfAscii(data, "CPPl", 0);
            if (cppl < 0) { report += "\"CPPl\"未検出。"; return null; }

            int list = cppl - 8;
            if (list < 0 || list + 8 > data.Length || FourCC(data, list) != "LIST")
            {
                report += "LISTヘッダがCPPlの直前に無い。";
                return null;
            }

            uint listSize = ReadU32(data, list + 4, be);

            int cpid = IndexOfAscii(data, "cpid", cppl + 4);
            if (cpid < 0) { report += "\"cpid\"未検出。"; return null; }

            // svapの位置（既定12。見つかれば優先）
            int svap = IndexOfAscii(data, "svap", 0);
            if (svap < 0) svap = 12;
            if (svap < 0 || svap > list) { report += "svap位置不正。"; return null; }

            // 組み立て:
            // [RIFX 8 + form4] + [svap..list手前] + [LIST size=4 + CPPl] + [cpid..末尾]
            int part1Len = list - svap;
            if (part1Len < 0) { report += "区間長不正。"; return null; }

            uint newRiffSize = riffSize - listSize + 4;

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.ASCII))
            {
                // ヘッダ
                bw.Write(Encoding.ASCII.GetBytes(rifx));
                var szBuf = new byte[4];
                WriteU32(szBuf, 0, newRiffSize, be);
                bw.Write(szBuf, 0, 4);
                bw.Write(Encoding.ASCII.GetBytes(form));

                // svap以降〜LIST直前
                bw.Write(data, svap, part1Len);

                // LIST(サイズ4) + CPPl
                bw.Write(Encoding.ASCII.GetBytes("LIST"));
                WriteU32(szBuf, 0, 4, be);
                bw.Write(szBuf, 0, 4);
                bw.Write(Encoding.ASCII.GetBytes("CPPl"));

                // cpid以降末尾
                bw.Write(data, cpid, data.Length - cpid);

                bw.Flush();
                changed = true;
                report += "CPPl領域を強制短縮。oldSize=" + riffSize + " newSize=" + newRiffSize + " listSize=" + listSize;
                return ms.ToArray();
            }
        }

        private static int IndexOfAscii(byte[] data, string token, int start)
        {
            byte[] pat = Encoding.ASCII.GetBytes(token);
            int last = data.Length - pat.Length;
            for (int i = start; i <= last; i++)
            {
                bool ok = true;
                for (int j = 0; j < pat.Length; j++)
                {
                    if (data[i + j] != pat[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }
    }
}
