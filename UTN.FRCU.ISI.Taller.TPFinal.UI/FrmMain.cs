using Common.Logging;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using UTN.FRCU.ISI.Taller.TPFinal.Model;

namespace UTN.FRCU.ISI.Taller.TPFinal.UI
{
    public partial class FrmMain : Form
    {

        /// <summary>
        /// Definición de logger para todas las instancias de la clase.
        /// </summary>
        private static readonly ILog cLogger = LogManager.GetLogger<FrmMain>();

        public FrmMain()
        {
            InitializeComponent();

            this.dgRssItems.AutoGenerateColumns = false;
        }

        private void FrmMain_Load(Object pSender, EventArgs pEventArgs)
        {
            // Simplemente se hace para que visualmente se vea la implementación de IRssReader que se está utilizando.
            this.lblStatus.Text = String.Format("Implementación IRssReader: {0}", IoCContainerLocator.Container.Resolve<IRssReader>().GetType().FullName);
        }

        private void btnUpdate_Click(Object pSender, EventArgs pEventArgs)
        {
            try
            {
                if (!this.bwRssReader.IsBusy)
                {
                    if (String.IsNullOrWhiteSpace(this.cmbUrl.Text))
                    {
                        throw new ArgumentException("Debe ingresar una URL");
                    }

                    Uri mUrl;

                    if (!Uri.TryCreate(this.cmbUrl.Text.Trim(), UriKind.Absolute, out mUrl))
                    {
                        throw new ArgumentException("La URL ingresada no es válida.");
                    }

                    cLogger.Info("Deshabilitando botón de actualización...");
                    this.btnUpdate.Enabled = false;

                    cLogger.Info("Cambiando a cursor de espera...");
                    this.Cursor = Cursors.WaitCursor;

                    cLogger.Info("Iniciando operación en worker thread...");
                    this.bwRssReader.RunWorkerAsync(mUrl);
                }
            }
            catch (Exception bEx)
            {
                cLogger.Error("Se ha producido un error al intentar actualizar los feeds.", bEx);
                MessageBox.Show(bEx.Message, "Ha ocurrido un error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bwRssReader_DoWork(Object pSender, DoWorkEventArgs pEventArgs)
        {
            cLogger.Info("Resolviendo instancia de IRssReader...");
            IRssReader mRssReader = IoCContainerLocator.Container.Resolve<IRssReader>();

            cLogger.Info("Obteniendo feeds...");
            pEventArgs.Result = mRssReader.Read((Uri) pEventArgs.Argument);
        }

        private void bwRssReader_RunWorkerCompleted(Object pSender, RunWorkerCompletedEventArgs pEventArgs)
        {
            if (pEventArgs.Error != null)
            {
                cLogger.Error("La obtención de feeds ha fallado.", pEventArgs.Error);

                MessageBox.Show(String.Format("No se han podido obtener datos de la fuente RSS: {0}", pEventArgs.Error.Message),
                                              "Ha ocurrido un error",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Error);
            }
            else if (!pEventArgs.Cancelled)
            {
                cLogger.Info("La obtención de feeds ha finalizado exitosamente.");    
                IEnumerable<RssItem> mItems = (IEnumerable<RssItem>) pEventArgs.Result;

                cLogger.Info("Realizando binding con la grilla...");
                this.dgRssItems.DataSource = mItems;

                cLogger.Debug(pLogger => pLogger("Se ha(n) obtenido {0} feeds.", mItems.Count()));
                this.lblStatus.Text = String.Format("Se ha(n) obtenido {0} elemento(s).", mItems.Count());
            }

            cLogger.Info("Habilitando botón de actualización...");
            this.btnUpdate.Enabled = true;

            cLogger.Info("Volviendo cursor a la normalidad...");
            this.Cursor = Cursors.Default;
        }
    }
}
