using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using NameFinder.Models;

namespace NameFinder.Services
{
    /// <summary>
    /// Обертка для OpcodeFinderService с поддержкой UI обновлений
    /// </summary>
    public class OpcodeFinderWrapper
    {
        private readonly IOpcodeFinderService _opcodeFinderService;
        private readonly Dispatcher _dispatcher;

        public OpcodeFinderWrapper(IOpcodeFinderService opcodeFinderService, Dispatcher dispatcher)
        {
            _opcodeFinderService = opcodeFinderService ?? throw new ArgumentNullException(nameof(opcodeFinderService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Находит опкоды с обновлением UI
        /// </summary>
        public async Task<List<string>> FindOpcodesWithUIAsync(
            PacketType packetType,
            PacketSource source,
            List<string> fileLines,
            Dictionary<int, List<string>> xrefs,
            Action<int> updateProgressBar,
            Action<int> updateMaxProgress,
            Action<string> updateTotalOpcodes,
            Action<string> updateNotFound,
            Action<string> updateTime,
            Action<SolidColorBrush> updateSemafor,
            Action<bool> updateButtonsEnabled)
        {
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Инициализация UI
            _dispatcher.Invoke(() =>
            {
                updateMaxProgress(xrefs?.Count ?? 0);
                updateProgressBar(0);
                updateTotalOpcodes("0");
                updateNotFound("0");
                updateTime("0");
                updateSemafor(Brushes.Yellow);
                updateButtonsEnabled(false);
            });

            try
            {
                var progress = new Progress<int>(percent =>
                {
                    _dispatcher.Invoke(() => updateProgressBar(percent));
                });

                var opcodes = await _opcodeFinderService.FindOpcodesAsync(
                    packetType,
                    source,
                    fileLines,
                    xrefs,
                    progress);

                var notFoundCount = opcodes.Count(o => o == "0xfff");

                stopWatch.Stop();

                // Обновление UI с результатами
                _dispatcher.Invoke(() =>
                {
                    updateTotalOpcodes(opcodes.Count.ToString());
                    updateNotFound(notFoundCount.ToString());
                    updateTime(stopWatch.Elapsed.ToString());
                    updateSemafor(Brushes.GreenYellow);
                    updateButtonsEnabled(true);
                });

                return opcodes;
            }
            catch (Exception ex)
            {
                stopWatch.Stop();
                
                _dispatcher.Invoke(() =>
                {
                    updateSemafor(Brushes.Red);
                    updateButtonsEnabled(true);
                    MessageBox.Show($"Ошибка при поиске опкодов: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });

                return new List<string>();
            }
        }
    }
}

