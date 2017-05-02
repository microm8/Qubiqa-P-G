using System.Drawing;

namespace Experior.Catalog.Qubiqa
{
    public class Catalog : Experior.Core.Catalog
    {
        public Catalog()
            : base("Qubiqa (Gantry)")
        {
            Simulation = Experior.Core.Environment.Simulation.Events | Experior.Core.Environment.Simulation.Physics;

            Common.Meshes = new Experior.Core.Resources.Meshes(System.Reflection.Assembly.GetExecutingAssembly());
            Common.Icons = new Experior.Core.Resources.Icons(System.Reflection.Assembly.GetExecutingAssembly());

            Add(Common.Icons.Get("MyAssembly"), "MyAssembly", "", Experior.Core.Environment.Simulation.Events | Experior.Core.Environment.Simulation.Physics, Create.MyAssembly);
        }

        public override Image Logo
        {
            get { return Common.Icons.Get("Logo"); }
        }
    }
}
