using System;
using System.ComponentModel;
using System.Data.SQLite;
using System.Windows;

namespace BudgetCal2
{
    class SQLite
    {
        public static void CreateTables()//for first time running if there's no database file
        {
            try
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "CREATE TABLE accounts (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, balance REAL, description TEXT, fileID TEXT); " +
                    "CREATE TABLE bcf (name TEXT, account INTEGER, FOREIGN KEY (account) REFERENCES accounts(id), PRIMARY KEY (name, account) ); " +
                    "CREATE TABLE transactions (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, description TEXT, category TEXT, amount REAL, repeat TEXT, account INTEGER, fileID TEXT, FOREIGN KEY (account) REFERENCES accounts(id) ); ";
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception) { }
        }

        public static void DropTables()//for testing purposes. enable in main window constructor if needed
        {
            try
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "DROP TABLE accounts; DROP TABLE transactions; DROP TABLE bcf; ";
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception) { }
        }

        internal static bool FileExists(string? name)//check if filename exists
        {
            bool has = false;
            try
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "SELECT * FROM bcf WHERE name=('" + name + "');";
                con.Open();
                var r = cmd.ExecuteReader();
                has = r.HasRows;
                con.Close();
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
            return has;
        }

        internal static BindingList<string> GetCalsNames()// returns a list of file names
        {
            try
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "select * from bcf group by name;";
                BindingList<string> names = new();
                con.Open();
                var res = cmd.ExecuteReader();
                while (res.Read()) { names.Add(res.GetString(0)); }
                con.Close();
                return names;
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
            return new BindingList<string>();
        }

        internal static BCFile Update(BCFile calFile)
        {
            if (calFile.Accounts != null)
            {
                if (calFile.Transactions != null)
                    if (calFile.Transactions.Count > 0)
                        try//delete transactions
                        {
                            SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                            SQLiteCommand cmd = con.CreateCommand();
                            cmd.CommandText = "delete from transactions where fileID = '" + calFile.Name + "'";
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                        catch (Exception e) { MessageBox.Show(e.Message + " :update/delTransact"); }
                try//delete bfc
                {
                    SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                    SQLiteCommand cmd = con.CreateCommand();
                    cmd.CommandText = "delete from bcf where name = '" + calFile.Name + "'";
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
                catch (Exception e) { MessageBox.Show(e.Message); }
                try//delete accounts
                {
                    SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                    SQLiteCommand cmd = con.CreateCommand();
                    cmd.CommandText = "delete from accounts where fileID = '" + calFile.Name + "'";
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
                catch (Exception e) { MessageBox.Show(e.Message); }
                try//insert accounts
                {
                    SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                    SQLiteCommand cmd = con.CreateCommand();
                    foreach (var a in calFile.Accounts)
                    {
                        if (a.Id == 0)
                        {
                            cmd.CommandText += "insert into accounts (name, balance, description, fileID) values ('" + a.Name + "', " + a.Balance + ", '" + a.Description + "','" + calFile.Name + "');";
                        }
                        else
                        {
                            cmd.CommandText += "insert into accounts (id, name, balance, description, fileID) values (" + a.Id + ", '" + a.Name + "', " + a.Balance + ", '" + a.Description + "','" + calFile.Name + "');";
                        }
                    }
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
                catch (Exception e) { MessageBox.Show(e.Message); }
                try//insert bfc
                {
                    SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                    SQLiteCommand cmd = con.CreateCommand();
                    foreach (var a in calFile.Accounts)
                    {
                        cmd.CommandText += "insert into bcf (name, account) values ('" + calFile.Name + "', " + a.Id + ");";
                    }
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
                catch (Exception e) { MessageBox.Show(e.Message); }
                if (calFile.Transactions != null)
                    if (calFile.Transactions.Count > 0)
                        try//insert transactions
                        {
                            SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                            SQLiteCommand cmd = con.CreateCommand();
                            foreach (var b in calFile.Transactions)
                            {
                                cmd.CommandText += "insert into transactions (name, description, category, amount, repeat, account, fileID) values ('" + b.Name + "', '" + b.Description + "', '" + b.Category + "',  " + b.Amount + ", '" + b.RepeatString + "', '" + b.Account + "', '" + calFile.Name + "');";
                            }
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                        catch (Exception e) { MessageBox.Show(e.Message + " :update/insertTransact"); }
            }
            return GetCal(calFile.Name);
        }

        internal static BCFile GetCal(string? name)
        {
            BCFile bcf = new() { Name = name, Accounts = new(), Transactions = new() };
            try//get accounts
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "SELECT * FROM accounts WHERE fileID='" + name + "';";
                con.Open();
                var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    if (r.GetString(4).Equals(name))
                        bcf.Accounts.Add(new Account() { Id = r.GetInt32(0), Name = r.GetString(1), Balance = r.GetDouble(2), Description = r.GetString(3) });
                }
                con.Close();
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
            try//get transactions
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "SELECT * FROM transactions WHERE fileID='" + name + "';";
                con.Open();
                var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    if (r.GetString(7).Equals(name))
                    {
                        bcf.Transactions.Add(new Transaction() { Id = r.GetInt32(0), Name = r.GetString(1), Description = r.GetString(2), Category = r.GetString(3), Amount = r.GetDouble(4), RepeatString = r.GetString(5), Account = r.GetInt32(6) });
                    }
                }
                con.Close();
            }
            catch (Exception e) { MessageBox.Show(e.Message + " :GetCal/getTransact"); }

            return bcf;
        }

        internal static void DelCal(string? name)
        {
            try//delete transactions
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "delete from transactions where fileID = '" + name + "'";
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception e) { MessageBox.Show(e.Message + " :delCal/delTransact"); }
            try//delete bfc
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "delete from bcf where name = '" + name + "'";
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
            try//delete accounts
            {
                SQLiteConnection con = new("Data Source=calDB.db; Version = 3; New = True; Compress = True; ");
                SQLiteCommand cmd = con.CreateCommand();
                cmd.CommandText = "delete from accounts where fileID = '" + name + "'";
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
        }
    }
}
