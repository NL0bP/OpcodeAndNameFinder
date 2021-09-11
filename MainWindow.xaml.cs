﻿using Microsoft.Win32;

using NameFinder.Conversion;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using static System.String;

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
        private bool _isOut;
        private bool _isIn;
        private static readonly uint csSecondaryOffsetSequence = 0;
        private bool FindOpcodeIn = true;
        private bool FindOpcodeOut = true;
        private bool FindStructIn = true;
        private bool FindStructOut = true;
        public bool isCompareCS = false;
        public bool isCompareSC = false;

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

        public bool isCleaningIn = false;
        public bool isCleaningOut = false;
        public const int DepthMax = 2;
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

            var notFoundCount = 0;
            var baseAddress = 0;
            var offsetAddres = 0;
            var subAddress = "";
            ListOpcodeSourceCS = new List<string>();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            for (var i = 0; i < XrefsIn.Count; i++)
            {
                var list = XrefsIn[i].ToList();
                var foundOpcode = false;
                foreach (var lst in list)
                {
                    subAddress = "";
                    //var regex = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                    // выделяем из "sub_39022C10+74A" -> "sub_39022C10" и "74A"
                    var regexSub = new Regex(@"sub_\w+|loc_\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(lst);
                    if (matchesSub.Count <= 0) { continue; }
                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    for (var index = 0; index < InListSource.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListSource[index]);
                        if (matches.Count <= 0) { continue; }

                        // нашли начало подпрограммы, ищем структуры, пока не "endp"
                        var regexEndp = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                        var foundEndp = false;
                        do
                        {
                            index++;
                            // ищем
                            // "mov     [ebp+var_10], 6Ch"
                            // "mov     dword ptr [eax+4], 217h"
                            var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s([1-9][0-f]{1,}|0[0-f]{1,})h?$|mov\s+dword\sptr\s\[\w{3}\+4\],\s([1-3][0-f]{1,3}|0[0-f]{1,3}|[1-9]{1})h?$", RegexOptions.IgnoreCase);
                            var matchesOpcode = regexOpcode.Match(InListSource[index]);
                            if (matchesOpcode.Groups.Count >= 3)
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString().Substring(0, 3);
                                            break;
                                    }
                                    foundOpcode = true; // нашли Opcode
                                    ListOpcodeSourceCS.Add(matchGroup);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString().Substring(0, 3);
                                            break;
                                    }
                                    ListOpcodeSourceCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                            var matchesEndp = regexEndp.Matches(InListSource[index]);
                            if (matchesEndp.Count <= 0) { continue; }

                            foundEndp = true;
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
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

            var notFoundCount = 0;
            var baseAddress = 0;
            var offsetAddres = 0;
            var subAddress = "";
            ListOpcodeSourceSC = new List<string>();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            for (var i = 0; i < XrefsIn.Count; i++)
            {
                var list = XrefsIn[i].ToList();
                var foundOpcode = false;
                foreach (var lst in list)
                {
                    subAddress = "";
                    //var regex = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                    // выделяем из "sub_39022C10+74A" -> "sub_39022C10" и "74A"
                    var regexSub = new Regex(@"sub_\w+|loc_\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(lst);
                    if (matchesSub.Count <= 0) { continue; }
                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    for (var index = 0; index < InListSource.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListSource[index]);
                        if (matches.Count <= 0) { continue; }

                        // нашли начало подпрограммы, ищем структуры, пока не "endp"
                        var regexEndp = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                        var foundEndp = false;
                        do
                        {
                            index++;
                            // ищем
                            // "mov     [ebp+var_10], 6Ch"
                            // "mov     dword ptr [eax+4], 217h"
                            //var regexOpcode = new Regex(@"((mov\s{1,}\[ebp\+var_\w{1,}\],\s([0-9a-f]{1,3})h?)|(mov\s{1,}dword\sptr\s\[eax\+4\],\s(([0-9a-f]{1,3}))h?))$", RegexOptions.IgnoreCase);
                            var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s([1-9][0-f]{1,}|0[0-f]{1,})h?$|mov\s+dword\sptr\s\[\w{3}\+4\],\s([1-3][0-f]{1,3}|0[0-f]{1,3}|[1-9]{1})h?$", RegexOptions.IgnoreCase);
                            var matchesOpcode = regexOpcode.Match(InListSource[index]);
                            if (matchesOpcode.Groups.Count >= 3)
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString().Substring(0, 3);
                                            break;
                                    }
                                    foundOpcode = true; // нашли Opcode
                                    ListOpcodeSourceSC.Add(matchGroup);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString().Substring(0, 3);
                                            break;
                                    }
                                    ListOpcodeSourceSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                            var matchesEndp = regexEndp.Matches(InListSource[index]);
                            if (matchesEndp.Count <= 0) { continue; }

                            foundEndp = true;
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
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

            var notFoundCount = 0;
            var baseAddress = 0;
            var offsetAddres = 0;
            var subAddress = "";
            ListOpcodeDestinationCS = new List<string>();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            for (var i = 0; i < XrefsOut.Count; i++)
            {
                var list = XrefsOut[i].ToList();
                var foundOpcode = false;
                foreach (var lst in list)
                {
                    subAddress = "";
                    //var regex = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                    // выделяем из "sub_39022C10+74A" -> "sub_39022C10" и "74A"
                    var regexSub = new Regex(@"sub_\w+|loc_\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(lst);
                    if (matchesSub.Count <= 0) { continue; }

                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();

                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    for (var index = 0; index < InListDestination.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListDestination[index]);
                        if (matches.Count <= 0) { continue; }

                        // нашли начало подпрограммы, ищем структуры, пока не "endp"
                        var regexEndp = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                        var foundEndp = false;
                        do
                        {
                            index++;
                            // ищем
                            // "mov     [ebp+var_10], 6Ch"
                            // "mov     dword ptr [eax+4], 217h"
                            //var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s([1-9][0-f]{1,}|0[0-f]{1,})h?$|mov\s+dword\sptr\s\[\w{3}\+4\],\s([1-3][0-f]{1,3}|0[0-f]{1,3}|[1-9]{1})h?$", RegexOptions.IgnoreCase);
                            //var regexOpcode = new Regex(@"(mov\s+\[ebp\+var_\w+\],\s([0-f]{1,3}))", RegexOptions.IgnoreCase);
                            var regexOpcode = new Regex(@"(mov\s+\[\w+\+\w+\],\s([1-9][0-f]{1,3}h?|[1-9]{1}h?|0[0-f]{1,3})h?|(mov\s+dword\sptr\s\[\w+\+\w+\],\s([0-f]{1,3})))", RegexOptions.IgnoreCase);
                            var matchesOpcode = regexOpcode.Match(InListDestination[index]);
                            if (matchesOpcode.Groups.Count >= 3)
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString().Substring(0, 3);
                                            break;
                                    }
                                    foundOpcode = true; // нашли Opcode
                                    ListOpcodeDestinationCS.Add(matchGroup);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString().Substring(0, 3);
                                            break;
                                    }
                                    ListOpcodeDestinationCS.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                            var matchesEndp = regexEndp.Matches(InListDestination[index]);
                            if (matchesEndp.Count <= 0) { continue; }

                            foundEndp = true;
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
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

            var notFoundCount = 0;
            var baseAddress = 0;
            var offsetAddres = 0;
            var subAddress = "";
            ListOpcodeDestinationSC = new List<string>();

            // здесь ищем ссылку на подпрограмму, где есть опкоды
            var found = false;
            for (var i = 0; i < XrefsOut.Count; i++)
            {
                var list = XrefsOut[i].ToList();
                var foundOpcode = false;
                foreach (var lst in list)
                {
                    subAddress = "";
                    //var regex = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                    // выделяем из "sub_39022C10+74A" -> "sub_39022C10" и "74A"
                    var regexSub = new Regex(@"sub_\w+|loc_\w+", RegexOptions.IgnoreCase);
                    var matchesSub = regexSub.Matches(lst);
                    if (matchesSub.Count <= 0) { continue; }
                    // "sub_39022C10"
                    subAddress = matchesSub[0].ToString();
                    // здесь ищем начало подпрограммы
                    // начнем с начала файла
                    found = false;
                    for (var index = 0; index < InListDestination.Count; index++)
                    {
                        var regex10 = new Regex(@"^" + subAddress, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        var matches = regex10.Matches(InListDestination[index]);
                        if (matches.Count <= 0) { continue; }

                        // нашли начало подпрограммы, ищем структуры, пока не "endp"
                        var regexEndp = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                        var foundEndp = false;
                        do
                        {
                            index++;
                            // ищем
                            // "mov     [ebp+var_10], 6Ch"
                            // "mov     dword ptr [eax+4], 217h"
                            //var regexOpcode = new Regex(@"mov\s+\[\w+\+\w+\],\s([1-9][0-f]{1,}|0[0-f]{1,})h?$|mov\s+dword\sptr\s\[\w{3}\+4\],\s([1-3][0-f]{1,3}|0[0-f]{1,3}|[1-9]{1})h?$", RegexOptions.IgnoreCase);
                            var regexOpcode = new Regex(@"(mov\s+dword\sptr\s\[\w+\+4\],\s([0-f]{1,3}))", RegexOptions.IgnoreCase);
                            var matchesOpcode = regexOpcode.Match(InListDestination[index]);
                            if (matchesOpcode.Groups.Count >= 3)
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[4].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[4].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[4].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[3].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[3].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[3].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[2].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[2].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[2].ToString().Substring(0, 3);
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
                                            matchGroup = "0x00" + matchesOpcode.Groups[1].ToString().Substring(0, 1);
                                            break;
                                        case 3:
                                            matchGroup = "0x0" + matchesOpcode.Groups[1].ToString().Substring(0, 2);
                                            break;
                                        case 4:
                                            matchGroup = "0x" + matchesOpcode.Groups[1].ToString().Substring(0, 3);
                                            break;
                                    }
                                    ListOpcodeDestinationSC.Add(matchGroup);
                                    foundOpcode = true; // нашли Opcode
                                }
                            }
                            var matchesEndp = regexEndp.Matches(InListDestination[index]);
                            if (matchesEndp.Count <= 0) { continue; }

                            foundEndp = true;
                        } while (!foundEndp && !foundOpcode);

                        if (foundEndp || foundOpcode)
                        {
                            break;
                        }
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
        }

        private List<string> CleanSourceSub(int idx)
        {
            var maxCount = InListSource.Count;
            var found = false;
            var tmpLst = new List<string>();
            var regex = new Regex(@"(proc\snear)", RegexOptions.IgnoreCase); // ищем начало подпрограммы
            for (var index = idx; index < maxCount; index++)
            {
                if (index % 1000 == 0)
                {
                    ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = index; }));
                }

                var matches = regex.Matches(InListSource[index]);
                if (matches.Count <= 0) { continue; }

                // нашли начало подпрограммы
                tmpLst.Add(InListSource[index]); // сохранили

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var regex2 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                var foundEndp = false;
                do
                {
                    index++;
                    var regex3 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(mov\s+\[\w+\+\w+\],\s([1-9][0-f]{1,3}h?|[1-9]{1}h?|0[0-f]{1,3})h?|(mov\s+dword\sptr\s\[\w+\+\w+\],\s([0-f]{1,3}))))", RegexOptions.IgnoreCase);
                    var matches3 = regex3.Matches(InListSource[index]);
                    foreach (var match3 in matches3)
                    {
                        tmpLst.Add(InListSource[index]); // сохранили
                        found = true; // нашли структуру
                    }
                    var matches2 = regex2.Matches(InListSource[index]);
                    if (matches2.Count <= 0) { continue; }

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
            var found = false;
            var tmpLst = new List<string>();
            var txtCS = "";
            TextBox11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtCS = TextBox11.Text; }));
            //var regex = new Regex(@"(dd\soffset\s\w+)|(dd\soffset\s\sub_)|(; DATA XREF:)|(; sub_)", RegexOptions.IgnoreCase); // ищем начало offsets
            var regex = new Regex(@"(dd\soffset\s)" + txtCS, RegexOptions.IgnoreCase); // ищем начало offsets
            for (var index = idx; index < InListSource.Count; index++)
            {
                if (index % 1000 == 0)
                {
                    ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = index; }));
                }
                var matches = regex.Matches(InListSource[index]);
                if (matches.Count <= 0) { continue; }

                // нашли, записываем следующие 5 строк, пока в начале пробелы
                var regex2 = new Regex(@"(^\s{8,})", RegexOptions.IgnoreCase); // ищем начало offsets
                MatchCollection matches2;
                do
                {
                    tmpLst.Add(InListSource[index]); // сохранили
                    index++;
                    matches2 = regex2.Matches(InListSource[index]);
                } while (matches2.Count > 0);

                found = true;
                index--;
            }
            if (!found)
            {
                // не нашли структуру
                return new List<string>();
            }

            return tmpLst;
        }
        private List<string> CleanSourceOffsSC(int idx)
        {
            var found = false;
            var tmpLst = new List<string>();
            var txtSC = "";
            TextBox12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtSC = TextBox12.Text; }));
            //var regex = new Regex(@"(dd\soffset\s\w+)|(dd\soffset\s\sub_)|(; DATA XREF:)|(; sub_)", RegexOptions.IgnoreCase); // ищем начало offsets
            var regex = new Regex(@"(dd\soffset\s)" + txtSC, RegexOptions.IgnoreCase); // ищем начало offsets
            for (var index = idx; index < InListSource.Count; index++)
            {
                if (index % 1000 == 0)
                {
                    ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = index; }));
                }
                var matches = regex.Matches(InListSource[index]);
                if (matches.Count <= 0) { continue; }

                // нашли, записываем следующие 5 строк, пока в начале пробелы
                var regex2 = new Regex(@"(^\s{8,})", RegexOptions.IgnoreCase); // ищем начало offsets
                MatchCollection matches2;
                do
                {
                    tmpLst.Add(InListSource[index]); // сохранили
                    index++;
                    matches2 = regex2.Matches(InListSource[index]);
                } while (matches2.Count > 0);

                found = true;
                index--;
            }
            if (!found)
            {
                // не нашли структуру
                return new List<string>();
            }

            return tmpLst;
        }

        private List<string> CleanDestinationSub(int idx)
        {
            var maxCount = InListDestination.Count;
            var found = false;
            var tmpLst = new List<string>();
            var regex = new Regex(@"(proc\snear)", RegexOptions.IgnoreCase); // ищем начало подпрограммы
            for (var index = idx; index < maxCount; index++)
            {
                if (index % 1000 == 0)
                {
                    ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = index; }));
                }
                var matches = regex.Matches(InListDestination[index]);
                if (matches.Count <= 0) { continue; }

                // нашли начало подпрограммы
                tmpLst.Add(InListDestination[index]); // сохранили

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var regex2 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                var foundEndp = false;
                do
                {
                    index++;
                    var regex3 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(mov\s+\[\w+\+\w+\],\s([1-9][0-f]{1,3}h?|[1-9]{1}h?|0[0-f]{1,3})h?|(mov\s+dword\sptr\s\[\w+\+\w+\],\s([0-f]{1,3}))))", RegexOptions.IgnoreCase);
                    var matches3 = regex3.Matches(InListDestination[index]);
                    foreach (var match3 in matches3)
                    {
                        tmpLst.Add(InListDestination[index]); // сохранили
                        found = true; // нашли структуру
                    }
                    var matches2 = regex2.Matches(InListDestination[index]);
                    if (matches2.Count <= 0) { continue; }

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
            var found = false;
            var tmpLst = new List<string>();
            var txtCS = "";
            TextBox21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtCS = TextBox21.Text; }));
            //var regex = new Regex(@"(dd\soffset\s\w+)|(dd\soffset\s\sub_)|(; DATA XREF:)|(; sub_)", RegexOptions.IgnoreCase); // ищем начало offsets
            var regex = new Regex(@"(dd\soffset\s)" + txtCS, RegexOptions.IgnoreCase); // ищем начало offsets
            for (var index = idx; index < InListDestination.Count; index++)
            {
                if (index % 1000 == 0)
                {
                    ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = index; }));
                }
                var matches = regex.Matches(InListDestination[index]);
                if (matches.Count <= 0) { continue; }

                // нашли, записываем следующие 5 строк, пока в начале пробелы
                var regex2 = new Regex(@"(^\s{8,})", RegexOptions.IgnoreCase); // ищем начало offsets
                MatchCollection matches2;
                do
                {
                    tmpLst.Add(InListDestination[index]); // сохранили
                    index++;
                    matches2 = regex2.Matches(InListDestination[index]);
                } while (matches2.Count > 0);

                found = true;
                index--;
            }
            if (!found)
            {
                // не нашли структуру
                return new List<string>();
            }

            return tmpLst;
        }
        private List<string> CleanDestinationSCOffs(int idx)
        {
            var found = false;
            var tmpLst = new List<string>();
            var txtSC = "";
            TextBox22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { txtSC = TextBox22.Text; }));
            //var regex = new Regex(@"(dd\soffset\s\w+)|(dd\soffset\s\sub_)|(; DATA XREF:)|(; sub_)", RegexOptions.IgnoreCase); // ищем начало offsets
            var regex = new Regex(@"(dd\soffset\s)" + txtSC, RegexOptions.IgnoreCase); // ищем начало offsets
            for (var index = idx; index < InListDestination.Count; index++)
            {
                if (index % 1000 == 0)
                {
                    ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = index; }));
                }
                var matches = regex.Matches(InListDestination[index]);
                if (matches.Count <= 0) { continue; }

                // нашли, записываем следующие 5 строк, пока в начале пробелы
                var regex2 = new Regex(@"(^\s{8,})", RegexOptions.IgnoreCase); // ищем начало offsets
                MatchCollection matches2;
                do
                {
                    tmpLst.Add(InListDestination[index]); // сохранили
                    index++;
                    matches2 = regex2.Matches(InListDestination[index]);
                } while (matches2.Count > 0);

                found = true;
                index--;
            }
            if (!found)
            {
                // не нашли структуру
                return new List<string>();
            }

            return tmpLst;
        }

        private void CleanSource()
        {
            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = 0; }));
            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Maximum = InListSource.Count; }));

            var tmp = new List<string>();

            // чистим списки
            var tmpSub = CleanSourceSub(0);
            tmp.AddRange(tmpSub);

            var tmpCSOffs = CleanSourceOffsCS(0);
            tmp.AddRange(tmpCSOffs);

            var tmpSCOffs = CleanSourceOffsSC(0);
            tmp.AddRange(tmpSCOffs);

            InListSource = new List<string>(tmp);

            ProgressBar11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar11.Value = InListSource.Count; }));
            ListView11.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView11.ItemsSource = InListSource; }));
            File.WriteAllLines(FilePathIn1, InListSource);
            CheckBoxCleaningIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { CheckBoxCleaningIn.IsChecked = false; }));
        }
        private void CleanDestination()
        {
            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = 0; }));
            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Maximum = InListDestination.Count; }));

            var tmp = new List<string>();

            // чистим списки
            var tmpSub = CleanDestinationSub(0);
            tmp.AddRange(tmpSub);

            var tmpCSOffs = CleanDestinationCSOffs(0);
            tmp.AddRange(tmpCSOffs);

            var tmpSCOffs = CleanDestinationSCOffs(0);
            tmp.AddRange(tmpSCOffs);

            InListDestination = new List<string>(tmp);

            ProgressBar21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar21.Value = InListDestination.Count; }));
            ListView21.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ListView21.ItemsSource = InListDestination; }));
            File.WriteAllLines(FilePathIn2, InListDestination);
            CheckBoxCleaningOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { CheckBoxCleaningOut.IsChecked = false; }));
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
            var regex4 = new Regex(@"^" + address, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
            for (var index = 0; index < InListSource.Count; index++)
            {
                var matches4 = regex4.Matches(InListSource[index]);
                if (matches4.Count <= 0) { continue; }

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var regex6 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                var foundEndp = false;
                tmpLst = new List<string>();
                //var str = "-->>";
                //lst.Add(str);
                do
                {
                    index++;
                    var regex5 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))", RegexOptions.IgnoreCase);
                    var matches5 = regex5.Matches(InListSource[index]);
                    foreach (var match5 in matches5)
                    {
                        if (match5.ToString() == "call    eax" || match5.ToString() == "call    ebx" || match5.ToString() == "call    edx" || match5.ToString() == "call    ecx")
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
                    var matches6 = regex6.Matches(InListSource[index]);
                    if (matches6.Count <= 0) { continue; }

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
            var lst = new List<string>();
            if (DepthOut == DepthMax)
            {
                return lst;
            }
            DepthOut++;
            //
            // начали работу по поиску структур пакетов
            //
            // начнем с начала файла
            var found = false;
            var regex4 = new Regex(@"^" + address, RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
            for (var index = 0; index < InListDestination.Count; index++)
            {
                var matches4 = regex4.Matches(InListDestination[index]);
                if (matches4.Count <= 0) { continue; }

                // нашли начало подпрограммы, ищем структуры, пока не "endp"
                var regex6 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                var foundEndp = false;
                lst = new List<string>();
                //var str = "-->>";
                //lst.Add(str);
                do
                {
                    index++;
                    var regex5 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))", RegexOptions.IgnoreCase);
                    var matches5 = regex5.Matches(InListDestination[index]);
                    foreach (var match5 in matches5)
                    {
                        if (match5.ToString() == "call    eax" || match5.ToString() == "call    ebx" || match5.ToString() == "call    edx" || match5.ToString() == "call    ecx")
                        {
                            continue;
                        }
                        if (match5.ToString().Length >= 4 && match5.ToString().Substring(0, 4) == "call")
                        {
                            var findList = FindStructureOut(match5.ToString().Substring(8));
                            if (findList.Count > 0)
                            {
                                lst.AddRange(findList);
                                found = true; // нашли структуру
                            }
                        }
                        else
                        {
                            lst.Add(match5.ToString()); // сохранили часть структуры пакета
                            found = true; // нашли структуру
                        }
                        //lst.Add(match5.ToString()); // сохранили часть структуры пакета
                    }
                    var matches6 = regex6.Matches(InListDestination[index]);
                    if (matches6.Count <= 0) { continue; }

                    foundEndp = true;
                } while (!foundEndp);

                //StructureSourceSC.Add(i, lst); // сохранили всю структуру пакета
                DepthOut--;
                //str = "<<--";
                //lst.Add(str);
                return lst;
            }
            if (!found)
            {
                // не нашли структуру
                //lst = new List<string>();
                //StructureSourceSC.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                DepthOut--;
                return new List<string>();
            }

            return lst;
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
                var regex = new Regex(@"dd\soffset\s" + str, RegexOptions.IgnoreCase);
                for (var index = 0; index < InListSource.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListSource[index]);
                    if (matches.Count <= 0) { continue; }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        var regexXREF = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListSource[tmpIdx]);
                        if (matchesXREF.Count <= 0) { continue; }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);
                    XrefsIn.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++;               // следующий номер пакета

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

                    var regex3 = new Regex(@"(dd\soffset\s)((\w{11,}|(nullsub)|(sub)_\w+))", RegexOptions.IgnoreCase); // ищем "sub_00000000"
                    var found = false;
                    do
                    {
                        index++;
                        var matches3 = regex3.Matches(InListSource[index]);
                        if (matches3.Count <= 0) { continue; }

                        foreach (var match3 in matches3)
                        {
                            var strin = match3.ToString();
                            ListSubSourceCS.Add(strin.Substring(10)); // сохранили адрес подпрограммы
                            found = true;
                        }
                    } while (!found);
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
                    for (var i = 0; i < ListSubSourceCS.Count; i++)
                    {
                        var found = false;
                        var regex4 = new Regex(@"^" + ListSubSourceCS[i], RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListSource.Count; index++)
                        {
                            var matches4 = regex4.Matches(InListSource[index]);
                            if (matches4.Count <= 0)
                            {
                                continue;
                            }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var regex6 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var regex5 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))", RegexOptions.IgnoreCase);
                                var matches5 = regex5.Matches(InListSource[index]);
                                foreach (var match5 in matches5)
                                {
                                    if (match5.ToString().Length >= 4 && match5.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureIn(match5.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(findList);
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(match5.ToString()); // сохранили часть структуры пакета
                                    }
                                }

                                var matches6 = regex6.Matches(InListSource[index]);
                                if (matches6.Count <= 0)
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
                            StructureSourceCS.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                        }

                        ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar12.Value = StructureSourceCS.Count; }));
                    }
                }
            }

            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = true; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = true; }));
            ButtonIn1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonIn1.IsEnabled = true; }));
            stopWatch.Stop();
            TextBox18.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox18.Text = stopWatch.Elapsed.ToString(); }));
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
                var regex = new Regex(@"dd\soffset\s" + str, RegexOptions.IgnoreCase);
                for (var index = 0; index < InListSource.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListSource[index]);
                    if (matches.Count <= 0) { continue; }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        var regexXREF = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListSource[tmpIdx]);
                        if (matchesXREF.Count <= 0) { continue; }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);
                    XrefsIn.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++;               // следующий номер пакета

                    var regex2 = new Regex(@"^(\S+)\s", RegexOptions.IgnoreCase);
                    var matches2 = regex2.Matches(InListSource[index]);
                    foreach (var match2 in matches2)
                    {
                        ListNameSourceSC.Add(match2.ToString()); // сохранили имя
                        foundName = true;
                    }
                    if (!foundName)
                    {
                        // не нашли имя пакета, бывает что его нет из-зи защиты themida
                        ListNameSourceSC.Add("SC_Unknown"); // сохранили адрес подпрограммы
                    }

                    var regex3 = new Regex(@"(dd\soffset\s)((\w{11,}|(nullsub)|(sub)_\w+))", RegexOptions.IgnoreCase); // ищем "sub_00000000"
                    var found = false;
                    do
                    {
                        index++;
                        var matches3 = regex3.Matches(InListSource[index]);
                        if (matches3.Count <= 0) { continue; }

                        foreach (var match3 in matches3)
                        {
                            var strin = match3.ToString();
                            ListSubSourceSC.Add(strin.Substring(10)); // сохранили адрес подпрограммы
                            found = true;
                        }
                    } while (!found);
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
                    for (var i = 0; i < ListSubSourceSC.Count; i++)
                    {
                        var found = false;
                        var regex4 =
                            new Regex(@"^" + ListSubSourceSC[i],
                                RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListSource.Count; index++)
                        {
                            var matches4 = regex4.Matches(InListSource[index]);
                            if (matches4.Count <= 0)
                            {
                                continue;
                            }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var regex6 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var regex5 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))",
                                    RegexOptions.IgnoreCase);
                                var matches5 = regex5.Matches(InListSource[index]);
                                foreach (var match5 in matches5)
                                {
                                    if (match5.ToString().Length >= 4 && match5.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureIn(match5.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(findList);
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(match5.ToString()); // сохранили часть структуры пакета
                                    }
                                }

                                var matches6 = regex6.Matches(InListSource[index]);
                                if (matches6.Count <= 0)
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
                            StructureSourceSC.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                        }

                        ProgressBar12.Dispatcher.Invoke(DispatcherPriority.Background,
                            new Action(() => { ProgressBar12.Value = StructureSourceSC.Count; }));
                    }
                }
            }

            BtnCsLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameIn.IsEnabled = true; }));
            BtnScLoadNameIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameIn.IsEnabled = true; }));
            BtnLoadIn.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadIn.IsEnabled = true; }));
            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = true; }));
            ButtonIn2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonIn2.IsEnabled = true; }));
            stopWatch.Stop();
            TextBox19.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox19.Text = stopWatch.Elapsed.ToString(); }));
            // уничтожаем ненужный список
            //ListSubSourceSC = new List<string>();
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
                var regex = new Regex(@"dd\soffset\s" + str, RegexOptions.IgnoreCase);
                for (var index = 0; index < InListDestination.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListDestination[index]);
                    if (matches.Count <= 0) { continue; }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        var regexXREF = new Regex(@"((sub_\w+)|(loc_\w{8}))", RegexOptions.IgnoreCase);
                        //var regexXREF = new Regex(@"((sub_\w+\+\w{1,3})|(loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListDestination[tmpIdx]);
                        if (matchesXREF.Count <= 0) { continue; }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);
                    XrefsOut.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++;               // следующий номер пакета

                    var regex2 = new Regex(@"^(\S+)\s", RegexOptions.IgnoreCase);
                    var matches2 = regex2.Matches(InListDestination[index]);
                    foreach (var match2 in matches2)
                    {
                        ListNameDestinationCS.Add(match2.ToString()); // сохранили имя
                        foundName = true;
                    }
                    if (!foundName)
                    {
                        // не нашли имя пакета, бывает что его нет из-зи защиты themida
                        ListNameDestinationCS.Add("Unknown"); // сохранили адрес подпрограммы
                    }

                    var regex3 = new Regex(@"(dd\soffset\s)((\w{11,}|(nullsub)|(sub)_\w+))", RegexOptions.IgnoreCase); // ищем "sub_00000000"
                    var found = false;
                    do
                    {
                        index++;
                        var matches3 = regex3.Matches(InListDestination[index]);
                        if (matches3.Count <= 0) { continue; }

                        foreach (var match3 in matches3)
                        {
                            var strin = match3.ToString();
                            ListSubDestinationCS.Add(strin.Substring(10)); // сохранили адрес подпрограммы
                            found = true;
                        }
                    } while (!found);
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
                    for (var i = 0; i < ListSubDestinationCS.Count; i++)
                    {
                        var found = false;
                        var regex4 =
                            new Regex(@"^" + ListSubDestinationCS[i],
                                RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListDestination.Count; index++)
                        {
                            var matches4 = regex4.Matches(InListDestination[index]);
                            if (matches4.Count <= 0)
                            {
                                continue;
                            }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var regex6 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var regex5 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))",
                                    RegexOptions.IgnoreCase);
                                var matches5 = regex5.Matches(InListDestination[index]);
                                foreach (var match5 in matches5)
                                {
                                    if (match5.ToString().Length >= 4 && match5.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureOut(match5.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(findList);
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(match5.ToString()); // сохранили часть структуры пакета
                                    }
                                }

                                var matches6 = regex6.Matches(InListDestination[index]);
                                if (matches6.Count <= 0)
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
                            StructureDestinationCS.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                        }

                        ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background,
                            new Action(() => { ProgressBar22.Value = StructureDestinationCS.Count; }));
                    }
                }
            }

            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = true; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = true; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = true; }));
            ButtonCsCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonCsCompare.IsEnabled = true; }));
            ButtonOut1.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonOut1.IsEnabled = true; }));
            stopWatch.Stop();
            TextBox28.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox28.Text = stopWatch.Elapsed.ToString(); }));

            // уничтожаем ненужный список
            //ListSubDestinationCS = new List<string>();
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
                var regex = new Regex(@"dd\soffset\s" + str, RegexOptions.IgnoreCase);
                for (var index = 0; index < InListDestination.Count; index++)
                {
                    var foundName = false;
                    var matches = regex.Matches(InListDestination[index]);
                    if (matches.Count <= 0) { continue; }

                    var lst = new List<string>();
                    var tmpIdx = index;
                    var tmpIdxMax = tmpIdx + 1;
                    do
                    {
                        tmpIdx++;
                        var regexXREF = new Regex(@"((sub_\w+\+\w{1,3}|loc_\w{8}))", RegexOptions.IgnoreCase);
                        // ищем "; DATA XREF: sub_3922E1C0+79↑o" или "; sub_3922E1C0:loc_3922E37F↑o"
                        var matchesXREF = regexXREF.Matches(InListDestination[tmpIdx]);
                        if (matchesXREF.Count <= 0) { continue; }

                        foreach (var match in matchesXREF)
                        {
                            lst.Add(match.ToString()); // сохранили XREF
                        }
                    } while (tmpIdx <= tmpIdxMax);
                    XrefsOut.Add(indexRefs, lst); // сохраним список XREF для пакета
                    indexRefs++;               // следующий номер пакета

                    var regex2 = new Regex(@"^(\S+)\s", RegexOptions.IgnoreCase);
                    var matches2 = regex2.Matches(InListDestination[index]);
                    foreach (var match2 in matches2)
                    {
                        ListNameDestinationSC.Add(match2.ToString()); // сохранили имя
                        foundName = true;
                    }
                    if (!foundName)
                    {
                        // не нашли имя пакета, бывает что его нет из-зи защиты themida
                        ListNameDestinationSC.Add("Unknown"); // сохранили адрес подпрограммы
                    }

                    var regex3 = new Regex(@"(dd\soffset\s)((\w{11,}|(nullsub)|(sub)_\w+))", RegexOptions.IgnoreCase); // ищем "sub_00000000"
                    var found = false;
                    do
                    {
                        index++;
                        var matches3 = regex3.Matches(InListDestination[index]);
                        if (matches3.Count <= 0) { continue; }

                        foreach (var match3 in matches3)
                        {
                            var strin = match3.ToString();
                            ListSubDestinationSC.Add(strin.Substring(10)); // сохранили адрес подпрограммы
                            found = true;
                        }
                    } while (!found);
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
                    for (var i = 0; i < ListSubDestinationSC.Count; i++)
                    {
                        var found = false;
                        var regex4 = new Regex(@"^" + ListSubDestinationSC[i], RegexOptions.IgnoreCase); // ищем начало подпрограммы, каждый раз с начала файла
                        for (var index = 0; index < InListDestination.Count; index++)
                        {
                            var matches4 = regex4.Matches(InListDestination[index]);
                            if (matches4.Count <= 0) { continue; }

                            // нашли начало подпрограммы, ищем структуры, пока не "endp"
                            var regex6 = new Regex(@"endp", RegexOptions.IgnoreCase); // ищем конец подпрограммы
                            var foundEndp = false;
                            var lst = new List<string>();
                            do
                            {
                                index++;
                                var regex5 = new Regex(@"(\x22[0-z._]+\x22)|(call\s{4}(sub_\w+)|(call\s{4}(\w+)))", RegexOptions.IgnoreCase);
                                var matches5 = regex5.Matches(InListDestination[index]);
                                foreach (var match5 in matches5)
                                {
                                    if (match5.ToString().Length >= 4 && match5.ToString().Substring(0, 4) == "call")
                                    {
                                        var findList = FindStructureOut(match5.ToString().Substring(8));
                                        if (findList.Count > 0)
                                        {
                                            lst.AddRange(findList);
                                        }
                                    }
                                    else
                                    {
                                        lst.Add(match5.ToString()); // сохранили часть структуры пакета
                                    }
                                }
                                var matches6 = regex6.Matches(InListDestination[index]);
                                if (matches6.Count <= 0) { continue; }

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
                            StructureDestinationSC.Add(i, lst); // сохраним пустой список, так как не нашли ничего
                        }

                        ProgressBar22.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ProgressBar22.Value = StructureDestinationSC.Count; }));
                    }
                }
            }

            BtnCsLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnCsLoadNameOut.IsEnabled = true; }));
            BtnScLoadNameOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnScLoadNameOut.IsEnabled = true; }));
            BtnLoadOut.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { BtnLoadOut.IsEnabled = true; }));
            ButtonScCompare.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonScCompare.IsEnabled = true; }));
            ButtonOut2.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { ButtonOut2.IsEnabled = true; }));

            stopWatch.Stop();
            TextBox29.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { TextBox29.Text = stopWatch.Elapsed.ToString(); }));

            // уничтожаем ненужный список
            //ListSubDestinationSC = new List<string>();
        }
        private void btn_Load_In_Click(object sender, RoutedEventArgs e)
        {
            if (OpenFileDialog1())
            {
                TextBoxPathIn.Text = FilePathIn1;
                _isIn = true;
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

                isCleaningIn = CheckBoxCleaningIn.IsChecked == true;
                if (isCleaningIn)
                {
                    new Thread(() =>
                    {
                        CleanSource();
                    }).Start();
                }

                stopWatch.Stop();
                TextBox15.Text = stopWatch.Elapsed.ToString();
                BtnCsLoadNameIn.IsEnabled = true;
                BtnScLoadNameIn.IsEnabled = true;
                isCompareCS = false;
                isCompareSC = false;
            }
            else
            {
                _isIn = false;
                MessageBox.Show("Для работы программы необходимо выбрать .asm файл!");
            }
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
            }
        }
        private void btn_SC_Load_Name1_Click(object sender, RoutedEventArgs e)
        {
            BtnCsLoadNameIn.IsEnabled = false;
            BtnScLoadNameIn.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;

            var txt = TextBox12.Text;
            DepthIn = 0;

            ListNameSourceSC = new List<string>();
            FindOpcodeIn = CheckBoxFindOpcodeIn.IsChecked == true;
            FindStructIn = CheckBoxFindStructIn.IsChecked == true;

            new Thread(() =>
            {
                FindSourceStructuresSC(txt);
                if (FindOpcodeIn)
                {
                    FindOpcodeSourceSC();
                }
            }).Start();
        }
        private void btn_CS_Load_Name1_Click(object sender, RoutedEventArgs e)
        {
            BtnCsLoadNameIn.IsEnabled = false;
            BtnScLoadNameIn.IsEnabled = false;
            BtnLoadIn.IsEnabled = false;
            var inText = TextBox11.Text;
            DepthIn = 0;

            ListNameSourceCS = new List<string>();
            FindOpcodeIn = CheckBoxFindOpcodeIn.IsChecked == true;
            FindStructIn = CheckBoxFindStructIn.IsChecked == true;

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
            if (OpenFileDialog2())
            {
                TextBoxPathOut.Text = FilePathIn2;
                _isOut = true;
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
                isCleaningOut = CheckBoxCleaningOut.IsChecked == true;
                if (isCleaningOut)
                {
                    new Thread(() =>
                    {
                        CleanDestination();
                    }).Start();
                }

                stopWatch.Stop();
                TextBox25.Text = stopWatch.Elapsed.ToString();
                BtnCsLoadNameOut.IsEnabled = true;
                BtnScLoadNameOut.IsEnabled = true;
                isCompareCS = false;
                isCompareSC = false;
            }
            else
            {
                _isOut = false;
                MessageBox.Show("Для работы программы необходимо выбрать .asm файл!");
            }
            if (_isIn && _isOut)
            {
                BtnLoadOut.IsEnabled = true;
            }
        }
        private void btn_SC_Load_Name2_Click(object sender, RoutedEventArgs e)
        {
            BtnCsLoadNameOut.IsEnabled = false;
            BtnScLoadNameOut.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;
            var outText = TextBox22.Text;
            DepthOut = 0;

            ListNameDestinationSC = new List<string>();
            FindOpcodeOut = CheckBoxFindOpcodeOut.IsChecked == true;
            FindStructOut = CheckBoxFindStructOut.IsChecked == true;

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
            BtnCsLoadNameOut.IsEnabled = false;
            BtnScLoadNameOut.IsEnabled = false;
            BtnLoadOut.IsEnabled = false;
            var txt = TextBox21.Text;
            DepthOut = 0;

            ListNameDestinationCS = new List<string>();
            FindOpcodeOut = CheckBoxFindOpcodeOut.IsChecked == true;
            FindStructOut = CheckBoxFindStructOut.IsChecked == true;

            new Thread(() =>
            {
                FindDestinationStructuresCS(txt);
                if (FindOpcodeOut)
                {
                    FindOpcodeDestinationCS();
                }
            }).Start();
        }
        private void CompareSourceStructuresCS(ref List<string> listNameSource, ref List<string> listNameDestination, ref List<string> listSubDestination, ref Dictionary<int, List<string>> dictSource, ref Dictionary<int, List<string>> dictDestination, List<string> listOpcodes)
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
                                    foundName = true;
                                }
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
                                    ListNameCompareCS[IdxD] = listNameSource[IdxS];

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
        private void CompareSourceStructuresSC(ref List<string> listNameSource, ref List<string> listNameDestination, ref List<string> listSubDestination, ref Dictionary<int, List<string>> dictSource, ref Dictionary<int, List<string>> dictDestination, List<string> listOpcodes)
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
                                    foundName = true;
                                }
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
                                    ListNameCompareSC[IdxD] = listNameSource[IdxS];

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
                CompareSourceStructuresCS(ref ListNameSourceCS, ref ListNameDestinationCS, ref ListSubDestinationCS, ref StructureSourceCS, ref StructureDestinationCS, ListOpcodeDestinationCS);
                isCompareCS = true;
            }
            else
            {
                ListNameCompareCS = new List<string>(ListNameCompare);
            }
            ListView31.ItemsSource = ListNameCompareCS;

            if (CheckBoxCompareManual.IsChecked == true)
            {
                var compareWindow = new CompareWindow();
                compareWindow.Show();
                compareWindow.CompareSourceStructures(ref ListNameSourceCS, ref ListNameDestinationCS, ref ListNameCompareCS, ref ListSubDestinationCS, ref StructureSourceCS, ref StructureDestinationCS, ListOpcodeDestinationCS);
            }

            var tmp = new List<string>();
            var idxD = 0;
            foreach (var t in ListNameCompareCS)
            {
                if (ListOpcodeDestinationCS.Count > 0)
                {
                    tmp.Add(t + " " + ListOpcodeDestinationCS[idxD]);
                }
                else
                {
                    tmp.Add(t + " " + "0xfff");
                }

                idxD++;
            }

            ListView32.ItemsSource = tmp;
            Button2Copy2.IsEnabled = true;
            Button2Copy2_Copy.IsEnabled = true;
        }
        private void button_Copy1_Click(object sender, RoutedEventArgs e)
        {
            // пробуем сравнивать структуры пакетов
            if (!isCompareSC)
            {
                CompareSourceStructuresSC(ref ListNameSourceSC, ref ListNameDestinationSC, ref ListSubDestinationSC, ref StructureSourceSC, ref StructureDestinationSC, ListOpcodeDestinationSC);
                isCompareSC = true;
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
            }

            var tmp = new List<string>();
            var idxD = 0;
            foreach (var t in ListNameCompareSC)
            {
                if (ListOpcodeDestinationCS.Count > 0)
                {
                    tmp.Add(t + " " + ListOpcodeDestinationSC[idxD]);
                }
                else
                {
                    tmp.Add(t + " " + "0xfff");
                }

                idxD++;
            }
            ListView32.ItemsSource = tmp;
            ButtonCopy2.IsEnabled = true;
            ButtonCopy2_Copy.IsEnabled = true;
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
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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
            //foreach (var lsts in InUseIn)
            //{
            //    var lst = "Packet name: " + ListNameCompare[i] + ", PacketBodyReader: " + ListSubDestinationCS[i] + ", Opcode: " + ListOpcodeDestinationCS[i];
            //    tmp.Add(lst);

            //    var src = StructureSourceCS[lsts.Key];
            //    var dst = StructureDestinationCS[lsts.Value];

            //    var ss = 0;
            //    var dd = 0;
            //    var str1 = "";
            //    var str2 = "";
            //    var count = Math.Max(src.Count, dst.Count);
            //    // проходим по самому длинному списку
            //    do
            //    {
            //        str1 = ss < src.Count ? src[ss] : "";
            //        str2 = dd < dst.Count ? dst[dd] : "";

            //        lst = ss + ": " + str1 + "\t\t" + ss + ": " + str2;
            //        tmp.Add(lst);

            //        ss++;
            //        dd++;
            //    } while (ss < count);

            //    tmp.Add("");
            //    i++;
            //}
            if (SaveFileDialog7())
            {
                File.WriteAllLines(FilePathOut7, tmp);
            }
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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

                tmp.Add("--------------------------------------------------------------------------------------------------------------------------------------------");
            }
            //foreach (var lsts in InUseIn)
            //{
            //    var lst = "Packet name: " + ListNameCompare[i] + ", PacketBodyReader: " + ListSubDestinationSC[i] + ", Opcode: " + ListOpcodeDestinationSC[i];
            //    tmp.Add(lst);

            //    var src = StructureSourceSC[lsts.Key];
            //    var dst = StructureDestinationSC[lsts.Value];

            //    var ss = 0;
            //    var dd = 0;
            //    var str1 = "";
            //    var str2 = "";
            //    var count = Math.Max(src.Count, dst.Count);
            //    // проходим по самому длинному списку
            //    do
            //    {
            //        str1 = ss < src.Count ? src[ss] : "";
            //        str2 = dd < dst.Count ? dst[dd] : "";

            //        lst = ss + ": " + str1 + "\t\t" + ss + ": " + str2;
            //        tmp.Add(lst);

            //        ss++;
            //        dd++;
            //    } while (ss < count);

            //    tmp.Add("");
            //    i++;
            //}
            if (SaveFileDialog8())
            {
                File.WriteAllLines(FilePathOut8, tmp);
            }
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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
            if (_isIn && _isOut)
            {
                BtnLoadIn.IsEnabled = true;
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
        /*
        * Which works out about 30% faster than PZahras (not that you'd notice with small amounts of data).
        * The BitConverter method itself is pretty quick, it's just having to do the replace which slows it down, so if you can live with the dashes then it's perfectly good.
        */
        public static string ByteArrayToString(byte[] data)
        {
            char[] lookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            int i = 0, p = 0, l = data.Length;
            char[] c = new char[l * 2 + 2];
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
    }
}
