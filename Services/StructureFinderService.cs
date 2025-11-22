using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NameFinder.Models;

namespace NameFinder.Services
{
    /// <summary>
    /// Сервис для поиска структур пакетов
    /// </summary>
    public class StructureFinderService : IStructureFinderService
    {
        private static void LogDebug(string message) => Debug.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}");
        private static void LogWarn(string message) => Debug.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss.fff} {message}");
        private const int DepthMax = 10;
        private int _currentDepthIn;
        private int _currentDepthOut;

        /// <summary>
        /// Находит структуры для Source пакетов
        /// </summary>
        public Dictionary<int, ObservableCollection<NameFinder.Struc>> FindSourceStructures(
            List<string> fileLines,
            PacketType packetType,
            string searchPattern,
            System.Action<int> progressCallback = null)
        {
            if (fileLines == null || fileLines.Count == 0)
                return new Dictionary<int, ObservableCollection<NameFinder.Struc>>();

            var structures = new Dictionary<int, ObservableCollection<NameFinder.Struc>>();
            var packetNames = new List<string>();
            var subAddresses = new List<string>();
            var xrefs = new Dictionary<int, List<string>>();

            _currentDepthIn = 0;

            // Поиск имен пакетов и XREF
            var regex = new Regex(@"^[a-zA-Z0-9_?@]+\s+dd\soffset\s" + searchPattern, RegexOptions.Compiled);
            var regexXREF = new Regex(@"(^\s+;[a-zA-Z:\s]*\s(sub_\w+|X2\w+|w+))", RegexOptions.Compiled);
            var indexRefs = 0;

            for (var index = 0; index < fileLines.Count; index++)
            {
                var foundName = false;
                var matches = regex.Matches(fileLines[index]);
                if (matches.Count <= 0)
                    continue;

                // Собираем XREF
                var lst = new List<string>();
                var tmpIdx = index;
                var tmpIdxMax = tmpIdx + 2;
                do
                {
                    tmpIdx++;
                    if (tmpIdx >= fileLines.Count)
                        break;

                    var matchesXREF = regexXREF.Matches(fileLines[tmpIdx]);
                    if (matchesXREF.Count <= 0)
                        continue;

                    foreach (var match in matchesXREF)
                    {
                        lst.Add(match.ToString());
                    }
                } while (tmpIdx < tmpIdxMax);

                xrefs.Add(indexRefs, lst);
                indexRefs++;

                // Извлекаем имя пакета
                var regex2 = new Regex(@"^(\S+)", RegexOptions.IgnoreCase);
                var matches2 = regex2.Matches(fileLines[index]);
                foreach (var match2 in matches2)
                {
                    packetNames.Add(match2.ToString());
                    foundName = true;
                }

                if (!foundName)
                {
                    packetNames.Add("CS_Unknown");
                }

                // Пропускаем строки с начальными пробелами
                do
                {
                    index++;
                    if (index >= fileLines.Count)
                        break;

                    var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.IgnoreCase);
                    var matchesSpace40 = regexSpace40.Matches(fileLines[index]);
                    if (matchesSpace40.Count <= 0)
                        break;
                } while (true);

                // Извлекаем адрес подпрограммы
                index++;
                if (index >= fileLines.Count)
                    continue;

                try
                {
                    // Используем regex с группами захвата для более надежного извлечения адреса
                    // Паттерн ищет "dd offset " и захватывает адрес после него
                    var regexBody = new Regex(@"dd\s+offset\s+(nullsub_\w+|sub_\w+|\w+)", RegexOptions.Compiled);
                    var matchesBodys = regexBody.Match(fileLines[index]);
                    if (matchesBodys.Success && matchesBodys.Groups.Count > 1)
                    {
                        // Используем первую группу захвата (индекс 1) для получения адреса
                        var address = matchesBodys.Groups[1].Value;
                        if (!string.IsNullOrEmpty(address))
                        {
                            subAddresses.Add(address);
                        }
                    }
                }
                catch (Exception)
                {
                    // Игнорируем ошибки парсинга
                }
            }

            // Поиск структур
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled);
            var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s+(sub_\w+)|(call\s+(\w+)))", RegexOptions.Compiled);

            for (var i = 0; i < subAddresses.Count; i++)
            {
                var found = false;
                var regexSub = new Regex(@"^" + subAddresses[i], RegexOptions.Compiled);

                for (var index = 0; index < fileLines.Count; index++)
                {
                    var matchesSub = regexSub.Matches(fileLines[index]);
                    if (matchesSub.Count <= 0)
                        continue;

                    // Нашли начало подпрограммы, ищем структуры
                    var foundEndp = false;
                    var lst = new ObservableCollection<NameFinder.Struc>();

                    do
                    {
                        var matchesCalls = regexCall.Matches(fileLines[index]);
                        foreach (var matchCall in matchesCalls)
                        {
                            if (matchCall.ToString().Length >= 4 && matchCall.ToString().Substring(0, 4) == "call")
                            {
                                var findList = FindStructureIn(matchCall.ToString().Substring(8), fileLines);
                                if (findList.Count > 0)
                                {
                                    foreach (var item in findList)
                                    {
                                        lst.Add(item);
                                    }
                                }
                            }
                            else
                            {
                                var aa = new NameFinder.Struc();
                                aa.Name = matchCall.ToString().Replace("\"", "");
                                index--;
                                var offset = fileLines[index].LastIndexOf("]", StringComparison.Ordinal) - 3;
                                
                                string num;
                                try
                                {
                                    num = offset < 0 ? "CC" : fileLines[index].Substring(offset, 2);
                                    aa.Type = Convert.ToInt32(num, 16);
                                }
                                catch (Exception)
                                {
                                    num = "CC";
                                    aa.Type = Convert.ToInt32(num, 16);
                                }
                                lst.Add(aa);
                                index++;
                            }
                        }

                        var matchesEndP = regexEndP.Matches(fileLines[index]);
                        if (matchesEndP.Count > 0)
                        {
                            foundEndp = true;
                        }
                        index++;
                    } while (index < fileLines.Count && !foundEndp);

                    structures.Add(i, lst);
                    found = true;
                    break;
                }

                if (!found)
                {
                    structures.Add(i, new ObservableCollection<NameFinder.Struc>());
                }

                progressCallback?.Invoke(structures.Count);
            }

            return structures;
        }

        /// <summary>
        /// Находит структуры для Destination пакетов
        /// </summary>
        public Dictionary<int, ObservableCollection<NameFinder.Struc>> FindDestinationStructures(
            List<string> fileLines,
            PacketType packetType,
            string searchPattern,
            System.Action<int> progressCallback = null)
        {
            // Аналогично FindSourceStructures, но для Destination
            // Логика очень похожа, можно использовать общий метод с параметром
            return FindSourceStructures(fileLines, packetType, searchPattern, progressCallback);
        }

        /// <summary>
        /// Сбрасывает глубину рекурсии для Source
        /// </summary>
        public void ResetDepthIn()
        {
            _currentDepthIn = 0;
        }

        /// <summary>
        /// Сбрасывает глубину рекурсии для Destination
        /// </summary>
        public void ResetDepthOut()
        {
            _currentDepthOut = 0;
        }

        /// <summary>
        /// Находит структуру по адресу (для Source)
        /// </summary>
        public ObservableCollection<NameFinder.Struc> FindStructureIn(string address, List<string> fileLines, int maxDepth = 10, bool useCallSpaces4 = false)
        {
            return FindStructureInternal(address, fileLines, maxDepth, ref _currentDepthIn, FindStructureIn, useCallSpaces4);
        }

        /// <summary>
        /// Находит структуру по адресу (для Destination)
        /// </summary>
        public ObservableCollection<NameFinder.Struc> FindStructureOut(string address, List<string> fileLines, int maxDepth = 10, bool useCallSpaces4 = true)
        {
            return FindStructureInternal(address, fileLines, maxDepth, ref _currentDepthOut, FindStructureOut, useCallSpaces4);
        }

        /// <summary>
        /// Рефакторинг: универсальный метод для поиска структуры по адресу
        /// Объединяет общую логику FindStructureIn и FindStructureOut
        /// </summary>
        private ObservableCollection<NameFinder.Struc> FindStructureInternal(
            string address,
            List<string> fileLines,
            int maxDepth,
            ref int currentDepth,
            Func<string, List<string>, int, bool, ObservableCollection<NameFinder.Struc>> recursiveCall,
            bool useCallSpaces4)
        {
            LogDebug($"FindStructureInternal: Начало поиска для адреса '{address}', currentDepth={currentDepth}, maxDepth={maxDepth}, useCallSpaces4={useCallSpaces4}");
            var tmpLst = new ObservableCollection<NameFinder.Struc>();
            if (currentDepth >= maxDepth)
            {
                LogWarn($"FindStructureInternal: Достигнута максимальная глубина {maxDepth} для адреса '{address}'");
                return tmpLst;
            }

            currentDepth++;
            LogDebug($"FindStructureInternal: Увеличена глубина до {currentDepth} для адреса '{address}'");
            var found = false;
            // В старом коде адрес НЕ экранировался - используем как есть
            var regexSub = new Regex(@"^" + address, RegexOptions.Compiled);
            LogDebug($"FindStructureInternal: Создан regex для адреса '{address}': '^{address}'");
            // Используем правильный regex в зависимости от useCallSpaces4
            var regexCallPattern = useCallSpaces4
                ? @"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))"
                : @"(\x22[0-z._]+\x22)|(call\s+(sub_\w+)|(call\s+(\w+)))";
            var regexCall = new Regex(regexCallPattern, RegexOptions.Compiled);

            for (var index = 0; index < fileLines.Count; index++)
            {
                var matches4 = regexSub.Matches(fileLines[index]);
                if (matches4.Count <= 0)
                    continue;

                LogDebug($"FindStructureInternal: Найдено начало подпрограммы '{address}' в строке {index}: '{fileLines[index]}'");
                var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.IgnoreCase);
                var foundEndp = false;
                tmpLst = new ObservableCollection<NameFinder.Struc>();

                do
                {
                    index++;
                    if (index >= fileLines.Count)
                        break;

                    var matches5 = regexCall.Matches(fileLines[index]);
                    foreach (var matchCall in matches5)
                    {
                        if (matchCall.ToString() == "call    eax" || matchCall.ToString() == "call    ebx" ||
                            matchCall.ToString() == "call    edx" || matchCall.ToString() == "call    ecx")
                        {
                            continue;
                        }

                        if (matchCall.ToString().Length >= 4 && matchCall.ToString().Substring(0, 4) == "call")
                        {
                            var callStr = matchCall.ToString();
                            // В старом коде использовался Substring(8) без Trim()
                            var callAddress = "";
                            if (callStr.Length >= 8)
                            {
                                // Формат "call    sub_xxx" или "call    xxx"
                                callAddress = callStr.Substring(8);
                            }
                            else if (callStr.Length >= 5)
                            {
                                // Формат "call sub_xxx" или "call xxx"
                                callAddress = callStr.Substring(5);
                            }
                            
                            // В старом коде не было проверки на пустую строку
                            if (callAddress.Length > 0)
                            {
                                // Пропускаем регистры и другие не-адреса подпрограмм
                                var lowerAddress = callAddress.ToLower();
                                if (lowerAddress == "eax" || lowerAddress == "ebx" || lowerAddress == "ecx" || 
                                    lowerAddress == "edx" || lowerAddress == "esi" || lowerAddress == "edi" || 
                                    lowerAddress == "esp" || lowerAddress == "ebp" ||
                                    lowerAddress == "ds" || lowerAddress == "cs" || lowerAddress == "es" || 
                                    lowerAddress == "fs" || lowerAddress == "gs" || lowerAddress == "ss" ||
                                    callAddress.StartsWith("__libm_") || callAddress == "floor" || 
                                    callAddress == "ceil" || callAddress == "sqrt")
                                {
                                    LogDebug($"FindStructureInternal: Пропущен не-валидный адрес '{callAddress}' (регистр или библиотечная функция)");
                                    continue;
                                }
                                
                                LogDebug($"FindStructureInternal: Рекурсивный вызов для адреса '{callAddress}' из подпрограммы '{address}' (глубина {currentDepth})");
                                var findList = recursiveCall(callAddress, fileLines, maxDepth, useCallSpaces4);
                                LogDebug($"FindStructureInternal: Результат рекурсивного вызова для '{callAddress}': найдено {findList.Count} структур");
                                if (findList.Count > 0)
                                {
                                    foreach (var item in findList)
                                    {
                                        tmpLst.Add(item);
                                    }
                                    found = true;
                                }
                            }
                            else
                            {
                                LogDebug($"FindStructureInternal: callAddress пустой для callStr: '{callStr}'");
                            }
                        }
                        else
                        {
                            var aa = new NameFinder.Struc();
                            aa.Name = matchCall.ToString().Replace("\"", "");
                            index--;
                            var offset = fileLines[index].LastIndexOf("]", StringComparison.Ordinal) - 3;
                            
                            string num;
                            try
                            {
                                num = offset < 0 ? "CC" : fileLines[index].Substring(offset, 2);
                                aa.Type = Convert.ToInt32(num, 16);
                            }
                            catch (Exception)
                            {
                                num = "CC";
                                aa.Type = Convert.ToInt32(num, 16);
                            }
                            index++;
                            tmpLst.Add(aa);
                            found = true;
                        }
                    }

                    var matches6 = regexEndP.Matches(fileLines[index]);
                    if (matches6.Count <= 0)
                        continue;

                    foundEndp = true;
                } while (!foundEndp);

                LogDebug($"FindStructureInternal: Найдена структура для адреса '{address}': {tmpLst.Count} элементов");
                currentDepth--;
                return tmpLst;
            }

            if (!found)
            {
                LogWarn($"FindStructureInternal: Не найдена структура для адреса '{address}'");
                currentDepth--;
                return new ObservableCollection<NameFinder.Struc>();
            }

            LogDebug($"FindStructureInternal: Завершено для адреса '{address}': {tmpLst.Count} элементов");
            return tmpLst;
        }
    }
}

