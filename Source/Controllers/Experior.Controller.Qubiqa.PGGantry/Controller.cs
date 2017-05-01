using Experior.Catalog.Qubiqa.TransferCar.Assemblies;

namespace Experior.Controller.Qubiqa.PGGantry
{
    public class Controller : Core.Controller
    {
        private TransferCarController transferCarController;

        public Controller()
            : base("Qubiqa.PGGantry")
        {
            var tcar = Experior.Core.Assemblies.Assembly.Items["Transfer car 1"] as TransferCar;
            if (tcar != null)
                transferCarController = new TransferCarController(tcar);
        }
    }
}