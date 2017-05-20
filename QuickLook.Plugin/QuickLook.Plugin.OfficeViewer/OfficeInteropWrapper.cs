using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Office.Interop.Word;
using Application = Microsoft.Office.Interop.Excel.Application;
using Task = System.Threading.Tasks.Task;

namespace QuickLook.Plugin.OfficeViewer
{
    internal class OfficeInteropWrapper : IDisposable
    {
        public enum FileTypeEnum
        {
            Word,
            Excel,
            PowerPoint
        }

        private readonly string _path;
        private readonly string _tempPdf = Path.GetTempFileName();

        private Application _excelApp;
        private Microsoft.Office.Interop.PowerPoint.Application _powerpointApp;
        private Microsoft.Office.Interop.Word.Application _wordApp;

        public OfficeInteropWrapper(string path)
        {
            _path = path;

            switch (Path.GetExtension(path).ToLower())
            {
                case ".doc":
                case ".docx":
                    FileType = FileTypeEnum.Word;
                    break;
                case ".xls":
                case ".xlsx":
                case ".xlsm":
                    FileType = FileTypeEnum.Excel;
                    break;
                case ".ppt":
                case ".pptx":
                    FileType = FileTypeEnum.PowerPoint;
                    break;
                default:
                    throw new NotSupportedException($"{path} is not supported.");
            }

            LoadApplication();
        }

        public FileTypeEnum FileType { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // communicate with COM in a separate thread
            Task.Run(() =>
                {
                    try
                    {
                        //_wordApp?.Documents.Close(false);
                        _wordApp?.Quit(false);
                        _wordApp = null;

                        _excelApp?.Workbooks.Close();
                        _excelApp?.Quit();
                        _excelApp = null;

                        _powerpointApp?.Quit();
                        _powerpointApp = null;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        Debug.WriteLine(e.StackTrace);
                    }
                })
                .Wait();
        }

        public string SaveAsPdf()
        {
            if (_wordApp == null && _excelApp == null && _powerpointApp == null)
                throw new Exception("Office application launch failed.");

            var succeeded = false;

            // communicate with COM in a separate thread
            Task.Run(() =>
                {
                    try
                    {
                        switch (FileType)
                        {
                            case FileTypeEnum.Word:
                                _wordApp.ActiveDocument.ExportAsFixedFormat(_tempPdf,
                                    WdExportFormat.wdExportFormatPDF);
                                succeeded = true;
                                break;
                            case FileTypeEnum.Excel:
                                _excelApp.ActiveWorkbook.ExportAsFixedFormat(XlFixedFormatType.xlTypePDF,
                                    _tempPdf);
                                succeeded = true;
                                break;
                            case FileTypeEnum.PowerPoint:
                                _powerpointApp.ActivePresentation.ExportAsFixedFormat(_tempPdf,
                                    PpFixedFormatType.ppFixedFormatTypePDF);
                                succeeded = true;
                                break;
                            default:
                                throw new NotSupportedException($"{_path} is not supported.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        Debug.WriteLine(e.StackTrace);
                    }
                })
                .Wait();

            if (succeeded)
                return FileType == FileTypeEnum.Excel
                    ? _tempPdf + ".pdf"
                    : _tempPdf; // Excel will add ".pdf" to our filename

            Dispose();
            return string.Empty;
        }

        private void LoadApplication()
        {
            var succeeded = false;

            // communicate with COM in a separate thread
            Task.Run(() =>
                {
                    try
                    {
                        switch (FileType)
                        {
                            case FileTypeEnum.Word:
                                _wordApp = new Microsoft.Office.Interop.Word.Application();
                                _wordApp.DisplayAlerts = WdAlertLevel.wdAlertsNone;
                                _wordApp.Documents.Add(_path);
                                succeeded = true;
                                break;
                            case FileTypeEnum.Excel:
                                _excelApp = new Application();
                                _excelApp.DisplayAlerts = false;
                                _excelApp.Workbooks.Add(_path);
                                var worksheets = _excelApp.ActiveWorkbook.Sheets;
                                if (worksheets != null)
                                    foreach (Worksheet sheet in worksheets)
                                        sheet.PageSetup.PrintGridlines = true;
                                succeeded = true;
                                break;
                            case FileTypeEnum.PowerPoint:
                                _powerpointApp = new Microsoft.Office.Interop.PowerPoint.Application();
                                _powerpointApp.DisplayAlerts = PpAlertLevel.ppAlertsNone;
                                _powerpointApp.Presentations.Open(_path);
                                succeeded = true;
                                break;
                            default:
                                throw new NotSupportedException($"{_path} is not supported.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        Debug.WriteLine(e.StackTrace);
                    }
                })
                .Wait();

            if (!succeeded)
                Dispose();
        }

        ~OfficeInteropWrapper()
        {
            Dispose();
        }
    }
}