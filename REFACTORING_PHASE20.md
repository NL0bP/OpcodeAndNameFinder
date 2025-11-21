# Рефакторинг - Фаза 20: Расширение PacketDataService для поддержки списков имен

## ✅ Выполнено

### 1. Расширен интерфейс IPacketDataService
- ✅ Добавлены свойства `SourcePacketNames` - списки имен пакетов (Source)
- ✅ Добавлены свойства `SourceSubNames` - списки имен подпрограмм (Source)
- ✅ Добавлены свойства `DestinationPacketNames` - списки имен пакетов (Destination)
- ✅ Добавлены свойства `DestinationSubNames` - списки имен подпрограмм (Destination)

### 2. Расширена реализация PacketDataService
- ✅ Реализованы все новые свойства
- ✅ Обновлены методы `ClearSource()` и `ClearDestination()` для очистки новых коллекций
- ✅ Инициализация всех словарей по типам пакетов (CS/SC)

## 📊 Преимущества

### Централизация данных
- **До:** Static поля `ListNameSourceCS/SC`, `ListSubSourceCS/SC` разбросаны по MainWindow
- **После:** Все данные в PacketDataService
- **Результат:** Единая точка управления данными

### Готовность к миграции
- **До:** Нет поддержки для списков имен в PacketDataService
- **После:** Полная поддержка через словари по типам пакетов
- **Результат:** Можно начать миграцию static полей

### Консистентность
- **До:** Разные подходы к хранению данных
- **После:** Единый подход через PacketDataService
- **Результат:** Консистентный код

## 🔄 Реализация

### Новые свойства в IPacketDataService
```csharp
// Списки имен пакетов (Source)
Dictionary<PacketType, List<string>> SourcePacketNames { get; }
Dictionary<PacketType, List<string>> SourceSubNames { get; }

// Списки имен пакетов (Destination)
Dictionary<PacketType, List<string>> DestinationPacketNames { get; }
Dictionary<PacketType, List<string>> DestinationSubNames { get; }
```

### Маппинг
- `ListNameSourceCS` → `SourcePacketNames[PacketType.CS]`
- `ListNameSourceSC` → `SourcePacketNames[PacketType.SC]`
- `ListSubSourceCS` → `SourceSubNames[PacketType.CS]`
- `ListSubSourceSC` → `SourceSubNames[PacketType.SC]`
- `ListNameDestinationCS` → `DestinationPacketNames[PacketType.CS]`
- `ListNameDestinationSC` → `DestinationPacketNames[PacketType.SC]`
- `ListSubDestinationCS` → `DestinationSubNames[PacketType.CS]`
- `ListSubDestinationSC` → `DestinationSubNames[PacketType.SC]`

## 📁 Обновленные файлы

### Services/IPacketDataService.cs
- ✅ Добавлены новые свойства для списков имен

### Services/PacketDataService.cs
- ✅ Реализованы новые свойства
- ✅ Обновлены методы очистки

## 📋 Следующие шаги

1. Создать свойства-обертки в MainWindow для обратной совместимости
2. Постепенно заменить использования static полей на PacketDataService
3. Добавить поддержку для структур (StructureSourceCS/SC)
4. Добавить поддержку для сравнения (ListNameCompareCS/SC)

## ⚠️ Обратная совместимость

✅ Интерфейс расширен, но не изменен
✅ Старые свойства остались без изменений
✅ Нет breaking changes
✅ Можно постепенно мигрировать

## 🎯 Достижения

- **Централизация:** Подготовка к миграции списков имен
- **Расширяемость:** Легко добавить новые типы данных
- **Консистентность:** Единый подход к хранению данных

---

*Обновлено: расширен PacketDataService для поддержки списков имен пакетов*

