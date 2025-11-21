# Рефакторинг - Фаза 28: Миграция ListNameCompareOutCS/SC на PacketDataService

## ✅ Выполнено

### 1. Добавлена поддержка CompareOutNames в PacketDataService
- ✅ Добавлено свойство `CompareOutNames` в `IPacketDataService`
- ✅ Реализовано в `PacketDataService` с поддержкой CS и SC типов
- ✅ Обновлен метод `ClearDestination()` для очистки `CompareOutNames`

### 2. Созданы свойства-обертки для ListNameCompareOut
- ✅ `ListNameCompareOutCS` - свойство, использующее `PacketDataService.CompareOutNames[PacketType.CS]`
- ✅ `ListNameCompareOutSC` - свойство, использующее `PacketDataService.CompareOutNames[PacketType.SC]`

### 3. Закомментированы static поля
- ✅ `ListNameCompareOutCS` и `ListNameCompareOutSC` заменены на закомментированные версии
- ✅ Сохранена обратная совместимость через свойства-обертки
- ✅ `ListNameCompare` оставлен как static (используется в CompareWindow как временный список)

## 📊 Преимущества

### Централизованное управление данными
- **До:** 2 static поля для ListNameCompareOut разбросаны по MainWindow
- **После:** Все данные в PacketDataService
- **Результат:** Единая точка управления данными

### Автоматическая очистка
- **До:** Ручная очистка ListNameCompareOutCS/SC
- **После:** Автоматическая очистка при вызове `ClearDestination()`
- **Результат:** Меньше кода, меньше ошибок

### Улучшенная тестируемость
- **До:** Static поля сложно тестировать
- **После:** Можно мокировать PacketDataService
- **Результат:** Возможность unit-тестирования

### Готовность к дальнейшему рефакторингу
- Все данные о сравнении Out в сервисе
- Можно постепенно мигрировать остальные поля
- Готовность к внедрению DI

## 🔄 Реализация

### Добавление в PacketDataService
```csharp
public Dictionary<PacketType, List<string>> CompareOutNames { get; } = new Dictionary<PacketType, List<string>>
{
    { PacketType.CS, new List<string>() },
    { PacketType.SC, new List<string>() }
};
```

### Свойства-обертки
```csharp
public List<string> ListNameCompareOutCS
{
    get => _packetDataService.CompareOutNames[Models.PacketType.CS];
    set
    {
        _packetDataService.CompareOutNames[Models.PacketType.CS].Clear();
        if (value != null)
        {
            _packetDataService.CompareOutNames[Models.PacketType.CS].AddRange(value);
        }
    }
}
```

### Автоматическая очистка
```csharp
public void ClearDestination()
{
    // ... другие очистки ...
    CompareOutNames[PacketType.CS].Clear();
    CompareOutNames[PacketType.SC].Clear();
    // ...
}
```

## 📁 Обновленные файлы

### Services/IPacketDataService.cs
- ✅ Добавлено свойство `CompareOutNames`

### Services/PacketDataService.cs
- ✅ Реализовано свойство `CompareOutNames`
- ✅ Обновлен метод `ClearDestination()` для очистки `CompareOutNames`

### MainWindow.xaml.cs
- ✅ Добавлены свойства-обертки для ListNameCompareOut (2 свойства)
- ✅ Закомментированы старые static поля
- ✅ Все обращения теперь используют PacketDataService

## 📋 Следующие шаги

1. Рассмотреть миграцию `ListNameCompare` (используется в CompareWindow как временный список - можно оставить как static)
2. Продолжить миграцию других static полей (если есть)
3. Полностью убрать static поля (кроме временных списков)

## ⚠️ Обратная совместимость

✅ Все существующие обращения работают
✅ Прозрачная миграция через свойства-обертки
✅ Нет breaking changes
✅ Можно постепенно заменять использования
✅ Автоматическая очистка при вызове ClearDestination

## 🎯 Достижения

- **Централизация данных:** Все данные о сравнении Out в PacketDataService
- **Обратная совместимость:** Свойства-обертки сохраняют совместимость
- **Автоматическая очистка:** ListNameCompareOutCS/SC очищаются автоматически
- **Готовность к тестированию:** Можно мокировать сервис
- **Готовность к DI:** Можно внедрить через конструктор
- **Улучшение архитектуры:** Убраны static модификаторы

## 📊 Статистика

- **Мигрировано полей:** 2 static поля → свойства-обертки
- **Создано свойств:** 2 свойства-обертки
- **Улучшение:** Централизация данных, автоматическая очистка, готовность к тестированию

## 🔍 Особенности реализации

### Использование CompareOutNames
`ListNameCompareOutCS/SC` используются для хранения результатов сравнения для Out пакетов (пакеты без известных имен). Они создаются на основе `ListNameCompareCS/SC` с добавлением опкодов.

### Оставление ListNameCompare как static
`ListNameCompare` оставлен как static, так как он используется только как временный список в `CompareWindow` для работы с данными во время сравнения. Это нормально для временных данных, которые не требуют централизованного управления.

### Автоматическая очистка
`ListNameCompareOutCS/SC` автоматически очищаются при вызове `ClearDestination()`, что упрощает управление данными и уменьшает вероятность ошибок.

---

*Обновлено: мигрированы ListNameCompareOutCS/SC на PacketDataService с автоматической очисткой*

