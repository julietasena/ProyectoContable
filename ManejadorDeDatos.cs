using SistemaContable;
public class ManejadorDatos
{
    public static BlockChain Nodo1 { get; set; } = new BlockChain();
    public static List<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    public static Cuenta CuentaCapital { get; set; } = new Cuenta("Capital", 0, 0);
    public static int IndiceCuenta { get; set; } = 1;

    // Resto de las funciones y variables relacionadas con la gestión de datos
}