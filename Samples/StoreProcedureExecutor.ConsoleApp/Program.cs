﻿namespace ConsoleApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using Microshaoft;
    using MySql.Data.MySqlClient;
    using Newtonsoft.Json.Linq;
    public static class Program
    {
        async static Task Main(string[] args)
        {

            ValueTupleDataTableTest();

            Console.WriteLine("press any key to continue ...");

            Console.ReadLine();

            Console.WriteLine("use MSSQL ...");
            var json =
            @"
{
    sql:
        ""
            set statistics io on
            set statistics time on
            set statistics profile on
            select
                '\""111\""' as F1, *
            from
                sys.objects

            select
                '\""222\""' as F, *
            from
                sys.objects
        ""
}
                        ";

            json = @"
        {
            a: [
                {F1:""asdsa"", F2: 123, F3: ""2019-01-01""}
            ]
        }

";
            var jTokenParameters = JToken.Parse(json);
            var spName = "usp_executesql";
            spName = "usp_testudt";

            var x = new MsSqlStoreProceduresExecutor();

            SqlConnection sqlConnection = new SqlConnection
                (
                    "Initial Catalog=test;Data Source=gateway.hyper-v.internal\\sql2019,11433;User=sa;Password=!@#123QWE"
                )
            {
                StatisticsEnabled = true
            };

            x.CachedParametersDefinitionExpiredInSeconds = 10;

            var entries = x
                    .ExecuteResultsAsAsyncEnumerable
                        (
                            sqlConnection
                            , spName
                            , jTokenParameters
                        );
            await foreach
                    (
                        var (
                                resultSetIndex
                                , rowIndex
                                , columns
                                , dataRecord
                            )
                        in
                        entries
                    )
            {
                Console.WriteLine
                            (
                                $"{nameof(resultSetIndex)}:{resultSetIndex}{{0}}{nameof(rowIndex)}:{rowIndex}{{0}}{nameof(dataRecord)}:{dataRecord[1]}"
                                , "\t"
                            );

            }

            Console.WriteLine("press any key to continue ...");
            Console.ReadLine();

            var result = await
                        x
                            .ExecuteJsonResultsAsync
                                (
                                    sqlConnection
                                    , spName
                                    , jTokenParameters
                                );

            Console.WriteLine(result);

            Console.WriteLine("press any key to continue ...");
            Console.ReadLine();

            Console.WriteLine("use MySQL ...");

            var xx = new MySqlStoreProceduresExecutor();
            var mySqlConnection = new MySqlConnection()
            {
                ConnectionString = "server=gateway.hyper-v.internal;uid=root;pwd=!@#123QWE;database=Test"
            };

            entries = xx.ExecuteResultsAsAsyncEnumerable
                            (
                                mySqlConnection
                                , "zsp_test"
                                , JToken.Parse(@"{ Param1 : ""11""}")
                            );

            await foreach
                    (
                        var (
                                resultSetIndex
                                , rowIndex
                                , columns
                                , dataRecord
                            )
                        in
                        entries
                    )
            {
                Console.WriteLine
                            (
                                $"{nameof(resultSetIndex)}:{resultSetIndex}{{0}}{nameof(rowIndex)}:{rowIndex}{{0}}{nameof(dataRecord)}:{dataRecord[1]}"
                                , "\t"
                            );

            }

            Console.WriteLine("Hello World!");
        }

        private static void ValueTupleDataTableTest()
        {
            Console.WriteLine("ValueTuple DataTable Test:");
            
            SqlConnection sqlConnection = new SqlConnection
                (
                    "Initial Catalog=test;Data Source=gateway.hyper-v.internal\\sql2019,11433;User=sa;Password=!@#123QWE"
                );
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.CommandText = "usp_testudt";
            sqlCommand.CommandType = CommandType.StoredProcedure;
            var sqlParameter = new SqlParameter("a", SqlDbType.Structured);
            sqlCommand.Connection = sqlConnection;
            sqlCommand.Parameters.Add(sqlParameter);

            DataTable dataTable;
            IDataReader dataReader;
            (string F1, int F2, DateTime F3) x = ("asdsad", 100, DateTime.Now);
            dataTable = x.GenerateEmptyDataTable(nameof(x.F1), "FF2");
            dataTable.Rows.Add(x.F1, x.F2, x.F3);
            sqlParameter.Value = dataTable;
            sqlConnection.Open();
            dataReader = sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
            while (dataReader.Read())
            {
                Console.WriteLine(dataReader.FieldCount);
            }
            dataReader.Close();
            //=================================================================

            (string F1, int F2, DateTime F3) = ("asdsad", 100, DateTime.Now);
            dataTable = typeof(ValueTuple<string, int, DateTime>)
                                    .GenerateEmptyDataTable
                                        (nameof(F1), "FFF2");
            dataTable.Rows.Add(F1, F2, F3);

            sqlParameter.Value = dataTable;
            sqlConnection.Open();
            dataReader = sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
            while (dataReader.Read())
            {
                Console.WriteLine(dataReader.FieldCount);
            }
            dataReader.Close();



            sqlConnection.Close();
        }
    }
}
