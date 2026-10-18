using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace SafeChm;

public partial class MainWindow : Window
{
    private string? _cacheDir;
    private string? _chmFile;

    public MainWindow(string[] args)
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await InitWebView();
            if (args.Length > 0 && args[0].EndsWith(".chm", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))
                OpenChm(args[0]);
        };
    }

    private async Task InitWebView()
    {
        var env = await CoreWebView2Environment.CreateAsync(null, ChmCache.WebView2Data);
        await WebView.EnsureCoreWebView2Async(env);
        var s = WebView.CoreWebView2.Settings;
        s.IsScriptEnabled = false;
        s.AreDefaultScriptDialogsEnabled = false;
        s.AreDevToolsEnabled = false;
        s.IsWebMessageEnabled = false;
        s.AreDefaultContextMenusEnabled = false;
        s.IsStatusBarEnabled = false;
        s.AreBrowserAcceleratorKeysEnabled = false;
        WebView.CoreWebView2.DownloadStarting += (_, e) => e.Cancel = true;
        WebView.CoreWebView2.NewWindowRequested += (_, e) => e.Handled = true;
        WebView.CoreWebView2.ContextMenuRequested += (_, e) => e.Handled = true;
        WebView.CoreWebView2.NavigationStarting += OnNavigationStarting;
        WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var uri = e.Uri;
        if (IsChmLocal(uri)) return;

        if (uri.StartsWith("ms-its:", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("mk:@MSITStore:", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            var local = RewriteMsIts(uri);
            if (local != null)
                WebView.CoreWebView2!.Navigate("https://chm.local/" + local);
            return;
        }

        if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            var r = MessageBox.Show($"在瀏覽器開啟外部連結？\n\n{uri}", "外部連結",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
                catch { }
            }
            return;
        }

        e.Cancel = true; // file:, about:, javascript:, data: etc. all blocked
    }

    private static bool IsChmLocal(string uri) =>
        uri.StartsWith("https://chm.local/", StringComparison.OrdinalIgnoreCase)
        || uri.StartsWith("http://chm.local/", StringComparison.OrdinalIgnoreCase)
        || string.Equals(uri, "https://chm.local", StringComparison.OrdinalIgnoreCase);

    private string? RewriteMsIts(string uri)
    {
        int idx = uri.IndexOf("::", StringComparison.Ordinal);
        if (idx < 0) return null;
        var chmName = Path.GetFileName(uri[..idx].Replace('/', '\\'));
        if (!string.IsNullOrEmpty(chmName) && _chmFile != null &&
            !string.Equals(chmName, Path.GetFileName(_chmFile), StringComparison.OrdinalIgnoreCase))
            return null; // points into a different CHM -> block
        var inner = uri[(idx + 2)..].TrimStart('/', '\\');
        if (inner.Length == 0) return null;
        int frag = inner.IndexOf('#');
        if (frag >= 0) inner = inner[..frag];
        return Uri.UnescapeDataString(inner);
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (!string.Equals(new Uri(e.Request.Uri).Host, "chm.local", StringComparison.OrdinalIgnoreCase))
        {
            e.Response = WebView.CoreWebView2!.Environment.CreateWebResourceResponse(
                new MemoryStream(Array.Empty<byte>()), 403, "Blocked", "");
        }
    }

    private void OpenChm(string path)
    {
        try
        {
            _chmFile = path;
            _cacheDir = ChmCache.Extract(path);
            Title = $"SafeChm - {Path.GetFileName(path)}";
            var toc = LoadToc();
            PopulateTree(toc);
            var core = WebView.CoreWebView2!;
            try { core.ClearVirtualHostNameToFolderMapping("chm.local"); } catch { }
            core.SetVirtualHostNameToFolderMapping("chm.local", _cacheDir, CoreWebView2HostResourceAccessKind.DenyCors);
            core.Navigate("https://chm.local/" + FindDefaultPage(_cacheDir, toc));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"開啟失敗：{ex.Message}", "SafeChm", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private List<TocEntry> LoadToc()
    {
        if (_cacheDir == null) return new();
        var hhc = Directory.EnumerateFiles(_cacheDir, "*.hhc", SearchOption.AllDirectories).FirstOrDefault();
        if (hhc == null) return new();
        var bytes = File.ReadAllBytes(hhc);
        return TocParser.Parse(EncodingDetector.Detect(bytes).GetString(bytes));
    }

    private void PopulateTree(List<TocEntry> entries)
    {
        TocTree.Items.Clear();
        foreach (var e in entries)
            TocTree.Items.Add(BuildNode(e));
    }

    private static TreeViewItem BuildNode(TocEntry e)
    {
        var node = new TreeViewItem
        {
            Header = string.IsNullOrEmpty(e.Name) ? e.Local : e.Name,
            Tag = e.Local,
            IsExpanded = e.Children.Count > 0
        };
        foreach (var c in e.Children) node.Items.Add(BuildNode(c));
        return node;
    }

    private void OnTocSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem { Tag: string local } && local.Length > 0)
            NavigateLocal(local);
    }

    private void NavigateLocal(string local) =>
        WebView.CoreWebView2?.Navigate("https://chm.local/" + Normalize(local));

    private static string Normalize(string local) =>
        string.Join("/", local.Replace('\\', '/').TrimStart('/').Split('/').Select(Uri.EscapeDataString));

    private static string FindDefaultPage(string dir, List<TocEntry> toc)
    {
        string? first = null;
        void Walk(IEnumerable<TocEntry> entries)
        {
            foreach (var e in entries)
            {
                if (first == null && !string.IsNullOrEmpty(e.Local)) first = e.Local;
                if (first == null) Walk(e.Children);
            }
        }
        Walk(toc);
        if (first != null) return Normalize(first);
        foreach (var cand in new[] { "index.html", "index.htm", "default.htm", "default.html" })
            if (File.Exists(Path.Combine(dir, cand))) return cand;
        var any = Directory.EnumerateFiles(dir, "*.htm?", SearchOption.AllDirectories).FirstOrDefault();
        return any != null ? Path.GetRelativePath(dir, any).Replace('\\', '/') : "";
    }

    private void OnOpen(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "CHM 檔案 (*.chm)|*.chm" };
        if (dlg.ShowDialog(this) == true) OpenChm(dlg.FileName);
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        if (_cacheDir == null)
        {
            MessageBox.Show("請先開啟一個 CHM 檔案。", "SafeChm", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "選擇匯出資料夾" };
        if (dlg.ShowDialog(this) == true)
        {
            var dest = Path.Combine(dlg.FolderName, "SafeChm-" + Path.GetFileNameWithoutExtension(_chmFile));
            CopyDirectory(_cacheDir, dest);
            MessageBox.Show($"已匯出至：{dest}", "SafeChm", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.EnumerateFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
        foreach (var d in Directory.EnumerateDirectories(src))
            CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    private void RunSearch()
    {
        if (_cacheDir == null) return;
        var q = SearchBox.Text.Trim();
        if (q.Length == 0) { ShowToc(); return; }
        ResultsList.ItemsSource = SearchEngine.Search(_cacheDir, q);
        TocTree.Visibility = Visibility.Collapsed;
        ResultsList.Visibility = Visibility.Visible;
    }

    private void ShowToc()
    {
        TocTree.Visibility = Visibility.Visible;
        ResultsList.Visibility = Visibility.Collapsed;
    }

    private void OnSearch(object sender, RoutedEventArgs e) => RunSearch();
    private void OnSearchKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) RunSearch(); }
    private void OnShowToc(object sender, RoutedEventArgs e) => ShowToc();

    private void OnResultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is SearchResult r)
            NavigateLocal(r.RelativePath);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0 &&
            files[0].EndsWith(".chm", StringComparison.OrdinalIgnoreCase))
            OpenChm(files[0]);
    }
}
