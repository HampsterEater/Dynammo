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
using System.Windows.Input;
using Dynammo.Arbitrator;
using Dynammo.Common;
using Dynammo.Client;
using Dynammo.Networking;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     This class is responsible for hosting an instance of a client used by the simulator.
    /// </summary>
    public sealed class ClientSimulatorThread : SimulatorThread
    {
        #region Private Members

        private GameClientService m_service = null;

        private string m_arbitratorHost;
        private ushort m_arbitratorPort;

        private int m_lifeOverTimer = Environment.TickCount;

        private int m_lastDirectionChange = Environment.TickCount;
        private int m_direction = 0;
        private float m_speed = 0.0f;

        private Zone m_lastMovementZone = null;

        private bool m_playerControlled = false;
        private int m_keyLeft = 0;
        private int m_keyRight = 0;
        private int m_keyUp = 0;
        private int m_keyDown = 0;

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets the game-client service that this thread is hosting.
        /// </summary>
        public GameClientService Service
        {
            get { return m_service; }
        }

        /// <summary>
        ///     Gets the hostname of the arbitrator this client connects.
        /// </summary>
        public string ArbitratorHost
        {
            get { return m_arbitratorHost; }
        }

        /// <summary>
        ///     Gets the port of the arbitrator this client connects.
        /// </summary>
        public ushort ArbitratorPort
        {
            get { return m_arbitratorPort; }
        }

        /// <summary>
        ///     Gets or sets if this client has its movement controlled by the player or not.
        /// </summary>
        public bool PlayerControlled
        {
            get { return m_playerControlled; }
            set { m_playerControlled = value; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Simulates movement of this client around the map.
        /// </summary>
        private void SimulateMovement()
        {
            bool forceMovementUpdate = (m_service.CurrentZone                                         != null &&
                                        m_lastMovementZone                                            != null &&
                                        m_service.CurrentZone.ID                                      != m_lastMovementZone.ID &&
                                        m_service.ConnectedToAllZoneSuperPeers(m_service.CurrentZone) == true ||
                                        m_service.SuperPeersDirty                                     == true);

            if (m_playerControlled == true)
            {
                int keyLeft  = (Keyboard.GetKeyStates(System.Windows.Input.Key.Left)  & KeyStates.Down) > 0 ? 1 : 0;
                int keyRight = (Keyboard.GetKeyStates(System.Windows.Input.Key.Right) & KeyStates.Down) > 0 ? 1 : 0;
                int keyUp    = (Keyboard.GetKeyStates(System.Windows.Input.Key.Up)    & KeyStates.Down) > 0 ? 1 : 0;
                int keyDown  = (Keyboard.GetKeyStates(System.Windows.Input.Key.Down)  & KeyStates.Down) > 0 ? 1 : 0;

                if ((keyLeft != m_keyLeft || keyRight != m_keyRight || keyUp != m_keyUp || keyDown != m_keyDown) ||
                    forceMovementUpdate == true)
                {
                    m_lastMovementZone = m_service.CurrentZone;
                    m_service.SetMovementVector(keyRight - keyLeft, keyDown - keyUp, m_settings.ClientMovementSpeedMax);
                }

                m_keyLeft = keyLeft;
                m_keyRight = keyRight;
                m_keyUp = keyUp;
                m_keyDown = keyDown;
            }
            else
            {
                if (Environment.TickCount > m_lastDirectionChange || forceMovementUpdate == true)
                {
                    m_lastDirectionChange = Environment.TickCount + (int)(m_settings.ClientDirectionChangeTimeMin + (RandomHelper.RandomInstance.NextDouble() * (m_settings.ClientDirectionChangeTimeMax - m_settings.ClientDirectionChangeTimeMin)));
                    m_direction = RandomHelper.RandomInstance.Next(0, 4);
                    m_speed = (float)(m_settings.ClientMovementSpeedMin + (RandomHelper.RandomInstance.NextDouble() * (m_settings.ClientMovementSpeedMax - m_settings.ClientMovementSpeedMin)));
                    m_lastMovementZone = m_service.CurrentZone;

                    switch (m_direction)
                    {
                        case 0: // Left
                            {
                                m_service.SetMovementVector(-1, 0, m_speed);
                                break;
                            }
                        case 1: // Right
                            {
                                m_service.SetMovementVector(1, 0, m_speed);
                                break;
                            }
                        case 2: // Up
                            {
                                m_service.SetMovementVector(0, -1, m_speed);
                                break;
                            }
                        case 3: // Down
                            {
                                m_service.SetMovementVector(0, 1, m_speed);
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        ///     Provides the functionality for this thread.
        /// </summary>
        protected override void EntryPoint()
        {
            Logger.Info("Starting new game client on thread #{0}.", LoggerVerboseLevel.High, Thread.CurrentThread.ManagedThreadId);

            // Setup the service.
            m_service = new GameClientService(m_settings, m_arbitratorHost, m_arbitratorPort);
            m_service.Initialize();

            // Calculate when our life is over.
            m_lifeOverTimer = Environment.TickCount + RandomHelper.RandomInstance.Next(m_settings.ClientLifetimeMin, m_settings.ClientLifetimeMax);

            bool connectedLastFrame = false;

            // Keep running until the service finishes or we are aborted.
            while (m_aborting == false)
            {
                // Are we out of time?
                if (Environment.TickCount > m_lifeOverTimer && 
                    m_playerControlled == false)
                {
                    break;
                }

                // If we are re-connected, do we need to login to our account?
                if (m_service.ConnectedToArbitrator == true && connectedLastFrame == false)
                {
                    LoginToAccount();
                    RegisterAsListening();
                }
                connectedLastFrame = m_service.ConnectedToArbitrator;

                // Simulate movement.
                SimulateMovement();

                // Update service.
                if (m_service.Poll())
                {
                    break;
                }

                Thread.Sleep(16); 
            }

            // Abort the service.
            m_service.Deinitialize();
        }

        /// <summary>
        ///     Registers the simulated client as listening and ready for peer connections.
        /// </summary>
        protected void RegisterAsListening()
        {
            Logger.Info("Attempting to register client as listening for peers ...", LoggerVerboseLevel.High);
            m_service.RegisterAsListening();
        }

        /// <summary>
        ///     Attempts to connect to a random simulator account, or if one dosen't exist, attempts to create it.
        /// </summary>
        protected void LoginToAccount()
        {
            Logger.Info("Attempting to login to randomised simulator account...", LoggerVerboseLevel.High);

            // Try and login to a random account.
            while (m_service.ConnectedToArbitrator == true && m_aborting == false)
            {
                int account_id = RandomHelper.RandomInstance.Next(0, 99999);
                LoginResult result = m_service.Login("simulator_" + account_id, "password");

                // Login success!
                if (result == LoginResult.Success || 
                    result == LoginResult.AlreadyLoggedIn)
                {
                    return;
                }

                // No account with this name? Lets create it then.
                else if (result == LoginResult.AccountNotFound)
                {
                    CreateAccountResult create_result = m_service.CreateAccount("simulator_" + account_id, "password", account_id + "@simulator.dynammo.com");
                    if (create_result == CreateAccountResult.Success)
                    {

                        // Attempt to connect to new account.
                        result = m_service.Login("simulator_" + account_id, "password");
                        if (result == LoginResult.Success)
                        {
                            return;
                        }

                    }
                }

                Thread.Sleep(50);
            }
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs a new instance of this simulator thread.
        /// </summary>
        /// <param name="arbitrator_host">Arbitrator host this client will connect to.</param>
        /// <param name="arbitrator_port">Arbitrator port this client will connect to.</param>
        public ClientSimulatorThread(string arbitrator_host, ushort arbitrator_port)
        {
            m_arbitratorHost = arbitrator_host;
            m_arbitratorPort = arbitrator_port;
        }

        #endregion
    }

}
