namespace Microshaoft.StoreProcedureExecutors
{
    using Microshaoft;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Composition;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Threading.Tasks;

    [Export(typeof(IStoreProcedureExecutable))]
    public class MsSQLStoreProcedureExecutorCompositionPlugin
                        : IStoreProcedureExecutable
                            , IParametersDefinitionCacheAutoRefreshable
    {
        public AbstractStoreProceduresExecutor
                    <SqlConnection, SqlCommand, SqlParameter>
                        _executor = new MsSqlStoreProceduresExecutor();
        public string DataBaseType => "mssql";////this.GetType().Name;
        public int CachedParametersDefinitionExpiredInSeconds
        {
            get;
            set;
        }
        public bool NeedAutoRefreshExecutedTimeForSlideExpire
        {
            get;
            set;
        }
        private void ExecuteProcess
                    (
                        string connectionString
                        , string storeProcedureName
                        , out JToken result
                        , JToken parameters
                        , Action<DbConnection> onExecuting
                        , Action<bool> onReturning
                        , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                        , bool enableStatistics = false
                        , int commandTimeoutInSeconds = 90
                    )
        {
            if
                (
                    CachedParametersDefinitionExpiredInSeconds > 0
                    &&
                    _executor
                        .CachedParametersDefinitionExpiredInSeconds
                    !=
                    CachedParametersDefinitionExpiredInSeconds
                )
            {
                _executor
                        .CachedParametersDefinitionExpiredInSeconds
                            = CachedParametersDefinitionExpiredInSeconds;
            }
            result = null;
            var connection = new SqlConnection(connectionString);
            if (enableStatistics)
            {
                connection.StatisticsEnabled = enableStatistics;
            }
            onExecuting(connection);

            //result = _executor
            //                .Execute
            //                        (
            //                            connection
            //                            , storeProcedureName
            //                            , parameters
            //                            , onReadRowColumnProcessFunc
            //                            , commandTimeoutInSeconds
            //                        );
            if (NeedAutoRefreshExecutedTimeForSlideExpire)
            {
                _executor
                    .RefreshCachedExecuted
                        (
                            connection
                            , storeProcedureName
                        );
            }
            onReturning(true);
            //return true;
        }

        public async Task
                        <
                            (bool Success, JToken Result)
                        >
                        ExecuteAsync
                        (
                            string connectionString
                            , string storeProcedureName
                            , JToken parameters = null
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            , bool enableStatistics = false
                            , int commandTimeoutInSeconds = 90
                        )
        {
            var r = false;
            JToken result = null;
            ExecuteProcess
                (
                    connectionString
                    , storeProcedureName
                    , out result
                    , parameters
                    , async (connection) =>
                    {
                        var sqlConnection = (SqlConnection) connection;
                        result = await _executor
                                            .ExecuteAsync
                                                    (
                                                        sqlConnection
                                                        , storeProcedureName
                                                        , parameters
                                                        , onReadRowColumnProcessFunc
                                                        , commandTimeoutInSeconds
                                                    );
                    }
                    , (x) =>
                    {
                        r = x;
                    }
                    , onReadRowColumnProcessFunc
                    , enableStatistics
                    , commandTimeoutInSeconds
                );
            return
                (
                    Success: r
                    , Result: result
                );

            //throw new NotImplementedException();
        }

        public (bool Success, JToken Result) Execute
                            (
                                string connectionString
                                , string storeProcedureName
                                , JToken parameters
                                , //Func<IDataReader, Type, string, int, int, (bool NeedDefaultProcess, JProperty Field)> 
                                    OnReadRowColumnProcessFunc
                                    onReadRowColumnProcessFunc
                                , bool enableStatistics
                                , int commandTimeoutInSeconds
                            )
        {
            var r = false;
            JToken result = null;
            JToken rr = null;
            ExecuteProcess
                (
                    connectionString
                    , storeProcedureName
                    , out result
                    , parameters
                    , (connection) =>
                    {
                        var sqlConnection = (SqlConnection) connection;
                        rr = _executor
                                        .Execute
                                                (
                                                    sqlConnection
                                                    , storeProcedureName
                                                    , parameters
                                                    , onReadRowColumnProcessFunc
                                                    , commandTimeoutInSeconds
                                                );
                    }
                    , (x) =>
                    {
                        r = x;
                    }
                    , onReadRowColumnProcessFunc
                    , enableStatistics
                    , commandTimeoutInSeconds            
                );
            result = rr;
            return
                (
                     Success: r
                     , Result: result


                );

            //throw new NotImplementedException();

        }

        
        
    }
}
