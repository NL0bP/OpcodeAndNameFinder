using System.Collections.Generic;
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

        public Dictionary<PacketType, List<string>> CompareNames { get; } = new Dictionary<PacketType, List<string>>
        {
            { PacketType.CS, new List<string>() },
            { PacketType.SC, new List<string>() }
        };

        public Dictionary<PacketType, Dictionary<int, int>> InUseMapping { get; } = new Dictionary<PacketType, Dictionary<int, int>>
        {
            { PacketType.CS, new Dictionary<int, int>() },
            { PacketType.SC, new Dictionary<int, int>() }
        };

        public Dictionary<int, bool> IsRenameDestination { get; } = new Dictionary<int, bool>();

        public Dictionary<PacketType, List<string>> SourceOpcodes { get; } = new Dictionary<PacketType, List<string>>
        {
            { PacketType.CS, new List<string>() },
            { PacketType.SC, new List<string>() }
        };

        public Dictionary<PacketType, List<string>> DestinationOpcodes { get; } = new Dictionary<PacketType, List<string>>
        {
            { PacketType.CS, new List<string>() },
            { PacketType.SC, new List<string>() }
        };

        // Списки имен пакетов (Source)
        public Dictionary<PacketType, List<string>> SourcePacketNames { get; } = new Dictionary<PacketType, List<string>>
        {
            { PacketType.CS, new List<string>() },
            { PacketType.SC, new List<string>() }
        };

        public Dictionary<PacketType, List<string>> SourceSubNames { get; } = new Dictionary<PacketType, List<string>>
        {
            { PacketType.CS, new List<string>() },
            { PacketType.SC, new List<string>() }
        };

        // Списки имен пакетов (Destination)
        public Dictionary<PacketType, List<string>> DestinationPacketNames { get; } = new Dictionary<PacketType, List<string>>
        {
            { PacketType.CS, new List<string>() },
            { PacketType.SC, new List<string>() }
        };

        public Dictionary<PacketType, List<string>> DestinationSubNames { get; } = new Dictionary<PacketType, List<string>>
        {
            { PacketType.CS, new List<string>() },
            { PacketType.SC, new List<string>() }
        };

        public List<string> SourceFileLines { get; } = new List<string>();
        public List<string> DestinationFileLines { get; } = new List<string>();

        // Структуры пакетов (Source) - используем Struc для обратной совместимости
        public Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>> SourceStructures { get; } = new Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>>
        {
            { PacketType.CS, new Dictionary<int, List<NameFinder.Struc>>() },
            { PacketType.SC, new Dictionary<int, List<NameFinder.Struc>>() }
        };

        // Структуры пакетов (Destination)
        public Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>> DestinationStructures { get; } = new Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>>
        {
            { PacketType.CS, new Dictionary<int, List<NameFinder.Struc>>() },
            { PacketType.SC, new Dictionary<int, List<NameFinder.Struc>>() }
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
            InUseMapping[PacketType.CS].Clear();
            InUseMapping[PacketType.SC].Clear();
            IsRenameDestination.Clear();
            DestinationStructures[PacketType.CS].Clear();
            DestinationStructures[PacketType.SC].Clear();
            DestinationFileLines.Clear();
        }
    }
}

