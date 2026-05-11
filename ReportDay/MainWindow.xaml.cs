using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace ReportDay
{
    public partial class MainWindow : Window
    {
        private bool isDarkTheme = true;
        private MediaPlayer _soundPlayer = new MediaPlayer();
        private readonly string savePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autosave.json");

        // Цвета для тем
        private static readonly string DarkBgColor = "#232323";
        private static readonly string DarkTextColor = "#E5E5E5";
        private static readonly string LightBgColor = "#FDFCF5";
        private static readonly string LightTextColor = "#333333";

        // Списки данных
        public ObservableCollection<ReportItem> Tasks { get; set; } = new ObservableCollection<ReportItem>();
        public ObservableCollection<ReportItem> Fixed { get; set; } = new ObservableCollection<ReportItem>();
        public ObservableCollection<ReportItem> Returned { get; set; } = new ObservableCollection<ReportItem>();

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
            DataContext = this;

            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                SaveData();
                PlaySound("saveText.mp3");
                e.Handled = true;
            }
        }
        private void AutoTimeSetup_Click(object sender, RoutedEventArgs e)
        {
            int hour = DateTime.Now.Hour;

            // 1. Настраиваем ПРИВЕТСТВИЕ
            if (hour >= 5 && hour < 12)
                GreetingCombo.SelectedIndex = 2; // Доброе утро
            else if (hour >= 12 && hour < 18)
                GreetingCombo.SelectedIndex = 0; // Добрый день
            else
                GreetingCombo.SelectedIndex = 1; // Добрый вечер

            // 2. Настраиваем ЗАГОЛОВКИ (логика: утро - планируем задачи, вечер - отчет "сегодня")
            if (hour < 15) // До 3 часов дня
            {
                HeaderSelector1.SelectedIndex = 0; // "1. Задачи"
            }
            else // После 3 часов дня переключаем на отчетный стиль
            {
                HeaderSelector1.SelectedIndex = 1; // "1. Сегодня"
                HeaderSelector2.SelectedIndex = 0; // "2. Пофикшено" (на случай если менял)
            }

            // Воспроизведем звук магии
            PlaySound("magic.mp3");

            // Небольшое уведомление, чтобы понять, что сработало
            // MessageBox.Show("Время учтено! Отчет подстроен.");
        }
        // --- ЛОГИКА СОХРАНЕНИЯ ---
        private void SaveData()
        {
            try
            {
                var data = new SaveModel { T = Tasks, F = Fixed, R = Returned };
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(savePath, json);
            }
            catch { }
        }

        private void LoadData()
        {
            if (File.Exists(savePath))
            {
                try
                {
                    string json = File.ReadAllText(savePath);
                    var data = JsonConvert.DeserializeObject<SaveModel>(json);
                    if (data != null)
                    {
                        if (data.T != null) foreach (var item in data.T) Tasks.Add(item);
                        if (data.F != null) foreach (var item in data.F) Fixed.Add(item);
                        if (data.R != null) foreach (var item in data.R) Returned.Add(item);
                    }
                }
                catch { }
            }
        }

        // --- ЛОГИКА ТЕМ ---
        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                var appRes = Application.Current.Resources.MergedDictionaries[0];
                var cc = new ColorConverter();
                string themeTag = selectedItem.Tag?.ToString();

                if (themeTag == "Light")
                {
                    appRes["WindowBackground"] = new SolidColorBrush((Color)cc.ConvertFrom(LightBgColor));
                    appRes["MainText"] = new SolidColorBrush((Color)cc.ConvertFrom(LightTextColor));
                    isDarkTheme = false;
                }
                else if (themeTag == "Dark")
                {
                    appRes["WindowBackground"] = new SolidColorBrush((Color)cc.ConvertFrom(DarkBgColor));
                    appRes["MainText"] = new SolidColorBrush((Color)cc.ConvertFrom(DarkTextColor));
                    isDarkTheme = true;
                }
            }
        }

        // --- ЛОГИКА ЗАГОЛОВКОВ ---
        private void HeaderSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox selector && selector.SelectedItem is ComboBoxItem item)
            {
                string currentHeader = item.Content.ToString();
            }
        }

        // --- УПРАВЛЕНИЕ СПИСКАМИ ---
        private void AddTask_Click(object sender, RoutedEventArgs e) { Tasks.Add(new ReportItem()); SaveData(); }
        private void AddFixed_Click(object sender, RoutedEventArgs e) { Fixed.Add(new ReportItem()); SaveData(); }
        private void AddReturned_Click(object sender, RoutedEventArgs e) { Returned.Add(new ReportItem { IsReturned = true }); SaveData(); }

        private void RemoveTask_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ReportItem;
            if (Tasks.Contains(item)) Tasks.Remove(item);
            else if (Fixed.Contains(item)) Fixed.Remove(item);
            else if (Returned.Contains(item)) Returned.Remove(item);
            SaveData();
        }

        // ПЕРЕМЕЩЕНИЕ ВВЕРХ / ВНИЗ
        private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveItem(sender, -1);
        private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveItem(sender, 1);

        private void MoveItem(object sender, int direction)
        {
            var item = (sender as FrameworkElement).DataContext as ReportItem;
            if (item == null) return;

            if (Tasks.Contains(item)) MoveInCollection(Tasks, item, direction);
            else if (Fixed.Contains(item)) MoveInCollection(Fixed, item, direction);
            else if (Returned.Contains(item)) MoveInCollection(Returned, item, direction);
            SaveData();
        }

        private void MoveInCollection(ObservableCollection<ReportItem> collection, ReportItem item, int direction)
        {
            int oldIndex = collection.IndexOf(item);
            int newIndex = oldIndex + direction;
            if (newIndex >= 0 && newIndex < collection.Count) collection.Move(oldIndex, newIndex);
        }

        // ПЕРЕНОС В ФИКСЫ
        private void MoveToFixed_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ReportItem;
            if (item != null && Tasks.Contains(item))
            {
                Tasks.Remove(item);
                Fixed.Add(item);
                SaveData();
            }
        }

        // ИЗ ЗАДАЧ -> В ВЕРНУЛ В РАБОТУ (Кнопка в списке задач)
        private void MoveToReturned_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ReportItem;
            if (item != null && Tasks.Contains(item))
            {
                Tasks.Remove(item);
                item.IsReturned = true; // Помечаем как возврат, если нужно
                Returned.Add(item);
                SaveData();
            }
        }

        // ИЗ ФИКСОВ -> В ВЕРНУЛ В РАБОТУ (Кнопка в списке фиксов "мало ли что")
        private void FixedToReturned_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ReportItem;
            if (item != null && Fixed.Contains(item))
            {
                Fixed.Remove(item);
                item.IsReturned = true;
                Returned.Add(item);
                SaveData();
            }
        }

        // ИЗ ВЕРНУЛ В РАБОТУ -> В ФИКСЫ (Кнопка в списке возвратов)
        private void ReturnedToFixed_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ReportItem;
            if (item != null && Returned.Contains(item))
            {
                Returned.Remove(item);
                item.IsReturned = false; // Снимаем флаг возврата при переносе в фиксы
                Fixed.Add(item);
                SaveData();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (string.IsNullOrWhiteSpace(textBox?.Text)) return;
                var context = textBox.DataContext as ReportItem;
                if (Tasks.Contains(context)) Tasks.Add(new ReportItem());
                else if (Fixed.Contains(context)) Fixed.Add(new ReportItem());
                else if (Returned.Contains(context)) Returned.Add(new ReportItem { IsReturned = true });
                SaveData();
            }
        }

        // --- ГЕНЕРАЦИЯ ОТЧЕТА ---
        private void CopyReport_Click(object sender, RoutedEventArgs e)
        {
            PlayDeckSound();
            StartFirework();

            StringBuilder sb = new StringBuilder();
            var greeting = (GreetingCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
            sb.AppendLine($"{greeting}!");
            sb.AppendLine();

            // СЕКЦИЯ 1
            if (Tasks.Any())
            {
                var h1 = (HeaderSelector1.SelectedItem as ComboBoxItem)?.Content.ToString();
                sb.AppendLine(h1);
                sb.AppendLine();
                for (int i = 0; i < Tasks.Count; i++)
                {
                    sb.AppendLine($"{1}.{i + 1}.\t{Tasks[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Tasks[i].Link)) sb.AppendLine($"\t{Tasks[i].Link}");
                    sb.AppendLine();
                }
            }

            // СЕКЦИЯ 2
            if (Fixed.Any())
            {
                var h2 = (HeaderSelector2.SelectedItem as ComboBoxItem)?.Content.ToString();
                sb.AppendLine(h2);
                sb.AppendLine();
                for (int i = 0; i < Fixed.Count; i++)
                {
                    sb.AppendLine($"{2}.{i + 1}.\t{Fixed[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Fixed[i].Link)) sb.AppendLine($"\t{Fixed[i].Link}");
                    sb.AppendLine();
                }
            }

            // СЕКЦИЯ 3
            if (Returned.Any())
            {
                var h3 = (HeaderSelector3.SelectedItem as ComboBoxItem)?.Content.ToString();
                sb.AppendLine(h3);
                sb.AppendLine();
                for (int i = 0; i < Returned.Count; i++)
                {
                    sb.AppendLine($"{3}.{i + 1}.\t{Returned[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Returned[i].Link)) sb.AppendLine($"\t{Returned[i].Link}");
                    if (!string.IsNullOrWhiteSpace(Returned[i].Reason)) sb.AppendLine($"\tПричина: {Returned[i].Reason}");
                    sb.AppendLine();
                }
            }

            Clipboard.SetText(sb.ToString().TrimEnd());

            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wishes.txt"); // Путь к файлу

            try
            {
                // Проверяем, существует ли файл, чтобы программа не вылетела
                if (File.Exists(filePath))
                {
                    // Читаем все строки из файла в массив
                    string[] wishes = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);

                    if (wishes.Length > 0)
                    {
                        var randomWish = wishes[new Random().Next(wishes.Length)];
                        MessageBox.Show($"Отчет скопирован в буфер обмена! \n\n{randomWish}", "Успех");
                    }
                }
                else
                {
                    MessageBox.Show("Отчет скопирован! (Файл с пожеланиями не найден)", "Успех");
                }
            }
            catch (Exception ex)
            {
                // На случай проблем с чтением файла
                MessageBox.Show("Ошибка при чтении пожеланий: " + ex.Message);
            }

            FireworksCanvas.Children.Clear();
        }

        private void StartFirework()
        {
            Random rnd = new Random();
            for (int i = 0; i < 20; i++)
            {
                var particle = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromRgb((byte)rnd.Next(100, 255), (byte)rnd.Next(100, 255), (byte)rnd.Next(100, 255))) };
                Canvas.SetLeft(particle, FireworksCanvas.ActualWidth / 2);
                Canvas.SetTop(particle, FireworksCanvas.ActualHeight - 50);
                FireworksCanvas.Children.Add(particle);

                double toX = rnd.NextDouble() * FireworksCanvas.ActualWidth;
                double toY = rnd.NextDouble() * (FireworksCanvas.ActualHeight / 2);

                var animX = new DoubleAnimation(toX, TimeSpan.FromSeconds(1));
                var animY = new DoubleAnimation(toY, TimeSpan.FromSeconds(1));
                var animOpacity = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1.2));

                particle.BeginAnimation(Canvas.LeftProperty, animX);
                particle.BeginAnimation(Canvas.TopProperty, animY);
                particle.BeginAnimation(OpacityProperty, animOpacity);
            }
        }

        private void ClearReport_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Сбросить все задачи и начать новый день?", "На абордаж!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Tasks.Clear();
                Fixed.Clear();
                Returned.Clear();
                SaveData();
                PlaySound("splash.mp3");

                DoubleAnimation shipAnim = new DoubleAnimation
                {
                    From = -60,
                    To = this.ActualWidth + 60,
                    Duration = TimeSpan.FromSeconds(3)
                };
                ShipEmoji.BeginAnimation(Canvas.LeftProperty, shipAnim);
            }
        }

        private void PlaySound(string fileName)
        {
            try
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (File.Exists(fullPath))
                {
                    _soundPlayer.Open(new Uri(fullPath, UriKind.Absolute));
                    _soundPlayer.Stop();
                    _soundPlayer.Play();
                }
            }
            catch { }
        }

        private void PlayDeckSound() => PlaySound("knock.mp3");
    }

    public class SaveModel
    {
        public ObservableCollection<ReportItem> T { get; set; }
        public ObservableCollection<ReportItem> F { get; set; }
        public ObservableCollection<ReportItem> R { get; set; }
    }

    public class ReportItem
    {
        public string Title { get; set; } = "";
        public string Link { get; set; } = "";
        public string Reason { get; set; } = "";
        public bool IsReturned { get; set; } = false;
    }
}