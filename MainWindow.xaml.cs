using NameFinder.Conversion;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NameFinder
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsLittleEndian { get; } = true;

        public EndianBitConverter Converter => (IsLittleEndian ? EndianBitConverter.Little : (EndianBitConverter)EndianBitConverter.Big);

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
        public bool isCompareCS = false;
        public bool isCompareSC = false;
        public static bool isRemoveOpcode = false;

        //public static Dictionary<int, int> InUseSource { get; set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> InUseIn { get; set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> InUseOut { get; set; } = new Dictionary<int, int>();
        public static Dictionary<int, bool> IsRenameDestination { get; set; } = new Dictionary<int, bool>();

        public static List<string> InListSource = new List<string>();
        public static List<string> ListNameSourceCS = new List<string>();
        public static List<string> ListNameSourceSC = new List<string>();
        public static List<string> ListSubSourceCS = new List<string>();

        public static List<string> ListSubSourceSC = new List<string>();

        // здесь будем собирать структуры пакетов, где index из listName1 и соответственно listSub1
        public static Dictionary<int, List<string>> StructureSourceCS = new Dictionary<int, List<string>>();
        public static Dictionary<int, List<string>> StructureSourceSC = new Dictionary<int, List<string>>();


        public static List<string> InListDestination = new List<string>();
        public static List<string> ListNameDestinationCS = new List<string>();
        public static List<string> ListNameDestinationSC = new List<string>();
        public static List<string> ListSubDestinationCS = new List<string>();

        public static List<string> ListSubDestinationSC = new List<string>();

        // здесь будем собирать структуры пакетов, где index из listName1 и соответственно listSub1
        public static Dictionary<int, List<string>> StructureDestinationCS = new Dictionary<int, List<string>>();
        public static Dictionary<int, List<string>> StructureDestinationSC = new Dictionary<int, List<string>>();

        public static Dictionary<int, List<string>> XrefsIn = new Dictionary<int, List<string>>();
        public static Dictionary<int, List<string>> XrefsOut = new Dictionary<int, List<string>>();
        public static List<string> ListOpcodeSourceCS = new List<string>();
        public static List<string> ListOpcodeSourceSC = new List<string>();
        public static List<string> ListOpcodeDestinationCS = new List<string>();
        public static List<string> ListOpcodeDestinationSC = new List<string>();

        public static List<string> ListNameCompareCS = new List<string>();
        public static List<string> ListNameCompareSC = new List<string>();
        public static List<string> ListNameCompare = new List<string>();
        public static List<string> ListNameCompareOutCS = new List<string>();
        public static List<string> ListNameCompareOutSC = new List<string>();

        public bool isCleaningIn = false;
        public bool isCleaningOut = false;
        public const int DepthMax = 10;
        public static int DepthIn = 0;
        public static int DepthOut = 0;
        public int IdxS = 0;
        public int IdxD = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void FindOpcodeSourceCS()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            ProgressBar13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar13.Value = 0; }));
            ProgressBar13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar13.Maximum = XrefsIn.Count; }));
            TextBox16Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy.Text = "0"; }));
            TextBox17Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy.Text = "0"; }));
            TextBox19Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy.Text = "0"; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.Yellow; }));
            ButtonSaveIn1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn1.IsEnabled = false; }));
            ButtonSaveIn2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn2.IsEnabled = false; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = false; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = false; }));

            _isInCs = false;
            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));

            var notFoundCount = 0;
            var subAddress = "";
            ListOpcodeSourceCS = new List<string>();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            var regexEndp = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
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
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    var find = "";
                    var ss = "";
                    for (var index = 0; index < InListSource.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListSource[index]);
                        if (matches.Count <= 0)
                        {
                            continue;
                        }

                        // нашли начало подпрограммы, ищем опкоды в структуре, пока не "endp"
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
                            // ищем сначала
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
                                    // нашли опкод
                                    break;
                                }

                                var matches2 = regexEndp.Matches(InListSource[index]);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
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

                ProgressBar13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar13.Value = ListOpcodeSourceCS.Count; }));
            }

            var lnCount = ListOpcodeSourceCS.Count;
            TextBox16Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy.Text = lnCount.ToString(); }));
            TextBox17Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy.Text = notFoundCount.ToString(); }));
            stopWatch.Stop();
            TextBox19Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy.Text = stopWatch.Elapsed.ToString(); }));
            ListView14.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView14.ItemsSource = ListOpcodeSourceCS; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.GreenYellow; }));
            ButtonSaveIn1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn1.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));

            _isInCs = true;
            if (_isInCs && _isOutCs)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = true; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
        }

        private void FindOpcodeSourceSC()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            ProgressBar13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar13.Value = 0; }));
            ProgressBar13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar13.Maximum = XrefsIn.Count; }));
            TextBox16Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy.Text = "0"; }));
            TextBox17Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy.Text = "0"; }));
            TextBox19Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy.Text = "0"; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.Yellow; }));
            ButtonSaveIn1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn1.IsEnabled = false; }));
            ButtonSaveIn2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn2.IsEnabled = false; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = false; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = false; }));
            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = false; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = false; }));

            _isInSc = false;
            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));

            var notFoundCount = 0;
            var subAddress = "";
            ListOpcodeSourceSC = new List<string>();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            var regexEndp = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
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
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    var find = "";
                    var ss = "";
                    for (var index = 0; index < InListSource.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListSource[index]);
                        if (matches.Count <= 0)
                        {
                            continue;
                        }

                        // нашли начало подпрограммы, ищем опкоды в структуре, пока не "endp"
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
                            // ищем сначала
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
                                    // нашли опкод
                                    break;
                                }

                                var matches2 = regexEndp.Matches(InListSource[index]);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
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

                ProgressBar13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar13.Value = ListOpcodeSourceSC.Count; }));
            }

            var lnCount = ListOpcodeSourceSC.Count;
            TextBox16Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy.Text = lnCount.ToString(); }));
            TextBox17Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy.Text = notFoundCount.ToString(); }));
            stopWatch.Stop();
            TextBox19Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy.Text = stopWatch.Elapsed.ToString(); }));
            ListView14.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView14.ItemsSource = ListOpcodeSourceSC; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.GreenYellow; }));
            ButtonSaveIn2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn2.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));
            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = true; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = true; }));

            _isInSc = true;
            if (_isInSc && _isOutSc)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = true; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
        }

        private void FindOpcodeDestinationCS()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            ProgressBar23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar23.Value = 0; }));
            ProgressBar23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar23.Maximum = XrefsOut.Count; }));
            TextBox16Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy1.Text = "0"; }));
            TextBox17Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy1.Text = "0"; }));
            TextBox19Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy1.Text = "0"; }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.Yellow; }));

            _isOutCs = false;
            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));

            var notFoundCount = 0;
            //var baseAddress = 0;
            //var offsetAddres = 0;
            var subAddress = "";
            ListOpcodeDestinationCS = new List<string>();

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
                            // ищем сначала
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
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

                ProgressBar23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar23.Value = ListOpcodeDestinationCS.Count; }));
            }

            var lnCount = ListOpcodeDestinationCS.Count;
            TextBox16Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy1.Text = lnCount.ToString(); }));
            TextBox17Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy1.Text = notFoundCount.ToString(); }));
            stopWatch.Stop();
            TextBox19Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy1.Text = stopWatch.Elapsed.ToString(); }));
            ListView24.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView24.ItemsSource = ListOpcodeDestinationCS; }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.GreenYellow; }));

            _isOutCs = true;
            if (_isInCs && _isOutCs)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = true; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
        }

        private void FindOpcodeDestinationSC()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            ProgressBar23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar23.Value = 0; }));
            ProgressBar23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar23.Maximum = XrefsOut.Count; }));
            TextBox16Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy1.Text = "0"; }));
            TextBox17Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy1.Text = "0"; }));
            TextBox19Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy1.Text = "0"; }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.Yellow; }));
            ButtonSaveOut1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveOut1.IsEnabled = false; }));
            ButtonSaveOut2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveOut2.IsEnabled = false; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = false; }));
            //BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = false; }));
            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = false; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = false; }));

            _isOutSc = false;
            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));

            var notFoundCount = 0;
            var subAddress = "";
            ListOpcodeDestinationSC = new List<string>();

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
                            // ищем сначала
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString();
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 2:
                                            matchGroup = "0x0" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 3:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[0].ToString();
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

                ProgressBar23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar23.Value = ListOpcodeDestinationSC.Count; }));
            }

            var lnCount = ListOpcodeDestinationSC.Count;
            TextBox16Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy1.Text = lnCount.ToString(); }));
            TextBox17Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy1.Text = notFoundCount.ToString(); }));
            stopWatch.Stop();
            TextBox19Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy1.Text = stopWatch.Elapsed.ToString(); }));
            ListView24.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView24.ItemsSource = ListOpcodeDestinationSC; }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.GreenYellow; }));
            ButtonSaveOut2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveOut2.IsEnabled = true; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = true; }));
            //BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));
            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = true; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = true; }));

            _isOutSc = true;
            if (_isInSc && _isOutSc)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = true; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
        }

        private static string Increase4(string str, int inc)
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

        private static string Decrease4(string str, int dec)
        {
            // mov     [ebp+var_48C], offset SCDominionDataPacket_0x0b2
            // mov     [ebp+var_488], 0B2h
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

        private List<string> CleanSourceSub(int idx)
        {
            var progress = CalcProgress(InListSource.Count);

            var maxCount = InListSource.Count;
            var found = false;
            var tmpLst = new List<string>();
            var regexProcNear = new Regex(@"(proc\s+near)", RegexOptions.Compiled); // ищем начало подпрограммы
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
            //var regexSub = new Regex(@"push\s+offset\s|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            for (var index = idx; index < maxCount; index++)
            {
                if (index % progress == 0)
                {
                    ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = index; }));
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
                        ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = index; }));
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
            var txtCS = "";
            TextBox11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtCS = TextBox11.Text; }));

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
                    ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background,
                        new Action(() => { ProgressBar11.Value = index; }));
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
                    tmpLst.Add(
                        "                                        ; DATA XREF: sub_FFFFFFFF"); // добавим отсутствующую строку
                }
                else
                {
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
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

        private List<string> CleanSourceOffsSC(int idx)
        {
            var progress = CalcProgress(InListSource.Count);

            var found = false;
            var tmpLst = new List<string>();
            var txtSC = "";
            TextBox12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtSC = TextBox12.Text; }));

            // ищем:
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
            // ищем начало offsets
            var regex = new Regex(@"(^[a-zA-Z0-9_?@]+\s+dd\soffset\s)" + txtSC, RegexOptions.Compiled);
            for (var index = idx; index < InListSource.Count; index++)
            {
                if (index % progress == 0)
                {
                    ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background,
                        new Action(() => { ProgressBar11.Value = index; }));
                }

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
                    tmpLst.Add("                                        ; DATA XREF: sub_FFFFFFFF"); // добавим отсутствующую строку
                }
                else
                {
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
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
            var txtSC = "";
            TextBox12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtSC = TextBox12.Text; }));
            var regex = new Regex(@"(^(\s+\w+\s+\d+)|^\s*$)", RegexOptions.IgnoreCase); // ищем мусорные строки 
            for (var index = idx; index < InListSource.Count; index++)
            {
                if (index % progress == 0)
                {
                    ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background,
                        new Action(() => { ProgressBar11.Value = index; }));
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
                // не нашли структуру
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
            var regexProcNear = new Regex(@"(proc\s+near)", RegexOptions.Compiled); // ищем начало подпрограммы
            var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
            //var regexSub = new Regex(@"push\s+offset\s|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            var regexSub = new Regex(@"push\s+offset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            for (var index = idx; index < maxCount; index++)
            {
                if (index % progress == 0)
                {
                    ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = index; }));
                }

                var matchesProcNear = regexProcNear.Matches(InListDestination[index]);
                if (matchesProcNear.Count <= 0)
                {
                    continue;
                }

                // нашли начало подпрограммы
                tmpLst.Add(InListDestination[index]); // сохранили

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var foundEndp = false;
                do
                {
                    if (index % progress == 0)
                    {
                        ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = index; }));
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
                    // нашли конец подпрограммы
                    tmpLst.Add(InListDestination[index]); // сохранили
                } while (index >= maxCount || !foundEndp);

            }

            if (!found)
            {
                // не нашли структуру
                return new List<string>();
            }

            return tmpLst;
        }

        private List<string> CleanDestinationCSOffs(int idx)
        {
            var progress = CalcProgress(InListDestination.Count);

            var found = false;
            var tmpLst = new List<string>();
            var txtCS = "";
            TextBox21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtCS = TextBox21.Text; }));

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
            for (var index = idx; index < InListDestination.Count; index++)
            {
                if (index % progress == 0)
                {
                    ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = index; }));
                }

                // ??_7X2ClientToWorldPacket@@6B@ dd offset CS_PACKETS_return_0
                var matches = regex.Matches(InListDestination[index]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                tmpLst.Add(InListDestination[index]); // сохранили
                index++;
                // проверим есть следующая строка с отступом 40 пробелов
                var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.Compiled);
                var matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
                if (matchesSpace40.Count <= 0)
                {
                    tmpLst.Add("                                        ; DATA XREF: sub_FFFFFFFF"); // добавим отсутствующую строку
                }
                else
                {
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
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
                // запишем
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
            var txtSC = "";
            TextBox22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtSC = TextBox22.Text; }));

            // ищем:
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
            // ищем начало offsets
            var regex = new Regex(@"(^[a-zA-Z0-9_?@]+\s+dd\soffset\s)" + txtSC, RegexOptions.Compiled);
            for (var index = idx; index < InListDestination.Count; index++)
            {
                if (index % progress == 0)
                {
                    ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = index; }));
                }

                var matches = regex.Matches(InListDestination[index]);
                if (matches.Count <= 0)
                {
                    continue;
                }

                tmpLst.Add(InListDestination[index]); // сохранили
                index++;

                // проверим есть следующая строка с отступом 40 пробелов
                var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.Compiled);
                var matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
                if (matchesSpace40.Count <= 0)
                {
                    tmpLst.Add("                                        ; DATA XREF: sub_FFFFFFFF"); // добавим отсутствующую строку
                }
                else
                {
                    // сначала нужно записать строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
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
                // запишем
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
            var regexClr = new Regex(@"\;\ssub_\d+|\;\sDATA\sXREF\:|proc\s+near|\s+endp\s*|\soffset\s|call\s+\w+|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            //var regexClr = new Regex(@"proc\s+near|\s+endp\s*|\sdd\soffset\s|push\s+offset\s|call\s+sub_\w+|mov\s+\[\w+\+\w+\],\soffset\s|mov\s+\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\+\w+\],\s([1-9]|[0-9A-F]{2,3}h)|mov\s+dword\sptr\s\[\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\soffset\s|mov\s+dword\sptr\s\[\w+\-\w+\],\s([1-9]|[0-9A-F]{2,3}h)", RegexOptions.Compiled);
            //var regexClr = new Regex(@"(^(\s+\w+\s+\d+)|^\s*$)", RegexOptions.IgnoreCase); // ищем мусорные строки 
            // узнаем количество строк в файле
            var maxCount = File.ReadLines(FilePathIn1).Count();
            var progress = CalcProgress(maxCount);

            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = 0; }));
            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Maximum = maxCount; }));

            // считываем по одной строке, отбрасываем не нужные и сохраняем нужные в InListSource
            var index = 0;
            using (var f = new StreamReader(FilePathIn1))
            {
                while ((s = f.ReadLine()) != null)
                {
                    if (index % progress == 0)
                    {
                        ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = index; }));
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

            // сохраним в файл
            File.WriteAllLines(FilePathIn1, InListSource);
            // заполним ListView
            ListView11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView11.ItemsSource = InListSource; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.GreenYellow; }));
            stopWatch.Stop();
            TextBox15.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox15.Text = stopWatch.Elapsed.ToString(); }));
        }

        private void CleanSource0()
        {
            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = 0; }));
            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Maximum = InListSource.Count; }));

            var tmp = new List<string>();

            // чистим сначала от пустых строк
            var tmpSpace = CleanSourceSpace(0);
            tmp.AddRange(tmpSpace);

            InListSource = new List<string>(tmp);

            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = InListSource.Count; }));
            ListView11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView11.ItemsSource = InListSource; }));
            File.WriteAllLines(FilePathIn1, InListSource);
            CheckBoxCleaningIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { CheckBoxCleaningIn.IsChecked = false; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.Yellow; }));
        }

        private void CleanSource()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = 0; }));
            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Maximum = InListSource.Count; }));

            var tmp = new List<string>();

            // затем ищем подпрограммы
            var tmpSub = CleanSourceSub(0);
            tmp.AddRange(tmpSub);

            // затем ищем оффсеты для CS
            var tmpCSOffs = CleanSourceOffsCS(0);
            tmp.AddRange(tmpCSOffs);

            // затем ищем оффсеты для SC
            var tmpSCOffs = CleanSourceOffsSC(0);
            tmp.AddRange(tmpSCOffs);

            InListSource = new List<string>(tmp);

            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = InListSource.Count; }));
            ListView11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView11.ItemsSource = InListSource; }));
            File.WriteAllLines(FilePathIn1, InListSource);
            CheckBoxCleaningIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { CheckBoxCleaningIn.IsChecked = false; }));

            stopWatch.Stop();
            TextBox15.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox15.Text = stopWatch.Elapsed.ToString(); }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.GreenYellow; }));
            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = true; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = true; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
        }

        private void CleanDestination()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = 0; }));
            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Maximum = InListDestination.Count; }));

            var tmp = new List<string>();

            // затем ищем подпрограммы
            var tmpSub = CleanDestinationSub(0);
            tmp.AddRange(tmpSub);

            // затем ищем оффсеты для CS
            var tmpCSOffs = CleanDestinationCSOffs(0);
            tmp.AddRange(tmpCSOffs);

            // затем ищем оффсеты для SC
            var tmpSCOffs = CleanDestinationSCOffs(0);
            tmp.AddRange(tmpSCOffs);

            InListDestination = new List<string>(tmp);

            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = InListDestination.Count; }));
            ListView21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView21.ItemsSource = InListDestination; }));
            File.WriteAllLines(FilePathIn2, InListDestination);
            CheckBoxCleaningOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { CheckBoxCleaningOut.IsChecked = false; }));

            stopWatch.Stop();
            TextBox25.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox25.Text = stopWatch.Elapsed.ToString(); }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.GreenYellow; }));
            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = true; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = true; }));
            //BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = true; }));
        }

        private List<string> FindStructureIn(string address)
        {
            var tmpLst = new List<string>();
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
                tmpLst = new List<string>();
                //var str = "-->>";
                //lst.Add(str);
                do
                {
                    index++;
                    var matches5 = regexCall.Matches(InListSource[index]);
                    foreach (var match5 in matches5)
                    {
                        if (match5.ToString() == "call    eax" || match5.ToString() == "call    ebx" ||
                            match5.ToString() == "call    edx" || match5.ToString() == "call    ecx")
                        {
                            continue;
                        }

                        if (match5.ToString().Length >= 4 && match5.ToString().Substring(0, 4) == "call")
                        {
                            var findList = FindStructureIn(match5.ToString().Substring(8));
                            if (findList.Count > 0)
                            {
                                tmpLst.AddRange(findList);
                                found = true; // нашли структуру
                            }
                        }
                        else
                        {
                            tmpLst.Add(match5.ToString()); // сохранили часть структуры пакета
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
                return new List<string>();
            }

            return tmpLst;
        }

        private List<string> FindStructureOut(string address)
        {
            var tmpLst = new List<string>();
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
            var regexSub =
                new Regex(@"^" + address, RegexOptions.Compiled); // ищем начало подпрограммы, каждый раз с начала файла
            var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))",
                RegexOptions.Compiled);
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
                tmpLst = new List<string>();
                //var str = "-->>";
                //lst.Add(str);
                do
                {
                    index++;
                    var matches5 = regexCall.Matches(InListDestination[index]);
                    foreach (var match5 in matches5)
                    {
                        if (match5.ToString() == "call    eax" || match5.ToString() == "call    ebx" ||
                            match5.ToString() == "call    edx" || match5.ToString() == "call    ecx")
                        {
                            continue;
                        }

                        if (match5.ToString().Length >= 4 && match5.ToString().Substring(0, 4) == "call")
                        {
                            var findList = FindStructureOut(match5.ToString().Substring(8));
                            if (findList.Count > 0)
                            {
                                tmpLst.AddRange(findList);
                                found = true; // нашли структуру
                            }
                        }
                        else
                        {
                            tmpLst.Add(match5.ToString()); // сохранили часть структуры пакета
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

                //StructureSourceSC.Add(i, lst); // сохранили всю структуру пакета
                DepthOut--;
                //str = "<<--";
                //lst.Add(str);
                return tmpLst;
            }

            if (!found)
            {
                // не нашли структуру
                //lst = new List<string>();
                //StructureSourceSC.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                DepthOut--;
                return new List<string>();
            }

            return tmpLst;
        }

        private void FindSourceStructuresCS(string str)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            // уничтожаем ненужный список
            StructureSourceCS = new Dictionary<int, List<string>>();
            ListNameSourceCS = new List<string>();
            ListSubSourceCS = new List<string>();
            XrefsIn = new Dictionary<int, List<string>>();

            TextBox13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox13.Text = "0"; }));
            TextBox14.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox14.Text = "0"; }));
            TextBox18.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox18.Text = "0"; }));
            ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Value = 0; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.Yellow; }));
            ButtonSaveIn1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn1.IsEnabled = false; }));
            ButtonSaveIn2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn2.IsEnabled = false; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = false; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = false; }));
            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = false; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = false; }));

            //
            // начали предварительную работу по поиску имен и ссылок на подпрограммы со структурами
            //
            //"CS_PACKET_TODAY_ASSIGNMENT_11Ah dd offset SC_PACKETS_return_2"
            var indexRefs = 0;

            // Создаем объект для блокировки.
            var lockObj = new object();
            // Блокируем объект.
            lock (lockObj)
            {
                var regex = new Regex(@"^[a-zA-Z0-9_?@]+\s+dd\soffset\s" + str, RegexOptions.Compiled);
                //var regexXREF = new Regex(@"(^\s+;[a-zA-Z:\s]*\s(sub_\w+|X2\w+))", RegexOptions.Compiled);
                var regexXREF = new Regex(@"(^\s+;[a-zA-Z:\s]*\s(sub_\w+|X2\w+|w+))", RegexOptions.Compiled);
                for (var index = 0; index < InListSource.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListSource[index]);
                    if (matches.Count <= 0)
                    {
                        continue;
                    }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        //var regexXREF = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListSource[tmpIdx]);
                        if (matchesXREF.Count <= 0)
                        {
                            continue;
                        }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);

                    XrefsIn.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++; // следующий номер пакета

                    var regex2 = new Regex(@"^(\S+)", RegexOptions.IgnoreCase);
                    var matches2 = regex2.Matches(InListSource[index]);
                    foreach (var match2 in matches2)
                    {
                        ListNameSourceCS.Add(match2.ToString()); // сохранили имя
                        foundName = true;
                    }

                    if (!foundName)
                    {
                        // не нашли имя пакета, бывает что его нет из-зи защиты themida
                        ListNameSourceCS.Add("CS_Unknown"); // сохранили адрес подпрограммы
                    }

                    // сначала нужно пропустить строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    do
                    {
                        index++;
                        var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.IgnoreCase);
                        var matchesSpace40 = regexSpace40.Matches(InListSource[index]);
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

                    // ищем "dd offset sub_395D0370"
                    // dd offset nullsub_18
                    // dd offset CSInteractGimmickPacket
                    // dd offset CSGmCommandPacket
                    var regexBody = new Regex(@"(dd\soffset\snullsub|dd\soffset\ssub_\w+|dd\soffset\s\w+)",
                        RegexOptions.Compiled);
                    var matchesBodys = regexBody.Match(InListSource[index]);
                    ListSubSourceCS.Add(matchesBodys.ToString().Substring(10)); // сохранили адрес подпрограммы
                }

                // закончили предварительную работу по поиску имен и ссылок на подпрограммы со структурами
                var lnCount = ListNameSourceCS.Count;
                TextBox13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox13.Text = lnCount.ToString(); }));
                var lsCount = ListSubSourceCS.Count;
                TextBox14.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox14.Text = lsCount.ToString(); }));
                ListView12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView12.ItemsSource = ListNameSourceCS; }));
                ListView13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView13.ItemsSource = ListSubSourceCS; }));

                ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Maximum = ListNameSourceCS.Count; }));
                if (FindStructIn)
                {
                    //
                    // начали предварительную работу по поиску структур пакетов
                    //
                    // начнем с начала файла
                    var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
                    var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s+(sub_\w+)|(call\s+(\w+)))",
                        RegexOptions.Compiled);
                    /*
                       sub_395D3050    proc near               ; CODE XREF: sub_391DE5C0+47↑p
                       push    offset aBc      ; "bc"
                       call    sub_395E18A0
                       call    sub_395E1730
                       call    sub_395E16B0
                       push    offset aAction  ; "action"
                       sub_395D3050    endp              
                       
                       sub_395E18A0    proc near               ; CODE XREF: .text:394B5DEC↑p
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aModified ; "modified"
                       sub_395E18A0    endp
                       
                       sub_395E1730    proc near               ; CODE XREF: .text:394B1C67↑p
                       push    offset aType    ; "type"
                       push    offset asc_396AFCE0 ; "x"
                       push    offset aY       ; "y"
                       push    offset aZ_0     ; "z"
                       push    offset aModified ; "modified"
                       sub_395E1730    endp
                       
                       sub_395E16B0    proc near               ; CODE XREF: .text:394B1CB7↑p
                       push    offset aType    ; "type"
                       push    offset aData    ; "data"
                       push    offset aData    ; "data"
                       push    offset aModified ; "modified"
                       sub_395E16B0    endp
                       
                    */

                    for (var i = 0; i < ListSubSourceCS.Count; i++)
                    {
                        var found = false;
                        var regexSub =
                            new Regex(@"^" + ListSubSourceCS[i],
                                RegexOptions.Compiled); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListSource.Count; index++)
                        {
                            var matchesSub = regexSub.Matches(InListSource[index]);
                            if (matchesSub.Count <= 0)
                            {
                                continue;
                            }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var matchesCalls = regexCall.Matches(InListSource[index]);
                                foreach (var matchCall in matchesCalls)
                                {
                                    if (matchCall.ToString().Length >= 4 &&
                                        matchCall.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureIn(matchCall.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(
                                                findList); // сохранили несколько строк структуры пакета найденной в подпрограмме
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(matchCall.ToString()); // сохранили одну строку структуры пакета
                                    }
                                }

                                var matchesEndP = regexEndP.Matches(InListSource[index]);
                                if (matchesEndP.Count <= 0)
                                {
                                    continue;
                                }

                                foundEndp = true;
                            } while (!foundEndp);

                            StructureSourceCS.Add(i, lst); // сохранили всю структуру пакета
                            found = true; // нашли структуру
                            break;
                        }

                        if (!found)
                        {
                            // не нашли структуру
                            var lst = new List<string>();
                            StructureSourceCS.Add(i, lst); // сохраним пустой список, так как ничего не нашли 
                        }

                        ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Value = StructureSourceCS.Count; }));
                    }
                }
            }

            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = true; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));

            _isInCs = true;
            if (_isInCs && _isOutCs)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = true; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }

            ButtonSaveIn1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn1.IsEnabled = true; }));
            stopWatch.Stop();
            TextBox18.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox18.Text = stopWatch.Elapsed.ToString(); }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.GreenYellow; }));
        }

        private void FindSourceStructuresSC(string str)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            // уничтожаем ненужный список
            StructureSourceSC = new Dictionary<int, List<string>>();
            ListNameSourceSC = new List<string>();
            ListSubSourceSC = new List<string>();
            XrefsIn = new Dictionary<int, List<string>>();

            TextBox16.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16.Text = "0"; }));
            TextBox17.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17.Text = "0"; }));
            TextBox19.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19.Text = "0"; }));
            ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Value = 0; }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.Yellow; }));
            ButtonSaveIn1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn1.IsEnabled = false; }));
            ButtonSaveIn2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn2.IsEnabled = false; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = false; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = false; }));
            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = false; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = false; }));

            //
            // начали предварительную работу по поиску имен и ссылок на подпрограммы со структурами
            //
            //"CS_PACKET_TODAY_ASSIGNMENT_11Ah dd offset SC_PACKETS_return_2"
            var indexRefs = 0;

            // Создаем объект для блокировки.
            var lockObj = new object();
            // Блокируем объект.
            lock (lockObj)
            {
                var regex = new Regex(@"^[a-zA-Z0-9_?@]+\s+dd\soffset\s" + str, RegexOptions.Compiled);
                var regexXREF = new Regex(@"(^\s+;[a-zA-Z:\s]*\s(sub_\w+|X2\w+|w+))", RegexOptions.Compiled);
                for (var index = 0; index < InListSource.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListSource[index]);
                    if (matches.Count <= 0)
                    {
                        continue;
                    }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        //var regexXREF = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListSource[tmpIdx]);
                        if (matchesXREF.Count <= 0)
                        {
                            continue;
                        }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);

                    XrefsIn.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++; // следующий номер пакета

                    var regex2 = new Regex(@"^(\S+)", RegexOptions.IgnoreCase);
                    var matches2 = regex2.Matches(InListSource[index]);
                    foreach (var match2 in matches2)
                    {
                        ListNameSourceSC.Add(match2.ToString()); // сохранили имя
                        foundName = true;
                    }

                    if (!foundName)
                    {
                        // не нашли имя пакета, бывает что его нет из-зи защиты themida
                        ListNameSourceSC.Add("CS_Unknown"); // сохранили адрес подпрограммы
                    }

                    // сначала нужно пропустить строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    do
                    {
                        index++;
                        var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.IgnoreCase);
                        var matchesSpace40 = regexSpace40.Matches(InListSource[index]);
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

                    // ищем "dd offset sub_395D0370"
                    // dd offset nullsub_18
                    // dd offset CSInteractGimmickPacket
                    // dd offset CSGmCommandPacket
                    var regexBody = new Regex(@"(dd\soffset\snullsub|dd\soffset\ssub_\w+|dd\soffset\s\w+)",
                        RegexOptions.Compiled);
                    var matchesBodys = regexBody.Match(InListSource[index]);
                    ListSubSourceSC.Add(matchesBodys.ToString().Substring(10)); // сохранили адрес подпрограммы
                }

                // закончили предварительную работу по поиску имен и ссылок на подпрограммы со структурами
                var lnCount = ListNameSourceSC.Count;
                TextBox16.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16.Text = lnCount.ToString(); }));
                var lsCount = ListSubSourceSC.Count;
                TextBox17.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17.Text = lsCount.ToString(); }));
                ListView12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView12.ItemsSource = ListNameSourceSC; }));
                ListView13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView13.ItemsSource = ListSubSourceSC; }));

                ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Maximum = ListNameSourceSC.Count; }));
                if (FindStructIn)
                {
                    //
                    // начали предварительную работу по поиску структур пакетов
                    //
                    // начнем с начала файла
                    var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
                    var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s+(sub_\w+)|(call\s+(\w+)))",
                        RegexOptions.Compiled);
                    /*
                       sub_395D3050    proc near               ; CODE XREF: sub_391DE5C0+47↑p
                       push    offset aBc      ; "bc"
                       call    sub_395E18A0
                       call    sub_395E1730
                       call    sub_395E16B0
                       push    offset aAction  ; "action"
                       sub_395D3050    endp              
                       
                       sub_395E18A0    proc near               ; CODE XREF: .text:394B5DEC↑p
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aModified ; "modified"
                       sub_395E18A0    endp
                       
                       sub_395E1730    proc near               ; CODE XREF: .text:394B1C67↑p
                       push    offset aType    ; "type"
                       push    offset asc_396AFCE0 ; "x"
                       push    offset aY       ; "y"
                       push    offset aZ_0     ; "z"
                       push    offset aModified ; "modified"
                       sub_395E1730    endp
                       
                       sub_395E16B0    proc near               ; CODE XREF: .text:394B1CB7↑p
                       push    offset aType    ; "type"
                       push    offset aData    ; "data"
                       push    offset aData    ; "data"
                       push    offset aModified ; "modified"
                       sub_395E16B0    endp
                       
                    */

                    for (var i = 0; i < ListSubSourceSC.Count; i++)
                    {
                        var found = false;
                        var regexSub = new Regex(@"^" + ListSubSourceSC[i], RegexOptions.Compiled); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListSource.Count; index++)
                        {
                            var matchesSub = regexSub.Matches(InListSource[index]);
                            if (matchesSub.Count <= 0)
                            {
                                continue;
                            }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var matchesCalls = regexCall.Matches(InListSource[index]);
                                foreach (var matchCall in matchesCalls)
                                {
                                    if (matchCall.ToString().Length >= 4 &&
                                        matchCall.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureIn(matchCall.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(
                                                findList); // сохранили несколько строк структуры пакета найденной в подпрограмме
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(matchCall.ToString()); // сохранили одну строку структуры пакета
                                    }
                                }

                                var matchesEndP = regexEndP.Matches(InListSource[index]);
                                if (matchesEndP.Count <= 0)
                                {
                                    continue;
                                }

                                foundEndp = true;
                            } while (!foundEndp);

                            StructureSourceSC.Add(i, lst); // сохранили всю структуру пакета
                            found = true; // нашли структуру
                            break;
                        }

                        if (!found)
                        {
                            // не нашли структуру
                            var lst = new List<string>();
                            StructureSourceSC.Add(i, lst); // сохраним пустой список, так как ничего не нашли 
                        }

                        ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Value = StructureSourceSC.Count; }));
                    }
                }
            }

            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = true; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
            BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));

            _isInSc = true;
            if (_isInSc && _isOutSc)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = true; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }

            ButtonSaveIn2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveIn2.IsEnabled = true; }));
            stopWatch.Stop();
            TextBox19.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19.Text = stopWatch.Elapsed.ToString(); }));
            Label_Semafor1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor1.Background = Brushes.GreenYellow; }));
            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = true; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = true; }));
        }

        private void FindDestinationStructuresCS(string str)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            // уничтожаем ненужный список
            StructureDestinationCS = new Dictionary<int, List<string>>();
            ListNameDestinationCS = new List<string>();
            ListSubDestinationCS = new List<string>();
            XrefsOut = new Dictionary<int, List<string>>();

            TextBox23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox23.Text = "0"; }));
            TextBox24.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox24.Text = "0"; }));
            TextBox28.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox28.Text = "0"; }));
            ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Value = 0; }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.Yellow; }));

            //
            // начали предварительную работу по поиску имен и ссылок на подпрограммы со структурами
            //
            //"CS_PACKET_TODAY_ASSIGNMENT_11Ah dd offset SC_PACKETS_return_2"
            var indexRefs = 0;

            // Создаем объект для блокировки.
            var lockObj = new object();
            // Блокируем объект.
            lock (lockObj)
            {
                var regex = new Regex(@"^[a-zA-Z0-9_?@]+\s+dd\soffset\s" + str, RegexOptions.Compiled);
                var regexXREF = new Regex(@"(^\s+;[a-zA-Z:\s]*\s(sub_\w+|X2\w+))", RegexOptions.Compiled);
                for (var index = 0; index < InListDestination.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListDestination[index]);
                    if (matches.Count <= 0)
                    {
                        continue;
                    }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        //var regexXREF = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListDestination[tmpIdx]);
                        if (matchesXREF.Count <= 0)
                        {
                            continue;
                        }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);

                    XrefsOut.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++; // следующий номер пакета

                    var regex2 = new Regex(@"^(\S+)", RegexOptions.IgnoreCase);
                    var matches2 = regex2.Matches(InListDestination[index]);
                    foreach (var match2 in matches2)
                    {
                        ListNameDestinationCS.Add(match2.ToString()); // сохранили имя
                        foundName = true;
                    }

                    if (!foundName)
                    {
                        // не нашли имя пакета, бывает что его нет из-зи защиты themida
                        ListNameDestinationCS.Add("CS_Unknown"); // сохранили адрес подпрограммы
                    }

                    // сначала нужно пропустить строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    do
                    {
                        index++;
                        var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.IgnoreCase);
                        var matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
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

                    // ищем "dd offset sub_395D0370"
                    // dd offset nullsub_18
                    // dd offset CSInteractGimmickPacket
                    // dd offset CSGmCommandPacket
                    var regexBody = new Regex(@"(dd\soffset\snullsub|dd\soffset\ssub_\w+|dd\soffset\s\w+)", RegexOptions.Compiled);
                    var matchesBodys = regexBody.Match(InListDestination[index]);
                    ListSubDestinationCS.Add(matchesBodys.ToString().Substring(10)); // сохранили адрес подпрограммы
                }

                // закончили предварительную работу по поиску имен и ссылок на подпрограммы со структурами
                var lnCount = ListNameDestinationCS.Count;
                TextBox23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox23.Text = lnCount.ToString(); }));
                var lsCount = ListSubDestinationCS.Count;
                TextBox24.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox24.Text = lsCount.ToString(); }));
                ListView22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView22.ItemsSource = ListNameDestinationCS; }));
                ListView23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView23.ItemsSource = ListSubDestinationCS; }));

                ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Maximum = ListNameDestinationCS.Count; }));
                if (FindStructOut)
                {
                    //
                    // начали предварительную работу по поиску структур пакетов
                    //
                    // начнем с начала файла
                    var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
                    var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))",
                        RegexOptions.Compiled);
                    /*
                       sub_395D3050    proc near               ; CODE XREF: sub_391DE5C0+47↑p
                       push    offset aBc      ; "bc"
                       call    sub_395E18A0
                       call    sub_395E1730
                       call    sub_395E16B0
                       push    offset aAction  ; "action"
                       sub_395D3050    endp              
                       
                       sub_395E18A0    proc near               ; CODE XREF: .text:394B5DEC↑p
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aModified ; "modified"
                       sub_395E18A0    endp
                       
                       sub_395E1730    proc near               ; CODE XREF: .text:394B1C67↑p
                       push    offset aType    ; "type"
                       push    offset asc_396AFCE0 ; "x"
                       push    offset aY       ; "y"
                       push    offset aZ_0     ; "z"
                       push    offset aModified ; "modified"
                       sub_395E1730    endp
                       
                       sub_395E16B0    proc near               ; CODE XREF: .text:394B1CB7↑p
                       push    offset aType    ; "type"
                       push    offset aData    ; "data"
                       push    offset aData    ; "data"
                       push    offset aModified ; "modified"
                       sub_395E16B0    endp
                       
                    */

                    for (var i = 0; i < ListSubDestinationCS.Count; i++)
                    {
                        var found = false;
                        var regexSub = new Regex(@"^" + ListSubDestinationCS[i], RegexOptions.Compiled); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListDestination.Count; index++)
                        {
                            var matchesSub = regexSub.Matches(InListDestination[index]);
                            if (matchesSub.Count <= 0)
                            {
                                continue;
                            }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var matchesCalls = regexCall.Matches(InListDestination[index]);
                                foreach (var matchCall in matchesCalls)
                                {
                                    if (matchCall.ToString().Length >= 4 &&
                                        matchCall.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureOut(matchCall.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(
                                                findList); // сохранили несколько строк структуры пакета найденной в подпрограмме
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(matchCall.ToString()); // сохранили одну строку структуры пакета
                                    }
                                }

                                var matchesEndP = regexEndP.Matches(InListDestination[index]);
                                if (matchesEndP.Count <= 0)
                                {
                                    continue;
                                }

                                foundEndp = true;
                            } while (!foundEndp);

                            StructureDestinationCS.Add(i, lst); // сохранили всю структуру пакета
                            found = true; // нашли структуру
                            break;
                        }

                        if (!found)
                        {
                            // не нашли структуру
                            var lst = new List<string>();
                            StructureDestinationCS.Add(i, lst); // сохраним пустой список, так как ничего не нашли 
                        }

                        ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Value = StructureDestinationCS.Count; }));
                    }
                }
            }

            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = true; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = true; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = true; }));
            //BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));

            _isOutCs = true;
            if (_isInCs && _isOutCs)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = true; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }

            ButtonSaveOut1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveOut1.IsEnabled = true; }));
            stopWatch.Stop();
            TextBox28.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox28.Text = stopWatch.Elapsed.ToString(); }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.GreenYellow; }));
        }

        private void FindDestinationStructuresSC(string str)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            // уничтожаем ненужный список
            StructureDestinationSC = new Dictionary<int, List<string>>();
            ListNameDestinationSC = new List<string>();
            ListSubDestinationSC = new List<string>();
            XrefsOut = new Dictionary<int, List<string>>();

            TextBox26.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox26.Text = "0"; }));
            TextBox27.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox27.Text = "0"; }));
            TextBox29.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox29.Text = "0"; }));
            ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Value = 0; }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.Yellow; }));
            ButtonSaveOut1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveOut1.IsEnabled = false; }));
            ButtonSaveOut2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveOut2.IsEnabled = false; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = false; }));
            //BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = false; }));
            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = false; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = false; }));

            //
            // начали предварительную работу по поиску имен и ссылок на подпрограммы со структурами
            //
            //"CS_PACKET_TODAY_ASSIGNMENT_11Ah dd offset SC_PACKETS_return_2"
            var indexRefs = 0;

            // Создаем объект для блокировки.
            var lockObj = new object();
            // Блокируем объект.
            lock (lockObj)
            {
                var regex = new Regex(@"^[a-zA-Z0-9_?@]+\s+dd\soffset\s" + str, RegexOptions.Compiled);
                var regexXREF = new Regex(@"(^\s+;[a-zA-Z:\s]*\s(sub_\w+|X2\w+|w+))", RegexOptions.Compiled);
                for (var index = 0; index < InListDestination.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListDestination[index]);
                    if (matches.Count <= 0)
                    {
                        continue;
                    }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        //var regexXREF = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListDestination[tmpIdx]);
                        if (matchesXREF.Count <= 0)
                        {
                            continue;
                        }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);

                    XrefsOut.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++; // следующий номер пакета

                    var regex2 = new Regex(@"^(\S+)", RegexOptions.IgnoreCase);
                    var matches2 = regex2.Matches(InListDestination[index]);
                    foreach (var match2 in matches2)
                    {
                        ListNameDestinationSC.Add(match2.ToString()); // сохранили имя
                        foundName = true;
                    }

                    if (!foundName)
                    {
                        // не нашли имя пакета, бывает что его нет из-зи защиты themida
                        ListNameDestinationSC.Add("SC_Unknown"); // сохранили адрес подпрограммы
                    }

                    // сначала нужно пропустить строки с начальными пробелами [40]; DATA XREF: sub_39015740+1A↑o, таких строк 1 или 2
                    do
                    {
                        index++;
                        var regexSpace40 = new Regex(@"^\s{40}", RegexOptions.IgnoreCase);
                        var matchesSpace40 = regexSpace40.Matches(InListDestination[index]);
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

                    // ищем "dd offset sub_395D0370"
                    // dd offset nullsub_18
                    // dd offset CSInteractGimmickPacket
                    // dd offset CSGmCommandPacket
                    var regexBody = new Regex(@"(dd\soffset\snullsub|dd\soffset\ssub_\w+|dd\soffset\s\w+)",
                        RegexOptions.Compiled);
                    var matchesBodys = regexBody.Match(InListDestination[index]);
                    ListSubDestinationSC.Add(matchesBodys.ToString().Substring(10)); // сохранили адрес подпрограммы
                }

                // закончили предварительную работу по поиску имен и ссылок на подпрограммы со структурами
                var lnCount = ListNameDestinationSC.Count;
                TextBox26.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox26.Text = lnCount.ToString(); }));
                var lsCount = ListSubDestinationSC.Count;
                TextBox27.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox27.Text = lsCount.ToString(); }));
                ListView22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView22.ItemsSource = ListNameDestinationSC; }));
                ListView23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView23.ItemsSource = ListSubDestinationSC; }));

                ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Maximum = ListNameDestinationSC.Count; }));

                if (FindStructOut)
                {
                    //
                    // начали предварительную работу по поиску структур пакетов
                    //
                    // начнем с начала файла
                    var regexEndP = new Regex(@"\s+endp\s*", RegexOptions.Compiled); // ищем конец подпрограммы
                    var regexCall = new Regex(@"(\x22[0-z._]+\x22)|(call\s+(sub_\w+)|(call\s+(\w+)))",
                        RegexOptions.Compiled);
                    /*
                       sub_395D3050    proc near               ; CODE XREF: sub_391DE5C0+47↑p
                       push    offset aBc      ; "bc"
                       call    sub_395E18A0
                       call    sub_395E1730
                       call    sub_395E16B0
                       push    offset aAction  ; "action"
                       sub_395D3050    endp              
                       
                       sub_395E18A0    proc near               ; CODE XREF: .text:394B5DEC↑p
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aType    ; "type"
                       push    offset aModified ; "modified"
                       sub_395E18A0    endp
                       
                       sub_395E1730    proc near               ; CODE XREF: .text:394B1C67↑p
                       push    offset aType    ; "type"
                       push    offset asc_396AFCE0 ; "x"
                       push    offset aY       ; "y"
                       push    offset aZ_0     ; "z"
                       push    offset aModified ; "modified"
                       sub_395E1730    endp
                       
                       sub_395E16B0    proc near               ; CODE XREF: .text:394B1CB7↑p
                       push    offset aType    ; "type"
                       push    offset aData    ; "data"
                       push    offset aData    ; "data"
                       push    offset aModified ; "modified"
                       sub_395E16B0    endp
                       
                    */

                    for (var i = 0; i < ListSubDestinationSC.Count; i++)
                    {
                        var found = false;
                        var regexSub = new Regex(@"^" + ListSubDestinationSC[i], RegexOptions.Compiled); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListDestination.Count; index++)
                        {
                            var matchesSub = regexSub.Matches(InListDestination[index]);
                            if (matchesSub.Count <= 0)
                            {
                                continue;
                            }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var matchesCalls = regexCall.Matches(InListDestination[index]);
                                foreach (var matchCall in matchesCalls)
                                {
                                    if (matchCall.ToString().Length >= 4 &&
                                        matchCall.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureOut(matchCall.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(
                                                findList); // сохранили несколько строк структуры пакета найденной в подпрограмме
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(matchCall.ToString()); // сохранили одну строку структуры пакета
                                    }
                                }

                                var matchesEndP = regexEndP.Matches(InListDestination[index]);
                                if (matchesEndP.Count <= 0)
                                {
                                    continue;
                                }

                                foundEndp = true;
                            } while (!foundEndp);

                            StructureDestinationSC.Add(i, lst); // сохранили всю структуру пакета
                            found = true; // нашли структуру
                            break;
                        }

                        if (!found)
                        {
                            // не нашли структуру
                            var lst = new List<string>();
                            StructureDestinationSC.Add(i, lst); // сохраним пустой список, так как ничего не нашли 
                        }

                        ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Value = StructureDestinationSC.Count; }));
                    }
                }
            }

            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = true; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = true; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = true; }));
            //BtnLoadIn_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn_Copy.IsEnabled = true; }));

            _isOutSc = true;
            if (_isInSc && _isOutSc)
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = true; }));
            }
            else
            {
                ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
                ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            }

            ButtonSaveOut2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonSaveOut2.IsEnabled = true; }));
            stopWatch.Stop();
            TextBox29.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox29.Text = stopWatch.Elapsed.ToString(); }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.GreenYellow; }));
            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = true; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = true; }));
        }

        private void btn_Load_In_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor1.Background = Brushes.Red;
            BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;
            ButtonSaveIn1.IsEnabled = false;
            ButtonSaveIn2.IsEnabled = false;

            if (OpenFileDialog1())
            {
                TextBoxPathIn.Text = FilePathIn1;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                // Создаем объект для блокировки.
                var lockObj = new object();
                lock (lockObj)
                {
                    InListSource = new List<string>();
                    // чтение из файла
                    InListSource = File.ReadAllLines(FilePathIn1).ToList();
                    // заполним ListView
                    ListView11.ItemsSource = InListSource;
                }

                // инициализируем прогрессбары и списки
                InitializeIn();

                isCleaningIn = CheckBoxCleaningIn.IsChecked == true;
                if (isCleaningIn)
                {
                    new Thread(() =>
                    {
                        CleanSource();
                    }).Start();
                }
                else
                {
                    BtnCsLoadNameIn.IsEnabled = true;
                    BtnScLoadNameIn.IsEnabled = true;
                    BtnLoadIn_Copy.IsEnabled = true;
                    BtnLoadIn.IsEnabled = true;
                }

                stopWatch.Stop();
                TextBox15.Text = stopWatch.Elapsed.ToString();
                isCompareCS = false;
                isCompareSC = false;
                Label_Semafor1.Background = Brushes.Yellow;
            }
            else
            {
                MessageBox.Show("Для работы программы необходимо выбрать .asm файл!");
                BtnLoadIn_Copy.IsEnabled = true;
                BtnLoadIn.IsEnabled = true;
            }
        }

        private void InitializeIn()
        {
            // инициализируем прогрессбары и списки
            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = 0; }));
            ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Value = 0; }));
            ProgressBar13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar13.Value = 0; }));

            ListView12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView12.ItemsSource = new List<string>(); }));
            ListView13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView14.ItemsSource = new List<string>(); }));
            ListView14.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView13.ItemsSource = new List<string>(); }));

            ListView31.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView31.ItemsSource = new List<string>(); }));
            ListView32.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView32.ItemsSource = new List<string>(); }));

            TextBox13.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox13.Text = "0"; }));
            TextBox14.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox14.Text = "0"; }));
            TextBox18.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox18.Text = "0"; }));

            TextBox16.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16.Text = "0"; }));
            TextBox17.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17.Text = "0"; }));
            TextBox19.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19.Text = "0"; }));

            TextBox16Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy.Text = "0"; }));
            TextBox17Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy.Text = "0"; }));
            TextBox19Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy.Text = "0"; }));

            TextBox32.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox32.Text = "0"; }));
            TextBox33.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox33.Text = "0"; }));

            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
            Button2Copy2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Button2Copy2.IsEnabled = false; }));
            Button2Copy2_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Button2Copy2_Copy.IsEnabled = false; }));

            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            ButtonCopy2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCopy2.IsEnabled = false; }));
            ButtonCopy2_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCopy2_Copy.IsEnabled = false; }));
        }

        private void InitializeOut()
        {
            // инициализируем прогрессбары и списки
            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = 0; }));
            ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Value = 0; }));
            ProgressBar23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar23.Value = 0; }));

            ListView22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView22.ItemsSource = new List<string>(); }));
            ListView23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView24.ItemsSource = new List<string>(); }));
            ListView24.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView23.ItemsSource = new List<string>(); }));

            ListView31.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView31.ItemsSource = new List<string>(); }));
            ListView32.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView32.ItemsSource = new List<string>(); }));

            TextBox23.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox23.Text = "0"; }));
            TextBox24.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox24.Text = "0"; }));
            TextBox28.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox28.Text = "0"; }));

            TextBox26.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox26.Text = "0"; }));
            TextBox27.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox27.Text = "0"; }));
            TextBox29.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox29.Text = "0"; }));

            TextBox16Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox16Copy1.Text = "0"; }));
            TextBox17Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox17Copy1.Text = "0"; }));
            TextBox19Copy1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19Copy1.Text = "0"; }));

            TextBox32.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox32.Text = "0"; }));
            TextBox33.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox33.Text = "0"; }));
        
            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = false; }));
            Button2Copy2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Button2Copy2.IsEnabled = false; }));
            Button2Copy2_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Button2Copy2_Copy.IsEnabled = false; }));

            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = false; }));
            ButtonCopy2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCopy2.IsEnabled = false; }));
            ButtonCopy2_Copy.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCopy2_Copy.IsEnabled = false; }));
        }

        private void btn_SC_Load_Name1_Click(object sender, RoutedEventArgs e)
        {
            
            InitializeIn();

            BtnCsLoadNameIn.IsEnabled = false;
            BtnScLoadNameIn.IsEnabled = false;
            BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;
            var inText = TextBox12.Text;
            DepthIn = 0;

            ListNameSourceSC = new List<string>();
            FindOpcodeIn = CheckBoxFindOpcodeIn.IsChecked == true;
            FindStructIn = CheckBoxFindStructIn.IsChecked == true;
            _isInSc = false;
            //_isOutSc = false;
            _isInCs = false;
            //_isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            new Thread(() =>
            {
                FindSourceStructuresSC(inText);

                if (FindOpcodeIn)
                {
                    FindOpcodeSourceSC();
                }
            }).Start();
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
                // Создаем объект для блокировки.
                var lockObj = new object();
                lock (lockObj)
                {
                    new Thread(() =>
                    {
                        PreCleanSource();
                    }).Start();
                }

                stopWatch.Stop();
                TextBox15.Text = stopWatch.Elapsed.ToString();
                isCompareCS = false;
                isCompareSC = false;
                Label_Semafor1.Background = Brushes.Yellow;
            }
            else
            {
                MessageBox.Show("Для работы программы необходимо выбрать .asm файл!");
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
            var inText = TextBox11.Text;
            DepthIn = 0;

            ListNameSourceCS = new List<string>();
            FindOpcodeIn = CheckBoxFindOpcodeIn.IsChecked == true;
            FindStructIn = CheckBoxFindStructIn.IsChecked == true;
            _isInSc = false;
            //_isOutSc = false;
            _isInCs = false;
            //_isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            new Thread(() =>
            {
                FindSourceStructuresCS(inText);

                if (FindOpcodeIn)
                {
                    FindOpcodeSourceCS();
                }
            }).Start();
        }

        private void btn_Load_Out_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor2.Background = Brushes.Red;
            //BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;

            if (OpenFileDialog2())
            {
                TextBoxPathOut.Text = FilePathIn2;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                // Создаем объект для блокировки.
                var lockObj = new object();
                lock (lockObj)
                {
                    InListDestination = new List<string>();
                    // чтение из файла
                    InListDestination = File.ReadAllLines(FilePathIn2).ToList();
                    // заполним ListView
                    ListView21.ItemsSource = InListDestination;
                }

                // инициализируем прогрессбары и списки
                InitializeOut();

                isCleaningOut = CheckBoxCleaningOut.IsChecked == true;
                if (isCleaningOut)
                {
                    new Thread(() =>
                    {
                        CleanDestination();
                    }).Start();
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
                MessageBox.Show("Для работы программы необходимо выбрать .asm файл!");
                //BtnLoadIn_Copy.IsEnabled = true;
                BtnLoadOut.IsEnabled = true;
            }
        }

        private void btn_SC_Load_Name2_Click(object sender, RoutedEventArgs e)
        {
            
            // инициализируем прогрессбары и списки
            InitializeOut();

            BtnCsLoadNameOut.IsEnabled = false;
            BtnScLoadNameOut.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;
            var outText = TextBox22.Text;
            DepthOut = 0;

            ListNameDestinationSC = new List<string>();
            FindOpcodeOut = CheckBoxFindOpcodeOut.IsChecked == true;
            FindStructOut = CheckBoxFindStructOut.IsChecked == true;
            //_isInSc = false;
            _isOutSc = false;
            // = false;
            _isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            new Thread(() =>
            {
                FindDestinationStructuresSC(outText);

                if (FindOpcodeOut)
                {
                    FindOpcodeDestinationSC();
                }
            }).Start();
        }

        private void btn_CS_Load_Name2_Click(object sender, RoutedEventArgs e)
        {
            
            // инициализируем прогрессбары и списки
            InitializeOut();

            BtnCsLoadNameOut.IsEnabled = false;
            BtnScLoadNameOut.IsEnabled = false;
            //BtnLoadIn_Copy.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;
            var inText = TextBox21.Text;
            DepthIn = 0;

            ListNameDestinationCS = new List<string>();
            FindOpcodeOut = CheckBoxFindOpcodeOut.IsChecked == true;
            FindStructOut = CheckBoxFindStructOut.IsChecked == true;
            //_isInSc = false;
            _isOutSc = false;
            //_isInCs = false;
            _isOutCs = false;
            isCompareCS = false;
            isCompareSC = false;

            new Thread(() =>
            {
                FindDestinationStructuresCS(inText);

                if (FindOpcodeOut)
                {
                    FindOpcodeDestinationCS();
                }
            }).Start();
        }

        private void CompareSourceStructuresCS(ref List<string> listNameSource, ref List<string> listNameDestination,
            ref List<string> listSubDestination, ref Dictionary<int, List<string>> dictSource,
            ref Dictionary<int, List<string>> dictDestination, List<string> listOpcodes)
        {
            // подготовим список
            ListNameCompareCS = new List<string>();
            foreach (var t in listNameDestination)
            {
                ListNameCompareCS.Add(t);
            }

            InUseIn = new Dictionary<int, int>();
            InUseOut = new Dictionary<int, int>();
            IsRenameDestination = new Dictionary<int, bool>();
            //var listCompare = new List<string>();
            //var foundNamePartial = false;
            var foundName = false;
            //var result = false;
            var badFound = 0;
            //var skipStr = 0;
            var repeat = true;

            // список структуры текущего сравнения имен
            //var ddList = new List<string>();
            //var dsList = new List<string>();
            // длины списков, могут отличаться, скорее всего список неизвестных имен длиннее, так как более новая версия
            var lenDestinationListName = listNameDestination.Count;
            var lenSourceListName = listNameSource.Count;
            // начнем с начала
            IdxD = 0;

            // Создаем объект для блокировки.
            var lockObj = new object();
            // Блокируем объект.
            lock (lockObj)
            {
                if (IdxD >= dictDestination.Count)
                {
                    IdxD = dictDestination.Count - 1;
                }

                //
                // начали предварительную работу по поиску структур пакетов
                //
                // начнем с начала файла
                do // проходим по списку имён? которые нужно найти, т.е. Destination
                {
                    // возьмем следующую структуру, для которой нужно найти новое имя
                    var ddList = dictDestination[IdxD];
                    //if (ddList.Count == 0)
                    //{
                    //    IdxD++;
                    //    repeat = true; // нужно будет повторять поиск
                    //    continue; // пропускаем пустые структуры
                    //}
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
                                    //foundName = false; // делаем пустые структуры непохожими
                                }
                                //else if (ddList.Count == 1 && dsList.Count == 1 && ddList[0] == dsList[0] &&
                                //         (ddList[0] == "\"type\"" || ddList[0] == "\"bc\""))
                                //{
                                //    foundName = false; // делаем структуры с одним полем "type" - непохожими
                                //}
                                else
                                {
                                    // количество строк в структурах совпадает
                                    for (var i = 0; i < ddList.Count; i++)
                                    {
                                        // сверим на одинаковость
                                        if (ddList[i] == dsList[i])
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

                                    // запишем новое имя на место неизвестного, которое нашли
                                    if (CheckBoxRemoveOpcode.IsChecked == true)
                                    {
                                        // удаляем оконечные опкоды в имени пакета
                                        var offset = listNameSource[IdxS].LastIndexOf("_", StringComparison.Ordinal);
                                        if (offset > 0)
                                        {
                                            var nameSource = listNameSource[IdxS].Substring(0, offset);
                                            ListNameCompareCS[IdxD] = nameSource;
                                        }
                                        else
                                        {
                                            ListNameCompareCS[IdxD] = listNameSource[IdxS];
                                        }
                                    }
                                    else
                                    {
                                        ListNameCompareCS[IdxD] = listNameSource[IdxS];
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
        }

        private void CompareSourceStructuresSC(ref List<string> listNameSource, ref List<string> listNameDestination,
            ref List<string> listSubDestination, ref Dictionary<int, List<string>> dictSource,
            ref Dictionary<int, List<string>> dictDestination, List<string> listOpcodes)
        {
            // подготовим список
            ListNameCompareSC = new List<string>();
            foreach (var t in listNameDestination)
            {
                ListNameCompareSC.Add(t);
            }

            InUseIn = new Dictionary<int, int>();
            InUseOut = new Dictionary<int, int>();
            IsRenameDestination = new Dictionary<int, bool>();
            //var listCompare = new List<string>();
            //var foundNamePartial = false;
            var foundName = false;
            //var result = false;
            var badFound = 0;
            //var skipStr = 0;
            var repeat = true;

            // список структуры текущего сравнения имен
            //var ddList = new List<string>();
            //var dsList = new List<string>();
            // длины списков, могут отличаться, скорее всего список неизвестных имен длиннее, так как более новая версия
            var lenDestinationListName = listNameDestination.Count;
            var lenSourceListName = listNameSource.Count;
            // начнем с начала
            IdxD = 0;

            // Создаем объект для блокировки.
            var lockObj = new object();
            // Блокируем объект.
            lock (lockObj)
            {
                if (IdxD >= dictDestination.Count)
                {
                    IdxD = dictDestination.Count - 1;
                }

                //
                // начали предварительную работу по поиску структур пакетов
                //
                // начнем с начала файла
                do // проходим по списку имён? которые нужно найти, т.е. Destination
                {
                    // возьмем следующую структуру, для которой нужно найти новое имя
                    var ddList = dictDestination[IdxD];
                    //if (ddList.Count == 0)
                    //{
                    //    IdxD++;
                    //    repeat = true; // нужно будет повторять поиск
                    //    continue; // пропускаем пустые структуры
                    //}
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
                                    //foundName = false; // делаем пустые структуры непохожими
                                }
                                //else if (ddList.Count == 1 && dsList.Count == 1 && ddList[0] == dsList[0] &&
                                //         (ddList[0] == "\"type\"" || ddList[0] == "\"bc\""))
                                //{
                                //    foundName = false; // делаем структуры с одним полем "type" - непохожими
                                //}
                                else
                                {
                                    // количество строк в структурах совпадает
                                    for (var i = 0; i < ddList.Count; i++)
                                    {
                                        // сверим на одинаковость
                                        if (ddList[i] == dsList[i])
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

                                    // запишем новое имя на место неизвестного, которое нашли
                                    if (CheckBoxRemoveOpcode.IsChecked == true)
                                    {
                                        // удаляем оконечные опкоды в имени пакета
                                        var offset = listNameSource[IdxS].LastIndexOf("_", StringComparison.Ordinal);
                                        if (offset > 0)
                                        {
                                            var nameSource = listNameSource[IdxS].Substring(0, offset);
                                            ListNameCompareSC[IdxD] = nameSource;
                                        }
                                        else
                                        {
                                            ListNameCompareSC[IdxD] = listNameSource[IdxS];
                                        }
                                    }
                                    else
                                    {
                                        ListNameCompareSC[IdxD] = listNameSource[IdxS];
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
        }

        private void button2_Copy1_Click(object sender, RoutedEventArgs e)
        {
            // пробуем сравнивать структуры пакетов
            if (!isCompareCS)
            {
                // результат работы метода в ListNameCompareCS
                CompareSourceStructuresCS(ref ListNameSourceCS, ref ListNameDestinationCS, ref ListSubDestinationCS, ref StructureSourceCS, ref StructureDestinationCS, ListOpcodeDestinationCS);
                // сравнение пакетов проведено
            }
            else
            {
                ListNameCompareCS = new List<string>(ListNameCompare);
            }

            ListView31.ItemsSource = ListNameCompareCS;

            if (CheckBoxCompareManual.IsChecked == true)
            {
                CompareWindow.isRemoveOpcode = isRemoveOpcode;
                var compareWindow = new CompareWindow();
                compareWindow.Show();
                compareWindow.CompareSourceStructures(ref ListNameSourceCS, ref ListNameDestinationCS, ref ListNameCompareCS, ref ListSubDestinationCS, ref StructureSourceCS, ref StructureDestinationCS, ListOpcodeDestinationCS);
                isCompareCS = true;
            }

            ListNameCompareOutCS = new List<string>();
            var idxD = 0;
            foreach (var t in ListNameCompareCS)
            {
                if (ListOpcodeDestinationCS.Count > 0)
                {
                    ListNameCompareOutCS.Add(t + "_" + ListOpcodeDestinationCS[idxD]);
                }
                else
                {
                    ListNameCompareOutCS.Add(t + "_" + "0xfff");
                }

                idxD++;
            }

            ListView32.ItemsSource = ListNameCompareOutCS;

            if (CheckBoxRename.IsChecked == true)
            {
                // сохраняем новые имена в исходник
                // InListDestination = ListNameDestinationCS <- ListNameCompareOutCS
                new Thread(() =>
                {
                    RenamePackets(ListNameDestinationCS, ListNameCompareOutCS);
                }).Start();
            }
            Button2Copy2.IsEnabled = true;
            Button2Copy2_Copy.IsEnabled = true;
        }

        private void button_Copy1_Click(object sender, RoutedEventArgs e)
        {
            // пробуем сравнивать структуры пакетов
            if (!isCompareSC)
            {
                // результат работы метода в ListNameCompareSC
                CompareSourceStructuresSC(ref ListNameSourceSC, ref ListNameDestinationSC, ref ListSubDestinationSC, ref StructureSourceSC, ref StructureDestinationSC, ListOpcodeDestinationSC);
                // сравнение пакетов проведено
            }
            else
            {
                ListNameCompareSC = new List<string>(ListNameCompare);
            }

            ListView31.ItemsSource = ListNameCompareSC;

            if (CheckBoxCompareManual.IsChecked == true)
            {
                var compareWindow = new CompareWindow();
                compareWindow.Show();
                compareWindow.CompareSourceStructures(ref ListNameSourceSC, ref ListNameDestinationSC, ref ListNameCompareSC, ref ListSubDestinationSC, ref StructureSourceSC, ref StructureDestinationSC, ListOpcodeDestinationSC);
                isCompareSC = true;
            }

            ListNameCompareOutSC = new List<string>();
            var idxD = 0;
            foreach (var t in ListNameCompareSC)
            {
                if (ListOpcodeDestinationSC.Count > 0)
                {
                    ListNameCompareOutSC.Add(t + "_" + ListOpcodeDestinationSC[idxD]);
                }
                else
                {
                    ListNameCompareOutSC.Add(t + "_" + "0xfff");
                }

                idxD++;
            }

            ListView32.ItemsSource = ListNameCompareOutSC;

            if (CheckBoxRename.IsChecked == true)
            {
                // сохраняем новые имена в исходник
                // InListDestination = ListNameDestinationCS <- ListNameCompareOutCS
                new Thread(() =>
                {
                    RenamePackets(ListNameDestinationSC, ListNameCompareOutSC);
                }).Start();
            }
            ButtonCopy2.IsEnabled = true;
            ButtonCopy2_Copy.IsEnabled = true;
        }

        private void RenamePackets(List<string> listName, List<string> listNameCompare)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.Yellow; }));
            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = 0; }));
            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Maximum = listName.Count; }));
            //
            // начали работу по поиску имен пакетов
            //
            for (int i = 0; i < listName.Count; i++)
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
                ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = i; }));
            }
            stopWatch.Stop();
            TextBox25.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox25.Text = stopWatch.Elapsed.ToString(); }));
            ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Value = listName.Count; }));
            Label_Semafor2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { Label_Semafor2.Background = Brushes.GreenYellow; }));
            listName = listNameCompare;
            ListView22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView22.ItemsSource = listName; }));
        }

        private string FilePathOut3 = "";

        private void button2_Copy2_Click(object sender, RoutedEventArgs e)
        {
            var tmp = new List<string>();
            for (var i = 0; i < ListNameCompareCS.Count; i++)
            {
                if (ListOpcodeDestinationCS.Count > 0)
                {
                    var lst = "Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + ListSubDestinationCS[i] + ", Opcode: " + ListOpcodeDestinationCS[i];
                    tmp.Add(lst);
                }
                else
                {
                    var lst = "Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + ListSubDestinationCS[i] + ", Opcode: 0xfff";
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
                if (ListOpcodeDestinationSC.Count > 0)
                {
                    var lst = "Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + ListSubDestinationSC[i] + ", Opcode: " + ListOpcodeDestinationSC[i];
                    tmp.Add(lst);
                }
                else
                {
                    var lst = "Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + ListSubDestinationSC[i] + ", Opcode: 0xfff";
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
            var tmp = new List<string>();
            var ss = 0;
            var dd = 0;
            var key = 0;
            var lst = "";
            List<string> src;
            List<string> dst;
            for (var i = 0; i < ListNameCompareCS.Count; i++)
            {
                ss = 0;
                dd = 0;
                if (ListOpcodeDestinationCS.Count > 0)
                {
                    lst = i + 1 + ": Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + ListSubDestinationCS[i] + ", Opcode: " + ListOpcodeDestinationCS[i];
                }
                else
                {
                    lst = i + 1 + ": Packet name: " + ListNameCompareCS[i] + ", PacketBodyReader: " + ListSubDestinationCS[i] + ", Opcode: 0xfff";
                }

                tmp.Add(lst);

                src = InUseOut.ContainsKey(i) ? StructureSourceCS[InUseOut[i]] : new List<string>();
                dst = StructureDestinationCS[i];

                var count = Math.Max(src.Count, dst.Count);
                // проходим по самому длинному списку
                do
                {
                    var str1 = ss < src.Count ? src[ss] : "";
                    var str2 = dd < dst.Count ? dst[dd] : "";

                    lst = ss + ": " + str1 + "\t\t" + ss + ": " + str2;
                    tmp.Add(lst);

                    ss++;
                    dd++;
                } while (ss < count);

                tmp.Add("--------------------------------------------------------------------------------------------------------------------------------------------");
            }
            if (SaveFileDialog7())
            {
                File.WriteAllLines(FilePathOut7, tmp);
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
            List<string> src;
            List<string> dst;
            for (var i = 0; i < ListNameCompareSC.Count; i++)
            {
                ss = 0;
                dd = 0;
                if (ListOpcodeDestinationSC.Count > 0)
                {
                    lst = i + 1 + ": Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + ListSubDestinationSC[i] + ", Opcode: " + ListOpcodeDestinationSC[i];
                }
                else
                {
                    lst = i + 1 + ": Packet name: " + ListNameCompareSC[i] + ", PacketBodyReader: " + ListSubDestinationSC[i] + ", Opcode: 0xfff";
                }

                tmp.Add(lst);

                src = InUseOut.ContainsKey(i) ? StructureSourceSC[InUseOut[i]] : new List<string>();
                dst = StructureDestinationSC[i];

                var count = Math.Max(src.Count, dst.Count);
                // проходим по самому длинному списку
                do
                {
                    var str1 = ss < src.Count ? src[ss] : "";
                    var str2 = dd < dst.Count ? dst[dd] : "";

                    lst = ss + ": " + str1 + "\t\t" + ss + ": " + str2;
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

        private void checkBox_Checked_In(object sender, RoutedEventArgs e)
        {
            //FindOpcodeIn = checkBoxOut.IsChecked == true;
        }

        private void checkBox_Checked_Out(object sender, RoutedEventArgs e)
        {
            //FindOpcodeOut = checkBoxOut.IsChecked == true;
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
            // Создаем объект для блокировки.
            var lockObj = new object();
            lock (lockObj)
            {
                using (var FBD = new FolderBrowserDialog())
                {
                    // сохраним путь к рабочей папке
                    string path = Environment.CurrentDirectory + "\\WorkDir.cfg";
                    FileInfo fileInf = new FileInfo(path);
                    if (fileInf.Exists)
                    {
                        FBD.SelectedPath = File.ReadAllLines(Environment.CurrentDirectory + "\\WorkDir.cfg")[0].ToString();
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

                DirectoryInfo dirInfo = new DirectoryInfo(DirPath + "\\data");
                if (!dirInfo.Exists)
                {
                    //dirInfo.Create();
                    dirInfo.CreateSubdirectory("cs");
                    dirInfo.CreateSubdirectory("sc");
                }

                // Проверяем на то что есть, что-либо сохранять
                if (ButtonCsCompare.IsEnabled == false && ButtonScCompare.IsEnabled == false)
                {
                    return;
                }

                if (ButtonSaveIn1.IsEnabled)
                {
                    File.WriteAllLines(DirPathCS + "\\TextBoxPathIn", new List<string> { TextBoxPathIn.Text });
                    File.WriteAllLines(DirPathCS + "\\TextBoxPathOut", new List<string> { TextBoxPathOut.Text });

                    File.WriteAllLines(DirPathCS + "\\AddressForClientPacketsIn", new List<string> { TextBox11.Text });
                    File.WriteAllLines(DirPathCS + "\\AddressForServerPacketsIn", new List<string> { TextBox12.Text });

                    File.WriteAllLines(DirPathCS + "\\AddressForClientPacketsOut", new List<string> { TextBox21.Text });
                    File.WriteAllLines(DirPathCS + "\\AddressForServerPacketsOut", new List<string> { TextBox22.Text });

                    File.WriteAllLines(DirPathCS + "\\NotFoundIn", new List<string> { TextBox17Copy.Text });
                    File.WriteAllLines(DirPathCS + "\\NotFoundOut", new List<string> { TextBox17Copy1.Text });

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

                    string json = JsonConvert.SerializeObject(InUseIn, Formatting.Indented);
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
                else
                {
                    File.WriteAllLines(DirPathSC + "\\TextBoxPathIn", new List<string> { TextBoxPathIn.Text });
                    File.WriteAllLines(DirPathSC + "\\TextBoxPathOut", new List<string> { TextBoxPathOut.Text });

                    File.WriteAllLines(DirPathSC + "\\AddressForClientPacketsIn", new List<string> { TextBox11.Text });
                    File.WriteAllLines(DirPathSC + "\\AddressForServerPacketsIn", new List<string> { TextBox12.Text });

                    File.WriteAllLines(DirPathSC + "\\AddressForClientPacketsOut", new List<string> { TextBox21.Text });
                    File.WriteAllLines(DirPathSC + "\\AddressForServerPacketsOut", new List<string> { TextBox22.Text });

                    File.WriteAllLines(DirPathSC + "\\NotFoundIn", new List<string> { TextBox17Copy.Text });
                    File.WriteAllLines(DirPathSC + "\\NotFoundOut", new List<string> { TextBox17Copy1.Text });

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

                    string json = JsonConvert.SerializeObject(InUseIn, Formatting.Indented);
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

            // Создаем объект для блокировки.
            var lockObj = new object();
            lock (lockObj)
            {
                DirPath = TextBoxPathIn.Text = File.ReadAllLines(Environment.CurrentDirectory + "\\WorkDir.cfg")[0].ToString();
                DirPathCS = DirPath + "\\data\\cs";
                DirPathSC = DirPath + "\\data\\sc";

                TextBoxPathIn.Text = File.ReadAllLines(DirPathCS + "\\TextBoxPathIn")[0].ToString();
                TextBoxPathOut.Text = File.ReadAllLines(DirPathCS + "\\TextBoxPathOut")[0].ToString();

                TextBox11.Text = File.ReadAllLines(DirPathCS + "\\AddressForClientPacketsIn")[0].ToString();
                TextBox12.Text = File.ReadAllLines(DirPathCS + "\\AddressForServerPacketsIn")[0].ToString();

                TextBox21.Text = File.ReadAllLines(DirPathCS + "\\AddressForClientPacketsOut")[0].ToString();
                TextBox22.Text = File.ReadAllLines(DirPathCS + "\\AddressForServerPacketsOut")[0].ToString();

                TextBox17Copy.Text = File.ReadAllLines(DirPathCS + "\\NotFoundIn")[0].ToString();
                TextBox17Copy1.Text = File.ReadAllLines(DirPathCS + "\\NotFoundOut")[0].ToString();

                isCompareCS = File.ReadAllLines(DirPathCS + "\\isCompareCS")[0] == "True";
                isCompareSC = File.ReadAllLines(DirPathCS + "\\isCompareSC")[0] == "True";

                InListSource = File.ReadAllLines(DirPath + "\\data\\InListSource").ToList();
                ListView11.ItemsSource = InListSource;
                InListDestination = File.ReadAllLines(DirPath + "\\data\\InListDestination").ToList();
                ListView21.ItemsSource = InListDestination;

                ListNameSourceCS = File.ReadAllLines(DirPathCS + "\\ListNameSourceCS").ToList();
                ListView12.ItemsSource = ListNameSourceCS;
                TextBox13.Text = ListNameSourceCS.Count.ToString();

                ListNameSourceSC = File.ReadAllLines(DirPathCS + "\\ListNameSourceSC").ToList();
                //ListView12.ItemsSource = ListNameSourceSC;
                TextBox16.Text = ListNameSourceSC.Count.ToString();

                ListSubSourceCS = File.ReadAllLines(DirPathCS + "\\ListSubSourceCS").ToList();
                ListView13.ItemsSource = ListSubSourceCS;
                TextBox14.Text = ListSubSourceCS.Count.ToString();

                ListSubSourceSC = File.ReadAllLines(DirPathCS + "\\ListSubSourceSC").ToList();
                //ListView13.ItemsSource = ListSubSourceSC;
                TextBox17.Text = ListSubSourceSC.Count.ToString();

                ListOpcodeSourceCS = File.ReadAllLines(DirPathCS + "\\ListOpcodeSourceCS").ToList();
                ListView14.ItemsSource = ListOpcodeSourceCS;
                TextBox16Copy.Text = ListOpcodeSourceCS.Count.ToString();

                ListOpcodeSourceSC = File.ReadAllLines(DirPathCS + "\\ListOpcodeSourceSC").ToList();
                //ListView14.ItemsSource = ListOpcodeSourceSC;
                //TextBox16Copy.Text = ListOpcodeSourceSC.Count.ToString();

                ListNameDestinationCS = File.ReadAllLines(DirPathCS + "\\ListNameDestinationCS").ToList();
                ListView22.ItemsSource = ListNameDestinationCS;
                TextBox23.Text = ListNameDestinationCS.Count.ToString();

                ListNameDestinationSC = File.ReadAllLines(DirPathCS + "\\ListNameDestinationSC").ToList();
                //ListView22.ItemsSource = ListNameDestinationSC;
                TextBox26.Text = ListNameDestinationSC.Count.ToString();

                ListSubDestinationCS = File.ReadAllLines(DirPathCS + "\\ListSubDestinationCS").ToList();
                ListView23.ItemsSource = ListSubDestinationCS;
                TextBox24.Text = ListSubDestinationCS.Count.ToString();

                ListSubDestinationSC = File.ReadAllLines(DirPathCS + "\\ListSubDestinationSC").ToList();
                //ListView23.ItemsSource = ListSubDestinationSC;
                TextBox27.Text = ListSubDestinationSC.Count.ToString();

                ListOpcodeDestinationCS = File.ReadAllLines(DirPathCS + "\\ListOpcodeDestinationCS").ToList();
                ListView24.ItemsSource = ListOpcodeDestinationCS;
                TextBox16Copy1.Text = ListOpcodeDestinationCS.Count.ToString();

                ListOpcodeDestinationSC = File.ReadAllLines(DirPathCS + "\\ListOpcodeDestinationSC").ToList();
                //ListView24.ItemsSource = ListOpcodeDestinationSC;
                //TextBox16Copy1.Text = ListOpcodeDestinationSC.Count.ToString();

                ListNameCompareCS = File.ReadAllLines(DirPathCS + "\\ListNameCompareCS").ToList();
                ListView31.ItemsSource = ListNameCompareCS;
                TextBox31.Text = ListNameCompareCS.Count.ToString();

                ListNameCompareSC = File.ReadAllLines(DirPathCS + "\\ListNameCompareSC").ToList();
                //ListView31.ItemsSource = ListNameCompareCS;
                //TextBox31.Text = ListNameCompareSC.Count.ToString();

                ListNameCompare = File.ReadAllLines(DirPathCS + "\\ListNameCompare").ToList();

                ListNameCompareOutCS = File.ReadAllLines(DirPathCS + "\\ListNameCompareOutCS").ToList();
                ListView32.ItemsSource = ListNameCompareOutCS;

                ListNameCompareOutSC = File.ReadAllLines(DirPathCS + "\\ListNameCompareOutSC").ToList();
                //ListView32.ItemsSource = ListNameCompareOutSC;

                string json = File.ReadAllText(DirPathCS + "\\InUseIn.json");
                InUseIn = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                json = File.ReadAllText(DirPathCS + "\\InUseOut.json");
                InUseOut = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                json = File.ReadAllText(DirPathCS + "\\IsRenameDestination.json");
                IsRenameDestination = JsonConvert.DeserializeObject<Dictionary<int, bool>>(json);

                json = File.ReadAllText(DirPathCS + "\\StructureSourceCS.json");
                StructureSourceCS = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                json = File.ReadAllText(DirPathCS + "\\StructureSourceSC.json");
                StructureSourceSC = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                json = File.ReadAllText(DirPathCS + "\\StructureDestinationCS.json");
                StructureDestinationCS = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                json = File.ReadAllText(DirPathCS + "\\StructureDestinationSC.json");
                StructureDestinationSC = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

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

                _isInCs = true;
                _isOutCs = true;
            }

            Label_Semafor1.Background = Brushes.GreenYellow;
            Label_Semafor2.Background = Brushes.GreenYellow;
        }

        private void btn_LoadSnapshotSC_Click(object sender, RoutedEventArgs e)
        {
            Label_Semafor1.Background = Brushes.Red;
            Label_Semafor2.Background = Brushes.Red;

            // Создаем объект для блокировки.
            var lockObj = new object();
            lock (lockObj)
            {
                DirPath = TextBoxPathIn.Text = File.ReadAllLines(Environment.CurrentDirectory + "\\WorkDir.cfg")[0].ToString();
                DirPathCS = DirPath + "\\data\\cs";
                DirPathSC = DirPath + "\\data\\sc";

                TextBoxPathIn.Text = File.ReadAllLines(DirPathSC + "\\TextBoxPathIn")[0].ToString();
                TextBoxPathOut.Text = File.ReadAllLines(DirPathSC + "\\TextBoxPathOut")[0].ToString();

                TextBox11.Text = File.ReadAllLines(DirPathSC + "\\AddressForClientPacketsIn")[0].ToString();
                TextBox12.Text = File.ReadAllLines(DirPathSC + "\\AddressForServerPacketsIn")[0].ToString();

                TextBox21.Text = File.ReadAllLines(DirPathSC + "\\AddressForClientPacketsOut")[0].ToString();
                TextBox22.Text = File.ReadAllLines(DirPathSC + "\\AddressForServerPacketsOut")[0].ToString();

                TextBox17Copy.Text = File.ReadAllLines(DirPathSC + "\\NotFoundIn")[0].ToString();
                TextBox17Copy1.Text = File.ReadAllLines(DirPathSC + "\\NotFoundOut")[0].ToString();

                isCompareCS = File.ReadAllLines(DirPathCS + "\\isCompareCS")[0] == "True";
                isCompareSC = File.ReadAllLines(DirPathCS + "\\isCompareSC")[0] == "True";

                InListSource = File.ReadAllLines(DirPath + "\\data\\InListSource").ToList();
                ListView11.ItemsSource = InListSource;
                InListDestination = File.ReadAllLines(DirPath + "\\data\\InListDestination").ToList();
                ListView21.ItemsSource = InListDestination;

                ListNameSourceCS = File.ReadAllLines(DirPathSC + "\\ListNameSourceCS").ToList();
                //ListView12.ItemsSource = ListNameSourceCS;
                TextBox13.Text = ListNameSourceCS.Count.ToString();

                ListNameSourceSC = File.ReadAllLines(DirPathSC + "\\ListNameSourceSC").ToList();
                ListView12.ItemsSource = ListNameSourceSC;
                TextBox16.Text = ListNameSourceSC.Count.ToString();

                ListSubSourceCS = File.ReadAllLines(DirPathSC + "\\ListSubSourceCS").ToList();
                //ListView13.ItemsSource = ListSubSourceCS;
                TextBox14.Text = ListSubSourceCS.Count.ToString();

                ListSubSourceSC = File.ReadAllLines(DirPathSC + "\\ListSubSourceSC").ToList();
                ListView13.ItemsSource = ListSubSourceSC;
                TextBox17.Text = ListSubSourceSC.Count.ToString();

                ListOpcodeSourceCS = File.ReadAllLines(DirPathSC + "\\ListOpcodeSourceCS").ToList();
                //ListView14.ItemsSource = ListOpcodeSourceCS;
                //TextBox16Copy.Text = ListOpcodeSourceCS.Count.ToString();

                ListOpcodeSourceSC = File.ReadAllLines(DirPathSC + "\\ListOpcodeSourceSC").ToList();
                ListView14.ItemsSource = ListOpcodeSourceSC;
                TextBox16Copy.Text = ListOpcodeSourceSC.Count.ToString();

                ListNameDestinationCS = File.ReadAllLines(DirPathSC + "\\ListNameDestinationCS").ToList();
                //ListView22.ItemsSource = ListNameDestinationCS;
                TextBox23.Text = ListNameDestinationCS.Count.ToString();

                ListNameDestinationSC = File.ReadAllLines(DirPathSC + "\\ListNameDestinationSC").ToList();
                ListView22.ItemsSource = ListNameDestinationSC;
                TextBox26.Text = ListNameDestinationSC.Count.ToString();

                ListSubDestinationCS = File.ReadAllLines(DirPathSC + "\\ListSubDestinationCS").ToList();
                //ListView23.ItemsSource = ListSubDestinationCS;
                TextBox24.Text = ListSubDestinationCS.Count.ToString();

                ListSubDestinationSC = File.ReadAllLines(DirPathSC + "\\ListSubDestinationSC").ToList();
                ListView23.ItemsSource = ListSubDestinationSC;
                TextBox27.Text = ListSubDestinationSC.Count.ToString();

                ListOpcodeDestinationCS = File.ReadAllLines(DirPathSC + "\\ListOpcodeDestinationCS").ToList();
                //ListView24.ItemsSource = ListOpcodeDestinationCS;
                //TextBox16Copy1.Text = ListOpcodeDestinationCS.Count.ToString();

                ListOpcodeDestinationSC = File.ReadAllLines(DirPathSC + "\\ListOpcodeDestinationSC").ToList();
                ListView24.ItemsSource = ListOpcodeDestinationSC;
                TextBox16Copy1.Text = ListOpcodeDestinationSC.Count.ToString();

                ListNameCompareCS = File.ReadAllLines(DirPathSC + "\\ListNameCompareCS").ToList();
                //ListView31.ItemsSource = ListNameCompareCS;
                //TextBox31.Text = ListNameCompareCS.Count.ToString();

                ListNameCompareSC = File.ReadAllLines(DirPathSC + "\\ListNameCompareSC").ToList();
                ListView31.ItemsSource = ListNameCompareSC;
                TextBox31.Text = ListNameCompareSC.Count.ToString();

                ListNameCompare = File.ReadAllLines(DirPathSC + "\\ListNameCompare").ToList();

                ListNameCompareOutCS = File.ReadAllLines(DirPathSC + "\\ListNameCompareOutCS").ToList();
                //ListView32.ItemsSource = ListNameCompareOutCS;

                ListNameCompareOutSC = File.ReadAllLines(DirPathSC + "\\ListNameCompareOutSC").ToList();
                ListView32.ItemsSource = ListNameCompareOutSC;

                string json = File.ReadAllText(DirPathSC + "\\InUseIn.json");
                InUseIn = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                json = File.ReadAllText(DirPathSC + "\\InUseOut.json");
                InUseOut = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                json = File.ReadAllText(DirPathSC + "\\IsRenameDestination.json");
                IsRenameDestination = JsonConvert.DeserializeObject<Dictionary<int, bool>>(json);

                json = File.ReadAllText(DirPathSC + "\\StructureSourceCS.json");
                StructureSourceCS = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                json = File.ReadAllText(DirPathSC + "\\StructureSourceSC.json");
                StructureSourceSC = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                json = File.ReadAllText(DirPathSC + "\\StructureDestinationCS.json");
                StructureDestinationCS = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

                json = File.ReadAllText(DirPathSC + "\\StructureDestinationSC.json");
                StructureDestinationSC = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(json);

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

                _isInSc = true;
                _isOutSc = true;
            }

            Label_Semafor1.Background = Brushes.GreenYellow;
            Label_Semafor2.Background = Brushes.GreenYellow;
        }

        private void button_EditOutOpcode_Click(object sender, RoutedEventArgs e)
        {
            if (ListView24.SelectedItem != null)
            {
                if (ButtonCsCompare.IsEnabled)
                {
                    ListOpcodeDestinationCS[ListView24.SelectedIndex] = TextBoxEditOutOpcode.Text;
                }
                else
                {
                    ListOpcodeDestinationSC[ListView24.SelectedIndex] = TextBoxEditOutOpcode.Text;
                }
                ListView24.SelectedItem = TextBoxEditOutOpcode.Text;
                ListView24.Items.Refresh();
            }
        }

        private void ListView24_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ListView24.SelectedItem != null)
            {
                TextBoxEditOutOpcode.Text = ListView24.SelectedItem.ToString();

                ListView22.SelectedIndex = ListView24.SelectedIndex;
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
            ListView12.UpdateLayout();
            ListView12.ScrollIntoView(ListView12.SelectedItem);

            ListView13.SelectedIndex = ListView14.SelectedIndex;
            ListView13.UpdateLayout();
            ListView13.ScrollIntoView(ListView13.SelectedItem);
        }

        private void ListView31_SelectionChanged(object sender, RoutedEventArgs e)
        {
            ListView32.SelectedIndex = ListView31.SelectedIndex;
            ListView32.UpdateLayout();
            ListView32.ScrollIntoView(ListView32.SelectedItem);
        }
    }
}
