//namespace Microshaoft
//{
//    using System.Collections.Generic;
//    using System.Data;
//    using System.Linq;
//    using System;
//    using Newtonsoft.Json.Linq;
//    using System.Data.SqlClient;
//    using System.Data.Common;
//    using System.Threading.Tasks;

//    public static partial class DataReaderHelper
//    {
//        public static IEnumerable<JToken> GetRowsJTokensEnumerable
//                     (
//                         this DbDataReader target
//                         , JArray columns = null
//                         , Func
//                                <
//                                    IDataReader
//                                    , Type        // fieldType
//                                    , string    // fieldName
//                                    , int       // row index
//                                    , int       // column index
//                                    ,
//                                        (
//                                            bool needDefaultProcess
//                                            , JProperty field   //  JObject Field 对象
//                                        )
//                                > onReadRowColumnProcessFunc = null
//                     )
//        {
//            GetRowsJTokensEnumerable
//                    (
//                        target
//                        , () =>
//                        {
//                            return
//                                target.Read();
//                        }
//                        , (x) =>
//                        {
//                            yield
//                                return
                                    

//                        }
                
//                    )
//        }

//    }
//}