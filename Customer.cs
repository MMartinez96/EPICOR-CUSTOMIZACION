// **************************************************
// Custom code for CustomerEntryForm
// Created: 04/05/2018 08:19:49 p. m.
// ADD: Nacional_c por Ship To (2026-08-06)
// v2: quita binding, guarda/carga por ShipToNum al cambiar árbol
// **************************************************

extern alias Erp_Contracts_BO_Vendor;


extern alias Erp_Contracts_BO_Customer;
extern alias Erp_Contracts_BO_BankAcct;
extern alias Erp_Contracts_BO_ShipTo;

using System;
using System.Collections.Generic;
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

public static class Script
{

	static EpiCombo TaxRegimeexternal;
	static EpiTextBox TaxRegimeinterno;
	static EpiTextBox txtName;
	private static EpiDataView edvCustomer;

	// --- ADD: Nacional por Ship To ---
	static EpiUltraCombo nacionalCombo;
	private static EpiDataView edvShipTo;
	private static bool nacionalLoading;
	private static string prevShipToKey;
	private static Dictionary<string, string> nacionalCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


	public static void InitializeCustomCode()
	{


		Script.Customer_Row.EpiRowChanging += new EpiRowChanging(Script.Customer_BeforeRowChange);
		Script.edvCustomer = ((EpiDataView)(Script.oTrans.EpiDataViews["Customer"]));
		Script.edvCustomer.EpiViewNotification += new EpiViewNotification(Script.edvCustomer_EpiViewNotification);
		Script.clickbtn.Click += new System.EventHandler(Script.clickbtn_Click);

		// --- ADD: Nacional por Ship To ---
		Script.edvShipTo = ((EpiDataView)(Script.oTrans.EpiDataViews["ShipTo"]));
		Script.edvShipTo.EpiViewNotification += new EpiViewNotification(Script.edvShipTo_EpiViewNotification);
		Script.edvShipTo.EpiRowChanging += new EpiRowChanging(Script.ShipTo_BeforeRowChange);
		Script.nacionalCombo = (EpiUltraCombo)csm.GetNativeControlReference("f39f555c-390b-43a6-9f66-081f76ffb9d8");
		if (Script.nacionalCombo != null)
		{
			Script.nacionalCombo.DataBindings.Clear();
			Script.nacionalCombo.ValueChanged += new EventHandler(Script.nacionalCombo_ValueChanged);
		}
		Script.prevShipToKey = null;
	}

	public static void DestroyCustomCode()
	{


		Script.Customer_Row.EpiRowChanging -= new EpiRowChanging(Script.Customer_BeforeRowChange);
		Script.edvCustomer.EpiViewNotification -= new EpiViewNotification(Script.edvCustomer_EpiViewNotification);
		Script.edvCustomer = null;
		Script.clickbtn.Click -= new System.EventHandler(Script.clickbtn_Click);

		if (Script.edvShipTo != null)
		{
			Script.edvShipTo.EpiViewNotification -= new EpiViewNotification(Script.edvShipTo_EpiViewNotification);
			Script.edvShipTo.EpiRowChanging -= new EpiRowChanging(Script.ShipTo_BeforeRowChange);
			Script.edvShipTo = null;
		}
		if (Script.nacionalCombo != null)
		{
			Script.nacionalCombo.ValueChanged -= new EventHandler(Script.nacionalCombo_ValueChanged);
			Script.nacionalCombo = null;
		}
		Script.prevShipToKey = null;
	}

	private static void Customer_BeforeRowChange(EpiRowChangingArgs args)
	{
		SaveNacionalToCurrentShipTo();
		Script.prevShipToKey = null;
	}
	
		public static void copyinfo(){
				
		TaxRegimeexternal= (EpiCombo)csm.GetNativeControlReference("17534e2e-f1df-4111-8995-0bb7b2767ba1");
						

			if ((edvCustomer.Row > -1))
			{
   edvCustomer.dataView[edvCustomer.Row]["TaxRegime_Interno_c"]=TaxRegimeexternal.Text;
}
		}

	private static void edvCustomer_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if ((args.NotifyType == EpiTransaction.NotifyType.Initialize))
		{
			Script.prevShipToKey = null;
			if ((args.Row > -1))
			{
					try 
					{	
							txtName= (EpiTextBox)csm.GetNativeControlReference("5483cdef-3049-4705-b597-28ae93bc7fdf");
							string customers = edvCustomer.dataView[edvCustomer.Row]["Name"].ToString();
												if (String.IsNullOrEmpty(customers))
													{
												txtName.Enabled=true;			 	
													}else{
										 				txtName.Enabled =false;
															}
					}
					catch (System.Exception ex)
					{
						
						ExceptionBox.Show(ex);
					}
			}
		}
	}

	private static void clickbtn_Click(object sender, System.EventArgs args)
	{
		copyinfo();
	}

	private static void ShipTo_BeforeRowChange(EpiRowChangingArgs args)
	{
		int rowToSave = (edvShipTo != null) ? edvShipTo.Row : -1;
		SaveNacionalToShipToRow(rowToSave);
		ScheduleLoadNacionalAfterRowChange();
	}

	private static void edvShipTo_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if (nacionalCombo == null || edvShipTo == null || edvShipTo.Row < 0)
			return;

		string currentKey = GetShipToKey(edvShipTo.Row);
		if (!string.Equals(currentKey, prevShipToKey, StringComparison.OrdinalIgnoreCase)
			|| args.NotifyType == EpiTransaction.NotifyType.Initialize
			|| args.NotifyType == EpiTransaction.NotifyType.AddRow)
		{
			LoadNacionalFromShipToRow(edvShipTo.Row);
		}
	}

	private static void nacionalCombo_ValueChanged(object sender, EventArgs args)
	{
		if (nacionalLoading)
			return;
		SaveNacionalToCurrentShipTo();
	}

	private static string GetShipToKey(int row)
	{
		if (edvShipTo == null || row < 0 || edvShipTo.dataView == null || row >= edvShipTo.dataView.Count)
			return null;

		try
		{
			DataRowView rowView = edvShipTo.dataView[row];
			if (rowView == null || rowView.Row.RowState == DataRowState.Deleted)
				return null;

			string cust = rowView.Row.Table.Columns.Contains("CustNum")
				? Convert.ToString(rowView["CustNum"])
				: string.Empty;
			string ship = rowView.Row.Table.Columns.Contains("ShipToNum")
				? Convert.ToString(rowView["ShipToNum"])
				: string.Empty;

			if (string.IsNullOrEmpty(cust) && string.IsNullOrEmpty(ship))
				return null;

			return cust + "|" + ship;
		}
		catch
		{
			return null;
		}
	}

	private static bool ShipToHasNacionalColumn()
	{
		if (edvShipTo == null || edvShipTo.dataView == null || edvShipTo.dataView.Table == null)
			return false;
		return edvShipTo.dataView.Table.Columns.Contains("Nacional_c");
	}

	private static string GetNacionalComboValue()
	{
		if (nacionalCombo == null)
			return string.Empty;

		if (nacionalCombo.Value != null && nacionalCombo.Value != DBNull.Value)
		{
			string v = nacionalCombo.Value.ToString();
			if (!string.IsNullOrWhiteSpace(v))
				return v.Trim();
		}

		if (!string.IsNullOrWhiteSpace(nacionalCombo.Text))
			return nacionalCombo.Text.Trim();

		return string.Empty;
	}

	private static void SetNacionalComboValue(string val)
	{
		if (nacionalCombo == null)
			return;

		try
		{
			nacionalLoading = true;
			if (string.IsNullOrEmpty(val))
				nacionalCombo.Value = string.Empty;
			else
				nacionalCombo.Value = val;
		}
		catch
		{
			try { nacionalCombo.Text = val ?? string.Empty; } catch { }
		}
		finally
		{
			nacionalLoading = false;
		}
	}

	private static string ReadNacionalFromRow(int row)
	{
		if (row < 0)
			return string.Empty;

		string key = GetShipToKey(row);
		string val = string.Empty;

		if (ShipToHasNacionalColumn())
		{
			try
			{
				object raw = edvShipTo.dataView[row]["Nacional_c"];
				if (raw != null && raw != DBNull.Value)
					val = raw.ToString().Trim();
			}
			catch { }
		}

		if (string.IsNullOrEmpty(val) && key != null && nacionalCache.ContainsKey(key))
			val = nacionalCache[key];

		return val ?? string.Empty;
	}

	private static void SaveNacionalToCurrentShipTo()
	{
		if (edvShipTo == null || edvShipTo.Row < 0)
			return;
		SaveNacionalToShipToRow(edvShipTo.Row);
	}

	private static void SaveNacionalToShipToRow(int row)
	{
		if (edvShipTo == null || row < 0 || nacionalCombo == null || nacionalLoading)
			return;

		string key = GetShipToKey(row);
		if (key == null)
			return;

		string val = GetNacionalComboValue();
		nacionalCache[key] = val;

		if (!ShipToHasNacionalColumn())
			return;

		try
		{
			DataRowView rowView = edvShipTo.dataView[row];
			if (rowView == null || rowView.Row.RowState == DataRowState.Deleted)
				return;

			rowView.BeginEdit();
			rowView["Nacional_c"] = val;
			rowView.EndEdit();
		}
		catch { }
	}

	private static void LoadNacionalFromShipToRow(int row)
	{
		if (edvShipTo == null || row < 0 || nacionalCombo == null)
			return;

		prevShipToKey = GetShipToKey(row);
		SetNacionalComboValue(ReadNacionalFromRow(row));
	}

	private static void LoadNacionalFromCurrentShipTo()
	{
		if (edvShipTo == null || edvShipTo.Row < 0)
			return;
		LoadNacionalFromShipToRow(edvShipTo.Row);
	}

	private static void ScheduleLoadNacionalAfterRowChange()
	{
		if (nacionalCombo == null)
			return;

		MethodInvoker loadAfterChange = delegate
		{
			if (nacionalCombo == null || nacionalCombo.IsDisposed || edvShipTo == null || edvShipTo.Row < 0)
				return;
			LoadNacionalFromCurrentShipTo();
		};

		try
		{
			if (nacionalCombo.IsHandleCreated && !nacionalCombo.IsDisposed)
				nacionalCombo.BeginInvoke(loadAfterChange);
			else
				loadAfterChange();
		}
		catch
		{
			loadAfterChange();
		}
	}
}