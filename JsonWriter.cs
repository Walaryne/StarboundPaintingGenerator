using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                patch.Add(initialstate.DeepClone());
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
