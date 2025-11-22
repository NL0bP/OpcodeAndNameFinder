using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        Dictionary<PacketType, ObservableCollection<string>> CompareNames { get; }
        Dictionary<PacketType, Dictionary<int, int>> InUseMapping { get; }
        Dictionary<int, bool> IsRenameDestination { get; }
        Dictionary<PacketType, ObservableCollection<string>> CompareOutNames { get; }
        
        // Опкоды
        Dictionary<PacketType, ObservableCollection<string>> SourceOpcodes { get; }
        Dictionary<PacketType, ObservableCollection<string>> DestinationOpcodes { get; }

        // Списки имен пакетов (Source)
        Dictionary<PacketType, ObservableCollection<string>> SourcePacketNames { get; }
        Dictionary<PacketType, ObservableCollection<string>> SourceSubNames { get; }
        
        // Списки имен пакетов (Destination)
        Dictionary<PacketType, ObservableCollection<string>> DestinationPacketNames { get; }
        Dictionary<PacketType, ObservableCollection<string>> DestinationSubNames { get; }

        // Файловые данные
        List<string> SourceFileLines { get; }
        List<string> DestinationFileLines { get; }

        // Структуры пакетов (Source) - используем Struc для обратной совместимости
        // TODO: В будущем можно мигрировать на StructureField из Models
        Dictionary<PacketType, Dictionary<int, ObservableCollection<NameFinder.Struc>>> SourceStructures { get; }
        
        // Структуры пакетов (Destination)
        Dictionary<PacketType, Dictionary<int, ObservableCollection<NameFinder.Struc>>> DestinationStructures { get; }

        void Clear();
        void ClearSource();
        void ClearDestination();
    }
}

