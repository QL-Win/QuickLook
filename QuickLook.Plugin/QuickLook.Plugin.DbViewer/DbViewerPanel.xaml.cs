using LiteDB;
using Microsoft.Data.Sqlite;
using MiniExcelLibs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace QuickLook.Plugin.DbViewer;

public partial class DbViewerPanel : UserControl
{
    private const int DefaultPageSize = 200;
    private string _path;
    private DatabaseType _databaseType;
    private string _currentObjectName;
    private int _totalCount;
    private bool _hasLoadedData;

    public bool HasLoadedData => _hasLoadedData;

    public DbViewerPanel()
    {
        InitializeComponent();
        pagination.PageSize = DefaultPageSize;
    }

    public void LoadDatabase(string path)
    {
        _path = path;
        _databaseType = DetectDatabaseType(path);
        _hasLoadedData = false;

        if (_databaseType == DatabaseType.Unknown)
        {
            statusTextBlock.Text = "无法识别的数据库格式。";
            return;
        }

        var objects = _databaseType switch
        {
            DatabaseType.SQLite => LoadSqliteNames(path),
            DatabaseType.LiteDb => LoadLiteDbNames(path),
            _ => Array.Empty<string>(),
        };

        tableComboBox.ItemsSource = objects;

        if (objects.Any())
        {
            tableComboBox.SelectedIndex = 0;
            _hasLoadedData = true;
        }
        else
        {
            statusTextBlock.Text = "数据库中没有可显示的表或集合。";
        }
    }

    public void ExportCurrentTableToExcel(string sourcePath)
    {
        if (string.IsNullOrEmpty(_currentObjectName))
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel 工作簿 (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = Path.GetFileNameWithoutExtension(sourcePath) + "_" + _currentObjectName + ".xlsx",
            Title = "导出为 Excel"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var table = _databaseType switch
            {
                DatabaseType.SQLite => LoadSqliteTable(sourcePath, _currentObjectName),
                DatabaseType.LiteDb => LoadLiteDbTable(sourcePath, _currentObjectName),
                _ => null,
            };

            if (table is null)
            {
                MessageBox.Show("无法导出数据。", "QuickLook", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MiniExcel.SaveAs(dialog.FileName, table);
            MessageBox.Show($"导出成功：{dialog.FileName}", "QuickLook", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "QuickLook", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tableComboBox.SelectedItem is string selected)
        {
            _currentObjectName = selected;
            LoadCurrentObject();
        }
    }

    private void Pagination_CurrentPageChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
        if (e.NewValue < 1)
            return;

        LoadCurrentPage();
    }

    private void LoadCurrentObject()
    {
        if (string.IsNullOrEmpty(_currentObjectName))
            return;

        _totalCount = _databaseType switch
        {
            DatabaseType.SQLite => GetSqliteRowCount(_path, _currentObjectName),
            DatabaseType.LiteDb => GetLiteDbRowCount(_path, _currentObjectName),
            _ => 0,
        };

        pagination.TotalCount = _totalCount;
        pagination.CurrentPage = 1;
        pageInfoTextBlock.Text = $"{_totalCount} 行 / 每页 {DefaultPageSize} 行";
        LoadCurrentPage();
    }

    private void LoadCurrentPage()
    {
        if (string.IsNullOrEmpty(_currentObjectName))
            return;

        var pageIndex = pagination.CurrentPage;
        pagination.PageSize = DefaultPageSize;

        DataTable table = _databaseType switch
        {
            DatabaseType.SQLite => LoadSqlitePage(_path, _currentObjectName, pageIndex, DefaultPageSize),
            DatabaseType.LiteDb => LoadLiteDbPage(_path, _currentObjectName, pageIndex, DefaultPageSize),
            _ => new DataTable(),
        };

        dataGrid.ItemsSource = table.DefaultView;
        pageInfoTextBlock.Text = $"{_totalCount} 行 / 每页 {DefaultPageSize} 行 / 第 {pagination.CurrentPage} 页，共 {pagination.PageCount} 页";
    }

    private static DatabaseType DetectDatabaseType(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var header = new byte[59]; // need 32+27 bytes for LiteDB magic
            var count = stream.Read(header, 0, header.Length);

            if (count >= 16)
            {
                var text = System.Text.Encoding.ASCII.GetString(header, 0, 16);
                if (text.StartsWith("SQLite format 3", StringComparison.Ordinal))
                    return DatabaseType.SQLite;
            }

            // LiteDB 5.x: magic string "** This is a LiteDB file **" at offset 32
            if (count >= 59)
            {
                var liteDbMagic = System.Text.Encoding.ASCII.GetString(header, 32, 27);
                if (liteDbMagic == "** This is a LiteDB file **")
                    return DatabaseType.LiteDb;
            }
        }
        catch
        {
        }

        return DatabaseType.Unknown;
    }

    private static string[] LoadSqliteNames(string path)
    {
        var items = new List<string>();

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%' ORDER BY name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(reader.GetString(0));
        }

        return items.ToArray();
    }

    private static string[] LoadLiteDbNames(string path)
    {
        using var db = new LiteDatabase(path);
        return db.GetCollectionNames().OrderBy(name => name).ToArray();
    }

    private static int GetSqliteRowCount(string path, string tableName)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int GetLiteDbRowCount(string path, string collectionName)
    {
        using var db = new LiteDatabase(path);
        var collection = db.GetCollection(collectionName);
        return (int)collection.Count();
    }

    private static DataTable LoadSqlitePage(string path, string tableName, int page, int pageSize)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM [{tableName}] LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";

        using var reader = command.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private static DataTable LoadLiteDbPage(string path, string collectionName, int page, int pageSize)
    {
        using var db = new LiteDatabase(path);
        var collection = db.GetCollection(collectionName);
        var docs = collection.Find(Query.All(), (page - 1) * pageSize, pageSize);

        return ConvertBsonDocumentsToTable(docs);
    }

    private static DataTable LoadSqliteTable(string path, string tableName)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM [{tableName}]";

        using var reader = command.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private static DataTable LoadLiteDbTable(string path, string collectionName)
    {
        using var db = new LiteDatabase(path);
        var collection = db.GetCollection(collectionName);
        var docs = collection.Find(Query.All());
        return ConvertBsonDocumentsToTable(docs);
    }

    private static DataTable ConvertBsonDocumentsToTable(IEnumerable<BsonDocument> docs)
    {
        var table = new DataTable();
        bool schemaCreated = false;

        foreach (var doc in docs)
        {
            if (!schemaCreated)
            {
                foreach (var key in doc.Keys)
                {
                    if (!table.Columns.Contains(key))
                    {
                        table.Columns.Add(key, typeof(string));
                    }
                }
                schemaCreated = true;
            }

            var row = table.NewRow();
            foreach (DataColumn column in table.Columns)
            {
                row[column.ColumnName] = doc.ContainsKey(column.ColumnName)
                    ? ConvertBsonValue(doc[column.ColumnName])
                    : string.Empty;
            }
            table.Rows.Add(row);
        }

        return table;
    }

    private static object ConvertBsonValue(BsonValue value)
    {
        if (value.IsNull)
            return string.Empty;

        return value.RawValue?.ToString() ?? value.ToString() ?? string.Empty;
    }

    private enum DatabaseType
    {
        Unknown,
        SQLite,
        LiteDb,
    }
}
