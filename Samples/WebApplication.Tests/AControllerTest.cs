// <copyright file="AControllerTest.cs">Copyright ©  2016</copyright>
using System;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApplication.Controllers.Versioned;

namespace WebApplication.Controllers.Versioned.Tests
{
    /// <summary>This class contains parameterized unit tests for AController</summary>
    [PexClass(typeof(AController))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class AControllerTest
    {
        /// <summary>Test stub for Get2(Int32)</summary>
        [PexMethod]
        public string Get2Test([PexAssumeUnderTest]AController target, int id)
        {
            string result = target.Get2(id);
            return result;
            // TODO: add assertions to method AControllerTest.Get2Test(AController, Int32)
        }

        /// <summary>Test stub for Get(Int32)</summary>
        [PexMethod]
        public string GetTest([PexAssumeUnderTest]AController target, int id)
        {
            string result = target.Get(id);
            return result;
            // TODO: add assertions to method AControllerTest.GetTest(AController, Int32)
        }
    }
}
