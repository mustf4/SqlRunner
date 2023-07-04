using SqlRunner.Models;
using SqlRunner.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;

namespace SqlRunner.Repository
{
    internal class DatabaseModel
    {
        public async Task<List<string>> GetDatabases()
        {
            string sql = "SELECT name, database_id, create_date FROM sys.databases;";
            DataTable dt = await SelectAsync(sql);
            if (dt == null)
                return new List<string>();

            List<string> result = new();
            foreach (DataRow row in dt.Rows)
            {
                if (row.ItemArray.Length > 0 && row.ItemArray[0] != null && !string.IsNullOrWhiteSpace(row.ItemArray[0]!.ToString()))
                {
                    result.Add(row.ItemArray[0]!.ToString()!);
                }
            }

            result.Sort();
            return result;
        }

        public async Task<List<string>> GetTables(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                return new List<string>();

            string sql = $"SELECT table_name, TABLE_SCHEMA FROM {databaseName}.INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE'";
            DataTable dt = await SelectAsync(sql);
            if (dt == null)
                return new List<string>();

            List<string> result = new();
            foreach (DataRow row in dt.Rows)
            {
                if (row.ItemArray.Length > 0 && row.ItemArray[0] != null && !string.IsNullOrWhiteSpace(row.ItemArray[0]!.ToString()))
                {
                    result.Add(row.ItemArray[1]!.ToString() + "." + row.ItemArray[0]!.ToString()!);
                }
            }

            result.Sort();
            return result;
        }

        public static async Task<List<Column>> GetColumns(string databaseName, string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return new();

            string schema = tableName[..tableName.IndexOf(".")];
            tableName = tableName[(tableName.IndexOf(".") + 1)..];

            string sql = @$"USE {databaseName}
                            SELECT COLUMN_NAME, ORDINAL_POSITION, DATA_TYPE
                            FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_SCHEMA = '{schema}' AND TABLE_NAME = N'{tableName}' AND TABLE_CATALOG = '{databaseName}'";

            DataTable dt = await SelectAsync(sql);
            if (dt == null)
                return new();

            List<Column> result = new();
            foreach (DataRow row in dt.Rows)
            {
                if (row.ItemArray.Length > 0 && row.ItemArray[0] != null && !string.IsNullOrWhiteSpace(row.ItemArray[0]!.ToString()))
                {
                    result.Add(new()
                    {
                        Name = row.ItemArray[0].ToString(),
                        Position = int.Parse(row.ItemArray[1].ToString()),
                        Type = row.ItemArray[2].ToString()
                    });
                }
            }

            return result;
        }

        public static async Task<int> GetCountAsync(string dbName, string tableName, string whereStatement)
        {
            string sql = $"select count(*) from {dbName}.{tableName} where {whereStatement}";

            DataTable dt = await SelectAsync(sql);
            DataRowCollection rows = dt?.Rows;
            return rows != null && rows.Count == 1 && rows[0].ItemArray.Length > 0 && rows[0].ItemArray[0] != null && !string.IsNullOrWhiteSpace(rows[0].ItemArray[0]!.ToString())
                ? int.Parse(rows[0].ItemArray[0].ToString())
                : 0;
        }

        public static async Task<DataTable> SelectAsync(string sql)
        {
            SqlConnection connnection = null;
            try
            {
                connnection = GetConnection();
                await connnection.OpenAsync();

                SqlCommand cmd = connnection.CreateCommand();
                cmd.CommandText = sql;

                SqlDataAdapter sda = new(cmd);
                DataTable dt = new("Table");
                sda.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message));
                return null;
            }
            finally
            {
                connnection?.Close();
            }
        }

        public static async Task<int> QueryAsync(string sql)
        {
            SqlConnection connnection = null;
            try
            {
                connnection = GetConnection();
                connnection.Open();

                SqlCommand cmd = connnection.CreateCommand();
                cmd.CommandText = sql;

                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message));
                return 0;
            }
            finally
            {
                connnection?.Close();
            }
        }

        private static SqlConnection GetConnection()
        {
            Preferences preferences = Serializer.Deserialize<Preferences>(Settings.PreferencesPath);
            //return new(@"Server=S-KV-CENTER-V20;Database=LastPurchasePrices;Trusted_Connection=Yes;TrustServerCertificate=Yes");
            //return new(@"Server=.\;Database=GoldenSafeguard;Trusted_Connection=Yes;TrustServerCertificate=Yes");
            return new(preferences.ConnectionString);
        }
    }
}
