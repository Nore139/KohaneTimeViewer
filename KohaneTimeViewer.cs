// こはねEX ベストタイム・スコア ビューア(KohaneTimeViewer)
// Steam版『ホッピングガールこはねEX』のセーブデータを読み取り、
// 各ステージのランク・ベストタイム・ベストスコアを一覧表示する。
// セーブデータは読み取るだけで、変更しない。
//
// セーブの形式: JSON → XOR(鍵 = 固定文字列 + SteamID64) → Base64
//
// ビルド: build.bat を実行(Windows標準の csc.exe / .NET Framework 4.8 を使用)
// ※ csc.exe が対応するのは C# 5 までなので、新しい書き方($"..." など)は使わない

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace KohaneTimeViewer
{
    // ---- 復号の処理 ----
    static class SaveCodec
    {
        const string KeyPrefix = "YourWaifu_RubbingSaltInTheYourWound_Aghhhhh";

        public static bool IsSteamId(string s)
        {
            return s != null && Regex.IsMatch(s, @"^\d{17}$");
        }

        static byte[] Xor(byte[] data, string steamId)
        {
            byte[] key = Encoding.ASCII.GetBytes(KeyPrefix + steamId);
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            }
            return result;
        }

        // ファイルの中身(Base64文字列) → JSON文字列
        public static string Decode(string fileText, string steamId)
        {
            byte[] encrypted = Convert.FromBase64String(fileText.Trim());
            byte[] plain = Xor(encrypted, steamId);
            return new UTF8Encoding(false).GetString(plain);
        }

        // 正しく復号できたかの判定。
        // 鍵の先頭43文字は固定なので、SteamIDが違っても先頭の {" は正しく出てしまう。
        // また化けた文字も : ; < などの普通の記号になるため、JSONとして読めるかで判定する。
        public static bool LooksLikeJson(string json)
        {
            return Parse(json) != null;
        }

        // JSON文字列 → 項目名と値の辞書。読めなければ null
        // 配列は object[]、整数は int、小数は decimal になる
        public static Dictionary<string, object> Parse(string json)
        {
            if (!json.StartsWith("{\"")) return null;
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = int.MaxValue;
                return js.DeserializeObject(json) as Dictionary<string, object>;
            }
            catch (ArgumentException) { return null; }
            catch (InvalidOperationException) { return null; }
        }
    }

    // ---- クリアタイム ----
    static class StageTimes
    {
        // 秒 → ゲーム内と同じ「分.秒.1/100秒」(例 57.2213 → 00.57.22、132.6346 → 02.12.63)
        // 1/100秒未満は切り捨て(ゲームが切り捨てか四捨五入かは未確認)
        public static string Format(decimal seconds)
        {
            long cs = (long)decimal.Floor(seconds * 100m);
            return (cs / 6000).ToString("00") + "." + (cs / 100 % 60).ToString("00") + "." + (cs % 100).ToString("00");
        }

        // 合計タイム用(1時間を超えるので「時間・分・秒」で出す)
        public static string FormatTotal(decimal seconds)
        {
            long cs = (long)decimal.Floor(seconds * 100m);
            string s = (cs / 100 % 60).ToString("00") + "秒" + (cs % 100).ToString("00");
            if (cs >= 360000) return (cs / 360000) + "時間" + (cs / 6000 % 60).ToString("00") + "分" + s;
            return (cs / 6000) + "分" + s;
        }

        // ステージ名の表示(ゲーム内の表記に合わせる)
        //   1〜100   → 01-01 〜 10-10(10 ステージで 1 ワールド。81〜90 = 09-01〜09-10 はゲームで確認済み)
        //   101〜110 → CS-01 〜 CS-10(チャンピオンシップ。ゲームで確認済み)
        public static string StageName(int index)
        {
            if (index > 100) return "CS-" + (index - 100).ToString("00");
            return ((index - 1) / 10 + 1).ToString("00") + "-" + ((index - 1) % 10 + 1).ToString("00");
        }

        // クリアランクの表示: 1 = D, 2 = C, 3 = B, 4 = A, 5 = S、0 = ランクなし
        // (ゲーム本体の表 [D, D, C, B, A, S] から特定。5 = S はゲームでも確認済み。E ランクは存在しない)
        static readonly string[] RankLetters = new string[] { "-", "D", "C", "B", "A", "S" };

        public static string RankName(decimal rank)
        {
            if (rank >= 0 && rank < RankLetters.Length && rank == decimal.Truncate(rank)) return RankLetters[(int)rank];
            return rank < 0 ? "-" : rank.ToString();
        }

        public static decimal ToNumber(object o)
        {
            try { return Convert.ToDecimal(o, System.Globalization.CultureInfo.InvariantCulture); }
            catch (Exception) { return -1; }
        }
    }

    // 列の見出しクリックで並べ替える(各行の Tag に列ごとの数値を入れておく)
    class TimeSorter : System.Collections.IComparer
    {
        public int Column;
        public bool Descending;
        public int Compare(object x, object y)
        {
            decimal a = ((decimal[])((ListViewItem)x).Tag)[Column];
            decimal b = ((decimal[])((ListViewItem)y).Tag)[Column];
            int r = a.CompareTo(b);
            return Descending ? -r : r;
        }
    }

    // ---- SteamID の自動判別と記憶 ----
    static class SteamIdStore
    {
        public static readonly string SaveRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @"AppData\LocalLow\D-O\Hopping Girl Kohane EX\Save");

        // 入力した SteamID は exe と同じ場所に保存する
        static readonly string StoreFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steamid.txt");

        // このパソコンのセーブフォルダにある SteamID のフォルダ
        public static List<string> SteamIdDirs()
        {
            List<string> dirs = new List<string>();
            try
            {
                if (!Directory.Exists(SaveRoot)) return dirs;
                foreach (string d in Directory.GetDirectories(SaveRoot))
                {
                    if (SaveCodec.IsSteamId(Path.GetFileName(d))) dirs.Add(d);
                }
            }
            catch (Exception) { }
            return dirs;
        }

        // 試す順番: セーブのあるフォルダ名 → 前回入力した ID → このパソコンのセーブフォルダにある ID
        public static List<string> Candidates(string savePath)
        {
            List<string> ids = new List<string>();
            Add(ids, Path.GetFileName(Path.GetDirectoryName(savePath)));
            Add(ids, Remembered());
            foreach (string d in SteamIdDirs()) Add(ids, Path.GetFileName(d));
            return ids;
        }

        static void Add(List<string> ids, string id)
        {
            if (SaveCodec.IsSteamId(id) && !ids.Contains(id)) ids.Add(id);
        }

        static string Remembered()
        {
            try { return File.Exists(StoreFile) ? File.ReadAllText(StoreFile).Trim() : null; }
            catch (Exception) { return null; }
        }

        public static void Remember(string id)
        {
            try { File.WriteAllText(StoreFile, id); }
            catch (Exception) { }  // 書き込めなくても次回また入力すればよいだけなので無視
        }
    }

    // ---- SteamID 入力ダイアログ ----
    class SteamIdDialog : Form
    {
        TextBox box = new TextBox();
        public string SteamId { get { return box.Text.Trim(); } }

        public SteamIdDialog()
        {
            Text = "SteamIDの入力";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 148);

            Label label = new Label();
            label.Text = "SteamIDを自動で判別できませんでした。\r\nSteamID64(17桁の数字)を入力してください。\r\n(一度入力すれば、次回からは自動で使います)";
            label.SetBounds(12, 12, 396, 54);
            box.SetBounds(12, 72, 396, 23);

            Button ok = new Button(); ok.Text = "OK"; ok.SetBounds(232, 108, 85, 28);
            ok.DialogResult = DialogResult.OK;
            Button cancel = new Button(); cancel.Text = "キャンセル"; cancel.SetBounds(323, 108, 85, 28);
            cancel.DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { label, box, ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
        }
    }

    // ---- セーブを開いて復号する ----
    static class SaveLoader
    {
        // 復号した JSON を返す。開けなかったときはメッセージを出して null を返す
        public static string Load(IWin32Window owner, string title, string path, out string steamId)
        {
            steamId = null;
            string fileText;
            try
            {
                fileText = ReadShared(path);
                Convert.FromBase64String(fileText.Trim());  // 形式チェックのみ
            }
            catch (FormatException)
            {
                MessageBox.Show(owner, "このファイルはセーブデータの形式(Base64)ではありません。", title,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "ファイルを読み込めませんでした。\r\n" + ex.Message, title,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // SteamID を自動で探す: フォルダ名 → 前回入力した ID → このパソコンのセーブフォルダにある ID
            string json = null;
            foreach (string id in SteamIdStore.Candidates(path))
            {
                string j = SaveCodec.Decode(fileText, id);
                if (SaveCodec.LooksLikeJson(j)) { steamId = id; json = j; break; }
            }

            // 見つからなければ入力してもらい、次回のために記憶する
            if (json == null)
            {
                SteamIdDialog idDlg = new SteamIdDialog();
                if (idDlg.ShowDialog(owner) != DialogResult.OK) return null;
                steamId = idDlg.SteamId;
                if (!SaveCodec.IsSteamId(steamId))
                {
                    MessageBox.Show(owner, "SteamIDは17桁の数字で入力してください。", title,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                json = SaveCodec.Decode(fileText, steamId);
                if (!SaveCodec.LooksLikeJson(json))
                {
                    MessageBox.Show(owner, "復号に失敗しました。\r\nSteamIDが違うか、別のゲームのファイルの可能性があります。", title,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                SteamIdStore.Remember(steamId);
            }

            return json;
        }

        // ゲームが起動中でも読めるように、共有モードで開く
        static string ReadShared(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader r = new StreamReader(fs, Encoding.ASCII))
            {
                return r.ReadToEnd();
            }
        }
    }

    // ---- ランク・ベストタイム・ベストスコアの表 ----
    class StageTimesView : UserControl
    {
        ListView times = new ListView();
        Label summary = new Label();
        TimeSorter sorter = new TimeSorter();

        public StageTimesView()
        {
            times.View = View.Details;
            times.FullRowSelect = true;
            times.GridLines = true;
            times.Dock = DockStyle.Fill;
            times.Columns.Add("ステージ", 110);
            times.Columns.Add("ランク", 80, HorizontalAlignment.Center);
            times.Columns.Add("ベストタイム", 150, HorizontalAlignment.Right);
            times.Columns.Add("ベストスコア", 150, HorizontalAlignment.Right);
            times.ListViewItemSorter = sorter;
            times.ColumnClick += delegate(object sender, ColumnClickEventArgs e)
            {
                if (sorter.Column == e.Column) sorter.Descending = !sorter.Descending;
                else { sorter.Column = e.Column; sorter.Descending = false; }
                times.Sort();
            };
            summary.Dock = DockStyle.Top; summary.Height = 28;
            summary.TextAlign = ContentAlignment.MiddleLeft;
            summary.Text = "  見出しをクリックすると並べ替えできます。";
            Controls.Add(times);
            Controls.Add(summary);
        }

        // 合計タイムの横に出す一言
        public string Note = "見出しクリックで並べ替え";

        // セーブの中身からタイムとスコアの表を作る
        public void ShowData(Dictionary<string, object> data)
        {
            object[] t = data.ContainsKey("stageBestTimeScore") ? data["stageBestTimeScore"] as object[] : null;
            object[] rank = data.ContainsKey("stage_clear_rank") ? data["stage_clear_rank"] as object[] : null;
            object[] score = data.ContainsKey("stage_best_score") ? data["stage_best_score"] as object[] : null;
            times.BeginUpdate();
            times.Items.Clear();
            if (t != null)
            {
                decimal total = 0;
                int count = 0;
                // 0 番はチュートリアルなので除く
                for (int i = 1; i < t.Length; i++)
                {
                    decimal sec = StageTimes.ToNumber(t[i]);
                    if (sec < 0) continue;
                    decimal r = rank != null && i < rank.Length ? StageTimes.ToNumber(rank[i]) : -1;
                    decimal sc = score != null && i < score.Length ? StageTimes.ToNumber(score[i]) : -1;
                    ListViewItem item = new ListViewItem(new string[] {
                        StageTimes.StageName(i), StageTimes.RankName(r),
                        StageTimes.Format(sec), sc < 0 ? "-" : sc.ToString("#,0") });
                    item.Tag = new decimal[] { i, r, sec, sc };  // 並べ替え用(列と同じ順番)
                    times.Items.Add(item);
                    total += sec;
                    count++;
                }
                summary.Text = "  記録のあるステージ: " + count + "   合計タイム: " + StageTimes.FormatTotal(total)
                    + "   (" + Note + ")";
            }
            times.EndUpdate();
        }
    }

    // ---- メイン画面 ----
    class ViewerForm : Form
    {
        StageTimesView timesView = new StageTimesView();
        Label status = new Label();
        Button reload = new Button();
        string currentPath;

        public ViewerForm()
        {
            Text = "こはねEX ベストタイム・スコア";
            ClientSize = new Size(620, 640);
            StartPosition = FormStartPosition.CenterScreen;

            Panel top = new Panel(); top.Dock = DockStyle.Top; top.Height = 44;
            Button open = new Button(); open.Text = "セーブを開く…"; open.SetBounds(8, 8, 130, 28);
            open.Click += delegate { OpenSave(); };
            reload.Text = "再読み込み"; reload.SetBounds(146, 8, 110, 28);
            reload.Enabled = false;
            reload.Click += delegate { if (currentPath != null) LoadSave(currentPath); };
            top.Controls.AddRange(new Control[] { open, reload });

            timesView.Dock = DockStyle.Fill;

            status.Dock = DockStyle.Bottom; status.Height = 24;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.Text = "「セーブを開く」から save.sav を選ぶか、save.sav をこの画面にドラッグしてください。";

            Controls.Add(timesView);
            Controls.Add(top);
            Controls.Add(status);

            // セーブファイルをウィンドウにドラッグして開けるようにする
            foreach (Control c in new Control[] { this, timesView })
            {
                c.AllowDrop = true;
                c.DragEnter += delegate(object sender, DragEventArgs e)
                {
                    e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
                };
                c.DragDrop += delegate(object sender, DragEventArgs e)
                {
                    string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files == null || files.Length == 0 || !File.Exists(files[0])) return;
                    Activate();
                    LoadSave(files[0]);
                };
            }
            // F5 でも再読み込み
            KeyPreview = true;
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.F5 && currentPath != null) { LoadSave(currentPath); e.Handled = true; }
            };
        }

        // このパソコンのセーブ(save.sav)を探す。複数あるときは一番新しいもの
        public static string FindLocalSave()
        {
            string best = null;
            DateTime bestTime = DateTime.MinValue;
            foreach (string dir in SteamIdStore.SteamIdDirs())
            {
                string p = Path.Combine(dir, "save.sav");
                if (!File.Exists(p)) continue;
                DateTime t = File.GetLastWriteTime(p);
                if (t > bestTime) { best = p; bestTime = t; }
            }
            return best;
        }

        void OpenSave()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "save.sav を選択";
            dlg.Filter = "セーブデータ (*.sav)|*.sav|すべてのファイル (*.*)|*.*";
            string local = FindLocalSave();
            if (local != null)
            {
                dlg.InitialDirectory = Path.GetDirectoryName(local);
                dlg.FileName = "save.sav";
            }
            else if (Directory.Exists(SteamIdStore.SaveRoot))
            {
                dlg.InitialDirectory = SteamIdStore.SaveRoot;
            }
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            LoadSave(dlg.FileName);
        }

        public void LoadSave(string path)
        {
            string steamId;
            string json = SaveLoader.Load(this, Text, path, out steamId);
            if (json == null) return;
            Dictionary<string, object> data = SaveCodec.Parse(json);
            if (data == null) return;

            timesView.ShowData(data);
            currentPath = path;
            reload.Enabled = true;
            status.Text = "読み込み: " + DateTime.Now.ToString("HH:mm:ss")
                + "   |   セーブの更新: " + File.GetLastWriteTime(path).ToString("yyyy/MM/dd HH:mm")
                + "   |   " + path;
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ViewerForm form = new ViewerForm();
            // exe のアイコンにセーブをドラッグして起動したらそれを、そうでなければこのパソコンのセーブを開く
            string path = args.Length > 0 && File.Exists(args[0]) ? args[0] : ViewerForm.FindLocalSave();
            if (path != null) form.Shown += delegate { form.LoadSave(path); };
            Application.Run(form);
        }
    }
}
