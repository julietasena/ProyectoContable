using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

public partial class Block : Form
{
    /*
//id del bloque
    public long Id { get; set; }
    //fecha en la que se creó el bloque
    public DateTime Timespamp { get; set; }
    //transacciones almacenadas dentro del bloque
    public Transaction[] Transactions { get; set; }
    //hashCode del bloque
    public string Hash { get; set; }
    //hashCode del bloque anterior
    public string PreviousHash { get; set; }
    //nro de prueba de trabajo
    public int Proof { get; set; }

    //constructor por defecto para serializar
    public Block() { }

    public Block(long index, List<Transaction> transactions, string previousHash)
    {
        Id = index;
        Transactions = transactions != null ? transactions.ToArray() : new Transaction[0];
        PreviousHash = previousHash;
        Timespamp = DateTime.Now;
    }
    //zona de "Minería"
    #region Mining

    //método que verifica si un hashCode es válido
    private bool hashIsValid(string text, int difficulty)
    {
        string hash = HashHelper.calculateHash(text);
        string zeros = string.Empty.PadLeft(difficulty, '0');

        //verifica si se cumple la dificultad solicitada,
        //si es así, retorna verdadero, sino, retorna falso
        return hash.StartsWith(zeros);
    }

    //minamos el bloque para validarlo
    public int mineBlock(int difficulty)
    {
        //inicializa un texto inicial sumando
        //el id, la fecha y el hashCode de todas las transacciones del bloque
        string initialText = string.Format("{0}{1}{2}", Id, Timespamp, Transactions.Select(t => t.Hash).Aggregate((i, j) => i + j));
        //inicializamos la prueba de trabajo en 0
        Proof = 0;
        string text = string.Format("{0}{1}", initialText, Proof);

        //mientras el hashCode no sea válido, se va a concatenar
        //el texto inicial con el nro de la prueba de trabajo
        while (!hashIsValid(text, difficulty))
        {
            Proof++;
            text = string.Format("{0}{1}", initialText, Proof);

        }

        //asignamos el hashCode al bloque con la ayuda de la clase HashHelper
        Hash = HashHelper.calculateHash(text);

        return Proof;
    }

    #endregion Mining
}

//Clase de las transacciones
public class Transaction
{
    //id de la transaccion
    public long Id { get; set; }
    //fecha y hora de la transaccion
    public DateTime Timespamp { get; set; }
    //cuenta que va a enviar la transaccion
    public string Sender { get; set; }
    //cuenta receptora de la transaccion
    public string Receiver { get; set; }
    //monto transferido
    public decimal Amount { get; set; }
    //hashCode generado con ayuda de la clase hashhelper
    public string Hash { get { return HashHelper.calculateHash(string.Format("{0}{1}{2}", Sender, Receiver, Amount)); } }
    //clave publica 
    public string PublicKey { get; set; }
    //firma de la transaccion
    public byte[] Signature { get; set; }

    //constructor por defecto para serializar
    public Transaction() { }
    public Transaction(string sender, string receiver, decimal amount)
    {
        Sender = sender;
        Receiver = receiver;
        Amount = amount;
        Timespamp = DateTime.Now;
    }
    //método encargado de realizar la firma
    public void sing(string publicKey, string privateKey)
    {
        PublicKey = publicKey;
        //asignamos la firma con el hash gracias al método sing de la clase RsaHelper
        Signature = RsaHelper.sing(Hash, privateKey);
    }
    //método que verifica si el hashCode es válido con la ayuda de la clase RsaHelper
    public bool isValid()
    {
        return RsaHelper.verify(Hash, Signature, PublicKey);
    }
}

//Clase de las cadenas de bloques
public class BlockChain : NodeRequester, INode
{
    //lista de bloques
    public List<Block> blocks { get; set; }
    //lista temporal de transacciones
    public List<Transaction> tempTransactions { get; set; }
    //declaramos como constante la difficultad en 4
    private const int difficulty = 4;
    //clave pública
    public string PublicKey { get; set; }
    //clave privada
    public string PrivateKey { get; set; }

    public BlockChain()
    {
        blocks = new List<Block>();
        tempTransactions = new List<Transaction>();
        //generamos las claves
        generateKeys();
    }

    //método que se encarga de generar las claves con la ayuda de la clase RsaHelper
    public void generateKeys()
    {
        string[] keys = RsaHelper.generateKeys();
        PublicKey = keys[0];
        PrivateKey = keys[1];
    }

    //método que se encargará de crear cada transacción
    public void newTransaction(string sender, string receiver, decimal amount, bool fromNet = false)
    {
        Transaction transaction = new Transaction(sender, receiver, amount);
        transaction.Id = tempTransactions.Count;
        //firmamos la transacción
        transaction.sing(PublicKey, PrivateKey);
        if (transaction.isValid())
        {
            tempTransactions.Add(transaction);
            //si la transacción no de la red
            if (!fromNet)
            {
                //actualizamos todos los nodos
                foreach (INode node in Nodes)
                {
                    node.performTransactionNet(this, sender, receiver, amount);
                }
            }
        }
    }

    //método que se encargará de crear cada bloque
    public void newBlock()
    {
        string previousHash = string.Empty;
        if (blocks.Count > 0)
            previousHash = blocks[blocks.Count - 1].Hash;
        Block block = new Block(blocks.Count, tempTransactions, previousHash);
        //justo después de crearlo, minamos el bloque
        block.mineBlock(difficulty);
        blocks.Add(block);

        tempTransactions = new List<Transaction>();
    }

    //método que es llamado cuando otro nodo recibe una transacción
    public void performTransactionNet(INodeRequester requester, string sender, string receiver, decimal amount)
    {
        newTransaction(sender, receiver, amount, true);
    }

    //método que recibe las peticiones de otros nodos
    public void prepareInfoToSend(INodeRequester requester, string code)
    {
        //código aleatorio y opcional
        if (code.Equals("EntireBlockChain"))
        {
            //usamos la clase serializeHelper y creamos un objeto serializaHelper
            SerializeHelper<BlockChain> sh = new SerializeHelper<BlockChain>();
            //declaramos la respuesta y le asignamos el objeto serializado
            string response = sh.serialize(this);
            //finalmente le pasamos la respuesta al método update
            requester.update(response);
        }
    }

    //llamaremos este método cada vez que queramos actualizar este nodo
    public void PerformConsensus()
    {
        //para cada nodo
        foreach (INode node in Nodes)
        {
            //prepara y envía la información
            node.prepareInfoToSend(this, "EntireBlockChain");//código aleatorio y opcional
        }
    }

    public override void update(string response)
    {
        //Creamos un objeto serializeHelper
        SerializeHelper<BlockChain> sh = new SerializeHelper<BlockChain>();
        //deserializamos y obtenemos una copia de la blockchain del nodo vecino
        BlockChain neigh = sh.deserialize(response);

        //si tiene más
        if (neigh.blocks.Count > blocks.Count)
        {
            string prevHash = string.Empty;
            //asignamos y copiamos directamente los bloques vecinos
            blocks = neigh.blocks;
        }
    }
}

//clase que nos ayudará a calcular los hashCodes
public class HashHelper
{
    //método que se encargará de calcular un hashCode
    public static string calculateHash(string text)
    {
        string myHashCalculated = string.Empty;
        //usamos el algoritmo SHA256
        using (SHA256 mySHA256 = SHA256.Create())
        {
            //hacemos uso de el codificador UTF8
            byte[] encodedText = new UTF8Encoding().GetBytes(text);
            //calculamos el hash con el algoritmo
            byte[] myHashArray = mySHA256.ComputeHash(encodedText);
            //convertimos el hashCode en un string
            myHashCalculated = BitConverter.ToString(myHashArray).Replace("-", string.Empty);
        }
        return myHashCalculated;
    }
}

//Clase que nos ayudará a trbajar con el algoritmo RSA
public class RsaHelper
{
    //Generamos las llaves con la ayuda del algoritmo RSA
    public static string[] generateKeys()
    {
        string[] keys = new string[2];
        //hacemos uso del algoritmo RSA
        using (RSACryptoServiceProvider Rsa = new RSACryptoServiceProvider())
        {
            //clave publica
            keys[0] = serializeRsaParameters(Rsa.ExportParameters(false)); //parametro publico
            //clave privada
            keys[1] = serializeRsaParameters(Rsa.ExportParameters(true)); //parametro privado
        }
        return keys;
    }

    //método para firmar una transacción
    public static byte[] sing(string text, string privateKey)
    {
        //deserializamos la clave privada en un objeto RSA
        RSAParameters key = deserializeRsaParameters(privateKey);
        byte[] signature;
        //usamos el algoritmo RSA para codificar
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        {
            //declaramos el codificador
            var encoder = new UTF8Encoding();
            //obtenemos el array de bytes del texto
            byte[] originalData = encoder.GetBytes(text);

            try
            {
                //importamos la clave
                rsa.ImportParameters(key);
                // firmamos con el array de bytes haciendo uso del algoritmo SHA512
                signature = rsa.SignData(originalData, CryptoConfig.MapNameToOID("SHA512"));
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                signature = null;
            }
            finally
            {
                //evitamos que se persista la clave
                rsa.PersistKeyInCsp = false;
            }
        }
        return signature;
    }

    //método para verificar los datos haciendo uso de la firma y la clave pública
    public static bool verify(string data, byte[] signature, string publicKey)
    {
        bool success = false;
        try
        {
            //deserializamos la clave pública
            RSAParameters key = deserializeRsaParameters(publicKey);
            //hacemos uso del algoritmo RSA
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                //declaramos el codificador UTF8
                var encoder = new UTF8Encoding();
                //obtenemos el array de bytes de los datos
                byte[] dataBytes = encoder.GetBytes(data);
                try
                {
                    //importamos la clave
                    rsa.ImportParameters(key);
                    //verificamos que los datos tengan la firma correspondiente
                    success = rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA512"), signature);
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    //evitamos la persistencia de la clave
                    rsa.PersistKeyInCsp = false;
                }
            }
        }
        catch (Exception) { }
        return success;
    }

    //Serializamos el objeto RSA
    private static string serializeRsaParameters(RSAParameters parameters)
    {
        var sw = new StringWriter();
        var xs = new XmlSerializer(typeof(RSAParameters));
        xs.Serialize(sw, parameters);
        return sw.ToString();
    }

    //Deserializamos el objeto RSA
    private static RSAParameters deserializeRsaParameters(string stringKey)
    {
        var sr = new StringReader(stringKey);
        var xr = new XmlSerializer(typeof(RSAParameters));
        return (RSAParameters)xr.Deserialize(sr);
    }
}

//clase que nos ayudará a serializar los objetos y recibe una clase genérica
public class SerializeHelper<T> where T : class
{
    //Serializamos el objeto y obtenemos un string
    public string serialize(T objectInstanced)
    {
        string objectString = string.Empty;
        //usamos un stringWriter para serializar
        using (var stringWriter = new StringWriter())
        {
            //declaramos el serializador y le pasamos el tipo del objeto
            var serializer = new XmlSerializer(objectInstanced.GetType());
            serializer.Serialize(stringWriter, objectInstanced);
            objectString = stringWriter.ToString();
        }
        return objectString;
    }
    //Deserializamos el string y obtenemos un objeto
    public T deserialize(string objectString)
    {
        T objectInstanced = null;
        //usamos un stringReader para leer los datos del string
        using (var stringReader = new StringReader(objectString.Trim()))
        {
            //declaramos el serializador y pasamos el tipo a retornar
            var serializer = new XmlSerializer(typeof(T));
            objectInstanced = serializer.Deserialize(stringReader) as T;
        }
        return objectInstanced;
    }
}

//interfaz/clase que representa el nodo al que vamos a llamar
public interface INode
{
    //método que actualizará las transacciones
    void performTransactionNet(INodeRequester requester, string sender, string receiver, decimal amount);
    //método para solicitar una copia de la blockchain
    void prepareInfoToSend(INodeRequester requester, string code);
}

//interfaz/clase que representa el nodo cliente
public interface INodeRequester
{
    //método para actualizar la cadena de bloques
    //response va a ser la cadena de bloques serializada
    void update(string response);
}

//clase abstracta que contendrá los nodos vecinos
public abstract class NodeRequester : INodeRequester
{
    //declaramos una lista de nodos privada
    private IList<INode> Nodes1 { get; set; }
    //hacemos uso de la lista privada para evitar nulos
    protected IList<INode> Nodes
    {
        get
        {
            if (Nodes1 == null)
                Nodes1 = new List<INode>();
            return Nodes1;
        }
        set
        {
            Nodes1 = value;
        }
    }
    public abstract void update(string response);
    //método para registrar nodos vecinos, que recibe un nodo
    public void registerNode(INode neigh)
    {
        if (Nodes == null)
            Nodes = new List<INode>();
        Nodes.Add(neigh);
    }
}

public class Cuenta
{
    public string Nombre { get; set; }
    public decimal Dinero { get; set; }
    public decimal DineroInicial { get; set; }
    public int Id { get; set; }

    public Cuenta(string nombre, decimal cantidad, int id)
    {
        Nombre = nombre;
        Dinero = cantidad;
        DineroInicial = cantidad;
        Id = id;
    }
}

public class principal
{
    static BlockChain nodo1 = new BlockChain();
    static List<Cuenta> cuentas = new List<Cuenta>();
    static Cuenta cuentaCapital = new Cuenta("Capital", 0, 0);
    static int indiceCuenta = 1;

    static int pedirN()
    {
        string cadena = string.Empty;
        int n;
        while (cadena == string.Empty || !int.TryParse(cadena, out n))
        {
            cadena = Console.ReadLine();
        }
        n = int.Parse(cadena);
        return n;
    }

    static void mostrarCuentas()
    {
        foreach (Cuenta c in cuentas)
        {
            Console.WriteLine(c.Id + "." + c.Nombre);
        }
    }
    static bool existeCuenta(int opcion)
    {
        foreach (Cuenta c in cuentas)
        {
            if (c.Id == opcion)
            {
                return true;
            }
        }
        return false;
    }
    static bool existeCuenta(string nombre)
    {
        foreach (Cuenta c in cuentas)
        {
            if (c.Nombre == nombre)
            {
                return true;
            }
        }
        return false;
    }

    static void registrarCuenta()
    {
        indiceCuenta++;
        Console.Clear();
        Console.WriteLine("Listado de cuentas:");
        Console.Write("1 - Mercaderías\n2 - Rodados\n3 - Inmuebles\n4 - Deudores por venta\n5 - Docuemntos a cobrar\n6 - Proveedores\n7 - Acreedores\n8 - Impuestos a pagar\n9 - Préstamos a pagar\n10 - Cuenta corriente\n");
        int opc = 0;
        bool esValido = false;
        string nombreCuenta="";
        while (esValido == false || existeCuenta(nombreCuenta) == true) { 
            Console.WriteLine("Escriba el número correspondiente a la cuenta que quiere utilizar:");
            opc = pedirN();
            switch (opc)
            {
                case 1:
                    nombreCuenta = "Mercaderías";
                    esValido = true;
                    break;
                case 2:
                    nombreCuenta = "Rodados";
                    esValido = true;
                    break;
                case 3:
                    nombreCuenta = "Inmuebles";
                    esValido = true;
                    break;
                case 4:
                    nombreCuenta = "Deudores por venta";
                    esValido = true;
                    break;
                case 5:
                    nombreCuenta = "Docuemntos a cobrar";
                    esValido = true;
                    break;
                case 6:
                    nombreCuenta = "Proveedores";
                    esValido = true;
                    break;
                case 7:
                    nombreCuenta = "Acreedores";
                    esValido = true;
                    break;
                case 8:
                    nombreCuenta = "Impuestos a pagar";
                    esValido = true;
                    break;
                case 9:
                    nombreCuenta = "Préstamos a pagar";
                    esValido = true;
                    break;
                case 10:
                    nombreCuenta = "Cuenta corriente";
                    esValido = true;
                    break;
                default:
                    Console.WriteLine("Debe colocar un número válido");
                    esValido=false;
                    nombreCuenta="";    
                    break;
            }
            if (existeCuenta(nombreCuenta) == true){
            Console.WriteLine("La cuenta ya existe.");   
            }
        }

       // string nombreCuenta = Console.ReadLine();
       // while (nombreCuenta == string.Empty || existeCuenta(nombreCuenta))
         //   nombreCuenta = Console.ReadLine();

        Console.WriteLine("\nIngrese el capital de la cuenta:");
        string amount = string.Empty;
        decimal n;
        while (!decimal.TryParse(amount, out n) || decimal.Parse(amount) < 0)
            amount = Console.ReadLine();

        cuentas.Add(new Cuenta(nombreCuenta, decimal.Parse(amount), indiceCuenta));
        nodo1.newTransaction(cuentaCapital.Nombre, nombreCuenta, decimal.Parse(amount));
    }

    public static void nuevaTransaccion()
    {
        Console.Clear();
        int idEmisora, idReceptora;
        bool dineroInsuficiente = false;
        do
        {
            Console.WriteLine("Seleccione la cuenta emisora:");
            mostrarCuentas();
            idEmisora = pedirN();
            Console.Clear();
        } while (!existeCuenta(idEmisora)); 
        do
        {
            Console.WriteLine("Seleccione la cuenta receptora:");
            mostrarCuentas();
            idReceptora = pedirN();
            if (idReceptora == idEmisora)
            {
                Console.Clear();
                Console.WriteLine("Las cuentas seleccionadas son la misma...");
                Console.WriteLine("Intente que sean diferentes...");
                Console.ReadKey();
            }
            Console.Clear();
        } while (!existeCuenta(idReceptora) || (idReceptora == idEmisora));
        Console.WriteLine("Ingrese el monto:");
        decimal amount;
        do
        {
            amount = pedirN();
            if (amount > cuentas[idEmisora - 1].Dinero)
            {
                Console.Clear();
                Console.WriteLine("La cuenta emisora no dispone de esa cantidad de dinero...");
                Console.WriteLine("Cancelando transacción...");
                Console.ReadKey();
                Console.Clear();
                dineroInsuficiente = true;
            }
        } while (amount < 1);

        if (!dineroInsuficiente)
        {
            nodo1.newTransaction(cuentas[idEmisora - 1].Nombre, cuentas[idReceptora - 1].Nombre, amount);
            cuentas[idEmisora - 1].Dinero -= amount;
            cuentas[idReceptora - 1].Dinero += amount;
        }
    }
    public static string agregarEspaciosLibroDiario(string cadena, bool esDebe, bool hayHaber)
    {
        int longitud = cadena.Length;
        if (esDebe)
        {
            for (int i = 0; i < 10 - longitud; i++)
            {
                cadena += " ";
            }
        }
        else
        {
            if (hayHaber)
            {
                for (int i = 0; i < 30 - longitud; i++)
                {
                    cadena += " ";
                }
            }
            else
            {
                for (int i = 0; i < 20 - longitud; i++)
                {
                    cadena += " ";
                }
            }
        }

        return cadena;
    }

    public static string agregarEspaciosLibroMayor(string cadena, bool esDebe, bool eshaber)
    {
        int longitud = cadena.Length;
        string aux = cadena;  
        if (esDebe)
        {
            for (int i = 0; i < 10 - longitud; i++)
            {
                cadena += " ";
            }
            cadena += "|";
        }
        else if (eshaber) 
        {
            cadena = string.Empty;
            for (int i = 0; i < 10; i++)
            {
                cadena += " ";
            }
            cadena += "|" + aux;
        }
        else
        {
            cadena = string.Empty;
            for (int i = 0; i < 21 - longitud; i++)
            {
                if (i != (21 - longitud)/2)
                {
                    cadena += " ";
                }
                else
                {
                    cadena += aux;
                }
            }
        }
        return cadena;
    }

    static void mostrarLibroMayor(Cuenta c ,DateOnly fechaDeTransaccion)
    {
        Console.WriteLine(agregarEspaciosLibroMayor(c.Nombre, false, false) + "\n---------------------");  
        decimal sumaDebe = 0, sumaHaber = 0;
        if (!c.Nombre.Equals("Capital"))
        {
            foreach (Block b in nodo1.blocks)
            {
                foreach (Transaction t in b.Transactions)
                {
                    DateOnly fechaComparar = DateOnly.FromDateTime(t.Timespamp);
                    if (t.Receiver.Equals(c.Nombre) && (fechaComparar <= fechaDeTransaccion))
                    {
                        Console.WriteLine(agregarEspaciosLibroMayor(t.Amount.ToString(), true, false));
                        sumaDebe += t.Amount;
                    }
                    else if (t.Sender.Equals(c.Nombre))
                    {
                        Console.WriteLine(agregarEspaciosLibroMayor(t.Amount.ToString(), false, true));
                        sumaHaber += t.Amount;
                    }
                }
            }
            Console.WriteLine("---------------------\n" + agregarEspaciosLibroMayor((sumaDebe - sumaHaber).ToString(), false, false) + "\n");
            cuentaCapital.Dinero += sumaDebe - sumaHaber;
        }
        else
        {
            Console.WriteLine(agregarEspaciosLibroMayor(c.Dinero.ToString(), true, false));
            Console.WriteLine("---------------------\n" + agregarEspaciosLibroMayor(c.Dinero.ToString(), false, false) + "\n");
        }
    }
    public static void ingresarEfectivo()
    {
        string amount = string.Empty;
        decimal n;
        Console.WriteLine("Ingrese el monto:");
        do
        {
            amount = Console.ReadLine();
        } while (!decimal.TryParse(amount, out n) || decimal.Parse(amount) < 1);

        cuentas[0].Dinero += decimal.Parse(amount);
        nodo1.newTransaction(cuentaCapital.Nombre, cuentas[0].Nombre, decimal.Parse(amount));
    }

    public static void menuTransaccion()
    {
        Console.Clear();
        Console.WriteLine("Seleccione la accion a realizar");
        Console.WriteLine("1.Iniciar una transferencia.");
        Console.WriteLine("2.Ingresar efectivo.");
        Console.WriteLine("3.Registrar una cuenta.");
        Console.WriteLine("4.Volver.");
        int opcion = pedirN();

        switch (opcion)
        {
            case 1:
                Console.Clear();
                bool cuentasVacias = true;
                foreach (Cuenta c in cuentas)
                {
                    if(c.Dinero > 0)
                    {
                        cuentasVacias = false;
                    }
                }
                if (!cuentasVacias)
                {
                    if (cuentas.Count > 1)
                    {
                        nuevaTransaccion();
                    }
                    else
                    {
                        Console.WriteLine("Debe registrar al menos una cuenta...");
                        Console.ReadKey();
                        Console.Clear();
                    }
                }
                else
                {
                    Console.WriteLine("Todas las cuentas están vacías...");
                    Console.ReadKey();
                    Console.Clear();
                }
                break;
            case 2:
                Console.Clear();
                ingresarEfectivo();
                break;
            case 3:
                Console.Clear();
                registrarCuenta();
                Console.Clear();
                break;
            case 4:
                Console.Clear();
                menu();
                break;
            default:
                Console.Clear();
                Console.WriteLine("La opción no existe...");
                Console.ReadKey();
                Console.Clear();
                break;
        }
    }
    public static void menu()
    {
        Console.WriteLine("|-----BIENVENIDO AL MENU-----|");
        Console.WriteLine("|1.Iniciar una operación.    |");
        Console.WriteLine("|2.Ver libro diario.         |");
        Console.WriteLine("|3.Ver libro mayor.          |");
        Console.WriteLine("|4.Salir.                    |");
        Console.WriteLine("|----------------------------|");
        int opcion = pedirN();

        switch (opcion)
        {
            case 1:
                char opcionTransaccion;
                //Se crean las transacciones que el usuario quiera
                do
                {
                    opcionTransaccion = '?';
                    menuTransaccion();
                    while (opcionTransaccion != 'n' && opcionTransaccion != 'y')
                    {
                        Console.Clear();
                        Console.WriteLine("Realizar una nueva transacción: y");
                        Console.WriteLine("Volver al menu principal: n");
                        opcionTransaccion = (char) Console.Read();
                    }
                } while (opcionTransaccion == 'y');
                //Cuando se terminan de ingresar las transacciones, se crea un nuevo bloque
                if(!(nodo1.tempTransactions.Count < 1))
                    nodo1.newBlock();
                Console.Clear();
                menu();
                break;
            case 2:
                Console.Clear();
                Console.WriteLine("\t\tLIBRO DIARIO");
                for (int i = 0; i < nodo1.blocks.Count; i++)
                {
                    //cada operacion es un bloque
                    Console.WriteLine("\n" + (i + 1) + "° Operación");
                    for (int j = 0; j < nodo1.blocks[i].Transactions.Length; j++)
                    {
                        Console.Write("\n" + (j + 1));
                        Console.Write("\tFecha: " + nodo1.blocks[i].Transactions[j].Timespamp);
                        Console.WriteLine("\n" + agregarEspaciosLibroDiario("Cuenta", false, false) + agregarEspaciosLibroDiario("Debe", true, false) + agregarEspaciosLibroDiario("Haber", true, false));
                        Console.WriteLine(agregarEspaciosLibroDiario(nodo1.blocks[i].Transactions[j].Receiver, false, false) + agregarEspaciosLibroDiario(nodo1.blocks[i].Transactions[j].Amount.ToString(), true, false));
                        Console.WriteLine(agregarEspaciosLibroDiario(nodo1.blocks[i].Transactions[j].Sender, false, true) + agregarEspaciosLibroDiario(nodo1.blocks[i].Transactions[j].Amount.ToString(), true, false));
                    }
                }
                Console.ReadKey();
                Console.Clear();
                menu();
                break;
            case 3:
                Console.Clear();
                List<DateOnly> listaFechas = new List<DateOnly>();
                foreach (Block b in nodo1.blocks)
                {
                    foreach(Transaction t in b.Transactions)
                    {
                        DateOnly fecha = DateOnly.FromDateTime(t.Timespamp);
                        listaFechas.Add(fecha);
                    }
                }
                HashSet<DateOnly> listaFechasHashSet = new HashSet<DateOnly>(listaFechas);
                listaFechas = listaFechasHashSet.ToList<DateOnly>();
                Console.WriteLine("Seleccione una fecha...");
                for(int i = 0; i < listaFechas.Count; i++)
                {
                    Console.WriteLine(i+1 + "." + listaFechas[i]);
                }
                int opcionfecha = -1;
                while(opcionfecha < 1 || opcionfecha > listaFechas.Count)
                {
                    opcionfecha = pedirN();
                }
                Console.Clear();
                Console.WriteLine("\t\tLIBRO MAYOR");
                cuentaCapital.Dinero = 0;
                foreach (Cuenta c in cuentas)
                {
                    mostrarLibroMayor(c, listaFechas[opcionfecha-1]);
                }
                mostrarLibroMayor(cuentaCapital, listaFechas[opcionfecha-1]);
                Console.ReadKey();
                Console.Clear();
                menu();
                break;
            case 4:
                Console.Clear();
                Console.WriteLine("Finalizando...");
                Console.ReadKey();
                Environment.Exit(1);
                break;
            default:
                Console.Clear();
                Console.WriteLine("La opción no existe...");
                Console.ReadKey();
                Console.Clear();
                menu();
                break;
        }
    }
    public static void Main(string[] args)
    {
        //BlockChain nodo1 = new BlockChain();

        //nodo1.newTransaction("Maxi", "Cande", 100);
        //nodo1.newTransaction("Cande", "Fede", 50);
        //nodo1.newBlock();

        //nodo1.newTransaction("Chiro", "Lauti", 100);
        //nodo1.newTransaction("Lauti", "Maxi", 50);
        //nodo1.newBlock();

        //BlockChain nodo2 = new BlockChain();

        //nodo1.registerNode(nodo2);
        //nodo2.registerNode(nodo1);

        //nodo2.PerformConsensus();

        //if (nodo1.blocks.Count == nodo2.blocks.Count)
        //{
        //    Console.WriteLine("Correcto");
        //}
        //else
        //{
        //    Console.WriteLine("Incorrecto");
        //}
        cuentas.Add(new Cuenta("Caja", 0, indiceCuenta));
        menu();
    }*/
}

