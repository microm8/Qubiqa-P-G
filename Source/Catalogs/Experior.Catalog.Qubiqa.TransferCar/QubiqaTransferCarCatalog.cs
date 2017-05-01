using System.Drawing;
using Experior.Core;

namespace Experior.Catalog.DanishCrown
{
    public class QubiqaTransferCarCatalog : Core.Catalog
    {
        public QubiqaTransferCarCatalog() : base("Qubiqa TransferCar")
        {
            Simulation = Environment.Simulation.Events;

            Common.Meshes = new Core.Resources.Meshes(System.Reflection.Assembly.GetExecutingAssembly());
            Common.Icons = new Core.Resources.Icons(System.Reflection.Assembly.GetExecutingAssembly());
            Add(Common.Icons.Get("TransferCar"), "Transfer car", Environment.Simulation.Events, Create.TransferCar);
        }

        public override Image Logo { get; } = null;
    }
}