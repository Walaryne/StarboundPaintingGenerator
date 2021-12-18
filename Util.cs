using System;
using System.IO;
using System.Reflection;
namespace StarboundPaintingGenerator
{
    public static class Util
    {
        public static string GetEmbeddedResourceContent(string resourceName)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            var source = new StreamReader(stream ?? throw new InvalidOperationException());
            string fileContent = source.ReadToEnd();
            source.Dispose();
            stream.Dispose();
            return fileContent;
        }
    }
}
