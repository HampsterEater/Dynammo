/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

// If defined the simulator threads we be run on their own seperate 
// threads, if not, they will be run single-threaded.
//#define SIMULATOR_MULTITHREADED

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynammo.Common;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     This class is responsible for spawning and looking after an individual thread that hosts
    ///     a service that the simulator is running (arbitrator/peer/etc).
    /// </summary>
    public abstract class SimulatorThread
    {
        #region Private Members

        protected Thread    m_thread    = null;
        
        protected Settings  m_settings  = null;

        protected bool      m_suspended = false;

        protected bool      m_aborting  = false;

        protected object    m_metaData  = null;

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets or sets meta data about this thread.
        /// </summary>
        public object MetaData
        {
            get { return m_metaData; }
            set { m_metaData = value; }
        }

        /// <summary>
        ///     Returns a unique identifier for this thread.
        /// </summary>
        public int ThreadID
        {
            get
            {
                return m_thread == null ? 0 : m_thread.ManagedThreadId;
            }
        }

        /// <summary>
        ///     Returns true if the thread is currently running.
        /// </summary>
        public bool IsRunning
        {
            get 
            {
                return m_thread == null ? false : m_thread.IsAlive; 
            }
        }

        /// <summary>
        ///     Returns true if the thread is currently paused.
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return m_thread == null ? false : m_suspended;
            }
        }

        /// <summary>
        ///     Returns true if the thread is currently aborting.
        /// </summary>
        public bool IsAborting
        {
            get
            {
                return m_aborting;
            }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Overridden in derived classes to provide the functionality for the thread we are hosting.
        /// </summary>
        protected abstract void EntryPoint();
    
        #endregion
        #region Public Methods

        /// <summary>
        ///     Begins running the thread.
        /// </summary>
        public void Run(Settings settings)
        {
            // Abort any old threads.
            if (m_thread != null)
            {
                m_thread.Abort();
                m_thread = null;
            }

            // Initialize our new thread.
            m_settings  = settings;
            m_suspended = false;
            m_aborting  = false;

            m_thread = new Thread(EntryPoint);
            //m_thread.Priority = ThreadPriority.AboveNormal;
            m_thread.SetApartmentState(ApartmentState.STA);
            m_thread.IsBackground = true;
            m_thread.Start();
        }

        /// <summary>
        ///     Pauses this thread.
        /// </summary>
        public void Pause()
        {
            if (m_thread == null)
            {
                throw new InvalidOperationException("Thread has not been started.");
            }

#pragma warning disable 0618
            m_thread.Suspend();
#pragma warning restore 0618

            m_suspended = true;
        }

        /// <summary>
        ///     Resumes this thread.
        /// </summary>
        public void Resume()
        {
            if (m_thread == null)
            {
                throw new InvalidOperationException("Thread has not been started.");
            }

#pragma warning disable 0618
            m_thread.Resume();
#pragma warning restore 0618

            m_suspended = false;
        }

        /// <summary>
        ///     Aborts this thread gracefully.
        /// </summary>
        public void Abort()
        {
            m_aborting = true;
        }

        #endregion
    }

}
