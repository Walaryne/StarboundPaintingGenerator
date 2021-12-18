namespace StarboundPaintingGenerator
{
    public class Item
    {
        public readonly string dir;
        public readonly string itemname;
        public readonly int width;
        public readonly int height;

        public Item(string dir, string itemname, int width, int height)
        {
            this.dir = dir;
            this.itemname = itemname;
            this.width = width;
            this.height = height;
        }
    }
}
