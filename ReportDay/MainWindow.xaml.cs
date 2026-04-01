using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes; // Для эллипсов салюта
using Newtonsoft.Json;
using System.IO;

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
            LoadData(); // Загружаем при старте
            DataContext = this;

            // Добавляем прослушивание клавиш для всего окна
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, зажат ли Ctrl и нажата ли клавиша S
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                SaveData(); // Вызываем твой метод сохранения

                // Опционально: можно показать короткое мигание или звук, 
                // чтобы понять, что сохранение прошло
                PlaySound("saveText.mp3");

                // Сообщаем системе, что мы обработали нажатие
                e.Handled = true;
            }
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
            catch { /* Игнорируем ошибки доступа к файлу */ }
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
                catch { /* Если файл поврежден - открываем чистым */ }
            }
        }

        // --- ЛОГИКА ТЕМ ---
        private void ChangeTheme_Click(object sender, RoutedEventArgs e)
        {
            var appRes = Application.Current.Resources.MergedDictionaries[0];
            var cc = new ColorConverter();

            if (isDarkTheme)
            {
                appRes["WindowBackground"] = new SolidColorBrush((Color)cc.ConvertFrom(LightBgColor));
                appRes["MainText"] = new SolidColorBrush((Color)cc.ConvertFrom(LightTextColor));
            }
            else
            {
                appRes["WindowBackground"] = new SolidColorBrush((Color)cc.ConvertFrom(DarkBgColor));
                appRes["MainText"] = new SolidColorBrush((Color)cc.ConvertFrom(DarkTextColor));
            }
            isDarkTheme = !isDarkTheme;
        }

        // --- ЛОГИКА ЗАГОЛОВКОВ ---
        private void Header1_Click(object sender, RoutedEventArgs e)
        {
            if (Header1.Content.ToString() == "1. Задачи") Header1.Content = "1. Сегодня";
            else if (Header1.Content.ToString() == "1. Сегодня") Header1.Content = "1. Созвоны";
            else Header1.Content = "1. Задачи";
        }

        private void Header2_Click(object sender, RoutedEventArgs e)
        {
            if (Header2.Content.ToString() == "2. Пофикшено") Header2.Content = "2. Сегодня";
            else Header2.Content = "2. Пофикшено";
        }

        private void Header3_Click(object sender, RoutedEventArgs e)
        {
            if (Header3.Content.ToString() == "3. Вернул в работу") Header3.Content = "3. Сегодня";
            else Header3.Content = "3. Вернул в работу";
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
            sb.AppendLine(); // Отступ после приветствия

            // --- СЕКЦИЯ 1 ---
            if (Tasks.Any())
            {
                sb.AppendLine(Header1.Content.ToString());
                sb.AppendLine(); // Пустая строка после заголовка секции
                for (int i = 0; i < Tasks.Count; i++)
                {
                    sb.AppendLine($"{i + 1}.{i + 1}.\t{Tasks[i].Title}"); // Номер с табом
                    if (!string.IsNullOrWhiteSpace(Tasks[i].Link))
                        sb.AppendLine($"\t{Tasks[i].Link}"); // Ссылка с табом

                    sb.AppendLine(); // Пустая строка между пунктами 1.1, 1.2 и т.д.
                }
            }

            // --- СЕКЦИЯ 2 ---
            if (Fixed.Any())
            {
                sb.AppendLine(Header2.Content.ToString());
                sb.AppendLine();
                for (int i = 0; i < Fixed.Count; i++)
                {
                    sb.AppendLine($"{2}.{i + 1}.\t{Fixed[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Fixed[i].Link))
                        sb.AppendLine($"\t{Fixed[i].Link}");

                    sb.AppendLine();
                }
            }

            // --- СЕКЦИЯ 3 ---
            if (Returned.Any())
            {
                sb.AppendLine(Header3.Content.ToString());
                sb.AppendLine();
                for (int i = 0; i < Returned.Count; i++)
                {
                    sb.AppendLine($"{3}.{i + 1}.\t{Returned[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Returned[i].Link))
                        sb.AppendLine($"\t{Returned[i].Link}");
                    if (!string.IsNullOrWhiteSpace(Returned[i].Reason))
                        sb.AppendLine($"\tПричина: {Returned[i].Reason}");

                    sb.AppendLine();
                }
            }

            Clipboard.SetText(sb.ToString().TrimEnd());

            string[] wishes = {
                "Продуктивного времени!", "Ты отлично справляешься!", "Магия в деталях.",
                "На Абордаж!! ихо хо хо хо сундук с сокровищами и бутылка рома",
                "твой отчёт - как произведение искусства!", "будь классным!",
                "Пусть код будет чистым!", "Яркого дня, как этот фейерверк!"
            };
            var randomWish = wishes[new Random().Next(wishes.Length)];

            MessageBox.Show($"Отчет скопирован в буфер обмена! \n\n{randomWish}", "Успех");
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

        // ОЧИСТКА
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
                    _soundPlayer.Stop(); // Сбрасываем, если звук уже играл
                    _soundPlayer.Play();
                }
            }
            catch { /* Тишина, если что-то пошло не так */ }
        }

        private void PlayDeckSound() => PlaySound("knock.mp3");
    }

    // Модели данных
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