using SqlRunner.Extensions;
using SqlRunner.Handlers;
using SqlRunner.Models;
using SqlRunner.Repository;
using SqlRunner.Utils;
using SqlRunner.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SqlRunner.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private Database _selectedDatabase;
        private Table _selectedTable;
        private Statement? _selectedStatement;
        private ICommand _runScriptCommand;
        private ICommand _preferencesCommand;
        private string _selectRowNumber = "10";
        private bool _isSelectSelected;
        private bool _isUpdateSelected;
        private bool _isDeleteSelected;
        private bool _isDatabaseSelected;
        private string _whereStatement;
        private string _updateStatement;
        private DataTable _resultTable;
        private bool _hasResult;
        private bool _isRunScriptEnabled;
        private string _resultStatusText;
        private string _affectedRecordsInfo;
        private Column _selectedColumn;
        private string _selectedOrderDirection;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Database> Databases { get; set; } = new();
        public ObservableCollection<Table> Tables { get; set; } = new();
        public ObservableCollection<Column> OrderColumns { get; set; } = new();
        public List<Statement> Statements => Enum.GetValues<Statement>().ToList();
        public List<string> OrderDirections => new() { "ASC", "DESC" };
        public Database SelectedDatabase
        {
            get => _selectedDatabase;
            set => PropertyChanged.ChangeAndNotify(ref _selectedDatabase, value, () => SelectedDatabase, OnSelectedDatabaseChanged);
        }

        public Table SelectedTable
        {
            get => _selectedTable;
            set => PropertyChanged.ChangeAndNotify(ref _selectedTable, value, () => SelectedTable, async () => await OnSelectedTableChangedAsync());
        }

        public Column SelectedOrderColumn
        {
            get => _selectedColumn;
            set => PropertyChanged.ChangeAndNotify(ref _selectedColumn, value, () => SelectedOrderColumn);
        }

        public Statement? SelectedStatement
        {
            get => _selectedStatement;
            set => PropertyChanged.ChangeAndNotify(ref _selectedStatement, value, () => SelectedStatement, OnSelectedStatementChagned);
        }

        public string SelectRowNumber
        {
            get => _selectRowNumber;
            set
            {
                if (_selectRowNumber != value)
                {
                    if (int.TryParse(value, out int number))
                    {
                        _selectRowNumber = number.ToString();
                    }
                    else if (string.IsNullOrWhiteSpace(value))
                    {
                        _selectRowNumber = "10";
                    }

                    PropertyChanged.Notify(() => SelectRowNumber);
                }
            }
        }

        public string SelectedOrderDirection
        { 
            get => _selectedOrderDirection; 
            set => PropertyChanged.ChangeAndNotify(ref _selectedOrderDirection, value, () => SelectedOrderDirection); 
        }

        public bool IsSelectSelected
        {
            get => _isSelectSelected;
            set => PropertyChanged.ChangeAndNotify(ref _isSelectSelected, value, () => IsSelectSelected);
        }

        public bool IsUpdateSelected
        {
            get => _isUpdateSelected;
            set => PropertyChanged.ChangeAndNotify(ref _isUpdateSelected, value, () => IsUpdateSelected);
        }

        public bool IsDeleteSelected
        {
            get => _isDeleteSelected;
            set => PropertyChanged.ChangeAndNotify(ref _isDeleteSelected, value, () => IsDeleteSelected);
        }

        public bool IsDatabaseSelected
        {
            get => _isDatabaseSelected;
            set => PropertyChanged.ChangeAndNotify(ref _isDatabaseSelected, value, () => IsDatabaseSelected);
        }

        public string WhereStatement
        {
            get => _whereStatement;
            set => PropertyChanged.ChangeAndNotify(ref _whereStatement, value, () => WhereStatement, OnWhereStatementChange);
        }

        public string UpdateStatement
        {
            get => _updateStatement;
            set => PropertyChanged.ChangeAndNotify(ref _updateStatement, value, () => UpdateStatement, OnUpdateStatementChange);
        }

        public DataTable ResultTable
        {
            get => _resultTable;
            set => PropertyChanged.ChangeAndNotify(ref _resultTable, value, () => ResultTable);
        }

        public bool HasResult
        {
            get => _hasResult;
            set => PropertyChanged.ChangeAndNotify(ref _hasResult, value, () => HasResult);
        }

        public bool IsRunScriptEnabled
        {
            get => _isRunScriptEnabled;
            set => PropertyChanged.ChangeAndNotify(ref _isRunScriptEnabled, value, () => IsRunScriptEnabled);
        }

        public string ResultStatusText
        {
            get => _resultStatusText;
            set => PropertyChanged.ChangeAndNotify(ref _resultStatusText, value, () => ResultStatusText);
        }

        public string AffectedRecordsInfo
        {
            get => _affectedRecordsInfo;
            set => PropertyChanged.ChangeAndNotify(ref _affectedRecordsInfo, value, () => AffectedRecordsInfo);
        }

        public ICommand RunScriptClickCommand => _runScriptCommand ??= new CommandHandler(() => RunScript(), true);

        public ICommand PreferencesCommand => _preferencesCommand ??= new CommandHandler(() => ShowPreferences(), true);

        public MainViewModel()
        {
            SelectedStatement = Statement.Select;
            SelectedOrderDirection = OrderDirections[0];

            Preferences preferences = GetPreferences();
            while (string.IsNullOrWhiteSpace(preferences.ConnectionString))
            {
                ShowPreferences();
                preferences = GetPreferences();
            }

            InitializeDatabasesAndTables();
        }

        private static Preferences GetPreferences()
        {
            return Serializer.Deserialize<Preferences>(Settings.PreferencesPath);
        }

        private void InitializeDatabasesAndTables()
        {
            InitializeDatabases().ContinueWith((t) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Database lastPurchasePriceDb = Databases.FirstOrDefault(db => db.Name == "LastPurchasePrices");
                    SelectedDatabase = lastPurchasePriceDb ?? (Databases.Any() ? Databases[0] : null);
                });
            });
        }

        private async Task InitializeDatabases()
        {
            Databases.Clear();
            DatabaseModel dbModel = new();
            List<string> databases = await dbModel.GetDatabases();
            foreach (string database in databases)
            {
                Databases.Add(new Database { Name = database });
            }
        }

        private async void RunScript()
        {
            ResultTable = new DataTable();
            HasResult = false;
            AffectedRecordsInfo = ResultStatusText = null;
            string sql;
            string verb;
            string noun;
            int affectedRowsCount;
            DataTable affectingResult;
            SanitizeUpdateStatement();
            SanitizeWhereStatement();

            if (SelectedTable == null)
                return;

            switch (SelectedStatement)
            {
                case Statement.Select:
                    sql = $"select top {SelectRowNumber} * from {SelectedDatabase.Name}.{SelectedTable.Name}";
                    if (!string.IsNullOrWhiteSpace(WhereStatement))
                    {
                        sql += $" where {WhereStatement}";
                    }

                    await DisplayAffectedRows(sql);
                    break;
                case Statement.Update:
                    if (string.IsNullOrWhiteSpace(UpdateStatement))
                    {
                        MessageBox.Show("Please fill 'UPDATE' statements first");
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(WhereStatement))
                    {
                        MessageBox.Show("Please fill 'WHERE' conditions first");
                        return;
                    }

                    sql = $"select count(*) from {SelectedDatabase.Name}.{SelectedTable.Name} where {WhereStatement}";
                    affectingResult = await DatabaseModel.SelectAsync(sql);

                    if (MessageBox.Show($"This run is going to affect the following count of rows: {affectingResult.Rows.Count}", "Update confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) != MessageBoxResult.Yes)
                        return;

                    sql = $"update {SelectedDatabase.Name}.{SelectedTable.Name} set {UpdateStatement} where {WhereStatement}";
                    affectedRowsCount = await DatabaseModel.QueryAsync(sql);

                    verb = affectedRowsCount > 1 ? "are" : "is";
                    noun = affectedRowsCount > 1 ? "rows" : "row";
                    AffectedRecordsInfo = $"There {verb} {affectedRowsCount} {noun} affected";
                    await DisplayAffectedRows();
                    break;
                case Statement.Delete:
                    if (string.IsNullOrWhiteSpace(WhereStatement))
                    {
                        MessageBox.Show("Please fill 'WHERE' conditions first");
                        return;
                    }

                    sql = $"select count(*) from {SelectedDatabase.Name}.{SelectedTable.Name} where {WhereStatement}";
                    affectingResult = await DatabaseModel.SelectAsync(sql);

                    if (MessageBox.Show($"This run is going to affect the following count of rows: {affectingResult.Rows.Count}", "Update confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) != MessageBoxResult.Yes)
                        return;

                    sql = $"delete from {SelectedDatabase.Name}.{SelectedTable.Name} where {WhereStatement}";
                    affectedRowsCount = await DatabaseModel.QueryAsync(sql);

                    verb = affectedRowsCount > 1 ? "are" : "is";
                    noun = affectedRowsCount > 1 ? "rows" : "row";
                    AffectedRecordsInfo = $"There {verb} {affectedRowsCount} {noun} affected";

                    await DisplayAffectedRows();
                    break;
                default:
                    break;
            }
        }

        private async Task DisplayAffectedRows(string sql = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                sql = $"select * from {SelectedDatabase.Name}.{SelectedTable.Name} where {WhereStatement}";

            if (!string.IsNullOrWhiteSpace(SelectedOrderColumn?.Name))
                sql += $" order by {SelectedOrderColumn.Name} {SelectedOrderDirection}";

            ResultTable = await DatabaseModel.SelectAsync(sql);
            HasResult = ResultTable != null;
            if (HasResult)
                ResultStatusText = $"{ResultTable.Rows.Count} rows";
        }

        private void ShowPreferences()
        {
            PreferencesWindow preferencesWindow = new();
            preferencesWindow.ShowDialog();

            Preferences preferences = GetPreferences();
            if (!string.IsNullOrWhiteSpace(preferences.ConnectionString))
                InitializeDatabasesAndTables();
        }

        private void SanitizeUpdateStatement()
        {
            if (string.IsNullOrWhiteSpace(UpdateStatement))
                return;

            UpdateStatement = FormatText(UpdateStatement);
            UpdateStatement = UpdateStatement.Replace("set ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        private void SanitizeWhereStatement()
        {
            if (string.IsNullOrWhiteSpace(WhereStatement))
                return;

            WhereStatement = FormatText(WhereStatement);
            WhereStatement = WhereStatement.Replace("where ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

            int unsupportedSymbolsIndex = WhereStatement.IndexOf("--");
            if (unsupportedSymbolsIndex > -1)
                WhereStatement = WhereStatement[..unsupportedSymbolsIndex];

            unsupportedSymbolsIndex = WhereStatement.IndexOf(";");
            if (unsupportedSymbolsIndex > -1)
                WhereStatement = WhereStatement[..unsupportedSymbolsIndex];
        }

        private static string FormatText(string statementText)
        {
            List<char> result = new();

            foreach (char c in statementText)
            {
                if (result.Count == 0)
                {
                    if (c != ' ')
                        result.Add(c);
                }
                else if (c == ' ' && result.Last() == ' ')
                    continue;
                else
                    result.Add(c);
            }

            return new string(result.ToArray()).Trim();
        }

        private async void OnSelectedDatabaseChanged()
        {
            Tables.Clear();
            if (SelectedDatabase != null && !string.IsNullOrWhiteSpace(SelectedDatabase.Name))
            {
                DatabaseModel dbModel = new();
                var tablesName = await dbModel.GetTables(SelectedDatabase.Name);
                foreach (string table in tablesName)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Tables.Add(new Table { Name = table });
                    });
                }
                IsDatabaseSelected = true;
            }
            else
            {
                IsDatabaseSelected = false;
            }
        }

        private async Task OnSelectedTableChangedAsync()
        {
            SelectedOrderColumn = null;
            OrderColumns.Clear();

            if (string.IsNullOrWhiteSpace(SelectedDatabase?.Name) || string.IsNullOrWhiteSpace(SelectedTable?.Name))
                return;

            List<Column> columns = await DatabaseModel.GetColumns(SelectedDatabase.Name, SelectedTable.Name);
            foreach (Column column in columns)
                OrderColumns.Add(column);

            if (OrderColumns.Count > 0)
                SelectedOrderColumn = OrderColumns[0];

            CheckRunScriptVisibility();
        }

        private void OnWhereStatementChange()
        {
            CheckRunScriptVisibility();
        }

        private void OnUpdateStatementChange()
        {
            CheckRunScriptVisibility();
        }

        private void CheckRunScriptVisibility()
        {
            IsRunScriptEnabled = SelectedTable != null && (SelectedStatement != Statement.Delete || !string.IsNullOrWhiteSpace(WhereStatement)) && (SelectedStatement != Statement.Update || !string.IsNullOrWhiteSpace(WhereStatement) && !string.IsNullOrWhiteSpace(UpdateStatement));
        }

        private void OnSelectedStatementChagned()
        {
            IsSelectSelected = IsUpdateSelected = IsDeleteSelected = false;
            switch (_selectedStatement)
            {
                case Statement.Select:
                    IsSelectSelected = true;
                    break;
                case Statement.Update:
                    IsUpdateSelected = true;
                    break;
                case Statement.Delete:
                    IsDeleteSelected = true;
                    break;
                default:
                    break;
            }
            CheckRunScriptVisibility();
        }
    }
}
