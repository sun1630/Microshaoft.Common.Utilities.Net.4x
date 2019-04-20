namespace Microshaoft
{
    using Newtonsoft.Json.Linq;
    using System.Data;
    using System.Data.Common;
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
                            TDbConnection connection
                            , string storeProcedureName
                            , JToken inputsParameters = null //string.Empty
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            //, bool enableStatistics = false
                            , int commandTimeoutInSeconds = 90
                        )
        {
            JToken r = null;
            InvokeExecute
                (
                    connection
                    ,storeProcedureName
                    ,(c) =>
                    {
                        c.Open();
                    }
                    , (context) =>
                    {
                        var command = context.Command;
                        var dataReader = command
                                            .ExecuteReader
                                                (
                                                    CommandBehavior
                                                        .CloseConnection
                                                );
                        context.Reader = dataReader;
                    }
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
                            TDbConnection connection
                            , string storeProcedureName
                            , JToken inputsParameters = null //string.Empty
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            //, bool enableStatistics = false
                            , int commandTimeoutInSeconds = 90
                        )
        {
            JToken r = null;
            InvokeExecute
                (
                    connection
                    , storeProcedureName
                    , async (c) =>
                    {
                        await c.OpenAsync();
                    }
                    , async (context) =>
                    {
                        var command = context.Command;
                        var dataReader = await command
                                                    .ExecuteReaderAsync
                                                        (
                                                            CommandBehavior
                                                                .CloseConnection
                                                        );
                        context.Reader = dataReader;
                    }
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