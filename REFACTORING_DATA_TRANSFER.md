# Стандартные методы передачи данных между окнами в WPF

## Текущая проблема
Сейчас используется множественное копирование списков и словарей, что неэффективно и сложно в поддержке.

## Рекомендуемые решения

### 1. ObservableCollection + DataBinding (Самый правильный подход)

**Преимущества:**
- Автоматическое обновление UI при изменении данных
- Нет необходимости в копировании
- Стандартный WPF подход

**Реализация:**
```csharp
// В MainWindow.xaml.cs
public ObservableCollection<string> ListNameCompareCS { get; set; } = new ObservableCollection<string>();

// В XAML
<ListView ItemsSource="{Binding ListNameCompareCS}" />

// В CompareWindow - работаем с той же коллекцией
compareWindow.ListNameCompareCS = this.ListNameCompareCS; // Передаем ссылку
// Изменения автоматически отобразятся в MainWindow
```

### 2. События для уведомления об изменениях

**Преимущества:**
- Минимальные изменения в коде
- Четкое разделение ответственности
- Легко тестировать

**Реализация:**
```csharp
// В CompareWindow.xaml.cs
public event EventHandler<DataChangedEventArgs> DataChanged;

public class DataChangedEventArgs : EventArgs
{
    public List<string> ListNameCompare { get; set; }
    public bool IsDestinationNameChanged { get; set; }
}

// В ButtonQuit_Click
DataChanged?.Invoke(this, new DataChangedEventArgs 
{ 
    ListNameCompare = ListNameCompare,
    IsDestinationNameChanged = isDestinationNameChanged 
});

// В MainWindow.button2_Copy1_Click
compareWindow.DataChanged += (s, e) => 
{
    if (e.IsDestinationNameChanged)
    {
        ListNameCompareCS = e.ListNameCompare; // Просто присваиваем
    }
};
```

### 3. Callback/Delegate (Самый простой для текущего кода)

**Преимущества:**
- Минимальные изменения
- Прямая передача данных
- Нет необходимости в событиях

**Реализация:**
```csharp
// В CompareWindow.xaml.cs
public Action<List<string>, bool> OnDataChanged { get; set; }

// В ButtonQuit_Click
OnDataChanged?.Invoke(ListNameCompare, isDestinationNameChanged);

// В MainWindow.button2_Copy1_Click
compareWindow.OnDataChanged = (listNameCompare, isChanged) => 
{
    if (isChanged)
    {
        ListNameCompareCS = listNameCompare;
    }
};
```

### 4. Передача ссылок напрямую (Если данные не изменяются структурно)

**Преимущества:**
- Самый простой вариант
- Нет копирования
- Работает если изменяются только элементы, а не структура списка

**Реализация:**
```csharp
// В CompareWindow - работаем напрямую с переданной коллекцией
public void CompareSourceStructures(List<string> listNameCompare)
{
    ListNameCompare = listNameCompare; // Сохраняем ссылку
    // Изменяем элементы напрямую
    ListNameCompare[0] = "NewName";
    // MainWindow увидит изменения автоматически
}
```

## Рекомендация для текущего проекта

**Вариант 2 (События)** - лучший баланс между простотой и правильностью:
- Минимальные изменения в коде
- Убирает необходимость в копировании
- Четкое разделение ответственности
- Легко расширять

**Альтернатива: Вариант 3 (Callback)** - если нужен самый простой вариант:
- Минимальный код
- Прямая передача данных
- Легко понять

