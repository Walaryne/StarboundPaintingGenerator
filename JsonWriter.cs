using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace StarboundPaintingGenerator
{
    public class JsonWriter
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
            JsonNode frames = JsonNode.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.example.frames"));
            JsonNode frameGrid = frames?["frameGrid"];
            JsonNode size = frameGrid?["size"];

            if (size != null)
            {
                size[0] = item.width;
                size[1] = item.height;
            }

            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            
            using var writer = new Utf8JsonWriter(sw.BaseStream, writerOptions);
            frames?.WriteTo(writer);
        }
        
        private static void WriteObject(Item item)
        {
            using StreamWriter sw = File.CreateText($"{item.dir}/{item.itemname}.object");
            JsonNode obj = JsonNode.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.example.object"));
            string title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.itemname);

            if (obj == null)
                return;
            obj["objectName"] = item.itemname;
            obj["description"] = $"A painting by {title}";
            obj["shortdescription"] = title;
            obj["inventoryIcon"] = $"{item.itemname}.png";

            JsonNode orientations = obj["orientations"];
            JsonNode orientation = orientations?[0];
            if (orientation != null)
            {
                orientation["image"] = $"{item.itemname}.png:<color>";
            }

            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };

            using var writer = new Utf8JsonWriter(sw.BaseStream, writerOptions);
            obj.WriteTo(writer);
        }
        
        private static void WriteRecipe(Item item)
        {
            using StreamWriter sw = File.CreateText($"{item.dir}/{item.itemname}.recipe");
            JsonNode recipe = JsonNode.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.example.recipe"));

            JsonNode output = recipe?["output"];
            if (output is not null)
            {
                output["item"] = item.itemname;
            }

            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            
            using var writer = new Utf8JsonWriter(sw.BaseStream, writerOptions);
            recipe?.WriteTo(writer);
        }
        
        private void WritePatches(string dir)
        {
            using StreamWriter sw = File.CreateText($"{dir}/player.config.patch");
            JsonNode patch = JsonNode.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.player.config.patch"));
            JsonNode patchCopy = JsonNode.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources.player.config.patch"));
            
            JsonArray spArray = patch?.AsArray();
            JsonArray initialCopy = patchCopy?.AsArray();

            spArray?.RemoveAt(0);

            foreach (Item item in items)
            {
                JsonArray entry = initialCopy?[0]?.AsArray();
                JsonObject value = entry?[0]?.AsObject();
                if (value is not null)
                {
                    value["item"] = item.itemname;
                }
                spArray?.Add(JsonNode.Parse(entry?.ToJsonString() ?? throw new InvalidOperationException())?.AsArray());
            }
            
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            
            using var writer = new Utf8JsonWriter(sw.BaseStream, writerOptions);
            patch?.WriteTo(writer);
        }
        
        private static void WriteMetadata(string dir)
        {
            using StreamWriter sw = File.CreateText($"{dir}/_metadata");
            JsonNode metadata = JsonNode.Parse(Util.GetEmbeddedResourceContent("StarboundPaintingGenerator.resources._metadata"));
            
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            
            using var writer = new Utf8JsonWriter(sw.BaseStream, writerOptions);
            metadata?.WriteTo(writer);
        }
    }
}
