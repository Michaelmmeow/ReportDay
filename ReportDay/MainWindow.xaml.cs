using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace ReportDay
{
    // Это основной класс твоего окна
    public partial class MainWindow : Window
    {
        private System.Windows.Media.MediaPlayer _soundPlayer = new System.Windows.Media.MediaPlayer();

        // Списки для каждой секции
        public ObservableCollection<ReportItem> Tasks { get; set; } = new ObservableCollection<ReportItem>();
        public ObservableCollection<ReportItem> Fixed { get; set; } = new ObservableCollection<ReportItem>();
        public ObservableCollection<ReportItem> Returned { get; set; } = new ObservableCollection<ReportItem>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // Привязываем данные к интерфейсу
        }

        // --- ЛОГИКА ДОБАВЛЕНИЯ ---

        private void AddTask() => Tasks.Add(new ReportItem());
        private void AddFixed() => Fixed.Add(new ReportItem());
        private void AddReturned() => Returned.Add(new ReportItem { IsReturned = true });

        // Событие нажатия Enter в полях ввода
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Смотрим, к какому списку принадлежит текстовое поле, в котором нажали Enter
                var textBox = sender as TextBox;
                var stackPanel = textBox?.Parent as StackPanel;

                // Если это нижняя секция "Вернул в работу"
                if (Returned.Any(x => x.Title == textBox.Text || x.Link == textBox.Text || x.Reason == textBox.Text))
                    AddReturned();
                // Если "Пофикшено"
                else if (Fixed.Any(x => x.Title == textBox.Text || x.Link == textBox.Text))
                    AddFixed();
                // По умолчанию — в задачи
                else
                    AddTask();
            }
        }

        // --- УДАЛЕНИЕ СТРОК ---
        private void RemoveTask_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ReportItem;

            // Пытаемся удалить из всех списков, где он может быть
            if (Tasks.Contains(item)) Tasks.Remove(item);
            else if (Fixed.Contains(item)) Fixed.Remove(item);
            else if (Returned.Contains(item)) Returned.Remove(item);
        }

        private void StartFirework()
        {
            Random rnd = new Random();
            // Создаем 20 "искорок"
            for (int i = 0; i < 20; i++)
            {
                var particle = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(
                        (byte)rnd.Next(100, 255), (byte)rnd.Next(100, 255), (byte)rnd.Next(100, 255)))
                };

                // Начальная позиция (центр кнопки или низ экрана)
                Canvas.SetLeft(particle, FireworksCanvas.ActualWidth / 2);
                Canvas.SetTop(particle, FireworksCanvas.ActualHeight - 50);
                FireworksCanvas.Children.Add(particle);

                // Анимация разлета
                double toX = rnd.NextDouble() * FireworksCanvas.ActualWidth;
                double toY = rnd.NextDouble() * (FireworksCanvas.ActualHeight / 2);

                var animX = new System.Windows.Media.Animation.DoubleAnimation(toX, TimeSpan.FromSeconds(1));
                var animY = new System.Windows.Media.Animation.DoubleAnimation(toY, TimeSpan.FromSeconds(1));
                var animOpacity = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromSeconds(1.2));

                particle.BeginAnimation(Canvas.LeftProperty, animX);
                particle.BeginAnimation(Canvas.TopProperty, animY);
                particle.BeginAnimation(OpacityProperty, animOpacity);
            }
        }

        // --- ГЕНЕРАЦИЯ И КОПИРОВАНИЕ ---
        private void CopyReport_Click(object sender, RoutedEventArgs e)
        {
            // 1. Звук створки сундука
            PlayDeckSound();

            // 2. Салют
            StartFirework();
            StringBuilder sb = new StringBuilder();

            // 0. Приветствие
            var greeting = (GreetingCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            sb.AppendLine($"{greeting}!");
            sb.AppendLine();

            // 1. Секция Задачи
            if (Tasks.Any())
            {
                sb.AppendLine("1. Задачи");
                for (int i = 0; i < Tasks.Count; i++)
                {
                    sb.AppendLine($"1.{i + 1}. {Tasks[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Tasks[i].Link)) sb.AppendLine(Tasks[i].Link);
                }
                sb.AppendLine();
            }

            // 2. Секция Пофикшено
            if (Fixed.Any())
            {
                sb.AppendLine("2. Пофикшено");
                for (int i = 0; i < Fixed.Count; i++)
                {
                    sb.AppendLine($"2.{i + 1}. {Fixed[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Fixed[i].Link)) sb.AppendLine(Fixed[i].Link);
                }
                sb.AppendLine();
            }

            // 3. Секция Вернул в работу
            if (Returned.Any())
            {
                sb.AppendLine("3. Вернул в работу");
                for (int i = 0; i < Returned.Count; i++)
                {
                    sb.AppendLine($"3.{i + 1}. {Returned[i].Title}");
                    if (!string.IsNullOrWhiteSpace(Returned[i].Link)) sb.AppendLine(Returned[i].Link);
                    sb.AppendLine($"Причина: {Returned[i].Reason}");
                }
            }


            Clipboard.SetText(sb.ToString().TrimEnd()); // Сначала копируем текст

            // Список твоих вдохновляющих фраз
            string[] wishes = {
        "Продуктивного времени!",
        "Ты отлично справляешься!",
        "Магия в деталях.",
        "Интуиция тебя не подвела!",
        "Отчёт готов, можно и отдохнуть.",
        "каждая строка - шаг к успеху!",
        "сегодняшний день крупица - счастливого долголетия",
        "твой отчёт - как произведение искусства!",
        "сев за работу, не забывай что ты творишь историю!",
        "твой отчёт - как путеводная звезда для команды!",
        "ты уникальный, и твой отчёт тоже!",
        "ты - мастер своего дела, и твой отчёт это подтверждает!",
        "ты - как волшебник, превращающий данные в успех!",
        "твой отчёт - как драгоценный камень, сияющий в команде!",
        "ты - как художник, рисующий картину успеха своим отчётом!",
        "получив отчёт, ты словно открываешь сундук с сокровищами для команды!",
        "капитан нарисовавший пунктир на сегодняшней карте(отчёте) - найдёт сокровище",
        "Леонардо Да Винчи тоже делал заметки на сегодняшний день",
        "На Абордаж!! ихо хо хо хо  сундук с сокровищами и бутылка рома",
        "сегодняшний день как игра - будь азартен",
    };

            // Выбираем случайную
            var randomWish = wishes[new Random().Next(wishes.Length)];

            // Показываем окно с этой фразой
            MessageBox.Show($"Отчет скопирован в буфер обмена! \n\n{randomWish}", "Успех");
            // Очищаем холст после салюта
            FireworksCanvas.Children.Clear();

        }

        // Обработчики кнопок "+" в интерфейсе
        private void AddTask_Click(object sender, RoutedEventArgs e) => AddTask();
        private void AddFixed_Click(object sender, RoutedEventArgs e) => AddFixed();
        private void AddReturned_Click(object sender, RoutedEventArgs e) => AddReturned();
        private void PlayDeckSound()
        {
            try
            {
                // Указываем путь к файлу (он будет лежать рядом с .exe)
                _soundPlayer.Open(new Uri(AppDomain.CurrentDomain.BaseDirectory + "knock.mp3"));
                _soundPlayer.Play();
            }
            catch { /* если звука нет, программа просто пойдет дальше */ }
        }
    }



    // ТА САМАЯ МОДЕЛЬ ДАННЫХ (можно оставить здесь внизу)
    public class ReportItem
    {
        public string Title { get; set; } = "";
        public string Link { get; set; } = "";
        public string Reason { get; set; } = "";
        public bool IsReturned { get; set; } = false; // Нужно для отображения поля причины
    }
}