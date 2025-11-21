using System;
using System.Collections.Generic;
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

            var opcodes = new List<string>();
            var notFoundCount = 0;

            await Task.Run(() =>
            {
                for (var i = 0; i < xrefs.Count; i++)
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
                            opcodes.Add(opcode);
                            foundOpcode = true;
                            break;
                        }
                    }

                    if (!foundOpcode)
                    {
                        opcodes.Add("0xfff"); // Не найден
                        notFoundCount++;
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

            return opcodes;
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

            // Ищем начало подпрограммы
            var subroutineStartIndex = -1;
            var subroutineStartPattern = RegexPatterns.CreateSubroutineStartPattern(subAddress);

            for (var i = 0; i < fileLines.Count; i++)
            {
                if (subroutineStartPattern.IsMatch(fileLines[i]))
                {
                    subroutineStartIndex = i;
                    break;
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
                    if (offsetMatch.Success)
                    {
                        // Определяем смещение для поиска опкода
                        offsetPattern = DetermineOffsetPattern(offsetMatch);
                        foundOffset = !string.IsNullOrEmpty(offsetPattern);
                    }
                }

                    // Если нашли offset, ищем опкод
                    if (foundOffset && !string.IsNullOrEmpty(offsetPattern))
                    {
                        var opcodePattern = new Regex(offsetPattern, RegexOptions.IgnoreCase);
                        var opcodeMatch = opcodePattern.Match(line);

                        if (opcodeMatch.Success && opcodeMatch.Groups.Count >= 2)
                        {
                            // Извлекаем опкод из группы 1
                            var opcodeValue = opcodeMatch.Groups[1].Value;
                            if (!string.IsNullOrEmpty(opcodeValue))
                            {
                                return FormatOpcode(opcodeValue);
                            }
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
            if (offsetMatch.Groups[1].Success)
            {
                var baseOffset = offsetMatch.Groups[1].Value;
                return DecreaseOffset(baseOffset, 4);
            }

            // Group 2: [eax] -> ищем [eax+4] (увеличиваем на 4)
            if (offsetMatch.Groups[2].Success)
            {
                var baseOffset = offsetMatch.Groups[2].Value;
                return IncreaseOffset(baseOffset, 4);
            }

            // Group 3: [esi+10C0h] -> ищем [esi+10C4h] (увеличиваем на 4)
            if (offsetMatch.Groups[3].Success)
            {
                var baseOffset = offsetMatch.Groups[3].Value;
                return IncreaseOffset(baseOffset, 4);
            }

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
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                    return find;
                }
                else
                {
                    // eax
                    fstr = offset + "\\+" + value;
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

