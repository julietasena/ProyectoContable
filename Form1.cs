namespace SistemaContable;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using MySql.Data.MySqlClient;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;



//clase de bloques
public class Block
{
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
        public void SaveBlockchainToFile(string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al guardar la cadena de bloques: {ex.Message}");
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

public partial class Form1 : Form
{

    public Form1(DataGridView dataGridViewLibroDiario)
    {
        this.dataGridViewLibroDiario = dataGridViewLibroDiario;
    }
    private ComboBox cmbCuentas; // Asegúrate de tener esta declaración
    private TextBox txtNombreCuenta;
    static BlockChain nodo1 = GetBlockchainFromFile();
    static List<Cuenta> cuentas = new List<Cuenta>();
    static Cuenta cuentaCapital = new Cuenta("Capital", 0, 0);
    public static int IndiceCuenta { get; set; } = 1;
    static int indiceCuenta = 1;

    public static BlockChain GetBlockchainFromFile()
    {
        try
        {
            var blockchain = JsonSerializer.Deserialize<BlockChain>(File.ReadAllText(@"blockchain.json"));

            return (blockchain != null) ? blockchain : new BlockChain();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la cadena de bloques: {ex.Message}");
            return new BlockChain();
        }

    }

    private DataGridView dataGridViewLibroDiario;
    private Panel panelDerecho = new Panel();

    public Form1()
    {
        InitializeComponent();
        CargarCuentas();
        panelDerecho = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BackgroundImage = new Bitmap(new WebClient().OpenRead("https://e1.pxfuel.com/desktop-wallpaper/814/514/desktop-wallpaper-accounting-accountant.jpg")),
            BackgroundImageLayout = ImageLayout.Stretch,
        };
        this.Controls.Add(panelDerecho);
            //Abrimos la conexión con la base de datos
        using (var connection = new MySqlConnection("server=127.0.0.1;database=contabilidad;username=root;password=;"))
        {
        connection.Open();

        //Creamos la tabla transactions
        var command = new MySqlCommand("CREATE TABLE IF NOT EXISTS transactions (id INT AUTO_INCREMENT PRIMARY KEY, timestamp DATETIME, sender VARCHAR(255), receiver VARCHAR(255), amount DECIMAL(10,2), hash VARCHAR(255), publicKey VARCHAR(255), signature VARCHAR(255));", connection);
        command.ExecuteNonQuery();

        //Importamos el JSON en un objeto BlockChain
        string json = File.ReadAllText(@"blockchain.json");
        BlockChain blockchain = Newtonsoft.Json.JsonConvert.DeserializeObject<BlockChain>(json);

        //Llenamos la base de datos
        FillDatabase(blockchain, connection);
        }
    }
private static void FillDatabase(BlockChain blockchain, MySqlConnection connection)
  {
    //Comprobamos la validez de la cadena de bloques
    // if (!blockchain.isValid())
    // {
    //   //La cadena de bloques no es válida
    //   return;
    // }
    var command = new MySqlCommand("TRUNCATE TABLE transactions;", connection);
    command.ExecuteNonQuery();

    //Iteramos por los bloques de la cadena de bloques
    foreach (Block block in blockchain.blocks)
    {
      //Iteramos por las transacciones del bloque
      foreach (Transaction transaction in block.Transactions)
      {
        //Insertamos la transacción en la base de datos
        command = new MySqlCommand("INSERT INTO transactions (timestamp, sender, receiver, amount, hash, publicKey, signature) VALUES (@timestamp, @sender, @receiver, @amount, @hash, @publicKey, @signature)", connection);
        command.Parameters.AddWithValue("@timestamp", transaction.Timespamp);
        command.Parameters.AddWithValue("@sender", transaction.Sender);
        command.Parameters.AddWithValue("@receiver", transaction.Receiver);
        command.Parameters.AddWithValue("@amount", transaction.Amount);
        command.Parameters.AddWithValue("@hash", transaction.Hash);
        command.Parameters.AddWithValue("@publicKey", transaction.PublicKey);
        command.Parameters.AddWithValue("@signature", transaction.Signature);

        command.ExecuteNonQuery();
      }
    }
  }
    private void cargoPanelDerecho()
    {
        panelDerecho.Controls.Add(cmbCuentas);
        panelDerecho.Controls.Add(btnSeleccionarCuenta);
        panelDerecho.Controls.Add(btnVolverMenuTransacciones);
        panelDerecho.Controls.Add(btnNuevaTransaccion);
    }

    private void CargarCuentas()
    {
        cmbCuentas.Items.Clear();
        // Configurar el ComboBox con las cuentas disponibles.
        cmbCuentas.Items.Add("Seleccione una cuenta");
        cmbCuentas.Items.Add("Inventario de Productos Terminados");
        cmbCuentas.Items.Add("Vehículos");
        cmbCuentas.Items.Add("Terrenos y Edificios");
        cmbCuentas.Items.Add("Clientes por Ventas");
        cmbCuentas.Items.Add("Documentos por Cobrar");
        cmbCuentas.Items.Add("Cuentas por Pagar a Proveedores");
        cmbCuentas.Items.Add("Acreedores Diversos");
        cmbCuentas.Items.Add("Impuestos Pendientes");
        cmbCuentas.Items.Add("Préstamos a Largo Plazo");
        cmbCuentas.Items.Add("Cuenta Bancaria");
        cmbCuentas.Items.Add("Equipos de Oficina");
        cmbCuentas.Items.Add("Suministros");
        cmbCuentas.Items.Add("Clientes Incobrables");
        cmbCuentas.Items.Add("Regalías");
        cmbCuentas.Items.Add("Gastos de Publicidad");
        cmbCuentas.Items.Add("Intereses Acumulados");
        cmbCuentas.Items.Add("Dividendos Pendientes");
        cmbCuentas.Items.Add("Ingresos por Servicios");
        cmbCuentas.Items.Add("Inversiones a Corto Plazo");
        cmbCuentas.Items.Add("Gastos de Viaje y Entretenimiento");


        // Seleccionar la primera cuenta por defecto.
        cmbCuentas.SelectedIndex = 0;
    }

    private void MostrarMenuPrincipal()
    {
        // Mostrar los controles del menú principal
        lblTituloPrincipal.Visible = true;
        btnIniciarOperacion.Visible = true;
        btnVerLibroDiario.Visible = true;
        btnVerLibroMayor.Visible = true;
        btnSalir.Visible = true;
    }

    private void OcultarMenuPrincipal()
    {
        // Ocultar los controles del menú principal
        btnIniciarOperacion.Visible = false;
        btnVerLibroDiario.Visible = false;
        btnVerLibroMayor.Visible = false;
        btnSalir.Visible = false;
        lblTituloPrincipal.Visible = false;
    }


    private void MostrarMenuTransacciones()
    {
        // Mostrar los controles del menú de transacciones
        lblTituloPrincipal.Visible = false;
        btnIniciarTransferencia.Visible = true;
        btnIngresarEfectivo.Visible = true;
        btnRegistrarCuenta.Visible = true; ;
        btnVolver.Visible = true;
        lblTituloTransacciones.Visible = true;
        lblTituloRegistrarCuenta.Visible = false;
        btnSeleccionarCuenta.Visible = false;
        btnNuevaTransaccion.Visible = false;
    }
    public void MostrarCuentas()
    {
        dataGridViewLibroDiario.Visible = false;
        cmbCuentas.Visible = true;
        btnSeleccionarCuenta.Visible = true;
        btnVolverMenuTransacciones.Visible = true;
    }
    private void OcultarMenuTransacciones()
    {
        // Ocultar los controles del menú de transacciones
        btnIniciarTransferencia.Visible = false;
        btnIngresarEfectivo.Visible = false;
        btnRegistrarCuenta.Visible = false;
        btnVolver.Visible = false;
        lblTituloTransacciones.Visible = false;
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
    private void registrarCuenta(int opcion)
    {
        indiceCuenta++;

        btnSeleccionarCuenta.Visible = true;
        btnVolverMenuTransacciones.Visible = true;
        cmbCuentas.Visible = true;
        OcultarMenuTransacciones();
        CargarCuentas();

        string nombreCuenta = ObtenerNombreCuenta(opcion);

        if (existeCuenta(nombreCuenta))
        {
            MessageBox.Show("La cuenta ya existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string mensaje = $"Ingrese el capital para {nombreCuenta}:";

        decimal capital = ObtenerInputDecimal(mensaje, nombreCuenta);

        cuentas.Add(new Cuenta(nombreCuenta, capital, indiceCuenta));
        nodo1.newTransaction(cuentaCapital.Nombre, nombreCuenta, capital);

        if (!(nodo1.tempTransactions.Count < 1))
        {
            //MessageBox.Show("Se crea el bloque", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            nodo1.newBlock();
            nodo1.SaveBlockchainToFile(@"blockchain.json");
        }

        //// Verificar si hay bloques en nodo1
        // if (nodo1.blocks.Count > 0)
        // {
        //     // Verificar si hay transacciones en el primer bloque
        //     if (nodo1.blocks[0].Transactions != null && nodo1.blocks[0].Transactions.Length > 0)
        //     {
        //         MessageBox.Show($"{nodo1.blocks[0].Transactions[0].Amount},{nombreCuenta},{capital}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //     }
        //     else
        //     {
        //         MessageBox.Show("No hay transacciones en el primer bloque.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //     }
        // }
        // else
        // {
        //     MessageBox.Show("No hay bloques en nodo1.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        // }

    }

    private void btnSeleccionarCuenta_Click(object sender, EventArgs e)
    {
        int opcion = cmbCuentas.SelectedIndex;

        if (opcion > 0)
        {
            registrarCuenta(opcion);
        }
        else
        {
            MessageBox.Show("Por favor, seleccione una cuenta.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private decimal ObtenerInputDecimal(string mensaje, string nombreCuenta)
    {
        string input = Microsoft.VisualBasic.Interaction.InputBox(mensaje, "Ingrese el monto", "0", -1, -1);

        if (decimal.TryParse(input, out decimal result) && result >= 0)
        {
            MessageBox.Show($"Monto ingresado: {result} para la cuenta {nombreCuenta}", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return result;
        }
        else
        {
            MessageBox.Show("Por favor, ingrese un monto válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return -1; // Valor indicativo de error o entrada inválida
        }
    }

    private string ObtenerNombreCuenta(int opcion)
    {
        switch (opcion)
        {
            case 1: return "Inventario de Productos Terminados";
            case 2: return "Vehículos";
            case 3: return "Terrenos y Edificios";
            case 4: return "Clientes por Ventas";
            case 5: return "Documentos por Cobrar";
            case 6: return "Cuentas por Pagar a Proveedores";
            case 7: return "Acreedores Diversos";
            case 8: return "Impuestos Pendientes";
            case 9: return "Préstamos a Largo Plazo";
            case 10: return "Cuenta Bancaria";
            case 11: return "Equipos de Oficina";
            case 12: return "Suministros";
            case 13: return "Clientes Incobrables";
            case 14: return "Regalías";
            case 15: return "Gastos de Publicidad";
            case 16: return "Intereses Acumulados";
            case 17: return "Dividendos Pendientes";
            case 18: return "Ingresos por Servicios";
            case 19: return "Inversiones a Corto Plazo";
            case 20: return "Gastos de Viaje y Entretenimiento";
            default: return string.Empty;
        }
    }

    private decimal ingresarEfectivo()
    {
        string input = Microsoft.VisualBasic.Interaction.InputBox("Ingrese el monto:", "Ingrese el monto", "0", -1, -1);

        if (decimal.TryParse(input, out decimal result) && result >= 0)
        {
            MessageBox.Show($"Monto ingresado: {result} para la cuenta {cuentas[0].Nombre}", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Actualiza el saldo de la cuenta correctamente
            cuentas[0].Dinero += result;
            nodo1.newTransaction(cuentaCapital.Nombre, cuentas[0].Nombre, result);
            if (!(nodo1.tempTransactions.Count < 1))
            {
                nodo1.newBlock();
                nodo1.SaveBlockchainToFile(@"blockchain.json");
            }
            return result;
        }
        else
        {
            MessageBox.Show("Por favor, ingrese un monto válido (número no negativo).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return -1; // Valor indicativo de error o entrada inválida
        }
    }
    private void mostrarCuentas()
    {
        // Construir un mensaje con el listado de cuentas
        string message = "Seleccione una cuenta:\n";
        foreach (Cuenta c in cuentas)
        {
            //c.Id = n;
            message += $"{c.Id} - {c.Nombre}\n";
        }

        MessageBox.Show(message, "Seleccione la cuenta", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private int pedirN()
    {
        int result;
        if (int.TryParse(Microsoft.VisualBasic.Interaction.InputBox("Ingrese un número de cuenta:", "Entrada de usuario", "0"), out result))
        {
            return result;
        }
        else
        {
            // Manejar el caso en que la entrada no sea un número válido
            MessageBox.Show("Por favor, ingrese un número válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return -1; // Otra indicación de que la entrada es inválida
        }
    }
    private void btnVerLibroMayor_Click(object sender, EventArgs e)
    {
        // Obtener las fechas únicas
        List<DateOnly> listaFechas = ObtenerFechasUnicas();

        // Mostrar un formulario para que el usuario seleccione una fecha
        using (var formFechas = new Form())
        {
            // Configurar la ventana
            formFechas.Text = "Seleccione una fecha";
            formFechas.StartPosition = FormStartPosition.CenterScreen;

            // Crear un ListBox para mostrar las fechas
            ListBox listBoxFechas = new ListBox();
            listBoxFechas.Dock = DockStyle.Fill;

            // Agregar las fechas al ListBox
            foreach (var fecha in listaFechas)
            {
                listBoxFechas.Items.Add(fecha);
            }

            // Agregar el ListBox a la ventana
            formFechas.Controls.Add(listBoxFechas);
            // Botón para seleccionar la fecha
            Button btnSeleccionarFecha = new Button();
            btnSeleccionarFecha.Text = "Seleccionar Fecha";
            btnSeleccionarFecha.Height = 40; // Ajusta esta altura según tus necesidades
            btnSeleccionarFecha.Dock = DockStyle.Bottom;

            btnSeleccionarFecha.Click += (s, ev) =>
            {
                if (listBoxFechas.SelectedItem != null)
                {
                    formFechas.DialogResult = DialogResult.OK;
                    //formFechas.Close();
                }
                else
                {
                    MessageBox.Show("Por favor, seleccione una fecha.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Agregar el botón a la ventana
            formFechas.Controls.Add(btnSeleccionarFecha);
            // Mostrar el formulario para seleccionar la fecha
            // Mostrar el formulario para seleccionar la fecha
            if (formFechas.ShowDialog() == DialogResult.OK)
            {
                DateOnly fechaSeleccionada = (DateOnly)listBoxFechas.SelectedItem;

                // // Crear una sola instancia de DataGridView para el Libro Mayor
                // DataGridView dataGridViewLibroMayor = new DataGridView();
                // dataGridViewLibroMayor.Dock = DockStyle.Fill;
                // dataGridViewLibroMayor.ReadOnly = true; 
                // Crear un DataGridView para mostrar el Libro Diario
                DataGridView dataGridViewLibroMayor = new DataGridView
                {
                    Width = panelDerecho.Width - 250, // Ajusta el ancho para tener en cuenta el margen izquierdo
                    Height = panelDerecho.Height,
                    Location = new Point(250, 0), // Establece la ubicación para tener en cuenta el margen izquierdo
                    ReadOnly = true, // Hacer el DataGridView de solo lectura
                    BackgroundColor = Color.LightSteelBlue  // Establecer el color de fondo del DataGridView
                };

                // Configurar las columnas
                dataGridViewLibroMayor.Columns.Add("Cuenta", "Cuenta");
                dataGridViewLibroMayor.Columns.Add("Debe", "Debe");
                dataGridViewLibroMayor.Columns.Add("Haber", "Haber");
                dataGridViewLibroMayor.Columns.Add("Total", "Total");

                // Configurar las columnas para alineación
                dataGridViewLibroMayor.Columns["Debe"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridViewLibroMayor.Columns["Haber"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                // Configurar el color de fondo de la fila de encabezados de columnas
                dataGridViewLibroMayor.EnableHeadersVisualStyles = false;
                dataGridViewLibroMayor.ColumnHeadersDefaultCellStyle.BackColor = Color.LightSteelBlue;
                cuentaCapital.Dinero = 0;

                foreach (Cuenta c in cuentas)
                {
                    // Llenar el DataGridView con los datos del Libro Mayor
                    mostrarLibroMayorEnTabla(dataGridViewLibroMayor, c, fechaSeleccionada);
                }
                mostrarLibroMayorEnTabla(dataGridViewLibroMayor, cuentaCapital, fechaSeleccionada);

                // Agregar el DataGridView al panelDerecho
                panelDerecho.Controls.Clear(); // Limpia cualquier contenido previo
                panelDerecho.Controls.Add(dataGridViewLibroMayor);
            }
        }
    }

    private void mostrarLibroMayorEnTabla(DataGridView dataGridViewLibroMayor, Cuenta c, DateOnly fechaDeTransaccion)
    {
        decimal sumaDebe = 0, sumaHaber = 0;

        foreach (Block b in nodo1.blocks)
        {
            foreach (Transaction t in b.Transactions)
            {
                DateOnly fechaComparar = DateOnly.FromDateTime(t.Timespamp);
                if (t.Receiver.Equals(c.Nombre) && (fechaComparar <= fechaDeTransaccion))
                {
                    dataGridViewLibroMayor.Rows.Add(t.Receiver, t.Amount.ToString("C"), "");
                    sumaDebe += t.Amount;
                }
                else if (t.Sender.Equals(c.Nombre))
                {
                    dataGridViewLibroMayor.Rows.Add(t.Sender, "", t.Amount.ToString("C"));
                    sumaHaber += t.Amount;
                }
            }
        }

        if (!c.Nombre.Equals("Capital"))
        {
            dataGridViewLibroMayor.Rows.Add("", "","", (sumaDebe - sumaHaber).ToString("C"));
            cuentaCapital.Dinero += sumaDebe - sumaHaber;
        }
        else
        {
            dataGridViewLibroMayor.Rows.Add("", "","", c.Dinero.ToString("C"));
        }
    }
    private void MostrarLibroDiarioEnDataGridView()
    {
        lblTituloPrincipal.Visible = false;
        lblLibroDiario.Visible = true;
        btnIniciarOperacion.Visible = false;
        btnVerLibroDiario.Visible = false;
        btnVerLibroMayor.Visible = false;
        btnSalir.Visible = false;
        btnVolver.Visible = true;
        string espacioEnBlanco = "";
        if (nodo1.blocks.Count > 0)
        {
            // Crear un DataGridView para mostrar el Libro Diario
            DataGridView dataGridViewLibroDiario = new DataGridView
            {
                Width = panelDerecho.Width - 250, // Ajusta el ancho para tener en cuenta el margen izquierdo
                Height = panelDerecho.Height,
                Location = new Point(250, 0), // Establece la ubicación para tener en cuenta el margen izquierdo
                ReadOnly = true,
                BackgroundColor = Color.LightSteelBlue  // Establecer el color de fondo del DataGridView
            };
            // Agregar columnas al DataGridView según tus necesidades
            dataGridViewLibroDiario.Columns.Add("Timestamp", "Fecha");
            dataGridViewLibroDiario.Columns.Add("Cuenta", "Cuenta");
            dataGridViewLibroDiario.Columns.Add("Debe", "Debe");
            dataGridViewLibroDiario.Columns.Add("Haber", "Haber");

            // Configurar el color de fondo de la fila de encabezados de columnas
            dataGridViewLibroDiario.EnableHeadersVisualStyles = false;
            dataGridViewLibroDiario.ColumnHeadersDefaultCellStyle.BackColor = Color.LightSteelBlue;
            // Llenar el DataGridView con los datos del libro diario
            for (int i = 0; i < nodo1.blocks.Count; i++)
            {
                for (int j = 0; j < nodo1.blocks[i].Transactions.Length; j++)
                {
                    // Obtener los valores de la transacción
                    DateTime timestamp = nodo1.blocks[i].Transactions[j].Timespamp;
                    string fechaFormateada = timestamp.ToString("dd/MM/yyyy"); // Formato: Año-Mes-Día
                    string receiver = nodo1.blocks[i].Transactions[j].Receiver;
                    decimal amount = nodo1.blocks[i].Transactions[j].Amount;
                    string sender = nodo1.blocks[i].Transactions[j].Sender;

                    // Agregar una fila al DataGridView con los valores de la transacción
                    dataGridViewLibroDiario.Rows.Add(fechaFormateada, espacioEnBlanco, espacioEnBlanco);
                    dataGridViewLibroDiario.Rows.Add(espacioEnBlanco, receiver, amount);
                    dataGridViewLibroDiario.Rows.Add(espacioEnBlanco, sender, espacioEnBlanco, amount);
                }
            }
            // Agregar el DataGridView al panelDerecho
            panelDerecho.Controls.Clear(); // Limpia cualquier contenido previo
            panelDerecho.Controls.Add(dataGridViewLibroDiario);
        }
        else
        {
            MessageBox.Show("Libro Diario sin registros.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Función para obtener fechas únicas
    private List<DateOnly> ObtenerFechasUnicas()
    {
        List<DateOnly> listaFechas = new List<DateOnly>();
        foreach (Block b in nodo1.blocks)
        {
            foreach (Transaction t in b.Transactions)
            {
                DateOnly fecha = DateOnly.FromDateTime(t.Timespamp);
                listaFechas.Add(fecha);
            }
        }
        HashSet<DateOnly> listaFechasHashSet = new HashSet<DateOnly>(listaFechas);
        listaFechas = listaFechasHashSet.ToList<DateOnly>();
        return listaFechas;
    }


    private void nuevaTransferencia()
    {
        int idEmisora, idReceptora;
        bool dineroInsuficiente = false;
        // Selección de cuenta emisora
        MessageBox.Show("Seleccione la cuenta emisora:", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
        mostrarCuentas();
        idEmisora = pedirN();

        // Verificar la existencia de la cuenta emisora
        if (!existeCuenta(idEmisora))
        {
            MessageBox.Show("La cuenta emisora seleccionada no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Selección de cuenta receptora
        MessageBox.Show("Seleccione la cuenta receptora:", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
        mostrarCuentas();
        idReceptora = pedirN();

        // Verificar la existencia de la cuenta receptora y que no sea la misma que la emisora
        if (!existeCuenta(idReceptora) || idReceptora == idEmisora)
        {
            MessageBox.Show("La cuenta receptora seleccionada no es válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Ingreso del monto
        decimal amount;
        string inputAmount = Microsoft.VisualBasic.Interaction.InputBox("Ingrese el monto de la Transferencia:", "Entrada de usuario", "0");
        if (!decimal.TryParse(inputAmount, out amount) || amount < 1)
        {
            MessageBox.Show("Por favor, ingrese un monto válido mayor que cero.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Verificar si la cuenta emisora dispone de la cantidad de dinero
        if (amount > cuentas[idEmisora - 1].Dinero)
        {
            MessageBox.Show("La cuenta emisora no dispone de esa cantidad de dinero.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!dineroInsuficiente)
        {
            // Realizar la transacción
            nodo1.newTransaction(cuentas[idEmisora - 1].Nombre, cuentas[idReceptora - 1].Nombre, amount);
            cuentas[idEmisora - 1].Dinero -= amount;
            cuentas[idReceptora - 1].Dinero += amount;
            nodo1.newBlock();
            nodo1.SaveBlockchainToFile(@"blockchain.json");
        }


        MessageBox.Show("Transacción realizada con éxito.", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    //EVENTOS BTN_CLICK
    private void btnRegistrarCuenta_Click(object sender, EventArgs e)
    {
        lblTituloPrincipal.Visible = false;
        lblTituloRegistrarCuenta.Visible = true;
        cargoPanelDerecho();
        // Ocultar el menú de transacciones
        OcultarMenuTransacciones();
        //mostrar combo box
        MostrarCuentas();
    }
    private void btnIniciarTransferencia_Click(object sender, EventArgs e)
    {
        dataGridViewLibroDiario.Visible = false;
        bool cuentasVacias = true;
        foreach (Cuenta c in cuentas)
        {
            if (c.Dinero > 0)
            {
                cuentasVacias = false;
            }
        }
        if (!cuentasVacias)
        {
            if (cuentas.Count > 1)
            {
                nuevaTransferencia();
            }
            else
            {
                MessageBox.Show("Debe registrar al menos una cuenta...", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        else
        {
            MessageBox.Show("Todas las cuentas están vacías...", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //Console.WriteLine("Todas las cuentas están vacías...");
        }
    }
    private void btnVolver_Click(object sender, EventArgs e)
    {
        // Ocultar el menú de transacciones
        OcultarMenuTransacciones();
        OcultarLibroDiario();
        // Mostrar el menú principal
        MostrarMenuPrincipal();
    }
    private void btnVolverMenuTransacciones_Click(object sender, EventArgs e)
    {
        lblTituloRegistrarCuenta.Visible = false;
        btnVolverMenuTransacciones.Visible = false;
        cmbCuentas.Visible = false;
        dataGridViewLibroDiario.Visible = false;
        // Mostrar el menú transacciones
        MostrarMenuTransacciones();
    }
    private void btnIngresarEfectivo_Click(object sender, EventArgs e)
    {
        dataGridViewLibroDiario.Visible = false;
        ingresarEfectivo();
    }
    private void btnIniciarOperacion_Click(object sender, EventArgs e)
    {
        dataGridViewLibroDiario.Visible = false;
        // Ocultar el menú principal
        OcultarMenuPrincipal();
        OcultarLibroDiario();

        // Mostrar el menú de transacciones
        MostrarMenuTransacciones();
    }

    private void btnVerLibroDiario_Click(object sender, EventArgs e)
    {
        lblLibroDiario.Visible = true;
        MostrarLibroDiarioEnDataGridView();
    }



    private void btnSalir_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        cuentas.Add(new Cuenta("Caja", 0, indiceCuenta));
        // Código que se ejecuta al cargar el formulario.
    }

    private void OcultarLibroDiario()
    {
        // Aquí puedes ajustar la lógica según las necesidades de ocultar el libro diario
        lblTituloPrincipal.Visible = true;
        lblLibroDiario.Visible = false;

        // Buscar el DataGridView del libro diario en los controles del panelDerecho
        DataGridView dataGridViewLibroDiario = panelDerecho.Controls.OfType<DataGridView>().FirstOrDefault();

        if (dataGridViewLibroDiario != null)
        {
            // Ocultar el DataGridView en lugar de borrar todos los controles
            dataGridViewLibroDiario.Visible = false;
        }
    }

}
