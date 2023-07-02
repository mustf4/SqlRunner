﻿using SqlRunner.Extensions;
using SqlRunner.Handlers;
using SqlRunner.Models;
using SqlRunner.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
        private string _selectRowNumber = "1";
        private bool _isSelectSelected;
        private bool _isUpdateSelected;
        private bool _isDeleteSelected;
        private bool _isDatabaseSelected;
        private string _whereStatement;
        private string _updateStatement;
        private DataTable _resultTable;
        private bool _hasResult;
        private bool _isRunScriptEnabled;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Database> Databases { get; set; } = new ObservableCollection<Database>();
        public ObservableCollection<Table> Tables { get; set; } = new ObservableCollection<Table>();
        public List<Statement> Statements { get; set; } = new List<Statement>();
        public Database SelectedDatabase
        {
            get => _selectedDatabase;
            set => PropertyChanged.ChangeAndNotify(ref _selectedDatabase, value, () => SelectedDatabase, OnSelectedDatabaseChanged);
        }

        public Table SelectedTable
        {
            get => _selectedTable;
            set => PropertyChanged.ChangeAndNotify(ref _selectedTable, value, () => SelectedTable, OnSelectedTableChanged);
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
                        _selectRowNumber = "1";
                    }

                    PropertyChanged.Notify(() => SelectRowNumber);
                }
            }
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

        public ICommand RunScriptClickCommand => _runScriptCommand ??= new CommandHandler(() => RunScript(), true);

        public MainViewModel()
        {
            InitializeDatabases().ContinueWith((t) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Statements.AddRange(Enum.GetValues<Statement>());
                    SelectedDatabase = Databases[0];
                });
            });
        }

        private async Task InitializeDatabases()
        {
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
            string sql;
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

                    ResultTable = await DatabaseModel.Select(sql);
                    HasResult = ResultTable != null;
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

                    SanitizeUpdateStatement();

                    sql = $"update {SelectedDatabase.Name}.{SelectedTable.Name} set {UpdateStatement} where {WhereStatement}";
                    await DatabaseModel.Query(sql);

                    sql = $"select * from {SelectedDatabase.Name}.{SelectedTable.Name} where {WhereStatement}";
                    ResultTable = await DatabaseModel.Select(sql);
                    HasResult = ResultTable != null;
                    break;
                case Statement.Delete:
                    if (string.IsNullOrWhiteSpace(WhereStatement))
                    {
                        MessageBox.Show("Please fill 'WHERE' conditions first");
                        return;
                    }

                    sql = $"delete from {SelectedDatabase.Name}.{SelectedTable.Name} where {WhereStatement}";
                    await DatabaseModel.Query(sql);

                    sql = $"select * from {SelectedDatabase.Name}.{SelectedTable.Name} where {WhereStatement}";
                    ResultTable = await DatabaseModel.Select(sql);
                    HasResult = ResultTable != null;
                    break;
                default:
                    break;
            }
        }

        private void SanitizeUpdateStatement()
        {
            if (string.IsNullOrWhiteSpace(UpdateStatement))
                return;

            UpdateStatement = UpdateStatement.Replace("set ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        private void SanitizeWhereStatement()
        {
            if (string.IsNullOrWhiteSpace(WhereStatement))
                return;

            WhereStatement = WhereStatement.Replace("where ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
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

        private void OnSelectedTableChanged()
        {
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