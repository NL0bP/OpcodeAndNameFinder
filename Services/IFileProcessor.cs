using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NameFinder.Services
{
    /// <summary>
    /// Интерфейс для обработки файлов
    /// </summary>
    public interface IFileProcessor
    {
        /// <summary>
        /// Читает файл построчно асинхронно
        /// </summary>
        Task<List<string>> ReadFileLinesAsync(string filePath, IProgress<int> progress = null);

        /// <summary>
        /// Записывает строки в файл
        /// </summary>
        Task WriteFileLinesAsync(string filePath, IEnumerable<string> lines);

        /// <summary>
        /// Проверяет существование файла
        /// </summary>
        bool FileExists(string filePath);

        /// <summary>
        /// Получает размер файла в строках (приблизительно)
        /// </summary>
        Task<long> GetFileLineCountAsync(string filePath);
    }
}

