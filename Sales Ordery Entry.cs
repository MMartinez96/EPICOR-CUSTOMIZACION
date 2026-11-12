// **************************************************
// Custom code for SalesOrderForm — CORREGIDO (MOQ al Leave, lógica MOQ 50=50)
// + Validación IMMEX/Nacional al guardar (CFXCH, CFXJZ, CFXMTY) — v3.1
// + BAQNACIONALCUSTOMER — v3.8 NACIONAL vs NO NACIONAL (vacío = NO NACIONAL)
//   Solo aplica a clientes del listado NacionalValidationCustIds (NÚMERO CLIENTE / CustID)
//   C094 CODIFLEX INC → EXCLUIDO de toda restricción NACIONAL/NO NACIONAL e IMMEX ProdCode
//   SERVURG → EXCLUIDO de toda restricción NACIONAL/NO NACIONAL e IMMEX ProdCode
//   NACIONAL     → bloquea FLX-IMMX / FLX-NORM
//   NO NACIONAL  → solo permite FLX-IMMX / FLX-NORM (bloquea COMER, MUESTRA, OFF_IMMX, OFF_NORM, etc.)
//   Ship To con Embarca vacío = domicilio principal Epicor (válido en CFXJZ y demás plantas)
// v3.9: Parte SERVURG excluida de validación NACIONAL/NO NACIONAL e IMMEX ProdCode (OV, factura, etc.)
// v4.0: Moneda OV bloqueada = moneda del cliente (UI + validación al guardar; evita USD por error)
// v4.1: Bloqueo combo moneda en toolbar (_sbtCurrencyToggle / Currency*)
// v4.2: PSCurrCode e InvCurrCode en Header > Detail — solo lectura
// v4.3: Pack Slip Currency — búsqueda por DataField + bloqueo reforzado
// v4.4: Pack Slip — bloquea TODOS los controles PSCurrCode/InvCurrCode en el form
// v4.5: C1417 COPELAND Ship To 1 (El Paso USA) excluido de validación NACIONAL/NO NACIONAL
// v4.6: C724 REGAL BELOIT Ship To 1 BRES GROUP (Eagle Pass USA) excluido — exportación
// v4.7: MOQ CFXJZ — valida al cargar línea, al salir de cantidad y al guardar; qty desde DataView
// v4.8: MOQ solo planta CFXJZ — CFXCH (y demás sites) sin restricción MOQ
// v4.9: C485 THERMO-DISC Ship To NACIONAL (COMPONENTES AVANZADOS DE MEXICO, Delicias CHH) excluido — validación NACIONAL/NO NACIONAL e IMMEX ProdCode
// Copiar este contenido al Script de personalización de Epicor.
// **************************************************

extern alias Erp_Contracts_BO_AlternatePart;
extern alias Erp_Contracts_BO_SalesOrder;
extern alias Erp_Contracts_BO_Quote;
extern alias Erp_Contracts_BO_Part;
extern alias Erp_Contracts_BO_Customer;
extern alias Erp_Contracts_BO_OrderDtlSearch;
extern alias Erp_Contracts_BO_OrderHist;
extern alias Erp_Contracts_BO_RMAProc;
extern alias Erp_Contracts_BO_QuoteDtlSearch;
extern alias Erp_Contracts_BO_SerialNumberSearch;
extern alias Erp_Contracts_BO_ShipTo;

using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Infragistics.Win.UltraWinToolbars;

public class Script
{
	// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **
	// Begin Wizard Added Module Level Variables **
	private EpiDataView edvOrderHed;
	private EpiDataView edvOrderDtl;
	private EpiBaseAdapter oTrans_ordAdapter;
	private EpiNumericEditor ventacantidad;
	// End Wizard Added Module Level Variables **

	// Add Custom Module Level Variables Here **
	private EpiTextBox txtShipToNumControl;
	private EpiTextBox txtShipToCustIdControl;
	private bool _clearingOrderDtlLine;
	private Control _currencyControl;
	private Control _psCurrControl;
	private Control _invCurrControl;
	private bool _psCurrLockHandlerAttached;

	private const string PSCurrCodeControlGuid = "077600b3-005e-4d2e-acbd-1578ebb23407";
	private const string InvCurrCodeControlGuid = "ad7d53be-413c-40da-89c2-bf38d2131a8a";

	public void InitializeCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
		// Begin Wizard Added Variable Initialization

		this.edvOrderDtl = ((EpiDataView)(this.oTrans.EpiDataViews["OrderDtl"]));
		this.edvOrderDtl.EpiViewNotification += new EpiViewNotification(this.edvOrderDtl_EpiViewNotification);
		this.baseToolbarsManager.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
		this.edvOrderHed = ((EpiDataView)(this.oTrans.EpiDataViews["OrderHed"]));
		this.edvOrderHed.EpiViewNotification += new EpiViewNotification(this.edvOrderHed_EpiViewNotification_Currency);
		this.oTrans_ordAdapter = ((EpiBaseAdapter)(this.csm.TransAdaptersHT["oTrans_ordAdapter"]));
		this.oTrans_ordAdapter.BeforeAdapterMethod += new BeforeAdapterMethod(this.oTrans_ordAdapter_BeforeAdapterMethod);
		// End Wizard Added Variable Initialization

		ventacantidad = (EpiNumericEditor)csm.GetNativeControlReference("fed8fcf1-c577-4d1f-9e27-b179b78eeb49");
		if (ventacantidad != null)
		{
			ventacantidad.Leave += new EventHandler(ventacantidad_Leave);
		}

		txtShipToNumControl = (EpiTextBox)csm.GetNativeControlReference("2fb94962-2ed8-42cd-963a-5bccaa2513bb");
		txtShipToCustIdControl = (EpiTextBox)csm.GetNativeControlReference("2ec46ff0-fd14-4616-af4a-5235d6414a1c");

		if (txtShipToNumControl != null)
		{
			txtShipToNumControl.TextChanged += new EventHandler(ShipToControl_TextChanged);
			txtShipToNumControl.Leave += new EventHandler(ShipToControl_Leave);
		}

		if (txtShipToCustIdControl != null)
		{
			txtShipToCustIdControl.TextChanged += new EventHandler(ShipToControl_TextChanged);
			txtShipToCustIdControl.Leave += new EventHandler(ShipToControl_Leave);
		}

		// Begin Wizard Added Custom Method Calls

		this.txtUPartNum.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtUPartNum_KeyUp);
		this.txtUPartNum.Validated += new System.EventHandler(this.txtUPartNum_Validated);
		// End Wizard Added Custom Method Calls
	}

	public void DestroyCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
		// Begin Wizard Added Object Disposal

		this.txtUPartNum.KeyUp -= new System.Windows.Forms.KeyEventHandler(this.txtUPartNum_KeyUp);
		this.edvOrderDtl.EpiViewNotification -= new EpiViewNotification(this.edvOrderDtl_EpiViewNotification);
		this.edvOrderDtl = null;
		if (this.edvOrderHed != null)
		{
			this.edvOrderHed.EpiViewNotification -= new EpiViewNotification(this.edvOrderHed_EpiViewNotification_Currency);
		}
		this.baseToolbarsManager.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
		this.txtUPartNum.Validated -= new System.EventHandler(this.txtUPartNum_Validated);
		this.oTrans_ordAdapter.BeforeAdapterMethod -= new BeforeAdapterMethod(this.oTrans_ordAdapter_BeforeAdapterMethod);
		this.oTrans_ordAdapter = null;

		if (txtShipToNumControl != null)
		{
			txtShipToNumControl.TextChanged -= new EventHandler(ShipToControl_TextChanged);
			txtShipToNumControl.Leave -= new EventHandler(ShipToControl_Leave);
		}

		if (txtShipToCustIdControl != null)
		{
			txtShipToCustIdControl.TextChanged -= new EventHandler(ShipToControl_TextChanged);
			txtShipToCustIdControl.Leave -= new EventHandler(ShipToControl_Leave);
		}

		txtShipToNumControl = null;
		txtShipToCustIdControl = null;
		_currencyControl = null;
		_psCurrControl = null;
		_invCurrControl = null;
		_psCurrLockHandlerAttached = false;

		if (ventacantidad != null)
		{
			ventacantidad.Leave -= new EventHandler(ventacantidad_Leave);
		}
	}

	private string GetOrderHedValue(params string[] columnNames)
	{
		try
		{
			if (edvOrderHed == null || edvOrderHed.Row < 0)
			{
				return string.Empty;
			}

			foreach (string columnName in columnNames)
			{
				if (edvOrderHed.dataView.Table.Columns.Contains(columnName))
				{
					object value = edvOrderHed.dataView[edvOrderHed.Row][columnName];
					return value == null ? string.Empty : value.ToString().Trim();
				}
			}
		}
		catch
		{
		}

		return string.Empty;
	}

	private static string GetDataRowString(DataRow row, params string[] columnNames)
	{
		if (row == null || row.RowState == DataRowState.Deleted)
		{
			return string.Empty;
		}

		for (int i = 0; i < columnNames.Length; i++)
		{
			if (!row.Table.Columns.Contains(columnNames[i]))
			{
				continue;
			}

			object value = row[columnNames[i]];
			if (value != null && value != DBNull.Value)
			{
				string text = value.ToString().Trim();
				if (text.Length > 0)
				{
					return text;
				}
			}
		}

		return string.Empty;
	}

	private string ResolveShipToNum()
	{
		string shipToNum = GetOrderHedValue("ShipToNum");
		if (!string.IsNullOrWhiteSpace(shipToNum))
		{
			return shipToNum.Trim();
		}

		if (txtShipToNumControl != null && !string.IsNullOrWhiteSpace(txtShipToNumControl.Text))
		{
			return txtShipToNumControl.Text.Trim();
		}

		try
		{
			EpiDataView relView = this.oTrans.EpiDataViews["OrderRel"] as EpiDataView;
			if (relView != null && relView.dataView != null)
			{
				for (int i = 0; i < relView.dataView.Count; i++)
				{
					string relShipTo = GetDataRowString(relView.dataView[i].Row, "ShipToNum");
					if (!string.IsNullOrWhiteSpace(relShipTo))
					{
						return relShipTo.Trim();
					}
				}
			}
		}
		catch
		{
		}

		return string.Empty;
	}

	private const string ImmexValidationVersion = "3.1";
	private const string NacionalValidationVersion = "3.9";
	private const string CurrencyLockVersion = "4.4";
	private const string DefaultOrderCurrencyCode = "MXN";
	private const string MoqPlantId = "CFXJZ";
	private const string MoqExcludedPlantId = "CFXCH";
	private const string MoqBaqName = "nummoc_c_ACTUALIZABLE";

	// Cliente excluido de TODA validación NACIONAL/NO NACIONAL e IMMEX ProdCode
	private const string ExcludedCustIdCodiflex = "C094";

	// Parte excluida de TODA validación NACIONAL/NO NACIONAL e IMMEX ProdCode
	private const string ExcludedPartNumServUrg = "SERVURG";

	// C1417 COPELAND — Ship To 1 (1281 Joe Battle, El Paso TX) sin restricción ProdCode
	private const string ExcludedCopelandCustId = "C1417";
	private const string ExcludedCopelandShipToNum = "1";

	// C724 REGAL BELOIT — Ship To 1 BRES GROUP (1746 Brown St, Eagle Pass TX USA) exportación
	private const string ExcludedRegalCustId = "C724";
	private const string ExcludedRegalShipToNum = "1";

	// C485 THERMO-DISC — Ship To NACIONAL (COMPONENTES AVANZADOS DE MEXICO, Delicias CHH)
	private const string ExcludedThermoDiscCustId = "C485";
	private const string ExcludedThermoDiscShipToNum = "NACIONAL";

	// Listado autorizado — validación NACIONAL / NO NACIONAL solo por NÚMERO CLIENTE (CustID)
	// NOTA: C094 CODIFLEX INC no está en el listado (excluido por completo)
	private static readonly string[] NacionalValidationCustIds =
	{
		"C485",   // THERM-O-DISC
		"C512",   // INTERMETRO
		"C664",   // FLEX
		"C724",   // REGAL
		"C827",   // WHITE RODGERS
		"C918",   // GECKO
		"C1023",  // LEONI
		"C1149",  // LEXMARK
		"C1217",  // MERSEN
		"C1285",  // COOPER
		"C1372",  // TECHTRONIC
		"C1378",  // FRANKLIN
		"C1417",  // COPELAND
		"C1428",  // MOLEX
		"C1480"   // WNC
	};

	private static readonly string[] ProdCodesFlexoExportOnly = { "FLX-IMMX", "FLXIMMX", "FLX-NORM", "FLXNORM" };
	private static readonly string[] ProdCodesBlockedWhenNoNacional = { "COMER", "MUESTRA", "OFF_IMMX", "OFFIMMX", "OFF-IMMX", "OFF_NORM", "OFFNORM", "OFF-NORM" };

	private static bool IsNacionalValidationCustomer(string custId)
	{
		if (string.IsNullOrWhiteSpace(custId))
			return false;

		custId = custId.Trim();
		for (int i = 0; i < NacionalValidationCustIds.Length; i++)
		{
			if (string.Equals(NacionalValidationCustIds[i], custId, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	private static bool IsExcludedFromNacionalImmexValidation(string partNum)
	{
		if (string.IsNullOrWhiteSpace(partNum))
			return false;

		return string.Equals(partNum.Trim(), ExcludedPartNumServUrg, StringComparison.OrdinalIgnoreCase);
	}

	private bool IsCopelandExcludedShipTo()
	{
		return IsExcludedCustShipTo(ExcludedCopelandCustId, ExcludedCopelandShipToNum);
	}

	private bool IsRegalExcludedShipTo()
	{
		return IsExcludedCustShipTo(ExcludedRegalCustId, ExcludedRegalShipToNum);
	}
/////////////////////////////////THERMODISC DELICIAS/////////////////////////////////////////
	private bool IsThermoDiscExcludedShipTo()
	{
		return IsExcludedCustShipTo(ExcludedThermoDiscCustId, ExcludedThermoDiscShipToNum);
	}

	private bool IsExcludedCustShipTo(string excludedCustId, string excludedShipToNum)
	{
		string custId = ResolveShipToCustId();
		if (string.IsNullOrWhiteSpace(custId))
			custId = ResolveSoldToCustId();
		if (string.IsNullOrWhiteSpace(custId))
			return false;

		if (!string.Equals(custId.Trim(), excludedCustId, StringComparison.OrdinalIgnoreCase))
			return false;

		return string.Equals(ResolveShipToNum().Trim(), excludedShipToNum, StringComparison.OrdinalIgnoreCase);
	}

	private bool ShouldApplyNacionalProdCodeValidation()
	{
		if (IsCodiflexIncExcludedCustomer())
			return false;

		if (IsCopelandExcludedShipTo())
			return false;

		if (IsRegalExcludedShipTo())
			return false;

		if (IsThermoDiscExcludedShipTo())
			return false;

		return IsNacionalValidationCustomer(ResolveShipToCustId())
			|| IsNacionalValidationCustomer(ResolveSoldToCustId());
	}

	private string ResolveSoldToCustId()
	{
		string soldToCustId = GetOrderHedValue("CustID", "CustomerCustID", "Customer_CustID", "BTCustID");
		if (!string.IsNullOrWhiteSpace(soldToCustId))
			return soldToCustId.Trim();

		return ResolveCustIdFromCustomerView();
	}

	private bool IsCodiflexIncExcludedCustomer()
	{
		string[] custIds = new string[]
		{
			ResolveShipToCustId(),
			ResolveSoldToCustId(),
			ResolveCustIdFromCustomerView()
		};

		for (int i = 0; i < custIds.Length; i++)
		{
			if (string.Equals(custIds[i], ExcludedCustIdCodiflex, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	private static bool IsShipToNacional(string nacional_c)
	{
		return !string.IsNullOrWhiteSpace(nacional_c);
	}

	private static bool IsFlexoExportProdCode(string prodCode)
	{
		return MatchesProdCodeList(prodCode, ProdCodesFlexoExportOnly);
	}

	private static bool MatchesProdCodeList(string prodCode, string[] list)
	{
		if (string.IsNullOrWhiteSpace(prodCode) || list == null)
			return false;

		prodCode = prodCode.Trim();
		for (int i = 0; i < list.Length; i++)
		{
			if (string.Equals(list[i], prodCode, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	private static bool TryGetProdCodeRestriction(string nacional_c, string prodCode, out string detail)
	{
		detail = string.Empty;
		if (string.IsNullOrWhiteSpace(prodCode))
			return false;

		if (IsShipToNacional(nacional_c))
		{
			if (IsFlexoExportProdCode(prodCode))
			{
				detail = "Ship To NACIONAL no permite ProdCode FLX-IMMX / FLX-NORM.";
				return true;
			}

			return false;
		}

		// NO NACIONAL — campo vacío cuenta como NO NACIONAL
		if (IsFlexoExportProdCode(prodCode))
			return false;

		if (MatchesProdCodeList(prodCode, ProdCodesBlockedWhenNoNacional))
		{
			detail = "Ship To NO NACIONAL no permite ProdCode " + prodCode.Trim()
				+ " (COMER, MUESTRA, OFF_IMMX, OFF_NORM). Solo FLX-IMMX / FLX-NORM.";
			return true;
		}

		detail = "Ship To NO NACIONAL solo permite ProdCode FLX-IMMX / FLX-NORM.";
		return true;
	}

	private string ResolveShipToCustId()
	{
		string shipToCustId = GetOrderHedValue("ShipToCustID", "ShipToCustId", "CustID", "CustomerCustID", "Customer_CustID");
		if (!string.IsNullOrWhiteSpace(shipToCustId))
		{
			return shipToCustId.Trim();
		}

		if (txtShipToCustIdControl != null && !string.IsNullOrWhiteSpace(txtShipToCustIdControl.Text))
		{
			return txtShipToCustIdControl.Text.Trim();
		}

		shipToCustId = GetOrderHedValue("CustNum", "CustomerID");
		if (!string.IsNullOrWhiteSpace(shipToCustId))
		{
			return shipToCustId.Trim();
		}

		shipToCustId = ResolveCustIdFromCustomerView();
		if (!string.IsNullOrWhiteSpace(shipToCustId))
		{
			return shipToCustId.Trim();
		}

		try
		{
			EpiDataView relView = this.oTrans.EpiDataViews["OrderRel"] as EpiDataView;
			if (relView != null && relView.dataView != null)
			{
				for (int i = 0; i < relView.dataView.Count; i++)
				{
					string relCustId = GetDataRowString(relView.dataView[i].Row,
						"ShipToCustNum", "ShipToCustID", "ShipToCustId", "CustID", "CustNum");
					if (!string.IsNullOrWhiteSpace(relCustId))
					{
						return relCustId.Trim();
					}
				}
			}
		}
		catch
		{
		}

		return string.Empty;
	}

	private string ResolveCustIdFromCustomerView()
	{
		try
		{
			EpiDataView custView = this.oTrans.EpiDataViews["Customer"] as EpiDataView;
			if (custView == null || custView.dataView == null || custView.Row < 0)
			{
				return string.Empty;
			}

			return GetDataRowString(custView.dataView[custView.Row].Row, "CustID", "CustomerID", "CustNum");
		}
		catch
		{
			return string.Empty;
		}
	}

	private string GetConsolidadoDisplayOrBaq()
	{
		try
		{
			if (consolidadotext != null && !string.IsNullOrWhiteSpace(consolidadotext.Text))
			{
				return consolidadotext.Text.Trim();
			}
		}
		catch
		{
		}

		return GetConsolidadoFromBaq();
	}

	private bool HasOrderHedShipToAddress()
	{
		string[] addressColumns = new string[]
		{
			"ShipToAddr1", "ShipToAddress1", "ShipToCity", "ShipToState", "ShipToZIP",
			"ShipToCountry", "Calc_ShipToAddressList"
		};

		for (int i = 0; i < addressColumns.Length; i++)
		{
			if (!string.IsNullOrWhiteSpace(GetOrderHedValue(addressColumns[i])))
			{
				return true;
			}
		}

		return false;
	}

	private bool IsShipToCaptured()
	{
		if (!string.IsNullOrWhiteSpace(GetConsolidadoDisplayOrBaq()))
		{
			return true;
		}

		string shipToCustId = ResolveShipToCustId();
		if (!string.IsNullOrWhiteSpace(shipToCustId))
		{
			return true;
		}

		if (HasOrderHedShipToAddress())
		{
			return true;
		}

		if (!string.IsNullOrWhiteSpace(ResolveShipToNum()))
		{
			return true;
		}

		return false;
	}

	private static string FormatShipToLabel(string shipToNum, string shipToCustId)
	{
		if (!string.IsNullOrWhiteSpace(shipToNum))
		{
			return shipToNum.Trim();
		}

		if (!string.IsNullOrWhiteSpace(shipToCustId))
		{
			return shipToCustId.Trim() + " (dom. principal)";
		}

		return "(dom. principal)";
	}

	private static bool ResolveIsNacionalShipTo(string shipToNum, string consolidado)
	{
		if (string.Equals((shipToNum ?? string.Empty).Trim(), "NACIONAL", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!string.IsNullOrWhiteSpace(consolidado))
		{
			string normalized = consolidado.Trim().ToUpperInvariant();
			if (normalized.IndexOf("NACIONAL", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (normalized.IndexOf(" IMPO", StringComparison.Ordinal) >= 0
				|| normalized.EndsWith("IMPO", StringComparison.Ordinal))
			{
				return false;
			}
		}

		return false;
	}

	private string ExecuteConsolidadoBaq(string idShip, string idCliente)
	{
		if (string.IsNullOrWhiteSpace(idCliente))
		{
			return string.Empty;
		}

		using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
		{
			adapterDynamicQuery.BOConnect();
			const string baqName = "BAQCONSOLIDADOOVEMB";
			QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("IDSHIP", idShip ?? string.Empty, "nvarchar", false, Guid.Empty, "A");
			ds.ExecutionParameter.AddExecutionParameterRow("IDCLIENTE", idCliente.Trim(), "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(baqName, ds);

			System.Data.DataTable results = adapterDynamicQuery.QueryResults.Tables["Results"];
			if (results == null || results.Rows.Count == 0)
			{
				return string.Empty;
			}

			object cell0 = results.Rows[0][0];
			return cell0 == null ? string.Empty : cell0.ToString().Trim();
		}
	}

	private string QueryShipToNacional_c()
	{
		string shipTo = ResolveShipToNum();
		string custId = ResolveShipToCustId();
		if (string.IsNullOrWhiteSpace(custId))
			return string.Empty;

		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(this.oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("SHIPTO", shipTo ?? string.Empty, "nvarchar", false, Guid.Empty, "A");
				ds.ExecutionParameter.AddExecutionParameterRow("CUSTID", custId, "nvarchar", false, Guid.Empty, "B");
				dq.ExecuteByID("BAQNACIONALCUSTOMER", ds);

				System.Data.DataTable t = dq.QueryResults.Tables["Results"];
				if (t == null || t.Rows.Count == 0)
					return string.Empty;

				System.Data.DataRow r = t.Rows[0];
				string[] hints = new string[] { "Nacional_c", "ShipTo_Nacional_c" };
				for (int c = 0; c < t.Columns.Count; c++)
				{
					string col = t.Columns[c].ColumnName ?? string.Empty;
					for (int h = 0; h < hints.Length; h++)
					{
						if (col.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) < 0)
							continue;
						object val = r[c];
						return val == null || val == DBNull.Value ? string.Empty : val.ToString().Trim();
					}
				}

				object cell0 = r[0];
				return cell0 == null || cell0 == DBNull.Value ? string.Empty : cell0.ToString().Trim();
			}
		}
		catch
		{
			return string.Empty;
		}
	}

	private static bool IsProdCodeRequiringNacionalShipTo(string prodCode)
	{
		return IsFlexoExportProdCode(prodCode);
	}

	private bool ValidateNacionalShipToForCurrentLine()
	{
		if (_clearingOrderDtlLine)
			return true;

		if (edvOrderDtl == null || edvOrderDtl.Row < 0 || edvOrderDtl.dataView == null)
			return true;

		return ValidateNacionalShipToForRow(edvOrderDtl.dataView[edvOrderDtl.Row].Row, true);
	}

	private bool ValidateNacionalShipToForRow(System.Data.DataRow row, bool clearLineOnFail)
	{
		if (row == null || row.RowState == DataRowState.Deleted)
			return true;

		if (!ShouldApplyNacionalProdCodeValidation())
			return true;

		string partNum = GetOrderDtlRowString(row, "PartNum");
		if (string.IsNullOrWhiteSpace(partNum))
			return true;

		if (IsExcludedFromNacionalImmexValidation(partNum))
			return true;

		string prodCode = GetOrderDtlRowString(row, "ProdCode");
		string nacional = QueryShipToNacional_c();
		string detail;
		if (!TryGetProdCodeRestriction(nacional, prodCode, out detail))
			return true;

		string message = "REVISA LA DIRECCIÓN DEL SHIP TO CON FACTURACIÓN";
		if (!string.IsNullOrWhiteSpace(detail))
			message += "\r\n\r\n" + detail;

		MessageBox.Show(
			message,
			"Ship To / Grupo de producto",
			MessageBoxButtons.OK,
			MessageBoxIcon.Warning);

		if (clearLineOnFail)
			ClearCurrentOrderDtlLineFields();

		return false;
	}

	private string ResolveCustomerCurrencyCode()
	{
		try
		{
			EpiDataView custView = this.oTrans.EpiDataViews["Customer"] as EpiDataView;
			if (custView != null && custView.dataView != null && custView.Row >= 0)
			{
				string cur = GetDataRowString(custView.dataView[custView.Row].Row, "CurrencyCode");
				if (!string.IsNullOrWhiteSpace(cur))
					return cur.Trim().ToUpperInvariant();
			}
		}
		catch
		{
		}

		string hedCur = GetOrderHedValue("CurrencyCode");
		if (!string.IsNullOrWhiteSpace(hedCur))
			return hedCur.Trim().ToUpperInvariant();

		return DefaultOrderCurrencyCode;
	}

	private void ApplyOrderCurrencyFromCustomer()
	{
		if (edvOrderHed == null || edvOrderHed.Row < 0 || edvOrderHed.dataView == null)
			return;

		string custCur = ResolveCustomerCurrencyCode();
		if (string.IsNullOrWhiteSpace(custCur))
			custCur = DefaultOrderCurrencyCode;

		DataRow row = edvOrderHed.dataView[edvOrderHed.Row].Row;
		if (row == null || row.RowState == DataRowState.Deleted)
			return;

		if (!row.Table.Columns.Contains("CurrencyCode"))
			return;

		string current = GetOrderHedValue("CurrencyCode");
		if (string.Equals(current, custCur, StringComparison.OrdinalIgnoreCase))
			return;

		row.BeginEdit();
		row["CurrencyCode"] = custCur;
		row.EndEdit();
	}

	private static string GetControlDataField(Control control)
	{
		if (control == null)
			return string.Empty;

		try
		{
			PropertyInfo pi = control.GetType().GetProperty("DataField");
			if (pi != null)
			{
				object value = pi.GetValue(control, null);
				return value == null ? string.Empty : value.ToString().Trim();
			}
		}
		catch
		{
		}

		return string.Empty;
	}

	private static void CollectControls(Control parent, System.Collections.Generic.List<Control> list)
	{
		if (parent == null || list == null)
			return;

		list.Add(parent);
		foreach (Control child in parent.Controls)
			CollectControls(child, list);
	}

	private Control FindControlByDataField(Control root, string dataField)
	{
		if (root == null || string.IsNullOrWhiteSpace(dataField))
			return null;

		System.Collections.Generic.List<Control> all = new System.Collections.Generic.List<Control>();
		CollectControls(root, all);

		for (int i = 0; i < all.Count; i++)
		{
			if (string.Equals(GetControlDataField(all[i]), dataField, StringComparison.OrdinalIgnoreCase))
				return all[i];
		}

		return null;
	}

	private Control FindControlByNameFragment(Control root, string fragment)
	{
		if (root == null || string.IsNullOrWhiteSpace(fragment))
			return null;

		System.Collections.Generic.List<Control> all = new System.Collections.Generic.List<Control>();
		CollectControls(root, all);

		for (int i = 0; i < all.Count; i++)
		{
			string name = all[i].Name ?? string.Empty;
			if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
				return all[i];
		}

		return null;
	}

	private void LockAllControlsWithDataField(Control root, string dataField)
	{
		if (root == null || string.IsNullOrWhiteSpace(dataField))
			return;

		System.Collections.Generic.List<Control> all = new System.Collections.Generic.List<Control>();
		CollectControls(root, all);

		for (int i = 0; i < all.Count; i++)
		{
			if (string.Equals(GetControlDataField(all[i]), dataField, StringComparison.OrdinalIgnoreCase))
				SetDetailCurrencyControlReadOnly(all[i], true);
		}
	}

	private static void DisableToolbarTool(ToolBase tool)
	{
		if (tool == null)
			return;

		tool.SharedProps.Enabled = false;

		ComboBoxTool combo = tool as ComboBoxTool;
		if (combo != null)
		{
			try { combo.DropDownStyle = Infragistics.Win.DropDownStyle.DropDownList; }
			catch { }
		}
	}

	private void LockCurrencyToolbarTools()
	{
		if (baseToolbarsManager == null)
			return;

		string[] keys = new string[]
		{
			"_sbtCurrencyToggle",
			"CurrencyCode",
			"DocCurrencyCode",
			"curCurrencyCode",
			"CurrencyToggle"
		};

		for (int i = 0; i < keys.Length; i++)
		{
			if (baseToolbarsManager.Tools.Exists(keys[i]))
				DisableToolbarTool(baseToolbarsManager.Tools[keys[i]]);
		}

		foreach (ToolBase tool in baseToolbarsManager.Tools)
		{
			if (tool == null)
				continue;

			string key = tool.Key ?? string.Empty;
			if (key.IndexOf("Currency", StringComparison.OrdinalIgnoreCase) >= 0
				|| key.IndexOf("Curr", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				DisableToolbarTool(tool);
			}
		}
	}

	private void LockCurrencyUiControl()
	{
		if (_currencyControl == null && SalesOrderForm != null && !SalesOrderForm.IsDisposed)
		{
			_currencyControl = FindControlByDataField(SalesOrderForm, "CurrencyCode");
			if (_currencyControl == null)
				_currencyControl = FindControlByDataField(SalesOrderForm, "DocCurrencyCode");
			if (_currencyControl == null)
				_currencyControl = FindControlByNameFragment(SalesOrderForm, "Currency");
		}

		if (_currencyControl != null)
			SetControlReadOnly(_currencyControl, true);

		LockCurrencyToolbarTools();
	}

	private void LockDetailCurrencyControls()
	{
		if (SalesOrderForm != null && !SalesOrderForm.IsDisposed)
		{
			LockAllControlsWithDataField(SalesOrderForm, "PSCurrCode");
			LockAllControlsWithDataField(SalesOrderForm, "InvCurrCode");

			if (_psCurrControl == null || _psCurrControl.Enabled)
			{
				_psCurrControl = csm.GetNativeControlReference(PSCurrCodeControlGuid) as Control;
				if (_psCurrControl == null)
					_psCurrControl = FindControlByDataField(SalesOrderForm, "PSCurrCode");
				if (_psCurrControl == null)
					_psCurrControl = FindControlByNameFragment(SalesOrderForm, "PSCurr");
			}

			if (_invCurrControl == null)
			{
				_invCurrControl = csm.GetNativeControlReference(InvCurrCodeControlGuid) as Control;
				if (_invCurrControl == null)
					_invCurrControl = FindControlByDataField(SalesOrderForm, "InvCurrCode");
			}
		}

		EnsurePsCurrLockHandler(_psCurrControl);
		SetDetailCurrencyControlReadOnly(_psCurrControl, true);
		SetDetailCurrencyControlReadOnly(_invCurrControl, true);
	}

	private void EnsurePsCurrLockHandler(Control control)
	{
		if (control == null || _psCurrLockHandlerAttached)
			return;

		control.BackColorChanged += new EventHandler(PsCurrControl_BackColorChanged);
		_psCurrLockHandlerAttached = true;
	}

	private void PsCurrControl_BackColorChanged(object sender, EventArgs e)
	{
		SetDetailCurrencyControlReadOnly(sender as Control, true);
	}

	private static void SetDetailCurrencyControlReadOnly(Control control, bool readOnly)
	{
		if (control == null)
			return;

		try
		{
			PropertyInfo pi = control.GetType().GetProperty("ReadOnly");
			if (pi != null && pi.CanWrite)
				pi.SetValue(control, readOnly, null);
		}
		catch
		{
		}

		control.Enabled = !readOnly;

		try
		{
			PropertyInfo dropPi = control.GetType().GetProperty("DropDownStyle");
			if (dropPi != null && readOnly)
				dropPi.SetValue(control, Infragistics.Win.DropDownStyle.DropDownList, null);
		}
		catch
		{
		}
	}

	private static void SetControlReadOnly(Control control, bool readOnly)
	{
		if (control == null)
			return;

		try
		{
			PropertyInfo pi = control.GetType().GetProperty("ReadOnly");
			if (pi != null && pi.CanWrite)
			{
				pi.SetValue(control, readOnly, null);
				return;
			}
		}
		catch
		{
		}

		control.Enabled = !readOnly;
	}

	private static string GetEpiNotifyColumnName(EpiDataView view, EpiNotifyArgs args)
	{
		try
		{
			if (view == null || view.dataView == null || view.dataView.Table == null || args == null)
				return string.Empty;

			object colRaw = args.Column;
			DataColumn dc = colRaw as DataColumn;
			if (dc != null)
				return dc.ColumnName;

			int idx = -1;
			if (colRaw != null)
			{
				try { idx = Convert.ToInt32(colRaw); }
				catch { return string.Empty; }
			}

			if (idx >= 0 && idx < view.dataView.Table.Columns.Count)
				return view.dataView.Table.Columns[idx].ColumnName;
		}
		catch
		{
		}

		return string.Empty;
	}

	private void EnforceOrderCurrencyLock()
	{
		ApplyOrderCurrencyFromCustomer();
		LockCurrencyUiControl();
		LockDetailCurrencyControls();
	}

	private void edvOrderHed_EpiViewNotification_Currency(EpiDataView view, EpiNotifyArgs args)
	{
		if (view == null || args == null)
			return;

		ApplyOrderCurrencyFromCustomer();

		if (args.NotifyType == EpiTransaction.NotifyType.Initialize)
		{
			LockCurrencyUiControl();
			LockDetailCurrencyControls();
			return;
		}

		string col = GetEpiNotifyColumnName(view, args);
		if (!string.IsNullOrWhiteSpace(col))
		{
			col = col.Trim();
			if (string.Equals(col, "CustNum", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(col, "BTCustNum", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(col, "CustID", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(col, "CustomerCustID", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(col, "CurrencyCode", StringComparison.OrdinalIgnoreCase))
			{
				ApplyOrderCurrencyFromCustomer();
				LockCurrencyUiControl();
				LockDetailCurrencyControls();
			}
		}
	}

	private void ValidateOrderCurrencyOnSave()
	{
		if (edvOrderHed == null || edvOrderHed.Row < 0)
			return;

		string custCur = ResolveCustomerCurrencyCode();
		if (string.IsNullOrWhiteSpace(custCur))
			custCur = DefaultOrderCurrencyCode;

		string ordCur = GetOrderHedValue("CurrencyCode");
		if (string.IsNullOrWhiteSpace(ordCur))
			ordCur = custCur;

		ordCur = ordCur.Trim().ToUpperInvariant();

		if (!string.Equals(ordCur, custCur, StringComparison.OrdinalIgnoreCase))
		{
			throw new UIException(
				"[Moneda v" + CurrencyLockVersion + "] La moneda de la orden (" + ordCur + ") "
				+ "no coincide con la moneda del cliente (" + custCur + ").\r\n\r\n"
				+ "No se permite cambiar la moneda manualmente. "
				+ "Si el cliente debe facturarse en otra moneda, corrija el maestro de Cliente o contacte a Finanzas.");
		}

		if (string.Equals(custCur, DefaultOrderCurrencyCode, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(ordCur, DefaultOrderCurrencyCode, StringComparison.OrdinalIgnoreCase))
		{
			throw new UIException(
				"[Moneda v" + CurrencyLockVersion + "] Este cliente factura en PESOS (MXN). "
				+ "No se puede guardar la orden en " + ordCur + ".");
		}
	}

	private void ValidateNacionalProdCodesOnSave()
	{
		if (edvOrderDtl == null || edvOrderDtl.dataView == null)
			return;

		if (!ShouldApplyNacionalProdCodeValidation())
			return;

		string nacional = QueryShipToNacional_c();
		bool isNacional = IsShipToNacional(nacional);
		System.Text.StringBuilder blocked = new System.Text.StringBuilder();
		System.Data.DataView detailView = edvOrderDtl.dataView;

		for (int rowIndex = 0; rowIndex < detailView.Count; rowIndex++)
		{
			System.Data.DataRow row = detailView[rowIndex].Row;
			if (row.RowState == DataRowState.Deleted)
				continue;

			string partNum = GetOrderDtlRowString(row, "PartNum");
			if (string.IsNullOrWhiteSpace(partNum))
				continue;

			if (IsExcludedFromNacionalImmexValidation(partNum))
				continue;

			string prodCode = GetOrderDtlRowString(row, "ProdCode");
			string detail;
			if (!TryGetProdCodeRestriction(nacional, prodCode, out detail))
				continue;

			string orderLine = GetOrderDtlRowString(row, "OrderLine");
			if (string.IsNullOrWhiteSpace(orderLine))
				orderLine = Convert.ToString(rowIndex + 1);

			blocked.AppendLine(string.Format(
				"Línea {0}: Parte {1}, ProdCode '{2}' — {3}",
				orderLine,
				partNum,
				string.IsNullOrWhiteSpace(prodCode) ? "(vacío)" : prodCode,
				detail));
		}

		if (blocked.Length == 0)
			return;

		string header = isNacional
			? "REVISA LA DIRECCIÓN DEL SHIP TO CON FACTURACIÓN\r\n\r\nShip To NACIONAL — líneas no permitidas:\r\n"
			: "REVISA LA DIRECCIÓN DEL SHIP TO CON FACTURACIÓN\r\n\r\nShip To NO NACIONAL — solo FLX-IMMX / FLX-NORM:\r\n";

		throw new UIException(header + blocked.ToString());
	}

	private void ClearCurrentOrderDtlLineFields()
	{
		_clearingOrderDtlLine = true;
		try
		{
			if (edvOrderDtl != null && edvOrderDtl.Row >= 0 && edvOrderDtl.dataView != null)
			{
				System.Data.DataRow row = edvOrderDtl.dataView[edvOrderDtl.Row].Row;
				if (row.RowState != DataRowState.Deleted)
				{
					row.BeginEdit();
					ClearOrderDtlColumn(row, "PartNum", string.Empty);
					ClearOrderDtlColumn(row, "LineDesc", string.Empty);
					ClearOrderDtlColumn(row, "ProdCode", string.Empty);
					ClearOrderDtlColumn(row, "OrderQty", 0m);
					ClearOrderDtlColumn(row, "SellingQuantity", 0m);
					ClearOrderDtlColumn(row, "UnitPrice", 0m);
					row.EndEdit();
				}
			}

			if (txtUPartNum != null)
				txtUPartNum.Text = string.Empty;

			txtCostoTotal.Text = string.Empty;
			nummin.Text = string.Empty;
			renta.Text = string.Empty;
			epiNumericEditorC1.Text = string.Empty;
			arancelaria.Text = string.Empty;
			validacionparte.Text = string.Empty;
			canthn.Text = string.Empty;
			resmillar.Text = string.Empty;
			productoserv.Text = string.Empty;

			oTrans.Refresh();
		}
		finally
		{
			_clearingOrderDtlLine = false;
		}
	}

	private static void ClearOrderDtlColumn(System.Data.DataRow row, string columnName, object emptyValue)
	{
		if (row == null || !row.Table.Columns.Contains(columnName))
			return;

		try
		{
			row[columnName] = emptyValue;
		}
		catch
		{
		}
	}

	private void ShipToControl_TextChanged(object sender, EventArgs e)
	{
		if (SalesOrderForm != null && !SalesOrderForm.IsDisposed)
		{
			SalesOrderForm.BeginInvoke((MethodInvoker)delegate
			{
				consultaConsolidadoOVEMB();
			});
		}
	}

	private void ShipToControl_Leave(object sender, EventArgs e)
	{
		consultaConsolidadoOVEMB();
	}

	private void ventacantidad_Leave(object sender, EventArgs e)
	{
		if (edvOrderDtl == null || edvOrderDtl.Row < 0)
			return;

		string part = edvOrderDtl.dataView[edvOrderDtl.Row]["PartNum"].ToString();
		if (string.IsNullOrWhiteSpace(part))
			return;

		if (!ShouldApplyMoqValidation())
			return;

		VALIDARCANTIDADVENTA(part);
	}

	private string ResolveOrderPlant()
	{
		string plant = GetOrderHedValue("Plant");
		if (!string.IsNullOrWhiteSpace(plant))
			return plant.Trim();

		if (edvOrderDtl != null && edvOrderDtl.Row >= 0 && edvOrderDtl.dataView != null)
		{
			plant = GetOrderDtlRowString(edvOrderDtl.dataView[edvOrderDtl.Row].Row, "Plant");
			if (!string.IsNullOrWhiteSpace(plant))
				return plant.Trim();
		}

		return string.Empty;
	}

	private static bool IsMoqPlant(string plantId)
	{
		return string.Equals((plantId ?? string.Empty).Trim(), MoqPlantId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsMoqExcludedPlant(string plantId)
	{
		return string.Equals((plantId ?? string.Empty).Trim(), MoqExcludedPlantId, StringComparison.OrdinalIgnoreCase);
	}

	private bool ShouldApplyMoqForRow(DataRow row)
	{
		if (row == null || row.RowState == DataRowState.Deleted)
			return false;

		string sessionPlant = ((Ice.Core.Session)oTrans.Session).PlantID ?? string.Empty;
		if (IsMoqExcludedPlant(sessionPlant))
			return false;

		string linePlant = GetOrderDtlRowString(row, "Plant");
		if (IsMoqExcludedPlant(linePlant))
			return false;
		if (IsMoqPlant(linePlant))
			return true;

		string orderPlant = ResolveOrderPlant();
		if (IsMoqExcludedPlant(orderPlant))
			return false;

		return IsMoqPlant(sessionPlant) || IsMoqPlant(orderPlant);
	}

	private bool ShouldApplyMoqValidation()
	{
		if (edvOrderDtl == null || edvOrderDtl.Row < 0 || edvOrderDtl.dataView == null)
			return false;

		return ShouldApplyMoqForRow(edvOrderDtl.dataView[edvOrderDtl.Row].Row);
	}

	private decimal ResolveSalesOrderQuantityForMoq(DataRow row)
	{
		if (row != null)
		{
			decimal qty;
			string selling = GetOrderDtlRowString(row, "SellingQuantity");
			if (TryParseDecimalFlexible(selling, out qty) && qty != 0m)
				return qty;

			string orderQty = GetOrderDtlRowString(row, "OrderQty");
			if (TryParseDecimalFlexible(orderQty, out qty) && qty != 0m)
				return qty;
		}

		if (ventacantidad != null && !string.IsNullOrWhiteSpace(ventacantidad.Text))
		{
			decimal fromControl;
			if (TryParseDecimalFlexible(ventacantidad.Text, out fromControl))
				return fromControl;
		}

		return 0m;
	}

	private bool TryGetMoqFromBaQ(string partNum, out decimal moq, out string moqDisplay)
	{
		moq = 0m;
		moqDisplay = string.Empty;
		if (string.IsNullOrWhiteSpace(partNum))
			return false;

		using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
		{
			adapterDynamicQuery.BOConnect();
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("PARTE", partNum.Trim(), "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(MoqBaqName, ds);

			System.Data.DataTable results = adapterDynamicQuery.QueryResults.Tables["Results"];
			if (results == null || results.Rows.Count == 0)
				return false;

			object cell0 = results.Rows[0][0];
			moqDisplay = (cell0 == null) ? string.Empty : cell0.ToString();
			return TryParseDecimalFlexible(moqDisplay, out moq);
		}
	}

	private void ValidateMoqOnSave()
	{
		if (edvOrderDtl == null || edvOrderDtl.dataView == null)
			return;

		System.Text.StringBuilder blocked = new System.Text.StringBuilder();
		System.Data.DataView detailView = edvOrderDtl.dataView;

		for (int rowIndex = 0; rowIndex < detailView.Count; rowIndex++)
		{
			System.Data.DataRow row = detailView[rowIndex].Row;
			if (row.RowState == DataRowState.Deleted)
				continue;

			if (!ShouldApplyMoqForRow(row))
				continue;

			string partNum = GetOrderDtlRowString(row, "PartNum");
			if (string.IsNullOrWhiteSpace(partNum))
				continue;

			decimal moq;
			string moqDisplay;
			string orderLine = GetOrderDtlRowString(row, "OrderLine");
			if (string.IsNullOrWhiteSpace(orderLine))
				orderLine = Convert.ToString(rowIndex + 1);

			if (!TryGetMoqFromBaQ(partNum, out moq, out moqDisplay))
			{
				blocked.AppendLine(string.Format(
					"Línea {0}: Parte {1} — no se encontró MOQ en el BAQ.",
					orderLine,
					partNum));
				continue;
			}

			decimal qty = ResolveSalesOrderQuantityForMoq(row);
			if (qty <= 0m || moq <= qty)
				continue;

			blocked.AppendLine(string.Format(
				"Línea {0}: Parte {1}, cantidad {2} — MOQ mínimo {3}.",
				orderLine,
				partNum,
				qty.ToString("0.###"),
				moqDisplay));
		}

		if (blocked.Length == 0)
			return;

		throw new UIException(
			"Cantidad solicitada no cumple con el MOQ. Favor de notificar al cliente y/o ventas.\r\n\r\n"
			+ blocked.ToString());
	}

	private void txtUPartNum_KeyUp(object sender, System.Windows.Forms.KeyEventArgs args)
	{
		// ** Place Event Handling Code Here **
	}

	public void margen()
	{
		decimal costo = decimal.Parse(txtCostoTotal.Text);
		decimal precio = decimal.Parse(edvOrderDtl.dataView[edvOrderDtl.Row]["UnitPrice"].ToString());
		decimal res = ((precio - costo) / precio * 100);
		epiNumericEditorC1.Value = res;
	}

	//////////////////////////////////INICIA MÈTODO  CONSULTACANT VENTA 2////////////////////////////////////////////////////////
	public void VALIDARCANTIDADVENTA2(string parte)
	{
		if (!ShouldApplyMoqValidation())
			return;

		EpiNumericEditor cantventa;
		EpiNumericEditor orderqty;
		EpiTextBox partess;
		string cantidadbaq;
		cantventa = ((EpiNumericEditor)csm.GetNativeControlReference("618db547-897d-4311-871b-507a00ac0046"));
		orderqty = ((EpiNumericEditor)csm.GetNativeControlReference("fed8fcf1-c577-4d1f-9e27-b179b78eeb49"));
		partess = ((EpiTextBox)csm.GetNativeControlReference("e7bda90e-e89b-4693-854a-0ea3c9187e8e"));

		DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
		adapterDynamicQuery.BOConnect();
		string BAQName = "nummoc_c_ACTUALIZABLE";
		QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
		ds.ExecutionParameter.AddExecutionParameterRow("PARTE", parte, "nvarchar", false, Guid.Empty, "A");
		adapterDynamicQuery.ExecuteByID(BAQName, ds);

		if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
		{
			cantidadbaq = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
			decimal cantmocd = Convert.ToDecimal(cantidadbaq);
			decimal cntventd = Convert.ToDecimal(orderqty.Text);
			string thisline = Convert.ToString(cntventd);

			if (cntventd < 0)
			{
				MessageBox.Show("Cantidad solicitada no cumple con el MOQ Favor de notificar al cliente y/o ventas");

				nummin.BackColor = Color.Red;
			}
			else
			{
				if (cantmocd <= cntventd)
				{
					nummin.BackColor = Color.Green;
					nummin.Text = cantidadbaq;
				}
				else
				{
					cantventa.Text = "0.00";

					MessageBox.Show("Cantidad solicitada no cumple con el MOQ Favor de notificar al cliente y/o ventas");

					nummin.Text = cantidadbaq;
					nummin.BackColor = Color.Red;
				}
			}
			adapterDynamicQuery.Dispose();
		}
	}
	/////////////////////////////////////TERMINA CONSULTA VALIDAR CANTIDAD VENTA2 ////////////////////////////////////////////////////////

	//////////////////////////////////// INICIA CONSULTA VALIDACION CANTIDAD VENTA///////////////////////////////////////////////////////

	public void VALIDARCANTIDADVENTA(string parte)
	{
		if (!ShouldApplyMoqValidation())
			return;

		EpiNumericEditor cantventa = (EpiNumericEditor)csm.GetNativeControlReference("618db547-897d-4311-871b-507a00ac0046");
		EpiTextBox partess = (EpiTextBox)csm.GetNativeControlReference("e7bda90e-e89b-4693-854a-0ea3c9187e8e");
		System.Data.DataRow currentRow = (edvOrderDtl != null && edvOrderDtl.Row >= 0 && edvOrderDtl.dataView != null)
			? edvOrderDtl.dataView[edvOrderDtl.Row].Row
			: null;

		decimal cantmocd;
		string cantidadbaq;
		if (!TryGetMoqFromBaQ(parte, out cantmocd, out cantidadbaq))
		{
			MessageBox.Show("No se encontró MOQ para esta parte. Revise la parte o el BAQ.");
			if (partess != null)
				partess.Text = "INGRESE LA PARTE";
			nummin.BackColor = Color.Red;
			oTrans.Refresh();
			return;
		}

		decimal cntventd = ResolveSalesOrderQuantityForMoq(currentRow);
		if (cntventd <= 0m)
		{
			nummin.Text = cantidadbaq;
			nummin.BackColor = Color.Gold;
			return;
		}

		if (cntventd < 0m)
		{
			MessageBox.Show("La cantidad no puede ser negativa.");
			if (partess != null)
				partess.Text = "INGRESE LA PARTE";
			nummin.BackColor = Color.Red;
			oTrans.Refresh();
			return;
		}

		if (cantmocd <= cntventd)
		{
			nummin.BackColor = Color.Green;
			nummin.Text = cantidadbaq;
		}
		else
		{
			if (cantventa != null)
				cantventa.Text = "0.00";
			MessageBox.Show("Cantidad solicitada no cumple con el MOQ. Favor de notificar al cliente y/o ventas.");
			if (partess != null)
				partess.Text = "INGRESE LA PARTE";
			nummin.Text = cantidadbaq;
			nummin.BackColor = Color.Red;
			oTrans.Refresh();
		}
	}

	private static bool TryParseDecimalFlexible(string text, out decimal value)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		text = text.Trim();

		if (decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out value))
		{
			return true;
		}
		if (decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
		{
			return true;
		}
		value = 0m;
		return false;
	}

	public void consultaOC(string oc)
	{
		EpiTextBox ordenventamaestra = (EpiTextBox)csm.GetNativeControlReference("4fceeeec-518c-4256-932e-34a4c1a584ee");
		string existe = "";
		DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
		adapterDynamicQuery.BOConnect();
		string BAQName = "BAQOCREPETIO";
		Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
		ds.ExecutionParameter.AddExecutionParameterRow("PO", oc, "nvarchar", false, Guid.Empty, "A");
		adapterDynamicQuery.ExecuteByID(BAQName, ds);
		if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
		{
			existe = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
			if (existe != ordenventamaestra.Text)
			{
				MessageBox.Show("ORDEN DE COMPRA DUPLICADA, FAVOR DE REVISAR");
			}
			else { }
			adapterDynamicQuery.Dispose();
		}
	}

	private void consultaConsolidadoOVEMB()
	{
		try
		{
			if (edvOrderHed == null || edvOrderHed.Row < 0)
			{
				consolidadotext.Text = string.Empty;
				return;
			}

			if (string.IsNullOrWhiteSpace(ResolveShipToCustId()))
			{
				consolidadotext.Text = string.Empty;
				return;
			}

			consolidadotext.Text = GetConsolidadoFromBaq();
		}
		catch (System.Exception ex)
		{
			consolidadotext.Text = string.Empty;
			ExceptionBox.Show(ex);
		}
	}

	public void consultaparte()
	{
		EpiTextBox txtLineDesc = (EpiTextBox)csm.GetNativeControlReference("389e7a2c-b0bd-4439-92ae-72a78efe0977");
		try
		{
			DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
			adapterDynamicQuery.BOConnect();
			string BAQName = "PART_CUS";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("PART", txtUPartNum.Text, "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(BAQName, ds);
			if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count == 0)
			{
				txtLineDesc.Text = string.Empty;
				txtUPartNum.Text = string.Empty;
			}
			if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			{
				txtUPartNum.Text = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
			}
			else
			{
				txtLineDesc.Text = string.Empty;
				txtUPartNum.Text = string.Empty;
			}
			adapterDynamicQuery.Dispose();
		}
		catch (System.Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	public void consultapartearance(EpiDataView view, string PARTE)
	{
		DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
		adapterDynamicQuery.BOConnect();
		string BAQName = "FRACCIONARANCELARIA";
		Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
		ds.ExecutionParameter.AddExecutionParameterRow("PARTE", PARTE, "nvarchar", false, Guid.Empty, "A");
		adapterDynamicQuery.ExecuteByID(BAQName, ds);
		if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			arancelaria.Text = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
		adapterDynamicQuery.Dispose();
	}

	public void estatus(EpiDataView view, string PARTE)
	{
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
		if (mensaje == "CAMBIO DE INGENIERÍA") { throw new UIException("Numero de parte en ''CAMBIO DE INGENIERIA'', comuníquese con el departamento correspondiente."); }
		if (mensaje == "COTIZACION") { throw new UIException("Numero de parte en ''COTIZACIÓN'', comuníquese con el departamento correspondiente."); }
	}

	private void consultaservice(string PARTES)
	{
		DynamicQueryAdapter adapter = new DynamicQueryAdapter(this.oTrans);
		adapter.BOConnect();
		string BAQName = ("OVPRDUCTSERVICE");
		Ice.BO.QueryExecutionDataSet poner = new Ice.BO.QueryExecutionDataSet();
		poner.ExecutionParameter.AddExecutionParameterRow("PARTE", PARTES, "nvarchar", false, Guid.Empty, "A");
		adapter.ExecuteByID(BAQName, poner);
		if (adapter.QueryResults.Tables["Results"].Rows.Count == 0)
		{
			productoserv.Text = string.Empty;
		}
		if (adapter.QueryResults.Tables["Results"].Rows.Count > 0)
		{
			productoserv.Text = adapter.QueryResults.Tables["Results"].Rows[0][0].ToString();
		}
		else { productoserv.Text = string.Empty; }
		adapter.Dispose();
	}

	private void consultacantidadUE(string PARTE)
	{
		DynamicQueryAdapter adapter = new DynamicQueryAdapter(this.oTrans);
		adapter.BOConnect();
		string BAQName = "BAQCANTue";
		Ice.BO.QueryExecutionDataSet ere = new Ice.BO.QueryExecutionDataSet();
		ere.ExecutionParameter.AddExecutionParameterRow("PART", PARTE, "nvarchar", false, Guid.Empty, "A");
		adapter.ExecuteByID(BAQName, ere);
		if (adapter.QueryResults.Tables["Results"].Rows.Count == 0)
		{
			canthn.Text = string.Empty; resmillar.Text = string.Empty;
		}
		if (adapter.QueryResults.Tables["Results"].Rows.Count > 0)
		{
			canthn.Text = adapter.QueryResults.Tables["Results"].Rows[0][0].ToString();
			calculo_millar();
		}
		else
		{
			canthn.Text = string.Empty; resmillar.Text = string.Empty;
		}

		adapter.Dispose();
	}

	private void calculo_millar()
	{
		string unidad = edvOrderDtl.dataView[edvOrderDtl.Row]["SalesUM"].ToString();
		decimal cantidadordenada = decimal.Parse(edvOrderDtl.dataView[edvOrderDtl.Row]["SellingQuantity"].ToString());
		decimal cantrollo = decimal.Parse(canthn.Text);
		if (unidad == "MIL")
		{
			resmillar.Text = Convert.ToString((cantidadordenada * 1000) / cantrollo);
		}
	}

	private void edvOrderDtl_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if ((args.NotifyType == EpiTransaction.NotifyType.Initialize))
		{
			if ((args.Row > -1))
			{
				try
				{
					SalesOrderForm.Cursor = Cursors.WaitCursor;
					string Part = view.dataView[args.Row]["PartNum"].ToString();
					if (string.IsNullOrWhiteSpace(Part))
						return;

					if (!ValidateNacionalShipToForCurrentLine())
					{
						SalesOrderForm.Cursor = Cursors.Default;
						return;
					}

					consultaservice(Part);
					consultapartearance(view, Part);
					estatus(view, Part);
					consultacantidadUE(Part);
					try { VALIDARCANTIDADVENTA(Part); } catch { }

					DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
					adapterDynamicQuery.BOConnect();
					string BAQName = "CostoUnitario";
					Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();

					ds.ExecutionParameter.AddExecutionParameterRow("PartNum", Part, "nvarchar", false, Guid.Empty, "A");

					adapterDynamicQuery.ExecuteByID(BAQName, ds);
					string UOM = edvOrderDtl.dataView[edvOrderDtl.Row]["SalesUM"].ToString();
					string CostoTotal = "0.000";
					if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
						CostoTotal = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][6].ToString();
					adapterDynamicQuery.Dispose();
					coonsultavlidacion(Part);
					decimal precioUnitario = decimal.Parse(edvOrderDtl.dataView[edvOrderDtl.Row]["UnitPrice"].ToString());

					decimal costoUnitario = decimal.Parse(CostoTotal);

					if (UOM == "PZA")
					{
						decimal CostoPZA = (costoUnitario / 1000);
						txtCostoTotal.Value = CostoPZA;
						if (precioUnitario >= CostoPZA)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable!";
						}
					}

					if (UOM == "MIL")
					{
						decimal CostoMIL = costoUnitario * 1;
						txtCostoTotal.Value = CostoMIL;
						if (CostoMIL >= precioUnitario)
						{
							renta.BackColor = Color.Red;
							renta.Text = "No rentable!";
						}
						else { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
					}

					if (UOM == "RL5000")
					{
						decimal CostoRL5000 = costoUnitario * 5;
						txtCostoTotal.Value = CostoRL5000;
						if (precioUnitario >= CostoRL5000) { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
						else { renta.BackColor = Color.Red; renta.Text = "No rentable !!"; }
					}

					if (UOM == "RL500")
					{
						string value = "0.500";
						decimal factor500 = Convert.ToDecimal(value);
						decimal CostoRL500 = costoUnitario * factor500;

						txtCostoTotal.Value = CostoRL500;
						if (precioUnitario >= CostoRL500) { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
						else { renta.BackColor = Color.Red; renta.Text = "No rentable !!"; }
					}

					if (UOM == "RL3000")
					{
						decimal CostoRL3000 = costoUnitario * 3;
						txtCostoTotal.Value = CostoRL3000;
						if (precioUnitario >= CostoRL3000)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "RL250")
					{
						string Factor250 = "0.25";
						decimal factor250s = Convert.ToDecimal(Factor250);
						decimal CostoRL250 = costoUnitario * factor250s;
						txtCostoTotal.Value = CostoRL250;
						if (precioUnitario >= CostoRL250)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "SERV")
					{
						string Factor1 = "1";
						decimal factor1S = Convert.ToDecimal(Factor1);
						decimal Costo1 = costoUnitario * factor1S;
						txtCostoTotal.Value = Costo1;
						if (precioUnitario >= Costo1)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "MSI")
					{
						string Factormsi = "1";
						decimal factormsito = Convert.ToDecimal(Factormsi);
						decimal Costomsi = costoUnitario * factormsito;
						txtCostoTotal.Value = Costomsi;
						if (precioUnitario >= Costomsi)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "RL2000")
					{
						decimal CostoRL2000 = costoUnitario * 2;
						txtCostoTotal.Value = CostoRL2000;
						if (precioUnitario >= CostoRL2000)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "RL150")
					{
						string rl150 = "0.15";
						decimal factor150 = Convert.ToDecimal(rl150);
						decimal CostoRL150 = costoUnitario * factor150;
						txtCostoTotal.Value = CostoRL150;
						if (precioUnitario >= CostoRL150)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "RL125")
					{
						string RL125String = "0.125";
						decimal factor125 = Convert.ToDecimal(RL125String);
						decimal CostoRL125 = costoUnitario * factor125;
						txtCostoTotal.Value = CostoRL125;
						if (precioUnitario >= CostoRL125)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "RL10K")
					{
						decimal CostoRL10K = costoUnitario * 10;
						txtCostoTotal.Value = CostoRL10K;
						if (precioUnitario >= CostoRL10K) { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
						else { renta.BackColor = Color.Red; renta.Text = "No rentable !!"; }
					}

					if (UOM == "RL1000")
					{
						decimal CostoRL1000 = costoUnitario * 1;
						txtCostoTotal.Value = CostoRL1000;
						if (precioUnitario >= CostoRL1000) { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
						else { renta.BackColor = Color.Red; renta.Text = "No rentable !!"; }
					}

					if (UOM == "RL100")
					{
						string factRL100 = "0.1";
						decimal factorRLS100 = Convert.ToDecimal(factRL100);
						decimal CostoRL100 = costoUnitario * factorRLS100;
						txtCostoTotal.Value = CostoRL100;
						if (precioUnitario >= CostoRL100) { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
						else { renta.BackColor = Color.Red; renta.Text = "No rentable !!"; }
					}

					if (UOM == "RL 400")
					{
						string factorRL400s = "0.4";
						decimal factorRL400 = Convert.ToDecimal(factorRL400s);
						decimal CostoRL400q = (costoUnitario * factorRL400);
						txtCostoTotal.Value = CostoRL400q;
						if (precioUnitario >= CostoRL400q)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "RL")
					{
						decimal CostoRL = costoUnitario * 1;
						txtCostoTotal.Value = CostoRL;
						if (precioUnitario >= CostoRL)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "HOJA")
					{
						decimal CostoHOJA = costoUnitario / 1000;

						txtCostoTotal.Value = CostoHOJA;
						if (precioUnitario >= CostoHOJA)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "ROLLO")
					{
						decimal CostoROLLO = costoUnitario * 1;
						txtCostoTotal.Value = CostoROLLO;
						if (precioUnitario >= CostoROLLO)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "PZA.")
					{
						decimal CostoCostoPZA2 = costoUnitario / 1000;
						txtCostoTotal.Value = CostoCostoPZA2;
						if (precioUnitario >= CostoCostoPZA2)
						{
							renta.BackColor = Color.Green;
							renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red;
							renta.Text = "No rentable !!";
						}
					}

					if (UOM == "PK")
					{
						decimal CostoPK = costoUnitario * 1;
						txtCostoTotal.Value = CostoPK;
						if (precioUnitario >= CostoPK)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "KGR")
					{
						string kgr = "0.32";
						decimal KGR2 = Convert.ToDecimal(kgr);
						decimal CostoKGR = costoUnitario / KGR2;
						txtCostoTotal.Value = CostoKGR;
						if (precioUnitario >= CostoKGR)
						{
							renta.BackColor = Color.Green; renta.Text = "Rentable !!";
						}
						else
						{
							renta.BackColor = Color.Red; renta.Text = "No rentable !!";
						}
					}

					if (UOM == "FT.")
					{
						string fta = "0.001";
						decimal factorF = Convert.ToDecimal(fta);
						decimal CostoFT = costoUnitario / factorF;
						txtCostoTotal.Value = CostoFT;
						if (precioUnitario >= CostoFT) { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
						else { renta.BackColor = Color.Red; renta.Text = "No rentable !!"; }
					}

					if (UOM == "FT")
					{
						string ft = "1";
						decimal factorFT = Convert.ToDecimal(ft);
						decimal CostoF = costoUnitario / factorFT;
						txtCostoTotal.Value = CostoF;
						if (precioUnitario >= CostoF) { renta.BackColor = Color.Green; renta.Text = "Rentable !!"; }
						else { renta.BackColor = Color.Red; renta.Text = "No rentable !!"; }
					}

					if (precioUnitario == 0)
					{
					}
					else { margen(); }

					SalesOrderForm.Cursor = Cursors.Default;
				}
				catch (System.Exception ex)
				{
					txtCostoTotal.Cursor = Cursors.Default;

					ExceptionBox.Show(ex);
				}
			}
		}
	}

	private void baseToolbarsManager_ToolClick(object sender, Infragistics.Win.UltraWinToolbars.ToolClickEventArgs args)
	{
		LockCurrencyToolbarTools();
		ApplyOrderCurrencyFromCustomer();
		LockDetailCurrencyControls();

		switch (args.Tool.Key)
		{
			case "DeleteTool":
				txtCostoTotal.Text = string.Empty;
				nummin.Text = string.Empty;
				renta.Text = string.Empty;
				epiNumericEditorC1.Text = string.Empty;
				arancelaria.Text = string.Empty;
				validacionparte.Text = string.Empty;
				canthn.Text = string.Empty;
				resmillar.Text = string.Empty;
				consolidadotext.Text = string.Empty;

				break;
			case "ClearTool":
				txtCostoTotal.Text = string.Empty;
				renta.Text = string.Empty;
				epiNumericEditorC1.Text = string.Empty;
				arancelaria.Text = string.Empty;
				validacionparte.Text = string.Empty;
				canthn.Text = string.Empty;
				resmillar.Text = string.Empty;
				consolidadotext.Text = string.Empty;

				break;
			case "SaveTool":
				string Hed = edvOrderHed.dataView[edvOrderHed.Row]["PONum"].ToString();

				consultaOC(Hed);
				consultaConsolidadoOVEMB();
				break;
		}
	}

	private void txtUPartNum_Validated(object sender, System.EventArgs args)
	{
		consultaparte();
		ValidateNacionalShipToForCurrentLine();
		if (edvOrderDtl != null && edvOrderDtl.Row >= 0)
		{
			string part = edvOrderDtl.dataView[edvOrderDtl.Row]["PartNum"].ToString();
			if (!string.IsNullOrWhiteSpace(part))
				try { VALIDARCANTIDADVENTA(part); } catch { }
		}
	}

	public void coonsultavlidacion(string PARTE)
	{
		string mensaje = "";
		DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
		adapterDynamicQuery.BOConnect();
		string BAQName = "BAQVALIDARPART";
		Ice.BO.QueryExecutionDataSet stat = new Ice.BO.QueryExecutionDataSet();
		stat.ExecutionParameter.AddExecutionParameterRow("PARTE", PARTE, "nvarchar", false, Guid.Empty, "A");
		adapterDynamicQuery.ExecuteByID(BAQName, stat);
		if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			mensaje = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString();
		adapterDynamicQuery.Dispose();
		if (mensaje == "VALIDADO")
		{
			validacionparte.BackColor = Color.LightGreen;
			validacionparte.Text = mensaje;
		}
		else
		{
			validacionparte.BackColor = Color.Yellow;
			validacionparte.Text = mensaje;
		}
	}

	private static readonly string[] PlantsImmexValidation = { "CFXCH", "CFXJZ", "CFXMTY" };
	private static readonly string[] ProdCodesNacionalShipTo = { "OFFIMMX", "COM", "COMER", "OFFNORM" };
	private static readonly string[] ProdCodesExportShipTo = { "FLXIMMX", "FLXNORM" };
	private static readonly string[] ProdCodesImmexFamily = { "OFFIMMX", "FLXIMMX" };
	private static readonly string[] ProdCodesNacionalFamily = { "OFFNORM", "FLXNORM", "COM", "COMER" };

	private static bool IsInProdCodeList(string value, string[] list)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		value = value.Trim();
		for (int i = 0; i < list.Length; i++)
		{
			if (string.Equals(list[i], value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsImmexValidationPlant(string plantId)
	{
		return IsInProdCodeList(plantId, PlantsImmexValidation);
	}

	private string GetOrderDtlRowString(DataRow row, params string[] columnNames)
	{
		if (row == null)
		{
			return string.Empty;
		}

		foreach (string columnName in columnNames)
		{
			if (row.Table.Columns.Contains(columnName))
			{
				object value = row[columnName];
				return value == null ? string.Empty : value.ToString().Trim();
			}
		}

		return string.Empty;
	}

	private string GetConsolidadoFromBaq()
	{
		string idCliente = ResolveShipToCustId();
		if (string.IsNullOrWhiteSpace(idCliente))
		{
			return string.Empty;
		}

		string idShip = ResolveShipToNum();
		string[] shipKeys = new string[]
		{
			idShip ?? string.Empty,
			string.Empty,
			"1",
			"DEFAULT",
			"PRINCIPAL"
		};

		System.Collections.Generic.HashSet<string> tried = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < shipKeys.Length; i++)
		{
			string key = shipKeys[i] ?? string.Empty;
			if (!tried.Add(key))
			{
				continue;
			}

			string consolidado = ExecuteConsolidadoBaq(key, idCliente);
			if (!string.IsNullOrWhiteSpace(consolidado))
			{
				return consolidado;
			}
		}

		return string.Empty;
	}

	private void ValidateImmexNacionalMaterialRules()
	{
		if (IsCodiflexIncExcludedCustomer())
			return;

		if (IsCopelandExcludedShipTo())
			return;

		if (IsRegalExcludedShipTo())
			return;

		if (IsThermoDiscExcludedShipTo())
			return;

		string currentPlant = ((Ice.Core.Session)oTrans.Session).PlantID;
		if (!IsImmexValidationPlant(currentPlant))
		{
			return;
		}

		if (edvOrderHed == null || edvOrderHed.Row < 0 || edvOrderDtl == null || edvOrderDtl.dataView == null)
		{
			return;
		}

		DataView detailView = edvOrderDtl.dataView;
		bool hasMaterialLines = false;

		for (int rowIndex = 0; rowIndex < detailView.Count; rowIndex++)
		{
			DataRow row = detailView[rowIndex].Row;
			if (row.RowState == DataRowState.Deleted)
			{
				continue;
			}

			if (!string.IsNullOrWhiteSpace(GetOrderDtlRowString(row, "PartNum")))
			{
				hasMaterialLines = true;
				break;
			}
		}

		if (!hasMaterialLines)
		{
			return;
		}

		if (!IsShipToCaptured())
		{
			throw new UIException(
				"[IMMEX v" + ImmexValidationVersion + "] Seleccione cliente y domicilio de embarque («Dom. embq.»).\r\n\r\n" +
				"Embarca vacío es válido si el consolidado amarillo se llena.");
		}

		string shipToCustId = ResolveShipToCustId();
		string shipToNum = ResolveShipToNum();
		string consolidado = GetConsolidadoDisplayOrBaq();
		if (string.IsNullOrWhiteSpace(consolidado))
		{
			consolidado = GetConsolidadoFromBaq();
		}
		if (string.IsNullOrWhiteSpace(consolidado))
		{
			string shipHint = string.IsNullOrWhiteSpace(shipToNum)
				? "domicilio principal (Embarca vacío)"
				: "Ship To '" + shipToNum + "'";
			throw new UIException(
				"[IMMEX v" + ImmexValidationVersion + "] No se encontró consolidado para cliente '" + shipToCustId + "' / " + shipHint + ".\r\n\r\n" +
				"Verifique Consolidado (UD) en Ship To Maintenance.\r\n" +
				"El BAQ BAQCONSOLIDADOOVEMB debe aceptar IDSHIP en blanco (dom. principal).");
		}

		bool isNacionalShipTo = ResolveIsNacionalShipTo(shipToNum, consolidado);
		string[] allowedProdCodes = isNacionalShipTo ? ProdCodesNacionalShipTo : ProdCodesExportShipTo;
		string shipToLabel = FormatShipToLabel(shipToNum, shipToCustId);

		bool hasImmexFamily = false;
		bool hasNacionalFamily = false;
		StringBuilder invalidLines = new StringBuilder();

		for (int rowIndex = 0; rowIndex < detailView.Count; rowIndex++)
		{
			DataRow row = detailView[rowIndex].Row;
			if (row.RowState == DataRowState.Deleted)
			{
				continue;
			}

			string partNum = GetOrderDtlRowString(row, "PartNum");
			if (string.IsNullOrWhiteSpace(partNum))
			{
				continue;
			}

			if (IsExcludedFromNacionalImmexValidation(partNum))
				continue;

			string prodCode = GetOrderDtlRowString(row, "ProdCode");
			string orderLine = GetOrderDtlRowString(row, "OrderLine");
			if (string.IsNullOrWhiteSpace(orderLine))
			{
				orderLine = Convert.ToString(rowIndex + 1);
			}

			if (!IsInProdCodeList(prodCode, allowedProdCodes))
			{
				invalidLines.AppendLine(string.Format(
					"Línea {0}: Parte {1}, ProdCode '{2}' no permitido para {3} (consolidado: {4}).",
					orderLine,
					partNum,
					string.IsNullOrWhiteSpace(prodCode) ? "(vacío)" : prodCode,
					shipToLabel,
					consolidado));
			}

			if (IsInProdCodeList(prodCode, ProdCodesImmexFamily))
			{
				hasImmexFamily = true;
			}

			if (IsInProdCodeList(prodCode, ProdCodesNacionalFamily))
			{
				hasNacionalFamily = true;
			}
		}

		if (hasImmexFamily && hasNacionalFamily)
		{
			throw new UIException(
				"No se permite mezclar materiales IMMEX (OFFIMMX/FLXIMMX) con materiales Nacionales (OFFNORM/FLXNORM/COM/COMER) en la misma orden de venta.");
		}

		if (invalidLines.Length > 0)
		{
			throw new UIException(
				"La orden contiene líneas con grupo de producto incompatible con el Ship To:\r\n" + invalidLines.ToString());
		}
	}

	private void oTrans_ordAdapter_BeforeAdapterMethod(object sender, BeforeAdapterMethodArgs args)
	{
		switch (args.MethodName)
		{
			case "Update":

				ValidateOrderCurrencyOnSave();
				ValidateNacionalProdCodesOnSave();
				ValidateMoqOnSave();

				string Hed = edvOrderHed.dataView[edvOrderHed.Row]["PONum"].ToString();

				if (edvOrderHed.Row > -1)
				{
					consultaConsolidadoOVEMB();
				}

				break;
		}
	}

	private void SalesOrderForm_Load(object sender, EventArgs args)
	{
		EpiTextBox partdescription = (EpiTextBox)csm.GetNativeControlReference("cb95b987-6975-4d4b-bb57-d42d8eaf7a0a");
		partdescription.Enabled = false;
		consultaConsolidadoOVEMB();
		EnforceOrderCurrencyLock();
		if (SalesOrderForm != null && !SalesOrderForm.IsDisposed)
		{
			SalesOrderForm.BeginInvoke((MethodInvoker)delegate
			{
				EnforceOrderCurrencyLock();
			});
			SalesOrderForm.BeginInvoke((MethodInvoker)delegate
			{
				_psCurrControl = null;
				LockDetailCurrencyControls();
			});
		}
	}
}