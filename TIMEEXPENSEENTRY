// **************************************************
// Custom code for TimeExpenseForm
// Created: 8/15/2018 10:18:22 AM
// v1.1: Suaje obligatorio CFXCH — jobs flexo CH / JZ- (excluye O-, corte-, SIST, etc.)
// **************************************************

extern alias Erp_Contracts_BO_Labor;
extern alias Erp_Contracts_BO_SerialNo;
extern alias Erp_Contracts_BO_EmpBasic;
extern alias Erp_Contracts_BO_ResourceGroup;
extern alias Erp_Contracts_BO_Reason;
extern alias Erp_Contracts_BO_Company;
extern alias Erp_Contracts_BO_Resource;
extern alias Erp_Contracts_BO_JobEntry;
extern alias Erp_Contracts_BO_JobAsmSearch;
extern alias Erp_Contracts_BO_JobOperSearch;

using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
using Infragistics.Win.UltraWinGrid;
using Infragistics.Win.UltraWinToolbars;

public class Script
{
	// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **
	// Begin Wizard Added Module Level Variables **

	private EpiBaseAdapter oTrans_laborAdapter;
	private EpiDataView edvLaborDtl;
	// End Wizard Added Module Level Variables **

	// Add Custom Module Level Variables Here **
	EpiDataView edvLaborDtll;
	EpiUltraCombo cbOprSeq;
	private EpiTextBox txtSuajeControl;

	private const string SuajeControlGuid = "f7f86896-4edc-4e90-8f4c-8f98f16eefe8";
	private const string SuajePlantId = "CFXCH";

	public void InitializeCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
		// Begin Wizard Added Variable Initialization

		this.oTrans_laborAdapter = ((EpiBaseAdapter)(this.csm.TransAdaptersHT["oTrans_laborAdapter"]));
		this.oTrans_laborAdapter.BeforeAdapterMethod += new BeforeAdapterMethod(this.oTrans_laborAdapter_BeforeAdapterMethod);

		this.minutostemp.ValueChanged += new System.EventHandler(this.minutostemp_ValueChanged);

		this.edvLaborDtl = ((EpiDataView)(this.oTrans.EpiDataViews["LaborDtl"]));
		this.edvLaborDtl.EpiViewNotification += new EpiViewNotification(this.edvLaborDtl_EpiViewNotification);
		this.baseToolbarsManager.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
		// End Wizard Added Variable Initialization

		txtSuajeControl = (EpiTextBox)csm.GetNativeControlReference(SuajeControlGuid);

		// Begin Wizard Added Custom Method Calls

		this.txtAnchoMateriaPrima.TextChanged += new System.EventHandler(this.txtAnchoMateriaPrima_TextChanged);
		// End Wizard Added Custom Method Calls
		cbOprSeq = ((EpiUltraCombo)csm.GetNativeControlReference("91b38e77-b997-4542-b486-038b7be4017f"));
		cbOprSeq.TextChanged += new EventHandler(this.cbOprSeq_TextChanged);
		txtFTCorrida.TextChanged += new EventHandler(this.txtFTCorrida_TextChanged);
		txtEtiquetasAlPaso.TextChanged += new EventHandler(this.txtEtiquetasAlPaso_TextChanged);
	}

	public void DestroyCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
		// Begin Wizard Added Object Disposal

		this.oTrans_laborAdapter.BeforeAdapterMethod -= new BeforeAdapterMethod(this.oTrans_laborAdapter_BeforeAdapterMethod);
		this.oTrans_laborAdapter = null;
		this.minutostemp.ValueChanged -= new System.EventHandler(this.minutostemp_ValueChanged);

		this.txtAnchoMateriaPrima.TextChanged -= new System.EventHandler(this.txtAnchoMateriaPrima_TextChanged);
		this.edvLaborDtl.EpiViewNotification -= new EpiViewNotification(this.edvLaborDtl_EpiViewNotification);
		this.edvLaborDtl = null;
		this.baseToolbarsManager.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
		// End Wizard Added Object Disposal

		// Begin Custom Code Disposal

		// End Custom Code Disposal
		cbOprSeq.TextChanged -= new EventHandler(this.cbOprSeq_TextChanged);
		txtFTCorrida.TextChanged -= new EventHandler(this.txtFTCorrida_TextChanged);
		txtEtiquetasAlPaso.TextChanged -= new EventHandler(this.txtEtiquetasAlPaso_TextChanged);
		txtSuajeControl = null;
	}

	private bool IsCfxchPlant()
	{
		try
		{
			string plantId = ((Ice.Core.Session)oTrans.Session).PlantID;
			return string.Equals(plantId, SuajePlantId, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static bool RequiresSuajeForJob(string jobNum)
	{
		if (string.IsNullOrWhiteSpace(jobNum))
			return false;

		jobNum = jobNum.Trim();

		if (jobNum.StartsWith("O-", StringComparison.OrdinalIgnoreCase))
			return false;

		if (jobNum.StartsWith("corte-", StringComparison.OrdinalIgnoreCase))
			return false;

		if (jobNum.StartsWith("SIST", StringComparison.OrdinalIgnoreCase))
			return false;

		if (jobNum.StartsWith("CH", StringComparison.OrdinalIgnoreCase))
			return true;

		if (jobNum.StartsWith("JZ-", StringComparison.OrdinalIgnoreCase))
			return true;

		return false;
	}

	private string ResolveSuajeValue()
	{
		try
		{
			if (edvLaborDtl != null && edvLaborDtl.Row > -1 && edvLaborDtl.dataView != null)
			{
				DataRow row = edvLaborDtl.dataView[edvLaborDtl.Row].Row;
				if (row != null && row.Table.Columns.Contains("Suaje_c"))
				{
					object value = row["Suaje_c"];
					if (value != null && value != DBNull.Value)
					{
						string text = value.ToString().Trim();
						if (text.Length > 0)
							return text;
					}
				}
			}
		}
		catch
		{
		}

		if (lbrsuaje != null && !string.IsNullOrWhiteSpace(lbrsuaje.Text))
			return lbrsuaje.Text.Trim();

		if (txtSuajeControl != null && !string.IsNullOrWhiteSpace(txtSuajeControl.Text))
			return txtSuajeControl.Text.Trim();

		return string.Empty;
	}

	private void ValidateSuajeRequired(string jobNum)
	{
		if (!IsCfxchPlant())
			return;

		if (!RequiresSuajeForJob(jobNum))
			return;

		if (!string.IsNullOrWhiteSpace(ResolveSuajeValue()))
			return;

		MessageBox.Show("CAPTURE EL SUAJE", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		throw new UIException("CAPTURE EL SUAJE");
	}

	private void minutostemp_ValueChanged(object sender, System.EventArgs args)
	{
		if (Convert.ToInt32(minutostemp.Value) == 0)
		{
			listatiempo.Enabled = false;
		}
		else
		{
			listatiempo.Enabled = true;
		}
	}

	private string threeDecimals(string[] values, bool round)
	{
		string ints = values[0].ToString();
		string decimals = "0";

		if (values.Length > 1)
		{
			if (values[1].ToString().Length > 2)
				decimals = values[1].ToString().Substring(0, 3);
			else
				decimals = values[1].ToString();

			if (round)
			{
				int decimalValue = int.Parse(decimals[0].ToString());
				if (decimalValue >= 5)
					ints = (int.Parse(ints) + 1).ToString();

				return ints;
			}
			else
			{
				return ints + "." + decimals;
			}
		}
		else
			return ints;
	}

	private void txtEtiquetasAlPaso_TextChanged(object sender, System.EventArgs args)
	{
		EpiDataView edvLaborDtl = ((EpiDataView)(this.oTrans.EpiDataViews["LaborDtl"])); //

		if (edvLaborDtl.Row > -1)
		{
			string uom = edvLaborDtl.dataView[edvLaborDtl.Row]["LaborUOM"].ToString().ToLower();
			string recursos = edvLaborDtl.dataView[edvLaborDtl.Row]["ResourceID"].ToString();
			if (cbOprSeq.Text == "10")
			{
				if (recursos == "PREN09-JZ")
				{
					if (uom == "mil")
					{
						decimal metros = decimal.Parse("39.3701");
						decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
						decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
						decimal largoReal = decimal.Parse(txtLargoReal.Text);
						if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
						{
							decimal scrap = ((ftCorrida * metros * etiquetasAlPaso) / largoReal) / 1000;

							string[] resultado = scrap.ToString().Split('.');
							scrap = decimal.Parse(threeDecimals(resultado, false));
							edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"] = scrap;
						}
					}
				}
				else
				{
					if (uom == "mil")
					{
						decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
						decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
						decimal largoReal = decimal.Parse(txtLargoReal.Text);
						if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
						{
							decimal scrap = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / 1000;

							string[] resultado = scrap.ToString().Split('.');
							scrap = decimal.Parse(threeDecimals(resultado, false));
							edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"] = scrap;
						}
					}
					if (uom == "rollo")
					{
						decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
						decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
						decimal largoReal = decimal.Parse(txtLargoReal.Text);
						decimal cantidadPorRollo = decimal.Parse(txtCantidadPorRollo.Text);
						if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0 && cantidadPorRollo != 0)
						{
							decimal scrap = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / cantidadPorRollo;

							string[] resultado = scrap.ToString().Split('.');
							scrap = decimal.Parse(threeDecimals(resultado, true));
							edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"] = scrap;
						}
					}
					if (uom == "pza")
					{
						decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
						decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
						decimal largoReal = decimal.Parse(txtLargoReal.Text);
						if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
						{
							decimal scrap = ((ftCorrida * 12) / (largoReal * etiquetasAlPaso));

							string[] resultado = scrap.ToString().Split('.');
							scrap = decimal.Parse(threeDecimals(resultado, true));
							edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"] = scrap;
						}
					}
				}
			}

			if (cbOprSeq.Text == "20")
			{
				if (recursos == "PRFL9-JZ")
				{
					decimal metros = decimal.Parse("39.3701");
					decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
					decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
					decimal largoReal = decimal.Parse(txtLargoReal.Text);
					if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
					{
						decimal labor = ((ftCorrida * metros * etiquetasAlPaso) / largoReal) / 1000;

						string[] resultado = labor.ToString().Split('.');
						labor = decimal.Parse(threeDecimals(resultado, false));
						edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"] = labor;
					}
				}
				else
				{
					if (uom == "mil")
					{
						decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
						decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
						decimal largoReal = decimal.Parse(txtLargoReal.Text);
						if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
						{
							decimal labor = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / 1000;

							string[] resultado = labor.ToString().Split('.');
							labor = decimal.Parse(threeDecimals(resultado, false));
							edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"] = labor;
						}
					}
					if (uom == "rollo")
					{
						decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
						decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
						decimal largoReal = decimal.Parse(txtLargoReal.Text);
						decimal cantidadPorRollo = decimal.Parse(txtCantidadPorRollo.Text);
						if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0 && cantidadPorRollo != 0)
						{
							decimal labor = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / cantidadPorRollo;

							string[] resultado = labor.ToString().Split('.');
							labor = decimal.Parse(threeDecimals(resultado, true));
							edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"] = labor;
						}
					}
					if (uom == "pza")
					{
						decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
						decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
						decimal largoReal = decimal.Parse(txtLargoReal.Text);
						if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
						{
							decimal labor = ((ftCorrida * 12) / (largoReal * etiquetasAlPaso));
							string[] resultado = labor.ToString().Split('.');
							labor = decimal.Parse(threeDecimals(resultado, true));
							edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"] = labor;
						}
					}
				}
			}
		}
	}

	private void txtFTCorrida_TextChanged(object sender, System.EventArgs args)
	{
		EpiDataView edvLaborDtl = ((EpiDataView)(this.oTrans.EpiDataViews["LaborDtl"])); //

		if (edvLaborDtl.Row > -1)
		{
			string uom = edvLaborDtl.dataView[edvLaborDtl.Row]["LaborUOM"].ToString().ToLower();

			if (cbOprSeq.Text == "10")
			{
				if (uom == "mil")
				{
					decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
					decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
					decimal largoReal = decimal.Parse(txtLargoReal.Text);
					if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
					{
						decimal scrap = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / 1000;

						string[] resultado = scrap.ToString().Split('.');
						scrap = decimal.Parse(threeDecimals(resultado, false));
						edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"] = scrap;
					}
				}
				if (uom == "rollo")
				{
					decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
					decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
					decimal largoReal = decimal.Parse(txtLargoReal.Text);
					decimal cantidadPorRollo = decimal.Parse(txtCantidadPorRollo.Text);
					if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0 && cantidadPorRollo != 0)
					{
						decimal scrap = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / cantidadPorRollo;

						string[] resultado = scrap.ToString().Split('.');
						scrap = decimal.Parse(threeDecimals(resultado, true));
						edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"] = scrap;
					}
				}
				if (uom == "pza")
				{
					decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
					decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
					decimal largoReal = decimal.Parse(txtLargoReal.Text);
					if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
					{
						decimal scrap = ((ftCorrida * 12) / (largoReal * etiquetasAlPaso));

						string[] resultado = scrap.ToString().Split('.');
						scrap = decimal.Parse(threeDecimals(resultado, true));
						edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"] = scrap;
					}
				}

				decimal anchoMateriaPrima = decimal.Parse(txtAnchoMateriaPrima.Text);
				decimal ftCorrida_ = decimal.Parse(txtFTCorrida.Text);
				txtMSI.Text = ((anchoMateriaPrima * ftCorrida_ * 12) / 1000).ToString();
			}

			if (cbOprSeq.Text == "20")
			{
				if (uom == "mil")
				{
					decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
					decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
					decimal largoReal = decimal.Parse(txtLargoReal.Text);
					if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
					{
						decimal labor = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / 1000;

						string[] resultado = labor.ToString().Split('.');
						labor = decimal.Parse(threeDecimals(resultado, false));
						edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"] = labor;
					}
				}
				if (uom == "rollo")
				{
					decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
					decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
					decimal largoReal = decimal.Parse(txtLargoReal.Text);
					decimal cantidadPorRollo = decimal.Parse(txtCantidadPorRollo.Text);
					if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0 && cantidadPorRollo != 0)
					{
						decimal labor = ((ftCorrida * 12 * etiquetasAlPaso) / largoReal) / cantidadPorRollo;

						string[] resultado = labor.ToString().Split('.');
						labor = decimal.Parse(threeDecimals(resultado, true));
						edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"] = labor;
					}
				}
				if (uom == "pza")
				{
					decimal ftCorrida = decimal.Parse(txtFTCorrida.Text);
					decimal etiquetasAlPaso = decimal.Parse(txtEtiquetasAlPaso.Text);
					decimal largoReal = decimal.Parse(txtLargoReal.Text);
					if (largoReal != 0 && etiquetasAlPaso != 0 && ftCorrida != 0)
					{
						decimal labor = ((ftCorrida * 12) / (largoReal * etiquetasAlPaso));
						string[] resultado = labor.ToString().Split('.');
						labor = decimal.Parse(threeDecimals(resultado, true));
						edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"] = labor;
					}
				}

				decimal anchoMateriaPrima = decimal.Parse(txtAnchoMateriaPrima.Text);
				decimal ftCorrida_ = decimal.Parse(txtFTCorrida.Text);
				txtMSI.Text = ((anchoMateriaPrima * ftCorrida_ * 12) / 1000).ToString();
			}
		}
	}

	private void cbOprSeq_TextChanged(object sender, System.EventArgs args)
	{
		if (cbOprSeq.Text == "" || cbOprSeq.Text == "0")
		{
			txtLargoReal.Text = "0";
			txtCantidadPorRollo.Text = "0";
		}
		else
		{
			EpiDataView edvJobHead = ((EpiDataView)(this.oTrans.EpiDataViews["JobHead"]));
			if (edvJobHead.Row > -1)
			{
				string partNum = edvJobHead.dataView[edvJobHead.Row]["PartNum"].ToString();
				consultaMatrizParaFormulas(partNum);
			}

			if (cbOprSeq.Text == "30")
			{
				txtAnchoMateriaPrima.Text = "0.00000";
				txtAnchoMateriaPrima.Enabled = false;
				txtMSI.Text = "0.00000";
			}
			else
				txtAnchoMateriaPrima.Enabled = true;

			decimal anchoMateriaPrima = decimal.Parse(txtAnchoMateriaPrima.Text);
			decimal ftCorrida_ = decimal.Parse(txtFTCorrida.Text);
			txtMSI.Text = ((anchoMateriaPrima * ftCorrida_ * 12) / 1000).ToString();
		}
	}

	private void oTrans_laborAdapter_BeforeAdapterMethod(object sender, BeforeAdapterMethodArgs args)
	{
		switch (args.MethodName)
		{
			case "Update":
				edvLaborDtl = ((EpiDataView)(this.oTrans.EpiDataViews["LaborDtl"]));

				if (edvLaborDtl.Row > -1)
				{
					string oprSeq = edvLaborDtl.dataView[edvLaborDtl.Row]["OprSeq"].ToString();
					string jobNum = edvLaborDtl.dataView[edvLaborDtl.Row]["JobNum"].ToString();
					decimal laborQty = decimal.Parse(edvLaborDtl.dataView[edvLaborDtl.Row]["LaborQty"].ToString());
					decimal scrapQty = decimal.Parse(edvLaborDtl.dataView[edvLaborDtl.Row]["ScrapQty"].ToString());
					string tipo = edvLaborDtl.dataView[edvLaborDtl.Row]["Tipo_c"].ToString();
					decimal anchoMateriaPrima = decimal.Parse(edvLaborDtl.dataView[edvLaborDtl.Row]["AnchoMateriaPrima_c"].ToString());
					decimal horas = decimal.Parse(edvLaborDtl.dataView[edvLaborDtl.Row]["LaborHrs"].ToString());
					if (String.IsNullOrEmpty(tipo))
						throw new UIException("Carga denegada - Seleccione Tipo.");

					if (oprSeq == "10")
					{
						int materiales = consultaMateriales(jobNum);
						if (materiales == 0)
							throw new UIException("Carga denegada - Falta emitir materia prima.");

						if (scrapQty == 0)
							throw new UIException("Carga denegada - Capture cantidad Scrap.");

						if (int.Parse(txtEtiquetasAlPaso.Text) == 0)
							throw new UIException("Carga denegada - Capture Etiquetas al Paso.");

						if (anchoMateriaPrima == (decimal)0)
							throw new UIException("Carga denegada - Capture Ancho de Materia Prima.");

						ValidateSuajeRequired(jobNum);
					}

					if (oprSeq == "20")
					{
						int rows = consultaBAQ(jobNum, "10");
						if (rows == 0)
							throw new UIException("Carga denegada - Complete la operación anterior.");
						if (laborQty == 0)
							throw new UIException("Carga denegada - Capture cantidad de mano de obra (Labor Qty)");

						if (int.Parse(txtEtiquetasAlPaso.Text) == 0)
							throw new UIException("Carga denegada - Capture Etiquetas al Paso.");

						if (anchoMateriaPrima == (decimal)0)
							throw new UIException("Carga denegada - Capture Ancho de Materia Prima.");

						ValidateSuajeRequired(jobNum);
					}

					if (oprSeq == "30")
					{
						edvLaborDtl.dataView[edvLaborDtl.Row]["AnchoMateriaPrima_c"] = 0;
						int rows = consultaBAQ(jobNum, "20");
						if (rows == 0)
							throw new UIException("Carga denegada - Complete la operación anterior.");
						if (laborQty == 0)
							throw new UIException("Carga denegada - Capture cantidad de mano de obra (Labor Qty)");
						if (int.Parse(txtEtiquetasAlPaso.Text) == 0)
							throw new UIException("Carga denegada - Capture Etiquetas al Paso.");
					}

					if (horas >= 12)
					{
						throw new UIException("Revise por favor su horario");
					}
				}
				break;
		}
	}

	public int consultaBAQ(string jobNum, string oprSeq)
	{
		int resultado = 0;
		try
		{
			DataTable dtDatos = new DataTable();

			DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
			adapterDynamicQuery.BOConnect();

			string BAQName = "TimeAndExpenseEntry";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();

			ds.ExecutionParameter.AddExecutionParameterRow("jobNum_", jobNum, "nvarchar", false, Guid.Empty, "A");
			if (oprSeq != "")
				ds.ExecutionParameter.AddExecutionParameterRow("oprSeq_", oprSeq, "nvarchar", false, Guid.Empty, "A");

			adapterDynamicQuery.ExecuteByID(BAQName, ds);

			dtDatos = adapterDynamicQuery.QueryResults.Tables["Results"];

			adapterDynamicQuery.Dispose();

			resultado = dtDatos.Rows.Count;

			return resultado;
		}
		catch (System.Exception ex)
		{
			ExceptionBox.Show(ex);
			return 0;
		}
	}

	public int consultaMateriales(string jobNum)
	{
		int resultado = 0;
		try
		{
			DataTable dtDatos = new DataTable();

			DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
			adapterDynamicQuery.BOConnect();

			string BAQName = "TimeAndExpenseEntryMaterial";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();

			ds.ExecutionParameter.AddExecutionParameterRow("jobNum_", jobNum, "nvarchar", false, Guid.Empty, "A");

			adapterDynamicQuery.ExecuteByID(BAQName, ds);

			dtDatos = adapterDynamicQuery.QueryResults.Tables["Results"];

			adapterDynamicQuery.Dispose();

			resultado = dtDatos.Rows.Count;

			return resultado;
		}
		catch (System.Exception ex)
		{
			ExceptionBox.Show(ex);
			return 0;
		}
	}

	private void consultaMatrizParaFormulas(string partNum)
	{
		try
		{
			DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
			adapterDynamicQuery.BOConnect();

			string BAQName = "ObtenerLargoReal";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();

			ds.ExecutionParameter.AddExecutionParameterRow("partNum", partNum, "nvarchar", false, Guid.Empty, "A");

			adapterDynamicQuery.ExecuteByID(BAQName, ds);

			if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			{
				txtLargoReal.Text = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
				txtCantidadPorRollo.Text = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString();
			}
			else
			{
				txtLargoReal.Text = "0";
				txtCantidadPorRollo.Text = "0";
			}

			adapterDynamicQuery.Dispose();
		}
		catch (System.Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	private void TimeExpenseForm_Load(object sender, EventArgs args)
	{
		cbTipo.DropDownStyle = UltraComboStyle.DropDownList;
		listatiempo.DropDownStyle = UltraComboStyle.DropDownList;
	}

	private void txtAnchoMateriaPrima_TextChanged(object sender, System.EventArgs args)
	{
		decimal anchoMateriaPrima = decimal.Parse(txtAnchoMateriaPrima.Text);
		decimal conversion = decimal.Parse("17.5");

		if (anchoMateriaPrima > conversion)
		{
			edvLaborDtl.dataView[edvLaborDtl.Row]["AnchoMateriaPrima_c"] = "0.0000";
			txtAnchoMateriaPrima.Text = "0.0000";
			throw new Exception("Verifique el ancho de materia prima usada por favor");
		}

		decimal ftCorrida_ = decimal.Parse(txtFTCorrida.Text);
		txtMSI.Text = ((anchoMateriaPrima * ftCorrida_ * 12) / 1000).ToString();
	}

	public void consultajob(string jobn)
	{
		string existe = "";
		try
		{
			DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
			adapterDynamicQuery.BOConnect();
			string BAQName = "BAQ_CHJOB";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("JOB", jobn, "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(BAQName, ds);

			if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
				existe = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
			if (jobn == existe)
			{
			}

			adapterDynamicQuery.Dispose();
		}
		catch (System.Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	private void edvLaborDtl_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if ((args.NotifyType == EpiTransaction.NotifyType.Initialize))
		{
			if ((args.Row > -1))
			{
				try
				{
					string job = edvLaborDtl.dataView[edvLaborDtl.Row]["JobNum"].ToString();

					TimeExpenseForm.Cursor = Cursors.WaitCursor;
					string jobnum = view.dataView[args.Row]["JobNum"].ToString();
					DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
					adapterDynamicQuery.BOConnect();
					string BAQName = "GASTO";
					Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();

					ds.ExecutionParameter.AddExecutionParameterRow("JOB", jobnum, "nvarchar", false, Guid.Empty, "A");
					adapterDynamicQuery.ExecuteByID(BAQName, ds);
					string Part1 = "";
					if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
						Part1 = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString();
					adapterDynamicQuery.Dispose();

					epiTextBoxC1.Text = Part1;

					TimeExpenseForm.Cursor = Cursors.Default;
				}
				catch (System.Exception ex)
				{
					TimeExpenseForm.Cursor = Cursors.Default;
					ExceptionBox.Show(ex);
				}
			}
		}
	}

	private void baseToolbarsManager_ToolClick(object sender, Infragistics.Win.UltraWinToolbars.ToolClickEventArgs args)
	{
		switch (args.Tool.Key)
		{
			case "DeleteTool":
				epiTextBoxC1.Text = string.Empty;
				break;
		}
	}
}
