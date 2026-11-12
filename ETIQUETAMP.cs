// **************************************************
// Custom code for BAQReportForm — CONSULTA PARTE
// Botón IMPRIMIR (EPGUID 0ba56231-ba65-4553-a8b3-4aec186f24fb):
//   Etiqueta 4 x 2 in F-CFX-AMP-01 REV.02 (Parte, Lote, Cant. + UM, FT, recuadro y QR)
//   QR = Parte <TAB> Descripción <TAB> Cantidad <TAB> UM <TAB> Lote <TAB> FT
//   Ventana propia con vista previa: impresora, copias, Preferencias del driver, guardar PDF/PNG
// No requiere referencias adicionales (la parte se lee con SearchFunctions.listLookup).
// **************************************************
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Ice.BO;
using Ice.UI;
using Ice.Lib;
using Ice.Adapters;
using Ice.Lib.Customization;
using Ice.Lib.ExtendedProps;
using Ice.Lib.Framework;
using Ice.Lib.Searches;
using Ice.UI.FormFunctions;

public class Script
{
	// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **
	// Begin Wizard Added Module Level Variables **

	// End Wizard Added Module Level Variables **

	// Add Custom Module Level Variables Here **
	private const string GuidBotonImprimir = "0ba56231-ba65-4553-a8b3-4aec186f24fb";
	private const string GuidParte = "f86e7fb0-4af6-472b-9157-cbe26942b40a";
	private const string GuidCantidad = "56672f06-d2fe-4bad-a6a6-d174ea19a5bb";
	private const string GuidLote = "7cc749c2-44a1-4446-a6c4-3dac47f0ec03";
	private const string GuidFt = "395ebed5-a619-4904-bd00-102af239f101";
	private const string TituloEtiqueta = "F-CFX-AMP-01 REV.02";

	private EpiButton btnImprimir;

	public void InitializeCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
		// Begin Wizard Added Variable Initialization

		// End Wizard Added Variable Initialization

		// Begin Wizard Added Custom Method Calls

		// End Wizard Added Custom Method Calls

		btnImprimir = csm.GetNativeControlReference(GuidBotonImprimir) as EpiButton;
		if (btnImprimir != null)
			btnImprimir.Click += new EventHandler(btnImprimir_Click);
	}

	public void DestroyCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
		// Begin Wizard Added Object Disposal

		// End Wizard Added Object Disposal

		// Begin Custom Code Disposal
		if (btnImprimir != null)
			btnImprimir.Click -= new EventHandler(btnImprimir_Click);
		btnImprimir = null;
		// End Custom Code Disposal
	}

	private void btnImprimir_Click(object sender, EventArgs e)
	{
		try
		{
			EtiquetaDatos datos = LeerDatosFormulario();
			if (datos == null)
				return;

			ImprimirEtiqueta(datos);
		}
		catch (Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	///////////////////////////////// DATOS DE LA ETIQUETA /////////////////////////////////
	private class EtiquetaDatos
	{
		public string Parte = string.Empty;
		public string Descripcion = string.Empty;
		public string Cantidad = string.Empty;
		public string Unidad = string.Empty;
		public string Lote = string.Empty;
		public string Ft = string.Empty;

		public string TextoQr()
		{
			return Parte + "\t" + Descripcion + "\t" + Cantidad + "\t" + Unidad + "\t" + Lote + "\t" + Ft;
		}
	}

	private string LeerCampo(string guid, string columna)
	{
		Control control = csm.GetNativeControlReference(guid) as Control;
		if (control != null && !string.IsNullOrWhiteSpace(control.Text))
			return control.Text.Trim();

		try
		{
			EpiDataView edv = oTrans.EpiDataViews["ReportParam"] as EpiDataView;
			if (edv != null && edv.Row >= 0 && edv.dataView.Table.Columns.Contains(columna))
			{
				object valor = edv.dataView[edv.Row][columna];
				if (valor != null && valor != DBNull.Value)
					return valor.ToString().Trim();
			}
		}
		catch
		{
		}

		return string.Empty;
	}

	private EtiquetaDatos LeerDatosFormulario()
	{
		EtiquetaDatos datos = new EtiquetaDatos();
		datos.Parte = LeerCampo(GuidParte, "field1");
		datos.Cantidad = LeerCampo(GuidCantidad, "Character02");
		datos.Lote = LeerCampo(GuidLote, "Character01");
		datos.Ft = LeerCampo(GuidFt, "Character03");

		if (datos.Parte.Length == 0)
		{
			MessageBox.Show("Capture el número de parte.", "Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return null;
		}

		if (datos.Cantidad.Length == 0)
		{
			MessageBox.Show("Capture la cantidad.", "Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return null;
		}

		if (!LeerParte(datos))
		{
			MessageBox.Show("La parte '" + datos.Parte + "' no existe en Epicor.", "Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return null;
		}

		return datos;
	}

	private bool LeerParte(EtiquetaDatos datos)
	{
		bool encontrado;
		string where = "PartNum = '" + datos.Parte.Replace("'", "''") + "'";
		DataSet ds = SearchFunctions.listLookup(oTrans, "PartAdapter", out encontrado, false, where);
		if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
			return false;

		DataRow row = ds.Tables[0].Rows[0];
		datos.Parte = ValorFila(row, "PartNum");
		datos.Descripcion = ValorFila(row, "PartDescription").Replace("\r", " ").Replace("\n", " ");
		datos.Unidad = ValorFila(row, "IUM");
		return datos.Parte.Length > 0;
	}

	private static string ValorFila(DataRow row, string columna)
	{
		if (row == null || !row.Table.Columns.Contains(columna) || row[columna] == DBNull.Value)
			return string.Empty;
		return Convert.ToString(row[columna]).Trim();
	}

	///////////////////////////////// IMPRESIÓN /////////////////////////////////
	private static string ArchivoUltimaImpresora()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Codiflex\EtiquetaConsultaParte\impresora.txt");
	}

	private static string LeerUltimaImpresora()
	{
		try
		{
			string archivo = ArchivoUltimaImpresora();
			return File.Exists(archivo) ? File.ReadAllText(archivo).Trim() : string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static void GuardarUltimaImpresora(string impresora)
	{
		try
		{
			string archivo = ArchivoUltimaImpresora();
			Directory.CreateDirectory(Path.GetDirectoryName(archivo));
			File.WriteAllText(archivo, impresora ?? string.Empty);
		}
		catch
		{
		}
	}

	private void ImprimirEtiqueta(EtiquetaDatos datos)
	{
		using (DialogoImpresion dialogo = new DialogoImpresion(datos))
			dialogo.ShowDialog();
	}

	[System.Runtime.InteropServices.DllImport("winspool.drv", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
	private static extern int DocumentProperties(IntPtr hwnd, IntPtr hPrinter, string pDeviceName, IntPtr pDevModeOutput, IntPtr pDevModeInput, int fMode);

	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern IntPtr GlobalLock(IntPtr hMem);

	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern bool GlobalUnlock(IntPtr hMem);

	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern IntPtr GlobalFree(IntPtr hMem);

	private static bool MostrarPreferenciasImpresora(IWin32Window dueno, PrinterSettings ajustes)
	{
		const int DM_OUT_BUFFER = 2, DM_IN_PROMPT = 4, DM_IN_BUFFER = 8, IDOK = 1;

		IntPtr hDevMode = ajustes.GetHdevmode(ajustes.DefaultPageSettings);
		IntPtr salida = IntPtr.Zero;
		try
		{
			int tam = DocumentProperties(dueno.Handle, IntPtr.Zero, ajustes.PrinterName, IntPtr.Zero, IntPtr.Zero, 0);
			if (tam <= 0)
				return false;

			salida = System.Runtime.InteropServices.Marshal.AllocHGlobal(tam);
			IntPtr entrada = GlobalLock(hDevMode);
			int r = DocumentProperties(dueno.Handle, IntPtr.Zero, ajustes.PrinterName, salida, entrada, DM_IN_BUFFER | DM_IN_PROMPT | DM_OUT_BUFFER);
			GlobalUnlock(hDevMode);

			if (r != IDOK)
				return false;

			ajustes.SetHdevmode(salida);
			ajustes.DefaultPageSettings.SetHdevmode(salida);
			return true;
		}
		finally
		{
			if (salida != IntPtr.Zero)
				System.Runtime.InteropServices.Marshal.FreeHGlobal(salida);
			GlobalFree(hDevMode);
		}
	}

	private static Bitmap RenderizarEtiqueta(EtiquetaDatos d, int dpi)
	{
		Bitmap bmp = new Bitmap(4 * dpi, 2 * dpi);
		bmp.SetResolution(dpi, dpi);
		using (Graphics g = Graphics.FromImage(bmp))
		{
			g.Clear(Color.White);
			DibujarEtiqueta(g, d);
		}
		return bmp;
	}

	private static void GuardarPdf(Bitmap bmp, string ruta)
	{
		int w = bmp.Width, h = bmp.Height, fila = (w + 7) / 8;
		int[] px = new int[w * h];
		System.Drawing.Imaging.BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h),
			System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		try
		{
			for (int y = 0; y < h; y++)
				System.Runtime.InteropServices.Marshal.Copy(new IntPtr(bd.Scan0.ToInt64() + (long)y * bd.Stride), px, y * w, w);
		}
		finally
		{
			bmp.UnlockBits(bd);
		}

		byte[] imagen = new byte[fila * h];
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				int c = px[y * w + x];
				int gris = ((c >> 16) & 255) * 30 + ((c >> 8) & 255) * 59 + (c & 255) * 11;
				if (gris >= 12800)
					imagen[y * fila + x / 8] |= (byte)(0x80 >> (x % 8));
			}

		System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
		string anchoPt = (w * 72.0 / bmp.HorizontalResolution).ToString("0.###", inv);
		string altoPt = (h * 72.0 / bmp.VerticalResolution).ToString("0.###", inv);
		string contenido = "q " + anchoPt + " 0 0 " + altoPt + " 0 0 cm /Im0 Do Q";

		using (MemoryStream ms = new MemoryStream())
		{
			long[] offsets = new long[6];
			Action<string> escribir = delegate(string s) { byte[] b = Encoding.ASCII.GetBytes(s); ms.Write(b, 0, b.Length); };

			escribir("%PDF-1.4\n");
			offsets[1] = ms.Position; escribir("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
			offsets[2] = ms.Position; escribir("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
			offsets[3] = ms.Position; escribir("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + anchoPt + " " + altoPt + "] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");
			offsets[4] = ms.Position; escribir("4 0 obj\n<< /Type /XObject /Subtype /Image /Width " + w + " /Height " + h + " /ColorSpace /DeviceGray /BitsPerComponent 1 /Length " + imagen.Length + " >>\nstream\n");
			ms.Write(imagen, 0, imagen.Length);
			escribir("\nendstream\nendobj\n");
			offsets[5] = ms.Position; escribir("5 0 obj\n<< /Length " + contenido.Length + " >>\nstream\n" + contenido + "\nendstream\nendobj\n");

			long xref = ms.Position;
			escribir("xref\n0 6\n0000000000 65535 f \n");
			for (int i = 1; i <= 5; i++)
				escribir(offsets[i].ToString("D10", inv) + " 00000 n \n");
			escribir("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref.ToString(inv) + "\n%%EOF\n");

			File.WriteAllBytes(ruta, ms.ToArray());
		}
	}

	///////////////////////////////// VENTANA DE IMPRESIÓN CON VISTA PREVIA /////////////////////////////////
	private class DialogoImpresion : Form
	{
		private readonly EtiquetaDatos datos;
		private readonly ComboBox cboImpresora = new ComboBox();
		private readonly NumericUpDown numCopias = new NumericUpDown();
		private readonly PictureBox picVista = new PictureBox();
		private readonly Label lblTamano = new Label();
		private PrinterSettings ajustes;
		private Bitmap vista;

		public DialogoImpresion(EtiquetaDatos d)
		{
			datos = d;

			Text = "Imprimir etiqueta — " + d.Parte;
			StartPosition = FormStartPosition.CenterScreen;
			FormBorderStyle = FormBorderStyle.Sizable;
			MinimizeBox = false;
			ShowInTaskbar = false;
			Font = new Font("Segoe UI", 9f);
			ClientSize = new Size(940, 520);
			MinimumSize = new Size(820, 480);

			Panel izquierdo = new Panel();
			izquierdo.Dock = DockStyle.Left;
			izquierdo.Width = 300;
			izquierdo.Padding = new Padding(16, 14, 12, 8);

			Panel inferior = new Panel();
			inferior.Dock = DockStyle.Bottom;
			inferior.Height = 56;
			inferior.BackColor = Color.FromArgb(243, 243, 243);

			Panel fondoVista = new Panel();
			fondoVista.Dock = DockStyle.Fill;
			fondoVista.BackColor = Color.FromArgb(214, 214, 214);
			fondoVista.Padding = new Padding(24, 34, 24, 24);

			Label lblVista = new Label();
			lblVista.Text = "Vista previa (4 x 2 in)";
			lblVista.AutoSize = true;
			lblVista.Location = new Point(24, 10);
			lblVista.ForeColor = Color.FromArgb(64, 64, 64);
			lblVista.BackColor = Color.Transparent;

			picVista.Dock = DockStyle.Fill;
			picVista.SizeMode = PictureBoxSizeMode.Zoom;
			picVista.BackColor = Color.Transparent;
			fondoVista.Controls.Add(picVista);
			fondoVista.Controls.Add(lblVista);
			lblVista.BringToFront();

			int y = 14;
			izquierdo.Controls.Add(Etiqueta("Impresora", 16, y, true)); y += 22;
			cboImpresora.DropDownStyle = ComboBoxStyle.DropDownList;
			cboImpresora.SetBounds(16, y, 268, 26);
			foreach (string p in PrinterSettings.InstalledPrinters)
				cboImpresora.Items.Add(p);
			izquierdo.Controls.Add(cboImpresora); y += 32;

			Button btnPreferencias = new Button();
			btnPreferencias.Text = "Preferencias de impresión...";
			btnPreferencias.SetBounds(16, y, 268, 30);
			btnPreferencias.Click += new EventHandler(btnPreferencias_Click);
			izquierdo.Controls.Add(btnPreferencias); y += 44;

			izquierdo.Controls.Add(Etiqueta("Copias", 16, y, true)); y += 22;
			numCopias.Minimum = 1;
			numCopias.Maximum = 999;
			numCopias.Value = 1;
			numCopias.SetBounds(16, y, 100, 26);
			izquierdo.Controls.Add(numCopias); y += 40;

			izquierdo.Controls.Add(Etiqueta("Tamaño de etiqueta", 16, y, true)); y += 22;
			lblTamano.SetBounds(16, y, 268, 20);
			izquierdo.Controls.Add(lblTamano); y += 34;

			izquierdo.Controls.Add(Etiqueta("Datos de la etiqueta", 16, y, true)); y += 22;
			Label lblDatos = new Label();
			lblDatos.SetBounds(16, y, 268, 170);
			lblDatos.Text = "Parte: " + d.Parte + "\r\n"
				+ "Descripción: " + d.Descripcion + "\r\n"
				+ "Cantidad: " + d.Cantidad + " " + d.Unidad + "\r\n"
				+ "Lote: " + d.Lote + "\r\n"
				+ "FT: " + d.Ft;
			izquierdo.Controls.Add(lblDatos);

			Button btnImprimir = Boton("Imprimir", 120);
			Button btnCancelar = Boton("Cancelar", 100);
			Button btnGuardar = Boton("Guardar como...", 140);
			btnImprimir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnCancelar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnGuardar.Location = new Point(16, 13);
			btnCancelar.Location = new Point(inferior.Width - 116, 13);
			btnImprimir.Location = new Point(inferior.Width - 246, 13);
			btnImprimir.Click += new EventHandler(btnImprimir_Click);
			btnCancelar.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
			btnGuardar.Click += new EventHandler(btnGuardar_Click);
			inferior.Controls.Add(btnGuardar);
			inferior.Controls.Add(btnImprimir);
			inferior.Controls.Add(btnCancelar);
			inferior.Resize += delegate
			{
				btnCancelar.Left = inferior.ClientSize.Width - btnCancelar.Width - 16;
				btnImprimir.Left = btnCancelar.Left - btnImprimir.Width - 10;
			};

			Controls.Add(fondoVista);
			Controls.Add(izquierdo);
			Controls.Add(inferior);
			AcceptButton = btnImprimir;
			CancelButton = btnCancelar;

			cboImpresora.SelectedIndexChanged += new EventHandler(cboImpresora_SelectedIndexChanged);
			string ultima = LeerUltimaImpresora();
			string predeterminada = new PrinterSettings().PrinterName;
			int idx = cboImpresora.Items.IndexOf(ultima);
			if (idx < 0)
				idx = cboImpresora.Items.IndexOf(predeterminada);
			if (idx < 0 && cboImpresora.Items.Count > 0)
				idx = 0;
			cboImpresora.SelectedIndex = idx;

			vista = RenderizarEtiqueta(datos, 300);
			using (Graphics g = Graphics.FromImage(vista))
			using (Pen borde = new Pen(Color.FromArgb(150, 150, 150), 3f))
				g.DrawRectangle(borde, 1, 1, vista.Width - 3, vista.Height - 3);
			picVista.Image = vista;
		}

		private static Label Etiqueta(string texto, int x, int y, bool negrita)
		{
			Label l = new Label();
			l.Text = texto;
			l.AutoSize = true;
			l.Location = new Point(x, y);
			if (negrita)
				l.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
			return l;
		}

		private static Button Boton(string texto, int ancho)
		{
			Button b = new Button();
			b.Text = texto;
			b.Size = new Size(ancho, 30);
			return b;
		}

		private void cboImpresora_SelectedIndexChanged(object sender, EventArgs e)
		{
			string nombre = cboImpresora.SelectedItem as string;
			ajustes = new PrinterSettings();
			if (!string.IsNullOrEmpty(nombre))
				ajustes.PrinterName = nombre;
			ActualizarTamano();
		}

		private void ActualizarTamano()
		{
			using (PrintDocument doc = new PrintDocument())
			{
				doc.PrinterSettings = ajustes;
				AjustarPapel(doc);
				PaperSize ps = doc.DefaultPageSettings.PaperSize;
				lblTamano.Text = "4 x 2 in  (" + ps.PaperName + ")";
			}
		}

		private void btnPreferencias_Click(object sender, EventArgs e)
		{
			if (ajustes == null || !ajustes.IsValid)
				return;

			try
			{
				short copias = (short)numCopias.Value;
				if (MostrarPreferenciasImpresora(this, ajustes))
				{
					if (ajustes.Copies > 1)
						numCopias.Value = Math.Min(numCopias.Maximum, ajustes.Copies);
					else
						ajustes.Copies = copias;
					ActualizarTamano();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "No se pudieron abrir las preferencias de la impresora.\r\n\r\n" + ex.Message,
					"Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void btnImprimir_Click(object sender, EventArgs e)
		{
			if (ajustes == null || !ajustes.IsValid)
			{
				MessageBox.Show(this, "Seleccione una impresora válida.", "Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			try
			{
				Cursor = Cursors.WaitCursor;
				using (PrintDocument doc = new PrintDocument())
				{
					doc.DocumentName = "Etiqueta " + datos.Parte;
					doc.PrinterSettings = ajustes;
					doc.PrinterSettings.Copies = (short)numCopias.Value;
					AjustarPapel(doc);
					doc.PrintPage += delegate(object s, PrintPageEventArgs pe)
					{
						DibujarEtiqueta(pe.Graphics, datos);
						pe.HasMorePages = false;
					};
					doc.Print();
				}

				GuardarUltimaImpresora(ajustes.PrinterName);
				DialogResult = DialogResult.OK;
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "No se pudo imprimir.\r\n\r\n" + ex.Message, "Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void btnGuardar_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				string nombre = "Etiqueta_" + datos.Parte + (datos.Lote.Length > 0 ? "_" + datos.Lote : string.Empty);
				foreach (char c in Path.GetInvalidFileNameChars())
					nombre = nombre.Replace(c, '_');

				sfd.Title = "Guardar etiqueta";
				sfd.Filter = "PDF (*.pdf)|*.pdf|Imagen PNG (*.png)|*.png";
				sfd.FileName = nombre;
				sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				if (sfd.ShowDialog(this) != DialogResult.OK)
					return;

				try
				{
					using (Bitmap bmp = RenderizarEtiqueta(datos, 300))
					{
						if (sfd.FilterIndex == 2 || sfd.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
							bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
						else
							GuardarPdf(bmp, sfd.FileName);
					}

					MessageBox.Show(this, "Etiqueta guardada en:\r\n" + sfd.FileName, "Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, "No se pudo guardar.\r\n\r\n" + ex.Message, "Imprimir etiqueta", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				picVista.Image = null;
				if (vista != null)
					vista.Dispose();
				vista = null;
			}
			base.Dispose(disposing);
		}
	}

	private static void AjustarPapel(PrintDocument doc)
	{
		PaperSize etiqueta = null;
		foreach (PaperSize ps in doc.PrinterSettings.PaperSizes)
		{
			int ancho = Math.Max(ps.Width, ps.Height);
			int alto = Math.Min(ps.Width, ps.Height);
			if (Math.Abs(ancho - 400) <= 8 && Math.Abs(alto - 200) <= 8)
			{
				etiqueta = ps;
				break;
			}
		}

		if (etiqueta == null)
			etiqueta = new PaperSize("Etiqueta 4 x 2 in", 400, 200);

		doc.DefaultPageSettings.PaperSize = etiqueta;
		doc.DefaultPageSettings.Landscape = etiqueta.Width < etiqueta.Height;
		doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
		doc.OriginAtMargins = false;
	}

	///////////////////////////////// DISEÑO 4 x 2 in /////////////////////////////////
	private static void DibujarEtiqueta(Graphics g, EtiquetaDatos d)
	{
		if (g == null || d == null)
			return;

		GraphicsUnit unidadOriginal = g.PageUnit;
		g.PageUnit = GraphicsUnit.Inch;
		g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		g.SmoothingMode = SmoothingMode.None;

		using (Brush negro = new SolidBrush(Color.Black))
		using (Pen marco = new Pen(Color.Black, 0.022f))
		{
			StringFormat centro = new StringFormat();
			centro.Alignment = StringAlignment.Center;
			centro.LineAlignment = StringAlignment.Center;

			using (Font f = FuenteQueQuepa(g, TituloEtiqueta, "Arial", 12.5f, FontStyle.Bold, 3.6f))
				g.DrawString(TituloEtiqueta, f, negro, new RectangleF(0.26f, 0.05f, 3.74f, 0.3f), centro);

			Texto(g, "PARTE:", 8.5f, negro, 0.14f, 0.5f, 1.4f);
			Texto(g, d.Parte, 9.5f, negro, 0.14f, 0.74f, 1.38f);

			Texto(g, "Lote:", 9.5f, negro, 0.15f, 1.07f, 0.38f);
			Texto(g, d.Lote, 9.5f, negro, 0.53f, 1.07f, 1.01f);

			Texto(g, "Cant.:", 9.5f, negro, 0.13f, 1.35f, 0.48f);
			Texto(g, d.Cantidad, 9.5f, negro, 0.66f, 1.35f, 0.46f);
			Texto(g, d.Unidad, 9.5f, negro, 1.16f, 1.33f, 0.34f);

			Texto(g, "FT:", 11.5f, negro, 0.15f, 1.63f, 0.38f);
			Texto(g, d.Ft, 9.5f, negro, 0.53f, 1.63f, 1.01f);

			g.DrawRectangle(marco, 1.6f, 0.42f, 1.3f, 1.4f);
		}

		DibujarQr(g, d.TextoQr(), 2.99f, 0.66f, 0.92f);
		g.PageUnit = unidadOriginal;
	}

	private static void Texto(Graphics g, string texto, float tamano, Brush brocha, float x, float yCentro, float anchoMax)
	{
		if (string.IsNullOrEmpty(texto))
			return;

		StringFormat izq = new StringFormat(StringFormatFlags.NoWrap);
		izq.Alignment = StringAlignment.Near;
		izq.LineAlignment = StringAlignment.Center;
		izq.Trimming = StringTrimming.None;

		using (Font f = FuenteQueQuepa(g, texto, "Arial", tamano, FontStyle.Bold, anchoMax))
			g.DrawString(texto, f, brocha, new RectangleF(x, yCentro - 0.15f, anchoMax + 0.1f, 0.3f), izq);
	}

	private static Font FuenteQueQuepa(Graphics g, string texto, string familia, float tamano, FontStyle estilo, float anchoMaxPulgadas)
	{
		float t = tamano;
		while (true)
		{
			Font f = new Font(familia, t, estilo);
			if (string.IsNullOrEmpty(texto) || t <= 5f || g.MeasureString(texto, f, 10000, StringFormat.GenericTypographic).Width <= anchoMaxPulgadas)
				return f;
			f.Dispose();
			t -= 0.5f;
		}
	}

	private static void DibujarQr(Graphics g, string texto, float xPulg, float yPulg, float ladoPulg)
	{
		bool[,] m = QrCodigo.Generar(Encoding.UTF8.GetBytes(texto));
		int n = m.GetLength(0);

		GraphicsState estado = g.Save();
		g.PageUnit = GraphicsUnit.Pixel;
		g.SmoothingMode = SmoothingMode.None;
		g.PixelOffsetMode = PixelOffsetMode.Half;

		float dpiX = g.DpiX, dpiY = g.DpiY;
		const int zonaSilencio = 4;
		float ladoPx = ladoPulg * Math.Min(dpiX, dpiY);
		float modulo = ladoPx / (n - 2 * zonaSilencio);
		float x0 = xPulg * dpiX - zonaSilencio * modulo;
		float y0 = yPulg * dpiY - zonaSilencio * modulo;

		int[] bordeX = new int[n + 1];
		int[] bordeY = new int[n + 1];
		for (int i = 0; i <= n; i++)
		{
			bordeX[i] = (int)Math.Round(x0 + i * modulo);
			bordeY[i] = (int)Math.Round(y0 + i * modulo);
		}

		using (Brush negro = new SolidBrush(Color.Black))
		{
			for (int y = 0; y < n; y++)
				for (int x = 0; x < n; x++)
					if (m[y, x])
						g.FillRectangle(negro, bordeX[x], bordeY[y], bordeX[x + 1] - bordeX[x], bordeY[y + 1] - bordeY[y]);
		}

		g.Restore(estado);
	}

	///////////////////////////////// GENERADOR QR (modo byte, corrección M, versiones 1-20) /////////////////////////////////
	private static class QrCodigo
	{
		private static readonly int[] EccPorBloque = { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26 };
		private static readonly int[] NumBloques = { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16 };
		private const int FormatoEccM = 0;

		public static bool[,] Generar(byte[] datos)
		{
			int version = 0;
			for (int v = 1; v <= 20; v++)
			{
				int bitsConteo = v <= 9 ? 8 : 16;
				if (4 + bitsConteo + datos.Length * 8 <= CodewordsDatos(v) * 8)
				{
					version = v;
					break;
				}
			}

			if (version == 0)
				throw new InvalidOperationException("El texto del QR es demasiado largo (" + datos.Length + " bytes).");

			int capacidad = CodewordsDatos(version) * 8;
			System.Collections.Generic.List<bool> bits = new System.Collections.Generic.List<bool>();
			AgregarBits(bits, 4, 4);
			AgregarBits(bits, datos.Length, version <= 9 ? 8 : 16);
			foreach (byte b in datos)
				AgregarBits(bits, b, 8);
			AgregarBits(bits, 0, Math.Min(4, capacidad - bits.Count));
			AgregarBits(bits, 0, (8 - bits.Count % 8) % 8);
			for (int relleno = 0xEC; bits.Count < capacidad; relleno ^= 0xEC ^ 0x11)
				AgregarBits(bits, relleno, 8);

			byte[] codewords = new byte[bits.Count / 8];
			for (int i = 0; i < bits.Count; i++)
				if (bits[i])
					codewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));

			Matriz mat = new Matriz(version);
			mat.DibujarPatrones();
			mat.DibujarCodewords(AgregarEccEIntercalar(codewords, version));

			int mejorMascara = 0;
			long mejorPenalizacion = long.MaxValue;
			for (int mascara = 0; mascara < 8; mascara++)
			{
				mat.AplicarMascara(mascara);
				mat.DibujarFormato(mascara);
				long p = mat.Penalizacion();
				if (p < mejorPenalizacion)
				{
					mejorPenalizacion = p;
					mejorMascara = mascara;
				}
				mat.AplicarMascara(mascara);
			}

			mat.AplicarMascara(mejorMascara);
			mat.DibujarFormato(mejorMascara);
			return mat.ConZonaSilencio(4);
		}

		private static void AgregarBits(System.Collections.Generic.List<bool> bits, int valor, int cantidad)
		{
			for (int i = cantidad - 1; i >= 0; i--)
				bits.Add(((valor >> i) & 1) != 0);
		}

		private static int ModulosDatos(int ver)
		{
			int resultado = (16 * ver + 128) * ver + 64;
			if (ver >= 2)
			{
				int numAlin = ver / 7 + 2;
				resultado -= (25 * numAlin - 10) * numAlin - 55;
				if (ver >= 7)
					resultado -= 36;
			}
			return resultado;
		}

		private static int CodewordsDatos(int ver)
		{
			return ModulosDatos(ver) / 8 - EccPorBloque[ver] * NumBloques[ver];
		}

		private static byte[] AgregarEccEIntercalar(byte[] datos, int ver)
		{
			int numBloques = NumBloques[ver];
			int eccLen = EccPorBloque[ver];
			int totales = ModulosDatos(ver) / 8;
			int cortos = numBloques - totales % numBloques;
			int largoCorto = totales / numBloques;

			byte[] divisor = DivisorRs(eccLen);
			byte[][] bloques = new byte[numBloques][];
			for (int i = 0, k = 0; i < numBloques; i++)
			{
				int largoDatos = largoCorto - eccLen + (i < cortos ? 0 : 1);
				byte[] dat = new byte[largoDatos];
				Array.Copy(datos, k, dat, 0, largoDatos);
				k += largoDatos;

				byte[] bloque = new byte[largoCorto + 1];
				Array.Copy(dat, bloque, largoDatos);
				byte[] ecc = RestoRs(dat, divisor);
				Array.Copy(ecc, 0, bloque, bloque.Length - eccLen, eccLen);
				bloques[i] = bloque;
			}

			byte[] resultado = new byte[totales];
			for (int i = 0, k = 0; i < bloques[0].Length; i++)
			{
				for (int j = 0; j < bloques.Length; j++)
				{
					if (i != largoCorto - eccLen || j >= cortos)
					{
						resultado[k] = bloques[j][i];
						k++;
					}
				}
			}
			return resultado;
		}

		private static byte[] DivisorRs(int grado)
		{
			byte[] resultado = new byte[grado];
			resultado[grado - 1] = 1;
			int raiz = 1;
			for (int i = 0; i < grado; i++)
			{
				for (int j = 0; j < resultado.Length; j++)
				{
					resultado[j] = (byte)MultiplicarRs(resultado[j], raiz);
					if (j + 1 < resultado.Length)
						resultado[j] ^= resultado[j + 1];
				}
				raiz = MultiplicarRs(raiz, 0x02);
			}
			return resultado;
		}

		private static byte[] RestoRs(byte[] datos, byte[] divisor)
		{
			byte[] resultado = new byte[divisor.Length];
			foreach (byte b in datos)
			{
				int factor = (b ^ resultado[0]) & 0xFF;
				Array.Copy(resultado, 1, resultado, 0, resultado.Length - 1);
				resultado[resultado.Length - 1] = 0;
				for (int i = 0; i < resultado.Length; i++)
					resultado[i] ^= (byte)MultiplicarRs(divisor[i], factor);
			}
			return resultado;
		}

		private static int MultiplicarRs(int x, int y)
		{
			int z = 0;
			for (int i = 7; i >= 0; i--)
			{
				z = (z << 1) ^ ((z >> 7) * 0x11D);
				z ^= ((y >> i) & 1) * x;
			}
			return z & 0xFF;
		}

		private class Matriz
		{
			private readonly int version;
			private readonly int tam;
			private readonly bool[,] modulos;
			private readonly bool[,] funcion;

			public Matriz(int ver)
			{
				version = ver;
				tam = ver * 4 + 17;
				modulos = new bool[tam, tam];
				funcion = new bool[tam, tam];
			}

			private void Fijar(int x, int y, bool oscuro)
			{
				modulos[y, x] = oscuro;
				funcion[y, x] = true;
			}

			public void DibujarPatrones()
			{
				for (int i = 0; i < tam; i++)
				{
					Fijar(6, i, i % 2 == 0);
					Fijar(i, 6, i % 2 == 0);
				}

				Buscador(3, 3);
				Buscador(tam - 4, 3);
				Buscador(3, tam - 4);

				int[] pos = PosicionesAlineacion();
				int num = pos.Length;
				for (int i = 0; i < num; i++)
					for (int j = 0; j < num; j++)
						if (!(i == 0 && j == 0 || i == 0 && j == num - 1 || i == num - 1 && j == 0))
							Alineacion(pos[i], pos[j]);

				DibujarFormato(0);
				DibujarVersion();
			}

			private int[] PosicionesAlineacion()
			{
				if (version == 1)
					return new int[0];

				int num = version / 7 + 2;
				int paso = (version * 8 + num * 3 + 5) / (num * 4 - 4) * 2;
				int[] r = new int[num];
				r[0] = 6;
				for (int i = num - 1, p = tam - 7; i >= 1; i--, p -= paso)
					r[i] = p;
				return r;
			}

			private void Buscador(int x, int y)
			{
				for (int dy = -4; dy <= 4; dy++)
				{
					for (int dx = -4; dx <= 4; dx++)
					{
						int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
						int xx = x + dx, yy = y + dy;
						if (xx >= 0 && xx < tam && yy >= 0 && yy < tam)
							Fijar(xx, yy, dist != 2 && dist != 4);
					}
				}
			}

			private void Alineacion(int x, int y)
			{
				for (int dy = -2; dy <= 2; dy++)
					for (int dx = -2; dx <= 2; dx++)
						Fijar(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
			}

			public void DibujarFormato(int mascara)
			{
				int dato = FormatoEccM << 3 | mascara;
				int resto = dato;
				for (int i = 0; i < 10; i++)
					resto = (resto << 1) ^ ((resto >> 9) * 0x537);
				int bits = (dato << 10 | resto) ^ 0x5412;

				for (int i = 0; i <= 5; i++)
					Fijar(8, i, Bit(bits, i));
				Fijar(8, 7, Bit(bits, 6));
				Fijar(8, 8, Bit(bits, 7));
				Fijar(7, 8, Bit(bits, 8));
				for (int i = 9; i < 15; i++)
					Fijar(14 - i, 8, Bit(bits, i));

				for (int i = 0; i < 8; i++)
					Fijar(tam - 1 - i, 8, Bit(bits, i));
				for (int i = 8; i < 15; i++)
					Fijar(8, tam - 15 + i, Bit(bits, i));
				Fijar(8, tam - 8, true);
			}

			private void DibujarVersion()
			{
				if (version < 7)
					return;

				int resto = version;
				for (int i = 0; i < 12; i++)
					resto = (resto << 1) ^ ((resto >> 11) * 0x1F25);
				int bits = version << 12 | resto;

				for (int i = 0; i < 18; i++)
				{
					bool b = Bit(bits, i);
					int a = tam - 11 + i % 3;
					int c = i / 3;
					Fijar(a, c, b);
					Fijar(c, a, b);
				}
			}

			private static bool Bit(int valor, int i)
			{
				return ((valor >> i) & 1) != 0;
			}

			public void DibujarCodewords(byte[] datos)
			{
				int i = 0;
				for (int derecha = tam - 1; derecha >= 1; derecha -= 2)
				{
					if (derecha == 6)
						derecha = 5;
					for (int vert = 0; vert < tam; vert++)
					{
						for (int j = 0; j < 2; j++)
						{
							int x = derecha - j;
							bool haciaArriba = ((derecha + 1) & 2) == 0;
							int y = haciaArriba ? tam - 1 - vert : vert;
							if (!funcion[y, x] && i < datos.Length * 8)
							{
								modulos[y, x] = ((datos[i >> 3] >> (7 - (i & 7))) & 1) != 0;
								i++;
							}
						}
					}
				}
			}

			public void AplicarMascara(int mascara)
			{
				for (int y = 0; y < tam; y++)
				{
					for (int x = 0; x < tam; x++)
					{
						bool invertir;
						switch (mascara)
						{
							case 0: invertir = (x + y) % 2 == 0; break;
							case 1: invertir = y % 2 == 0; break;
							case 2: invertir = x % 3 == 0; break;
							case 3: invertir = (x + y) % 3 == 0; break;
							case 4: invertir = (x / 3 + y / 2) % 2 == 0; break;
							case 5: invertir = x * y % 2 + x * y % 3 == 0; break;
							case 6: invertir = (x * y % 2 + x * y % 3) % 2 == 0; break;
							default: invertir = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
						}
						if (invertir && !funcion[y, x])
							modulos[y, x] = !modulos[y, x];
					}
				}
			}

			public long Penalizacion()
			{
				long p = 0;

				for (int y = 0; y < tam; y++)
				{
					int corrida = 1;
					for (int x = 1; x < tam; x++)
					{
						if (modulos[y, x] == modulos[y, x - 1]) corrida++;
						else { if (corrida >= 5) p += corrida - 2; corrida = 1; }
					}
					if (corrida >= 5) p += corrida - 2;
				}

				for (int x = 0; x < tam; x++)
				{
					int corrida = 1;
					for (int y = 1; y < tam; y++)
					{
						if (modulos[y, x] == modulos[y - 1, x]) corrida++;
						else { if (corrida >= 5) p += corrida - 2; corrida = 1; }
					}
					if (corrida >= 5) p += corrida - 2;
				}

				for (int y = 0; y < tam - 1; y++)
					for (int x = 0; x < tam - 1; x++)
					{
						bool c = modulos[y, x];
						if (c == modulos[y, x + 1] && c == modulos[y + 1, x] && c == modulos[y + 1, x + 1])
							p += 3;
					}

				int oscuros = 0;
				for (int y = 0; y < tam; y++)
					for (int x = 0; x < tam; x++)
						if (modulos[y, x]) oscuros++;
				int total = tam * tam;
				int k = (Math.Abs(oscuros * 20 - total * 10) + total - 1) / total - 1;
				p += Math.Max(0, k) * 10;

				return p;
			}

			public bool[,] ConZonaSilencio(int borde)
			{
				int n = tam + borde * 2;
				bool[,] r = new bool[n, n];
				for (int y = 0; y < tam; y++)
					for (int x = 0; x < tam; x++)
						r[y + borde, x + borde] = modulos[y, x];
				return r;
			}
		}
	}
}
