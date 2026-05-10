using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

            string[] wishes = {

            "Проснись, Самурай, этот отчёт сам себя не доставит!",
            "Твой код — чистый хром, а баги — всего лишь программный мусор.",
            "В Найт-Сити легендами становятся после смерти, но ты стань легендой при жизни.",
            "Твой имплант продуктивности сегодня работает на 200%.",
            "Никаких компромиссов, даже если на хвосте вся Арасака.",
            "Стиль важнее субстанции, но твой отчёт хорош в обоих смыслах.",
            "Ты взломал этот день быстрее, чем Ти-Баг протоколы защиты.",
            "Жизнь на грани — это не баг, это фича твоего драйва.",
            "Твой бэклог выжжен так же чисто, как Серебряная Рука сжёг Микоши.",
            "Мир принадлежит тем, кто умеет переписывать правила.",
            "Твоя эффективность сегодня — это настоящий Сандевистан.",
            "Даже в самом тёмном киберпространстве ты сияешь как неон.",
            "Лёд (ICE) взломан, доступ к успеху подтверждён.",
            "Твой потенциал безграничен, как старый интернет за Чёрным Заслоном.",
            "Будь осторожен, чумба, этот отчёт слишком горяч для обычных серверов.",

            //☣ Resident Evil 4: Выживание(Leon, Ada, Ashley)
            "Где все? На созвоне? Надеюсь, не ушли на бинго...",
            "Леон, ты сделал это! Теперь прыгай в гидроцикл, пора отдыхать.",
            "Отчёт готов. Твой ранг за сегодня: S+.",
            "Ада всегда на шаг впереди, но сегодня ты её догнал.",
            "Эшли в безопасности, задачи выполнены. Ты — настоящий агент.",
            "Твои правки точны, как выстрел Леона в глаз гиганту.",
            "Не позволяй багам залезть тебе в голову, как Лас-Плагас.",
            "Инвентарь идеально упорядочен. Тетрис-мастер!",
            "Торговец бы сказал: 'Strangah... strangah... now THAT is a report!'",
            "Даже Саддлер не смог бы остановить твой прогресс сегодня.",
            "Ада прислала подарок: этот отчёт скопирован идеально.",
            "Твой код крепче, чем причёска Леона в самый разгар боя.",
            "Ты выжил в этом спринте без единой красной травы.",
            "Внимание к деталям — твой ключ, открывающий любые двери с эмблемами.",
            "Никаких 'YOU ARE DEAD'. Только полная победа.",

            //🧙‍♀️ Little Witch in the Woods: Уютная магия(Ellie)
            "Немного волшебной пыльцы, капля старания — и отчёт готов!",
            "Элли гордилась бы твоим мастерством зельеварения в коде.",
            "Пусть твой день будет уютным, как домик ведьмы в лесу.",
            "Ты нашёл редкий ингредиент успеха!",
            "Магия — это просто технология, которую мы ещё не поняли. Ты — маг.",
            "Даже маленькая ведьма может совершить большие дела.",
            "Твои задачи разлетелись, как листья на ветру после заклинания.",
            "Свари себе кофе, добавь щепотку радости и наслаждайся результатом.",
            "Твой отчёт пахнет лесными цветами и свежими идеями.",
            "Пусть твоя метла всегда летит в нужную сторону.",

            //🎯 Fortnite: Королевская битва(Victory Royale)
            "Топ-1! Твой отчёт — это Victory Royale сегодняшнего дня.",
            "Ты застроился так быстро, что дедлайны не смогли тебя достать.",
            "Лут собран, задачи закрыты, зона не поджала. Красава!",
            "Твой скилл растёт быстрее, чем уровень в новом сезоне.",
            "Никакого тильта, только уверенный пуш к цели.",
            "Ты выдал 200 в голову всем проблемам.",
            "Твой код надёжен, как металлическая постройка в финальном круге.",
            "Прыгай из боевого автобуса прямо в поток продуктивности!",
            "Твои идеи — это легендарный золотой дроп.",
            "Эмоция победы разрешена! Ты это заслужил.",


            //✨ Воодушевление и Пасхалки
            "Всё, что тебе нужно — это вера, капля удачи и кнопка Copy.",
            "Ты не один. С тобой весь твой опыт и верный кот (или как минимум я).",
            "Сделай паузу. Даже Леон отдыхает между схватками с боссами.",
            "Твой успех неизбежен, как титры в конце игры.",
            "Ты прошёл проверку на прочность. Уровень повышен!",
            "Помни: ты — Избранный. По крайней мере, для этого текстового поля.",
            "Твой отчёт — это письмо из Хогвартса, которое наконец дошло.",
            "Ты силён, как Кратос, и быстр, как Соник.",
            "Никогда не сдавайся. Даже если у тебя остался 1 HP.",
            "Ты — сокровище, которое не нужно искать на карте.",

            //🌀 Кибер - Магия и Атмосфера
            "Твои мысли текут быстрее оптоволокна.",
            "Ты — призрак в доспехах, штурмующий реальность.",
            "Пусть твой пинг в жизни будет минимальным, а радость — максимальной.",
            "Синхронизация завершена. Ты в гармонии с миром.",
            "Твой код поёт песню будущего.",
            "Ты взломал систему скуки!",
            "Твоя энергия заряжает всё вокруг, как реактор будущего.",
            "Ты — неоновый луч в сером небе города.",
            "Магия внутри тебя сильнее любых скриптов.",
            "Добро пожаловать на следующий уровень бытия.",

            //🔥 Финальный рывок(The End Game)
            "Отчёт готов. Миссия выполнена. Уважение +100.",
            "Ты — легенда, о которой будут писать в патчноутах истории.",
            "Твой труд — это искусство, скрытое в цифрах.",
            "Ты победил финального босса недели!",
            "Время сохраняться и наслаждаться моментом.",
            "Твой потенциал — это God Mode, активированный навсегда.",
            "Ты сделал этот мир ярче на несколько мегабайт.",
            "Код написан, отчёт скопирован, ты — великолепен.",
            "Помни: ты потрясающий! (Да-да, это отсылка к Киану).",
            "На абордаж следующей задачи! Но сначала — отдых, Капитан!",

            //🧠 Ghost in the Shell (Призрак в доспехах)
            "Твой Призрак шепчет, что этот отчёт безупречен.",
            "В океане данных ты — самый совершенный алгоритм.",
            "Твоя кибер-душа сегодня синхронизирована с успехом на 100%.",
            "Границы между человеком и машиной стираются, когда ты начинаешь кодить.",
            "Твой интеллект взломал защитный периметр этой недели.",
            "Сеть обширна и безгранична, как и твои возможности.",
            "Ты — не просто часть системы, ты её архитектор.",
            "Пусть твой ghost-сигнал всегда остается чистым от помех и багов.",
            "Синхронизация завершена. Погружение в поток продуктивности выполнено.",
            "Даже в искусственном теле твой творческий дух абсолютно реален.",

            //🌐 Serial Experiments Lain (Эксперименты Лэйн)
            "И зачем тебе тело, если ты уже в Сети? Отчёт успешно оцифрован.",
            "Граница между реальным миром и Wired стерта. Ты везде одновременно.",
            "Нет смысла помнить то, что не было записано в логах. Но этот отчёт записан.",
            "Люди существуют только благодаря памяти других. Сеть помнит твой успех.",
            "Present Day, Present Time! Ха-ха-ха! Ты вовремя.",
            "Твой Призрак обретает форму в бесконечном потоке данных.",
            "Ты — это информация. Информация должна распространяться. Копирование завершено.",
            "Не важно, где ты находишься. В Сети ты всегда рядом с целью.",
            "Твой разум подключен напрямую к протоколу седьмого поколения.",
            "Пророчество сбылось: сегодня ты самый эффективный узел системы.",
            "Всё связано. Каждая строчка твоего кода меняет структуру реальности.",
            "Ты слышишь гул проводов? Это шум твоей продуктивности.",
            "Лэйн одобряет твой доступ. Ты — часть Wired.",
            "Реальность — это просто серия электрических импульсов. Ты ими управляешь.",
            "Закрой глаза... и ты увидишь, как данные превращаются в твой идеальный отчёт."

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