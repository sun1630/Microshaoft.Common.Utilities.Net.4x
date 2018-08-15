﻿#if !XAMARIN
namespace Microshaoft
{
    //using MySql.Data.MySqlClient;
    using Newtonsoft.Json.Linq;
    using Npgsql;
    using NpgsqlTypes;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Linq;
    public static class NpgSqlHelper
    {
        public static int CachedExecutingParametersExpiredInSeconds
        {
            get
            {
                return
                    SqlHelper
                        .CachedExecutingParametersExpiredInSeconds;
            }
            set
            {
                SqlHelper
                    .CachedExecutingParametersExpiredInSeconds = value;
            }
        }

        private static NpgsqlParameter
                        onQueryDefinitionsSetInputParameterProcessFunc
                            (
                                NpgsqlParameter parameter
                            )
        {
            parameter.NpgsqlDbType = NpgsqlDbType.Varchar;
            return parameter;
        }
        private static NpgsqlParameter
                        onQueryDefinitionsSetReturnParameterProcessFunc
                            (
                                NpgsqlParameter parameter
                            )
        {
            parameter.NpgsqlDbType = NpgsqlDbType.Integer;
            return parameter;
        }
        private static NpgsqlParameter
                       onQueryDefinitionsReadOneDbParameterProcessFunc
                           (
                               IDataReader reader
                               , NpgsqlParameter parameter
                           )
        {
            var npgSqlDbTypeName = //(string)(reader["TYPE_NAME"]);
                    (string)(reader["DATA_TYPE"]);
            NpgsqlDbType sqlDbType = (NpgsqlDbType)Enum.Parse(typeof(NpgsqlDbType), npgSqlDbTypeName, true);
            parameter
                .NpgsqlDbType = sqlDbType;
            if ((parameter.NpgsqlDbType == NpgsqlDbType.Numeric))
            {
                parameter.Scale = (byte)(((short)(reader["NUMERIC_SCALE"]) & 255));
                parameter.Precision = (byte)(((short)(reader["NUMERIC_PRECISION"]) & 255));
            }
            return parameter;
        }
        private static NpgsqlParameter
                       onExecutingSetDbParameterTypeProcessFunc
                            (
                                NpgsqlParameter definitionNpgsqlParameter
                                , NpgsqlParameter cloneNpgsqlParameter
                            )
        {
            //to do
            cloneNpgsqlParameter.NpgsqlDbType = definitionNpgsqlParameter.NpgsqlDbType;
            return cloneNpgsqlParameter;
        }

        private static object
               onExecutingSetDbParameterValueProcessFunc
                    (
                        NpgsqlParameter parameter
                        , JToken jValue
                    )
        {
            object r = null;
            var jValueText = jValue.ToString();

            if
                (
                    parameter.NpgsqlDbType == NpgsqlDbType.Varchar
                    //||
                    //parameter.NpgsqlDbType == NpgsqlDbType.NVarChar
                    //||
                    //parameter.NpgsqlDbType == NpgsqlDbType.Char
                    //||
                    //parameter.NpgsqlDbType == NpgsqlDbType.NChar
                    ||
                    parameter.NpgsqlDbType == NpgsqlDbType.Text
                    ||
                    parameter.NpgsqlDbType == NpgsqlDbType.Char
                    //||
                    //parameter.NpgsqlDbType == NpgsqlDbType.
                )
            {
                r = jValueText;
            }
            else if
                (
                    parameter.NpgsqlDbType == NpgsqlDbType.Date
                    ||
                    parameter.NpgsqlDbType == NpgsqlDbType.Time
                    //||
                    //parameter.NpgsqlDbType == NpgsqlDbType.SmallDateTime
                    //||
                    //parameter.NpgsqlDbType == NpgsqlDbType.Date
                    //||
                    //parameter.NpgsqlDbType == NpgsqlDbType.DateTime
                )
            {
                r = DateTime.Parse(jValueText);
            }
            //else if
            //    (
            //        target.MySqlDbType == MySqlDbType.DateTimeOffset
            //    )
            //{
            //    r = new JValue((DateTimeOffset)target.Value);
            //}
            else if
                (
                    parameter.NpgsqlDbType == NpgsqlDbType.Bit
                )
            {
                r = bool.Parse(jValueText);
            }
            //else if
            //    (
            //        parameter.MySqlDbType == MySqlDbType.Decimal
            //    )
            //{
            //    r = decimal.Parse(jValueText);
            //}
            else if
                (
                    parameter.NpgsqlDbType == NpgsqlDbType.Double
                )
            {
                r = Double.Parse(jValueText);
            }
            else if
                (
                    parameter.NpgsqlDbType == NpgsqlDbType.Real
                )
            {
                r = Double.Parse(jValueText);
            }
            else if
                (
                    parameter.NpgsqlDbType == NpgsqlDbType.Uuid
                )
            {
                r = Guid.Parse(jValueText);
            }
            //else if
            //    (
            //        parameter.MySqlDbType == MySqlDbType.UInt16
            //    )
            //{
            //    r = ushort.Parse(jValueText);
            //}
            //else if
            //    (
            //        parameter.MySqlDbType == MySqlDbType.UInt24
            //        ||
            //        parameter.MySqlDbType == MySqlDbType.UInt32
            //    )
            //{
            //    r = uint.Parse(jValueText);
            //}
            //else if
            //    (
            //        parameter.MySqlDbType == MySqlDbType.UInt64
            //    )
            //{
            //    r = ulong.Parse(jValueText);
            //}
            //else if
            //   (
            //       parameter.MySqlDbType == MySqlDbType.Int16
            //   )
            //{
            //    r = short.Parse(jValueText);
            //}
            //else if
            //   (
            //        parameter.MySqlDbType == MySqlDbType.Int24
            //        ||
            //        parameter.MySqlDbType == MySqlDbType.Int32
            //   )
            //{
            //    r = int.Parse(jValueText);

            //}
            else if
               (
                    parameter.NpgsqlDbType == NpgsqlDbType.Bigint
               )
            {
                r = long.Parse(jValueText);
            }
            return r;

        }

        public static List<NpgsqlParameter> GenerateExecuteParameters
                                (
                                    string connectionString
                                    , string storeProcedureName
                                    , JToken inputsParameters
                                )
        {
            var result
                    = SqlHelper
                        .GenerateStoreProcedureExecuteParameters
                            <NpgsqlConnection, NpgsqlCommand, NpgsqlParameter>
                                (
                                    connectionString
                                    , storeProcedureName
                                    , inputsParameters
                                    , onQueryDefinitionsSetInputParameterProcessFunc
                                    , onQueryDefinitionsSetReturnParameterProcessFunc
                                    , onQueryDefinitionsReadOneDbParameterProcessFunc
                                    , onExecutingSetDbParameterTypeProcessFunc
                                    , onExecutingSetDbParameterValueProcessFunc
                                );
            return result;
        }
        public static JToken StoreProcedureExecute
                               (
                                   NpgsqlConnection connection
                                   , string storeProcedureName
                                   , string p = null //string.Empty
                                   , int commandTimeout = 90
                               )
        {
            JToken inputsParameters = JObject.Parse(p);
            return
                StoreProcedureExecute
                        (
                            connection
                            , storeProcedureName
                            , inputsParameters
                            , commandTimeout
                        );
        }

        public static JToken StoreProcedureExecute
                                (
                                    NpgsqlConnection connection
                                    , string storeProcedureName
                                    , JToken inputsParameters = null //string.Empty
                                    , int commandTimeout = 90
                                )
        {
            var r = SqlHelper
                        .StoreProcedureExecute
                            <NpgsqlConnection, NpgsqlCommand, NpgsqlParameter>
                                (
                                    connection
                                    , storeProcedureName
                                    , onQueryDefinitionsSetInputParameterProcessFunc
                                    , onQueryDefinitionsSetReturnParameterProcessFunc
                                    , onQueryDefinitionsReadOneDbParameterProcessFunc
                                    , onExecutingSetDbParameterTypeProcessFunc
                                    , onExecutingSetDbParameterValueProcessFunc
                                    , inputsParameters
                                    , commandTimeout
                                );
            return r;
        }
        public static void
                RefreshCachedStoreProcedureExecuted
                                (
                                    NpgsqlConnection connection
                                    , string storeProcedureName
                                )
        {
            SqlHelper
                    .RefreshCachedStoreProcedureExecuted
                            (
                                connection
                                , storeProcedureName
                            );
        }
    }
}
#endif