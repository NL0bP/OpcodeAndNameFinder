namespace NameFinder.Constants
{
    /// <summary>
    /// Константы для работы с пакетами
    /// </summary>
    public static class PacketConstants
    {
        /// <summary>
        /// Максимальное значение опкода для специальной обработки
        /// </summary>
        public const int MaxOpcodeValue = 32;

        /// <summary>
        /// Минимальное смещение для имени пакета
        /// </summary>
        public const int MinNameOffset = 3;

        /// <summary>
        /// Максимальная глубина рекурсии при поиске структур
        /// </summary>
        public const int MaxDepth = 10;

        /// <summary>
        /// Префикс для неизвестных адресов
        /// </summary>
        public const string UnknownAddressPrefix = "off_";

        /// <summary>
        /// Префикс для подпрограмм
        /// </summary>
        public const string SubroutinePrefix = "sub_";

        /// <summary>
        /// Префикс для X2 функций
        /// </summary>
        public const string X2Prefix = "X2";

        /// <summary>
        /// Префикс для CS пакетов
        /// </summary>
        public const string CSPrefix = "CS";

        /// <summary>
        /// Префикс для SC пакетов
        /// </summary>
        public const string SCPrefix = "SC";

        /// <summary>
        /// Имя функции для CS пакетов
        /// </summary>
        public const string CS_PACKETS_RETURN_0 = "CS_PACKETS_return_0";

        /// <summary>
        /// Имя функции для SC пакетов
        /// </summary>
        public const string SC_PACKETS_RETURN_2 = "SC_PACKETS_return_2";
    }
}

