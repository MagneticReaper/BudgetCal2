using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BudgetCal2
{
    class BCFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private string? name;
        private BindingList<Account>? accounts;
        private BindingList<Transaction>? transactions;
        public string? Name { get => name; set { name = value; OnPropertyChanged(); } }
        public BindingList<Account>? Accounts { get => accounts; set { accounts = value; OnPropertyChanged(); } }
        public BindingList<Transaction>? Transactions { get => transactions; set { transactions = value; OnPropertyChanged(); } }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    class Account : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private int id;
        private string name = "";
        private double balance;
        private string description = "";
        public int Id { get => id; set { id = value; OnPropertyChanged(); } }
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }
        public double Balance { get => balance; set { balance = value; OnPropertyChanged(); } }
        public string Description { get => description; set { description = value; OnPropertyChanged(); } }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    class Transaction : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private int id;
        private string? name;
        private string? description;
        private string? category;
        private double amount;
        private string? repeat;
        private int account;

        public Transaction() { repeat = JsonSerializer.Serialize(new RepeatContainer() { Type = "Once", Occurences = new BindingList<DateTime>() { DateTime.Now } }); }
        public int Id { get => id; set { id = value; OnPropertyChanged(); } }
        public string? Name { get => name; set { name = value; OnPropertyChanged(); } }
        public string? Description { get => description; set { description = value; OnPropertyChanged(); } }
        public string? Category { get => category; set { category = value; OnPropertyChanged(); } }
        public double Amount { get => amount; set { amount = value; OnPropertyChanged(); } }
        public string? RepeatString { get => repeat; set { repeat = value; OnPropertyChanged(); } }
        public RepeatContainer? Repeat
        {
            get
            {
                if (repeat != null)
                    if (repeat.Length > 0)
                        return JsonSerializer.Deserialize<RepeatContainer>(repeat);
                return null;
            }
            set
            {

                repeat = JsonSerializer.Serialize(value);
                OnPropertyChanged();
            }
        }
        public int Account { get => account; set { account = value; OnPropertyChanged(); } }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    class DateContainer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private DateTime firstDay;
        private DateTime selectedDate;
        public DateTime SelectedDate { get => selectedDate; set { selectedDate = value; firstDay = value.AddDays(1 - value.Day); OnPropertyChanged(); } }
        public DateTime FirstDay { get => firstDay; }
        public override string ToString()
        {
            return SelectedDate.ToString("MMMM yyy");
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    class RepeatContainer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private BindingList<DateTime> occurences = new();
        private string? type;
        public string? Type { get => type; set { type = value; OnPropertyChanged(); } }
        public BindingList<DateTime> Occurences { get => occurences; set { occurences = value; OnPropertyChanged(); } }
        public override string ToString()
        {
            return type + "; " + occurences.Count + " events";
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
