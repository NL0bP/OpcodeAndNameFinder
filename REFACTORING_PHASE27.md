# Рефакторинг - Фаза 27: Миграция IsRenameDestination на PacketDataService

## ✅ Выполнено

### 1. Добавлена поддержка IsRenameDestination в PacketDataService
- ✅ Добавлено свойство `IsRenameDestination` в `IPacketDataService`
- ✅ Реализовано в `PacketDataService` как `Dictionary<int, bool>`
- ✅ Обновлен метод `ClearDestination()` для очистки `IsRenameDestination`

### 2. Создано свойство-обертка
- ✅ `IsRenameDestination` - свойство, использующее `PacketDataService.IsRenameDestination`
- ✅ Автоматическая синхронизация при установке значения

### 3. Закомментировано static поле
- ✅ `IsRenameDestination` заменено на закомментированную версию
- ✅ Сохранена обратная совместимость через свойство-обертку

## 📊 Преимущества

### Централизованное управление данными
- **До:** 1 static поле для IsRenameDestination разбросано по MainWindow
- **После:** Все данные в PacketDataService
- **Результат:** Единая точка управления данными

### Автоматическая очистка
- **До:** Ручная очистка IsRenameDestination
- **После:** Автоматическая очистка при вызове `ClearDestination()`
- **Результат:** Меньше кода, меньше ошибок

### Улучшенная тестируемость
- **До:** Static поле сложно тестировать
- **После:** Можно мокировать PacketDataService
- **Результат:** Возможность unit-тестирования

### Готовность к дальнейшему рефакторингу
- Все данные о переименовании в сервисе
- Можно постепенно мигрировать остальные поля
- Готовность к внедрению DI

## 🔄 Реализация

### Добавление в PacketDataService
```csharp
public Dictionary<int, bool> IsRenameDestination { get; } = new Dictionary<int, bool>();
```

### Свойство-обертка
```csharp
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
```

### Автоматическая очистка
```csharp
public void ClearDestination()
{
    // ... другие очистки ...
    IsRenameDestination.Clear();
    // ...
}
```

## 📁 Обновленные файлы

### Services/IPacketDataService.cs
- ✅ Добавлено свойство `IsRenameDestination`

### Services/PacketDataService.cs
- ✅ Реализовано свойство `IsRenameDestination`
- ✅ Обновлен метод `ClearDestination()` для очистки `IsRenameDestination`

### MainWindow.xaml.cs
- ✅ Добавлено свойство-обертка для `IsRenameDestination`
- ✅ Закомментировано старое static поле
- ✅ Все обращения теперь используют PacketDataService

## 📋 Следующие шаги

1. Проверить и мигрировать `ListNameCompareOutCS/SC` на PacketDataService
2. Проверить использование `ListNameCompare` (используется в CompareWindow как временный список)
3. Продолжить миграцию других static полей
4. Полностью убрать static поля

## ⚠️ Обратная совместимость

✅ Все существующие обращения работают
✅ Прозрачная миграция через свойство-обертку
✅ Нет breaking changes
✅ Можно постепенно заменять использования
✅ Автоматическая очистка при вызове ClearDestination

## 🎯 Достижения

- **Централизация данных:** Все данные о переименовании в PacketDataService
- **Обратная совместимость:** Свойство-обертка сохраняет совместимость
- **Автоматическая очистка:** IsRenameDestination очищается автоматически
- **Готовность к тестированию:** Можно мокировать сервис
- **Готовность к DI:** Можно внедрить через конструктор
- **Улучшение архитектуры:** Убран static модификатор

## 📊 Статистика

- **Мигрировано полей:** 1 static поле → свойство-обертка
- **Создано свойств:** 1 свойство-обертка
- **Улучшение:** Централизация данных, автоматическая очистка, готовность к тестированию

## 🔍 Особенности реализации

### Простое свойство-обертка
В отличие от других свойств, `IsRenameDestination` не разделяется по типам пакетов (CS/SC), поэтому реализация проще - просто делегирует к `PacketDataService.IsRenameDestination`.

### Автоматическая очистка
`IsRenameDestination` автоматически очищается при вызове `ClearDestination()`, что упрощает управление данными и уменьшает вероятность ошибок.

### Использование в методах сравнения
`IsRenameDestination` используется в методах `CompareSourceStructuresCS` и `CompareSourceStructuresSC` для отслеживания, какие пакеты были переименованы. Теперь это управляется через PacketDataService.

---

*Обновлено: мигрирован IsRenameDestination на PacketDataService с автоматической очисткой*

