﻿using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Alexw.StubServer.Tests
{
    [TestFixture]
    public class StubServerTests
    {
        private Core.StubServer _instance;

        [SetUp]
        public void SetUp()
        {
            _instance = new Core.StubServer();
        }

        [TearDown]
        public void TearDown()
        {
            _instance.Dispose();
        }

        [TestCase("invalid-uri")]
        [TestCase("")]
        [TestCase(null)]
        public void Constructing_InvalidAddress_ThrowsArgumentException(string argument)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                using (new Core.StubServer(argument))
                {
                    // do nothing
                }
            });
        }

        [Test]
        public async Task RulesExists_ValidRequest_RequestManipulated()
        {
            _instance.Rules.Add(context => context.Request.Uri.PathAndQuery.StartsWith(@"/hello/world"), context =>
            {
                var bytes = Encoding.UTF8.GetBytes("hello world");
                context.Response.Body.Write(bytes, 0, bytes.Length);
            });

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(_instance.Address + @"/hello/world");

                Assert.AreEqual(200, (int) response.StatusCode);
            }
        }

        [Test]
        public async Task RuleDoesNotExist_ValidRequest_Returns404()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(_instance.Address + @"/does/not/match");

                Assert.AreEqual(404, (int)response.StatusCode);
            }
        }

        [Test]
        public async Task NoRules_ValidRequest_Returns404()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(_instance.Address);

                Assert.AreEqual(404, (int)response.StatusCode);
            }
        }

        [Test]
        public async Task ValidRequest_LimitApplied()
        {
            using (var client = new HttpClient())
            {
                for (var i = 0; i < 300; i++)
                {
                    await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{_instance.Address}/{i}"));
                }
            }

            Assert.AreEqual(250, _instance.Recorded.Count);
            Assert.AreEqual($"{_instance.Address}/50", _instance.Recorded.First().Uri.ToString());
            Assert.AreEqual($"{_instance.Address}/299", _instance.Recorded.Last().Uri.ToString());
        }
    }
}
