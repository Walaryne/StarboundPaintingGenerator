using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;

namespace StarboundPaintingGenerator
{
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
Setting either (but not both) to 0 tells ImageMagick to fill in based on aspect ratio
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
                items.Add(new Item(dir, fileNoExt, image.Width, image.Height));
            }

            JsonWriter jsonWriter = new(items, outputdir);
            
            jsonWriter.WriteAll();

            Console.Out.WriteLine("Finished!");
        }
    }
}
