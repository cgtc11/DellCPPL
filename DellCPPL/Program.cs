using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DellCPPL
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0)
            {
                var sb = new StringBuilder();

                foreach (var p in args)
                {
                    try
                    {
                        if (!File.Exists(p)) { sb.AppendLine($"{p} : ファイルなし"); sb.AppendLine(); continue; }

                        long beforeBytes = new FileInfo(p).Length;
                        string report;
                        bool changed;
                        byte[] src = File.ReadAllBytes(p);
                        var result = RiffAep.RemoveCpplAuto(src, Path.GetExtension(p), out report, out changed);

                        var dir = Path.GetDirectoryName(p);
                        var name = Path.GetFileNameWithoutExtension(p);
                        var ext = Path.GetExtension(p);
                        var outPath = Path.Combine(dir, name + "_DellCPPl" + ext);

                        sb.AppendLine($"入力＝{Path.GetFileName(p)} : {beforeBytes:N0} B");

                        if (changed && result != null)
                        {
                            File.WriteAllBytes(outPath, result);
                            long afterBytes = result.LongLength;
                            sb.AppendLine($"出力＝{Path.GetFileName(outPath)} : {afterBytes:N0} B");
                        }
                        else
                        {
                            sb.AppendLine("出力＝処理不要（CPPl/CPPI なし）");
                        }

                        sb.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{Path.GetFileName(p)} : エラー {ex.Message}");
                        sb.AppendLine();
                    }
                }

                MessageBox.Show(sb.ToString().TrimEnd(), "DellCPPL 結果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.Run(new Form1());
        }
    }
}
