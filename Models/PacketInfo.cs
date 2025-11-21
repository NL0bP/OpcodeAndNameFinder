using System.Collections.Generic;

namespace NameFinder.Models
{
    /// <summary>
    /// Информация о пакете
    /// </summary>
    public class PacketInfo
    {
        public string Name { get; set; }
        public string SubAddress { get; set; }
        public string Opcode { get; set; }
        public List<StructureField> Structure { get; set; } = new List<StructureField>();
        public List<string> Xrefs { get; set; } = new List<string>();
    }

    /// <summary>
    /// Поле структуры пакета
    /// </summary>
    public class StructureField
    {
        public int Type { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Тип пакета (Client-to-Server или Server-to-Client)
    /// </summary>
    public enum PacketType
    {
        CS, // Client-to-Server
        SC  // Server-to-Client
    }

    /// <summary>
    /// Источник данных (входной файл с известными именами или выходной без имен)
    /// </summary>
    public enum PacketSource
    {
        In,   // Входной файл (с известными именами)
        Out   // Выходной файл (без имен)
    }
}

