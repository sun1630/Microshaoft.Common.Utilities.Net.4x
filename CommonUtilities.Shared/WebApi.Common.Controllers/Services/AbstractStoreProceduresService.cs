#if NETCOREAPP2_X
namespace Microshaoft.Web
{
    using Microshaoft;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public interface IStoreProceduresService
    {
        (bool Success, JToken Result)
               Process
                       (
                           string connectionString
                           , string dataBaseType
                           , string storeProcedureName
                           , JToken parameters = null
                           , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                           , bool enableStatistics = false
                           , int commandTimeoutInSeconds = 90
                       );
        Task<(bool Success, JToken Result)>
               ProcessAsync
                       (
                           string connectionString
                           , string dataBaseType
                           , string storeProcedureName
                           , JToken parameters = null
                           , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                           , bool enableStatistics = false
                           , int commandTimeoutInSeconds = 90
                       );
    }




    public interface IStoreProceduresWebApiService
    {
        (
            int StatusCode
            , string Message
            , JToken Result
        )
            ProcessByRoute
                (
                    string routeName
                    , JToken parameters = null
                    , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                    , string httpMethod = "Get"
                    //, bool enableStatistics = false
                    , int commandTimeoutInSeconds = 101
                );
    }
    public abstract class
                AbstractStoreProceduresService
                                : IStoreProceduresWebApiService, IStoreProceduresService
    {
        private class StoreProcedureComparer
                        : IEqualityComparer<IStoreProcedureExecutable>
        {
            public bool Equals
                            (
                                IStoreProcedureExecutable x
                                , IStoreProcedureExecutable y
                            )
            {
                return
                    (x.DataBaseType == y.DataBaseType);
            }
            public int GetHashCode(IStoreProcedureExecutable obj)
            {
                return -1;
            }
        }
        private static object _locker = new object();
        protected readonly IConfiguration _configuration;

        public AbstractStoreProceduresService(IConfiguration configuration)
        {
            _configuration = configuration;
            Initialize();
        }
        //for override from derived class
        public virtual void Initialize()
        {
            _cachedParametersDefinitionExpiredInSeconds =
                _configuration
                        .GetValue<int>("CachedParametersDefinitionExpiredInSeconds");
            _needAutoRefreshExecutedTimeForSlideExpire =
                _configuration
                        .GetValue<bool>("NeedAutoRefreshExecutedTimeForSlideExpire");
            LoadDynamicExecutors();
        }
        protected virtual string[] GetDynamicExecutorsPathsProcess()
        {
            var result =
                    _configuration
                        .GetSection("DynamicExecutorsPaths")
                        .AsEnumerable()
                        .Select
                            (
                                (x) =>
                                {
                                    return
                                        x.Value;
                                }
                            )
                        .ToArray();
            return result;
        }

        protected virtual void LoadDynamicExecutors
                        (
                        //string dynamicLoadExecutorsPathsJsonFile = "dynamicCompositionPluginsPaths.json"
                        )
        {
            var executingDirectory = Path
                                        .GetDirectoryName
                                                (
                                                    Assembly
                                                        .GetExecutingAssembly()
                                                        .Location
                                                );
            var executors =
                    GetDynamicExecutorsPathsProcess
                            (
                            //dynamicLoadExecutorsPathsJsonFile
                            )
                        .Select
                            (
                                (x) =>
                                {
                                    var path = x;
                                    if (!path.IsNullOrEmptyOrWhiteSpace())
                                    {
                                        if
                                            (
                                                x.StartsWith(".")
                                            )
                                        {
                                            path = path.TrimStart('.', '\\', '/');
                                        }
                                        path = Path.Combine
                                                        (
                                                            executingDirectory
                                                            , path
                                                        );
                                    }
                                    return path;
                                }
                            )
                        .Where
                            (
                                (x) =>
                                {
                                    return
                                        (
                                            !x
                                                .IsNullOrEmptyOrWhiteSpace()
                                            &&
                                            Directory
                                                .Exists(x)
                                        );
                                }
                            )
                        .SelectMany
                            (
                                (x) =>
                                {
                                    var r = CompositionHelper
                                                .ImportManyExportsComposeParts
                                                    <IStoreProcedureExecutable>
                                                        (x);
                                    return r;
                                }
                            );
            var indexedExecutors =
                    executors
                        .Distinct
                            (
                                 new StoreProcedureComparer()
                            )
                        .ToDictionary
                            (
                                (x) =>
                                {
                                    return
                                        x.DataBaseType;
                                }
                                ,
                                (x) =>
                                {
                                    IParametersDefinitionCacheAutoRefreshable
                                        rr = x as IParametersDefinitionCacheAutoRefreshable;
                                    if (rr != null)
                                    {
                                        rr
                                            .CachedParametersDefinitionExpiredInSeconds
                                                = CachedParametersDefinitionExpiredInSeconds;
                                        rr
                                            .NeedAutoRefreshExecutedTimeForSlideExpire
                                                = NeedAutoRefreshExecutedTimeForSlideExpire;
                                    }
                                    return x;
                                }
                                , StringComparer
                                        .OrdinalIgnoreCase
                            );
            _locker
                .LockIf
                    (
                        () =>
                        {
                            var r = (_indexedExecutors == null);
                            return r;
                        }
                        , () =>
                        {
                            _indexedExecutors = indexedExecutors;
                        }
                    );
        }
        private int _cachedParametersDefinitionExpiredInSeconds = 3600;
        protected virtual int CachedParametersDefinitionExpiredInSeconds
        {
            get => _cachedParametersDefinitionExpiredInSeconds;
            private set => _cachedParametersDefinitionExpiredInSeconds = value;
        }
        private bool _needAutoRefreshExecutedTimeForSlideExpire = true;
        protected virtual bool NeedAutoRefreshExecutedTimeForSlideExpire
        {
            get => _needAutoRefreshExecutedTimeForSlideExpire;
            private set => _needAutoRefreshExecutedTimeForSlideExpire = value;
        }
        private IDictionary<string, IStoreProcedureExecutable>
                                _indexedExecutors;

        public 
            (int StatusCode, string Message, JToken Result)
                    ProcessByRoute
                        (
                            string routeName
                            , JToken parameters = null
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            , string httpMethod = "Get"
                            , int commandTimeoutInSeconds = 101
                        )
        {
           

            JToken result = null;
            var statusCode = 200;
            var message = string.Empty;

            void exec(StoreProcedureHasInfo h, ref JToken r)
            {
                var rr = Process
                            (
                                h.ConnectionString
                                , h.DataBaseType
                                , h.StoreProcedureName
                                , parameters
                                , onReadRowColumnProcessFunc
                                , h.EnableStatistics
                                , commandTimeoutInSeconds
                            );
                r = rr.Result;
                result = rr.Result;
            }
            InvokeProcessByRoute
                (
                    routeName
                    , exec
                    , parameters
                    , onReadRowColumnProcessFunc
                    , httpMethod
                    , commandTimeoutInSeconds

                );
             return
                 (
                     statusCode
                     , message
                     , result
                 );
        }


        public
            async
                Task<(int StatusCode, string Message, JToken Result)>
                    ProcessByRouteAsync
                        (
                            string routeName
                            , JToken parameters = null
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            , string httpMethod = "Get"
                            , int commandTimeoutInSeconds = 101
                        )
        {
            JToken result = null;
            var statusCode = 200;
            var message = string.Empty;
            InvokeProcessByRoute
                (
                    routeName
                    , null
                    //async (has, r) =>
                    //{
                    //    var rrr = Process
                    //        (
                    //            has.ConnectionString
                    //            , has.DataBaseType
                    //            , has.StoreProcedureName
                    //            , parameters
                    //            , onReadRowColumnProcessFunc
                    //            , has.EnableStatistics
                    //            , commandTimeoutInSeconds
                    //        );
                    //    result = rrr.Result;
                    //}
                    , parameters
                    , onReadRowColumnProcessFunc
                    , httpMethod
                    , commandTimeoutInSeconds

                );
            return
                (
                    statusCode
                    , message
                    , result
                );
        }


        private
                //(
                //    int StatusCode
                //    , string Message
                //    , JToken Result
                //)
                void
                    InvokeProcessByRoute
                        (
                            string routeName
                            , MethodRefParametersInvokingHandler<StoreProcedureHasInfo, JToken> onProcessing
                            , JToken parameters = null
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            , string httpMethod = "Get"
                            //, bool enableStatistics = false
                            , int commandTimeoutInSeconds = 101
                        )
        {
         

            JToken result = null;
            var statusCode = 200;
            var message = string.Empty;
            var has = TryGetStoreProcedureInfo
                        (
                            routeName
                            , httpMethod
                        );
            if
                (
                    has.Has
                    &&
                    has.StatusCode == 200
                )
            {
                //var success = Process
                //                (
                //                    has.ConnectionString
                //                    , has.DataBaseType
                //                    , has.StoreProcedureName
                //                    , out result
                //                    , parameters
                //                    , onReadRowColumnProcessFunc
                //                    , has.EnableStatistics
                //                    , has.CommandTimeoutInSeconds
                //                );
                var success = false;
                onProcessing
                    (
                        has
                        , ref result
                     
                    );
                var jObject = result
                                    ["Outputs"]
                                    ["Parameters"] as JObject;
                if (jObject != null)
                {
                    JToken jv = null;
                    if
                        (
                            jObject
                                .TryGetValue
                                    (
                                        "HttpResponseStatusCode"
                                        , StringComparison
                                                .OrdinalIgnoreCase
                                        , out jv
                                    )
                        )
                    {
                        statusCode = jv.Value<int>();
                    }
                    jv = null;
                    if
                        (
                            jObject
                                .TryGetValue
                                    (
                                        "HttpResponseMessage"
                                        , StringComparison
                                                .OrdinalIgnoreCase
                                        , out jv
                                    )
                        )
                    {
                        message = jv.Value<string>();
                    }
                }
                if (success)
                {
                    //support custom output nest json by JSONPath in JsonFile Config
                    var outputsConfiguration = _configuration
                                                    .GetSection
                                                        ($"Routes:{routeName}:{has.HttpMethod}:Outputs");
                    if (outputsConfiguration.Exists())
                    {
                        var mappings = outputsConfiguration
                                            .GetChildren()
                                            .Select
                                                (
                                                    (x) =>
                                                    {
                                                        (
                                                            string TargetJPath
                                                            , string SourceJPath
                                                        )
                                                            r =
                                                                (
                                                                    x.Key
                                                                   , x.Get<string>()
                                                                );
                                                        return r;
                                                    }
                                                );
                        result = result
                                    .MapToNew
                                        (
                                            mappings
                                        );
                    }
                }
            }
            else
            {
                statusCode = has.StatusCode;
                message = has.Message;
            }
            //return
            //    (
            //        statusCode
            //        , message
            //        , result
            //    );
        }

        private void
            InvokeProcess
                (
                    string connectionString
                    , string dataBaseType
                    , string storeProcedureName
                    , MethodRefParametersInvokingHandler<IStoreProcedureExecutable, JToken> onExecuting
                    , JToken parameters = null
                    , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                    , bool enableStatistics = false
                    , int commandTimeoutInSeconds = 90
                )
        {
            var r = false;
            JToken result = null;
            var beginTime = DateTime.Now;
            IStoreProcedureExecutable executor = null;
            r = _indexedExecutors
                        .TryGetValue
                            (
                                dataBaseType
                                , out executor
                            );
            if (r)
            {
                //r = executor
                //        .Execute
                //            (
                //                connectionString
                //                , storeProcedureName
                //                , out result
                //                , parameters
                //                , onReadRowColumnProcessFunc
                //                , enableStatistics
                //                , commandTimeoutInSeconds
                //            );
                onExecuting(executor, ref result);
            }
            if (!r)
            {
                result = null;
                return;// r;
            }
            result["BeginTime"] = beginTime;
            var endTime = DateTime.Now;
            result["EndTime"] = endTime;
            result["DurationInMilliseconds"]
                    = DateTimeHelper
                            .MillisecondsDiff
                                    (
                                        beginTime
                                        , endTime
                                    );
            //return r;
        }


        public virtual 
                (
                    bool Success
                    , JToken Result
                )
                Process
                        (
                            string connectionString
                            , string dataBaseType
                            , string storeProcedureName
                            , JToken parameters = null
                            , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                            , bool enableStatistics = false
                            , int commandTimeoutInSeconds = 90
                        )
        {
            (
                bool Success
                , JToken Result
            ) r = ( Success: false, Result : null);

            void exec(IStoreProcedureExecutable executor, ref JToken result)
            {
                var rr = executor
                                        .Execute
                                            (
                                                connectionString
                                                , storeProcedureName
                                                , parameters
                                                , onReadRowColumnProcessFunc
                                                , enableStatistics
                                                , commandTimeoutInSeconds
                                            );
                r.Success = rr.Success;
                r.Result = rr.Result;
                result = rr.Result;
            }


            InvokeProcess
                    (
                        connectionString
                        , dataBaseType
                        , storeProcedureName
                        , exec
                        , parameters
                        , onReadRowColumnProcessFunc
                        , enableStatistics
                        , commandTimeoutInSeconds

                    );
            return r;

            
        }

        public virtual async
                Task
                    <
                        (
                            bool Success
                            , JToken Result
                        )
                    >
        ProcessAsync
                (
                    string connectionString
                    , string dataBaseType
                    , string storeProcedureName
                    //, out JToken result
                    , JToken parameters = null
                    , OnReadRowColumnProcessFunc onReadRowColumnProcessFunc = null
                    , bool enableStatistics = false
                    , int commandTimeoutInSeconds = 90
                )
        {
            (
                 bool Success
                 , JToken Result
             ) r = (Success: false, Result: null);

            InvokeProcess
                    (
                        connectionString
                        , dataBaseType
                        , storeProcedureName
                        , null
                        //async (executor) =>
                        //{
                        //    var rr = await executor
                        //                .ExecuteAsync
                        //                        (
                        //                            connectionString
                        //                            , storeProcedureName
                        //                            , parameters
                        //                            , onReadRowColumnProcessFunc
                        //                            , enableStatistics
                        //                            , commandTimeoutInSeconds
                        //                        );
                        //    r.Success = rr.Success;
                        //    r.Result = rr.Result;
                        //}
                        , parameters
                        , onReadRowColumnProcessFunc
                        , enableStatistics
                        , commandTimeoutInSeconds

                    );
            return r;
        }


        protected virtual
            StoreProcedureHasInfo
            TryGetStoreProcedureInfo
                        (
                            string routeName
                            , string httpMethod
                        )
        {
            var success = true;
            var statusCode = 500;
            string message = "ok";
            var connectionString = string.Empty;
            var storeProcedureName = string.Empty;
            var dataBaseType = string.Empty;
            var commandTimeoutInSeconds = 120;

            var enableStatistics = false;
            StoreProcedureHasInfo
                Result()
            {
                return new StoreProcedureHasInfo()
                {
                    Has = success
                       ,
                    StatusCode = statusCode
                       ,
                    HttpMethod = httpMethod
                       ,
                    Message = message
                       ,
                    ConnectionString = connectionString
                       ,
                    DataBaseType = dataBaseType
                       ,
                    StoreProcedureName = storeProcedureName
                       ,
                    CommandTimeoutInSeconds = commandTimeoutInSeconds
                       ,
                    EnableStatistics = enableStatistics
                };
            }
            var routeConfiguration = _configuration
                                            .GetSection($"Routes:{routeName}");
            if (!routeConfiguration.Exists())
            {
                success = false;
                statusCode = 404;
                message = $"{routeName} not found";
            }
            if (!success)
            {
                return Result();
            }
            if
                (
                    !httpMethod
                        .StartsWith
                            (
                                "http"
                                , StringComparison
                                    .OrdinalIgnoreCase
                            )
                )
            {
                httpMethod = "http" + httpMethod;
            }
            var actionConfiguration = routeConfiguration
                                            .GetSection($"{httpMethod}");
            if (!actionConfiguration.Exists())
            {
                success = false;
                statusCode = 403;
                message = $"{httpMethod} verb forbidden";
            }
            if (!success)
            {
                return Result();
            }
            var connectionID = actionConfiguration
                                    .GetValue<string>("ConnectionID");
            success = !connectionID.IsNullOrEmptyOrWhiteSpace();
            if (!success)
            {
                statusCode = 500;
                message = $"Database connectionID error";
                return Result();
            }
            var connectionConfiguration =
                                _configuration
                                        .GetSection($"Connections:{connectionID}");
            connectionString = connectionConfiguration
                                        .GetValue<string>("ConnectionString");
            success = !connectionString.IsNullOrEmptyOrWhiteSpace();
            if (!success)
            {
                statusCode = 500;
                message = $"Database connection string error";
                return Result();
            }
            dataBaseType = connectionConfiguration
                                    .GetValue<string>("DataBaseType");
            if (connectionConfiguration.GetSection("CommandTimeoutInSeconds").Exists())
            {
                commandTimeoutInSeconds = connectionConfiguration.GetValue<int>("CommandTimeoutInSeconds");
            }
            success = !dataBaseType.IsNullOrEmptyOrWhiteSpace();
            if (!success)
            {
                statusCode = 500;
                message = $"Database Type error";
                return Result();
            }
            storeProcedureName = actionConfiguration
                                        .GetValue<string>("StoreProcedureName");
            enableStatistics = connectionConfiguration
                                        .GetValue<bool>("EnableStatistics");
            if (enableStatistics)
            {
                if (actionConfiguration.GetSection("EnableStatistics").Exists())
                {
                    enableStatistics = actionConfiguration
                                            .GetValue<bool>("EnableStatistics");
                }
            }
            if (actionConfiguration.GetSection("CommandTimeoutInSeconds").Exists())
            {
                commandTimeoutInSeconds = actionConfiguration
                                                .GetValue<int>("CommandtimeoutInSeconds");
            }
            success = !storeProcedureName.IsNullOrEmptyOrWhiteSpace();
            if (!success)
            {
                statusCode = 500;
                message = $"Database StoreProcedure Name error";
                return Result();
            }
            //success = true;
            statusCode = 200;
            return Result();
        }

        
    }
}
#endif