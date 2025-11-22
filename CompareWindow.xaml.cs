using System;
using Microsoft.Win32;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NameFinder
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class CompareWindow : Window
    {
        private readonly MainWindow _mainWindow;

        public int IdxS { get; set; }
        public int IdxD { get; set; }
        public bool isSourceNameChanged { get; set; }
        public bool isDestinationNameChanged { get; set; }
        public bool isResetOpcode { get; set; }

        public CompareWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            
            // Центрируем окно на экране
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public static ObservableCollection<string> ListNameCompare = new ObservableCollection<string>();
        public static ObservableCollection<string> ListNameSource = new ObservableCollection<string>();
        public static ObservableCollection<string> ListNameDestination = new ObservableCollection<string>();
        public static ObservableCollection<string> ListSubDestination = new ObservableCollection<string>();
        public static Dictionary<int, ObservableCollection<Struc>> StructureSource = new Dictionary<int, ObservableCollection<Struc>>();
        public static Dictionary<int, ObservableCollection<Struc>> StructureDestination = new Dictionary<int, ObservableCollection<Struc>>();
        public static ObservableCollection<string> ListOpcodeDestination = new ObservableCollection<string>();
        public static bool isRemoveOpcode = false;
        public static string StructStringIn = "";
        public static string StructStringOut = "";


        public void CompareSourceStructures(
            ObservableCollection<string> listNameSource,
            ObservableCollection<string> listNameDestination,
            ObservableCollection<string> listNameCompare,
            ObservableCollection<string> listSubDestination,
            Dictionary<int, ObservableCollection<Struc>> structureSource,
            Dictionary<int, ObservableCollection<Struc>> structureDestination,
            ObservableCollection<string> listOpcodeDestination)
        {
            // Логирование полученных параметров
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.CompareSourceStructures: Получены параметры. listNameSource.Count={listNameSource?.Count ?? 0}, listNameDestination.Count={listNameDestination?.Count ?? 0}, listNameCompare.Count={listNameCompare?.Count ?? 0}, listSubDestination.Count={listSubDestination?.Count ?? 0}");
            
            if (listNameSource == null || listNameSource.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.CompareSourceStructures: listNameSource пуст или null!");
            }
            if (listNameDestination == null || listNameDestination.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.CompareSourceStructures: listNameDestination пуст или null!");
            }
            if (listNameCompare == null || listNameCompare.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.CompareSourceStructures: listNameCompare пуст или null!");
            }
            if (listSubDestination == null || listSubDestination.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.CompareSourceStructures: listSubDestination пуст или null!");
            }
            
            // ВАЖНО: Работаем напрямую с переданными ObservableCollection - передаем ссылки, не копии!
            // Это позволяет автоматически обновлять UI в MainWindow при изменении данных
            ListNameCompare = listNameCompare;
            ListNameSource = listNameSource;
            ListNameDestination = listNameDestination;
            ListSubDestination = listSubDestination; // Передаем ссылку на ObservableCollection
            StructureSource = structureSource; // Передаем ссылку на Dictionary с ObservableCollection
            StructureDestination = structureDestination; // Передаем ссылку на Dictionary с ObservableCollection
            ListOpcodeDestination = listOpcodeDestination;
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.CompareSourceStructures: Используются ссылки на ObservableCollection. ListNameSource.Count={ListNameSource.Count}, ListNameDestination.Count={ListNameDestination.Count}, ListNameCompare.Count={ListNameCompare.Count}, ListSubDestination.Count={ListSubDestination.Count}");

            // начнем с начала
            IdxD = 0;
            IdxS = 0;
            isSourceNameChanged = false;
            isResetOpcode = false;
            isDestinationNameChanged = false;
            ShowList();
        }

        private void BtnNextCsIn_Click(object sender, RoutedEventArgs e)
        {
            IdxS++; //взять следующий пакет
            ShowList();
        }

        private void BtnNextL_Click(object sender, RoutedEventArgs e)
        {
            IdxS--; //взять следующий пакет
            IdxD--; //взять следующий пакет
            ShowList();
        }

        private void BtnNextR_Click(object sender, RoutedEventArgs e)
        {
            IdxS++; //взять следующий пакет
            IdxD++; //взять следующий пакет
            ShowList();
        }

        private void BtnNextCsOut_Click(object sender, RoutedEventArgs e)
        {
            IdxD++; //взять следующий пакет
            ShowList();
        }

        private void BtnPrevCsIn_Click(object sender, RoutedEventArgs e)
        {
            IdxS--; //взять предыдущий пакет
            ShowList();
        }

        private void BtnPrevCsOut_Click(object sender, RoutedEventArgs e)
        {
            IdxD--; //взять предыдущий пакет
            ShowList();
        }

        private void BtnAddNameCs_Click(object sender, RoutedEventArgs e)
        {
            // проверим что имя не используется
            var offset = 0;
            // Рефакторинг: используем ссылку на экземпляр MainWindow вместо статического доступа
            if (_mainWindow.InUseIn.TryGetValue(IdxS, out var useOut))
            {
                _mainWindow.InUseOut.Remove(useOut);
                ListNameCompare[useOut] = ListNameDestination[useOut];
            }
            if (_mainWindow.InUseOut.TryGetValue(IdxD, out var useIn))
            {
                _mainWindow.InUseIn.Remove(useIn);
            }
            if (_mainWindow.InUseIn.ContainsKey(IdxS))
            {
                _mainWindow.InUseIn[IdxS] = IdxD;
            }
            else
            {
                _mainWindow.InUseIn.Add(IdxS, IdxD); // отметим, что найденное имя занято
            }

            if (_mainWindow.InUseOut.ContainsKey(IdxD))
            {
                _mainWindow.InUseOut[IdxD] = IdxS;
            }
            else
            {
                _mainWindow.InUseOut.Add(IdxD, IdxS); // отметим, что найденное имя занято
            }

            // запишем новое имя на место неизвестного, которое нашли
            if (isRemoveOpcode)
            {
                // удаляем оконечные опкоды в имени пакета
                offset = ListNameSource[IdxS].LastIndexOf("_", StringComparison.Ordinal);
                if (offset > 3)
                {
                    var nameSource = ListNameSource[IdxS].Substring(0, offset);
                    ListNameCompare[IdxD] = nameSource;
                }
                else
                {
                    // переименовываем имена пакетов
                    if (ListNameSource[IdxS][0].ToString() == "o" ||
                        ListNameSource[IdxS][1].ToString() == "f" ||
                        ListNameSource[IdxS][2].ToString() == "f")
                    {
                        // не переименовываем если имя начинается off_
                        ListNameCompare[IdxD] = ListNameDestination[IdxD];
                    }
                    else
                    {
                        // не переименовываем, если имя содержит Unknown
                        offset = ListNameSource[IdxS].IndexOf("unknown", StringComparison.OrdinalIgnoreCase);
                        if (offset == -1)
                        {
                            // переименовываем имена пакетов
                            ListNameCompare[IdxD] = ListNameSource[IdxS];
                        }
                        else
                        {
                            // не переименовываем, если имя содержит Unknown
                            ListNameCompare[IdxD] = ListNameDestination[IdxD];
                        }
                    }
                }
            }
            else
            {
                if (ListNameSource[IdxS][0].ToString() == "o" ||
                    ListNameSource[IdxS][1].ToString() == "f" ||
                    ListNameSource[IdxS][2].ToString() == "f")
                {
                    // не переименовываем если имя начинается off_
                    ListNameCompare[IdxD] = ListNameDestination[IdxD];
                }
                else
                {
                    // не переименовываем, если имя содержит Unknown
                    offset = ListNameSource[IdxS].IndexOf("unknown", StringComparison.OrdinalIgnoreCase);
                    if (offset == -1)
                    {
                        // переименовываем имена пакетов
                        ListNameCompare[IdxD] = ListNameSource[IdxS];
                    }
                    else
                    {
                        // не переименовываем, если имя содержит Unknown
                        ListNameCompare[IdxD] = ListNameDestination[IdxD];
                    }
                }
            }

            // удаляем оконечные опкоды в имени пакета
            if (CheckBoxRemoveOpcode.IsChecked == true)
            {
                offset = ListNameCompare[IdxD].LastIndexOf("_", StringComparison.Ordinal);
                if (offset > 3)
                {
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(0, offset);
                }
            }

            //  ToTitleCase
            if (CheckBoxToTitleCase.IsChecked == true)
            {
                if (ListNameCompare[IdxD][0].ToString() != "o" ||
                    ListNameCompare[IdxD][1].ToString() != "f" ||
                    ListNameCompare[IdxD][2].ToString() != "f")
                {
                    // удаляем CS|SC только в начале имени
                    RemoveCSSC();
                    ListNameCompare[IdxD] = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ListNameCompare[IdxD].ToLower());
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Replace("_", "");
                    // удаляем "On" только в начале имени
                    offset = ListNameCompare[IdxD].LastIndexOf("On", StringComparison.Ordinal);
                    if (offset == 0)
                    {
                        ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(2, ListNameCompare[IdxD].Length - 2);
                    }
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Replace("_", "");
                    // добавим CS|SC в начале имени
                    AddCSSC();
                }
            }
            // Remove CS|SC & Packet & @@6B@ in ver.0.5.1
            if (CheckBoxAdd.IsChecked == true)
            {
                if (ListNameCompare[IdxD][0].ToString() != "o" ||
                    ListNameCompare[IdxD][1].ToString() != "f" ||
                    ListNameCompare[IdxD][2].ToString() != "f")
                {
                    // удаляем CS|SC только в начале имени
                    RemoveCSSC();
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Replace("@@6B@", "");
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Replace("PACKET", "");
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Replace("Packet", "");
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Replace("packet", "");
                    // удаляем "On" только в начале имени
                    offset = ListNameCompare[IdxD].LastIndexOf("On", StringComparison.Ordinal);
                    if (offset == 0)
                    {
                        ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(2, ListNameCompare[IdxD].Length - 2);
                    }
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Replace("_", "");
                    // добавим CS|SC в начале имени
                    AddCSSC();
                }
            }

            // Добавим 'Packet' в конец имени пакета
            if (CheckBoxAdd.IsChecked == true)
            {
                if (ListNameCompare[IdxD][0].ToString() != "o" ||
                    ListNameCompare[IdxD][1].ToString() != "f" ||
                    ListNameCompare[IdxD][2].ToString() != "f")
                {
                    // удаляем CS|SC только в начале имени
                    RemoveCSSC();
                    offset = ListNameCompare[IdxD].LastIndexOf("Packet", StringComparison.OrdinalIgnoreCase);
                    if (offset <= 0)
                    {
                        ListNameCompare[IdxD] += "Packet";
                    }
                    else
                    {
                        ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(0, offset);
                        ListNameCompare[IdxD] += "Packet";
                    }
                    // добавим CS|SC в начале имени
                    AddCSSC();
                }
            }

            // отобразим на форме результаты
            ShowList();
        }

        private void AddCSSC()
        {
            // добавим CS|SC в начале имени
            if (MainWindow.isCS)
            {
                if (ListNameCompare[IdxD][0].ToString() != "C" ||
                    ListNameCompare[IdxD][1].ToString() != "S")
                {
                    if (ListNameCompare[IdxD][0].ToString() != "X" ||
                        ListNameCompare[IdxD][1].ToString() != "2")
                    {
                        ListNameCompare[IdxD] = "CS" + ListNameCompare[IdxD];
                    }
                }
            }
            else
            {
                if (ListNameCompare[IdxD][0].ToString() != "S" ||
                    ListNameCompare[IdxD][1].ToString() != "C")
                {
                    if (ListNameCompare[IdxD][0].ToString() != "X" ||
                        ListNameCompare[IdxD][1].ToString() != "2")
                    {
                        ListNameCompare[IdxD] = "SC" + ListNameCompare[IdxD];
                    }
                }
            }
        }

        private void RemoveCSSC()
        {
            int offset;
            if (MainWindow.isCS)
            {
                // удаляем ??_7 только в начале имени
                offset = ListNameCompare[IdxD].IndexOf("??_7", StringComparison.OrdinalIgnoreCase);
                if (offset == 0)
                {
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(4, ListNameCompare[IdxD].Length - 4);
                }
            }
            else
            {
                // удаляем ??_7 только в начале имени
                offset = ListNameCompare[IdxD].IndexOf("??_7", StringComparison.OrdinalIgnoreCase);
                if (offset == 0)
                {
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(4, ListNameCompare[IdxD].Length - 4);
                }
            }
            if (MainWindow.isCS)
            {
                // удаляем CS|SC только в начале имени
                offset = ListNameCompare[IdxD].IndexOf("cs", StringComparison.OrdinalIgnoreCase);
                if (offset == 0)
                {
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(2, ListNameCompare[IdxD].Length - 2);
                }
            }
            else
            {
                // удаляем CS|SC только в начале имени
                offset = ListNameCompare[IdxD].IndexOf("sc", StringComparison.OrdinalIgnoreCase);
                if (offset == 0)
                {
                    ListNameCompare[IdxD] = ListNameCompare[IdxD].Substring(2, ListNameCompare[IdxD].Length - 2);
                }
            }
        }

        private void ShowList()
        {
            // Проверка на пустые списки
            if (ListNameSource == null || ListNameSource.Count == 0)
            {
                TextBoxNameIn.Text = "";
                TextBoxTotalIn.Text = "0";
                TextBoxCurPktIn.Text = "0";
                ListView11.ItemsSource = null;
                return;
            }

            if (ListNameCompare == null || ListNameCompare.Count == 0)
            {
                TextBoxNameOut.Text = "";
                TextBoxTotalOut.Text = "0";
                TextBoxOpcodeOut.Text = "";
                TextBoxCurPktOut.Text = "0";
                ListView21.ItemsSource = null;
                return;
            }

            if (ListOpcodeDestination == null || ListOpcodeDestination.Count == 0)
            {
                TextBoxOpcodeOut.Text = "";
            }

            // Корректировка индексов
            if (IdxS >= ListNameSource.Count)
            {
                IdxS = 0;
            }

            if (IdxD >= ListNameCompare.Count)
            {
                IdxD = 0;
            }

            if (IdxS < 0)
            {
                IdxS = ListNameSource.Count > 0 ? ListNameSource.Count - 1 : 0;
            }

            if (IdxD < 0)
            {
                IdxD = ListNameCompare.Count > 0 ? ListNameCompare.Count - 1 : 0;
            }

            // Проверка границ перед обращением к элементам
            if (IdxS < 0 || IdxS >= ListNameSource.Count)
            {
                TextBoxNameIn.Text = "";
                TextBoxTotalIn.Text = ListNameSource.Count.ToString();
                TextBoxCurPktIn.Text = "0";
                ListView11.ItemsSource = null;
                return;
            }

            TextBoxNameIn.Text = ListNameSource[IdxS];
            TextBoxTotalIn.Text = ListNameSource.Count.ToString();
            var idxs = IdxS + 1;
            TextBoxCurPktIn.Text = idxs.ToString();
            
            // Проверка наличия ключа в словаре перед обращением
            if (!StructureSource.ContainsKey(IdxS))
            {
                ListView11.ItemsSource = "nullsub";
            }
            else if (StructureSource[IdxS].Count == 0)
            {
                ListView11.ItemsSource = "nullsub";
            }
            else
            {
                var lis = StructureSource[IdxS];
                var source = new List<string>();
                foreach (var li in lis)
                {
                    switch (StructStringIn)
                    {
                        case "struct ver0.5":
                            // Действия для выбранного варианта 1
                            source.Add((TypeEnum_05)li.Type + " " + li.Name);
                            break;
                        case "struct ver1.2+":
                            // Действия для выбранного варианта 2
                            source.Add((TypeEnum_12)li.Type + " " + li.Name);
                            break;
                        case "struct ver6.0+":
                            // Действия для выбранного варианта 3
                            source.Add((TypeEnum_60)li.Type + " " + li.Name);
                            break;
                        case "struct ver8.0+":
                            // Действия для выбранного варианта 4
                            source.Add((TypeEnum_80)li.Type + " " + li.Name);
                            break;
                        default:
                            source.Add((TypeEnum_80)li.Type + " " + li.Name);
                            break;
                    }
                    //if (MainWindow.isTypeEnumNewIn)
                    //{
                    //    source.Add((TypeEnum_35)li.Type + " " + li.Name);
                    //}
                    //else
                    //{
                    //    source.Add((TypeEnum_12)li.Type + " " + li.Name);
                    //}
                }
                ListView11.ItemsSource = source.ToList();
            }

            // Проверка границ перед обращением к элементам
            if (IdxD < 0 || IdxD >= ListNameCompare.Count)
            {
                TextBoxNameOut.Text = "";
                TextBoxTotalOut.Text = ListNameCompare.Count.ToString();
                TextBoxOpcodeOut.Text = "";
                TextBoxCurPktOut.Text = "0";
                ListView21.ItemsSource = null;
                return;
            }

            TextBoxNameOut.Text = ListNameCompare[IdxD];
            TextBoxTotalOut.Text = ListNameCompare.Count.ToString();
            
            // Проверка границ для ListOpcodeDestination
            if (ListOpcodeDestination != null && IdxD < ListOpcodeDestination.Count)
            {
                TextBoxOpcodeOut.Text = ListOpcodeDestination[IdxD];
            }
            else
            {
                TextBoxOpcodeOut.Text = "";
            }
            
            var idxd = IdxD + 1;
            TextBoxCurPktOut.Text = idxd.ToString();

            // Проверка наличия ключа в словаре перед обращением
            if (!StructureDestination.ContainsKey(IdxD))
            {
                ListView21.ItemsSource = "nullsub";
            }
            else if (StructureDestination[IdxD].Count == 0)
            {
                ListView21.ItemsSource = "nullsub";
            }
            else
            {
                var lis = StructureDestination[IdxD];
                var source = new List<string>();
                foreach (var li in lis)
                {
                    switch (StructStringOut)
                    {
                        case "struct ver0.5":
                            // Действия для выбранного варианта 1
                            source.Add((TypeEnum_05)li.Type + " " + li.Name);
                            break;
                        case "struct ver1.2+":
                            // Действия для выбранного варианта 2
                            source.Add((TypeEnum_12)li.Type + " " + li.Name);
                            break;
                        case "struct ver6.0+":
                            // Действия для выбранного варианта 3
                            source.Add((TypeEnum_60)li.Type + " " + li.Name);
                            break;
                        case "struct ver8.0+":
                            // Действия для выбранного варианта 4
                            source.Add((TypeEnum_80)li.Type + " " + li.Name);
                            break;
                        default:
                            source.Add((TypeEnum_80)li.Type + " " + li.Name);
                            break;
                    }
                    //if (MainWindow.isTypeEnumNewOut)
                    //{
                    //    source.Add((TypeEnum_35)li.Type + " " + li.Name);
                    //}
                    //else
                    //{
                    //    source.Add((TypeEnum_12)li.Type + " " + li.Name);
                    //}
                }
                ListView21.ItemsSource = source.ToList();
            }

            // проверим, что имя не занято
            // Рефакторинг: используем ссылку на экземпляр MainWindow вместо статического доступа
            if (_mainWindow.InUseIn.TryGetValue(IdxS, out var value))
            {
                checkBoxInUse.IsChecked = true;
                var idxs2 = value + 1;
                TextBoxPktInUse.Text = idxs2.ToString();
            }
            else
            {
                checkBoxInUse.IsChecked = false;
                TextBoxPktInUse.Text = "0";
            }

            if (_mainWindow.InUseOut.TryGetValue(IdxD, out var value1))
            {
                checkBoxOutUse.IsChecked = true;
                var idxd2 = value1 + 1;
                TextBoxPktOutUse.Text = idxd2.ToString();
            }
            else
            {
                checkBoxOutUse.IsChecked = false;
                TextBoxPktOutUse.Text = "0";
            }
        }

        private void TextBox21_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ListNameCompare.Count != 0)
            {
                if (MainWindow.ListNameCompare.Count == 0)
                {
                    MainWindow.ListNameCompare = new ObservableCollection<string>(ListNameCompare);
                }
                MainWindow.ListNameCompare[IdxD] = TextBoxNameOut.Text;
                ListNameCompare[IdxD] = TextBoxNameOut.Text;
                isDestinationNameChanged = true;
            }
        }

        private void TextBox11_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ListNameSource.Count != 0)
            {
                ListNameSource[IdxS] = TextBoxNameIn.Text;
            }
        }

        private void ButtonIn1_Click(object sender, RoutedEventArgs e)
        {
            var tmp = new List<string>();
            for (var i = 0; i < ListNameCompare.Count; i++)
            {
                if (ListOpcodeDestination.Count > 0)
                {
                    var lst = "Packet name: " + ListNameCompare[i] + ", PacketBodyReader: " + ListSubDestination[i] + ", Opcode: " + ListOpcodeDestination[i];
                    tmp.Add(lst);
                }
                else
                {
                    var lst = "Packet name: " + ListNameCompare[i] + ", PacketBodyReader: " + ListSubDestination[i] + ", Opcode: 0xfff";
                    tmp.Add(lst);
                }
            }

            if (SaveFileDialog3())
            {
                File.WriteAllLines(FilePath, tmp);
            }


            // сохраняем в виде файла для замены имен пакетов в IDA
            tmp = new List<string>();
            for (var i = 0; i < ListNameDestination.Count; i++)
            {
                if (ListNameCompare[i][0].ToString() == "o" && ListNameCompare[i][1].ToString() == "f" && ListNameCompare[i][2].ToString() == "f")
                {
                    continue;
                }

                var lst = ListNameDestination[i] + " " + ListNameCompare[i].Replace("Packet", "") + "_" + ListOpcodeDestination[i];
                tmp.Add(lst);
            }
            File.WriteAllLines(FilePath + "_IDA.txt", tmp);

            // сохраняем в виде файла для внесения опкодов в PDEC
            tmp = new List<string>();
            for (var i = 0; i < ListNameCompare.Count; i++)
            {
                if (ListOpcodeDestination.Count > 0)
                {
                    var nameCompare = "";
                    var lst = "";
                    if (ListNameCompare[i][0].ToString() != "o" || ListNameCompare[i][1].ToString() != "f" || ListNameCompare[i][2].ToString() != "f")
                    {
                        nameCompare = ListNameCompare[i].Replace("Packet", "");
                    }
                    else
                    {
                        nameCompare = ListNameCompare[i];
                    }
                    // добавим проверку, что опкод меньше 0x1F
                    int.TryParse(ListOpcodeDestination[i].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var number);
                    if (number < 32)
                    {
                        lst = "        <packet type=\"" + ListOpcodeDestination[i] + "\" level=\"0x05\" desc=\"" + nameCompare + "\">";
                    }
                    else
                    {
                        lst = "        <packet type=\"" + ListOpcodeDestination[i] + "\" desc=\"" + nameCompare + "\">";
                    }
                    tmp.Add(lst);
                }
                else
                {
                    break;
                }
            }

            File.WriteAllLines(FilePath + "_PDEC.txt", tmp);

            if (FilePath == null)
            {
                return; // выходим, если нажали Cancel в выборе имени файла
            }
            // сохраняем в виде файла для внесения опкодов в AAEMU
            var offset = FilePath.LastIndexOf("\\", StringComparison.Ordinal) + 1;
            var name = FilePath.Substring(offset);
            name = name.Substring(0, name.Length - 3);
            tmp = new List<string>();
            if (name == "CSOffsets")
            {
                tmp.Add("namespace AAEmu.Game.Core.Packets.C2G");
            }
            else if (name == "SCOffsets")
            {
                tmp.Add("namespace AAEmu.Game.Core.Packets.G2C");
            }
            else
            {
                tmp.Add("namespace AAEmu.Game.Core.Packets.");
            }
            tmp.Add("{");
            tmp.Add("    public static class " + name);
            tmp.Add("    {");
            tmp.Add("        // All opcodes here are updated for version client_XX_rXXXXXX");

            for (var i = 0; i < ListNameCompare.Count; i++)
            {
                if (ListOpcodeDestination.Count > 0)
                {
                    var lst = "        public const ushort " + ListNameCompare[i] + " = " + ListOpcodeDestination[i] + ";";
                    tmp.Add(lst);
                }
                else
                {
                    break;
                }
            }
            tmp.Add("    }");
            tmp.Add("}");

            File.WriteAllLines(FilePath + "_AAEMU.cs", tmp);
        }

        private string FilePath { get; set; }

        public bool SaveFileDialog3()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Offsets File|*.cs",
                FileName = "Offsets.cs",
                Title = "Save As Text File"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                FilePath = saveFileDialog.FileName;
                return true;
            }

            return false;
        }

        private void ButtonQuit_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.ButtonQuit_Click: Начало. isSourceNameChanged={isSourceNameChanged}, isDestinationNameChanged={isDestinationNameChanged}, isResetOpcode={isResetOpcode}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.ButtonQuit_Click: ListNameSource.Count={ListNameSource.Count}, ListNameCompare.Count={ListNameCompare.Count}, ListOpcodeDestination.Count={ListOpcodeDestination.Count}");
            
            // ВАЖНО: Теперь мы работаем напрямую с ObservableCollection из MainWindow
            // Изменения уже применены к коллекциям, так как мы работали со ссылками
            // Нужно только обновить флаги и обновить ListNameCompareOut, если нужно
            
            if (isSourceNameChanged)
            {
                // Изменения уже применены к ObservableCollection, UI обновится автоматически
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.ButtonQuit_Click: isSourceNameChanged=True. Изменения уже применены к ObservableCollection.");
            }
            if (isDestinationNameChanged)
            {
                // Изменения уже применены к ObservableCollection, UI обновится автоматически
                // Но нужно обновить ListNameCompareOut, если он используется
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.ButtonQuit_Click: isDestinationNameChanged=True. Изменения уже применены к ObservableCollection. Count={ListNameCompare.Count}");
            }
            if (isResetOpcode)
            {
                // Изменения уже применены к ObservableCollection, UI обновится автоматически
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.ButtonQuit_Click: isResetOpcode=True. Изменения уже применены к ObservableCollection. Count={ListOpcodeDestination.Count}");
            }

            // Обновляем статический ListNameCompare для обратной совместимости
            MainWindow.ListNameCompare = new ObservableCollection<string>(ListNameCompare);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {System.DateTime.Now:HH:mm:ss.fff} CompareWindow.ButtonQuit_Click: Обновлен MainWindow.ListNameCompare. Count={MainWindow.ListNameCompare.Count}");

            Close();
        }

        private void BtnClearName_Click(object sender, RoutedEventArgs e)
        {
            // Рефакторинг: используем ссылку на экземпляр MainWindow вместо статического доступа
            if (!_mainWindow.InUseOut.ContainsKey(IdxD))
            {
                return;
            }

            var useIn = _mainWindow.InUseOut[IdxD];
            TextBoxNameOut.Text = ListNameDestination[IdxD];
            TextBoxTotalOut.Text = ListNameDestination.Count.ToString();

            ListNameCompare[IdxD] = ListNameDestination[IdxD];
            _mainWindow.InUseOut.Remove(IdxD);
            _mainWindow.InUseIn.Remove(useIn);

            // проверим что на панели нужное имя
            if (useIn == IdxS)
            {
                // проверим, что имя не занято
                // Рефакторинг: используем ссылку на экземпляр MainWindow вместо статического доступа
                if (_mainWindow.InUseIn.TryGetValue(IdxS, out var value))
                {
                    checkBoxInUse.IsChecked = true;
                    var idxs2 = value + 1;
                    TextBoxPktInUse.Text = idxs2.ToString();
                }
                else
                {
                    checkBoxInUse.IsChecked = false;
                    TextBoxPktInUse.Text = "0";
                }
            }

            // Рефакторинг: используем ссылку на экземпляр MainWindow вместо статического доступа
            if (_mainWindow.InUseOut.TryGetValue(IdxD, out var value1))
            {
                checkBoxOutUse.IsChecked = true;
                var idxd2 = value1 + 1;
                TextBoxPktOutUse.Text = idxd2.ToString();
            }
            else
            {
                checkBoxOutUse.IsChecked = false;
                TextBoxPktOutUse.Text = "0";
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            // Рефакторинг: используем ссылку на экземпляр MainWindow вместо статического доступа
            if (!_mainWindow.InUseIn.ContainsKey(IdxS))
            {
                return;
            }

            var useOut = _mainWindow.InUseIn[IdxS];
            ListNameCompare[useOut] = ListNameDestination[useOut];

            // проверим что на панели нужное имя
            if (useOut == IdxD)
            {
                // проверим, что имя не занято
                if (_mainWindow.InUseOut.ContainsKey(useOut))
                {
                    checkBoxOutUse.IsChecked = false;
                    TextBoxPktOutUse.Text = "0";
                    ListNameCompare[useOut] = ListNameDestination[useOut]; // восстановим старое имя пакета
                    TextBoxNameOut.Text = ListNameDestination[useOut];
                    TextBoxTotalOut.Text = ListNameDestination.Count.ToString();
                }
            }
            checkBoxInUse.IsChecked = false;
            TextBoxPktInUse.Text = "0";

            _mainWindow.InUseOut.Remove(useOut);
            _mainWindow.InUseIn.Remove(IdxS);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var idxd = TextBoxCurPktOut.Text;
            IdxD = Convert.ToInt32(idxd) - 1;
            if (IdxD < 0)
            {
                IdxD = 0;
            }
            if (IdxD > ListNameDestination.Count - 1)
            {
                IdxD = ListNameDestination.Count - 1;
            }
            ShowList();
        }

        private void button1_Copy_Click(object sender, RoutedEventArgs e)
        {
            var idxs = TextBoxCurPktIn.Text;
            IdxS = Convert.ToInt32(idxs) - 1;
            if (IdxS < 0)
            {
                IdxS = 0;
            }
            if (IdxS > ListNameSource.Count - 1)
            {
                IdxS = ListNameSource.Count - 1;
            }
            ShowList();
        }

        private void CheckBoxForceRename_Checked(object sender, RoutedEventArgs e)
        {
            TextBoxNameIn.IsEnabled = true;
            isSourceNameChanged = true;
        }
        private void CheckBoxForceRename_UnChecked(object sender, RoutedEventArgs e)
        {
            TextBoxNameIn.IsEnabled = false;
        }

        private void BtnSetOpcode_Click(object sender, RoutedEventArgs e)
        {
            var opcode = TextBoxOpcodeOut.Text;
            var idxd = TextBoxCurPktOut.Text;
            IdxD = Convert.ToInt32(idxd) - 1;
            if (IdxD < 0)
            {
                IdxD = 0;
            }
            if (IdxD > ListNameDestination.Count - 1)
            {
                IdxD = ListNameDestination.Count - 1;
            }
            ListOpcodeDestination[IdxD] = opcode;
            isResetOpcode = true;

            ShowList();
        }
    }
}
