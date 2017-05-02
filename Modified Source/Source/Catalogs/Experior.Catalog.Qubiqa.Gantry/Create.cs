using Experior.Core.Assemblies;

namespace Experior.Catalog.Qubiqa
{
    internal class Common
    {
        public static Experior.Core.Resources.Meshes Meshes;
        public static Experior.Core.Resources.Icons Icons;
    }

    public class Create
    {
        public static Assembly MyAssembly(string title, string subtitle, object properties)
        {
            var info = new Experior.Catalog.Qubiqa.Assemblies.GantryInfo { name = Experior.Core.Assemblies.Assembly.GetValidName("Gantry") };
            var assembly = new Experior.Catalog.Qubiqa.Assemblies.Gantry(info);
            return assembly;
        }
    }
}