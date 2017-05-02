using Experior.Catalog.Qubiqa.TransferCar.Assemblies;
using Experior.Core.Assemblies;

namespace Experior.Catalog.DanishCrown
{
    public static class Create
    {
        public static Assembly TransferCar(string title, string subtitle, object properties)
        {
            var info = new TransferCarInfo
            {
                name = Assembly.GetValidName("Transfer car "),
                height = 0,
                length = 20
            };
            return new TransferCar(info);
        }
    }
}