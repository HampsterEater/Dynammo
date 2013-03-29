/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dynammo.Arbitrator;
using Dynammo.Common;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     This class is responsible for hosting an instance of an arbitrator used by the simulator.
    /// </summary>
    public sealed class ArbitratorSimulatorThread : SimulatorThread
    {
        #region Private Members

        private ArbitratorService m_service = null;

        private int m_lifeOverTimer = Environment.TickCount;

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets the arbitrator service that this thread is hosting.
        /// </summary>
        public ArbitratorService Service
        {
            get { return m_service; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Provides the functionality for this thread.
        /// </summary>
        protected override void EntryPoint()
        {
            Logger.Info("Starting new arbitrator on thread #{0}.", LoggerVerboseLevel.High, Thread.CurrentThread.ManagedThreadId);

            m_service = new ArbitratorService(m_settings);
            m_service.Initialize();
            
            // Calculate when our life is over.
            m_lifeOverTimer = Environment.TickCount + RandomHelper.RandomInstance.Next(m_settings.ArbitratorLifetimeMin, m_settings.ArbitratorLifetimeMax);

            // Keep running until the service finishes or we are aborted.
            while (m_aborting == false)
            {
                // Are we out of time?
                if (Environment.TickCount > m_lifeOverTimer)
                {
                    break;
                }

                m_service.Poll();

                Thread.Sleep(50);
            }

            // Abort the service.
            m_service.Deinitialize();
        }

        #endregion
        #region Public Methods

        #endregion
    }

}
