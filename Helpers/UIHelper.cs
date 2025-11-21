using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NameFinder.Helpers
{
    /// <summary>
    /// Вспомогательный класс для работы с UI в WPF
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Выполняет действие в UI потоке
        /// </summary>
        public static void InvokeUI(Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Background)
        {
            if (dispatcher == null || action == null)
                return;

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(priority, action);
            }
        }

        /// <summary>
        /// Выполняет действие в UI потоке асинхронно
        /// </summary>
        public static async System.Threading.Tasks.Task InvokeUIAsync(Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Background)
        {
            if (dispatcher == null || action == null)
                return;

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                await dispatcher.InvokeAsync(action, priority);
            }
        }

        /// <summary>
        /// Группирует несколько UI обновлений в один вызов Dispatcher
        /// </summary>
        public static void InvokeUIBatch(Dispatcher dispatcher, params Action[] actions)
        {
            if (dispatcher == null || actions == null || actions.Length == 0)
                return;

            InvokeUI(dispatcher, () =>
            {
                foreach (var action in actions)
                {
                    action?.Invoke();
                }
            });
        }

        /// <summary>
        /// Группирует несколько UI обновлений в один асинхронный вызов Dispatcher
        /// </summary>
        public static async System.Threading.Tasks.Task InvokeUIBatchAsync(Dispatcher dispatcher, params Action[] actions)
        {
            if (dispatcher == null || actions == null || actions.Length == 0)
                return;

            await InvokeUIAsync(dispatcher, () =>
            {
                foreach (var action in actions)
                {
                    action?.Invoke();
                }
            });
        }

        /// <summary>
        /// Показывает сообщение об ошибке
        /// </summary>
        public static void ShowError(Dispatcher dispatcher, string message, string title = "Ошибка")
        {
            InvokeUI(dispatcher, () =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// Показывает информационное сообщение
        /// </summary>
        public static void ShowInfo(Dispatcher dispatcher, string message, string title = "Информация")
        {
            InvokeUI(dispatcher, () =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}

