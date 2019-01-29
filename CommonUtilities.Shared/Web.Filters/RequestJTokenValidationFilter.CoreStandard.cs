﻿#if NETCOREAPP2_X
namespace Microshaoft.Web
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public interface IJTokenModelParameterValidator
    {
        string Name { get; }


        (bool IsValid, JsonResult result) Validate(JToken parameters);

    }

    public class ValidateModelFilterAttribute
                    :
                        //AuthorizeAttribute
                        Attribute
                        , IActionFilter
    {
        private object _locker = new object();
        private readonly IConfiguration _configuration;
        private IDictionary<string, IJTokenModelParameterValidator>
                        _indexedValidators;
        public ValidateModelFilterAttribute(IConfiguration configuration)
        {
            _configuration = configuration;
            Initialize();
        }
        public virtual void Initialize()
        {
            //InstanceID = Interlocked.Increment(ref InstancesSeed);
            LoadDynamicExecutors();


        }
        protected virtual string[] GetDynamicLoadExecutorsPathsProcess()
        {


            var result =
                    _configuration
                        .GetSection("DynamicLoadExecutorsPaths")
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
                           string dynamicLoadExecutorsPathsJsonFile = "dynamicLoadExecutorsPaths.json"
                        )
        {
            var executingDirectory = Path
                                        .GetDirectoryName
                                                (
                                                    Assembly
                                                        .GetExecutingAssembly()
                                                        .Location
                                                );
            var validators =
                    GetDynamicLoadExecutorsPathsProcess
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
                                    var r =
                                        CompositionHelper
                                            .ImportManyExportsComposeParts
                                                <IJTokenModelParameterValidator>
                                                    (x);
                                    return r;
                                }
                            );
            var indexedValidators =
                    validators
                       
                        .ToDictionary
                            (
                                (x) =>
                                {
                                    return
                                        x.Name;
                                }

                                , StringComparer
                                        .OrdinalIgnoreCase
                            );
            _locker
                .LockIf
                    (
                        () =>
                        {
                            var r = (_indexedValidators == null);
                            return r;
                        }
                        , () =>
                        {
                            _indexedValidators = indexedValidators;
                        }
                    );
        }
        public virtual void OnActionExecuting(ActionExecutingContext context)
        {
            (bool IsValid, JsonResult Result)
                    r = (IsValid: true, null);

            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            var routeName = (string)context.ActionArguments["routeName"];
            var httpMethod = $"http{request.Method}";

            var validatorConfiguration =
                                _configuration
                                        .GetSection($"Routes:{routeName}:{httpMethod}:Validator");
            if (validatorConfiguration.Exists())
            {
                var validatorName = validatorConfiguration.Value;
                var parameters = context.ActionArguments["parameters"] as JToken;
                var rr = _indexedValidators
                                       .TryGetValue
                                               (
                                                   validatorName
                                                   , out var validator
                                               );

                if (rr)
                {
                    r = validator.Validate(parameters);
                }
                else
                {
                    r.IsValid = false;
                    r.Result = new JsonResult
                                    (
                                        new
                                        {
                                            StatusCode = 400
                                            ,
                                            Message = "无法效验"
                                        }
                                    )
                    {
                        StatusCode = 400
                                    ,
                        ContentType = "application/json"
                    };
                }
                if (!r.IsValid)
                {
                    context
                        .Result = r.Result;
                }
            }    
        
            
        }
        public virtual void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }
}
#endif