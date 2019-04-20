//namespace Microshaoft
//{
//    using Newtonsoft.Json.Linq;
//    using System;
//    using System.Collections.Concurrent;
//    using System.Collections.Generic;
//    using System.Data;
//    using System.Data.Common;
//    using System.Data.SqlClient;
//    using System.Linq;
//    using System.Threading.Tasks;

//    public abstract partial class
//            AbstractStoreProceduresExecutor
//                    <TDbConnection, TDbCommand, TDbParameter>
//                        where
//                                TDbConnection : DbConnection, new()
//                        where
//                                TDbCommand : DbCommand, new()
//                        where
//                                TDbParameter : DbParameter, new()
//    {
//        public async Task<JToken>
//                    ExecuteAsync
//                        (
//                            DbConnection connection
//                            , string storeProcedureName
//                            , JToken inputsParameters = null //string.Empty
//                            , Func
//                                <
//                                    IDataReader
//                                    , Type        // fieldType
//                                    , string    // fieldName
//                                    , int       // row index
//                                    , int       // column index
//                                    ,
//                                        (
//                                            bool NeedDefaultProcess
//                                            , JProperty Field   //  JObject Field 对象
//                                        )
//                                > onReadRowColumnProcessFunc = null
//                            //, bool enableStatistics = false
//                            , int commandTimeoutInSeconds = 90
//                        )
//        {
//            var dataSource = connection.DataSource;
//            var dataBaseName = connection.Database;
//            SqlConnection sqlConnection = connection as SqlConnection;
//            var isSqlConnection = (sqlConnection != null);
//            var statisticsEnabled = false;
//            if (isSqlConnection)
//            {
//                statisticsEnabled = sqlConnection.StatisticsEnabled;
//            }
//            SqlCommand sqlCommand = null;
//            StatementCompletedEventHandler onStatementCompletedEventHandlerProcessAction = null;
//            SqlInfoMessageEventHandler onSqlInfoMessageEventHandlerProcessAction = null;
//            try
//            {
//                using
//                    (
//                        TDbCommand command = new TDbCommand()
//                        {
//                            CommandType = CommandType.StoredProcedure
//                            , CommandTimeout = commandTimeoutInSeconds
//                            , CommandText = storeProcedureName
//                            , Connection = connection
//                        }
//                    )
//                {

//                    if (commandTimeoutInSeconds > 0)
//                    {
//                        command.CommandTimeout = commandTimeoutInSeconds;
//                    }
//                    var dbParameters
//                            = GenerateExecuteParameters
//                                    (
//                                        connection.ConnectionString
//                                        , storeProcedureName
//                                        , inputsParameters
//                                    );
//                    if (dbParameters != null)
//                    {
//                        var parameters = dbParameters.ToArray();
//                        command
//                            .Parameters
//                            .AddRange(parameters);
//                    }
//                    connection.Open();
//                    var result = new JObject
//                    {
//                        {
//                            "BeginTime"
//                            , null
//                        }
//                        ,
//                        {
//                            "EndTime"
//                            , null
//                        }
//                        ,
//                        {
//                            "DurationInMilliseconds"
//                            , null
//                        }
//                        ,
//                        {
//                            "Outputs"
//                            , new JObject
//                                {
//                                    {
//                                        "Parameters"
//                                            , null
//                                    }
//                                    ,
//                                    {
//                                        "ResultSets"
//                                            , new JArray()
//                                    }
//                                }
//                        }
//                    };
//                    var dataReader = await command
//                                                .ExecuteReaderAsync
//                                                    (
//                                                        CommandBehavior
//                                                            .CloseConnection
//                                                    );

//                    int resultSetID = 0;
//                    int messageID = 0;
//                    JArray recordCounts = null;
//                    JArray messages = null;
//                    if (statisticsEnabled)
//                    {
//                        recordCounts = new JArray();
//                        messages = new JArray();
//                        sqlCommand = command as SqlCommand;
//                        onStatementCompletedEventHandlerProcessAction =
//                                (sender, statementCompletedEventArgs) =>
//                                {
//                                    recordCounts.Add(statementCompletedEventArgs.RecordCount);
//                                };
//                        sqlCommand.StatementCompleted += onStatementCompletedEventHandlerProcessAction;
//                        onSqlInfoMessageEventHandlerProcessAction =
//                        (sender, sqlInfoMessageEventArgs) =>
//                        {
//                            messageID++;
//                            messages
//                                    .Add
//                                        (
//                                            new JObject()
//                                            {
//                                                {
//                                                    "MessageID"
//                                                    , messageID
//                                                }
//                                                ,
//                                                {
//                                                    "ResultSetID"
//                                                    , resultSetID
//                                                }
//                                                ,
//                                                {
//                                                    "Source"
//                                                    , sqlInfoMessageEventArgs.Source
//                                                }
//                                                ,
//                                                {
//                                                    "Message"
//                                                    , sqlInfoMessageEventArgs.Message
//                                                }
//                                                ,
//                                                {
//                                                    "DealTime"
//                                                    , DateTime.Now
//                                                }
//                                            }
//                                        );
//                        };
//                        sqlConnection.InfoMessage += onSqlInfoMessageEventHandlerProcessAction;
//                    }
//                    do
//                    {
//                        var columns = dataReader
//                                        .GetColumnsJArray();
//                        var rows = dataReader
//                                        .AsRowsJTokensEnumerable
//                                            (
//                                                columns
//                                                , onReadRowColumnProcessFunc
//                                            );
//                        var resultSet = new JObject
//                                {
//                                    {
//                                        "Columns"
//                                        , columns
//                                    }
//                                    ,
//                                    {
//                                        "Rows"
//                                        , new JArray(rows)
//                                    }
//                                };
//                        (
//                            (JArray)
//                                result
//                                    ["Outputs"]
//                                    ["ResultSets"]
//                        )
//                        .Add
//                            (
//                                resultSet
//                            );
//                        resultSetID++;
//                    }
//                    while (dataReader.NextResult());
//                    dataReader.Close();
//                    JObject jOutputParameters = null;
//                    if (dbParameters != null)
//                    {
//                        var outputParameters =
//                                    dbParameters
//                                            .Where
//                                                (
//                                                    (x) =>
//                                                    {
//                                                        return
//                                                            (
//                                                                x
//                                                                    .Direction
//                                                                !=
//                                                                ParameterDirection
//                                                                    .Input
//                                                            );
//                                                    }
//                                                );
//                        foreach (var x in outputParameters)
//                        {
//                            if (jOutputParameters == null)
//                            {
//                                jOutputParameters = new JObject();
//                            }
//                            jOutputParameters
//                                    .Add
//                                        (
//                                            x.ParameterName.TrimStart('@', '?')
//                                            , new JValue(x.Value)
//                                        );
//                        }
//                    }
//                    if (jOutputParameters != null)
//                    {
//                        result["Outputs"]["Parameters"] = jOutputParameters;
//                    }
//                    if (statisticsEnabled)
//                    {
//                        //if (sqlConnection.StatisticsEnabled)
//                        {
//                            var j = new JObject();
//                            var statistics = sqlConnection.RetrieveStatistics();
//                            var json = JsonHelper.Serialize(statistics);
//                            var jStatistics = JObject.Parse(json);
//                            var jCurrent = result["DurationInMilliseconds"];
//                            jCurrent
//                                .Parent
//                                .AddAfterSelf
//                                    (
//                                        new JProperty
//                                                (
//                                                    "DataBaseStatistics"
//                                                    , jStatistics
//                                                )
//                                    );
//                            if (messages != null)
//                            {
//                                result["DataBaseStatistics"]["Messages"] = messages;
//                            }
//                            if (recordCounts != null)
//                            {
//                                jCurrent
//                                    .Parent
//                                    .AddAfterSelf
//                                            (
//                                                new JProperty
//                                                        (
//                                                            "RecordCounts"
//                                                            , recordCounts
//                                                        )
//                                            );
//                            }
//                        }
//                        if (onStatementCompletedEventHandlerProcessAction != null)
//                        {
//                            sqlCommand.StatementCompleted -= onStatementCompletedEventHandlerProcessAction;
//                            onStatementCompletedEventHandlerProcessAction = null;
//                        }
//                        if (onSqlInfoMessageEventHandlerProcessAction != null)
//                        {
//                            sqlConnection.InfoMessage -= onSqlInfoMessageEventHandlerProcessAction;
//                            onSqlInfoMessageEventHandlerProcessAction = null;
//                        }
//                    }
//                    return result;
//                }
//            }
//            finally
//            {
//                if (isSqlConnection)
//                {
//                    if (onStatementCompletedEventHandlerProcessAction != null)
//                    {
//                        sqlCommand.StatementCompleted -= onStatementCompletedEventHandlerProcessAction;
//                        onStatementCompletedEventHandlerProcessAction = null;
//                    }
//                    if (onSqlInfoMessageEventHandlerProcessAction != null)
//                    {
//                        sqlConnection.InfoMessage -= onSqlInfoMessageEventHandlerProcessAction;
//                        onSqlInfoMessageEventHandlerProcessAction = null;
//                    }
//                    if (sqlConnection.StatisticsEnabled)
//                    {
//                        sqlConnection.StatisticsEnabled = false;
//                    }
//                    sqlConnection = null;
//                }
//                if (connection.State != ConnectionState.Closed)
//                {
//                    connection.Close();
//                }
//                connection = null;
//            }
//        }
//    }
//}