using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BudgetCal2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private readonly UserPreferenceChangedEventHandler? UserPreferenceChanged;
        private static MainWindow? bcInstance;
        readonly DateContainer viewDate = new();
        private BCFile calFile = new();
        private BindingList<String> cals = new();
        public static MainWindow? BCInstance { get => bcInstance; set => bcInstance = value; }

        public MainWindow()
        {
            UserPreferenceChanged = new UserPreferenceChangedEventHandler(AccentBrush.SystemEvents_UserPreferenceChanged);
            SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
            BCInstance = this;
            viewDate.SelectedDate = DateTime.Now;
            //SQLite.DropTables();
            SQLite.CreateTables();
            InitializeComponent();
            CollapseAll();
            MakeCalGrid();

            labelDate.DataContext = viewDate;
            Background = new SolidColorBrush(AccentBrush.AccentColor.GetAccentColor());
        }
        public void BgUpdate(Brush brush)
        {
            Background = brush;
        }

        private void MakeCalGrid()
        {
            //clear
            gridCal.Children.Clear();
            gridCal.RowDefinitions.Clear();
            gridCal.RowDefinitions.Add(new() { Height = new GridLength(25) });
            //weekday labels
            DateTime temp = new();
            for (int i = 0; i < 7; i++)
            {
                Label w = new()
                {
                    Content = (temp.ToString("dddd")[0] + "").ToUpper() + temp.ToString("dddd").Remove(0, 1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0)
                };

                Grid.SetRow(w, 0);
                Grid.SetColumn(w, i);
                if (i == 5 || i == 6)
                {
                    w.Foreground = Brushes.LightPink;
                }
                else
                {
                    w.Foreground = Brushes.White;
                }
                gridCal.Children.Add(w);
                temp = temp.AddDays(1);
            }
            //build balance
            DateTime temp2 = new(DateTime.Now.Year, 1, 1);
            temp2 = temp2.AddDays(-1);
            int aa = calFile.Accounts == null ? 0 : calFile.Accounts.Count;
            List<double>[][] balancesY1 = new List<double>[aa][];
            for (int h = 0; h < 10; h++)
            {
                for (int d = 0; d < 366; d++)
                {

                    for (int i = 0; i < aa; i++)
                    {
                        if (balancesY1[i] == null)
                            balancesY1[i] = new List<double>[10];
                        if (balancesY1[i][h] == null)
                            balancesY1[i][h] = new();

                        //find all transactions that match the current account and day, sum the amounts
                        if (calFile.Transactions != null)
                            foreach (var t in calFile.Transactions)
                            {
                                t.Repeat ??= new RepeatContainer() { Type = "Once", Occurences = new BindingList<DateTime>() { DateTime.Now } };
                            }
                        double trans = calFile.Transactions.ToImmutableList().FindAll(t => t.Account == calFile.Accounts.ElementAt(i).Id).FindAll(t => t.Repeat.Occurences.Contains(temp2)).Sum(t => t.Amount);
                        if (h == 0)
                        {
                            balancesY1[i][h].Add(d == 0 ? calFile.Accounts.ElementAt(i).Balance : balancesY1[i][h].ElementAt(d - 1) + trans);
                        }
                        else
                        {
                            balancesY1[i][h].Add(d == 0 ? (balancesY1[i][h - 1].ElementAt(balancesY1[i][h - 1].Count - 1) + trans) : balancesY1[i][h].ElementAt(d - 1) + trans);
                        }
                    }
                    temp2 = temp2.AddDays(1);
                }
            }
            // build stats line

            try
            {
                if (comboAcc.SelectedIndex != comboAcc.Items.Count - 1)
                {
                    var Points = new Point[366];
                    double mx = balancesY1[comboAcc.SelectedIndex][viewDate.SelectedDate.Year - DateTime.Now.Year].Max();
                    double mn = balancesY1[comboAcc.SelectedIndex][viewDate.SelectedDate.Year - DateTime.Now.Year].Min();
                    double max = mx > 0 - mn ? mx : 0 - mn;
                    labelStatMax.Content = max.ToString("C", CultureInfo.CurrentCulture);
                    labelStatHalfMax.Content = (max / 2).ToString("C", CultureInfo.CurrentCulture);
                    labelStatMin.Content = (0 - max).ToString("C", CultureInfo.CurrentCulture);
                    labelStatHalfMin.Content = (0 - (max / 2)).ToString("C", CultureInfo.CurrentCulture);
                    double adj = max / 400;
                    for (int i = 0; i < Points.Length; i++)
                    {
                        Points[i] = new Point { X = i * 3, Y = balancesY1[comboAcc.SelectedIndex][viewDate.SelectedDate.Year - DateTime.Now.Year].ElementAt(i) / adj };//stat chart points
                    }
                    DataContext = new ViewModel
                    {
                        Segments = new List<Segment>(Points.Zip(Points.Skip(1), (a, b) => new Segment { From = a, To = b }))
                    };
                }
                else//all lines
                {
                    var Points = new Point[366 * balancesY1.Length];
                    double maxAll = 0;
                    for (int act = 0; act < balancesY1.Length; act++)
                    {
                        double mx = balancesY1[act][viewDate.SelectedDate.Year - DateTime.Now.Year].Max();
                        double mn = balancesY1[act][viewDate.SelectedDate.Year - DateTime.Now.Year].Min();
                        double max = mx > 0 - mn ? mx : 0 - mn;
                        if (max > maxAll)
                            maxAll = max;
                    }
                    for (int act = 0; act < balancesY1.Length; act++)
                    {
                        double adj = maxAll / 400;
                        for (int i = 0; i < 366; i++)
                        {
                            Points[i + (act * 366)] = new Point { X = i * 3, Y = balancesY1[act][viewDate.SelectedDate.Year - DateTime.Now.Year].ElementAt(i) / adj };//stat chart points
                        }
                        labelStatMax.Content = maxAll.ToString("C", CultureInfo.CurrentCulture);
                        labelStatHalfMax.Content = (maxAll / 2).ToString("C", CultureInfo.CurrentCulture);
                        labelStatMin.Content = (0 - maxAll).ToString("C", CultureInfo.CurrentCulture);
                        labelStatHalfMin.Content = (0 - (maxAll / 2)).ToString("C", CultureInfo.CurrentCulture);


                    }
                    IList<Segment> segments = new List<Segment>(Points.Zip(Points.Skip(1), (a, b) => new Segment { From = a, To = b }));
                    for (int act = balancesY1.Length - 1; act > 0; act--)
                    {
                        segments.ElementAt(act * 366 - 1).To = new Point { X = 0, Y = 0 };
                        segments.ElementAt(act * 366 - 1).From = new Point { X = 0, Y = 0 };
                    }
                    DataContext = new ViewModel() { Segments = segments };
                }
            }
            catch (Exception) { }
            //day cells
            temp = new DateTime(viewDate.SelectedDate.Year, viewDate.SelectedDate.Month, 1);
            int dayOffset = ((int)temp.DayOfWeek + 6) % 7;
            temp = new(DateTime.Now.Year, viewDate.SelectedDate.Month, 1);

            for (int i = 0; i < DateTime.DaysInMonth(viewDate.SelectedDate.Year, viewDate.SelectedDate.Month); i++)
            {
                if (((i + dayOffset) / 7) + 1 > gridCal.RowDefinitions.Count - 1)
                    gridCal.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                ScrollViewer scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                StackPanel spv = new() { Background = new SolidColorBrush(Color.FromArgb(31, 0, 0, 0)), Margin = new Thickness(1) };
                StackPanel sph = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(1) };
                Label w = new() { FontSize = 14, Content = i + 1, VerticalAlignment = VerticalAlignment.Top, Foreground = Brushes.White };
                string vContent = "";
                try
                {
                    vContent += balancesY1[comboAcc.SelectedIndex][viewDate.SelectedDate.Year - DateTime.Now.Year].ElementAt(temp.DayOfYear) + "";//set array for choosing account and year here

                }
                catch (Exception)
                {
                    try
                    {
                        double all = 0;
                        for (int g = 0; g < balancesY1.Length; g++)
                        {
                            all += balancesY1[g][viewDate.SelectedDate.Year - DateTime.Now.Year].ElementAt(temp.DayOfYear);
                        }
                        vContent += all;
                    }
                    catch (Exception) { }
                }

                if (vContent.Length < 1)
                    vContent += "0";
                temp = temp.AddDays(1);
                Label v = new()
                {
                    FontSize = 10,
                    Content = vContent,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = double.Parse(vContent) >= 0 ? Brushes.LightGreen : Brushes.LightPink
                };
                v.Content = double.Parse(vContent).ToString("C", CultureInfo.CurrentCulture);
                w.Padding = new Thickness(2);
                w.Margin = new Thickness(0);
                v.Padding = new Thickness(2);
                v.Margin = new Thickness(0);
                Grid.SetRow(scroll, ((i + dayOffset) / 7) + 1);
                Grid.SetColumn(scroll, (i + dayOffset) % 7);
                Grid.SetRow(v, ((i + dayOffset) / 7) + 1);
                Grid.SetColumn(v, (i + dayOffset) % 7);
                if ((i + dayOffset) % 7 == 5 || (i + dayOffset) % 7 == 6)
                    w.Foreground = Brushes.LightPink;
                sph.Children.Add(w);
                gridCal.Children.Add(v);
                spv.Children.Add(sph);
                scroll.Content = spv;
                gridCal.Children.Add(scroll);
                if (calFile.Transactions != null)//add trascations to current day
                    foreach (Transaction tr in calFile.Transactions)
                    {
                        foreach (var item in tr.Repeat.Occurences)
                        {
                            if (viewDate.SelectedDate.Year == item.Year && viewDate.SelectedDate.Month == item.Month && i + 1 == item.Day)
                            {
                                scroll = new ScrollViewer() { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
                                StackPanel sph2 = new()
                                {
                                    Orientation = Orientation.Horizontal,
                                    Margin = new Thickness(1),
                                    Background = new SolidColorBrush(Color.FromArgb(31, 0, 0, 0))
                                };
                                if ((comboAcc.SelectedItem as Account) != null)
                                    sph2.Background = tr.Account == (comboAcc.SelectedItem as Account).Id ? new SolidColorBrush(Color.FromArgb(127, 0, 0, 0)) : new SolidColorBrush(Color.FromArgb(191, 127, 127, 127));
                                Label amountLabel = new()
                                {
                                    Content = tr.Amount.ToString("C", CultureInfo.CurrentCulture),
                                    FontSize = 10,
                                    Padding = new Thickness(1),
                                    Margin = new Thickness(1),
                                    Foreground = tr.Amount >= 0 ? Brushes.LightGreen : Brushes.LightPink
                                };
                                sph2.Children.Add(amountLabel);
                                sph2.Children.Add(new Label { Content = tr.Name, FontSize = 10, Padding = new Thickness(2), Margin = new Thickness(0), Foreground = Brushes.White });
                                scroll.Content = sph2;
                                spv.Children.Add(scroll);
                            }
                        }
                    }
            }
        }
        private void ResetButtonBG()
        {
            btAbout.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
            btAcc.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
            btCal.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
            btFile.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
            btNew.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
            btStats.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
            btTrans.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
        }
        private void Button_Click_New(object sender, RoutedEventArgs e)
        {
            ResetButtonBG();
            btNew.Background = new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));
            labelNewFileStatus.Content = "";
            CollapseAll();
            panelNewFile.Visibility = Visibility.Visible;
        }

        private void CollapseAll()
        {
            gridCal.Visibility = Visibility.Collapsed;
            panelNewFile.Visibility = Visibility.Collapsed;
            panelOpenFile.Visibility = Visibility.Collapsed;
            panelAbout.Visibility = Visibility.Collapsed;
            panelAccounts.Visibility = Visibility.Collapsed;
            listAccounts.Visibility = Visibility.Collapsed;
            panelTransactions.Visibility = Visibility.Collapsed;
            listTransactions.Visibility = Visibility.Collapsed;
            gridCal.Visibility = Visibility.Collapsed;
            panelRepeat.Visibility = Visibility.Collapsed;
            selectAcc.Visibility = Visibility.Collapsed;
            panelStat.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_NewOK(object sender, RoutedEventArgs e)
        {
            if (textboxNewFileName.Text.Length > 0)
            {
                if (!SQLite.FileExists(textboxNewFileName.Text))
                {
                    calFile = new()
                    {
                        Name = textboxNewFileName.Text,
                        Accounts = new(),
                        Transactions = new()
                    };
                    calFile.Accounts.Add(new Account() { Name = "Debit", Balance = 0 });
                    calFile = SQLite.Update(calFile);
                    CalFileChanged();
                    labelNewFileStatus.Content = "File created.";
                }
                else
                {
                    labelNewFileStatus.Content = "File already exists";
                }
                textboxNewFileName.Text = "";
            }
            else
            {
                labelNewFileStatus.Content = "File name cannot be empty";
            }
        }

        private void CalFileChanged()
        {
            calFile.PropertyChanged += CalFile_PropertyChanged;
            if (calFile.Transactions != null)
                calFile.Transactions.ListChanged += CalFile_PropertyChanged;
            if (calFile.Accounts != null)
                calFile.Accounts.ListChanged += CalFile_PropertyChanged;
            listAccounts.ItemsSource = calFile.Accounts;
            comboAcc.ItemsSource = calFile.Accounts;
            comboAcc.SelectedIndex = 0;
            AccountIDreadOnly();
            listTransactions.ItemsSource = calFile.Transactions;
            HideTransactionID();
        }

        private void Button_Click_PrevYear(object sender, RoutedEventArgs e)
        {
            if (viewDate.SelectedDate.Year > DateTime.Now.Year)
                viewDate.SelectedDate = viewDate.SelectedDate.AddYears(-1);
            MakeCalGrid();
        }
        private void Button_Click_PrevMonth(object sender, RoutedEventArgs e)
        {
            if (viewDate.SelectedDate.AddMonths(-1).Year >= DateTime.Now.Year)
                viewDate.SelectedDate = viewDate.SelectedDate.AddMonths(-1);
            MakeCalGrid();
        }
        private void Button_Click_NextMonth(object sender, RoutedEventArgs e)
        {
            if (viewDate.SelectedDate.AddMonths(1).Year <= DateTime.Now.AddYears(9).Year)
                viewDate.SelectedDate = viewDate.SelectedDate.AddMonths(1);
            MakeCalGrid();
        }
        private void Button_Click_NextYear(object sender, RoutedEventArgs e)
        {
            if (viewDate.SelectedDate.Year < DateTime.Now.AddYears(9).Year)
                viewDate.SelectedDate = viewDate.SelectedDate.AddYears(1);
            MakeCalGrid();
        }

        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            if (calFile.Name != null)
            {
                btSave.Background = new SolidColorBrush(Color.FromArgb(31, 255, 255, 255));
                SQLite.Update(calFile);
                calFile = SQLite.GetCal(calFile.Name);
                CalFileChanged();
            }
        }

        private void Button_Click_Open(object sender, RoutedEventArgs e)
        {
            ResetButtonBG();
            btFile.Background = new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));
            CollapseAll();
            panelOpenFile.Visibility = Visibility.Visible;
            labelOpenFileStatus.Content = "";
            cals = SQLite.GetCalsNames();
            listboxOpenFileName.ItemsSource = cals;
            if (cals.Count == 0) labelOpenFileStatus.Content = "No files found.";
        }

        private void Button_Click_About(object sender, RoutedEventArgs e)
        {
            ResetButtonBG();
            btAbout.Background = new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));
            CollapseAll();
            panelAbout.Visibility = Visibility.Visible;
        }

        private void Button_Click_Accounts(object sender, RoutedEventArgs e)
        {
            ResetButtonBG();
            btAcc.Background = new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));
            CalFileChanged();
            CollapseAll();
            panelAccounts.Visibility = Visibility.Visible;
            listAccounts.Visibility = Visibility.Visible;
            AccountIDreadOnly();
        }
        private void Button_Click_Transactions(object sender, RoutedEventArgs e)
        {
            ResetButtonBG();
            btTrans.Background = new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));
            CollapseAll();
            panelTransactions.Visibility = Visibility.Visible;
            listTransactions.Visibility = Visibility.Visible;
            listTransactions.Opacity = 1;
            HideTransactionID();
        }

        private void Button_Click_OpenOk(object sender, RoutedEventArgs e)
        {
            if (listboxOpenFileName.SelectedValue != null)
            {
                calFile = SQLite.GetCal(listboxOpenFileName.SelectedValue.ToString());
                CalFileChanged();
                labelOpenFileStatus.Content = "Loaded " + calFile.Name;
            }
        }

        private void CalFile_PropertyChanged(object? sender, EventArgs e)
        {
            btSave.Background = new SolidColorBrush(Color.FromArgb(63, 0, 0, 0));
        }

        private void Button_Click_OpenDelete(object sender, RoutedEventArgs e)
        {
            if (listboxOpenFileName.SelectedValue != null)
            {
                MessageBoxResult a = MessageBox.Show("Are you sure you want to delete " + listboxOpenFileName.SelectedValue.ToString(), "Confirming...", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (a == MessageBoxResult.Yes)
                {
                    SQLite.DelCal(listboxOpenFileName.SelectedValue.ToString());
                    cals.Remove(listboxOpenFileName.SelectedValue.ToString() + "");
                }
            }
        }

        public void AccountIDreadOnly()
        {
            if (listAccounts.Columns.Count >= 1)
            {
                listAccounts.Columns[0].IsReadOnly = true;
                Style readOnly = new() { TargetType = typeof(DataGridCell) };
                readOnly.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromArgb(63, 255, 255, 255))));
                readOnly.Setters.Add(new Setter(PaddingProperty, new Thickness(5)));
                readOnly.Setters.Add(new Setter(MarginProperty, new Thickness(1)));
                readOnly.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
                listAccounts.Columns[0].CellStyle = readOnly;
                listAccounts.Columns[2].Header = "Balance on jan 1st of this year";
            }
        }

        private void ListAccounts_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AccountIDreadOnly();
        }

        private void ListAccounts_IsVisibleChanged(object sender, EventArgs e)
        {
            AccountIDreadOnly();
        }
        public void HideTransactionID()
        {
            if (listTransactions.Columns.Count >= 7)
            {
                listTransactions.Columns[0].Visibility = Visibility.Hidden;
                listTransactions.Columns[5].Visibility = Visibility.Hidden;
                listTransactions.Columns[7].Header = "Account ID #";
            }
        }

        private void ListTransactions_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            HideTransactionID();
        }

        private void ListTransactions_IsVisibleChanged(object sender, EventArgs e)
        {
            HideTransactionID();
        }

        private void Button_Click_Calendar(object sender, RoutedEventArgs e)
        {
            ResetButtonBG();
            btCal.Background = new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));
            CalFileChanged();
            CollapseAll();
            MakeCalGrid();
            gridCal.Visibility = Visibility.Visible;
            selectAcc.Visibility = Visibility.Visible;
        }
        private static DataGridRow? row;
        private void ListTransactions_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            panelRepeat.Visibility = Visibility.Collapsed;
            listTransactions.Opacity = 1;
            if (e.Column.DisplayIndex == 6)
            {
                row = e.Row;
                listTransactions.Opacity = 0.5;
                panelRepeat.Visibility = Visibility.Visible;
            }
        }

        private void Button_Click_RepeatOK(object sender, RoutedEventArgs e)
        {
            RepeatContainer a = new();
            if (repeatStart.SelectedDate != null)
            {
                int occ = 0;
                DateTime tracker = repeatStart.SelectedDate.Value;
                a.Type = repeatType.Text;
                if (!repeatType.Text.Contains("Once"))
                {
                    if (repeatEnd.SelectedDate != null)
                        while (tracker <= repeatEnd.SelectedDate.Value)
                        {
                            if (occ < int.Parse(repeatMax.Text) || int.Parse(repeatMax.Text) == 0)
                            {
                                a.Occurences.Add(tracker);
                                switch (repeatType.Text)
                                {
                                    case "Every Week":
                                    tracker = tracker.AddDays(7);
                                    break;
                                    case "Every 2 Weeks":
                                    tracker = tracker.AddDays(14);
                                    break;
                                    case "Every Month":
                                    tracker = tracker.AddMonths(1);
                                    break;
                                    case "Every 3 Months":
                                    tracker = tracker.AddMonths(3);
                                    break;
                                    case "Every Year":
                                    tracker = tracker.AddYears(1);
                                    break;
                                    default:
                                    break;
                                }
                                occ++;
                            }
                            if (occ == int.Parse(repeatMax.Text))
                                break;
                        }
                }
                else
                {
                    try
                    {
                        a.Occurences.Add(repeatStart.SelectedDate.Value);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            if (row != null)
                if ((row.Item as Transaction) != null)
                {
                    (row.Item as Transaction).Repeat = a;
                    repeatEnd.SelectedDate = null;
                    repeatStart.SelectedDate = null;
                    repeatMax.Text = "0";
                    //repeatType.SelectedIndex = 5;
                }

            panelRepeat.Visibility = Visibility.Collapsed;
            listTransactions.Opacity = 1;
        }

        private void ComboAcc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MakeCalGrid();
        }

        public class Segment
        {
            public Point From { get; set; }
            public Point To { get; set; }
        }

        public class ViewModel
        {
            public IList<Segment> Segments { get; set; }
        }
        private void BtStats_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll();
            MakeCalGrid();
            ResetButtonBG();
            btStats.Background = new SolidColorBrush(Color.FromArgb(127, 0, 0, 0));
            selectAcc.Visibility = Visibility.Visible;
            panelStat.Visibility = Visibility.Visible;
        }
    }
}
