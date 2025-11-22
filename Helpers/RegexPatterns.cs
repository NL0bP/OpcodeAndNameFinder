using System.Text.RegularExpressions;

namespace NameFinder.Helpers
{
    /// <summary>
    /// Компилированные регулярные выражения для парсинга asm файлов
    /// </summary>
    public static class RegexPatterns
    {
        /// <summary>
        /// Конец подпрограммы
        /// </summary>
        public static readonly Regex EndProcedure = new Regex(@"\s+endp\s*", RegexOptions.Compiled);

        /// <summary>
        /// Поиск offset в mov инструкциях (только с off_)
        /// </summary>
        public static readonly Regex OffsetPattern = new Regex(
            @"mov\s+\[(\w+\+[0-9a-fA-F]+h?|\w+\+\w+)\],\soffset\s+off_|mov\s+dword\s+ptr\s+\[(\w+)\],\soffset\s+off_|mov\s+dword\s+ptr\s+\[(\w+\+[0-9a-fA-F]+h?|\w+\+\w+)\],\soffset\s+off_|mov\s+dword\s+ptr\s+\[(ebp\+var_\w+)\],\soffset\s+off_|mov\s+dword\s+ptr\s+\[(ebp\+var_\w+\+[0-9a-fA-F]+)\],\soffset\s+off_",
            RegexOptions.Compiled);

        /// <summary>
        /// Поиск опкода в mov инструкциях
        /// </summary>
        public static readonly Regex OpcodePattern = new Regex(
            @"\[\w+\-(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)\w+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h)\w+[0-9a-fA-F]+\+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Поиск подпрограммы (sub_ или X2)
        /// </summary>
        public static readonly Regex SubroutinePattern = new Regex(@"sub_\w+|X2\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Создает regex для поиска начала подпрограммы
        /// </summary>
        public static Regex CreateSubroutineStartPattern(string subAddress)
        {
            return new Regex(@"^" + Regex.Escape(subAddress), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// Создает regex для поиска опкода по смещению
        /// </summary>
        public static Regex CreateOpcodeOffsetPattern(string offset)
        {
            return new Regex(Regex.Escape(offset), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}

