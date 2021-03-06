// <copyright file="PexAssemblyInfo.cs">Copyright ©  2016</copyright>
using Microsoft.Pex.Framework.Coverage;
using Microsoft.Pex.Framework.Creatable;
using Microsoft.Pex.Framework.Instrumentation;
using Microsoft.Pex.Framework.Settings;
using Microsoft.Pex.Framework.Validation;

// Microsoft.Pex.Framework.Settings
[assembly: PexAssemblySettings(TestFramework = "VisualStudioUnitTest")]

// Microsoft.Pex.Framework.Instrumentation
[assembly: PexAssemblyUnderTest("WebApplication")]
[assembly: PexInstrumentAssembly("System.Xml.Linq")]
[assembly: PexInstrumentAssembly("System.Core")]
[assembly: PexInstrumentAssembly("Newtonsoft.Json")]
[assembly: PexInstrumentAssembly("Microsoft.AspNet.Identity.EntityFramework")]
[assembly: PexInstrumentAssembly("Microshaoft.WebApi.Utilities.Lite.Net.4.6.x")]
[assembly: PexInstrumentAssembly("System.Net.Http.Formatting")]
[assembly: PexInstrumentAssembly("System.Web.Http.WebHost")]
[assembly: PexInstrumentAssembly("System.Web.Http.Owin")]
[assembly: PexInstrumentAssembly("Microsoft.CSharp")]
[assembly: PexInstrumentAssembly("System.Runtime.Serialization")]
[assembly: PexInstrumentAssembly("EntityFramework")]
[assembly: PexInstrumentAssembly("Microsoft.Owin.Security.Cookies")]
[assembly: PexInstrumentAssembly("Microsoft.AspNet.Identity.Core")]
[assembly: PexInstrumentAssembly("Microsoft.AspNet.Identity.Owin")]
[assembly: PexInstrumentAssembly("Microsoft.Owin.Security")]
[assembly: PexInstrumentAssembly("Microsoft.Owin")]
[assembly: PexInstrumentAssembly("System.Web.Optimization")]
[assembly: PexInstrumentAssembly("System.Web.Mvc")]
[assembly: PexInstrumentAssembly("System.Web.Http")]
[assembly: PexInstrumentAssembly("System.Net.Http")]
[assembly: PexInstrumentAssembly("System.ComponentModel.DataAnnotations")]
[assembly: PexInstrumentAssembly("System.Web")]
[assembly: PexInstrumentAssembly("Microsoft.Owin.Security.OAuth")]
[assembly: PexInstrumentAssembly("Owin")]

// Microsoft.Pex.Framework.Creatable
[assembly: PexCreatableFactoryForDelegates]

// Microsoft.Pex.Framework.Validation
[assembly: PexAllowedContractRequiresFailureAtTypeUnderTestSurface]
[assembly: PexAllowedXmlDocumentedException]

// Microsoft.Pex.Framework.Coverage
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Xml.Linq")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Core")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Newtonsoft.Json")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.AspNet.Identity.EntityFramework")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microshaoft.WebApi.Utilities.Lite.Net.4.6.x")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Net.Http.Formatting")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Web.Http.WebHost")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Web.Http.Owin")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.CSharp")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Runtime.Serialization")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "EntityFramework")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.Owin.Security.Cookies")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.AspNet.Identity.Core")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.AspNet.Identity.Owin")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.Owin.Security")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.Owin")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Web.Optimization")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Web.Mvc")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Web.Http")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Net.Http")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.ComponentModel.DataAnnotations")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "System.Web")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Microsoft.Owin.Security.OAuth")]
[assembly: PexCoverageFilterAssembly(PexCoverageDomain.UserOrTestCode, "Owin")]

