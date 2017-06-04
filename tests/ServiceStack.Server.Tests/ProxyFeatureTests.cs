﻿using System;
using System.IO;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Server.Tests
{
    [TestFixture]
    public class ProxyFeatureTests
    {
        private static string ListeningOn = "http://localhost:20000/";
        //private static string ListeningOn = Config.ListeningOn;
        //private static string ListeningOn = "http://localhost:55799/";

        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(ProxyFeatureTests), typeof(ProxyFeatureTests).GetAssembly()) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new ProxyFeature(
                    matchingRequests: req => req.PathInfo.StartsWith("/test"),
                    resolveUrl: req => "http://test.servicestack.net" + req.RawUrl.Replace("/test", "/"))
                {
                    TransformRequest = TransformRequest,
                    TransformResponse = TransformResponse,
                });

                Plugins.Add(new ProxyFeature(
                    matchingRequests: req => req.PathInfo.StartsWith("/techstacks"),
                    resolveUrl: req => "http://techstacks.io" + req.RawUrl.Replace("/techstacks", "/"))
                {
                    TransformRequest = TransformRequest,
                    TransformResponse = TransformResponse,
                });

                //Allow this proxy server to issue ss-id/ss-pid Session Cookies
                //Plugins.Add(new SessionFeature());
            }

            private Stream TransformRequest(IHttpRequest req, Stream reqStream)
            {
                var reqReplace = req.QueryString["reqReplace"];
                if (reqReplace != null)
                {
                    var reqBody = reqStream.ReadFully().FromUtf8Bytes();
                    var parts = reqReplace.SplitOnFirst(',');
                    var replacedBody = reqBody.Replace(parts[0], parts[1]);
                    return MemoryStreamFactory.GetStream(replacedBody.ToUtf8Bytes());
                }
                return reqStream;
            }

            private Stream TransformResponse(IHttpResponse res, Stream resStream)
            {
                var req = res.Request;
                var resReplace = req.QueryString["resReplace"];
                if (resReplace != null)
                {
                    var reqBody = resStream.ReadFully().FromUtf8Bytes();
                    var parts = resReplace.SplitOnFirst(',');
                    var replacedBody = reqBody.Replace(parts[0], parts[1]);
                    return MemoryStreamFactory.GetStream(replacedBody.ToUtf8Bytes());
                }
                return resStream;
            }
        }

        private readonly ServiceStackHost appHost;

        public ProxyFeatureTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        [Route("/echo/types")]
        public partial class EchoTypes
            : IReturn<EchoTypes>
        {
            public virtual byte Byte { get; set; }
            public virtual short Short { get; set; }
            public virtual int Int { get; set; }
            public virtual long Long { get; set; }
            public virtual ushort UShort { get; set; }
            public virtual uint UInt { get; set; }
            public virtual ulong ULong { get; set; }
            public virtual float Float { get; set; }
            public virtual double Double { get; set; }
            public virtual decimal Decimal { get; set; }
            public virtual string String { get; set; }
            public virtual DateTime DateTime { get; set; }
            public virtual TimeSpan TimeSpan { get; set; }
            public virtual DateTimeOffset DateTimeOffset { get; set; }
            public virtual Guid Guid { get; set; }
            public virtual Char Char { get; set; }
        }

        [Test]
        public void Can_proxy_to_test_servicestack()
        {
            var client = new JsonServiceClient(ListeningOn.CombineWith("test"));

            var request = new EchoTypes
            {
                Byte = 1,
                Short = 2,
                Int = 3,
                Long = 4,
                Float = 1.1f,
                String = "foo"
            };
            var response = client.Post(request);

            Assert.That(response.Byte, Is.EqualTo(1));
            Assert.That(response.Short, Is.EqualTo(2));
            Assert.That(response.Int, Is.EqualTo(3));
            Assert.That(response.Long, Is.EqualTo(4));
            Assert.That(response.Float, Is.EqualTo(1.1f));
            Assert.That(response.String, Is.EqualTo("foo"));
        }

        [Test]
        public async Task Can_proxy_to_test_servicestack_Async()
        {
            var client = new JsonHttpClient(ListeningOn.CombineWith("test"));

            var request = new EchoTypes
            {
                Byte = 1,
                Short = 2,
                Int = 3,
                Long = 4,
                Float = 1.1f,
                String = "foo"
            };
            var response = await client.PostAsync(request);

            Assert.That(response.Byte, Is.EqualTo(1));
            Assert.That(response.Short, Is.EqualTo(2));
            Assert.That(response.Int, Is.EqualTo(3));
            Assert.That(response.Long, Is.EqualTo(4));
            Assert.That(response.Float, Is.EqualTo(1.1f));
            Assert.That(response.String, Is.EqualTo("foo"));
        }

        [Test]
        public void Can_TransformRequest_when_proxying_to_test()
        {
            var request = new EchoTypes
            {
                Byte = 1,
                Short = 2,
                Int = 3,
                Long = 4,
                Float = 1.1f,
                String = "foo"
            };

            var url = ListeningOn.CombineWith("test")
                .CombineWith("/echo/types")
                .AddQueryParam("reqReplace", "foo,bar");

            var response = url.PostJsonToUrl(request)
                .FromJson<EchoTypes>();

            Assert.That(response.Byte, Is.EqualTo(1));
            Assert.That(response.Short, Is.EqualTo(2));
            Assert.That(response.Int, Is.EqualTo(3));
            Assert.That(response.Long, Is.EqualTo(4));
            Assert.That(response.Float, Is.EqualTo(1.1f));
            Assert.That(response.String, Is.EqualTo("bar"));
        }

        [Test]
        public void Can_TransformResponse_when_proxying_to_test()
        {
            var request = new EchoTypes
            {
                Byte = 1,
                Short = 2,
                Int = 3,
                Long = 4,
                Float = 1.1f,
                String = "foo"
            };

            var url = ListeningOn.CombineWith("test")
                .CombineWith("/echo/types")
                .AddQueryParam("resReplace", "foo,bar");

            var response = url.PostJsonToUrl(request)
                .FromJson<EchoTypes>();

            Assert.That(response.Byte, Is.EqualTo(1));
            Assert.That(response.Short, Is.EqualTo(2));
            Assert.That(response.Int, Is.EqualTo(3));
            Assert.That(response.Long, Is.EqualTo(4));
            Assert.That(response.Float, Is.EqualTo(1.1f));
            Assert.That(response.String, Is.EqualTo("bar"));
        }

        [Route("/technology/{Slug}")]
        public partial class GetTechnology
            : IReturn<GetTechnologyResponse>
        {
            public virtual string Slug { get; set; }
        }

        public partial class GetTechnologyResponse
        {
            public virtual DateTime Created { get; set; }
            public virtual Technology Technology { get; set; }
            public virtual ResponseStatus ResponseStatus { get; set; }
        }
        public partial class Technology
            : TechnologyBase
        {
        }

        public partial class TechnologyBase
        {
            public virtual long Id { get; set; }
            public virtual string Name { get; set; }
            public virtual string VendorName { get; set; }
            public virtual string VendorUrl { get; set; }
            public virtual string ProductUrl { get; set; }
            public virtual string LogoUrl { get; set; }
            public virtual string Description { get; set; }
            public virtual DateTime Created { get; set; }
            public virtual string CreatedBy { get; set; }
            public virtual DateTime LastModified { get; set; }
            public virtual string LastModifiedBy { get; set; }
            public virtual string OwnerId { get; set; }
            public virtual string Slug { get; set; }
            public virtual bool LogoApproved { get; set; }
            public virtual bool IsLocked { get; set; }
            public virtual DateTime? LastStatusUpdate { get; set; }
        }

        [Test]
        public void Can_proxy_to_techstacks()
        {
            var client = new JsonServiceClient(ListeningOn.CombineWith("techstacks"));

            var request = new GetTechnology
            {
                Slug = "ServiceStack"
            };
            var response = client.Get(request);

            Assert.That(response.Technology.VendorUrl, Is.EqualTo("https://servicestack.net"));
        }

        [Test]
        public async Task Can_proxy_to_techstacks_Async()
        {
            var client = new JsonServiceClient(ListeningOn.CombineWith("techstacks"));

            var request = new GetTechnology
            {
                Slug = "ServiceStack"
            };
            var response = await client.GetAsync(request);

            Assert.That(response.Technology.VendorUrl, Is.EqualTo("https://servicestack.net"));
        }

        [Test]
        public void Can_authenticate_with_downstream_server()
        {
            var client = new JsonServiceClient(ListeningOn.CombineWith("test"))
            {
                ResponseFilter = res =>
                {
                    var ssId = res.Cookies["ss-id"];
                    Assert.That(ssId.Value, Is.Not.Null);
                }
            };

            var response = client.Post(new Authenticate
            {
                provider = "credentials",
                UserName = "test",
                Password = "test",
            });

            Assert.That(response.UserId, Is.Not.EqualTo(0));
            Assert.That(response.SessionId, Is.Not.Null);
            Assert.That(response.UserName, Is.Not.Null);
        }

        [Test]
        public void Does_proxy_test_Exceptions()
        {
            var client = new JsonServiceClient(ListeningOn.CombineWith("test"));

            try
            {
                var response = client.Post(new Authenticate
                {
                    provider = "credentials",
                    UserName = "invalid",
                    Password = "password",
                });
            }
            catch (WebServiceException webEx)
            {
                var status = webEx.ResponseStatus;
                Assert.That(webEx.StatusCode, Is.EqualTo(401));
                Assert.That(webEx.StatusDescription, Is.EqualTo("Unauthorized"));
                Assert.That(status.ErrorCode, Is.EqualTo("Unauthorized"));
                Assert.That(status.Message, Is.EqualTo("Invalid UserName or Password"));
            }
        }
    }
}