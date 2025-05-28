namespace Assets.Scripts.Utils
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Универсальный класс для показа стандартного
    /// Open/Save File Dialog под Windows (comdlg32.dll).
    /// Использование фильтров в формате .NET: 
    ///   "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*"
    /// </summary>
    public static class FileDialog
    {
        /// <summary>
        /// Показать диалог выбора (Open File). 
        /// </summary>
        /// <param name="filter">
        /// Фильтр в формате .NET: 
        /// "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        /// </param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="initialDirectory">Стартовая папка (по умолчанию Assets)</param>
        /// <param name="multiSelect">Можно ли выбрать сразу несколько файлов</param>
        /// <returns>
        /// Массив путей или null, если пользователь отменил.
        /// </returns>
        public static string[] ShowOpen(
            string filter = "All files (*.*)|*.*",
            string title = "Open File",
            string initialDirectory = null,
            bool multiSelect = false)
        {
            var ofn = OpenFileName.Create();
            ofn.filter = ConvertFilter(filter);
            ofn.defExt = ExtractDefaultExt(filter);
            ofn.title = title;
            ofn.initialDir = initialDirectory ?? Application.dataPath;
            ofn.flags = OpenFileName.OFN_EXPLORER
                             | OpenFileName.OFN_FILEMUSTEXIST
                             | OpenFileName.OFN_PATHMUSTEXIST;
            if (multiSelect)
                ofn.flags |= OpenFileName.OFN_ALLOWMULTISELECT;

            bool ok = NativeFileDialog.GetOpenFileName(ofn);
            if (!ok) 
                return null;

            return ParseFileNames(ofn.file);
        }

        /// <summary>
        /// Показать диалог сохранения (Save File).
        /// </summary>
        /// <param name="filter">Фильтр в формате .NET</param>
        /// <param name="title">Заголовок окна</param>
        /// <param name="initialDirectory">Стартовая папка</param>
        /// <returns>Выбранный путь или null</returns>
        public static string ShowSave(
            string filter = "All files (*.*)|*.*",
            string title = "Save File",
            string initialDirectory = null)
        {
            var ofn = OpenFileName.Create();
            ofn.filter = ConvertFilter(filter);
            ofn.defExt = ExtractDefaultExt(filter);
            ofn.title = title;
            ofn.initialDir = initialDirectory ?? Application.dataPath;
            ofn.flags = OpenFileName.OFN_EXPLORER
                             | OpenFileName.OFN_OVERWRITEPROMPT;

            bool ok = NativeFileDialog.GetSaveFileName(ofn);
            return ok ? ofn.file : null;
        }

        // ======== вспомогательные методы ========

        // Разбор результата (multi-select)
        private static string[] ParseFileNames(string raw)
        {
            var parts = raw.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;
            if (parts.Length == 1) return new[] { parts[0] };

            // при множественном выборе первый элемент — директория, дальше — имена файлов
            var dir = parts[0];
            var files = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
                files[i - 1] = Path.Combine(dir, parts[i]);
            return files;
        }

        // Конвертирует .NET-строку фильтра в формат COMDLG
        private static string ConvertFilter(string filter)
        {
            // "desc1|*.ext1|desc2|*.ext2" → "desc1\0*.ext1\0desc2\0*.ext2\0\0"
            var parts = filter.Split('|');
            return string.Join("\0", parts) + "\0\0";
        }

        // Извлекаем первое расширение без точки
        private static string ExtractDefaultExt(string filter)
        {
            var parts = filter.Split('|');
            if (parts.Length >= 2)
            {
                var pat = parts[1].Trim();
                var idx = pat.LastIndexOf('.');
                if (idx >= 0 && idx < pat.Length - 1)
                    return pat.Substring(idx + 1);
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Обёртка OPENFILENAME для P/Invoke
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = new string('\0', 4096);
        public int maxFile = 4096;
        public string fileTitle = new string('\0', 256);
        public int maxFileTitle = 256;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;

        // Общие флаги
        public const int OFN_EXPLORER = 0x00080000;
        public const int OFN_FILEMUSTEXIST = 0x00001000;
        public const int OFN_PATHMUSTEXIST = 0x00000800;
        public const int OFN_ALLOWMULTISELECT = 0x00000200;
        public const int OFN_OVERWRITEPROMPT = 0x00000002;

        internal static OpenFileName Create()
        {
            var ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            return ofn;
        }
    }

    /// <summary>
    /// Нативные вызовы в comdlg32.dll
    /// </summary>
    internal static class NativeFileDialog
    {
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);
    }
}
