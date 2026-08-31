extern alias Erp_Contracts_BO_Part;
extern alias Erp_Contracts_BO_PartPlantSearch;
extern alias Erp_Contracts_BO_PO;
extern alias Erp_Contracts_BO_PartOnHandWhse;
extern alias Erp_Contracts_BO_Vendor;
extern alias Erp_Contracts_BO_VendorPPSearch;

using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Drawing;
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
using Infragistics.Win.UltraWinGrid;

public class Script
{
    // ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **
    // Begin Wizard Added Module Level Variables **
    EpiDataView edvPartForm;
    private UD10Adapter _ud10Adapter;
    private EpiDataView _edvPart;
    private EpiDataView _edvUD10;
    private string _Key1UD10;
    private string _Key2UD10;
    private string _Key3UD10;
    private string _Key4UD10;
    private string _Key5UD10;
    private DataView Part_DataView;
    private EpiBaseAdapter oTrans_adapter;
    private EpiDataView edvPart;
    EpiDataView partSt;
    EpiDataView UD10;
    // End Wizard Added Module Level Variables **

    // Add Custom Module Level Variables Here **

    public void InitializeCustomCode()
    {
        // ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
        // Begin Wizard Added Variable Initialization
        SearchOnUD02AdapterFillDropDown();
        edvPartForm = ((EpiDataView)(this.oTrans.EpiDataViews["Part"]));
        _edvUD10 = ((EpiDataView)(this.oTrans.EpiDataViews["UD10View"]));
        this._Key1UD10 = string.Empty;
        this._Key2UD10 = string.Empty;
        this._Key3UD10 = string.Empty;
        this._Key4UD10 = string.Empty;
        this._Key5UD10 = string.Empty;
        this.baseToolbarsManager.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClickForUD10);
        //this.PartForm.BeforeToolClick += new Ice.Lib.Framework.BeforeToolClickEventHandler(this.PartForm_BeforeToolClickForUD10);
        this.Part_Row.EpiRowChanged += new EpiRowChanged(this.Part_AfterRowChangeForUD10);
        this.Part_DataView = this.Part_Row.dataView;
        this.Part_DataView.ListChanged += new ListChangedEventHandler(this.Part_DataView_ListChangedForUD10);
        this.oTrans_adapter = ((EpiBaseAdapter)(this.csm.TransAdaptersHT["oTrans_adapter"]));
        this.oTrans_adapter.AfterAdapterMethod += new AfterAdapterMethod(this.oTrans_adapter_AfterAdapterMethod);
        this.edvPart = ((EpiDataView)(this.oTrans.EpiDataViews["Part"]));
        this.edvPart.EpiViewNotification += new EpiViewNotification(this.edvPart_EpiViewNotification);
        this.oTrans_adapter.BeforeAdapterMethod += new BeforeAdapterMethod(this.oTrans_adapter_BeforeAdapterMethod);

        // Imagen IMG-PART desde BAQ
        this.btnRefreshImg.Click += new System.EventHandler(this.btnRefreshImg_Click);
        CargarImagenPart();
        // End Wizard Added Variable Initialization

        // Begin Wizard Added Custom Method Calls

        InitializeUD10Adapter();
        this.lgCmbNoTintas.ValueChanged += new System.EventHandler(this.lgCmbNoTintas_ValueChanged);
        this.lgNmeVelocidadStd.LostFocus += new System.EventHandler(this.lgNmeVelocidadStd_LostFocus);
        this.lgNmeAnchoMp1.LostFocus += new System.EventHandler(this.lgNmeAnchoMp1_LostFocus);
        this.lgNmeLinerLatIzq.LostFocus += new System.EventHandler(this.lgNmeLinerLatIzq_LostFocus);
        this.lgNmeLinerLatDer.LostFocus += new System.EventHandler(this.lgNmeLinerLatDer_LostFocus);
        this.lgTxtAncho.LostFocus += new System.EventHandler(this.lgTxtAncho_LostFocus);
        this.lgTxtLargo.LostFocus += new System.EventHandler(this.lgTxtLargo_LostFocus);
        this.lgTxtGapVertical.LostFocus += new System.EventHandler(this.lgTxtGapVertical_LostFocus);


        fechaenvio1.Enabled = false;
        fecharegreso1.Enabled = false;
        acmulado1.Enabled = false;
        fechaenvio2.Enabled = false;
        fecharegreso2.Enabled = false;
        acumulado2.Enabled = false;
        fechaenvio3.Enabled = false;
        fecharegreso3.Enabled = false;
        acumulado3.Enabled = false;
        fechaaenvio4.Enabled = false;
        fecharegreso4.Enabled = false;
        acumulado4.Enabled = false;
        fechaenvio5.Enabled = false;
        fecharegreso5.Enabled = false;
        acumulado5.Enabled = false;
        fechaenvio6.Enabled = false;
        fecharegreso6.Enabled = false;
        acumulado6.Enabled = false;
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        numregistro.Enabled = false;
        fechareg.Enabled = false;
        descripcioncambio.Enabled = false;
        responsablereg1.Enabled = false;
        placass.Enabled = false;
        stockk.Enabled = false;

        numreg2.Enabled = false;
        fecha2reg.Enabled = false;
        descripcionreg2.Enabled = false;
        responsablereg2.Enabled = false;
        placas2.Enabled = false;
        stock2.Enabled = false;




        numreg3.Enabled = false;
        fechareg3.Enabled = false;
        descreg3.Enabled = false;
        responsable3reg.Enabled = false;
        placa3reg.Enabled = false;
        stockreg3.Enabled = false;


        num4reg.Enabled = false;
        fechareg4.Enabled = false;
        descreg4.Enabled = false;
        responsablereg4.Enabled = false;
        placasreg4.Enabled = false;
        stock4reg.Enabled = false;


        numreg5.Enabled = false;
        fechareg5.Enabled = false;
        descreg5.Enabled = false;
        responsablereg5.Enabled = false;
        placasreg5.Enabled = false;
        stockreg5.Enabled = false;

        numreg6.Enabled = false;
        fechareg6.Enabled = false;
        descrreg6.Enabled = false;
        responsablereg6.Enabled = false;
        placareg6.Enabled = false;
        stockreg6.Enabled = false;


        numreg7.Enabled = false;
        fechareg7.Enabled = false;
        descripcionreg7.Enabled = false;
        responsablereg7.Enabled = false;
        placasreg7.Enabled = false;
        stockreg7.Enabled = false;


        numreg8.Enabled = false;
        fechareg8.Enabled = false;
        descripcionreg8.Enabled = false;
        responsablereg8.Enabled = false;
        placasreg8.Enabled = false;
        stockreg8.Enabled = false;



        numreg9.Enabled = false;
        fechareg9.Enabled = false;
        desreg9.Enabled = false;
        responsablereg9.Enabled = false;
        placasreg9.Enabled = false;
        stockreg9.Enabled = false;


        numreg10.Enabled = false;
        feachareg10.Enabled = false;
        descrpreg10.Enabled = false;
        responsablereg10.Enabled = false;
        placasre10.Enabled = false;
        stockreg10.Enabled = false;

        rev101.Enabled = false;
        rev22.Enabled = false;
        rev33.Enabled = false;
        rev44.Enabled = false;
        rev55.Enabled = false;
        rev66.Enabled = false;
        rev77.Enabled = false;
        rev88.Enabled = false;
        rev99.Enabled = false;
        rev100.Enabled = false;



        this.lgNmeAnchoMp1.TextChanged += new System.EventHandler(this.lgNmeAnchoMp1_TextChanged);
        this.lgNmeVelocidadStd.TextChanged += new System.EventHandler(this.lgNmeVelocidadStd_TextChanged);
        this.lgNmeLinerLatIzq.TextChanged += new System.EventHandler(this.lgNmeLinerLatIzq_TextChanged);
        this.lgNmeLinerLatDer.TextChanged += new System.EventHandler(this.lgNmeLinerLatDer_TextChanged);
        this.lgTxtAncho.TextChanged += new System.EventHandler(this.lgTxtAncho_TextChanged);
        this.lgTxtLargo.TextChanged += new System.EventHandler(this.lgTxtLargo_TextChanged);
        this.lgTxtGapVertical.TextChanged += new System.EventHandler(this.lgTxtGapVertical_TextChanged);
        this.refialdonum.ValueChanged += new System.EventHandler(this.refialdonum_ValueChanged);
        this.seleccionnum.ValueChanged += new System.EventHandler(this.seleccionnum_ValueChanged);
        // End Wizard Added Custom Method Calls
    }

    public void DestroyCustomCode()
    {
        // ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
        // Begin Wizard Added Object Disposal

        if ((this._ud10Adapter != null))
        {
            this._ud10Adapter.Dispose();
            this._ud10Adapter = null;
        }
        this._edvUD10 = null;
        this._edvPart = null;
        this._Key1UD10 = null;
        this._Key2UD10 = null;
        this._Key3UD10 = null;
        this._Key4UD10 = null;
        this._Key5UD10 = null;
        this.baseToolbarsManager.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClickForUD10);
        //this.PartForm.BeforeToolClick -= new Ice.Lib.Framework.BeforeToolClickEventHandler(this.PartForm_BeforeToolClickForUD10);
        this.Part_Row.EpiRowChanged -= new EpiRowChanged(this.Part_AfterRowChangeForUD10);
        this.Part_DataView.ListChanged -= new ListChangedEventHandler(this.Part_DataView_ListChangedForUD10);
        this.Part_DataView = null;
        this.oTrans_adapter.AfterAdapterMethod -= new AfterAdapterMethod(this.oTrans_adapter_AfterAdapterMethod);
        this.oTrans_adapter = null;
        this.edvPart.EpiViewNotification -= new EpiViewNotification(this.edvPart_EpiViewNotification);
        this.btnRefreshImg.Click -= new System.EventHandler(this.btnRefreshImg_Click);
        LimpiarImagenPart();
        this.edvPart = null;
        this.lgCmbNoTintas.ValueChanged -= new System.EventHandler(this.lgCmbNoTintas_ValueChanged);
        this.lgNmeVelocidadStd.LostFocus -= new System.EventHandler(this.lgNmeVelocidadStd_LostFocus);
        this.lgNmeAnchoMp1.LostFocus -= new System.EventHandler(this.lgNmeAnchoMp1_LostFocus);
        this.lgNmeLinerLatIzq.LostFocus -= new System.EventHandler(this.lgNmeLinerLatIzq_LostFocus);
        this.lgNmeLinerLatDer.LostFocus -= new System.EventHandler(this.lgNmeLinerLatDer_LostFocus);
        this.lgTxtAncho.LostFocus -= new System.EventHandler(this.lgTxtAncho_LostFocus);
        this.lgTxtLargo.LostFocus -= new System.EventHandler(this.lgTxtLargo_LostFocus);
        this.lgTxtGapVertical.LostFocus -= new System.EventHandler(this.lgTxtGapVertical_LostFocus);
        this.lgNmeAnchoMp1.TextChanged -= new System.EventHandler(this.lgNmeAnchoMp1_TextChanged);
        this.lgNmeVelocidadStd.TextChanged -= new System.EventHandler(this.lgNmeVelocidadStd_TextChanged);
        this.lgNmeLinerLatIzq.TextChanged -= new System.EventHandler(this.lgNmeLinerLatIzq_TextChanged);
        this.lgNmeLinerLatDer.TextChanged -= new System.EventHandler(this.lgNmeLinerLatDer_TextChanged);
        this.lgTxtAncho.TextChanged -= new System.EventHandler(this.lgTxtAncho_TextChanged);
        this.lgTxtLargo.TextChanged -= new System.EventHandler(this.lgTxtLargo_TextChanged);
        this.lgTxtGapVertical.TextChanged -= new System.EventHandler(this.lgTxtGapVertical_TextChanged);
        this.oTrans_adapter.BeforeAdapterMethod -= new BeforeAdapterMethod(this.oTrans_adapter_BeforeAdapterMethod);
        this.refialdonum.ValueChanged -= new System.EventHandler(this.refialdonum_ValueChanged);
        this.seleccionnum.ValueChanged -= new System.EventHandler(this.seleccionnum_ValueChanged);
        // End Wizard Added Object Disposal

        // Begin Custom Code Disposal

        // End Custom Code Disposal
    }

    private void InitializeUD10Adapter()
    {
        // Create an instance of the Adapter.
        this._ud10Adapter = new UD10Adapter(this.oTrans);
        this._ud10Adapter.BOConnect();

        // Add Adapter Table to List of Views
        // This allows you to bind controls to the custom UD Table
        this._edvUD10 = new EpiDataView();
        this._edvUD10.dataView = new DataView(this._ud10Adapter.UD10Data.UD10);
        this._edvUD10.AddEnabled = true;
        //this._edvUD10.AddText = "New UD10";
        this._edvUD10.AddText = "New Matriz de Partes";
        if ((this.oTrans.EpiDataViews.ContainsKey("UD10View") == false))
        {
            this.oTrans.Add("UD10View", this._edvUD10);
        }

        // Initialize EpiDataView field.
        this._edvPart = ((EpiDataView)(this.oTrans.EpiDataViews["Part"]));

        // Set the parent view / keys for UD child view
        string[] parentKeyFields = new string[1];
        string[] childKeyFields = new string[1];
        parentKeyFields[0] = "PartNum";
        childKeyFields[0] = "Key1";
        this._edvUD10.SetParentView(this._edvPart, parentKeyFields, childKeyFields);

        if ((this.oTrans.PrimaryAdapter != null))
        {
            // this.oTrans.PrimaryAdapter.GetCurrentDataSet(Ice.Lib.Searches.DataSetMode.RowsDataSet).Tables.Add(this._edvUD10.dataView.Table.Clone())
        }

    }

    private void GetUD10Data(string key1, string key2, string key3, string key4, string key5)
    {
        if ((this._Key1UD10 != key1) || (this._Key2UD10 != key2) || (this._Key3UD10 != key3) || (this._Key4UD10 != key4) || (this._Key5UD10 != key5))
        {
            // Build where clause for search.
            string whereClause = "Key1 = \'" + key1 + "\' And Key2 = \'" + key2 + "\' And Key3 = \'" + key3 + "\' And Key4 = \'" + key4 + "\'";
            System.Collections.Hashtable whereClauses = new System.Collections.Hashtable(1);
            whereClauses.Add("UD10", whereClause);

            // Call the adapter search.
            SearchOptions searchOptions = SearchOptions.CreateRuntimeSearch(whereClauses, DataSetMode.RowsDataSet);
            this._ud10Adapter.InvokeSearch(searchOptions);

            if ((this._ud10Adapter.UD10Data.UD10.Rows.Count > 0))
            {
                this._edvUD10.Row = 0;
            }
            else
            {
                this._edvUD10.Row = -1;
            }

            // Notify that data was updated.
            this._edvUD10.Notify(new EpiNotifyArgs(this.oTrans, this._edvUD10.Row, this._edvUD10.Column));

            // Set key fields to their new values.
            this._Key1UD10 = key1;
            this._Key2UD10 = key2;
            this._Key3UD10 = key3;
            this._Key4UD10 = key4;
            this._Key5UD10 = key5;
        }
    }

    private void GetNewUD10Record()
    {
        DataRow parentViewRow = this._edvPart.CurrentDataRow;
        // Check for existence of Parent Row.
        if ((parentViewRow == null))
        {
            return;
        }
        if (this._ud10Adapter.GetaNewUD10())
        {
            string partnum = parentViewRow["PartNum"].ToString();

            // Get unique row count id for Key5
            int rowCount = this._ud10Adapter.UD10Data.UD10.Rows.Count;
            int lineNum = rowCount;
            bool goodIndex = false;
            while ((goodIndex == false))
            {
                // Check to see if index exists
                DataRow[] matchingRows = this._ud10Adapter.UD10Data.UD10.Select("Key5 = \'" + lineNum.ToString() + "\'");
                if ((matchingRows.Length > 0))
                {
                    lineNum = (lineNum + 1);
                }
                else
                {
                    goodIndex = true;
                }
            }

            // Set initial UD Key values
            DataRow editRow = this._ud10Adapter.UD10Data.UD10.Rows[(rowCount - 1)];
            editRow.BeginEdit();
            //MessageBox.Show(partnum);
            editRow["Key1"] = partnum;
            editRow["Key2"] = "0";
            editRow["Key3"] = "0";
            editRow["Key4"] = "0";
            editRow["Key5"] = "0";
            lgTxtTinta1.Enabled = false;
            lgTxtTinta2.Enabled = false;
            lgTxtTinta3.Enabled = false;
            lgTxtTinta4.Enabled = false;
            lgTxtTinta5.Enabled = false;
            lgTxtTinta6.Enabled = false;
            lgTxtTinta7.Enabled = false;
            lgTxtTinta8.Enabled = false;
            editRow.EndEdit();

            // Notify that data was updated.
            this._edvUD10.Notify(new EpiNotifyArgs(this.oTrans, (rowCount - 1), this._edvUD10.Column));
        }
    }

    private void SaveUD10Record()
    {
        // Save adapter data
        this._ud10Adapter.Update();
    }

    private void DeleteUD10Record()
    {
        // Check to see if deleted view is ancestor view
        bool isAncestorView = false;
        Ice.Lib.Framework.EpiDataView parView = this._edvUD10.ParentView;
        while ((parView != null))
        {
            if ((this.oTrans.LastView == parView))
            {
                isAncestorView = true;
                break;
            }
            else
            {
                parView = parView.ParentView;
            }
        }

        // If Ancestor View then delete all child rows
        if (isAncestorView)
        {
            DataRow[] drsDeleted = this._ud10Adapter.UD10Data.UD10.Select("Key1 = \'" + this._Key1UD10 + "\' AND Key2 = \'" + this._Key2UD10 + "\' AND Key3 = \'" + this._Key3UD10 + "\' AND Key4 = \'" + this._Key4UD10 + "\'");
            for (int i = 0; (i < drsDeleted.Length); i = (i + 1))
            {
                this._ud10Adapter.Delete(drsDeleted[i]);
            }
        }
        else
        {
            if ((this.oTrans.LastView == this._edvUD10))
            {
                if ((this._edvUD10.Row >= 0))
                {
                    DataRow drDeleted = ((DataRow)(this._ud10Adapter.UD10Data.UD10.Rows[this._edvUD10.Row]));
                    if ((drDeleted != null))
                    {
                        if (this._ud10Adapter.Delete(drDeleted))
                        {
                            if ((_edvUD10.Row > 0))
                            {
                                _edvUD10.Row = (_edvUD10.Row - 1);
                            }

                            // Notify that data was updated.
                            this._edvUD10.Notify(new EpiNotifyArgs(this.oTrans, this._edvUD10.Row, this._edvUD10.Column));
                        }
                    }
                }
            }
        }
    }

    private void UndoUD10Changes()
    {
        this._ud10Adapter.UD10Data.RejectChanges();

        // Notify that data was updated.
        this._edvUD10.Notify(new EpiNotifyArgs(this.oTrans, this._edvUD10.Row, this._edvUD10.Column));
    }

    private void ClearUD10Data()
    {
        this._Key1UD10 = string.Empty;
        this._Key2UD10 = string.Empty;
        this._Key3UD10 = string.Empty;
        this._Key4UD10 = string.Empty;
        this._Key5UD10 = string.Empty;

        this._ud10Adapter.UD10Data.Clear();

        // Notify that data was updated.
        this._edvUD10.Notify(new EpiNotifyArgs(this.oTrans, this._edvUD10.Row, this._edvUD10.Column));
    }

    private void baseToolbarsManager_ToolClickForUD10(object sender, Infragistics.Win.UltraWinToolbars.ToolClickEventArgs args)
    {
        // EpiMessageBox.Show(args.Tool.Key);
        //MessageBox.Show(args.Tool.Key);
        switch (args.Tool.Key)
        {
            case "EpiAddNewPart":
            case "EpiAddNewNew Matriz de Partes":
                GetNewUD10Record();
                break;

            case "ClearTool":
                ClearUD10Data();
                break;

            case "DeleteTool":
                DeleteUD10Record();
                break;

            case "UndoTool":
                UndoUD10Changes();
                break;
            case "SaveTool":
                SaveUD10Record();
                break;
        }
    }


    private void Part_AfterRowChangeForUD10(EpiRowChangedArgs args)
    {
        // ** add AfterRowChange event handler
        string partnum = args.CurrentView.dataView[args.CurrentRow]["PartNum"].ToString();
        GetUD10Data(partnum, "0", "0", "0", "0");
    }

    private void Part_DataView_ListChangedForUD10(object sender, ListChangedEventArgs args)
    {
        // ** add ListChanged event handler
        string partnum = Part_DataView[0]["PartNum"].ToString();
        GetUD10Data(partnum, "0", "0", "0", "0");
    }

    private void oTrans_adapter_AfterAdapterMethod(object sender, AfterAdapterMethodArgs args)
    {
        // ** Argument Properties and Uses **
        // ** args.MethodName **
        // ** Add Event Handler Code **
        string partnum = "";
        // ** Use MessageBox to find adapter method name
        switch (args.MethodName)
        {
            case "GetByID":
                try
                {
                    partnum = edvPartForm.dataView[edvPartForm.Row]["PartNum"].ToString();//args.CurrentView.dataView[args.CurrentRow]["PartNum"].ToString();
                    GetUD10Data(partnum, "0", "0", "0", "0");
                }
                catch { }
                break;

        }

    }

    private void edvPart_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
    {
        string partnum = "";
        try
        {
            if (args.Row > -1)
            {
                try
                {
                    partnum = edvPartForm.dataView[edvPartForm.Row]["PartNum"].ToString();
                    GetUD10Data(partnum, "0", "0", "0", "0");
                }
                catch { }
                CargarImagenPart();
            }
            else
            {
                LimpiarImagenPart();
            }
        }
        catch { }
    }

    private void lgCmbNoTintas_ValueChanged(object sender, System.EventArgs args)
    {
        // ** Place Event Handling Code Here **
        switch (lgCmbNoTintas.Text)
        {
            case "0":
                lgTxtTinta1.Enabled = false;
                lgTxtTinta2.Enabled = false;
                lgTxtTinta3.Enabled = false;
                lgTxtTinta4.Enabled = false;
                lgTxtTinta5.Enabled = false;
                lgTxtTinta6.Enabled = false;
                lgTxtTinta7.Enabled = false;
                lgTxtTinta8.Enabled = false;
                break;
            case "1":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = false;
                lgTxtTinta3.Enabled = false;
                lgTxtTinta4.Enabled = false;
                lgTxtTinta5.Enabled = false;
                lgTxtTinta6.Enabled = false;
                lgTxtTinta7.Enabled = false;
                lgTxtTinta8.Enabled = false;
                break;
            case "2":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = true;
                lgTxtTinta3.Enabled = false;
                lgTxtTinta4.Enabled = false;
                lgTxtTinta5.Enabled = false;
                lgTxtTinta6.Enabled = false;
                lgTxtTinta7.Enabled = false;
                lgTxtTinta8.Enabled = false;
                break;
            case "3":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = true;
                lgTxtTinta3.Enabled = true;
                lgTxtTinta4.Enabled = false;
                lgTxtTinta5.Enabled = false;
                lgTxtTinta6.Enabled = false;
                lgTxtTinta7.Enabled = false;
                lgTxtTinta8.Enabled = false;
                break;
            case "4":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = true;
                lgTxtTinta3.Enabled = true;
                lgTxtTinta4.Enabled = true;
                lgTxtTinta5.Enabled = false;
                lgTxtTinta6.Enabled = false;
                lgTxtTinta7.Enabled = false;
                lgTxtTinta8.Enabled = false;
                break;
            case "5":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = true;
                lgTxtTinta3.Enabled = true;
                lgTxtTinta4.Enabled = true;
                lgTxtTinta5.Enabled = true;
                lgTxtTinta6.Enabled = false;
                lgTxtTinta7.Enabled = false;
                lgTxtTinta8.Enabled = false;
                break;
            case "6":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = true;
                lgTxtTinta3.Enabled = true;
                lgTxtTinta4.Enabled = true;
                lgTxtTinta5.Enabled = true;
                lgTxtTinta6.Enabled = true;
                lgTxtTinta7.Enabled = false;
                lgTxtTinta8.Enabled = false;
                break;
            case "7":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = true;
                lgTxtTinta3.Enabled = true;
                lgTxtTinta4.Enabled = true;
                lgTxtTinta5.Enabled = true;
                lgTxtTinta6.Enabled = true;
                lgTxtTinta7.Enabled = true;
                lgTxtTinta8.Enabled = false;
                break;
            case "8":
                lgTxtTinta1.Enabled = true;
                lgTxtTinta2.Enabled = true;
                lgTxtTinta3.Enabled = true;
                lgTxtTinta4.Enabled = true;
                lgTxtTinta5.Enabled = true;
                lgTxtTinta6.Enabled = true;
                lgTxtTinta7.Enabled = true;
                lgTxtTinta8.Enabled = true;
                break;
        }
    }

    /*
      MSI/MIN  =    Number09    =   (Number08 * 12)*(Number01 / 1000)
      MSIMILL  =    Variable      = (ShortChar03+(Number18+Number19))*(ShortChart04+ShortChart05)
      MILL/MSI =    Number11      = 1 / MSIMILL;
      MILL/MIN =    Number10      = Number09 * Number11
      FT/MILL  =    Number12      = (MSIMILL * 1000)/(12 * Number01)
      FT/MSI   =    Number13      = Number12 * Number11;

  */

    //Variables para calculos
    Decimal Number01;
    Decimal ShortChar03;
    Decimal ShortChar04;
    Decimal ShortChar05;
    Decimal Number08;
    Decimal Number18;
    Decimal Number19;
    Decimal MSIMILL = 0.0M;
    Decimal Number09;
    Decimal Number11;
    Decimal Number10;
    Decimal Number12;
    Decimal Number13;

    private void CalcularMSIMIN()
    {
        try
        {
            if (_edvUD10 == null || _edvUD10.Row < 0) return;

            Number01 = Convert.ToDecimal(_edvUD10.dataView[_edvUD10.Row]["Number01"].ToString());
            Number08 = Convert.ToDecimal(_edvUD10.dataView[_edvUD10.Row]["Number08"].ToString());
            Number09 = (Number08 * 12) * (Number01 / 1000);
            _edvUD10.dataView[_edvUD10.Row]["Number09"] = Number09;
        }
        catch { }
    }
    private void lgNmeVelocidadStd_LostFocus(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();

    }
    private void lgNmeAnchoMp1_LostFocus(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
    }

    private void CalcularMSIMILL()
    {
        try
        {
            if (_edvUD10 == null || _edvUD10.Row < 0) return;

            ShortChar03 = Convert.ToDecimal(_edvUD10.dataView[_edvUD10.Row]["ShortChar03"].ToString());
            Number18 = Convert.ToDecimal(_edvUD10.dataView[_edvUD10.Row]["Number18"].ToString());
            Number19 = Convert.ToDecimal(_edvUD10.dataView[_edvUD10.Row]["Number19"].ToString());
            ShortChar04 = Convert.ToDecimal(_edvUD10.dataView[_edvUD10.Row]["ShortChar04"].ToString());
            ShortChar05 = Convert.ToDecimal(_edvUD10.dataView[_edvUD10.Row]["ShortChar05"].ToString());
            MSIMILL = (ShortChar03 + (Number18 + Number19)) * (ShortChar04 + ShortChar05);

            CalcularOtros();
        }
        catch { }
    }
    private void CalcularOtros()
    {
        try
        {
            if (_edvUD10 == null || _edvUD10.Row < 0) return;
            if (MSIMILL == 0) return;

            Number11 = 1 / MSIMILL;
            _edvUD10.dataView[_edvUD10.Row]["Number11"] = Number11;
            Number10 = Number09 * Number11;
            _edvUD10.dataView[_edvUD10.Row]["Number10"] = Number10;
            Number12 = (MSIMILL * 1000) / (12 * Number01);
            _edvUD10.dataView[_edvUD10.Row]["Number12"] = Number12;
            Number13 = Number12 * Number11;
            _edvUD10.dataView[_edvUD10.Row]["Number13"] = Number13;
        }
        catch { }
    }
    private void lgNmeLinerLatIzq_LostFocus(object sender, System.EventArgs args)
    {
        CalcularMSIMILL();
    }
    private void lgNmeLinerLatDer_LostFocus(object sender, System.EventArgs args)
    {
        CalcularMSIMILL();
    }
    private void lgTxtAncho_LostFocus(object sender, System.EventArgs args)
    {
        CalcularMSIMILL();
    }
    private void lgTxtLargo_LostFocus(object sender, System.EventArgs args)
    {
        CalcularMSIMILL();
    }
    private void lgTxtGapVertical_LostFocus(object sender, System.EventArgs args)
    {
        CalcularMSIMILL();
    }


    private void lgNmeAnchoMp1_TextChanged(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
        CalcularMSIMILL();
    }
    private void lgNmeVelocidadStd_TextChanged(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
        CalcularMSIMILL();
    }
    private void lgNmeLinerLatIzq_TextChanged(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
        CalcularMSIMILL();
    }
    private void lgNmeLinerLatDer_TextChanged(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
        CalcularMSIMILL();
    }
    private void lgTxtAncho_TextChanged(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
        CalcularMSIMILL();
    }
    private void lgTxtLargo_TextChanged(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
        CalcularMSIMILL();
    }
    private void lgTxtGapVertical_TextChanged(object sender, System.EventArgs args)
    {
        CalcularMSIMIN();
        CalcularMSIMILL();
    }

    //JDEQ 2013-11-22
    //Método que llena el combo de Clasificación Arancelaria
    private void SearchOnUD02AdapterFillDropDown()
    {
        // Wizard Generated Search Method
        // You will need to call this method from another method in custom code
        // For example, [Form]_Load or [Button]_Click

        bool recSelected;
        DataTable dtClasificacion = new DataTable();
        dtClasificacion.Columns.Add("Clasificacion", typeof(string));
        //dtClasificacion.Columns.Add("ID",typeof(string));

        string whereClause = string.Empty;
        System.Data.DataSet dsUD02Adapter = Ice.UI.FormFunctions.SearchFunctions.listLookup(this.oTrans, "UD02Adapter", out recSelected, false, whereClause);
        if (recSelected)
        {
            // Set EpiUltraCombo Properties
            foreach (DataRow dr in dsUD02Adapter.Tables[0].Rows)
            {
                DataRow drC = dtClasificacion.NewRow();
                drC["Clasificacion"] = dr["Key1"].ToString() + "-" + dr["Character01"].ToString();
                //drC["ID"] = dr["Key1"].ToString();

                dtClasificacion.Rows.Add(drC);
            }

            this.jeCmbClasificacionArancelaria.DataSource = dtClasificacion;
            this.jeCmbClasificacionArancelaria.ValueMember = "Clasificacion";
            this.jeCmbClasificacionArancelaria.DisplayMember = "Clasificacion";
            string[] fields = new string[] {
                    "Clasificacion"};
            this.jeCmbClasificacionArancelaria.SetColumnFilter(fields);
        }
    }

    private void oTrans_adapter_BeforeAdapterMethod(object sender, BeforeAdapterMethodArgs args)
    {
        // ** Argument Properties and Uses **
        // ** args.MethodName **
        // ** Add Event Handler Code **

        // ** Use MessageBox to find adapter method name
        // EpiMessageBox.Show(args.MethodName)
        switch (args.MethodName)
        {
            case "Update":
                partSt = ((EpiDataView)(this.oTrans.EpiDataViews["Part"]));


                if (partSt.Row > -1)
                {
                    EpiCheckBox checkboxs = (EpiCheckBox)csm.GetNativeControlReference("9bf0e691-3530-427d-931b-a9c02af8c075");
                    string classe = partSt.dataView[partSt.Row]["ClassId"].ToString();
                    string PARTE = partSt.dataView[partSt.Row]["PartNum"].ToString();
                    string departament = partSt.dataView[partSt.Row]["Departamento_c"].ToString();
                    string Estatus_c = partSt.dataView[partSt.Row]["Estatus_c"].ToString();
                    string Tipo_c = partSt.dataView[partSt.Row]["Tipo_c"].ToString();
                    decimal maquina = Convert.ToDecimal(lgNmeUECaja.Text);
                    decimal MSI = decimal.Parse(partSt.dataView[partSt.Row]["MsiUnidadProd_c"].ToString());
                    string grupo = partSt.dataView[partSt.Row]["ProdCode"].ToString();
                    string cliente = partSt.dataView[partSt.Row]["Cliente_c"].ToString();
                    string codigoproductoservicio = partSt.dataView[partSt.Row]["MXProdServCode"].ToString();


                    if (departament == "Flexo" && txtSalida.Text == "")
                        throw new UIException("INGRESE LA SALIDA EN LA MATRIZ DE PARTES");

                    if (departament == "Flexo" && maquina == 0)
                        throw new UIException("INGRESE LA SALIDA MAQUINA EN LA MATRIZ DE PARTES");

                    if (String.IsNullOrEmpty(classe))
                        throw new UIException("FAVOR DE INGRESAR LA CLASE");

                    if (String.IsNullOrEmpty(departament))
                        throw new UIException("FAVOR DE INGERSAR EL DEPARTAMENTO");


                    if (departament == "Flexo" && String.IsNullOrEmpty(Estatus_c))
                        throw new UIException("FAVOR DE INGERSAR EL ESTATUS ");

                    if (departament == "Flexo" && String.IsNullOrEmpty(Tipo_c))
                        throw new UIException("FAVOR DE INGERSAR EL TIPO");


                    if (departament == "Offset" && String.IsNullOrEmpty(Estatus_c))
                        throw new UIException("FAVOR DE INGERSAR EL ESTATUS ");

                    if (departament == "Offset" && String.IsNullOrEmpty(Tipo_c))
                        throw new UIException("FAVOR DE INGERSAR EL TIPO");


                    if (departament == "MP flexo" && String.IsNullOrEmpty(Tipo_c))
                        throw new UIException("FAVOR DE INGERSAR EL TIPO ");

                    if (departament == "MP offset" && String.IsNullOrEmpty(Tipo_c))
                        throw new UIException("FAVOR DE INGERSAR EL TIPO");



                    /////////////////////////////INGRESO DE GRUPO////////////////////////////////// 

                    if (departament == "Flexo" && String.IsNullOrEmpty(grupo))
                        throw new UIException("FAVOR DE INGERSAR EL GRUPO ");

                    if (departament == "Offset" && String.IsNullOrEmpty(grupo))
                        throw new UIException("FAVOR DE INGERSAR EL GRUPO ");

                    if (departament == "Comer" && String.IsNullOrEmpty(grupo))
                        throw new UIException("FAVOR DE INGERSAR EL GRUPO ");

                    //////////////////////////////////////////////////////////////////////////////////////////////////////

                    if (departament == "MP flexo" && MSI == 0)
                        throw new UIException("FAVOR DE INGRESAR ANCHO DE MP");

                    if (departament == "Flexo" && MSI == 0)
                        throw new UIException("FAVOR DE INGRESAR MSI ING");
                    /////////////////////////////////////////EXCEPCIÓN CLIENTE////////////////////////////////////////////

                    if (departament == "Flexo" && String.IsNullOrEmpty(cliente))
                        throw new UIException("FAVOR DE INGRESAR EL CLIENTE");

                    if (departament == "Offset" && String.IsNullOrEmpty(cliente))
                        throw new UIException("FAVOR DE INGRESAR EL CLIENTE");

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (departament == "MP flexo" && checkboxs.Checked == false)
                        checkboxs.Checked = true;
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////
                    ////////////////////////////////////////////////CÓDIGO DE PRODUCTO Y SERVICIO////////////////////////
                    if (departament == "Flexo" && String.IsNullOrEmpty(codigoproductoservicio))
                        throw new UIException("FAVOR DE INGERSAR EL CÓDIGO DE PRODUCTO Y SERVICIO");

                    if (departament == "Offset" && String.IsNullOrEmpty(codigoproductoservicio))
                        throw new UIException("FAVOR DE INGERSAR EL CÓDIGO DE PRODUCTO Y SERVICIO");

                    if (departament == "Serigrafia" && String.IsNullOrEmpty(codigoproductoservicio))
                        throw new UIException("FAVOR DE INGERSAR EL CÓDIGO DE PRODUCTO Y SERVICIO");

                    if (departament == "Comer" && String.IsNullOrEmpty(codigoproductoservicio))
                        throw new UIException("FAVOR DE INGERSAR EL CÓDIGO DE PRODUCTO Y SERVICIO");

                    if (departament == "Digital" && String.IsNullOrEmpty(codigoproductoservicio))
                        throw new UIException("FAVOR DE INGERSAR EL CÓDIGO DE PRODUCTO Y SERVICIO");
                }

                break;
        }

    }
    private void refialdonum_ValueChanged(object sender, System.EventArgs args)
    {
        switch (refialdonum.Text)
        {
            case "0":
                fechaenvio1.Enabled = false;
                fecharegreso1.Enabled = false;
                acmulado1.Enabled = false;
                fechaenvio2.Enabled = false;
                fecharegreso2.Enabled = false;
                acumulado2.Enabled = false;
                fechaenvio3.Enabled = false;
                fecharegreso3.Enabled = false;
                acumulado3.Enabled = false;
                fechaaenvio4.Enabled = false;
                fecharegreso4.Enabled = false;
                acumulado4.Enabled = false;
                fechaenvio5.Enabled = false;
                fecharegreso5.Enabled = false;
                acumulado5.Enabled = false;
                fechaenvio6.Enabled = false;
                fecharegreso6.Enabled = false;
                acumulado6.Enabled = false;
                break;
            case "1":
                fechaenvio1.Enabled = true;

                fecharegreso1.Enabled = true;
                acmulado1.Enabled = true;
                fechaenvio2.Enabled = false;
                fecharegreso2.Enabled = false;
                acumulado2.Enabled = false;
                fechaenvio3.Enabled = false;
                fecharegreso3.Enabled = false;
                acumulado3.Enabled = false;
                fechaaenvio4.Enabled = false;
                fecharegreso4.Enabled = false;
                acumulado4.Enabled = false;
                fechaenvio5.Enabled = false;
                fecharegreso5.Enabled = false;
                acumulado5.Enabled = false;
                fechaenvio6.Enabled = false;
                fecharegreso6.Enabled = false;
                acumulado6.Enabled = false;
                break;
            case "2":
                fechaenvio1.Enabled = true;
                fecharegreso1.Enabled = true;
                acmulado1.Enabled = true;
                fechaenvio2.Enabled = true;
                fecharegreso2.Enabled = true;
                acumulado2.Enabled = true;
                fechaenvio3.Enabled = false;
                fecharegreso3.Enabled = false;
                acumulado3.Enabled = false;
                fechaaenvio4.Enabled = false;
                fecharegreso4.Enabled = false;
                acumulado4.Enabled = false;
                fechaenvio5.Enabled = false;
                fecharegreso5.Enabled = false;
                acumulado5.Enabled = false;
                fechaenvio6.Enabled = false;
                fecharegreso6.Enabled = false;
                acumulado6.Enabled = false;
                break;
            case "3":
                fechaenvio1.Enabled = true;
                fecharegreso1.Enabled = true;
                acmulado1.Enabled = true;
                fechaenvio2.Enabled = true;
                fecharegreso2.Enabled = true;
                acumulado2.Enabled = true;
                fechaenvio3.Enabled = true;
                fecharegreso3.Enabled = true;
                acumulado3.Enabled = true;
                fechaaenvio4.Enabled = false;
                fecharegreso4.Enabled = false;
                acumulado4.Enabled = false;
                fechaenvio5.Enabled = false;
                fecharegreso5.Enabled = false;
                acumulado5.Enabled = false;
                fechaenvio6.Enabled = false;
                fecharegreso6.Enabled = false;
                acumulado6.Enabled = false;
                break;
            case "4":
                fechaenvio1.Enabled = true;
                fecharegreso1.Enabled = true;
                acmulado1.Enabled = true;
                fechaenvio2.Enabled = true;
                fecharegreso2.Enabled = true;
                acumulado2.Enabled = true;
                fechaenvio3.Enabled = true;
                fecharegreso3.Enabled = true;
                acumulado3.Enabled = true;
                fechaaenvio4.Enabled = true;
                fecharegreso4.Enabled = true;
                acumulado4.Enabled = true;
                fechaenvio5.Enabled = false;
                fecharegreso5.Enabled = false;
                acumulado5.Enabled = false;
                fechaenvio6.Enabled = false;
                fecharegreso6.Enabled = false;
                acumulado6.Enabled = false;
                break;
            case "5":
                fechaenvio1.Enabled = true;
                fecharegreso1.Enabled = true;
                acmulado1.Enabled = true;
                fechaenvio2.Enabled = true;
                fecharegreso2.Enabled = true;
                acumulado2.Enabled = true;
                fechaenvio3.Enabled = true;
                fecharegreso3.Enabled = true;
                acumulado3.Enabled = true;
                fechaaenvio4.Enabled = true;
                fecharegreso4.Enabled = true;
                acumulado4.Enabled = true;
                fechaenvio5.Enabled = true;
                fecharegreso5.Enabled = true;
                acumulado5.Enabled = true;
                fechaenvio6.Enabled = false;
                fecharegreso6.Enabled = false;
                acumulado6.Enabled = false;
                break;

            case "6":
                fechaenvio1.Enabled = true;
                fecharegreso1.Enabled = true;
                acmulado1.Enabled = true;
                fechaenvio2.Enabled = true;
                fecharegreso2.Enabled = true;
                acumulado2.Enabled = true;
                fechaenvio3.Enabled = true;
                fecharegreso3.Enabled = true;
                acumulado3.Enabled = true;
                fechaaenvio4.Enabled = true;
                fecharegreso4.Enabled = true;
                acumulado4.Enabled = true;
                fechaenvio5.Enabled = true;
                fecharegreso5.Enabled = true;
                acumulado5.Enabled = true;
                fechaenvio6.Enabled = true;
                fecharegreso6.Enabled = true;
                acumulado6.Enabled = true;
                break;




        }
    }



    private void PartForm_Load(object sender, EventArgs args)
    {
        EpiUltraCombo group = (EpiUltraCombo)csm.GetNativeControlReference("460acbc9-46ec-4426-8b49-6575184752c6");
        EpiUltraCombo classid = (EpiUltraCombo)csm.GetNativeControlReference("62f97e78-853d-4ccf-b000-3bcdd08954b4");

        prestadopara.DropDownStyle = UltraComboStyle.DropDownList;
        seleccionfigura.DropDownStyle = UltraComboStyle.DropDownList;
        plantatxt.DropDownStyle = UltraComboStyle.DropDownList;
        combotipo.DropDownStyle = UltraComboStyle.DropDownList;
        group.DropDownStyle = UltraComboStyle.DropDownList;
        classid.DropDownStyle = UltraComboStyle.DropDownList;
        epiUltraComboC1.DropDownStyle = UltraComboStyle.DropDownList;
        epiUltraComboC2.DropDownStyle = UltraComboStyle.DropDownList;
        cbDepartamento.DropDownStyle = UltraComboStyle.DropDownList;
        epiUltraComboC3.DropDownStyle = UltraComboStyle.DropDownList;
    }


    ///////////////////////////////////////////////////////////////////////////////
    private void btnRefreshImg_Click(object sender, System.EventArgs args)
    {
        CargarImagenPart();
    }

    private bool EsExtensionImagen(string ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return false;

        string ext = Path.GetExtension(ruta.Trim()).ToLowerInvariant();
        switch (ext)
        {
            case ".jpg":
            case ".jpeg":
            case ".png":
            case ".bmp":
            case ".gif":
            case ".tif":
            case ".tiff":
            case ".webp":
                return true;
            default:
                return false;
        }
    }

    private string LeerRutaDesdeFilaBAQ(DataRow row, DataTable dt)
    {
        string ruta = "";

        if (dt.Columns.Contains("Calculated_RutaImagen"))
            ruta = Convert.ToString(row["Calculated_RutaImagen"]);
        else if (dt.Columns.Contains("RutaImagen"))
            ruta = Convert.ToString(row["RutaImagen"]);
        else if (dt.Columns.Contains("Part_XFileName"))
            ruta = Convert.ToString(row["Part_XFileName"]);
        else if (dt.Columns.Contains("XFileRef_XFileName"))
            ruta = Convert.ToString(row["XFileRef_XFileName"]);

        return (ruta ?? "").Trim();
    }

    private void CargarImagenPart()
    {
        try
        {
            if (this.edvPart == null || this.edvPart.Row < 0)
            {
                LimpiarImagenPart();
                return;
            }

            DataRow drPart = this.edvPart.dataView[this.edvPart.Row].Row;
            string partNum = Convert.ToString(drPart["PartNum"]).Trim();

            if (string.IsNullOrEmpty(partNum))
            {
                LimpiarImagenPart();
                return;
            }

            string ruta = ObtenerRutaImagenPorBAQ(partNum);

            if (string.IsNullOrEmpty(ruta) || !EsExtensionImagen(ruta) || !File.Exists(ruta))
            {
                LimpiarImagenPart();
                return;
            }

            byte[] bytes = File.ReadAllBytes(ruta);

            using (MemoryStream ms = new MemoryStream(bytes))
            using (Image imgTemp = Image.FromStream(ms))
            {
                Image nuevaImagen = new Bitmap(imgTemp);
                LimpiarImagenPart();
                pbImgPartFile.Image = nuevaImagen;
            }
        }
        catch
        {
            // PDF u otro formato no imagen: dejar el cuadro vacio sin mensaje
            LimpiarImagenPart();
        }
    }

    private string ObtenerRutaImagenPorBAQ(string partNum)
    {
        string ruta = "";

        DynamicQueryAdapter adapter = new DynamicQueryAdapter(this.oTrans);

        try
        {
            adapter.BOConnect();

            Ice.BO.QueryExecutionDataSet qeds = new Ice.BO.QueryExecutionDataSet();

            qeds.ExecutionParameter.AddExecutionParameterRow(
                "partNum",
                partNum,
                "nvarchar",
                false,
                Guid.Empty,
                "A"
            );
            

            adapter.ExecuteByID("CFX_PartImgPath", qeds);

            DataTable dt = adapter.QueryResults.Tables["Results"];

            if (dt != null && dt.Rows.Count > 0)
            {
                // Recorrer attachments (BAQ ordenado DESC) y tomar el primer archivo que sea imagen
                foreach (DataRow row in dt.Rows)
                {
                    string candidata = LeerRutaDesdeFilaBAQ(row, dt);
                    if (!EsExtensionImagen(candidata)) continue;
                    if (!File.Exists(candidata)) continue;
                    return candidata;
                }
            }
        }
        catch (Exception ex)
        {
            string msg = ex.Message ?? "";
            if (msg.IndexOf("CFX_PartImgPath", StringComparison.OrdinalIgnoreCase) >= 0
                && msg.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "";
            }

            MessageBox.Show("Error consultando BAQ CFX_PartImgPath:\n" + ex.Message);
        }
        finally
        {
            adapter.Dispose();
        }

        return ruta;
    }

    private void LimpiarImagenPart()
    {
        try
        {
            if (pbImgPartFile != null && pbImgPartFile.Image != null)
            {
                object viejaObj = pbImgPartFile.Image;
                pbImgPartFile.Image = null;
                Image viejaImg = viejaObj as Image;
                if (viejaImg != null)
                    viejaImg.Dispose();
            }
        }
        catch { }
    }
    ////////////////////////////////////////////////////////////////////////////////

    private void seleccionnum_ValueChanged(object sender, System.EventArgs args)
    {
        switch (seleccionnum.Text)
        {

            case "0":
                numregistro.Enabled = false;
                fechareg.Enabled = false;
                descripcioncambio.Enabled = false;
                responsablereg1.Enabled = false;
                placass.Enabled = false;
                stockk.Enabled = false;

                numreg2.Enabled = false;
                fecha2reg.Enabled = false;
                descripcionreg2.Enabled = false;
                responsablereg2.Enabled = false;
                placas2.Enabled = false;
                stock2.Enabled = false;




                numreg3.Enabled = false;
                fechareg3.Enabled = false;
                descreg3.Enabled = false;
                responsable3reg.Enabled = false;
                placa3reg.Enabled = false;
                stockreg3.Enabled = false;


                num4reg.Enabled = false;
                fechareg4.Enabled = false;
                descreg4.Enabled = false;
                responsablereg4.Enabled = false;
                placasreg4.Enabled = false;
                stock4reg.Enabled = false;


                numreg5.Enabled = false;
                fechareg5.Enabled = false;
                descreg5.Enabled = false;
                responsablereg5.Enabled = false;
                placasreg5.Enabled = false;
                stockreg5.Enabled = false;

                numreg6.Enabled = false;
                fechareg6.Enabled = false;
                descrreg6.Enabled = false;
                responsablereg6.Enabled = false;
                placareg6.Enabled = false;
                stockreg6.Enabled = false;


                numreg7.Enabled = false;
                fechareg7.Enabled = false;
                descripcionreg7.Enabled = false;
                responsablereg7.Enabled = false;
                placasreg7.Enabled = false;
                stockreg7.Enabled = false;


                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;

                rev101.Enabled = false;
                rev22.Enabled = false;
                rev33.Enabled = false;
                rev44.Enabled = false;
                rev55.Enabled = false;
                rev66.Enabled = false;
                rev77.Enabled = false;
                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;
                break;

            case "1":

                numregistro.Enabled = true;

                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;
                rev101.Enabled = true;

                numreg2.Enabled = false;
                fecha2reg.Enabled = false;
                descripcionreg2.Enabled = false;
                responsablereg2.Enabled = false;
                placas2.Enabled = false;
                stock2.Enabled = false;
                rev22.Enabled = false;



                numreg3.Enabled = false;
                fechareg3.Enabled = false;
                descreg3.Enabled = false;
                responsable3reg.Enabled = false;
                placa3reg.Enabled = false;
                stockreg3.Enabled = false;


                num4reg.Enabled = false;
                fechareg4.Enabled = false;
                descreg4.Enabled = false;
                responsablereg4.Enabled = false;
                placasreg4.Enabled = false;
                stock4reg.Enabled = false;


                numreg5.Enabled = false;
                fechareg5.Enabled = false;
                descreg5.Enabled = false;
                responsablereg5.Enabled = false;
                placasreg5.Enabled = false;
                stockreg5.Enabled = false;

                numreg6.Enabled = false;
                fechareg6.Enabled = false;
                descrreg6.Enabled = false;
                responsablereg6.Enabled = false;
                placareg6.Enabled = false;
                stockreg6.Enabled = false;


                numreg7.Enabled = false;
                fechareg7.Enabled = false;
                descripcionreg7.Enabled = false;
                responsablereg7.Enabled = false;
                placasreg7.Enabled = false;
                stockreg7.Enabled = false;


                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;
                rev33.Enabled = false;
                rev44.Enabled = false;
                rev55.Enabled = false;
                rev66.Enabled = false;
                rev77.Enabled = false;
                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;


                break;
            case "2":
                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;
                rev101.Enabled = true;

                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;
                rev22.Enabled = true;


                numreg3.Enabled = false;
                fechareg3.Enabled = false;
                descreg3.Enabled = false;
                responsable3reg.Enabled = false;
                placa3reg.Enabled = false;
                stockreg3.Enabled = false;


                num4reg.Enabled = false;
                fechareg4.Enabled = false;
                descreg4.Enabled = false;
                responsablereg4.Enabled = false;
                placasreg4.Enabled = false;
                stock4reg.Enabled = false;


                numreg5.Enabled = false;
                fechareg5.Enabled = false;
                descreg5.Enabled = false;
                responsablereg5.Enabled = false;
                placasreg5.Enabled = false;
                stockreg5.Enabled = false;

                numreg6.Enabled = false;
                fechareg6.Enabled = false;
                descrreg6.Enabled = false;
                responsablereg6.Enabled = false;
                placareg6.Enabled = false;
                stockreg6.Enabled = false;


                numreg7.Enabled = false;
                fechareg7.Enabled = false;
                descripcionreg7.Enabled = false;
                responsablereg7.Enabled = false;
                placasreg7.Enabled = false;
                stockreg7.Enabled = false;


                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;

                rev33.Enabled = false;
                rev44.Enabled = false;
                rev55.Enabled = false;
                rev66.Enabled = false;
                rev77.Enabled = false;
                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;

                break;
            case "3":
                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;
                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;


                num4reg.Enabled = false;
                fechareg4.Enabled = false;
                descreg4.Enabled = false;
                responsablereg4.Enabled = false;
                placasreg4.Enabled = false;
                stock4reg.Enabled = false;


                numreg5.Enabled = false;
                fechareg5.Enabled = false;
                descreg5.Enabled = false;
                responsablereg5.Enabled = false;
                placasreg5.Enabled = false;
                stockreg5.Enabled = false;

                numreg6.Enabled = false;
                fechareg6.Enabled = false;
                descrreg6.Enabled = false;
                responsablereg6.Enabled = false;
                placareg6.Enabled = false;
                stockreg6.Enabled = false;


                numreg7.Enabled = false;
                fechareg7.Enabled = false;
                descripcionreg7.Enabled = false;
                responsablereg7.Enabled = false;
                placasreg7.Enabled = false;
                stockreg7.Enabled = false;


                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;
                rev44.Enabled = false;
                rev55.Enabled = false;
                rev66.Enabled = false;
                rev77.Enabled = false;
                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;

                break;
            case "4":
                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;


                num4reg.Enabled = true;
                fechareg4.Enabled = true;
                descreg4.Enabled = true;
                responsablereg4.Enabled = true;
                placasreg4.Enabled = true;
                stock4reg.Enabled = true;

                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;
                rev44.Enabled = true;

                numreg5.Enabled = false;
                fechareg5.Enabled = false;
                descreg5.Enabled = false;
                responsablereg5.Enabled = false;
                placasreg5.Enabled = false;
                stockreg5.Enabled = false;

                numreg6.Enabled = false;
                fechareg6.Enabled = false;
                descrreg6.Enabled = false;
                responsablereg6.Enabled = false;
                placareg6.Enabled = false;
                stockreg6.Enabled = false;


                numreg7.Enabled = false;
                fechareg7.Enabled = false;
                descripcionreg7.Enabled = false;
                responsablereg7.Enabled = false;
                placasreg7.Enabled = false;
                stockreg7.Enabled = false;


                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;


                rev55.Enabled = false;
                rev66.Enabled = false;
                rev77.Enabled = false;
                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;

                break;
            case "5":

                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;


                num4reg.Enabled = true;
                fechareg4.Enabled = true;
                descreg4.Enabled = true;
                responsablereg4.Enabled = true;
                placasreg4.Enabled = true;
                stock4reg.Enabled = true;




                numreg5.Enabled = true;
                fechareg5.Enabled = true;
                descreg5.Enabled = true;
                responsablereg5.Enabled = true;
                placasreg5.Enabled = true;
                stockreg5.Enabled = true;

                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;
                rev44.Enabled = true;
                rev55.Enabled = true;

                numreg6.Enabled = false;
                fechareg6.Enabled = false;
                descrreg6.Enabled = false;
                responsablereg6.Enabled = false;
                placareg6.Enabled = false;
                stockreg6.Enabled = false;


                numreg7.Enabled = false;
                fechareg7.Enabled = false;
                descripcionreg7.Enabled = false;
                responsablereg7.Enabled = false;
                placasreg7.Enabled = false;
                stockreg7.Enabled = false;


                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;



                rev66.Enabled = false;
                rev77.Enabled = false;
                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;

                break;

            case "6":
                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;


                num4reg.Enabled = true;
                fechareg4.Enabled = true;
                descreg4.Enabled = true;
                responsablereg4.Enabled = true;
                placasreg4.Enabled = true;
                stock4reg.Enabled = true;


                numreg5.Enabled = true;
                fechareg5.Enabled = true;
                descreg5.Enabled = true;
                responsablereg5.Enabled = true;
                placasreg5.Enabled = true;
                stockreg5.Enabled = true;

                numreg6.Enabled = true;
                fechareg6.Enabled = true;
                descrreg6.Enabled = true;
                responsablereg6.Enabled = true;
                placareg6.Enabled = true;
                stockreg6.Enabled = true;


                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;
                rev44.Enabled = true;
                rev55.Enabled = true;
                rev66.Enabled = true;


                numreg7.Enabled = false;
                fechareg7.Enabled = false;
                descripcionreg7.Enabled = false;
                responsablereg7.Enabled = false;
                placasreg7.Enabled = false;
                stockreg7.Enabled = false;


                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;


                rev77.Enabled = false;
                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;

                break;

            case "7":


                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;


                num4reg.Enabled = true;
                fechareg4.Enabled = true;
                descreg4.Enabled = true;
                responsablereg4.Enabled = true;
                placasreg4.Enabled = true;
                stock4reg.Enabled = true;


                numreg5.Enabled = true;
                fechareg5.Enabled = true;
                descreg5.Enabled = true;
                responsablereg5.Enabled = true;
                placasreg5.Enabled = true;
                stockreg5.Enabled = true;

                numreg6.Enabled = true;
                fechareg6.Enabled = true;
                descrreg6.Enabled = true;
                responsablereg6.Enabled = true;
                placareg6.Enabled = true;
                stockreg6.Enabled = true;


                numreg7.Enabled = true;
                fechareg7.Enabled = true;
                descripcionreg7.Enabled = true;
                responsablereg7.Enabled = true;
                placasreg7.Enabled = true;
                stockreg7.Enabled = true;

                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;
                rev44.Enabled = true;
                rev55.Enabled = true;
                rev66.Enabled = true;
                rev77.Enabled = true;





                numreg8.Enabled = false;
                fechareg8.Enabled = false;
                descripcionreg8.Enabled = false;
                responsablereg8.Enabled = false;
                placasreg8.Enabled = false;
                stockreg8.Enabled = false;



                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;


                rev88.Enabled = false;
                rev99.Enabled = false;
                rev100.Enabled = false;




                break;
            case "8":

                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;


                num4reg.Enabled = true;
                fechareg4.Enabled = true;
                descreg4.Enabled = true;
                responsablereg4.Enabled = true;
                placasreg4.Enabled = true;
                stock4reg.Enabled = true;


                numreg5.Enabled = true;
                fechareg5.Enabled = true;
                descreg5.Enabled = true;
                responsablereg5.Enabled = true;
                placasreg5.Enabled = true;
                stockreg5.Enabled = true;

                numreg6.Enabled = true;
                fechareg6.Enabled = true;
                descrreg6.Enabled = true;
                responsablereg6.Enabled = true;
                placareg6.Enabled = true;
                stockreg6.Enabled = true;


                numreg7.Enabled = true;
                fechareg7.Enabled = true;
                descripcionreg7.Enabled = true;
                responsablereg7.Enabled = true;
                placasreg7.Enabled = true;
                stockreg7.Enabled = true;

                numreg8.Enabled = true;
                fechareg8.Enabled = true;
                descripcionreg8.Enabled = true;
                responsablereg8.Enabled = true;
                placasreg8.Enabled = true;
                stockreg8.Enabled = true;

                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;
                rev44.Enabled = true;
                rev55.Enabled = true;
                rev66.Enabled = true;
                rev77.Enabled = true;
                rev88.Enabled = true;




                numreg9.Enabled = false;
                fechareg9.Enabled = false;
                desreg9.Enabled = false;
                responsablereg9.Enabled = false;
                placasreg9.Enabled = false;
                stockreg9.Enabled = false;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;

                rev99.Enabled = false;
                rev100.Enabled = false;



                break;
            case "9":
                numregistro.Enabled = true;
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;


                num4reg.Enabled = true;
                fechareg4.Enabled = true;
                descreg4.Enabled = true;
                responsablereg4.Enabled = true;
                placasreg4.Enabled = true;
                stock4reg.Enabled = true;


                numreg5.Enabled = true;
                fechareg5.Enabled = true;
                descreg5.Enabled = true;
                responsablereg5.Enabled = true;
                placasreg5.Enabled = true;
                stockreg5.Enabled = true;

                numreg6.Enabled = true;
                fechareg6.Enabled = true;
                descrreg6.Enabled = true;
                responsablereg6.Enabled = true;
                placareg6.Enabled = true;
                stockreg6.Enabled = true;


                numreg7.Enabled = true;
                fechareg7.Enabled = true;
                descripcionreg7.Enabled = true;
                responsablereg7.Enabled = true;
                placasreg7.Enabled = true;
                stockreg7.Enabled = true;

                numreg8.Enabled = true;
                fechareg8.Enabled = true;
                descripcionreg8.Enabled = true;
                responsablereg8.Enabled = true;
                placasreg8.Enabled = true;
                stockreg8.Enabled = true;


                numreg9.Enabled = true;
                fechareg9.Enabled = true;
                desreg9.Enabled = true;
                responsablereg9.Enabled = true;
                placasreg9.Enabled = true;
                stockreg9.Enabled = true;


                numreg10.Enabled = false;
                feachareg10.Enabled = false;
                descrpreg10.Enabled = false;
                responsablereg10.Enabled = false;
                placasre10.Enabled = false;
                stockreg10.Enabled = false;
                rev100.Enabled = false;

                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;
                rev44.Enabled = true;
                rev55.Enabled = true;
                rev66.Enabled = true;
                rev77.Enabled = true;
                rev88.Enabled = true;
                rev99.Enabled = true;


                break;
            case "10":
                fechareg.Enabled = true;
                descripcioncambio.Enabled = true;
                responsablereg1.Enabled = true;
                placass.Enabled = true;
                stockk.Enabled = true;


                numreg2.Enabled = true;
                fecha2reg.Enabled = true;
                descripcionreg2.Enabled = true;
                responsablereg2.Enabled = true;
                placas2.Enabled = true;
                stock2.Enabled = true;


                numreg3.Enabled = true;
                fechareg3.Enabled = true;
                descreg3.Enabled = true;
                responsable3reg.Enabled = true;
                placa3reg.Enabled = true;
                stockreg3.Enabled = true;


                num4reg.Enabled = true;
                fechareg4.Enabled = true;
                descreg4.Enabled = true;
                responsablereg4.Enabled = true;
                placasreg4.Enabled = true;
                stock4reg.Enabled = true;


                numreg5.Enabled = true;
                fechareg5.Enabled = true;
                descreg5.Enabled = true;
                responsablereg5.Enabled = true;
                placasreg5.Enabled = true;
                stockreg5.Enabled = true;

                numreg6.Enabled = true;
                fechareg6.Enabled = true;
                descrreg6.Enabled = true;
                responsablereg6.Enabled = true;
                placareg6.Enabled = true;
                stockreg6.Enabled = true;


                numreg7.Enabled = true;
                fechareg7.Enabled = true;
                descripcionreg7.Enabled = true;
                responsablereg7.Enabled = true;
                placasreg7.Enabled = true;
                stockreg7.Enabled = true;

                numreg8.Enabled = true;
                fechareg8.Enabled = true;
                descripcionreg8.Enabled = true;
                responsablereg8.Enabled = true;
                placasreg8.Enabled = true;
                stockreg8.Enabled = true;


                numreg9.Enabled = true;
                fechareg9.Enabled = true;
                desreg9.Enabled = true;
                responsablereg9.Enabled = true;
                placasreg9.Enabled = true;
                stockreg9.Enabled = true;


                numreg10.Enabled = true;
                feachareg10.Enabled = true;
                descrpreg10.Enabled = true;
                responsablereg10.Enabled = true;
                placasre10.Enabled = true;
                stockreg10.Enabled = true;


                rev101.Enabled = true;
                rev22.Enabled = true;
                rev33.Enabled = true;
                rev44.Enabled = true;
                rev55.Enabled = true;
                rev66.Enabled = true;
                rev77.Enabled = true;
                rev88.Enabled = true;
                rev99.Enabled = true;
                rev100.Enabled = true;
                break;

        }


    }
}


//////////////////////////////ASESOR DE PARTES/////////////////////////////////////////////////

// **************************************************
// Custom code for PartAdvisorForm
// Created: 08/11/2025 09:22:24 a. m.
// **************************************************

extern alias Erp_Contracts_BO_PartAdvisor;
extern alias Erp_Contracts_BO_Part;

using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using Ice.UI;
using Ice.Lib;
using Ice.Adapters;
using Ice.Lib.Customization;
using Ice.Lib.ExtendedProps;
using Ice.Lib.Framework;
using Ice.Lib.Searches;
using Ice.UI.FormFunctions;
using System.IO;
using System.Drawing;
using Ice.BO;
using Ice.Tablesets;
using Epicor.ServiceModel;
using System.Collections;

public class Script
{
    // ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **
    // Begin Wizard Added Module Level Variables **

    private EpiDataView edvPartView;
    // End Wizard Added Module Level Variables **

    // Add Custom Module Level Variables Here **
    private ArrayList soldItViews;
    private Hashtable orderUserCache;

    public void InitializeCustomCode()
    {
        // ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
        // Begin Wizard Added Variable Initialization

        this.edvPartView = ((EpiDataView)(this.oTrans.EpiDataViews["PartView"]));
        this.edvPartView.EpiViewNotification += new EpiViewNotification(this.edvPartView_EpiViewNotification);
        this.baseToolbarsManager.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
        // End Wizard Added Variable Initialization

        // Begin Wizard Added Custom Method Calls
        this.soldItViews = new ArrayList();
        this.orderUserCache = new Hashtable();
        HookSoldItViews();
        FillSoldItUsersAll();
        // End Wizard Added Custom Method Calls
    }

    public void DestroyCustomCode()
    {
        // ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
        // Begin Wizard Added Object Disposal

        this.edvPartView.EpiViewNotification -= new EpiViewNotification(this.edvPartView_EpiViewNotification);
        this.baseToolbarsManager.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
        UnhookSoldItViews();
        this.edvPartView = null;
        // End Wizard Added Object Disposal

        // Begin Custom Code Disposal
        this.soldItViews = null;
        this.orderUserCache = null;
        // End Custom Code Disposal
    }

    public void BUSQUEDAINFOUL(string parte)
    {
        string ulmaterial = "";
        string ulno = "";

        DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
        adapterDynamicQuery.BOConnect();
        string BAQName = "BAQPARTADVISOR";
        QueryExecutionDataSet ds = new QueryExecutionDataSet();

        ds.ExecutionParameter.AddExecutionParameterRow("PARTE", parte, "nvarchar", false, Guid.Empty, "A");
        adapterDynamicQuery.ExecuteByID(BAQName, ds);

        if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
        {
            ulmaterial = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
            ulno = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString();

            if (ulmaterial != "" || ulno != "")
            {
                ulmaterialadvisor.Text = ulmaterial;
                ulfileadvisor.Text = ulno;
            }
            else
            {
                ulmaterialadvisor.Text = " ";
                ulfileadvisor.Text = " ";
            }
        }

        adapterDynamicQuery.Dispose();
    }

    private void HookSoldItViews()
    {
        foreach (DictionaryEntry de in this.oTrans.EpiDataViews)
        {
            EpiDataView v = de.Value as EpiDataView;
            if (v == null || v.dataView == null || v.dataView.Table == null) continue;
            if (!v.dataView.Table.Columns.Contains("OrderNum")) continue;

            if (!this.soldItViews.Contains(v))
            {
                v.EpiViewNotification += new EpiViewNotification(this.soldItView_EpiViewNotification);
                this.soldItViews.Add(v);
            }
        }
    }

    private void UnhookSoldItViews()
    {
        if (this.soldItViews == null) return;

        foreach (object obj in this.soldItViews)
        {
            EpiDataView v = obj as EpiDataView;
            if (v != null)
                v.EpiViewNotification -= new EpiViewNotification(this.soldItView_EpiViewNotification);
        }
    }

    private void soldItView_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
    {
        if (args.NotifyType == EpiTransaction.NotifyType.Initialize ||
            args.NotifyType == EpiTransaction.NotifyType.InitAndResetTreeNodes ||
            args.NotifyType == EpiTransaction.NotifyType.AddRow)
        {
            FillSoldItUsers(view);
        }
    }

    private string GetOrderEntryPerson(int orderNum)
    {
        if (orderNum <= 0) return "";

        if (this.orderUserCache.ContainsKey(orderNum))
            return Convert.ToString(this.orderUserCache[orderNum]);

        string entryPerson = "";

        DynamicQueryAdapter dqa = new DynamicQueryAdapter(this.oTrans);
        try
        {
            dqa.BOConnect();

            QueryExecutionDataSet qeds = new QueryExecutionDataSet();
            qeds.ExecutionParameter.AddExecutionParameterRow(
                "ORDERNUM",
                orderNum.ToString(),
                "int",
                false,
                Guid.Empty,
                "A"
            );

            dqa.ExecuteByID("BAQ_ORDERENTRYPERSON", qeds);

            if (dqa.QueryResults != null &&
                dqa.QueryResults.Tables.Contains("Results") &&
                dqa.QueryResults.Tables["Results"].Rows.Count > 0)
            {
                // Si en tu BAQ el alias sale distinto, cambia este nombre de columna:
                entryPerson = Convert.ToString(dqa.QueryResults.Tables["Results"].Rows[0]["OrderHed_EntryPerson"]);
            }
        }
        catch
        {
            entryPerson = "";
        }
        finally
        {
            dqa.Dispose();
        }

        this.orderUserCache[orderNum] = entryPerson;
        return entryPerson;
    }

    private void FillSoldItUsers(EpiDataView view)
    {
        if (view == null || view.dataView == null || view.dataView.Table == null) return;

        DataTable t = view.dataView.Table;
        if (!t.Columns.Contains("OrderNum")) return;

        if (!t.Columns.Contains("UsuarioSoldIt"))
        {
            DataColumn c = new DataColumn("UsuarioSoldIt", typeof(string));
            c.Caption = "Usuario";
            t.Columns.Add(c);
        }

        foreach (DataRow r in t.Rows)
        {
            int orderNum = 0;
            try { orderNum = Convert.ToInt32(r["OrderNum"]); } catch { orderNum = 0; }

            r["UsuarioSoldIt"] = GetOrderEntryPerson(orderNum);
        }
    }

    private void FillSoldItUsersAll()
    {
        if (this.soldItViews == null) return;

        foreach (object obj in this.soldItViews)
        {
            EpiDataView v = obj as EpiDataView;
            if (v != null) FillSoldItUsers(v);
        }
    }



    private void edvPartView_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
    {
        // ** Argument Properties and Uses **
        // view.dataView[args.Row]["FieldName"]
        // args.Row, args.Column, args.Sender, args.NotifyType
        // NotifyType.Initialize, NotifyType.AddRow, NotifyType.DeleteRow, NotifyType.InitLastView, NotifyType.InitAndResetTreeNodes
        if (args.NotifyType == EpiTransaction.NotifyType.Initialize && args.Row > -1)
        {
    
            try
            {
                string parte = edvPartView.dataView[edvPartView.Row]["PartNum"].ToString();
                BUSQUEDAINFOUL(parte);

                if (this.orderUserCache != null) this.orderUserCache.Clear();
                HookSoldItViews();
                FillSoldItUsersAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error: " + ex.Message);
            }
        }
    }

    private void baseToolbarsManager_ToolClick(object sender, Infragistics.Win.UltraWinToolbars.ToolClickEventArgs args)
    {
        switch (args.Tool.Key)
        {
            case "ClearTool":
                ulmaterialadvisor.Text = string.Empty;
                ulfileadvisor.Text = string.Empty;
                if (this.orderUserCache != null) this.orderUserCache.Clear();
                break;

            case "RefreshTool":
                if (this.orderUserCache != null) this.orderUserCache.Clear();
                HookSoldItViews();
                FillSoldItUsersAll();
                break;
        }
    }
}