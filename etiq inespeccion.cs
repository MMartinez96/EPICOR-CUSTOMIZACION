// **************************************************
// Custom code for BAQReportForm
// Created: 20/02/2024 04:11:56 p. m.
// **************************************************
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
using System.IO;
using System.Drawing;

public class Script
{
public event EventHandler Closed;
EpiTextBox frm1;
EpiTextBox frm2;
EpiNumericEditor frm3;
 EpiDataView warehouses ;  
EpiUltraCombo selectimpresion;

		private EpiDataView edvReportParam;


	public static DialogResult InputBox(string title, string promptText, ref string value)
{
   Form form = new Form();
Label label = new Label();
TextBox textBox = new TextBox();
Button buttonOk = new Button();
//Button buttonCancel = new Button();
form.Text = title;
label.Text = promptText;
buttonOk.Text = "OK";
//buttonCancel.Text = "Cancel";
buttonOk.DialogResult = DialogResult.OK;
//buttonCancel.DialogResult = DialogResult.Cancel;
label.SetBounds(85, 30, 400, 13);
textBox.SetBounds(85, 55, 85, 20);
textBox.UseSystemPasswordChar = true;
buttonOk.SetBounds(85, 90, 67, 60);
//buttonCancel.SetBounds(400, 160, 160, 60);
form.Controls.AddRange(new Control[] { label, textBox, buttonOk });
form.AcceptButton = buttonOk;
//form.CancelButton = buttonCancel;
DialogResult dialogResult = form.ShowDialog();
value = textBox.Text;
return dialogResult; 


}  

	public void InitializeCustomCode()
	{
			this.edvReportParam=((EpiDataView)(this.oTrans.EpiDataViews["ReportParam"]));
	this.edvReportParam.EpiViewNotification += new EpiViewNotification(this.edvReportParam_EpiViewNotification);
	this.BAQReportForm.Closing += new System.ComponentModel.CancelEventHandler(this.BAQReportForm_Closing);
					frm1 = ((EpiTextBox)csm.GetNativeControlReference("fabc6621-06b7-43ce-b7b8-44eba68588af"));  
					frm2 = ((EpiTextBox)csm.GetNativeControlReference("27e27b9a-a27a-48ca-821d-19f194488412")); 
					frm3 = ((EpiNumericEditor)csm.GetNativeControlReference("feaa6bf6-a7c6-4ae5-a675-f2f64b1d1fe4")); 

					frm1.Enabled=false;
					frm2.Enabled=false; 
					frm3.Enabled=false; 

	string value = "";
	
	if (InputBox("CODIGO", "ESCANEE SU ACCESO", ref value) == DialogResult.OK)
{
consultaempleado(value);
}else{
salir();

}
	}

	public void DestroyCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
		// Begin Wizard Added Object Disposal

		this.edvReportParam.EpiViewNotification -= new EpiViewNotification(this.edvReportParam_EpiViewNotification);
		this.edvReportParam = null;
		this.BAQReportForm.Closing -= new System.ComponentModel.CancelEventHandler(this.BAQReportForm_Closing);
		// End Wizard Added Object Disposal

		// Begin Custom Code Disposal

		// End Custom Code Disposal
	}
public void salir(){
if ((edvReportParam.Row > -1))
			{ 
	Timer= new Timer();
			Timer.Interval=1000;
			Timer.Tick += new EventHandler(timer_tick);

BAQReportForm.Hide();
}
}

public void searchimage(string nombre){

	
string existe="";
	 string ruta="G";
			string ext=".JPG";
	  DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
            adapterDynamicQuery.BOConnect();
            string BAQName = "BAQ_SEARCHIMAGE";
            Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
            ds.ExecutionParameter.AddExecutionParameterRow("CODIGO", nombre  , "nvarchar", false, Guid.Empty, "A");
            adapterDynamicQuery.ExecuteByID(BAQName, ds);
        
            if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0){
					existe= adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][2].ToString(); 
			
				pictureBox1.Image = new Bitmap(@"\\erp.erp11codi.local\\EMPLEADOS\\CUU\\"+existe+ext);	 
				
					
				//edvReportParam.dataView[edvReportParam.Row]["Character04"]=parte;
 				adapterDynamicQuery.Dispose();

				}else{}



}

public void searchJOBHEAD(string job){

selectimpresion = ((EpiUltraCombo)csm.GetNativeControlReference("135850df-39e3-4022-93a3-edeff913a646")); 

string jobnum =edvReportParam.dataView[edvReportParam.Row]["field1"].ToString();
	//EpiLabel clientelbl = ((EpiLabel)csm.GetNativeControlReference("c8a0ee4b-1cdb-4834-a565-cce1baede3a8"));  
				
           // frm2 = ((EpiTextBox)csm.GetNativeControlReference("27e27b9a-a27a-48ca-821d-19f194488412"));
string cliente="";
	string parte=""; 
	  DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
            adapterDynamicQuery.BOConnect();
            string BAQName = "BAQ_JOBHEAD";
            Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
            ds.ExecutionParameter.AddExecutionParameterRow("JOBNUM", job  , "nvarchar", false, Guid.Empty, "A");
            adapterDynamicQuery.ExecuteByID(BAQName, ds);
        
            if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0){
					parte= adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString(); 
					 cliente = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString();
				//clientelbl.Text=cliente;
				if(cliente=="GECKO"|| cliente=="LIGHTOLIER" || cliente=="COPELAND" || cliente=="EMERSON GUADALAJARA" || cliente=="MILWUAKEE"|| cliente=="WHITE RODGERS" || cliente=="EMERSON" || cliente=="EMERSON WHITE RODGERS" || cliente=="EMERSON MTY" || cliente=="COOPER LIGTHING" || cliente=="SIGNIFY" || cliente=="SIGNIFY MONTERREY" || cliente=="SIGNIFY CANADA"){
						edvReportParam.dataView[edvReportParam.Row]["ReportStyleNum"]=1001;
					}
					else{
					edvReportParam.dataView[edvReportParam.Row]["ReportStyleNum"]=1;
						}
 				adapterDynamicQuery.Dispose();
					}

					}


public void salir(){
	


	
}

public void consultaempleado(string code){   
            
            frm1 = ((EpiTextBox)csm.GetNativeControlReference("fabc6621-06b7-43ce-b7b8-44eba68588af"));  
            frm2 = ((EpiTextBox)csm.GetNativeControlReference("27e27b9a-a27a-48ca-821d-19f194488412")); 
            frm3 = ((EpiNumericEditor)csm.GetNativeControlReference("feaa6bf6-a7c6-4ae5-a675-f2f64b1d1fe4"));   
                        string existe="";
                        string city="";              
					 	string name="";
							string consultacodigo="";
string message = "USUARIO INEXISTENTE";
					string title = "ACCESO NO AUTORIZADO";   
		if ((edvReportParam.Row > -1))
			{  
                                        try
                                            {               
            DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
            adapterDynamicQuery.BOConnect();
            string BAQName = "BAQ_EMPLEADO";
            Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
            ds.ExecutionParameter.AddExecutionParameterRow("IDEMP", code  , "nvarchar", false, Guid.Empty, "A");
            adapterDynamicQuery.ExecuteByID(BAQName, ds);
        
            if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0){
					consultacodigo= adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][2].ToString(); 
					 city = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString();
                existe = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();          
				 name = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][3].ToString(); 
                if (city=="Chihuahua" ||city=="chihuahua" || city=="CHIHUAHUA") 
                {
                 if(code == consultacodigo){ 
                    frm1.Enabled=true;
                    //frm2.Enabled=true; 
                   edvReportParam.dataView[edvReportParam.Row]["Character04"]=existe;
					frm2.Enabled=false; 
					labeluser1.Text=name;
                    frm3.Enabled=true; 
					searchimage(code);
			Timer= new Timer();
			Timer.Interval=1000;
			Timer.Tick += new EventHandler(timer_tick);
			Timer.Start();
			tempolbl.Text="LA SESIÒN CADUCA EN : ";
			label9.Text = Counter.ToString();			
			

                  } else{
				
					MessageBox.Show(title,message);

                    frm1.Enabled=false;
                    
					frm2.Enabled=false; 
					
                    frm3.Enabled=false; }  
                }else {
			MessageBox.Show("CLAVE EQUIVOCADO", "ACCESO DENEGADO");

                    frm1.Enabled=false;
					
                    frm2.Enabled=false;
					
                    frm3.Enabled=false; 
					
                }
                  

    
               adapterDynamicQuery.Dispose();}
				else{
			MessageBox.Show("USUARIO NO EXISTE","ACCESO DENEGADO" );
				Timer= new Timer();
			Timer.Interval=1000;
			Timer.Tick += new EventHandler(timer_tick);
			//Timer.Start();
			//tempolbl.Text="TIEMPO RESTANTE";
			//label9.Text = Counter.ToString();
                 
}
                        
        } catch (System.Exception ex)
        {
            ExceptionBox.Show(ex);  
        }}
            }


Timer Timer;
//////////////////////CONTADOR DE SEGUNDOS AQUI////////////////////////////////7
int Counter=90;
///////////////////////////////////////////////////////////////////////////////77


	private void BAQReportForm_Load(object sender, EventArgs args)
	{

			
	}





		private void timer_tick(object sender, EventArgs e){
if ((edvReportParam.Row > -1))
			{
			
			 frm1 = ((EpiTextBox)csm.GetNativeControlReference("fabc6621-06b7-43ce-b7b8-44eba68588af"));  
            frm2 = ((EpiTextBox)csm.GetNativeControlReference("27e27b9a-a27a-48ca-821d-19f194488412")); 
            frm3 = ((EpiNumericEditor)csm.GetNativeControlReference("feaa6bf6-a7c6-4ae5-a675-f2f64b1d1fe4"));
			Counter --;
			if (Counter ==0 ){
    		Timer.Stop();
  	       frm1.Enabled=false;
             frm2.Enabled=false; 
             frm3.Enabled=false; 
				frm1.Text=""; frm2.Text="";  frm3.Text="";
			}
		label9.Text= Counter.ToString();
			}
}
       

	private void edvReportParam_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		// ** Argument Properties and Uses **
		// view.dataView[args.Row]["FieldName"]
		// args.Row, args.Column, args.Sender, args.NotifyType
		// NotifyType.Initialize, NotifyType.AddRow, NotifyType.DeleteRow, NotifyType.InitLastView, NotifyType.InitAndResetTreeNodes
		if ((args.NotifyType == EpiTransaction.NotifyType.Initialize))
		{
			if ((edvReportParam.Row > -1))
			{
				//frm1 = ((EpiTextBox)csm.GetNativeControlReference("fabc6621-06b7-43ce-b7b8-44eba68588af")); 
		string jobnum =edvReportParam.dataView[edvReportParam.Row]["field1"].ToString();

selectimpresion = ((EpiUltraCombo)csm.GetNativeControlReference("135850df-39e3-4022-93a3-edeff913a646"));

			searchJOBHEAD(jobnum);
			}
		}
	}


	private void BAQReportForm_Closing(object sender, System.ComponentModel.CancelEventArgs args)
	{
	if ((edvReportParam.Row > -1))
			{
				if (Timer.Enabled=true)
					{
					args.Cancel=false;
					Timer.Stop();
					BAQReportForm.Activate();	 
					
}					

        // Then assume that X has been clicked and act accordingly.
	}
}}

