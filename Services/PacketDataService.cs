using System.Collections.Generic;
using System.Collections.ObjectModel;
using NameFinder.Models;

namespace NameFinder.Services
{
    /// <summary>
    /// Реализация сервиса для работы с данными пакетов
    /// </summary>
    public class PacketDataService : IPacketDataService
    {
        public Dictionary<PacketType, List<PacketInfo>> SourcePackets { get; } = new Dictionary<PacketType, List<PacketInfo>>
        {
            { PacketType.CS, new List<PacketInfo>() },
            { PacketType.SC, new List<PacketInfo>() }
        };

        public Dictionary<PacketType, Dictionary<int, List<string>>> SourceXrefs { get; } = new Dictionary<PacketType, Dictionary<int, List<string>>>
        {
            { PacketType.CS, new Dictionary<int, List<string>>() },
            { PacketType.SC, new Dictionary<int, List<string>>() }
        };

        public Dictionary<PacketType, List<PacketInfo>> DestinationPackets { get; } = new Dictionary<PacketType, List<PacketInfo>>
        {
            { PacketType.CS, new List<PacketInfo>() },
            { PacketType.SC, new List<PacketInfo>() }
        };

        public Dictionary<PacketType, Dictionary<int, List<string>>> DestinationXrefs { get; } = new Dictionary<PacketType, Dictionary<int, List<string>>>
        {
            { PacketType.CS, new Dictionary<int, List<string>>() },
            { PacketType.SC, new Dictionary<int, List<string>>() }
        };

        public Dictionary<PacketType, ObservableCollection<string>> CompareNames { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        public Dictionary<PacketType, Dictionary<int, int>> InUseMapping { get; } = new Dictionary<PacketType, Dictionary<int, int>>
        {
            { PacketType.CS, new Dictionary<int, int>() },
            { PacketType.SC, new Dictionary<int, int>() }
        };

        public Dictionary<int, bool> IsRenameDestination { get; } = new Dictionary<int, bool>();

        public Dictionary<PacketType, ObservableCollection<string>> CompareOutNames { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        public Dictionary<PacketType, ObservableCollection<string>> SourceOpcodes { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        public Dictionary<PacketType, ObservableCollection<string>> DestinationOpcodes { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        // Списки имен пакетов (Source)
        public Dictionary<PacketType, ObservableCollection<string>> SourcePacketNames { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        public Dictionary<PacketType, ObservableCollection<string>> SourceSubNames { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        // Списки имен пакетов (Destination)
        public Dictionary<PacketType, ObservableCollection<string>> DestinationPacketNames { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        public Dictionary<PacketType, ObservableCollection<string>> DestinationSubNames { get; } = new Dictionary<PacketType, ObservableCollection<string>>
        {
            { PacketType.CS, new ObservableCollection<string>() },
            { PacketType.SC, new ObservableCollection<string>() }
        };

        public List<string> SourceFileLines { get; } = new List<string>();
        public List<string> DestinationFileLines { get; } = new List<string>();

        // Структуры пакетов (Source) - используем Struc для обратной совместимости
        public Dictionary<PacketType, Dictionary<int, ObservableCollection<NameFinder.Struc>>> SourceStructures { get; } = new Dictionary<PacketType, Dictionary<int, ObservableCollection<NameFinder.Struc>>>
        {
            { PacketType.CS, new Dictionary<int, ObservableCollection<NameFinder.Struc>>() },
            { PacketType.SC, new Dictionary<int, ObservableCollection<NameFinder.Struc>>() }
        };

        // Структуры пакетов (Destination)
        public Dictionary<PacketType, Dictionary<int, ObservableCollection<NameFinder.Struc>>> DestinationStructures { get; } = new Dictionary<PacketType, Dictionary<int, ObservableCollection<NameFinder.Struc>>>
        {
            { PacketType.CS, new Dictionary<int, ObservableCollection<NameFinder.Struc>>() },
            { PacketType.SC, new Dictionary<int, ObservableCollection<NameFinder.Struc>>() }
        };

        public void Clear()
        {
            ClearSource();
            ClearDestination();
        }

        public void ClearSource()
        {
            foreach (var packets in SourcePackets.Values)
            {
                packets.Clear();
            }
            foreach (var xrefs in SourceXrefs.Values)
            {
                xrefs.Clear();
            }
            SourceOpcodes[PacketType.CS].Clear();
            SourceOpcodes[PacketType.SC].Clear();
            SourcePacketNames[PacketType.CS].Clear();
            SourcePacketNames[PacketType.SC].Clear();
            SourceSubNames[PacketType.CS].Clear();
            SourceSubNames[PacketType.SC].Clear();
            SourceStructures[PacketType.CS].Clear();
            SourceStructures[PacketType.SC].Clear();
            SourceFileLines.Clear();
        }

        public void ClearDestination()
        {
            foreach (var packets in DestinationPackets.Values)
            {
                packets.Clear();
            }
            foreach (var xrefs in DestinationXrefs.Values)
            {
                xrefs.Clear();
            }
            DestinationOpcodes[PacketType.CS].Clear();
            DestinationOpcodes[PacketType.SC].Clear();
            DestinationPacketNames[PacketType.CS].Clear();
            DestinationPacketNames[PacketType.SC].Clear();
            DestinationSubNames[PacketType.CS].Clear();
            DestinationSubNames[PacketType.SC].Clear();
            CompareNames[PacketType.CS].Clear();
            CompareNames[PacketType.SC].Clear();
            CompareOutNames[PacketType.CS].Clear();
            CompareOutNames[PacketType.SC].Clear();
            InUseMapping[PacketType.CS].Clear();
            InUseMapping[PacketType.SC].Clear();
            IsRenameDestination.Clear();
            DestinationStructures[PacketType.CS].Clear();
            DestinationStructures[PacketType.SC].Clear();
            DestinationFileLines.Clear();
        }
    }
}

