/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dynammo.Networking;
using Dynammo.Common;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     Shows the user a form where they can modify settings for a new simulation.
    /// </summary>
    public partial class NewSimulationForm : Form
    {
        #region Private Members

        private Settings m_simulationSettings = new Settings();

        #endregion
        #region Properties

        /// <summary>
        ///     Gets the settings class configured by this form.
        /// </summary>
        public Settings SimulationSettings
        {
            get { return m_simulationSettings; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="settings">Initial value of configurable settings.</param>
        public NewSimulationForm(Settings settings)
        {
            m_simulationSettings = settings;
            InitializeComponent();
        }

        /// <summary>
        ///     Validates database settings to make sure we can connect to it and its setup correctly.
        /// </summary>
        private bool ValidateDatabase()
        {
            DBConnection conn = new DBConnection();

            Logger.Info("Attempting to connect to database on " + m_simulationSettings.DatabaseHostname + ":" + m_simulationSettings.DatabasePort, LoggerVerboseLevel.Normal);

            Task<bool> task = conn.ConnectAsync(m_simulationSettings.DatabaseHostname, m_simulationSettings.DatabasePort, m_simulationSettings.DatabaseName, m_simulationSettings.DatabaseUsername, m_simulationSettings.DatabasePassword);
            task.Wait();

            if (task.Result == true)
            {
                Logger.Info("Connected successfully, database information is valid.", LoggerVerboseLevel.Normal);

                conn.DisconnectAsync().Wait();
                return true;
            }
            else
            {
                Logger.Error("Failed to connect to database.", LoggerVerboseLevel.Normal);

                return false;
            }
        }

        #endregion
        #region Events

        /// <summary>
        ///     Invoked when the form loads.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void NewSimulationForm_Load(object sender, EventArgs e)
        {
            settingsPropertyGrid.SelectedObject = m_simulationSettings;
        }

        /// <summary>
        ///     Invoked when the OK button is clicked.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void okButton_Click(object sender, EventArgs e)
        {
            okButton.Enabled = false;
            okButton.Text    = "Validating ...";
            Application.DoEvents();

            if (!ValidateDatabase())
            {
                okButton.Enabled = true;
                okButton.Text    = "Begin Simulation";

                MessageBox.Show("The database information appears to be invalid, as a connection could not be made to it, or the schema is not valid.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        /// <summary>
        ///     Invoked when the close button is clicked.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        #endregion
    }
}
