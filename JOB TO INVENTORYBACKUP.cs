// **************************************************
// Custom code for RcptToInvForm — Recibo Job a Inventario + SERIALIZADO PT
// Botón RECIBO SERIALIZADO epguid: 72176ef3-37c7-45ab-ab22-33dc9bed474c
// Label SERIALIZADO epguid: 3a2e16ca-73fa-4715-b195-093cc09b4100
// Flag parte serializada: UD10.ShortChar06 (Matriz de partes)
// Datos serializacion guardados en JobHead_UD (Serializador BRK / Guardar en Epicor):
//   SerialPrefix_c, SerialFrom_c, SerialTo_c, RollCount_c, SerializedQty_c
//   orden_compra_c, OrdenDeVenta_c, LotNum_c, LastPrintDate_c, LastPrintedBy_c
// Confirmacion PT por rollo: Ice.UD37 (reflexion UD37Adapter + ref ensamblado)
//   Key1=JobNum | Key2=RollNum | Character01=PartNum | Character02=Prefix
//   Character03=SerialFrom | Character04=SerialTo | Character05=Estado
//   Character06=ConfirmadoPor | Number01=RollNum | Number02=Cantidad | Date01=Fecha
// BAQ cliente parte: BAQ_CLIENTE_PARTE param @PARTE = PartTran.PartNum
//   SELECT Part.Cliente_c AS Part_Cliente_c FROM Erp.Part WHERE Part.PartNum = @PARTE
//
// Lectura JobHead: BAQ BAQ-SERIAL-JOBHEAD con parametro @JobNum (obligatorio filtro WHERE)
//
// BAQ BAQ-SERIAL-JOBHEAD (crear una vez en Epicor):
//   SELECT j.JobNum, j.PartNum, p.PartDescription,
//          jud.orden_compra_c, jud.OrdenDeVenta_c, jud.CustID_c,
//          ISNULL(pud.Cliente_c, '') AS Cliente_c,
//          jud.SerialPrefix_c, jud.SerialFrom_c, jud.SerialTo_c,
//          jud.RollCount_c, jud.SerializedQty_c, j.ProdQty
//   FROM Erp.JobHead j
//   LEFT JOIN Erp.JobHead_UD jud ON jud.ForeignSysRowID = j.SysRowID
//   LEFT JOIN Erp.Part p ON p.Company = j.Company AND p.PartNum = j.PartNum
//   LEFT JOIN Erp.Part_UD pud ON pud.ForeignSysRowID = p.SysRowID
//   WHERE j.JobNum = @JobNum
//   Parametro: JobNum (nvarchar)
// v2: Grid ampliado, confirmacion rapida, IMPRIMIR etiquetas 3x2 (core+caja) ZEBRAEPICOR
// v2.1: Panel captura compacto, Imprimir en footer, confirmacion sin recargar grid
// v2.2: Barra inferior unica (captura + IMPRIMIR + Listo + Cerrar), grid maximizado
// v2.3: Vista previa etiquetas + impresion solo rollos marcados en columna Imprimir
// v2.4: Impresion estable — TCP primero, rollo por rollo, P/Invoke Unicode, sin crash Epicor
// v2.5: Dialogo impresion — Zebra Windows, seleccion rollos, vista previa 3x2, sin IP
// v2.6: Anti-crash — solo Zebra, impresion en background, ZPL por etiqueta, sin DoEvents
// v2.7: Apertura rapida — cache rollos, prefetch al cambiar Job, sin Sync UD37 al abrir
// v2.8: LOTE=Job+fecha, START/END mas claros, solo impresoras Zebra instaladas
// v2.9: Impresion segura — hilo UI, puerto Zebra/TCP, helper externo, sin crash Epicor
// v3.0: Sin P/Invoke en Epicor — solo helper + TCP; 110XiIIIPlus; validacion y aviso de error
// v3.1: Etiquetas — Cliente desde Part.Cliente_c, ROLLO en core, filas ZPL sin tachar texto
// v3.2: Cliente por PartTran.PartNum + BAQ_CLIENTE_PARTE (@PARTE)
// v3.3: Confirmacion impresion (N etiquetas / Zebra) + ZPL 3x2 @ 200dpi alineado
// v3.4: Texto mas grande; ROLLO debajo START/END (core) y debajo QTY (caja)
// v3.5: Rejilla proporcional 3x2 — llena etiqueta, ROLLO en fila propia sin tachar lineas
// v3.6: Core centrado vertical + fuentes mas grandes; REV vertical legible en caja
// v4.0: 1 etiqueta CORE por rollo + 1 etiqueta CAJA resumen (todos los rollos START/END)
// v4.1: Selector CORE x rollo en dialogo + linea Job/Parte/Cliente
// v4.2: Etiqueta CAJA estilo mockup (CLIENTE/CODIGO/QTY + tabla ROLLO|START|END auto-ajuste)
// v4.3: Impresion incluye rollos Recibido (ej. Rollo 1 ya confirmado en PT)
// v4.4: Impresion solo rollos confirmados (Recibido) + refresh UD37/BAQ completo
// v4.5: Dialogo impresion — rollos Recibido visibles al confirmar; no bajar status; preseleccion Imprimir
// v4.6: ZPL 3x2 @ 300dpi (ZTC 110Xi4 / ZEBRAEPICOR 192.168.60.122) — fuentes y rejilla escaladas
// v4.6.1: Fix crash al imprimir — clave duplicada 110Xi4/110XI4 en BuiltInDefaults (OrdinalIgnoreCase)
// v4.6.2: BuiltInDefaults sin inicializador frágil; match parcial por clave más específica (sin alias 110XI)
// v4.6.3: Sin Dictionary en cctor — solo arreglo BuiltInDefaultEntries (evita crash si Epicor tenía DLL viejo)
// v4.6.4: Built-in IP antes de leer disco; File.Exists con timeout (otro PC sin C:\SERIALIZADOR no cuelga)
// v4.7: Candado liberacion costo job — BAQ BAQLIBERACIONCOSTOJOB (solo planta CFXJZ / CFXJZ SALE)
// **************************************************

extern alias Erp_Contracts_BO_ReceiptsFromMfg;
extern alias Erp_Contracts_BO_PkgControlIDMaint;
extern alias Erp_Contracts_BO_Part;
extern alias Erp_Contracts_BO_Warehse;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Erp.Adapters;
using Erp.UI;
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

	private EpiDataView edvPartTran;
	EpiTextBox jobtxtst;
	EpiUltraCombo ultracombos;
	EpiCombo epicombos;
	BAQCombo baqcombos;
	Erp.UI.Controls.Combos.JobPartSearchCombo moy;
	EpiNumericEditor qty;
	EpiNumericEditor qtssy;
	// End Wizard Added Module Level Variables **

	// Add Custom Module Level Variables Here **
	private const string ReciboSerializadoButtonEpGuid = "72176ef3-37c7-45ab-ab22-33dc9bed474c";
	private const string LblSerializadoEpGuid = "3a2e16ca-73fa-4715-b195-093cc09b4100";

	private Control _btnReciboSerializado;
	private Control _lblSerializado;
	private EpiBaseAdapter _oTrans_partTranAdapter;

	private string _currentJobNum = string.Empty;
	private string _currentPartNum = string.Empty;
	private bool _requiresSerializado;
	private bool _hasSerialData;

	private string _lastRefreshJob = string.Empty;
	private string _lastRefreshPart = string.Empty;
	private string _lastCostReleaseJob = string.Empty;
	private bool _lastCostReleaseOk = true;

	public void InitializeCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
		// Begin Wizard Added Variable Initialization

		this.edvPartTran = ((EpiDataView)(this.oTrans.EpiDataViews["PartTran"]));
		this.edvPartTran.EpiViewNotification += new EpiViewNotification(this.edvPartTran_EpiViewNotification);
		// End Wizard Added Variable Initialization

		// Begin Wizard Added Custom Method Calls

		this.cehcar2.CheckedChanged += new System.EventHandler(this.cehcar2_CheckedChanged);
		this.checar1.CheckedChanged += new System.EventHandler(this.checar1_CheckedChanged);
		this.checar4.CheckedChanged += new System.EventHandler(this.checar4_CheckedChanged);
		this.checar3.CheckedChanged += new System.EventHandler(this.checar3_CheckedChanged);
		this.checar45.CheckedChanged += new System.EventHandler(this.checar45_CheckedChanged);
		this.epiChec.CheckedChanged += new System.EventHandler(this.epiChec_CheckedChanged);
		// End Wizard Added Custom Method Calls

		materap.Enabled = false;
		okey.Enabled = false;
		prod.Enabled = false;
		inspas.Enabled = false;
		logist.Enabled = false;
		rebobinado.Enabled = false;

		try
		{
			if (csm.TransAdaptersHT.Contains("oTrans_partTranAdapter"))
			{
				_oTrans_partTranAdapter = (EpiBaseAdapter)csm.TransAdaptersHT["oTrans_partTranAdapter"];
				_oTrans_partTranAdapter.BeforeAdapterMethod += PartTran_BeforeAdapterMethod;
			}
		}
		catch { }

		WireSerializadoReceiptControls();
		WireJobRefreshEvents();
	}

	private void WireJobRefreshEvents()
	{
		try
		{
			Control jobComboCtrl = csm.GetNativeControlReference("f08ed85a-b31f-4baf-b58f-34560daec64a");
			if (jobComboCtrl != null)
			{
				jobComboCtrl.Validated -= JobOrQty_Changed;
				jobComboCtrl.Validated += JobOrQty_Changed;
			}

			EpiNumericEditor qtys = csm.GetNativeControlReference("6689cec5-2666-4ad3-8079-7827df4540dd") as EpiNumericEditor;
			if (qtys != null)
			{
				qtys.ValueChanged -= JobOrQty_Changed;
				qtys.ValueChanged += JobOrQty_Changed;
			}
		}
		catch { }
	}

	private void JobOrQty_Changed(object sender, EventArgs e)
	{
		ApplyJobCostReleaseGate(GetPartTranField("JobNum"), true);
		RefreshSerializadoState();
	}

	private void WireSerializadoReceiptControls()
	{
		try
		{
			_btnReciboSerializado = csm.GetNativeControlReference(ReciboSerializadoButtonEpGuid) as Control;
			if (_btnReciboSerializado != null)
			{
				_btnReciboSerializado.Click -= BtnReciboSerializado_Click;
				_btnReciboSerializado.Click += BtnReciboSerializado_Click;
			}

			_lblSerializado = csm.GetNativeControlReference(LblSerializadoEpGuid) as Control;
			ResetSerializadoLabel(false);
		}
		catch { }
	}

	public void DestroyCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
		// Begin Wizard Added Object Disposal

		this.cehcar2.CheckedChanged -= new System.EventHandler(this.cehcar2_CheckedChanged);
		this.checar1.CheckedChanged -= new System.EventHandler(this.checar1_CheckedChanged);
		this.checar4.CheckedChanged -= new System.EventHandler(this.checar4_CheckedChanged);
		this.checar3.CheckedChanged -= new System.EventHandler(this.checar3_CheckedChanged);
		this.checar45.CheckedChanged -= new System.EventHandler(this.checar45_CheckedChanged);
		this.epiChec.CheckedChanged -= new System.EventHandler(this.epiChec_CheckedChanged);
		this.edvPartTran.EpiViewNotification -= new EpiViewNotification(this.edvPartTran_EpiViewNotification);
		this.edvPartTran = null;
		// End Wizard Added Object Disposal

		if (_oTrans_partTranAdapter != null)
		{
			_oTrans_partTranAdapter.BeforeAdapterMethod -= PartTran_BeforeAdapterMethod;
			_oTrans_partTranAdapter = null;
		}

		if (_btnReciboSerializado != null)
		{
			_btnReciboSerializado.Click -= BtnReciboSerializado_Click;
			_btnReciboSerializado = null;
		}

		_lblSerializado = null;
	}

	private void BtnReciboSerializado_Click(object sender, EventArgs e)
	{
		try
		{
			RefreshSerializadoState();

			string blockReason;
			if (!JobCostReleaseValidator.IsReceiptAllowed(oTrans, _currentJobNum, out blockReason))
			{
				MessageBox.Show(blockReason, "Job no validado", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (!_requiresSerializado)
			{
				MessageBox.Show(
					"Esta parte no está marcada como Serializado en la Matriz de partes (UD10.ShortChar06).",
					"Recibo Serializado",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}

			if (string.IsNullOrWhiteSpace(_currentJobNum))
			{
				MessageBox.Show("Seleccione un Job antes de confirmar el serializado.", "Recibo Serializado",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (!_hasSerialData)
			{
				MessageBox.Show(
					"No hay datos de serializado en JobHead_UD para el Job " + _currentJobNum + ".\n\n" +
					"Verifique SerialFrom_c, RollCount_c y SerializedQty_c.\n" +
					"Solicite a Producción ejecutar Guardar en Epicor desde Job Entry.\n\n" +
					"Si en SQL sí aparecen los datos, agregue filtro JobNum = @JobNum al BAQ BAQ-SERIAL-JOBHEAD " +
					"(sin filtro el BAQ trae todos los jobs y congela Epicor).",
					"Recibo Serializado",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			decimal tranQty = GetThisTransactionQty();
			RollBundle bundle = SerializadoReceiptService.GetOrBuildBundle(oTrans, _currentJobNum, _currentPartNum);
			using (SerializadoReceiptForm frm = new SerializadoReceiptForm(oTrans, bundle, tranQty))
			{
				frm.ShowDialog(GetOwnerForm());
			}

			SerializadoReceiptService.InvalidateJobBundle(_currentJobNum);
			RefreshSerializadoState();
		}
		catch (Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	private Form GetOwnerForm()
	{
		try
		{
			return RcptToInvForm;
		}
		catch
		{
			return Form.ActiveForm;
		}
	}

	private void RefreshSerializadoState()
	{
		string job = GetPartTranField("JobNum");
		string part = GetPartTranField("PartNum");

		if (string.Equals(job, _lastRefreshJob, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(part, _lastRefreshPart, StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(job))
		{
			return;
		}

		_lastRefreshJob = job;
		_lastRefreshPart = part;
		_currentJobNum = job;
		_currentPartNum = part;

		_requiresSerializado = SerializadoReceiptService.IsPartSerialized(oTrans, _currentPartNum);
		_hasSerialData = _requiresSerializado
			&& !string.IsNullOrWhiteSpace(_currentJobNum)
			&& SerializadoReceiptService.HasSavedRolls(oTrans, _currentJobNum);

		if (_requiresSerializado && _hasSerialData && !string.IsNullOrWhiteSpace(_currentJobNum))
			SerializadoReceiptService.PrefetchJobBundle(oTrans, _currentJobNum, _currentPartNum);

		ApplySerializadoLabel(_requiresSerializado, _hasSerialData);
		SetReciboSerializadoButtonState(_requiresSerializado);
	}

	private void ForceRefreshSerializadoState()
	{
		_lastRefreshJob = string.Empty;
		_lastRefreshPart = string.Empty;
		SerializadoReceiptService.ClearJobCache(_currentJobNum);
		RefreshSerializadoState();
	}

	private void ApplySerializadoLabel(bool requiresSerial, bool hasData)
	{
		if (_lblSerializado == null)
			return;

		if (requiresSerial)
		{
			_lblSerializado.BackColor = Color.FromArgb(102, 187, 106);
			_lblSerializado.ForeColor = Color.Black;
			try
			{
				_lblSerializado.Font = new Font(_lblSerializado.Font.FontFamily, _lblSerializado.Font.Size, FontStyle.Bold);
			}
			catch { }

			if (hasData)
				_lblSerializado.Text = "SERIALIZADO — CONFIRMAR ROLLOS";
			else
				_lblSerializado.Text = "SERIALIZADO — SIN DATOS EN EPICOR";
		}
		else
			ResetSerializadoLabel(false);
	}

	private void ResetSerializadoLabel(bool requiresSerial)
	{
		if (_lblSerializado == null)
			return;

		_lblSerializado.BackColor = requiresSerial ? Color.FromArgb(102, 187, 106) : SystemColors.Control;
		_lblSerializado.ForeColor = Color.Black;
		try
		{
			_lblSerializado.Font = new Font(_lblSerializado.Font.FontFamily, _lblSerializado.Font.Size,
				requiresSerial ? FontStyle.Bold : FontStyle.Regular);
		}
		catch { }

		_lblSerializado.Text = requiresSerial ? "SERIALIZADO" : "SERIALIZADO";
	}

	private void SetReciboSerializadoButtonState(bool enabled)
	{
		if (_btnReciboSerializado == null)
			return;
		_btnReciboSerializado.Enabled = enabled;
	}

	private string GetPartTranField(string column)
	{
		try
		{
			if (edvPartTran == null || edvPartTran.Row < 0 || edvPartTran.dataView == null)
				return string.Empty;
			if (!edvPartTran.dataView.Table.Columns.Contains(column))
				return string.Empty;
			object v = edvPartTran.dataView[edvPartTran.Row][column];
			return v == null || v == DBNull.Value ? string.Empty : v.ToString().Trim();
		}
		catch { return string.Empty; }
	}

	private decimal GetThisTransactionQty()
	{
		try
		{
			EpiNumericEditor qtys = (EpiNumericEditor)csm.GetNativeControlReference("6689cec5-2666-4ad3-8079-7827df4540dd");
			if (qtys != null && qtys.Value != null && qtys.Value != DBNull.Value)
			{
				decimal d;
				if (decimal.TryParse(qtys.Value.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out d))
					return d;
			}
		}
		catch { }

		try
		{
			if (edvPartTran != null && edvPartTran.Row >= 0)
			{
				string[] cols = new string[] { "TranQty", "ActTranQty", "Qty" };
				for (int i = 0; i < cols.Length; i++)
				{
					if (!edvPartTran.dataView.Table.Columns.Contains(cols[i]))
						continue;
					object v = edvPartTran.dataView[edvPartTran.Row][cols[i]];
					if (v != null && v != DBNull.Value)
					{
						decimal d;
						if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out d))
							return d;
					}
				}
			}
		}
		catch { }

		return 0m;
	}

	private void PartTran_BeforeAdapterMethod(object sender, BeforeAdapterMethodArgs args)
	{
		if (!string.Equals(args.MethodName, "Update", StringComparison.OrdinalIgnoreCase))
			return;

		string jobNum = GetPartTranField("JobNum");
		string blockReason;
		if (!JobCostReleaseValidator.IsReceiptAllowed(oTrans, jobNum, out blockReason))
			throw new UIException(blockReason);

		RefreshSerializadoState();
		if (!_requiresSerializado)
			return;

		decimal tranQty = GetThisTransactionQty();
		if (tranQty <= 0m)
			return;

		string error;
		if (!SerializadoReceiptService.ValidateReceiptReady(oTrans, _currentJobNum, tranQty, out error))
			throw new UIException(error);
	}

	private void cehcar2_CheckedChanged(object sender, System.EventArgs args)
	{
		materap.Enabled = cehcar2.Checked;
	}

	private void checar1_CheckedChanged(object sender, System.EventArgs args)
	{
		okey.Enabled = checar1.Checked;
	}

	private void checar4_CheckedChanged(object sender, System.EventArgs args)
	{
		inspas.Enabled = checar4.Checked;
	}

	private void checar3_CheckedChanged(object sender, System.EventArgs args)
	{
		prod.Enabled = checar3.Checked;
	}

	private void checar45_CheckedChanged(object sender, System.EventArgs args)
	{
		logist.Enabled = checar45.Checked;
	}

	private void epiChec_CheckedChanged(object sender, System.EventArgs args)
	{
		rebobinado.Enabled = epiChec.Checked;
	}

	private void consultaliberacion_costo_job(string JOBS)
	{
		ApplyJobCostReleaseGate(JOBS, true);
	}

	private void ApplyJobCostReleaseGate(string jobNum, bool showMessage)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
		{
			SetQtyEditorEnabled(true);
			_lastCostReleaseJob = string.Empty;
			_lastCostReleaseOk = true;
			return;
		}

		string jobKey = jobNum.Trim();
		string blockReason;
		_lastCostReleaseOk = JobCostReleaseValidator.IsReceiptAllowed(oTrans, jobKey, out blockReason);
		_lastCostReleaseJob = jobKey;

		if (_lastCostReleaseOk)
		{
			SetQtyEditorEnabled(true);
			return;
		}

		try
		{
			jobtxtst = (EpiTextBox)csm.GetNativeControlReference("b48167cc-28d1-4f1d-83df-22ac3405f09e");
			if (jobtxtst != null)
				jobtxtst.Text = string.Empty;
		}
		catch { }

		SetQtyEditorEnabled(false);

		if (showMessage)
		{
			MessageBox.Show(
				string.IsNullOrWhiteSpace(blockReason)
					? "Ir directo con gerencia o subgerencia para validar el job"
					: blockReason,
				"Job no validado",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
	}

	private void SetQtyEditorEnabled(bool enabled)
	{
		try
		{
			EpiNumericEditor qtys = (EpiNumericEditor)csm.GetNativeControlReference("6689cec5-2666-4ad3-8079-7827df4540dd");
			if (qtys != null)
				qtys.Enabled = enabled;
		}
		catch { }
	}

	private void edvPartTran_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if (args.NotifyType == EpiTransaction.NotifyType.AddRow
			|| args.NotifyType == EpiTransaction.NotifyType.Initialize
			|| args.NotifyType == EpiTransaction.NotifyType.InitLastView)
		{
			if (args.Row > -1)
			{
				string jobnum = edvPartTran.dataView[edvPartTran.Row]["JobNum"].ToString();
				if (JobCostReleaseValidator.IsCorteJob(jobnum))
					SetQtyEditorEnabled(true);
				else
					ApplyJobCostReleaseGate(jobnum, false);

				RefreshSerializadoState();
			}
		}
	}
}

// =============================================================================
// Candado — Liberacion costo job (BAQ actualizable BAQLIBERACIONCOSTOJOB)
// Col 0 = palomita liberacion | Col 1 = Plant del job
// Solo aplica plantas CFXJZ / CFXJZ SALE (familia JZ). CFXCH reservado para activar despues.
// =============================================================================

public static class JobCostReleaseValidator
{
	private const string BaqName = "BAQLIBERACIONCOSTOJOB";
	private const string BaqParamJob = "JOB";

	public static bool IsCorteJob(string jobNum)
	{
		return !string.IsNullOrWhiteSpace(jobNum)
			&& jobNum.IndexOf("CORTE-", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static bool IsCfxjzFamilyPlant(string plant)
	{
		if (string.IsNullOrWhiteSpace(plant))
			return false;

		plant = plant.Trim();
		if (string.Equals(plant, "CFXJZ", StringComparison.OrdinalIgnoreCase))
			return true;
		if (string.Equals(plant, "CFXJZ SALE", StringComparison.OrdinalIgnoreCase))
			return true;
		return plant.StartsWith("CFXJZ", StringComparison.OrdinalIgnoreCase);
	}

	public static bool TryQuery(object oTrans, string jobNum, out bool costReleased, out string jobPlant)
	{
		costReleased = false;
		jobPlant = string.Empty;
		if (string.IsNullOrWhiteSpace(jobNum))
			return false;

		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow(
					BaqParamJob, jobNum.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID(BaqName, ds);

				DataTable t = dq.QueryResults.Tables["Results"];
				if (t == null || t.Rows.Count == 0)
					return false;

				DataRow row = t.Rows[0];
				costReleased = ParseTruthy(row[0]);
				if (t.Columns.Count > 1)
					jobPlant = Convert.ToString(row[1]).Trim();
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	public static bool IsReceiptAllowed(object oTrans, string jobNum, out string blockReason)
	{
		blockReason = string.Empty;
		if (string.IsNullOrWhiteSpace(jobNum))
			return true;
		if (IsCorteJob(jobNum))
			return true;

		bool costReleased;
		string jobPlant;
		if (!TryQuery(oTrans, jobNum, out costReleased, out jobPlant))
			return true;

		if (!IsCfxjzFamilyPlant(jobPlant))
			return true;

		if (costReleased)
			return true;

		blockReason =
			"Este job no tiene liberación de costo.\n\n" +
			"SubGerencia o gerencia debe validar el job antes de recibir en PT.\n\n" +
			"Planta: " + jobPlant;
		return false;
	}

	private static bool ParseTruthy(object value)
	{
		if (value == null || value == DBNull.Value)
			return false;
		if (value is bool)
			return (bool)value;
		string s = value.ToString().Trim();
		if (s.Length == 0)
			return false;
		if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase))
			return false;
		if (string.Equals(s, "N", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(s, "NO", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(s, "FALSE", StringComparison.OrdinalIgnoreCase))
			return false;
		return true;
	}
}

// =============================================================================
// SERVICIO — JobHead_UD + UD10 matriz + confirmacion rollos PT
// =============================================================================

public sealed class SerializadoReceiptRollRow
{
	public int RollNum;
	public string Prefix = "J0";
	public string SerialFrom = string.Empty;
	public string SerialTo = string.Empty;
	public int Quantity;
	public string Status = "Impreso";
	public string ConfirmedBy = string.Empty;
	public DateTime? ConfirmedOn;
}

public sealed class SerializadoReceiptJobInfo
{
	public string JobNum = string.Empty;
	public string PartNum = string.Empty;
	public string PartDescription = string.Empty;
	public string PurchaseOrder = string.Empty;
	public string SalesOrder = string.Empty;
	public string Cliente = string.Empty;
	public string LotNum = string.Empty;
	public string SerialPrefix = "J0";
	public string SerialFrom = string.Empty;
	public string SerialTo = string.Empty;
	public int RollCount;
	public decimal SerializedQty;
}

public sealed class RollBundle
{
	public SerializadoReceiptJobInfo JobInfo;
	public List<SerializadoReceiptRollRow> Rolls = new List<SerializadoReceiptRollRow>();
}

public static class SerializadoReceiptService
{
	private const int DefaultLabelsPerRoll = 1000;
	private const string SerialJobBaqName = "BAQ-SERIAL-JOBHEAD";
	private const string PartClienteBaqName = "BAQ_CLIENTE_PARTE";
	private const int MaxBaqRowsWithoutFilter = 25;

	private static readonly Dictionary<string, SerializadoReceiptJobInfo> JobSerialCache
		= new Dictionary<string, SerializadoReceiptJobInfo>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, bool> PartSerializedCache
		= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, int> LabelsPerRollCache
		= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, RollBundle> JobBundleCache
		= new Dictionary<string, RollBundle>(StringComparer.OrdinalIgnoreCase);

	public static void ClearJobCache(string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return;
		string key = jobNum.Trim();
		JobSerialCache.Remove(key);
		JobBundleCache.Remove(key);
	}

	public static void InvalidateJobBundle(string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return;
		JobBundleCache.Remove(jobNum.Trim());
	}

	public static void PrefetchJobBundle(object oTrans, string jobNum, string partNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return;
		string key = jobNum.Trim();
		if (JobBundleCache.ContainsKey(key))
			return;
		try
		{
			JobBundleCache[key] = BuildJobBundle(oTrans, jobNum, partNum);
		}
		catch { }
	}

	public static RollBundle GetOrBuildBundle(object oTrans, string jobNum, string partNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return new RollBundle { JobInfo = new SerializadoReceiptJobInfo() };

		string key = jobNum.Trim();
		RollBundle cached;
		if (JobBundleCache.TryGetValue(key, out cached) && cached != null)
			return CloneBundle(cached);

		RollBundle bundle = BuildJobBundle(oTrans, jobNum, partNum);
		JobBundleCache[key] = CloneBundle(bundle);
		return bundle;
	}

	private static RollBundle BuildJobBundle(object oTrans, string jobNum, string partNum)
	{
		SerializadoReceiptJobInfo info = LoadJobSerialData(oTrans, jobNum);
		if (!string.IsNullOrWhiteSpace(partNum))
			info.PartNum = partNum.Trim();

		List<SerializadoReceiptRollRow> rolls = BuildRollListFromInfo(oTrans, info);
		if (rolls.Count > 0)
			RefreshRollStatuses(oTrans, jobNum, rolls);

		return new RollBundle
		{
			JobInfo = CloneJobInfo(info),
			Rolls = CloneRollList(rolls)
		};
	}

	public static void EnrichJobInfoIfNeeded(object oTrans, SerializadoReceiptJobInfo info)
	{
		EnrichJobInfoIfNeeded(oTrans, info, null);
	}

	public static void EnrichJobInfoIfNeeded(object oTrans, SerializadoReceiptJobInfo info, string partNumFromPartTran)
	{
		if (info == null)
			return;
		if (!string.IsNullOrWhiteSpace(partNumFromPartTran))
			info.PartNum = partNumFromPartTran.Trim();
		if (string.IsNullOrWhiteSpace(info.PurchaseOrder))
			LoadPoFromOv(oTrans, info);
		LoadClienteFromPart(oTrans, info);
	}

	public static RollBundle CloneBundle(RollBundle src)
	{
		if (src == null)
			return new RollBundle { JobInfo = new SerializadoReceiptJobInfo() };
		return new RollBundle
		{
			JobInfo = CloneJobInfo(src.JobInfo),
			Rolls = CloneRollList(src.Rolls)
		};
	}

	public static List<SerializadoReceiptRollRow> CloneRollList(List<SerializadoReceiptRollRow> src)
	{
		List<SerializadoReceiptRollRow> list = new List<SerializadoReceiptRollRow>();
		if (src == null)
			return list;
		for (int i = 0; i < src.Count; i++)
		{
			SerializadoReceiptRollRow r = src[i];
			list.Add(new SerializadoReceiptRollRow
			{
				RollNum = r.RollNum,
				Prefix = r.Prefix,
				SerialFrom = r.SerialFrom,
				SerialTo = r.SerialTo,
				Quantity = r.Quantity,
				Status = r.Status,
				ConfirmedBy = r.ConfirmedBy,
				ConfirmedOn = r.ConfirmedOn
			});
		}
		return list;
	}

	public static bool IsPartSerialized(object oTrans, string partNum)
	{
		if (string.IsNullOrWhiteSpace(partNum))
			return false;

		string partKey = partNum.Trim();
		bool cached;
		if (PartSerializedCache.TryGetValue(partKey, out cached))
			return cached;

		bool result = false;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("partNum_", partKey, "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID("CODIFLEX-UD10", ds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t != null && t.Rows.Count > 0)
				{
					DataRow row = t.Rows[0];
					for (int c = 0; c < t.Columns.Count; c++)
					{
						string col = t.Columns[c].ColumnName ?? string.Empty;
						if (col.IndexOf("ShortChar06", StringComparison.OrdinalIgnoreCase) < 0
							&& col.IndexOf("Serializado", StringComparison.OrdinalIgnoreCase) < 0)
							continue;
						if (IsTruthy(row[c]))
						{
							result = true;
							break;
						}
					}
				}
			}
		}
		catch { }

		PartSerializedCache[partKey] = result;
		return result;
	}

	private static bool IsTruthy(object value)
	{
		if (value == null || value == DBNull.Value)
			return false;
		string s = value.ToString().Trim();
		if (s.Length == 0)
			return false;
		if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase))
			return false;
		if (string.Equals(s, "N", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(s, "NO", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(s, "FALSE", StringComparison.OrdinalIgnoreCase))
			return false;
		return true;
	}

	public static bool HasSavedRolls(object oTrans, string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return false;

		string key = jobNum.Trim();
		SerializadoReceiptJobInfo info = LoadJobSerialData(oTrans, jobNum);
		if (string.IsNullOrWhiteSpace(info.SerialFrom) || info.SerializedQty <= 0m)
			return false;

		if (info.RollCount <= 0)
		{
			PostProcessJobInfo(oTrans, info);
			if (info.RollCount > 0)
				JobSerialCache[key] = CloneJobInfo(info);
		}

		return info.RollCount > 0
			|| (info.SerializedQty > 0m && !string.IsNullOrWhiteSpace(info.SerialFrom));
	}

	public static SerializadoReceiptJobInfo LoadJobInfo(object oTrans, string jobNum, string partNum)
	{
		SerializadoReceiptJobInfo info = LoadJobSerialData(oTrans, jobNum);
		if (!string.IsNullOrWhiteSpace(partNum))
			info.PartNum = partNum.Trim();

		LoadClienteFromPart(oTrans, info);

		return info;
	}

	private static SerializadoReceiptJobInfo LoadJobSerialData(object oTrans, string jobNum)
	{
		SerializadoReceiptJobInfo empty = new SerializadoReceiptJobInfo { JobNum = jobNum ?? string.Empty };
		if (string.IsNullOrWhiteSpace(jobNum))
			return empty;

		string key = jobNum.Trim();
		SerializadoReceiptJobInfo cached;
		if (JobSerialCache.TryGetValue(key, out cached) && cached != null)
			return CloneJobInfo(cached);

		SerializadoReceiptJobInfo info = new SerializadoReceiptJobInfo { JobNum = key };
		if (TryLoadJobInfoFromBaq(oTrans, info))
			PostProcessJobInfo(oTrans, info);

		JobSerialCache[key] = CloneJobInfo(info);
		return CloneJobInfo(info);
	}

	public static SerializadoReceiptJobInfo CloneJobInfo(SerializadoReceiptJobInfo src)
	{
		if (src == null)
			return new SerializadoReceiptJobInfo();
		return new SerializadoReceiptJobInfo
		{
			JobNum = src.JobNum,
			PartNum = src.PartNum,
			PartDescription = src.PartDescription,
			PurchaseOrder = src.PurchaseOrder,
			SalesOrder = src.SalesOrder,
			Cliente = src.Cliente,
			LotNum = src.LotNum,
			SerialPrefix = src.SerialPrefix,
			SerialFrom = src.SerialFrom,
			SerialTo = src.SerialTo,
			RollCount = src.RollCount,
			SerializedQty = src.SerializedQty
		};
	}

	private static bool HasSerialUdData(SerializadoReceiptJobInfo info)
	{
		return info != null
			&& !string.IsNullOrWhiteSpace(info.SerialFrom)
			&& info.SerializedQty > 0m;
	}

	private static bool TryLoadJobInfoFromBaq(object oTrans, SerializadoReceiptJobInfo info)
	{
		if (string.IsNullOrWhiteSpace(info.JobNum))
			return false;

		string targetJob = info.JobNum.Trim();
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("JobNum", targetJob, "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID(SerialJobBaqName, ds);

				DataTable t = dq.QueryResults.Tables["Results"];
				if (t == null || t.Rows.Count == 0)
					return false;

				if (t.Rows.Count > MaxBaqRowsWithoutFilter)
				{
					DataRow bulkMatch = FindBaqJobRow(t, targetJob);
					if (bulkMatch == null)
						return false;

					SerializadoReceiptJobInfo bulkTemp = new SerializadoReceiptJobInfo { JobNum = info.JobNum };
					ApplyBaqRowToJobInfo(bulkMatch, t, bulkTemp);
					if (!HasSerialUdData(bulkTemp))
						return false;

					MergeJobInfo(info, bulkTemp);
					return true;
				}

				DataRow row = FindBaqJobRow(t, targetJob);
				if (row == null)
					return false;

				SerializadoReceiptJobInfo temp = new SerializadoReceiptJobInfo { JobNum = info.JobNum };
				ApplyBaqRowToJobInfo(row, t, temp);
				if (!HasSerialUdData(temp))
					return false;

				MergeJobInfo(info, temp);
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	private static DataRow FindBaqJobRow(DataTable t, string targetJob)
	{
		if (t == null || t.Rows.Count == 0)
			return null;

		for (int i = 0; i < t.Rows.Count; i++)
		{
			DataRow r = t.Rows[i];
			string rowJob = ReadString(r, t, new string[] { "JobHead_JobNum", "JobNum" }, 0);
			if (string.Equals(rowJob, targetJob, StringComparison.OrdinalIgnoreCase))
				return r;
		}

		return t.Rows.Count == 1 ? t.Rows[0] : null;
	}

	private static void MergeJobInfo(SerializadoReceiptJobInfo target, SerializadoReceiptJobInfo source)
	{
		if (target == null || source == null)
			return;
		if (!string.IsNullOrWhiteSpace(source.PartNum))
			target.PartNum = source.PartNum;
		if (!string.IsNullOrWhiteSpace(source.PartDescription))
			target.PartDescription = source.PartDescription;
		if (!string.IsNullOrWhiteSpace(source.PurchaseOrder))
			target.PurchaseOrder = source.PurchaseOrder;
		if (!string.IsNullOrWhiteSpace(source.SalesOrder))
			target.SalesOrder = source.SalesOrder;
		if (!string.IsNullOrWhiteSpace(source.Cliente))
			target.Cliente = source.Cliente;
		if (!string.IsNullOrWhiteSpace(source.LotNum))
			target.LotNum = source.LotNum;
		if (!string.IsNullOrWhiteSpace(source.SerialPrefix))
			target.SerialPrefix = source.SerialPrefix;
		if (!string.IsNullOrWhiteSpace(source.SerialFrom))
			target.SerialFrom = source.SerialFrom;
		if (!string.IsNullOrWhiteSpace(source.SerialTo))
			target.SerialTo = source.SerialTo;
		if (source.RollCount > 0)
			target.RollCount = source.RollCount;
		if (source.SerializedQty > 0m)
			target.SerializedQty = source.SerializedQty;
	}

	private static void ApplyBaqRowToJobInfo(DataRow r, DataTable t, SerializadoReceiptJobInfo info)
	{
		if (string.IsNullOrWhiteSpace(info.PartNum))
			info.PartNum = ReadString(r, t, new string[] { "PartNum", "JobHead_PartNum" }, 0);
		info.PartDescription = ReadString(r, t, new string[] { "PartDescription", "Description" }, 1);
		info.PurchaseOrder = ReadString(r, t, new string[] { "orden_compra_c", "PurchaseOrder", "PO" }, 2);
		info.SalesOrder = ReadString(r, t, new string[] { "OrdenDeVenta_c", "SalesOrder", "OV" }, 3);
		info.Cliente = ReadString(r, t, new string[] { "Cliente_c", "Part_Cliente_c", "Cliente", "CustID_c" }, 4);
		info.LotNum = ReadString(r, t, new string[] { "LotNum_c", "LotNum" }, 11);
		info.SerialPrefix = ReadString(r, t, new string[] { "SerialPrefix_c", "SerialPrefix" }, 5);
		if (string.IsNullOrWhiteSpace(info.SerialPrefix))
			info.SerialPrefix = "J0";
		info.SerialFrom = ReadString(r, t, new string[] { "SerialFrom_c", "SerialFrom" }, 6);
		info.SerialTo = ReadString(r, t, new string[] { "SerialTo_c", "SerialTo" }, 7);
		info.RollCount = ReadInt(r, t, new string[] { "RollCount_c", "RollCount" }, 8);
		info.SerializedQty = ReadDecimal(r, t, new string[] { "SerializedQty_c", "SerializedQty" }, 9);

		decimal prodQty = ReadDecimal(r, t, new string[] { "ProdQty", "JobHead_ProdQty" }, 10);
		if (info.SerializedQty <= 0m && prodQty > 0m)
			info.SerializedQty = prodQty;
	}

	private static void PostProcessJobInfo(object oTrans, SerializadoReceiptJobInfo info)
	{
		if (info.RollCount <= 0 && info.SerializedQty > 0m)
		{
			int perRoll = GetLabelsPerRollFromUd10(oTrans, info.PartNum);
			if (perRoll > 0)
				info.RollCount = SplitIntoRollCapacities((int)Math.Round(info.SerializedQty, 0), perRoll).Count;
		}
	}

	private static void LoadPoFromOv(object oTrans, SerializadoReceiptJobInfo info)
	{
		if (string.IsNullOrWhiteSpace(info.SalesOrder))
			return;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet qds = new Ice.BO.QueryExecutionDataSet();
				qds.ExecutionParameter.AddExecutionParameterRow("OV", info.SalesOrder.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID("BAQPO", qds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t != null && t.Rows.Count > 0 && t.Rows[0][0] != DBNull.Value)
					info.PurchaseOrder = t.Rows[0][0].ToString().Trim();
			}
		}
		catch { }
	}

	private static void LoadClienteFromPart(object oTrans, SerializadoReceiptJobInfo info)
	{
		if (info == null || string.IsNullOrWhiteSpace(info.PartNum))
			return;

		TryLoadClienteFromPartBaq(oTrans, info.PartNum.Trim(), info);
		if (!string.IsNullOrWhiteSpace(info.Cliente))
			return;

		string fromView = TryReadClienteFromEpiViews(oTrans);
		if (!string.IsNullOrWhiteSpace(fromView))
		{
			info.Cliente = fromView;
			return;
		}

		LoadJobInfoFromPart(oTrans, info.PartNum, info);
		if (!string.IsNullOrWhiteSpace(info.Cliente))
			return;

		TryLoadClienteFromSerialJobBaq(oTrans, info);
	}

	private static void TryLoadClienteFromPartBaq(object oTrans, string partNum, SerializadoReceiptJobInfo info)
	{
		if (info == null || string.IsNullOrWhiteSpace(partNum))
			return;

		string cliente;
		if (ExecutePartClienteBaq(oTrans, partNum, "PARTE", out cliente)
			|| ExecutePartClienteBaq(oTrans, partNum, "Parte", out cliente))
		{
			if (!string.IsNullOrWhiteSpace(cliente))
				info.Cliente = cliente.Trim();
		}
	}

	private static bool ExecutePartClienteBaq(object oTrans, string partNum, string paramName, out string cliente)
	{
		cliente = string.Empty;
		if (string.IsNullOrWhiteSpace(partNum) || string.IsNullOrWhiteSpace(paramName))
			return false;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow(paramName, partNum.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID(PartClienteBaqName, ds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t == null || t.Rows.Count == 0)
					return false;
				cliente = ReadString(t.Rows[0], t, new string[] { "Part_Cliente_c", "Cliente_c", "Cliente" }, -1);
				if (string.IsNullOrWhiteSpace(cliente))
				{
					for (int c = 0; c < t.Columns.Count; c++)
					{
						string col = t.Columns[c].ColumnName ?? string.Empty;
						if (col.IndexOf("Cliente", StringComparison.OrdinalIgnoreCase) < 0)
							continue;
						object val = t.Rows[0][c];
						if (val == null || val == DBNull.Value)
							continue;
						cliente = val.ToString().Trim();
						if (cliente.Length > 0)
							break;
					}
				}
				return !string.IsNullOrWhiteSpace(cliente);
			}
		}
		catch
		{
			return false;
		}
	}

	private static string TryReadClienteFromEpiViews(object oTrans)
	{
		try
		{
			System.Reflection.PropertyInfo pi = oTrans.GetType().GetProperty("EpiDataViews");
			if (pi == null)
				return string.Empty;
			object viewsObj = pi.GetValue(oTrans, null);
			System.Collections.IDictionary views = viewsObj as System.Collections.IDictionary;
			if (views == null)
				return string.Empty;

			string[] viewNames = new string[] { "Part", "JobHead", "PartTran" };
			for (int i = 0; i < viewNames.Length; i++)
			{
				if (!views.Contains(viewNames[i]))
					continue;
				EpiDataView view = views[viewNames[i]] as EpiDataView;
				if (view == null || view.Row < 0 || view.dataView == null || view.dataView.Table == null)
					continue;
				if (!view.dataView.Table.Columns.Contains("Cliente_c"))
					continue;
				object val = view.dataView[view.Row]["Cliente_c"];
				if (val == null || val == DBNull.Value)
					continue;
				string s = val.ToString().Trim();
				if (s.Length > 0)
					return s;
			}
		}
		catch { }
		return string.Empty;
	}

	private static void TryLoadClienteFromSerialJobBaq(object oTrans, SerializadoReceiptJobInfo info)
	{
		if (info == null || string.IsNullOrWhiteSpace(info.JobNum))
			return;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("JobNum", info.JobNum.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID(SerialJobBaqName, ds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t == null || t.Rows.Count == 0)
					return;
				string cliente = ReadString(t.Rows[0], t, new string[] { "Cliente_c", "Part_Cliente_c", "Cliente", "CustID_c" }, -1);
				if (!string.IsNullOrWhiteSpace(cliente))
					info.Cliente = cliente;
			}
		}
		catch { }
	}

	private static void LoadJobInfoFromPart(object oTrans, string partNum, SerializadoReceiptJobInfo info)
	{
		if (string.IsNullOrWhiteSpace(partNum))
			return;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("partNum_", partNum.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID("CODIFLEX-UD10", ds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t == null || t.Rows.Count == 0)
					return;
				DataRow r = t.Rows[0];
				string cliente = ReadString(r, t, new string[] { "Cliente_c", "Cliente", "Part_Cliente_c" }, -1);
				if (string.IsNullOrWhiteSpace(cliente))
				{
					for (int c = 0; c < t.Columns.Count; c++)
					{
						string col = t.Columns[c].ColumnName ?? string.Empty;
						if (col.IndexOf("Cliente_c", StringComparison.OrdinalIgnoreCase) < 0)
							continue;
						cliente = r[c] == DBNull.Value ? string.Empty : r[c].ToString().Trim();
						if (cliente.Length > 0)
							break;
					}
				}
				if (!string.IsNullOrWhiteSpace(cliente))
					info.Cliente = cliente;
			}
		}
		catch { }
	}

	public static List<SerializadoReceiptRollRow> LoadRolls(object oTrans, string jobNum)
	{
		return LoadRolls(oTrans, jobNum, true);
	}

	public static List<SerializadoReceiptRollRow> LoadRolls(object oTrans, string jobNum, bool syncUd37)
	{
		RollBundle bundle = GetOrBuildBundle(oTrans, jobNum, string.Empty);
		return CloneRollList(bundle.Rolls);
	}

	public static void RefreshRollStatuses(object oTrans, string jobNum, List<SerializadoReceiptRollRow> rolls)
	{
		if (rolls == null || rolls.Count == 0 || string.IsNullOrWhiteSpace(jobNum))
			return;
		PtSerialRollRepository.ApplyStoredStatus(oTrans, jobNum.Trim(), rolls);
	}

	public static bool IsReceivedStatus(string status)
	{
		return string.Equals((status ?? string.Empty).Trim(), "Recibido", StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsRollConfirmed(SerializadoReceiptRollRow roll)
	{
		if (roll == null)
			return false;
		if (IsReceivedStatus(roll.Status))
			return true;
		return !string.IsNullOrWhiteSpace(roll.ConfirmedBy);
	}

	public static List<SerializadoReceiptRollRow> GetConfirmedRolls(List<SerializadoReceiptRollRow> rolls)
	{
		List<SerializadoReceiptRollRow> list = new List<SerializadoReceiptRollRow>();
		if (rolls == null)
			return list;
		for (int i = 0; i < rolls.Count; i++)
		{
			if (IsRollConfirmed(rolls[i]))
				list.Add(rolls[i]);
		}
		return list;
	}

	public static List<SerializadoReceiptRollRow> GetRollsForPrintDialog(object oTrans, string jobNum, List<SerializadoReceiptRollRow> rolls)
	{
		if (rolls == null || rolls.Count == 0)
			return new List<SerializadoReceiptRollRow>();

		List<SerializadoReceiptRollRow> working = CloneRollList(rolls);
		Dictionary<int, SerializadoReceiptRollRow> confirmedSnapshot = new Dictionary<int, SerializadoReceiptRollRow>();
		for (int i = 0; i < rolls.Count; i++)
		{
			if (!IsRollConfirmed(rolls[i]))
				continue;
			confirmedSnapshot[rolls[i].RollNum] = rolls[i];
		}

		if (!string.IsNullOrWhiteSpace(jobNum))
			RefreshRollStatuses(oTrans, jobNum, working);

		foreach (KeyValuePair<int, SerializadoReceiptRollRow> entry in confirmedSnapshot)
		{
			for (int i = 0; i < working.Count; i++)
			{
				if (working[i].RollNum != entry.Key)
					continue;

				working[i].Status = "Recibido";
				if (!string.IsNullOrWhiteSpace(entry.Value.ConfirmedBy))
					working[i].ConfirmedBy = entry.Value.ConfirmedBy;
				if (entry.Value.ConfirmedOn.HasValue)
					working[i].ConfirmedOn = entry.Value.ConfirmedOn;
				break;
			}
		}

		return GetConfirmedRolls(working);
	}

	private static List<SerializadoReceiptRollRow> BuildRollList(object oTrans, string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return new List<SerializadoReceiptRollRow>();
		SerializadoReceiptJobInfo info = LoadJobSerialData(oTrans, jobNum);
		return BuildRollListFromInfo(oTrans, info);
	}

	private static List<SerializadoReceiptRollRow> BuildRollListFromInfo(object oTrans, SerializadoReceiptJobInfo info)
	{
		List<SerializadoReceiptRollRow> list = new List<SerializadoReceiptRollRow>();
		if (info == null || string.IsNullOrWhiteSpace(info.SerialFrom) || info.SerializedQty <= 0m)
			return list;

		int totalQty = (int)Math.Round(info.SerializedQty, 0);
		int labelsPerRoll = GetLabelsPerRollFromUd10(oTrans, info.PartNum);
		List<int> capacities = SplitIntoRollCapacities(totalQty, labelsPerRoll);
		if (info.RollCount > 0 && capacities.Count != info.RollCount && info.RollCount != capacities.Count)
		{
			if (info.RollCount < capacities.Count)
				capacities = capacities.GetRange(0, info.RollCount);
		}

		long serialFrom = ParseSerialDigits(info.SerialFrom);
		if (serialFrom <= 0L)
			return list;

		string prefix = string.IsNullOrWhiteSpace(info.SerialPrefix)
			? ExtractPrefix(info.SerialFrom, "J0")
			: info.SerialPrefix;

		long current = serialFrom;
		for (int i = 0; i < capacities.Count; i++)
		{
			int qty = capacities[i];
			if (qty <= 0)
				continue;
			long serialTo = current + qty - 1L;
			SerializadoReceiptRollRow row = new SerializadoReceiptRollRow();
			row.RollNum = list.Count + 1;
			row.Prefix = prefix;
			row.SerialFrom = FormatSerialDisplay(prefix, current);
			row.SerialTo = FormatSerialDisplay(prefix, serialTo);
			row.Quantity = qty;
			row.Status = "Impreso";
			list.Add(row);
			current = serialTo + 1L;
		}

		return list;
	}

	public static bool TryConfirmRoll(object oTrans, string jobNum, string startInput, string endInput, out string message)
	{
		return TryConfirmRollInternal(oTrans, jobNum, startInput, endInput, 0, null, out message);
	}

	public static bool TryConfirmRoll(object oTrans, string jobNum, string startInput, string endInput,
		List<SerializadoReceiptRollRow> cachedRolls, out string message)
	{
		return TryConfirmRollInternal(oTrans, jobNum, startInput, endInput, 0, cachedRolls, out message);
	}

	public static bool TryConfirmRollByNumber(object oTrans, string jobNum, int rollNum, out string message)
	{
		message = string.Empty;
		List<SerializadoReceiptRollRow> rolls = LoadRolls(oTrans, jobNum);
		if (rolls.Count == 0)
		{
			message = "No hay rollos registrados para este Job.";
			return false;
		}

		SerializadoReceiptRollRow match = null;
		for (int i = 0; i < rolls.Count; i++)
		{
			if (rolls[i].RollNum == rollNum)
			{
				match = rolls[i];
				break;
			}
		}

		if (match == null)
		{
			message = "No se encontro el rollo #" + rollNum + ".";
			return false;
		}

		return TryConfirmRollInternal(oTrans, jobNum, match.SerialFrom, match.SerialTo, rollNum, null, out message);
	}

	private static bool TryConfirmRollInternal(object oTrans, string jobNum, string startInput, string endInput,
		int expectedRollNum, List<SerializadoReceiptRollRow> cachedRolls, out string message)
	{
		message = string.Empty;
		List<SerializadoReceiptRollRow> rolls = cachedRolls;
		if (rolls == null || rolls.Count == 0)
			rolls = LoadRolls(oTrans, jobNum, false);
		if (rolls.Count == 0)
		{
			message = "No hay rollos registrados para este Job.";
			return false;
		}

		string startNorm = NormalizeSerial(startInput);
		string endNorm = NormalizeSerial(endInput);
		if (startNorm.Length == 0 || endNorm.Length == 0)
		{
			message = "Capture START y END de la etiqueta física.";
			return false;
		}

		SerializadoReceiptRollRow match = null;
		for (int i = 0; i < rolls.Count; i++)
		{
			SerializadoReceiptRollRow r = rolls[i];
			if (string.Equals(NormalizeSerial(r.SerialFrom), startNorm, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeSerial(r.SerialTo), endNorm, StringComparison.OrdinalIgnoreCase))
			{
				match = r;
				break;
			}
		}

		if (match == null)
		{
			message = "El rango START/END no coincide con ningún rollo guardado en producción.";
			return false;
		}

		if (expectedRollNum > 0 && match.RollNum != expectedRollNum)
		{
			message = "El rollo seleccionado no coincide con START/END capturados.";
			return false;
		}

		if (string.Equals(match.Status, "Recibido", StringComparison.OrdinalIgnoreCase))
		{
			message = "El rollo #" + match.RollNum + " ya fue confirmado anteriormente.";
			return false;
		}

		string user = GetUserId(oTrans);
		try
		{
			SerializadoReceiptJobInfo jobInfo = LoadJobSerialData(oTrans, jobNum);
			PtSerialRollRepository.ConfirmRoll(oTrans, jobNum, jobInfo.PartNum, match, user);
			match.Status = "Recibido";
			match.ConfirmedBy = user;
			match.ConfirmedOn = DateTime.Now;
		}
		catch (Exception ex)
		{
			message = "Error al confirmar rollo: " + ex.Message;
			return false;
		}

		message = "Rollo #" + match.RollNum + " confirmado correctamente (" + match.Quantity.ToString("N0") + " pzas).";
		return true;
	}

	public static bool ValidateReceiptReady(object oTrans, string jobNum, decimal tranQty, out string error)
	{
		return ValidateReceiptReady(oTrans, jobNum, tranQty, null, out error);
	}

	public static bool ValidateReceiptReady(object oTrans, string jobNum, decimal tranQty,
		List<SerializadoReceiptRollRow> cachedRolls, out string error)
	{
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(jobNum) || tranQty <= 0m)
			return true;

		List<SerializadoReceiptRollRow> rolls = cachedRolls;
		if (rolls == null || rolls.Count == 0)
			rolls = LoadRolls(oTrans, jobNum, false);
		if (rolls.Count == 0)
		{
			error = "El Job requiere confirmación de serializado pero no hay datos en JobHead_UD (SerialFrom_c / RollCount_c).";
			return false;
		}

		int confirmedQty = 0;
		int pendingQty = 0;
		for (int i = 0; i < rolls.Count; i++)
		{
			if (string.Equals(rolls[i].Status, "Recibido", StringComparison.OrdinalIgnoreCase))
				confirmedQty += rolls[i].Quantity;
			else
				pendingQty += rolls[i].Quantity;
		}

		if (confirmedQty < (int)Math.Round(tranQty, 0))
		{
			error = "Debe confirmar los rollos serializados antes de recibir.\n\n" +
				"Cantidad de esta transacción: " + tranQty.ToString("N0") + "\n" +
				"Cantidad confirmada en PT: " + confirmedQty.ToString("N0") + "\n\n" +
				"Presione RECIBO SERIALIZADO y confirme rollo por rollo.";
			return false;
		}

		return true;
	}

	public static int CountConfirmed(List<SerializadoReceiptRollRow> rolls)
	{
		int n = 0;
		for (int i = 0; i < rolls.Count; i++)
			if (string.Equals(rolls[i].Status, "Recibido", StringComparison.OrdinalIgnoreCase))
				n++;
		return n;
	}

	private static string NormalizeSerial(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return string.Empty;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
			if (char.IsDigit(input[i]))
				sb.Append(input[i]);
		return sb.ToString();
	}

	private static string ExtractPrefix(string input, string fallback)
	{
		if (string.IsNullOrWhiteSpace(input))
			return fallback;
		StringBuilder prefix = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
		{
			if (char.IsDigit(input[i]))
				break;
			prefix.Append(input[i]);
		}
		return prefix.Length == 0 ? fallback : prefix.ToString();
	}

	private static string ReadString(DataRow row, DataTable t, string[] hints, int fallbackIndex)
	{
		for (int c = 0; c < t.Columns.Count; c++)
		{
			string name = t.Columns[c].ColumnName ?? string.Empty;
			for (int h = 0; h < hints.Length; h++)
			{
				if (name.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
					return Convert.ToString(row[c]).Trim();
			}
		}
		if (fallbackIndex >= 0 && fallbackIndex < t.Columns.Count)
			return Convert.ToString(row[fallbackIndex]).Trim();
		return string.Empty;
	}

	private static int ReadInt(DataRow row, DataTable t, string[] hints, int fallbackIndex)
	{
		string s = ReadString(row, t, hints, fallbackIndex);
		decimal d;
		if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
			return (int)Math.Round(d, 0);
		return 0;
	}

	private static decimal ReadDecimal(DataRow row, DataTable t, string[] hints, int fallbackIndex)
	{
		string s = ReadString(row, t, hints, fallbackIndex);
		decimal d;
		return decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d) ? d : 0m;
	}

	private static int GetLabelsPerRollFromUd10(object oTrans, string partNum)
	{
		if (string.IsNullOrWhiteSpace(partNum))
			return DefaultLabelsPerRoll;

		string partKey = partNum.Trim();
		int cached;
		if (LabelsPerRollCache.TryGetValue(partKey, out cached) && cached > 0)
			return cached;

		int result = DefaultLabelsPerRoll;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("partNum_", partKey, "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID("CODIFLEX-UD10", ds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t != null && t.Rows.Count > 0)
				{
					DataRow row = t.Rows[0];
					int number16 = ReadInt(row, t, new string[] { "Number16", "UD10_Number16", "CantUE", "Cant_UE" }, -1);
					if (number16 > 0)
						result = number16;
					else
					{
						int numEtiquetas = ReadInt(row, t, new string[] { "NumEtiquetas", "Num_Etiquetas" }, 9);
						if (numEtiquetas > 0)
							result = numEtiquetas;
					}
				}
			}
		}
		catch { }

		LabelsPerRollCache[partKey] = result;
		return result;
	}

	private static List<int> SplitIntoRollCapacities(int totalQty, int labelsPerRoll)
	{
		List<int> list = new List<int>();
		if (totalQty <= 0)
			return list;
		if (labelsPerRoll <= 0)
			labelsPerRoll = DefaultLabelsPerRoll;
		int remaining = totalQty;
		while (remaining > 0)
		{
			int chunk = remaining > labelsPerRoll ? labelsPerRoll : remaining;
			list.Add(chunk);
			remaining -= chunk;
		}
		return list;
	}

	private static long ParseSerialDigits(string input)
	{
		string norm = NormalizeSerial(input);
		long value;
		return long.TryParse(norm, out value) ? value : 0L;
	}

	private static string FormatSerialDisplay(string prefix, long serialNumber)
	{
		return (prefix ?? string.Empty) + serialNumber.ToString();
	}

	private static Ice.Core.Session GetSession(object oTrans)
	{
		try
		{
			System.Reflection.PropertyInfo pi = oTrans.GetType().GetProperty("Session");
			if (pi != null)
			{
				object val = pi.GetValue(oTrans, null);
				if (val != null)
					return (Ice.Core.Session)val;
			}
		}
		catch { }
		return null;
	}

	public static string GetCompanyCode(object oTrans)
	{
		try
		{
			Ice.Core.Session session = GetSession(oTrans);
			if (session != null && !string.IsNullOrWhiteSpace(session.CompanyID))
				return session.CompanyID.Trim();
		}
		catch { }

		try
		{
			System.Reflection.PropertyInfo companyPi = oTrans.GetType().GetProperty("Company");
			if (companyPi != null)
			{
				object companyObj = companyPi.GetValue(oTrans, null);
				string companyText = companyObj as string;
				if (!string.IsNullOrWhiteSpace(companyText))
					return companyText.Trim();

				if (companyObj != null)
				{
					string companyName = ReadObjectStringProperty(companyObj, "Company");
					if (!string.IsNullOrWhiteSpace(companyName))
						return companyName.Trim();
				}
			}

			System.Reflection.PropertyInfo companyIdPi = oTrans.GetType().GetProperty("CompanyID");
			if (companyIdPi != null)
			{
				string companyId = Convert.ToString(companyIdPi.GetValue(oTrans, null));
				if (!string.IsNullOrWhiteSpace(companyId))
					return companyId.Trim();
			}
		}
		catch { }

		return string.Empty;
	}

	private static string ReadObjectStringProperty(object target, string propertyName)
	{
		if (target == null || string.IsNullOrWhiteSpace(propertyName))
			return string.Empty;

		try
		{
			System.Reflection.PropertyInfo pi = target.GetType().GetProperty(propertyName);
			if (pi == null)
				return string.Empty;
			object val = pi.GetValue(target, null);
			return val == null ? string.Empty : Convert.ToString(val).Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string GetUserId(object oTrans)
	{
		string userKey = string.Empty;
		try
		{
			Ice.Core.Session session = GetSession(oTrans);
			if (session != null)
			{
				if (!string.IsNullOrWhiteSpace(session.UserName))
					userKey = session.UserName.Trim();
				else if (!string.IsNullOrWhiteSpace(session.EmployeeID))
					userKey = session.EmployeeID.Trim();
			}
		}
		catch { }

		if (string.IsNullOrWhiteSpace(userKey))
			userKey = Environment.UserName;

		return userKey;
	}
}

// =============================================================================
// UD37 — Confirmacion PT (reflexion — compila sin referencia UD37Adapter)
// =============================================================================

public static class PtSerialRollRepository
{
	public static void SyncRolls(object oTrans, string jobNum, string partNum, List<SerializadoReceiptRollRow> rolls)
	{
		if (rolls == null || rolls.Count == 0 || string.IsNullOrWhiteSpace(jobNum))
			return;

		string job = jobNum.Trim();
		string company = SerializadoReceiptService.GetCompanyCode(oTrans);
		string openError;
		using (Ud37Store store = Ud37Store.Open(oTrans, out openError))
		{
			if (store == null)
				return;

			for (int i = 0; i < rolls.Count; i++)
			{
				SerializadoReceiptRollRow roll = rolls[i];
				string rollKey = roll.RollNum.ToString(CultureInfo.InvariantCulture);
				bool exists = store.TryLoadRecord(job, rollKey);
				if (!exists)
				{
					store.EnsureNewRow();
					store.WriteRoll(store.GetRow(true), company, job, rollKey, partNum, roll, "Impreso", string.Empty, null, true);
					store.Update();
				}
			}
		}
	}

	public static void ApplyStoredStatus(object oTrans, string jobNum, List<SerializadoReceiptRollRow> rolls)
	{
		if (rolls == null || rolls.Count == 0 || string.IsNullOrWhiteSpace(jobNum))
			return;

		Dictionary<int, SerializadoReceiptRollRow> byRoll = new Dictionary<int, SerializadoReceiptRollRow>();
		for (int i = 0; i < rolls.Count; i++)
			byRoll[rolls[i].RollNum] = rolls[i];

		TryApplyStatusFromBaq(oTrans, jobNum, byRoll);
		TryApplyStatusFromUd37(oTrans, jobNum, byRoll);
	}

	public static void ApplyStoredStatusFast(object oTrans, string jobNum, List<SerializadoReceiptRollRow> rolls)
	{
		ApplyStoredStatus(oTrans, jobNum, rolls);
	}

	private static bool TryLoadUd37RollRow(Ud37Store store, string job, int rollNum, out object udRow)
	{
		udRow = null;
		if (store == null || rollNum <= 0)
			return false;

		string[] keys = new string[]
		{
			rollNum.ToString(CultureInfo.InvariantCulture),
			rollNum.ToString("00", CultureInfo.InvariantCulture),
			rollNum.ToString("000", CultureInfo.InvariantCulture)
		};

		for (int i = 0; i < keys.Length; i++)
		{
			if (!store.TryLoadRecord(job, keys[i]))
				continue;

			udRow = store.GetRow(false);
			if (udRow != null)
				return true;
		}

		return false;
	}

	private static int ResolveRollNumFromBaqRow(DataRow r, DataTable t)
	{
		int rollNum = ReadBaqInt(r, t, new string[] { "RollNum", "Number01", "UD37_Number01" });
		if (rollNum > 0)
			return rollNum;

		string key2 = ReadBaqField(r, t, new string[] { "Key2", "UD37_Key2" });
		if (string.IsNullOrWhiteSpace(key2))
			return 0;

		int parsed;
		if (int.TryParse(key2.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
			return parsed;

		return 0;
	}

	private static bool TryApplyStatusFromUd37(object oTrans, string jobNum, Dictionary<int, SerializadoReceiptRollRow> byRoll)
	{
		string openError;
		using (Ud37Store store = Ud37Store.Open(oTrans, out openError))
		{
			if (store == null)
				return false;

			string job = jobNum.Trim();
			bool any = false;

			foreach (KeyValuePair<int, SerializadoReceiptRollRow> entry in byRoll)
			{
				int rollNum = entry.Key;
				SerializadoReceiptRollRow row = entry.Value;
				object udRow;
				if (!TryLoadUd37RollRow(store, job, rollNum, out udRow))
					continue;

				string status = Ud37Store.ReadField(udRow, "Character05");
				if (!string.IsNullOrWhiteSpace(status))
				{
					if (!(SerializadoReceiptService.IsRollConfirmed(row) && !SerializadoReceiptService.IsReceivedStatus(status)))
					{
						row.Status = status;
						any = true;
					}
				}

				string confirmedBy = Ud37Store.ReadField(udRow, "Character06");
				if (!string.IsNullOrWhiteSpace(confirmedBy))
					row.ConfirmedBy = confirmedBy;

				string confirmedOnText = Ud37Store.ReadField(udRow, "Date01");
				if (!string.IsNullOrWhiteSpace(confirmedOnText))
				{
					DateTime dt;
					if (DateTime.TryParse(confirmedOnText, out dt))
						row.ConfirmedOn = dt;
				}
			}

			return any;
		}
	}

	public static void ConfirmRoll(object oTrans, string jobNum, string partNum, SerializadoReceiptRollRow roll, string userId)
	{
		if (roll == null || string.IsNullOrWhiteSpace(jobNum))
			throw new InvalidOperationException("Datos de rollo incompletos.");

		string company = SerializadoReceiptService.GetCompanyCode(oTrans);
		if (string.IsNullOrWhiteSpace(company))
			throw new InvalidOperationException("No se pudo determinar la compania Epicor.");

		string job = jobNum.Trim();
		string rollKey = roll.RollNum.ToString(CultureInfo.InvariantCulture);
		string user = string.IsNullOrWhiteSpace(userId) ? Environment.UserName : userId.Trim();
		string openError;

		using (Ud37Store store = Ud37Store.Open(oTrans, out openError))
		{
			if (store == null)
				throw new InvalidOperationException(
					"No se pudo abrir Ice.UD37.\n\n" +
					(openError ?? string.Empty) + "\n\n" +
					"En la personalizacion: Assembly Reference Manager → agregar Ice.Adapters.UD37.dll\n" +
					"Y verifique UD37 habilitado en User Defined Table Maintenance.");

			bool exists = store.TryLoadRecord(job, rollKey);
			if (!exists)
				store.EnsureNewRow();

			store.WriteRoll(store.GetRow(!exists), company, job, rollKey, partNum, roll, "Recibido", user, DateTime.Now, !exists);
			store.Update();
		}
	}

	private static bool TryApplyStatusFromBaq(object oTrans, string jobNum, Dictionary<int, SerializadoReceiptRollRow> byRoll)
	{
		bool any = false;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("JobNum", jobNum.Trim(), "nvarchar", false, Guid.Empty, "A");
				string company = SerializadoReceiptService.GetCompanyCode(oTrans);
				if (!string.IsNullOrWhiteSpace(company))
					ds.ExecutionParameter.AddExecutionParameterRow("Company", company, "nvarchar", false, Guid.Empty, "B");
				dq.ExecuteByID("BAQ-PT-SERIAL-UD37", ds);

				DataTable t = dq.QueryResults.Tables["Results"];
				if (t == null || t.Rows.Count == 0)
					return false;

				for (int i = 0; i < t.Rows.Count; i++)
				{
					DataRow r = t.Rows[i];
					int rollNum = ResolveRollNumFromBaqRow(r, t);
					if (rollNum <= 0)
						continue;

					SerializadoReceiptRollRow row;
					if (!byRoll.TryGetValue(rollNum, out row))
						continue;

					string status = ReadBaqField(r, t, new string[] { "Estado", "Status", "Character05", "UD37_Character05" });
					if (!string.IsNullOrWhiteSpace(status))
					{
						if (!(SerializadoReceiptService.IsRollConfirmed(row) && !SerializadoReceiptService.IsReceivedStatus(status)))
						{
							row.Status = status;
							any = true;
						}
					}

					row.ConfirmedBy = ReadBaqField(r, t, new string[] { "ConfirmadoPor", "ConfirmedBy", "Character06", "UD37_Character06" });
					string confirmedOnText = ReadBaqField(r, t, new string[] { "FechaConfirmacion", "ConfirmedOn", "Date01", "UD37_Date01" });
					if (!string.IsNullOrWhiteSpace(confirmedOnText))
					{
						DateTime dt;
						if (DateTime.TryParse(confirmedOnText, out dt))
							row.ConfirmedOn = dt;
					}
				}
			}
		}
		catch { }

		return any;
	}

	private static int ReadBaqInt(DataRow row, DataTable table, string[] hints)
	{
		string text = ReadBaqField(row, table, hints);
		decimal d;
		if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
			return (int)Math.Round(d, 0);
		return 0;
	}

	private static string ReadBaqField(DataRow row, DataTable table, string[] hints)
	{
		for (int c = 0; c < table.Columns.Count; c++)
		{
			string name = table.Columns[c].ColumnName ?? string.Empty;
			for (int h = 0; h < hints.Length; h++)
			{
				if (name.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
					return Convert.ToString(row[c]).Trim();
			}
		}
		return string.Empty;
	}

	private static string ExtractDigits(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return string.Empty;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
			if (char.IsDigit(input[i]))
				sb.Append(input[i]);
		return sb.ToString();
	}

	private sealed class Ud37Store : IDisposable
	{
		private object _adapter;

		public static Ud37Store Open(object oTrans, out string error)
		{
			error = string.Empty;
			try
			{
				System.Type adapterType = FindUd37AdapterType();
				if (adapterType == null)
				{
					error = "Tipo UD37Adapter no encontrado en memoria.";
					return null;
				}

				object adapter = System.Activator.CreateInstance(adapterType, oTrans);
				Invoke(adapter, "BOConnect");
				return new Ud37Store { _adapter = adapter };
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return null;
			}
		}

		public bool TryLoadRecord(string key1, string key2)
		{
			string where = string.Format(
				"Key1 = '{0}' AND Key2 = '{1}'",
				EscapeWhereValue(key1),
				EscapeWhereValue(key2));

			if (TryInvokeSearch(where))
				return GetUd37RowCount() > 0;

			return TryGetById(key1, key2);
		}

		private bool TryGetById(string key1, string key2)
		{
			try
			{
				System.Reflection.MethodInfo getById = FindMethod(_adapter.GetType(), "GetByID", new object[]
				{
					key1 ?? string.Empty,
					key2 ?? string.Empty,
					string.Empty,
					string.Empty,
					string.Empty
				});

				if (getById == null)
					return false;

				object result = getById.Invoke(_adapter, new object[]
				{
					key1 ?? string.Empty,
					key2 ?? string.Empty,
					string.Empty,
					string.Empty,
					string.Empty
				});

				if (result is bool)
					return (bool)result;

				return GetUd37RowCount() > 0;
			}
			catch
			{
				return false;
			}
		}

		private bool TryInvokeSearch(string whereClause)
		{
			try
			{
				System.Collections.Hashtable whereClauses = new System.Collections.Hashtable(1);
				whereClauses.Add("UD37", whereClause);
				SearchOptions opts = SearchOptions.CreateRuntimeSearch(whereClauses, DataSetMode.RowsDataSet);
				Invoke(_adapter, "InvokeSearch", opts);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private int GetUd37RowCount()
		{
			object data = GetProperty(_adapter, "UD37Data");
			if (data == null)
				return 0;

			object table = GetProperty(data, "UD37");
			if (table == null)
			{
				System.Data.DataSet ds = data as System.Data.DataSet;
				if (ds != null && ds.Tables.Count > 0)
					return ds.Tables[0].Rows.Count;
				return 0;
			}

			DataTable dt = table as DataTable;
			if (dt != null)
				return dt.Rows.Count;

			System.Reflection.PropertyInfo countPi = table.GetType().GetProperty("Count");
			if (countPi == null)
				return 0;

			return Convert.ToInt32(countPi.GetValue(table, null));
		}

		private static string EscapeWhereValue(string value)
		{
			if (string.IsNullOrEmpty(value))
				return string.Empty;
			return value.Replace("'", "''");
		}

		public void EnsureNewRow()
		{
			try
			{
				System.Reflection.MethodInfo getNew = FindMethod(_adapter.GetType(), "GetaNewUD37", null);
				if (getNew != null)
				{
					object result = getNew.Invoke(_adapter, null);
					if (result is bool && (bool)result)
						return;
				}
			}
			catch { }

			Invoke(_adapter, "AddNew");
		}

		public void Update()
		{
			Invoke(_adapter, "Update");
		}

		public object GetRow(bool isNew)
		{
			object data = GetProperty(_adapter, "UD37Data");
			if (data == null)
				return null;

			object table = GetProperty(data, "UD37");
			if (table == null)
			{
				System.Data.DataSet ds = data as System.Data.DataSet;
				if (ds != null && ds.Tables.Count > 0)
					table = ds.Tables[0];
			}

			DataTable dt = table as DataTable;
			if (dt != null)
			{
				if (dt.Rows.Count == 0)
					return null;
				return isNew ? dt.Rows[dt.Rows.Count - 1] : dt.Rows[0];
			}

			if (table == null)
				return null;

			System.Reflection.PropertyInfo countPi = table.GetType().GetProperty("Count");
			if (countPi == null)
				return null;

			int count = Convert.ToInt32(countPi.GetValue(table, null));
			if (count <= 0)
				return null;

			int index = isNew ? count - 1 : 0;
			System.Reflection.PropertyInfo itemPi = table.GetType().GetProperty("Item", new System.Type[] { typeof(int) });
			if (itemPi == null)
				return null;

			return itemPi.GetValue(table, new object[] { index });
		}

		public void WriteRoll(object row, string company, string job, string rollKey, string partNum,
			SerializadoReceiptRollRow roll, string status, string confirmedBy, DateTime? confirmedOn, bool isNewRecord)
		{
			if (row == null)
				throw new InvalidOperationException("No se pudo obtener fila UD37.");

			if (isNewRecord)
			{
				SetField(row, "Company", company);
				SetField(row, "Key1", job);
				SetField(row, "Key2", rollKey);
				SetField(row, "Key3", string.Empty);
				SetField(row, "Key4", string.Empty);
				SetField(row, "Key5", string.Empty);
			}

			SetField(row, "Character01", string.IsNullOrWhiteSpace(partNum) ? string.Empty : partNum.Trim());
			SetField(row, "Character02", string.IsNullOrWhiteSpace(roll.Prefix) ? "J0" : roll.Prefix.Trim());
			SetField(row, "Character03", ExtractDigits(roll.SerialFrom));
			SetField(row, "Character04", ExtractDigits(roll.SerialTo));
			SetField(row, "Character05", status ?? string.Empty);
			SetField(row, "Character06", confirmedBy ?? string.Empty);
			SetField(row, "Number01", roll.RollNum);
			SetField(row, "Number02", roll.Quantity);
			SetField(row, "Date01", confirmedOn.HasValue ? (object)confirmedOn.Value : DBNull.Value);
			SetField(row, "RowMod", "U");
		}

		public static string ReadField(object row, string fieldName)
		{
			if (row == null)
				return string.Empty;

			DataRow dr = row as DataRow;
			if (dr != null && dr.Table.Columns.Contains(fieldName))
				return Convert.ToString(dr[fieldName]).Trim();

			System.Reflection.PropertyInfo pi = row.GetType().GetProperty(fieldName);
			if (pi != null)
				return Convert.ToString(pi.GetValue(row, null)).Trim();

			return string.Empty;
		}

		private static void SetField(object row, string fieldName, object value)
		{
			DataRow dr = row as DataRow;
			if (dr != null)
			{
				if (dr.Table.Columns.Contains(fieldName))
				{
					dr.BeginEdit();
					dr[fieldName] = value ?? DBNull.Value;
					dr.EndEdit();
				}
				return;
			}

			System.Reflection.PropertyInfo pi = row.GetType().GetProperty(fieldName);
			if (pi != null && pi.CanWrite)
				pi.SetValue(row, value, null);
		}

		private static System.Type FindUd37AdapterType()
		{
			string[] typeNames = new string[]
			{
				"Ice.Adapters.UD37Adapter",
				"UD37Adapter"
			};

			string[] assemblyNames = new string[]
			{
				"Ice.Adapters.UD37",
				"Ice.Adapters",
				"Ice.UD37.Adapters"
			};

			for (int a = 0; a < assemblyNames.Length; a++)
			{
				try
				{
					System.Reflection.Assembly asm = System.Reflection.Assembly.Load(assemblyNames[a]);
					for (int t = 0; t < typeNames.Length; t++)
					{
						System.Type found = asm.GetType(typeNames[t], false);
						if (found != null)
							return found;
					}
				}
				catch { }
			}

			for (int t = 0; t < typeNames.Length; t++)
			{
				System.Type found = System.Type.GetType(typeNames[t] + ", Ice.Adapters.UD37");
				if (found != null)
					return found;
				found = System.Type.GetType(typeNames[t] + ", Ice.Adapters");
				if (found != null)
					return found;
			}

			System.Reflection.Assembly[] loaded = System.AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < loaded.Length; i++)
			{
				for (int t = 0; t < typeNames.Length; t++)
				{
					System.Type found = loaded[i].GetType(typeNames[t], false);
					if (found != null)
						return found;
				}

				try
				{
					System.Type[] types = loaded[i].GetTypes();
					for (int j = 0; j < types.Length; j++)
					{
						if (types[j] != null && types[j].Name == "UD37Adapter")
							return types[j];
					}
				}
				catch (System.Reflection.ReflectionTypeLoadException ex)
				{
					System.Type[] types = ex.Types;
					if (types != null)
					{
						for (int j = 0; j < types.Length; j++)
						{
							if (types[j] != null && types[j].Name == "UD37Adapter")
								return types[j];
						}
					}
				}
				catch { }
			}

			return null;
		}

		private static object GetProperty(object target, string propertyName)
		{
			if (target == null)
				return null;
			System.Reflection.PropertyInfo pi = target.GetType().GetProperty(propertyName);
			if (pi == null)
				return null;
			return pi.GetValue(target, null);
		}

		private static System.Reflection.MethodInfo FindMethod(System.Type targetType, string methodName, object[] args)
		{
			int paramCount = args == null ? 0 : args.Length;
			System.Reflection.MethodInfo[] methods = targetType.GetMethods(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

			for (int i = 0; i < methods.Length; i++)
			{
				System.Reflection.MethodInfo method = methods[i];
				if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
					continue;

				System.Reflection.ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length != paramCount)
					continue;

				bool matches = true;
				for (int p = 0; p < parameters.Length; p++)
				{
					if (args[p] == null)
						continue;
					if (!parameters[p].ParameterType.IsAssignableFrom(args[p].GetType()))
					{
						matches = false;
						break;
					}
				}

				if (matches)
					return method;
			}

			if (string.Equals(methodName, "GetByID", StringComparison.Ordinal) && paramCount == 5)
			{
				return targetType.GetMethod("GetByID",
					new System.Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
			}

			if (paramCount == 0)
			{
				System.Reflection.MethodInfo noArgs = targetType.GetMethod(methodName, System.Type.EmptyTypes);
				if (noArgs != null)
					return noArgs;
			}

			return null;
		}

		private static object Invoke(object target, string methodName, params object[] args)
		{
			System.Reflection.MethodInfo mi = FindMethod(target.GetType(), methodName, args);
			if (mi == null)
				throw new MissingMethodException(target.GetType().FullName, methodName);
			return mi.Invoke(target, args);
		}

		public void Dispose()
		{
			if (_adapter == null)
				return;
			try
			{
				Invoke(_adapter, "Dispose");
			}
			catch { }
			_adapter = null;
		}
	}
}

// =============================================================================
// IMPRESION ZPL — Etiquetas 3" x 2" core + caja (impresora Zebra Windows)
// =============================================================================

public sealed class PtSerialPrintDialog : Form
{
	public string SelectedPrinter { get; private set; }
	public List<SerializadoReceiptRollRow> SelectedRolls { get; private set; }
	public int CoreCopiesPerRoll
	{
		get { return _numCoreCopies != null ? (int)_numCoreCopies.Value : 1; }
	}

	private readonly SerializadoReceiptJobInfo _job;
	private readonly List<SerializadoReceiptRollRow> _printableRolls = new List<SerializadoReceiptRollRow>();
	private readonly CheckedListBox _rollList;
	private readonly ComboBox _cboPrinter;
	private readonly Label _lblSummary;
	private readonly Label _lblJobInfo;
	private readonly NumericUpDown _numCoreCopies;

	public PtSerialPrintDialog(SerializadoReceiptJobInfo job, List<SerializadoReceiptRollRow> allRolls, ICollection<int> preselectedRollNums)
	{
		_job = job;
		Text = "Imprimir etiquetas PT";
		FormBorderStyle = FormBorderStyle.Sizable;
		StartPosition = FormStartPosition.CenterParent;
		MaximizeBox = true;
		MinimizeBox = false;
		MinimumSize = new Size(520, 440);
		ClientSize = new Size(560, 480);
		Font = new Font("Segoe UI", 9f);

		HashSet<int> preselected = new HashSet<int>();
		if (preselectedRollNums != null)
		{
			foreach (int n in preselectedRollNums)
				preselected.Add(n);
		}

		Label lblInfo = new Label
		{
			Text = "Solo aparecen rollos CONFIRMADOS (Recibido) en PT.\n" +
				"Marque los que van en ESTA CAJA: 1 etiqueta CAJA + 1 CORE por rollo (x copias).",
			Location = new Point(12, 10),
			Size = new Size(530, 36),
			ForeColor = Color.FromArgb(0, 70, 140),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};

		Label lblPrinter = new Label { Text = "Impresora:", Location = new Point(12, 50), AutoSize = true };
		_cboPrinter = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Location = new Point(90, 46),
			Width = 458,
			Font = new Font("Segoe UI", 9f)
		};
		Load += PtSerialPrintDialog_Load;

		string clienteDisplay = string.IsNullOrWhiteSpace(_job != null ? _job.Cliente : null)
			? "(sin cliente)"
			: _job.Cliente.Trim();
		_lblJobInfo = new Label
		{
			Text = string.Format("Job: {0}   |   Parte: {1}   |   Cliente: {2}",
				_job != null && !string.IsNullOrWhiteSpace(_job.JobNum) ? _job.JobNum.Trim() : "—",
				_job != null && !string.IsNullOrWhiteSpace(_job.PartNum) ? _job.PartNum.Trim() : "—",
				clienteDisplay),
			Location = new Point(12, 76),
			Size = new Size(536, 18),
			ForeColor = Color.FromArgb(64, 64, 64),
			Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
		};

		Label lblRolls = new Label
		{
			Text = "Rollos en esta CAJA:",
			Location = new Point(12, 100),
			AutoSize = true,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};

		Button btnAll = new Button { Text = "Todos", Location = new Point(130, 96), Width = 64, Height = 26 };
		Button btnNone = new Button { Text = "Ninguno", Location = new Point(200, 96), Width = 72, Height = 26 };

		Label lblCoreCopies = new Label
		{
			Text = "Copias CORE/rollo:",
			Location = new Point(286, 100),
			AutoSize = true,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
		_numCoreCopies = new NumericUpDown
		{
			Location = new Point(408, 97),
			Width = 48,
			Height = 24,
			Minimum = 1,
			Maximum = 99,
			Value = 1,
			TextAlign = HorizontalAlignment.Center,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
		_numCoreCopies.ValueChanged += delegate { UpdateSummary(); };

		Label lblCajaHint = new Label
		{
			Location = new Point(462, 100),
			Size = new Size(86, 26),
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Color.FromArgb(120, 120, 120),
			Font = new Font("Segoe UI", 8f),
			Text = "1 CAJA"
		};

		_rollList = new CheckedListBox
		{
			Location = new Point(12, 128),
			Size = new Size(536, 258),
			CheckOnClick = true,
			Font = new Font("Consolas", 9f),
			IntegralHeight = false
		};

		if (allRolls != null)
		{
			for (int i = 0; i < allRolls.Count; i++)
			{
				SerializadoReceiptRollRow roll = allRolls[i];
				_printableRolls.Add(roll);
				int idx = _rollList.Items.Add(FormatRollLine(roll));
				bool check = preselected.Contains(roll.RollNum) || preselected.Count == 0;
				_rollList.SetItemChecked(idx, check);
			}
		}

		_lblSummary = new Label
		{
			Location = new Point(12, 394),
			Size = new Size(536, 20),
			ForeColor = Color.FromArgb(0, 86, 179),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};

		Button btnPreview = new Button
		{
			Text = "Vista previa",
			Location = new Point(12, 424),
			Width = 110,
			Height = 32,
			BackColor = Color.FromArgb(92, 107, 192),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
		Button btnPrint = new Button
		{
			Text = "Imprimir",
			Location = new Point(368, 424),
			Width = 88,
			Height = 32,
			BackColor = Color.FromArgb(253, 126, 20),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
		Button btnCancel = new Button
		{
			Text = "Cancelar",
			Location = new Point(462, 424),
			Width = 86,
			Height = 32,
			DialogResult = DialogResult.Cancel
		};

		btnAll.Click += delegate
		{
			for (int i = 0; i < _rollList.Items.Count; i++)
				_rollList.SetItemChecked(i, true);
			UpdateSummary();
		};
		btnNone.Click += delegate
		{
			for (int i = 0; i < _rollList.Items.Count; i++)
				_rollList.SetItemChecked(i, false);
			UpdateSummary();
		};
		_rollList.ItemCheck += delegate { UpdateSummary(); };
		btnPreview.Click += BtnPreview_Click;
		btnPrint.Click += BtnPrint_Click;

		Controls.Add(lblInfo);
		Controls.Add(lblPrinter);
		Controls.Add(_cboPrinter);
		Controls.Add(_lblJobInfo);
		Controls.Add(lblRolls);
		Controls.Add(btnAll);
		Controls.Add(btnNone);
		Controls.Add(lblCoreCopies);
		Controls.Add(_numCoreCopies);
		Controls.Add(lblCajaHint);
		Controls.Add(_rollList);
		Controls.Add(_lblSummary);
		Controls.Add(btnPreview);
		Controls.Add(btnPrint);
		Controls.Add(btnCancel);
		CancelButton = btnCancel;
		AcceptButton = btnPrint;

		if (_printableRolls.Count == 0)
			_lblSummary.Text = "No hay rollos confirmados (Recibido) para imprimir.";
		else
			UpdateSummary();
	}

	private void PtSerialPrintDialog_Load(object sender, EventArgs e)
	{
		try
		{
			LoadPrinters(_cboPrinter);
		}
		catch { }
	}

	private static string FormatRollLine(SerializadoReceiptRollRow roll)
	{
		return string.Format("Rollo {0,2}  |  {1} -> {2}  |  {3:N0} pzas  |  {4}",
			roll.RollNum, roll.SerialFrom, roll.SerialTo, roll.Quantity, roll.Status ?? string.Empty);
	}

	private static void LoadPrinters(ComboBox cbo)
	{
		if (cbo == null)
			return;

		string defaultName = PtSerialLabelPrinter.DefaultPrinterName;
		string selectedBefore = cbo.SelectedItem != null ? cbo.SelectedItem.ToString() : string.Empty;
		cbo.Items.Clear();
		cbo.Enabled = true;

		try
		{
			foreach (string p in PrinterSettings.InstalledPrinters)
			{
				if (IsZebraPrinterName(p))
					cbo.Items.Add(p);
			}
		}
		catch { }

		if (cbo.Items.Count == 0)
		{
			cbo.Items.Add("(Sin impresora Zebra instalada en Windows)");
			cbo.SelectedIndex = 0;
			cbo.Enabled = false;
			return;
		}

		int pick = -1;
		if (!string.IsNullOrWhiteSpace(selectedBefore))
		{
			for (int i = 0; i < cbo.Items.Count; i++)
			{
				if (string.Equals(cbo.Items[i].ToString(), selectedBefore, StringComparison.OrdinalIgnoreCase))
				{
					pick = i;
					break;
				}
			}
		}
		if (pick < 0)
		{
			for (int i = 0; i < cbo.Items.Count; i++)
			{
				if (string.Equals(cbo.Items[i].ToString(), defaultName, StringComparison.OrdinalIgnoreCase))
				{
					pick = i;
					break;
				}
			}
		}
		if (pick < 0)
			pick = 0;
		cbo.SelectedIndex = pick;
	}

	private static bool IsZebraPrinterName(string name)
	{
		return PtZebraPrintTransport.IsZebraPrinterName(name);
	}

	private List<SerializadoReceiptRollRow> GetCheckedRolls()
	{
		List<SerializadoReceiptRollRow> list = new List<SerializadoReceiptRollRow>();
		for (int i = 0; i < _rollList.Items.Count && i < _printableRolls.Count; i++)
		{
			if (_rollList.GetItemChecked(i))
				list.Add(_printableRolls[i]);
		}
		return list;
	}

	private void UpdateSummary()
	{
		int n = 0;
		for (int i = 0; i < _rollList.Items.Count; i++)
		{
			if (_rollList.GetItemChecked(i))
				n++;
		}
		int x = CoreCopiesPerRoll;
		int coreTotal = n * x;
		int labelTotal = coreTotal + 1;
		string warn = n > PtSerialLabelPrinter.BoxLabelMaxRecommendedRolls
			? string.Format("  (CAJA se ajusta; recomendado max {0} rollos)", PtSerialLabelPrinter.BoxLabelMaxRecommendedRolls)
			: string.Empty;
		_lblSummary.Text = string.Format(
			"1 CAJA ({0} rollos en tabla) + {1} CORE ({0} x {2}) = {3} etiqueta(s) 3\" x 2\"{4}",
			n, coreTotal, x, labelTotal, warn);
	}

	private void BtnPreview_Click(object sender, EventArgs e)
	{
		List<SerializadoReceiptRollRow> rolls = GetCheckedRolls();
		if (rolls.Count == 0)
		{
			MessageBox.Show("Marque al menos un rollo para la vista previa.", Text,
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		PtSerialLabelPrinter.ShowPreview(this, _job, rolls, CoreCopiesPerRoll);
	}

	private void BtnPrint_Click(object sender, EventArgs e)
	{
		if (!_cboPrinter.Enabled || _cboPrinter.SelectedItem == null)
		{
			MessageBox.Show(
				"No hay impresora Zebra instalada en Windows.\nInstale el driver ZTC/ZEBRAEPICOR e intente de nuevo.",
				Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		string printer = _cboPrinter.SelectedItem.ToString();
		if (printer.StartsWith("(", StringComparison.Ordinal) || !IsZebraPrinterName(printer))
		{
			MessageBox.Show("Seleccione una impresora Zebra instalada.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		List<SerializadoReceiptRollRow> rolls = GetCheckedRolls();
		if (rolls.Count == 0)
		{
			MessageBox.Show("Marque al menos un rollo confirmado para esta CAJA.", Text,
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		if (rolls.Count > PtSerialLabelPrinter.BoxLabelMaxRollsPerLabel)
		{
			MessageBox.Show(string.Format(
				"Demasiados rollos para 1 etiqueta CAJA ({0}).\nSeleccione maximo {1} rollos por caja fisica.",
				rolls.Count, PtSerialLabelPrinter.BoxLabelMaxRollsPerLabel), Text,
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		string setupError;
		if (!PtZebraPrintTransport.ValidatePrintSetup(printer, out setupError))
		{
			MessageBox.Show(setupError, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		SelectedPrinter = printer;
		SelectedRolls = rolls;
		DialogResult = DialogResult.OK;
		Close();
	}
}

public sealed class PtPrintProgressForm : Form
{
	public bool PrintOk { get; private set; }
	public string PrintError { get; private set; }
	public int LabelsSent { get; private set; }
	public int RollsSent { get; private set; }
	public int CoreLabelsSent { get; private set; }
	public bool BoxLabelSent { get; private set; }

	private readonly string _printerName;
	private readonly SerializadoReceiptJobInfo _job;
	private readonly List<SerializadoReceiptRollRow> _rolls;
	private readonly int _coreCopiesPerRoll;
	private readonly Label _lblStatus;
	private readonly ProgressBar _progress;

	public PtPrintProgressForm(string printerName, SerializadoReceiptJobInfo job,
		List<SerializadoReceiptRollRow> rolls, int coreCopiesPerRoll)
	{
		_printerName = printerName;
		_job = job;
		_rolls = rolls;
		_coreCopiesPerRoll = coreCopiesPerRoll < 1 ? 1 : coreCopiesPerRoll;

		Text = "Imprimiendo etiquetas PT";
		FormBorderStyle = FormBorderStyle.FixedDialog;
		StartPosition = FormStartPosition.CenterParent;
		MaximizeBox = false;
		MinimizeBox = false;
		ControlBox = false;
		ClientSize = new Size(440, 100);
		ShowInTaskbar = false;

		_lblStatus = new Label
		{
			Location = new Point(12, 12),
			Size = new Size(416, 32),
			Text = "Preparando envio ZPL...",
			Font = new Font("Segoe UI", 9f)
		};
		_progress = new ProgressBar
		{
			Location = new Point(12, 52),
			Size = new Size(416, 22),
			Style = ProgressBarStyle.Continuous,
			Minimum = 0,
			Maximum = 100
		};

		Controls.Add(_lblStatus);
		Controls.Add(_progress);

		Shown += PtPrintProgressForm_Shown;
	}

	public void ReportPrintProgress(int percent, string message)
	{
		if (percent >= 0)
			_progress.Value = Math.Max(0, Math.Min(100, percent));
		if (!string.IsNullOrWhiteSpace(message))
			_lblStatus.Text = message;
		Refresh();
		Application.DoEvents();
	}

	private void PtPrintProgressForm_Shown(object sender, EventArgs e)
	{
		Shown -= PtPrintProgressForm_Shown;
		try
		{
			string error;
			int labelsSent;
			int rollsSent;
			PrintOk = PtSerialLabelPrinter.TryPrintSelectedRolls(
				_printerName, _job, _rolls, _coreCopiesPerRoll, this, out error, out labelsSent, out rollsSent);
			PrintError = error ?? string.Empty;
			LabelsSent = labelsSent;
			RollsSent = rollsSent;
			CoreLabelsSent = PtSerialLabelPrinter.LastCoreLabelsSent;
			BoxLabelSent = PtSerialLabelPrinter.LastBoxLabelSent;
		}
		catch (Exception ex)
		{
			PrintOk = false;
			PrintError = ex.Message;
		}
		Close();
	}
}

public static class PtSerialLabelPrinter
{
	public const string DefaultPrinterName = "ZEBRAEPICOR";
	public const int LabelWidthHundredthsInch = 300;
	public const int LabelHeightHundredthsInch = 200;
	// 3" x 2" @ 300 dpi (ZTC 110Xi4-300dpi / ZEBRAEPICOR @ 192.168.60.122)
	private const int ZplDpi = 300;
	private const int ZplWidthDots = 900;
	private const int ZplHeightDots = 600;
	private const int ZplMargin = 12;
	private const int ZplMidX = 450;
	private const int ZplLeftColX = 24;
	private const int ZplRightColX = 456;
	private const int ZplContentW = 876;

	public const int BoxLabelMaxRecommendedRolls = 12;
	public const int BoxLabelMaxRollsPerLabel = 20;

	public static int LastCoreLabelsSent { get; private set; }
	public static bool LastBoxLabelSent { get; private set; }

	private sealed class PtBoxLabelLayout
	{
		public int InnerX;
		public int InnerY;
		public int InnerW;
		public int InnerH;
		public int LabelColW;
		public int HeaderRowH;
		public int HeaderBlockH;
		public int TableTop;
		public int TableHeaderH;
		public int DataRowH;
		public int HdrLabelFont;
		public int HdrValueFont;
		public int TableHdrFont;
		public int TableDataFont;
		public int ColRolloW;
		public int ColStartW;
		public int ColEndW;
		public int ColStartX;
		public int ColEndX;
		public int RevX;
		public int RevFont;

		public static PtBoxLabelLayout Compute(int rollCount)
		{
			rollCount = Math.Max(1, rollCount);
			PtBoxLabelLayout m = new PtBoxLabelLayout();
			m.InnerX = 15;
			m.InnerY = 12;
			m.RevX = 804;
			m.InnerW = m.RevX - m.InnerX - 12;
			m.InnerH = 576;
			m.LabelColW = 168;
			m.RevFont = 30;

			if (rollCount <= 3)
			{
				m.HeaderRowH = 54;
				m.HdrLabelFont = 24;
				m.HdrValueFont = 42;
				m.TableDataFont = 45;
			}
			else if (rollCount <= 5)
			{
				m.HeaderRowH = 48;
				m.HdrLabelFont = 21;
				m.HdrValueFont = 36;
				m.TableDataFont = 39;
			}
			else if (rollCount <= 8)
			{
				m.HeaderRowH = 42;
				m.HdrLabelFont = 20;
				m.HdrValueFont = 30;
				m.TableDataFont = 33;
			}
			else if (rollCount <= 12)
			{
				m.HeaderRowH = 36;
				m.HdrLabelFont = 18;
				m.HdrValueFont = 27;
				m.TableDataFont = 27;
			}
			else
			{
				m.HeaderRowH = 33;
				m.HdrLabelFont = 17;
				m.HdrValueFont = 24;
				m.TableDataFont = 23;
			}

			m.HeaderBlockH = m.HeaderRowH * 3;
			m.TableTop = m.InnerY + m.HeaderBlockH;
			int tableSpace = m.InnerH - m.HeaderBlockH;
			m.TableHeaderH = rollCount <= 6 ? 39 : 33;
			m.DataRowH = Math.Max(24, Math.Min(69, (tableSpace - m.TableHeaderH) / rollCount));
			m.TableHdrFont = Math.Max(20, m.TableDataFont - 3);
			m.ColRolloW = rollCount <= 8 ? 114 : 96;
			int rem = m.InnerW - m.ColRolloW;
			m.ColStartW = rem / 2;
			m.ColEndW = rem - m.ColStartW;
			m.ColStartX = m.InnerX + m.ColRolloW;
			m.ColEndX = m.ColStartX + m.ColStartW;
			return m;
		}
	}

	private static int ZplLeftColWidth()
	{
		return ZplMidX - ZplLeftColX - 4;
	}

	private static int ZplRightColWidth()
	{
		return ZplWidthDots - ZplMargin - ZplRightColX;
	}

	public static bool TryPrintSelectedRolls(string printerName, SerializadoReceiptJobInfo job,
		List<SerializadoReceiptRollRow> rolls, out string error)
	{
		int labelsSent;
		int rollsSent;
		return TryPrintSelectedRolls(printerName, job, rolls, 1, null, out error, out labelsSent, out rollsSent);
	}

	public static bool TryPrintSelectedRolls(string printerName, SerializadoReceiptJobInfo job,
		List<SerializadoReceiptRollRow> rolls, PtPrintProgressForm progress, out string error)
	{
		int labelsSent;
		int rollsSent;
		return TryPrintSelectedRolls(printerName, job, rolls, 1, progress, out error, out labelsSent, out rollsSent);
	}

	public static void ShowPrintResultDialog(IWin32Window owner, int labelsSent, int rollsSent,
		string printerName, bool fullSuccess, string errorDetail, int coreLabels, bool boxLabelSent,
		int coreCopiesPerRoll)
	{
		string printer = string.IsNullOrWhiteSpace(printerName) ? "(sin nombre)" : printerName.Trim();
		StringBuilder msg = new StringBuilder();
		if (fullSuccess)
			msg.AppendLine("Impresion enviada correctamente.");
		else if (labelsSent > 0)
			msg.AppendLine("Impresion parcial — algunas etiquetas no se enviaron.");
		else
			msg.AppendLine("No se envio ninguna etiqueta a la impresora.");

		msg.AppendLine();
		msg.AppendLine(string.Format("Etiquetas CORE ({0} por rollo): {1}", coreCopiesPerRoll, coreLabels));
		msg.AppendLine(string.Format("Etiqueta CAJA (resumen rollos): {0}", boxLabelSent ? "1" : "0"));
		msg.AppendLine(string.Format("Total enviadas: {0}", labelsSent));
		msg.AppendLine(string.Format("Rollos en la caja: {0}", rollsSent));
		msg.AppendLine(string.Format("Impresora Zebra: {0}", printer));

		if (!string.IsNullOrWhiteSpace(errorDetail))
		{
			msg.AppendLine();
			msg.Append(errorDetail.Trim());
		}

		MessageBox.Show(
			msg.ToString(),
			fullSuccess ? "Impresion Zebra" : (labelsSent > 0 ? "Impresion parcial" : "Error de impresion"),
			MessageBoxButtons.OK,
			fullSuccess ? MessageBoxIcon.Information : (labelsSent > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Error));
	}

	public static bool TryPrintSelectedRolls(string printerName, SerializadoReceiptJobInfo job,
		List<SerializadoReceiptRollRow> rolls, int coreCopiesPerRoll, PtPrintProgressForm progress,
		out string error, out int labelsSent, out int rollsSent)
	{
		error = string.Empty;
		labelsSent = 0;
		rollsSent = 0;
		if (coreCopiesPerRoll < 1)
			coreCopiesPerRoll = 1;
		if (job == null || rolls == null || rolls.Count == 0)
		{
			error = "No hay rollos seleccionados para imprimir.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(printerName))
		{
			error = "Seleccione una impresora Zebra.";
			return false;
		}

		string setupError;
		if (!PtZebraPrintTransport.ValidatePrintSetup(printerName, out setupError))
		{
			error = setupError;
			return false;
		}

		int totalCoreLabels = rolls.Count * coreCopiesPerRoll;
		List<SerializadoReceiptRollRow> coresOk = new List<SerializadoReceiptRollRow>();
		StringBuilder errors = new StringBuilder();
		int coreSent = 0;
		for (int i = 0; i < rolls.Count; i++)
		{
			SerializadoReceiptRollRow roll = rolls[i];
			for (int copy = 0; copy < coreCopiesPerRoll; copy++)
			{
				if (progress != null)
				{
					int pct = totalCoreLabels > 0
						? (int)((double)coreSent / (totalCoreLabels + 1) * 100.0)
						: 0;
					progress.ReportPrintProgress(pct, string.Format(
						"CORE rollo {0}/{1} copia {2}/{3} (#{4})...",
						i + 1, rolls.Count, copy + 1, coreCopiesPerRoll, roll.RollNum));
				}

				string rollError;
				if (!TrySendZpl(printerName, BuildCoreLabelZpl(job, roll), out rollError))
				{
					errors.AppendLine(string.Format("Rollo #{0} CORE copia {1}: {2}",
						roll.RollNum, copy + 1, rollError));
					break;
				}

				labelsSent++;
				coreSent++;
			}

			if (coreSent < ((i + 1) * coreCopiesPerRoll))
				break;

			coresOk.Add(roll);
		}

		if (coresOk.Count == 0)
		{
			error = errors.Length > 0 ? errors.ToString().Trim() : "No se pudo imprimir.";
			return false;
		}

		if (progress != null)
			progress.ReportPrintProgress(90, string.Format("Etiqueta CAJA ({0} rollo(s))...", coresOk.Count));

		string boxError;
		bool boxOk = TrySendZpl(printerName, BuildBoxLabelZpl(job, coresOk), out boxError);
		if (boxOk)
		{
			labelsSent++;
			rollsSent = coresOk.Count;
		}
		else
		{
			errors.AppendLine("Etiqueta CAJA: " + boxError);
		}

		if (progress != null)
			progress.ReportPrintProgress(100, "Finalizado.");

		if (boxOk && coresOk.Count == rolls.Count)
		{
			LastCoreLabelsSent = coreSent;
			LastBoxLabelSent = true;
			return true;
		}

		LastCoreLabelsSent = coreSent;
		LastBoxLabelSent = boxOk;

		if (coresOk.Count > 0)
		{
			error = string.Format(
				boxOk
					? "Se imprimieron {0} CORE + 1 CAJA, pero faltaron algunos rollos.\n{1}"
					: "Se imprimieron {0} CORE pero fallo la etiqueta CAJA.\n{1}",
				coreSent, errors.ToString().Trim());
		}
		else
			error = errors.Length > 0 ? errors.ToString().Trim() : "No se pudo imprimir.";
		return false;
	}

	public static void ShowPreview(IWin32Window owner, SerializadoReceiptJobInfo job,
		List<SerializadoReceiptRollRow> rolls, int coreCopiesPerRoll)
	{
		if (coreCopiesPerRoll < 1)
			coreCopiesPerRoll = 1;
		if (job == null || rolls == null || rolls.Count == 0)
		{
			MessageBox.Show("Marque la columna Imprimir en los rollos que desea previsualizar.", "Vista previa",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		try
		{
			_previewJob = job;
			_previewRolls = rolls;
			_previewCoreCopiesPerRoll = coreCopiesPerRoll;
			_previewPageIndex = 0;

			using (PrintDocument doc = new PrintDocument())
			{
				doc.DocumentName = "Etiquetas PT 3x2";
				doc.DefaultPageSettings.PaperSize = new PaperSize("Etiqueta3x2", LabelWidthHundredthsInch, LabelHeightHundredthsInch);
				doc.DefaultPageSettings.Landscape = false;
				doc.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);
				doc.PrintPage += Preview_PrintPage;
				using (PrintPreviewDialog preview = new PrintPreviewDialog())
				{
					preview.Document = doc;
					preview.Width = 720;
					preview.Height = 820;
					preview.StartPosition = FormStartPosition.CenterParent;
					preview.Text = string.Format("Vista previa 3\" x 2\" — {0} CORE + 1 CAJA ({1} rollos)",
						(rolls.Count * coreCopiesPerRoll) + 1, rolls.Count);
					preview.ShowDialog(owner);
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("No se pudo abrir la vista previa:\n" + ex.Message, "Vista previa",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			_previewRolls = null;
			_previewJob = null;
			_previewPageIndex = 0;
		}
	}

	private static SerializadoReceiptJobInfo _previewJob;
	private static List<SerializadoReceiptRollRow> _previewRolls;
	private static int _previewCoreCopiesPerRoll = 1;
	private static int _previewPageIndex;

	private static int PreviewPageCount(List<SerializadoReceiptRollRow> rolls, int coreCopiesPerRoll)
	{
		if (rolls == null || rolls.Count == 0)
			return 0;
		if (coreCopiesPerRoll < 1)
			coreCopiesPerRoll = 1;
		return (rolls.Count * coreCopiesPerRoll) + 1;
	}

	private static void Preview_PrintPage(object sender, PrintPageEventArgs e)
	{
		if (_previewRolls == null || _previewJob == null)
		{
			e.HasMorePages = false;
			return;
		}

		int coreCopies = _previewCoreCopiesPerRoll < 1 ? 1 : _previewCoreCopiesPerRoll;
		int totalPages = PreviewPageCount(_previewRolls, coreCopies);
		if (_previewPageIndex >= totalPages)
		{
		 e.HasMorePages = false;
			return;
		}

		Rectangle b = e.MarginBounds;
		int corePages = _previewRolls.Count * coreCopies;
		if (_previewPageIndex < corePages)
		{
			int rollIdx = _previewPageIndex / coreCopies;
			DrawCoreLabelGraphics(e.Graphics, b, _previewJob, _previewRolls[rollIdx]);
		}
		else
			DrawMultiRollBoxLabelGraphics(e.Graphics, b, _previewJob, SortRollsForBoxLabel(_previewRolls));

		_previewPageIndex++;
		e.HasMorePages = _previewPageIndex < totalPages;
	}

	private static void DrawCoreLabelGraphics(Graphics g, Rectangle rect, SerializadoReceiptJobInfo job, SerializadoReceiptRollRow roll)
	{
		using (Pen pen = new Pen(Color.Black, 2f))
		using (Font fPart = new Font("Segoe UI", 14f, FontStyle.Bold))
		using (Font fLbl = new Font("Segoe UI", 9f, FontStyle.Bold))
		using (Font fVal = new Font("Segoe UI", 11f, FontStyle.Bold))
		using (Font fSerLbl = new Font("Segoe UI", 10f, FontStyle.Bold))
		using (Font fSer = new Font("Segoe UI", 15f, FontStyle.Bold))
		using (Font fRoll = new Font("Segoe UI", 13f, FontStyle.Bold))
		{
			int m = Math.Max(4, rect.Width / 80);
			int midX = rect.Left + rect.Width / 2;

			g.DrawRectangle(pen, rect);
			int headH = rect.Height / 11;
			DrawCenteredText(g, fPart, job.PartNum ?? string.Empty,
				new Rectangle(rect.Left + m, rect.Top + 2, rect.Width - (m * 2), headH));

			int boxTop = rect.Top + headH + 4;
			int boxH = rect.Bottom - m - boxTop;
			int boxLeft = rect.Left + m;
			int boxW = rect.Width - (m * 2);
			g.DrawRectangle(pen, boxLeft, boxTop, boxW, boxH);

			int rowH = boxH / 5;
			int y = boxTop;
			DrawCorePreviewRow(g, pen, fLbl, fVal, boxLeft, midX, boxLeft + boxW, y, rowH, "LOTE", GetLotDisplay(job));
			y += rowH;
			DrawCorePreviewRow(g, pen, fLbl, fVal, boxLeft, midX, boxLeft + boxW, y, rowH, "PURCHASE-ORDER", job.PurchaseOrder ?? string.Empty);
			y += rowH;
			DrawCorePreviewRow(g, pen, fLbl, fVal, boxLeft, midX, boxLeft + boxW, y, rowH, "ISSUE QUANTITY", roll.Quantity.ToString(CultureInfo.InvariantCulture));

			y += rowH;
			g.DrawLine(pen, boxLeft, y, boxLeft + boxW, y);
			g.DrawLine(pen, midX, y, midX, y + rowH);

			int halfLeft = midX - boxLeft;
			int halfRight = boxLeft + boxW - midX;
			DrawCenteredText(g, fSerLbl, "START", new Rectangle(boxLeft, y + 4, halfLeft, rowH / 4));
			DrawCenteredText(g, fSerLbl, "END", new Rectangle(midX, y + 4, halfRight, rowH / 4));
			DrawCenteredText(g, fSer, ExtractDigitsOnly(roll.SerialFrom),
				new Rectangle(boxLeft, y + rowH / 4 + 2, halfLeft, rowH - rowH / 4 - 4));
			DrawCenteredText(g, fSer, ExtractDigitsOnly(roll.SerialTo),
				new Rectangle(midX, y + rowH / 4 + 2, halfRight, rowH - rowH / 4 - 4));

			y += rowH;
			g.DrawLine(pen, boxLeft, y, boxLeft + boxW, y);
			DrawCenteredText(g, fRoll, "ROLLO: " + roll.RollNum.ToString(CultureInfo.InvariantCulture),
				new Rectangle(boxLeft, y + 4, boxW, rowH - 8));
		}
	}

	private static void DrawCorePreviewRow(Graphics g, Pen pen, Font fLbl, Font fVal, int left, int midX, int right, int y, int rowH, string label, string value)
	{
		DrawCenteredText(g, fLbl, label, new Rectangle(left + 4, y, midX - left - 8, rowH - 2));
		DrawCenteredText(g, fVal, value, new Rectangle(midX + 4, y, right - midX - 8, rowH - 2));
		g.DrawLine(pen, left + 4, y + rowH - 1, right - 4, y + rowH - 1);
	}

	private static void DrawMultiRollBoxLabelGraphics(Graphics g, Rectangle rect, SerializadoReceiptJobInfo job,
		List<SerializadoReceiptRollRow> rolls)
	{
		if (rolls == null || rolls.Count == 0)
			return;

		PtBoxLabelLayout m = PtBoxLabelLayout.Compute(rolls.Count);
		float sx = rect.Width / (float)ZplWidthDots;
		float sy = rect.Height / (float)ZplHeightDots;

		int totalQty = 0;
		for (int i = 0; i < rolls.Count; i++)
			totalQty += rolls[i].Quantity;

		using (Pen pen = new Pen(Color.Black, Math.Max(1.5f, 2f * sx)))
		using (Font fHdrLbl = new Font("Segoe UI", Math.Max(6f, m.HdrLabelFont * sx * 0.55f), FontStyle.Bold))
		using (Font fHdrVal = new Font("Segoe UI", Math.Max(7f, m.HdrValueFont * sx * 0.55f), FontStyle.Bold))
		using (Font fTblHdr = new Font("Segoe UI", Math.Max(6f, m.TableHdrFont * sx * 0.55f), FontStyle.Bold))
		using (Font fTbl = new Font("Segoe UI", Math.Max(7f, m.TableDataFont * sx * 0.55f), FontStyle.Bold))
		using (Font fRev = new Font("Segoe UI", Math.Max(6f, m.RevFont * sx * 0.5f), FontStyle.Bold))
		using (StringFormat sfVert = new StringFormat { FormatFlags = StringFormatFlags.DirectionVertical })
		{
			int ix = rect.Left + (int)(m.InnerX * sx);
			int iy = rect.Top + (int)(m.InnerY * sy);
			int iw = (int)(m.InnerW * sx);
			int ih = (int)(m.InnerH * sy);
			int revW = (int)((ZplWidthDots - m.RevX) * sx) + 4;
			Rectangle inner = new Rectangle(ix, iy, iw, ih);
			g.DrawRectangle(pen, inner);

			DrawBoxPreviewHeaderRow(g, pen, fHdrLbl, fHdrVal, inner, m, 0, "CLIENTE :", GetClienteDisplay(job), sx, sy);
			DrawBoxPreviewHeaderRow(g, pen, fHdrLbl, fHdrVal, inner, m, 1, "CODIGO  :", job.PartNum ?? string.Empty, sx, sy);
			DrawBoxPreviewHeaderRow(g, pen, fHdrLbl, fHdrVal, inner, m, 2, "QTY     :",
				totalQty.ToString(CultureInfo.InvariantCulture), sx, sy);

			int tableTop = iy + (int)(m.HeaderBlockH * sy);
			g.DrawLine(pen, inner.Left, tableTop, inner.Right, tableTop);

			int tableBottom = inner.Bottom;
			int colStartPx = ix + (int)(m.ColRolloW * sx);
			int colEndPx = ix + (int)((m.ColRolloW + m.ColStartW) * sx);
			g.DrawLine(pen, colStartPx, tableTop, colStartPx, tableBottom);
			g.DrawLine(pen, colEndPx, tableTop, colEndPx, tableBottom);

			int hdrH = (int)(m.TableHeaderH * sy);
			int rowH = (int)(m.DataRowH * sy);
			DrawCenteredText(g, fTblHdr, "ROLLO", new Rectangle(inner.Left, tableTop + 2, colStartPx - inner.Left, hdrH));
			DrawCenteredText(g, fTblHdr, "START", new Rectangle(colStartPx, tableTop + 2, colEndPx - colStartPx, hdrH));
			DrawCenteredText(g, fTblHdr, "END", new Rectangle(colEndPx, tableTop + 2, inner.Right - colEndPx, hdrH));
			g.DrawLine(pen, inner.Left, tableTop + hdrH, inner.Right, tableTop + hdrH);

			for (int i = 0; i < rolls.Count; i++)
			{
				SerializadoReceiptRollRow roll = rolls[i];
				int y = tableTop + hdrH + (i * rowH);
				if (i > 0)
					g.DrawLine(pen, inner.Left, y, inner.Right, y);

				DrawCenteredText(g, fTbl, roll.RollNum.ToString(CultureInfo.InvariantCulture),
					new Rectangle(inner.Left, y + 2, colStartPx - inner.Left, rowH - 4));
				DrawCenteredText(g, fTbl, ExtractDigitsOnly(roll.SerialFrom),
					new Rectangle(colStartPx, y + 2, colEndPx - colStartPx, rowH - 4));
				DrawCenteredText(g, fTbl, ExtractDigitsOnly(roll.SerialTo),
					new Rectangle(colEndPx, y + 2, inner.Right - colEndPx, rowH - 4));
			}

			Rectangle revRect = new Rectangle(rect.Right - revW, rect.Top + (int)(m.InnerY * sy), revW - 2, ih);
			g.DrawString("F-CFX-EMB-02 REV 06", fRev, Brushes.Black, revRect, sfVert);
		}
	}

	private static void DrawBoxPreviewHeaderRow(Graphics g, Pen pen, Font fLbl, Font fVal, Rectangle inner,
		PtBoxLabelLayout m, int rowIndex, string label, string value, float sx, float sy)
	{
		int rowH = (int)(m.HeaderRowH * sy);
		int y = inner.Top + rowIndex * rowH;
		if (rowIndex > 0)
			g.DrawLine(pen, inner.Left, y, inner.Right, y);

		int labelW = (int)(m.LabelColW * sx);
		g.DrawString(label, fLbl, Brushes.Black, inner.Left + 6, y + Math.Max(2, (rowH - fLbl.Height) / 2));
		g.DrawString(value ?? string.Empty, fVal, Brushes.Black, inner.Left + labelW + 4, y + Math.Max(2, (rowH - fVal.Height) / 2));
	}

	private static void DrawBoxLabelGraphics(Graphics g, Rectangle rect, SerializadoReceiptJobInfo job, SerializadoReceiptRollRow roll)
	{
		using (Pen pen = new Pen(Color.Black, 2f))
		using (Font fLbl = new Font("Segoe UI", 10f, FontStyle.Bold))
		using (Font fVal = new Font("Segoe UI", 13f, FontStyle.Bold))
		using (Font fQty = new Font("Segoe UI", 16f, FontStyle.Bold))
		using (Font fRoll = new Font("Segoe UI", 14f, FontStyle.Bold))
		using (Font fRev = new Font("Segoe UI", 11f, FontStyle.Bold))
		using (StringFormat sfVert = new StringFormat { FormatFlags = StringFormatFlags.DirectionVertical })
		{
			int m = Math.Max(4, rect.Width / 80);
			int revW = rect.Width / 12;
			Rectangle inner = new Rectangle(rect.Left + m, rect.Top + m, rect.Width - (m * 2) - revW, rect.Height - (m * 2));
			g.DrawRectangle(pen, inner);

			string cliente = GetClienteDisplay(job);
			int secH = inner.Height / 4;
			DrawBoxSection(g, pen, fLbl, fVal, inner, 0, secH, "CLIENTE :", cliente, false);
			DrawBoxSection(g, pen, fLbl, fVal, inner, secH, secH, "CODIGO  :", job.PartNum ?? string.Empty, false);
			DrawBoxSection(g, pen, fLbl, fQty, inner, secH * 2, secH, "QTY     :", roll.Quantity.ToString(CultureInfo.InvariantCulture), true);
			DrawBoxSection(g, pen, fLbl, fRoll, inner, secH * 3, secH, string.Empty,
				"ROLLO: " + roll.RollNum.ToString(CultureInfo.InvariantCulture), true);

			Rectangle revRect = new Rectangle(rect.Right - revW + 2, rect.Top + m, revW - 4, rect.Height - (m * 2));
			g.DrawString("F-CFX-EMB-02 REV 06", fRev, Brushes.Black, revRect, sfVert);
		}
	}

	private static void DrawBoxSection(Graphics g, Pen pen, Font fLbl, Font fVal, Rectangle inner,
		int offsetY, int secH, string label, string value, bool valueOnlyCentered)
	{
		int y = inner.Top + offsetY;
		if (offsetY > 0)
			g.DrawLine(pen, inner.Left, y, inner.Right, y);

		if (valueOnlyCentered && string.IsNullOrEmpty(label))
		{
			DrawCenteredText(g, fVal, value, new Rectangle(inner.Left + 6, y + 8, inner.Width - 12, secH - 12));
			return;
		}

		if (!string.IsNullOrEmpty(label))
			g.DrawString(label, fLbl, Brushes.Black, inner.Left + 8, y + 6);

		Rectangle valRect = new Rectangle(inner.Left + 8, y + (string.IsNullOrEmpty(label) ? 8 : 22), inner.Width - 16, secH - 28);
		DrawCenteredText(g, fVal, valueOnlyCentered && !string.IsNullOrEmpty(label)
			? label.TrimEnd(':').TrimEnd() + "  " + value
			: value, valRect);
	}

	private static void DrawCenteredText(Graphics g, Font font, string text, Rectangle rect)
	{
		using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
		{
			g.DrawString(text ?? string.Empty, font, Brushes.Black, rect, sf);
		}
	}

	private static string BuildCoreLabelZpl(SerializadoReceiptJobInfo job, SerializadoReceiptRollRow roll)
	{
		string part = ZplText(job.PartNum);
		string lot = ZplText(GetLotDisplay(job));
		string po = ZplText(job.PurchaseOrder);
		string qty = roll.Quantity.ToString(CultureInfo.InvariantCulture);
		string start = ExtractDigitsOnly(roll.SerialFrom);
		string end = ExtractDigitsOnly(roll.SerialTo);
		string rollo = roll.RollNum.ToString(CultureInfo.InvariantCulture);
		int leftW = ZplLeftColWidth();
		int rightW = ZplRightColWidth();

		const int boxY = 42;
		const int boxH = 543;
		const int rowH = 78;
		const int rolloRowY = 432;
		int row0 = boxY + 6;
		int serialY = row0 + (rowH * 3);

		StringBuilder z = new StringBuilder();
		AppendZplStart(z);
		z.Append("^FO0,0^FB").Append(ZplWidthDots).Append(",1,0,C^A0N,45,45^FD").Append(part).Append("^FS");
		z.Append("^FO").Append(ZplMargin).Append(",").Append(boxY).Append("^GB").Append(ZplContentW).Append(",").Append(boxH).Append(",4^FS");
		z.Append("^FO").Append(ZplMidX).Append(",").Append(boxY).Append("^GB3,").Append(rolloRowY - boxY).Append(",3^FS");

		DrawCoreRow(z, row0, "LOTE", lot, 30, 39, rowH);
		DrawCoreRow(z, row0 + rowH, "PURCHASE-ORDER", po, 27, 36, rowH);
		DrawCoreRow(z, row0 + (rowH * 2), "ISSUE QUANTITY", qty, 27, 39, rowH);

		z.Append("^FO").Append(ZplMargin).Append(",").Append(serialY).Append("^GB").Append(ZplContentW).Append(",3,3^FS");
		z.Append("^FO").Append(ZplLeftColX).Append(",").Append(serialY + 15).Append("^FB").Append(leftW)
			.Append(",1,0,C^A0N,30,30^FDSTART^FS");
		z.Append("^FO").Append(ZplRightColX).Append(",").Append(serialY + 15).Append("^FB").Append(rightW)
			.Append(",1,0,C^A0N,30,30^FDEND^FS");
		z.Append("^FO").Append(ZplLeftColX).Append(",").Append(serialY + 54).Append("^FB").Append(leftW)
			.Append(",1,0,C^A0N,60,60^FD").Append(start).Append("^FS");
		z.Append("^FO").Append(ZplRightColX).Append(",").Append(serialY + 54).Append("^FB").Append(rightW)
			.Append(",1,0,C^A0N,60,60^FD").Append(end).Append("^FS");

		z.Append("^FO").Append(ZplMargin).Append(",").Append(rolloRowY).Append("^GB").Append(ZplContentW).Append(",3,3^FS");
		z.Append("^FO").Append(ZplMargin).Append(",").Append(rolloRowY + 24).Append("^FB").Append(ZplContentW)
			.Append(",1,0,C^A0N,45,45^FDROLLO: ").Append(rollo).Append("^FS");
		z.Append("^XZ");
		return z.ToString();
	}

	private static void AppendZplStart(StringBuilder z)
	{
		z.Append("^XA^MUd,").Append(ZplDpi).Append(",").Append(ZplDpi)
			.Append("^PW").Append(ZplWidthDots).Append("^LL").Append(ZplHeightDots).Append("^LH0,0^CI28");
	}

	private static int ZplRowTextY(int rowTop, int rowHeight, int fontDots)
	{
		int y = rowTop + (rowHeight - fontDots) / 2;
		return y < rowTop + 2 ? rowTop + 2 : y;
	}

	private static void DrawCoreRow(StringBuilder z, int rowTop, string label, string value, int labelSize, int valueSize, int rowHeight)
	{
		int leftW = ZplLeftColWidth();
		int rightW = ZplRightColWidth();
		int labelY = ZplRowTextY(rowTop, rowHeight, labelSize);
		int valueY = ZplRowTextY(rowTop, rowHeight, valueSize);
		z.Append("^FO").Append(ZplLeftColX).Append(",").Append(labelY).Append("^FB").Append(leftW).Append(",1,0,C^A0N,")
			.Append(labelSize).Append(",").Append(labelSize).Append("^FD").Append(label).Append("^FS");
		z.Append("^FO").Append(ZplRightColX).Append(",").Append(valueY).Append("^FB").Append(rightW).Append(",1,0,C^A0N,")
			.Append(valueSize).Append(",").Append(valueSize).Append("^FD").Append(value).Append("^FS");
		z.Append("^FO").Append(ZplMargin).Append(",").Append(rowTop + rowHeight).Append("^GB").Append(ZplContentW).Append(",3,3^FS");
	}

	private static List<SerializadoReceiptRollRow> SortRollsForBoxLabel(List<SerializadoReceiptRollRow> rolls)
	{
		if (rolls == null || rolls.Count == 0)
			return new List<SerializadoReceiptRollRow>();
		List<SerializadoReceiptRollRow> sorted = new List<SerializadoReceiptRollRow>(rolls);
		sorted.Sort(delegate(SerializadoReceiptRollRow a, SerializadoReceiptRollRow b)
		{
			return a.RollNum.CompareTo(b.RollNum);
		});
		return sorted;
	}

	private static string BuildBoxLabelZpl(SerializadoReceiptJobInfo job, List<SerializadoReceiptRollRow> rolls)
	{
		if (rolls == null || rolls.Count == 0)
			return string.Empty;

		List<SerializadoReceiptRollRow> sorted = SortRollsForBoxLabel(rolls);
		PtBoxLabelLayout m = PtBoxLabelLayout.Compute(sorted.Count);
		string cliente = ZplText(GetClienteDisplay(job));
		string part = ZplText(job.PartNum);
		int totalQty = 0;
		for (int i = 0; i < sorted.Count; i++)
			totalQty += sorted[i].Quantity;
		string qty = totalQty.ToString(CultureInfo.InvariantCulture);
		int tableH = m.InnerH - m.HeaderBlockH;
		int valueW = m.InnerW - m.LabelColW - 6;

		StringBuilder z = new StringBuilder();
		AppendZplStart(z);

		z.Append("^FO").Append(m.InnerX).Append(",").Append(m.InnerY).Append("^GB")
			.Append(m.InnerW).Append(",").Append(m.InnerH).Append(",3^FS");

		for (int r = 1; r < 3; r++)
		{
			int y = m.InnerY + r * m.HeaderRowH;
			z.Append("^FO").Append(m.InnerX).Append(",").Append(y).Append("^GB")
				.Append(m.InnerW).Append(",2,2^FS");
		}
		z.Append("^FO").Append(m.InnerX).Append(",").Append(m.TableTop).Append("^GB")
			.Append(m.InnerW).Append(",2,2^FS");

		AppendBoxHeaderRowZpl(z, m, 0, "CLIENTE :", cliente, valueW);
		AppendBoxHeaderRowZpl(z, m, 1, "CODIGO  :", part, valueW);
		AppendBoxHeaderRowZpl(z, m, 2, "QTY     :", qty, valueW);

		z.Append("^FO").Append(m.ColStartX).Append(",").Append(m.TableTop).Append("^GB2,")
			.Append(tableH).Append(",2^FS");
		z.Append("^FO").Append(m.ColEndX).Append(",").Append(m.TableTop).Append("^GB2,")
			.Append(tableH).Append(",2^FS");

		int hdrBottom = m.TableTop + m.TableHeaderH;
		z.Append("^FO").Append(m.InnerX).Append(",").Append(hdrBottom).Append("^GB")
			.Append(m.InnerW).Append(",2,2^FS");

		z.Append("^FO").Append(m.InnerX).Append(",").Append(m.TableTop + 4).Append("^FB").Append(m.ColRolloW)
			.Append(",1,0,C^A0N,").Append(m.TableHdrFont).Append(",").Append(m.TableHdrFont).Append("^FDROLLO^FS");
		z.Append("^FO").Append(m.ColStartX).Append(",").Append(m.TableTop + 4).Append("^FB").Append(m.ColStartW)
			.Append(",1,0,C^A0N,").Append(m.TableHdrFont).Append(",").Append(m.TableHdrFont).Append("^FDSTART^FS");
		z.Append("^FO").Append(m.ColEndX).Append(",").Append(m.TableTop + 4).Append("^FB").Append(m.ColEndW)
			.Append(",1,0,C^A0N,").Append(m.TableHdrFont).Append(",").Append(m.TableHdrFont).Append("^FDEND^FS");

		for (int i = 0; i < sorted.Count; i++)
		{
			SerializadoReceiptRollRow roll = sorted[i];
			int rowY = hdrBottom + (i * m.DataRowH);
			if (i > 0)
				z.Append("^FO").Append(m.InnerX).Append(",").Append(rowY).Append("^GB")
					.Append(m.InnerW).Append(",2,2^FS");

			string rollo = roll.RollNum.ToString(CultureInfo.InvariantCulture);
			string start = ExtractDigitsOnly(roll.SerialFrom);
			string end = ExtractDigitsOnly(roll.SerialTo);
			int textY = rowY + Math.Max(2, (m.DataRowH - m.TableDataFont) / 2);

			z.Append("^FO").Append(m.InnerX).Append(",").Append(textY).Append("^FB").Append(m.ColRolloW)
				.Append(",1,0,C^A0N,").Append(m.TableDataFont).Append(",").Append(m.TableDataFont)
				.Append("^FD").Append(rollo).Append("^FS");
			z.Append("^FO").Append(m.ColStartX).Append(",").Append(textY).Append("^FB").Append(m.ColStartW)
				.Append(",1,0,C^A0N,").Append(m.TableDataFont).Append(",").Append(m.TableDataFont)
				.Append("^FD").Append(start).Append("^FS");
			z.Append("^FO").Append(m.ColEndX).Append(",").Append(textY).Append("^FB").Append(m.ColEndW)
				.Append(",1,0,C^A0N,").Append(m.TableDataFont).Append(",").Append(m.TableDataFont)
				.Append("^FD").Append(end).Append("^FS");
		}

		int revY = m.InnerY + (m.InnerH / 2) - 135;
		if (revY < m.InnerY + 12)
			revY = m.InnerY + 12;
		z.Append("^FO").Append(m.RevX).Append(",").Append(revY).Append("^A0B,")
			.Append(m.RevFont).Append(",").Append(m.RevFont).Append("^FDF-CFX-EMB-02 REV 06^FS");
		z.Append("^XZ");
		return z.ToString();
	}

	private static void AppendBoxHeaderRowZpl(StringBuilder z, PtBoxLabelLayout m, int rowIndex,
		string label, string value, int valueW)
	{
		int y = m.InnerY + rowIndex * m.HeaderRowH;
		int labelY = ZplRowTextY(y, m.HeaderRowH, m.HdrLabelFont);
		int valueY = ZplRowTextY(y, m.HeaderRowH, m.HdrValueFont);
		z.Append("^FO").Append(m.InnerX + 6).Append(",").Append(labelY).Append("^A0N,")
			.Append(m.HdrLabelFont).Append(",").Append(m.HdrLabelFont).Append("^FD").Append(label).Append("^FS");
		z.Append("^FO").Append(m.InnerX + m.LabelColW).Append(",").Append(valueY).Append("^FB")
			.Append(valueW).Append(",2,0,L^A0N,").Append(m.HdrValueFont).Append(",").Append(m.HdrValueFont)
			.Append("^FD").Append(value).Append("^FS");
	}

	private static string GetClienteDisplay(SerializadoReceiptJobInfo job)
	{
		if (job == null || string.IsNullOrWhiteSpace(job.Cliente))
			return "—";
		return job.Cliente.Trim().ToUpperInvariant();
	}

	private static string GetLotDisplay(SerializadoReceiptJobInfo job)
	{
		string date = DateTime.Now.ToString("ddMMyyyy");
		if (job != null && !string.IsNullOrWhiteSpace(job.JobNum))
			return job.JobNum.Trim() + date;
		return date;
	}

	private static string ExtractDigitsOnly(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return string.Empty;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
			if (char.IsDigit(input[i]))
				sb.Append(input[i]);
		return sb.Length > 0 ? sb.ToString() : input.Trim();
	}

	private static string ZplText(string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		return value.Replace("\\", "\\\\").Replace("^", "\\^").Replace("~", "\\~");
	}

	public static bool TrySendZpl(string printerName, string zpl, out string error)
	{
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(zpl))
		{
			error = "No hay datos ZPL para imprimir.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(printerName))
		{
			error = "Seleccione una impresora Zebra.";
			return false;
		}

		byte[] payload = Encoding.UTF8.GetBytes(zpl);
		string name = printerName.Trim();

		if (PtZebraPrintTransport.TrySendViaExternalHelper(name, payload, out error))
			return true;

		if (PtZebraPrintTransport.TrySendViaTcp(name, payload, out error))
			return true;

		if (string.IsNullOrWhiteSpace(error))
		{
			error = "No se pudo enviar ZPL a '" + name + "'.\n"
				+ "Verifique CfxPtZplSend.exe, zebra_tcp.map o que la impresora Zebra este encendida.";
		}
		return false;
	}
}

public static class PtZebraPrintConfig
{
	public const string Version = "4.6.4";
	public const int TcpPortZebra = 9100;

	private static readonly string[] MapPaths = new string[]
	{
		@"C:\SERIALIZADOR\tools\zebra_tcp.map",
		@"C:\Program Files\CodiFlex\SerializadorBRK\zebra_tcp.map"
	};

	private static readonly KeyValuePair<string, string>[] BuiltInDefaultEntries = new KeyValuePair<string, string>[]
	{
		new KeyValuePair<string, string>("ZEBRAEPICOR", "192.168.60.122"),
		new KeyValuePair<string, string>("ZTC 110Xi4-300dpi", "192.168.60.122"),
		new KeyValuePair<string, string>("110Xi4", "192.168.60.122"),
		new KeyValuePair<string, string>("ZTC 110XiIIIPlus-200dpi", "192.168.60.247"),
		new KeyValuePair<string, string>("110XiIIIPlus", "192.168.60.247"),
		new KeyValuePair<string, string>("110XiIII", "192.168.60.247")
	};

	private static bool TryGetBuiltInExact(string name, out string ip)
	{
		ip = string.Empty;
		for (int i = 0; i < BuiltInDefaultEntries.Length; i++)
		{
			if (string.Equals(name, BuiltInDefaultEntries[i].Key, StringComparison.OrdinalIgnoreCase))
			{
				ip = BuiltInDefaultEntries[i].Value;
				return true;
			}
		}
		return false;
	}

	private static bool TryMatchBuiltInPartial(string name, out string ip)
	{
		ip = string.Empty;
		int bestLen = 0;
		string bestIp = null;
		for (int i = 0; i < BuiltInDefaultEntries.Length; i++)
		{
			string key = BuiltInDefaultEntries[i].Key;
			if (key.Length <= bestLen)
				continue;
			if (name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				bestLen = key.Length;
				bestIp = BuiltInDefaultEntries[i].Value;
			}
		}
		if (bestIp == null)
			return false;
		ip = bestIp;
		return true;
	}

	private static Dictionary<string, string> _mapCache;
	private static DateTime _mapCacheStamp = DateTime.MinValue;
	private static bool _mapLoadSkipped;
	private static readonly HashSet<string> SlowFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private const int FileProbeTimeoutMs = 2000;
	private static readonly Regex IpRegex = new Regex(@"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d?\d)\b");

	private static bool TryProbeFileExists(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || SlowFilePaths.Contains(path))
			return false;

		bool exists = false;
		Thread worker = new Thread(delegate()
		{
			try { exists = File.Exists(path); }
			catch { exists = false; }
		});
		worker.IsBackground = true;
		worker.Start();
		if (!worker.Join(FileProbeTimeoutMs))
		{
			SlowFilePaths.Add(path);
			return false;
		}
		return exists;
	}

	public static bool TryProbeHelperExe(string path)
	{
		return TryProbeFileExists(path);
	}

	private static bool TryProbeLastWriteUtc(string path, out DateTime stampUtc)
	{
		stampUtc = DateTime.MinValue;
		if (string.IsNullOrWhiteSpace(path) || SlowFilePaths.Contains(path))
			return false;

		bool ok = false;
		DateTime localStamp = DateTime.MinValue;
		Thread worker = new Thread(delegate()
		{
			try
			{
				localStamp = File.GetLastWriteTimeUtc(path);
				ok = true;
			}
			catch { ok = false; }
		});
		worker.IsBackground = true;
		worker.Start();
		if (!worker.Join(FileProbeTimeoutMs))
		{
			SlowFilePaths.Add(path);
			return false;
		}
		if (!ok)
			return false;
		stampUtc = localStamp;
		return true;
	}

	public static bool TryResolveTcpIp(string printerName, out string ip)
	{
		ip = string.Empty;
		if (string.IsNullOrWhiteSpace(printerName))
			return false;

		string name = printerName.Trim();
		Match m = IpRegex.Match(name);
		if (m.Success)
		{
			ip = m.Value;
			return true;
		}

		string exact;
		if (TryGetBuiltInExact(name, out exact))
		{
			ip = exact;
			return true;
		}
		if (TryMatchBuiltInPartial(name, out ip))
			return true;

		LoadMap();
		if (_mapCache != null && _mapCache.TryGetValue(name, out exact))
		{
			ip = exact;
			return true;
		}
		if (_mapCache != null)
		{
			foreach (KeyValuePair<string, string> kv in _mapCache)
			{
				if (name.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0
					|| kv.Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					ip = kv.Value;
					return true;
				}
			}
		}
		return false;
	}

	private static void LoadMap()
	{
		if (_mapLoadSkipped)
		{
			if (_mapCache == null)
				_mapCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			return;
		}

		string mapPath = null;
		DateTime stamp = DateTime.MinValue;
		for (int i = 0; i < MapPaths.Length; i++)
		{
			if (!TryProbeFileExists(MapPaths[i]))
				continue;
			DateTime t;
			if (!TryProbeLastWriteUtc(MapPaths[i], out t))
				continue;
			if (mapPath == null || t > stamp)
			{
				mapPath = MapPaths[i];
				stamp = t;
			}
		}
		if (mapPath == null)
		{
			_mapCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_mapCacheStamp = DateTime.MinValue;
			if (SlowFilePaths.Count >= MapPaths.Length)
				_mapLoadSkipped = true;
			return;
		}
		if (_mapCache != null && stamp == _mapCacheStamp)
			return;

		Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string[] lines = File.ReadAllLines(mapPath);
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i].Trim();
				if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
					continue;
				int eq = line.IndexOf('=');
				if (eq <= 0)
					continue;
				string key = line.Substring(0, eq).Trim();
				string val = line.Substring(eq + 1).Trim();
				int colon = val.IndexOf(':');
				if (colon > 0)
					val = val.Substring(0, colon).Trim();
				if (key.Length > 0 && val.Length > 0)
					map[key] = val;
			}
		}
		catch { }

		_mapCache = map;
		_mapCacheStamp = stamp;
	}
}

public static class PtZebraPrintTransport
{
	private static readonly string[] HelperPaths = new string[]
	{
		@"C:\SERIALIZADOR\tools\CfxPtZplSend.exe",
		@"C:\Program Files\CodiFlex\SerializadorBRK\CfxPtZplSend.exe",
		@"C:\Program Files\CodiFlex\CfxPtZplSend\CfxPtZplSend.exe"
	};

	public static bool IsZebraPrinterName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return false;
		return name.IndexOf("ZEBRA", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZTC", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZT4", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZT2", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZT6", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZDESIGNER", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZPL", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("110XI", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("XI111", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("XI4", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("XI3", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("105SL", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZD4", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZD6", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("ZM4", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static bool ValidatePrintSetup(string printerName, out string error)
	{
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(printerName))
		{
			error = "Seleccione una impresora Zebra instalada en Windows.";
			return false;
		}
		if (!IsZebraPrinterName(printerName))
		{
			error = "La impresora '" + printerName.Trim() + "' no parece Zebra.\n"
				+ "Use solo impresoras Zebra ZPL (ZTC, 110XiIII, ZEBRAEPICOR, etc.).";
			return false;
		}

		string ip;
		if (TryResolveTcpIp(printerName, out ip))
			return true;
		if (!string.IsNullOrWhiteSpace(FindHelperExe()))
			return true;

		error = "No se puede imprimir de forma segura desde Epicor.\n\n"
			+ "ZEBRAEPICOR ya tiene IP integrada (192.168.60.122) — no requiere C:\\SERIALIZADOR.\n\n"
			+ "Opcional: CfxPtZplSend.exe en C:\\SERIALIZADOR\\tools\\\n"
			+ "Opcional: zebra_tcp.map con NombreImpresora=IP\n\n"
			+ "Epicor mostrara este aviso en lugar de cerrarse.";
		return false;
	}

	private static bool TryResolveTcpIp(string printerName, out string ip)
	{
		return PtZebraPrintConfig.TryResolveTcpIp(printerName, out ip);
	}

	public static bool TrySendViaExternalHelper(string printerName, byte[] payload, out string error)
	{
		error = string.Empty;
		string helper = FindHelperExe();
		if (string.IsNullOrWhiteSpace(helper))
			return false;

		string zplFile = Path.Combine(Path.GetTempPath(), "cfxzpl_" + Guid.NewGuid().ToString("N") + ".zpl");
		try
		{
			File.WriteAllBytes(zplFile, payload);
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = helper,
				Arguments = "\"" + printerName + "\" \"" + zplFile + "\"",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using (Process proc = Process.Start(psi))
			{
				if (proc == null)
				{
					error = "No se pudo iniciar CfxPtZplSend.exe";
					return false;
				}
				if (!proc.WaitForExit(25000))
				{
					try { proc.Kill(); } catch { }
					error = "Tiempo de espera agotado en CfxPtZplSend.exe";
					return false;
				}
				if (proc.ExitCode == 0)
					return true;
				error = proc.StandardError.ReadToEnd();
				if (string.IsNullOrWhiteSpace(error))
					error = proc.StandardOutput.ReadToEnd();
				if (string.IsNullOrWhiteSpace(error))
					error = "CfxPtZplSend salio con codigo " + proc.ExitCode.ToString(CultureInfo.InvariantCulture);
				return false;
			}
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
		finally
		{
			try { if (File.Exists(zplFile)) File.Delete(zplFile); } catch { }
		}
	}

	private static string FindHelperExe()
	{
		for (int i = 0; i < HelperPaths.Length; i++)
		{
			if (PtZebraPrintConfig.TryProbeHelperExe(HelperPaths[i]))
				return HelperPaths[i];
		}
		return null;
	}

	public static bool TrySendViaTcp(string printerName, byte[] payload, out string error)
	{
		error = string.Empty;
		string ip;
		if (!TryResolveTcpIp(printerName, out ip))
		{
			error = "No hay IP TCP configurada para '" + printerName + "'.";
			return false;
		}
		return TrySendTcp(ip, PtZebraPrintConfig.TcpPortZebra, payload, out error);
	}

	public static bool TrySendTcp(string ip, int port, byte[] payload, out string error)
	{
		error = string.Empty;
		TcpClient client = null;
		try
		{
			client = new TcpClient();
			IAsyncResult ar = client.BeginConnect(ip, port, null, null);
			if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(6), false))
			{
				error = "Timeout TCP " + ip + ":" + port.ToString(CultureInfo.InvariantCulture);
				return false;
			}
			client.EndConnect(ar);
			client.SendTimeout = 12000;
			using (NetworkStream stream = client.GetStream())
			{
				stream.WriteTimeout = 12000;
				stream.Write(payload, 0, payload.Length);
				stream.Flush();
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "TCP " + ip + " — " + ex.Message;
			return false;
		}
		finally
		{
			if (client != null)
			{
				try { client.Close(); } catch { }
			}
		}
	}
}

// =============================================================================
// MODAL — Confirmacion de Serializado Recibo PT (segunda imagen / mockup)
// =============================================================================

public sealed class SerializadoReceiptForm : Form
{
	private readonly object _oTrans;
	private readonly string _jobNum;
	private readonly string _partNum;
	private readonly decimal _tranQty;

	private readonly SerializadoReceiptJobInfo _jobInfo;
	private List<SerializadoReceiptRollRow> _rolls = new List<SerializadoReceiptRollRow>();
	private bool _rollsInitialized;

	private readonly DataGridView _grid;
	private readonly TextBox _txtStart;
	private readonly TextBox _txtEnd;
	private readonly Label _lblResumen;
	private Label _lblProgress;
	private Label _lblSub;
	private readonly Button _btnListo;
	private readonly Button _btnImprimir;

	public SerializadoReceiptForm(object oTrans, RollBundle bundle, decimal tranQty)
	{
		_oTrans = oTrans;
		_jobInfo = SerializadoReceiptService.CloneJobInfo(bundle != null ? bundle.JobInfo : null);
		_rolls = SerializadoReceiptService.CloneRollList(bundle != null ? bundle.Rolls : null);
		_jobNum = _jobInfo.JobNum ?? string.Empty;
		_partNum = _jobInfo.PartNum ?? string.Empty;
		_tranQty = tranQty;
		_rollsInitialized = true;

		Text = "Confirmacion de Serializado — Recibo PT";
		Size = new Size(1040, 880);
		StartPosition = FormStartPosition.CenterParent;
		MinimumSize = new Size(980, 700);
		BackColor = Color.FromArgb(245, 247, 250);
		Font = new Font("Segoe UI", 9f);
		FormBorderStyle = FormBorderStyle.Sizable;
		EnableGridDoubleBuffer(this);

		Panel header = BuildHeader();
		Panel actionBar = BuildActionBar(out _txtStart, out _txtEnd, out _btnListo, out _btnImprimir, out _lblResumen);
		Panel body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 2, 10, 2) };

		_grid = BuildGrid();
		body.Controls.Add(_grid);

		Controls.Add(body);
		Controls.Add(actionBar);
		Controls.Add(header);

		BindRollsToGrid();
		Shown += SerializadoReceiptForm_Shown;
	}

	private void SerializadoReceiptForm_Shown(object sender, EventArgs e)
	{
		Shown -= SerializadoReceiptForm_Shown;
		try
		{
			SerializadoReceiptService.EnrichJobInfoIfNeeded(_oTrans, _jobInfo, _partNum);
			if (_lblSub != null)
				_lblSub.Text = FormatHeaderSubline();
		}
		catch { }
	}

	private string FormatHeaderSubline()
	{
		return string.Format("Job: {0}   |   Parte: {1}   |   PO: {2}   |   Cliente: {3}",
			_jobNum, _jobInfo.PartNum, _jobInfo.PurchaseOrder, _jobInfo.Cliente);
	}

	private Panel BuildHeader()
	{
		Panel p = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.FromArgb(0, 86, 179), Padding = new Padding(12, 4, 12, 4) };
		Label title = new Label
		{
			Text = "Confirmacion de Serializado — Recibo PT",
			ForeColor = Color.White,
			Font = new Font("Segoe UI Semibold", 13f),
			Location = new Point(10, 4),
			AutoSize = true
		};
		_lblSub = new Label
		{
			Text = FormatHeaderSubline(),
			ForeColor = Color.FromArgb(210, 230, 255),
			Font = new Font("Segoe UI", 8.5f),
			Location = new Point(12, 28),
			AutoSize = true
		};
		_lblProgress = new Label
		{
			Text = "Progreso: 0/0 rollos confirmados   |   IMPRIMIR ETIQ.: seleccione rollos y Zebra en el dialogo",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
			Location = new Point(12, 50),
			Size = new Size(980, 22)
		};
		p.Controls.Add(title);
		p.Controls.Add(_lblSub);
		p.Controls.Add(_lblProgress);
		return p;
	}

	private Panel BuildActionBar(out TextBox txtStart, out TextBox txtEnd, out Button btnListo, out Button btnImprimir, out Label lblStatus)
	{
		TextBox startBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle };
		TextBox endBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle };
		Label statusLabel = new Label
		{
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Color.DimGray,
			Text = "Listo.",
			Font = new Font("Segoe UI", 8.25f),
			Padding = new Padding(6, 0, 0, 0)
		};

		Panel host = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 50,
			BackColor = Color.FromArgb(240, 242, 245),
			Padding = new Padding(8, 6, 8, 6)
		};

		TableLayoutPanel t = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 12,
			RowCount = 1
		};
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
		t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		t.Controls.Add(MakeInlineLabel("START"), 0, 0);
		t.Controls.Add(startBox, 1, 0);
		t.Controls.Add(MakeInlineLabel("END"), 2, 0);
		t.Controls.Add(endBox, 3, 0);

		Button btnConfirm = MakeButton("Confirmar", Color.FromArgb(0, 86, 179), 108);
		btnConfirm.Dock = DockStyle.Fill;
		btnConfirm.Click += BtnConfirmRoll_Click;
		t.Controls.Add(btnConfirm, 4, 0);

		Button btnScan = MakeButton("Escanear", Color.FromArgb(108, 117, 125), 74);
		btnScan.Dock = DockStyle.Fill;
		btnScan.Click += delegate
		{
			startBox.Focus();
			statusLabel.Text = "Escanee START y END.";
		};
		t.Controls.Add(btnScan, 5, 0);

		Button btnClear = MakeButton("Limpiar", Color.FromArgb(108, 117, 125), 58);
		btnClear.Dock = DockStyle.Fill;
		btnClear.Click += delegate
		{
			startBox.Clear();
			endBox.Clear();
			startBox.Focus();
		};
		t.Controls.Add(btnClear, 6, 0);

		t.Controls.Add(statusLabel, 7, 0);

		Button btnPreview = MakeButton("Vista previa", Color.FromArgb(92, 107, 192), 104);
		btnPreview.Dock = DockStyle.Fill;
		btnPreview.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
		btnPreview.Click += BtnVistaPrevia_Click;
		t.Controls.Add(btnPreview, 8, 0);

		btnImprimir = MakeButton("IMPRIMIR ETIQ.", Color.FromArgb(253, 126, 20), 124);
		btnImprimir.Dock = DockStyle.Fill;
		btnImprimir.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
		btnImprimir.Click += BtnImprimir_Click;
		t.Controls.Add(btnImprimir, 9, 0);

		btnListo = MakeButton("Listo recibir", Color.FromArgb(0, 86, 179), 106);
		btnListo.Dock = DockStyle.Fill;
		btnListo.Click += BtnListo_Click;
		t.Controls.Add(btnListo, 10, 0);

		Button btnClose = MakeButton("Cerrar", Color.FromArgb(108, 117, 125), 64);
		btnClose.Dock = DockStyle.Fill;
		btnClose.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
		t.Controls.Add(btnClose, 11, 0);

		host.Controls.Add(t);

		txtStart = startBox;
		txtEnd = endBox;
		lblStatus = statusLabel;
		return host;
	}

	private DataGridView BuildGrid()
	{
		DataGridView grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			ReadOnly = false,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			RowHeadersVisible = false,
			BackgroundColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			ColumnHeadersHeight = 28,
			RowTemplate = { Height = 22 },
			ScrollBars = ScrollBars.Both,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
		};
		EnableGridDoubleBuffer(grid);

		DataGridViewCheckBoxColumn chkCol = new DataGridViewCheckBoxColumn
		{
			Name = "Sel",
			HeaderText = "Imprimir",
			Width = 58,
			ReadOnly = false
		};
		grid.Columns.Add(chkCol);
		grid.Columns.Add("RollNum", "Rollo");
		grid.Columns["RollNum"].Width = 52;
		grid.Columns["RollNum"].ReadOnly = true;
		grid.Columns.Add("Prefix", "Prefijo");
		grid.Columns["Prefix"].Width = 58;
		grid.Columns["Prefix"].ReadOnly = true;
		grid.Columns.Add("SerialFrom", "START");
		grid.Columns["SerialFrom"].Width = 110;
		grid.Columns["SerialFrom"].ReadOnly = true;
		grid.Columns.Add("SerialTo", "END");
		grid.Columns["SerialTo"].Width = 110;
		grid.Columns["SerialTo"].ReadOnly = true;
		grid.Columns.Add("Quantity", "Cantidad");
		grid.Columns["Quantity"].Width = 72;
		grid.Columns["Quantity"].ReadOnly = true;
		grid.Columns.Add("Status", "Estado");
		grid.Columns["Status"].Width = 78;
		grid.Columns["Status"].ReadOnly = true;
		grid.Columns.Add("ConfirmedBy", "Confirmado por");
		grid.Columns["ConfirmedBy"].ReadOnly = true;
		grid.Columns["ConfirmedBy"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
		DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn
		{
			Name = "Accion",
			HeaderText = "Accion",
			Text = "Confirmar",
			UseColumnTextForButtonValue = true,
			Width = 88,
			ReadOnly = true
		};
		grid.Columns.Add(btnCol);
		grid.CellContentClick += Grid_CellContentClick;
		grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
		grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
		return grid;
	}

	private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Sel")
			return;

		bool markPending = false;
		for (int i = 0; i < _grid.Rows.Count && i < _rolls.Count; i++)
		{
			if (string.Equals(_rolls[i].Status, "Recibido", StringComparison.OrdinalIgnoreCase))
				continue;
			object chk = _grid.Rows[i].Cells["Sel"].Value;
			if (chk == null || chk == DBNull.Value || !Convert.ToBoolean(chk))
			{
				markPending = true;
				break;
			}
		}

		for (int i = 0; i < _grid.Rows.Count && i < _rolls.Count; i++)
		{
			if (string.Equals(_rolls[i].Status, "Recibido", StringComparison.OrdinalIgnoreCase))
			{
				_grid.Rows[i].Cells["Sel"].Value = false;
				continue;
			}
			_grid.Rows[i].Cells["Sel"].Value = markPending;
		}

		_lblResumen.Text = markPending
			? "Rollos pendientes marcados para vista previa / impresion."
			: "Seleccion de impresion limpiada.";
	}

	private static void EnableGridDoubleBuffer(Control control)
	{
		try
		{
			System.Reflection.PropertyInfo pi = typeof(Control).GetProperty("DoubleBuffered",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (pi != null)
				pi.SetValue(control, true, null);
		}
		catch { }
	}

	private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
	{
		if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
			_grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
	}

	private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
			return;
		if (_grid.Columns[e.ColumnIndex].Name != "Accion")
			return;

		SerializadoReceiptRollRow roll = _rolls[e.RowIndex];
		if (string.Equals(roll.Status, "Recibido", StringComparison.OrdinalIgnoreCase))
		{
			MessageBox.Show("El rollo #" + roll.RollNum + " ya fue confirmado.", "Recibo Serializado",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		_txtStart.Text = roll.SerialFrom;
		_txtEnd.Text = roll.SerialTo;
		_lblResumen.Text = string.Format(
			"Rollo #{0}: verifique START/END contra la etiqueta fisica y presione Confirmar este rollo.",
			roll.RollNum);
		_txtStart.Focus();
		_txtStart.SelectAll();
	}

	private static Label MakeInlineLabel(string text)
	{
		return new Label
		{
			Text = text,
			AutoSize = true,
			Margin = new Padding(4, 6, 2, 0),
			Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
		};
	}

	private void BtnConfirmRoll_Click(object sender, EventArgs e)
	{
		Cursor prev = Cursor;
		Cursor = Cursors.WaitCursor;
		try
		{
			string startCap = _txtStart.Text == null ? string.Empty : _txtStart.Text.Trim();
			string endCap = _txtEnd.Text == null ? string.Empty : _txtEnd.Text.Trim();
			string msg;
			if (!SerializadoReceiptService.TryConfirmRoll(_oTrans, _jobNum, startCap, endCap, _rolls, out msg))
			{
				MessageBox.Show(msg, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				_lblResumen.Text = msg;
				return;
			}

			_lblResumen.Text = msg;
			_txtStart.Clear();
			_txtEnd.Clear();

			int confirmedIdx = -1;
			for (int i = 0; i < _rolls.Count; i++)
			{
				SerializadoReceiptRollRow r = _rolls[i];
				if (!string.Equals(r.Status, "Recibido", StringComparison.OrdinalIgnoreCase))
					continue;
				if (string.Equals(r.SerialFrom, startCap, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(r.SerialTo, endCap, StringComparison.OrdinalIgnoreCase))
				{
					confirmedIdx = i;
					break;
				}
			}

			if (confirmedIdx >= 0)
				UpdateRollRowUi(confirmedIdx);
			else
				UpdateProgressOnly();

			_txtStart.Focus();
		}
		finally
		{
			Cursor = prev;
		}
	}

	private void UpdateRollRowUi(int rowIndex)
	{
		if (rowIndex < 0 || rowIndex >= _rolls.Count || rowIndex >= _grid.Rows.Count)
		{
			UpdateProgressOnly();
			return;
		}

		SerializadoReceiptRollRow r = _rolls[rowIndex];
		DataGridViewRow row = _grid.Rows[rowIndex];
		row.Cells["Status"].Value = r.Status;
		row.Cells["ConfirmedBy"].Value = r.ConfirmedBy;
		row.Cells["Accion"].Value = "OK";
		row.Cells["Sel"].Value = true;
		ApplyStatusRowStyle(row, true);
		UpdateProgressOnly();
		_lblResumen.Text = string.Format(
			"Rollo #{0} confirmado — marcado para IMPRIMIR ETIQ. Presione IMPRIMIR ETIQ. para etiquetar esta caja.",
			r.RollNum);

		try
		{
			int next = rowIndex + 1;
			if (next < _grid.Rows.Count)
			{
				_grid.FirstDisplayedScrollingRowIndex = Math.Max(0, next - 3);
				_grid.CurrentCell = _grid.Rows[next].Cells["SerialFrom"];
			}
		}
		catch { }
	}

	private void UpdateProgressOnly()
	{
		int confirmed = SerializadoReceiptService.CountConfirmed(_rolls);
		_lblProgress.Text = string.Format("Progreso: {0}/{1} rollos confirmados   |   Esta transaccion: {2:N0} pzas",
			confirmed, _rolls.Count, _tranQty);
		string err;
		_btnListo.Enabled = SerializadoReceiptService.ValidateReceiptReady(_oTrans, _jobNum, _tranQty, _rolls, out err);
	}

	private static void ApplyStatusRowStyle(DataGridViewRow row, bool received)
	{
		if (received)
		{
			row.Cells["Status"].Style.ForeColor = Color.FromArgb(46, 125, 50);
			row.Cells["Status"].Style.Font = new Font(row.DataGridView.Font, FontStyle.Bold);
		}
		else
		{
			row.Cells["Status"].Style.ForeColor = Color.FromArgb(230, 126, 34);
			row.Cells["Status"].Style.Font = new Font(row.DataGridView.Font, FontStyle.Bold);
		}
	}

	private void BtnVistaPrevia_Click(object sender, EventArgs e)
	{
		try
		{
			SerializadoReceiptService.EnrichJobInfoIfNeeded(_oTrans, _jobInfo, _partNum);
			List<SerializadoReceiptRollRow> confirmed = SerializadoReceiptService.GetRollsForPrintDialog(_oTrans, _jobNum, _rolls);
			if (confirmed.Count == 0)
			{
				MessageBox.Show("No hay rollos confirmados (Recibido) para imprimir.\nConfirme rollos en la tabla antes de imprimir.",
					"Imprimir", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			using (PtSerialPrintDialog dlg = new PtSerialPrintDialog(_jobInfo, confirmed, GetPreselectedRollNumsFromGrid()))
				dlg.ShowDialog(this);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error al abrir impresion:\n" + ex.Message, "Vista previa", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void BtnImprimir_Click(object sender, EventArgs e)
	{
		try
		{
			SerializadoReceiptService.EnrichJobInfoIfNeeded(_oTrans, _jobInfo, _partNum);
			List<SerializadoReceiptRollRow> confirmed = SerializadoReceiptService.GetRollsForPrintDialog(_oTrans, _jobNum, _rolls);
			if (confirmed.Count == 0)
			{
				MessageBox.Show("No hay rollos confirmados (Recibido) para imprimir.\nConfirme rollos en la tabla antes de imprimir.",
					"Imprimir", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string printerName;
			List<SerializadoReceiptRollRow> selected;
			int coreCopies = 1;
			using (PtSerialPrintDialog dlg = new PtSerialPrintDialog(_jobInfo, confirmed, GetPreselectedRollNumsFromGrid()))
			{
				if (dlg.ShowDialog(this) != DialogResult.OK)
					return;
				printerName = dlg.SelectedPrinter;
				selected = dlg.SelectedRolls;
				coreCopies = dlg.CoreCopiesPerRoll;
			}

			if (selected == null || selected.Count == 0)
				return;

			_btnImprimir.Enabled = false;
			Cursor = Cursors.WaitCursor;

			bool printOk;
			string printError;
			int labelsSent = 0;
			int rollsSent = 0;
			int coreLabels = 0;
			bool boxLabelSent = false;
			using (PtPrintProgressForm progress = new PtPrintProgressForm(printerName, _jobInfo, selected, coreCopies))
			{
				progress.ShowDialog(this);
				printOk = progress.PrintOk;
				printError = progress.PrintError;
				labelsSent = progress.LabelsSent;
				rollsSent = progress.RollsSent;
				coreLabels = progress.CoreLabelsSent;
				boxLabelSent = progress.BoxLabelSent;
			}

			PtSerialLabelPrinter.ShowPrintResultDialog(this, labelsSent, rollsSent, printerName, printOk, printError,
				coreLabels, boxLabelSent, coreCopies);

			if (!printOk)
			{
				_lblResumen.Text = labelsSent > 0
					? string.Format("Impresion parcial: {0} etiqueta(s) a {1}.", labelsSent, printerName)
					: "Error al imprimir. Use solo impresora Zebra ZPL.";
				return;
			}

			_lblResumen.Text = string.Format("{0} CORE + {1} CAJA = {2} etiqueta(s) ({3} rollos) a {4}.",
				coreLabels, boxLabelSent ? 1 : 0, labelsSent, rollsSent, printerName);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error al imprimir:\n" + ex.Message, "Imprimir", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_lblResumen.Text = "Error al imprimir.";
		}
		finally
		{
			_btnImprimir.Enabled = true;
			Cursor = Cursors.Default;
		}
	}

	private List<int> GetPreselectedRollNumsFromGrid()
	{
		List<int> list = new List<int>();
		for (int i = 0; i < _grid.Rows.Count && i < _rolls.Count; i++)
		{
			if (!SerializadoReceiptService.IsRollConfirmed(_rolls[i]))
				continue;
			object chk = _grid.Rows[i].Cells["Sel"].Value;
			if (chk != null && chk != DBNull.Value && Convert.ToBoolean(chk))
				list.Add(_rolls[i].RollNum);
		}
		return list;
	}

	private List<SerializadoReceiptRollRow> GetSelectedRollsForPrint()
	{
		List<SerializadoReceiptRollRow> list = new List<SerializadoReceiptRollRow>();
		for (int i = 0; i < _grid.Rows.Count && i < _rolls.Count; i++)
		{
			object chk = _grid.Rows[i].Cells["Sel"].Value;
			bool selected = chk != null && chk != DBNull.Value && Convert.ToBoolean(chk);
			if (selected)
				list.Add(_rolls[i]);
		}
		return list;
	}

	private void BtnListo_Click(object sender, EventArgs e)
	{
		string error;
		if (!SerializadoReceiptService.ValidateReceiptReady(_oTrans, _jobNum, _tranQty, _rolls, out error))
		{
			MessageBox.Show(error, "Pendiente de confirmacion", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		DialogResult = DialogResult.OK;
		Close();
	}

	private void ReloadRolls(bool fullReload)
	{
		if (fullReload || !_rollsInitialized)
		{
			RollBundle bundle = SerializadoReceiptService.GetOrBuildBundle(_oTrans, _jobNum, _partNum);
			_rolls = SerializadoReceiptService.CloneRollList(bundle.Rolls);
			_rollsInitialized = true;
		}
		else
			SerializadoReceiptService.RefreshRollStatuses(_oTrans, _jobNum, _rolls);

		BindRollsToGrid();
	}

	private void BindRollsToGrid()
	{
		Dictionary<int, bool> priorSelection = new Dictionary<int, bool>();
		for (int i = 0; i < _grid.Rows.Count && i < _rolls.Count; i++)
		{
			object chk = _grid.Rows[i].Cells["Sel"].Value;
			if (chk != null && chk != DBNull.Value)
				priorSelection[_rolls[i].RollNum] = Convert.ToBoolean(chk);
		}

		_grid.SuspendLayout();
		try
		{
			_grid.Rows.Clear();
			int scrollTarget = -1;
			for (int i = 0; i < _rolls.Count; i++)
			{
				SerializadoReceiptRollRow r = _rolls[i];
				bool isReceived = SerializadoReceiptService.IsRollConfirmed(r);
				bool sel = priorSelection.ContainsKey(r.RollNum) ? priorSelection[r.RollNum] : isReceived;
				int rowIdx = _grid.Rows.Add(sel, r.RollNum, r.Prefix, r.SerialFrom, r.SerialTo,
					r.Quantity.ToString("N0"), r.Status, r.ConfirmedBy, isReceived ? "OK" : "Confirmar");

				DataGridViewRow row = _grid.Rows[rowIdx];
				ApplyStatusRowStyle(row, isReceived);
				if (!isReceived && scrollTarget < 0)
					scrollTarget = rowIdx;
			}

			if (scrollTarget >= 0 && scrollTarget < _grid.Rows.Count)
			{
				try
				{
					_grid.FirstDisplayedScrollingRowIndex = Math.Max(0, scrollTarget - 2);
					_grid.CurrentCell = _grid.Rows[scrollTarget].Cells["SerialFrom"];
				}
				catch { }
			}
			else if (_grid.Rows.Count > 0)
			{
				try
				{
					_grid.FirstDisplayedScrollingRowIndex = Math.Max(0, _grid.Rows.Count - 8);
				}
				catch { }
			}
		}
		finally
		{
			_grid.ResumeLayout(true);
		}

		int confirmed = SerializadoReceiptService.CountConfirmed(_rolls);
		_lblProgress.Text = string.Format("Progreso: {0}/{1} rollos confirmados   |   Esta transaccion: {2:N0} pzas",
			confirmed, _rolls.Count, _tranQty);

		string err;
		_btnListo.Enabled = SerializadoReceiptService.ValidateReceiptReady(_oTrans, _jobNum, _tranQty, _rolls, out err);
	}

	private static Button MakeButton(string text, Color back, int width)
	{
		Button btn = new Button
		{
			Text = text,
			Width = width,
			Height = 32,
			BackColor = back,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Margin = new Padding(6, 0, 0, 0)
		};
		btn.FlatAppearance.BorderSize = 0;
		return btn;
	}
}
