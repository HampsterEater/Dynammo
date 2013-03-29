/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------
///  Dynammo.Simulator:
///  
///     This application allows testing of how the Dynammo 
///     architecture works. It permits the users to setup and run
///     the full arhitecture whilst viewing and logging statistics.
/// -----------------------------------------------------------------

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dynammo.Common;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     The main class for our program, contains the entry point and main message loop.
    /// </summary>
    public static class Program
    {
        #region Private Members

        private static SimulatorForm m_simulatorForm;
        private static Simulator     m_simulator;

        #endregion
        #region Public Methods

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Logger.Info("Simulator started.", LoggerVerboseLevel.Normal);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize simulator and the form controlling it.
            m_simulator     = new Simulator();
            m_simulatorForm = new SimulatorForm(m_simulator);

            m_simulatorForm.Show();

            // Keep running until the simulator form is closed.
            while (m_simulatorForm.Visible)
            {
                // Poll simulator.
                m_simulator.Poll();

                // Post UI events.
                Application.DoEvents();

                // We yield here so we don't end up using an entire CPU core on
                // our message loop.
                Thread.Sleep(1);
            }
        }

        #endregion
    }
}
