using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NameFinder.Models;

namespace NameFinder.Services
{
    /// <summary>
    /// Интерфейс для поиска опкодов в asm файлах
    /// </summary>
    public interface IOpcodeFinderService
    {
        /// <summary>
        /// Находит опкоды для пакетов
        /// </summary>
        /// <param name="packetType">Тип пакета (CS/SC)</param>
        /// <param name="source">Источник (In/Out)</param>
        /// <param name="fileLines">Строки файла</param>
        /// <param name="xrefs">Xrefs для каждого пакета</param>
        /// <param name="progress">Отчет о прогрессе</param>
        /// <returns>Список найденных опкодов</returns>
        Task<List<string>> FindOpcodesAsync(
            PacketType packetType,
            PacketSource source,
            List<string> fileLines,
            Dictionary<int, List<string>> xrefs,
            IProgress<int> progress = null);
    }
}

