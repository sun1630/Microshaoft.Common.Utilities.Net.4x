﻿#if !NETFRAMEWORK4_X && NETCOREAPP2_X
namespace Microshaoft.WebApi.Controllers
{
    using Microshaoft;
    using Microshaoft.Web;
    using Microshaoft.WebApi.ModelBinders;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;

    public abstract partial class 
                AbstractStoreProceduresExecutorControllerBase
                    :
                        ControllerBase
    {

        private readonly Regex _digitsRegex = new Regex(@"^\d+$");
        private readonly byte[] _utf8HeaderBytes = new byte[]
                                                        {
                                                            0xEF
                                                            , 0xBB
                                                            , 0xBF
                                                        };
        private readonly CsvFormatterOptions _csvFormatterOptions;
        //= new CsvFormatterOptions() 
        //{
        //    CsvColumnsDelimiter = ","
        //    , UseSingleLineHeaderInCsv = true
        //    , IncludeExcelDelimiterHeader = false
        //    , DigitsTextSuffix = "\t"
        //};

        private string GetFieldValue(IDataReader reader, int fieldIndex)
        {
            var @value = string.Empty;
            //if (jToken != null)
            {
                var fieldType = reader.GetFieldType(fieldIndex);

                if (fieldType == typeof(DateTime))
                {
                    //@value = ((DateTime) jValue).ToString("yyyy-MM-ddTHH:mm:ss.fffff");
                    var x = reader.GetDateTime(fieldIndex);
                    @value = $@"""{x.ToString(_csvFormatterOptions.DateTimeFormat)}""";
                }
                else
                {
                    @value = reader.GetValue(fieldIndex).ToString();
                    @value = @value.Replace(@"""", @"""""");
                    if (fieldType == typeof(string))
                    {
                        if (!string.IsNullOrEmpty(_csvFormatterOptions.DigitsTextSuffix))
                        {
                            if (_digitsRegex.IsMatch(@value))
                            {
                                //避免在Excel中csv文本数字自动变科学计数法
                                @value += _csvFormatterOptions.DigitsTextSuffix;
                                //@value = $@"=""{@value}""";
                            }
                        }
                    }
                    //Check if the value contains a delimiter and place it in quotes if so
                    if
                        (
                            @value.Contains(_csvFormatterOptions.CsvColumnsDelimiter)
                            ||
                            @value.Contains("\r")
                            ||
                            @value.Contains("\n")
                        )
                    {
                        @value = $@"""{@value}""";
                    }
                }
            }
            return @value;
        }

        [HttpDelete]
        [HttpGet]
        [HttpHead]
        [HttpOptions]
        [HttpPatch]
        [HttpPost]
        [HttpPut]
        [
             Route
                 (
                     "bigdataexport/{routeName}/"
                 )
        ]
        [OperationsAuthorizeFilter(false)]
        [RequestJTokenParametersDefaultProcessFilter]
        public async Task
                             ProcessActionRequest
                                 (
                                     [FromRoute]
                                        string routeName
                                     , [ModelBinder(typeof(JTokenModelBinder))]
                                        JToken parameters = null
                                 )
        {
            var request = HttpContext
                                .Request;
            var httpMethod = $"http{request.Method}";
            var encodingName = (string)request.Query["e"];
            Encoding e = null;
            if (!encodingName.IsNullOrEmptyOrWhiteSpace())
            {
                e = Encoding
                        .GetEncoding(encodingName);
            }
            else
            {
                e = Encoding.UTF8;
            }

            var response = HttpContext
                                   .Response;
            var downloadFileName = $"{routeName}.csv";
            var downloadFileNameConfiguration =
                    _configuration
                            .GetSection
                                (
                                    $"Routes:{routeName}:{httpMethod}:Exporting:DownloadFileName"
                                );
            if (downloadFileNameConfiguration.Exists())
            {
                downloadFileName = downloadFileNameConfiguration.Value;
            }
            downloadFileName = HttpUtility.UrlEncode(downloadFileName, e);
            response
                    .Headers
                    .Add
                        (
                            "Content-Disposition"
                            , $@"attachment; filename=""{downloadFileName}"""
                        );
            using
                (
                    var streamWriter = new StreamWriter
                                                (
                                                    response.Body
                                                    , e
                                                )
                )
            {
                if (e.GetType() == Encoding.UTF8.GetType())
                {
                    await
                        response
                            .Body
                            .WriteAsync
                                (
                                    _utf8HeaderBytes
                                );
                }
                if (_csvFormatterOptions.IncludeExcelDelimiterHeader)
                {
                    //乱码
                    await
                        streamWriter
                            .WriteLineAsync
                                (
                                    $"sep ={_csvFormatterOptions.CsvColumnsDelimiter}"
                                );
                }
                var outputColumnsConfiguration =
                    _configuration
                            .GetSection
                                (
                                    $"Routes:{routeName}:{httpMethod}:Exporting:OutputColumns"
                                );

                (
                    string ColumnName
                    , string ColumnTitle
                )[] outputColumns = null;
                if (outputColumnsConfiguration.Exists())
                {
                    outputColumns = outputColumnsConfiguration
                                        .GetChildren()
                                        .Select
                                            (
                                                (x) =>
                                                {
                                                    var columnName = x
                                                                        .GetValue<string>
                                                                                ("ColumnName");
                                                    var columnTitle = x
                                                                        .GetValue
                                                                                (
                                                                                    "ColumnTitle"
                                                                                    , columnName
                                                                                );
                                                    return
                                                        (
                                                            ColumnName: columnName
                                                            , ColumnTitle: columnTitle
                                                        );
                                                }
                                            )
                                        .ToArray();
                    if (_csvFormatterOptions.UseSingleLineHeaderInCsv)
                    {
                        var j = 0;
                        var columnsHeaderLine = outputColumns
                                                        .Aggregate
                                                            (
                                                                string.Empty
                                                                , (x, y) =>
                                                                {
                                                                    if (j > 0)
                                                                    {
                                                                        x += _csvFormatterOptions
                                                                                        .CsvColumnsDelimiter;
                                                                    }
                                                                    x += y.ColumnTitle;
                                                                    j++;
                                                                    return
                                                                            x;
                                                                }
                                                            );
                        await
                            streamWriter
                                .WriteLineAsync
                                        (
                                            columnsHeaderLine
                                        );
                    }
                }
                _service
                    .RowsProcess
                        (
                            routeName
                            , parameters
                            , (resultSetIndex, reader, columns, rowIndex) =>
                            {
                                if (rowIndex == 0)
                                {
                                    if (_csvFormatterOptions.UseSingleLineHeaderInCsv)
                                    {
                                        if (outputColumns == null)
                                        {
                                            var j = 0;
                                            var columnsHeaderLine = columns
                                                                        .Aggregate
                                                                            (
                                                                                string.Empty
                                                                                , (x, y) =>
                                                                                {
                                                                                    if (j > 0)
                                                                                    {
                                                                                        x += _csvFormatterOptions
                                                                                                .CsvColumnsDelimiter;
                                                                                    }
                                                                                    x += y["ColumnName"].ToString();
                                                                                    j++;
                                                                                    return
                                                                                            x;
                                                                                }
                                                                            );
                                            //await
                                            streamWriter
                                                    .WriteLine
                                                            (
                                                                columnsHeaderLine
                                                            );
                                        }
                                    }
                                }
                                string line = string.Empty;
                                if (outputColumns == null)
                                {
                                    var fieldsCount = reader.FieldCount;
                                    for (var fieldIndex = 0; fieldIndex < fieldsCount; fieldIndex++)
                                    {
                                        if (fieldIndex > 0)
                                        {
                                            line += _csvFormatterOptions.CsvColumnsDelimiter;
                                        }
                                        line += GetFieldValue(reader, fieldIndex);
                                    }
                                }
                                else
                                {
                                    var j = 0;
                                    foreach (var (columnName, columnTitle) in outputColumns)
                                    {
                                        if (j > 0)
                                        {
                                            line += _csvFormatterOptions.CsvColumnsDelimiter;
                                        }
                                        if
                                            (
                                                columns
                                                    .Any
                                                        (
                                                            (x) =>
                                                            {
                                                                return
                                                                    string
                                                                        .Compare
                                                                            (
                                                                                x["ColumnName"].ToString()
                                                                                , columnName
                                                                                , true
                                                                            ) == 0;
                                                                        
                                                            }

                                                        )
                                            )
                                        {
                                            var fieldIndex = reader.GetOrdinal(columnName);
                                            line += GetFieldValue(reader, fieldIndex);
                                        }
                                        j ++;
                                    }
                                }
                                //await
                                streamWriter
                                    .WriteLineAsync(line);
                                //i++;
                            }
                            , Request.Method
                            //, 102
                        );
            }
        }
    }
}
#endif