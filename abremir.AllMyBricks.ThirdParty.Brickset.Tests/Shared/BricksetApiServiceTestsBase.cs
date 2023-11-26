﻿using System.IO;
using System.Reflection;
using abremir.AllMyBricks.ThirdParty.Brickset.Extensions;
using Flurl.Http.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace abremir.AllMyBricks.ThirdParty.Brickset.Tests.Shared
{
    public class BricksetApiServiceTestsBase
    {
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        protected HttpTest _httpTestFake;

        [TestInitialize]
        public void TestInitialize()
        {
            _httpTestFake = new HttpTest();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _httpTestFake.Dispose();
        }

        protected string GetResultFileFromResource(string fileName)
        {
            var resourcePath = $"{GetAssemblyName()}.BricksetApiResponses.{GetType().GetDescription()}.{fileName}.json";

            using Stream stream = _assembly.GetManifestResourceStream(resourcePath);
            using var streamReader = new StreamReader(stream);

            return streamReader.ReadToEnd();
        }

        private string GetAssemblyName()
        {
            return _assembly.GetName().Name;
        }
    }
}
