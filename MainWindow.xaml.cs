using NameFinder.Conversion;
using NameFinder.Services;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RadioButton = System.Windows.Controls;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NameFinder
{
    public enum TypeEnum_05
    {
        UInt64 = 0x38,
        UInt32 = 0x3C,
        UInt16 = 0x40,
        Byte = 0x44,
        Int64 = 0x48,
        Int32 = 0x4C,
        Int16 = 0x50,
        SByte = 0x54,
        Angles = 0x58, // float x, y, z
        Quaternion = 0x5C, // float x, y, z, w
        Times3Q = 0x60, // long x, y, z
        Vector3 = 0x64, // float x, y, z
        Times2D = 0x68, // uint x, y
        Vector2 = 0x6C, // float x, y
        Float = 0x70,
        Bool = 0x74,
        Bool2 = 0x78,
        Bc = 0xC4, // 3 Bytes
        Bytes = 0xC8, // 3 Bytes
        String = 0xCC,
        String2 = 0xD0
    }

    public enum TypeEnum_12 // до 5.0.7.0 такие же
    {
        UInt64 = 0x38,
        UInt32 = 0x3C,
        UInt16 = 0x40,
        Byte = 0x44,
        Int64 = 0x48,
        Int32 = 0x4C,
        Int16 = 0x50,
        SByte = 0x54,
        Angles = 0x58, // float x, y, z
        Quaternion = 0x5C, // float x, y, z, w
        Times3Q = 0x60, // long x, y, z
        Vector3 = 0x64, // float x, y, z
        Times2D = 0x68, // uint x, y
        Vector2 = 0x6C, // float x, y
        Float2 = 0x70,
        Float = 0x74,
        Bool = 0x78,
        Bc = 0xCC, // 3 Bytes
        Bytes = 0xD0, // 3 Bytes
        String = 0xD4,
        String2 = 0xD8,
        String3 = 0xDC,
        String4 = 0xE0
    }

    public enum TypeEnum_60 // до 8.0
    {
        UInt64 = 0x3C,
        UInt32 = 0x40,
        UInt16 = 0x44,
        Byte = 0x48,
        Int64 = 0x4C,
        Int32 = 0x50,
        Int16 = 0x54,
        SByte = 0x58,
        Angles = 0x5C, // float x, y, z
        Quaternion = 0x60, // float x, y, z, w
        Times3Q = 0x60, // long x, y, z
        Vector3 = 0x64, // float x, y, z
        Times2D = 0x68, // uint x, y
        Vector2 = 0x70, // float x, y
        Float1 = 0x74,
        Float = 0x78,
        Bool = 0x7C,
        objId = 0xC4,
        objId2 = 0xC8,
        Bc = 0xCC, // 3 Bytes
        Bytes = 0xD0, // 3 Bytes
        Bytes3 = 0xD4, // 3 Bytes
        String = 0xE4,
        String2 = 0xE8, // Bites()
        String3 = 0xEC
    }

    public enum TypeEnum_80
    {
        UInt64 = 0x3C,
        UInt32 = 0x40,
        UInt16 = 0x44,
        Byte = 0x48,
        Int64 = 0x4C,
        Int32 = 0x50,
        Int16 = 0x54,
        SByte = 0x58,
        Angles = 0x5C, // float x, y, z
        Quaternion = 0x60, // float x, y, z, w
        Times3Q = 0x60, // long x, y, z
        Vector3 = 0x64, // float x, y, z
        Times2D = 0x68, // uint x, y
        Vector2 = 0x70, // float x, y
        Float1 = 0x74,
        Float = 0x78,
        Bool = 0x7C,
        objId = 0xC4,
        objId2 = 0xC8,
        Bc = 0xCC, // 3 Bytes
        Bytes = 0xD0, // 3 Bytes
        Bytes3 = 0xD4, // 3 Bytes
        String = 0xE4,
        String2 = 0xE8, // Bites()
        String3 = 0xEC
    }

    public class Struc
    {
        public int Type { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static void LogDebug(string message) => System.Diagnostics.Debug.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}");
        private static void LogWarn(string message) => System.Diagnostics.Debug.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss.fff} {message}");
        private static void LogError(string message, Exception ex = null)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
            if (ex != null) System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
        }

        public bool IsLittleEndian { get; } = true;

        public EndianBitConverter Converter => (IsLittleEndian ? EndianBitConverter.Little : (EndianBitConverter)EndianBitConverter.Big);

        // Сервисы (рефакторинг)
        private readonly IPacketDataService _packetDataService;
        private readonly IFileProcessor _fileProcessor;
        private readonly IOpcodeFinderService _opcodeFinderService;
        private readonly Services.OpcodeFinderWrapper _opcodeFinderWrapper;
        private readonly Services.IStructureFinderService _structureFinderService;
        
        // Временные сохраненные копии списков для ручного сравнения CS
        private ObservableCollection<string> _savedListSubDestinationCS;
        private ObservableCollection<string> _savedListNameDestinationCS;
        private ObservableCollection<string> _savedListNameSourceCS;
        private Dictionary<int, List<Struc>> _savedStructureSourceCS;
        private Dictionary<int, List<Struc>> _savedStructureDestinationCS;
        
        // Временные сохраненные копии списков для ручного сравнения SC
        private ObservableCollection<string> _savedListSubDestinationSC;
        private ObservableCollection<string> _savedListNameDestinationSC;
        private ObservableCollection<string> _savedListNameSourceSC;
        private Dictionary<int, List<Struc>> _savedStructureSourceSC;
        private Dictionary<int, List<Struc>> _savedStructureDestinationSC;

        private readonly string[] _inF;
        private readonly string[] _outF;
        private bool _isOutCs;
        private bool _isInCs;
        private bool _isOutSc;
        private bool _isInSc;
        private static readonly uint csSecondaryOffsetSequence = 0;
        private bool FindOpcodeIn = true;
        private bool FindOpcodeOut = true;
        private bool FindStructIn = true;
        private bool FindStructOut = true;
        public static bool isCompareCS = false;
        public static bool isCompareSC = false;
        public static bool isRemoveOpcode = false;

        public static bool isCS = false;

        //public static Dictionary<int, int> InUseSource { get; set; } = new Dictionary<int, int>();
        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // public static Dictionary<int, int> InUseIn { get; set; } = new Dictionary<int, int>();
        // public static Dictionary<int, int> InUseOut { get; set; } = new Dictionary<int, int>();
        // Рефакторинг: заменено на свойство-обертку, использующее PacketDataService
        // public static Dictionary<int, bool> IsRenameDestination { get; set; } = new Dictionary<int, bool>();

        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // public static List<string> InListSource = new List<string>();
        // public static List<string> ListNameSourceCS = new List<string>();
        // public static List<string> ListNameSourceSC = new List<string>();
        // public static List<string> ListSubSourceCS = new List<string>();
        // public static List<string> ListSubSourceSC = new List<string>();

        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // здесь будем собирать структуры пакетов, где index из listName1 и соответственно listSub1
        // public static Dictionary<int, List<Struc>> StructureSourceCS = new Dictionary<int, List<Struc>>();
        // public static Dictionary<int, List<Struc>> StructureSourceSC = new Dictionary<int, List<Struc>>();


        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // public static List<string> InListDestination = new List<string>();
        // public static List<string> ListNameDestinationCS = new List<string>();
        // public static List<string> ListNameDestinationSC = new List<string>();
        // public static List<string> ListSubDestinationCS = new List<string>();
        // public static List<string> ListSubDestinationSC = new List<string>();

        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // здесь будем собирать структуры пакетов, где index из listName1 и соответственно listSub1
        // public static Dictionary<int, List<Struc>> StructureDestinationCS = new Dictionary<int, List<Struc>>();
        // public static Dictionary<int, List<Struc>> StructureDestinationSC = new Dictionary<int, List<Struc>>();

        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // public static Dictionary<int, List<string>> XrefsIn = new Dictionary<int, List<string>>();
        // public static Dictionary<int, List<string>> XrefsOut = new Dictionary<int, List<string>>();
        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // public static List<string> ListOpcodeSourceCS = new List<string>();
        // public static List<string> ListOpcodeSourceSC = new List<string>();
        // public static List<string> ListOpcodeDestinationCS = new List<string>();
        // public static List<string> ListOpcodeDestinationSC = new List<string>();

        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // public static List<string> ListNameCompareCS = new List<string>();
        // public static List<string> ListNameCompareSC = new List<string>();
        public static ObservableCollection<string> ListNameCompare = new ObservableCollection<string>(); // Используется в CompareWindow как временный список
        // Рефакторинг: заменено на свойства-обертки, использующие PacketDataService
        // public static List<string> ListNameCompareOutCS = new List<string>();
        // public static List<string> ListNameCompareOutSC = new List<string>();

        public bool isCleaningIn = false;
        public bool isCleaningOut = false;
        public const int DepthMax = 10;
        public static int DepthIn = 0;
        public static int DepthOut = 0;
        public int IdxS = 0;
        public int IdxD = 0;
        public string StructStringIn = "";
        public string StructStringOut = "";
        //object lockObj;

        public MainWindow()
        {
            InitializeComponent();
            
            // Центрируем окно на экране
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Создаем директорию для логов, если её нет, и обнуляем лог при старте
            try
            {
                var logsDir = Path.Combine(Environment.CurrentDirectory, "logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }
                
                // Обнуляем лог при старте приложения
                var logFile = Path.Combine(logsDir, "debug.log");
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                }
                
                // Логируем начало новой сессии
                LogDebug("MainWindow: Приложение запущено, лог обнулен");
            }
            catch (Exception ex)
            {
                // Используем System.Diagnostics.Debug напрямую, так как LogDebug может не работать до инициализации
                System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} MainWindow: Не удалось инициализировать логирование: {ex}");
            }
            
            // Инициализация сервисов (рефакторинг)
            _packetDataService = new PacketDataService();
            _fileProcessor = new FileProcessor();
            _opcodeFinderService = new OpcodeFinderService();
            _opcodeFinderWrapper = new Services.OpcodeFinderWrapper(_opcodeFinderService, Dispatcher);
            _structureFinderService = new Services.StructureFinderService();
            
            LogDebug("MainWindow: Инициализация завершена");
            
            // Создаем объект для блокировки.
            //lockObj = new object();
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceFileLines
        /// </summary>
        private List<string> InListSource
        {
            get => _packetDataService.SourceFileLines;
            set
            {
                _packetDataService.SourceFileLines.Clear();
                if (value != null)
                {
                    _packetDataService.SourceFileLines.AddRange(value);
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourcePacketNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameSourceCS
        {
            get => _packetDataService.SourcePacketNames[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.SourcePacketNames[Models.PacketType.CS], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourcePacketNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameSourceSC
        {
            get => _packetDataService.SourcePacketNames[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.SourcePacketNames[Models.PacketType.SC], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceSubNames
        /// </summary>
        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceSubNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListSubSourceCS
        {
            get => _packetDataService.SourceSubNames[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.SourceSubNames[Models.PacketType.CS], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceSubNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListSubSourceSC
        {
            get => _packetDataService.SourceSubNames[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.SourceSubNames[Models.PacketType.SC], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationPacketNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameDestinationCS
        {
            get => _packetDataService.DestinationPacketNames[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.DestinationPacketNames[Models.PacketType.CS], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationPacketNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameDestinationSC
        {
            get => _packetDataService.DestinationPacketNames[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.DestinationPacketNames[Models.PacketType.SC], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationSubNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListSubDestinationCS
        {
            get => _packetDataService.DestinationSubNames[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.DestinationSubNames[Models.PacketType.CS], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationSubNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListSubDestinationSC
        {
            get => _packetDataService.DestinationSubNames[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.DestinationSubNames[Models.PacketType.SC], value);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceOpcodes
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListOpcodeSourceCS
        {
            get => _packetDataService.SourceOpcodes[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.SourceOpcodes[Models.PacketType.CS], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceOpcodes
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListOpcodeSourceSC
        {
            get => _packetDataService.SourceOpcodes[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.SourceOpcodes[Models.PacketType.SC], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationOpcodes
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListOpcodeDestinationCS
        {
            get => _packetDataService.DestinationOpcodes[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.DestinationOpcodes[Models.PacketType.CS], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationOpcodes
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListOpcodeDestinationSC
        {
            get => _packetDataService.DestinationOpcodes[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.DestinationOpcodes[Models.PacketType.SC], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.CompareNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameCompareCS
        {
            get => _packetDataService.CompareNames[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.CompareNames[Models.PacketType.CS], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.CompareNames
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameCompareSC
        {
            get => _packetDataService.CompareNames[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.CompareNames[Models.PacketType.SC], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceXrefs[PacketType.CS]
        /// Примечание: XrefsIn синхронизируется одинаково для CS и SC, поэтому используем CS данные
        /// </summary>
        public Dictionary<int, List<string>> XrefsIn
        {
            get => _packetDataService.SourceXrefs[Models.PacketType.CS];
            set
            {
                _packetDataService.SourceXrefs[Models.PacketType.CS].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.SourceXrefs[Models.PacketType.CS][kvp.Key] = new List<string>(kvp.Value);
                    }
                    // Синхронизируем SC с теми же данными
                    _packetDataService.SourceXrefs[Models.PacketType.SC].Clear();
                    foreach (var kvp in value)
                    {
                        _packetDataService.SourceXrefs[Models.PacketType.SC][kvp.Key] = new List<string>(kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationXrefs[PacketType.CS]
        /// Примечание: XrefsOut синхронизируется одинаково для CS и SC, поэтому используем CS данные
        /// </summary>
        public Dictionary<int, List<string>> XrefsOut
        {
            get => _packetDataService.DestinationXrefs[Models.PacketType.CS];
            set
            {
                _packetDataService.DestinationXrefs[Models.PacketType.CS].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.DestinationXrefs[Models.PacketType.CS][kvp.Key] = new List<string>(kvp.Value);
                    }
                    // Синхронизируем SC с теми же данными
                    _packetDataService.DestinationXrefs[Models.PacketType.SC].Clear();
                    foreach (var kvp in value)
                    {
                        _packetDataService.DestinationXrefs[Models.PacketType.SC][kvp.Key] = new List<string>(kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationFileLines
        /// </summary>
        private List<string> InListDestination
        {
            get => _packetDataService.DestinationFileLines;
            set
            {
                _packetDataService.DestinationFileLines.Clear();
                if (value != null)
                {
                    _packetDataService.DestinationFileLines.AddRange(value);
                }
            }
        }

        /// <summary>
        /// Вспомогательный метод для копирования коллекций, устраняющий дублирование кода
        /// </summary>
        /// <typeparam name="T">Тип элементов коллекции</typeparam>
        /// <param name="target">Целевая коллекция</param>
        /// <param name="source">Исходная коллекция</param>
        /// <param name="checkReferenceEquals">Проверять ли ReferenceEquals перед копированием</param>
        private void CopyCollection<T>(ObservableCollection<T> target, IEnumerable<T> source, bool checkReferenceEquals = true)
        {
            if (checkReferenceEquals && ReferenceEquals(source, target))
            {
                return;
            }
            target.Clear();
            if (source != null)
            {
                foreach (var item in source)
                {
                    target.Add(item);
                }
            }
        }

        /// <summary>
        /// Рефакторинг: синхронизирует XrefsIn с PacketDataService
        /// Теперь не нужен, так как свойство XrefsIn автоматически синхронизирует данные
        /// Оставлен для обратной совместимости, но теперь просто синхронизирует SC с CS данными
        /// </summary>
        private void SyncXrefsInToService()
        {
            // Синхронизируем SC с CS данными (CS уже обновлен через свойство XrefsIn)
            var csXrefs = _packetDataService.SourceXrefs[Models.PacketType.CS];
            _packetDataService.SourceXrefs[Models.PacketType.SC].Clear();
            foreach (var kvp in csXrefs)
            {
                _packetDataService.SourceXrefs[Models.PacketType.SC][kvp.Key] = new List<string>(kvp.Value);
            }
        }

        /// <summary>
        /// Рефакторинг: синхронизирует XrefsOut с PacketDataService
        /// Теперь не нужен, так как свойство XrefsOut автоматически синхронизирует данные
        /// Оставлен для обратной совместимости, но теперь просто синхронизирует SC с CS данными
        /// </summary>
        private void SyncXrefsOutToService()
        {
            // Синхронизируем SC с CS данными (CS уже обновлен через свойство XrefsOut)
            var csXrefs = _packetDataService.DestinationXrefs[Models.PacketType.CS];
            _packetDataService.DestinationXrefs[Models.PacketType.SC].Clear();
            foreach (var kvp in csXrefs)
            {
                _packetDataService.DestinationXrefs[Models.PacketType.SC][kvp.Key] = new List<string>(kvp.Value);
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.InUseMapping[PacketType.CS]
        /// Примечание: InUseIn синхронизируется одинаково для CS и SC, поэтому используем CS данные
        /// </summary>
        public Dictionary<int, int> InUseIn
        {
            get => _packetDataService.InUseMapping[Models.PacketType.CS];
            set
            {
                _packetDataService.InUseMapping[Models.PacketType.CS].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.InUseMapping[Models.PacketType.CS][kvp.Key] = kvp.Value;
                    }
                    // Синхронизируем SC с теми же данными
                    _packetDataService.InUseMapping[Models.PacketType.SC].Clear();
                    foreach (var kvp in value)
                    {
                        _packetDataService.InUseMapping[Models.PacketType.SC][kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.InUseMapping[PacketType.CS] (для Out используем тот же словарь)
        /// Примечание: InUseOut синхронизируется одинаково для CS и SC, поэтому используем CS данные
        /// TODO: Возможно, нужно разделить InUseIn и InUseOut в PacketDataService
        /// </summary>
        public Dictionary<int, int> InUseOut
        {
            get => _packetDataService.InUseMapping[Models.PacketType.CS];
            set
            {
                // Для InUseOut используем тот же словарь, что и для InUseIn
                // В будущем можно разделить на отдельные свойства в PacketDataService
                _packetDataService.InUseMapping[Models.PacketType.CS].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.InUseMapping[Models.PacketType.CS][kvp.Key] = kvp.Value;
                    }
                    // Синхронизируем SC с теми же данными
                    _packetDataService.InUseMapping[Models.PacketType.SC].Clear();
                    foreach (var kvp in value)
                    {
                        _packetDataService.InUseMapping[Models.PacketType.SC][kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceStructures[PacketType.CS]
        /// Теперь возвращает Dictionary с ObservableCollection для автоматического обновления UI
        /// </summary>
        public Dictionary<int, ObservableCollection<Struc>> StructureSourceCS
        {
            get => _packetDataService.SourceStructures[Models.PacketType.CS];
            set
            {
                // Если присваивается тот же словарь, не нужно ничего делать
                if (ReferenceEquals(value, _packetDataService.SourceStructures[Models.PacketType.CS]))
                {
                    return;
                }
                _packetDataService.SourceStructures[Models.PacketType.CS].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.SourceStructures[Models.PacketType.CS][kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.SourceStructures[PacketType.SC]
        /// Теперь возвращает Dictionary с ObservableCollection для автоматического обновления UI
        /// </summary>
        public Dictionary<int, ObservableCollection<Struc>> StructureSourceSC
        {
            get => _packetDataService.SourceStructures[Models.PacketType.SC];
            set
            {
                // Если присваивается тот же словарь, не нужно ничего делать
                if (ReferenceEquals(value, _packetDataService.SourceStructures[Models.PacketType.SC]))
                {
                    return;
                }
                _packetDataService.SourceStructures[Models.PacketType.SC].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.SourceStructures[Models.PacketType.SC][kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationStructures[PacketType.CS]
        /// Теперь возвращает Dictionary с ObservableCollection для автоматического обновления UI
        /// </summary>
        public Dictionary<int, ObservableCollection<Struc>> StructureDestinationCS
        {
            get => _packetDataService.DestinationStructures[Models.PacketType.CS];
            set
            {
                // Если присваивается тот же словарь, не нужно ничего делать
                if (ReferenceEquals(value, _packetDataService.DestinationStructures[Models.PacketType.CS]))
                {
                    return;
                }
                _packetDataService.DestinationStructures[Models.PacketType.CS].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.DestinationStructures[Models.PacketType.CS][kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.DestinationStructures[PacketType.SC]
        /// Теперь возвращает Dictionary с ObservableCollection для автоматического обновления UI
        /// </summary>
        public Dictionary<int, ObservableCollection<Struc>> StructureDestinationSC
        {
            get => _packetDataService.DestinationStructures[Models.PacketType.SC];
            set
            {
                // Если присваивается тот же словарь, не нужно ничего делать
                if (ReferenceEquals(value, _packetDataService.DestinationStructures[Models.PacketType.SC]))
                {
                    return;
                }
                _packetDataService.DestinationStructures[Models.PacketType.SC].Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.DestinationStructures[Models.PacketType.SC][kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.IsRenameDestination
        /// </summary>
        public Dictionary<int, bool> IsRenameDestination
        {
            get => _packetDataService.IsRenameDestination;
            set
            {
                _packetDataService.IsRenameDestination.Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _packetDataService.IsRenameDestination[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.CompareOutNames[PacketType.CS]
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameCompareOutCS
        {
            get => _packetDataService.CompareOutNames[Models.PacketType.CS];
            set => CopyCollection(_packetDataService.CompareOutNames[Models.PacketType.CS], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: свойство-обертка для обратной совместимости
        /// Использует PacketDataService.CompareOutNames[PacketType.SC]
        /// Теперь возвращает ObservableCollection для автоматического обновления UI
        /// </summary>
        public ObservableCollection<string> ListNameCompareOutSC
        {
            get => _packetDataService.CompareOutNames[Models.PacketType.SC];
            set => CopyCollection(_packetDataService.CompareOutNames[Models.PacketType.SC], value, checkReferenceEquals: false);
        }

        /// <summary>
        /// Рефакторинг: универсальный метод для поиска опкодов
        /// Объединяет общую логику всех четырех async методов поиска опкодов
        /// </summary>
        private async Task FindOpcodesAsyncInternal(
            Models.PacketType packetType,
            Models.PacketSource packetSource,
            List<string> fileLines,
            Dictionary<int, List<string>> xrefs,
            ObservableCollection<string> opcodeList,
            Action syncXrefsAction,
            Action<int> progressUpdate,
            Action<int> maxUpdate,
            Action<string> countUpdate,
            Action<string> notFoundUpdate,
            Action<string> timeUpdate,
            Action<Brush> brushUpdate,
            Action<bool> enabledUpdate,
            Action<ObservableCollection<string>> listViewUpdate,
            Func<(bool canCompareCS, bool canCompareSC)> compareFlagsGetter,
            Action<bool, bool> compareButtonsUpdate,
            string errorPrefix)
        {
            try
            {
                // Синхронизируем XREFs с PacketDataService
                syncXrefsAction?.Invoke();
                
                var opcodes = await _opcodeFinderWrapper.FindOpcodesWithUIAsync(
                    packetType,
                    packetSource,
                    fileLines,
                    xrefs,
                    progressUpdate,
                    maxUpdate,
                    countUpdate,
                    notFoundUpdate,
                    timeUpdate,
                    brushUpdate,
                    enabledUpdate);

                // Сохраняем результаты
                opcodeList.Clear();
                foreach (var opcode in opcodes)
                {
                    opcodeList.Add(opcode);
                }

                // Рефакторинг: используем UIHelper для группировки UI обновлений
                var opcodeCount = opcodes.Count;
                var notFoundCount = opcodes.Count(o => o == "0xfff");
                var (canCompareCS, canCompareSC) = compareFlagsGetter();

                await Helpers.UIHelper.InvokeUIBatchAsync(Dispatcher,
                    () => listViewUpdate(new ObservableCollection<string>(opcodes)),
                    () => countUpdate(opcodeCount.ToString()),
                    () => notFoundUpdate(notFoundCount.ToString()),
                    () => compareButtonsUpdate(canCompareCS, canCompareSC)
                );
            }
            catch (Exception ex)
            {
                // Рефакторинг: используем UIHelper для обработки ошибок
                Helpers.UIHelper.ShowError(Dispatcher, $"Ошибка при поиске опкодов {errorPrefix}: {ex.Message}", "Ошибка");
                await Helpers.UIHelper.InvokeUIAsync(Dispatcher, () =>
                {
                    brushUpdate(Brushes.Red);
                });
            }
        }

        /// <summary>
        /// Рефакторинг: новый async метод для поиска опкодов CS (Source) с использованием сервиса
        /// </summary>
        private async Task FindOpcodeSourceCSAsync()
        {
            await FindOpcodesAsyncInternal(
                Models.PacketType.CS,
                Models.PacketSource.In,
                InListSource,
                _packetDataService.SourceXrefs[Models.PacketType.CS],
                _packetDataService.SourceOpcodes[Models.PacketType.CS],
                () => SyncXrefsInToService(),
                percent => ProgressBar13.Value = percent,
                max => ProgressBar13.Maximum = max,
                count => TextBox16Copy.Text = count,
                notFound => TextBox17Copy.Text = notFound,
                time => TextBox19Copy.Text = time,
                brush => Label_Semafor1.Background = brush,
                enabled =>
                {
                    ButtonSaveIn1.IsEnabled = enabled;
                    ButtonSaveIn2.IsEnabled = enabled;
                    BtnLoadIn.IsEnabled = enabled;
                    BtnLoadIn_Copy.IsEnabled = enabled;
                    BtnCsLoadNameIn.IsEnabled = enabled;
                    BtnScLoadNameIn.IsEnabled = enabled;
                    BtnMakePktIn.IsEnabled = enabled;
                    BtnGotoOpcodeIn.IsEnabled = enabled;
                    ButtonCsCompare.IsEnabled = enabled;
                    ButtonScCompare.IsEnabled = enabled;
                },
                opcodes => ListView14.ItemsSource = opcodes,
                () =>
                {
                    _isInCs = true;
                    var canCompareCS = _isInCs && _isOutCs;
                    return (canCompareCS, !canCompareCS);
                },
                (canCompareCS, canCompareSC) =>
                {
                    ButtonCsCompare.IsEnabled = canCompareCS;
                    ButtonScCompare.IsEnabled = canCompareSC;
                },
                "CS"
            );
        }

        private void FindOpcodeSourceCS()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var maxXrefs = XrefsIn.Count;
            _isInCs = false;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar13.Value = 0,
                () => ProgressBar13.Maximum = maxXrefs,
                () => TextBox16Copy.Text = "0",
                () => TextBox17Copy.Text = "0",
                () => TextBox19Copy.Text = "0",
                () => Label_Semafor1.Background = Brushes.Yellow,
                () => ButtonSaveIn1.IsEnabled = false,
                () => ButtonSaveIn2.IsEnabled = false,
                () => BtnLoadIn.IsEnabled = false,
                () => BtnLoadIn_Copy.IsEnabled = false,
                () => BtnCsLoadNameIn.IsEnabled = false,
                () => BtnScLoadNameIn.IsEnabled = false,
                () => BtnMakePktIn.IsEnabled = false,
                () => BtnGotoOpcodeIn.IsEnabled = false,
                () => ButtonCsCompare.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = false
            );

            var notFoundCount = 0;
            var subAddress = "";
            ListOpcodeSourceCS.Clear();
            var found = false;
            //
            // ищем конец подпрограммы
            //
            var regexEndp = new Regex(@"\s+endp\s*", RegexOptions.Compiled);
            //
            // здесь ищем ссылку на подпрограмму, где есть опкоды
            //
            var regexOffset = new Regex(@"mov\s+\[(\w+\+\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s", RegexOptions.Compiled);
            //var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\-\w+\],\s+([0-9A-F]+)", RegexOptions.Compiled);
            var regexOpcode = new Regex(@"\[\w+\-(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)\w+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h)\w+[0-9a-fA-F]+\+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)", RegexOptions.Compiled);

            for (var i = 0; i < XrefsIn.Count; i++)
            {
                var list = XrefsIn[i].ToList();
                var foundOpcode = false;
                foreach (var str in list)
                {
                    // выделяем из "; DATA XREF: sub_39024010" -> "sub_39024010"
                    // "; DATA XREF: X2__GameClient__ClientDrivenNpc__UpdateMovementSync" -> "X2__GameClient__ClientDrivenNpc__UpdateMovementSync"
                    // "; sub_394045F0" -> "sub_394045F0"
                    var regexSub = new Regex(@"sub_\w+|X2\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(str);
                    if (matchesSub.Count <= 0)
                    {
                        continue;
                    }

                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();
                    //
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    //
                    found = false;
                    var find = "";
                    var ss = "";
                    for (var index = 0; index < InListSource.Count; index++)
                    {
                        //
                        // ищем начало подпрограммы, каждый раз с начала файла
                        //
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase);
                        var matches = regex10.Matches(InListSource[index]);
                        if (matches.Count <= 0)
                        {
                            continue;
                        }
                        //
                        // нашли начало подпрограммы, ищем опкоды в структуре, пока не "endp"
                        //
                        var foundEndp = false;
                        do
                        {
                            var matchesEndp = regexEndp.Matches(InListSource[index]);
                            if (matchesEndp.Count > 0)
                            {
                                foundEndp = true;
                                continue;
                            }

                            index++;
                            // ищем сначала текст
                            // "mov     [ebp+var_50], offset ??_7CSWorldRayCastingPacket@@6B@ ; const CSWorldRayCastingPacket::`vftable'",
                            // а в следующей строке опкод
                            // "mov     [ebp+var_4C], 0C0h"
                            // или
                            // "mov     dword ptr [eax+4], 71h"
                            // бывает, что не следующая строка, а через несколько строк
                            /*
                            mov     dword ptr [eax], offset ??_7CSChangeLootingRulePacket@@6B@ ; const CSChangeLootingRulePacket::`vftable'
                            mov     byte ptr [eax+10h], 2
                            mov     byte ptr [eax+18h], 1
                            mov     dword ptr [ebp+var_20+4], 1D4h
                            */
                            var matchesOffset = regexOffset.Match(InListSource[index]);
                            if (matchesOffset.Groups.Count <= 1)
                            {
                                continue;
                            }
                            //group 1 = mov\s+\[(\w+\+\w+)\],\soffset\s|
                            if (matchesOffset.Groups[1].Length > 0)
                            {
                                // mov     [ebp+var_48C], offset SCDominionDataPacket_0x0b2
                                // mov     [ebp+var_488], 0B2h
                                ss = matchesOffset.Groups[1].ToString();
                                find = Decrease4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 2 = mov\s+dword\sptr\s\[(\w+)\],\soffset\s|
                            else if (matchesOffset.Groups[2].Length > 0)
                            {
                                ss = matchesOffset.Groups[2].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 3 = mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s
                            else if (matchesOffset.Groups[3].Length > 0)
                            {
                                // mov     dword ptr [esi+10C0h], offset SCUnitDeathPacket_0x1f5
                                // mov     dword ptr [esi+10C4h], 1F5h
                                ss = matchesOffset.Groups[3].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            var matchesOpcode = regexOpcode.Match(InListSource[index]);
                            //
                            // пропускаем строки, пока не "endp"
                            //
                            foundEndp = false;
                            do
                            {
                                index++;
                                matchesOpcode = regexOpcode.Match(InListSource[index]);
                                if (matchesOpcode.Groups.Count >= 2)
                                {
                                    //
                                    // нашли опкод
                                    //
                                    break;
                                }

                                var matches2 = regexEndp.Matches(InListSource[index]);
                                //
                                // нашли конец подпрограммы
                                //
                                if (matches2.Count > 0)
                                    foundEndp = true;
                            } while (!foundEndp);

                            if (matchesOpcode.Groups.Count >= 2)
                            {
                                if (matchesOpcode.Groups[4].ToString() != "" && matchesOpcode.Groups[4].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[4].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[4];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                    }

                                    ListOpcodeSourceCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[3].ToString() != "" && matchesOpcode.Groups[3].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[3].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[3];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                    }

                                    ListOpcodeSourceCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[2].ToString() != "" && matchesOpcode.Groups[2].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[2].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[2];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                    }

                                    ListOpcodeSourceCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[1].ToString() != "" && matchesOpcode.Groups[1].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[1].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[1];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                    }

                                    ListOpcodeSourceCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[0].ToString() != "" && matchesOpcode.Groups[0].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[0].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[0];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                    }

                                    ListOpcodeSourceCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
                    }

                    if (foundOpcode)
                    {
                        break;
                    }
                }

                if (!foundOpcode)
                {
                    notFoundCount++;
                    ListOpcodeSourceCS.Add("0xfff"); // не нашли опкод
                }

                // Рефакторинг: используем UIHelper для обновления прогрессбара
                Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar13.Value = ListOpcodeSourceCS.Count);
            }

            var lnCount = ListOpcodeSourceCS.Count;
            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => TextBox16Copy.Text = lnCount.ToString(),
                () => TextBox17Copy.Text = notFoundCount.ToString(),
                () => TextBox19Copy.Text = elapsed,
                () => ListView14.ItemsSource = ListOpcodeSourceCS,
                () => Label_Semafor1.Background = Brushes.GreenYellow,
                () => ButtonSaveIn1.IsEnabled = true,
                () => BtnLoadIn.IsEnabled = true,
                () => BtnLoadIn_Copy.IsEnabled = true,
                () => BtnMakePktIn.IsEnabled = true,
                () => BtnGotoOpcodeIn.IsEnabled = true,
                () => BtnCsLoadNameIn.IsEnabled = true,
                () => BtnScLoadNameIn.IsEnabled = true
            );

            _isInCs = true;
            var canCompareCS = _isInCs && _isOutCs;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ButtonCsCompare.IsEnabled = canCompareCS,
                () => ButtonScCompare.IsEnabled = false
            );
        }

        /// <summary>
        /// Рефакторинг: новый async метод для поиска опкодов SC (Source) с использованием сервиса
        /// </summary>
        private async Task FindOpcodeSourceSCAsync()
        {
            await FindOpcodesAsyncInternal(
                Models.PacketType.SC,
                Models.PacketSource.In,
                InListSource,
                _packetDataService.SourceXrefs[Models.PacketType.SC],
                _packetDataService.SourceOpcodes[Models.PacketType.SC],
                () => SyncXrefsInToService(),
                percent => ProgressBar13.Value = percent,
                max => ProgressBar13.Maximum = max,
                count => TextBox16Copy.Text = count,
                notFound => TextBox17Copy.Text = notFound,
                time => TextBox19Copy.Text = time,
                brush => Label_Semafor1.Background = brush,
                enabled =>
                {
                    ButtonSaveIn1.IsEnabled = enabled;
                    ButtonSaveIn2.IsEnabled = enabled;
                    BtnLoadIn.IsEnabled = enabled;
                    BtnLoadIn_Copy.IsEnabled = enabled;
                    BtnCsLoadNameIn.IsEnabled = enabled;
                    BtnScLoadNameIn.IsEnabled = enabled;
                    BtnMakePktIn.IsEnabled = enabled;
                    BtnGotoOpcodeIn.IsEnabled = enabled;
                    ButtonCsCompare.IsEnabled = enabled;
                    ButtonScCompare.IsEnabled = enabled;
                },
                opcodes => ListView14.ItemsSource = opcodes,
                () =>
                {
                    _isInSc = true;
                    var canCompareSC = _isInSc && _isOutSc;
                    return (!canCompareSC, canCompareSC);
                },
                (canCompareCS, canCompareSC) =>
                {
                    ButtonCsCompare.IsEnabled = canCompareCS;
                    ButtonScCompare.IsEnabled = canCompareSC;
                },
                "SC"
            );
        }

        private void FindOpcodeSourceSC()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var maxXrefs = XrefsIn.Count;
            _isInSc = false;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar13.Value = 0,
                () => ProgressBar13.Maximum = maxXrefs,
                () => TextBox16Copy.Text = "0",
                () => TextBox17Copy.Text = "0",
                () => TextBox19Copy.Text = "0",
                () => Label_Semafor1.Background = Brushes.Yellow,
                () => ButtonSaveIn1.IsEnabled = false,
                () => ButtonSaveIn2.IsEnabled = false,
                () => BtnLoadIn.IsEnabled = false,
                () => BtnLoadIn_Copy.IsEnabled = false,
                () => BtnCsLoadNameIn.IsEnabled = false,
                () => BtnScLoadNameIn.IsEnabled = false,
                () => BtnMakePktIn.IsEnabled = false,
                () => BtnGotoOpcodeIn.IsEnabled = false,
                () => ButtonCsCompare.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = false
            );

            var notFoundCount = 0;
            var subAddress = "";
            ListOpcodeSourceSC.Clear();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            //
            // ищем конец подпрограммы
            //
            var regexEndp = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
            //
            // здесь ищем ссылку на подпрограмму, где есть опкоды
            //
            var regexOffset = new Regex(@"mov\s+\[(\w+\+\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s", RegexOptions.Compiled);
            //var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\-\w+\],\s+([0-9A-F]+)", RegexOptions.Compiled);
            var regexOpcode = new Regex(@"\[\w+\-(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)\w+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h)\w+[0-9a-fA-F]+\+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)", RegexOptions.Compiled);

            for (var i = 0; i < XrefsIn.Count; i++)
            {
                var list = XrefsIn[i].ToList();
                var foundOpcode = false;
                foreach (var str in list)
                {
                    //var regex = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                    // выделяем из "; DATA XREF: sub_39024010" -> "sub_39024010"
                    // "; DATA XREF: X2__GameClient__ClientDrivenNpc__UpdateMovementSync" -> "X2__GameClient__ClientDrivenNpc__UpdateMovementSync"
                    // "; sub_394045F0" -> "sub_394045F0"
                    var regexSub = new Regex(@"sub_\w+|X2\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(str);
                    if (matchesSub.Count <= 0)
                    {
                        continue;
                    }

                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();
                    //
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    //
                    found = false;
                    var find = "";
                    var ss = "";
                    for (var index = 0; index < InListSource.Count; index++)
                    {
                        //
                        // ищем начало подпрограммы, каждый раз с начала файла
                        //
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase);
                        var matches = regex10.Matches(InListSource[index]);
                        if (matches.Count <= 0)
                        {
                            continue;
                        }
                        //
                        // нашли начало подпрограммы, ищем опкоды в структуре, пока не "endp"
                        //
                        var foundEndp = false;
                        do
                        {
                            var matchesEndp = regexEndp.Matches(InListSource[index]);
                            if (matchesEndp.Count > 0)
                            {
                                foundEndp = true;
                                continue;
                            }

                            index++;
                            // ищем сначала текст
                            // "mov     [ebp+var_50], offset ??_7CSWorldRayCastingPacket@@6B@ ; const CSWorldRayCastingPacket::`vftable'",
                            // а в следующей строке опкод
                            // "mov     [ebp+var_4C], 0C0h"
                            // или
                            // "mov     dword ptr [eax+4], 71h"
                            // бывает, что не следующая строка, а через несколько строк
                            /*
                            mov     dword ptr [eax], offset ??_7CSChangeLootingRulePacket@@6B@ ; const CSChangeLootingRulePacket::`vftable'
                            mov     byte ptr [eax+10h], 2
                            mov     byte ptr [eax+18h], 1
                            mov     dword ptr [eax+4], 71h
                            */
                            var matchesOffset = regexOffset.Match(InListSource[index]);
                            if (matchesOffset.Groups.Count <= 1)
                            {
                                continue;
                            }
                            //group 1 = mov\s+\[(\w+\+\w+)\],\soffset\s|
                            if (matchesOffset.Groups[1].Length > 0)
                            {
                                // mov     [ebp+var_48C], offset SCDominionDataPacket_0x0b2
                                // mov     [ebp+var_488], 0B2h
                                ss = matchesOffset.Groups[1].ToString();
                                find = Decrease4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 2 = mov\s+dword\sptr\s\[(\w+)\],\soffset\s|
                            else if (matchesOffset.Groups[2].Length > 0)
                            {
                                ss = matchesOffset.Groups[2].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 3 = mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s
                            else if (matchesOffset.Groups[3].Length > 0)
                            {
                                // mov     dword ptr [esi+10C0h], offset SCUnitDeathPacket_0x1f5
                                // mov     dword ptr [esi+10C4h], 1F5h
                                ss = matchesOffset.Groups[3].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            var matchesOpcode = regexOpcode.Match(InListSource[index]);

                            // пропускаем строки, пока не "endp"
                            foundEndp = false;
                            do
                            {
                                index++;
                                matchesOpcode = regexOpcode.Match(InListSource[index]);
                                if (matchesOpcode.Groups.Count >= 2)
                                {
                                    //
                                    // нашли опкод
                                    //
                                    break;
                                }

                                var matches2 = regexEndp.Matches(InListSource[index]);
                                //
                                // нашли конец подпрограммы
                                //
                                if (matches2.Count > 0)
                                    foundEndp = true;
                            } while (!foundEndp);


                            if (matchesOpcode.Groups.Count >= 2)
                            {
                                if (matchesOpcode.Groups[4].ToString() != "" && matchesOpcode.Groups[4].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[4].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[4];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                    }

                                    ListOpcodeSourceSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[3].ToString() != "" && matchesOpcode.Groups[3].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[3].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[3];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                    }

                                    ListOpcodeSourceSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[2].ToString() != "" && matchesOpcode.Groups[2].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[2].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[2];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                    }

                                    ListOpcodeSourceSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[1].ToString() != "" && matchesOpcode.Groups[1].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[1].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[1];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                    }

                                    ListOpcodeSourceSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[0].ToString() != "" && matchesOpcode.Groups[0].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[0].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[0];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                    }

                                    ListOpcodeSourceSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
                    }

                    if (foundOpcode)
                    {
                        break;
                    }
                }

                if (!foundOpcode)
                {
                    notFoundCount++;
                    ListOpcodeSourceSC.Add("0xfff"); // не нашли опкод
                }

                // Рефакторинг: используем UIHelper для обновления прогрессбара
                Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar13.Value = ListOpcodeSourceSC.Count);
            }

            var lnCount = ListOpcodeSourceSC.Count;
            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => TextBox16Copy.Text = lnCount.ToString(),
                () => TextBox17Copy.Text = notFoundCount.ToString(),
                () => TextBox19Copy.Text = elapsed,
                () => ListView14.ItemsSource = ListOpcodeSourceSC,
                () => Label_Semafor1.Background = Brushes.GreenYellow,
                () => ButtonSaveIn2.IsEnabled = true,
                () => BtnLoadIn.IsEnabled = true,
                () => BtnLoadIn_Copy.IsEnabled = true,
                () => BtnCsLoadNameIn.IsEnabled = true,
                () => BtnScLoadNameIn.IsEnabled = true,
                () => BtnMakePktIn.IsEnabled = true,
                () => BtnGotoOpcodeIn.IsEnabled = true
            );

            _isInSc = true;
            var canCompareSC = _isInSc && _isOutSc;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ButtonCsCompare.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = canCompareSC
            );
        }

        /// <summary>
        /// Рефакторинг: новый async метод для поиска опкодов CS (Destination) с использованием сервиса
        /// </summary>
        private async Task FindOpcodeDestinationCSAsync()
        {
            await FindOpcodesAsyncInternal(
                Models.PacketType.CS,
                Models.PacketSource.Out,
                InListDestination,
                _packetDataService.DestinationXrefs[Models.PacketType.CS],
                _packetDataService.DestinationOpcodes[Models.PacketType.CS],
                () => SyncXrefsOutToService(),
                percent => ProgressBar23.Value = percent,
                max => ProgressBar23.Maximum = max,
                count => TextBox16Copy1.Text = count,
                notFound => TextBox17Copy1.Text = notFound,
                time => TextBox19Copy1.Text = time,
                brush => Label_Semafor2.Background = brush,
                enabled =>
                {
                    ButtonSaveOut1.IsEnabled = enabled;
                    ButtonSaveOut2.IsEnabled = enabled;
                    BtnLoadOut.IsEnabled = enabled;
                    ButtonCsCompare.IsEnabled = enabled;
                    ButtonScCompare.IsEnabled = enabled;
                },
                opcodes => ListView24.ItemsSource = opcodes,
                () =>
                {
                    _isOutCs = true;
                    var canCompareCS = _isInCs && _isOutCs;
                    return (canCompareCS, !canCompareCS);
                },
                (canCompareCS, canCompareSC) =>
                {
                    ButtonCsCompare.IsEnabled = canCompareCS;
                    ButtonScCompare.IsEnabled = canCompareSC;
                },
                "CS (Destination)"
            );
        }

        private void FindOpcodeDestinationCS()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var maxXrefs = XrefsOut.Count;
            _isOutCs = false;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar23.Value = 0,
                () => ProgressBar23.Maximum = maxXrefs,
                () => TextBox16Copy1.Text = "0",
                () => TextBox17Copy1.Text = "0",
                () => TextBox19Copy1.Text = "0",
                () => Label_Semafor2.Background = Brushes.Yellow,
                () => ButtonSaveOut1.IsEnabled = false,
                () => ButtonSaveOut2.IsEnabled = false,
                () => BtnLoadOut.IsEnabled = false,
                () => ButtonCsCompare.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = false
            );

            var notFoundCount = 0;
            //var baseAddress = 0;
            //var offsetAddres = 0;
            var subAddress = "";
            ListOpcodeDestinationCS.Clear();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            var regexEndp = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
            var regexOffset = new Regex(@"mov\s+\[(\w+\+\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s", RegexOptions.Compiled);
            //var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\-\w+\],\s+([0-9A-F]+)", RegexOptions.Compiled);
            var regexOpcode = new Regex(@"\[\w+\-(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)\w+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h)\w+[0-9a-fA-F]+\+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)", RegexOptions.Compiled);

            for (var i = 0; i < XrefsOut.Count; i++)
            {
                var list = XrefsOut[i].ToList();
                var foundOpcode = false;
                foreach (var str in list)
                {
                    //var regex = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                    // выделяем из "; DATA XREF: sub_39024010" -> "sub_39024010"
                    // "; DATA XREF: X2__GameClient__ClientDrivenNpc__UpdateMovementSync" -> "X2__GameClient__ClientDrivenNpc__UpdateMovementSync"
                    // "; sub_394045F0" -> "sub_394045F0"
                    var regexSub = new Regex(@"sub_\w+|X2\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(str);
                    if (matchesSub.Count <= 0)
                    {
                        continue;
                    }

                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    var find = "";
                    var ss = "";
                    for (var index = 0; index < InListDestination.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListDestination[index]);
                        if (matches.Count <= 0)
                        {
                            continue;
                        }

                        // нашли начало подпрограммы, ищем опкоды в структуре, пока не "endp"
                        var foundEndp = false;
                        do
                        {
                            var matchesEndp = regexEndp.Matches(InListDestination[index]);
                            if (matchesEndp.Count > 0)
                            {
                                foundEndp = true;
                                continue;
                            }

                            index++;
                            // ищем сначала текст
                            // "mov     [ebp+var_50], offset ??_7CSWorldRayCastingPacket@@6B@ ; const CSWorldRayCastingPacket::`vftable'",
                            // а в следующей строке опкод
                            // "mov     [ebp+var_4C], 0C0h"
                            // или
                            // "mov     dword ptr [eax+4], 71h"
                            // бывает, что не следующая строка, а через несколько строк
                            /*
                            mov     dword ptr [eax], offset ??_7CSChangeLootingRulePacket@@6B@ ; const CSChangeLootingRulePacket::`vftable'
                            mov     byte ptr [eax+10h], 2
                            mov     byte ptr [eax+18h], 1
                            mov     dword ptr [eax+4], 71h
                            */
                            var matchesOffset = regexOffset.Match(InListDestination[index]);
                            if (matchesOffset.Groups.Count <= 1)
                            {
                                continue;
                            }
                            //group 1 = mov\s+\[(\w+\+\w+)\],\soffset\s|
                            if (matchesOffset.Groups[1].Length > 0)
                            {
                                // mov     [ebp+var_48C], offset SCDominionDataPacket_0x0b2
                                // mov     [ebp+var_488], 0B2h
                                ss = matchesOffset.Groups[1].ToString();
                                find = Decrease4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 2 = mov\s+dword\sptr\s\[(\w+)\],\soffset\s|
                            else if (matchesOffset.Groups[2].Length > 0)
                            {
                                ss = matchesOffset.Groups[2].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 3 = mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s
                            else if (matchesOffset.Groups[3].Length > 0)
                            {
                                // mov     dword ptr [esi+10C0h], offset SCUnitDeathPacket_0x1f5
                                // mov     dword ptr [esi+10C4h], 1F5h
                                ss = matchesOffset.Groups[3].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }

                            var matchesOpcode = regexOpcode.Match(InListDestination[index]);

                            // пропускаем строки, пока не "endp"
                            foundEndp = false;
                            do
                            {
                                index++;
                                matchesOpcode = regexOpcode.Match(InListDestination[index]);
                                if (matchesOpcode.Groups.Count >= 2)
                                {
                                    // нашли опкод
                                    break;
                                }

                                var matches2 = regexEndp.Matches(InListDestination[index]);
                                if (matches2.Count <= 0)
                                {
                                    continue;
                                }

                                // нашли конец подпрограммы
                                foundEndp = true;
                            } while (!foundEndp);


                            if (matchesOpcode.Groups.Count >= 2)
                            {
                                if (matchesOpcode.Groups[4].ToString() != "" && matchesOpcode.Groups[4].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[4].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[4];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                    }

                                    ListOpcodeDestinationCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[3].ToString() != "" && matchesOpcode.Groups[3].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[3].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[3];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                    }

                                    ListOpcodeDestinationCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[2].ToString() != "" && matchesOpcode.Groups[2].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[2].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[2];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                    }

                                    ListOpcodeDestinationCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[1].ToString() != "" && matchesOpcode.Groups[1].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[1].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[1];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                    }

                                    ListOpcodeDestinationCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[0].ToString() != "" && matchesOpcode.Groups[0].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[0].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[0];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                    }

                                    ListOpcodeDestinationCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
                    }

                    if (foundOpcode)
                    {
                        break;
                    }
                }

                if (!foundOpcode)
                {
                    notFoundCount++;
                    ListOpcodeDestinationCS.Add("0xfff"); // не нашли опкод
                }

                // Рефакторинг: используем UIHelper для обновления прогрессбара
                Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar23.Value = ListOpcodeDestinationCS.Count);
            }

            var lnCount = ListOpcodeDestinationCS.Count;
            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();
            _isOutCs = true;
            var canCompareCS = _isInCs && _isOutCs;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => TextBox16Copy1.Text = lnCount.ToString(),
                () => TextBox17Copy1.Text = notFoundCount.ToString(),
                () => TextBox19Copy1.Text = elapsed,
                () => ListView24.ItemsSource = ListOpcodeDestinationCS,
                () => Label_Semafor2.Background = Brushes.GreenYellow,
                () => ButtonSaveOut1.IsEnabled = true,
                () => BtnLoadOut.IsEnabled = true,
                () => ButtonCsCompare.IsEnabled = canCompareCS,
                () => ButtonScCompare.IsEnabled = false
            );
        }

        /// <summary>
        /// Рефакторинг: новый async метод для поиска опкодов SC (Destination) с использованием сервиса
        /// </summary>
        private async Task FindOpcodeDestinationSCAsync()
        {
            await FindOpcodesAsyncInternal(
                Models.PacketType.SC,
                Models.PacketSource.Out,
                InListDestination,
                _packetDataService.DestinationXrefs[Models.PacketType.SC],
                _packetDataService.DestinationOpcodes[Models.PacketType.SC],
                () => SyncXrefsOutToService(),
                percent => ProgressBar23.Value = percent,
                max => ProgressBar23.Maximum = max,
                count => TextBox16Copy1.Text = count,
                notFound => TextBox17Copy1.Text = notFound,
                time => TextBox19Copy1.Text = time,
                brush => Label_Semafor2.Background = brush,
                enabled =>
                {
                    ButtonSaveOut1.IsEnabled = enabled;
                    ButtonSaveOut2.IsEnabled = enabled;
                    BtnLoadOut.IsEnabled = enabled;
                    ButtonCsCompare.IsEnabled = enabled;
                    ButtonScCompare.IsEnabled = enabled;
                },
                opcodes => ListView24.ItemsSource = opcodes,
                () =>
                {
                    _isOutSc = true;
                    var canCompareSC = _isInSc && _isOutSc;
                    return (!canCompareSC, canCompareSC);
                },
                (canCompareCS, canCompareSC) =>
                {
                    ButtonCsCompare.IsEnabled = canCompareCS;
                    ButtonScCompare.IsEnabled = canCompareSC;
                },
                "SC (Destination)"
            );
        }

        private void FindOpcodeDestinationSC()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var maxXrefs = XrefsOut.Count;
            _isOutSc = false;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar23.Value = 0,
                () => ProgressBar23.Maximum = maxXrefs,
                () => TextBox16Copy1.Text = "0",
                () => TextBox17Copy1.Text = "0",
                () => TextBox19Copy1.Text = "0",
                () => Label_Semafor2.Background = Brushes.Yellow,
                () => ButtonSaveOut1.IsEnabled = false,
                () => ButtonSaveOut2.IsEnabled = false,
                () => BtnLoadOut.IsEnabled = false,
                () => BtnCsLoadNameOut.IsEnabled = false,
                () => BtnScLoadNameOut.IsEnabled = false,
                () => BtnMakePktOut.IsEnabled = false,
                () => BtnUpdStruct.IsEnabled = false,
                () => BtnGotoOpcodeOut.IsEnabled = false,
                () => ButtonEditOutOpcode.IsEnabled = false,
                () => ButtonCsCompare.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = false
            );

            var notFoundCount = 0;
            var subAddress = "";
            ListOpcodeDestinationSC.Clear();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            var regexEndp = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
            var regexOffset = new Regex(@"mov\s+\[(\w+\+\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+)\],\soffset\s|mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s", RegexOptions.Compiled);
            //var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s+([0-9A-F]+)|mov\s+dword\sptr\s\[\w+\-\w+\],\s+([0-9A-F]+)", RegexOptions.Compiled);
            var regexOpcode = new Regex(@"\[\w+\-(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h+)\w+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)|\[\w+\+(?![0-9a-f]+h)\w+[0-9a-fA-F]+\+[0-9a-fA-F]+\],\s([0-9a-fA-F]+)", RegexOptions.Compiled);

            for (var i = 0; i < XrefsOut.Count; i++)
            {
                var list = XrefsOut[i].ToList();
                var foundOpcode = false;
                foreach (var str in list)
                {
                    //var regex = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                    // выделяем из "; DATA XREF: sub_39024010" -> "sub_39024010"
                    // "; DATA XREF: X2__GameClient__ClientDrivenNpc__UpdateMovementSync" -> "X2__GameClient__ClientDrivenNpc__UpdateMovementSync"
                    // "; sub_394045F0" -> "sub_394045F0"
                    var regexSub = new Regex(@"sub_\w+|X2\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(str);
                    if (matchesSub.Count <= 0)
                    {
                        continue;
                    }

                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    var find = "";
                    var ss = "";
                    for (var index = 0; index < InListDestination.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListDestination[index]);
                        if (matches.Count <= 0)
                        {
                            continue;
                        }

                        // нашли начало подпрограммы, ищем опкоды в структуре, пока не "endp"
                        var foundEndp = false;
                        do
                        {
                            var matchesEndp = regexEndp.Matches(InListDestination[index]);
                            if (matchesEndp.Count > 0)
                            {
                                foundEndp = true;
                                continue;
                            }

                            index++;
                            // ищем сначала текст
                            // "mov     [ebp+var_50], offset ??_7CSWorldRayCastingPacket@@6B@ ; const CSWorldRayCastingPacket::`vftable'",
                            // а в следующей строке опкод
                            // "mov     [ebp+var_4C], 0C0h"
                            // или
                            // "mov     dword ptr [eax+4], 71h"
                            // бывает, что не следующая строка, а через несколько строк
                            /*
                            mov     dword ptr [eax], offset ??_7CSChangeLootingRulePacket@@6B@ ; const CSChangeLootingRulePacket::`vftable'
                            mov     byte ptr [eax+10h], 2
                            mov     byte ptr [eax+18h], 1
                            mov     dword ptr [eax+4], 71h
                            */
                            var matchesOffset = regexOffset.Match(InListDestination[index]);
                            if (matchesOffset.Groups.Count <= 1)
                            {
                                continue;
                            }
                            //group 1 = mov\s+\[(\w+\+\w+)\],\soffset\s|
                            if (matchesOffset.Groups[1].Length > 0)
                            {
                                // mov     [ebp+var_48C], offset SCDominionDataPacket_0x0b2
                                // mov     [ebp+var_488], 0B2h
                                ss = matchesOffset.Groups[1].ToString();
                                find = Decrease4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 2 = mov\s+dword\sptr\s\[(\w+)\],\soffset\s|
                            else if (matchesOffset.Groups[2].Length > 0)
                            {
                                ss = matchesOffset.Groups[2].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }
                            //group 3 = mov\s+dword\sptr\s\[(\w+\+\w+)\],\soffset\s
                            else if (matchesOffset.Groups[3].Length > 0)
                            {
                                // mov     dword ptr [esi+10C0h], offset SCUnitDeathPacket_0x1f5
                                // mov     dword ptr [esi+10C4h], 1F5h
                                ss = matchesOffset.Groups[3].ToString();
                                find = Increase4(ss, 4);
                                regexOpcode = new Regex(@"" + find, RegexOptions.IgnoreCase);
                            }

                            var matchesOpcode = regexOpcode.Match(InListDestination[index]);

                            // пропускаем строки, пока не "endp"
                            foundEndp = false;
                            do
                            {
                                index++;
                                matchesOpcode = regexOpcode.Match(InListDestination[index]);
                                if (matchesOpcode.Groups.Count >= 2)
                                {
                                    // нашли опкод
                                    break;
                                }

                                var matches2 = regexEndp.Matches(InListDestination[index]);
                                if (matches2.Count > 0)
                                    foundEndp = true; // нашли конец подпрограммы
                            } while (!foundEndp);

                            if (matchesOpcode.Groups.Count >= 2)
                            {
                                if (matchesOpcode.Groups[4].ToString() != "" && matchesOpcode.Groups[4].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[4].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[4];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4];
                                            break;
                                    }

                                    foundOpcode = true; // нашли Opcode
                                    ListOpcodeDestinationSC.Add(matchGroup);
                                }
                                else if (matchesOpcode.Groups[3].ToString() != "" && matchesOpcode.Groups[3].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[3].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[3];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3];
                                            break;
                                    }

                                    ListOpcodeDestinationSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[2].ToString() != "" && matchesOpcode.Groups[2].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[2].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[2];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2];
                                            break;
                                    }

                                    ListOpcodeDestinationSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[1].ToString() != "" && matchesOpcode.Groups[1].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[1].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[1];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1];
                                            break;
                                    }

                                    ListOpcodeDestinationSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                                else if (matchesOpcode.Groups[0].ToString() != "" && matchesOpcode.Groups[0].ToString() != "0")
                                {
                                    var matchGroup = "";
                                    switch (matchesOpcode.Groups[0].Length)
                                    {
                                        case 1:
                                            matchGroup = "0x00" + matchesOpcode.Groups[0];
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0];
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0];
                                            break;
                                    }

                                    ListOpcodeDestinationSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
                    }

                    if (foundOpcode)
                    {
                        break;
                    }
                }

                if (!foundOpcode)
                {
                    notFoundCount++;
                    ListOpcodeDestinationSC.Add("0xfff"); // не нашли опкод
                }

                // Рефакторинг: используем UIHelper для обновления прогрессбара
                Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar23.Value = ListOpcodeDestinationSC.Count);
            }

            var lnCount = ListOpcodeDestinationSC.Count;
            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();
            _isOutSc = true;
            var canCompareSC = _isInSc && _isOutSc;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => TextBox16Copy1.Text = lnCount.ToString(),
                () => TextBox17Copy1.Text = notFoundCount.ToString(),
                () => TextBox19Copy1.Text = elapsed,
                () => ListView24.ItemsSource = ListOpcodeDestinationSC,
                () => Label_Semafor2.Background = Brushes.GreenYellow,
                () => ButtonSaveOut2.IsEnabled = true,
                () => BtnLoadOut.IsEnabled = true,
                () => BtnCsLoadNameOut.IsEnabled = true,
                () => BtnScLoadNameOut.IsEnabled = true,
                () => BtnMakePktOut.IsEnabled = true,
                () => BtnUpdStruct.IsEnabled = true,
                () => BtnGotoOpcodeOut.IsEnabled = true,
                () => ButtonEditOutOpcode.IsEnabled = true,
                () => ButtonCsCompare.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = canCompareSC
            );
        }

        private static string Increase4(string str, int inc)
        {
            try
            {
                // mov     dword ptr [esi+10C0h], offset SCUnitDeathPacket_0x1f5
                // mov     dword ptr [esi+10C4h], 1F5h
                int offset;
                string prefix;
                string numb;
                int num;
                string fstr;
                string find;
                string postfix;

                if (str.LastIndexOf("h", StringComparison.Ordinal) > 0)
                {
                    // esi+10C0h
                    str = str.Replace("h", ""); // сотрем h в конце строки
                    postfix = "h";
                }
                else
                {
                    postfix = "";
                }

                if (str.LastIndexOf("_", StringComparison.Ordinal) > 0)
                {
                    // esi+var_10C
                    offset = str.LastIndexOf("_", StringComparison.Ordinal) + 1;
                    prefix = str.Substring(0, offset);
                    numb = str.Substring(offset);
                    num = Convert.ToInt32(numb, 16) + inc;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                }
                else if (str.LastIndexOf("+", StringComparison.Ordinal) > 0)
                {
                    // esi+10C0
                    offset = str.LastIndexOf("+", StringComparison.Ordinal) + 1;
                    prefix = str.Substring(0, offset);
                    numb = str.Substring(offset);
                    num = Convert.ToInt32(numb, 16) + inc;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                }
                else
                {
                    // eax
                    fstr = str + "\\+" + inc;
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                }

                return find;
            }
            catch (Exception e)
            {
                return "@@@@@@";
            }
        }

        private static string Decrease4(string str, int dec)
        {
            try
            {
                // mov     [ebp+var_48C], offset SCDominionDataPacket_0x0b2
                // mov     [ebp+var_488], 0B2h
                // mov     dword ptr [ebp+var_20+4], 1D4h
                int offset;
                string prefix;
                string numb;
                int num;
                string fstr;
                string find;
                string postfix;

                if (str.LastIndexOf("h", StringComparison.Ordinal) > 0)
                {
                    str = str.Replace("h", ""); // сотрем h в конце строки
                    postfix = "h";
                }
                else
                {
                    postfix = "";
                }

                if (str.LastIndexOf("_", StringComparison.Ordinal) > 0)
                {
                    // esi+var_10C
                    offset = str.LastIndexOf("_", StringComparison.Ordinal) + 1;
                    prefix = str.Substring(0, offset);
                    numb = str.Substring(offset);
                    num = Convert.ToInt32(numb, 16) - dec;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                }
                else if (str.LastIndexOf("+", StringComparison.Ordinal) > 0)
                {
                    // esi+10C0
                    offset = str.LastIndexOf("+", StringComparison.Ordinal) + 1;
                    prefix = str.Substring(0, offset);
                    numb = str.Substring(offset);
                    num = Convert.ToInt32(numb, 16) - dec;
                    numb = num.ToString("X");
                    fstr = prefix + numb + postfix;
                    fstr = fstr.Replace("+", "\\+");
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                }
                else
                {
                    // eax
                    fstr = str + "\\-" + dec;
                    find = "\\[" + fstr + "\\],\\s([0-9a-fA-F]+)";
                }

                return find;
            }
            catch (Exception e)
            {
                return "@@@@@@";
            }
        }

        private List<string> CleanSourceSub(int idx)
        {
            var progress = CalcProgress(InListSource.Count);

            var maxCount = InListSource.Count;
            var found = false;
            var tmpLst = new List<string>();
            var regexProcNear = new Regex(@"(proc\s+near)", RegexOptions.Compiled); // ищем начало подпрограммы
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
            //var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+e[abcd]x,\s\[e[abcd]x\+([1-9]|[0-9A-F]{2,3}h)\]", RegexOptions.Compiled);
            var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+e[abcd]x,\s\[e[abcd]x\+([0-9A-F]{2}h)\]", RegexOptions.Compiled);
            for (var index = idx; index < maxCount; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                }

                var matchesProcNear = regexProcNear.Matches(InListSource[index]);
                if (matchesProcNear.Count <= 0)
                {
                    continue;
                }

                // нашли начало подпрограммы
                tmpLst.Add(InListSource[index]); // сохранили

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var foundEndp = false;
                do
                {
                    if (index % progress == 0)
                    {
                        // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                    }

                    index++;

                    // ищем:
                    // push    offset aDialog  ; "dialog"
                    // push\s+offset\s|
                    // call    sub_392299B0
                    // call\s+sub_\w+|
                    //                 mov     [ebp+var_A64C], offset ??_7CharacterStatePacket@@6B@ ; const CharacterStatePacket::`vftable'
                    // mov\s+\[\w+\+\w+\],\soffset\s|
                    //                  mov     [ebp+var_A648], 2Fh
                    // mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|
                    //                mov     dword ptr [eax+4], 1ADh
                    // mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|
                    //                mov     dword ptr [eax], offset ??_7SCSetBountyPermittedPacket@@6B@ ; const SCSetBountyPermittedPacket::`vftable'
                    // mov\s+dword\sptr\s\[\w+\],\soffset\s|
                    //                mov     dword ptr [ebp-10h], offset ??_7CSChangeSlaveTargetPacket@@6B@ ; const CSChangeSlaveTargetPacket::`vftable'
                    // mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|
                    //                mov     dword ptr [ebp-0Ch], 2Bh
                    // mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|

                    var matchesSub = regexSub.Matches(InListSource[index]);
                    if (matchesSub.Count > 0)
                    {
                        tmpLst.Add(InListSource[index]); // сохранили
                        found = true; // нашли структуру
                    }

                    var matches2 = regexEndP.Matches(InListSource[index]);
                    if (matches2.Count <= 0)
                    {
                        continue;
                    }

                    foundEndp = true;
                    // нашли конец подпрограммы
                    tmpLst.Add(InListSource[index]); // сохранили
                } while (index >= maxCount || !foundEndp);

            }

            if (!found)
            {
                // не нашли структуру
                return new List<string>();
            }

            return tmpLst;
        }

        private List<string> CleanSourceOffsCS(int idx)
        {
            var progress = CalcProgress(InListSource.Count);

            var found = false;
            var tmpLst = new List<string>();
            // Рефакторинг: используем UIHelper для чтения значения из UI
            var txtCS = "";
            Helpers.UIHelper.InvokeUI(Dispatcher, () => txtCS = TextBox11.Text);

            // ищем:
            /*
             off_3A01C97C    dd offset CS_PACKETS_return_0
                или
             ??_7CSBroadcastVisualOptionPacket@@6B@ dd offset CS_PACKETS_return_0
            // ^[a-zA-Z0-9_?@]+\s+dd\soffset\s               + txtCS
                                    ; DATA XREF: sub_391D5940+100↑o
                                    ; sub_39347360+69↑o ...
                            dd offset CS_SC_PACKET
                            dd offset sub_395DB460
            // ищем отступ: ^\s{8,}

            // бывает такое
                CSResturnAddrsPacket_0xfff dd offset CS_PACKETS_return_0
                            dd offset CS_PACKETS
                            dd offset sub_39807490
            // надо добавлять строку
                                     ; DATA XREF: sub_7F000000

            */
            // ищем начало offsets
            // ищем:
            var regex = new Regex(@"(^[a-zA-Z0-9_?@]+\s+dd\soffset\s)" + txtCS, RegexOptions.Compiled);
            for (var index = idx; index < InListSource.Count; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                }

                // ??_7X2ClientToWorldPacket@@6B@ dd offset CS_PACKETS_return_0
                var matches = regex.Matches(InListSource[index]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                tmpLst.Add(InListSource[index]); // сохранили
                index++;
                // проверим есть следующая строка с отступом 40 пробелов
                var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.Compiled);
                var matchesSpace40 = regexSpace40.Matches(InListSource[index]);
                if (matchesSpace40.Count <= 0)
                {
                    //
                    // добавим отсутствующую строку
                    //
                    tmpLst.Add("                                        ; DATA XREF: sub_FFFFFFFF");
                }
                else
                {
                    //
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    //
                    do
                    {
                        tmpLst.Add(InListSource[index]); // сохранили
                        index++;
                        matchesSpace40 = regexSpace40.Matches(InListSource[index]);
                        if (matchesSpace40.Count <= 0)
                        {
                            break;
                        }
                    } while (true);
                }

                // dd offset CS_PACKET
                // или
                // dd offset SC_PACKET
                // затем сохраняем одну строку с начальными пробелами  [16]dd offset CS_SC_PACKET
                tmpLst.Add(InListSource[index]); // сохранили
                index++;
                //
                // запишем
                //
                // dd offset sub_395D0370
                // или
                // dd offset nullsub_18
                // или
                // dd offset CSInteractGimmickPacket
                // или
                // dd offset CSGmCommandPacket
                tmpLst.Add(InListSource[index]); // сохранили
                //index++;
                found = true;
            }

            return !found ? new List<string>() : tmpLst;
        }

        private List<string> CleanSourceOffsSC(int idx)
        {
            var progress = CalcProgress(InListSource.Count);

            var found = false;
            var tmpLst = new List<string>();
            // Рефакторинг: используем UIHelper для чтения значения из UI
            var txtSC = "";
            Helpers.UIHelper.InvokeUI(Dispatcher, () => txtSC = TextBox12.Text);
            //
            // ищем:
            //
            /*
               ??_7SCSetBountyDonePacket@@6B@ dd offset SC_PACKETS_return_2
            // ^[a-zA-Z0-9_?@]+\sdd\soffset\s         + txtSC
                                        ; DATA XREF: sub_3920B790+15↑o
                                        ; sub_3920B820+17↑o
                               dd offset CS_SC_PACKET
                               dd offset sub_395DCB90
                               dd offset ??_R4SCBountyPaidPacket@@6B@ ; const SCBountyPaidPacket::`RTTI Complete Object Locator'
               ; const SCBountyPaidPacket::`vftable'
               ??_7SCBountyPaidPacket@@6B@ dd offset SC_PACKETS_return_2
                                        ; DATA XREF: sub_3920B880+15↑o
                                        ; sub_3920B910+17↑o
                               dd offset CS_SC_PACKET
                               dd offset sub_395DCC80
            // ищем отступ: ^\s{8,}
                               align 8
               
             */
            //
            // ищем начало offsets
            //
            var regex = new Regex(@"(^[a-zA-Z0-9_?@]+\s+dd\soffset\s)" + txtSC, RegexOptions.Compiled);
            for (var index = idx; index < InListSource.Count; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                }

                var matches = regex.Matches(InListSource[index]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                tmpLst.Add(InListSource[index]); // сохранили
                index++;
                //
                // проверим есть следующая строка с отступом 40 пробелов
                //
                var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.Compiled);
                var matchesSpace40 = regexSpace40.Matches(InListSource[index]);
                if (matchesSpace40.Count <= 0)
                {
                    //
                    // добавим отсутствующую строку
                    //
                    tmpLst.Add("                                        ; DATA XREF: sub_FFFFFFFF");
                }
                else
                {
                    //
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    //
                    do
                    {
                        tmpLst.Add(InListSource[index]); // сохранили
                        index++;
                        matchesSpace40 = regexSpace40.Matches(InListSource[index]);
                        if (matchesSpace40.Count <= 0)
                        {
                            break;
                        }
                    } while (true);
                }

                // dd offset CS_PACKET
                // или
                // dd offset SC_PACKET
                // затем сохраняем одну строку с начальными пробелами  [16]dd offset CS_SC_PACKET
                tmpLst.Add(InListSource[index]); // сохранили
                index++;
                // запишем
                // dd offset sub_395D0370
                // или
                // dd offset nullsub_18
                // или
                // dd offset CSInteractGimmickPacket
                // или
                // dd offset CSGmCommandPacket
                tmpLst.Add(InListSource[index]); // сохранили
                //index++;
                found = true;
            }

            return !found ? new List<string>() : tmpLst;
        }

        private List<string> CleanSourceSpace(int idx)
        {
            var progress = CalcProgress(InListSource.Count);

            var found = false;
            var tmpLst = new List<string>();
            var tmpLst2 = new List<string>();
            // Рефакторинг: используем UIHelper для чтения значения из UI
            var txtSC = "";
            Helpers.UIHelper.InvokeUI(Dispatcher, () => txtSC = TextBox12.Text);
            var regex = new Regex(@"(^(\s+\w+\s+\d+)|^\s*$)", RegexOptions.IgnoreCase); // ищем мусорные строки 
            for (var index = idx; index < InListSource.Count; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                }

                var matches = regex.Matches(InListSource[index]);
                if (matches.Count > 0)
                {
                    //tmpLst2.Add(InListSource[index]); // сохранили мусор, для теста
                    continue;
                }

                tmpLst.Add(InListSource[index]); // сохранили
                found = true;
            }
            //var InListSource0 = new List<string>(tmpLst2);
            //ListView21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView21.ItemsSource = InListSource0; }));

            if (!found)
            {
                //
                // не нашли структуру
                //
                return new List<string>();
            }

            return tmpLst;
        }

        private List<string> CleanDestinationSub(int idx)
        {
            var progress = CalcProgress(InListDestination.Count);

            var maxCount = InListDestination.Count;
            var found = false;
            var tmpLst = new List<string>();
            //
            // ищем начало подпрограммы
            //
            var regexProcNear = new Regex(@"(proc\s+near)", RegexOptions.Compiled);
            //
            // ищем конец подпрограммы
            //
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled);
            //
            // ищем в теле подпрограммы то, что хотим оставить
            //
            //var regexSub = new Regex(@"push\s+offset\s|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            //var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+e[abcd]x,\s\[e[abcd]x\+([0-9A-F]{2}h)\]", RegexOptions.Compiled);
            for (var index = idx; index < maxCount; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar21.Value = index);
                }

                var matchesProcNear = regexProcNear.Matches(InListDestination[index]);
                if (matchesProcNear.Count <= 0)
                {
                    continue;
                }
                //
                // нашли начало подпрограммы
                //
                tmpLst.Add(InListDestination[index]); // сохранили
                //
                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                //
                var foundEndp = false;
                do
                {
                    if (index % progress == 0)
                    {
                        // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar21.Value = index);
                    }

                    index++;
                    //
                    // ищем:
                    //
                    // push    offset aDialog  ; "dialog"
                    // push\s+offset\s|
                    // call    sub_392299B0
                    // call\s+sub_\w+|
                    //                 mov     [ebp+var_A64C], offset ??_7CharacterStatePacket@@6B@ ; const CharacterStatePacket::`vftable'
                    // mov\s+\[\w+\+\w+\],\soffset\s|
                    //                  mov     [ebp+var_A648], 2Fh
                    // mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|
                    //                mov     dword ptr [eax+4], 1ADh
                    // mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|
                    //                mov     dword ptr [eax], offset ??_7SCSetBountyPermittedPacket@@6B@ ; const SCSetBountyPermittedPacket::`vftable'
                    // mov\s+dword\sptr\s\[\w+\],\soffset\s|
                    //                mov     dword ptr [ebp-10h], offset ??_7CSChangeSlaveTargetPacket@@6B@ ; const CSChangeSlaveTargetPacket::`vftable'
                    // mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|
                    //                mov     dword ptr [ebp-0Ch], 2Bh
                    // mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|

                    var matchesSub = regexSub.Matches(InListDestination[index]);
                    if (matchesSub.Count > 0)
                    {
                        tmpLst.Add(InListDestination[index]); // сохранили
                        found = true; // нашли структуру
                    }

                    var matches2 = regexEndP.Matches(InListDestination[index]);
                    if (matches2.Count <= 0)
                    {
                        continue;
                    }

                    foundEndp = true;
                    //
                    // нашли конец подпрограммы
                    //
                    tmpLst.Add(InListDestination[index]); // сохранили
                } while (index >= maxCount || !foundEndp);

            }

            if (!found)
            {
                //
                // не нашли структуру
                //
                return new List<string>();
            }

            return tmpLst;
        }

        private List<string> CleanSourceSubWithRetn(int idx)
        {
            var progress = CalcProgress(InListSource.Count);

            var maxCount = InListSource.Count;
            var found = false;
            var tmpLst = new List<string>();
            var regexProcNear = new Regex(@"(proc\s+near)", RegexOptions.Compiled);
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled);
            var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+e[abcd]x,\s\[e[abcd]x\+([0-9A-F]{2}h)\]", RegexOptions.Compiled);
            var regexRetn = new Regex(@"retn", RegexOptions.Compiled);

            for (var index = idx; index < maxCount; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                }

                var matchesProcNear = regexProcNear.Matches(InListSource[index]);
                if (matchesProcNear.Count <= 0)
                    continue;

                tmpLst.Add(InListSource[index]); // сохраняем

                var foundEndp = false;
                var foundEndpString = "";
                var foundRetnString = "";
                bool foundRetn = false;

                index++;

                do
                {
                    if (index % progress == 0)
                    {
                        // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                    }

                    var matchesSub = regexSub.Matches(InListSource[index]);
                    if (matchesSub.Count > 0)
                    {
                        tmpLst.Add(InListSource[index]); // сохраняем полезные строки процедуры
                    }

                    var matchesRetn = regexRetn.Match(InListSource[index]);
                    if (matchesRetn.Success)
                    {
                        foundRetn = true;
                        foundRetnString = InListSource[index];
                    }

                    // Если найден 'endp', продолжаем поиск до 'retn'
                    var matchesEndp = regexEndP.Match(InListSource[index]);
                    if (matchesEndp.Success)
                    {
                        foundEndp = true;
                        foundEndpString = InListSource[index];
                    }

                    // если вдруг найдем начало следующей процедуры, завершаем текущую процедуру
                    matchesProcNear = regexProcNear.Matches(InListSource[index]);
                    if (matchesProcNear.Count > 0 || (foundRetn && foundEndp))
                    {
                        tmpLst.Add(foundRetnString); // сохраняем `retn`
                        tmpLst.Add(foundEndpString); // сохраняем `endp`
                        index--;
                        break;
                    }

                    index++;
                } while (index < maxCount);
            }

            return tmpLst;
        }

        private List<string> CleanDestinationSubWithRetn(int idx)
        {
            var progress = CalcProgress(InListDestination.Count);

            var maxCount = InListDestination.Count;
            var found = false;
            var tmpLst = new List<string>();
            var regexProcNear = new Regex(@"(proc\s+near)", RegexOptions.Compiled);
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled);
            var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+e[abcd]x,\s\[e[abcd]x\+([0-9A-F]{2}h)\]", RegexOptions.Compiled);
            var regexRetn = new Regex(@"retn", RegexOptions.Compiled);

            for (var index = idx; index < maxCount; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar21.Value = index);
                }

                var matchesProcNear = regexProcNear.Matches(InListDestination[index]);
                if (matchesProcNear.Count <= 0)
                    continue;

                tmpLst.Add(InListDestination[index]); // сохраняем

                var foundEndp = false;
                var foundEndpString = "";
                var foundRetnString = "";
                bool foundRetn = false;

                index++;

                do
                {
                    if (index % progress == 0)
                    {
                        // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar21.Value = index);
                    }

                    var matchesSub = regexSub.Matches(InListDestination[index]);
                    if (matchesSub.Count > 0)
                    {
                        tmpLst.Add(InListDestination[index]); // сохраняем полезные строки процедуры
                    }

                    var matchesRetn = regexRetn.Match(InListDestination[index]);
                    if (matchesRetn.Success)
                    {
                        foundRetn = true;
                        foundRetnString = InListDestination[index];
                    }

                    // Если найден 'endp', продолжаем поиск до 'retn'
                    var matchesEndp = regexEndP.Match(InListDestination[index]);
                    if (matchesEndp.Success)
                    {
                        foundEndp = true;
                        foundEndpString = InListDestination[index];
                    }

                    // если вдруг найдем начало следующей процедуры, завершаем текущую процедуру
                    matchesProcNear = regexProcNear.Matches(InListDestination[index]);
                    if (matchesProcNear.Count > 0 || (foundRetn && foundEndp))
                    {
                        tmpLst.Add(foundRetnString); // сохраняем `retn`
                        tmpLst.Add(foundEndpString); // сохраняем `endp`
                        index--;
                        break;
                    }

                    index++;
                } while (index < maxCount);
            }

            return tmpLst;
        }

        private List<string> CleanDestinationCSOffs(int idx)
        {
            var progress = CalcProgress(InListDestination.Count);

            var found = false;
            var tmpLst = new List<string>();
            // Рефакторинг: используем UIHelper для чтения значения из UI
            var txtCS = "";
            Helpers.UIHelper.InvokeUI(Dispatcher, () => txtCS = TextBox21.Text);
            //
            // ищем:
            //
            /*
             off_3A01C97C    dd offset CS_PACKETS_return_0
                или
             ??_7CSBroadcastVisualOptionPacket@@6B@ dd offset CS_PACKETS_return_0
            // ^[a-zA-Z0-9_?@]+\s+dd\soffset\s               + txtCS
                                    ; DATA XREF: sub_391D5940+100↑o
                                    ; sub_39347360+69↑o ...
                            dd offset CS_SC_PACKET
                            dd offset sub_395DB460
            // ищем отступ: ^\s{8,}

            // бывает такое
                CSResturnAddrsPacket_0xfff dd offset CS_PACKETS_return_0
                            dd offset CS_PACKETS
                            dd offset sub_39807490
            // надо добавлять строку
                                     ; DATA XREF: sub_7F000000

            */
            //
            // ищем начало offsets
            // ищем:
            var regex = new Regex(@"(^[a-zA-Z0-9_?@]+\s+dd\soffset\s)" + txtCS, RegexOptions.Compiled);
            for (var index = idx; index < InListDestination.Count; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar21.Value = index);
                }

                // ??_7X2ClientToWorldPacket@@6B@ dd offset CS_PACKETS_return_0
                var matches = regex.Matches(InListDestination[index]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                tmpLst.Add(InListDestination[index]); // сохранили
                index++;
                //
                // проверим есть следующая строка с отступом 40 пробелов
                //
                var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.Compiled);
                var matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
                if (matchesSpace40.Count <= 0)
                {
                    //
                    // добавим отсутствующую строку
                    //
                    tmpLst.Add("                                        ; DATA XREF: sub_FFFFFFFF");
                }
                else
                {
                    //
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    //
                    do
                    {
                        tmpLst.Add(InListDestination[index]); // сохранили
                        index++;
                        matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
                        if (matchesSpace40.Count <= 0)
                        {
                            break;
                        }
                    } while (true);
                }

                // dd offset CS_PACKET
                // или
                // dd offset SC_PACKET
                // затем сохраняем одну строку с начальными пробелами  [16]dd offset CS_SC_PACKET
                tmpLst.Add(InListDestination[index]); // сохранили
                index++;
                //
                // запишем
                //
                // dd offset sub_395D0370
                // или
                // dd offset nullsub_18
                // или
                // dd offset CSInteractGimmickPacket
                // или
                // dd offset CSGmCommandPacket
                tmpLst.Add(InListDestination[index]); // сохранили
                                                      //index++;
                found = true;
            }

            return !found ? new List<string>() : tmpLst;
        }

        private List<string> CleanDestinationSCOffs(int idx)
        {
            var progress = CalcProgress(InListDestination.Count);

            var found = false;
            var tmpLst = new List<string>();
            // Рефакторинг: используем UIHelper для чтения значения из UI
            var txtSC = "";
            Helpers.UIHelper.InvokeUI(Dispatcher, () => txtSC = TextBox22.Text);
            //
            // ищем:
            //
            /*
               ??_7SCSetBountyDonePacket@@6B@ dd offset SC_PACKETS_return_2
            // ^[a-zA-Z0-9_?@]+\sdd\soffset\s         + txtSC
                                        ; DATA XREF: sub_3920B790+15↑o
                                        ; sub_3920B820+17↑o
                               dd offset CS_SC_PACKET
                               dd offset sub_395DCB90
                               dd offset ??_R4SCBountyPaidPacket@@6B@ ; const SCBountyPaidPacket::`RTTI Complete Object Locator'
               ; const SCBountyPaidPacket::`vftable'
               ??_7SCBountyPaidPacket@@6B@ dd offset SC_PACKETS_return_2
                                        ; DATA XREF: sub_3920B880+15↑o
                                        ; sub_3920B910+17↑o
                               dd offset CS_SC_PACKET
                               dd offset sub_395DCC80
            // ищем отступ: ^\s{8,}
                               align 8

             */
            //
            // ищем начало offsets
            //
            var regex = new Regex(@"(^[a-zA-Z0-9_?@]+\s+dd\soffset\s)" + txtSC, RegexOptions.Compiled);
            for (var index = idx; index < InListDestination.Count; index++)
            {
                if (index % progress == 0)
                {
                    // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar21.Value = index);
                }

                var matches = regex.Matches(InListDestination[index]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                tmpLst.Add(InListDestination[index]); // сохранили
                index++;
                //
                // проверим есть следующая строка с отступом 40 пробелов
                //
                var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.Compiled);
                var matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
                if (matchesSpace40.Count <= 0)
                {
                    //
                    // добавим отсутствующую строку
                    //
                    tmpLst.Add("                                        ; DATA XREF: sub_FFFFFFFF");
                }
                else
                {
                    //
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    //
                    do
                    {
                        tmpLst.Add(InListDestination[index]); // сохранили
                        index++;
                        matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
                        if (matchesSpace40.Count <= 0)
                        {
                            break;
                        }
                    } while (true);
                }

                // dd offset CS_PACKET
                // или
                // dd offset SC_PACKET
                //
                // затем сохраняем одну строку с начальными пробелами  [16]dd offset CS_SC_PACKET
                //
                tmpLst.Add(InListDestination[index]); // сохранили
                index++;
                //
                // запишем
                //
                // dd offset sub_395D0370
                // или
                // dd offset nullsub_18
                // или
                // dd offset CSInteractGimmickPacket
                // или
                // dd offset CSGmCommandPacket
                tmpLst.Add(InListDestination[index]); // сохранили
                                                      //index++;
                found = true;
            }

            return !found ? new List<string>() : tmpLst;
        }

        private static int CalcProgress(int maxCount)
        {
            var progress = maxCount / 100;
            if (progress == 0)
            {
                progress = 100;
            }

            return progress;
        }

        private void PreCleanSource()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            InListSource = new List<string>();
            string s;
            //var regexClr = new Regex(@"\;\ssub_\d+|\;\sDATA\sXREF\:|proc\s+near|\s+endp\s*|\soffset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+e[abcd]x,\s\[e[abcd]x\+([1-9]|[0-9A-F]{2,3}h)\]", RegexOptions.Compiled);
            var regexClr = new Regex(@"\;\ssub_\d+|\;\sDATA\sXREF\:|proc\s+near|\s+endp\s*|\soffset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+e[abcd]x,\s\[e[abcd]x\+([0-9A-F]{2}h)\]", RegexOptions.Compiled);
            //
            // узнаем количество строк в файле
            //
            var maxCount = File.ReadLines(FilePathIn1).Count();
            var progress = CalcProgress(maxCount);

            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar11.Value = 0,
                () => ProgressBar11.Maximum = maxCount
            );
            //
            // считываем по одной строке, отбрасываем не нужные и сохраняем нужные в InListSource
            //
            var index = 0;
            using (var f = new StreamReader(FilePathIn1))
            {
                while ((s = f.ReadLine()) != null)
                {
                    if (index % progress == 0)
                    {
                        // Рефакторинг: используем UIHelper для обновления прогрессбара
                    Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar11.Value = index);
                    }

                    index++;
                    // что-нибудь делаем с прочитанной строкой s
                    var matchesProcClr = regexClr.Matches(s);
                    if (matchesProcClr.Count <= 0)
                    {
                        continue;
                    }

                    InListSource.Add(s); // сохранили
                }
            }
            //
            // сохраним в файл
            //
            File.WriteAllLines(FilePathIn1, InListSource);
            //
            // заполним ListView
            //
            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ListView11.ItemsSource = InListSource,
                () => BtnLoadIn_Copy.IsEnabled = true,
                () => BtnLoadIn.IsEnabled = true,
                () => Label_Semafor1.Background = Brushes.GreenYellow,
                () => TextBox15.Text = elapsed
            );
        }

        private void CleanSource0()
        {
            var maxCount = InListSource.Count;
            var tmp = new List<string>();
            //
            // чистим сначала от пустых строк
            //
            var tmpSpace = CleanSourceSpace(0);
            tmp.AddRange(tmpSpace);

            InListSource = new List<string>(tmp);

            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar11.Value = 0,
                () => ProgressBar11.Maximum = maxCount,
                () => ProgressBar11.Value = InListSource.Count,
                () => ListView11.ItemsSource = InListSource,
                () => CheckBoxCleaningIn.IsChecked = false,
                () => Label_Semafor1.Background = Brushes.Yellow
            );
            
            File.WriteAllLines(FilePathIn1, InListSource);
        }

        /// <summary>
        /// CleanSource - очистка файла от не нужной информации
        /// </summary>
        private void CleanSource()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var maxCount = InListSource.Count;
            var tmp = new List<string>();

            // затем ищем подпрограммы
            var tmpSub = CleanSourceSubWithRetn(0);
            //var tmpSub = CleanSourceSub(0);
            tmp.AddRange(tmpSub);
            //
            // затем ищем оффсеты для CS
            //
            var tmpCSOffs = CleanSourceOffsCS(0);
            tmp.AddRange(tmpCSOffs);
            //
            // затем ищем оффсеты для SC
            //
            var tmpSCOffs = CleanSourceOffsSC(0);
            tmp.AddRange(tmpSCOffs);

            InListSource = new List<string>(tmp);
            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();

            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar11.Value = 0,
                () => ProgressBar11.Maximum = maxCount,
                () => ProgressBar11.Value = InListSource.Count,
                () => ListView11.ItemsSource = InListSource,
                () => CheckBoxCleaningIn.IsChecked = false,
                () => TextBox15.Text = elapsed,
                () => Label_Semafor1.Background = Brushes.GreenYellow,
                () => BtnCsLoadNameIn.IsEnabled = true,
                () => BtnScLoadNameIn.IsEnabled = true,
                () => BtnLoadIn_Copy.IsEnabled = true,
                () => BtnLoadIn.IsEnabled = true
            );
            
            File.WriteAllLines(FilePathIn1, InListSource);
        }

        /// <summary>
        /// CleanDestination - очистка файла от не нужной информации
        /// </summary>
        private void CleanDestination()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var maxCount = InListDestination.Count;
            var tmp = new List<string>();

            // затем ищем подпрограммы
            var tmpSub = CleanDestinationSubWithRetn(0);
            //var tmpSub = CleanDestinationSub(0);
            tmp.AddRange(tmpSub);
            //
            // затем ищем оффсеты для CS
            //
            var tmpCSOffs = CleanDestinationCSOffs(0);
            tmp.AddRange(tmpCSOffs);
            //
            // затем ищем оффсеты для SC
            //
            var tmpSCOffs = CleanDestinationSCOffs(0);
            tmp.AddRange(tmpSCOffs);

            InListDestination = new List<string>(tmp);
            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();

            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar21.Value = 0,
                () => ProgressBar21.Maximum = maxCount,
                () => ProgressBar21.Value = InListDestination.Count,
                () => ListView21.ItemsSource = InListDestination,
                () => CheckBoxCleaningOut.IsChecked = false,
                () => TextBox25.Text = elapsed,
                () => Label_Semafor2.Background = Brushes.GreenYellow,
                () => BtnCsLoadNameOut.IsEnabled = true,
                () => BtnScLoadNameOut.IsEnabled = true,
                () => BtnLoadOut.IsEnabled = true
            );
            
            File.WriteAllLines(FilePathIn2, InListDestination);
        }

        private ObservableCollection<Struc> FindStructureIn(string address)
        {
            var tmpLst = new ObservableCollection<Struc>();
            if (DepthIn == DepthMax)
            {
                return tmpLst;
            }

            DepthIn++;
            //
            // начали работу по поиску структур пакетов
            //
            // начнем с начала файла
            var found = false;
            var regexSub = new Regex(@"^" + address, RegexOptions.Compiled); // ищем начало подпрограммы, каждый раз с начала файла
            var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))", RegexOptions.Compiled);
            for (var index = 0; index < InListSource.Count; index++)
            {
                var matches4 = regexSub.Matches(InListSource[index]);
                if (matches4.Count <= 0)
                {
                    continue;
                }

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                var foundEndp = false;
                tmpLst = new ObservableCollection<Struc>();
                do
                {
                    index++;
                    var matches5 = regexCall.Matches(InListSource[index]);
                    foreach (var matchCall in matches5)
                    {
                        if (matchCall.ToString() == "call    eax" || matchCall.ToString() == "call    ebx" ||
                            matchCall.ToString() == "call    edx" || matchCall.ToString() == "call    ecx")
                        {
                            continue;
                        }

                        if (matchCall.ToString().Length >= 4 && matchCall.ToString().Substring(0, 4) == "call")
                        {
                            var findList = FindStructureIn(matchCall.ToString().Substring(8));
                            if (findList.Count > 0)
                            {
                                foreach (var item in findList)
                                {
                                    tmpLst.Add(item);
                                }
                                found = true; // нашли структуру
                            }
                        }
                        else
                        {
                            var aa = new Struc();
                            aa.Name = matchCall.ToString().Replace("\"", "");
                            index--;
                            var offset = InListSource[index].LastIndexOf("]", StringComparison.Ordinal) - 3;
                            // проверка на bc
                            string num;
                            try
                            {
                                num = offset < 0 ? "CC" : InListSource[index].Substring(offset, 2);
                                aa.Type = Convert.ToInt32(num, 16);
                            }
                            catch (Exception)
                            {
                                num = "CC";
                                aa.Type = Convert.ToInt32(num, 16);
                            }
                            index++;
                            tmpLst.Add(aa); // сохранили часть структуры пакета
                                            //tmpLst.Add(match5.ToString()); // сохранили часть структуры пакета
                            found = true; // нашли структуру
                        }
                    }

                    var matches6 = regexEndP.Matches(InListSource[index]);
                    if (matches6.Count <= 0)
                    {
                        continue;
                    }

                    foundEndp = true;
                } while (!foundEndp);

                //StructureSourceSC.Add(i, lst); // сохранили всю структуру пакета
                DepthIn--;
                //str = "<<--";
                //lst.Add(str);
                return tmpLst;
            }

            if (!found)
            {
                // не нашли структуру
                //lst = new List<string>();
                //StructureSourceSC.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                DepthIn--;
                return new ObservableCollection<Struc>();
            }

            return tmpLst;
        }

        private ObservableCollection<Struc> FindStructureOut(string address)
        {
            var tmpLst = new ObservableCollection<Struc>();
            if (DepthOut == DepthMax)
            {
                return tmpLst;
            }

            DepthOut++;
            //
            // начали работу по поиску структур пакетов
            //
            // начнем с начала файла
            var found = false;
            var regexSub = new Regex(@"^" + address, RegexOptions.Compiled); // ищем начало подпрограммы, каждый раз с начала файла
            var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))", RegexOptions.Compiled);
            for (var index = 0; index < InListDestination.Count; index++)
            {
                var matches4 = regexSub.Matches(InListDestination[index]);
                if (matches4.Count <= 0)
                {
                    continue;
                }

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                var foundEndp = false;
                tmpLst = new ObservableCollection<Struc>();
                do
                {
                    index++;
                    var matches5 = regexCall.Matches(InListDestination[index]);
                    foreach (var matchCall in matches5)
                    {
                        if (matchCall.ToString() == "call    eax" || matchCall.ToString() == "call    ebx" || matchCall.ToString() == "call    edx" || matchCall.ToString() == "call    ecx")
                        {
                            continue;
                        }

                        if (matchCall.ToString().Length >= 4 && matchCall.ToString().Substring(0, 4) == "call")
                        {
                            var findList = FindStructureOut(matchCall.ToString().Substring(8));
                            if (findList.Count > 0)
                            {
                                foreach (var item in findList)
                                {
                                    tmpLst.Add(item);
                                }
                                found = true; // нашли структуру
                            }
                        }
                        else
                        {
                            var aa = new Struc();
                            aa.Name = matchCall.ToString().Replace("\"", "");
                            index--;
                            var offset = InListDestination[index].LastIndexOf("]", StringComparison.Ordinal) - 3;
                            // проверка на bc
                            string num;
                            try
                            {
                                num = offset < 0 ? "CC" : InListDestination[index].Substring(offset, 2);
                                aa.Type = Convert.ToInt32(num, 16);
                            }
                            catch (Exception)
                            {
                                num = "CC";
                                aa.Type = Convert.ToInt32(num, 16);
                            }
                            index++;
                            tmpLst.Add(aa); // сохранили часть структуры пакета
                            found = true; // нашли структуру
                        }
                    }

                    var matches6 = regexEndP.Matches(InListDestination[index]);
                    if (matches6.Count <= 0)
                    {
                        continue;
                    }

                    foundEndp = true;
                } while (!foundEndp);

                DepthOut--;
                return tmpLst;
            }

            if (!found)
            {
                // не нашли структуру
                //lst = new List<string>();
                //StructureSourceSC.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                DepthOut--;
                return new ObservableCollection<Struc>();
            }

            return tmpLst;
        }

        /// <summary>
        /// Рефакторинг: универсальный метод для поиска структур пакетов
        /// Объединяет общую логику всех четырех методов FindSourceStructuresCS, FindSourceStructuresSC, FindDestinationStructuresCS, FindDestinationStructuresSC
        /// </summary>
        private void FindStructuresInternal(
            List<string> fileLines,
            string searchPattern,
            Dictionary<int, ObservableCollection<Struc>> structures,
            ObservableCollection<string> packetNames,
            IList<string> subAddresses,
            Dictionary<int, List<string>> xrefs,
            string unknownNamePrefix,
            Func<string, List<string>, int, bool, ObservableCollection<Struc>> findStructureFunc,
            Action<int> progressUpdate,
            bool findStruct,
            bool useCallSpaces4,
            bool skipRegisterCalls,
            Action initialUIUpdate,
            Action<int, int> afterExtractionUIUpdate,
            Action<string, bool, bool> finalUIUpdate,
            Action<bool> setFlag)
        {
            LogDebug($"FindStructuresInternal: Начало. searchPattern='{searchPattern}', fileLines.Count={fileLines?.Count ?? 0}, findStruct={findStruct}, useCallSpaces4={useCallSpaces4}, skipRegisterCalls={skipRegisterCalls}, unknownNamePrefix='{unknownNamePrefix}'");
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Инициализация структур данных
            structures.Clear();
            packetNames.Clear();
            subAddresses.Clear();
            xrefs.Clear();
            LogDebug($"FindStructuresInternal: Структуры данных очищены");

            // Начальное обновление UI
            initialUIUpdate?.Invoke();

            // Извлечение информации о пакетах
            ExtractPacketInfo(fileLines, searchPattern, packetNames, subAddresses, xrefs, unknownNamePrefix);

            // Обновление UI после извлечения информации
            var packetCount = packetNames.Count;
            var subCount = subAddresses.Count;
            LogDebug($"FindStructuresInternal: После ExtractPacketInfo. packetCount={packetCount}, subCount={subCount}");
            
            // Дополнительная диагностика: проверяем, что subAddresses действительно заполнен
            // Примечание: subAddresses имеет тип List<string>, а свойства теперь ObservableCollection<string>
            // Используем сравнение содержимого или проверку через ReferenceEquals для внутренних коллекций
            if (ReferenceEquals(subAddresses, _packetDataService.DestinationSubNames[Models.PacketType.CS]))
            {
                LogDebug($"FindStructuresInternal: subAddresses является ListSubDestinationCS. Count={ListSubDestinationCS.Count}");
                if (ListSubDestinationCS.Count > 0)
                {
                    var firstAddresses = string.Join(", ", ListSubDestinationCS.Take(5));
                    LogDebug($"FindStructuresInternal: Первые 5 адресов в ListSubDestinationCS: {firstAddresses}");
                }
            }
            else if (ReferenceEquals(subAddresses, _packetDataService.DestinationSubNames[Models.PacketType.SC]))
            {
                LogDebug($"FindStructuresInternal: subAddresses является ListSubDestinationSC. Count={ListSubDestinationSC.Count}");
            }
            else if (ReferenceEquals(subAddresses, _packetDataService.SourceSubNames[Models.PacketType.CS]))
            {
                LogDebug($"FindStructuresInternal: subAddresses является ListSubSourceCS. Count={ListSubSourceCS.Count}");
            }
            else if (ReferenceEquals(subAddresses, _packetDataService.SourceSubNames[Models.PacketType.SC]))
            {
                LogDebug($"FindStructuresInternal: subAddresses является ListSubSourceSC. Count={ListSubSourceSC.Count}");
            }
            else
            {
                LogWarn($"FindStructuresInternal: subAddresses не является ни одним из известных списков. subAddresses.Count={subAddresses.Count}");
            }
            
            afterExtractionUIUpdate?.Invoke(packetCount, subCount);

            // Поиск структур, если нужно
            if (findStruct)
            {
                LogDebug($"FindStructuresInternal: Начинаем поиск структур для {subCount} адресов");
                FindStructuresForSubAddresses(
                    fileLines,
                    subAddresses,
                    structures,
                    findStructureFunc,
                    progressUpdate,
                    useCallSpaces4,
                    skipRegisterCalls
                );
                LogDebug($"FindStructuresInternal: Поиск структур завершен. Найдено структур: {structures.Count}");
            }
            else
            {
                LogDebug($"FindStructuresInternal: Поиск структур пропущен (findStruct=false)");
            }

            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed.ToString();
            setFlag?.Invoke(true);
            var canCompareCS = _isInCs && _isOutCs;
            var canCompareSC = _isInSc && _isOutSc;

            // Финальное обновление UI
            finalUIUpdate?.Invoke(elapsed, canCompareCS, canCompareSC);
            LogDebug($"FindStructuresInternal: Завершено. Время выполнения: {elapsed}, structures.Count={structures.Count}");
            
            // Дополнительная диагностика: проверяем, что структуры действительно в словаре
            if (structures == StructureSourceCS)
            {
                LogDebug($"FindStructuresInternal: Проверка StructureSourceCS после завершения. Count={StructureSourceCS.Count}, ReferenceEquals={ReferenceEquals(structures, StructureSourceCS)}");
                if (StructureSourceCS.Count > 0)
                {
                    var firstKeys = string.Join(", ", StructureSourceCS.Keys.Take(5));
                    LogDebug($"FindStructuresInternal: Первые 5 ключей в StructureSourceCS: {firstKeys}");
                }
            }
            else if (structures == StructureDestinationCS)
            {
                LogDebug($"FindStructuresInternal: Проверка StructureDestinationCS после завершения. Count={StructureDestinationCS.Count}, ReferenceEquals={ReferenceEquals(structures, StructureDestinationCS)}");
                if (StructureDestinationCS.Count > 0)
                {
                    var firstKeys = string.Join(", ", StructureDestinationCS.Keys.Take(5));
                    LogDebug($"FindStructuresInternal: Первые 5 ключей в StructureDestinationCS: {firstKeys}");
                }
            }
            else
            {
                LogWarn($"FindStructuresInternal: structures не является ни StructureSourceCS, ни StructureDestinationCS. structures.Count={structures.Count}");
            }
        }

        /// <summary>
        /// Рефакторинг: извлекает общую логику поиска имен пакетов, адресов подпрограмм и XREF
        /// </summary>
        private void ExtractPacketInfo(
            List<string> fileLines,
            string searchPattern,
            ObservableCollection<string> packetNames,
            IList<string> subAddresses,
            Dictionary<int, List<string>> xrefs,
            string unknownNamePrefix = "CS_Unknown")
        {
            LogDebug($"ExtractPacketInfo: Начало извлечения информации. searchPattern={searchPattern}, fileLines.Count={fileLines?.Count ?? 0}");
            var regex = new Regex(@"^[a-zA-Z0-9_?@]+\s+dd\soffset\s" + searchPattern, RegexOptions.Compiled);
            var regexXREF = new Regex(@"(^\s+;[a-zA-Z:\s]*\s(sub_\w+|X2\w+|w+))", RegexOptions.Compiled);
            var indexRefs = 0;

            for (var index = 0; index < fileLines.Count; index++)
            {
                var foundName = false;
                var matches = regex.Matches(fileLines[index]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                var lst = new List<string>();
                var tmpIdx = index;
                var tmpIdxMax = tmpIdx + 2;
                do
                {
                    tmpIdx++;
                    if (tmpIdx >= fileLines.Count)
                        break;

                    // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                    var matchesXREF = regexXREF.Matches(fileLines[tmpIdx]);
                    if (matchesXREF.Count <= 0)
                    {
                        continue;
                    }

                    foreach (var match in matchesXREF)
                    {
                        lst.Add(match.ToString()); // сохранили XREF
                    }
                } while (tmpIdx < tmpIdxMax);

                xrefs.Add(indexRefs, lst); // сохраним список XREF для пакета
                indexRefs++; // следующий номер пакета

                var regex2 = new Regex(@"^(\S+)", RegexOptions.IgnoreCase);
                var matches2 = regex2.Matches(fileLines[index]);
                foreach (var match2 in matches2)
                {
                    packetNames.Add(match2.ToString()); // сохранили имя
                    foundName = true;
                }

                if (!foundName)
                {
                    // не нашли имя пакета, бывает что его нет из-зи защиты themida
                    packetNames.Add(unknownNamePrefix); // сохранили адрес подпрограммы
                }

                // сначала нужно пропустить строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                do
                {
                    index++;
                    if (index >= fileLines.Count)
                        break;

                    var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.IgnoreCase);
                    var matchesSpace40 = regexSpace40.Matches(fileLines[index]);
                    if (matchesSpace40.Count <= 0)
                    {
                        break;
                    }
                } while (true);

                // пропускаем
                // dd offset CS_PACKET
                // или
                // dd offset SC_PACKET
                // затем одну строку с начальными пробелами  [16]dd offset CS_SC_PACKET
                index++;
                if (index >= fileLines.Count)
                    continue;

                // ищем "dd offset sub_395D0370"
                // dd offset nullsub_18
                // dd offset CSInteractGimmickPacket
                // dd offset CSGmCommandPacket
                try
                {
                    // Используем regex с группами захвата для более надежного извлечения адреса
                    // Паттерн ищет "dd offset " и захватывает адрес после него
                    var regexBody = new Regex(@"dd\s+offset\s+(nullsub_\w+|sub_\w+|\w+)", RegexOptions.Compiled);
                    var matchesBodys = regexBody.Match(fileLines[index]);
                    LogDebug($"ExtractPacketInfo: Строка {index}: '{fileLines[index]}', matchesBodys.Success={matchesBodys.Success}, Groups.Count={matchesBodys.Groups.Count}");
                    if (matchesBodys.Success && matchesBodys.Groups.Count > 1)
                    {
                        // Используем первую группу захвата (индекс 1) для получения адреса
                        var address = matchesBodys.Groups[1].Value;
                        LogDebug($"ExtractPacketInfo: Извлечен адрес: '{address}' из строки {index}");
                        if (!string.IsNullOrEmpty(address))
                        {
                            subAddresses.Add(address); // сохранили адрес подпрограммы
                            LogDebug($"ExtractPacketInfo: Добавлен адрес подпрограммы: '{address}', всего адресов: {subAddresses.Count}");
                        }
                        else
                        {
                            LogWarn($"ExtractPacketInfo: Адрес пустой для строки {index}");
                        }
                    }
                    else
                    {
                        LogDebug($"ExtractPacketInfo: Не удалось извлечь адрес из строки {index}: '{fileLines[index]}'");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"ExtractPacketInfo: Ошибка при обработке строки {index}: '{fileLines[index]}'", ex);
                    // Рефакторинг: используем UIHelper для показа ошибки
                    Helpers.UIHelper.ShowError(Dispatcher, $"Проверьте исходные данные файла в IDA, где-то в строке: {index}!", "Error");
                }
            }
            LogDebug($"ExtractPacketInfo: Завершено. Найдено пакетов: {packetNames.Count}, адресов подпрограмм: {subAddresses.Count}");
        }

        /// <summary>
        /// Рефакторинг: извлекает общую логику поиска структур для подпрограмм
        /// </summary>
        private void FindStructuresForSubAddresses(
            List<string> fileLines,
            IList<string> subAddresses,
            Dictionary<int, ObservableCollection<Struc>> structures,
            Func<string, List<string>, int, bool, ObservableCollection<Struc>> findStructureFunc,
            Action<int> progressUpdate,
            bool useCallSpaces4 = false,
            bool skipRegisterCalls = false)
        {
            LogDebug($"FindStructuresForSubAddresses: Начало поиска структур. subAddresses.Count={subAddresses?.Count ?? 0}, fileLines.Count={fileLines?.Count ?? 0}, useCallSpaces4={useCallSpaces4}");
            if (subAddresses == null || subAddresses.Count == 0)
            {
                LogWarn("FindStructuresForSubAddresses: subAddresses пуст или null");
                return;
            }

            // Рефакторинг: создаем regex паттерны один раз
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled);
            var regexCallPattern = useCallSpaces4
                ? @"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))"
                : @"(\x22[0-z._]+\x22)|(call\s+(sub_\w+)|(call\s+(\w+)))";
            var regexCall = new Regex(regexCallPattern, RegexOptions.Compiled);

            // Рефакторинг: сбрасываем глубину рекурсии один раз в начале поиска всех структур
            // Сбрасываем обе глубины, так как не знаем, какая функция используется
            _structureFinderService.ResetDepthIn();
            _structureFinderService.ResetDepthOut();

            for (var i = 0; i < subAddresses.Count; i++)
            {
                LogDebug($"FindStructuresForSubAddresses: Обработка адреса [{i}]: '{subAddresses[i]}'");
                var found = false;
                // В старом коде адрес НЕ экранировался - используем как есть
                var regexSub = new Regex(@"^" + subAddresses[i], RegexOptions.Compiled);
                LogDebug($"FindStructuresForSubAddresses: Создан regex для адреса '{subAddresses[i]}': '^{subAddresses[i]}'");
                
                for (var index = 0; index < fileLines.Count; index++)
                {
                    var matchesSub = regexSub.Matches(fileLines[index]);
                    if (matchesSub.Count <= 0)
                    {
                        continue;
                    }

                    LogDebug($"FindStructuresForSubAddresses: Найдено начало подпрограммы '{subAddresses[i]}' в строке {index}: '{fileLines[index]}'");
                    // Нашли начало подпрограммы, ищем структуры, пока не "endp"
                    var foundEndp = false;
                    var lst = new ObservableCollection<Struc>();
                    
                    do
                    {
                        var matchesCalls = regexCall.Matches(fileLines[index]);
                        foreach (var matchCall in matchesCalls)
                        {
                            var callStr = matchCall.ToString();
                            
                            // Рефакторинг: пропускаем вызовы регистров, если нужно
                            if (skipRegisterCalls && (callStr == "call    eax" || callStr == "call    ebx" ||
                                callStr == "call    edx" || callStr == "call    ecx"))
                            {
                                continue;
                            }

                            if (callStr.Length >= 4 && callStr.Substring(0, 4) == "call")
                            {
                                // Рефакторинг: используем переданную функцию для поиска структуры
                                // В старом коде использовался Substring(8) без Trim()
                                var callAddress = "";
                                if (callStr.Length >= 8)
                                {
                                    // Формат "call    sub_xxx" или "call    xxx"
                                    callAddress = callStr.Substring(8);
                                }
                                else if (callStr.Length >= 5)
                                {
                                    // Формат "call sub_xxx" или "call xxx"
                                    callAddress = callStr.Substring(5);
                                }
                                
                                // В старом коде не было проверки на пустую строку
                                if (callAddress.Length > 0)
                                {
                                    // Пропускаем регистры и другие не-адреса подпрограмм
                                    var lowerAddress = callAddress.ToLower();
                                    if (lowerAddress == "eax" || lowerAddress == "ebx" || lowerAddress == "ecx" || 
                                        lowerAddress == "edx" || lowerAddress == "esi" || lowerAddress == "edi" || 
                                        lowerAddress == "esp" || lowerAddress == "ebp" ||
                                        lowerAddress == "ds" || lowerAddress == "cs" || lowerAddress == "es" || 
                                        lowerAddress == "fs" || lowerAddress == "gs" || lowerAddress == "ss" ||
                                        callAddress.StartsWith("__libm_") || callAddress == "floor" || 
                                        callAddress == "ceil" || callAddress == "sqrt")
                                    {
                                        LogDebug($"FindStructuresForSubAddresses: Пропущен не-валидный адрес '{callAddress}' (регистр или библиотечная функция)");
                                        continue;
                                    }
                                    
                                    LogDebug($"FindStructuresForSubAddresses: Рекурсивный вызов для адреса '{callAddress}' из подпрограммы '{subAddresses[i]}'");
                                    var findList = findStructureFunc(callAddress, fileLines, DepthMax, useCallSpaces4);
                                    LogDebug($"FindStructuresForSubAddresses: Результат рекурсивного вызова для '{callAddress}': найдено {findList.Count} структур");
                                    if (findList.Count > 0)
                                    {
                                        foreach (var item in findList)
                                        {
                                            lst.Add(item);
                                        }
                                        found = true; // нашли структуру
                                    }
                                }
                                else
                                {
                                    LogDebug($"FindStructuresForSubAddresses: callAddress пустой для callStr: '{callStr}'");
                                }
                            }
                            else
                            {
                                var aa = new Struc();
                                aa.Name = callStr.Replace("\"", "");
                                index--;
                                var offset = fileLines[index].LastIndexOf("]", StringComparison.Ordinal) - 3;
                                
                                string num;
                                try
                                {
                                    num = offset < 0 ? "CC" : fileLines[index].Substring(offset, 2);
                                    aa.Type = Convert.ToInt32(num, 16);
                                }
                                catch (Exception)
                                {
                                    num = "CC";
                                    aa.Type = Convert.ToInt32(num, 16);
                                }
                                lst.Add(aa);
                                index++;
                            }
                        }

                        var matchesEndP = regexEndP.Matches(fileLines[index]);
                        if (matchesEndP.Count > 0)
                        {
                            foundEndp = true;
                        }
                        index++;
                    } while (index < fileLines.Count && !foundEndp);

                    LogDebug($"FindStructuresForSubAddresses: Найдена структура для адреса [{i}] '{subAddresses[i]}': {lst.Count} элементов");
                    structures.Add(i, lst);
                    found = true;
                    break;
                }

                if (!found)
                {
                    LogWarn($"FindStructuresForSubAddresses: Не найдена структура для адреса [{i}] '{subAddresses[i]}'");
                    // Не нашли структуру
                    structures.Add(i, new ObservableCollection<Struc>());
                }

                // Рефакторинг: обновляем прогресс
                progressUpdate(structures.Count);
            }
            LogDebug($"FindStructuresForSubAddresses: Завершено. Найдено структур: {structures.Count}");
        }

        private void FindSourceStructuresCS(string str)
        {
            LogDebug($"FindSourceStructuresCS: Начало. searchPattern='{str}'");
            // Рефакторинг: используем универсальный метод
            FindStructuresInternal(
                InListSource,
                str,
                StructureSourceCS,
                ListNameSourceCS,
                ListSubSourceCS,
                XrefsIn,
                "CS_Unknown",
                (address, lines, depth, useSpaces4) => _structureFinderService.FindStructureIn(address, lines, depth, useSpaces4),
                count => Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar12.Value = count),
                FindStructIn,
                false,
                false,
                () => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox13.Text = "0",
                    () => TextBox14.Text = "0",
                    () => TextBox18.Text = "0",
                    () => ProgressBar11.Value = InListSource.Count,
                    () => ProgressBar12.Value = 0,
                    () => Label_Semafor1.Background = Brushes.Yellow,
                    () => ButtonSaveIn1.IsEnabled = false,
                    () => ButtonSaveIn2.IsEnabled = false
                ),
                (packetCount, subCount) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox13.Text = packetCount.ToString(),
                    () => TextBox14.Text = subCount.ToString(),
                    () => ListView12.ItemsSource = ListNameSourceCS,
                    () => ListView13.ItemsSource = ListSubSourceCS,
                    () => ProgressBar12.Maximum = packetCount
                ),
                (elapsed, canCompareCS, canCompareSC) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => BtnLoadIn.IsEnabled = true,
                    () => BtnLoadIn_Copy.IsEnabled = true,
                    () => ButtonCsCompare.IsEnabled = canCompareCS,
                    () => ButtonScCompare.IsEnabled = false,
                    () => ButtonSaveIn1.IsEnabled = true,
                    () => TextBox18.Text = elapsed,
                    () => Label_Semafor1.Background = Brushes.GreenYellow,
                    () => BtnMakePktIn.IsEnabled = true,
                    () => BtnGotoOpcodeIn.IsEnabled = true,
                    () => BtnCsLoadNameIn.IsEnabled = true,
                    () => BtnScLoadNameIn.IsEnabled = true
                ),
                isInCs => _isInCs = isInCs
            );
        }

        private void FindSourceStructuresSC(string str)
        {
            LogDebug($"FindSourceStructuresSC: Начало. searchPattern='{str}'");
            // Рефакторинг: используем универсальный метод
            FindStructuresInternal(
                InListSource,
                str,
                StructureSourceSC,
                ListNameSourceSC,
                ListSubSourceSC,
                XrefsIn,
                "CS_Unknown",
                (address, lines, depth, useSpaces4) => _structureFinderService.FindStructureIn(address, lines, depth, useSpaces4),
                count => Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar12.Value = count),
                FindStructIn,
                false,
                false,
                () => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox16.Text = "0",
                    () => TextBox17.Text = "0",
                    () => TextBox19.Text = "0",
                    () => ProgressBar11.Value = InListSource.Count,
                    () => ProgressBar12.Value = 0,
                    () => Label_Semafor1.Background = Brushes.Yellow,
                    () => ButtonSaveIn1.IsEnabled = false,
                    () => ButtonSaveIn2.IsEnabled = false,
                    () => BtnLoadIn.IsEnabled = false,
                    () => BtnLoadIn_Copy.IsEnabled = false,
                    () => BtnCsLoadNameIn.IsEnabled = false,
                    () => BtnScLoadNameIn.IsEnabled = false,
                    () => BtnMakePktIn.IsEnabled = false,
                    () => BtnGotoOpcodeIn.IsEnabled = false
                ),
                (packetCount, subCount) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox16.Text = packetCount.ToString(),
                    () => TextBox17.Text = subCount.ToString(),
                    () => ListView12.ItemsSource = ListNameSourceSC,
                    () => ListView13.ItemsSource = ListSubSourceSC,
                    () => ProgressBar12.Maximum = packetCount
                ),
                (elapsed, canCompareCS, canCompareSC) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => BtnLoadIn.IsEnabled = true,
                    () => BtnLoadIn_Copy.IsEnabled = true,
                    () => ButtonCsCompare.IsEnabled = false,
                    () => ButtonScCompare.IsEnabled = canCompareSC,
                    () => ButtonSaveIn2.IsEnabled = true,
                    () => TextBox19.Text = elapsed,
                    () => Label_Semafor1.Background = Brushes.GreenYellow,
                    () => BtnMakePktIn.IsEnabled = true,
                    () => BtnGotoOpcodeIn.IsEnabled = true,
                    () => BtnCsLoadNameIn.IsEnabled = true,
                    () => BtnScLoadNameIn.IsEnabled = true
                ),
                isInSc => _isInSc = isInSc
            );
        }

        private void FindDestinationStructuresCS(string str)
        {
            LogDebug($"FindDestinationStructuresCS: Начало. searchPattern='{str}'");
            LogDebug($"FindDestinationStructuresCS: ListSubDestinationCS.Count до вызова FindStructuresInternal: {ListSubDestinationCS.Count}");
            // Рефакторинг: используем универсальный метод
            FindStructuresInternal(
                InListDestination,
                str,
                StructureDestinationCS,
                ListNameDestinationCS,
                ListSubDestinationCS,
                XrefsOut,
                "CS_Unknown",
                (address, lines, depth, useSpaces4) => _structureFinderService.FindStructureOut(address, lines, depth, useSpaces4),
                count => Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar22.Value = count),
                FindStructOut,
                true,
                false,
                () => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox23.Text = "0",
                    () => TextBox24.Text = "0",
                    () => TextBox28.Text = "0",
                    () => ProgressBar21.Value = InListDestination.Count,
                    () => ProgressBar22.Value = 0,
                    () => Label_Semafor2.Background = Brushes.Yellow,
                    () => ButtonSaveOut1.IsEnabled = false,
                    () => ButtonSaveOut2.IsEnabled = false,
                    () => BtnLoadOut.IsEnabled = false,
                    () => BtnCsLoadNameOut.IsEnabled = false,
                    () => BtnScLoadNameOut.IsEnabled = false,
                    () => BtnUpdStruct.IsEnabled = false,
                    () => ButtonEditOutOpcode.IsEnabled = false,
                    () => BtnMakePktOut.IsEnabled = false,
                    () => BtnGotoOpcodeOut.IsEnabled = false,
                    () => ButtonCsCompare.IsEnabled = false,
                    () => ButtonScCompare.IsEnabled = false,
                    () => BtnSaveSnapshot.IsEnabled = false,
                    () => BtnLoadSnapshotCS.IsEnabled = false,
                    () => BtnLoadSnapshotSC.IsEnabled = false
                ),
                (packetCount, subCount) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox23.Text = packetCount.ToString(),
                    () => TextBox24.Text = subCount.ToString(),
                    () => ListView22.ItemsSource = ListNameDestinationCS,
                    () => ListView23.ItemsSource = ListSubDestinationCS,
                    () => ProgressBar22.Maximum = packetCount
                ),
                (elapsed, canCompareCS, canCompareSC) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => BtnCsLoadNameOut.IsEnabled = true,
                    () => BtnScLoadNameOut.IsEnabled = true,
                    () => BtnLoadOut.IsEnabled = true,
                    () => ButtonCsCompare.IsEnabled = canCompareCS,
                    () => ButtonScCompare.IsEnabled = false,
                    () => ButtonSaveOut1.IsEnabled = true,
                    () => TextBox28.Text = elapsed,
                    () => Label_Semafor2.Background = Brushes.GreenYellow,
                    () => BtnUpdStruct.IsEnabled = true,
                    () => ButtonEditOutOpcode.IsEnabled = true,
                    () => BtnMakePktOut.IsEnabled = true,
                    () => BtnGotoOpcodeOut.IsEnabled = true,
                    () => BtnSaveSnapshot.IsEnabled = true,
                    () => BtnLoadSnapshotCS.IsEnabled = true,
                    () => BtnLoadSnapshotSC.IsEnabled = true
                ),
                isOutCs => _isOutCs = isOutCs
            );
            LogDebug($"FindDestinationStructuresCS: Завершено. ListSubDestinationCS.Count после вызова FindStructuresInternal: {ListSubDestinationCS.Count}");
            if (ListSubDestinationCS.Count > 0)
            {
                var firstAddresses = string.Join(", ", ListSubDestinationCS.Take(5));
                LogDebug($"FindDestinationStructuresCS: Первые 5 адресов в ListSubDestinationCS: {firstAddresses}");
            }
            else
            {
                LogWarn("FindDestinationStructuresCS: ListSubDestinationCS пуст после завершения FindStructuresInternal!");
            }
        }

        private void FindDestinationStructuresSC(string str)
        {
            LogDebug($"FindDestinationStructuresSC: Начало. searchPattern='{str}'");
            // Рефакторинг: используем универсальный метод
            FindStructuresInternal(
                InListDestination,
                str,
                StructureDestinationSC,
                ListNameDestinationSC,
                ListSubDestinationSC,
                XrefsOut,
                "SC_Unknown",
                (address, lines, depth, useSpaces4) => _structureFinderService.FindStructureOut(address, lines, depth, useSpaces4),
                count => Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar22.Value = count),
                FindStructOut,
                false,
                true,
                () => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox26.Text = "0",
                    () => TextBox27.Text = "0",
                    () => TextBox29.Text = "0",
                    () => ProgressBar21.Value = InListDestination.Count,
                    () => ProgressBar22.Value = 0,
                    () => Label_Semafor2.Background = Brushes.Yellow,
                    () => ButtonSaveOut1.IsEnabled = false,
                    () => ButtonSaveOut2.IsEnabled = false,
                    () => BtnLoadOut.IsEnabled = false,
                    () => BtnCsLoadNameOut.IsEnabled = false,
                    () => BtnScLoadNameOut.IsEnabled = false,
                    () => BtnUpdStruct.IsEnabled = false,
                    () => ButtonEditOutOpcode.IsEnabled = false,
                    () => BtnMakePktOut.IsEnabled = false,
                    () => BtnGotoOpcodeOut.IsEnabled = false,
                    () => ButtonCsCompare.IsEnabled = false,
                    () => ButtonScCompare.IsEnabled = false,
                    () => BtnSaveSnapshot.IsEnabled = false,
                    () => BtnLoadSnapshotCS.IsEnabled = false,
                    () => BtnLoadSnapshotSC.IsEnabled = false
                ),
                (packetCount, subCount) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => TextBox26.Text = packetCount.ToString(),
                    () => TextBox27.Text = subCount.ToString(),
                    () => ListView22.ItemsSource = ListNameDestinationSC,
                    () => ListView23.ItemsSource = ListSubDestinationSC,
                    () => ProgressBar22.Maximum = packetCount
                ),
                (elapsed, canCompareCS, canCompareSC) => Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                    () => BtnCsLoadNameOut.IsEnabled = true,
                    () => BtnScLoadNameOut.IsEnabled = true,
                    () => BtnLoadOut.IsEnabled = true,
                    () => ButtonCsCompare.IsEnabled = false,
                    () => ButtonScCompare.IsEnabled = canCompareSC,
                    () => ButtonSaveOut2.IsEnabled = true,
                    () => TextBox29.Text = elapsed,
                    () => Label_Semafor2.Background = Brushes.GreenYellow,
                    () => BtnUpdStruct.IsEnabled = true,
                    () => ButtonEditOutOpcode.IsEnabled = true,
                    () => BtnMakePktOut.IsEnabled = true,
                    () => BtnGotoOpcodeOut.IsEnabled = true,
                    () => BtnSaveSnapshot.IsEnabled = true,
                    () => BtnLoadSnapshotCS.IsEnabled = true,
                    () => BtnLoadSnapshotSC.IsEnabled = true
                ),
                isOutSc => _isOutSc = isOutSc
            );
        }

        private async void btn_Load_In_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor1.Background = Brushes.Red;
            BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;
            BtnMakePktIn.IsEnabled = false;
            BtnGotoOpcodeIn.IsEnabled = false;
            ButtonSaveIn1.IsEnabled = false;
            ButtonSaveIn2.IsEnabled = false;

            if (OpenFileDialog1())
            {
                TextBoxPathIn.Text = FilePathIn1;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    // Рефакторинг: использование FileProcessor вместо File.ReadAllLines
                    var progress = new Progress<int>(percent =>
                    {
                        Dispatcher.Invoke(() => ProgressBar11.Value = percent);
                    });
                    
                    // Загружаем файл напрямую в PacketDataService
                    var fileLines = await _fileProcessor.ReadFileLinesAsync(FilePathIn1, progress);
                    _packetDataService.SourceFileLines.Clear();
                    _packetDataService.SourceFileLines.AddRange(fileLines);
                    
                    // Рефакторинг: используем UIHelper для обновления UI
                    await Helpers.UIHelper.InvokeUIAsync(Dispatcher, () =>
                    {
                        ListView11.ItemsSource = _packetDataService.SourceFileLines;
                    });
                }
                catch (Exception ex)
                {
                    // Рефакторинг: используем UIHelper для показа ошибки
                    Helpers.UIHelper.ShowError(Dispatcher, $"Ошибка при загрузке файла: {ex.Message}", "Ошибка");
                    Helpers.UIHelper.InvokeUI(Dispatcher, () =>
                    {
                        Label_Semafor1.Background = Brushes.Red;
                        BtnLoadIn.IsEnabled = true;
                    });
                    return;
                }

                // инициализируем прогрессбары и списки
                InitializeIn();

                isCleaningIn = CheckBoxCleaningIn.IsChecked == true;
                if (isCleaningIn)
                {
                    await Task.Run(() =>
                    {
                        CleanSource();
                    });
                }
                else
                {
                    BtnCsLoadNameIn.IsEnabled = true;
                    BtnScLoadNameIn.IsEnabled = true;
                    BtnLoadIn_Copy.IsEnabled = true;
                    BtnLoadIn.IsEnabled = true;
                    BtnMakePktIn.IsEnabled = true;
                    BtnGotoOpcodeIn.IsEnabled = true;
                }

                stopWatch.Stop();
                TextBox15.Text = stopWatch.Elapsed.ToString();
                isCompareCS = false;
                isCompareSC = false;
                Label_Semafor1.Background = Brushes.Yellow;
            }
            else
            {
                // Рефакторинг: используем UIHelper для показа сообщения
                Helpers.UIHelper.ShowInfo(Dispatcher, "Для работы программы необходимо выбрать .asm файл!", "Error");
                BtnLoadIn_Copy.IsEnabled = true;
                BtnLoadIn.IsEnabled = true;
            }
        }

        private void InitializeIn()
        {
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar11.Value = 0,
                () => ProgressBar12.Value = 0,
                () => ProgressBar13.Value = 0,
                () => ListView12.ItemsSource = new List<string>(),
                () => ListView13.ItemsSource = new List<string>(),
                () => ListView14.ItemsSource = new List<string>(),
                () => ListView31.ItemsSource = new List<string>(),
                () => ListView32.ItemsSource = new List<string>(),
                () => TextBox13.Text = "0",
                () => TextBox14.Text = "0",
                () => TextBox18.Text = "0",
                () => TextBox16.Text = "0",
                () => TextBox17.Text = "0",
                () => TextBox19.Text = "0",
                () => TextBox16Copy.Text = "0",
                () => TextBox17Copy.Text = "0",
                () => TextBox19Copy.Text = "0",
                () => TextBox32.Text = "0",
                () => TextBox33.Text = "0",
                () => ButtonCsCompare.IsEnabled = false,
                () => Button2Copy2.IsEnabled = false,
                () => Button2Copy2_Copy.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = false,
                () => ButtonCopy2.IsEnabled = false,
                () => ButtonCopy2_Copy.IsEnabled = false
            );
        }

        private void InitializeOut()
        {
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => ProgressBar21.Value = 0,
                () => ProgressBar22.Value = 0,
                () => ProgressBar23.Value = 0,
                () => ListView22.ItemsSource = new List<string>(),
                () => ListView23.ItemsSource = new List<string>(),
                () => ListView24.ItemsSource = new List<string>(),
                () => ListView31.ItemsSource = new List<string>(),
                () => ListView32.ItemsSource = new List<string>(),
                () => TextBox23.Text = "0",
                () => TextBox24.Text = "0",
                () => TextBox28.Text = "0",
                () => TextBox26.Text = "0",
                () => TextBox27.Text = "0",
                () => TextBox29.Text = "0",
                () => TextBox16Copy1.Text = "0",
                () => TextBox17Copy1.Text = "0",
                () => TextBox19Copy1.Text = "0",
                () => TextBox32.Text = "0",
                () => TextBox33.Text = "0",
                () => ButtonCsCompare.IsEnabled = false,
                () => Button2Copy2.IsEnabled = false,
                () => Button2Copy2_Copy.IsEnabled = false,
                () => ButtonScCompare.IsEnabled = false,
                () => ButtonCopy2.IsEnabled = false,
                () => ButtonCopy2_Copy.IsEnabled = false
            );
        }

        private void btn_SC_Load_Name1_Click(object sender, RoutedEventArgs e)
        {
            InitializeIn();

            BtnCsLoadNameIn.IsEnabled = false;
            BtnScLoadNameIn.IsEnabled = false;
            BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;
            BtnMakePktIn.IsEnabled = false;
            BtnGotoOpcodeIn.IsEnabled = false;
            var inText = TextBox12.Text;
            DepthIn = 0;

            ListNameSourceSC.Clear();
            FindOpcodeIn = CheckBoxFindOpcodeIn.IsChecked == true;
            FindStructIn = CheckBoxFindStructIn.IsChecked == true;
            _isInSc = false;
            //_isOutSc = false;
            _isInCs = false;
            //_isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            // Рефакторинг: используем async/await вместо Thread
            _ = Task.Run(async () =>
            {
                FindSourceStructuresSC(inText);

                if (FindOpcodeIn)
                {
                    // Рефакторинг: используем новый сервис для поиска опкодов
                    await FindOpcodeSourceSCAsync();
                }
            });
        }

        private void btn_CS_Clear_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor1.Background = Brushes.Red;
            BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;

            if (OpenFileDialog1())
            {
                TextBoxPathIn.Text = FilePathIn1;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                //lock (lockObj)
                {
                    // Рефакторинг: используем Task.Run вместо Thread
                    _ = Task.Run(() =>
                    {
                        PreCleanSource();
                    });
                }

                stopWatch.Stop();
                TextBox15.Text = stopWatch.Elapsed.ToString();
                isCompareCS = false;
                isCompareSC = false;
                Label_Semafor1.Background = Brushes.Yellow;
            }
            else
            {
                // Рефакторинг: используем UIHelper для показа сообщения
                Helpers.UIHelper.ShowInfo(Dispatcher, "Для работы программы необходимо выбрать .asm файл!", "Error");
                BtnLoadIn_Copy.IsEnabled = true;
                BtnLoadIn.IsEnabled = true;
            }
        }

        private void btn_CS_Load_Name1_Click(object sender, RoutedEventArgs e)
        {
            InitializeIn();

            BtnCsLoadNameIn.IsEnabled = false;
            BtnScLoadNameIn.IsEnabled = false;
            BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;
            BtnMakePktIn.IsEnabled = false;
            BtnGotoOpcodeIn.IsEnabled = false;
            var inText = TextBox11.Text;
            DepthIn = 0;

            ListNameSourceCS.Clear();
            FindOpcodeIn = CheckBoxFindOpcodeIn.IsChecked == true;
            FindStructIn = CheckBoxFindStructIn.IsChecked == true;
            _isInSc = false;
            //_isOutSc = false;
            _isInCs = false;
            //_isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            // Рефакторинг: используем async/await вместо Thread
            _ = Task.Run(async () =>
            {
                FindSourceStructuresCS(inText);

                if (FindOpcodeIn)
                {
                    // Рефакторинг: используем новый сервис для поиска опкодов
                    await FindOpcodeSourceCSAsync();
                }
            });
        }

        private async void btn_Load_Out_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor2.Background = Brushes.Red;
            //BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;

            if (OpenFileDialog2())
            {
                TextBoxPathOut.Text = FilePathIn2;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    // Рефакторинг: использование FileProcessor вместо File.ReadAllLines
                    var progress = new Progress<int>(percent =>
                    {
                        Dispatcher.Invoke(() => ProgressBar21.Value = percent);
                    });
                    
                    // Загружаем файл напрямую в PacketDataService
                    var fileLines = await _fileProcessor.ReadFileLinesAsync(FilePathIn2, progress);
                    _packetDataService.DestinationFileLines.Clear();
                    _packetDataService.DestinationFileLines.AddRange(fileLines);
                    
                    // Рефакторинг: используем UIHelper для обновления UI
                    await Helpers.UIHelper.InvokeUIAsync(Dispatcher, () =>
                    {
                        ListView21.ItemsSource = _packetDataService.DestinationFileLines;
                    });
                }
                catch (Exception ex)
                {
                    // Рефакторинг: используем UIHelper для показа ошибки
                    Helpers.UIHelper.ShowError(Dispatcher, $"Ошибка при загрузке файла: {ex.Message}", "Ошибка");
                    Helpers.UIHelper.InvokeUI(Dispatcher, () =>
                    {
                        Label_Semafor2.Background = Brushes.Red;
                        BtnLoadOut.IsEnabled = true;
                    });
                    return;
                }
                //
                // инициализируем прогрессбары и списки
                //
                InitializeOut();

                isCleaningOut = CheckBoxCleaningOut.IsChecked == true;
                if (isCleaningOut)
                {
                    await Task.Run(() =>
                    {
                        CleanDestination();
                    });
                }
                else
                {
                    BtnCsLoadNameOut.IsEnabled = true;
                    BtnScLoadNameOut.IsEnabled = true;
                    //BtnLoadIn_Copy.IsEnabled = true;
                    BtnLoadOut.IsEnabled = true;
                }

                stopWatch.Stop();
                TextBox25.Text = stopWatch.Elapsed.ToString();
                isCompareCS = false;
                isCompareSC = false;
                Label_Semafor2.Background = Brushes.Yellow;
            }
            else
            {
                // Рефакторинг: используем UIHelper для показа сообщения
                Helpers.UIHelper.ShowInfo(Dispatcher, "Для работы программы необходимо выбрать .asm файл!", "Error");
                //BtnLoadIn_Copy.IsEnabled = true;
                BtnLoadOut.IsEnabled = true;
            }
        }

        private void btn_UpdStruct_Click(object sender, RoutedEventArgs e)
        {
            // инициализируем прогрессбары и списки
            //InitializeOut();

            BtnCsLoadNameOut.IsEnabled = false;
            BtnScLoadNameOut.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;
            string outText;
            DepthOut = 0;

            //ListNameDestinationSC = new List<string>();
            FindOpcodeOut = CheckBoxFindOpcodeOut.IsChecked == true;
            FindStructOut = CheckBoxFindStructOut.IsChecked == true;
            //_isInSc = false;
            _isOutSc = false;
            // = false;
            _isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;
            BtnUpdStruct.IsEnabled = false;
            if (ButtonSaveOut1.IsEnabled)
            {
                outText = TextBox21.Text;
                // Рефакторинг: используем Task.Run вместо Thread
                _ = Task.Run(() =>
                {
                    FindDestinationStructuresCS(outText);
                });
            }
            else
            {
                outText = TextBox22.Text;
                // Рефакторинг: используем Task.Run вместо Thread
                _ = Task.Run(() =>
                {
                    FindDestinationStructuresSC(outText);
                });
            }
        }

        private void btn_SC_Load_Name2_Click(object sender, RoutedEventArgs e)
        {
            //
            // инициализируем прогрессбары и списки
            //
            InitializeOut();

            BtnCsLoadNameOut.IsEnabled = false;
            BtnScLoadNameOut.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;
            var outText = TextBox22.Text;
            DepthOut = 0;

            ListNameDestinationSC.Clear();
            FindOpcodeOut = CheckBoxFindOpcodeOut.IsChecked == true;
            FindStructOut = CheckBoxFindStructOut.IsChecked == true;
            //_isInSc = false;
            _isOutSc = false;
            // = false;
            _isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            // Рефакторинг: используем async/await вместо Thread
            _ = Task.Run(async () =>
            {
                FindDestinationStructuresSC(outText);

                if (FindOpcodeOut)
                {
                    // Рефакторинг: используем новый сервис для поиска опкодов
                    await FindOpcodeDestinationSCAsync();
                }
            });
        }

        private void btn_CS_Load_Name2_Click(object sender, RoutedEventArgs e)
        {
            //
            // инициализируем прогрессбары и списки
            //
            InitializeOut();

            BtnCsLoadNameOut.IsEnabled = false;
            BtnScLoadNameOut.IsEnabled = false;
            //BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;
            var inText = TextBox21.Text;
            DepthIn = 0;

            ListNameDestinationCS.Clear();
            FindOpcodeOut = CheckBoxFindOpcodeOut.IsChecked == true;
            FindStructOut = CheckBoxFindStructOut.IsChecked == true;
            //_isInSc = false;
            _isOutSc = false;
            //_isInCs = false;
            _isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            // Рефакторинг: используем async/await вместо Thread
            _ = Task.Run(async () =>
            {
                FindDestinationStructuresCS(inText);

                if (FindOpcodeOut)
                {
                    // Рефакторинг: используем новый сервис для поиска опкодов
                    await FindOpcodeDestinationCSAsync();
                }
            });
        }

        private void CompareSourceStructuresCS(ref ObservableCollection<string> listNameSource, ref ObservableCollection<string> listNameDestination, ref ObservableCollection<string> listSubDestination, ref Dictionary<int, ObservableCollection<Struc>> dictSource, ref Dictionary<int, ObservableCollection<Struc>> dictDestination, ObservableCollection<string> listOpcodes)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            // подготовим список
            ListNameCompareCS.Clear();
            foreach (var t in listNameDestination)
            {
                ListNameCompareCS.Add(t);
            }

            InUseIn = new Dictionary<int, int>();
            InUseOut = new Dictionary<int, int>();
            IsRenameDestination = new Dictionary<int, bool>();
            var foundName = false;
            var badFound = 0;
            var repeat = true;
            var totalFound = 0;
            var totalNotfound = 0;
            // список структуры текущего сравнения имен
            // длины списков, могут отличаться, скорее всего список неизвестных имен длиннее, так как более новая версия
            var lenDestinationListName = listNameDestination.Count;
            var lenSourceListName = listNameSource.Count;
            // начнем с начала
            IdxD = 0;

            // Блокируем объект.
            //lock (lockObj)
            {
                if (IdxD >= dictDestination.Count)
                {
                    IdxD = dictDestination.Count - 1;
                }
                //
                // начали предварительную работу по поиску структур пакетов
                //
                // начнем с начала файла
                try
                {
                    do // проходим по списку имён, которые нужно найти, т.е. Destination
                    {
                        // возьмем следующую структуру, для которой нужно найти новое имя
                        var ddList = dictDestination[IdxD];
                        IdxS = 0;
                        do
                        {
                            badFound = 0;
                            // проверим, что имя не занято
                            if (!InUseIn.ContainsKey(IdxS))
                            {
                                // возьмем следующую структуру, с которой нужно свериться и решить, что имя нашли
                                var dsList = dictSource[IdxS];
                                if (ddList.Count == dsList.Count)
                                {
                                    if (ddList.Count == 0 && dsList.Count == 0)
                                    {
                                        foundName = true; // делаем пустые структуры похожими
                                    }
                                    else
                                    {
                                        // количество строк в структурах совпадает
                                        for (var i = 0; i < ddList.Count; i++)
                                        {
                                            // сверим на одинаковость
                                            if (ddList[i].Name == dsList[i].Name)
                                            {
                                                foundName = true;
                                            }
                                            else
                                            {
                                                badFound++;
                                                foundName = false;
                                            }
                                        }
                                    }

                                    if (foundName && badFound == 0)
                                    {
                                        if (InUseIn.ContainsKey(IdxS))
                                        {
                                            InUseIn[IdxS] = IdxD;
                                        }
                                        else
                                        {
                                            InUseIn.Add(IdxS, IdxD); // отметим, что найденное имя занято
                                        }

                                        if (InUseOut.ContainsKey(IdxD))
                                        {
                                            InUseOut[IdxD] = IdxS;
                                        }
                                        else
                                        {
                                            InUseOut.Add(IdxD, IdxS); // отметим, что найденное имя занято
                                        }

                                        // запишем новое имя на место неизвестного (проверяем на off_XXXX), которое нашли
                                        if (CheckBoxRenameOff.IsChecked == true)
                                        {
                                            if (listNameDestination[IdxD][0].ToString() == "o" ||
                                                listNameDestination[IdxD][1].ToString() == "f" ||
                                                listNameDestination[IdxD][2].ToString() == "f")
                                            {
                                                ListNameCompareCS[IdxD] = listNameSource[IdxS];
                                                totalFound++; // подсчитываем найденные имена
                                            }
                                            else
                                            {
                                                ListNameCompareCS[IdxD] = listNameDestination[IdxD];
                                                totalNotfound++; // подсчитываем ненайденные имена
                                            }
                                        }
                                        else
                                        {
                                            // переименовываем имена пакетов
                                            if (listNameSource[IdxS][0].ToString() == "o" ||
                                                listNameSource[IdxS][1].ToString() == "f" ||
                                                listNameSource[IdxS][2].ToString() == "f")
                                            {
                                                // не переименовываем если имя начинается off_
                                                ListNameCompareCS[IdxD] = listNameDestination[IdxD];
                                                totalNotfound++; // подсчитываем ненайденные имена
                                            }
                                            else
                                            {
                                                // не переименовываем, если имя содержит Unknown
                                                var offset = listNameSource[IdxS].IndexOf("unknown", StringComparison.OrdinalIgnoreCase);
                                                if (offset == -1)
                                                {
                                                    // переименовываем имена пакетов
                                                    ListNameCompareCS[IdxD] = listNameSource[IdxS];
                                                    totalFound++; // подсчитываем найденные имена
                                                }
                                                else
                                                {
                                                    // не переименовываем, если имя содержит Unknown
                                                    ListNameCompareCS[IdxD] = listNameDestination[IdxD];
                                                    totalNotfound++; // подсчитываем ненайденные имена
                                                }
                                            }
                                        }

                                        repeat = false; // больше не повторять поиск
                                    }
                                    else
                                    {
                                        IdxS++; // взять следующее
                                        repeat = true; // нужно будет повторять поиск
                                    }
                                }
                                else
                                {
                                    IdxS++; // взять следующее
                                    repeat = true; // нужно будет повторять поиск
                                }
                            }
                            else
                            {
                                IdxS++;
                            }
                        } while (IdxS < lenSourceListName && repeat);

                        IdxD++;
                        repeat = true; // нужно будет повторять поиск
                    } while (IdxD < lenDestinationListName);
                }
                catch (Exception)
                {
                    // Рефакторинг: используем UIHelper для показа ошибки
                    Helpers.UIHelper.ShowError(Dispatcher, "Opcodes not found!", "Error");

                }
            }
            //TextBox31.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox31.Text = totalFound.ToString(); }));
            //totalNotfound = dictDestination.Count - totalFound;
            stopWatch.Stop();
            var elapsedTime = stopWatch.Elapsed.ToString();
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => TextBox32.Text = totalNotfound.ToString(),
                () => TextBox33.Text = elapsedTime
            );
        }

        private void CompareSourceStructuresSC(ref ObservableCollection<string> listNameSource, ref ObservableCollection<string> listNameDestination, ref ObservableCollection<string> listSubDestination, ref Dictionary<int, ObservableCollection<Struc>> dictSource, ref Dictionary<int, ObservableCollection<Struc>> dictDestination, ObservableCollection<string> listOpcodes)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            // подготовим список
            ListNameCompareSC.Clear();
            foreach (var t in listNameDestination)
            {
                ListNameCompareSC.Add(t);
            }

            InUseIn = new Dictionary<int, int>();
            InUseOut = new Dictionary<int, int>();
            IsRenameDestination = new Dictionary<int, bool>();
            var foundName = false;
            var badFound = 0;
            var repeat = true;
            var totalFound = 0;
            var totalNotfound = 0;
            // длины списков, могут отличаться, скорее всего список неизвестных имен длиннее, так как более новая версия
            var lenDestinationListName = listNameDestination.Count;
            var lenSourceListName = listNameSource.Count;
            // начнем с начала
            IdxD = 0;

            // Блокируем объект.
            //lock (lockObj)
            {
                if (IdxD >= dictDestination.Count)
                {
                    IdxD = dictDestination.Count - 1;
                }

                //
                // начали предварительную работу по поиску структур пакетов
                //
                // начнем с начала файла
                do // проходим по списку имён, которые нужно найти, т.е. Destination
                {
                    // возьмем следующую структуру, для которой нужно найти новое имя
                    var ddList = dictDestination[IdxD];
                    IdxS = 0;
                    do
                    {
                        badFound = 0;
                        // проверим, что имя не занято
                        if (!InUseIn.ContainsKey(IdxS))
                        {
                            // возьмем следующую структуру, с которой нужно свериться и решить, что имя нашли
                            var dsList = dictSource[IdxS];
                            if (ddList.Count == dsList.Count)
                            {
                                if (ddList.Count == 0 && dsList.Count == 0)
                                {
                                    foundName = true; // делаем пустые структуры похожими
                                }
                                else
                                {
                                    // количество строк в структурах совпадает
                                    for (var i = 0; i < ddList.Count; i++)
                                    {
                                        // сверим на одинаковость
                                        if (ddList[i].Name == dsList[i].Name)
                                        {
                                            foundName = true;
                                        }
                                        else
                                        {
                                            badFound++;
                                            foundName = false;
                                        }
                                    }
                                }

                                if (foundName && badFound == 0)
                                {
                                    if (InUseIn.ContainsKey(IdxS))
                                    {
                                        InUseIn[IdxS] = IdxD;
                                    }
                                    else
                                    {
                                        InUseIn.Add(IdxS, IdxD); // отметим, что найденное имя занято
                                    }

                                    if (InUseOut.ContainsKey(IdxD))
                                    {
                                        InUseOut[IdxD] = IdxS;
                                    }
                                    else
                                    {
                                        InUseOut.Add(IdxD, IdxS); // отметим, что найденное имя занято
                                    }

                                    // запишем новое имя на место неизвестного (проверяем на off_XXXX), которое нашли
                                    if (CheckBoxRenameOff.IsChecked == true)
                                    {
                                        if (listNameDestination[IdxD][0].ToString() == "o" ||
                                            listNameDestination[IdxD][1].ToString() == "f" ||
                                            listNameDestination[IdxD][2].ToString() == "f")
                                        {
                                            ListNameCompareSC[IdxD] = listNameSource[IdxS];
                                            totalFound++; // подсчитываем найденные имена
                                        }
                                        else
                                        {
                                            ListNameCompareSC[IdxD] = listNameDestination[IdxD];
                                            totalNotfound++; // подсчитываем ненайденные имена
                                        }
                                    }
                                    else
                                    {
                                        // переименовываем имена пакетов
                                        if (listNameSource[IdxS][0].ToString() == "o" ||
                                            listNameSource[IdxS][1].ToString() == "f" ||
                                            listNameSource[IdxS][2].ToString() == "f")
                                        {
                                            // не переименовываем если имя начинается off_
                                            ListNameCompareSC[IdxD] = listNameDestination[IdxD];
                                            totalNotfound++; // подсчитываем ненайденные имена
                                        }
                                        else
                                        {
                                            // не переименовываем, если имя содержит Unknown
                                            var offset = listNameSource[IdxS].IndexOf("unknown", StringComparison.OrdinalIgnoreCase);
                                            if (offset == -1)
                                            {
                                                // переименовываем имена пакетов
                                                ListNameCompareSC[IdxD] = listNameSource[IdxS];
                                                totalFound++; // подсчитываем найденные имена
                                            }
                                            else
                                            {
                                                // не переименовываем, если имя содержит Unknown
                                                ListNameCompareSC[IdxD] = listNameDestination[IdxD];
                                                totalNotfound++; // подсчитываем ненайденные имена
                                            }
                                        }
                                    }

                                    repeat = false; // болше не повторять поиск
                                }
                                else
                                {
                                    IdxS++; // взять следующее
                                    repeat = true; // нужно будет повторять поиск
                                }
                            }
                            else
                            {
                                IdxS++; // взять следующее
                                repeat = true; // нужно будет повторять поиск
                            }
                        }
                        else
                        {
                            IdxS++;
                        }
                    } while (IdxS < lenSourceListName && repeat);

                    IdxD++;
                    repeat = true; // нужно будет повторять поиск
                } while (IdxD < lenDestinationListName);
            }
            stopWatch.Stop();
            var elapsedTime = stopWatch.Elapsed.ToString();
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => TextBox32.Text = totalNotfound.ToString(),
                () => TextBox33.Text = elapsedTime
            );
        }

        private void button2_Copy1_Click(object sender, RoutedEventArgs e)
        {
            // пробуем сравнивать структуры пакетов
            if (CheckBoxLock.IsChecked != true)
            {
                if (!isCompareCS)
                {
                    // результат работы метода в ListNameCompareCS
                    // ВАЖНО: сохраняем копии списков ДО вызова CompareSourceStructuresCS, чтобы они не потерялись
                    // Сохраняем копии всегда, независимо от isCompareCS, чтобы они были доступны при открытии CompareWindow
                    _savedListSubDestinationCS = ListSubDestinationCS != null && ListSubDestinationCS.Count > 0 ? new ObservableCollection<string>(ListSubDestinationCS) : null;
                    _savedListNameDestinationCS = ListNameDestinationCS != null && ListNameDestinationCS.Count > 0 ? new ObservableCollection<string>(ListNameDestinationCS) : null;
                    _savedListNameSourceCS = ListNameSourceCS != null && ListNameSourceCS.Count > 0 ? new ObservableCollection<string>(ListNameSourceCS) : null;
                    // Убрано сохранение структур, так как теперь передаем ссылки напрямую
                    
                    LogDebug($"button2_Copy1_Click: Сохранены копии списков перед CompareSourceStructuresCS. _savedListSubDestinationCS.Count={_savedListSubDestinationCS?.Count ?? 0}, _savedListNameDestinationCS.Count={_savedListNameDestinationCS?.Count ?? 0}, _savedListNameSourceCS.Count={_savedListNameSourceCS?.Count ?? 0}");
                    
                    // Рефакторинг: создаем локальные переменные для ref параметров, так как свойства нельзя передавать как ref
                    var listNameSourceCS = ListNameSourceCS;
                    var listNameDestinationCS = ListNameDestinationCS;
                    var listSubDestinationCS = ListSubDestinationCS;
                    var structureSourceCS = StructureSourceCS;
                    var structureDestinationCS = StructureDestinationCS;
                    CompareSourceStructuresCS(ref listNameSourceCS, ref listNameDestinationCS, ref listSubDestinationCS, ref structureSourceCS, ref structureDestinationCS, ListOpcodeDestinationCS);
                    
                    // Восстанавливаем списки, если они были очищены
                    if (_savedListSubDestinationCS != null && (listSubDestinationCS == null || listSubDestinationCS.Count == 0))
                    {
                        listSubDestinationCS = new ObservableCollection<string>(_savedListSubDestinationCS);
                        LogDebug($"button2_Copy1_Click: Восстановлен listSubDestinationCS из сохраненной копии. Count={listSubDestinationCS.Count}");
                    }
                    // Убрано восстановление списков, так как теперь передаем ссылки на ObservableCollection напрямую
                    // Убрано восстановление структур, так как теперь передаем ссылки напрямую
                    
                    ListNameSourceCS = listNameSourceCS;
                    ListNameDestinationCS = listNameDestinationCS;
                    ListSubDestinationCS = listSubDestinationCS;
                    StructureSourceCS = structureSourceCS;
                    StructureDestinationCS = structureDestinationCS;
                    // сравнение пакетов проведено
                    CheckBoxLock.IsChecked = false;
                }
                else
                {
                    ListNameCompareCS.Clear();
                    foreach (var item in ListNameCompare)
                    {
                        ListNameCompareCS.Add(item);
                    }
                    CheckBoxLock.IsChecked = true;
                }

                // удаляем оконечные опкоды в имени пакета
                if (CheckBoxRemoveOpcode.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareCS.Count; i++)
                    {
                        var offset = ListNameCompareCS[i].LastIndexOf("_", StringComparison.Ordinal);
                        if (offset > 3)
                        {
                            ListNameCompareCS[i] = ListNameCompareCS[i].Substring(0, offset);
                        }
                    }
                }

                // ToTitleCase
                if (CheckBoxToTitleCase.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareCS.Count; i++)
                    {
                        if (ListNameCompareCS[i][0].ToString() != "o" ||
                            ListNameCompareCS[i][1].ToString() != "f" ||
                            ListNameCompareCS[i][2].ToString() != "f")
                        {
                            // удаляем CS|SC только в начале имени
                            RemoveCS(i);
                            ListNameCompareCS[i] = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ListNameCompareCS[i].ToLower());
                            ListNameCompareCS[i] = ListNameCompareCS[i].Replace("_", "");
                            // добавим CS|SC в начале имени
                            AddCS(i);
                        }
                    }
                }

                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => CheckBoxToTitleCase.IsChecked = false);

                // Remove CS & Packet & @@6B@ in ver.0.5.1
                if (CheckBoxRemovePacket.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareCS.Count; i++)
                    {
                        if (ListNameCompareCS[i][0].ToString() != "o" ||
                            ListNameCompareCS[i][1].ToString() != "f" ||
                            ListNameCompareCS[i][2].ToString() != "f")
                        {
                            // удаляем CS|SC только в начале имени
                            RemoveCS(i);
                            ListNameCompareCS[i] = ListNameCompareCS[i].Replace("@@6B@", "");
                            ListNameCompareCS[i] = ListNameCompareCS[i].Replace("PACKET", "");
                            ListNameCompareCS[i] = ListNameCompareCS[i].Replace("Packet", "");
                            ListNameCompareCS[i] = ListNameCompareCS[i].Replace("packet", "");
                            ListNameCompareCS[i] = ListNameCompareCS[i].Replace("_", "");
                            // добавим CS|SC в начале имени
                            AddCS(i);
                        }
                    }
                }

                // Добавим 'Packet' в конец имени пакета
                if (CheckBoxAdd.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareCS.Count; i++)
                    {
                        if (ListNameCompareCS[i][0].ToString() != "o" ||
                            ListNameCompareCS[i][1].ToString() != "f" ||
                            ListNameCompareCS[i][2].ToString() != "f")
                        {
                            var offset = ListNameCompareCS[i].LastIndexOf("Packet", StringComparison.Ordinal);
                            if (offset <= 0)
                            {
                                ListNameCompareCS[i] += "Packet";
                            }
                            else
                            {
                                ListNameCompareCS[i] = ListNameCompareCS[i].Substring(0, offset);
                                ListNameCompareCS[i] += "Packet";
                            }

                            // добавим CS|SC в начале имени
                            AddCS(i);
                        }
                    }
                }

                ListView24.ItemsSource = ListOpcodeDestinationCS;
                ListView31.ItemsSource = ListNameCompareCS;
                ListView32.ItemsSource = ListNameCompareOutCS;
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => TextBox31.Text = ListView31.Items.Count.ToString());
            }

            if (CheckBoxCompareManual.IsChecked == true)
            {
                isCS = true;
                CompareWindow.isRemoveOpcode = isRemoveOpcode;
                CompareWindow.StructStringIn = StructStringIn;
                CompareWindow.StructStringOut = StructStringOut;
                
                // ВАЖНО: сохраняем копии списков перед открытием CompareWindow, если они еще не сохранены или пустые
                // Это нужно для случая, когда CompareWindow открывается повторно (isCompareCS уже true)
                if (_savedListSubDestinationCS == null || _savedListSubDestinationCS.Count == 0)
                {
                    _savedListSubDestinationCS = ListSubDestinationCS != null && ListSubDestinationCS.Count > 0 ? new ObservableCollection<string>(ListSubDestinationCS) : null;
                    LogDebug($"button2_Copy1_Click (CS Compare): Сохранены копии _savedListSubDestinationCS перед открытием CompareWindow. Count={_savedListSubDestinationCS?.Count ?? 0}");
                }
                if (_savedListNameDestinationCS == null || _savedListNameDestinationCS.Count == 0)
                {
                    _savedListNameDestinationCS = ListNameDestinationCS != null && ListNameDestinationCS.Count > 0 ? new ObservableCollection<string>(ListNameDestinationCS) : null;
                    LogDebug($"button2_Copy1_Click (CS Compare): Сохранены копии _savedListNameDestinationCS перед открытием CompareWindow. Count={_savedListNameDestinationCS?.Count ?? 0}");
                }
                if (_savedListNameSourceCS == null || _savedListNameSourceCS.Count == 0)
                {
                    _savedListNameSourceCS = ListNameSourceCS != null && ListNameSourceCS.Count > 0 ? new ObservableCollection<string>(ListNameSourceCS) : null;
                    LogDebug($"button2_Copy1_Click (CS Compare): Сохранены копии _savedListNameSourceCS перед открытием CompareWindow. Count={_savedListNameSourceCS?.Count ?? 0}");
                }
                // ВАЖНО: Теперь передаем ссылки на ObservableCollection напрямую, без копирования!
                // ObservableCollection автоматически обновляет UI при изменении данных
                // Для List (не ObservableCollection) создаем копии только для безопасности
                var listSubDestinationCS = _savedListSubDestinationCS != null ? new ObservableCollection<string>(_savedListSubDestinationCS) : (ListSubDestinationCS ?? new ObservableCollection<string>());
                
                // ВАЖНО: Передаем ссылки на Dictionary с ObservableCollection напрямую - без копирования!
                // Изменения в CompareWindow автоматически отобразятся в MainWindow
                
                // Логирование перед передачей в CompareWindow
                LogDebug($"button2_Copy1_Click (CS Compare): Перед открытием CompareWindow. ListNameSourceCS.Count={ListNameSourceCS.Count}, ListNameDestinationCS.Count={ListNameDestinationCS.Count}, ListNameCompareCS.Count={ListNameCompareCS.Count}, ListSubDestinationCS.Count={ListSubDestinationCS.Count}");
                
                // Если ListNameCompareCS пустой, инициализируем его из ListNameDestinationCS
                if (ListNameCompareCS.Count == 0 && ListNameDestinationCS.Count > 0)
                {
                    ListNameCompareCS.Clear();
                    foreach (var item in ListNameDestinationCS)
                    {
                        ListNameCompareCS.Add(item);
                    }
                    LogDebug($"button2_Copy1_Click (CS Compare): Инициализирован ListNameCompareCS из ListNameDestinationCS. Count={ListNameCompareCS.Count}");
                }
                
                var compareWindow = new CompareWindow(this);
                LogDebug($"button2_Copy1_Click (CS Compare): Вызов CompareSourceStructures. Передаем ссылки на ObservableCollection. ListNameSourceCS.Count={ListNameSourceCS.Count}, ListNameDestinationCS.Count={ListNameDestinationCS.Count}, ListNameCompareCS.Count={ListNameCompareCS.Count}, listSubDestinationCS.Count={listSubDestinationCS.Count}");
                
                // ВАЖНО: Передаем ссылки на ObservableCollection напрямую - без копирования!
                // Изменения в CompareWindow автоматически отобразятся в MainWindow
                compareWindow.CompareSourceStructures(
                    ListNameSourceCS,  // Передаем ссылку на ObservableCollection
                    ListNameDestinationCS,  // Передаем ссылку на ObservableCollection
                    ListNameCompareCS,  // Передаем ссылку на ObservableCollection
                    listSubDestinationCS,
                    StructureSourceCS,  // Передаем ссылку на Dictionary с ObservableCollection
                    StructureDestinationCS,  // Передаем ссылку на Dictionary с ObservableCollection
                    ListOpcodeDestinationCS);  // Передаем ссылку на ObservableCollection
                
                LogDebug($"button2_Copy1_Click (CS Compare): После CompareSourceStructures. ListNameCompareCS.Count={ListNameCompareCS.Count}");
                
                // Открываем окно модально, чтобы дождаться закрытия
                compareWindow.ShowDialog();
                
                // ВАЖНО: Изменения уже применены к ObservableCollection, UI обновится автоматически!
                // Нужно только обновить ListNameCompareOutCS, если были изменения
                if (compareWindow.isDestinationNameChanged || compareWindow.isResetOpcode)
                {
                    LogDebug($"button2_Copy1_Click (CS Compare): Обнаружены изменения. isDestinationNameChanged={compareWindow.isDestinationNameChanged}, isResetOpcode={compareWindow.isResetOpcode}");
                }
                isCompareCS = true;
                CheckBoxLock.IsChecked = true;
            }

            // ВАЖНО: формируем ListNameCompareOutCS из обновленного ListNameCompareCS
            // ObservableCollection автоматически обновит UI при изменении
            ListNameCompareOutCS.Clear();
            var idxD = 0;
            foreach (var t in ListNameCompareCS)
            {
                if (ListOpcodeDestinationCS != null && ListOpcodeDestinationCS.Count > idxD)
                {
                    ListNameCompareOutCS.Add(t + "_" + ListOpcodeDestinationCS[idxD]);
                }
                else
                {
                    ListNameCompareOutCS.Add(t + "_" + "0xfff");
                }
                idxD++;
            }

            // Рефакторинг: ObservableCollection автоматически обновляет UI, но нужно установить ItemsSource один раз
            // Если ItemsSource уже установлен на эту коллекцию, обновление произойдет автоматически
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => 
                {
                    if (ListView24.ItemsSource != ListOpcodeDestinationCS)
                        ListView24.ItemsSource = ListOpcodeDestinationCS;
                    if (ListView31.ItemsSource != ListNameCompareCS)
                        ListView31.ItemsSource = ListNameCompareCS; // ObservableCollection автоматически обновит UI
                    if (ListView32.ItemsSource != ListNameCompareOutCS)
                        ListView32.ItemsSource = ListNameCompareOutCS; // ObservableCollection автоматически обновит UI
                    TextBox31.Text = ListNameCompareCS.Count.ToString();
                }
            );
            
            LogDebug($"button2_Copy1_Click (CS Compare): Обновлены ListView31 и ListView32. ListNameCompareCS.Count={ListNameCompareCS.Count}, ListNameCompareOutCS.Count={ListNameCompareOutCS.Count}");

            if (CheckBoxRename.IsChecked == true)
            {
                // сохраняем новые имена в исходник
                // InListDestination = ListNameDestinationCS <- ListNameCompareOutCS
                // Рефакторинг: используем Task.Run вместо Thread
                _ = Task.Run(() =>
                {
                    RenamePackets(ListNameDestinationCS, ListNameCompareOutCS);
                    ListNameDestinationCS = ListNameCompareOutCS;
                });
            }
            Button2Copy2.IsEnabled = true;
            Button2Copy2_Copy.IsEnabled = true;
        }

        // Рефакторинг: убран static, так как метод использует нестатические свойства
        private void AddCS(int i)
        {
            // добавим CS|SC в начале имени
            if (ListNameCompareCS[i][0].ToString() != "C" ||
                ListNameCompareCS[i][1].ToString() != "S")
            {
                if (ListNameCompareCS[i][0].ToString() != "X" ||
                    ListNameCompareCS[i][1].ToString() != "2")
                {
                    ListNameCompareCS[i] = "CS" + ListNameCompareCS[i];
                }
            }
        }

        // Рефакторинг: убран static, так как метод использует нестатические свойства
        private void RemoveCS(int i)
        {
            // удаляем ??_7 только в начале имени
            var offset = ListNameCompareCS[i].IndexOf("??_7", StringComparison.OrdinalIgnoreCase);
            if (offset == 0)
            {
                ListNameCompareCS[i] = ListNameCompareCS[i].Substring(4, ListNameCompareCS[i].Length - 4);
            }
            // удаляем CS|SC только в начале имени
            offset = ListNameCompareCS[i].IndexOf("cs", StringComparison.OrdinalIgnoreCase);
            if (offset == 0)
            {
                ListNameCompareCS[i] = ListNameCompareCS[i].Substring(2, ListNameCompareCS[i].Length - 2);
            }
        }

        private void button_Copy1_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxLock.IsChecked != true)
            {
                // пробуем сравнивать структуры пакетов
                if (!isCompareSC)
                {
                    // результат работы метода в ListNameCompareSC
                    // ВАЖНО: сохраняем копии списков ДО вызова CompareSourceStructuresSC, чтобы они не потерялись
                    _savedListSubDestinationSC = ListSubDestinationSC != null && ListSubDestinationSC.Count > 0 ? new ObservableCollection<string>(ListSubDestinationSC) : null;
                    _savedListNameDestinationSC = ListNameDestinationSC != null && ListNameDestinationSC.Count > 0 ? new ObservableCollection<string>(ListNameDestinationSC) : null;
                    _savedListNameSourceSC = ListNameSourceSC != null && ListNameSourceSC.Count > 0 ? new ObservableCollection<string>(ListNameSourceSC) : null;
                    _savedStructureSourceSC = StructureSourceSC != null && StructureSourceSC.Count > 0 ? new Dictionary<int, List<Struc>>() : null;
                    if (_savedStructureSourceSC != null)
                    {
                        foreach (var kvp in StructureSourceSC)
                        {
                            _savedStructureSourceSC[kvp.Key] = new List<Struc>(kvp.Value);
                        }
                    }
                    _savedStructureDestinationSC = StructureDestinationSC != null && StructureDestinationSC.Count > 0 ? new Dictionary<int, List<Struc>>() : null;
                    if (_savedStructureDestinationSC != null)
                    {
                        foreach (var kvp in StructureDestinationSC)
                        {
                            _savedStructureDestinationSC[kvp.Key] = new List<Struc>(kvp.Value);
                        }
                    }
                    
                    LogDebug($"button_Copy1_Click: Сохранены копии списков перед CompareSourceStructuresSC. _savedListSubDestinationSC.Count={_savedListSubDestinationSC?.Count ?? 0}, _savedListNameDestinationSC.Count={_savedListNameDestinationSC?.Count ?? 0}, _savedListNameSourceSC.Count={_savedListNameSourceSC?.Count ?? 0}");
                    
                    // Рефакторинг: создаем локальные переменные для ref параметров, так как свойства нельзя передавать как ref
                    var listNameSourceSC = ListNameSourceSC;
                    var listNameDestinationSC = ListNameDestinationSC;
                    var listSubDestinationSC = ListSubDestinationSC;
                    var structureSourceSC = StructureSourceSC;
                    var structureDestinationSC = StructureDestinationSC;
                    CompareSourceStructuresSC(ref listNameSourceSC, ref listNameDestinationSC, ref listSubDestinationSC, ref structureSourceSC, ref structureDestinationSC, ListOpcodeDestinationSC);
                    
                    // Восстанавливаем списки, если они были очищены
                    if (_savedListSubDestinationSC != null && (listSubDestinationSC == null || listSubDestinationSC.Count == 0))
                    {
                        listSubDestinationSC = new ObservableCollection<string>(_savedListSubDestinationSC);
                        LogDebug($"button_Copy1_Click: Восстановлен listSubDestinationSC из сохраненной копии. Count={listSubDestinationSC.Count}");
                    }
                    // Убрано восстановление списков и структур, так как теперь передаем ссылки на ObservableCollection напрямую
                    
                    ListNameSourceSC = listNameSourceSC;
                    ListNameDestinationSC = listNameDestinationSC;
                    ListSubDestinationSC = listSubDestinationSC;
                    StructureSourceSC = structureSourceSC;
                    StructureDestinationSC = structureDestinationSC;
                    // сравнение пакетов проведено
                    CheckBoxLock.IsChecked = false;
                }
                else
                {
                    ListNameCompareSC.Clear();
                    foreach (var item in ListNameCompare)
                    {
                        ListNameCompareSC.Add(item);
                    }
                    CheckBoxLock.IsChecked = true;
                }

                // удаляем оконечные опкоды в имени пакета
                if (CheckBoxRemoveOpcode.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareSC.Count; i++)
                    {
                        var offset = ListNameCompareSC[i].LastIndexOf("_", StringComparison.Ordinal);
                        if (offset > 3)
                        {
                            ListNameCompareSC[i] = ListNameCompareSC[i].Substring(0, offset);
                        }
                    }
                }

                // ToTitleCase
                if (CheckBoxToTitleCase.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareSC.Count; i++)
                    {
                        if (ListNameCompareSC[i][0].ToString() != "o" ||
                            ListNameCompareSC[i][1].ToString() != "f" ||
                            ListNameCompareSC[i][2].ToString() != "f")
                        {
                            // удаляем CS|SC только в начале имени
                            RemoveSC(i);
                            ListNameCompareSC[i] = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ListNameCompareSC[i].ToLower());
                            ListNameCompareSC[i] = ListNameCompareSC[i].Replace("_", "");
                            // добавим CS|SC в начале имени
                            AddSC(i);
                        }
                    }
                }

                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => CheckBoxToTitleCase.IsChecked = false);

                // Remove SC & Packet & @@6B@ in ver.0.5.1
                if (CheckBoxRemovePacket.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareSC.Count; i++)
                    {
                        if (ListNameCompareSC[i][0].ToString() != "o" ||
                            ListNameCompareSC[i][1].ToString() != "f" ||
                            ListNameCompareSC[i][2].ToString() != "f")
                        {
                            // удаляем CS|SC только в начале имени
                            RemoveSC(i);
                            ListNameCompareSC[i] = ListNameCompareSC[i].Replace("@@6B@", "");
                            ListNameCompareSC[i] = ListNameCompareSC[i].Replace("PACKET", "");
                            ListNameCompareSC[i] = ListNameCompareSC[i].Replace("Packet", "");
                            ListNameCompareSC[i] = ListNameCompareSC[i].Replace("packet", "");
                            ListNameCompareSC[i] = ListNameCompareSC[i].Replace("_", "");
                            // добавим CS|SC в начале имени
                            AddSC(i);
                        }
                    }
                }

                // Добавим 'Packet' в конец имени пакета
                if (CheckBoxAdd.IsChecked == true)
                {
                    for (var i = 0; i < ListNameCompareSC.Count; i++)
                    {
                        if (ListNameCompareSC[i][0].ToString() != "o" ||
                            ListNameCompareSC[i][1].ToString() != "f" ||
                            ListNameCompareSC[i][2].ToString() != "f")
                        {
                            var offset = ListNameCompareSC[i].LastIndexOf("Packet", StringComparison.Ordinal);
                            if (offset <= 0)
                            {
                                ListNameCompareSC[i] += "Packet";
                            }
                            else
                            {
                                ListNameCompareSC[i] = ListNameCompareSC[i].Substring(0, offset);
                                ListNameCompareSC[i] += "Packet";
                            }

                            // добавим CS|SC в начале имени
                            AddSC(i);
                        }
                    }
                }

                ListView24.ItemsSource = ListOpcodeDestinationSC;
                ListView31.ItemsSource = ListNameCompareSC;
                ListView32.ItemsSource = ListNameCompareOutSC;
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => TextBox31.Text = ListView31.Items.Count.ToString());
            }

            if (CheckBoxCompareManual.IsChecked == true)
            {
                isCS = false;
                CompareWindow.isRemoveOpcode = isRemoveOpcode;
                CompareWindow.StructStringIn = StructStringIn;
                CompareWindow.StructStringOut = StructStringOut;
                
                // ВАЖНО: сохраняем копии списков перед открытием CompareWindow, если они еще не сохранены или пустые
                // Это нужно для случая, когда CompareWindow открывается повторно (isCompareSC уже true)
                if (_savedListSubDestinationSC == null || _savedListSubDestinationSC.Count == 0)
                {
                    _savedListSubDestinationSC = ListSubDestinationSC != null && ListSubDestinationSC.Count > 0 ? new ObservableCollection<string>(ListSubDestinationSC) : null;
                    LogDebug($"button2_Copy2_Click (SC Compare): Сохранены копии _savedListSubDestinationSC перед открытием CompareWindow. Count={_savedListSubDestinationSC?.Count ?? 0}");
                }
                if (_savedListNameDestinationSC == null || _savedListNameDestinationSC.Count == 0)
                {
                    _savedListNameDestinationSC = ListNameDestinationSC != null && ListNameDestinationSC.Count > 0 ? new ObservableCollection<string>(ListNameDestinationSC) : null;
                    LogDebug($"button2_Copy2_Click (SC Compare): Сохранены копии _savedListNameDestinationSC перед открытием CompareWindow. Count={_savedListNameDestinationSC?.Count ?? 0}");
                }
                if (_savedListNameSourceSC == null || _savedListNameSourceSC.Count == 0)
                {
                    _savedListNameSourceSC = ListNameSourceSC != null && ListNameSourceSC.Count > 0 ? new ObservableCollection<string>(ListNameSourceSC) : null;
                    LogDebug($"button2_Copy2_Click (SC Compare): Сохранены копии _savedListNameSourceSC перед открытием CompareWindow. Count={_savedListNameSourceSC?.Count ?? 0}");
                }
                // ВАЖНО: Теперь передаем ссылки на ObservableCollection напрямую, без копирования!
                // ObservableCollection автоматически обновляет UI при изменении данных
                // Для List (не ObservableCollection) создаем копии только для безопасности
                var listSubDestinationSC = _savedListSubDestinationSC != null ? new ObservableCollection<string>(_savedListSubDestinationSC) : (ListSubDestinationSC ?? new ObservableCollection<string>());
                
                // Логирование перед передачей в CompareWindow
                LogDebug($"button2_Copy2_Click (SC Compare): Перед открытием CompareWindow. ListNameSourceSC.Count={ListNameSourceSC.Count}, ListNameDestinationSC.Count={ListNameDestinationSC.Count}, ListNameCompareSC.Count={ListNameCompareSC.Count}, ListSubDestinationSC.Count={ListSubDestinationSC.Count}");
                
                // Если ListNameCompareSC пустой, инициализируем его из ListNameDestinationSC
                if (ListNameCompareSC.Count == 0 && ListNameDestinationSC.Count > 0)
                {
                    ListNameCompareSC.Clear();
                    foreach (var item in ListNameDestinationSC)
                    {
                        ListNameCompareSC.Add(item);
                    }
                    LogDebug($"button2_Copy2_Click (SC Compare): Инициализирован ListNameCompareSC из ListNameDestinationSC. Count={ListNameCompareSC.Count}");
                }
                
                var compareWindow = new CompareWindow(this);
                LogDebug($"button2_Copy2_Click (SC Compare): Вызов CompareSourceStructures. Передаем ссылки на ObservableCollection. ListNameSourceSC.Count={ListNameSourceSC.Count}, ListNameDestinationSC.Count={ListNameDestinationSC.Count}, ListNameCompareSC.Count={ListNameCompareSC.Count}, listSubDestinationSC.Count={listSubDestinationSC.Count}");
                
                // ВАЖНО: Передаем ссылки на ObservableCollection напрямую - без копирования!
                // Изменения в CompareWindow автоматически отобразятся в MainWindow
                compareWindow.CompareSourceStructures(
                    ListNameSourceSC,  // Передаем ссылку на ObservableCollection
                    ListNameDestinationSC,  // Передаем ссылку на ObservableCollection
                    ListNameCompareSC,  // Передаем ссылку на ObservableCollection
                    listSubDestinationSC,
                    StructureSourceSC,  // Передаем ссылку на Dictionary с ObservableCollection
                    StructureDestinationSC,  // Передаем ссылку на Dictionary с ObservableCollection
                    ListOpcodeDestinationSC);  // Передаем ссылку на ObservableCollection
                
                LogDebug($"button2_Copy2_Click (SC Compare): После CompareSourceStructures. ListNameCompareSC.Count={ListNameCompareSC.Count}");
                
                // Открываем окно модально, чтобы дождаться закрытия
                compareWindow.ShowDialog();
                
                // ВАЖНО: Изменения уже применены к ObservableCollection, UI обновится автоматически!
                // Нужно только залогировать изменения
                if (compareWindow.isSourceNameChanged || compareWindow.isDestinationNameChanged || compareWindow.isResetOpcode)
                {
                    LogDebug($"button2_Copy2_Click (SC Compare): Обнаружены изменения. isSourceNameChanged={compareWindow.isSourceNameChanged}, isDestinationNameChanged={compareWindow.isDestinationNameChanged}, isResetOpcode={compareWindow.isResetOpcode}");
                }
                isCompareSC = true;
                CheckBoxLock.IsChecked = true;
            }

            // ВАЖНО: формируем ListNameCompareOutSC из обновленного ListNameCompareSC
            // ObservableCollection автоматически обновит UI при изменении
            ListNameCompareOutSC.Clear();
            var idxD = 0;
            foreach (var t in ListNameCompareSC)
            {
                if (ListOpcodeDestinationSC != null && ListOpcodeDestinationSC.Count > idxD)
                {
                    ListNameCompareOutSC.Add(t + "_" + ListOpcodeDestinationSC[idxD]);
                }
                else
                {
                    ListNameCompareOutSC.Add(t + "_" + "0xfff");
                }
                idxD++;
            }

            // Рефакторинг: ObservableCollection автоматически обновляет UI, но нужно установить ItemsSource один раз
            // Если ItemsSource уже установлен на эту коллекцию, обновление произойдет автоматически
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => 
                {
                    if (ListView24.ItemsSource != ListOpcodeDestinationSC)
                        ListView24.ItemsSource = ListOpcodeDestinationSC;
                    if (ListView31.ItemsSource != ListNameCompareSC)
                        ListView31.ItemsSource = ListNameCompareSC; // ObservableCollection автоматически обновит UI
                    if (ListView32.ItemsSource != ListNameCompareOutSC)
                        ListView32.ItemsSource = ListNameCompareOutSC; // ObservableCollection автоматически обновит UI
                    TextBox31.Text = ListNameCompareSC.Count.ToString();
                }
            );
            
            LogDebug($"button2_Copy2_Click (SC Compare): Обновлены ListView31 и ListView32. ListNameCompareSC.Count={ListNameCompareSC.Count}, ListNameCompareOutSC.Count={ListNameCompareOutSC.Count}");

            if (CheckBoxRename.IsChecked == true)
            {
                // сохраняем новые имена в исходник
                // Рефакторинг: используем Task.Run вместо Thread
                _ = Task.Run(() =>
                {
                    RenamePackets(ListNameDestinationSC, ListNameCompareOutSC);
                    ListNameDestinationSC = ListNameCompareOutSC;
                });
            }
            ButtonCopy2.IsEnabled = true;
            ButtonCopy2_Copy.IsEnabled = true;
        }

        // Рефакторинг: убран static, так как метод использует нестатические свойства
        private void AddSC(int i)
        {
            // добавим CS|SC в начале имени
            if (ListNameCompareSC[i][0].ToString() != "S" ||
                ListNameCompareSC[i][1].ToString() != "C")
            {
                if (ListNameCompareSC[i][0].ToString() != "X" ||
                    ListNameCompareSC[i][1].ToString() != "2")
                {
                    ListNameCompareSC[i] = "SC" + ListNameCompareSC[i];
                }
            }
        }

        // Рефакторинг: убран static, так как метод использует нестатические свойства
        private void RemoveSC(int i)
        {
            // удаляем ??_7 только в начале имени
            var offset = ListNameCompareSC[i].IndexOf("??_7", StringComparison.OrdinalIgnoreCase);
            if (offset == 0)
            {
                ListNameCompareSC[i] = ListNameCompareSC[i].Substring(4, ListNameCompareSC[i].Length - 4);
            }
            // удаляем CS|SC только в начале имени
            offset = ListNameCompareSC[i].IndexOf("sc", StringComparison.OrdinalIgnoreCase);
            if (offset == 0)
            {
                ListNameCompareSC[i] = ListNameCompareSC[i].Substring(2, ListNameCompareSC[i].Length - 2);
            }
        }

        private void RenamePackets(ObservableCollection<string> listName, ObservableCollection<string> listNameCompare)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => Label_Semafor2.Background = Brushes.Yellow,
                () => ProgressBar21.Value = 0,
                () => ProgressBar21.Maximum = listName.Count
            );
            //
            // начали работу по поиску имен пакетов
            //
            for (var i = 0; i < listName.Count; i++)
            {
                // ищем имя пакета, с начала файла до конца файла
                var found = false;
                var regexSub = new Regex(@"" + listName[i], RegexOptions.Compiled);
                for (var index = 0; index < InListDestination.Count; index++)
                {
                    var matchesSub = regexSub.Matches(InListDestination[index]);
                    if (matchesSub.Count <= 0)
                    {
                        continue;
                    }

                    // нашли старое имя пакета, заменяем на новое
                    InListDestination[index] = InListDestination[index].Replace(listName[i], listNameCompare[i]); // переименуем имя пакета
                }
                // Рефакторинг: используем UIHelper для обновления прогрессбара
                Helpers.UIHelper.InvokeUI(Dispatcher, () => ProgressBar21.Value = i);
            }
            stopWatch.Stop();
            var elapsedTime = stopWatch.Elapsed.ToString();
            listName = listNameCompare;
            
            // Рефакторинг: используем UIHelper для группировки UI обновлений
            Helpers.UIHelper.InvokeUIBatch(Dispatcher,
                () => TextBox25.Text = elapsedTime,
                () => ProgressBar22.Value = listName.Count,
                () => Label_Semafor2.Background = Brushes.GreenYellow,
                () => ListView22.ItemsSource = listName
            );
        }

        private string FilePathOut3 = "";

        private void button2_Copy2_Click(object sender, RoutedEventArgs e)
        {
            var tmp = new List<string>();
            for (var i = 0; i < ListNameCompareCS.Count; i++)
            {
                // Проверяем границы для ListSubDestinationCS и ListOpcodeDestinationCS
                var packetBodyReader = i < ListSubDestinationCS.Count ? ListSubDestinationCS[i] : "";
                var opcode = "";
                
                if (ListOpcodeDestinationCS.Count > 0 && i < ListOpcodeDestinationCS.Count)
                {
                    opcode = ListOpcodeDestinationCS[i];
                    var lst = "Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: " + opcode;
                    tmp.Add(lst);
                }
                else
                {
                    var lst = "Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: 0xfff";
                    tmp.Add(lst);
                }
            }

            if (SaveFileDialog3())
            {
                File.WriteAllLines(FilePathOut3, tmp);
            }
        }

        private string FilePathOut4 = "";

        private void button_Copy2_Click(object sender, RoutedEventArgs e)
        {
            var tmp = new List<string>();
            for (var i = 0; i < ListNameCompareSC.Count; i++)
            {
                // Проверяем границы для ListSubDestinationSC и ListOpcodeDestinationSC
                var packetBodyReader = i < ListSubDestinationSC.Count ? ListSubDestinationSC[i] : "";
                var opcode = "";
                
                if (ListOpcodeDestinationSC.Count > 0 && i < ListOpcodeDestinationSC.Count)
                {
                    opcode = ListOpcodeDestinationSC[i];
                    var lst = "Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: " + opcode;
                    tmp.Add(lst);
                }
                else
                {
                    var lst = "Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: 0xfff";
                    tmp.Add(lst);
                }
            }

            if (SaveFileDialog4())
            {
                File.WriteAllLines(FilePathOut4, tmp);
            }
        }

        private string FilePathOut7 = "";

        private void button_SaveStructCS_Click(object sender, RoutedEventArgs e)
        {
            LogDebug($"button_SaveStructCS_Click: Начало сохранения. ListNameCompareCS.Count={ListNameCompareCS?.Count ?? 0}, StructureSourceCS.Count={StructureSourceCS?.Count ?? 0}, StructureDestinationCS.Count={StructureDestinationCS?.Count ?? 0}, InUseOut.Count={InUseOut?.Count ?? 0}");
            var tmp = new List<string>();
            var ss = 0;
            var dd = 0;
            var lst = "";
            ObservableCollection<Struc> src;
            ObservableCollection<Struc> dst;
            for (var i = 0; i < ListNameCompareCS.Count; i++)
            {
                ss = 0;
                dd = 0;
                // Проверяем границы для ListSubDestinationCS и ListOpcodeDestinationCS
                var packetBodyReader = i < ListSubDestinationCS.Count ? ListSubDestinationCS[i] : "";
                var opcode = "";
                
                if (ListOpcodeDestinationCS.Count > 0 && i < ListOpcodeDestinationCS.Count)
                {
                    opcode = ListOpcodeDestinationCS[i];
                    lst = i + 1 + ": Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: " + opcode;
                }
                else
                {
                    lst = i + 1 + ": Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: 0xfff";
                }

                tmp.Add(lst);

                // Получаем структуру Source через маппинг InUseOut
                // InUseOut[i] содержит индекс в Source списке, соответствующий индексу i в Destination списке
                if (InUseOut != null && InUseOut.TryGetValue(i, out var value) && StructureSourceCS != null && StructureSourceCS.ContainsKey(value))
                {
                    src = StructureSourceCS[value];
                    LogDebug($"button_SaveStructCS_Click: Пакет [{i}] '{ListNameCompareCS[i]}': Найдена Source структура через InUseOut[{i}]={value}, элементов: {src.Count}");
                }
                else
                {
                    // Если маппинг не найден, пытаемся получить структуру напрямую по индексу i
                    // (на случай, если индексы совпадают)
                    if (InUseOut != null && InUseOut.TryGetValue(i, out var value2))
                    {
                        LogDebug($"button_SaveStructCS_Click: Пакет [{i}] '{ListNameCompareCS[i]}': InUseOut[{i}]={value2}, но StructureSourceCS не содержит ключ {value2}");
                    }
                    else
                    {
                        LogDebug($"button_SaveStructCS_Click: Пакет [{i}] '{ListNameCompareCS[i]}': InUseOut не содержит ключ {i}");
                    }
                    src = StructureSourceCS != null && StructureSourceCS.ContainsKey(i) 
                        ? StructureSourceCS[i] 
                        : new ObservableCollection<Struc>();
                    if (src.Count > 0)
                    {
                        LogDebug($"button_SaveStructCS_Click: Пакет [{i}] '{ListNameCompareCS[i]}': Найдена Source структура напрямую по индексу {i}, элементов: {src.Count}");
                    }
                }
                
                // Получаем структуру Destination по индексу i (это словарь, а не список)
                dst = StructureDestinationCS != null && StructureDestinationCS.TryGetValue(i, out var dstValue) 
                    ? dstValue 
                    : new ObservableCollection<Struc>();
                if (dst.Count > 0)
                {
                    LogDebug($"button_SaveStructCS_Click: Пакет [{i}] '{ListNameCompareCS[i]}': Найдена Destination структура по индексу {i}, элементов: {dst.Count}");
                }
                else if (i < 5) // Логируем только первые 5 для отладки
                {
                    LogWarn($"button_SaveStructCS_Click: Пакет [{i}] '{ListNameCompareCS[i]}': Destination структура пустая (StructureDestinationCS не содержит ключ {i})");
                }

                var count = Math.Max(src.Count, dst.Count);
                // проходим по самому длинному списку
                do
                {
                    var str1 = ss < src.Count ? src[ss].Name : "";
                    var str2 = dd < dst.Count ? dst[dd].Name : "";

                    // Исправлено: было ss дважды, должно быть ss и dd
                    lst = ss + ": " + str1 + "\t\t" + dd + ": " + str2;
                    tmp.Add(lst);

                    ss++;
                    dd++;
                } while (ss < count);

                tmp.Add("--------------------------------------------------------------------------------------------------------------------------------------------");
            }
            LogDebug($"button_SaveStructCS_Click: Завершено. Подготовлено {tmp.Count} строк для сохранения");
            if (SaveFileDialog7())
            {
                File.WriteAllLines(FilePathOut7, tmp);
                LogDebug($"button_SaveStructCS_Click: Файл сохранен: {FilePathOut7}");
            }
        }

        private string FilePathOut8 = "";

        private void button_SaveStructSC_Click(object sender, RoutedEventArgs e)
        {
            var tmp = new List<string>();
            var ss = 0;
            var dd = 0;
            var key = 0;
            var lst = "";
            ObservableCollection<Struc> src;
            ObservableCollection<Struc> dst;
            for (var i = 0; i < ListNameCompareSC.Count; i++)
            {
                ss = 0;
                dd = 0;
                // Проверяем границы для ListSubDestinationSC и ListOpcodeDestinationSC
                var packetBodyReader = i < ListSubDestinationSC.Count ? ListSubDestinationSC[i] : "";
                var opcode = "";
                
                if (ListOpcodeDestinationSC.Count > 0 && i < ListOpcodeDestinationSC.Count)
                {
                    opcode = ListOpcodeDestinationSC[i];
                    lst = i + 1 + ": Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: " + opcode;
                }
                else
                {
                    lst = i + 1 + ": Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + packetBodyReader + ", Opcode: 0xfff";
                }

                tmp.Add(lst);

                // Получаем структуру Source через маппинг InUseOut
                // InUseOut[i] содержит индекс в Source списке, соответствующий индексу i в Destination списке
                if (InUseOut != null && InUseOut.TryGetValue(i, out var value) && StructureSourceSC != null && StructureSourceSC.ContainsKey(value))
                {
                    src = StructureSourceSC[value];
                }
                else
                {
                    // Если маппинг не найден, пытаемся получить структуру напрямую по индексу i
                    // (на случай, если индексы совпадают)
                    src = StructureSourceSC != null && StructureSourceSC.ContainsKey(i) 
                        ? StructureSourceSC[i] 
                        : new ObservableCollection<Struc>();
                }
                
                // Получаем структуру Destination по индексу i (это словарь, а не список)
                dst = StructureDestinationSC != null && StructureDestinationSC.TryGetValue(i, out var dstValue) 
                    ? dstValue 
                    : new ObservableCollection<Struc>();

                var count = Math.Max(src.Count, dst.Count);
                // проходим по самому длинному списку
                do
                {
                    var str1 = ss < src.Count ? src[ss].Name : "";
                    var str2 = dd < dst.Count ? dst[dd].Name : "";

                    // Исправлено: было ss дважды, должно быть ss и dd
                    lst = ss + ": " + str1 + "\t\t" + dd + ": " + str2;
                    tmp.Add(lst);

                    ss++;
                    dd++;
                } while (ss < count);

                tmp.Add(
                    "--------------------------------------------------------------------------------------------------------------------------------------------");
            }
            if (SaveFileDialog8())
            {
                File.WriteAllLines(FilePathOut8, tmp);
            }
        }

        private string FilePathOut51 = "";

        private void button_Out_Save_CSOpcode_Click(object sender, RoutedEventArgs e)
        {
            var lst = new List<string>();
            for (var i = 0; i < ListNameDestinationCS.Count; i++)
            {
                if (ListOpcodeDestinationCS.Count > 0)
                {
                    lst.Add("Packet name: " + ListNameDestinationCS[i] + ", PacketBodyReader: " + ListSubDestinationCS[i] + ", Opcode: " + ListOpcodeDestinationCS[i]);
                }
                else
                {
                    lst.Add("Packet name: " + ListNameDestinationCS[i] + ", PacketBodyReader: " + ListSubDestinationCS[i] + ", Opcode: " + "0xfff");
                }
            }

            if (SaveFileDialog51())
            {
                File.WriteAllLines(FilePathOut51, lst);
            }
        }

        private string FilePathOut61 = "";

        private void button_Out_Save_SCOpcode_Click(object sender, RoutedEventArgs e)
        {
            var FilePath = TextBoxPathOut.Text;
            var lst = new List<string>();
            for (var i = 0; i < ListNameDestinationSC.Count; i++)
            {
                if (ListOpcodeDestinationSC.Count > 0)
                {
                    lst.Add("Packet name: " + ListNameDestinationSC[i] + ", PacketBodyReader: " + ListSubDestinationSC[i] + ", Opcode: " + ListOpcodeDestinationSC[i]);
                }
                else
                {
                    lst.Add("Packet name: " + ListNameDestinationSC[i] + ", PacketBodyReader: " + ListSubDestinationSC[i] + ", Opcode: " + "0xfff");
                }
            }

            if (SaveFileDialog61())
            {
                File.WriteAllLines(FilePathOut61, lst);
            }
        }

        private string FilePathIn5 = "";

        private void button_In_Save_CSOpcode_Click(object sender, RoutedEventArgs e)
        {
            var FilePath = TextBoxPathIn.Text;
            var lst = new List<string>();
            for (var i = 0; i < ListNameSourceCS.Count; i++)
            {
                if (ListOpcodeSourceCS.Count > 0)
                {
                    lst.Add("Packet name: " + ListNameSourceCS[i] + ", PacketBodyReader: " + ListSubSourceCS[i] + ", Opcode: " + ListOpcodeSourceCS[i]);
                }
                else
                {
                    lst.Add("Packet name: " + ListNameSourceCS[i] + ", PacketBodyReader: " + ListSubSourceCS[i] + ", Opcode: " + "0xfff");
                }
            }

            if (SaveFileDialog5())
            {
                File.WriteAllLines(FilePathIn5, lst);
            }
        }

        private string FilePathIn6 = "";

        private void button_In_Save_SCOpcode_Click(object sender, RoutedEventArgs e)
        {
            var lst = new List<string>();
            for (var i = 0; i < ListNameSourceSC.Count; i++)
            {
                if (ListOpcodeSourceSC.Count > 0)
                {
                    lst.Add("Packet name: " + ListNameSourceSC[i] + ", PacketBodyReader: " + ListSubSourceSC[i] + ", Opcode: " + ListOpcodeSourceSC[i]);
                }
                else
                {
                    lst.Add("Packet name: " + ListNameSourceSC[i] + ", PacketBodyReader: " + ListSubSourceSC[i] + ", Opcode: " + "0xfff");
                }
            }

            if (SaveFileDialog6())
            {
                File.WriteAllLines(FilePathIn6, lst);
            }
        }

        private string FilePathIn1 = "";

        public bool OpenFileDialog1()
        {
            var openFileDialog1 = new OpenFileDialog
            {
                Filter = "Asm File|*.asm",
                FileName = "New Text Doucment",
                Title = "Open As Text File"
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                FilePathIn1 = openFileDialog1.FileName;
                return true;
            }

            return false;
        }

        private string FilePathIn2 = "";

        public bool OpenFileDialog2()
        {
            var openFileDialog2 = new OpenFileDialog
            {
                Filter = "Asm File|*.asm",
                FileName = "New Text Doucment",
                Title = "Open As Text File"
            };

            if (openFileDialog2.ShowDialog() == true)
            {
                FilePathIn2 = openFileDialog2.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog3()
        {
            var saveFileDialog3 = new SaveFileDialog
            {
                Filter = "CSOffsets File|*.cs",
                FileName = "CSOffsets.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog3.ShowDialog() == true)
            {
                FilePathOut3 = saveFileDialog3.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog4()
        {
            var saveFileDialog4 = new SaveFileDialog
            {
                Filter = "SCOffsets File|*.cs",
                FileName = "SCOffsets.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog4.ShowDialog() == true)
            {
                FilePathOut4 = saveFileDialog4.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog5()
        {
            var saveFileDialog5 = new SaveFileDialog
            {
                Filter = "CSOpcodesIn File|*.cs",
                FileName = "CSOpcodesIn.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog5.ShowDialog() == true)
            {
                FilePathIn5 = saveFileDialog5.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog6()
        {
            var saveFileDialog6 = new SaveFileDialog
            {
                Filter = "SCOpcodesIn File|*.cs",
                FileName = "SCOpcodesIn.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog6.ShowDialog() == true)
            {
                FilePathIn6 = saveFileDialog6.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog51()
        {
            var saveFileDialog51 = new SaveFileDialog
            {
                Filter = "CSOpcodesOut File|*.cs",
                FileName = "CSOpcodesOut.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog51.ShowDialog() == true)
            {
                FilePathOut51 = saveFileDialog51.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog61()
        {
            var saveFileDialog61 = new SaveFileDialog
            {
                Filter = "SCOpcodesOut File|*.cs",
                FileName = "SCOpcodesOut.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog61.ShowDialog() == true)
            {
                FilePathOut61 = saveFileDialog61.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog7()
        {
            var saveFileDialog7 = new SaveFileDialog
            {
                Filter = "CSStructs File|*.cs",
                FileName = "CSStructs.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog7.ShowDialog() == true)
            {
                FilePathOut7 = saveFileDialog7.FileName;
                return true;
            }

            return false;
        }

        public bool SaveFileDialog8()
        {
            var saveFileDialog8 = new SaveFileDialog
            {
                Filter = "SCStructs File|*.cs",
                FileName = "SCStructs.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog8.ShowDialog() == true)
            {
                FilePathOut8 = saveFileDialog8.FileName;
                return true;
            }

            return false;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        /*
        * Which works out about 30% faster than PZahras (not that you'd notice with small amounts of data).
        * The BitConverter method itself is pretty quick, it's just having to do the replace which slows it down, so if you can live with the dashes then it's perfectly good.
        */
        public static string ByteArrayToString(byte[] data)
        {
            var lookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            int i = 0, p = 0, l = data.Length;
            var c = new char[l * 2 + 2];
            byte d;
            //int p = 2; c[0] = '0'; c[1] = 'x'; //если хотим 0x
            while (i < l)
            {
                d = data[i++];
                c[p++] = lookup[d / 0x10];
                c[p++] = lookup[d % 0x10];
            }

            return new string(c, 0, c.Length);
        }

        private string DirPath = ".\\data";
        private string DirPathCS = ".\\data\\cs";
        private string DirPathSC = ".\\data\\sc";

        private void btn_SaveSnapshot_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor1.Background = Brushes.Red;
            Label_Semafor2.Background = Brushes.Red;

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            //lock (lockObj)
            {
                using (var FBD = new FolderBrowserDialog())
                {
                    // сохраним путь к рабочей папке
                    var path = Environment.CurrentDirectory + "\\WorkDir.cfg";
                    var fileInf = new FileInfo(path);
                    if (fileInf.Exists)
                    {
                        FBD.SelectedPath = File.ReadAllLines(Environment.CurrentDirectory + "\\WorkDir.cfg")[0];
                    }
                    else
                    {
                        FBD.SelectedPath = Environment.CurrentDirectory;
                    }

                    if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        //MessageBox.Show(FBD.SelectedPath);
                        DirPath = FBD.SelectedPath;
                        DirPathCS = DirPath + "\\data\\cs";
                        DirPathSC = DirPath + "\\data\\sc";
                        File.WriteAllLines(Environment.CurrentDirectory + "\\WorkDir.cfg", new List<string> { DirPath });
                    }
                }

                var dirInfo = new DirectoryInfo(DirPath + "\\data");
                if (!dirInfo.Exists)
                {
                    //dirInfo.Create();
                    dirInfo.CreateSubdirectory("cs");
                    dirInfo.CreateSubdirectory("sc");
                }

                //// Проверяем на то что есть, что-либо сохранять
                //if (ButtonCsCompare.IsEnabled == false && ButtonScCompare.IsEnabled == false)
                //{
                //    return;
                //}

                if (ButtonCsCompare.IsEnabled)
                {
                    try
                    {
                        File.WriteAllLines(DirPathCS + "\\TextBoxPathIn", new List<string> { TextBoxPathIn.Text });
                        File.WriteAllLines(DirPathCS + "\\TextBoxPathOut", new List<string> { TextBoxPathOut.Text });

                        File.WriteAllLines(DirPathCS + "\\AddressForClientPacketsIn", new List<string> { TextBox11.Text });
                        File.WriteAllLines(DirPathCS + "\\AddressForServerPacketsIn", new List<string> { TextBox12.Text });

                        File.WriteAllLines(DirPathCS + "\\AddressForClientPacketsOut", new List<string> { TextBox21.Text });
                        File.WriteAllLines(DirPathCS + "\\AddressForServerPacketsOut", new List<string> { TextBox22.Text });

                        File.WriteAllLines(DirPathCS + "\\NotFoundIn", new List<string> { TextBox17Copy.Text });
                        File.WriteAllLines(DirPathCS + "\\NotFoundOut", new List<string> { TextBox17Copy1.Text });

                        File.WriteAllLines(DirPathCS + "\\NameNotFound", new List<string> { TextBox32.Text });

                        File.WriteAllLines(DirPathCS + "\\isCompareCS", new List<string> { isCompareCS.ToString() });
                        File.WriteAllLines(DirPathCS + "\\isCompareSC", new List<string> { isCompareSC.ToString() });


                        File.WriteAllLines(DirPath + "\\data\\InListSource", InListSource);
                        File.WriteAllLines(DirPath + "\\data\\InListDestination", InListDestination);

                        File.WriteAllLines(DirPathCS + "\\ListNameSourceCS", ListNameSourceCS);
                        File.WriteAllLines(DirPathCS + "\\ListNameSourceSC", new List<string>());
                        File.WriteAllLines(DirPathCS + "\\ListSubSourceCS", ListSubSourceCS);
                        File.WriteAllLines(DirPathCS + "\\ListSubSourceSC", new List<string>());
                        File.WriteAllLines(DirPathCS + "\\ListOpcodeSourceCS", ListOpcodeSourceCS);
                        File.WriteAllLines(DirPathCS + "\\ListOpcodeSourceSC", new List<string>());

                        File.WriteAllLines(DirPathCS + "\\ListNameDestinationCS", ListNameDestinationCS);
                        File.WriteAllLines(DirPathCS + "\\ListNameDestinationSC", new List<string>());
                        File.WriteAllLines(DirPathCS + "\\ListSubDestinationCS", ListSubDestinationCS);
                        File.WriteAllLines(DirPathCS + "\\ListSubDestinationSC", new List<string>());
                        File.WriteAllLines(DirPathCS + "\\ListOpcodeDestinationCS", ListOpcodeDestinationCS);
                        File.WriteAllLines(DirPathCS + "\\ListOpcodeDestinationSC", new List<string>());

                        File.WriteAllLines(DirPathCS + "\\ListNameCompareCS", ListNameCompareCS);
                        File.WriteAllLines(DirPathCS + "\\ListNameCompareSC", new List<string>());

                        File.WriteAllLines(DirPathCS + "\\ListNameCompareOutCS", ListNameCompareOutCS);
                        File.WriteAllLines(DirPathCS + "\\ListNameCompareOutSC", new List<string>());
                        File.WriteAllLines(DirPathCS + "\\ListNameCompare", ListNameCompare);

                        var json = JsonConvert.SerializeObject(InUseIn, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\InUseIn.json", json);

                        json = JsonConvert.SerializeObject(InUseOut, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\InUseOut.json", json);

                        json = JsonConvert.SerializeObject(IsRenameDestination, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\IsRenameDestination.json", json);

                        json = JsonConvert.SerializeObject(StructureSourceCS, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\StructureSourceCS.json", json);

                        json = JsonConvert.SerializeObject(StructureSourceSC, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\StructureSourceSC.json", json);

                        json = JsonConvert.SerializeObject(StructureDestinationCS, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\StructureDestinationCS.json", json);

                        json = JsonConvert.SerializeObject(StructureDestinationSC, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\StructureDestinationSC.json", json);

                        json = JsonConvert.SerializeObject(XrefsIn, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\XrefsIn.json", json);

                        json = JsonConvert.SerializeObject(XrefsOut, Formatting.Indented);
                        File.WriteAllText(DirPathCS + "\\XrefsOut.json", json);
                    }
                    catch (Exception exception)
                    {
                        // Рефакторинг: используем UIHelper для показа ошибки
                        Helpers.UIHelper.ShowError(Dispatcher, exception.Message, "Error");
                    }
                }

                if (ButtonScCompare.IsEnabled)
                {
                    try
                    {
                        File.WriteAllLines(DirPathSC + "\\TextBoxPathIn", new List<string> { TextBoxPathIn.Text });

                        File.WriteAllLines(DirPathSC + "\\TextBoxPathOut", new List<string> { TextBoxPathOut.Text });

                        File.WriteAllLines(DirPathSC + "\\AddressForClientPacketsIn", new List<string> { TextBox11.Text });
                        File.WriteAllLines(DirPathSC + "\\AddressForServerPacketsIn", new List<string> { TextBox12.Text });

                        File.WriteAllLines(DirPathSC + "\\AddressForClientPacketsOut", new List<string> { TextBox21.Text });
                        File.WriteAllLines(DirPathSC + "\\AddressForServerPacketsOut", new List<string> { TextBox22.Text });

                        File.WriteAllLines(DirPathSC + "\\NotFoundIn", new List<string> { TextBox17Copy.Text });
                        File.WriteAllLines(DirPathSC + "\\NotFoundOut", new List<string> { TextBox17Copy1.Text });

                        File.WriteAllLines(DirPathSC + "\\NameNotFound", new List<string> { TextBox32.Text });

                        File.WriteAllLines(DirPathCS + "\\isCompareCS", new List<string> { isCompareCS.ToString() });
                        File.WriteAllLines(DirPathCS + "\\isCompareSC", new List<string> { isCompareSC.ToString() });

                        File.WriteAllLines(DirPath + "\\data\\InListSource", InListSource);
                        File.WriteAllLines(DirPath + "\\data\\InListDestination", InListDestination);

                        File.WriteAllLines(DirPathSC + "\\ListNameSourceCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListNameSourceSC", ListNameSourceSC);
                        File.WriteAllLines(DirPathSC + "\\ListSubSourceCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListSubSourceSC", ListSubSourceSC);
                        File.WriteAllLines(DirPathSC + "\\ListOpcodeSourceCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListOpcodeSourceSC", ListOpcodeSourceSC);

                        File.WriteAllLines(DirPathSC + "\\ListNameDestinationCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListNameDestinationSC", ListNameDestinationSC);
                        File.WriteAllLines(DirPathSC + "\\ListSubDestinationCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListSubDestinationSC", ListSubDestinationSC);
                        File.WriteAllLines(DirPathSC + "\\ListOpcodeDestinationCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListOpcodeDestinationSC", ListOpcodeDestinationSC);

                        File.WriteAllLines(DirPathSC + "\\ListNameCompareCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListNameCompareSC", ListNameCompareSC);

                        File.WriteAllLines(DirPathSC + "\\ListNameCompareOutCS", new List<string>());
                        File.WriteAllLines(DirPathSC + "\\ListNameCompareOutSC", ListNameCompareOutSC);
                        File.WriteAllLines(DirPathSC + "\\ListNameCompare", ListNameCompare);

                        var json = JsonConvert.SerializeObject(InUseIn, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\InUseIn.json", json);

                        json = JsonConvert.SerializeObject(InUseOut, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\InUseOut.json", json);

                        json = JsonConvert.SerializeObject(IsRenameDestination, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\IsRenameDestination.json", json);

                        json = JsonConvert.SerializeObject(StructureSourceCS, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\StructureSourceCS.json", json);

                        json = JsonConvert.SerializeObject(StructureSourceSC, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\StructureSourceSC.json", json);

                        json = JsonConvert.SerializeObject(StructureDestinationCS, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\StructureDestinationCS.json", json);

                        json = JsonConvert.SerializeObject(StructureDestinationSC, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\StructureDestinationSC.json", json);

                        json = JsonConvert.SerializeObject(XrefsIn, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\XrefsIn.json", json);

                        json = JsonConvert.SerializeObject(XrefsOut, Formatting.Indented);
                        File.WriteAllText(DirPathSC + "\\XrefsOut.json", json);
                    }
                    catch (Exception exception)
                    {
                        // Рефакторинг: используем UIHelper для показа ошибки
                        Helpers.UIHelper.ShowError(Dispatcher, exception.Message, "Error");
                    }
                }
            }

            stopWatch.Stop();
            TextBox15.Text = stopWatch.Elapsed.ToString();
            Label_Semafor1.Background = Brushes.GreenYellow;
            Label_Semafor2.Background = Brushes.GreenYellow;
        }

        private void btn_LoadSnapshotCS_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor1.Background = Brushes.Red;
            Label_Semafor2.Background = Brushes.Red;

            //lock (lockObj)
            {
                try
                {
                    DirPath = File.ReadAllLines(Environment.CurrentDirectory + "\\WorkDir.cfg")[0];
                    DirPathCS = DirPath + "\\data\\cs";
                    DirPathSC = DirPath + "\\data\\sc";

                    TextBoxPathIn.Text = File.ReadAllLines(DirPathCS + "\\TextBoxPathIn")[0];
                    TextBoxPathOut.Text = File.ReadAllLines(DirPathCS + "\\TextBoxPathOut")[0];

                    TextBox11.Text = File.ReadAllLines(DirPathCS + "\\AddressForClientPacketsIn")[0];
                    TextBox12.Text = File.ReadAllLines(DirPathCS + "\\AddressForServerPacketsIn")[0];

                    TextBox21.Text = File.ReadAllLines(DirPathCS + "\\AddressForClientPacketsOut")[0];
                    TextBox22.Text = File.ReadAllLines(DirPathCS + "\\AddressForServerPacketsOut")[0];

                    TextBox17Copy.Text = File.ReadAllLines(DirPathCS + "\\NotFoundIn")[0];
                    TextBox17Copy1.Text = File.ReadAllLines(DirPathCS + "\\NotFoundOut")[0];
                    TextBox32.Text = File.ReadAllLines(DirPathCS + "\\NameNotFound")[0];

                    isCompareCS = File.ReadAllLines(DirPathCS + "\\isCompareCS")[0] == "True";
                    isCompareSC = File.ReadAllLines(DirPathCS + "\\isCompareSC")[0] == "True";
                    InListSource = File.ReadAllLines(DirPath + "\\data\\InListSource").ToList();
                    ListView11.ItemsSource = InListSource;
                    InListDestination = File.ReadAllLines(DirPath + "\\data\\InListDestination").ToList();
                    ListView21.ItemsSource = InListDestination;

                    ListNameSourceCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameSourceCS"))
                    {
                        ListNameSourceCS.Add(line);
                    }
                    ListView12.ItemsSource = ListNameSourceCS;
                    TextBox13.Text = ListNameSourceCS.Count.ToString();

                    ListNameSourceSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameSourceSC"))
                    {
                        ListNameSourceSC.Add(line);
                    }
                    //ListView12.ItemsSource = ListNameSourceSC;
                    TextBox16.Text = ListNameSourceSC.Count.ToString();

                    ListSubSourceCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListSubSourceCS"))
                    {
                        ListSubSourceCS.Add(line);
                    }
                    ListView13.ItemsSource = ListSubSourceCS;
                    TextBox14.Text = ListSubSourceCS.Count.ToString();

                    ListSubSourceSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListSubSourceSC"))
                    {
                        ListSubSourceSC.Add(line);
                    }
                    //ListView13.ItemsSource = ListSubSourceSC;
                    TextBox17.Text = ListSubSourceSC.Count.ToString();

                    ListOpcodeSourceCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListOpcodeSourceCS"))
                    {
                        ListOpcodeSourceCS.Add(line);
                    }
                    ListView14.ItemsSource = ListOpcodeSourceCS;
                    TextBox16Copy.Text = ListOpcodeSourceCS.Count.ToString();

                    ListOpcodeSourceSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListOpcodeSourceSC"))
                    {
                        ListOpcodeSourceSC.Add(line);
                    }
                    //ListView14.ItemsSource = ListOpcodeSourceSC;
                    //TextBox16Copy.Text = ListOpcodeSourceSC.Count.ToString();

                    ListNameDestinationCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameDestinationCS"))
                    {
                        ListNameDestinationCS.Add(line);
                    }
                    ListView22.ItemsSource = ListNameDestinationCS;
                    TextBox23.Text = ListNameDestinationCS.Count.ToString();

                    ListNameDestinationSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameDestinationSC"))
                    {
                        ListNameDestinationSC.Add(line);
                    }
                    //ListView22.ItemsSource = ListNameDestinationSC;
                    TextBox26.Text = ListNameDestinationSC.Count.ToString();

                    ListSubDestinationCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListSubDestinationCS"))
                    {
                        ListSubDestinationCS.Add(line);
                    }
                    ListView23.ItemsSource = ListSubDestinationCS;
                    TextBox24.Text = ListSubDestinationCS.Count.ToString();

                    ListSubDestinationSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListSubDestinationSC"))
                    {
                        ListSubDestinationSC.Add(line);
                    }
                    //ListView23.ItemsSource = ListSubDestinationSC;
                    TextBox27.Text = ListSubDestinationSC.Count.ToString();

                    ListOpcodeDestinationCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListOpcodeDestinationCS"))
                    {
                        ListOpcodeDestinationCS.Add(line);
                    }
                    ListView24.ItemsSource = ListOpcodeDestinationCS;
                    TextBox16Copy1.Text = ListOpcodeDestinationCS.Count.ToString();

                    ListOpcodeDestinationSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListOpcodeDestinationSC"))
                    {
                        ListOpcodeDestinationSC.Add(line);
                    }
                    //ListView24.ItemsSource = ListOpcodeDestinationSC;
                    //TextBox16Copy1.Text = ListOpcodeDestinationSC.Count.ToString();

                    ListNameCompareCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameCompareCS"))
                    {
                        ListNameCompareCS.Add(line);
                    }
                    ListView31.ItemsSource = ListNameCompareCS;
                    TextBox31.Text = ListNameCompareCS.Count.ToString();

                    ListNameCompareSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameCompareSC"))
                    {
                        ListNameCompareSC.Add(line);
                    }
                    //ListView31.ItemsSource = ListNameCompareCS;
                    //TextBox31.Text = ListNameCompareSC.Count.ToString();

                    ListNameCompare.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameCompare"))
                    {
                        ListNameCompare.Add(line);
                    }

                    ListNameCompareOutCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameCompareOutCS"))
                    {
                        ListNameCompareOutCS.Add(line);
                    }
                    ListView32.ItemsSource = ListNameCompareOutCS;

                    ListNameCompareOutSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathCS + "\\ListNameCompareOutSC"))
                    {
                        ListNameCompareOutSC.Add(line);
                    }
                    //ListView32.ItemsSource = ListNameCompareOutSC;

                    var json = File.ReadAllText(DirPathCS + "\\InUseIn.json");
                    InUseIn = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                    json = File.ReadAllText(DirPathCS + "\\InUseOut.json");
                    InUseOut = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                    json = File.ReadAllText(DirPathCS + "\\IsRenameDestination.json");
                    IsRenameDestination = JsonConvert.DeserializeObject<Dictionary<int, bool>>(json);

                    json = File.ReadAllText(DirPathCS + "\\StructureSourceCS.json");
                    var structureSourceCS = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureSourceCS.Clear();
                    if (structureSourceCS != null)
                    {
                        foreach (var kvp in structureSourceCS)
                        {
                            StructureSourceCS[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathCS + "\\StructureSourceSC.json");
                    var structureSourceSC = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureSourceSC.Clear();
                    if (structureSourceSC != null)
                    {
                        foreach (var kvp in structureSourceSC)
                        {
                            StructureSourceSC[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathCS + "\\StructureDestinationCS.json");
                    var structureDestinationCS = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureDestinationCS.Clear();
                    if (structureDestinationCS != null)
                    {
                        foreach (var kvp in structureDestinationCS)
                        {
                            StructureDestinationCS[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathCS + "\\StructureDestinationSC.json");
                    var structureDestinationSC = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureDestinationSC.Clear();
                    if (structureDestinationSC != null)
                    {
                        foreach (var kvp in structureDestinationSC)
                        {
                            StructureDestinationSC[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathCS + "\\XrefsIn.json");
                    XrefsIn = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                    json = File.ReadAllText(DirPathCS + "\\XrefsOut.json");
                    XrefsOut = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                    BtnLoadIn_Copy.IsEnabled = true;
                    BtnLoadIn.IsEnabled = true;
                    BtnCsLoadNameIn.IsEnabled = true;
                    BtnScLoadNameIn.IsEnabled = true;
                    ButtonSaveIn1.IsEnabled = true;
                    ButtonSaveIn2.IsEnabled = false;
                    BtnLoadOut.IsEnabled = true;
                    BtnCsLoadNameOut.IsEnabled = true;
                    BtnScLoadNameOut.IsEnabled = true;
                    ButtonSaveOut1.IsEnabled = true;
                    ButtonSaveOut2.IsEnabled = false;
                    ButtonCsCompare.IsEnabled = true;
                    ButtonScCompare.IsEnabled = false;
                    Button2Copy2.IsEnabled = false;
                    Button2Copy2_Copy.IsEnabled = false;
                    ButtonCopy2.IsEnabled = false;
                    ButtonCopy2_Copy.IsEnabled = false;
                    if (isCompareCS)
                    {
                        CheckBoxLock.IsChecked = true;
                    }

                    _isInCs = true;
                    _isOutCs = true;
                }
                catch (Exception exception)
                {
                    // Рефакторинг: используем UIHelper для показа ошибки
                    Helpers.UIHelper.ShowError(Dispatcher, exception.Message, "Error");
                }
            }

            Label_Semafor1.Background = Brushes.GreenYellow;
            Label_Semafor2.Background = Brushes.GreenYellow;
        }

        private void btn_LoadSnapshotSC_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor1.Background = Brushes.Red;
            Label_Semafor2.Background = Brushes.Red;

            //lock (lockObj)
            {
                try
                {
                    DirPath = File.ReadAllLines(Environment.CurrentDirectory + "\\WorkDir.cfg")[0];
                    DirPathCS = DirPath + "\\data\\cs";
                    DirPathSC = DirPath + "\\data\\sc";

                    TextBoxPathIn.Text = File.ReadAllLines(DirPathSC + "\\TextBoxPathIn")[0];
                    TextBoxPathOut.Text = File.ReadAllLines(DirPathSC + "\\TextBoxPathOut")[0];

                    TextBox11.Text = File.ReadAllLines(DirPathSC + "\\AddressForClientPacketsIn")[0];
                    TextBox12.Text = File.ReadAllLines(DirPathSC + "\\AddressForServerPacketsIn")[0];

                    TextBox21.Text = File.ReadAllLines(DirPathSC + "\\AddressForClientPacketsOut")[0];
                    TextBox22.Text = File.ReadAllLines(DirPathSC + "\\AddressForServerPacketsOut")[0];

                    TextBox17Copy.Text = File.ReadAllLines(DirPathSC + "\\NotFoundIn")[0];
                    TextBox17Copy1.Text = File.ReadAllLines(DirPathSC + "\\NotFoundOut")[0];
                    TextBox32.Text = File.ReadAllLines(DirPathSC + "\\NameNotFound")[0];

                    isCompareCS = File.ReadAllLines(DirPathCS + "\\isCompareCS")[0] == "True";
                    isCompareSC = File.ReadAllLines(DirPathCS + "\\isCompareSC")[0] == "True";

                    InListSource = File.ReadAllLines(DirPath + "\\data\\InListSource").ToList();
                    ListView11.ItemsSource = InListSource;
                    InListDestination = File.ReadAllLines(DirPath + "\\data\\InListDestination").ToList();
                    ListView21.ItemsSource = InListDestination;

                    ListNameSourceCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameSourceCS"))
                    {
                        ListNameSourceCS.Add(line);
                    }
                    //ListView12.ItemsSource = ListNameSourceCS;
                    TextBox13.Text = ListNameSourceCS.Count.ToString();

                    ListNameSourceSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameSourceSC"))
                    {
                        ListNameSourceSC.Add(line);
                    }
                    ListView12.ItemsSource = ListNameSourceSC;
                    TextBox16.Text = ListNameSourceSC.Count.ToString();

                    ListSubSourceCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListSubSourceCS"))
                    {
                        ListSubSourceCS.Add(line);
                    }
                    //ListView13.ItemsSource = ListSubSourceCS;
                    TextBox14.Text = ListSubSourceCS.Count.ToString();

                    ListSubSourceSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListSubSourceSC"))
                    {
                        ListSubSourceSC.Add(line);
                    }
                    ListView13.ItemsSource = ListSubSourceSC;
                    TextBox17.Text = ListSubSourceSC.Count.ToString();

                    ListOpcodeSourceCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListOpcodeSourceCS"))
                    {
                        ListOpcodeSourceCS.Add(line);
                    }
                    //ListView14.ItemsSource = ListOpcodeSourceCS;
                    //TextBox16Copy.Text = ListOpcodeSourceCS.Count.ToString();

                    ListOpcodeSourceSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListOpcodeSourceSC"))
                    {
                        ListOpcodeSourceSC.Add(line);
                    }
                    ListView14.ItemsSource = ListOpcodeSourceSC;
                    TextBox16Copy.Text = ListOpcodeSourceSC.Count.ToString();

                    ListNameDestinationCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameDestinationCS"))
                    {
                        ListNameDestinationCS.Add(line);
                    }
                    //ListView22.ItemsSource = ListNameDestinationCS;
                    TextBox23.Text = ListNameDestinationCS.Count.ToString();

                    ListNameDestinationSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameDestinationSC"))
                    {
                        ListNameDestinationSC.Add(line);
                    }
                    ListView22.ItemsSource = ListNameDestinationSC;
                    TextBox26.Text = ListNameDestinationSC.Count.ToString();

                    ListSubDestinationCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListSubDestinationCS"))
                    {
                        ListSubDestinationCS.Add(line);
                    }
                    //ListView23.ItemsSource = ListSubDestinationCS;
                    TextBox24.Text = ListSubDestinationCS.Count.ToString();

                    ListSubDestinationSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListSubDestinationSC"))
                    {
                        ListSubDestinationSC.Add(line);
                    }
                    ListView23.ItemsSource = ListSubDestinationSC;
                    TextBox27.Text = ListSubDestinationSC.Count.ToString();

                    ListOpcodeDestinationCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListOpcodeDestinationCS"))
                    {
                        ListOpcodeDestinationCS.Add(line);
                    }
                    //ListView24.ItemsSource = ListOpcodeDestinationCS;
                    //TextBox16Copy1.Text = ListOpcodeDestinationCS.Count.ToString();

                    ListOpcodeDestinationSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListOpcodeDestinationSC"))
                    {
                        ListOpcodeDestinationSC.Add(line);
                    }
                    ListView24.ItemsSource = ListOpcodeDestinationSC;
                    TextBox16Copy1.Text = ListOpcodeDestinationSC.Count.ToString();

                    ListNameCompareCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameCompareCS"))
                    {
                        ListNameCompareCS.Add(line);
                    }
                    //ListView31.ItemsSource = ListNameCompareCS;
                    //TextBox31.Text = ListNameCompareCS.Count.ToString();

                    ListNameCompareSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameCompareSC"))
                    {
                        ListNameCompareSC.Add(line);
                    }
                    ListView31.ItemsSource = ListNameCompareSC;
                    TextBox31.Text = ListNameCompareSC.Count.ToString();

                    ListNameCompare.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameCompare"))
                    {
                        ListNameCompare.Add(line);
                    }

                    ListNameCompareOutCS.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameCompareOutCS"))
                    {
                        ListNameCompareOutCS.Add(line);
                    }
                    //ListView32.ItemsSource = ListNameCompareOutCS;

                    ListNameCompareOutSC.Clear();
                    foreach (var line in File.ReadAllLines(DirPathSC + "\\ListNameCompareOutSC"))
                    {
                        ListNameCompareOutSC.Add(line);
                    }
                    ListView32.ItemsSource = ListNameCompareOutSC;

                    var json = File.ReadAllText(DirPathSC + "\\InUseIn.json");
                    InUseIn = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                    json = File.ReadAllText(DirPathSC + "\\InUseOut.json");
                    InUseOut = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                    json = File.ReadAllText(DirPathSC + "\\IsRenameDestination.json");
                    IsRenameDestination = JsonConvert.DeserializeObject<Dictionary<int, bool>>(json);

                    json = File.ReadAllText(DirPathSC + "\\StructureSourceCS.json");
                    var structureSourceCS5 = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureSourceCS.Clear();
                    if (structureSourceCS5 != null)
                    {
                        foreach (var kvp in structureSourceCS5)
                        {
                            StructureSourceCS[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathSC + "\\StructureSourceSC.json");
                    var structureSourceSC5 = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureSourceSC.Clear();
                    if (structureSourceSC5 != null)
                    {
                        foreach (var kvp in structureSourceSC5)
                        {
                            StructureSourceSC[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathSC + "\\StructureDestinationCS.json");
                    var structureDestinationCS6 = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureDestinationCS.Clear();
                    if (structureDestinationCS6 != null)
                    {
                        foreach (var kvp in structureDestinationCS6)
                        {
                            StructureDestinationCS[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathSC + "\\StructureDestinationSC.json");
                    var structureDestinationSC6 = JsonConvert.DeserializeObject<Dictionary<int, List<Struc>>>(json);
                    StructureDestinationSC.Clear();
                    if (structureDestinationSC6 != null)
                    {
                        foreach (var kvp in structureDestinationSC6)
                        {
                            StructureDestinationSC[kvp.Key] = new ObservableCollection<Struc>(kvp.Value);
                        }
                    }

                    json = File.ReadAllText(DirPathSC + "\\XrefsIn.json");
                    XrefsIn = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                    json = File.ReadAllText(DirPathSC + "\\XrefsOut.json");
                    XrefsOut = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                    BtnLoadIn_Copy.IsEnabled = true;
                    BtnLoadIn.IsEnabled = true;
                    BtnCsLoadNameIn.IsEnabled = true;
                    BtnScLoadNameIn.IsEnabled = true;
                    ButtonSaveIn1.IsEnabled = false;
                    ButtonSaveIn2.IsEnabled = true;
                    BtnLoadOut.IsEnabled = true;
                    BtnCsLoadNameOut.IsEnabled = true;
                    BtnScLoadNameOut.IsEnabled = true;
                    ButtonSaveOut1.IsEnabled = false;
                    ButtonSaveOut2.IsEnabled = true;
                    ButtonCsCompare.IsEnabled = false;
                    ButtonScCompare.IsEnabled = true;
                    ButtonCopy2.IsEnabled = false;
                    ButtonCopy2_Copy.IsEnabled = false;
                    Button2Copy2.IsEnabled = false;
                    Button2Copy2_Copy.IsEnabled = false;
                    if (isCompareSC)
                    {
                        CheckBoxLock.IsChecked = true;
                    }

                    _isInSc = true;
                    _isOutSc = true;
                }
                catch (Exception exception)
                {
                    // Рефакторинг: используем UIHelper для показа ошибки
                    Helpers.UIHelper.ShowError(Dispatcher, exception.Message, "Error");
                }
            }

            Label_Semafor1.Background = Brushes.GreenYellow;
            Label_Semafor2.Background = Brushes.GreenYellow;
        }

        private void button_EditOutOpcode_Click(object sender, RoutedEventArgs e)
        {
            if (ListView24.SelectedItem != null)
            {
                if (ButtonSaveOut1.IsEnabled)
                {
                    ListOpcodeDestinationCS[ListView24.SelectedIndex] = TextBoxEditOutOpcode.Text;
                }
                else
                {
                    ListOpcodeDestinationSC[ListView24.SelectedIndex] = TextBoxEditOutOpcode.Text;
                }
                ListView24.SelectedItem = TextBoxEditOutOpcode.Text;
                ListView24.Items.Refresh();
                var notFound = 0;
                foreach (var item in ListView24.Items)
                {
                    if (item.ToString() == "0xfff")
                    {
                        notFound++;
                    }
                }
                TextBox17Copy1.Text = notFound.ToString();
            }
        }

        private void ListView12_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ListView12.SelectedItem != null)
            {
                ListView13.SelectedIndex = ListView12.SelectedIndex;
                TextBoxNumIn.Text = (ListView12.SelectedIndex + 1).ToString();
                ListView13.UpdateLayout();
                ListView13.ScrollIntoView(ListView13.SelectedItem);

                ListView14.SelectedIndex = ListView12.SelectedIndex;
                ListView14.UpdateLayout();
                ListView14.ScrollIntoView(ListView14.SelectedItem);
            }
        }

        private void ListView13_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ListView13.SelectedItem != null)
            {
                ListView12.SelectedIndex = ListView13.SelectedIndex;
                TextBoxNumIn.Text = (ListView13.SelectedIndex + 1).ToString();
                ListView12.UpdateLayout();
                ListView12.ScrollIntoView(ListView12.SelectedItem);

                ListView14.SelectedIndex = ListView13.SelectedIndex;
                ListView14.UpdateLayout();
                ListView14.ScrollIntoView(ListView14.SelectedItem);
            }
        }

        private void ListView32_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ListView32.SelectedItem != null)
            {
                ListView31.SelectedIndex = ListView32.SelectedIndex;
                TextBoxNumOut.Text = (ListView32.SelectedIndex + 1).ToString();
                ListView31.UpdateLayout();
                ListView31.ScrollIntoView(ListView31.SelectedItem);

                ListView22.SelectedIndex = ListView32.SelectedIndex;
                ListView22.UpdateLayout();
                ListView22.ScrollIntoView(ListView22.SelectedItem);

                ListView23.SelectedIndex = ListView32.SelectedIndex;
                ListView23.UpdateLayout();
                ListView23.ScrollIntoView(ListView23.SelectedItem);

                ListView24.SelectedIndex = ListView32.SelectedIndex;
                ListView24.UpdateLayout();
                ListView24.ScrollIntoView(ListView24.SelectedItem);
            }
        }

        private void ListView23_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ListView23.SelectedItem != null)
            {
                ListView22.SelectedIndex = ListView23.SelectedIndex;
                TextBoxNumOut.Text = (ListView23.SelectedIndex + 1).ToString();
                ListView22.UpdateLayout();
                ListView22.ScrollIntoView(ListView22.SelectedItem);

                ListView24.SelectedIndex = ListView23.SelectedIndex;
                ListView24.UpdateLayout();
                ListView24.ScrollIntoView(ListView24.SelectedItem);
            }
        }

        private void ListView22_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ListView22.SelectedItem != null)
            {
                ListView23.SelectedIndex = ListView22.SelectedIndex;
                TextBoxNumOut.Text = (ListView22.SelectedIndex + 1).ToString();
                ListView23.UpdateLayout();
                ListView23.ScrollIntoView(ListView23.SelectedItem);

                ListView24.SelectedIndex = ListView22.SelectedIndex;
                ListView24.UpdateLayout();
                ListView24.ScrollIntoView(ListView24.SelectedItem);
            }
        }

        private void ListView24_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ListView24.SelectedItem != null)
            {
                TextBoxEditOutOpcode.Text = ListView24.SelectedItem.ToString();

                ListView22.SelectedIndex = ListView24.SelectedIndex;
                TextBoxNumOut.Text = (ListView24.SelectedIndex + 1).ToString();
                ListView22.UpdateLayout();
                ListView22.ScrollIntoView(ListView22.SelectedItem);

                ListView23.SelectedIndex = ListView24.SelectedIndex;
                ListView23.UpdateLayout();
                ListView23.ScrollIntoView(ListView23.SelectedItem);
            }
        }

        private void ListView14_SelectionChanged(object sender, RoutedEventArgs e)
        {
            ListView12.SelectedIndex = ListView14.SelectedIndex;
            TextBoxNumIn.Text = (ListView14.SelectedIndex + 1).ToString();
            ListView12.UpdateLayout();
            ListView12.ScrollIntoView(ListView12.SelectedItem);

            ListView13.SelectedIndex = ListView14.SelectedIndex;
            ListView13.UpdateLayout();
            ListView13.ScrollIntoView(ListView13.SelectedItem);
        }

        private void ListView31_SelectionChanged(object sender, RoutedEventArgs e)
        {
            ListView32.SelectedIndex = ListView31.SelectedIndex;
            TextBoxNumOut.Text = (ListView31.SelectedIndex + 1).ToString();
            ListView32.UpdateLayout();
            ListView32.ScrollIntoView(ListView32.SelectedItem);

            ListView22.SelectedIndex = ListView31.SelectedIndex;
            ListView22.UpdateLayout();
            ListView22.ScrollIntoView(ListView22.SelectedItem);

            ListView23.SelectedIndex = ListView31.SelectedIndex;
            ListView23.UpdateLayout();
            ListView23.ScrollIntoView(ListView23.SelectedItem);

            ListView24.SelectedIndex = ListView31.SelectedIndex;
            ListView24.UpdateLayout();
            ListView24.ScrollIntoView(ListView24.SelectedItem);
        }

        private string FilePathName { get; set; }
        private string FilePath { get; set; }

        public bool SavePktFileDialog(string name)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Packet File|*.cs",
                FileName = name + ".cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var offset = saveFileDialog.FileName.LastIndexOf("\\", StringComparison.Ordinal) + 1;
                FilePath = saveFileDialog.FileName.Substring(0, offset);
                FilePathName = saveFileDialog.FileName;
                return true;
            }

            return false;
        }

        private void BtnMakePktIn_Click(object sender, RoutedEventArgs e)
        {
            FilePath = null;
            if (ListView12.SelectedItem != null)
            {
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor1.Background = Brushes.Yellow);
                var name = ListView12.SelectedItem.ToString();
                //GotoNameIn(name);
                if (ButtonSaveIn1.IsEnabled)
                {
                    // сохраняем в виде файла
                    for (var i = 0; i < ListNameSourceCS.Count; i++)
                    {
                        // удалим опкоды в конце имени
                        string nameSource;
                        if (CheckBoxRemoveOpcode.IsChecked == true)
                        {
                            // удаляем оконечные опкоды в имени пакета
                            var offset = ListNameSourceCS[i].LastIndexOf("_", StringComparison.Ordinal);
                            if (offset > 0)
                            {
                                nameSource = ListNameSourceCS[i].Substring(0, offset);
                            }
                            else
                            {
                                nameSource = ListNameSourceCS[i];
                            }
                        }
                        else
                        {
                            nameSource = ListNameSourceCS[i];
                        }

                        var tmp = new List<string>();
                        var lst = "";
                        lst = "using AAEmu.Commons.Network;";
                        tmp.Add(lst);
                        lst = "using AAEmu.Game.Core.Network.Game;";
                        tmp.Add(lst);
                        lst = "";
                        tmp.Add(lst);
                        lst = "namespace AAEmu.Game.Core.Packets.C2G";
                        tmp.Add(lst);
                        lst = "{";
                        tmp.Add(lst);
                        lst = "    public class " + nameSource + " : GamePacket";
                        tmp.Add(lst);
                        lst = "    {";
                        tmp.Add(lst);
                        lst = "        public " + nameSource + " : base(CSOffsets." + nameSource + ", 1)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);
                        lst = "        }";
                        tmp.Add(lst);
                        lst = "";
                        tmp.Add(lst);
                        lst = "        public override void Read(PacketStream stream)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);
                        foreach (var str in StructureSourceCS[i])
                        {
                            switch (StructStringIn)
                            {
                                case "struct ver0.5":
                                    // Действия для выбранного варианта 1
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_05)str.Type + "();";
                                    break;
                                case "struct ver1.2+": // до 5+
                                    // Действия для выбранного варианта 2
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_12)str.Type + "();";
                                    break;
                                case "struct ver6.0+":
                                    // Действия для выбранного варианта 3
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_60)str.Type + "();";
                                    break;
                                case "struct ver8.0+":
                                    // Действия для выбранного варианта 4
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_80)str.Type + "();";
                                    break;
                                default:
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_80)str.Type + "();";
                                    break;
                            }
                            //if ( CheckBoxTypeEnumNewIn.IsChecked == true )
                            //{
                            //    lst = "            var " + str.Name.Replace( "\"", "" ) + " = stream.Read" + (TypeEnum_35)str.Type + "();";
                            //}
                            //else
                            //{
                            //    lst = "            var " + str.Name.Replace( "\"", "" ) + " = stream.Read" + (TypeEnum_12)str.Type + "();";
                            //}
                            tmp.Add(lst);
                        }
                        lst = "        }";
                        tmp.Add(lst);
                        lst = "    }";
                        tmp.Add(lst);
                        lst = "}";
                        tmp.Add(lst);
                        if (FilePath != null)
                        {
                            File.WriteAllLines(FilePath + nameSource + ".cs", tmp);
                        }
                        else
                        {
                            if (SavePktFileDialog(nameSource))
                            {
                                File.WriteAllLines(FilePathName, tmp);
                            }
                        }
                    }
                }
                else
                {
                    // сохраняем в виде файла
                    for (var i = 0; i < ListNameSourceSC.Count; i++)
                    {
                        // удалим опкоды в конце имени
                        string nameSource;
                        if (CheckBoxRemoveOpcode.IsChecked == true)
                        {
                            // удаляем оконечные опкоды в имени пакета
                            var offset = ListNameSourceSC[i].LastIndexOf("_", StringComparison.Ordinal);
                            if (offset > 0)
                            {
                                nameSource = ListNameSourceSC[i].Substring(0, offset);
                            }
                            else
                            {
                                nameSource = ListNameSourceSC[i];
                            }
                        }
                        else
                        {
                            nameSource = ListNameSourceSC[i];
                        }

                        var tmp = new List<string>();
                        var lst = "";
                        lst = "using AAEmu.Commons.Network;";
                        tmp.Add(lst);
                        lst = "using AAEmu.Game.Core.Network.Game;";
                        tmp.Add(lst);
                        lst = "";
                        tmp.Add(lst);
                        lst = "namespace AAEmu.Game.Core.Packets.G2C";
                        tmp.Add(lst);
                        lst = "{";
                        tmp.Add(lst);
                        lst = "    public class " + nameSource + " : GamePacket";
                        tmp.Add(lst);
                        lst = "    {";
                        tmp.Add(lst);
                        foreach (var str in StructureSourceSC[i])
                        {
                            switch (StructStringIn)
                            {
                                case "struct ver0.5":
                                    // Действия для выбранного варианта 1
                                    lst = "        private readonly " + (TypeEnum_05)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                case "struct ver1.2+":
                                    // Действия для выбранного варианта 2
                                    lst = "        private readonly " + (TypeEnum_12)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                case "struct ver6.0+":
                                    // Действия для выбранного варианта 3
                                    lst = "        private readonly " + (TypeEnum_60)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                case "struct ver8.0+":
                                    // Действия для выбранного варианта 4
                                    lst = "        private readonly " + (TypeEnum_80)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                default:
                                    lst = "        private readonly " + (TypeEnum_80)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                            }
                            //if ( CheckBoxTypeEnumNewIn.IsChecked == true )
                            //{
                            //    lst = "        private readonly " + (TypeEnum_35)str.Type + " _" + str.Name.Replace( "\"", "" ) + ";";
                            //}
                            //else
                            //{
                            //    lst = "        private readonly " + (TypeEnum_12)str.Type + " _" + str.Name.Replace( "\"", "" ) + ";";
                            //}
                            tmp.Add(lst);
                        }
                        lst = "";
                        tmp.Add(lst);

                        lst = "        public " + nameSource + "(";
                        var li = StructureSourceSC[i];
                        for (var j = 0; j < li.Count; j++)
                        {
                            switch (StructStringIn)
                            {
                                case "struct ver0.5":
                                    // Действия для выбранного варианта 1
                                    lst += "" + (TypeEnum_05)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                case "struct ver1.2+":
                                    // Действия для выбранного варианта 2
                                    lst += "" + (TypeEnum_12)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                case "struct ver6.0+":
                                    // Действия для выбранного варианта 3
                                    lst += "" + (TypeEnum_60)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                case "struct ver8.0+":
                                    // Действия для выбранного варианта 4
                                    lst += "" + (TypeEnum_80)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                default:
                                    lst += "" + (TypeEnum_80)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                            }
                            //if ( CheckBoxTypeEnumNewIn.IsChecked == true )
                            //{
                            //    lst += "" + (TypeEnum_35)li[j].Type + " " + li[j].Name.Replace( "\"", "" );
                            //}
                            //else
                            //{
                            //    lst += "" + (TypeEnum_12)li[j].Type + " " + li[j].Name.Replace( "\"", "" );
                            //}
                            if (j < li.Count - 1)
                            {
                                lst += ", ";
                            }
                        }
                        lst += ") : base(SCOffsets." + nameSource + ", 1)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);

                        foreach (var str in StructureSourceSC[i])
                        {
                            lst = "            _" + str.Name.Replace("\"", "") + " = " + str.Name.Replace("\"", "") + ";";
                            tmp.Add(lst);
                        }

                        lst = "        }";
                        tmp.Add(lst);

                        lst = "";
                        tmp.Add(lst);
                        lst = "        public override void Write(PacketStream stream)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);
                        foreach (var str in StructureSourceSC[i])
                        {
                            lst = "            stream.Write(_" + str.Name.Replace("\"", "") + ");";
                            tmp.Add(lst);
                        }
                        lst = "";
                        tmp.Add(lst);
                        lst = "            return stream;";
                        tmp.Add(lst);
                        lst = "        }";
                        tmp.Add(lst);
                        lst = "    }";
                        tmp.Add(lst);
                        lst = "}";
                        tmp.Add(lst);
                        if (FilePath != null)
                        {
                            File.WriteAllLines(FilePath + nameSource + ".cs", tmp);
                        }
                        else
                        {
                            if (SavePktFileDialog(nameSource))
                            {
                                File.WriteAllLines(FilePathName, tmp);
                            }
                        }
                    }
                }
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor1.Background = Brushes.GreenYellow);
            }
            else
            {
                // Рефакторинг: используем UIHelper для показа информации
                Helpers.UIHelper.ShowInfo(Dispatcher, "Выберите любое имя пакета!", "Information");
            }
        }

        private void BtnMakePktOut_Click(object sender, RoutedEventArgs e)
        {
            FilePath = null;
            if (ListView22.SelectedItem != null)
            {
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.Yellow);
                var name = ListView22.SelectedItem.ToString();
                //GotoNameIn(name);
                if (ButtonSaveOut1.IsEnabled)
                {
                    // сохраняем в виде файла
                    for (var i = 0; i < ListNameDestinationCS.Count; i++)
                    {
                        // удалим опкоды в конце имени
                        string nameSource;
                        if (CheckBoxRemoveOpcode.IsChecked == true)
                        {
                            // удаляем оконечные опкоды в имени пакета
                            if (ListNameDestinationCS[i][0].ToString() != "o" || ListNameDestinationCS[i][1].ToString() != "f" || ListNameDestinationCS[i][2].ToString() != "f")
                            {
                                var offset = ListNameDestinationCS[i].LastIndexOf("_", StringComparison.Ordinal);
                                if (offset > 0)
                                {
                                    nameSource = ListNameDestinationCS[i].Substring(0, offset);
                                }
                                else
                                {
                                    nameSource = ListNameDestinationCS[i];
                                }
                            }
                            else
                            {
                                nameSource = ListNameDestinationCS[i];
                            }
                        }
                        else
                        {
                            nameSource = ListNameDestinationCS[i];
                        }

                        var tmp = new List<string>();
                        var lst = "";
                        lst = "using AAEmu.Commons.Network;";
                        tmp.Add(lst);
                        lst = "using AAEmu.Game.Core.Network.Game;";
                        tmp.Add(lst);
                        lst = "";
                        tmp.Add(lst);
                        lst = "namespace AAEmu.Game.Core.Packets.C2G";
                        tmp.Add(lst);
                        lst = "{";
                        tmp.Add(lst);
                        lst = "    public class " + nameSource + " : GamePacket";
                        tmp.Add(lst);
                        lst = "    {";
                        tmp.Add(lst);
                        lst = "        public " + nameSource + " : base(CSOffsets." + nameSource + ", 1)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);
                        lst = "        }";
                        tmp.Add(lst);
                        lst = "";
                        tmp.Add(lst);
                        lst = "        public override void Read(PacketStream stream)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);
                        foreach (var str in StructureDestinationCS[i])
                        {
                            switch (StructStringOut)
                            {
                                case "struct ver0.5":
                                    // Действия для выбранного варианта 1
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_05)str.Type + "();";
                                    break;
                                case "struct ver1.2+":
                                    // Действия для выбранного варианта 2
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_12)str.Type + "();";
                                    break;
                                case "struct ver6.0+":
                                    // Действия для выбранного варианта 3
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_60)str.Type + "();";
                                    break;
                                case "struct ver8.0+":
                                    // Действия для выбранного варианта 4
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_80)str.Type + "();";
                                    break;
                                default:
                                    lst = "            var " + str.Name.Replace("\"", "") + " = stream.Read" + (TypeEnum_80)str.Type + "();";
                                    break;
                            }
                            //if ( CheckBoxTypeEnumNewOut.IsChecked == true )
                            //{
                            //    lst = "            var " + str.Name.Replace( "\"", "" ) + " = stream.Read" + (TypeEnum_35)str.Type + "();";
                            //}
                            //else
                            //{
                            //    lst = "            var " + str.Name.Replace( "\"", "" ) + " = stream.Read" + (TypeEnum_12)str.Type + "();";
                            //}
                            tmp.Add(lst);
                        }
                        lst = "        }";
                        tmp.Add(lst);
                        lst = "    }";
                        tmp.Add(lst);
                        lst = "}";
                        tmp.Add(lst);
                        if (FilePath != null)
                        {
                            File.WriteAllLines(FilePath + nameSource + ".cs", tmp);
                        }
                        else
                        {
                            if (SavePktFileDialog(nameSource))
                            {
                                File.WriteAllLines(FilePathName, tmp);
                            }
                        }
                    }
                }
                else
                {
                    // сохраняем в виде файла
                    for (var i = 0; i < ListNameDestinationSC.Count; i++)
                    {
                        // удалим опкоды в конце имени
                        string nameSource;
                        if (CheckBoxRemoveOpcode.IsChecked == true)
                        {
                            // удаляем оконечные опкоды в имени пакета
                            if (ListNameDestinationSC[i][0].ToString() != "o" || ListNameDestinationSC[i][1].ToString() != "f" || ListNameDestinationSC[i][2].ToString() != "f")
                            {
                                var offset = ListNameDestinationSC[i].LastIndexOf("_", StringComparison.Ordinal);
                                if (offset > 0)
                                {
                                    nameSource = ListNameDestinationSC[i].Substring(0, offset);
                                }
                                else
                                {
                                    nameSource = ListNameDestinationSC[i];
                                }
                            }
                            else
                            {
                                nameSource = ListNameDestinationSC[i];
                            }
                        }
                        else
                        {
                            nameSource = ListNameDestinationSC[i];
                        }

                        var tmp = new List<string>();
                        var lst = "";
                        lst = "using AAEmu.Commons.Network;";
                        tmp.Add(lst);
                        lst = "using AAEmu.Game.Core.Network.Game;";
                        tmp.Add(lst);
                        lst = "";
                        tmp.Add(lst);
                        lst = "namespace AAEmu.Game.Core.Packets.G2C";
                        tmp.Add(lst);
                        lst = "{";
                        tmp.Add(lst);
                        lst = "    public class " + nameSource + " : GamePacket";
                        tmp.Add(lst);
                        lst = "    {";
                        tmp.Add(lst);
                        foreach (var str in StructureDestinationSC[i])
                        {
                            switch (StructStringOut)
                            {
                                case "struct ver0.5":
                                    // Действия для выбранного варианта 1
                                    lst = "        private readonly " + (TypeEnum_05)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                case "struct ver1.2+":
                                    // Действия для выбранного варианта 2
                                    lst = "        private readonly " + (TypeEnum_12)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                case "struct ver6.0+":
                                    // Действия для выбранного варианта 3
                                    lst = "        private readonly " + (TypeEnum_60)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                case "struct ver8.0+":
                                    // Действия для выбранного варианта 4
                                    lst = "        private readonly " + (TypeEnum_80)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                                default:
                                    lst = "        private readonly " + (TypeEnum_80)str.Type + " _" + str.Name.Replace("\"", "") + ";";
                                    break;
                            }
                            //if ( CheckBoxTypeEnumNewOut.IsChecked == true )
                            //{
                            //    lst = "        private readonly " + (TypeEnum_35)str.Type + " _" + str.Name.Replace( "\"", "" ) + ";";
                            //}
                            //else
                            //{
                            //    lst = "        private readonly " + (TypeEnum_12)str.Type + " _" + str.Name.Replace( "\"", "" ) + ";";
                            //}
                            tmp.Add(lst);
                        }
                        lst = "";
                        tmp.Add(lst);

                        lst = "        public " + nameSource + "(";
                        var li = StructureDestinationSC[i];
                        for (var j = 0; j < li.Count; j++)
                        {
                            switch (StructStringOut)
                            {
                                case "struct ver0.5":
                                    // Действия для выбранного варианта 1
                                    lst += "" + (TypeEnum_05)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                case "struct ver1.2+":
                                    // Действия для выбранного варианта 2
                                    lst += "" + (TypeEnum_12)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                case "struct ver6.0+":
                                    // Действия для выбранного варианта 3
                                    lst += "" + (TypeEnum_60)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                case "struct ver8.0+":
                                    // Действия для выбранного варианта 4
                                    lst += "" + (TypeEnum_80)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                                default:
                                    lst += "" + (TypeEnum_80)li[j].Type + " " + li[j].Name.Replace("\"", "");
                                    break;
                            }
                            //if ( CheckBoxTypeEnumNewOut.IsChecked == true )
                            //{
                            //    lst += "" + (TypeEnum_35)li[j].Type + " " + li[j].Name.Replace( "\"", "" );
                            //}
                            //else
                            //{
                            //    lst += "" + (TypeEnum_12)li[j].Type + " " + li[j].Name.Replace( "\"", "" );
                            //}
                            if (j < li.Count - 1)
                            {
                                lst += ", ";
                            }
                        }
                        lst += ") : base(SCOffsets." + nameSource + ", 1)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);

                        foreach (var str in StructureDestinationSC[i])
                        {
                            lst = "            _" + str.Name.Replace("\"", "") + " = " + str.Name.Replace("\"", "") + ";";
                            tmp.Add(lst);
                        }

                        lst = "        }";
                        tmp.Add(lst);

                        lst = "";
                        tmp.Add(lst);
                        lst = "        public override void Write(PacketStream stream)";
                        tmp.Add(lst);
                        lst = "        {";
                        tmp.Add(lst);
                        foreach (var str in StructureDestinationSC[i])
                        {
                            lst = "            stream.Write(_" + str.Name.Replace("\"", "") + ");";
                            tmp.Add(lst);
                        }
                        lst = "";
                        tmp.Add(lst);
                        lst = "            return stream;";
                        tmp.Add(lst);
                        lst = "        }";
                        tmp.Add(lst);
                        lst = "    }";
                        tmp.Add(lst);
                        lst = "}";
                        tmp.Add(lst);
                        if (FilePath != null)
                        {
                            File.WriteAllLines(FilePath + nameSource + ".cs", tmp);
                        }
                        else
                        {
                            if (SavePktFileDialog(nameSource))
                            {
                                File.WriteAllLines(FilePathName, tmp);
                            }
                        }
                    }
                }
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.GreenYellow);
            }
            else
            {
                // Рефакторинг: используем UIHelper для показа информации
                Helpers.UIHelper.ShowInfo(Dispatcher, "Выберите любое имя пакета!", "Information");
            }
        }

        private int loopIn = 0;
        private int prevIn = 0;
        private int currIn = 0;

        private void BtnGotoOpcodeIn_Click(object sender, RoutedEventArgs e)
        {
            if (ListView12.SelectedItem == null)
                return;

            var regexXREF = new Regex(@"^\s+;[a-zA-Z:\s]*\s(sub_\w+)|(X2\w+)|(w+)", RegexOptions.Compiled);
            // Рефакторинг: используем UIHelper для обновления UI
            Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor1.Background = Brushes.Yellow);
            var name = ListView12.SelectedItem.ToString();
            var idx = GotoNameIn(name);
            if (loopIn == 0)
            {
                loopIn = 1;
                prevIn = idx;
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor1.Background = Brushes.GreenYellow);
                return;
            }

            if (prevIn != idx)
            {
                loopIn = 1;
                prevIn = idx;
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor1.Background = Brushes.GreenYellow);
                return;
            }
            if (idx > 0)
            {
                switch (loopIn)
                {
                    case 1:
                        {
                            currIn = idx;
                            idx += loopIn;
                            // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                            var matchesXREF = regexXREF.Match(InListSource[idx]);
                            if (matchesXREF.Groups.Count <= 1)
                            {
                                prevIn = idx - loopIn;
                                loopIn = 0;
                                break;
                            }
                            var ss = "";
                            //group 1 = sub_\w+
                            if (matchesXREF.Groups[1].Length > 0)
                            {
                                ss = matchesXREF.Groups[1].ToString();
                                GotoNameIn(ss);
                            }
                            //group 2 = X2\w+
                            else if (matchesXREF.Groups[2].Length > 0)
                            {
                                ss = matchesXREF.Groups[2].ToString();
                                GotoNameIn(ss);
                            }
                            //group 3 = w+
                            else if (matchesXREF.Groups[3].Length > 0)
                            {
                                ss = matchesXREF.Groups[3].ToString();
                                GotoNameIn(ss);
                            }

                            loopIn = 2;
                            prevIn = currIn;

                            break;
                        }
                    case 2:
                        {
                            idx += loopIn;
                            // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                            var matchesXREF = regexXREF.Match(InListSource[idx]);
                            if (matchesXREF.Groups.Count <= 1)
                            {
                                prevIn = idx - loopIn;
                                loopIn = 1;
                                break;
                            }
                            var ss = "";
                            //group 1 = sub_\w+
                            if (matchesXREF.Groups[1].Length > 0)
                            {
                                ss = matchesXREF.Groups[1].ToString();
                                GotoNameIn(ss);
                            }
                            //group 2 = X2\w+
                            else if (matchesXREF.Groups[2].Length > 0)
                            {
                                ss = matchesXREF.Groups[2].ToString();
                                GotoNameIn(ss);
                            }
                            //group 3 = w+
                            else if (matchesXREF.Groups[3].Length > 0)
                            {
                                ss = matchesXREF.Groups[3].ToString();
                                GotoNameIn(ss);
                            }

                            loopIn = 0;
                            prevIn = currIn;
                            break;
                        }
                }
            }
            // Рефакторинг: используем UIHelper для обновления UI
            Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor1.Background = Brushes.GreenYellow);
        }

        private int GotoNameIn(string name)
        {
            Regex regex;

            var str = name;
            str = str.Replace("?", ".");
            str = str.Replace("@", ".");
            str = str.Replace("+", ".");
            if (loopIn == 0)
            {
                regex = new Regex(@"^" + str + @"\sdd\soffset", RegexOptions.Compiled);
            }
            else
            {
                regex = new Regex(@"^" + str, RegexOptions.Compiled);
            }
            for (var i = 0; i < InListSource.Count; i++)
            {
                var matches = regex.Matches(InListSource[i]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                ListView11.SelectedIndex = i;
                ListView11.UpdateLayout();
                ListView11.ScrollIntoView(ListView11.SelectedItem);
                return i;
            }
            return 0;
        }

        private void BtnGotoNameOut_Click(object sender, RoutedEventArgs e)
        {
            if (ListView22.SelectedItem != null)
            {
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.Yellow);
                var name = ListView22.SelectedItem.ToString();
                GotoNameOut(name);
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.GreenYellow);
            }
        }

        private int loopOut = 0;
        private int prevOut = 0;
        private int currOut = 0;
        private void BtnGotoOpcodeOut_Click(object sender, RoutedEventArgs e)
        {
            if (ListView22.SelectedItem == null)
                return;

            var regexXREF = new Regex(@"^\s+;[a-zA-Z:\s]*\s(sub_\w+)|(X2\w+)|(w+)", RegexOptions.Compiled);
            // Рефакторинг: используем UIHelper для обновления UI
            Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.Yellow);
            var name = ListView22.SelectedItem.ToString();
            var idx = GotoNameOut(name);
            if (loopOut == 0)
            {
                loopOut = 1;
                prevOut = idx;
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.GreenYellow);
                return;
            }
            else if (prevOut != idx)
            {
                loopOut = 1;
                prevOut = idx;
                // Рефакторинг: используем UIHelper для обновления UI
                Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.GreenYellow);
                return;
            }
            if (idx > 0)
            {
                switch (loopOut)
                {
                    case 1:
                        {
                            currOut = idx;
                            idx += loopOut;
                            // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                            var matchesXREF = regexXREF.Match(InListDestination[idx]);
                            if (matchesXREF.Groups.Count <= 1)
                            {
                                prevOut = idx - loopOut;
                                loopOut = 0;
                                break;
                            }
                            var ss = "";
                            //group 1 = sub_\w+
                            if (matchesXREF.Groups[1].Length > 0)
                            {
                                ss = matchesXREF.Groups[1].ToString();
                                GotoNameOut(ss);
                            }
                            //group 2 = X2\w+
                            else if (matchesXREF.Groups[2].Length > 0)
                            {
                                ss = matchesXREF.Groups[2].ToString();
                                GotoNameOut(ss);
                            }
                            //group 3 = w+
                            else if (matchesXREF.Groups[3].Length > 0)
                            {
                                ss = matchesXREF.Groups[3].ToString();
                                GotoNameOut(ss);
                            }

                            loopOut = 2;
                            prevOut = currOut;

                            break;
                        }
                    case 2:
                        {
                            idx += loopOut;
                            // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                            var matchesXREF = regexXREF.Match(InListDestination[idx]);
                            if (matchesXREF.Groups.Count <= 1)
                            {
                                prevOut = idx - loopOut;
                                loopOut = 1;
                                break;
                            }
                            var ss = "";
                            //group 1 = sub_\w+
                            if (matchesXREF.Groups[1].Length > 0)
                            {
                                ss = matchesXREF.Groups[1].ToString();
                                GotoNameOut(ss);
                            }
                            //group 2 = X2\w+
                            else if (matchesXREF.Groups[2].Length > 0)
                            {
                                ss = matchesXREF.Groups[2].ToString();
                                GotoNameOut(ss);
                            }
                            //group 3 = w+
                            else if (matchesXREF.Groups[3].Length > 0)
                            {
                                ss = matchesXREF.Groups[3].ToString();
                                GotoNameOut(ss);
                            }

                            loopOut = 0;
                            prevOut = currOut;
                            break;
                        }
                }
            }
            // Рефакторинг: используем UIHelper для обновления UI
            Helpers.UIHelper.InvokeUI(Dispatcher, () => Label_Semafor2.Background = Brushes.GreenYellow);
        }
        private int GotoNameOut(string name)
        {
            Regex regex;

            var str = name;
            str = str.Replace("?", ".");
            str = str.Replace("@", ".");
            str = str.Replace("+", ".");
            if (loopOut == 0)
            {
                regex = new Regex(@"^" + str + @"\sdd\soffset", RegexOptions.Compiled);
            }
            else
            {
                regex = new Regex(@"^" + str, RegexOptions.Compiled);
            }
            for (var i = 0; i < InListDestination.Count; i++)
            {
                var matches = regex.Matches(InListDestination[i]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                ListView21.SelectedIndex = i;
                ListView21.UpdateLayout();
                ListView21.ScrollIntoView(ListView21.SelectedItem);
                return i;
            }
            return 0;
        }

        private void CheckBoxLock_Checked(object sender, RoutedEventArgs e)
        {
            if (ButtonCsCompare.IsEnabled)
            {
                isCompareCS = CheckBoxLock.IsChecked == true;
            }
            else
            {
                isCompareSC = CheckBoxLock.IsChecked == true;
            }
        }

        private void RadioButton_Checked_In(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton.RadioButton;
            if (radioButton != null && radioButton.IsChecked == true)
            {
                // Доступ к выбранному варианту
                var selectedOption = radioButton.Content.ToString();

                // Здесь вы можете выполнить нужные действия, в зависимости от выбранного варианта
                // Например:
                StructStringIn = selectedOption;

                //switch (selectedOption)
                //{
                //    case "struct ver0.5":
                //        // Действия для выбранного варианта 1
                //        StructStringIn = selectedOption;
                //        break;
                //    case "struct ver1.2+":
                //        // Действия для выбранного варианта 2
                //        StructStringIn = selectedOption;
                //        break;
                //    case "struct ver6.0+":
                //        // Действия для выбранного варианта 3
                //        StructStringIn = selectedOption;
                //        break;
                //    case "struct ver8.0+":
                //        // Действия для выбранного варианта 4
                //        StructStringIn = selectedOption;
                //        break;
                //    default:
                //        StructStringIn = selectedOption;
                //        break;
                //}
            }
        }

        private void RadioButton_Checked_Out(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton.RadioButton;
            if (radioButton != null && radioButton.IsChecked == true)
            {
                // Доступ к выбранному варианту
                var selectedOption = radioButton.Content.ToString();

                // Здесь вы можете выполнить нужные действия, в зависимости от выбранного варианта
                // Например:
                StructStringOut = selectedOption;
                //switch (selectedOption)
                //{
                //    case "struct ver0.5":
                //        // Действия для выбранного варианта 1
                //        StructStringOut = "struct ver0.5";
                //        break;
                //    case "struct ver1.2+":
                //        // Действия для выбранного варианта 2
                //        StructStringOut = "struct ver1.2+";
                //        break;
                //    case "struct ver6.0+":
                //        // Действия для выбранного варианта 3
                //        StructStringOut = "struct ver6.0+";
                //        break;
                //    case "struct ver8.0+":
                //        // Действия для выбранного варианта 4
                //        StructStringOut = "struct ver8.0+";
                //        break;
                //    default:
                //        StructStringOut = "struct ver8.0+";
                //        break;
                //}
            }
        }
    }
}
