using System.Collections.Generic;
using System.Collections.ObjectModel;
using NameFinder.Models;

namespace NameFinder.Services
{
    /// <summary>
    /// Интерфейс для поиска структур пакетов
    /// </summary>
    public interface IStructureFinderService
    {
        /// <summary>
        /// Находит структуры для Source пакетов
        /// </summary>
        /// <param name="fileLines">Строки файла</param>
        /// <param name="packetType">Тип пакета (CS/SC)</param>
        /// <param name="searchPattern">Паттерн поиска</param>
        /// <param name="progressCallback">Callback для обновления прогресса</param>
        /// <returns>Словарь структур по индексам пакетов</returns>
        Dictionary<int, ObservableCollection<NameFinder.Struc>> FindSourceStructures(
            List<string> fileLines,
            PacketType packetType,
            string searchPattern,
            System.Action<int> progressCallback = null);

        /// <summary>
        /// Находит структуры для Destination пакетов
        /// </summary>
        /// <param name="fileLines">Строки файла</param>
        /// <param name="packetType">Тип пакета (CS/SC)</param>
        /// <param name="searchPattern">Паттерн поиска</param>
        /// <param name="progressCallback">Callback для обновления прогресса</param>
        /// <returns>Словарь структур по индексам пакетов</returns>
        Dictionary<int, ObservableCollection<NameFinder.Struc>> FindDestinationStructures(
            List<string> fileLines,
            PacketType packetType,
            string searchPattern,
            System.Action<int> progressCallback = null);

        /// <summary>
        /// Находит структуру по адресу (для Source)
        /// </summary>
        ObservableCollection<NameFinder.Struc> FindStructureIn(string address, List<string> fileLines, int maxDepth = 10, bool useCallSpaces4 = false);

        /// <summary>
        /// Находит структуру по адресу (для Destination)
        /// </summary>
        ObservableCollection<NameFinder.Struc> FindStructureOut(string address, List<string> fileLines, int maxDepth = 10, bool useCallSpaces4 = true);

        /// <summary>
        /// Сбрасывает глубину рекурсии для Source
        /// </summary>
        void ResetDepthIn();

        /// <summary>
        /// Сбрасывает глубину рекурсии для Destination
        /// </summary>
        void ResetDepthOut();
    }
}

