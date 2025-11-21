using System;
using System.Collections.Generic;
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
        private const int DepthMax = 10;
        private int _currentDepthIn;
        private int _currentDepthOut;

        /// <summary>
        /// Находит структуры для Source пакетов
        /// </summary>
        public Dictionary<int, List<NameFinder.Struc>> FindSourceStructures(
            List<string> fileLines,
            PacketType packetType,
            string searchPattern,
            System.Action<int> progressCallback = null)
        {
            if (fileLines == null || fileLines.Count == 0)
                return new Dictionary<int, List<NameFinder.Struc>>();

            var structures = new Dictionary<int, List<NameFinder.Struc>>();
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
                    var regexBody = new Regex(@"(dd\soffset\snullsub|dd\soffset\ssub_\w+|dd\soffset\s\w+)", RegexOptions.Compiled);
                    var matchesBodys = regexBody.Match(fileLines[index]);
                    if (matchesBodys.Success)
                    {
                        subAddresses.Add(matchesBodys.ToString().Substring(10));
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
                    var lst = new List<NameFinder.Struc>();

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
                                    lst.AddRange(findList);
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
                    structures.Add(i, new List<NameFinder.Struc>());
                }

                progressCallback?.Invoke(structures.Count);
            }

            return structures;
        }

        /// <summary>
        /// Находит структуры для Destination пакетов
        /// </summary>
        public Dictionary<int, List<NameFinder.Struc>> FindDestinationStructures(
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
        /// Находит структуру по адресу (для Source)
        /// </summary>
        public List<NameFinder.Struc> FindStructureIn(string address, List<string> fileLines, int maxDepth = 10)
        {
            return FindStructureInternal(address, fileLines, maxDepth, ref _currentDepthIn, FindStructureIn);
        }

        /// <summary>
        /// Находит структуру по адресу (для Destination)
        /// </summary>
        public List<NameFinder.Struc> FindStructureOut(string address, List<string> fileLines, int maxDepth = 10)
        {
            return FindStructureInternal(address, fileLines, maxDepth, ref _currentDepthOut, FindStructureOut);
        }

        /// <summary>
        /// Рефакторинг: универсальный метод для поиска структуры по адресу
        /// Объединяет общую логику FindStructureIn и FindStructureOut
        /// </summary>
        private List<NameFinder.Struc> FindStructureInternal(
            string address,
            List<string> fileLines,
            int maxDepth,
            ref int currentDepth,
            Func<string, List<string>, int, List<NameFinder.Struc>> recursiveCall)
        {
            var tmpLst = new List<NameFinder.Struc>();
            if (currentDepth >= maxDepth)
            {
                return tmpLst;
            }

            currentDepth++;
            var found = false;
            var regexSub = new Regex(@"^" + address, RegexOptions.Compiled);
            var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))", RegexOptions.Compiled);

            for (var index = 0; index < fileLines.Count; index++)
            {
                var matches4 = regexSub.Matches(fileLines[index]);
                if (matches4.Count <= 0)
                    continue;

                var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.IgnoreCase);
                var foundEndp = false;
                tmpLst = new List<Struc>();

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
                            if (callStr.Length >= 8)
                            {
                                var callAddress = callStr.Substring(8).Trim();
                                if (!string.IsNullOrEmpty(callAddress))
                                {
                                    var findList = recursiveCall(callAddress, fileLines, maxDepth);
                                    if (findList.Count > 0)
                                    {
                                        tmpLst.AddRange(findList);
                                        found = true;
                                    }
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

                currentDepth--;
                return tmpLst;
            }

            if (!found)
            {
                currentDepth--;
                return new List<NameFinder.Struc>();
            }

            return tmpLst;
        }
    }
}

