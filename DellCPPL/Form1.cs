using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DellCPPL
{
    public partial class Form1 : Form
    {
        TextBox log;
        Button btnPick;

        public Form1()
        {
            InitializeComponent();

            this.Text = "AEP/AEPX DellCPPL";
            this.Width = 860;
            this.Height = 520;

            this.AllowDrop = true;
            this.DragEnter += OnDragEnter;
            this.DragDrop += OnDragDrop;

            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var info = new Label
            {
                Text = ".aep / .aepx をドラッグ＆ドロップ（複数可）。処理結果は「<元名>_DellCPPl.<拡張子>」として別名保存。元ファイルは変更しない。",
                AutoSize = true
            };
            panel.Controls.Add(info);

            var topBar = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill };
            btnPick = new Button { Text = "ファイル選択…" };
            btnPick.Click += (s, e) => PickFiles();
            topBar.Controls.Add(btnPick);
            panel.Controls.Add(topBar);

            log = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, ReadOnly = true, Dock = DockStyle.Fill, Font = new System.Drawing.Font("Consolas", 10) };
            panel.Controls.Add(log);

            Controls.Add(panel);
        }

        void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void OnDragDrop(object sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            ProcessMany(paths.Where(p =>
                p.EndsWith(".aep", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith(".aepx", StringComparison.OrdinalIgnoreCase)));
        }

        void PickFiles()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "After Effects Project (*.aep;*.aepx)|*.aep;*.aepx",
                Multiselect = true
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) ProcessMany(dlg.FileNames);
            }
        }

        void ProcessMany(IEnumerable<string> files)
        {
            foreach (var f in files)
            {
                try
                {
                    Append("--- " + f);
                    var bytes = File.ReadAllBytes(f);
                    string report;
                    bool changed;
                    var result = RiffAep.RemoveCpplAuto(bytes, Path.GetExtension(f), out report, out changed);
                    Append(report);

                    if (!changed || result == null)
                    {
                        Append("変更なし。");
                        continue;
                    }

                    var dir = Path.GetDirectoryName(f);
                    var name = Path.GetFileNameWithoutExtension(f);
                    var ext = Path.GetExtension(f);
                    var outPath = Path.Combine(dir, name + "_DellCPPl" + ext);

                    File.WriteAllBytes(outPath, result);
                    Append("出力: " + outPath + "（元ファイルは未変更）");
                }
                catch (Exception ex)
                {
                    Append("エラー: " + ex.Message);
                }
            }
        }

        void Append(string s)
        {
            log.AppendText(s + Environment.NewLine);
        }
    }
}
