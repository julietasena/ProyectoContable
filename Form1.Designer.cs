using System.Drawing.Drawing2D;
using System.Net;

namespace SistemaContable;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        //botones
        this.btnIniciarOperacion = new System.Windows.Forms.Button();
        this.btnVerLibroDiario = new System.Windows.Forms.Button();
        this.btnVerLibroMayor = new System.Windows.Forms.Button();
        this.btnSalir = new System.Windows.Forms.Button();
        this.btnIniciarTransferencia = new System.Windows.Forms.Button();
        this.btnIngresarEfectivo = new System.Windows.Forms.Button();
        this.btnRegistrarCuenta = new System.Windows.Forms.Button();
        this.btnVolver = new System.Windows.Forms.Button();
        this.btnSeleccionarCuenta = new System.Windows.Forms.Button();
        this.btnNuevaTransaccion = new System.Windows.Forms.Button();
        this.btnVolverMenuTransacciones = new System.Windows.Forms.Button();
        this.dataGridViewLibroDiario = new DataGridView();
        this.lblTituloPrincipal = new Label();
        this.lblTituloTransacciones = new Label();
        this.lblTituloRegistrarCuenta = new Label();
        this.lblLibroDiario = new Label();
        this.lblTitulo = new Label();

        Panel panelIzquierdo = new Panel
        {
            Dock = DockStyle.Left,
            Width = 250,
            BackColor = Color.LightSteelBlue
        };
        this.Controls.Add(panelIzquierdo);

        // Panel panelDerecho = new Panel
        // {
        //     Dock = DockStyle.Fill,
        //     BackColor = Color.White,
        //     BackgroundImage = new Bitmap(new WebClient().OpenRead("https://e1.pxfuel.com/desktop-wallpaper/814/514/desktop-wallpaper-accounting-accountant.jpg")),
        //     BackgroundImageLayout = ImageLayout.Stretch
        // };
        // this.Controls.Add(panelDerecho);

        //Título para el Inicio
        this.lblTitulo.Location = new System.Drawing.Point(50, 80);
        this.lblTitulo.TextAlign = ContentAlignment.MiddleLeft;
        this.lblTitulo.Font = new Font("Arial", 15, FontStyle.Bold);
        this.lblTitulo.BackColor = Color.Transparent;
        this.lblTitulo.Text = "SISTEMA CONTABLE";
        this.lblTitulo.Name = "lblTitulo";
        this.lblTitulo.Size = new System.Drawing.Size(210, 90);
        panelIzquierdo.Controls.Add(lblTitulo);

        // Título para el menú principal
        this.lblTituloPrincipal.Location = new System.Drawing.Point(35, 170);
        this.lblTituloPrincipal.Font = new Font("Arial", 15, FontStyle.Regular);
        this.lblTituloPrincipal.BackColor = Color.Transparent;
        this.lblTituloPrincipal.Text = "Menú Principal";
        this.lblTituloPrincipal.Name = "lblTituloPrincipal";
        this.lblTituloPrincipal.Size = new System.Drawing.Size(210, 30);
        panelIzquierdo.Controls.Add(lblTituloPrincipal);

        // Título para el menú de transacciones
        this.lblTituloTransacciones.Location = new System.Drawing.Point(35, 170);
        this.lblTituloTransacciones.Font = new Font("Arial", 15, FontStyle.Regular);
        this.lblTituloTransacciones.BackColor = Color.Transparent;
        this.lblTituloTransacciones.Text = "Menú de Transacciones";
        this.lblTituloTransacciones.Name = "lblTituloTransacciones";
        this.lblTituloTransacciones.Size = new System.Drawing.Size(250, 90);
        panelIzquierdo.Controls.Add(lblTituloTransacciones);

        // Título para el libro diario
        this.lblLibroDiario.Location = new System.Drawing.Point(50, 170);
        this.lblLibroDiario.Font = new Font("Arial", 15, FontStyle.Regular);
        this.lblLibroDiario.BackColor = Color.Transparent;
        this.lblLibroDiario.Text = "Libro Diario";
        this.lblLibroDiario.Name = "lblLibroDiario";
        this.lblLibroDiario.Size = new System.Drawing.Size(250, 40);
        panelIzquierdo.Controls.Add(lblLibroDiario);
        
        // Título registrar cuenta
        this.lblTituloRegistrarCuenta.Location = new System.Drawing.Point(10, 170);
        this.lblTituloRegistrarCuenta.Font = new Font("Arial", 15, FontStyle.Regular);
        this.lblTituloRegistrarCuenta.Text = "Registrar una cuenta";
        this.lblTituloRegistrarCuenta.Name = "lblTituloRegistrarCuenta";
        this.lblTituloRegistrarCuenta.Size = new System.Drawing.Size(250, 30);
        panelIzquierdo.Controls.Add(lblTituloRegistrarCuenta);
        
        //MENÚ PRINCIPAL

        // btnIniciarOperacion
        this.btnIniciarOperacion.Location = new System.Drawing.Point(50, 280);
        this.btnIniciarOperacion.Name = "btnIniciarOperacion";
        this.btnIniciarOperacion.Size = new System.Drawing.Size(150, 30);
        this.btnIniciarOperacion.Text = "Iniciar operación";
        this.btnIniciarOperacion.Click += new System.EventHandler(this.btnIniciarOperacion_Click);

        // btnVerLibroDiario
        this.btnVerLibroDiario.Location = new System.Drawing.Point(50, 320);
        this.btnVerLibroDiario.Name = "btnVerLibroDiario";
        this.btnVerLibroDiario.Size = new System.Drawing.Size(150, 30);
        this.btnVerLibroDiario.Text = "Ver libro diario";
        this.btnVerLibroDiario.Click += new System.EventHandler(this.btnVerLibroDiario_Click);

        // btnVerLibroMayor
        this.btnVerLibroMayor.Location = new System.Drawing.Point(50, 360);
        this.btnVerLibroMayor.Name = "btnVerLibroMayor";
        this.btnVerLibroMayor.Size = new System.Drawing.Size(150, 30);
        this.btnVerLibroMayor.Text = "Ver libro mayor";
        this.btnVerLibroMayor.Click += new System.EventHandler(this.btnVerLibroMayor_Click);

        // btnSalir
        this.btnSalir.Location = new System.Drawing.Point(50, 400);
        this.btnSalir.Name = "btnSalir";
        this.btnSalir.Size = new System.Drawing.Size(150, 30);
        this.btnSalir.Text = "Salir";
        this.btnSalir.Click += new System.EventHandler(this.btnSalir_Click);

        //MENÚ TRANSACCIONES
        // btnIniciarTransferencia
        this.btnIniciarTransferencia.Location = new System.Drawing.Point(25, 280);
        this.btnIniciarTransferencia.Name = "btnIniciarTransferencia";
        this.btnIniciarTransferencia.Size = new System.Drawing.Size(200, 30);
        this.btnIniciarTransferencia.Text = "Iniciar una transferencia";
        this.btnIniciarTransferencia.Click += new System.EventHandler(this.btnIniciarTransferencia_Click);

        // btnIngresarEfectivo
        this.btnIngresarEfectivo.Location = new System.Drawing.Point(25, 320);
        this.btnIngresarEfectivo.Name = "btnIngresarEfectivo";
        this.btnIngresarEfectivo.Size = new System.Drawing.Size(200, 30);
        this.btnIngresarEfectivo.Text = "Ingresar efectivo";
        this.btnIngresarEfectivo.Click += new System.EventHandler(this.btnIngresarEfectivo_Click);

        // btnRegistrarCuenta
        this.btnRegistrarCuenta.Location = new System.Drawing.Point(25, 360);
        this.btnRegistrarCuenta.Name = "btnRegistrarCuenta";
        this.btnRegistrarCuenta.Size = new System.Drawing.Size(200, 30);
        this.btnRegistrarCuenta.Text = "Registrar una cuenta";
        this.btnRegistrarCuenta.Click += new System.EventHandler(this.btnRegistrarCuenta_Click);

        // btnVolver
        this.btnVolver.Location = new System.Drawing.Point(25, 400);
        this.btnVolver.Name = "btnVolver";
        this.btnVolver.Size = new System.Drawing.Size(200, 30);
        this.btnVolver.Text = "Volver al menú principal";
        this.btnVolver.Click += new System.EventHandler(this.btnVolver_Click);

        // cmbCuentas
        this.cmbCuentas = new System.Windows.Forms.ComboBox();
        this.cmbCuentas.Location = new System.Drawing.Point(400, 40);
        this.cmbCuentas.Name = "cmbCuentas";
        this.cmbCuentas.Size = new System.Drawing.Size(250, 21);
        this.cmbCuentas.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        panelDerecho.Controls.Add(cmbCuentas);

        //seleccionar cuenta
        this.btnSeleccionarCuenta.Location = new System.Drawing.Point(400, 280);
        this.btnSeleccionarCuenta.Name = "btnSeleccionarCuenta";
        this.btnSeleccionarCuenta.Size = new System.Drawing.Size(250, 30);
        this.btnSeleccionarCuenta.Text = "Seleccionar Cuenta";
        this.btnSeleccionarCuenta.Click += new System.EventHandler(this.btnSeleccionarCuenta_Click);

        //btnVolverMenuTransacciones
        this.btnVolverMenuTransacciones.Location = new System.Drawing.Point(400, 340);
        this.btnVolverMenuTransacciones.Name = "btnVolverMenuTransacciones";
        this.btnVolverMenuTransacciones.Size = new System.Drawing.Size(250, 30);
        this.btnVolverMenuTransacciones.Text = "Volver al menú de transacciones";
        this.btnVolverMenuTransacciones.Click += new System.EventHandler(this.btnVolverMenuTransacciones_Click);

        // btnNuevaTransaccion
        this.btnNuevaTransaccion = new System.Windows.Forms.Button();
        this.btnNuevaTransaccion.Location = new System.Drawing.Point(400, 380);
        this.btnNuevaTransaccion.Name = "btnOK";
        this.btnNuevaTransaccion.Size = new System.Drawing.Size(250, 30);
        this.btnNuevaTransaccion.Text = "Realizar una nueva transaccion";
        this.btnNuevaTransaccion.Click += new System.EventHandler(this.btnIniciarOperacion_Click);
        // Agregar otros controles según sea necesario.

        //...

        //Oculto todo excepto el menu principal
        btnIniciarTransferencia.Hide();
        btnIngresarEfectivo.Hide();
        btnRegistrarCuenta.Hide();
        btnVolver.Hide();
        cmbCuentas.Hide();
        btnNuevaTransaccion.Hide();
        btnVolverMenuTransacciones.Hide();
        dataGridViewLibroDiario.Hide();
        lblTituloTransacciones.Hide();
        lblTituloRegistrarCuenta.Hide();
        btnSeleccionarCuenta.Hide();
        lblLibroDiario.Hide();

        // FormPrincipal
        this.ClientSize = new System.Drawing.Size(800, 500);
        panelIzquierdo.Controls.Add(btnIniciarOperacion);
        panelIzquierdo.Controls.Add(btnVerLibroDiario);
        panelIzquierdo.Controls.Add(btnVerLibroMayor);
        panelIzquierdo.Controls.Add(btnSalir);
        panelIzquierdo.Controls.Add(btnIniciarTransferencia);
        panelIzquierdo.Controls.Add(btnIngresarEfectivo);
        panelIzquierdo.Controls.Add(btnRegistrarCuenta);
        panelIzquierdo.Controls.Add(btnVolver);

        panelDerecho.Controls.Add(btnNuevaTransaccion);        
        panelDerecho.Controls.Add(btnSeleccionarCuenta);
        panelDerecho.Controls.Add(btnVolverMenuTransacciones);
        

        //...

        this.Name = "FormPrincipal";
        this.Text = "Sistema Contable";
        this.Load += new System.EventHandler(this.Form1_Load);
        this.ResumeLayout(false);
    }

    //...

    #endregion

    private System.Windows.Forms.Button btnIniciarOperacion;
    private System.Windows.Forms.Button btnVerLibroDiario;
    private System.Windows.Forms.Button btnVerLibroMayor;
    private System.Windows.Forms.Button btnSalir;
    private System.Windows.Forms.Button btnIniciarTransferencia;
    private System.Windows.Forms.Button btnIngresarEfectivo;
    private System.Windows.Forms.Button btnRegistrarCuenta;
    private System.Windows.Forms.Button btnVolver;
    private System.Windows.Forms.Button btnSeleccionarCuenta;
    private System.Windows.Forms.Button btnNuevaTransaccion;
    private System.Windows.Forms.Button btnVolverMenuTransacciones;
    private System.Windows.Forms.Label lblTituloPrincipal;
    private System.Windows.Forms.Label lblTituloTransacciones;
    private System.Windows.Forms.Label lblTituloRegistrarCuenta;
    private System.Windows.Forms.Label lblLibroDiario;
    private System.Windows.Forms.Label lblTitulo;

    public object Properties { get; private set; }
}
