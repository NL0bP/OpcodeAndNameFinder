using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NameFinder.Constants;
using NameFinder.Helpers;
using NameFinder.Models;

namespace NameFinder.Services
{
    /// <summary>
    /// Сервис для поиска опкодов в asm файлах
    /// </summary>
    public class OpcodeFinderService : IOpcodeFinderService
    {
        // Кэш для индексов подпрограмм (адрес -> индекс строки)
        private readonly ConcurrentDictionary<string, int> _subroutineIndexCache = new ConcurrentDictionary<string, int>();
        
        // Кэш для Regex паттернов опкодов
        private readonly ConcurrentDictionary<string, Regex> _opcodePatternCache = new ConcurrentDictionary<string, Regex>();
        
        // Храним размер файла для проверки, нужно ли переиндексировать
        private int _lastFileLinesCount = 0;

        /// <summary>
        /// Находит опкоды для пакетов
        /// </summary>
        public async Task<List<string>> FindOpcodesAsync(
            PacketType packetType,
            PacketSource source,
            List<string> fileLines,
            Dictionary<int, List<string>> xrefs,
            IProgress<int> progress = null)
        {
            if (fileLines == null || fileLines.Count == 0)
                return new List<string>();

            if (xrefs == null || xrefs.Count == 0)
                return new List<string>();

            // Предварительная индексация файла для быстрого поиска подпрограмм
            // Индексируем только если кэш пуст или размер файла изменился
            if (_subroutineIndexCache.Count == 0 || _lastFileLinesCount != fileLines.Count)
            {
                await Task.Run(() => BuildSubroutineIndex(fileLines));
                _lastFileLinesCount = fileLines.Count;
            }

            var opcodes = new string[xrefs.Count];
            var processedCount = 0;
            var lockObj = new object();

            // Последовательный поиск опкодов (временно отключен параллелизм для диагностики)
            // Если проблема не в параллелизме, можно вернуть Parallel.For
            await Task.Run(() =>
            {
                for (int i = 0; i < xrefs.Count; i++)
                {
                    var foundOpcode = false;
                    var xrefList = xrefs[i]?.ToList() ?? new List<string>();

                    foreach (var xrefLine in xrefList)
                    {
                        // Извлекаем адрес подпрограммы
                        var subAddress = ExtractSubroutineAddress(xrefLine);
                        if (string.IsNullOrEmpty(subAddress))
                            continue;

                        // Ищем опкод в подпрограмме
                        var opcode = FindOpcodeInSubroutine(fileLines, subAddress);
                        if (!string.IsNullOrEmpty(opcode))
                        {
                            opcodes[i] = opcode;
                            foundOpcode = true;
                            break;
                        }
                    }

                    if (!foundOpcode)
                    {
                        opcodes[i] = "0xfff"; // Не найден
                    }

                    // Отчет о прогрессе
                    if (i % 10 == 0 && progress != null)
                    {
                        var percent = (int)((double)i / xrefs.Count * 100);
                        progress.Report(percent);
                    }
                }

                if (progress != null)
                {
                    progress.Report(100);
                }
            });

            return opcodes.ToList();
        }

        /// <summary>
        /// Строит индекс подпрограмм для быстрого поиска
        /// </summary>
        private void BuildSubroutineIndex(List<string> fileLines)
        {
            _subroutineIndexCache.Clear();
            
            // Последовательная индексация для гарантии правильности
            // Параллелизация здесь не критична, так как это делается один раз
            for (int i = 0; i < fileLines.Count; i++)
            {
                var line = fileLines[i];
                // Ищем строки вида "sub_XXXXXXXX    proc near" или "sub_XXXXXXXX    proc far"
                // Также проверяем строки, которые начинаются с адреса подпрограммы
                var match = RegexPatterns.SubroutinePattern.Match(line);
                if (match.Success)
                {
                    var subAddress = match.Value;
                    // Проверяем, что это действительно начало подпрограммы
                    // Подпрограмма начинается со строки вида "sub_XXXXXXXX    proc near" или просто "sub_XXXXXXXX"
                    var trimmedLine = line.TrimStart();
                    if (line.Contains("proc near") || line.Contains("proc far") || 
                        trimmedLine.StartsWith(subAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        // Используем индекс или обновляем, если нашли более раннее вхождение
                        // Важно: используем первое вхождение (минимальный индекс)
                        _subroutineIndexCache.AddOrUpdate(subAddress, i, (key, oldValue) => Math.Min(oldValue, i));
                    }
                }
            }
        }

        /// <summary>
        /// Извлекает адрес подпрограммы из строки xref
        /// </summary>
        private string ExtractSubroutineAddress(string xrefLine)
        {
            if (string.IsNullOrWhiteSpace(xrefLine))
                return null;

            var match = RegexPatterns.SubroutinePattern.Match(xrefLine);
            return match.Success ? match.Value : null;
        }

        /// <summary>
        /// Находит опкод в подпрограмме
        /// </summary>
        private string FindOpcodeInSubroutine(List<string> fileLines, string subAddress)
        {
            if (string.IsNullOrEmpty(subAddress) || fileLines == null)
                return null;

            // Используем кэш для быстрого поиска начала подпрограммы
            int subroutineStartIndex = -1;
            if (!_subroutineIndexCache.TryGetValue(subAddress, out subroutineStartIndex))
            {
                // Если не найдено в кэше, ищем линейно (fallback)
                var subroutineStartPattern = RegexPatterns.CreateSubroutineStartPattern(subAddress);
                for (var i = 0; i < fileLines.Count; i++)
                {
                    if (subroutineStartPattern.IsMatch(fileLines[i]))
                    {
                        subroutineStartIndex = i;
                        // Добавляем в кэш для будущих поисков
                        _subroutineIndexCache.TryAdd(subAddress, i);
                        break;
                    }
                }
            }
            
            if (subroutineStartIndex == -1)
                return null;

            // Ищем опкод до конца подпрограммы
            var offsetPattern = "";
            var foundOffset = false;

            for (var i = subroutineStartIndex; i < fileLines.Count; i++)
            {
                var line = fileLines[i];

                // Проверяем конец подпрограммы
                if (RegexPatterns.EndProcedure.IsMatch(line))
                    break;

                // Ищем offset инструкцию
                if (!foundOffset)
                {
                    var offsetMatch = RegexPatterns.OffsetPattern.Match(line);
                    #if DEBUG
                    Trace.WriteLine($"[OPCODE DEBUG] Checking offset line {i}: {line.Trim()}");
                    Trace.WriteLine($"[OPCODE DEBUG] Offset match success: {offsetMatch.Success}, Groups count: {offsetMatch.Groups.Count}");
                    #endif
                    if (offsetMatch.Success)
                    {
                        #if DEBUG
                        for (int g = 1; g < offsetMatch.Groups.Count; g++)
                        {
                            if (offsetMatch.Groups[g].Success)
                            {
                                Trace.WriteLine($"[OPCODE DEBUG] Offset Group[{g}]: '{offsetMatch.Groups[g].Value}'");
                            }
                        }
                        #endif
                        // Определяем смещение для поиска опкода
                        offsetPattern = DetermineOffsetPattern(offsetMatch);
                        foundOffset = !string.IsNullOrEmpty(offsetPattern);
                        #if DEBUG
                        if (foundOffset)
                        {
                            Trace.WriteLine($"[OPCODE DEBUG] Created opcode pattern: {offsetPattern}");
                        }
                        #endif
                    }
                }

                    // Если нашли offset, ищем опкод
                    if (foundOffset && !string.IsNullOrEmpty(offsetPattern))
                    {
                        // Используем кэш для Regex паттернов
                        var opcodePattern = _opcodePatternCache.GetOrAdd(offsetPattern, 
                            pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                        var opcodeMatch = opcodePattern.Match(line);
                        
                        #if DEBUG
                        Trace.WriteLine($"[OPCODE DEBUG] Checking opcode line {i}: {line.Trim()}");
                        Trace.WriteLine($"[OPCODE DEBUG] Opcode match success: {opcodeMatch.Success}, Groups count: {opcodeMatch.Groups.Count}");
                        #endif
                        
                        if (opcodeMatch.Success)
                        {
                            #if DEBUG
                            for (int g = 0; g < opcodeMatch.Groups.Count; g++)
                            {
                                Trace.WriteLine($"[OPCODE DEBUG] Opcode Group[{g}]: Success={opcodeMatch.Groups[g].Success}, Length={opcodeMatch.Groups[g].Length}, Value='{opcodeMatch.Groups[g].Value}'");
                            }
                            #endif
                            
                            // Для паттернов с ebp+var_XXX опкод находится в Groups[2]
                            // Для паттерна с любым регистром +4 (Group 2): Groups[1] = "dword ptr " (если есть), Groups[2] = регистр, Groups[3] = опкод
                            // Для паттерна с ebp+var_XXX+4: Groups[1] = "dword ptr " (если есть), Groups[2] = опкод, Groups[3] = пробел/;/$ в конце
                            // Для других паттернов опкод находится в Groups[1]
                            string opcodeValue = null;
                            
                            // Сначала проверяем, является ли это паттерном с ebp+var_XXX+4
                            // В этом случае Groups[2] содержит опкод, а Groups[3] содержит пробел/;/$ в конце
                            if (opcodeMatch.Groups.Count >= 3 && opcodeMatch.Groups[2].Length > 0)
                            {
                                var group2Value = opcodeMatch.Groups[2].Value.Trim();
                                // Если Groups[2] выглядит как опкод (hex число с h или без), а не как регистр
                                if (System.Text.RegularExpressions.Regex.IsMatch(group2Value, @"^[0-9a-fA-F]+h?$|^\d+$"))
                                {
                                    // Это паттерн с ebp+var_XXX+4: Groups[2] = опкод
                                    opcodeValue = group2Value;
                                    #if DEBUG
                                    Trace.WriteLine($"[OPCODE DEBUG] Extracting opcode from Groups[2] (ebp+var_XXX+4 pattern): '{opcodeValue}'");
                                    #endif
                                }
                                else if (opcodeMatch.Groups.Count >= 4 && opcodeMatch.Groups[3].Length > 0)
                                {
                                    // Это паттерн с любым регистром +4: Groups[2] = регистр, Groups[3] = опкод
                                    opcodeValue = opcodeMatch.Groups[3].Value;
                                    #if DEBUG
                                    Trace.WriteLine($"[OPCODE DEBUG] Extracting opcode from Groups[3] (any register +4 pattern): '{opcodeValue}'");
                                    #endif
                                }
                                else
                                {
                                    // Паттерн с dword ptr и ebp+var_XXX (без +4): Groups[2] = опкод
                                    opcodeValue = group2Value;
                                    #if DEBUG
                                    Trace.WriteLine($"[OPCODE DEBUG] Extracting opcode from Groups[2] (ebp+var_XXX pattern): '{opcodeValue}'");
                                    #endif
                                }
                            }
                            else if (opcodeMatch.Groups.Count >= 2 && opcodeMatch.Groups[1].Length > 0)
                            {
                                // Обычный паттерн: Groups[1] = опкод
                                opcodeValue = opcodeMatch.Groups[1].Value;
                                #if DEBUG
                                Trace.WriteLine($"[OPCODE DEBUG] Extracting opcode from Groups[1] (simple pattern): '{opcodeValue}'");
                                #endif
                            }

                            if (!string.IsNullOrEmpty(opcodeValue))
                            {
                                var formatted = FormatOpcode(opcodeValue);
                                #if DEBUG
                                Trace.WriteLine($"[OPCODE DEBUG] Formatted opcode: '{formatted}'");
                                #endif
                                return formatted;
                            }
                            #if DEBUG
                            else
                            {
                                Trace.WriteLine($"[OPCODE DEBUG] No valid opcode value found in groups");
                            }
                            #endif
                        }
                    }
            }

            return null;
        }

        /// <summary>
        /// Определяет паттерн смещения для поиска опкода
        /// </summary>
        private string DetermineOffsetPattern(Match offsetMatch)
        {
            // Group 1: [ebp+var_50] -> ищем [ebp+var_4C] (уменьшаем на 4)
            // Это для паттерна без "dword ptr": mov [ebp+var_XXX], offset off_XXX
            if (offsetMatch.Groups[1].Success)
            {
                var baseOffset = offsetMatch.Groups[1].Value;
                #if DEBUG
                Trace.WriteLine($"[OPCODE DEBUG] Group 1 matched: baseOffset='{baseOffset}', using DecreaseOffset");
                #endif
                return DecreaseOffset(baseOffset, 4);
            }

            // Group 2: [eax] -> ищем [eax+4] (увеличиваем на 4)
            if (offsetMatch.Groups[2].Success)
            {
                var baseOffset = offsetMatch.Groups[2].Value;
                #if DEBUG
                Trace.WriteLine($"[OPCODE DEBUG] Group 2 matched: baseOffset='{baseOffset}', using IncreaseOffset");
                #endif
                return IncreaseOffset(baseOffset, 4);
            }

            // Group 3: [esi+10C0h] -> ищем [esi+10C4h] (увеличиваем на 4)
            if (offsetMatch.Groups[3].Success)
            {
                var baseOffset = offsetMatch.Groups[3].Value;
                #if DEBUG
                Trace.WriteLine($"[OPCODE DEBUG] Group 3 matched: baseOffset='{baseOffset}', using IncreaseOffset");
                #endif
                return IncreaseOffset(baseOffset, 4);
            }

            // Group 4: [ebp+var_34] -> ищем [ebp+var_34+4] (увеличиваем на 4)
            if (offsetMatch.Groups[4].Success)
            {
                var baseOffset = offsetMatch.Groups[4].Value;
                #if DEBUG
                Trace.WriteLine($"[OPCODE DEBUG] Group 4 matched: baseOffset='{baseOffset}', using IncreaseOffset");
                #endif
                return IncreaseOffset(baseOffset, 4);
            }

            // Group 5: [ebp+var_34+8] -> ищем [ebp+var_34+Ch] (увеличиваем на 4)
            if (offsetMatch.Groups[5].Success)
            {
                var baseOffset = offsetMatch.Groups[5].Value;
                #if DEBUG
                Trace.WriteLine($"[OPCODE DEBUG] Group 5 matched: baseOffset='{baseOffset}', using IncreaseOffset");
                #endif
                return IncreaseOffset(baseOffset, 4);
            }

            #if DEBUG
            Trace.WriteLine($"[OPCODE DEBUG] No offset group matched");
            #endif
            return null;
        }

        /// <summary>
        /// Уменьшает смещение на указанное значение (аналог Decrease4)
        /// </summary>
        private string DecreaseOffset(string offset, int value)
        {
            try
            {
                // mov     [ebp+var_48C], offset SCDominionDataPacket_0x0b2
                // mov     [ebp+var_488], 0B2h
                string postfix = "";
                string prefix;
                string numb;
                int num;
                string fstr;
                string find;

                if (offset.LastIndexOf("h", StringComparison.Ordinal) > 0)
                {
                    offset = offset.Replace("h", "");
                    postfix = "h";
                }

                if (offset.LastIndexOf("_", StringComparison.Ordinal) > 0)
                {
                    // esi+var_10C
                    var offsetIndex = offset.LastIndexOf("_", StringComparison.Ordinal) + 1;
                    prefix = offset.Substring(0, offsetIndex);
                    numb = offset.Substring(offsetIndex);
                    num = Convert.ToInt32(numb, 16) - value;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                    return find;
                }
                else if (offset.LastIndexOf("+", StringComparison.Ordinal) > 0)
                {
                    // esi+10C0
                    var offsetIndex = offset.LastIndexOf("+", StringComparison.Ordinal) + 1;
                    prefix = offset.Substring(0, offsetIndex);
                    numb = offset.Substring(offsetIndex);
                    num = Convert.ToInt32(numb, 16) - value;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                    return find;
                }
                else
                {
                    // eax
                    fstr = offset + "\\-" + value;
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                    return find;
                }
            }
            catch
            {
                return "@@@@@@";
            }
        }

        /// <summary>
        /// Увеличивает смещение на указанное значение (аналог Increase4)
        /// </summary>
        private string IncreaseOffset(string offset, int value)
        {
            try
            {
                // mov     dword ptr [esi+10C0h], offset SCUnitDeathPacket_0x1f5
                // mov     dword ptr [esi+10C4h], 1F5h
                // mov     dword ptr [ebp+var_34], offset off_39D11730
                // mov     dword ptr [ebp+var_34+4], 43h
                string postfix = "";
                string prefix;
                string numb;
                int num;
                string fstr;
                string find;

                if (offset.LastIndexOf("h", StringComparison.Ordinal) > 0)
                {
                    offset = offset.Replace("h", "");
                    postfix = "h";
                }

                // Специальная обработка для ebp+var_XXX - нужно добавить +4, а не изменять число
                if (offset.Contains("ebp+var_"))
                {
                    // ebp+var_34 -> ebp+var_34+4
                    // ebp+var_34+8 -> ebp+var_34+Ch
                    if (offset.LastIndexOf("+", StringComparison.Ordinal) > offset.IndexOf("var_"))
                    {
                        // Уже есть + после var_, например: ebp+var_34+8
                        var offsetIndex = offset.LastIndexOf("+", StringComparison.Ordinal) + 1;
                        prefix = offset.Substring(0, offsetIndex);
                        numb = offset.Substring(offsetIndex);
                        num = Convert.ToInt32(numb, 16) + value;
                        numb = num.ToString("X");
                        fstr = prefix + numb + postfix;
                    }
                    else
                    {
                        // Нет + после var_, например: ebp+var_34
                        fstr = offset + "+" + value.ToString("X");
                    }
                    fstr = fstr.Replace("+", "\\+");
                    // Учитываем возможное наличие "dword ptr" перед "[ebp+var_XXX+число]"
                    // Также учитываем возможные комментарии после опкода
                    find = "mov\\s+(dword\\s+ptr\\s+)?\\[" + fstr + "\\],\\s*([0-9a-fA-F]+h?)(\\s|;|$)";
                    return find;
                }
                else if (offset.LastIndexOf("_", StringComparison.Ordinal) > 0)
                {
                    // esi+var_10C
                    var offsetIndex = offset.LastIndexOf("_", StringComparison.Ordinal) + 1;
                    prefix = offset.Substring(0, offsetIndex);
                    numb = offset.Substring(offsetIndex);
                    num = Convert.ToInt32(numb, 16) + value;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                    return find;
                }
                else if (offset.LastIndexOf("+", StringComparison.Ordinal) > 0)
                {
                    // esi+10C0
                    var offsetIndex = offset.LastIndexOf("+", StringComparison.Ordinal) + 1;
                    prefix = offset.Substring(0, offsetIndex);
                    numb = offset.Substring(offsetIndex);
                    num = Convert.ToInt32(numb, 16) + value;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    // Учитываем возможное наличие "dword ptr" перед "[register+offset]"
                    find = "(dword\\s+ptr\\s+)?\\[" + fstr + "\\],\\s*([0-9a-fA-F]+h?)(\\s|;|$)";
                    return find;
                }
                else
                {
                    // eax, ecx, esi и т.д. - просто регистр
                    // Опкод может быть в любом регистре с +4, не обязательно в том же
                    // Например: mov dword ptr [ecx], offset off_XXX -> mov dword ptr [eax+4], 6Eh
                    // Учитываем возможное наличие "dword ptr" перед "[register+offset]"
                    find = "mov\\s+(dword\\s+ptr\\s+)?\\[(\\w+)\\+4\\],\\s+([0-9a-fA-F]+h?|\\d+)(\\s|;|$)";
                    return find;
                }
            }
            catch
            {
                return "@@@@@@";
            }
        }

        /// <summary>
        /// Форматирует опкод в стандартный вид (0xXXX)
        /// </summary>
        private string FormatOpcode(string opcode)
        {
            if (string.IsNullOrWhiteSpace(opcode))
                return "0xfff";

            // Убираем суффикс 'h' если есть
            opcode = opcode.TrimEnd('h', 'H');

            // Парсим как hex
            if (int.TryParse(opcode, NumberStyles.HexNumber, null, out var value))
            {
                return $"0x{value:X3}";
            }

            return "0xfff";
        }
    }
}

