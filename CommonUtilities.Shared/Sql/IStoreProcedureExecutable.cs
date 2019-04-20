#if !NETFRAMEWORK4_6_X
namespace Microshaoft
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Data;
    using System.Threading.Tasks;

    public delegate
                (
                                bool NeedDefaultProcess
                                , JProperty Field   //  JObject Field 对象
                )
                    OnReadRowColumnProcessFunc
                    (
                        IDataReader dataReader
                        , Type fieldType
                        , string fieldName
                        , int rowIndex
                        , int columnIndex
                    );
    public class StoreProcedureHasInfo
    {
        public bool Has;
        public int StatusCode;
        public string HttpMethod;
        public string Message;
        public string ConnectionString;
        public string DataBaseType;
        public string StoreProcedureName;
        public int CommandTimeoutInSeconds;
        public bool EnableStatistics;


    }


    public interface IStoreProcedureExecutable
    {
        string DataBaseType
        {
            get;
        }
        (bool Success, JToken Result) Execute
                (
                    string connectionString
                    , string storeProcedureName
                    , JToken parameters = null
                    , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                    , bool enableStatistics = false
                    , int commandTimeoutInSeconds = 90
                );
        Task<(bool Success,JToken Result)> ExecuteAsync
                (
                    string connectionString
                    , string storeProcedureName
                    , JToken parameters = null
                    , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                    , bool enableStatistics = false
                    , int commandTimeoutInSeconds = 90
                );
    }
    public interface IParametersDefinitionCacheAutoRefreshable
    {
        int CachedParametersDefinitionExpiredInSeconds
        {
            get;
            set;
        }
        bool NeedAutoRefreshExecutedTimeForSlideExpire
        {
            get;
            set;
        }
    }
}
#endif