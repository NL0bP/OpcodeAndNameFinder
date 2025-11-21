using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NameFinder.Services
{
    /// <summary>
    /// Реализация обработчика файлов с поддержкой больших файлов
    /// </summary>
    public class FileProcessor : IFileProcessor
    {
        private const int BufferSize = 8192; // 8KB буфер

        public async Task<List<string>> ReadFileLinesAsync(string filePath, IProgress<int> progress = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            var lines = new List<string>();
            var lineCount = 0;
            var totalLines = await GetFileLineCountAsync(filePath);

            using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8, true, BufferSize))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                    lineCount++;

                    // Отчет о прогрессе каждые 1000 строк
                    if (lineCount % 1000 == 0 && progress != null && totalLines > 0)
                    {
                        var percent = (int)((double)lineCount / totalLines * 100);
                        progress.Report(percent);
                    }
                }
            }

            if (progress != null)
            {
                progress.Report(100);
            }

            return lines;
        }

        public async Task WriteFileLinesAsync(string filePath, IEnumerable<string> lines)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8, BufferSize))
            {
                foreach (var line in lines)
                {
                    await writer.WriteLineAsync(line);
                }
            }
        }

        public bool FileExists(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
        }

        public async Task<long> GetFileLineCountAsync(string filePath)
        {
            if (!FileExists(filePath))
                return 0;

            // Быстрая оценка количества строк
            var fileInfo = new FileInfo(filePath);
            var estimatedLines = fileInfo.Length / 80; // Примерно 80 символов на строку

            // Более точный подсчет для небольших файлов
            if (fileInfo.Length < 10 * 1024 * 1024) // < 10MB
            {
                long count = 0;
                using (var reader = new StreamReader(filePath))
                {
                    while (await reader.ReadLineAsync() != null)
                    {
                        count++;
                    }
                }
                return count;
            }

            return estimatedLines;
        }
    }
}

