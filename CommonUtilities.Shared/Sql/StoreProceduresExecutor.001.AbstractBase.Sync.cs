namespace Microshaoft
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract partial class
            AbstractStoreProceduresExecutor
                    <TDbConnection, TDbCommand, TDbParameter>
                        where
                                TDbConnection : DbConnection, new()
                        where
                                TDbCommand : DbCommand, new()
                        where
                                TDbParameter : DbParameter, new()
    {
        public virtual JToken
                    Execute
                        (
                            DbConnection connection
                            , string storeProcedureName
                            , JToken inputsParameters = null //string.Empty
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            //, bool enableStatistics = false
                            , int commandTimeoutInSeconds = 90
                        )
        {
            void exec(DbCommand cmd, ref DbDataReader reader)
            {
                reader = cmd
                                            .ExecuteReader
                                                    (
                                                        CommandBehavior.CloseConnection
                                                    );

            }

            JToken r = null;
            Execute
                (
                    connection
                    ,storeProcedureName
                    ,(c) =>
                    {
                        c.Open();
                    }
                    , exec
                    , (result) =>
                    {
                        r = result;
                    }
                    , inputsParameters
                    , onReadRowColumnProcessFunc
                    , commandTimeoutInSeconds
                );
            return r;

        }


        public async Task<JToken>
                    ExecuteAsync
                        (
                            DbConnection connection
                            , string storeProcedureName
                            , JToken inputsParameters = null //string.Empty
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            //, bool enableStatistics = false
                            , int commandTimeoutInSeconds = 90
                        )
        {
            JToken r = null;
            Execute
                (
                    connection
                    , storeProcedureName
                    , async (c) =>
                    {
                        await connection.OpenAsync();
                    }
                    , null
                    //async (command, ref dataReader) =>
                    //{
                    //    dataReader = await command
                    //                .ExecuteReaderAsync
                    //                    (
                    //                        CommandBehavior.CloseConnection
                    //                    );
                    //}
                    , (result) =>
                    {
                        r = result;
                    }
                    , inputsParameters
                    , onReadRowColumnProcessFunc
                    , commandTimeoutInSeconds
                );
            return r;

        }

    }
}