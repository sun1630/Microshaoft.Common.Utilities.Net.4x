﻿#if NETFRAMEWORK4_X
namespace Microshaoft.WorkFlows.Activities
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Activities;

    public abstract class AbstractJTokenWrapperIoActivity : NativeActivity<JTokenWrapper>
    {
        [RequiredArgument]
        public InArgument<JTokenWrapper> Inputs { get; set; }

        public abstract JTokenWrapper OnExecuteProcess(NativeActivityContext context);
        

        protected override void Execute(NativeActivityContext context)
        {
            var inputs = Inputs.Get(context);
            JObject jObject = inputs.TokenAs<JObject>();
            var bookmarkName = string.Empty;
            if
                (
                    jObject
                        .TryGetValue
                            (
                                "bookmarkName"
                                , StringComparison.OrdinalIgnoreCase
                                , out var j
                            )
                )
            {
                bookmarkName = j.Value<string>();
            }
            var hasBookmark = bookmarkName.IsNullOrEmptyOrWhiteSpace();
            if (hasBookmark)
            {
                context
                        .CreateBookmark
                            (
                                bookmarkName
                                , new BookmarkCallback
                                    (
                                        (x, y, z) =>
                                        {
                                            var result = OnResumeBookmarkProcess(x, y);
                                            Result
                                                .Set
                                                    (
                                                        context
                                                        , result
                                                    );

                                        }
                                    )
                            );
            }
            else
            {
                var result = OnExecuteProcess(context);
                Result
                    .Set
                        (
                            context
                            , result
                        );
            }
        }

        // NativeActivity derived activities that do asynchronous operations by calling 
        // one of the CreateBookmark overloads defined on System.Activities.NativeActivityContext 
        // must override the CanInduceIdle property and return true.
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        public abstract JTokenWrapper OnResumeBookmarkProcess
                                    (
                                        NativeActivityContext context
                                        , Bookmark bookmark
                                      //  , object state
                                    );
    }
}
#endif