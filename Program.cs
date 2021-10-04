using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using ImageMagick;
using Newtonsoft.Json.Linq;

namespace StarboundPaintingGenerator
{
    internal struct Item
    {
        public string dir;
        public string itemname;
        public int width;
        public int height;
    }
    
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Out.WriteLine("Not enough args given, need <image folder> <output folder>");
                Console.Out.WriteLine(
@"
USAGE:
StarboundPaintingGenerator(.exe) <image directory> <output directory> [<max width> <max height>]
Max Width defaults to 0
Max Height defaults to 300
");
                Environment.Exit(1);
            }

            var imagedir = new DirectoryInfo(args[0]);
            var outputdir = new DirectoryInfo(args[1]);

            if (!imagedir.Exists || imagedir.GetFiles().Length == 0)
            {
                Console.Out.WriteLine("Image dir does not exist or is empty, put some images in there!");
                Environment.Exit(1);
            }

            if (!outputdir.Exists) Directory.CreateDirectory(outputdir.FullName);

            var imageFiles = imagedir.GetFiles().ToList();
            List<Item> items = new();

            foreach (FileInfo file in imageFiles)
            {
                string fileNoExt = Path.GetFileNameWithoutExtension(file.Name);
                var dir = $"{outputdir.FullName}/objects/paintings/{fileNoExt}";
                Directory.CreateDirectory(dir);
                using var image = new MagickImage(file.FullName);
                if (args.Length > 2 && !(args.Length < 4))
                {
                    if (short.TryParse(args[2], out short width) && short.TryParse(args[3], out short height))
                    {
                        image.Resize(width, height);
                    }
                    else
                    {
                        Console.Out.WriteLine("Could not parse width and height arguments, exiting.");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    image.Resize(0, 300);
                }
                image.Write($"{dir}/{fileNoExt}.png");
                items.Add(new Item {dir = dir, height = image.Height, itemname = fileNoExt, width = image.Width});
            }

            JsonWriter jsonWriter = new(items, outputdir);
            
            jsonWriter.WriteAll();

            Console.Out.WriteLine("Finished!");
        }
    }

    internal static class Util
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
    
    internal class JsonWriter
    {
        private readonly List<Item> items;
        private readonly DirectoryInfo outputdir;

        public JsonWriter(List<Item> items, DirectoryInfo outputdir)
        {
            this.items = items;
            this.outputdir = outputdir;
        }

        public void WriteAll()
        {
            foreach (Item item in items)
            {
                WriteFrames(item);
                WriteObject(item);
                WriteRecipe(item);
            }
            
            WritePatches(outputdir.FullName);
            WriteMetadata(outputdir.FullName);
        }
        
        private static void WriteFrames(Item item)
        {
            using StreamWriter sw = File.CreateText($"{item.dir}/{item.itemname}.frames");
            JObject frames = JObject.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.example.frames"));
            var frameGrid = (JObject)frames["frameGrid"];
            var size = (JArray)frameGrid?["size"];

            if (size != null)
            {
                size[0] = item.width;
                size[1] = item.height;
            }

            using var writer = new JsonTextWriter(sw) {Formatting = Formatting.Indented};
            frames.WriteTo(writer);
        }
        
        private static void WriteObject(Item item)
        {
            using StreamWriter sw = File.CreateText($"{item.dir}/{item.itemname}.object");
            JObject obj = JObject.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.example.object"));
            string title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.itemname);
            
            obj["objectName"] = item.itemname;
            obj["description"] = $"A painting by {title}";
            obj["shortdescription"] = title;
            obj["inventoryIcon"] = $"{item.itemname}.png";

            var orientations = (JArray)obj["orientations"];
            
            if (orientations != null)
            {
                var orientation = (JObject)orientations[0];
                orientation["image"] = $"{item.itemname}.png:<color>";
            }
            
            using var writer = new JsonTextWriter(sw) {Formatting = Formatting.Indented};
            obj.WriteTo(writer);
        }
        
        private static void WriteRecipe(Item item)
        {
            using StreamWriter sw = File.CreateText($"{item.dir}/{item.itemname}.recipe");
            JObject recipe = JObject.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.example.recipe"));

            var output = (JObject)recipe["output"];
            if (output != null)
            {
                output["item"] = item.itemname;
            }

            using var writer = new JsonTextWriter(sw) {Formatting = Formatting.Indented};
            recipe.WriteTo(writer);
        }
        
        private void WritePatches(string dir)
        {
            using StreamWriter sw = File.CreateText($"{dir}/player.config.patch");
            JArray patch = JArray.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.player.config.patch"));
            var spArray = (JArray)patch[0];
            var initialstate = (JArray)spArray.DeepClone();

            spArray.Remove();

            foreach (Item item in items)
            {
                var entry = (JObject)initialstate[0];
                var value = (JObject)entry["value"];
                if (value != null)
                {
                    value["item"] = item.itemname;
                }
                patch.Add(initialstate);
            }
            
            using var writer = new JsonTextWriter(sw) {Formatting = Formatting.Indented};
            patch.WriteTo(writer);
        }
        
        private static void WriteMetadata(string dir)
        {
            using StreamWriter sw = File.CreateText($"{dir}/_metadata");
            JObject metadata = JObject.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources._metadata"));
            
            using var writer = new JsonTextWriter(sw) {Formatting = Formatting.Indented};
            metadata.WriteTo(writer);
        }
    }
}
