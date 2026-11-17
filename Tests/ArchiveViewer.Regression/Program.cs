using QuickLook.Plugin.ArchiveViewer.ArchiveFile;
using QuickLook.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Xml;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

internal static class Program
{
    private static int _failures;

    [STAThread]
    private static int Main(string[] args)
    {
        // No Window.Show, desktop input, or external UI automation is used.
        Run("Rows are keyboard focusable", FocusableRows);
        Run("Right expands/enters; Left selects parent/collapses", TreeNavigation);
        Run("Expansion retains selection and descendant state", ExpansionState);
        Run("Up/Down/Home/End and leaves stay within the visible rows", BoundaryNavigation);
        Run("RTL tree directions mirror correctly", RightToLeft);
        Run("Double-click toggles rows without toggling expander twice", DoubleClick);
        Run("Long names remain available in tooltips", FullNames);
        Run("Automation exposes full names and expand/collapse", Automation);
        Run("10000 children are batched and virtualized", LargeExpansion);
        Run("Headers follow horizontal scrolling at narrow widths", NarrowLayout);
        Run("Header columns align with row columns", ColumnAlignment);
        Run("Long filename columns stay aligned across widths and scrolling", LongNameColumnAlignment);
        Run("Accessibility strings are localized in every plugin language", LocalizedAutomationLabels);
        Run("Rows grow for larger fonts", LargeFonts);
        Run("Empty, multiple roots and disposed views", Lifecycle);
        Run("Actual ZIP parser: empty, files, multi-root and nested", ArchiveFixtures);
        foreach (var path in args)
            Run("Actual archive parsing and expansion", () => ExistingArchive(path));
        Console.WriteLine($"Failures: {_failures}");
        return _failures == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { _failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static ArchiveFileListView View(params ArchiveFileEntry[] roots)
    {
        var view = new ArchiveFileListView();
        view.SetDataContext(roots);
        Layout(view);
        return view;
    }

    private static ListBox List(ArchiveFileListView view) => (ListBox)view.FindName("fileList");

    private static void Layout(FrameworkElement view)
    {
        view.Measure(new Size(800, 320));
        view.Arrange(new Rect(0, 0, 800, 320));
        view.UpdateLayout();
    }

    private static void Key(ListBox list, Key key)
    {
        var e = new KeyEventArgs(Keyboard.PrimaryDevice, new TestSource(), 0, key)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        list.RaiseEvent(e);
    }

    private static ArchiveFileEntry Tree()
    {
        var root = new ArchiveFileEntry("root", true);
        var folder = new ArchiveFileEntry("folder", true, root);
        var nested = new ArchiveFileEntry("nested", true, folder);
        new ArchiveFileEntry("leaf.txt", false, nested);
        new ArchiveFileEntry("root-file.txt", false, root);
        return root;
    }

    private static void FocusableRows()
    {
        using (var view = View(Tree()))
        {
            var list = List(view);
            Check(list.Focusable, "ListBox is not focusable");
            var item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            Check(item != null && item.Focusable, "Row is not focusable");
        }
    }

    private static void TreeNavigation()
    {
        using (var view = View(Tree()))
        {
            var list = List(view);
            var folder = view.Rows[1];
            list.SelectedItem = folder;
            Key(list, System.Windows.Input.Key.Right);
            Check(folder.IsExpanded && view.Rows.Count == 4, "Right did not expand folder");
            Check(ReferenceEquals(list.SelectedItem, folder), "Expand lost selection");
            Key(list, System.Windows.Input.Key.Right);
            Check(((ArchiveFileRow)list.SelectedItem).Entry.Name == "nested", "Right did not enter first child");
            Key(list, System.Windows.Input.Key.Left);
            Check(ReferenceEquals(list.SelectedItem, folder), "Left did not return to parent");
            Key(list, System.Windows.Input.Key.Left);
            Check(!folder.IsExpanded && view.Rows.Count == 3, "Left did not collapse folder");
            Key(list, System.Windows.Input.Key.Left);
            Check(((ArchiveFileRow)list.SelectedItem).Entry.Name == "root", "Left did not select root");
        }
    }

    private static void ExpansionState()
    {
        using (var view = View(Tree()))
        {
            var list = List(view);
            var folder = view.Rows[1];
            list.SelectedItem = folder;
            Key(list, System.Windows.Input.Key.Right);
            var nested = view.Rows[2];
            list.SelectedItem = nested;
            Key(list, System.Windows.Input.Key.Right);
            Check(view.Rows.Count == 5, "Nested expansion failed");
            list.SelectedItem = folder;
            Key(list, System.Windows.Input.Key.Left);
            Check(view.Rows.Count == 3 && nested.IsExpanded, "Collapse lost descendant state");
            Key(list, System.Windows.Input.Key.Right);
            Check(view.Rows.Count == 5 && ReferenceEquals(view.Rows[2], nested), "Reopen lost cached descendants");
        }
    }

    private static void FullNames()
    {
        var name = new string('x', 240) + ".json";
        using (var view = View(new ArchiveFileEntry(name, false)))
        {
            var item = (ListBoxItem)List(view).ItemContainerGenerator.ContainerFromIndex(0);
            Check(item?.ToolTip as string == name, "Full file name not exposed in tooltip");
        }
    }

    private static void BoundaryNavigation()
    {
        using (var view = View(Tree()))
        {
            var list = List(view);
            Key(list, System.Windows.Input.Key.Down);
            Check(ReferenceEquals(list.SelectedItem, view.Rows[0]), "First Down skipped the first row");
            Key(list, System.Windows.Input.Key.Up);
            Check(ReferenceEquals(list.SelectedItem, view.Rows[0]), "Up passed the first row");
            Key(list, System.Windows.Input.Key.End);
            var leaf = view.Rows.Last();
            Check(ReferenceEquals(list.SelectedItem, leaf), "End did not select the final row");
            Key(list, System.Windows.Input.Key.Right);
            Check(ReferenceEquals(list.SelectedItem, leaf) && !leaf.IsExpanded, "A file was expanded");
            Key(list, System.Windows.Input.Key.Down);
            Check(ReferenceEquals(list.SelectedItem, leaf), "Down passed the final row");
            Key(list, System.Windows.Input.Key.Home);
            Check(ReferenceEquals(list.SelectedItem, view.Rows[0]), "Home did not select the first row");
        }
    }

    private static void RightToLeft()
    {
        using (var view = View(Tree()))
        {
            view.FlowDirection = FlowDirection.RightToLeft;
            var folder = view.Rows[1];
            List(view).SelectedItem = folder;
            Key(List(view), System.Windows.Input.Key.Left);
            Check(folder.IsExpanded, "RTL Left did not expand");
            Key(List(view), System.Windows.Input.Key.Right);
            Check(!folder.IsExpanded, "RTL Right did not collapse");
        }
    }

    private static void DoubleClick()
    {
        using (var view = View(Tree()))
        {
            var list = List(view);
            var row = view.Rows[1];
            var container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(row);
            list.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            { RoutedEvent = Control.MouseDoubleClickEvent, Source = container });
            Check(row.IsExpanded, "Double-click did not expand folder");
            Layout(view);
            container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(row);
            var expander = Visuals<Button>(container).First();
            list.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            { RoutedEvent = Control.MouseDoubleClickEvent, Source = expander });
            Check(row.IsExpanded, "Expander double-click triggered an additional toggle");
        }
    }

    private static void Automation()
    {
        using (var view = View(Tree()))
        {
            var peer = UIElementAutomationPeer.CreatePeerForElement(List(view));
            var children = peer.GetChildren();
            Check(children != null, "Automation has no rows");
            var folder = children.FirstOrDefault(p => p.GetName() == "folder");
            Check(folder != null, "Automation row name is not the full entry name");
            var provider = folder.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
            Check(provider != null, "Folder lacks ExpandCollapse pattern");
            provider.Expand();
            Check(view.Rows[1].IsExpanded && view.Rows.Count == 4, "Automation expansion did not update rows");
            provider.Collapse();
            Check(!view.Rows[1].IsExpanded && view.Rows.Count == 3, "Automation collapse failed");
            List(view).SelectedItem = view.Rows[1];
            Key(List(view), System.Windows.Input.Key.Right);
            Check(provider.ExpandCollapseState == System.Windows.Automation.ExpandCollapseState.Expanded,
                "Automation peer has stale state after keyboard expansion");
            List(view).IsEnabled = false;
            var disabledRejected = false;
            try { provider.Collapse(); }
            catch (System.Windows.Automation.ElementNotEnabledException) { disabledRejected = true; }
            Check(disabledRejected, "Automation modified a disabled control");
        }
    }

    private static void LargeExpansion()
    {
        var root = new ArchiveFileEntry("root", true);
        var folder = new ArchiveFileEntry("large", true, root);
        for (var i = 0; i < 10000; i++) new ArchiveFileEntry($"folder_{i:D5}", true, folder);
        using (var view = View(root))
        {
            var list = List(view);
            list.SelectedItem = view.Rows[1];
            var resets = 0;
            view.Rows.CollectionChanged += (_, e) => { if (e.Action == NotifyCollectionChangedAction.Reset) resets++; };
            Key(list, System.Windows.Input.Key.Right);
            Check(view.Rows.Count == 10002 && resets == 1, "Expansion was not one batched update");
            Layout(view);
            var realized = Enumerable.Range(0, list.Items.Count).Count(i => list.ItemContainerGenerator.ContainerFromIndex(i) != null);
            Check(realized < 100, $"Too many containers: {realized}");
            Console.WriteLine($"  Large archive: {view.Rows.Count} visible data rows, {realized} realized containers");
            Check(view.Rows.Last().Entry.Name == "folder_09999", "Last folder missing or out of order");
        }
    }

    private static void Lifecycle()
    {
        using (var view = View())
        {
            Check(view.Rows.Count == 0, "Empty context has rows");
            var first = Tree();
            var second = new ArchiveFileEntry("other", true);
            view.SetDataContext(new[] { first, second });
            Check(view.Rows.Count == 2 && view.Rows.All(r => !r.IsExpanded), "Multiple roots auto-expanded");
            view.SetDataContext(null);
            Check(view.Rows.Count == 0, "Null context not cleared");
            view.Dispose();
            view.SetDataContext(new[] { first });
            Check(view.Rows.Count == 0, "Disposed view accepted new rows");
        }
    }

    private static void NarrowLayout()
    {
        using (var view = View(Tree()))
        {
            view.Measure(new Size(350, 320));
            view.Arrange(new Rect(0, 0, 350, 320));
            view.UpdateLayout();
            var scroll = Visuals<ScrollViewer>(List(view)).First();
            Check(scroll.ScrollableWidth > 0, "Narrow view cannot reach clipped columns");
            scroll.ScrollToRightEnd();
            view.UpdateLayout();
            var header = (ScrollViewer)view.FindName("columnHeaders");
            Check(Math.Abs(scroll.HorizontalOffset - header.HorizontalOffset) < 1, "Header/data offsets differ");
        }
    }

    private static void ColumnAlignment()
    {
        using (var view = View(new ArchiveFileEntry("file.json", false) { Size = 12345, ModifiedDate = new DateTime(2026, 11, 16) }))
        {
            var header = Visuals<TextBlock>((ScrollViewer)view.FindName("columnHeaders")).First(t => t.Text == "Original Size");
            var row = (ListBoxItem)List(view).ItemContainerGenerator.ContainerFromIndex(0);
            var value = Visuals<TextBlock>(row).First(t => Grid.GetColumn(t) == 1);
            var headerX = header.TransformToAncestor(view).Transform(new Point()).X;
            var rowX = value.TransformToAncestor(view).Transform(new Point()).X;
            Check(Math.Abs(headerX - rowX) <= 1, $"Size column misaligned: header={headerX}, row={rowX}");
        }
    }


    private static void LongNameColumnAlignment()
    {
        var name = new string('x', 800) + ".json";
        using (var view = View(new ArchiveFileEntry(name, false)
               { Size = 12345, ModifiedDate = new DateTime(2026, 11, 16) }))
        {
            var list = List(view);
            var header = (ScrollViewer)view.FindName("columnHeaders");
            var scroll = Visuals<ScrollViewer>(list).First();
            foreach (var width in new[] { 800d, 350d })
            {
                view.Measure(new Size(width, 320));
                view.Arrange(new Rect(0, 0, width, 320));
                view.UpdateLayout();
                foreach (var scrollToEnd in new[] { false, true })
                {
                    if (scrollToEnd) scroll.ScrollToRightEnd();
                    else scroll.ScrollToLeftEnd();
                    view.UpdateLayout();

                    if (width >= 800)
                        Check(scroll.ScrollableWidth < 4, "Long filename unexpectedly widened the viewport");
                    else
                        Check(scroll.ScrollableWidth > 0, "Narrow viewport did not enable horizontal scrolling");
                    var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
                    Check(row != null, "Long filename row is no longer realized");
                    var headerGrid = (Grid)view.FindName("headerColumns");
                    var rowGrid = Visuals<Grid>(row).First(g => g.ColumnDefinitions.Count == 3);
                    Check(Math.Abs(headerGrid.ActualWidth - rowGrid.ActualWidth) <= 1,
                        "Header and row have different measured widths");
                    var labels = Visuals<TextBlock>(row).ToList();
                    var headers = Visuals<TextBlock>(header).ToList();
                    foreach (var column in new[] { 1, 2 })
                    {
                        var label = labels.First(t => Grid.GetColumn(t) == column);
                        var caption = headers.First(t => Grid.GetColumn(t) == column);
                        var actual = label.TransformToAncestor(view).Transform(new Point()).X;
                        var expected = caption.TransformToAncestor(view).Transform(new Point()).X;
                        Check(Math.Abs(actual - expected) <= 2.5,
                            $"Column {column} misaligned at width={width}, rightEnd={scrollToEnd}: header={expected}, row={actual}");
                    }
                    Check(Math.Abs(scroll.HorizontalOffset - header.HorizontalOffset) < 1,
                        "Header and data scroll offsets differ for a long filename");
                }
            }
        }
    }

    private static void LocalizedAutomationLabels()
    {
        var file = Path.Combine(Path.GetDirectoryName(typeof(ArchiveFileListView).Assembly.Location), "Translations.config");
        var doc = new XmlDocument();
        doc.Load(file);
        var count = 0;
        foreach (XmlNode locale in doc.DocumentElement.ChildNodes)
        {
            if (locale.NodeType != XmlNodeType.Element) continue;
            count++;
            foreach (var id in new[] { "AV_ArchiveContents", "AV_ExpandOrCollapseFolder", "AV_LevelFormat" })
            {
                var entry = locale.SelectSingleNode(id);
                Check(entry != null && !string.IsNullOrWhiteSpace(entry.InnerText),
                    $"Missing {id} for {locale.Name}");
                if (id == "AV_LevelFormat")
                    Check(entry.InnerText.Contains("{0}"), $"Level format missing placeholder for {locale.Name}");
            }
        }
        Check(count >= 25, "Unexpectedly few locale definitions");

        var chinese = CultureInfo.GetCultureInfo("zh-CN");
        Check(TranslationHelper.Get("AV_ArchiveContents", file, chinese) == "压缩包内容",
            "Chinese accessibility label was not loaded");
        using (var view = View(Tree()))
        {
            var list = List(view);
            Check(AutomationProperties.GetName(list) == ArchiveFileStrings.ArchiveContents,
                "Archive list automation name does not use translations");
            var folder = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(1);
            var expander = Visuals<Button>(folder).First();
            Check(AutomationProperties.GetHelpText(expander) == ArchiveFileStrings.ExpandOrCollapseFolder,
                "Folder expand button help text is not localized");
            var peer = UIElementAutomationPeer.CreatePeerForElement(list);
            var folderPeer = peer.GetChildren().First(p => p.GetName() == "folder");
            Check(folderPeer.GetHelpText() == string.Format(CultureInfo.CurrentUICulture,
                ArchiveFileStrings.LevelFormat, 2), "Folder level automation text is not localized");
        }
    }

    private static IEnumerable<T> Visuals<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Visuals<T>(child)) yield return descendant;
        }
    }

    private static void LargeFonts()
    {
        using (var view = View(new ArchiveFileEntry("large-font.json", false)))
        {
            view.FontSize = 28;
            Layout(view);
            var row = (ListBoxItem)List(view).ItemContainerGenerator.ContainerFromIndex(0);
            var name = Visuals<TextBlock>(row).First(t => Grid.GetColumn(t) == 3);
            Check(row.ActualHeight >= name.DesiredSize.Height, "Fixed row height clips larger text");
        }
    }

    private static void ArchiveFixtures()
    {
        foreach (var names in new[] { new string[0], new[] { "a.txt", "b.json" },
                     new[] { "first/a.txt", "second/b.txt" }, new[] { "root/nested/leaf.txt" } })
        {
            var path = Path.Combine(Path.GetTempPath(), "quicklook-regression-" + Guid.NewGuid() + ".zip");
            try
            {
                using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
                    foreach (var name in names)
                        using (var writer = new StreamWriter(zip.CreateEntry(name).Open())) writer.Write("fixture");

                using (var panel = new ArchiveInfoPanel(path))
                {
                    var frame = new DispatcherFrame();
                    var timeout = DateTime.UtcNow.AddSeconds(15);
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
                    timer.Tick += (_, __) => { if (panel.LoadPercent >= 100 || DateTime.UtcNow >= timeout) frame.Continue = false; };
                    timer.Start();
                    try { Dispatcher.PushFrame(frame); }
                    finally { timer.Stop(); }
                    Check(panel.LoadPercent >= 100, "ZIP parser did not finish: " + string.Join(",", names));
                    var view = (ArchiveFileListView)panel.FindName("fileListView");
                    Check(view.Rows.Count == (names.Length == 0 ? 0 : 2), "Unexpected ZIP roots");
                    if (names.Length == 1)
                    {
                        List(view).SelectedItem = view.Rows[1];
                        Key(List(view), System.Windows.Input.Key.Right);
                        Check(view.Rows.Last().Entry.Name == "leaf.txt", "Nested ZIP leaf missing");
                    }
                }
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
    }

    private static void ExistingArchive(string path)
    {
        using (var panel = new ArchiveInfoPanel(Path.GetFullPath(path)))
        {
            var frame = new DispatcherFrame();
            var timeout = DateTime.UtcNow.AddSeconds(45);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            timer.Tick += (_, __) => { if (panel.LoadPercent >= 100 || DateTime.UtcNow >= timeout) frame.Continue = false; };
            timer.Start();
            try { Dispatcher.PushFrame(frame); }
            finally { timer.Stop(); }
            Check(panel.LoadPercent >= 100, "Archive parsing failed or timed out");
            var view = (ArchiveFileListView)panel.FindName("fileListView");
            var folder = view.Rows.FirstOrDefault(r => r.HasChildren && !r.IsExpanded);
            Check(folder != null, "Expected a collapsed directory to exercise");
            List(view).SelectedItem = folder;
            var before = view.Rows.Count;
            Key(List(view), System.Windows.Input.Key.Right);
            Check(view.Rows.Count == before + folder.Entry.Children.Count, "Expanded archive rows do not match directory entries");
            Key(List(view), System.Windows.Input.Key.Left);
            Check(view.Rows.Count == before, "Archive collapse did not restore the row count");
            Console.WriteLine($"  Parsed archive: {folder.Entry.Children.Count} direct children expanded and collapsed");
        }
    }

    private sealed class TestSource : PresentationSource
    {
        public override Visual RootVisual { get; set; }
        public override bool IsDisposed => false;
        protected override CompositionTarget GetCompositionTargetCore() => null;
    }
}
