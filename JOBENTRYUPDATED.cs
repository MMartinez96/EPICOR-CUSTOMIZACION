// **************************************************
// Custom code for JobEntryForm — incluye MOQ (CFXJZ) + SERIALIZADO
// MOQ epguid: 96cf1f82-e55d-42ee-b41a-caf25e263ac1 (MOQTEXT)
// Botón Serializado epguid: 7e3c0614-510b-4a31-983b-eb5c2ea2ef96
// Botón Otros epguid: 2f2a467a-e961-4c65-b4a8-25951d78fb02
// BAQ reutilizados: BAQPO, CODIFLEX-UD10, FT_BAQ, BAQCONSULTAVALIDACIONMUESTRA
// v2: carga diferida JobHead + caché BAQ por sesión (abre Job Entry más rápido)
// v2.1: orden_compra_c se llena al cambiar OrdenDeVenta_c + refresh UI (epguid b7fbb965...)
// v2.2: FT corrida (epguid 637e5ce4...) — recarga en InitLastView/GetByID/JobNum y evita BAQ sin JobNum
// v2.3: FT corrida — siempre repinta UI tras rebind Epicor, reintentos con delay y no cachea FT vacío
// v2.4: Ocultar Fecha inspección/entrega cliente tras Clear/rebind; FT independiente de validaciones y post-Update
// v2.5: FT_BAQ al salir de cantidad (Tab/clic fuera) — numQuantity epguid c6c8b7d5-e166-4ef7-9dc1-8efcb608ddfa
// v2.6: Si FT_BAQ viene vacío (job sin guardar), calcula FT = cantidad(MIL) x UD10.Number12 (FT/MILL)
// v2.7: Al tabular cantidad SIEMPRE recalcula desde pantalla; BAQ solo respaldo; Number12 estricto; formato FT corregido
// v2.8: JobProd ColumnChanged + Tab/Enter en numQuantity; FT inmediato sin esperar programar
// v2.9: No reescribir Quantity/ProdQty al tabular — solo leer para FT (evita que la cantidad se devuelva)
// v3.0: Restaurar comentatiostx (desvComentario) por usuario; FT_BAQ al liberar job (Released)
// v3.1: comentariosttt epguid 1c580add... (JobHead.desv_comentario_c); evita cuelgue SValdez
// v3.2: comentariosttt multilínea (Enter = nueva línea) para planeadores autorizados
// v3.7: Rendimiento UD/render — evita BAQ repetidos, ocultar campos 1 vez, menos reintentos
// v3.4: VALIDADO/NO VALIDADO — refresco en InitLastView/GetByID/cambio PartNum; lee Part.validare_c; invalida caché
// v3.5: Consecutivo folios — columna Defectos en rollo en vista previa/impresión; botón Guardar PDF
// v3.6: Serializado tab1 — prefijo grande editable (amarillo) + PO consulta destacada abajo
// v3.6.1: Prefijo — textbox compacto (2 caracteres)
// v3.6.2: Prefijo — borde visible, sin default J0 (usuario debe teclear)
// v3.6.3: Prefijo — textbox limpio con borde 3D completo (sin marco partido)
// v3.8: Botón Otros epguid 2f2a467a... — mismo serializado sin PO/prefijo; header azul degradado suave
// v3.8.1: Otros — sin columna Prefijo en grid ni PO/Prefijo en impresión/vista previa/PDF
// v3.9.2: No ocultar Fecha entrega / Fecha producción (UD); sin Height=0; mejor detección Customization
// v3.9.3: Proteger Cliente (lblCliente/txtCustID_c Part.Cliente_c); HideAdjacentEditor sin afectar vecinos; rehide sin full scan
// v3.10: FT estable — MakeToStockQty dispara cálculo UD10; FT_BAQ no pisa cantidad editada
// v3.11: Al consultar/crear job siempre muestra FT = cantidad × UD10 si FT_BAQ vacío; lock solo al editar cantidad
// v3.12: Consulta usa FT_BAQ primero (traveler); UD10 solo al editar cantidad; HasFtCorrida no invalida BAQ
// v3.13: FT_BAQ siempre si hay JobNum; fórmula UD10 completa (no Number12 obsoleto); sin lock en carga
// v3.14: FT solo si Make To Stock + Get Details; limpia si falta uno; consulta job con reintentos FT_BAQ
// v3.15: Detecta ops/materiales y qty MTS en todas las filas; sin bucle Detail*; no congela Job Entry
// v3.16: Consulta job programado repinta FT_BAQ; job nuevo limpia TOTAL FT CORRIDA
// v3.17: FT solo al Guardar (Update) si MTS+Get Details; consulta solo FT_BAQ; sin auto-calc
// v3.18: Consulta repinta FT tras layout Epicor; QueryFtBaQ por columna FT; limpia FT al teclear MTS
// v3.19: Limpia FT al cambiar job (no arrastra anterior); BAQ consulta con reintentos y variantes JobNum
// v3.20: FT auto al consultar job — reintentos escalonados sin cancelarse; sin depender de Refresh
// v3.21: Consulta FT liviana — 1 timer + caché BAQ; sin tormenta de reintentos (Job Entry rápido)
// v3.22: Repinta epiNumericEditorC1/epguid tras consulta; InitLastView = GetByID (sin Refresh)
// v3.23: FT al terminar carga del job (ExecuteDeferredJobHeadLoad) — mismo momento que Refresh
// v3.24: Consulta FT directa (BAQ + epiNumericEditorC1); restaura Refresh y carga de job
// v3.25: Debounce consulta FT — ya no se cancela entre GetByID y carga diferida
// v3.26: Consulta FT = patrón Job Tracker (FT_BAQ cache + row[0] → epiNumericEditorC1); sin debounce
// v3.27: FT_BAQ copiado literal Job Tracker; GetByID; evita que Leave de cantidad borre FT al consultar
// v3.28: GetByID difiere FT_BAQ (BeginInvoke); no cachea FT vacío/. ; repinta desde caché numérica
// v3.29: Consulta job — limpia FT al cambiar job (GetByID); carga BAQ tras limpiar; no repinta FT de job anterior
// v3.30: Consulta FT — limpia siempre; aplica vía GetFtCorridaControl; reintentos si Epicor aún no terminó GetByID
// v3.31: RequestFtConsultForJob — árbol/lista (Initialize), GetByID, reintentos hasta ~3s; limpia al cambiar job
// v3.32: FT más rápido — dedupe consultas, caché 1er intento, menos reintentos, sin FT duplicado en deferred
// **************************************************

extern alias Erp_Contracts_BO_JobEntry;
extern alias Erp_Contracts_BO_JobMtlSearch;
extern alias Erp_Contracts_BO_Project;
extern alias Erp_Contracts_BO_Part;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ice.Lib;
using Erp.BO;
using Ice.BO;
using Erp.Proxy.BO;
using Erp.UI;
using Ice.Adapters;
using Erp.Adapters;
using Ice.Lib.Customization;
using Ice.Lib.ExtendedProps;
using Ice.UI.FormFunctions;
using Ice.Lib.Framework;
using Ice.Lib.Searches;
using Epicor.Mfg.Lib;

public class Script
{
	EpiDataView warehouse;

	// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **
	// Begin Wizard Added Module Level Variables **

	private EpiDataView edvJobHead;
	private EpiBaseAdapter oTrans_jobAdapter;
	// End Wizard Added Module Level Variables **

	EpiDataView JobSt;

	// Add Custom Module Level Variables Here **
	EpiUltraCombo cbProdGroup;
	Ice.Core.Session session;
	string QR;
	string materialQR;
	string userID;
	EpiTextBox txtPartNum;
	Control desvComentario_c;

	private const string MoqControlEpGuid = "96cf1f82-e55d-42ee-b41a-caf25e263ac1";
	private const string MoqBaqName = "nummoc_c_ACTUALIZABLE";
	private const string MoqPlantId = "CFXJZ";

	private const string SerializadoButtonEpGuid = "7e3c0614-510b-4a31-983b-eb5c2ea2ef96";
	private const string OtrosButtonEpGuid = "2f2a467a-e961-4c65-b4a8-25951d78fb02";
	private const string OrdenCompraControlEpGuid = "b7fbb965-a54d-4161-a36e-323ebecff2c2";
	private const string FtCorridaControlEpGuid = "637e5ce4-606f-4cfe-9dfe-6c8698385599";
	private const string QuantityControlEpGuid = "c6c8b7d5-e166-4ef7-9dc1-8efcb608ddfa";
	private const string MakeToStockQtyControlName = "MakeToStockQty";
	private const string DesvComentarioControlEpGuid = "1c580add-87b1-447a-8562-3446f310b347";
	private const string DesvComentarioControlName = "comentariosttt";
	private const string ValidacionParteControlEpGuid = "026fcb95-e8a6-42ed-b83f-26ffe9733ec8";
	private const string ClienteControlEpGuid = "16cc37ea-d689-4411-995a-71b7194250f1";
	private const string ClienteLabelControlName = "lblCliente";
	private const string ClienteFieldControlName = "txtCustID_c";
	private const string ValidacionParteBaqName = "BAQVALIDARPART";
	private const string FtCorridaBaqName = "FT_BAQ";
	private Control _btnSerializado;
	private Control _btnOtros;
	private Control _validacionParteControl;
	private Control _txtOrdenDeVenta;
	private Control _numQuantity;
	private Control _makeToStockQty;

	private string _moqViolationHandledSig;
	private string _moqTrackedJobNum;

	private EpiDataView edvJobProd;

	private string _pendingDeferredJobKey;
	private int _pendingDeferredRow = -1;
	private string _lastLoadedFtJobNum = string.Empty;
	private string _ftConsultJobNum = string.Empty;
	private string _pendingFtDisplayValue = string.Empty;
	private bool _ftFieldApplyScheduled;
	private int _deferredFtGeneration;
	private bool _jobHeadColumnChangedWired;
	private bool _jobProdColumnChangedWired;
	private readonly List<Control> _quantityLeaveControls = new List<Control>();
	private Timer _ftQtyRefreshTimer;
	private int _ftQtyRefreshGeneration;
	private int _ftRefreshWhenReadyGeneration;
	private int _ftWatchGeneration;
	private string _ftWatchExpectedJob = string.Empty;
	private string _ftConsultInFlightJob = string.Empty;
	private DateTime _ftSuppressClearUntilUtc = DateTime.MinValue;
	private decimal _pendingQtyMilForFt = -1m;
	private int _validacionRefreshGeneration;
	private string _pendingValidacionPart = string.Empty;
	private string _lastDeferredJobLoadKey = string.Empty;
	private bool _hiddenFieldsLayoutApplied;
	private string _lastValidacionUiText = string.Empty;
	private Color _lastValidacionUiBackColor = Color.Empty;

	private const int MaxFtConsultAttempts = 5;
	private const int MaxDeferredFtAttempts = 4;
	private const int MaxValidacionRefreshAttempts = 4;
	private const int MaxHiddenFieldApplyAttempts = 2;
	private const int MaxFtFieldApplyAttempts = 4;
	private const int MaxQuantityWireAttempts = 10;

	/// <summary>True mientras el diseñador de Form Customization está abierto.</summary>
	private bool IsJobEntryCustomizationMode()
	{
		try
		{
			if (baseToolbarsManager != null && baseToolbarsManager.Tools != null)
			{
				string[] customizationToolKeys = new string[]
				{
					"SaveCustomizationTool",
					"ExitCustomizationTool",
					"CancelCustomizationTool",
					"CustomizationTool"
				};
				for (int i = 0; i < customizationToolKeys.Length; i++)
				{
					Infragistics.Win.UltraWinToolbars.ToolBase tool =
						baseToolbarsManager.Tools[customizationToolKeys[i]];
					if (tool != null && tool.SharedProps != null && tool.SharedProps.Visible)
						return true;
				}
			}
		}
		catch
		{
		}

		try
		{
			Form host = JobEntryForm;
			if (host == null)
				return false;
			string title = host.Text ?? string.Empty;
			if (title.IndexOf("Customization", StringComparison.OrdinalIgnoreCase) >= 0)
				return true;
			if (title.IndexOf("Personaliz", StringComparison.OrdinalIgnoreCase) >= 0)
				return true;
		}
		catch
		{
		}

		return false;
	}

	private bool ShouldRunRuntimeUiLogic()
	{
		return !IsJobEntryCustomizationMode();
	}

	public void InitializeCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
		// Begin Wizard Added Variable Initialization

		this.baseToolbarsManager.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
		this.edvJobHead = ((EpiDataView)(this.oTrans.EpiDataViews["JobHead"]));

		this.edvJobHead.EpiViewNotification += new EpiViewNotification(this.edvJobHead_EpiViewNotification);
		this.oTrans_jobAdapter = ((EpiBaseAdapter)(this.csm.TransAdaptersHT["oTrans_jobAdapter"]));
		this.oTrans_jobAdapter.BeforeAdapterMethod += new BeforeAdapterMethod(this.oTrans_jobAdapter_BeforeAdapterMethod);
		this.oTrans_jobAdapter.AfterAdapterMethod += new AfterAdapterMethod(this.oTrans_jobAdapter_AfterAdapterMethod);
		// End Wizard Added Variable Initialization

		// Begin Wizard Added Custom Method Calls

		// End Wizard Added Custom Method Calls
		session = (Ice.Core.Session)oTrans.Session;
		cbProdGroup = ((EpiUltraCombo)csm.GetNativeControlReference("98575694-7db5-4d7e-adce-7a4cb1504942"));
		QR = "";
		materialQR = "";
		userID = session.UserID;
		txtPartNum = ((EpiTextBox)csm.GetNativeControlReference("543b58f3-576d-4131-badf-b9f7e3d446c5"));
		txtPartNum.KeyUp += new System.Windows.Forms.KeyEventHandler(txtPartNum_KeyUp);
		txtPartNum.Leave += new System.EventHandler(txtPartNum_Leave);

		try
		{
			_numQuantity = csm.GetNativeControlReference(QuantityControlEpGuid) as Control;
			if (_numQuantity == null)
				_numQuantity = FindControlByName(JobEntryForm, "numQuantity");
			if (_numQuantity != null)
				WireQuantityLeaveControl(_numQuantity);
			WireMakeToStockQtyControl();
		}
		catch
		{
		}

		try
		{
			if (oTrans.EpiDataViews.ContainsKey("JobProd"))
			{
				edvJobProd = (EpiDataView)oTrans.EpiDataViews["JobProd"];
				edvJobProd.EpiViewNotification += new EpiViewNotification(edvJobProd_EpiViewNotification);
			}
		}
		catch
		{
		}

		try
		{
			if (oTrans.EpiDataViews.ContainsKey("Part"))
			{
				EpiDataView edvPart = (EpiDataView)oTrans.EpiDataViews["Part"];
				edvPart.EpiViewNotification += new EpiViewNotification(edvPart_EpiViewNotification);
			}
		}
		catch
		{
		}

		try
		{
			_validacionParteControl = csm.GetNativeControlReference(ValidacionParteControlEpGuid) as Control;
			if (_validacionParteControl == null)
				_validacionParteControl = FindControlByName(JobEntryForm, "validacionparte");
		}
		catch
		{
		}

		if (!ShouldRunRuntimeUiLogic())
			return;

		ApplyDesvComentarioAccess();
		ApplyHiddenJobEntryFields();
		ScheduleApplyHiddenJobEntryFields(0);
		WireSerializadoButton();
		WireOtrosButton();
		WireJobHeadColumnChanged();
		WireJobProdColumnChanged();
		WireOrdenVentaLeave();
		WireQuantityLeaveEvents();
		ScheduleWireQuantityLeave(0);
	}

	private static Control FindControlByName(Control parent, string name)
	{
		if (parent == null || string.IsNullOrEmpty(name))
			return null;
		if (string.Equals(parent.Name, name, StringComparison.OrdinalIgnoreCase))
			return parent;
		foreach (Control child in parent.Controls)
		{
			Control found = FindControlByName(child, name);
			if (found != null)
				return found;
		}
		return null;
	}

	private static string NormalizeSearchText(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return string.Empty;
		string s = value.Trim().ToLowerInvariant();
		s = s.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
		return s;
	}

	private static bool IsDesvComentarioControl(Control control)
	{
		if (control == null)
			return false;
		if (string.Equals(control.Name, DesvComentarioControlName, StringComparison.OrdinalIgnoreCase))
			return true;
		if (string.Equals(control.Name, "comentatiostx", StringComparison.OrdinalIgnoreCase))
			return true;
		if (string.Equals(control.Name, "desvComentario_c", StringComparison.OrdinalIgnoreCase))
			return true;
		return false;
	}

	private static bool ContainsDesvComentarioControl(Control root)
	{
		if (root == null)
			return false;
		if (IsDesvComentarioControl(root))
			return true;
		for (int i = 0; i < root.Controls.Count; i++)
		{
			if (ContainsDesvComentarioControl(root.Controls[i]))
				return true;
		}
		return false;
	}

	private static bool IsFechaEntregaClienteControlName(string normalizedName)
	{
		if (string.IsNullOrEmpty(normalizedName))
			return false;
		if (normalizedName.IndexOf("fechaentreacliente", StringComparison.Ordinal) >= 0)
			return true;
		if (normalizedName.IndexOf("fechaentregaclient", StringComparison.Ordinal) >= 0)
			return true;
		if (normalizedName.IndexOf("fechaentregacliente", StringComparison.Ordinal) >= 0)
			return true;
		if (normalizedName.IndexOf("labelfechaentrega", StringComparison.Ordinal) >= 0)
			return true;
		return normalizedName.IndexOf("fecha", StringComparison.Ordinal) >= 0
			&& normalizedName.IndexOf("cliente", StringComparison.Ordinal) >= 0
			&& (normalizedName.IndexOf("entrega", StringComparison.Ordinal) >= 0
				|| normalizedName.IndexOf("entreacliente", StringComparison.Ordinal) >= 0);
	}

	private static bool IsProtectedClienteControl(Control control)
	{
		if (control == null)
			return false;

		string n = NormalizeSearchText(control.Name);
		if (IsFechaEntregaClienteControlName(n))
			return false;

		if (string.Equals(n, ClienteLabelControlName, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(n, ClienteFieldControlName, StringComparison.OrdinalIgnoreCase))
			return true;
		if (n.IndexOf("cliente_c", StringComparison.Ordinal) >= 0)
			return true;
		if (n == "txtcustid" || n == "lblcliente")
			return true;

		string t = NormalizeSearchText(control.Text);
		if (t == "cliente")
			return true;

		return false;
	}

	private static bool IsProtectedOperationalFieldControl(Control control)
	{
		if (control == null || IsProtectedClienteControl(control))
			return true;
		if (IsDesvComentarioControl(control) || IsProtectedCustomDateControl(control))
			return true;

		string n = NormalizeSearchText(control.Name);
		if (n.IndexOf("ordencompra", StringComparison.Ordinal) >= 0
			|| n.IndexOf("orden_compra", StringComparison.Ordinal) >= 0)
			return true;
		if (n.IndexOf("ordendeventa", StringComparison.Ordinal) >= 0
			|| n.IndexOf("orden_de_venta", StringComparison.Ordinal) >= 0
			|| n == "txtordendeve")
			return true;
		if (n.IndexOf("validacionparte", StringComparison.Ordinal) >= 0)
			return true;
		if (n.IndexOf("epinumericeditorc1", StringComparison.Ordinal) >= 0
			|| n.IndexOf("lbaelft", StringComparison.Ordinal) >= 0)
			return true;
		if (n.IndexOf("moq", StringComparison.Ordinal) >= 0)
			return true;

		return false;
	}

	private static bool IsProtectedCustomDateControl(Control control)
	{
		if (control == null)
			return false;
		if (IsProtectedClienteControl(control))
			return true;

		string n = NormalizeSearchText(control.Name);
		string t = NormalizeSearchText(control.Text);

		if (n.IndexOf("fechaprod", StringComparison.Ordinal) >= 0)
			return true;
		if (n.IndexOf("fecha_prod", StringComparison.Ordinal) >= 0)
			return true;
		if (n.IndexOf("fechaentrega", StringComparison.Ordinal) >= 0
			&& n.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return true;
		if (n.IndexOf("fecha_entrega", StringComparison.Ordinal) >= 0
			&& n.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return true;
		if (t.IndexOf("fecha de produccion", StringComparison.Ordinal) >= 0)
			return true;
		if (t.IndexOf("fecha produccion", StringComparison.Ordinal) >= 0)
			return true;
		if (t.IndexOf("fecha entrega", StringComparison.Ordinal) >= 0
			&& t.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return true;
		if (t.IndexOf("fecha de entrega", StringComparison.Ordinal) >= 0
			&& t.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return true;

		return false;
	}

	private static bool ShouldHideJobEntryFieldLabel(string labelText, Control control)
	{
		string t = NormalizeSearchText(labelText);
		if (t.Length == 0)
			return false;
		if (IsProtectedClienteControl(control))
			return false;
		if (t == "cliente")
			return false;
		if (IsProtectedCustomDateControl(control))
			return false;
		if (t.IndexOf("desviacion", StringComparison.Ordinal) >= 0)
			return false;
		if (t.IndexOf("fecha de produccion", StringComparison.Ordinal) >= 0)
			return false;
		if (t.IndexOf("fecha produccion", StringComparison.Ordinal) >= 0)
			return false;
		if (t.IndexOf("fecha entrega", StringComparison.Ordinal) >= 0
			&& t.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return false;
		if (t.IndexOf("fecha de entrega", StringComparison.Ordinal) >= 0
			&& t.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return false;
		if (t.IndexOf("fecha inspeccion", StringComparison.Ordinal) >= 0)
			return true;
		if (t.IndexOf("fecha de entrega cliente", StringComparison.Ordinal) >= 0)
			return true;
		if (t.IndexOf("fecha entrega cliente", StringComparison.Ordinal) >= 0)
			return true;
		if (t == "comentarios" || t == "comentario")
		{
			if (ContainsDesvComentarioControl(control))
				return false;
			return true;
		}
		return false;
	}

	private static bool ShouldHideJobEntryFieldName(string controlName)
	{
		string n = NormalizeSearchText(controlName);
		if (n.Length == 0)
			return false;
		if (n.IndexOf("desvcomentario", StringComparison.Ordinal) >= 0)
			return false;
		if (n.IndexOf("txtcustid", StringComparison.Ordinal) >= 0
			|| n == "lblcliente"
			|| n.IndexOf("cliente_c", StringComparison.Ordinal) >= 0)
			return false;
		if (n.IndexOf("comentatiostx", StringComparison.Ordinal) >= 0)
			return false;
		if (n.IndexOf("comentariosttt", StringComparison.Ordinal) >= 0)
			return false;
		if (n.IndexOf("fechaprod", StringComparison.Ordinal) >= 0)
			return false;
		if (n.IndexOf("fecha_prod", StringComparison.Ordinal) >= 0)
			return false;
		if (n.IndexOf("fechaentrega", StringComparison.Ordinal) >= 0
			&& n.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return false;
		if (n.IndexOf("fecha_entrega", StringComparison.Ordinal) >= 0
			&& n.IndexOf("cliente", StringComparison.Ordinal) < 0)
			return false;
		if (IsFechaEntregaClienteControlName(n))
			return true;
		if (n.IndexOf("fechainspe", StringComparison.Ordinal) >= 0)
			return true;
		if (n.IndexOf("labelfechainsp", StringComparison.Ordinal) >= 0)
			return true;
		if (n.IndexOf("comentarios_c", StringComparison.Ordinal) >= 0 && n.IndexOf("desv", StringComparison.Ordinal) < 0)
			return true;
		return false;
	}

	private static bool IsLikelyBoundEditor(Control control)
	{
		if (control == null)
			return false;
		if (control is EpiTextBox || control is EpiNumericEditor)
			return true;
		string typeName = control.GetType().Name ?? string.Empty;
		if (typeName.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0)
			return true;
		if (typeName.IndexOf("Calendar", StringComparison.OrdinalIgnoreCase) >= 0)
			return true;
		if (typeName.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) >= 0)
			return true;
		return false;
	}

	private static void HideJobEntryControl(Control control)
	{
		if (control == null || IsProtectedOperationalFieldControl(control))
			return;
		control.Visible = false;
		control.Enabled = false;
		EpiTextBox tb = control as EpiTextBox;
		if (tb != null)
			tb.ReadOnly = true;
	}

	private static bool ShouldHideEditorForLabel(Control labelControl, Control editor)
	{
		if (labelControl == null || editor == null || IsProtectedOperationalFieldControl(editor))
			return false;

		string labelName = NormalizeSearchText(labelControl.Name);
		string editorName = NormalizeSearchText(editor.Name);

		if (labelName.IndexOf("fechainsp", StringComparison.Ordinal) >= 0
			&& editorName.IndexOf("fechainspe", StringComparison.Ordinal) >= 0)
			return true;
		if (labelName.IndexOf("fechaentrega", StringComparison.Ordinal) >= 0
			&& IsFechaEntregaClienteControlName(editorName))
			return true;
		if (labelName.IndexOf("comentario", StringComparison.Ordinal) >= 0
			&& editorName.IndexOf("comentario", StringComparison.Ordinal) >= 0
			&& editorName.IndexOf("desv", StringComparison.Ordinal) < 0)
			return true;

		return ShouldHideJobEntryFieldName(editor.Name);
	}

	private static void HideAdjacentEditor(Control labelControl)
	{
		if (labelControl == null || labelControl.Parent == null)
			return;

		Control parent = labelControl.Parent;
		int startIndex = parent.Controls.IndexOf(labelControl);
		if (startIndex >= 0)
		{
			for (int i = startIndex + 1; i < parent.Controls.Count; i++)
			{
				Control candidate = parent.Controls[i];
				if (!IsLikelyBoundEditor(candidate))
					continue;
				if (!ShouldHideEditorForLabel(labelControl, candidate))
					continue;
				HideJobEntryControl(candidate);
				return;
			}
		}

		int labelTop = labelControl.Top;
		Control best = null;
		int bestDist = int.MaxValue;
		for (int i = 0; i < parent.Controls.Count; i++)
		{
			Control sibling = parent.Controls[i];
			if (sibling == null || sibling == labelControl || !IsLikelyBoundEditor(sibling))
				continue;
			if (!ShouldHideEditorForLabel(labelControl, sibling))
				continue;
			int dist = Math.Abs(sibling.Top - labelTop) + Math.Abs(sibling.Left - labelControl.Left);
			if (dist < bestDist)
			{
				bestDist = dist;
				best = sibling;
			}
		}
		if (best != null && bestDist <= 80)
			HideJobEntryControl(best);
	}

	private static void HideJobEntryFieldsRecursive(Control root)
	{
		if (root == null)
			return;

		if (IsDesvComentarioControl(root) || IsProtectedCustomDateControl(root) || IsProtectedClienteControl(root))
			return;

		for (int i = 0; i < root.Controls.Count; i++)
			HideJobEntryFieldsRecursive(root.Controls[i]);

		if (ShouldHideJobEntryFieldName(root.Name))
		{
			HideJobEntryControl(root);
			return;
		}

		string text = root.Text ?? string.Empty;
		if (ShouldHideJobEntryFieldLabel(text, root))
		{
			HideJobEntryControl(root);
			HideAdjacentEditor(root);
		}
	}

	private void ResetHiddenFieldsLayoutCache()
	{
		_hiddenFieldsLayoutApplied = false;
	}

	private void ApplyHiddenJobEntryFields()
	{
		if (!ShouldRunRuntimeUiLogic())
			return;

		try
		{
			if (!_hiddenFieldsLayoutApplied)
			{
				HideJobEntryFieldsRecursive(JobEntryForm);
				_hiddenFieldsLayoutApplied = true;
			}
			else
				RehideKnownJobEntryFields();

			EnsureProtectedJobEntryFieldsVisible();
			ApplyDesvComentarioAccess();
		}
		catch
		{
		}
	}

	private void RehideKnownJobEntryFields()
	{
		if (!ShouldRunRuntimeUiLogic() || JobEntryForm == null)
			return;

		try
		{
			string[] hideByName = new string[]
			{
				"fechainspeccionjob", "labelfechainsp",
				"fechaentreacliente", "labelfechaentrega"
			};
			for (int i = 0; i < hideByName.Length; i++)
			{
				Control c = FindControlByName(JobEntryForm, hideByName[i]);
				if (c == null)
					continue;
				if (ShouldHideJobEntryFieldName(c.Name) || ShouldHideJobEntryFieldLabel(c.Text, c))
					HideJobEntryControl(c);
			}

			Control lblEntrega = FindControlByName(JobEntryForm, "labelfechaentrega");
			if (lblEntrega != null && ShouldHideJobEntryFieldLabel(lblEntrega.Text, lblEntrega))
			{
				HideJobEntryControl(lblEntrega);
				HideAdjacentEditor(lblEntrega);
			}
		}
		catch
		{
		}
	}

	private void EnsureProtectedJobEntryFieldsVisible()
	{
		if (!ShouldRunRuntimeUiLogic() || JobEntryForm == null)
			return;

		try
		{
			Control[] pinned = new Control[]
			{
				csm.GetNativeControlReference(ClienteControlEpGuid) as Control,
				FindControlByName(JobEntryForm, ClienteFieldControlName),
				FindControlByName(JobEntryForm, ClienteLabelControlName),
				FindControlByName(JobEntryForm, "txtordenDeVenta"),
				FindControlByName(JobEntryForm, "lblOrdenDeVenta"),
				csm.GetNativeControlReference(OrdenCompraControlEpGuid) as Control,
				csm.GetNativeControlReference(FtCorridaControlEpGuid) as Control,
				csm.GetNativeControlReference(ValidacionParteControlEpGuid) as Control,
				FindControlByName(JobEntryForm, "fechaprodjob"),
				FindControlByName(JobEntryForm, "labelfechaprod")
			};

			for (int i = 0; i < pinned.Length; i++)
			{
				Control c = pinned[i];
				if (c == null || c.IsDisposed)
					continue;
				if (!c.Visible)
					c.Visible = true;
			}
		}
		catch
		{
		}
	}

	private void ScheduleApplyHiddenJobEntryFields(int attempt)
	{
		if (!ShouldRunRuntimeUiLogic())
			return;

		if (attempt < 0 || attempt >= MaxHiddenFieldApplyAttempts)
			return;

		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed)
				return;

			MethodInvoker work = delegate
			{
				try
				{
					ApplyHiddenJobEntryFields();
					if (edvJobHead != null && edvJobHead.Row >= 0)
					{
						string jobNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
						if (!string.IsNullOrWhiteSpace(jobNum)
							&& string.Equals(_lastLoadedFtJobNum, jobNum.Trim(), StringComparison.OrdinalIgnoreCase)
							&& string.Equals(_ftWatchExpectedJob, jobNum.Trim(), StringComparison.OrdinalIgnoreCase)
							&& IsUsableFtBaQDisplay(_pendingFtDisplayValue))
						{
							TryApplyFtCorridaField(_pendingFtDisplayValue);
							if (!HasFtCorridaDisplayValue())
							{
								string cachedFt;
								if (JobEntryBaqCache.TryGetFtBaQ(jobNum.Trim(), out cachedFt)
									&& !string.IsNullOrWhiteSpace(cachedFt))
									RepaintFtCorridaFromCache(jobNum.Trim());
							}
						}
					}
					if (attempt + 1 < MaxHiddenFieldApplyAttempts)
						ScheduleApplyHiddenJobEntryFields(attempt + 1);
				}
				catch
				{
				}
			};

			if (!host.IsHandleCreated)
				return;

			if (attempt == 0)
			{
				host.BeginInvoke(work);
				return;
			}

			Timer retryTimer = new Timer();
			retryTimer.Interval = Math.Min(100 + (attempt * 50), 350);
			retryTimer.Tick += delegate(object s, EventArgs ev)
			{
				retryTimer.Stop();
				retryTimer.Dispose();
				if (host.IsDisposed)
					return;
				host.BeginInvoke(work);
			};
			retryTimer.Start();
		}
		catch
		{
		}
	}

	private void WireOrdenVentaLeave()
	{
		try
		{
			if (_txtOrdenDeVenta == null)
				_txtOrdenDeVenta = FindControlByName(JobEntryForm, "txtordenDeVe");
			if (_txtOrdenDeVenta == null)
				return;

			_txtOrdenDeVenta.Leave -= OrdenDeVenta_Leave;
			_txtOrdenDeVenta.Leave += OrdenDeVenta_Leave;
			_txtOrdenDeVenta.Validated -= OrdenDeVenta_Leave;
			_txtOrdenDeVenta.Validated += OrdenDeVenta_Leave;
		}
		catch
		{
		}
	}

	private void ScheduleWireQuantityLeave(int attempt)
	{
		if (attempt < 0 || attempt >= MaxQuantityWireAttempts)
			return;

		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed)
				return;

			MethodInvoker work = delegate
			{
				try
				{
					bool wired = WireQuantityLeaveEvents();
					if (!wired && attempt + 1 < MaxQuantityWireAttempts)
						ScheduleWireQuantityLeave(attempt + 1);
				}
				catch
				{
				}
			};

			if (!host.IsHandleCreated)
				return;

			if (attempt == 0)
			{
				host.BeginInvoke(work);
				return;
			}

			Timer retryTimer = new Timer();
			retryTimer.Interval = Math.Min(120 + (attempt * 60), 500);
			retryTimer.Tick += delegate(object s, EventArgs ev)
			{
				retryTimer.Stop();
				retryTimer.Dispose();
				if (host.IsDisposed)
					return;
				host.BeginInvoke(work);
			};
			retryTimer.Start();
		}
		catch
		{
		}
	}

	private bool WireQuantityLeaveEvents()
	{
		try
		{
			Control ctrl = _numQuantity;
			if (ctrl == null)
				ctrl = csm.GetNativeControlReference(QuantityControlEpGuid) as Control;
			if (ctrl == null)
				ctrl = FindControlByName(JobEntryForm, "numQuantity");

			string[] fallbackNames = new string[]
			{
				"numProdQty", "neProdQty", "txtProdQty",
				"numRequiredQty", "neRequiredQty", "txtRequiredQty"
			};
			for (int i = 0; i < fallbackNames.Length && ctrl == null; i++)
				ctrl = FindControlByName(JobEntryForm, fallbackNames[i]);

			if (ctrl == null)
				return false;

			_numQuantity = ctrl;
			WireQuantityLeaveControl(ctrl);
			WireMakeToStockQtyControl();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void WireMakeToStockQtyControl()
	{
		try
		{
			if (_makeToStockQty == null || _makeToStockQty.IsDisposed)
				_makeToStockQty = FindControlByName(JobEntryForm, MakeToStockQtyControlName);
			if (_makeToStockQty == null)
				return;
			WireQuantityLeaveControl(_makeToStockQty);
		}
		catch
		{
		}
	}

	private void WireQuantityLeaveControl(Control ctrl)
	{
		if (ctrl == null)
			return;

		for (int i = 0; i < _quantityLeaveControls.Count; i++)
		{
			if (ReferenceEquals(_quantityLeaveControls[i], ctrl))
				return;
		}

		ctrl.Leave -= QuantityField_Leave;
		ctrl.Leave += QuantityField_Leave;
		ctrl.Validated -= QuantityField_Leave;
		ctrl.Validated += QuantityField_Leave;
		ctrl.KeyDown -= QuantityField_KeyDown;
		ctrl.KeyDown += QuantityField_KeyDown;

		Infragistics.Win.UltraWinEditors.UltraNumericEditor une =
			ctrl as Infragistics.Win.UltraWinEditors.UltraNumericEditor;
		if (une != null)
		{
			une.AfterExitEditMode -= QuantityField_AfterExitEditMode;
			une.AfterExitEditMode += QuantityField_AfterExitEditMode;
		}

		_quantityLeaveControls.Add(ctrl);
	}

	private void QuantityField_AfterExitEditMode(object sender, EventArgs e)
	{
		Control ctrl = sender as Control;
		decimal qty = 0m;
		if (TryReadQuantityFromControl(ctrl, out qty))
			ScheduleRefreshFtFromQuantity(qty);
		else
			ScheduleRefreshFtFromQuantity();
	}

	private void QuantityField_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode != Keys.Tab && e.KeyCode != Keys.Enter)
			return;

		Control ctrl = sender as Control;
		decimal qty = 0m;
		if (TryReadQuantityFromControl(ctrl, out qty))
			ScheduleRefreshFtFromQuantity(qty);
		else
			ScheduleRefreshFtFromQuantity();
	}

	private void QuantityField_Leave(object sender, EventArgs e)
	{
		try
		{
			Form host = JobEntryForm;
			if (host != null && !host.IsDisposed && host.IsHandleCreated)
			{
				host.BeginInvoke((MethodInvoker)delegate { ScheduleRefreshFtFromQuantity(); });
				return;
			}
			ScheduleRefreshFtFromQuantity();
		}
		catch
		{
		}
	}

	private void ScheduleRefreshFtFromQuantity()
	{
		ScheduleRefreshFtFromQuantity(-1m);
	}

	private void SuppressFtClearDuringJobLoad()
	{
		_ftSuppressClearUntilUtc = DateTime.UtcNow.AddSeconds(5);
	}

	private void ScheduleRefreshFtFromQuantity(decimal capturedQtyMil)
	{
		if (capturedQtyMil > 0m)
			_pendingQtyMilForFt = capturedQtyMil;
		// No borrar FT mientras carga/consulta job (Leave de cantidad dispara al abrir job)
		if (DateTime.UtcNow < _ftSuppressClearUntilUtc)
			return;
		// v3.18: no calcular al teclear MTS; limpiar FT hasta Guardar (no durante carga de job)
		ClearFtCorridaField();
	}

	private void FtQtyRefreshTimer_Tick(object sender, EventArgs e)
	{
		try
		{
			if (_ftQtyRefreshTimer != null)
				_ftQtyRefreshTimer.Stop();
		}
		catch
		{
		}
	}

	private void RefreshFtCorridaNow()
	{
		RefreshFtCorridaUnified();
	}

	private void RefreshFtCorridaAfterRelease()
	{
		RefreshFtCorridaUnified();
	}

	private void RefreshFtCorridaFromBaQOnly()
	{
		RefreshFtCorridaUnified();
	}

	private void RefreshFtCorridaUnified()
	{
		RefreshFtCorridaUnified(-1m);
	}

	private void RefreshFtCorridaUnified(decimal preferredQtyMil)
	{
		try
		{
			TryCommitQuantityEdits();
			if (edvJobHead == null || edvJobHead.Row < 0)
				return;

			decimal readyQtyMil;
			if (!IsFtCalculationReady(out readyQtyMil))
				return;

			decimal forcedQty = preferredQtyMil;
			if (forcedQty <= 0m && _pendingQtyMilForFt > 0m)
			{
				forcedQty = _pendingQtyMilForFt;
				_pendingQtyMilForFt = -1m;
			}
			if (forcedQty <= 0m)
				forcedQty = readyQtyMil;

			string jobNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
			string partNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "PartNum");

			if (!string.IsNullOrWhiteSpace(jobNum))
			{
				JobEntryBaqCache.InvalidateFtBaQ(jobNum);
				_lastLoadedFtJobNum = string.Empty;
				string ftFromBaQ = QueryFtBaQ(jobNum);
				if (!IsStaleFtCacheValue(ftFromBaQ))
				{
					JobEntryBaqCache.SetFtBaQ(jobNum, ftFromBaQ);
					SetFtCorridaField(ftFromBaQ);
					_lastLoadedFtJobNum = jobNum;
					return;
				}
			}

			if (!string.IsNullOrWhiteSpace(partNum))
				JobEntryBaqCache.InvalidateFtPerMill(partNum);

			if (forcedQty > 0m && TryCalculateAndSetFtCorrida(forcedQty))
				return;
			TryCalculateAndSetFtCorrida();
		}
		catch
		{
		}
	}

	private void ClearFtCorridaField()
	{
		_lastLoadedFtJobNum = string.Empty;
		_pendingFtDisplayValue = string.Empty;
		SetFtCorridaField(string.Empty);
	}

	private void ForceClearFtCorridaUi()
	{
		_lastLoadedFtJobNum = string.Empty;
		_ftConsultJobNum = string.Empty;
		_pendingFtDisplayValue = string.Empty;
		try
		{
			Control c = GetFtCorridaControl();
			if (c != null)
			{
				EpiNumericEditor ne = c as EpiNumericEditor;
				if (ne != null)
				{
					ne.Value = null;
					ne.Text = string.Empty;
					ne.Refresh();
					return;
				}

				c.Text = string.Empty;
				c.Refresh();
				return;
			}
		}
		catch
		{
		}

		try
		{
			epiNumericEditorC1.Value = null;
			epiNumericEditorC1.Text = string.Empty;
		}
		catch
		{
		}
	}

	/// <summary>Copia literal de Job Tracker FT_BAQ — epiNumericEditorC1.Text = row[0].</summary>
	private static bool IsUsableFtBaQDisplay(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return false;
		string t = value.Trim();
		return t.Length > 0 && !string.Equals(t, ".", StringComparison.Ordinal);
	}

	private void FT_BAQ(string jobnum)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(jobnum))
				return;

			jobnum = jobnum.Trim();
			string cached;
			if (JobEntryBaqCache.TryGetFtBaQ(jobnum, out cached) && IsUsableFtBaQDisplay(cached))
			{
				ApplyFtBaQToControl(jobnum, cached);
				return;
			}

			using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
			{
				adapterDynamicQuery.BOConnect();
				string BAQName = "FT_BAQ";
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("JOBNUM", jobnum, "nvarchar", false, Guid.Empty, "A");
				adapterDynamicQuery.ExecuteByID(BAQName, ds);
				string Part1 = string.Empty;
				if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
					Part1 = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();

				if (IsUsableFtBaQDisplay(Part1))
				{
					JobEntryBaqCache.SetFtBaQ(jobnum, Part1.Trim());
					ApplyFtBaQToControl(jobnum, Part1.Trim());
				}
			}
		}
		catch (Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	private void ApplyFtBaQToControl(string jobnum, string ftText)
	{
		if (string.IsNullOrWhiteSpace(jobnum) || !IsUsableFtBaQDisplay(ftText))
			return;

		ftText = ftText.Trim();
		jobnum = jobnum.Trim();
		SuppressFtClearDuringJobLoad();
		_pendingFtDisplayValue = ftText;
		_ftConsultJobNum = jobnum;
		_lastLoadedFtJobNum = jobnum;

		EnsureProtectedJobEntryFieldsVisible();
		if (!TryApplyFtCorridaField(ftText))
		{
			try
			{
				epiNumericEditorC1.Text = ftText;
			}
			catch
			{
			}
		}
	}

	private void FT_BAQ(string jobnum, bool forceReload)
	{
		if (!string.IsNullOrWhiteSpace(jobnum) && forceReload)
			JobEntryBaqCache.InvalidateFtBaQ(jobnum.Trim());
		FT_BAQ(jobnum);
	}

	/// <summary>Consulta FT para job existente — limpia job anterior, BAQ + reintentos (árbol, GetByID, Refresh).</summary>
	private void RequestFtConsultForJob(string jobNum)
	{
		RequestFtConsultForJob(jobNum, false);
	}

	private void RequestFtConsultForJob(string jobNum, bool forceRefresh)
	{
		if (!ShouldRunRuntimeUiLogic())
			return;

		if (string.IsNullOrWhiteSpace(jobNum))
		{
			ResetFtCorridaForNewJob();
			return;
		}

		jobNum = jobNum.Trim();

		if (!forceRefresh
			&& string.Equals(_ftWatchExpectedJob, jobNum, StringComparison.OrdinalIgnoreCase)
			&& HasFtCorridaDisplayValueForJob(jobNum))
			return;

		if (!forceRefresh
			&& string.Equals(_ftConsultInFlightJob, jobNum, StringComparison.OrdinalIgnoreCase))
			return;

		if (!string.Equals(_lastLoadedFtJobNum, jobNum, StringComparison.OrdinalIgnoreCase))
			ForceClearFtCorridaUi();

		_ftWatchExpectedJob = jobNum;
		_ftConsultInFlightJob = jobNum;
		_ftWatchGeneration++;
		RunFtConsultAttempt(jobNum, _ftWatchGeneration, 0);
	}

	private void ClearFtConsultInFlight(string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return;
		if (string.Equals(_ftConsultInFlightJob, jobNum.Trim(), StringComparison.OrdinalIgnoreCase))
			_ftConsultInFlightJob = string.Empty;
	}

	private void LoadFtCorridaOnJobConsult(string jobNum)
	{
		RequestFtConsultForJob(jobNum, true);
	}

	private void RunFtConsultAttempt(string expectedJob, int generation, int attempt)
	{
		if (generation != _ftWatchGeneration)
			return;

		if (attempt < 0 || attempt >= MaxFtConsultAttempts)
			return;

		try
		{
			Form host = JobEntryForm;
			MethodInvoker work = delegate
			{
				if (generation != _ftWatchGeneration)
					return;

				string targetJob = expectedJob;
				if (edvJobHead != null && edvJobHead.Row >= 0)
				{
					string currentJob = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
					if (!string.IsNullOrWhiteSpace(currentJob))
						targetJob = currentJob.Trim();
				}

				if (string.IsNullOrWhiteSpace(targetJob))
				{
					ScheduleNextFtConsultAttempt(expectedJob, generation, attempt);
					return;
				}

				if (!string.Equals(targetJob, expectedJob, StringComparison.OrdinalIgnoreCase))
					return;

				if (!string.Equals(_lastLoadedFtJobNum, targetJob, StringComparison.OrdinalIgnoreCase))
					ForceClearFtCorridaUi();

				SuppressFtClearDuringJobLoad();
				EnsureProtectedJobEntryFieldsVisible();
				FT_BAQ(targetJob, attempt > 0);

				if (HasFtCorridaDisplayValueForJob(targetJob))
				{
					ClearFtConsultInFlight(targetJob);
					return;
				}

				ScheduleNextFtConsultAttempt(expectedJob, generation, attempt);
			};

			if (attempt == 0)
				work();
			else if (host != null && !host.IsDisposed && host.IsHandleCreated)
				host.BeginInvoke(work);
			else
				work();
		}
		catch
		{
		}
	}

	private void ScheduleNextFtConsultAttempt(string expectedJob, int generation, int attempt)
	{
		if (generation != _ftWatchGeneration)
			return;

		if (attempt + 1 >= MaxFtConsultAttempts)
		{
			ClearFtConsultInFlight(expectedJob);
			return;
		}

		Timer retryTimer = new Timer();
		retryTimer.Interval = Math.Min(180 + ((attempt + 1) * 180), 900);
		retryTimer.Tick += delegate(object s, EventArgs ev)
		{
			retryTimer.Stop();
			retryTimer.Dispose();
			RunFtConsultAttempt(expectedJob, generation, attempt + 1);
		};
		retryTimer.Start();
	}

	private bool HasFtCorridaDisplayValueForJob(string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return false;

		if (!string.Equals(_lastLoadedFtJobNum, jobNum.Trim(), StringComparison.OrdinalIgnoreCase))
			return false;

		return HasFtCorridaDisplayValue();
	}

	private void RepaintFtCorridaFromCache(string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return;

		string cached;
		if (!JobEntryBaqCache.TryGetFtBaQ(jobNum.Trim(), out cached) || !IsUsableFtBaQDisplay(cached))
			return;

		ApplyFtBaQToControl(jobNum.Trim(), cached.Trim());
	}

	private void ResetFtCorridaForNewJob()
	{
		_deferredFtGeneration++;
		_ftRefreshWhenReadyGeneration++;
		_ftWatchGeneration++;
		_ftWatchExpectedJob = string.Empty;
		_ftConsultInFlightJob = string.Empty;
		_lastLoadedFtJobNum = string.Empty;
		_ftConsultJobNum = string.Empty;
		_pendingFtDisplayValue = string.Empty;
		_pendingQtyMilForFt = -1m;
		ForceClearFtCorridaUi();
	}

	private void ScheduleCalculateFtOnSave(int attempt)
	{
		if (attempt < 0 || attempt >= MaxDeferredFtAttempts)
			return;

		_ftRefreshWhenReadyGeneration++;
		int generation = _ftRefreshWhenReadyGeneration;

		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed)
				return;

			MethodInvoker work = delegate
			{
				try
				{
					if (generation != _ftRefreshWhenReadyGeneration)
						return;
					if (edvJobHead == null || edvJobHead.Row < 0)
						return;

					TryCommitQuantityEdits();

					decimal qtyMil;
					if (!IsFtCalculationReady(out qtyMil))
					{
						if (attempt + 1 < MaxDeferredFtAttempts)
							ScheduleCalculateFtOnSave(attempt + 1);
						return;
					}

					string jobNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
					if (!string.IsNullOrWhiteSpace(jobNum))
					{
						JobEntryBaqCache.InvalidateFtBaQ(jobNum);
						_lastLoadedFtJobNum = string.Empty;
					}

					RefreshFtCorridaUnified(qtyMil);
					ScheduleApplyFtCorridaField(0);
				}
				catch
				{
				}
			};

			if (!host.IsHandleCreated)
				return;

			if (attempt == 0)
			{
				host.BeginInvoke(work);
				return;
			}

			Timer retryTimer = new Timer();
			retryTimer.Interval = Math.Min(250 + (attempt * 150), 1200);
			retryTimer.Tick += delegate(object s, EventArgs ev)
			{
				retryTimer.Stop();
				retryTimer.Dispose();
				if (host.IsDisposed || generation != _ftRefreshWhenReadyGeneration)
					return;
				host.BeginInvoke(work);
			};
			retryTimer.Start();
		}
		catch
		{
		}
	}

	/// <summary>Get Details aplicado: JobEngineered/Released o tiene operaciones/materiales en el job.</summary>
	private bool HasGetDetailsApplied()
	{
		if (IsCurrentJobEngineered() || IsCurrentJobReleased())
			return true;
		return HasJobOperRows() || HasJobMtlRows();
	}

	private bool HasJobOperRows()
	{
		string[] viewNames = new string[] { "JobOper", "JobOpDtl", "JobAsmOper" };
		for (int v = 0; v < viewNames.Length; v++)
		{
			if (CountEpiDataViewRows(viewNames[v]) > 0)
				return true;
		}
		return false;
	}

	private bool HasJobMtlRows()
	{
		string[] viewNames = new string[] { "JobMtl", "JobMaterial" };
		for (int v = 0; v < viewNames.Length; v++)
		{
			if (CountEpiDataViewRows(viewNames[v]) > 0)
				return true;
		}
		return false;
	}

	private int CountEpiDataViewRows(string viewName)
	{
		try
		{
			if (!oTrans.EpiDataViews.ContainsKey(viewName))
				return 0;
			EpiDataView view = (EpiDataView)oTrans.EpiDataViews[viewName];
			if (view == null || view.dataView == null)
				return 0;

			int count = 0;
			for (int i = 0; i < view.dataView.Count; i++)
			{
				if (view.dataView[i].Row.RowState == DataRowState.Deleted)
					continue;
				count++;
			}
			if (count > 0)
				return count;

			if (view.dataView.Table != null)
			{
				for (int r = 0; r < view.dataView.Table.Rows.Count; r++)
				{
					if (view.dataView.Table.Rows[r].RowState == DataRowState.Deleted)
						continue;
					count++;
				}
			}
			return count;
		}
		catch
		{
			return 0;
		}
	}

	private bool TryGetMakeToStockQtyMil(out decimal qtyMil)
	{
		qtyMil = 0m;
		try
		{
			WireMakeToStockQtyControl();
			if (_makeToStockQty != null && TryReadQuantityFromControl(_makeToStockQty, out qtyMil))
				return true;

			if (_numQuantity != null && TryReadQuantityFromControl(_numQuantity, out qtyMil))
				return true;

			if (edvJobProd != null && edvJobProd.dataView != null && edvJobProd.dataView.Table != null)
			{
				string[] cols = new string[] { "RequiredQty", "BaseRequiredQty", "Quantity", "ProdQty" };

				if (edvJobProd.Row >= 0 && TryReadJobProdQtyAtRow(edvJobProd.Row, cols, out qtyMil))
					return true;

				for (int r = 0; r < edvJobProd.dataView.Count; r++)
				{
					if (TryReadJobProdQtyAtRow(r, cols, out qtyMil))
						return true;
				}

				DataTable table = edvJobProd.dataView.Table;
				for (int r = 0; r < table.Rows.Count; r++)
				{
					if (table.Rows[r].RowState == DataRowState.Deleted)
						continue;
					for (int i = 0; i < cols.Length; i++)
					{
						if (!table.Columns.Contains(cols[i]))
							continue;
						object v = table.Rows[r][cols[i]];
						if (v != null && v != DBNull.Value
							&& TryParseDecimalFlexible(v.ToString(), out qtyMil) && qtyMil > 0m)
							return true;
					}
				}
			}

			if (HasGetDetailsApplied() && edvJobHead != null && edvJobHead.Row >= 0)
			{
				object v = edvJobHead.dataView[edvJobHead.Row]["ProdQty"];
				if (v != null && v != DBNull.Value
					&& TryParseDecimalFlexible(v.ToString(), out qtyMil) && qtyMil > 0m)
					return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool TryReadJobProdQtyAtRow(int rowIndex, string[] cols, out decimal qtyMil)
	{
		qtyMil = 0m;
		try
		{
			if (edvJobProd == null || edvJobProd.dataView == null || edvJobProd.dataView.Table == null)
				return false;
			if (rowIndex < 0 || rowIndex >= edvJobProd.dataView.Count)
				return false;

			for (int i = 0; i < cols.Length; i++)
			{
				if (!edvJobProd.dataView.Table.Columns.Contains(cols[i]))
					continue;
				object v = edvJobProd.dataView[rowIndex][cols[i]];
				if (v != null && v != DBNull.Value
					&& TryParseDecimalFlexible(v.ToString(), out qtyMil) && qtyMil > 0m)
					return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool IsFtCalculationReady(out decimal qtyMil)
	{
		qtyMil = 0m;
		if (!HasGetDetailsApplied())
			return false;
		return TryGetMakeToStockQtyMil(out qtyMil);
	}

	private void RefreshFtCorridaNow(bool preferBaQFirst)
	{
		RefreshFtCorridaUnified();
	}

	private static bool IsProposedBooleanTrue(object value)
	{
		if (value == null || value == DBNull.Value)
			return false;
		if (value is bool)
			return (bool)value;
		string s = value.ToString().Trim();
		return s == "1"
			|| string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase);
	}

	private bool IsCurrentJobReleased()
	{
		try
		{
			if (edvJobHead == null || edvJobHead.Row < 0)
				return false;
			if (!edvJobHead.dataView.Table.Columns.Contains("JobReleased"))
				return false;
			return IsProposedBooleanTrue(edvJobHead.dataView[edvJobHead.Row]["JobReleased"]);
		}
		catch
		{
			return false;
		}
	}

	private bool IsCurrentJobEngineered()
	{
		try
		{
			if (edvJobHead == null || edvJobHead.Row < 0)
				return false;
			if (!edvJobHead.dataView.Table.Columns.Contains("JobEngineered"))
				return false;
			return IsProposedBooleanTrue(edvJobHead.dataView[edvJobHead.Row]["JobEngineered"]);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Job ya programado: FT_BAQ es la fuente correcta (traveler).</summary>
	private bool ShouldUseFtBaQAsSource()
	{
		return IsCurrentJobReleased() || IsCurrentJobEngineered();
	}

	private void ScheduleRefreshFtAfterRelease()
	{
		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed || !host.IsHandleCreated)
			{
				RefreshFtCorridaFromBaQOnly();
				return;
			}

			host.BeginInvoke((MethodInvoker)delegate
			{
				RefreshFtCorridaFromBaQOnly();
			});
		}
		catch
		{
			RefreshFtCorridaFromBaQOnly();
		}
	}

	private static bool IsQuantityColumn(string columnName)
	{
		if (string.IsNullOrWhiteSpace(columnName))
			return false;
		return string.Equals(columnName, "RequiredQty", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(columnName, "BaseRequiredQty", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(columnName, "Quantity", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(columnName, "ProdQty", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(columnName, "OurQuantity", StringComparison.OrdinalIgnoreCase);
	}

	private void TryCommitQuantityEdits()
	{
		TryCommitJobHeadUiEdits();
	}

	private void TryCommitJobHeadUiEdits()
	{
		try
		{
			Form host = JobEntryForm;
			if (host != null && !host.IsDisposed)
			{
				host.Validate();
				if (host.ActiveControl != null)
					host.ActiveControl.Update();
			}
			if (edvJobHead != null && edvJobHead.Row >= 0 && edvJobHead.dataView != null)
				edvJobHead.dataView[edvJobHead.Row].EndEdit();
			if (edvJobProd != null && edvJobProd.Row >= 0 && edvJobProd.dataView != null)
				edvJobProd.dataView[edvJobProd.Row].EndEdit();
		}
		catch
		{
		}
	}

	private void TryLoadFtCorridaOnQuantityLeave()
	{
		ScheduleRefreshFtFromQuantity();
	}

	private static string FormatFtDisplay(decimal totalFt)
	{
		return totalFt.ToString("0.0000", CultureInfo.InvariantCulture);
	}

	private static bool IsUd10Number12Column(string colName)
	{
		if (string.IsNullOrEmpty(colName))
			return false;
		if (colName.Equals("Number12", StringComparison.OrdinalIgnoreCase))
			return true;
		int idx = colName.LastIndexOf("Number12", StringComparison.OrdinalIgnoreCase);
		return idx >= 0 && idx + 8 == colName.Length;
	}

	private bool HasFtCorridaDisplayValue()
	{
		try
		{
			Control c = GetFtCorridaControl();
			if (c == null)
				return !string.IsNullOrWhiteSpace(_pendingFtDisplayValue)
					&& !string.Equals(_pendingFtDisplayValue.Trim(), ".", StringComparison.Ordinal);

			EpiNumericEditor ne = c as EpiNumericEditor;
			string text = ne != null ? ne.Text : c.Text;
			if (string.IsNullOrWhiteSpace(text) || string.Equals(text.Trim(), ".", StringComparison.Ordinal))
				return false;

			decimal d;
			return TryParseDecimalFlexible(text, out d) && d > 0m;
		}
		catch
		{
			return false;
		}
	}

	private Control GetFtCorridaControl()
	{
		try
		{
			Control c = csm.GetNativeControlReference(FtCorridaControlEpGuid) as Control;
			if (c == null)
				c = epiNumericEditorC1 as Control;
			if (c == null)
				c = FindControlByName(JobEntryForm, "epiNumericEditorC1");
			return c;
		}
		catch
		{
			return null;
		}
	}

	private string GetFtCorridaControlText()
	{
		try
		{
			Control c = GetFtCorridaControl();
			if (c == null)
				return string.Empty;
			EpiNumericEditor ne = c as EpiNumericEditor;
			if (ne != null)
				return ne.Text == null ? string.Empty : ne.Text.Trim();
			return c.Text == null ? string.Empty : c.Text.Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private bool TryGetQuantityMilForFt(out decimal qtyMil)
	{
		qtyMil = 0m;
		try
		{
			if (_makeToStockQty != null && TryReadQuantityFromControl(_makeToStockQty, out qtyMil))
				return true;

			if (_numQuantity != null && TryReadQuantityFromControl(_numQuantity, out qtyMil))
				return true;

			if (edvJobProd != null && edvJobProd.Row >= 0
				&& edvJobProd.dataView != null && edvJobProd.dataView.Table != null)
			{
				string[] cols = new string[] { "RequiredQty", "BaseRequiredQty", "Quantity", "ProdQty" };
				for (int i = 0; i < cols.Length; i++)
				{
					if (!edvJobProd.dataView.Table.Columns.Contains(cols[i]))
						continue;
					object v = edvJobProd.dataView[edvJobProd.Row][cols[i]];
					if (v != null && v != DBNull.Value
						&& TryParseDecimalFlexible(v.ToString(), out qtyMil) && qtyMil > 0m)
						return true;
				}
			}

			if (edvJobHead != null && edvJobHead.Row >= 0)
			{
				object v = edvJobHead.dataView[edvJobHead.Row]["ProdQty"];
				if (v != null && v != DBNull.Value
					&& TryParseDecimalFlexible(v.ToString(), out qtyMil) && qtyMil > 0m)
					return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool TryReadQuantityFromControl(Control ctrl, out decimal qty)
	{
		qty = 0m;
		if (ctrl == null)
			return false;

		EpiNumericEditor ne = ctrl as EpiNumericEditor;
		if (ne != null && ne.Value != null && ne.Value != DBNull.Value
			&& TryParseDecimalFlexible(Convert.ToString(ne.Value), out qty) && qty > 0m)
			return true;

		return TryParseDecimalFlexible(ctrl.Text, out qty) && qty > 0m;
	}

	private decimal QueryFtPerMillFromUd10(string partNum)
	{
		if (string.IsNullOrWhiteSpace(partNum))
			return 0m;

		decimal cached;
		if (JobEntryBaqCache.TryGetFtPerMill(partNum, out cached))
			return cached;

		decimal ftPerMill = 0m;
		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("partNum_", partNum.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID("CODIFLEX-UD10", ds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t != null && t.Rows.Count > 0)
					ftPerMill = ResolveFtPerMillFromUd10Row(t.Rows[0], t);
			}
		}
		catch
		{
		}

		if (ftPerMill > 0m)
			JobEntryBaqCache.SetFtPerMill(partNum, ftPerMill);
		return ftPerMill;
	}

	private static decimal ResolveFtPerMillFromUd10Row(DataRow row, DataTable table)
	{
		decimal computed = ComputeFtPerMillFromUd10Row(row, table);
		if (computed > 0m)
			return computed;
		return ReadUd10Number12Strict(row, table);
	}

	private static decimal ComputeFtPerMillFromUd10Row(DataRow row, DataTable table)
	{
		try
		{
			decimal number01 = ReadUd10DecimalByHint(row, table, new string[] { "Number01", "UD10_Number01" }, 1);
			if (number01 <= 0m)
				return 0m;

			decimal shortChar03 = ReadUd10DecimalByHint(row, table, new string[] { "ShortChar03", "UD10_ShortChar03" }, -1);
			decimal shortChar04 = ReadUd10DecimalByHint(row, table, new string[] { "ShortChar04", "UD10_ShortChar04" }, -1);
			decimal shortChar05 = ReadUd10DecimalByHint(row, table, new string[] { "ShortChar05", "UD10_ShortChar05" }, -1);
			decimal number18 = ReadUd10DecimalByHint(row, table, new string[] { "Number18", "UD10_Number18" }, 18);
			decimal number19 = ReadUd10DecimalByHint(row, table, new string[] { "Number19", "UD10_Number19" }, 19);

			decimal msimill = (shortChar03 + (number18 + number19)) * (shortChar04 + shortChar05);
			if (msimill <= 0m)
				return 0m;

			return (msimill * 1000m) / (12m * number01);
		}
		catch
		{
			return 0m;
		}
	}

	private static decimal ReadUd10DecimalByHint(DataRow row, DataTable table, string[] columnHints, int fallbackIndex)
	{
		for (int c = 0; c < table.Columns.Count; c++)
		{
			string colName = table.Columns[c].ColumnName ?? string.Empty;
			for (int h = 0; h < columnHints.Length; h++)
			{
				if (!colName.Equals(columnHints[h], StringComparison.OrdinalIgnoreCase)
					&& colName.IndexOf(columnHints[h], StringComparison.OrdinalIgnoreCase) < 0)
					continue;
				decimal parsed;
				if (TryParseDecimalFromObject(row[c], out parsed))
					return parsed;
			}
		}
		if (fallbackIndex >= 0 && fallbackIndex < table.Columns.Count)
		{
			decimal parsed;
			if (TryParseDecimalFromObject(row[fallbackIndex], out parsed))
				return parsed;
		}
		return 0m;
	}

	private static decimal ReadUd10Number12Strict(DataRow row, DataTable table)
	{
		string[] exactNames = new string[]
		{
			"UD10_Number12", "Calculated_Number12", "Number12"
		};
		for (int n = 0; n < exactNames.Length; n++)
		{
			if (!table.Columns.Contains(exactNames[n]))
				continue;
			decimal parsed;
			if (TryParseDecimalFromObject(row[exactNames[n]], out parsed) && parsed > 0m)
				return parsed;
		}

		for (int c = 0; c < table.Columns.Count; c++)
		{
			string colName = table.Columns[c].ColumnName ?? string.Empty;
			if (!IsUd10Number12Column(colName))
				continue;
			decimal parsed;
			if (TryParseDecimalFromObject(row[c], out parsed) && parsed > 0m)
				return parsed;
		}
		return 0m;
	}

	private static bool TryParseDecimalFromObject(object value, out decimal d)
	{
		d = 0m;
		if (value == null || value == DBNull.Value)
			return false;
		string s = value.ToString();
		if (string.IsNullOrWhiteSpace(s))
			return false;
		s = s.Trim();
		if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
			return true;
		return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
	}

	private bool TryCalculateAndSetFtCorrida()
	{
		return TryCalculateAndSetFtCorrida(-1m);
	}

	private bool TryCalculateAndSetFtCorrida(decimal forcedQtyMil)
	{
		if (edvJobHead == null || edvJobHead.Row < 0)
			return false;

		decimal readyQty;
		if (!IsFtCalculationReady(out readyQty))
			return false;

		string partNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "PartNum");
		string jobNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
		decimal qtyMil = forcedQtyMil;
		if (qtyMil <= 0m && !TryGetMakeToStockQtyMil(out qtyMil))
			qtyMil = readyQty;
		if (string.IsNullOrWhiteSpace(partNum) || qtyMil <= 0m)
			return false;

		decimal ftPerMill = QueryFtPerMillFromUd10(partNum);
		if (ftPerMill <= 0m)
			return false;

		decimal totalFt = qtyMil * ftPerMill;
		string display = FormatFtDisplay(totalFt);
		SetFtCorridaField(display);

		if (!string.IsNullOrWhiteSpace(jobNum))
		{
			JobEntryBaqCache.SetFtBaQ(jobNum, display);
			_lastLoadedFtJobNum = jobNum;
		}
		return true;
	}

	private bool IsPlausibleFtResult(decimal ftValue, decimal qtyMil, decimal ftPerMill)
	{
		if (ftValue <= 0m)
			return false;
		if (qtyMil <= 0m || ftPerMill <= 0m)
			return ftValue >= 10m;

		decimal expected = qtyMil * ftPerMill;
		if (expected <= 0m)
			return ftValue >= 10m;

		decimal diff = Math.Abs(ftValue - expected);
		decimal tolerance = Math.Max(expected * 0.05m, 1m);
		return diff <= tolerance;
	}

	private void OrdenDeVenta_Leave(object sender, EventArgs e)
	{
		try
		{
			if (edvJobHead == null || edvJobHead.Row < 0)
				return;

			Control ctrl = sender as Control;
			if (ctrl != null && edvJobHead.dataView != null && edvJobHead.dataView.Table != null
				&& edvJobHead.dataView.Table.Columns.Contains("OrdenDeVenta_c"))
			{
				string ovText = ctrl.Text == null ? string.Empty : ctrl.Text.Trim();
				if (ovText.Length > 0)
				{
					object current = edvJobHead.dataView[edvJobHead.Row]["OrdenDeVenta_c"];
					string currentText = current == null || current == DBNull.Value ? string.Empty : current.ToString().Trim();
					if (!string.Equals(currentText, ovText, StringComparison.OrdinalIgnoreCase))
						edvJobHead.dataView[edvJobHead.Row]["OrdenDeVenta_c"] = ovText;
				}
			}

			TryPopulateOrdenCompraFromOv(edvJobHead, edvJobHead.Row);
		}
		catch
		{
		}
	}

	private void WireJobHeadColumnChanged()
	{
		try
		{
			if (_jobHeadColumnChangedWired || edvJobHead == null || edvJobHead.dataView == null || edvJobHead.dataView.Table == null)
				return;

			edvJobHead.dataView.Table.ColumnChanged -= JobHead_Table_ColumnChanged;
			edvJobHead.dataView.Table.ColumnChanged += JobHead_Table_ColumnChanged;
			_jobHeadColumnChangedWired = true;
		}
		catch
		{
		}
	}

	private void WireJobProdColumnChanged()
	{
		try
		{
			if (_jobProdColumnChangedWired || edvJobProd == null || edvJobProd.dataView == null || edvJobProd.dataView.Table == null)
				return;

			edvJobProd.dataView.Table.ColumnChanged -= JobProd_Table_ColumnChanged;
			edvJobProd.dataView.Table.ColumnChanged += JobProd_Table_ColumnChanged;
			_jobProdColumnChangedWired = true;
		}
		catch
		{
		}
	}

	private void JobProd_Table_ColumnChanged(object sender, DataColumnChangeEventArgs e)
	{
		try
		{
			if (edvJobProd == null || edvJobProd.Row < 0 || e.Row == null || e.Column == null)
				return;
			if (!IsQuantityColumn(e.Column.ColumnName))
				return;

			DataRowView currentRowView = edvJobProd.dataView[edvJobProd.Row];
			if (currentRowView == null || e.Row != currentRowView.Row)
				return;

			decimal qty = 0m;
			if (e.ProposedValue != null && e.ProposedValue != DBNull.Value
				&& TryParseDecimalFlexible(Convert.ToString(e.ProposedValue), out qty) && qty > 0m)
				_pendingQtyMilForFt = qty;
		}
		catch
		{
		}
	}

	private void JobHead_Table_ColumnChanged(object sender, DataColumnChangeEventArgs e)
	{
		try
		{
			if (edvJobHead == null || edvJobHead.Row < 0 || e.Row == null || e.Column == null)
				return;

			DataRowView currentRowView = edvJobHead.dataView[edvJobHead.Row];
			if (currentRowView == null || e.Row != currentRowView.Row)
				return;

			if (string.Equals(e.Column.ColumnName, "JobNum", StringComparison.OrdinalIgnoreCase))
			{
				string proposedJob = e.ProposedValue == null || e.ProposedValue == DBNull.Value
					? string.Empty
					: e.ProposedValue.ToString().Trim();
				if (string.IsNullOrWhiteSpace(proposedJob))
					ResetFtCorridaForNewJob();
				else
					LoadFtCorridaOnJobConsult(proposedJob);
				return;
			}

			if (string.Equals(e.Column.ColumnName, "PartNum", StringComparison.OrdinalIgnoreCase))
			{
				string part = e.ProposedValue == null || e.ProposedValue == DBNull.Value
					? string.Empty
					: e.ProposedValue.ToString().Trim();
				if (string.IsNullOrWhiteSpace(part))
				{
					ApplyValidacionParteUi(string.Empty);
					SetFtCorridaField(string.Empty);
					return;
				}

				JobEntryBaqCache.InvalidateValidacionParte(part);
				JobEntryBaqCache.InvalidateFtPerMill(part);
				ScheduleRefreshValidacionParte(part, 0);
				return;
			}

			if (string.Equals(e.Column.ColumnName, "ProdQty", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (string.Equals(e.Column.ColumnName, "JobReleased", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (string.Equals(e.Column.ColumnName, "JobEngineered", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (!string.Equals(e.Column.ColumnName, "OrdenDeVenta_c", StringComparison.OrdinalIgnoreCase))
				return;

			TryPopulateOrdenCompraFromOv(edvJobHead, edvJobHead.Row);
		}
		catch
		{
		}
	}

	private void WireSerializadoButton()
	{
		try
		{
			_btnSerializado = csm.GetNativeControlReference(SerializadoButtonEpGuid) as Control;
			if (_btnSerializado == null)
				return;
			_btnSerializado.Click -= btnSerializado_Click;
			_btnSerializado.Click += btnSerializado_Click;
		}
		catch
		{
		}
	}

	private void btnSerializado_Click(object sender, EventArgs e)
	{
		OpenSerializadoForm(SerializadoFormMode.BrkSerializado);
	}

	private void WireOtrosButton()
	{
		try
		{
			_btnOtros = csm.GetNativeControlReference(OtrosButtonEpGuid) as Control;
			if (_btnOtros == null)
				return;
			_btnOtros.Click -= btnOtros_Click;
			_btnOtros.Click += btnOtros_Click;
		}
		catch
		{
		}
	}

	private void btnOtros_Click(object sender, EventArgs e)
	{
		OpenSerializadoForm(SerializadoFormMode.Otros);
	}

	private void OpenSerializadoForm(SerializadoFormMode mode)
	{
		try
		{
			string formLabel = mode == SerializadoFormMode.Otros ? "Otros" : "Serializado";
			if (edvJobHead == null || edvJobHead.Row < 0)
			{
				MessageBox.Show("Abra o cree un Job antes de usar " + formLabel + ".", formLabel,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string jobNum = GetJobField("JobNum");
			if (string.IsNullOrWhiteSpace(jobNum))
			{
				MessageBox.Show("El Job no tiene número. Guarde el Job primero.", formLabel,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			string cliente = ResolveClienteJobEntry();
			using (var frm = new SerializadoJobEntryForm(JobEntryForm, oTrans, edvJobHead, edvJobProd, cliente, PersistSerializadoJob, userID, mode))
				frm.ShowDialog(JobEntryForm);
		}
		catch (Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	private string ResolveClienteJobEntry()
	{
		try
		{
			string[] viewNames = new string[] { "Part", "JobHead", "JobAsmbl", "JobProd" };
			for (int i = 0; i < viewNames.Length; i++)
			{
				if (!oTrans.EpiDataViews.ContainsKey(viewNames[i]))
					continue;
				EpiDataView view = (EpiDataView)oTrans.EpiDataViews[viewNames[i]];
				if (view == null || view.Row < 0 || view.dataView == null || view.dataView.Table == null)
					continue;
				if (!view.dataView.Table.Columns.Contains("Cliente_c"))
					continue;
				object val = view.dataView[view.Row]["Cliente_c"];
				if (val != null && val != DBNull.Value)
				{
					string s = val.ToString().Trim();
					if (s.Length > 0)
						return s;
				}
			}
		}
		catch { }
		return string.Empty;
	}

	private string GetJobField(string column)
	{
		try
		{
			if (edvJobHead == null || edvJobHead.Row < 0)
				return string.Empty;
			if (!edvJobHead.dataView.Table.Columns.Contains(column))
				return string.Empty;
			object v = edvJobHead.dataView[edvJobHead.Row][column];
			return v == null || v == DBNull.Value ? string.Empty : v.ToString().Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private bool PersistSerializadoJob(out string error)
	{
		error = string.Empty;
		try
		{
			if (oTrans_jobAdapter == null)
			{
				error = "Adapter JobHead no disponible en Job Entry.";
				return false;
			}
			oTrans_jobAdapter.Update();
			return true;
		}
		catch (Exception ex)
		{
			error = SerializadoSaveService.FormatExceptionMessage(ex);
			return false;
		}
	}

	private static string GetEpiNotifyColumnName(EpiDataView view, EpiNotifyArgs args)
	{
		try
		{
			if (view == null || view.dataView == null || view.dataView.Table == null)
				return string.Empty;
			object colRaw = args.Column;
			System.Data.DataColumn dc = colRaw as System.Data.DataColumn;
			if (dc != null)
				return dc.ColumnName;
			int idx = -1;
			if (colRaw != null)
			{
				try { idx = System.Convert.ToInt32(colRaw); }
				catch { return string.Empty; }
			}
			if (idx >= 0 && idx < view.dataView.Table.Columns.Count)
				return view.dataView.Table.Columns[idx].ColumnName;
		}
		catch { }
		return string.Empty;
	}

	private void edvJobProd_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		try
		{
			if (!ShouldRunRuntimeUiLogic())
				return;

			if (edvJobHead == null || edvJobHead.Row < 0)
				return;

			if (args.NotifyType == EpiTransaction.NotifyType.Initialize)
			{
				WireJobProdColumnChanged();
				return;
			}

			if (args.NotifyType == EpiTransaction.NotifyType.InitLastView)
			{
				WireJobProdColumnChanged();
				return;
			}

			string cn = GetEpiNotifyColumnName(view, args);
			if (IsQuantityColumn(cn))
			{
				if (((Ice.Core.Session)oTrans.Session).PlantID != MoqPlantId)
					return;

				string part = edvJobHead.dataView[edvJobHead.Row]["PartNum"] == null
					? string.Empty
					: edvJobHead.dataView[edvJobHead.Row]["PartNum"].ToString();
				if (string.IsNullOrWhiteSpace(part))
					return;

				VALIDARMOQJOB(part);
			}
		}
		catch { }
	}

	private void txtPartNum_Leave(object sender, EventArgs e)
	{
		if (edvJobHead == null || edvJobHead.Row < 0)
			return;

		string part = edvJobHead.dataView[edvJobHead.Row]["PartNum"] == null
			? string.Empty
			: edvJobHead.dataView[edvJobHead.Row]["PartNum"].ToString();
		if (string.IsNullOrWhiteSpace(part))
		{
			ApplyValidacionParteUi(string.Empty);
			return;
		}

		part = part.Trim();
		JobEntryBaqCache.InvalidateValidacionParte(part);
		ScheduleRefreshValidacionParte(part, 0);

		string currentPlant = ((Ice.Core.Session)oTrans.Session).PlantID;
		if (currentPlant == MoqPlantId)
			VALIDARMOQJOB(part);
	}

	private bool TryGetQuantityForMoq(out decimal qty)
	{
		qty = 0m;
		try
		{
			EpiDataView jp = null;
			if (oTrans.EpiDataViews.ContainsKey("JobProd"))
				jp = (EpiDataView)oTrans.EpiDataViews["JobProd"];
			if (jp != null && jp.Row >= 0 && jp.dataView != null && jp.dataView.Table != null)
			{
				string[] cols = new string[] { "RequiredQty", "BaseRequiredQty" };
				for (int i = 0; i < cols.Length; i++)
				{
					if (!jp.dataView.Table.Columns.Contains(cols[i]))
						continue;
					object v = jp.dataView[jp.Row][cols[i]];
					if (v == null || v == DBNull.Value)
						continue;
					if (TryParseDecimalFlexible(v.ToString(), out qty))
						return true;
				}
			}
			if (edvJobHead != null && edvJobHead.Row >= 0)
			{
				object v = edvJobHead.dataView[edvJobHead.Row]["ProdQty"];
				if (v != null && v != DBNull.Value && TryParseDecimalFlexible(v.ToString(), out qty))
					return true;
			}
		}
		catch { }
		return false;
	}

	private void ClearMoqViolationState() { _moqViolationHandledSig = null; }

	private static string GetCurrentJobNumSafe(EpiDataView edv)
	{
		try
		{
			if (edv == null || edv.Row < 0 || edv.dataView == null || edv.dataView.Table == null)
				return string.Empty;
			if (!edv.dataView.Table.Columns.Contains("JobNum"))
				return string.Empty;
			object j = edv.dataView[edv.Row]["JobNum"];
			if (j == null || j == DBNull.Value)
				return string.Empty;
			return j.ToString().Trim();
		}
		catch { return string.Empty; }
	}

	private static string BuildMoqViolationSignature(string jobNum, string parte, decimal qty, string kind, decimal moq)
	{
		return jobNum + "\x1e" + parte + "\x1e" + qty.ToString(CultureInfo.InvariantCulture) + "\x1e" + kind + "\x1e" + moq.ToString(CultureInfo.InvariantCulture);
	}

	private void ClearMakeToStockAndPart()
	{
		try
		{
			txtPartNum.Text = string.Empty;
			if (edvJobHead != null && edvJobHead.Row >= 0)
			{
				try { edvJobHead.dataView[edvJobHead.Row]["PartNum"] = string.Empty; }
				catch { }
			}
			EpiDataView jp = null;
			if (oTrans.EpiDataViews.ContainsKey("JobProd"))
				jp = (EpiDataView)oTrans.EpiDataViews["JobProd"];
			if (jp != null && jp.Row >= 0 && jp.dataView != null && jp.dataView.Table != null)
			{
				string[] zeroCols = new string[] { "RequiredQty", "BaseRequiredQty" };
				for (int z = 0; z < zeroCols.Length; z++)
				{
					if (jp.dataView.Table.Columns.Contains(zeroCols[z]))
					{
						try { jp.dataView[jp.Row][zeroCols[z]] = 0m; }
						catch { }
					}
				}
				string[] clearCols = new string[] { "WarehouseCode", "PartNum" };
				for (int c = 0; c < clearCols.Length; c++)
				{
					if (jp.dataView.Table.Columns.Contains(clearCols[c]))
					{
						try { jp.dataView[jp.Row][clearCols[c]] = string.Empty; }
						catch { }
					}
				}
			}
		}
		catch { }
		try { oTrans.Refresh(); }
		catch { }
	}

	public void VALIDARMOQJOB(string parte)
	{
		string currentPlant = ((Ice.Core.Session)oTrans.Session).PlantID;
		if (currentPlant != MoqPlantId)
			return;
		if (edvJobHead == null || edvJobHead.Row < 0)
			return;

		string jobNum = GetCurrentJobNumSafe(edvJobHead);
		if (jobNum.Length > 0 && _moqTrackedJobNum != jobNum)
		{
			_moqTrackedJobNum = jobNum;
			ClearMoqViolationState();
		}

		decimal cntqty;
		if (!TryGetQuantityForMoq(out cntqty))
			cntqty = 0m;

		if (cntqty == 0m)
		{
			ClearMoqViolationState();
			return;
		}

		using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
		{
			adapterDynamicQuery.BOConnect();
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("PARTE", parte, "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(MoqBaqName, ds);

			System.Data.DataTable results = adapterDynamicQuery.QueryResults.Tables["Results"];
			if (results == null || results.Rows.Count == 0)
			{
				string sigNo = BuildMoqViolationSignature(jobNum, parte, cntqty, "NOBAQ", 0m);
				if (_moqViolationHandledSig == sigNo)
					return;
				_moqViolationHandledSig = sigNo;
				MessageBox.Show("No se encontró MOQ para esta parte. Revise la parte o el BAQ.", "MOQ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				SetMoqControlTextAndColor("INGRESE LA PARTE", Color.Red);
				ClearMakeToStockAndPart();
				return;
			}

			object cell0 = results.Rows[0][0];
			string cantidadbaq = (cell0 == null) ? string.Empty : cell0.ToString();
			decimal cantmocd;
			if (!TryParseDecimalFlexible(cantidadbaq, out cantmocd))
			{
				string sigBad = BuildMoqViolationSignature(jobNum, parte, cntqty, "BADMOQ", 0m);
				if (_moqViolationHandledSig == sigBad)
					return;
				_moqViolationHandledSig = sigBad;
				MessageBox.Show("El MOQ devuelto por el BAQ no es un número válido.", "MOQ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (cntqty < 0m)
			{
				string sigNeg = BuildMoqViolationSignature(jobNum, parte, cntqty, "NEG", 0m);
				if (_moqViolationHandledSig == sigNeg)
					return;
				_moqViolationHandledSig = sigNeg;
				MessageBox.Show("La cantidad no puede ser negativa.", "MOQ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				SetMoqControlTextAndColor("INGRESE LA PARTE", Color.Red);
				ClearMakeToStockAndPart();
				return;
			}

			if (cantmocd <= cntqty)
			{
				SetMoqControlTextAndColor(cantidadbaq, Color.Green);
				ClearMoqViolationState();
			}
			else
			{
				string sig = BuildMoqViolationSignature(jobNum, parte, cntqty, "LTMOQ", cantmocd);
				if (_moqViolationHandledSig == sig)
					return;
				_moqViolationHandledSig = sig;
				MessageBox.Show("Cantidad solicitada no cumple con el MOQ. Favor de notificar al cliente y/o ventas.", "MOQ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				SetMoqControlTextAndColor(cantidadbaq, Color.Red);
				ClearMakeToStockAndPart();
			}
		}
	}

	private static bool TryParseDecimalFlexible(string s, out decimal d)
	{
		d = 0m;
		if (string.IsNullOrWhiteSpace(s))
			return false;
		s = s.Trim();
		if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
			return true;
		return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
	}

	private void SetMoqControlTextAndColor(string text, Color backColor)
	{
		try
		{
			Control c = csm.GetNativeControlReference(MoqControlEpGuid);
			if (c == null)
				return;
			EpiTextBox tb = c as EpiTextBox;
			if (tb != null) { tb.Text = text; tb.BackColor = backColor; return; }
			EpiNumericEditor ne = c as EpiNumericEditor;
			if (ne != null) { ne.Text = text; ne.BackColor = backColor; return; }
			c.Text = text;
			c.BackColor = backColor;
		}
		catch { }
	}

	private void ApplyDesvComentarioAccess()
	{
		if (!ShouldRunRuntimeUiLogic())
			return;

		string[] usuariosPermitidos = { "SValdez", "KCabrera", "Eperez" };
		try
		{
			if (desvComentario_c == null)
			{
				desvComentario_c = csm.GetNativeControlReference(DesvComentarioControlEpGuid) as Control;
				if (desvComentario_c == null && JobEntryForm != null)
					desvComentario_c = FindControlByName(JobEntryForm, DesvComentarioControlName);
			}
			if (desvComentario_c == null)
				return;

			bool permitir = usuariosPermitidos.Any(u => string.Equals(userID, u, StringComparison.OrdinalIgnoreCase));

			if (permitir)
			{
				Control parent = desvComentario_c.Parent;
				if (parent != null)
				{
					if (!parent.Visible)
						parent.Visible = true;
					try
					{
						if (parent.Height <= 1)
							parent.Height = 80;
					}
					catch { }
				}
				if (desvComentario_c.Height <= 1)
					desvComentario_c.Height = 72;
				if (desvComentario_c.Width <= 1)
					desvComentario_c.Width = 220;
			}

			if (desvComentario_c.Visible != permitir)
				desvComentario_c.Visible = permitir;
			if (desvComentario_c.Enabled != permitir)
				desvComentario_c.Enabled = permitir;

			EpiTextBox tb = desvComentario_c as EpiTextBox;
			if (tb != null)
			{
				if (permitir)
				{
					tb.Multiline = true;
					tb.AcceptsReturn = true;
					tb.AcceptsTab = false;
					tb.ScrollBars = ScrollBars.Vertical;
					if (tb.Height < 60)
						tb.Height = 72;
				}
				if (tb.ReadOnly == permitir)
					tb.ReadOnly = !permitir;
			}
		}
		catch { }
	}

	public void DestroyCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
		// Begin Wizard Added Object Disposal

		this.baseToolbarsManager.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
		if (this.edvJobHead != null)
		{
			try
			{
				if (edvJobHead.dataView != null && edvJobHead.dataView.Table != null)
					edvJobHead.dataView.Table.ColumnChanged -= JobHead_Table_ColumnChanged;
			}
			catch { }
			this.edvJobHead.EpiViewNotification -= new EpiViewNotification(this.edvJobHead_EpiViewNotification);
			this.edvJobHead = null;
		}
		this.oTrans_jobAdapter.BeforeAdapterMethod -= new BeforeAdapterMethod(this.oTrans_jobAdapter_BeforeAdapterMethod);
		this.oTrans_jobAdapter.AfterAdapterMethod -= new AfterAdapterMethod(this.oTrans_jobAdapter_AfterAdapterMethod);
		this.oTrans_jobAdapter = null;
		// End Wizard Added Object Disposal

		txtPartNum.KeyUp -= new System.Windows.Forms.KeyEventHandler(txtPartNum_KeyUp);
		txtPartNum.Leave -= new System.EventHandler(txtPartNum_Leave);
		if (edvJobProd != null)
		{
			try
			{
				if (edvJobProd.dataView != null && edvJobProd.dataView.Table != null)
					edvJobProd.dataView.Table.ColumnChanged -= JobProd_Table_ColumnChanged;
			}
			catch { }
			edvJobProd.EpiViewNotification -= new EpiViewNotification(edvJobProd_EpiViewNotification);
			edvJobProd = null;
		}
		if (_ftQtyRefreshTimer != null)
		{
			_ftQtyRefreshTimer.Stop();
			_ftQtyRefreshTimer.Dispose();
			_ftQtyRefreshTimer = null;
		}
		if (_btnSerializado != null)
		{
			_btnSerializado.Click -= btnSerializado_Click;
			_btnSerializado = null;
		}
		if (_btnOtros != null)
		{
			_btnOtros.Click -= btnOtros_Click;
			_btnOtros = null;
		}
		if (_txtOrdenDeVenta != null)
		{
			_txtOrdenDeVenta.Leave -= OrdenDeVenta_Leave;
			_txtOrdenDeVenta.Validated -= OrdenDeVenta_Leave;
			_txtOrdenDeVenta = null;
		}
	}

	private void txtPartNum_KeyUp(object sender, System.Windows.Forms.KeyEventArgs args)
	{
		MessageBox.Show("Estimado usuario, Utilice el Botón de Búsqueda para el Numero de Parte.", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);
		txtPartNum.Text = "";
	}

	private void baseToolbarsManager_ToolClick(object sender, Infragistics.Win.UltraWinToolbars.ToolClickEventArgs args)
	{
		switch (args.Tool.Key)
		{
			case "RefreshTool":
				if (edvJobHead != null && edvJobHead.Row >= 0)
				{
					string refreshJob = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
					if (!string.IsNullOrWhiteSpace(refreshJob))
						LoadFtCorridaOnJobConsult(refreshJob.Trim());
				}
				break;
			case "ClearTool":
				ResetFtCorridaForNewJob();
				_validacionRefreshGeneration++;
				_pendingValidacionPart = string.Empty;
				ApplyValidacionParteUi(string.Empty);
				ScheduleApplyHiddenJobEntryFields(0);
				break;
		}
	}

	private void consultaMatriz(string partNum, string tipo)
	{
		try
		{
			DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
			adapterDynamicQuery.BOConnect();
			string BAQName = "CODIFLEX-UD10";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("partNum_", partNum.Trim(), "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(BAQName, ds);
			DataTable table = adapterDynamicQuery.QueryResults.Tables["Results"];
			adapterDynamicQuery.Dispose();

			if (tipo == "pt")
			{
				if (table != null && table.Rows.Count > 0)
				{
					QR += "LOC: " + table.Rows[0][7].ToString() + Environment.NewLine;
					QR += "TH: " + table.Rows[0][6].ToString() + Environment.NewLine;
					QR += "Rep. EJE: " + table.Rows[0][8].ToString() + Environment.NewLine;
					QR += "Etiqueta al paso: " + table.Rows[0][3].ToString() + Environment.NewLine;
					QR += "Num Etiquetas: " + table.Rows[0][9].ToString() + Environment.NewLine;
					QR += "Diametro de Corte: " + table.Rows[0][11].ToString() + Environment.NewLine;
					QR += "Liner Izquierdo: " + table.Rows[0][4].ToString() + Environment.NewLine;
					QR += "Liner Derecho: " + table.Rows[0][5].ToString() + Environment.NewLine;
					QR += "Terminado: " + table.Rows[0][10].ToString() + Environment.NewLine;
					QR += "Medida de etiqueta Largo: " + table.Rows[0][0].ToString() + Environment.NewLine;
					QR += "Medida de etiqueta Ancho: " + table.Rows[0][1].ToString() + Environment.NewLine;
					QR += "Salida Insp.: " + table.Rows[0][12].ToString() + Environment.NewLine;
				}
			}
			else
			{
				if (table != null && table.Rows.Count > 0)
					QR += "Ancho MP Sugerido: " + table.Rows[0][2].ToString() + Environment.NewLine;
				else
					QR += "Ancho MP Sugerido: 0" + Environment.NewLine;
			}
		}
		catch (Exception error)
		{
			MessageBox.Show("Consulta UD10" + Environment.NewLine + error.Message, "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	public void estatus(EpiDataView view, string PARTE)
	{
		if (string.IsNullOrWhiteSpace(PARTE))
			return;

		string cached;
		if (JobEntryBaqCache.TryGetEstatus(PARTE, out cached))
		{
			if (cached == "CAMBIO DE INGENIERÍA")
				throw new UIException("Numero de parte en ''CAMBIO DE INGENIERIA'', comuníquese con el departamento correspondiente.");
			return;
		}

		string mensaje = "";
		DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
		adapterDynamicQuery.BOConnect();
		string BAQName = "BAQOVRES";
		Ice.BO.QueryExecutionDataSet stat = new Ice.BO.QueryExecutionDataSet();
		stat.ExecutionParameter.AddExecutionParameterRow("PARTE", PARTE, "nvarchar", false, Guid.Empty, "A");
		adapterDynamicQuery.ExecuteByID(BAQName, stat);
		if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			mensaje = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
		adapterDynamicQuery.Dispose();
		JobEntryBaqCache.SetEstatus(PARTE, mensaje);
		if (mensaje == "CAMBIO DE INGENIERÍA")
			throw new UIException("Numero de parte en ''CAMBIO DE INGENIERIA'', comuníquese con el departamento correspondiente.");
	}

	public void coonsultavlidacion(string PARTE)
	{
		coonsultavlidacion(PARTE, false);
	}

	public void coonsultavlidacion(string PARTE, bool forceReload)
	{
		if (string.IsNullOrWhiteSpace(PARTE))
		{
			ApplyValidacionParteUi(string.Empty);
			return;
		}

		PARTE = PARTE.Trim();
		string mensaje;

		if (TryGetValidacionFromPartDataView(PARTE, out mensaje))
		{
			JobEntryBaqCache.SetValidacionParte(PARTE, mensaje);
			ApplyValidacionParteUi(mensaje);
			return;
		}

		if (!forceReload && JobEntryBaqCache.TryGetValidacionParte(PARTE, out mensaje))
		{
			ApplyValidacionParteUi(mensaje);
			return;
		}

		mensaje = LoadValidacionMensajeFromBaQ(PARTE);
		JobEntryBaqCache.SetValidacionParte(PARTE, mensaje);
		ApplyValidacionParteUi(mensaje);
	}

	private bool TryGetValidacionFromPartDataView(string expectedPart, out string mensaje)
	{
		mensaje = string.Empty;
		try
		{
			if (!oTrans.EpiDataViews.ContainsKey("Part"))
				return false;

			EpiDataView partView = (EpiDataView)oTrans.EpiDataViews["Part"];
			if (partView == null || partView.Row < 0 || partView.dataView == null || partView.dataView.Table == null)
				return false;

			DataTable table = partView.dataView.Table;
			if (!table.Columns.Contains("validare_c"))
				return false;

			string loadedPart = SafeDataViewString(partView, partView.Row, "PartNum");
			if (!string.IsNullOrWhiteSpace(expectedPart)
				&& !string.Equals(loadedPart, expectedPart.Trim(), StringComparison.OrdinalIgnoreCase))
				return false;

			object value = partView.dataView[partView.Row]["validare_c"];
			if (value == null || value == DBNull.Value)
				return false;

			mensaje = IsProposedBooleanTrue(value) ? "VALIDADO" : "NO VALIDADO";
			return true;
		}
		catch
		{
			return false;
		}
	}

	private string LoadValidacionMensajeFromBaQ(string partNum)
	{
		try
		{
			DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
			adapterDynamicQuery.BOConnect();
			Ice.BO.QueryExecutionDataSet stat = new Ice.BO.QueryExecutionDataSet();
			stat.ExecutionParameter.AddExecutionParameterRow("PARTE", partNum, "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(ValidacionParteBaqName, stat);
			DataTable table = adapterDynamicQuery.QueryResults.Tables["Results"];
			adapterDynamicQuery.Dispose();
			return ResolveValidacionMensajeFromBaQ(table);
		}
		catch
		{
			return "NO VALIDADO";
		}
	}

	private static string ResolveValidacionMensajeFromBaQ(DataTable table)
	{
		if (table == null || table.Rows.Count == 0)
			return "NO VALIDADO";

		DataRow row = table.Rows[0];
		string[] preferredColumns = new string[]
		{
			"validare_c", "Validare_c", "VALIDADO", "Validado", "EstadoValidacion", "Mensaje", "Calculated_Validacion"
		};

		for (int i = 0; i < preferredColumns.Length; i++)
		{
			string colName = preferredColumns[i];
			if (!table.Columns.Contains(colName))
				continue;

			string parsed = ParseValidacionCellValue(row[colName]);
			if (!string.IsNullOrWhiteSpace(parsed))
				return parsed;
		}

		for (int colIndex = 1; colIndex >= 0; colIndex--)
		{
			if (table.Columns.Count <= colIndex)
				continue;

			string parsed = ParseValidacionCellValue(row[colIndex]);
			if (!string.IsNullOrWhiteSpace(parsed))
				return parsed;
		}

		return "NO VALIDADO";
	}

	private static string ParseValidacionCellValue(object value)
	{
		if (value == null || value == DBNull.Value)
			return string.Empty;

		if (value is bool)
			return (bool)value ? "VALIDADO" : "NO VALIDADO";

		string text = value.ToString().Trim();
		if (text.Length == 0)
			return string.Empty;

		if (IsProposedBooleanTrue(value))
			return "VALIDADO";

		if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "false", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "no", StringComparison.OrdinalIgnoreCase))
			return "NO VALIDADO";

		if (string.Equals(text, "VALIDADO", StringComparison.OrdinalIgnoreCase))
			return "VALIDADO";

		if (string.Equals(text, "NO VALIDADO", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "NO VALIDADA", StringComparison.OrdinalIgnoreCase))
			return "NO VALIDADO";

		return text;
	}

	private Control ResolveValidacionParteControl()
	{
		try
		{
			if (_validacionParteControl != null && !_validacionParteControl.IsDisposed)
				return _validacionParteControl;

			_validacionParteControl = csm.GetNativeControlReference(ValidacionParteControlEpGuid) as Control;
			if (_validacionParteControl == null)
				_validacionParteControl = FindControlByName(JobEntryForm, "validacionparte");
			if (_validacionParteControl == null)
			{
				try { _validacionParteControl = validacionparte as Control; }
				catch { }
			}
		}
		catch
		{
		}

		return _validacionParteControl;
	}

	private void ApplyValidacionParteUi(string mensaje)
	{
		Control ctrl = ResolveValidacionParteControl();
		if (ctrl == null)
			return;

		mensaje = mensaje == null ? string.Empty : mensaje.Trim();
		if (mensaje.Length == 0)
			mensaje = "NO VALIDADO";

		bool validado = string.Equals(mensaje, "VALIDADO", StringComparison.OrdinalIgnoreCase);
		Color bg = validado ? Color.LightGreen : Color.Yellow;
		string text = validado ? "VALIDADO" : mensaje;
		if (string.Equals(_lastValidacionUiText, text, StringComparison.Ordinal)
			&& _lastValidacionUiBackColor.ToArgb() == bg.ToArgb())
			return;

		ctrl.BackColor = bg;
		ctrl.Text = text;
		_lastValidacionUiText = text;
		_lastValidacionUiBackColor = bg;
	}

	private void ScheduleRefreshValidacionParte(string part, int attempt)
	{
		if (string.IsNullOrWhiteSpace(part))
		{
			ApplyValidacionParteUi(string.Empty);
			return;
		}

		part = part.Trim();
		_pendingValidacionPart = part;
		int generation = ++_validacionRefreshGeneration;

		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed)
			{
				try { coonsultavlidacion(part, false); }
				catch { }
				return;
			}

			bool forceReload = attempt > 0;

			int delayMs = attempt == 0 ? 80 : Math.Min(150 + (attempt * 100), 700);
			Timer refreshTimer = new Timer();
			refreshTimer.Interval = delayMs;
			refreshTimer.Tick += delegate(object s, EventArgs ev)
			{
				refreshTimer.Stop();
				refreshTimer.Dispose();
				if (generation != _validacionRefreshGeneration)
					return;
				if (!string.Equals(_pendingValidacionPart, part, StringComparison.OrdinalIgnoreCase))
					return;

				try
				{
					coonsultavlidacion(part, forceReload);
				}
				catch
				{
				}

				string ignored;
				if (attempt + 1 < MaxValidacionRefreshAttempts && !TryGetValidacionFromPartDataView(part, out ignored))
					ScheduleRefreshValidacionParte(part, attempt + 1);
			};
			refreshTimer.Start();
		}
		catch
		{
			try { coonsultavlidacion(part, false); }
			catch { }
		}
	}

	private void edvPart_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if (args.Row <= -1)
			return;

		if (args.NotifyType != EpiTransaction.NotifyType.Initialize
			&& args.NotifyType != EpiTransaction.NotifyType.InitLastView
			&& args.NotifyType != EpiTransaction.NotifyType.AddRow)
			return;

		if (edvJobHead == null || edvJobHead.Row < 0)
			return;

		string part = SafeDataViewString(edvJobHead, edvJobHead.Row, "PartNum");
		if (string.IsNullOrWhiteSpace(part))
			return;

		JobEntryBaqCache.InvalidateValidacionParte(part);
		ScheduleRefreshValidacionParte(part, 0);
	}

	private void oTrans_jobAdapter_BeforeAdapterMethod(object sender, BeforeAdapterMethodArgs args)
	{
		if (string.Equals(args.MethodName, "GetByID", StringComparison.OrdinalIgnoreCase))
		{
			_ftWatchGeneration++;
			ForceClearFtCorridaUi();
			_lastLoadedFtJobNum = string.Empty;
			_ftConsultJobNum = string.Empty;
			_pendingFtDisplayValue = string.Empty;
			_ftWatchExpectedJob = string.Empty;
			_ftConsultInFlightJob = string.Empty;
			_lastDeferredJobLoadKey = string.Empty;
		}

		switch (args.MethodName)
		{
			case "Update":
				TryCommitJobHeadUiEdits();
				JobSt = ((EpiDataView)(this.oTrans.EpiDataViews["JobHead"]));
				warehouse = ((EpiDataView)(this.oTrans.EpiDataViews["JobProd"]));
				if (JobSt.Row > -1)
				{
					string ordenventa = JobSt.dataView[JobSt.Row]["OrdenDeVenta_c"].ToString();
					string planta = JobSt.dataView[JobSt.Row]["Plant"].ToString();
					if (String.IsNullOrEmpty(ordenventa))
						throw new UIException("FAVOR DE INGRESAR LA ORDEN DE VENTA");

					if (warehouse.Row > -1)
					{
						string ware = warehouse.dataView[warehouse.Row]["WarehouseCode"].ToString();
						string parte = warehouse.dataView[warehouse.Row]["PartNum"].ToString();

						if (planta == "CFXCH")
						{
							if (ware == "301" || ware == "302" || ware == "303"
								|| ware == "304" || ware == "305" || ware == "311" || ware == "312" || ware == "101" || ware == "102" || ware == "103"
								|| ware == "104" || ware == "105" || ware == "111" || ware == "112")
								throw new UIException("ALMACÉN INCORRECTO, REVISE EL ALMACÉN");
						}
						if (planta == "CFXMTY")
						{
							if (ware == "1" || ware == "2" || ware == "3"
							|| ware == "4" || ware == "5" || ware == "11" || ware == "12" || ware == "101" || ware == "102" || ware == "103"
							|| ware == "104" || ware == "105" || ware == "111" || ware == "112" || ware == "203" || ware == "204" || ware == "205"
							|| ware == "211" || ware == "212" || ware == "402" || ware == "403" || ware == "404" || ware == "405" || ware == "411" || ware == "412")
								throw new UIException("ALMACÉN INCORRECTO, REVISE EL ALMACÉN");
						}
						if (planta == "CFXJZ")
						{
							if (ware == "1" || ware == "2" || ware == "3"
							|| ware == "4" || ware == "5" || ware == "11" || ware == "12" || ware == "301" || ware == "302" || ware == "303"
							|| ware == "304" || ware == "305" || ware == "311" || ware == "312" || ware == "203" || ware == "204" || ware == "205"
							|| ware == "211" || ware == "212" || ware == "402" || ware == "403" || ware == "404" || ware == "405" || ware == "411" || ware == "412")
								throw new UIException("ALMACÉN INCORRECTO, REVISE EL ALMACÉN");
						}
					}
				}
				break;
		}
	}

	private void SetOrdenCompraField(EpiDataView view, string po)
	{
		if (view == null || view.Row < 0 || string.IsNullOrWhiteSpace(po))
			return;

		po = po.Trim();
		try
		{
			if (view.dataView != null && view.dataView.Table != null
				&& view.dataView.Table.Columns.Contains("orden_compra_c"))
			{
				view.dataView[view.Row]["orden_compra_c"] = po;
				view.dataView[view.Row].EndEdit();
			}

			Control c = csm.GetNativeControlReference(OrdenCompraControlEpGuid);
			if (c != null)
				c.Text = po;
		}
		catch
		{
		}
	}

	private void TryPopulateOrdenCompraFromOv(EpiDataView view, int row)
	{
		if (view == null || row < 0)
			return;

		string ov = SafeDataViewString(view, row, "OrdenDeVenta_c");
		string occ = SafeDataViewString(view, row, "orden_compra_c");
		if (!string.IsNullOrWhiteSpace(ov) && string.IsNullOrWhiteSpace(occ))
			consultaOP(view, ov);
	}

	private void consultaOP(EpiDataView view, string ov)
	{
		if (string.IsNullOrWhiteSpace(ov) || view == null || view.Row < 0)
			return;

		ov = ov.Trim();

		string cachedPo;
		if (JobEntryBaqCache.TryGetPoFromOv(ov, out cachedPo) && !string.IsNullOrWhiteSpace(cachedPo))
		{
			SetOrdenCompraField(view, cachedPo);
			return;
		}

		string po = string.Empty;
		try
		{
			using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
			{
				adapterDynamicQuery.BOConnect();
				string BAQName = "BAQPO";
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("OV", ov, "nvarchar", false, Guid.Empty, "A");
				adapterDynamicQuery.ExecuteByID(BAQName, ds);
				DataTable results = adapterDynamicQuery.QueryResults.Tables["Results"];
				if (results != null && results.Rows.Count > 0 && results.Rows[0][0] != DBNull.Value)
					po = results.Rows[0][0].ToString().Trim();
			}
		}
		catch
		{
		}

		JobEntryBaqCache.SetPoFromOv(ov, po);
		if (!string.IsNullOrWhiteSpace(po))
			SetOrdenCompraField(view, po);
	}

	private void validacionmuestraparte(string parte)
	{
		if (string.IsNullOrWhiteSpace(parte))
			return;

		string muestra;
		if (JobEntryBaqCache.TryGetMuestraParte(parte, out muestra))
		{
			ApplyMuestraParteUi(muestra);
			return;
		}

		DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
		adapterDynamicQuery.BOConnect();
		string BAQName = "BAQCONSULTAVALIDACIONMUESTRA";
		Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
		ds.ExecutionParameter.AddExecutionParameterRow("PARTE", parte, "nvarchar", false, Guid.Empty, "A");
		adapterDynamicQuery.ExecuteByID(BAQName, ds);
		muestra = " ";
		if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			muestra = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
		adapterDynamicQuery.Dispose();
		JobEntryBaqCache.SetMuestraParte(parte, muestra);
		ApplyMuestraParteUi(muestra);
	}

	private void ApplyMuestraParteUi(string muestra)
	{
		if (muestra == "MUESTRA")
		{
			Muestravaliadc.Text = muestra;
			Muestravaliadc.BackColor = Color.Yellow;
		}
		else
			Muestravaliadc.Text = muestra;
	}

	private static string SafeDataViewString(EpiDataView view, int row, string column)
	{
		try
		{
			if (view == null || row < 0 || view.dataView == null || view.dataView.Table == null)
				return string.Empty;
			if (!view.dataView.Table.Columns.Contains(column))
				return string.Empty;
			object v = view.dataView[row][column];
			return v == null || v == DBNull.Value ? string.Empty : v.ToString().Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private void SetFtCorridaField(string value)
	{
		_pendingFtDisplayValue = value == null ? string.Empty : value.Trim();
		if (edvJobHead != null && edvJobHead.Row >= 0)
		{
			string jobNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
			if (!string.IsNullOrWhiteSpace(jobNum))
				_ftConsultJobNum = jobNum.Trim();
		}
		if (TryApplyFtCorridaField(_pendingFtDisplayValue))
			return;

		ScheduleApplyFtCorridaField(0);
	}

	private void ScheduleApplyFtCorridaField(int attempt)
	{
		if (attempt < 0 || attempt >= MaxFtFieldApplyAttempts)
			return;

		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed)
				return;

			MethodInvoker work = delegate
			{
				try
				{
					if (TryApplyFtCorridaField(_pendingFtDisplayValue))
						return;
					if (attempt + 1 < MaxFtFieldApplyAttempts)
						ScheduleApplyFtCorridaField(attempt + 1);
				}
				catch
				{
				}
			};

			if (!host.IsHandleCreated)
				return;

			if (attempt == 0)
			{
				host.BeginInvoke(work);
				return;
			}

			Timer retryTimer = new Timer();
			retryTimer.Interval = Math.Min(80 + (attempt * 60), 600);
			retryTimer.Tick += delegate(object s, EventArgs ev)
			{
				retryTimer.Stop();
				retryTimer.Dispose();
				if (host.IsDisposed)
					return;
				host.BeginInvoke(work);
			};
			retryTimer.Start();
		}
		catch
		{
		}
	}

	private bool TryApplyFtCorridaField(string text)
	{
		try
		{
			Control c = GetFtCorridaControl();
			if (c == null)
				return false;

			EpiNumericEditor ne = c as EpiNumericEditor;
			if (ne != null)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					ne.Value = null;
					ne.Text = string.Empty;
				}
				else
				{
					decimal d;
					if (TryParseDecimalFlexible(text, out d))
					{
						ne.Value = d;
						ne.Text = FormatFtDisplay(d);
					}
					else
						ne.Text = text;
				}
				ne.Refresh();
				return true;
			}

			c.Text = text ?? string.Empty;
			c.Refresh();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsStaleFtCacheValue(string value)
	{
		return string.IsNullOrWhiteSpace(value);
	}

	private string QueryFtBaQ(string jobNum)
	{
		return QueryFtBaQ(jobNum, false);
	}

	private string QueryFtBaQ(string jobNum, bool skipCache)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return string.Empty;

		jobNum = jobNum.Trim();

		if (!skipCache)
		{
			string cachedFt;
			if (JobEntryBaqCache.TryGetFtBaQ(jobNum, out cachedFt) && !IsStaleFtCacheValue(cachedFt))
				return cachedFt;
		}

		string ftValue = ExecuteFtBaQQuery(jobNum, "JOBNUM");
		if (!IsStaleFtCacheValue(ftValue))
		{
			JobEntryBaqCache.SetFtBaQ(jobNum, ftValue);
			return ftValue;
		}

		string altKey = jobNum.TrimStart('0');
		if (altKey.Length > 0 && !string.Equals(altKey, jobNum, StringComparison.Ordinal))
		{
			ftValue = ExecuteFtBaQQuery(altKey, "JOBNUM");
			if (!IsStaleFtCacheValue(ftValue))
			{
				JobEntryBaqCache.SetFtBaQ(jobNum, ftValue);
				return ftValue;
			}
		}

		ftValue = ExecuteFtBaQQuery(jobNum, "JobNum");
		if (!IsStaleFtCacheValue(ftValue))
			JobEntryBaqCache.SetFtBaQ(jobNum, ftValue);

		return ftValue;
	}

	private string ExecuteFtBaQQuery(string jobKey, string paramName)
	{
		try
		{
			using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
			{
				adapterDynamicQuery.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow(paramName, jobKey, "nvarchar", false, Guid.Empty, "A");
				adapterDynamicQuery.ExecuteByID(FtCorridaBaqName, ds);
				DataTable results = adapterDynamicQuery.QueryResults.Tables["Results"];
				if (results == null || results.Rows.Count == 0)
					return string.Empty;
				object cell = results.Rows[0][0];
				if (cell == null || cell == DBNull.Value)
					return string.Empty;
				string ftText = cell.ToString().Trim();
				if (ftText.Length == 0 || string.Equals(ftText, ".", StringComparison.Ordinal))
					return string.Empty;
				return ftText;
			}
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string ParseFtBaQResults(DataTable results, string jobKey)
	{
		if (results == null || results.Rows.Count == 0)
			return string.Empty;

		DataRow row = results.Rows[0];
		string[] columnHints = new string[]
		{
			"FTCorrida", "Calculated_FTCorrida", "TotalFT", "TotalFt", "FT", "Ft", "Number12"
		};
		for (int h = 0; h < columnHints.Length; h++)
		{
			for (int c = 0; c < results.Columns.Count; c++)
			{
				string colName = results.Columns[c].ColumnName ?? string.Empty;
				if (colName.IndexOf(columnHints[h], StringComparison.OrdinalIgnoreCase) < 0)
					continue;
				string parsed = ParseFtBaQCellValue(row[c], jobKey);
				if (!string.IsNullOrWhiteSpace(parsed))
					return parsed;
			}
		}

		for (int c = 0; c < results.Columns.Count; c++)
		{
			string parsed = ParseFtBaQCellValue(row[c], jobKey);
			if (!string.IsNullOrWhiteSpace(parsed))
				return parsed;
		}
		return string.Empty;
	}

	private static string ParseFtBaQCellValue(object cell, string jobNum)
	{
		if (cell == null || cell == DBNull.Value)
			return string.Empty;

		string parsed = cell.ToString().Trim();
		if (parsed.Length == 0 || string.Equals(parsed, ".", StringComparison.Ordinal))
			return string.Empty;

		if (!string.IsNullOrWhiteSpace(jobNum)
			&& string.Equals(parsed, jobNum.Trim(), StringComparison.OrdinalIgnoreCase))
			return string.Empty;

		decimal ftValue;
		if (TryParseDecimalFlexible(parsed, out ftValue) && ftValue > 0m)
			return ftValue.ToString("0.0000", CultureInfo.InvariantCulture);

		return string.Empty;
	}

	private void TryLoadFtCorrida(EpiDataView view, int row, bool forceReload)
	{
		if (view == null || row < 0)
			return;

		string jobNum = SafeDataViewString(view, row, "JobNum");
		if (string.IsNullOrWhiteSpace(jobNum))
		{
			_lastLoadedFtJobNum = string.Empty;
			SetFtCorridaField(string.Empty);
			return;
		}

		bool sameJob = string.Equals(_lastLoadedFtJobNum, jobNum, StringComparison.OrdinalIgnoreCase);
		string ftValue = string.Empty;

		if (!forceReload && sameJob
			&& JobEntryBaqCache.TryGetFtBaQ(jobNum, out ftValue)
			&& !IsStaleFtCacheValue(ftValue))
		{
			SetFtCorridaField(ftValue);
			return;
		}

		if (!forceReload
			&& JobEntryBaqCache.TryGetFtBaQ(jobNum, out ftValue)
			&& !IsStaleFtCacheValue(ftValue))
		{
			SetFtCorridaField(ftValue);
			_lastLoadedFtJobNum = jobNum;
			return;
		}

		ftValue = QueryFtBaQ(jobNum);
		if (!IsStaleFtCacheValue(ftValue))
		{
			JobEntryBaqCache.SetFtBaQ(jobNum, ftValue);
			SetFtCorridaField(ftValue);
			_lastLoadedFtJobNum = jobNum;
		}
		else
			SetFtCorridaField(string.Empty);
	}

	private void ScheduleDeferredFtLoad(EpiDataView view, int row)
	{
		ScheduleDeferredFtLoad(view, row, 0);
	}

	private void ScheduleDeferredFtLoad(EpiDataView view, int row, int attempt)
	{
		if (view == null || row < 0 || attempt < 0 || attempt >= MaxDeferredFtAttempts)
			return;

		if (attempt == 0)
			_deferredFtGeneration++;

		int generation = _deferredFtGeneration;

		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed)
				return;

			MethodInvoker work = delegate
			{
				try
				{
					if (generation != _deferredFtGeneration)
						return;
					if (edvJobHead == null || edvJobHead.Row < 0)
						return;

					string readyJobNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
					if (string.IsNullOrWhiteSpace(readyJobNum))
					{
						ScheduleDeferredFtLoad(edvJobHead, edvJobHead.Row, attempt + 1);
						return;
					}

					decimal gateQty;
					if (!IsFtCalculationReady(out gateQty))
					{
						if (attempt + 1 < MaxDeferredFtAttempts)
							ScheduleDeferredFtLoad(edvJobHead, edvJobHead.Row, attempt + 1);
						return;
					}

					TryLoadFtCorrida(edvJobHead, edvJobHead.Row, attempt > 0);

					if (!HasFtCorridaDisplayValue())
						TryCalculateAndSetFtCorrida();

					if (!HasFtCorridaDisplayValue())
						TryApplyFtCorridaField(_pendingFtDisplayValue);

					if (HasFtCorridaDisplayValue())
						return;

					if (attempt + 1 < MaxDeferredFtAttempts)
						ScheduleDeferredFtLoad(edvJobHead, edvJobHead.Row, attempt + 1);
				}
				catch
				{
				}
			};

			if (!host.IsHandleCreated)
				return;

			if (attempt == 0)
			{
				host.BeginInvoke(work);
				return;
			}

			Timer retryTimer = new Timer();
			retryTimer.Interval = Math.Min(120 + (attempt * 60), 500);
			retryTimer.Tick += delegate(object s, EventArgs ev)
			{
				retryTimer.Stop();
				retryTimer.Dispose();
				if (host.IsDisposed || generation != _deferredFtGeneration)
					return;
				host.BeginInvoke(work);
			};
			retryTimer.Start();
		}
		catch
		{
		}
	}

	private static bool IsJobLoadAdapterMethod(string method)
	{
		return string.Equals(method, "GetByID", StringComparison.OrdinalIgnoreCase);
	}

	private void oTrans_jobAdapter_AfterAdapterMethod(object sender, AfterAdapterMethodArgs args)
	{
		if (!ShouldRunRuntimeUiLogic())
			return;

		string method = args.MethodName == null ? string.Empty : args.MethodName.Trim();
		bool isJobLoad = IsJobLoadAdapterMethod(method);
		bool isUpdate = string.Equals(method, "Update", StringComparison.OrdinalIgnoreCase);
		if (!isJobLoad && !isUpdate)
			return;

		try
		{
			if (edvJobHead == null || edvJobHead.Row < 0)
				return;
			_lastDeferredJobLoadKey = string.Empty;
			RehideKnownJobEntryFields();
			EnsureProtectedJobEntryFieldsVisible();

			string jobNum = SafeDataViewString(edvJobHead, edvJobHead.Row, "JobNum");
			if (string.IsNullOrWhiteSpace(jobNum))
			{
				ResetFtCorridaForNewJob();
			}
			else if (isJobLoad)
			{
				RequestFtConsultForJob(jobNum.Trim());
			}
			else if (isUpdate)
			{
				ScheduleCalculateFtOnSave(0);
			}

			ScheduleApplyHiddenJobEntryFields(0);

			if (isJobLoad)
			{
				string part = SafeDataViewString(edvJobHead, edvJobHead.Row, "PartNum");
				if (!string.IsNullOrWhiteSpace(part))
				{
					JobEntryBaqCache.InvalidateValidacionParte(part);
					ScheduleRefreshValidacionParte(part, 1);
				}
			}
		}
		catch
		{
		}
	}

	private void ScheduleDeferredJobHeadLoad(EpiDataView view, int row)
	{
		if (view == null || row < 0)
			return;

		string part = SafeDataViewString(view, row, "PartNum");
		string jobNum = SafeDataViewString(view, row, "JobNum");
		string key = jobNum + "\x1e" + part;
		if (!string.IsNullOrWhiteSpace(jobNum)
			&& string.Equals(_lastDeferredJobLoadKey, key, StringComparison.Ordinal))
			return;

		_pendingDeferredJobKey = key + "\x1e" + row.ToString(CultureInfo.InvariantCulture);
		_pendingDeferredRow = row;

		try
		{
			Form host = JobEntryForm;
			if (host == null || host.IsDisposed)
				return;

			MethodInvoker work = delegate
			{
				try
				{
					if (edvJobHead == null || edvJobHead.Row < 0)
						return;
					if (!string.Equals(_pendingDeferredJobKey, key, StringComparison.Ordinal))
						return;
					ExecuteDeferredJobHeadLoad(edvJobHead, edvJobHead.Row);
				}
				catch (Exception ex)
				{
					JobEntryForm.Cursor = Cursors.Default;
					ExceptionBox.Show(ex);
				}
			};

			if (host.IsHandleCreated)
				host.BeginInvoke(work);
			else
			{
				EventHandler onLoad = null;
				onLoad = delegate(object s, EventArgs ev)
				{
					host.Load -= onLoad;
					if (host.IsDisposed)
						return;
					host.BeginInvoke(work);
				};
				host.Load += onLoad;
			}
		}
		catch
		{
		}
	}

	private void ExecuteDeferredJobHeadLoad(EpiDataView view, int row)
	{
		string part = SafeDataViewString(view, row, "PartNum");
		string jobnum = SafeDataViewString(view, row, "JobNum");
		string loadKey = jobnum + "\x1e" + part;
		if (!string.IsNullOrWhiteSpace(jobnum)
			&& string.Equals(_lastDeferredJobLoadKey, loadKey, StringComparison.Ordinal))
			return;

		_lastDeferredJobLoadKey = loadKey;
		SuppressFtClearDuringJobLoad();

		ApplyHiddenJobEntryFields();

		try
		{
			estatus(view, part);
		}
		catch (Exception ex)
		{
			ExceptionBox.Show(ex);
		}

		try { validacionmuestraparte(part); } catch { }
		try { TryPopulateOrdenCompraFromOv(view, row); } catch { }
		try { coonsultavlidacion(part); } catch { }
		try { VALIDARMOQJOB(part); } catch { }

		ScheduleApplyHiddenJobEntryFields(0);
	}

	private void edvJobHead_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if (args.Row <= -1)
			return;

		if (!ShouldRunRuntimeUiLogic())
			return;

		if (args.NotifyType == EpiTransaction.NotifyType.Initialize)
		{
			string initJobNum = SafeDataViewString(view, args.Row, "JobNum");
			if (string.IsNullOrWhiteSpace(initJobNum))
				ResetFtCorridaForNewJob();
			else
				RequestFtConsultForJob(initJobNum);

			WireJobHeadColumnChanged();
			WireJobProdColumnChanged();
			WireOrdenVentaLeave();
			WireQuantityLeaveEvents();
			ScheduleWireQuantityLeave(0);
			ApplyHiddenJobEntryFields();
			ScheduleDeferredJobHeadLoad(view, args.Row);
			ScheduleApplyHiddenJobEntryFields(0);
			return;
		}

		if (args.NotifyType == EpiTransaction.NotifyType.AddRow)
		{
			ResetFtCorridaForNewJob();
			return;
		}

		if (args.NotifyType == EpiTransaction.NotifyType.InitLastView)
		{
			_lastDeferredJobLoadKey = string.Empty;
			TryPopulateOrdenCompraFromOv(view, args.Row);
			WireJobProdColumnChanged();
			WireQuantityLeaveEvents();
			ScheduleWireQuantityLeave(0);

			string loadJobNum = SafeDataViewString(view, args.Row, "JobNum");
			if (string.IsNullOrWhiteSpace(loadJobNum))
				ResetFtCorridaForNewJob();
			else
				RequestFtConsultForJob(loadJobNum);

			ScheduleDeferredJobHeadLoad(view, args.Row);
			RehideKnownJobEntryFields();
			EnsureProtectedJobEntryFieldsVisible();
			ScheduleApplyHiddenJobEntryFields(0);
			return;
		}

		string headCol = GetEpiNotifyColumnName(view, args);
	}
}

public static class JobEntryBaqCache
{
	private static readonly Dictionary<string, string> EstatusByPart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> ValidacionByPart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> MuestraByPart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> PoByOv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> FtByJob = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, decimal> FtPerMillByPart = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, Ud10CacheEntry> Ud10ByPart =
		new Dictionary<string, Ud10CacheEntry>(StringComparer.OrdinalIgnoreCase);

	private static string NormalizeKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return string.Empty;
		return value.Trim();
	}

	public static bool TryGetEstatus(string part, out string value)
	{
		return TryGet(EstatusByPart, part, out value);
	}

	public static void SetEstatus(string part, string value)
	{
		Set(EstatusByPart, part, value);
	}

	public static bool TryGetValidacionParte(string part, out string value)
	{
		return TryGet(ValidacionByPart, part, out value);
	}

	public static void SetValidacionParte(string part, string value)
	{
		Set(ValidacionByPart, part, value);
	}

	public static void InvalidateValidacionParte(string part)
	{
		part = NormalizeKey(part);
		if (part.Length == 0)
			return;
		ValidacionByPart.Remove(part);
	}

	public static bool TryGetMuestraParte(string part, out string value)
	{
		return TryGet(MuestraByPart, part, out value);
	}

	public static void SetMuestraParte(string part, string value)
	{
		Set(MuestraByPart, part, value);
	}

	public static bool TryGetPoFromOv(string ov, out string value)
	{
		return TryGet(PoByOv, ov, out value);
	}

	public static void SetPoFromOv(string ov, string value)
	{
		Set(PoByOv, ov, value);
	}

	public static bool TryGetFtBaQ(string jobNum, out string value)
	{
		return TryGet(FtByJob, jobNum, out value);
	}

	public static void SetFtBaQ(string jobNum, string value)
	{
		Set(FtByJob, jobNum, value);
	}

	public static void InvalidateFtBaQ(string jobNum)
	{
		jobNum = NormalizeKey(jobNum);
		if (jobNum.Length == 0)
			return;
		FtByJob.Remove(jobNum);
	}

	public static void InvalidateFtPerMill(string partNum)
	{
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0)
			return;
		FtPerMillByPart.Remove(partNum);
	}

	public static bool TryGetFtPerMill(string partNum, out decimal value)
	{
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0)
		{
			value = 0m;
			return false;
		}
		return FtPerMillByPart.TryGetValue(partNum, out value);
	}

	public static void SetFtPerMill(string partNum, decimal value)
	{
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0 || value <= 0m)
			return;
		FtPerMillByPart[partNum] = value;
	}

	public static bool TryGetUd10Matrix(string partNum, out int labelsPerRoll, out string salidaInsp)
	{
		labelsPerRoll = 0;
		salidaInsp = string.Empty;
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0)
			return false;

		Ud10CacheEntry entry;
		if (!Ud10ByPart.TryGetValue(partNum, out entry))
			return false;

		labelsPerRoll = entry.LabelsPerRoll;
		salidaInsp = entry.SalidaInsp;
		return true;
	}

	public static void SetUd10Matrix(string partNum, int labelsPerRoll, string salidaInsp)
	{
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0)
			return;

		Ud10CacheEntry entry = new Ud10CacheEntry();
		entry.LabelsPerRoll = labelsPerRoll;
		entry.SalidaInsp = salidaInsp == null ? string.Empty : salidaInsp;
		Ud10ByPart[partNum] = entry;
	}

	private sealed class Ud10CacheEntry
	{
		public int LabelsPerRoll;
		public string SalidaInsp = string.Empty;
	}

	private static bool TryGet(Dictionary<string, string> map, string key, out string value)
	{
		key = NormalizeKey(key);
		if (key.Length == 0)
		{
			value = string.Empty;
			return false;
		}
		return map.TryGetValue(key, out value);
	}

	private static void Set(Dictionary<string, string> map, string key, string value)
	{
		key = NormalizeKey(key);
		if (key.Length == 0)
			return;
		map[key] = value == null ? string.Empty : value;
	}
}

// =============================================================================
// FORMULARIO SERIALIZADO — Consecutivo de folios + cálculo de rollos
// =============================================================================

public sealed class SerializadoJobData
{
	public string JobNum = string.Empty;
	public string PartNum = string.Empty;
	public string PartDescription = string.Empty;
	public string PurchaseOrder = string.Empty;
	public string SalesOrder = string.Empty;
	public string Cliente = string.Empty;
	public string SerialPrefix = string.Empty;
	public string Salida = "1";
	public decimal TotalQty;
	public int LabelsPerRoll = 1000;
	public long SerialFrom;
}

public sealed class SerializadoRollRow
{
	public int RollNum;
	public string Prefix = string.Empty;
	public long SerialFrom;
	public long SerialTo;
	public int Quantity;
	public string Defectos = string.Empty;
	public string SerialFromDisplay { get { return Prefix + SerialFrom.ToString(); } }
	public string SerialToDisplay { get { return Prefix + SerialTo.ToString(); } }
}

public static class SerializadoCalculator
{
	public const int DefaultLabelsPerRoll = 1000;

	public static long ParseSerialNumber(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return 0L;
		StringBuilder digits = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
			if (char.IsDigit(input[i]))
				digits.Append(input[i]);
		long value;
		return long.TryParse(digits.ToString(), out value) ? value : 0L;
	}

	public static string ExtractPrefix(string input, string fallback)
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

	public static long CalculateSerialTo(long serialFrom, int quantity)
	{
		return serialFrom + (quantity - 1);
	}

	public static List<int> SplitIntoRollCapacities(int totalQty, int labelsPerRoll)
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

	public static List<SerializadoRollRow> BuildRolls(string prefix, long globalFrom, List<int> rollQuantities)
	{
		List<SerializadoRollRow> rolls = new List<SerializadoRollRow>();
		long current = globalFrom;
		for (int i = 0; i < rollQuantities.Count; i++)
		{
			int qty = rollQuantities[i];
			if (qty <= 0)
				continue;
			long to = CalculateSerialTo(current, qty);
			rolls.Add(new SerializadoRollRow
			{
				RollNum = rolls.Count + 1,
				Prefix = prefix,
				SerialFrom = current,
				SerialTo = to,
				Quantity = qty
			});
			current = to + 1L;
		}
		return rolls;
	}

	public static List<SerializadoRollRow> BuildRollsByCapacity(string prefix, long serialFrom, int totalQty, int labelsPerRoll)
	{
		return BuildRolls(prefix, serialFrom, SplitIntoRollCapacities(totalQty, labelsPerRoll));
	}
}

public static class SerializadoBaqService
{
	public static string GetPoFromOv(object oTrans, string ov)
	{
		if (string.IsNullOrWhiteSpace(ov))
			return string.Empty;

		string cached;
		if (JobEntryBaqCache.TryGetPoFromOv(ov, out cached))
			return cached;

		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("OV", ov.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID("BAQPO", ds);
				DataTable t = dq.QueryResults.Tables["Results"];
				if (t != null && t.Rows.Count > 0 && t.Rows[0][0] != DBNull.Value)
				{
					string po = t.Rows[0][0].ToString().Trim();
					JobEntryBaqCache.SetPoFromOv(ov, po);
					return po;
				}
			}
		}
		catch { }
		JobEntryBaqCache.SetPoFromOv(ov, string.Empty);
		return string.Empty;
	}

	public static void LoadUd10Matrix(object oTrans, string partNum, out int labelsPerRoll, out string salidaInsp)
	{
		labelsPerRoll = SerializadoCalculator.DefaultLabelsPerRoll;
		salidaInsp = "1";
		if (string.IsNullOrWhiteSpace(partNum))
			return;

		int cachedLabels;
		string cachedSalida;
		if (JobEntryBaqCache.TryGetUd10Matrix(partNum, out cachedLabels, out cachedSalida))
		{
			if (cachedLabels > 0)
				labelsPerRoll = cachedLabels;
			if (!string.IsNullOrWhiteSpace(cachedSalida))
				salidaInsp = cachedSalida;
			return;
		}

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
				{
					JobEntryBaqCache.SetUd10Matrix(partNum, labelsPerRoll, salidaInsp);
					return;
				}
				DataRow row = t.Rows[0];

				int number16 = ReadBestNumber16FromBaq(row, t);
				if (number16 > 0)
					labelsPerRoll = number16;
				else
				{
					int numEtiquetas = ReadIntFromUd10Table(row, t, new string[] { "NumEtiquetas", "Num_Etiquetas" }, 9);
					if (numEtiquetas > 0)
						labelsPerRoll = numEtiquetas;
				}

				string salida = ReadStringFromUd10Table(row, t, new string[] { "ShortChar06", "SalidaInsp", "Salida_Insp", "Salida" }, 12);
				if (!string.IsNullOrWhiteSpace(salida))
					salidaInsp = salida;
			}
		}
		catch { }

		JobEntryBaqCache.SetUd10Matrix(partNum, labelsPerRoll, salidaInsp);
	}

	private static int ReadBestNumber16FromBaq(DataRow row, DataTable table)
	{
		int named = ReadIntFromUd10Table(row, table, new string[] { "Number16", "UD10_Number16", "CantUE", "Cant_UE", "Number_16" }, -1);
		if (named > 0)
			return named;

		int col9 = ReadIntFromUd10Table(row, table, new string[] { "NumEtiquetas", "Num_Etiquetas" }, 9);
		int maxNumberCol = 0;
		for (int c = 0; c < table.Columns.Count; c++)
		{
			string colName = table.Columns[c].ColumnName ?? string.Empty;
			if (colName.IndexOf("Number", StringComparison.OrdinalIgnoreCase) < 0)
				continue;
			int parsed = TryParsePositiveInt(row[c]);
			if (parsed > maxNumberCol)
				maxNumberCol = parsed;
		}
		if (maxNumberCol > 0)
			return maxNumberCol;

		for (int c = 10; c < table.Columns.Count && c <= 25; c++)
		{
			int parsed = TryParsePositiveInt(row[c]);
			if (parsed >= 100 && parsed <= 50000 && parsed > col9)
				return parsed;
		}

		if (col9 > 0)
			return col9;
		return ReadIntFromUd10Table(row, table, new string[] { }, 16);
	}

	private static int ReadIntFromUd10Table(DataRow row, DataTable table, string[] columnHints, int fallbackIndex)
	{
		for (int c = 0; c < table.Columns.Count; c++)
		{
			string colName = table.Columns[c].ColumnName ?? string.Empty;
			for (int h = 0; h < columnHints.Length; h++)
			{
				if (colName.IndexOf(columnHints[h], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					int parsed = TryParsePositiveInt(row[c]);
					if (parsed > 0)
						return parsed;
				}
			}
		}
		if (fallbackIndex >= 0 && fallbackIndex < table.Columns.Count)
		{
			int parsed = TryParsePositiveInt(row[fallbackIndex]);
			if (parsed > 0)
				return parsed;
		}
		return 0;
	}

	private static string ReadStringFromUd10Table(DataRow row, DataTable table, string[] columnHints, int fallbackIndex)
	{
		for (int c = 0; c < table.Columns.Count; c++)
		{
			string colName = table.Columns[c].ColumnName ?? string.Empty;
			for (int h = 0; h < columnHints.Length; h++)
			{
				if (colName.IndexOf(columnHints[h], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					string s = Convert.ToString(row[c]);
					if (!string.IsNullOrWhiteSpace(s))
						return s.Trim();
				}
			}
		}
		if (fallbackIndex >= 0 && fallbackIndex < table.Columns.Count)
		{
			string s = Convert.ToString(row[fallbackIndex]);
			if (!string.IsNullOrWhiteSpace(s))
				return s.Trim();
		}
		return string.Empty;
	}

	private static int TryParsePositiveInt(object value)
	{
		if (value == null || value == DBNull.Value)
			return 0;
		decimal d;
		if (decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.CurrentCulture, out d))
		{
			int i = (int)Math.Round(d, 0);
			return i > 0 ? i : 0;
		}
		return 0;
	}

	public static decimal GetJobQuantity(EpiDataView edvJobHead, EpiDataView edvJobProd)
	{
		decimal raw = 0m;
		string uom = GetField(edvJobHead, "IUM");
		try
		{
			if (edvJobProd != null && edvJobProd.Row >= 0 && edvJobProd.dataView != null)
			{
				if (string.IsNullOrWhiteSpace(uom) && edvJobProd.dataView.Table.Columns.Contains("IUM"))
					uom = GetField(edvJobProd, "IUM");

				string[] cols = new string[] { "RequiredQty", "BaseRequiredQty" };
				for (int i = 0; i < cols.Length; i++)
				{
					if (!edvJobProd.dataView.Table.Columns.Contains(cols[i]))
						continue;
					object v = edvJobProd.dataView[edvJobProd.Row][cols[i]];
					if (v != null && v != DBNull.Value)
					{
						decimal d;
						if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out d) && d > 0m)
						{
							raw = d;
							break;
						}
					}
				}
			}
			if (raw <= 0m && edvJobHead != null && edvJobHead.Row >= 0)
			{
				object v = edvJobHead.dataView[edvJobHead.Row]["ProdQty"];
				if (v != null && v != DBNull.Value)
				{
					decimal d;
					if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out d))
						raw = d;
				}
			}
		}
		catch { }
		return NormalizeQuantity(raw, uom);
	}

	private static decimal NormalizeQuantity(decimal qty, string uom)
	{
		if (string.IsNullOrWhiteSpace(uom))
			return qty;
		string u = uom.Trim().ToUpperInvariant();
		if (u == "MIL")
			return qty * 1000m;
		return qty;
	}

	public static string GetField(EpiDataView view, string column)
	{
		try
		{
			if (view == null || view.Row < 0 || view.dataView == null)
				return string.Empty;
			if (!view.dataView.Table.Columns.Contains(column))
				return string.Empty;
			object v = view.dataView[view.Row][column];
			return v == null || v == DBNull.Value ? string.Empty : v.ToString().Trim();
		}
		catch { return string.Empty; }
	}
}

public delegate bool SerializadoPersistHandler(out string error);

public enum SerializadoFormMode
{
	BrkSerializado,
	Otros
}

public static class SerializadoSaveService
{
	public static bool TrySave(EpiDataView edvJobHead, SerializadoJobData data, List<SerializadoRollRow> rolls,
		SerializadoPersistHandler persist, string userId, out string error)
	{
		return TrySave(edvJobHead, data, rolls, persist, userId, true, out error);
	}

	public static bool TrySave(EpiDataView edvJobHead, SerializadoJobData data, List<SerializadoRollRow> rolls,
		SerializadoPersistHandler persist, string userId, bool requirePrefix, out string error)
	{
		error = string.Empty;
		if (edvJobHead == null || edvJobHead.Row < 0)
		{
			error = "No hay registro JobHead activo.";
			return false;
		}
		if (data == null || string.IsNullOrWhiteSpace(data.JobNum))
		{
			error = "El Job no tiene número.";
			return false;
		}
		if (rolls == null || rolls.Count == 0)
		{
			error = "Calcule los rollos antes de guardar en Epicor.";
			return false;
		}
		if (persist == null)
		{
			error = "No hay método de guardado configurado en Job Entry.";
			return false;
		}

		int totalQty = (int)Math.Round(data.TotalQty, 0);
		if (totalQty <= 0)
		{
			error = "La cantidad total debe ser mayor que cero.";
			return false;
		}

		string prefix = data.SerialPrefix == null ? string.Empty : data.SerialPrefix.Trim();
		if (prefix.Length == 0 && requirePrefix)
		{
			error = "Ingrese el prefijo serial (2 caracteres) antes de guardar en Epicor.";
			return false;
		}
		// UD: SerialPrefix_c = prefijo | SerialFrom_c / SerialTo_c = solo dígitos (sin prefijo)
		string serialFromUd = rolls[0].SerialFrom.ToString(CultureInfo.InvariantCulture);
		string serialToUd = rolls[rolls.Count - 1].SerialTo.ToString(CultureInfo.InvariantCulture);

		string lengthError = ValidateUdStringLength(edvJobHead, "SerialPrefix_c", prefix);
		if (lengthError != null) { error = lengthError; return false; }
		lengthError = ValidateUdStringLength(edvJobHead, "SerialFrom_c", serialFromUd);
		if (lengthError != null) { error = lengthError; return false; }
		lengthError = ValidateUdStringLength(edvJobHead, "SerialTo_c", serialToUd);
		if (lengthError != null) { error = lengthError; return false; }

		List<string> missing = new List<string>();
		if (!SetViewValue(edvJobHead, "SerialPrefix_c", prefix))
			missing.Add("SerialPrefix_c");
		if (!SetViewValue(edvJobHead, "SerialFrom_c", serialFromUd))
			missing.Add("SerialFrom_c");
		if (!SetViewValue(edvJobHead, "SerialTo_c", serialToUd))
			missing.Add("SerialTo_c");
		if (!SetViewValue(edvJobHead, "RollCount_c", rolls.Count))
			missing.Add("RollCount_c");
		if (!SetViewValue(edvJobHead, "SerializedQty_c", totalQty))
			missing.Add("SerializedQty_c");

		if (missing.Count > 0)
		{
			error = "JobHead no expone estos campos UD: " + string.Join(", ", missing.ToArray())
				+ "\nVerifique que los UD estén publicados en Job Entry.";
			return false;
		}

		SetViewValue(edvJobHead, "LastPrintedBy_c", string.IsNullOrWhiteSpace(userId) ? Environment.UserName : userId);
		SetViewValue(edvJobHead, "LastPrintDate_c", DateTime.Now);
		SetViewValue(edvJobHead, "SalidaInsp_c", string.IsNullOrWhiteSpace(data.Salida) ? "1" : data.Salida.Trim());

		if (!persist(out error))
		{
			if (string.IsNullOrWhiteSpace(error))
				error = "Update del Job falló sin detalle.";
			error += "\n\nLos valores quedaron en pantalla. Si el error es de validación del Job, corríjalo y use el botón Guardar del Job Entry.";
			return false;
		}

		return true;
	}

	public static string FormatExceptionMessage(Exception ex)
	{
		Exception current = ex;
		while (current != null)
		{
			if (current is System.Reflection.TargetInvocationException && current.InnerException != null)
			{
				current = current.InnerException;
				continue;
			}
			string msg = current.Message == null ? string.Empty : current.Message.Trim();
			if (msg.Length > 0 && !msg.StartsWith("Exception has been thrown by the target of an invocation", StringComparison.OrdinalIgnoreCase))
				return msg;
			if (current.InnerException != null)
			{
				current = current.InnerException;
				continue;
			}
			break;
		}
		return ex == null ? "Error desconocido." : ex.Message;
	}

	private static string ValidateUdStringLength(EpiDataView view, string column, string value)
	{
		if (view == null || view.dataView == null || view.dataView.Table == null)
			return null;
		if (!view.dataView.Table.Columns.Contains(column))
			return null;
		if (string.IsNullOrEmpty(value))
			return null;

		System.Data.DataColumn dc = view.dataView.Table.Columns[column];
		if (dc.DataType != typeof(string))
			return null;
		if (dc.MaxLength <= 0)
			return null;
		if (value.Length <= dc.MaxLength)
			return null;

		return string.Format(
			"El valor '{0}' ({1} caracteres) excede el máximo de {2} ({3} caracteres) en Job Entry.\n" +
			"SerialFrom_c y SerialTo_c deben guardar solo el número (sin prefijo); el prefijo va en SerialPrefix_c.\n" +
			"Si en UD Column Maintenance ya tiene x(20), regenere el data model del Job (ICE / Extended Properties) y vuelva a abrir Job Entry.",
			value, value.Length, column, dc.MaxLength);
	}

	private static bool SetViewValue(EpiDataView view, string column, object value)
	{
		try
		{
			if (view == null || view.Row < 0 || view.dataView == null || view.dataView.Table == null)
				return false;
			if (!view.dataView.Table.Columns.Contains(column))
				return false;

			System.Data.DataColumn dc = view.dataView.Table.Columns[column];
			object converted = CoerceValue(dc.DataType, value);
			view.dataView[view.Row][column] = converted == null ? (object)DBNull.Value : converted;
			view.dataView[view.Row].EndEdit();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static object CoerceValue(Type targetType, object value)
	{
		if (value == null || value == DBNull.Value)
			return null;
		if (targetType == typeof(string))
			return Convert.ToString(value);
		if (targetType == typeof(decimal) || targetType == typeof(double) || targetType == typeof(float))
		{
			decimal d;
			if (decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.CurrentCulture, out d))
				return Convert.ChangeType(d, targetType, CultureInfo.InvariantCulture);
			return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
		}
		if (targetType == typeof(int) || targetType == typeof(long) || targetType == typeof(short))
			return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
		if (targetType == typeof(DateTime))
		{
			if (value is DateTime)
				return value;
			DateTime dt;
			if (DateTime.TryParse(Convert.ToString(value), CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
				return dt;
			return DateTime.Now;
		}
		if (targetType == typeof(bool))
		{
			bool b;
			if (bool.TryParse(Convert.ToString(value), out b))
				return b;
		}
		return value;
	}
}

public sealed class SerializadoJobEntryForm : Form
{
	private readonly object _oTrans;
	private readonly EpiDataView _edvJobHead;
	private readonly EpiDataView _edvJobProd;
	private readonly SerializadoPersistHandler _persistHandler;
	private readonly string _userId;
	private readonly SerializadoFormMode _mode;
	private readonly SerializadoJobData _data = new SerializadoJobData();
	private readonly List<SerializadoRollRow> _rolls = new List<SerializadoRollRow>();

	private readonly TextBox _txtJob;
	private readonly TextBox _txtPart;
	private readonly TextBox _txtDesc;
	private readonly TextBox _txtPo;
	private readonly TextBox _txtOv;
	private readonly TextBox _txtCliente;
	private readonly TextBox _txtPrefix;
	private readonly Label _lblPoDisplay;
	private readonly TextBox _txtSalida;
	private readonly NumericUpDown _numTotal;
	private readonly NumericUpDown _numPerRoll;
	private readonly TextBox _txtSerialFrom;
	private readonly TextBox _txtSerialTo;
	private readonly Label _lblResumen;
	private readonly Label _lblCantUe;
	private readonly DataGridView _grid;
	private readonly TabControl _tabControl;

	private bool IsOtrosMode { get { return _mode == SerializadoFormMode.Otros; } }

	public SerializadoJobEntryForm(Form owner, object oTrans, EpiDataView edvJobHead, EpiDataView edvJobProd, string cliente, SerializadoPersistHandler persistHandler, string userId)
		: this(owner, oTrans, edvJobHead, edvJobProd, cliente, persistHandler, userId, SerializadoFormMode.BrkSerializado)
	{
	}

	public SerializadoJobEntryForm(Form owner, object oTrans, EpiDataView edvJobHead, EpiDataView edvJobProd, string cliente, SerializadoPersistHandler persistHandler, string userId, SerializadoFormMode mode)
	{
		_oTrans = oTrans;
		_edvJobHead = edvJobHead;
		_edvJobProd = edvJobProd;
		_persistHandler = persistHandler;
		_userId = userId ?? string.Empty;
		_mode = mode;
		Text = IsOtrosMode ? "Otros — Consecutivo de folios" : "Serializado — Consecutivo de folios";
		Size = new Size(1080, 720);
		StartPosition = FormStartPosition.CenterParent;
		MinimumSize = new Size(900, 620);
		BackColor = Color.FromArgb(245, 247, 250);
		Font = new Font("Segoe UI", 9f);
		FormBorderStyle = FormBorderStyle.Sizable;

		Panel header = BuildHeader();
		Label lblPoDisplay;
		_tabControl = BuildTabs(out _txtJob, out _txtPart, out _txtDesc, out _txtPo, out _txtOv, out _txtCliente,
			out _txtPrefix, out lblPoDisplay, out _txtSalida, out _numTotal, out _numPerRoll,
			out _txtSerialFrom, out _txtSerialTo, out _lblResumen, out _lblCantUe, out _grid);
		_lblPoDisplay = lblPoDisplay;
		_tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
		Panel footer = BuildFooter();

		Controls.Add(_tabControl);
		Controls.Add(footer);
		Controls.Add(header);

		LoadFromJobCore(edvJobHead, edvJobProd, cliente);
		this.Load += this.SerializadoJobEntryForm_Load;
	}

	private void SerializadoJobEntryForm_Load(object sender, EventArgs e)
	{
		this.Load -= this.SerializadoJobEntryForm_Load;
		if (IsDisposed)
			return;
		LoadFromJobBaqData(_edvJobProd);
	}

	private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (_tabControl.SelectedIndex == 1)
			TryAutoCalculateRolls(false);
	}

	private Panel BuildHeader()
	{
		Panel p = new Panel { Dock = DockStyle.Top, Height = 64 };
		if (IsOtrosMode)
		{
			p.Paint += HeaderPanel_PaintOtrosGradient;
		}
		else
		{
			p.BackColor = Color.FromArgb(0, 86, 179);
		}
		Label title = new Label
		{
			Text = IsOtrosMode ? "CodiFlex — Serializado (Otros)" : "CodiFlex — Serializado de producción",
			ForeColor = Color.White,
			Font = new Font("Segoe UI Semibold", 13f),
			Location = new Point(16, 10),
			AutoSize = true,
			BackColor = Color.Transparent
		};
		Label sub = new Label
		{
			Text = "Consecutivo de folios | F-CFX-PLN-02 REV. 2",
			ForeColor = Color.FromArgb(200, 220, 255),
			Font = new Font("Segoe UI", 8.5f),
			Location = new Point(18, 36),
			AutoSize = true,
			BackColor = Color.Transparent
		};
		p.Controls.Add(title);
		p.Controls.Add(sub);
		return p;
	}

	private void HeaderPanel_PaintOtrosGradient(object sender, PaintEventArgs e)
	{
		Panel panel = sender as Panel;
		if (panel == null)
			return;

		Rectangle rect = panel.ClientRectangle;
		if (rect.Width <= 0 || rect.Height <= 0)
			return;

		using (LinearGradientBrush brush = new LinearGradientBrush(
			rect,
			Color.FromArgb(48, 82, 124),
			Color.FromArgb(92, 128, 168),
			LinearGradientMode.Horizontal))
		{
			e.Graphics.FillRectangle(brush, rect);
		}
	}

	private Panel BuildFooter()
	{
		Panel p = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(12, 8, 12, 8) };
		FlowLayoutPanel flow = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
		Button btnClose = MakeButton("Cerrar", Color.FromArgb(108, 117, 125), 100);
		btnClose.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
		Button btnPreview = MakeButton("Vista Previa", Color.FromArgb(0, 86, 179), 120);
		btnPreview.Click += delegate { ShowPrintPreview(); };
		Button btnPrint = MakeButton("Imprimir", Color.FromArgb(253, 126, 20), 110);
		btnPrint.Click += delegate { PrintConsecutivo(); };
		Button btnSaveFooter = MakeButton("Guardar en Epicor", Color.FromArgb(40, 167, 69), 140);
		btnSaveFooter.Click += delegate { GuardarEnEpicor(); };
		Button btnCalc = MakeButton("Calcular Automático", Color.FromArgb(0, 86, 179), 150);
		btnCalc.Click += delegate { TryAutoCalculateRolls(true); };
		flow.Controls.Add(btnClose);
		flow.Controls.Add(btnSaveFooter);
		flow.Controls.Add(btnPrint);
		flow.Controls.Add(btnPreview);
		flow.Controls.Add(btnCalc);
		p.Controls.Add(flow);
		return p;
	}

	private TabControl BuildTabs(out TextBox txtJob, out TextBox txtPart, out TextBox txtDesc, out TextBox txtPo,
		out TextBox txtOv, out TextBox txtCliente, out TextBox txtPrefix, out Label lblPoDisplay, out TextBox txtSalida,
		out NumericUpDown numTotal, out NumericUpDown numPerRoll, out TextBox txtSerialFrom, out TextBox txtSerialTo,
		out Label lblResumen, out Label lblCantUe, out DataGridView grid)
	{
		TabControl tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };
		TabPage tab1 = new TabPage("1. Datos del Job");
		TabPage tab2 = new TabPage("2. Configurar Rollos");

		Panel tab1Host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };
		TableLayoutPanel t1 = new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			ColumnCount = 4,
			RowCount = 4,
			Height = 168,
			GrowStyle = TableLayoutPanelGrowStyle.FixedSize
		};
		t1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
		t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		t1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
		t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		for (int r = 0; r < 3; r++)
			t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
		t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

		txtJob = MakeReadOnlyBox();
		txtPart = MakeReadOnlyBox();
		txtDesc = MakeReadOnlyBox();
		txtPo = MakeReadOnlyBox();
		txtOv = MakeReadOnlyBox();
		txtCliente = MakeReadOnlyBox();
		txtCliente.BackColor = Color.Honeydew;
		txtPrefix = MakeLargePrefixBox();
		txtSalida = MakeEditableBox();
		numTotal = new NumericUpDown { Minimum = 1, Maximum = 99999999, ThousandsSeparator = true, Font = new Font("Segoe UI", 10f) };
		numPerRoll = new NumericUpDown { Minimum = 1, Maximum = 99999999, ThousandsSeparator = true, Width = 180, Font = new Font("Segoe UI", 10f) };

		AddJobRow(t1, 0, "Job", txtJob, "Parte", txtPart);
		AddJobRow(t1, 1, "Descripción", txtDesc, "Cliente", txtCliente);
		AddJobRow(t1, 2, "O.V.", txtOv, null, null);
		AddJobRow(t1, 3, "SALIDA", txtSalida, "Cantidad Total", numTotal);

		Panel prefixPanel = new Panel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(0, 10, 0, 0) };
		Label lblPrefixPrompt = new Label
		{
			Text = "Ingrese el prefijo:",
			ForeColor = Color.Red,
			Font = new Font("Segoe UI", 12f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(0, 0),
			TextAlign = ContentAlignment.MiddleLeft
		};
		txtPrefix.Location = new Point(0, 30);
		txtPrefix.Size = new Size(90, 46);
		txtPrefix.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		prefixPanel.Controls.Add(lblPrefixPrompt);
		prefixPanel.Controls.Add(txtPrefix);

		Panel poPanel = new Panel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(0, 8, 0, 0) };
		lblPoDisplay = new Label
		{
			Dock = DockStyle.Fill,
			Text = "PO:",
			TextAlign = ContentAlignment.MiddleCenter,
			BackColor = Color.FromArgb(255, 255, 0),
			ForeColor = Color.Black,
			Font = new Font("Segoe UI", 24f, FontStyle.Bold),
			BorderStyle = BorderStyle.FixedSingle
		};
		poPanel.Controls.Add(lblPoDisplay);

		if (!IsOtrosMode)
		{
			tab1Host.Controls.Add(poPanel);
			tab1Host.Controls.Add(prefixPanel);
		}
		tab1Host.Controls.Add(t1);
		tab1.Controls.Add(tab1Host);

		Panel p2top = new Panel { Dock = DockStyle.Top, Height = 168, Padding = new Padding(16, 12, 16, 4) };
		TableLayoutPanel t2h = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 4 };
		t2h.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
		t2h.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
		t2h.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
		t2h.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
		t2h.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
		t2h.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		t2h.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
		t2h.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
		t2h.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
		t2h.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		txtSerialFrom = MakeEditableBox();
		txtSerialTo = MakeReadOnlyBox();
		t2h.Controls.Add(MakeLabel("Serial From (UL)"), 0, 0);
		t2h.Controls.Add(txtSerialFrom, 1, 0);
		t2h.Controls.Add(MakeLabel("Serial To (UL)"), 2, 0);
		t2h.Controls.Add(txtSerialTo, 3, 0);

		t2h.Controls.Add(MakeLabel("Etiquetas por rollo"), 0, 1);
		t2h.Controls.Add(WrapNumeric(numPerRoll), 1, 1);
		lblCantUe = new Label
		{
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Color.FromArgb(0, 86, 179),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Text = "Cant/UE (UD10.Number16): 1,000 etiquetas por rollo"
		};
		t2h.Controls.Add(lblCantUe, 2, 1);
		t2h.SetColumnSpan(lblCantUe, 4);

		FlowLayoutPanel p2btns = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Padding = new Padding(0, 2, 0, 0)
		};
		Button btnAddRoll = MakeButton("Agregar Rollo", Color.FromArgb(0, 86, 179), 120);
		btnAddRoll.Click += delegate { TryAutoCalculateRolls(true); };
		Button btnCalcTab = MakeButton("Calcular Automático", Color.FromArgb(0, 86, 179), 150);
		btnCalcTab.Click += delegate { TryAutoCalculateRolls(true); };
		Button btnClear = MakeButton("Limpiar Rollos", Color.FromArgb(108, 117, 125), 120);
		btnClear.Click += delegate { ClearRolls(); };
		Button btnPreviewTab = MakeButton("Vista Previa", Color.FromArgb(0, 86, 179), 110);
		btnPreviewTab.Click += delegate { ShowPrintPreview(); };
		Button btnPrintTab = MakeButton("Imprimir", Color.FromArgb(253, 126, 20), 100);
		btnPrintTab.Click += delegate { PrintConsecutivo(); };
		Button btnSaveTab = MakeButton("Guardar en Epicor", Color.FromArgb(40, 167, 69), 140);
		btnSaveTab.Click += delegate { GuardarEnEpicor(); };
		p2btns.Controls.Add(btnAddRoll);
		p2btns.Controls.Add(btnCalcTab);
		p2btns.Controls.Add(btnClear);
		p2btns.Controls.Add(btnPreviewTab);
		p2btns.Controls.Add(btnPrintTab);
		p2btns.Controls.Add(btnSaveTab);
		t2h.Controls.Add(p2btns, 0, 2);
		t2h.SetColumnSpan(p2btns, 6);

		lblResumen = new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = Color.DimGray,
			TextAlign = ContentAlignment.MiddleLeft,
			Text = "Configure serial inicial y presione Calcular Automático."
		};
		t2h.Controls.Add(lblResumen, 0, 3);
		t2h.SetColumnSpan(lblResumen, 6);
		p2top.Controls.Add(t2h);

		numPerRoll.ValueChanged += delegate { UpdateCantUeLabel(); };

		grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			ReadOnly = true,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			RowHeadersVisible = false,
			BackgroundColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			ColumnHeadersHeight = 32,
			RowTemplate = { Height = 28 }
		};
		grid.Columns.Add("RollNum", "Rollo");
		if (!IsOtrosMode)
			grid.Columns.Add("Prefix", "Prefijo");
		grid.Columns.Add("SerialFrom", "De");
		grid.Columns.Add("SerialTo", "A");
		grid.Columns.Add("Quantity", "Cantidad");
		grid.Columns.Add("Defectos", "Defectos en Rollos");
		grid.ReadOnly = false;
		for (int ci = 0; ci < grid.Columns.Count; ci++)
			grid.Columns[ci].ReadOnly = !string.Equals(grid.Columns[ci].Name, "Defectos", StringComparison.Ordinal);

		tab2.Controls.Add(grid);
		tab2.Controls.Add(p2top);

		tabs.TabPages.Add(tab1);
		tabs.TabPages.Add(tab2);
		return tabs;
	}

	private void LoadFromJobCore(EpiDataView edvJobHead, EpiDataView edvJobProd, string cliente)
	{
		_data.JobNum = SerializadoBaqService.GetField(edvJobHead, "JobNum");
		_data.PartNum = SerializadoBaqService.GetField(edvJobHead, "PartNum");
		_data.PartDescription = SerializadoBaqService.GetField(edvJobHead, "PartDescription");
		_data.SalesOrder = SerializadoBaqService.GetField(edvJobHead, "OrdenDeVenta_c");
		_data.PurchaseOrder = SerializadoBaqService.GetField(edvJobHead, "orden_compra_c");
		_data.Cliente = cliente;

		if (string.IsNullOrWhiteSpace(_data.Cliente))
			_data.Cliente = SerializadoBaqService.GetField(edvJobHead, "Cliente_c");

		_data.TotalQty = SerializadoBaqService.GetJobQuantity(edvJobHead, edvJobProd);

		_txtJob.Text = _data.JobNum;
		_txtPart.Text = _data.PartNum;
		_txtDesc.Text = _data.PartDescription;
		if (!IsOtrosMode)
			SetPurchaseOrderDisplay(_data.PurchaseOrder);
		_txtOv.Text = _data.SalesOrder;
		_txtCliente.Text = _data.Cliente;
		_txtPrefix.Text = _data.SerialPrefix;
		_txtSalida.Text = _data.Salida;
		_numTotal.Value = _data.TotalQty > 0m ? Math.Max(1m, _data.TotalQty) : 1m;
		_numPerRoll.Value = Math.Max(1, _data.LabelsPerRoll);
		UpdateCantUeLabel();

		string savedPrefix = SerializadoBaqService.GetField(edvJobHead, "SerialPrefix_c");
		string savedSerialFrom = SerializadoBaqService.GetField(edvJobHead, "SerialFrom_c");
		if (!IsOtrosMode && !string.IsNullOrWhiteSpace(savedPrefix))
		{
			_data.SerialPrefix = savedPrefix;
			_txtPrefix.Text = savedPrefix;
		}
		else if (IsOtrosMode)
		{
			_data.SerialPrefix = string.Empty;
			_txtPrefix.Text = string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(savedSerialFrom))
		{
			long savedNum = SerializadoCalculator.ParseSerialNumber(savedSerialFrom);
			_txtSerialFrom.Text = savedNum > 0L ? savedNum.ToString(CultureInfo.InvariantCulture) : savedSerialFrom;
		}

		decimal savedQty = 0m;
		string savedQtyText = SerializadoBaqService.GetField(edvJobHead, "SerializedQty_c");
		if (decimal.TryParse(savedQtyText, NumberStyles.Any, CultureInfo.CurrentCulture, out savedQty) && savedQty > 0m)
			_numTotal.Value = Math.Max(1m, savedQty);

		if (!string.IsNullOrWhiteSpace(savedSerialFrom) && savedQty > 0m)
			TryAutoCalculateRolls(false);
	}

	private void LoadFromJobBaqData(EpiDataView edvJobProd)
	{
		if (!IsOtrosMode && string.IsNullOrWhiteSpace(_data.PurchaseOrder) && !string.IsNullOrWhiteSpace(_data.SalesOrder))
		{
			_data.PurchaseOrder = SerializadoBaqService.GetPoFromOv(_oTrans, _data.SalesOrder);
			SetPurchaseOrderDisplay(_data.PurchaseOrder);
		}

		int labelsPerRoll;
		string salida;
		SerializadoBaqService.LoadUd10Matrix(_oTrans, _data.PartNum, out labelsPerRoll, out salida);
		_data.LabelsPerRoll = labelsPerRoll;
		_data.Salida = salida;
		_txtSalida.Text = _data.Salida;
		_numPerRoll.Value = Math.Max(1, _data.LabelsPerRoll);
		UpdateCantUeLabel();
	}

	private void LoadFromJob(EpiDataView edvJobHead, EpiDataView edvJobProd, string cliente)
	{
		LoadFromJobCore(edvJobHead, edvJobProd, cliente);
		LoadFromJobBaqData(edvJobProd);
	}

	private void UpdateCantUeLabel()
	{
		_lblCantUe.Text = string.Format("Cant/UE (UD10.Number16): {0:N0} etiquetas por rollo", _numPerRoll.Value);
	}

	private void ClearRolls()
	{
		_rolls.Clear();
		_grid.Rows.Clear();
		_txtSerialTo.Clear();
		_lblResumen.Text = "Rollos limpiados. Ingrese Serial From y presione Calcular Automático.";
	}

	private void TryAutoCalculateRolls(bool showWarning)
	{
		if (!IsOtrosMode && string.IsNullOrWhiteSpace(_txtPrefix.Text))
		{
			if (showWarning)
				MessageBox.Show("Ingrese el prefijo (2 caracteres) antes de calcular rollos.", "Validación",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_data.SerialFrom = SerializadoCalculator.ParseSerialNumber(_txtSerialFrom.Text);
		if (_data.SerialFrom <= 0L)
		{
			if (showWarning)
				MessageBox.Show("Ingrese el Serial From (UL) numérico antes de calcular rollos.", "Validación",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		CalculateRolls();
	}

	private void CalculateRolls()
	{
		_data.JobNum = _txtJob.Text.Trim();
		_data.PartNum = _txtPart.Text.Trim();
		_data.PurchaseOrder = _txtPo.Text.Trim();
		_data.SalesOrder = _txtOv.Text.Trim();
		_data.Cliente = _txtCliente.Text.Trim();
		_data.SerialPrefix = IsOtrosMode ? string.Empty : _txtPrefix.Text.Trim().ToUpperInvariant();
		if (!IsOtrosMode && _data.SerialPrefix.Length == 0)
			return;
		_data.Salida = string.IsNullOrWhiteSpace(_txtSalida.Text) ? "1" : _txtSalida.Text.Trim();
		_data.TotalQty = _numTotal.Value;
		_data.LabelsPerRoll = (int)_numPerRoll.Value;
		_data.SerialFrom = SerializadoCalculator.ParseSerialNumber(_txtSerialFrom.Text);
		if (_data.SerialFrom <= 0L)
			return;

		int totalQty = (int)Math.Round(_data.TotalQty, 0);
		_rolls.Clear();
		_rolls.AddRange(SerializadoCalculator.BuildRollsByCapacity(
			_data.SerialPrefix, _data.SerialFrom, totalQty, _data.LabelsPerRoll));

		_grid.Rows.Clear();
		for (int i = 0; i < _rolls.Count; i++)
		{
			SerializadoRollRow r = _rolls[i];
			if (IsOtrosMode)
				_grid.Rows.Add(r.RollNum, r.SerialFrom, r.SerialTo, r.Quantity, r.Defectos);
			else
				_grid.Rows.Add(r.RollNum, r.Prefix, r.SerialFrom, r.SerialTo, r.Quantity, r.Defectos);
		}

		if (_rolls.Count > 0)
			_txtSerialTo.Text = _rolls[_rolls.Count - 1].SerialToDisplay;

		_lblResumen.Text = string.Format(
			"Rollos configurados | Rollos: {0} (Cant/UE UD10: {1:N0} etiquetas/rollo) | Total piezas: {2:N0} | Fórmula: Serial To = Inicio + (Cantidad - 1)",
			_rolls.Count, _data.LabelsPerRoll, totalQty);
	}

	private void SyncDefectosFromGrid()
	{
		for (int i = 0; i < _rolls.Count && i < _grid.Rows.Count; i++)
		{
			object cell = _grid.Rows[i].Cells["Defectos"].Value;
			_rolls[i].Defectos = cell == null || cell == DBNull.Value ? string.Empty : cell.ToString().Trim();
		}
	}

	private void ShowPrintPreview()
	{
		TryAutoCalculateRolls(false);
		if (_rolls.Count == 0)
		{
			MessageBox.Show("Ingrese Serial From y calcule los rollos antes de la vista previa.", "Aviso",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		SyncDefectosFromGrid();

		try
		{
			SerializadoConsecutivoPrinter.ShowPreview(this, _data, _rolls, IsOtrosMode);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error en vista previa:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void GuardarEnEpicor()
	{
		TryAutoCalculateRolls(true);
		if (_rolls.Count == 0)
			return;

		string error;
		if (!SerializadoSaveService.TrySave(_edvJobHead, _data, _rolls, _persistHandler, _userId, !IsOtrosMode, out error))
		{
			MessageBox.Show("No se pudo guardar en Epicor:\n" + error, "Guardar",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		MessageBox.Show(
			string.Format(
				"Serializado guardado en JobHead_UD.\n\nJob: {0}\nRollos: {1}\nDe: {2}\nA: {3}\nCantidad: {4:N0}",
				_data.JobNum,
				_rolls.Count,
				_rolls[0].SerialFromDisplay,
				_rolls[_rolls.Count - 1].SerialToDisplay,
				_data.TotalQty),
			"Guardar en Epicor",
			MessageBoxButtons.OK,
			MessageBoxIcon.Information);
	}

	private void PrintConsecutivo()
	{
		TryAutoCalculateRolls(false);
		if (_rolls.Count == 0)
		{
			MessageBox.Show("Ingrese Serial From y calcule los rollos antes de imprimir.", "Aviso",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		SyncDefectosFromGrid();

		using (SerializadoPrinterSelectDialog dlg = new SerializadoPrinterSelectDialog())
		{
			if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.SelectedPrinter))
				return;

			try
			{
				SerializadoConsecutivoPrinter.Print(dlg.SelectedPrinter, _data, _rolls, IsOtrosMode);
				MessageBox.Show("Consecutivo de folios enviado a:\n" + dlg.SelectedPrinter, "Impresión",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al imprimir:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	private static void AddJobRow(TableLayoutPanel layout, int row, string leftLabel, Control leftCtrl, string rightLabel, Control rightCtrl)
	{
		bool alignTop = (row == 4);
		int topInset = alignTop ? 8 : 6;

		layout.Controls.Add(MakeFieldLabel(leftLabel, alignTop), 0, row);
		leftCtrl.Dock = DockStyle.Fill;
		leftCtrl.Margin = new Padding(0, topInset, 8, 0);
		layout.Controls.Add(leftCtrl, 1, row);

		if (!string.IsNullOrEmpty(rightLabel) && rightCtrl != null)
		{
			layout.Controls.Add(MakeFieldLabel(rightLabel, alignTop), 2, row);
			if (rightCtrl is NumericUpDown)
				layout.Controls.Add(PlaceNumeric((NumericUpDown)rightCtrl, topInset), 3, row);
			else
			{
				rightCtrl.Dock = DockStyle.Fill;
				rightCtrl.Margin = new Padding(0, topInset, 0, 0);
				layout.Controls.Add(rightCtrl, 3, row);
			}
		}
	}

	private static Panel PlaceNumeric(NumericUpDown num, int topInset)
	{
		num.Dock = DockStyle.None;
		num.Anchor = AnchorStyles.Left | AnchorStyles.Top;
		num.Width = 200;
		num.Height = 23;
		num.Margin = new Padding(0);
		num.TextAlign = HorizontalAlignment.Right;
		Panel host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0, topInset, 0, 0) };
		host.Controls.Add(num);
		return host;
	}

	private static Panel WrapNumeric(NumericUpDown num)
	{
		num.Dock = DockStyle.None;
		num.Anchor = AnchorStyles.Left | AnchorStyles.Top;
		num.Width = 180;
		num.Height = 23;
		num.Margin = new Padding(0);
		Panel host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
		host.Controls.Add(num);
		return host;
	}

	private static Label MakeFieldLabel(string text, bool alignTop)
	{
		return new Label
		{
			Text = text,
			Dock = DockStyle.Fill,
			TextAlign = alignTop ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft,
			Padding = alignTop ? new Padding(0, 8, 0, 0) : new Padding(0),
			Margin = new Padding(0),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
	}

	private static void AddField(TableLayoutPanel layout, int row, string l1, Control c1, string l2, Control c2)
	{
		layout.Controls.Add(MakeLabel(l1), 0, row);
		layout.Controls.Add(c1, 1, row);
		layout.Controls.Add(MakeLabel(l2), 2, row);
		layout.Controls.Add(c2, 3, row);
	}

	private static Label MakeLabel(string text)
	{
		return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
	}

	private static TextBox MakeReadOnlyBox()
	{
		return new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
	}

	private static TextBox MakeEditableBox()
	{
		return new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
	}

	private static TextBox MakeLargePrefixBox()
	{
		return new TextBox
		{
			BorderStyle = BorderStyle.Fixed3D,
			BackColor = Color.FromArgb(255, 255, 102),
			ForeColor = Color.Black,
			Font = new Font("Segoe UI", 22f, FontStyle.Bold),
			MaxLength = 2,
			Text = string.Empty,
			TextAlign = HorizontalAlignment.Center,
			CharacterCasing = CharacterCasing.Upper
		};
	}

	private void SetPurchaseOrderDisplay(string po)
	{
		po = po == null ? string.Empty : po.Trim();
		_txtPo.Text = po;
		if (_lblPoDisplay != null)
			_lblPoDisplay.Text = po.Length == 0 ? "PO:" : "PO: " + po;
	}

	private static Button MakeButton(string text, Color back, int width)
	{
		return new Button
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
	}
}

public sealed class SerializadoPrinterSelectDialog : Form
{
	private readonly ComboBox _cbo;
	public string SelectedPrinter { get; private set; }

	public SerializadoPrinterSelectDialog()
	{
		Text = "Seleccionar impresora";
		Size = new Size(480, 200);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		StartPosition = FormStartPosition.CenterParent;
		MaximizeBox = false;
		MinimizeBox = false;

		Label lbl = new Label { Text = "Impresoras instaladas:", Location = new Point(16, 16), AutoSize = true };
		_cbo = new ComboBox { Location = new Point(16, 40), Width = 430, DropDownStyle = ComboBoxStyle.DropDownList };
		foreach (string p in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
			_cbo.Items.Add(p);
		if (_cbo.Items.Count > 0)
			_cbo.SelectedIndex = 0;

		Button btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(280, 100), Width = 80 };
		Button btnOk = new Button { Text = "Imprimir", Location = new Point(366, 100), Width = 80 };
		btnOk.Click += delegate
		{
			if (_cbo.SelectedItem == null) return;
			SelectedPrinter = _cbo.SelectedItem.ToString();
			DialogResult = DialogResult.OK;
			Close();
		};

		Controls.Add(lbl);
		Controls.Add(_cbo);
		Controls.Add(btnCancel);
		Controls.Add(btnOk);
		CancelButton = btnCancel;
		AcceptButton = btnOk;
	}
}

public sealed class SerializadoConsecutivoPreviewForm : Form
{
	private readonly SerializadoJobData _job;
	private readonly List<SerializadoRollRow> _rolls;
	private readonly bool _hidePoAndPrefix;
	private PrintDocument _doc;

	public SerializadoConsecutivoPreviewForm(SerializadoJobData job, List<SerializadoRollRow> rolls)
		: this(job, rolls, false)
	{
	}

	public SerializadoConsecutivoPreviewForm(SerializadoJobData job, List<SerializadoRollRow> rolls, bool hidePoAndPrefix)
	{
		_job = job;
		_rolls = rolls;
		_hidePoAndPrefix = hidePoAndPrefix;
		Text = "Vista previa — Consecutivo de folios";
		Size = new Size(1100, 780);
		StartPosition = FormStartPosition.CenterParent;
		MinimumSize = new Size(800, 600);

		Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = SystemColors.Control };
		FlowLayoutPanel flow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Padding = new Padding(6, 6, 6, 6)
		};

		Button btnPrint = new Button { Text = "Imprimir", Width = 90, Height = 28 };
		Button btnPdf = new Button { Text = "Guardar PDF", Width = 110, Height = 28 };
		Button btnClose = new Button { Text = "Cerrar", Width = 80, Height = 28 };

		btnPrint.Click += BtnPrint_Click;
		btnPdf.Click += BtnPdf_Click;
		btnClose.Click += delegate { Close(); };

		flow.Controls.Add(btnPrint);
		flow.Controls.Add(btnPdf);
		flow.Controls.Add(btnClose);
		toolbar.Controls.Add(flow);

		_doc = CreatePreviewDocument();
		PrintPreviewControl preview = new PrintPreviewControl
		{
			Dock = DockStyle.Fill,
			Document = _doc,
			Zoom = 1.0,
			UseAntiAlias = true
		};

		Controls.Add(preview);
		Controls.Add(toolbar);
		CancelButton = btnClose;
	}

	private PrintDocument CreatePreviewDocument()
	{
		PrintDocument doc = new PrintDocument();
		doc.DefaultPageSettings.Landscape = true;
		doc.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);
		doc.PrintPage += PreviewDoc_PrintPage;
		return doc;
	}

	private int _pageIndex;

	private void PreviewDoc_PrintPage(object sender, PrintPageEventArgs e)
	{
		SerializadoConsecutivoPrinter.RenderPage(e, _job, _rolls, ref _pageIndex, _hidePoAndPrefix);
	}

	private void BtnPrint_Click(object sender, EventArgs e)
	{
		using (SerializadoPrinterSelectDialog dlg = new SerializadoPrinterSelectDialog())
		{
			if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.SelectedPrinter))
				return;
			try
			{
				SerializadoConsecutivoPrinter.Print(dlg.SelectedPrinter, _job, _rolls, _hidePoAndPrefix);
				MessageBox.Show("Consecutivo enviado a:\n" + dlg.SelectedPrinter, Text,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al imprimir:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	private void BtnPdf_Click(object sender, EventArgs e)
	{
		using (SaveFileDialog sfd = new SaveFileDialog())
		{
			sfd.Filter = "PDF (*.pdf)|*.pdf";
			sfd.DefaultExt = "pdf";
			sfd.AddExtension = true;
			sfd.FileName = string.Format(CultureInfo.InvariantCulture,
				"Consecutivo_{0}_{1:yyyyMMdd}.pdf",
				string.IsNullOrWhiteSpace(_job.JobNum) ? "Job" : _job.JobNum.Trim(),
				DateTime.Now);
			if (sfd.ShowDialog(this) != DialogResult.OK)
				return;

			string error;
			if (!SerializadoConsecutivoPrinter.TrySaveAsPdf(_job, _rolls, sfd.FileName, _hidePoAndPrefix, out error))
			{
				MessageBox.Show("No se pudo guardar el PDF:\n" + error, Text,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			MessageBox.Show("PDF guardado en:\n" + sfd.FileName, Text,
				MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		if (_doc != null)
		{
			_doc.Dispose();
			_doc = null;
		}
		base.OnFormClosed(e);
	}
}

public static class SerializadoConsecutivoPrinter
{
	private static readonly Color CyanHeader = Color.FromArgb(153, 204, 255);
	private static readonly Color LavenderTitle = Color.FromArgb(204, 192, 218);

	private static SerializadoJobData _job;
	private static List<SerializadoRollRow> _rolls;
	private static int _pageIndex;
	private static bool _hidePoAndPrefix;

	public static void Print(string printerName, SerializadoJobData job, List<SerializadoRollRow> rolls)
	{
		Print(printerName, job, rolls, false);
	}

	public static void Print(string printerName, SerializadoJobData job, List<SerializadoRollRow> rolls, bool hidePoAndPrefix)
	{
		using (PrintDocument doc = CreatePrintDocument(job, rolls, hidePoAndPrefix))
		{
			doc.PrinterSettings.PrinterName = printerName;
			doc.Print();
		}
	}

	public static void ShowPreview(IWin32Window owner, SerializadoJobData job, List<SerializadoRollRow> rolls)
	{
		ShowPreview(owner, job, rolls, false);
	}

	public static void ShowPreview(IWin32Window owner, SerializadoJobData job, List<SerializadoRollRow> rolls, bool hidePoAndPrefix)
	{
		using (SerializadoConsecutivoPreviewForm frm = new SerializadoConsecutivoPreviewForm(job, rolls, hidePoAndPrefix))
			frm.ShowDialog(owner);
	}

	public static bool TrySaveAsPdf(SerializadoJobData job, List<SerializadoRollRow> rolls, string pdfPath, out string error)
	{
		return TrySaveAsPdf(job, rolls, pdfPath, false, out error);
	}

	public static bool TrySaveAsPdf(SerializadoJobData job, List<SerializadoRollRow> rolls, string pdfPath, bool hidePoAndPrefix, out string error)
	{
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(pdfPath))
		{
			error = "Ruta PDF invalida.";
			return false;
		}

		string pdfPrinter = ResolvePdfPrinterName();
		if (string.IsNullOrWhiteSpace(pdfPrinter))
		{
			error = "No se encontro impresora PDF en Windows (ej. Microsoft Print to PDF).";
			return false;
		}

		try
		{
			using (PrintDocument doc = CreatePrintDocument(job, rolls, hidePoAndPrefix))
			{
				doc.PrinterSettings.PrinterName = pdfPrinter;
				doc.PrinterSettings.PrintToFile = true;
				doc.PrinterSettings.PrintFileName = pdfPath;
				doc.PrintController = new StandardPrintController();
				doc.Print();
			}
			return File.Exists(pdfPath);
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static string ResolvePdfPrinterName()
	{
		foreach (string name in PrinterSettings.InstalledPrinters)
		{
			if (name.IndexOf("Microsoft Print to PDF", StringComparison.OrdinalIgnoreCase) >= 0)
				return name;
		}
		foreach (string name in PrinterSettings.InstalledPrinters)
		{
			if (name.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0)
				return name;
		}
		return null;
	}

	private static PrintDocument CreatePrintDocument(SerializadoJobData job, List<SerializadoRollRow> rolls)
	{
		return CreatePrintDocument(job, rolls, false);
	}

	private static PrintDocument CreatePrintDocument(SerializadoJobData job, List<SerializadoRollRow> rolls, bool hidePoAndPrefix)
	{
		_job = job;
		_rolls = rolls;
		_pageIndex = 0;
		_hidePoAndPrefix = hidePoAndPrefix;

		PrintDocument doc = new PrintDocument();
		doc.DefaultPageSettings.Landscape = true;
		doc.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);
		doc.PrintPage += Doc_PrintPage;
		return doc;
	}

	private static void Doc_PrintPage(object sender, PrintPageEventArgs e)
	{
		RenderPage(e, _job, _rolls, ref _pageIndex, _hidePoAndPrefix);
	}

	internal static void RenderPage(PrintPageEventArgs e, SerializadoJobData job, List<SerializadoRollRow> rolls, ref int pageIndex)
	{
		RenderPage(e, job, rolls, ref pageIndex, false);
	}

	internal static void RenderPage(PrintPageEventArgs e, SerializadoJobData job, List<SerializadoRollRow> rolls, ref int pageIndex, bool hidePoAndPrefix)
	{
		Graphics g = e.Graphics;
		Rectangle bounds = e.MarginBounds;
		float y = bounds.Top;
		float contentW = bounds.Width;

		using (Font fTitle = new Font("Segoe UI", 16f, FontStyle.Bold))
		using (Font fMetaLabel = new Font("Segoe UI", 9f, FontStyle.Bold))
		using (Font fMetaValue = new Font("Segoe UI", 11f, FontStyle.Bold))
		using (Font fFooter = new Font("Segoe UI", 11f, FontStyle.Bold))
		using (Font fHead = new Font("Segoe UI", 11f, FontStyle.Bold))
		using (Font fCell = new Font("Segoe UI", 12f, FontStyle.Bold))
		using (Pen pen = new Pen(Color.Black, 1.25f))
		{
			float headerTop = y;
			DrawCfxLogo(g, bounds.Left, headerTop, 52f);

			float titleW = 340f;
			float titleX = bounds.Left + (contentW - titleW) / 2f;
			RectangleF titleRect = new RectangleF(titleX, headerTop, titleW, 36f);
			g.FillRectangle(new SolidBrush(LavenderTitle), titleRect);
			g.DrawRectangle(pen, titleRect.X, titleRect.Y, titleRect.Width, titleRect.Height);
			DrawCenteredText(g, fTitle, "Consecutivo de folios", titleRect);

			float metaW = 360f;
			float metaX = bounds.Right - metaW;
			float metaRowH = 26f;
			float metaLabelW = metaW * 0.58f;
			float metaValueW = metaW * 0.42f;
			float metaY = headerTop;
			DrawMetaRow(g, pen, fMetaLabel, fMetaValue, "CODIGO", "F-CFX-PLN-02", metaX, metaY, metaLabelW, metaValueW, metaRowH);
			metaY += metaRowH;
			DrawMetaRow(g, pen, fMetaLabel, fMetaValue, "FECHA DE ELABORACION", "11/30/2022", metaX, metaY, metaLabelW, metaValueW, metaRowH);
			metaY += metaRowH;
			DrawMetaRow(g, pen, fMetaLabel, fMetaValue, "FECHA DE REVISION", "28/02/2025", metaX, metaY, metaLabelW, metaValueW, metaRowH);
			metaY += metaRowH;
			DrawMetaRow(g, pen, fMetaLabel, fMetaValue, "REVISION", "2", metaX, metaY, metaLabelW, metaValueW, metaRowH);

			y = Math.Max(headerTop + 44f, metaY + metaRowH + 8f);

			string[] headers;
			string[] values;
			float[] widths;
			if (hidePoAndPrefix)
			{
				headers = new string[] { "JOB", "No de parte", "Cliente", "Cantidad Total", "Cantidad de rollos" };
				values = new string[]
				{
					job.JobNum,
					job.PartNum,
					job.Cliente,
					((int)Math.Round(job.TotalQty, 0)).ToString("N0"),
					rolls.Count.ToString()
				};
				widths = new float[] { 0.12f, 0.32f, 0.14f, 0.21f, 0.21f };
			}
			else
			{
				headers = new string[] { "PO", "JOB", "No de parte", "Cliente", "Cantidad Total", "Cantidad de rollos" };
				values = new string[]
				{
					job.PurchaseOrder,
					job.JobNum,
					job.PartNum,
					job.Cliente,
					((int)Math.Round(job.TotalQty, 0)).ToString("N0"),
					rolls.Count.ToString()
				};
				widths = new float[] { 0.14f, 0.10f, 0.28f, 0.12f, 0.18f, 0.18f };
			}
			float headerH = 30f;
			float rowH = 32f;
			float x = bounds.Left;

			for (int i = 0; i < headers.Length; i++)
			{
				float w = contentW * widths[i];
				DrawCell(g, pen, fHead, fCell, headers[i], x, y, w, headerH, true, CyanHeader);
				x += w;
			}
			y += headerH;
			x = bounds.Left;
			for (int i = 0; i < values.Length; i++)
			{
				float w = contentW * widths[i];
				DrawCell(g, pen, fHead, fCell, values[i], x, y, w, rowH, false, Color.White);
				x += w;
			}

			y += rowH + 18f;

			string[] dHeaders;
			float[] dWidths;
			if (hidePoAndPrefix)
			{
				dHeaders = new string[] { "Rollo /\nNumero", "De", "A", "Cantidad", "Defectos en\nRollos" };
				dWidths = new float[] { 0.15f, 0.22f, 0.22f, 0.18f, 0.23f };
			}
			else
			{
				dHeaders = new string[] { "Rollo /\nNumero", "", "De", "A", "Cantidad", "Defectos en\nRollos" };
				dWidths = new float[] { 0.13f, 0.05f, 0.20f, 0.20f, 0.17f, 0.25f };
			}
			float detailHeaderH = 40f;
			x = bounds.Left;
			for (int i = 0; i < dHeaders.Length; i++)
			{
				float w = contentW * dWidths[i];
				DrawCell(g, pen, fHead, fCell, dHeaders[i], x, y, w, detailHeaderH, true, CyanHeader);
				x += w;
			}
			y += detailHeaderH;

			int rowsPerPage = 14;
			int start = pageIndex * rowsPerPage;
			int end = Math.Min(start + rowsPerPage, rolls.Count);
			for (int r = start; r < end; r++)
			{
				SerializadoRollRow roll = rolls[r];
				x = bounds.Left;
				string[] cells;
				if (hidePoAndPrefix)
				{
					cells = new string[]
					{
						roll.RollNum.ToString(),
						roll.SerialFrom.ToString(),
						roll.SerialTo.ToString(),
						roll.Quantity.ToString("N0"),
						string.IsNullOrWhiteSpace(roll.Defectos) ? string.Empty : roll.Defectos.Trim()
					};
				}
				else
				{
					cells = new string[]
					{
						roll.RollNum.ToString(),
						roll.Prefix,
						roll.SerialFrom.ToString(),
						roll.SerialTo.ToString(),
						roll.Quantity.ToString("N0"),
						string.IsNullOrWhiteSpace(roll.Defectos) ? string.Empty : roll.Defectos.Trim()
					};
				}
				for (int i = 0; i < cells.Length; i++)
				{
					float w = contentW * dWidths[i];
					DrawCell(g, pen, fHead, fCell, cells[i], x, y, w, rowH, false, Color.White);
					x += w;
				}
				y += rowH;
			}

			y += 10f;
			g.DrawString(string.Format("O.V.: {0}  |  SALIDA: {1}  |  Usuario: {2}",
				job.SalesOrder, job.Salida, Environment.UserName),
				fFooter, Brushes.Black, bounds.Left, y);

			pageIndex++;
			e.HasMorePages = (pageIndex * rowsPerPage) < rolls.Count;
		}
	}

	
	private static Image _cachedLogo;

	private static Image LoadCfxLogo()
	{
		if (_cachedLogo != null)
			return _cachedLogo;
		try
		{
			string[] paths = new string[]
			{
				@"C:\CodiFlex\Resources\cfx.png",
				@"C:\CodiFlex\Resources\cfx.jpg",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CodiFlex", "cfx.png")
			};
			for (int i = 0; i < paths.Length; i++)
			{
				if (File.Exists(paths[i]))
				{
					_cachedLogo = Image.FromFile(paths[i]);
					return _cachedLogo;
				}
			}
			byte[] bytes = Convert.FromBase64String(CfxLogoBase64);
			using (MemoryStream ms = new MemoryStream(bytes))
				_cachedLogo = Image.FromStream(ms);
			return _cachedLogo;
		}
		catch
		{
			return null;
		}
	}

	private static void DrawCfxLogo(Graphics g, float x, float y, float maxHeight)
	{
		Image logo = LoadCfxLogo();
		if (logo == null)
		{
			using (Font fFallback = new Font("Segoe UI", 20f, FontStyle.Bold))
				g.DrawString("CodiFlex", fFallback, Brushes.Black, x, y);
			return;
		}
		float ratio = logo.Width / (float)logo.Height;
		float h = maxHeight;
		float w = h * ratio;
		g.DrawImage(logo, x, y, w, h);
	}

	private static readonly string CfxLogoBase64 =
"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";

private static void DrawMetaRow(Graphics g, Pen pen, Font labelFont, Font valueFont, string label, string value, float x, float y, float labelW, float valueW, float h)
	{
		RectangleF labelRect = new RectangleF(x, y, labelW, h);
		RectangleF valueRect = new RectangleF(x + labelW, y, valueW, h);
		g.FillRectangle(new SolidBrush(Color.FromArgb(220, 235, 252)), labelRect);
		g.FillRectangle(Brushes.White, valueRect);
		g.DrawRectangle(pen, labelRect.X, labelRect.Y, labelRect.Width, labelRect.Height);
		g.DrawRectangle(pen, valueRect.X, valueRect.Y, valueRect.Width, valueRect.Height);
		DrawCenteredText(g, labelFont, label, new RectangleF(labelRect.X + 3f, labelRect.Y + 2f, labelRect.Width - 6f, labelRect.Height - 4f));
		DrawCenteredText(g, valueFont, value, new RectangleF(valueRect.X + 3f, valueRect.Y + 2f, valueRect.Width - 6f, valueRect.Height - 4f));
	}

	private static void DrawCell(Graphics g, Pen pen, Font headFont, Font cellFont, string text, float x, float y, float w, float h, bool header, Color fill)
	{
		RectangleF rect = new RectangleF(x, y, w, h);
		g.FillRectangle(new SolidBrush(fill), rect);
		g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
		RectangleF textRect = new RectangleF(rect.X + 4f, rect.Y + 4f, rect.Width - 8f, rect.Height - 8f);
		if (header)
			DrawWrappedCenteredText(g, headFont, text ?? string.Empty, textRect);
		else
			DrawCenteredText(g, cellFont, text ?? string.Empty, textRect);
	}

	private static void DrawWrappedCenteredText(Graphics g, Font font, string text, RectangleF rect)
	{
		StringFormat sf = new StringFormat
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center,
			Trimming = StringTrimming.Word,
			FormatFlags = StringFormatFlags.LineLimit
		};
		g.DrawString(text, font, Brushes.Black, rect, sf);
	}

	private static void DrawCenteredText(Graphics g, Font font, string text, RectangleF rect)
	{
		StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		g.DrawString(text, font, Brushes.Black, rect, sf);
	}
}
