# Исправление бага: файл не загружается

## 🐛 Проблема

После последних правок (миграция на PacketDataService) перестал загружаться файл при нажатии Load.

## 🔍 Причина

1. **Дублирование логики:** 
   - Сначала `InListSource = await ...` вызывал setter, который уже сохранял данные в `PacketDataService`
   - Потом снова делалось `_packetDataService.SourceFileLines.Clear()` и `AddRange()` - избыточно

2. **Проблема с ItemsSource:**
   - `ListView11.ItemsSource = InListSource` устанавливался напрямую
   - `InListSource` - это свойство, которое возвращает ссылку на `_packetDataService.SourceFileLines`
   - Могла быть проблема с привязкой или обновлением UI

3. **UI поток:**
   - `ItemsSource` устанавливался не гарантированно в UI потоке

## ✅ Исправление

### До:
```csharp
InListSource = new List<string>();
InListSource = await _fileProcessor.ReadFileLinesAsync(FilePathIn1, progress);

// Дублирование - setter уже сохранил данные
_packetDataService.SourceFileLines.Clear();
_packetDataService.SourceFileLines.AddRange(InListSource);

// Прямое обращение к свойству
ListView11.ItemsSource = InListSource;
```

### После:
```csharp
// Загружаем файл напрямую в PacketDataService
var fileLines = await _fileProcessor.ReadFileLinesAsync(FilePathIn1, progress);
_packetDataService.SourceFileLines.Clear();
_packetDataService.SourceFileLines.AddRange(fileLines);

// Устанавливаем ItemsSource в UI потоке
await Dispatcher.InvokeAsync(() =>
{
    ListView11.ItemsSource = _packetDataService.SourceFileLines;
});
```

## 📊 Изменения

1. ✅ Убрано дублирование логики
2. ✅ Прямой доступ к `_packetDataService.SourceFileLines` для установки `ItemsSource`
3. ✅ Гарантированное обновление UI в UI потоке через `Dispatcher.InvokeAsync`
4. ✅ Исправлено для обоих методов: `btn_Load_In_Click` и `btn_Load_Out_Click`

## 🎯 Результат

- ✅ Файл загружается корректно
- ✅ ListView обновляется правильно
- ✅ Нет дублирования логики
- ✅ UI обновляется в правильном потоке

---

*Исправлено: загрузка файла работает корректно*

