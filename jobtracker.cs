// **************************************************
// Custom code for JobTrackerForm — Serializado PT (consulta rollos recibidos)
// Botón Serializado epguid: 6ab3296f-7d31-481e-869f-ccd299ba1d1c
// Datos: JobHead_UD (rollos) + Ice.UD37 (confirmación PT Producto Terminado)
// BAQs: BAQ-SERIAL-JOBHEAD, BAQ-PT-SERIAL-UD37, CODIFLEX-UD10
// v2: carga diferida JobHead + caché BAQ por sesión (abre Job Tracker más rápido)
// v2.2: columna Fecha recibido PT en consulta rollos (Ice.UD37 Date01)
// **************************************************

extern alias Erp_Contracts_BO_JobEntry;
extern alias Erp_Contracts_BO_JobProdSearch;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
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
using Ice.Lib.Broadcast;

public class Script
{
	// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **
	// Begin Wizard Added Module Level Variables **

	private EpiDataView edvJobHead;
	// End Wizard Added Module Level Variables **

	// Add Custom Module Level Variables Here **
	EpiTextBox txtPart;
	EpiTextBox txtMtlPart;
	EpiUltraCombo cbProdGroup;
	Ice.Core.Session session;
	string QR;
	string materialQR;
	string userID;

	EpiUltraGrid dgLaborDtl;

	private const string BtnSerializadoEpGuid = "6ab3296f-7d31-481e-869f-ccd299ba1d1c";
	private Control _btnSerializado;

	private string _pendingDeferredJobKey;

	public void InitializeCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Variable Initialization' lines **
		// Begin Wizard Added Variable Initialization

		this.baseToolbarsManager.ToolClick += new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);
		this.edvJobHead = ((EpiDataView)(this.oTrans.EpiDataViews["JobHead"]));
		this.edvJobHead.EpiViewNotification += new EpiViewNotification(this.edvJobHead_EpiViewNotification);
		// End Wizard Added Variable Initialization

		// Begin Wizard Added Custom Method Calls


		// End Wizard Added Custom Method Calls
		session = (Ice.Core.Session)oTrans.Session;
		cbProdGroup = ((EpiUltraCombo)csm.GetNativeControlReference("6f23ca2c-b9de-4b69-9e5b-77f770bb2562"));
		txtPart = ((EpiTextBox)csm.GetNativeControlReference("543b58f3-576d-4131-badf-b9f7e3d446c5"));
		txtMtlPart = ((EpiTextBox)csm.GetNativeControlReference("c75de9d3-8ea7-4b05-b08b-8eed8d7c4796"));
		txtPart.TextChanged += new EventHandler(this.txtPart_TextChanged);
		txtMtlPart.TextChanged += new EventHandler(this.txtMtlPart_TextChanged);
		QR = "";
		materialQR = "";
		userID = session.UserID;

		dgLaborDtl = ((EpiUltraGrid)csm.GetNativeControlReference("f45f4532-176c-4a87-8459-4cf95e66fd38"));
		this.dgLaborDtl.InitializeRow += new Infragistics.Win.UltraWinGrid.InitializeRowEventHandler(this.dgLaborDtl_InitializeRow);

		WireSerializadoButton();
	}

	private void WireSerializadoButton()
	{
		try
		{
			_btnSerializado = csm.GetNativeControlReference(BtnSerializadoEpGuid) as Control;
			if (_btnSerializado != null)
			{
				_btnSerializado.Click -= BtnSerializado_Click;
				_btnSerializado.Click += BtnSerializado_Click;
			}
		}
		catch { }
	}

	private void BtnSerializado_Click(object sender, EventArgs e)
	{
		try
		{
			if (edvJobHead == null || edvJobHead.Row < 0)
			{
				MessageBox.Show("Seleccione un Job.", "Serializado PT", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string jobNum = Convert.ToString(edvJobHead.dataView[edvJobHead.Row]["JobNum"]).Trim();
			string partNum = Convert.ToString(edvJobHead.dataView[edvJobHead.Row]["PartNum"]).Trim();

			if (string.IsNullOrWhiteSpace(jobNum))
			{
				MessageBox.Show("No hay Job seleccionado.", "Serializado PT", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			using (JobTrackerSerializadoForm dlg = new JobTrackerSerializadoForm(oTrans, jobNum, partNum))
			{
				dlg.ShowDialog(this.JobTrackerForm);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error al abrir consulta de serializado:\n" + ex.Message, "Serializado PT",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	public void DestroyCustomCode()
	{
		// ** Wizard Insert Location - Do not delete 'Begin/End Wizard Added Object Disposal' lines **
		// Begin Wizard Added Object Disposal

		this.baseToolbarsManager.ToolClick -= new Infragistics.Win.UltraWinToolbars.ToolClickEventHandler(this.baseToolbarsManager_ToolClick);

		this.edvJobHead.EpiViewNotification -= new EpiViewNotification(this.edvJobHead_EpiViewNotification);
		this.edvJobHead = null;
		// End Wizard Added Object Disposal

		// Begin Custom Code Disposal

		// End Custom Code Disposal
		if (_btnSerializado != null)
			_btnSerializado.Click -= BtnSerializado_Click;

		txtPart.TextChanged -= new EventHandler(this.txtPart_TextChanged);
		txtMtlPart.TextChanged -= new EventHandler(this.txtMtlPart_TextChanged);

		this.dgLaborDtl.InitializeRow -= new Infragistics.Win.UltraWinGrid.InitializeRowEventHandler(this.dgLaborDtl_InitializeRow);
	}

	private void txtPart_TextChanged(object sender, System.EventArgs args)
	{

	}

	private void txtMtlPart_TextChanged(object sender, System.EventArgs args)
	{

	}

	private void consultaMatriz(string partNum, string tipo)
	{
		try
		{
			string[] rowValues;
			if (!JobTrackerBaqCache.TryGetUd10Row(partNum, out rowValues))
			{
				using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
				{
					adapterDynamicQuery.BOConnect();
					string BAQName = "CODIFLEX-UD10";
					Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
					ds.ExecutionParameter.AddExecutionParameterRow("partNum_", partNum.Trim(), "nvarchar", false, Guid.Empty, "A");
					adapterDynamicQuery.ExecuteByID(BAQName, ds);

					DataTable table = adapterDynamicQuery.QueryResults.Tables["Results"];
					rowValues = JobTrackerBaqCache.BuildUd10Row(table);
					JobTrackerBaqCache.SetUd10Row(partNum, rowValues);
				}
			}

			if (tipo == "pt")
			{
				if (rowValues != null && rowValues.Length >= 13)
				{
					QR += "LOC: " + rowValues[7] + Environment.NewLine;
					QR += "TH: " + rowValues[6] + Environment.NewLine;
					QR += "Rep. EJE: " + rowValues[8] + Environment.NewLine;
					QR += "Etiqueta al paso: " + rowValues[3] + Environment.NewLine;
					QR += "Num Etiquetas: " + rowValues[9] + Environment.NewLine;
					QR += "Diametro de Corte: " + rowValues[11] + Environment.NewLine;
					QR += "Liner Izquierdo: " + rowValues[4] + Environment.NewLine;
					QR += "Liner Derecho: " + rowValues[5] + Environment.NewLine;
					QR += "Terminado: " + rowValues[10] + Environment.NewLine;
					QR += "Medida de etiqueta Largo: " + rowValues[0] + Environment.NewLine;
					QR += "Medida de etiqueta Ancho: " + rowValues[1] + Environment.NewLine;
					QR += "Salida Insp.: " + rowValues[12] + Environment.NewLine;
				}
			}
			else
			{
				if (rowValues != null && rowValues.Length >= 3)
					QR += "Ancho MP Sugerido: " + rowValues[2] + Environment.NewLine;
				else
					QR += "Ancho MP Sugerido: 0" + Environment.NewLine;
			}
		}
		catch (Exception error)
		{
			MessageBox.Show("Consulta UD10" + Environment.NewLine + error.Message, "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void baseToolbarsManager_ToolClick(object sender, Infragistics.Win.UltraWinToolbars.ToolClickEventArgs args)
	{
		switch (args.Tool.Key)
		{
			case "JobTravPrint":
				try
				{
					EpiDataView edvJobHead = ((EpiDataView)(this.oTrans.EpiDataViews["JobHead"]));

					if (edvJobHead.Row > -1)
					{
						string partNum = edvJobHead.dataView[edvJobHead.Row]["PartNum"].ToString();
						string jobNum = edvJobHead.dataView[edvJobHead.Row]["JobNum"].ToString();
						DateTime reqBy = DateTime.Parse(edvJobHead.dataView[edvJobHead.Row]["ReqDueDate"].ToString());
						QR += jobNum + Environment.NewLine;
						QR += "Cliente: " + cbProdGroup.Text + Environment.NewLine;
						QR += "Parte: " + partNum + Environment.NewLine;
						QR += "Req. Para: " + reqBy.ToString("dd/MM/yyyy") + Environment.NewLine;

						consultaMatriz(partNum, "pt");
					}

					EpiDataView edvJobMtl = ((EpiDataView)(this.oTrans.EpiDataViews["JobMtl"]));

					if (edvJobMtl.Row > -1)
					{
						QR += "Materiales" + Environment.NewLine;
						for (int x = 0; x < edvJobMtl.dataView.Count; x++)
						{
							string partNum = edvJobMtl.dataView[x]["PartNum"].ToString();
							string description = edvJobMtl.dataView[x]["Description"].ToString();
							QR += "Parte: " + partNum + " / " + description + Environment.NewLine;
							consultaMatriz(partNum, "mt");
							string cantReq = edvJobMtl.dataView[x]["RequiredQty"].ToString();
							QR += "Cant. Req: " + cantReq + Environment.NewLine;
						}
					}

					string archivo = "QR.exe";
					string deRuta = @"\\192.168.55.244\EpicorData\PROGRAMAS_EPICOR\JobTraveler2.0";
					string aRuta = @"C:\PROGRAMAS_EPICOR\JobTraveler2.0";

					if (!System.IO.Directory.Exists(aRuta))
						Copiar_Pantallas(archivo, deRuta, aRuta);

					using (StreamWriter writer = new StreamWriter(aRuta + "/CoQR.txt"))
						writer.Write(QR);

					System.Diagnostics.Process.Start(aRuta + "/" + archivo);
				}
				catch (Exception error) { }
				break;
			case "ClearTool":
				epiNumericEditorC1.Text = string.Empty;
				break;
		}
	}

	private void Copiar_Pantallas(string archivo, string deRuta, string aRuta)
	{
		try
		{
			string fileName = archivo;
			string sourcePath = deRuta;
			string targetPath = aRuta;

			string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
			string destFile = System.IO.Path.Combine(targetPath, fileName);

			if (!System.IO.Directory.Exists(targetPath))
				System.IO.Directory.CreateDirectory(targetPath);

			System.IO.File.Copy(sourceFile, destFile, true);

			if (System.IO.Directory.Exists(sourcePath))
			{
				string[] files = System.IO.Directory.GetFiles(sourcePath);
				foreach (string s in files)
				{
					fileName = System.IO.Path.GetFileName(s);
					destFile = System.IO.Path.Combine(targetPath, fileName);
					System.IO.File.Copy(s, destFile, true);
				}
			}
		}
		catch (Exception error)
		{
			MessageBox.Show(error.Message);
		}
	}

	private void tratamientoresiduoespecial(string moy)
	{
		try
		{
			string cached;
			if (JobTrackerBaqCache.TryGetResiduo(moy, out cached))
			{
				resultadotrata.Text = cached;
				return;
			}

			JobTrackerForm.Cursor = Cursors.WaitCursor;
			DynamicQueryAdapter adapterDynamicQuery = null;
			try
			{
				adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans);
				adapterDynamicQuery.BOConnect();

				string BAQName = "BAQESPRESIDUOS";
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("PARTE", moy, "nvarchar", false, Guid.Empty, "A");
				adapterDynamicQuery.ExecuteByID(BAQName, ds);

				bool valorCheckbox = false;

				if (adapterDynamicQuery.QueryResults != null &&
					adapterDynamicQuery.QueryResults.Tables.Contains("Results") &&
					adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
				{
					object valor = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0]["Part_tratamientoespresiduo_c"];
					if (valor != null && valor != DBNull.Value && !string.IsNullOrEmpty(valor.ToString()))
					{
						string strValor = valor.ToString().Trim();
						valorCheckbox = (strValor == "1" || strValor.Equals("true", StringComparison.OrdinalIgnoreCase) ||
							strValor.Equals("cierto", StringComparison.OrdinalIgnoreCase));
					}
				}

				string texto = valorCheckbox ? "SI" : "NO";
				JobTrackerBaqCache.SetResiduo(moy, texto);
				resultadotrata.Text = texto;
			}
			finally
			{
				if (adapterDynamicQuery != null)
					adapterDynamicQuery.Dispose();
				JobTrackerForm.Cursor = Cursors.Default;
			}
		}
		catch (System.Exception ex)
		{
			JobTrackerForm.Cursor = Cursors.Default;
			ExceptionBox.Show(ex);
		}
	}

	private void jobcostopinitariBAQ(string job, string partnum, string completed)
	{
		try
		{
			DynamicQueryAdapter jobquery = new DynamicQueryAdapter(this.oTrans);
			DynamicQueryAdapter moyQuery = new DynamicQueryAdapter(this.oTrans);
			jobquery.BOConnect();
			moyQuery.BOConnect();
			string BAQName3 = "JOBCOSTO";
			string BAQName2 = "PUNITARIOBAQ";
			Ice.BO.QueryExecutionDataSet ds2 = new Ice.BO.QueryExecutionDataSet();
			Ice.BO.QueryExecutionDataSet ds3 = new Ice.BO.QueryExecutionDataSet();
			ds3.ExecutionParameter.AddExecutionParameterRow("JOBNUM", job, "nvarchar", false, Guid.Empty, "A");
			ds2.ExecutionParameter.AddExecutionParameterRow("PART", partnum, "nvarchar", false, Guid.Empty, "A");
			moyQuery.ExecuteByID(BAQName2, ds2);
			jobquery.ExecuteByID(BAQName3, ds3);
			string jobnums = " ";
			string preciounit = " ";
			if (moyQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			{
				preciounit = moyQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
				decimal preciodec = Convert.ToDecimal(preciounit);
				precio.Value = preciodec;
			}
			if (jobquery.QueryResults.Tables["Results"].Rows.Count > 0)
			{
				jobnums = jobquery.QueryResults.Tables["Results"].Rows[0][0].ToString();
				decimal obsnums1 = Convert.ToDecimal(jobnums);
				decimal attempt1 = Convert.ToDecimal(completed);
				if (attempt1 != 0)
				{
					total.Value = 0;
					decimal totaldef = obsnums1 / attempt1;
					total.Value = totaldef;
				}
			}
		}
		catch (System.Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	private void FT_BAQ(string jobnum)
	{
		try
		{
			string cached;
			if (JobTrackerBaqCache.TryGetFtBaQ(jobnum, out cached))
			{
				epiNumericEditorC1.Text = cached;
				return;
			}

			using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
			{
				adapterDynamicQuery.BOConnect();
				string BAQName = "FT_BAQ";
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("JOBNUM", jobnum, "nvarchar", false, Guid.Empty, "A");
				adapterDynamicQuery.ExecuteByID(BAQName, ds);
				string Part1 = " ";
				if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
					Part1 = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
				JobTrackerBaqCache.SetFtBaQ(jobnum, Part1);
				epiNumericEditorC1.Text = Part1;
			}
		}
		catch (System.Exception ex)
		{
			ExceptionBox.Show(ex);
		}
	}

	private void BAQLARGOREAL(string partes)
	{
		JobTrackerBaqCache.LargoRealEntry cached;
		if (JobTrackerBaqCache.TryGetLargoReal(partes, out cached))
		{
			numlreal.Text = Convert.ToString(cached.LargoReal);
			etiquetasalpaso.Text = Convert.ToString(cached.EtPaso);
			epiNumericEditorC2.Text = Convert.ToString(cached.Cant);
			return;
		}

		using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
		{
			adapterDynamicQuery.BOConnect();
			string BAQName = "BAQLARGOREAL";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("PARTE", partes, "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(BAQName, ds);

			if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			{
				decimal largo = Convert.ToDecimal(adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString());
				decimal gap = Convert.ToDecimal(adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString());
				decimal etpaso = Convert.ToDecimal(adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][2].ToString());
				decimal cant = Convert.ToDecimal(adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][3].ToString());
				decimal largoreal = (gap + largo);

				JobTrackerBaqCache.SetLargoReal(partes, largoreal, etpaso, cant);
				numlreal.Text = Convert.ToString(largoreal);
				etiquetasalpaso.Text = Convert.ToString(etpaso);
				epiNumericEditorC2.Text = Convert.ToString(cant);
			}
		}
	}

	private string departamentosearch(string partesss)
	{
		string cached;
		if (JobTrackerBaqCache.TryGetDepartamento(partesss, out cached))
			return cached;

		using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
		{
			adapterDynamicQuery.BOConnect();
			string BAQName = "BAQPARTEDEPAR";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("PARTE", partesss, "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(BAQName, ds);
			string partef = " ";
			if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
				partef = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
			JobTrackerBaqCache.SetDepartamento(partesss, partef);
			return partef;
		}
	}

	private void searchmp(string jobt)
	{
		string job = edvJobHead.dataView[edvJobHead.Row]["JobNum"].ToString();
		string partnum = edvJobHead.dataView[edvJobHead.Row]["PartNum"].ToString();
		string completed = edvJobHead.dataView[edvJobHead.Row]["QtyCompleted"].ToString();

		string jobss;
		if (!JobTrackerBaqCache.TryGetSearchMp(jobt, out jobss))
		{
			using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
			{
				adapterDynamicQuery.BOConnect();
				string BAQName = "BAQSEARCHMP";
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("JOB", jobt, "nvarchar", false, Guid.Empty, "A");
				adapterDynamicQuery.ExecuteByID(BAQName, ds);
				jobss = " ";
				if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
					jobss = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
			}
			JobTrackerBaqCache.SetSearchMp(jobt, jobss);
		}

		if (!string.IsNullOrWhiteSpace(jobss))
		{
			string depto = departamentosearch(partnum);
			if (depto == "Flexo" || depto == "Serigrafia" || depto == "Digital")
			{
				jobcostopinitariBAQ(job, partnum, completed);
				FT_BAQ(job);
				BAQLARGOREAL(partnum);
			}
		}
	}

	private void searchdatosinspeccion(string parte)
	{
		JobTrackerBaqCache.MatrizInspeccionEntry cached;
		if (JobTrackerBaqCache.TryGetMatrizInspeccion(parte, out cached))
		{
			ApplyMatrizInspeccionUi(cached);
			return;
		}

		using (DynamicQueryAdapter adapterDynamicQuery = new DynamicQueryAdapter(this.oTrans))
		{
			adapterDynamicQuery.BOConnect();
			string BAQName = "BAQMATRIZINSPECCION";
			Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
			ds.ExecutionParameter.AddExecutionParameterRow("PARTE", parte, "nvarchar", false, Guid.Empty, "A");
			adapterDynamicQuery.ExecuteByID(BAQName, ds);
			if (adapterDynamicQuery.QueryResults.Tables["Results"].Rows.Count > 0)
			{
				JobTrackerBaqCache.MatrizInspeccionEntry entry = new JobTrackerBaqCache.MatrizInspeccionEntry();
				entry.EtqPaso = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][0].ToString();
				entry.NumEtiqPaso = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][1].ToString();
				entry.Presentacion = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][2].ToString();
				entry.SalidaInspeccion = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][3].ToString();
				entry.LinerIzquierdo = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][4].ToString();
				entry.LinerDerecho = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][5].ToString();
				entry.Comentarios = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][6].ToString();
				entry.NumCore = adapterDynamicQuery.QueryResults.Tables["Results"].Rows[0][7].ToString();
				JobTrackerBaqCache.SetMatrizInspeccion(parte, entry);
				ApplyMatrizInspeccionUi(entry);
			}
		}
	}

	private void ApplyMatrizInspeccionUi(JobTrackerBaqCache.MatrizInspeccionEntry entry)
	{
		if (entry == null)
			return;
		etqpaso.Value = entry.EtqPaso;
		numetiqpaso.Value = entry.NumEtiqPaso;
		presentacionparte.Text = entry.Presentacion;
		salidainspeccion.Value = entry.SalidaInspeccion;
		linerizquierdo.Value = entry.LinerIzquierdo;
		linerderecho.Value = entry.LinerDerecho;
		comentariosinspeccion.Text = entry.Comentarios;
		numcore.Value = entry.NumCore;
	}

	private static string SafeDataViewString(EpiDataView view, int row, string column)
	{
		try
		{
			if (view == null || row < 0 || view.dataView == null || row >= view.dataView.Count)
				return string.Empty;
			object v = view.dataView[row][column];
			return v == null || v == DBNull.Value ? string.Empty : v.ToString().Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private void ScheduleDeferredJobHeadLoad(EpiDataView view, int row)
	{
		if (view == null || row < 0)
			return;

		string part = SafeDataViewString(view, row, "PartNum");
		string jobNum = SafeDataViewString(view, row, "JobNum");
		string key = jobNum + "\x1e" + part + "\x1e" + row.ToString(CultureInfo.InvariantCulture);
		if (string.Equals(_pendingDeferredJobKey, key, StringComparison.Ordinal))
			return;

		_pendingDeferredJobKey = key;

		try
		{
			Form host = JobTrackerForm;
			if (host == null || host.IsDisposed)
				return;

			MethodInvoker work = new MethodInvoker(delegate
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
					JobTrackerForm.Cursor = Cursors.Default;
					ExceptionBox.Show(ex);
				}
			});

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
		string job = SafeDataViewString(view, row, "JobNum");
		string partejobh = SafeDataViewString(view, row, "PartNum");
		searchmp(job);
		searchdatosinspeccion(partejobh);
		tratamientoresiduoespecial(partejobh);
	}

	private void edvJobHead_EpiViewNotification(EpiDataView view, EpiNotifyArgs args)
	{
		if ((args.NotifyType == EpiTransaction.NotifyType.Initialize))
		{
			if ((args.Row > -1))
				ScheduleDeferredJobHeadLoad(view, args.Row);
		}
	}

	private void dgLaborDtl_InitializeRow(object sender, Infragistics.Win.UltraWinGrid.InitializeRowEventArgs args)
	{
		if (!dgLaborDtl.DisplayLayout.Bands[0].Columns.Exists("HoraCreacion"))
			dgLaborDtl.DisplayLayout.Bands[0].Columns.Add("HoraCreacion", "Hora Creación");

		TimeSpan time = TimeSpan.FromSeconds(int.Parse(args.Row.Cells["CreateTime"].Value.ToString()));
		args.Row.Cells["HoraCreacion"].Value = time.ToString(@"hh\:mm\:ss");
	}
}

// =============================================================================
// Caché BAQ por sesión — Job Tracker
// =============================================================================

public static class JobTrackerBaqCache
{
	private static readonly Dictionary<string, string> DepartamentoByPart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> SearchMpByJob = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> FtByJob = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> ResiduoByPart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string[]> Ud10RowByPart = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, MatrizInspeccionEntry> MatrizByPart = new Dictionary<string, MatrizInspeccionEntry>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, LargoRealEntry> LargoRealByPart = new Dictionary<string, LargoRealEntry>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, int> LabelsPerRollByPart = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, JobTrackerSerialJobInfo> SerialJobByJob = new Dictionary<string, JobTrackerSerialJobInfo>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, List<PtSerialUd37CacheRow>> PtUd37ByJob = new Dictionary<string, List<PtSerialUd37CacheRow>>(StringComparer.OrdinalIgnoreCase);

	public sealed class MatrizInspeccionEntry
	{
		public string EtqPaso = string.Empty;
		public string NumEtiqPaso = string.Empty;
		public string Presentacion = string.Empty;
		public string SalidaInspeccion = string.Empty;
		public string LinerIzquierdo = string.Empty;
		public string LinerDerecho = string.Empty;
		public string Comentarios = string.Empty;
		public string NumCore = string.Empty;
	}

	public sealed class LargoRealEntry
	{
		public decimal LargoReal;
		public decimal EtPaso;
		public decimal Cant;
	}

	private static string NormalizeKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return string.Empty;
		return value.Trim();
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

	public static bool TryGetDepartamento(string part, out string value)
	{
		return TryGet(DepartamentoByPart, part, out value);
	}

	public static void SetDepartamento(string part, string value)
	{
		Set(DepartamentoByPart, part, value);
	}

	public static bool TryGetSearchMp(string job, out string value)
	{
		return TryGet(SearchMpByJob, job, out value);
	}

	public static void SetSearchMp(string job, string value)
	{
		Set(SearchMpByJob, job, value);
	}

	public static bool TryGetFtBaQ(string jobNum, out string value)
	{
		return TryGet(FtByJob, jobNum, out value);
	}

	public static void SetFtBaQ(string jobNum, string value)
	{
		Set(FtByJob, jobNum, value);
	}

	public static bool TryGetResiduo(string part, out string value)
	{
		return TryGet(ResiduoByPart, part, out value);
	}

	public static void SetResiduo(string part, string value)
	{
		Set(ResiduoByPart, part, value);
	}

	public static bool TryGetUd10Row(string partNum, out string[] rowValues)
	{
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0)
		{
			rowValues = null;
			return false;
		}
		return Ud10RowByPart.TryGetValue(partNum, out rowValues);
	}

	public static void SetUd10Row(string partNum, string[] rowValues)
	{
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0)
			return;
		Ud10RowByPart[partNum] = rowValues;
	}

	public static string[] BuildUd10Row(DataTable table)
	{
		if (table == null || table.Rows.Count == 0)
			return null;

		DataRow row = table.Rows[0];
		int colCount = table.Columns.Count;
		int take = colCount < 13 ? colCount : 13;
		string[] values = new string[take];
		for (int i = 0; i < take; i++)
			values[i] = row[i] == null || row[i] == DBNull.Value ? string.Empty : row[i].ToString();
		return values;
	}

	public static bool TryGetMatrizInspeccion(string part, out MatrizInspeccionEntry entry)
	{
		part = NormalizeKey(part);
		if (part.Length == 0)
		{
			entry = null;
			return false;
		}
		return MatrizByPart.TryGetValue(part, out entry);
	}

	public static void SetMatrizInspeccion(string part, MatrizInspeccionEntry entry)
	{
		part = NormalizeKey(part);
		if (part.Length == 0 || entry == null)
			return;
		MatrizByPart[part] = entry;
	}

	public static bool TryGetLargoReal(string part, out LargoRealEntry entry)
	{
		part = NormalizeKey(part);
		if (part.Length == 0)
		{
			entry = null;
			return false;
		}
		return LargoRealByPart.TryGetValue(part, out entry);
	}

	public static void SetLargoReal(string part, decimal largoReal, decimal etPaso, decimal cant)
	{
		part = NormalizeKey(part);
		if (part.Length == 0)
			return;
		LargoRealEntry entry = new LargoRealEntry();
		entry.LargoReal = largoReal;
		entry.EtPaso = etPaso;
		entry.Cant = cant;
		LargoRealByPart[part] = entry;
	}

	public static bool TryGetLabelsPerRoll(string partNum, out int labelsPerRoll)
	{
		partNum = NormalizeKey(partNum);
		labelsPerRoll = 0;
		if (partNum.Length == 0)
			return false;
		return LabelsPerRollByPart.TryGetValue(partNum, out labelsPerRoll);
	}

	public static void SetLabelsPerRoll(string partNum, int labelsPerRoll)
	{
		partNum = NormalizeKey(partNum);
		if (partNum.Length == 0)
			return;
		LabelsPerRollByPart[partNum] = labelsPerRoll;
	}

	public static bool TryGetSerialJobInfo(string jobNum, out JobTrackerSerialJobInfo info)
	{
		jobNum = NormalizeKey(jobNum);
		if (jobNum.Length == 0)
		{
			info = null;
			return false;
		}
		return SerialJobByJob.TryGetValue(jobNum, out info);
	}

	public static void SetSerialJobInfo(string jobNum, JobTrackerSerialJobInfo info)
	{
		jobNum = NormalizeKey(jobNum);
		if (jobNum.Length == 0 || info == null)
			return;
		SerialJobByJob[jobNum] = info;
	}

	public static bool TryGetPtUd37Rows(string jobNum, out List<PtSerialUd37CacheRow> rows)
	{
		jobNum = NormalizeKey(jobNum);
		if (jobNum.Length == 0)
		{
			rows = null;
			return false;
		}
		return PtUd37ByJob.TryGetValue(jobNum, out rows);
	}

	public static void SetPtUd37Rows(string jobNum, List<PtSerialUd37CacheRow> rows)
	{
		jobNum = NormalizeKey(jobNum);
		if (jobNum.Length == 0)
			return;
		PtUd37ByJob[jobNum] = rows;
	}
}

public sealed class PtSerialUd37CacheRow
{
	public int RollNum;
	public string Status = string.Empty;
	public string ConfirmedBy = string.Empty;
	public DateTime? ConfirmedOn;
}

// =============================================================================
// CONSULTA — Rollos PT recibidos (solo lectura, Ice.UD37)
// =============================================================================

public sealed class PtSerialRollViewRow
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

public sealed class JobTrackerSerialJobInfo
{
	public string JobNum = string.Empty;
	public string PartNum = string.Empty;
	public string PurchaseOrder = string.Empty;
	public string Cliente = string.Empty;
	public string SerialPrefix = "J0";
	public string SerialFrom = string.Empty;
	public int RollCount;
	public decimal SerializedQty;
}

public static class JobTrackerPtSerialService
{
	private const string SerialJobBaqName = "BAQ-SERIAL-JOBHEAD";
	private const string PtUd37BaqName = "BAQ-PT-SERIAL-UD37";

	public static JobTrackerSerialJobInfo LoadJobInfo(object oTrans, string jobNum, string partNum)
	{
		JobTrackerSerialJobInfo info = new JobTrackerSerialJobInfo();
		info.JobNum = jobNum ?? string.Empty;
		info.PartNum = partNum ?? string.Empty;

		if (string.IsNullOrWhiteSpace(jobNum))
			return info;

		JobTrackerSerialJobInfo cached;
		if (JobTrackerBaqCache.TryGetSerialJobInfo(jobNum, out cached) && cached != null)
			return CloneJobInfo(cached);

		try
		{
			using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
			{
				dq.BOConnect();
				Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
				ds.ExecutionParameter.AddExecutionParameterRow("JobNum", jobNum.Trim(), "nvarchar", false, Guid.Empty, "A");
				dq.ExecuteByID(SerialJobBaqName, ds);

				DataTable t = dq.QueryResults.Tables["Results"];
				if (t != null && t.Rows.Count > 0)
				{
					DataRow r = t.Rows[0];
					info.PartNum = ReadField(r, t, new string[] { "PartNum" }, info.PartNum);
					info.PurchaseOrder = ReadField(r, t, new string[] { "orden_compra_c", "PurchaseOrder" }, string.Empty);
					info.Cliente = ReadField(r, t, new string[] { "CustID_c", "Cliente" }, string.Empty);
					info.SerialPrefix = ReadField(r, t, new string[] { "SerialPrefix_c", "SerialPrefix" }, "J0");
					info.SerialFrom = ReadField(r, t, new string[] { "SerialFrom_c", "SerialFrom" }, string.Empty);
					info.RollCount = ReadInt(r, t, new string[] { "RollCount_c", "RollCount" });
					info.SerializedQty = ReadDecimal(r, t, new string[] { "SerializedQty_c", "SerializedQty", "ProdQty" });
				}
			}
		}
		catch { }

		if (info.RollCount <= 0 && info.SerializedQty > 0m)
		{
			int perRoll = GetLabelsPerRoll(oTrans, info.PartNum);
			if (perRoll > 0)
				info.RollCount = SplitCapacities((int)Math.Round(info.SerializedQty, 0), perRoll).Count;
		}

		JobTrackerBaqCache.SetSerialJobInfo(jobNum, CloneJobInfo(info));
		return info;
	}

	private static JobTrackerSerialJobInfo CloneJobInfo(JobTrackerSerialJobInfo source)
	{
		JobTrackerSerialJobInfo copy = new JobTrackerSerialJobInfo();
		if (source == null)
			return copy;
		copy.JobNum = source.JobNum;
		copy.PartNum = source.PartNum;
		copy.PurchaseOrder = source.PurchaseOrder;
		copy.Cliente = source.Cliente;
		copy.SerialPrefix = source.SerialPrefix;
		copy.SerialFrom = source.SerialFrom;
		copy.RollCount = source.RollCount;
		copy.SerializedQty = source.SerializedQty;
		return copy;
	}

	public static List<PtSerialRollViewRow> LoadPtRolls(object oTrans, string jobNum, string partNum)
	{
		List<PtSerialRollViewRow> list = new List<PtSerialRollViewRow>();
		JobTrackerSerialJobInfo info = LoadJobInfo(oTrans, jobNum, partNum);

		if (string.IsNullOrWhiteSpace(info.SerialFrom) || info.SerializedQty <= 0m)
			return list;

		int totalQty = (int)Math.Round(info.SerializedQty, 0);
		int labelsPerRoll = GetLabelsPerRoll(oTrans, info.PartNum);
		List<int> capacities = SplitCapacities(totalQty, labelsPerRoll);
		if (info.RollCount > 0 && capacities.Count > info.RollCount)
			capacities = capacities.GetRange(0, info.RollCount);

		long serialFrom = ParseDigits(info.SerialFrom);
		if (serialFrom <= 0L)
			return list;

		string prefix = string.IsNullOrWhiteSpace(info.SerialPrefix) ? "J0" : info.SerialPrefix.Trim();
		long current = serialFrom;
		for (int i = 0; i < capacities.Count; i++)
		{
			int qty = capacities[i];
			if (qty <= 0)
				continue;
			long serialTo = current + qty - 1L;
			PtSerialRollViewRow row = new PtSerialRollViewRow();
			row.RollNum = list.Count + 1;
			row.Prefix = prefix;
			row.SerialFrom = prefix + current.ToString();
			row.SerialTo = prefix + serialTo.ToString();
			row.Quantity = qty;
			row.Status = "Impreso";
			list.Add(row);
			current = serialTo + 1L;
		}

		ApplyUd37Status(oTrans, jobNum, list);
		return list;
	}

	public static int CountReceived(List<PtSerialRollViewRow> rolls)
	{
		int n = 0;
		for (int i = 0; i < rolls.Count; i++)
			if (string.Equals(rolls[i].Status, "Recibido", StringComparison.OrdinalIgnoreCase))
				n++;
		return n;
	}

	private static void ApplyUd37Status(object oTrans, string jobNum, List<PtSerialRollViewRow> rolls)
	{
		if (rolls == null || rolls.Count == 0 || string.IsNullOrWhiteSpace(jobNum))
			return;

		Dictionary<int, PtSerialRollViewRow> byRoll = new Dictionary<int, PtSerialRollViewRow>();
		for (int i = 0; i < rolls.Count; i++)
			byRoll[rolls[i].RollNum] = rolls[i];

		List<PtSerialUd37CacheRow> cachedRows;
		if (!JobTrackerBaqCache.TryGetPtUd37Rows(jobNum, out cachedRows))
		{
			cachedRows = new List<PtSerialUd37CacheRow>();
			try
			{
				using (DynamicQueryAdapter dq = new DynamicQueryAdapter(oTrans))
				{
					dq.BOConnect();
					Ice.BO.QueryExecutionDataSet ds = new Ice.BO.QueryExecutionDataSet();
					ds.ExecutionParameter.AddExecutionParameterRow("JobNum", jobNum.Trim(), "nvarchar", false, Guid.Empty, "A");
					string company = GetCompanyCode(oTrans);
					if (!string.IsNullOrWhiteSpace(company))
						ds.ExecutionParameter.AddExecutionParameterRow("Company", company, "nvarchar", false, Guid.Empty, "B");
					dq.ExecuteByID(PtUd37BaqName, ds);

					DataTable t = dq.QueryResults.Tables["Results"];
					if (t != null && t.Rows.Count > 0)
					{
						for (int i = 0; i < t.Rows.Count; i++)
						{
							DataRow r = t.Rows[i];
							int rollNum = ReadInt(r, t, new string[] { "RollNum", "Number01", "UD37_Number01" });
							if (rollNum <= 0)
								continue;

							PtSerialUd37CacheRow cacheRow = new PtSerialUd37CacheRow();
							cacheRow.RollNum = rollNum;
							cacheRow.Status = ReadField(r, t, new string[] { "Estado", "Status", "Character05" }, string.Empty);
							cacheRow.ConfirmedBy = ReadField(r, t, new string[] { "ConfirmadoPor", "ConfirmedBy", "Character06" }, string.Empty);

							string dateText = ReadField(r, t, new string[] {
								"FechaConfirmacion", "FechaRecibido", "ConfirmedOn", "Date01", "UD37_Date01"
							}, string.Empty);
							if (!string.IsNullOrWhiteSpace(dateText))
							{
								DateTime dt;
								if (DateTime.TryParse(dateText, out dt))
									cacheRow.ConfirmedOn = dt;
							}
							cachedRows.Add(cacheRow);
						}
					}
				}
			}
			catch { }

			JobTrackerBaqCache.SetPtUd37Rows(jobNum, cachedRows);
		}

		for (int i = 0; i < cachedRows.Count; i++)
		{
			PtSerialUd37CacheRow cacheRow = cachedRows[i];
			PtSerialRollViewRow row;
			if (!byRoll.TryGetValue(cacheRow.RollNum, out row))
				continue;

			if (!string.IsNullOrWhiteSpace(cacheRow.Status))
				row.Status = cacheRow.Status;
			row.ConfirmedBy = cacheRow.ConfirmedBy;
			row.ConfirmedOn = cacheRow.ConfirmedOn;
		}
	}

	private static int GetLabelsPerRoll(object oTrans, string partNum)
	{
		if (string.IsNullOrWhiteSpace(partNum))
			return 1400;

		int cached;
		if (JobTrackerBaqCache.TryGetLabelsPerRoll(partNum, out cached) && cached > 0)
			return cached;

		int labelsPerRoll = 1400;
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
				{
					JobTrackerBaqCache.SetUd10Row(partNum, JobTrackerBaqCache.BuildUd10Row(t));
					for (int c = 0; c < t.Columns.Count; c++)
					{
						string name = t.Columns[c].ColumnName ?? string.Empty;
						if (name.IndexOf("Number16", StringComparison.OrdinalIgnoreCase) >= 0
							|| name.IndexOf("Cant", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							decimal d = ReadDecimal(t.Rows[0], t, new string[] { name });
							if (d > 0m)
							{
								labelsPerRoll = (int)Math.Round(d, 0);
								break;
							}
						}
					}
				}
			}
		}
		catch { }

		JobTrackerBaqCache.SetLabelsPerRoll(partNum, labelsPerRoll);
		return labelsPerRoll;
	}

	private static List<int> SplitCapacities(int totalQty, int perRoll)
	{
		List<int> list = new List<int>();
		if (totalQty <= 0 || perRoll <= 0)
			return list;
		int remaining = totalQty;
		while (remaining > 0)
		{
			int take = remaining >= perRoll ? perRoll : remaining;
			list.Add(take);
			remaining -= take;
		}
		return list;
	}

	private static long ParseDigits(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return 0L;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
			if (char.IsDigit(input[i]))
				sb.Append(input[i]);
		long val;
		if (long.TryParse(sb.ToString(), out val))
			return val;
		return 0L;
	}

	private static string ReadField(DataRow row, DataTable table, string[] hints, string fallback)
	{
		string v = ReadField(row, table, hints);
		return string.IsNullOrWhiteSpace(v) ? fallback : v;
	}

	private static string ReadField(DataRow row, DataTable table, string[] hints)
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

	private static int ReadInt(DataRow row, DataTable table, string[] hints)
	{
		decimal d = ReadDecimal(row, table, hints);
		return (int)Math.Round(d, 0);
	}

	private static decimal ReadDecimal(DataRow row, DataTable table, string[] hints)
	{
		string text = ReadField(row, table, hints);
		decimal d;
		if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
			return d;
		return 0m;
	}

	private static string GetCompanyCode(object oTrans)
	{
		try
		{
			System.Reflection.PropertyInfo pi = oTrans.GetType().GetProperty("Session");
			if (pi != null)
			{
				Ice.Core.Session session = pi.GetValue(oTrans, null) as Ice.Core.Session;
				if (session != null && !string.IsNullOrWhiteSpace(session.CompanyID))
					return session.CompanyID.Trim();
			}
		}
		catch { }
		return string.Empty;
	}
}

// =============================================================================
// MODAL — Solo tabla de rollos PT (Job Tracker)
// =============================================================================

public sealed class JobTrackerSerializadoForm : Form
{
	private readonly object _oTrans;
	private readonly string _jobNum;
	private readonly string _partNum;
	private JobTrackerSerialJobInfo _jobInfo;
	private readonly DataGridView _grid;
	private Label _lblProgress;
	private Label _lblSub;

	public JobTrackerSerializadoForm(object oTrans, string jobNum, string partNum)
	{
		_oTrans = oTrans;
		_jobNum = jobNum ?? string.Empty;
		_partNum = partNum ?? string.Empty;
		_jobInfo = new JobTrackerSerialJobInfo();
		_jobInfo.JobNum = _jobNum;
		_jobInfo.PartNum = _partNum;

		Text = "Confirmacion de Serializado — Recibo PT";
		Size = new Size(920, 520);
		StartPosition = FormStartPosition.CenterParent;
		MinimumSize = new Size(820, 420);
		BackColor = Color.FromArgb(245, 247, 250);
		Font = new Font("Segoe UI", 9f);
		FormBorderStyle = FormBorderStyle.Sizable;

		Panel header = BuildHeader();
		Panel body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8) };

		Label info = new Label
		{
			Dock = DockStyle.Top,
			Height = 40,
			Text = "Rollos confirmados en Producto Terminado (Ice.UD37). Solo consulta.",
			ForeColor = Color.FromArgb(0, 86, 179),
			BackColor = Color.FromArgb(231, 245, 255),
			Padding = new Padding(10, 8, 10, 8),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};

		_grid = BuildGrid();
		body.Controls.Add(_grid);
		body.Controls.Add(info);

		Panel footer = BuildFooter();

		Controls.Add(body);
		Controls.Add(footer);
		Controls.Add(header);

		_lblProgress.Text = "Cargando rollos...";
		this.Load += this.JobTrackerSerializadoForm_Load;
	}

	private void JobTrackerSerializadoForm_Load(object sender, EventArgs e)
	{
		this.Load -= this.JobTrackerSerializadoForm_Load;
		if (IsDisposed)
			return;
		try
		{
			_jobInfo = JobTrackerPtSerialService.LoadJobInfo(_oTrans, _jobNum, _partNum);
			UpdateHeaderSub();
			LoadGrid();
		}
		catch (Exception ex)
		{
			_lblProgress.Text = "Error al cargar rollos: " + ex.Message;
		}
	}

	private void UpdateHeaderSub()
	{
		if (_lblSub == null || _jobInfo == null)
			return;
		_lblSub.Text = string.Format("Job: {0}   |   Parte: {1}   |   PO: {2}   |   Cliente: {3}",
			_jobNum, _jobInfo.PartNum, _jobInfo.PurchaseOrder, _jobInfo.Cliente);
	}

	private Panel BuildHeader()
	{
		Panel p = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.FromArgb(0, 86, 179), Padding = new Padding(16, 10, 16, 10) };
		Label title = new Label
		{
			Text = "Confirmacion de Serializado — Recibo PT",
			ForeColor = Color.White,
			Font = new Font("Segoe UI Semibold", 13f),
			Location = new Point(12, 8),
			AutoSize = true
		};
		Label sub = new Label
		{
			Text = string.Format("Job: {0}   |   Parte: {1}   |   PO: {2}   |   Cliente: {3}",
				_jobNum, _partNum, string.Empty, string.Empty),
			ForeColor = Color.FromArgb(210, 230, 255),
			Font = new Font("Segoe UI", 9f),
			Location = new Point(14, 36),
			AutoSize = true
		};
		_lblSub = sub;
		_lblProgress = new Label
		{
			Text = "Progreso: 0/0 rollos recibidos en PT",
			ForeColor = Color.White,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Location = new Point(14, 56),
			AutoSize = true
		};
		p.Controls.Add(title);
		p.Controls.Add(sub);
		p.Controls.Add(_lblProgress);
		return p;
	}

	private Panel BuildFooter()
	{
		Panel p = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White, Padding = new Padding(12, 8, 12, 8) };
		FlowLayoutPanel flow = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
		Button btnClose = new Button
		{
			Text = "Cerrar",
			Width = 100,
			Height = 30,
			BackColor = Color.FromArgb(108, 117, 125),
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat
		};
		btnClose.FlatAppearance.BorderSize = 0;
		btnClose.Click += delegate { Close(); };
		flow.Controls.Add(btnClose);
		p.Controls.Add(flow);
		return p;
	}

	private DataGridView BuildGrid()
	{
		DataGridView grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			ReadOnly = true,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			RowHeadersVisible = false,
			BackgroundColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			ColumnHeadersHeight = 34,
			RowTemplate = { Height = 30 }
		};
		grid.Columns.Add("RollNum", "Rollo");
		grid.Columns.Add("Prefix", "Prefijo");
		grid.Columns.Add("SerialFrom", "START");
		grid.Columns.Add("SerialTo", "END");
		grid.Columns.Add("Quantity", "Cantidad");
		grid.Columns.Add("Status", "Estado");
		grid.Columns.Add("ConfirmedBy", "Confirmado por");
		grid.Columns.Add("ConfirmedOn", "Fecha recibido PT");
		grid.Columns["ConfirmedOn"].DefaultCellStyle.Format = "dd/MM/yyyy";
		grid.Columns["ConfirmedOn"].MinimumWidth = 115;
		return grid;
	}

	private static string FormatPtReceivedDate(DateTime? confirmedOn)
	{
		if (!confirmedOn.HasValue)
			return string.Empty;
		return confirmedOn.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
	}

	private void LoadGrid()
	{
		List<PtSerialRollViewRow> rolls = JobTrackerPtSerialService.LoadPtRolls(_oTrans, _jobNum, _partNum);
		_grid.Rows.Clear();

		if (rolls.Count == 0)
		{
			_lblProgress.Text = "Sin datos de serializado para este Job (JobHead_UD / UD37).";
			return;
		}

		for (int i = 0; i < rolls.Count; i++)
		{
			PtSerialRollViewRow r = rolls[i];
			int idx = _grid.Rows.Add(
				r.RollNum,
				r.Prefix,
				r.SerialFrom,
				r.SerialTo,
				r.Quantity.ToString("N0"),
				r.Status,
				r.ConfirmedBy,
				FormatPtReceivedDate(r.ConfirmedOn));

			DataGridViewRow row = _grid.Rows[idx];
			if (string.Equals(r.Status, "Recibido", StringComparison.OrdinalIgnoreCase))
			{
				row.Cells["Status"].Style.ForeColor = Color.FromArgb(46, 125, 50);
				row.Cells["Status"].Style.Font = new Font(_grid.Font, FontStyle.Bold);
			}
			else
			{
				row.Cells["Status"].Style.ForeColor = Color.FromArgb(230, 126, 34);
				row.Cells["Status"].Style.Font = new Font(_grid.Font, FontStyle.Bold);
			}
		}

		int received = JobTrackerPtSerialService.CountReceived(rolls);
		_lblProgress.Text = string.Format("Progreso: {0}/{1} rollos recibidos en PT", received, rolls.Count);
	}
}
