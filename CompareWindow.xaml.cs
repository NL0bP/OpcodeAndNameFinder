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
        public static Dictionary<int, List<string>> StructureSource = new Dictionary<int, List<string>>();
        public static Dictionary<int, List<string>> StructureDestination = new Dictionary<int, List<string>>();
        public static List<string> ListOpcodeDestination = new List<string>();

        public void CompareSourceStructures(
            ref List<string> listNameSource,
            ref List<string> listNameDestination,
            ref List<string> listNameCompare,
            ref List<string> listSubDestination,
            ref Dictionary<int, List<string>> structureSource,
            ref Dictionary<int, List<string>> structureDestination,
            List<string> listOpcodeDestination)
        {
            ListNameCompare = new List<string>(listNameCompare);
            ListNameSource = new List<string>(listNameSource);
            ListNameDestination = new List<string>(listNameDestination);
            ListSubDestination = new List<string>(listSubDestination);
            StructureSource = new Dictionary<int, List<string>>(structureSource);
            StructureDestination = new Dictionary<int, List<string>>(structureDestination);
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
            var useIn = 0;
            var useOut = 0;
            if (MainWindow.InUseIn.ContainsKey(IdxS))
            {
                useOut = MainWindow.InUseIn[IdxS];
                //var useIn = MainWindow.InUseOut[useOut];
                //MainWindow.InUseIn.Remove(IdxS);
                MainWindow.InUseOut.Remove(useOut);
                ListNameCompare[useOut] = ListNameDestination[useOut];
                //TextBox21.Text = ListNameDestination[IdxD];
            }
            if (MainWindow.InUseOut.ContainsKey(IdxD))
            {
                useIn = MainWindow.InUseOut[IdxD];
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
            // запишем новое имя на место неизвестного, которое нашли
            // удаляем оконечные опкоды в имени пакета
            var offset = ListNameSource[IdxS].LastIndexOf("_", StringComparison.Ordinal);
            if (offset > 0)
            {
                var nameSource = ListNameSource[IdxS].Substring(0, offset);
                ListNameCompare[IdxD] = nameSource;
            }
            else
            {
                ListNameCompare[IdxD] = ListNameSource[IdxS];
            }
            // отобразим на форме результаты
            ShowList();
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
                ListView11.ItemsSource = StructureSource[IdxS].ToList();
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
                ListView21.ItemsSource = StructureDestination[IdxD].ToList();
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
                    var lst = "Packet name: " + ListNameCompare[i] + ", PacketBodyReader: " + ListSubDestination[i] +
                              ", Opcode: " + ListOpcodeDestination[i];
                    tmp.Add(lst);
                }
                else
                {
                    var lst = "Packet name: " + ListNameCompare[i] + ", PacketBodyReader: " + ListSubDestination[i] +
                              ", Opcode: 0xfff";
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
                    var lst = "        <packet type=\"" + ListOpcodeDestination[i] + "\" desc=\"" + ListNameCompare[i] + "\">";
                    tmp.Add(lst);
                }
                else
                {
                    break;
                }
            }

            File.WriteAllLines(FilePath + "_PDEC.txt", tmp);

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
