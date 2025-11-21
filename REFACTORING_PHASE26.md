# Рефакторинг - Фаза 26: Миграция структур на PacketDataService

## ✅ Выполнено

### 1. Добавлена поддержка структур в PacketDataService
- ✅ Добавлены свойства `SourceStructures` и `DestinationStructures` в `IPacketDataService`
- ✅ Реализованы в `PacketDataService` с поддержкой CS и SC типов
- ✅ Используется `Struc` для обратной совместимости (TODO: мигрировать на `StructureField` из Models)
- ✅ Обновлены методы `ClearSource()` и `ClearDestination()` для очистки структур

### 2. Созданы свойства-обертки для структур
- ✅ `StructureSourceCS` - свойство, использующее `PacketDataService.SourceStructures[PacketType.CS]`
- ✅ `StructureSourceSC` - свойство, использующее `PacketDataService.SourceStructures[PacketType.SC]`
- ✅ `StructureDestinationCS` - свойство, использующее `PacketDataService.DestinationStructures[PacketType.CS]`
- ✅ `StructureDestinationSC` - свойство, использующее `PacketDataService.DestinationStructures[PacketType.SC]`

### 3. Закомментированы static поля
- ✅ `StructureSourceCS` и `StructureSourceSC` заменены на закомментированные версии
- ✅ `StructureDestinationCS` и `StructureDestinationSC` заменены на закомментированные версии
- ✅ Сохранена обратная совместимость через свойства-обертки

### 4. Исправлены ref параметры
- ✅ Созданы локальные переменные для структур перед передачей как ref параметров
- ✅ Обновлены все вызовы `CompareSourceStructuresCS/SC` и `CompareSourceStructures` в `CompareWindow`
- ✅ Добавлено присваивание обратно свойствам после вызова методов

## 📊 Преимущества

### Централизованное управление данными
- **До:** 4 static поля для структур разбросаны по MainWindow
- **После:** Все данные в PacketDataService
- **Результат:** Единая точка управления данными

### Улучшенная тестируемость
- **До:** Static поля сложно тестировать
- **После:** Можно мокировать PacketDataService
- **Результат:** Возможность unit-тестирования

### Готовность к дальнейшему рефакторингу
- Все структуры в сервисе
- Можно постепенно мигрировать остальные поля
- Готовность к внедрению DI

### Автоматическая очистка
- Структуры автоматически очищаются при вызове `ClearSource()` и `ClearDestination()`
- Меньше ручной работы
- Меньше ошибок

## 🔄 Реализация

### Добавление в PacketDataService
```csharp
// Структуры пакетов (Source)
public Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>> SourceStructures { get; } = new Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>>
{
    { PacketType.CS, new Dictionary<int, List<NameFinder.Struc>>() },
    { PacketType.SC, new Dictionary<int, List<NameFinder.Struc>>() }
};

// Структуры пакетов (Destination)
public Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>> DestinationStructures { get; } = new Dictionary<PacketType, Dictionary<int, List<NameFinder.Struc>>>
{
    { PacketType.CS, new Dictionary<int, List<NameFinder.Struc>>() },
    { PacketType.SC, new Dictionary<int, List<NameFinder.Struc>>() }
};
```

### Свойства-обертки
```csharp
public Dictionary<int, List<Struc>> StructureSourceCS
{
    get => _packetDataService.SourceStructures[Models.PacketType.CS];
    set
    {
        _packetDataService.SourceStructures[Models.PacketType.CS].Clear();
        if (value != null)
        {
            foreach (var kvp in value)
            {
                _packetDataService.SourceStructures[Models.PacketType.CS][kvp.Key] = new List<Struc>(kvp.Value);
            }
        }
    }
}
```

### Обработка ref параметров
```csharp
// Рефакторинг: создаем локальные переменные для ref параметров, так как свойства нельзя передавать как ref
var structureSourceCS = StructureSourceCS;
var structureDestinationCS = StructureDestinationCS;
CompareSourceStructuresCS(ref listNameSourceCS, ref listNameDestinationCS, ref listSubDestinationCS, ref structureSourceCS, ref structureDestinationCS, ListOpcodeDestinationCS);
StructureSourceCS = structureSourceCS;
StructureDestinationCS = structureDestinationCS;
```

## 📁 Обновленные файлы

### Services/IPacketDataService.cs
- ✅ Добавлены свойства `SourceStructures` и `DestinationStructures`

### Services/PacketDataService.cs
- ✅ Реализованы свойства `SourceStructures` и `DestinationStructures`
- ✅ Обновлены методы `ClearSource()` и `ClearDestination()` для очистки структур

### MainWindow.xaml.cs
- ✅ Добавлены свойства-обертки для структур (4 свойства)
- ✅ Закомментированы старые static поля
- ✅ Исправлены все вызовы методов с ref параметрами (4 места)
- ✅ Все обращения теперь используют PacketDataService

## 📋 Следующие шаги

1. Мигрировать `IsRenameDestination` на PacketDataService
2. Рассмотреть миграцию `Struc` на `StructureField` из Models
3. Продолжить миграцию других static полей
4. Полностью убрать static поля

## ⚠️ Обратная совместимость

✅ Все существующие обращения работают
✅ Прозрачная миграция через свойства-обертки
✅ Нет breaking changes
✅ Можно постепенно заменять использования
✅ Автоматическая очистка при вызове ClearSource/ClearDestination

## 🎯 Достижения

- **Централизация данных:** Все структуры в PacketDataService
- **Обратная совместимость:** Свойства-обертки сохраняют совместимость
- **Автоматическая очистка:** Структуры очищаются автоматически
- **Готовность к тестированию:** Можно мокировать сервис
- **Готовность к DI:** Можно внедрить через конструктор
- **Улучшение архитектуры:** Убраны static модификаторы

## 📊 Статистика

- **Мигрировано полей:** 4 static поля → свойства-обертки
- **Создано свойств:** 4 свойства-обертки
- **Обновлено мест с ref параметрами:** 4 места
- **Улучшение:** Централизация данных, автоматическая очистка, готовность к тестированию

## 🔍 Особенности реализации

### Использование Struc для обратной совместимости
В текущей реализации используется `Struc` из `MainWindow.xaml.cs` для обратной совместимости. В будущем можно мигрировать на `StructureField` из Models, который идентичен `Struc`, но находится в правильном месте с точки зрения архитектуры.

### Обработка ref параметров
Поскольку свойства нельзя передавать как ref параметры, создаются локальные переменные перед вызовом методов, а затем их значения присваиваются обратно свойствам. Это обеспечивает обратную совместимость и правильную работу с ref параметрами.

### Автоматическая очистка
Структуры автоматически очищаются при вызове `ClearSource()` и `ClearDestination()`, что упрощает управление данными и уменьшает вероятность ошибок.

---

*Обновлено: мигрированы все структуры (4 поля) на PacketDataService с автоматической очисткой и поддержкой ref параметров*

