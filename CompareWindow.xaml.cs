using System;
using Microsoft.Win32;

using System.Collections.Generic;
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
        public int IdxS { get; set; }
        public int IdxD { get; set; }

        public CompareWindow()
        {
            InitializeComponent();
        }

        public static List<string> ListNameCompare = new List<string>();
        public static List<string> ListNameSource = new List<string>();
        public static List<string> ListNameDestination = new List<string>();
        public static List<string> ListSubDestination = new List<string>();
        public static Dictionary<int, List<Struc>> StructureSource = new Dictionary<int, List<Struc>>();
        public static Dictionary<int, List<Struc>> StructureDestination = new Dictionary<int, List<Struc>>();
        public static List<string> ListOpcodeDestination = new List<string>();
        public static bool isRemoveOpcode = false;

        public void CompareSourceStructures(
            ref List<string> listNameSource,
            ref List<string> listNameDestination,
            ref List<string> listNameCompare,
            ref List<string> listSubDestination,
            ref Dictionary<int, List<Struc>> structureSource,
            ref Dictionary<int, List<Struc>> structureDestination,
            List<string> listOpcodeDestination)
        {
            ListNameCompare = new List<string>(listNameCompare);
            ListNameSource = new List<string>(listNameSource);
            ListNameDestination = new List<string>(listNameDestination);
            ListSubDestination = new List<string>(listSubDestination);
            StructureSource = new Dictionary<int, List<Struc>>(structureSource);
            StructureDestination = new Dictionary<int, List<Struc>>(structureDestination);
            ListOpcodeDestination = new List<string>(listOpcodeDestination);

            MainWindow.ListNameCompare = new List<string>(ListNameCompare);


            // начнем с начала
            IdxD = 0;
            IdxS = 0;
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
            if (MainWindow.InUseIn.ContainsKey(IdxS))
            {
                var useOut = MainWindow.InUseIn[IdxS];
                MainWindow.InUseOut.Remove(useOut);
                ListNameCompare[useOut] = ListNameDestination[useOut];
            }
            if (MainWindow.InUseOut.ContainsKey(IdxD))
            {
                var useIn = MainWindow.InUseOut[IdxD];
                MainWindow.InUseIn.Remove(useIn);
            }
            if (MainWindow.InUseIn.ContainsKey(IdxS))
            {
                MainWindow.InUseIn[IdxS] = IdxD;
            }
            else
            {
                MainWindow.InUseIn.Add(IdxS, IdxD); // отметим, что найденное имя занято
            }

            if (MainWindow.InUseOut.ContainsKey(IdxD))
            {
                MainWindow.InUseOut[IdxD] = IdxS;
            }
            else
            {
                MainWindow.InUseOut.Add(IdxD, IdxS); // отметим, что найденное имя занято
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
                    if (ListNameSource[IdxS][0].ToString() == "o" ||
                        ListNameSource[IdxS][1].ToString() == "f" ||
                        ListNameSource[IdxS][2].ToString() == "f")
                    {
                        // не переименовываем если имя начинается off_
                        ListNameCompare[IdxD] = ListNameDestination[IdxD];
                    }
                    else
                    {
                        ListNameCompare[IdxD] = ListNameSource[IdxS];
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
                    ListNameCompare[IdxD] = ListNameSource[IdxS];
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
            // Remove Packet
            if (CheckBoxAdd.IsChecked == true)
            {
                if (ListNameCompare[IdxD][0].ToString() != "o" ||
                    ListNameCompare[IdxD][1].ToString() != "f" ||
                    ListNameCompare[IdxD][2].ToString() != "f")
                {
                    // удаляем CS|SC только в начале имени
                    RemoveCSSC();
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
                IdxS = ListNameSource.Count - 1;
            }

            if (IdxD < 0)
            {
                IdxD = ListNameCompare.Count - 1;
            }

            TextBox11.Text = ListNameSource[IdxS];
            TextBox12.Text = ListNameSource.Count.ToString();
            var idxs = IdxS + 1;
            TextBox13.Text = idxs.ToString();
            if (StructureSource[IdxS].Count == 0)
            {
                ListView11.ItemsSource = "nullsub";
            }
            else
            {
                var lis = StructureSource[IdxS];
                var source = new List<string>();
                foreach (var li in lis)
                {
                    if (MainWindow.isTypeEnumNewIn)
                    {
                        source.Add((TypeEnum2)li.Type + " " + li.Name);
                    }
                    else
                    {
                        source.Add((TypeEnum)li.Type + " " + li.Name);
                    }
                }
                ListView11.ItemsSource = source.ToList();
            }

            TextBox21.Text = ListNameCompare[IdxD];
            TextBox22.Text = ListNameCompare.Count.ToString();
            var idxd = IdxD + 1;
            TextBox23.Text = idxd.ToString();

            if (StructureDestination[IdxD].Count == 0)
            {
                ListView21.ItemsSource = "nullsub";
            }
            else
            {
                var lis = StructureDestination[IdxD];
                var source = new List<string>();
                foreach (var li in lis)
                {
                    if (MainWindow.isTypeEnumNewOut)
                    {
                        source.Add((TypeEnum2)li.Type + " " + li.Name);
                    }
                    else
                    {
                        source.Add((TypeEnum)li.Type + " " + li.Name);
                    }
                }
                ListView21.ItemsSource = source.ToList();
            }

            // проверим, что имя не занято
            if (MainWindow.InUseIn.ContainsKey(IdxS))
            {
                checkBoxInUse.IsChecked = true;
                var idxs2 = MainWindow.InUseIn[IdxS] + 1;
                TextBox12_Copy.Text = idxs2.ToString();
            }
            else
            {
                checkBoxInUse.IsChecked = false;
                TextBox12_Copy.Text = "0";
            }

            if (MainWindow.InUseOut.ContainsKey(IdxD))
            {
                checkBoxOutUse.IsChecked = true;
                var idxd2 = MainWindow.InUseOut[IdxD] + 1;
                TextBox22_Copy.Text = idxd2.ToString();
            }
            else
            {
                checkBoxOutUse.IsChecked = false;
                TextBox22_Copy.Text = "0";
            }
        }

        private void TextBox21_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (ListNameCompare.Count != 0)
            {
                ListNameCompare[IdxD] = TextBox21.Text;
            }
        }

        private void TextBox11_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (ListNameSource.Count != 0)
            {
                ListNameSource[IdxS] = TextBox11.Text;
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

            // сохраняем в виде файла для внесения опкодов в PDEC
            tmp = new List<string>();
            for (var i = 0; i < ListNameCompare.Count; i++)
            {
                if (ListOpcodeDestination.Count > 0)
                {
                    var nameCompare = "";
                    if (ListNameCompare[i][0].ToString() != "o" || ListNameCompare[i][1].ToString() != "f" || ListNameCompare[i][2].ToString() != "f")
                    {
                        nameCompare = ListNameCompare[i].Replace("Packet", "");
                    }
                    else
                    {
                        nameCompare = ListNameCompare[i];
                    }
                    var lst = "        <packet type=\"" + ListOpcodeDestination[i] + "\" desc=\"" + nameCompare + "\">";
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

        private void ButtonIn2_Click(object sender, RoutedEventArgs e)
        {
            // продублируем в оба списка
            //MainWindow.ListNameDestinationSC = new List<string>(ListNameCompare);
            //MainWindow.ListNameDestinationCS = new List<string>(ListNameCompare);

            //MainWindow.ListNameCompareSC = new List<string>(ListNameCompare);
            //MainWindow.ListNameCompareCS = new List<string>(ListNameCompare);

            MainWindow.ListNameCompare = new List<string>(ListNameCompare);

            Close();
        }

        private void BtnClearName_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.InUseOut.ContainsKey(IdxD))
            {
                return;
            }

            var useIn = MainWindow.InUseOut[IdxD];
            TextBox21.Text = ListNameDestination[IdxD];
            TextBox22.Text = ListNameDestination.Count.ToString();

            ListNameCompare[IdxD] = ListNameDestination[IdxD];
            MainWindow.InUseOut.Remove(IdxD);
            MainWindow.InUseIn.Remove(useIn);

            // проверим что на панели нужное имя
            if (useIn == IdxS)
            {
                // проверим, что имя не занято
                if (MainWindow.InUseIn.ContainsKey(IdxS))
                {
                    checkBoxInUse.IsChecked = true;
                    var idxs2 = MainWindow.InUseIn[IdxS] + 1;
                    TextBox12_Copy.Text = idxs2.ToString();
                }
                else
                {
                    checkBoxInUse.IsChecked = false;
                    TextBox12_Copy.Text = "0";
                }
            }

            if (MainWindow.InUseOut.ContainsKey(IdxD))
            {
                checkBoxOutUse.IsChecked = true;
                var idxd2 = MainWindow.InUseOut[IdxD] + 1;
                TextBox22_Copy.Text = idxd2.ToString();
            }
            else
            {
                checkBoxOutUse.IsChecked = false;
                TextBox22_Copy.Text = "0";
            }
        }

        private void TextBox21_OnTextChangedChanged(object sender, TextChangedEventArgs e)
        {
            if (ListNameCompare.Count != 0)
            {
                MainWindow.ListNameCompare[IdxD] = TextBox21.Text;
                ListNameCompare[IdxD] = TextBox21.Text;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.InUseIn.ContainsKey(IdxS))
            {
                return;
            }

            var useOut = MainWindow.InUseIn[IdxS];
            ListNameCompare[useOut] = ListNameDestination[useOut];

            // проверим что на панели нужное имя
            if (useOut == IdxD)
            {
                // проверим, что имя не занято
                if (MainWindow.InUseOut.ContainsKey(useOut))
                {
                    checkBoxOutUse.IsChecked = false;
                    TextBox22_Copy.Text = "0";
                    ListNameCompare[useOut] = ListNameDestination[useOut]; // восстановим старое имя пакета
                    TextBox21.Text = ListNameDestination[useOut];
                    TextBox22.Text = ListNameDestination.Count.ToString();
                }
            }
            checkBoxInUse.IsChecked = false;
            TextBox12_Copy.Text = "0";

            MainWindow.InUseOut.Remove(useOut);
            MainWindow.InUseIn.Remove(IdxS);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var idxd = TextBox23.Text;
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
            var idxs = TextBox13.Text;
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
    }
}
