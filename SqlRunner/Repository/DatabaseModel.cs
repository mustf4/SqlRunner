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
            DataTable dt = await Select(sql);
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
            DataTable dt = await Select(sql);
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

        public static async Task<DataTable> Select(string sql)
        {
            SqlConnection connnection = null;
            try
            {
                connnection = GetConnection();
                await connnection.OpenAsync();

                SqlCommand cmd = connnection.CreateCommand();
                cmd.CommandText = sql;

                SqlDataAdapter sda = new(cmd);
                DataTable dt = new("tables");
                sda.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
            finally
            {
                connnection?.Close();
            }
        }

        public static async Task Query(string sql)
        {
            SqlConnection connnection = null;
            try
            {
                connnection = GetConnection();
                connnection.Open();

                SqlCommand cmd = connnection.CreateCommand();
                cmd.CommandText = sql;

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connnection?.Close();
            }
        }

        private static SqlConnection GetConnection()
        {
            //SqlConnection thisConnection = new(@"Server=S-KV-CENTER-V20;Database=LastPurchasePrices;Trusted_Connection=Yes;TrustServerCertificate=Yes");
            return new(@"Server=.\;Database=GoldenSafeguard;Trusted_Connection=Yes;TrustServerCertificate=Yes");
        }
    }
}
