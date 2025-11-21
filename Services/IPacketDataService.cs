using System.Collections.Generic;
using NameFinder.Models;

namespace NameFinder.Services
{
    /// <summary>
    /// Сервис для работы с данными пакетов
    /// </summary>
    public interface IPacketDataService
    {
        // Source data (известные имена)
        Dictionary<PacketType, List<PacketInfo>> SourcePackets { get; }
        Dictionary<PacketType, Dictionary<int, List<string>>> SourceXrefs { get; }
        
        // Destination data (неизвестные имена)
        Dictionary<PacketType, List<PacketInfo>> DestinationPackets { get; }
        Dictionary<PacketType, Dictionary<int, List<string>>> DestinationXrefs { get; }
        
        // Сравнение
        Dictionary<PacketType, List<string>> CompareNames { get; }
        Dictionary<PacketType, Dictionary<int, int>> InUseMapping { get; }
        
        // Опкоды
        Dictionary<PacketType, List<string>> SourceOpcodes { get; }
        Dictionary<PacketType, List<string>> DestinationOpcodes { get; }

        // Списки имен пакетов (Source)
        Dictionary<PacketType, List<string>> SourcePacketNames { get; }
        Dictionary<PacketType, List<string>> SourceSubNames { get; }
        
        // Списки имен пакетов (Destination)
        Dictionary<PacketType, List<string>> DestinationPacketNames { get; }
        Dictionary<PacketType, List<string>> DestinationSubNames { get; }

        // Файловые данные
        List<string> SourceFileLines { get; }
        List<string> DestinationFileLines { get; }

        // Структуры пакетов (Source) - используем Struc для обратной совместимости
        // TODO: В будущем можно мигрировать на StructureField из Models
        Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>> SourceStructures { get; }
        
        // Структуры пакетов (Destination)
        Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>> DestinationStructures { get; }

        void Clear();
        void ClearSource();
        void ClearDestination();
    }
}

