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
using Dynammo.Common;
using Dynammo.Networking;
using Dynammo.Client;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     Form used to display the current state of the simulation.
    /// </summary>
    public partial class SimulatorForm : Form
    {
        #region Private Members

        private const int           MAX_LOG_COUNT           = 100;

        private Settings            m_simulationSettings    = new Settings();

        private Simulator           m_simulator             = null;

        private bool                m_waitingForFormClose   = false;

        private int                 m_graphUpdateTimer      = Environment.TickCount;
        private int                 m_graphStartTime        = Environment.TickCount;

        #endregion
        #region Private Methods

        /// <summary>
        ///     Constructor for this form.
        /// </summary>
        /// <param name="simulator">Simulator this form is displaying the state of.</param>
        public SimulatorForm(Simulator simulator)
        {
            m_simulator = simulator;

            InitializeComponent();
        }

        /// <summary>
        ///     Appends new log messages to the logging tree view.
        /// </summary>
        public void UpdateSimulationLog()
        {
            bool updatedList = false;

            // Add most recent logs.
            foreach (string s in Logger.GetRecentLogs())
            {
                TreeNode node = new TreeNode();
                node.Text     = s;

                if (s.Contains("ERROR"))
                {
                    node.ImageIndex         = 2;
                    node.SelectedImageIndex = 2;
                }
                else if (s.Contains("WARNING"))
                {
                    node.ImageIndex         = 1;
                    node.SelectedImageIndex = 1;
                }
                else
                {
                    node.ImageIndex         = 0;
                    node.SelectedImageIndex = 0;
                }

                logTreeView.Nodes.Add(node);

                updatedList = true;
            }

            // Remove old logs from tree view.
            while (logTreeView.GetNodeCount(false) > MAX_LOG_COUNT)
            {
                logTreeView.Nodes.RemoveAt(0);
                updatedList = true;
            }

            // Select latest log.
            if (logTreeView.Nodes.Count > 0 && updatedList == true)
            {
                logTreeView.SelectedNode = logTreeView.Nodes[logTreeView.Nodes.Count - 1];
            }
        }

        /// <summary>
        ///     Removes all nodes that don't belong to anything anymore from a tree view.
        /// </summary>
        /// <param name="nodes">Collection of nodes to look through.</param>
        /// <returns>True if any nodes were removed.</returns>
        private bool RemoveNoneExistingNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode subnode in nodes)
            {
                if (RemoveNoneExistingNodes(subnode.Nodes) == true)
                {
                    return true;
                }

                if (subnode.Tag != null &&
                    m_simulator.ArbitratorStates.Contains(subnode.Tag) == false &&
                    m_simulator.ClientStates.Contains(subnode.Tag) == false)
                {
                    nodes.Remove(subnode);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Adds new nodes to the network state treeview.
        /// </summary>
        public void UpdateNetworkState()
        {
            TreeNode node = null;

            // Add the initial nodes if we haven't already.
            if (networkStateTreeView.Nodes.Count <= 0 && m_simulator.IsRunning == true && m_simulator.IsAborting == false)
            {
                node                    = new TreeNode();
                node.Text               = "Database Server";
                node.SelectedImageIndex = 4;
                node.ImageIndex         = 4;

                networkStateTreeView.Nodes.Add(node);
            }

            // Only load other nodes if we are running/not-aborting.
            if (m_simulator.IsRunning == false || m_simulator.IsAborting == true)
            {
                return;
            }

            // Remove all nodes that don't exist anymore.
            bool listModified = true;
            while (listModified)
            {
                listModified = RemoveNoneExistingNodes(networkStateTreeView.Nodes);
            }

            // Add all arbitrator states.
            foreach (SimulatorArbitratorState arbitrator in m_simulator.ArbitratorStates)
            {
                if (arbitrator.MetaData == null)
                {
                    node                    = new TreeNode();
                    node.Text               = "Arbitrator #" + arbitrator.DatabaseSettings["id"].ToString();
                    node.SelectedImageIndex = 6;
                    node.ImageIndex         = 6;
                    node.Tag                = arbitrator;

                    arbitrator.MetaData     = node;

                    networkStateTreeView.Nodes.Add(node);
                }

                // Highlight the arbitrator if its in control.
                node          = arbitrator.MetaData as TreeNode;

                if (arbitrator.IsMaster == true)
                {
                    if (node.ForeColor != Color.Green)
                    {
                        node.ForeColor = Color.Green;
                        node.Text = "Arbitrator #" + arbitrator.DatabaseSettings["id"].ToString() + " [Master]";
                    }
                }
                else
                {
                    if (node.ForeColor != Color.Black)
                    {
                        node.ForeColor = Color.Black;
                        node.Text = "Arbitrator #" + arbitrator.DatabaseSettings["id"].ToString() + " [Slave]";
                    }
                }

                // Add all clients connected to this arbitrator.
                foreach (SimulatorClientState client in m_simulator.ClientStates)
                {
                    if (client.MetaData == null && (int)client.DatabaseSettings["arbitrator_id"] == (int)arbitrator.DatabaseSettings["id"])
                    {
                        node                    = new TreeNode();
                        node.Text               = "Client #" + client.DatabaseSettings["id"].ToString();
                        node.SelectedImageIndex = 5;
                        node.ImageIndex         = 5;
                        node.Tag                = client;

                        client.MetaData         = node;

                        ((TreeNode)arbitrator.MetaData).Nodes.Add(node);
                    }
                }
            }

            // Show / Hide information panel.
            if (networkStateTreeView.SelectedNode != null)
            {
                networkNodeInfoPanel.Visible = true;

                // Database selected?
                if (networkStateTreeView.SelectedNode.Tag == null)
                {
                    nodeNameAddressLabel.Text   = "Database";
                    nodeUsernameLabel.Text      = ""; 
                    nodeIPAddressLabel.Text     = m_simulationSettings.DatabaseHostname + ":" + m_simulationSettings.DatabasePort;               
                }

                // Arbitrator selected?
                else if (networkStateTreeView.SelectedNode.Tag is SimulatorArbitratorState)
                {
                    SimulatorArbitratorState arbitrator = networkStateTreeView.SelectedNode.Tag as SimulatorArbitratorState;
                    nodeNameAddressLabel.Text           = "Arbitrator #" + arbitrator.DatabaseSettings["id"].ToString();
                    nodeUsernameLabel.Text              = "";
                    nodeIPAddressLabel.Text             = arbitrator.DatabaseSettings["ip_address"].ToString() + ":" + arbitrator.DatabaseSettings["port"].ToString();
                }

                // Or client?
                else if (networkStateTreeView.SelectedNode.Tag is SimulatorClientState)
                {
                    SimulatorClientState client     = networkStateTreeView.SelectedNode.Tag as SimulatorClientState;
                    nodeNameAddressLabel.Text       = "Client #" + client.DatabaseSettings["id"].ToString();
                    nodeUsernameLabel.Text          = client.Account != null ? "Username: " + client.Account.Username : "Logging In";
                    nodeIPAddressLabel.Text         = client.DatabaseSettings["ip_address"].ToString() + ":" + client.DatabaseSettings["port"].ToString();
                }

            }
            else
            {
                networkNodeInfoPanel.Visible = false;
            }
        }

        /// <summary>
        ///     Updates the current window control state based on what the simulation is doing.
        /// </summary>
        private void UpdateWindowState()
        {
            stopSimulationToolStripButton.Enabled  = m_simulator.IsAborting == false && (m_simulator.IsRunning || m_simulator.IsPaused);
            pauseSimulationToolStripButton.Enabled = m_simulator.IsAborting == false && (m_simulator.IsRunning && !m_simulator.IsPaused);
            startSimulationToolStripButton.Enabled = m_simulator.IsAborting == false && (!m_simulator.IsRunning || m_simulator.IsPaused);
        }

        /// <summary>
        ///     Begins the simulation.
        /// </summary>
        private void BeginSimulation()
        {
            if (m_simulator.IsRunning == true && m_simulator.IsPaused == false)
            {
                StopSimulation();
                return;
            }

            if (m_simulator.IsPaused == true)
            {
                m_simulator.Resume();
                Logger.Info("Simulation resumed.", LoggerVerboseLevel.Normal);
            }
            else
            {
                m_simulator.Start(m_simulationSettings);
                Logger.Info("Simulation started.", LoggerVerboseLevel.Normal);
            }

            UpdateWindowState();
        }

        /// <summary>
        ///     Pauses the simulation.
        /// </summary>
        private void PauseSimulation()
        {
            if (!m_simulator.IsRunning)
            {
                return;
            }

            Logger.Info("Simulation paused.", LoggerVerboseLevel.Normal);

            m_simulator.Pause();

            UpdateWindowState();
        }

        /// <summary>
        ///     Stops the simulation.
        /// </summary>
        private void StopSimulation()
        {
            if (!m_simulator.IsRunning)
            {
                return;
            }
            
            Logger.Info("Simulation stopped.", LoggerVerboseLevel.Normal);

            m_simulator.Stop();
            networkStateTreeView.Nodes.Clear();

            UpdateWindowState();
        }

        #endregion
        #region Form Events

        /// <summary>
        ///     Invoked when the form is loaded.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void SimulatorForm_Load(object sender, EventArgs e)
        {
            logVerbosityComboBox.SelectedIndex = (int)Logger.VerbosityLevel;
        }

        /// <summary>
        ///     Invoked when the form is closing.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void SimulatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_simulator.IsRunning == true || m_simulator.IsAborting == true)
            {
                m_waitingForFormClose = true;

                if (m_simulator.IsAborting == false)
                {
                    StopSimulation();
                }

                e.Cancel = true;
                return;
            }
        }

        /// <summary>
        ///     Updates the bandwidth graphs with new data.
        /// </summary>
        private void UpdateGraphs()
        {
            // Only add graph points once a second.
            if (Environment.TickCount - m_graphUpdateTimer < 250)
            {
                return;
            }
            if (m_simulator.IsRunning == false || m_simulator.IsPaused == true)
            {
                return;
            }
            int     elapsed         = Environment.TickCount - m_graphUpdateTimer;
            float   rate_multiplier = 1000.0f / elapsed;
            m_graphUpdateTimer = Environment.TickCount;

            // Only update graphs if we are on the correct tab as its slow.
            if (logWindowTabber.SelectedIndex == 0)
            {
                return;
            }

            // Calculate total bandwidth amounts.
            int total_arbitrator_bandwidth_in = 0;
            int total_arbitrator_bandwidth_out = 0;
            int total_arbitrators = 0;
            int total_client_bandwidth_in = 0;
            int total_client_bandwidth_out = 0;
            int total_clients = 0; 
            int total_super_peers = 0;

            foreach (SimulatorThread thread in m_simulator.Threads)
            {
                ArbitratorSimulatorThread arbitrator = thread as ArbitratorSimulatorThread;
                if (arbitrator != null && arbitrator.Service != null)
                {
                    total_arbitrator_bandwidth_in   += arbitrator.Service.DeltaBandwidthIn;
                    total_arbitrator_bandwidth_out  += arbitrator.Service.DeltaBandwidthOut;
                    total_arbitrators++;
                }

                ClientSimulatorThread client = thread as ClientSimulatorThread;
                if (client != null && client.Service != null)
                {
                    total_client_bandwidth_in       += client.Service.DeltaBandwidthIn;
                    total_client_bandwidth_out      += client.Service.DeltaBandwidthOut;
                    total_clients++;

                    foreach (SuperPeer peer in client.Service.SuperPeers)
                    {
                        if (peer.IsActive == true)
                        {
                            total_super_peers++;
                        }
                    }
                }
            }

            // Work out correct time to add points at.
            if (m_graphStartTime == 0)
            {
                m_graphStartTime = Environment.TickCount;
            }
            int time = (Environment.TickCount - m_graphStartTime) / 1000;

            // Add total points.
            arbitratorBandwidthPanel.AddDataPoint("Total Bandwidth Out (kb/s)", (total_arbitrator_bandwidth_out / 1024.0f) * rate_multiplier, time);
            arbitratorBandwidthPanel.AddDataPoint("Total Bandwidth In (kb/s)", (total_arbitrator_bandwidth_in / 1024.0f) * rate_multiplier, time);
            arbitratorBandwidthPanel.AddDataPoint("Total Arbitrators", total_arbitrators, time);
            arbitratorBandwidthPanel.AddDataPoint("Total Clients", total_clients, time);

            clientBandwidthPanel.AddDataPoint("Total Bandwidth Out (kb/s)", (total_client_bandwidth_out / 1024.0f) * rate_multiplier, time);
            clientBandwidthPanel.AddDataPoint("Total Bandwidth In (kb/s)", (total_client_bandwidth_in / 1024.0f) * rate_multiplier, time);
            clientBandwidthPanel.AddDataPoint("Total Clients", total_clients, time);
            clientBandwidthPanel.AddDataPoint("Total Super Peers", total_super_peers, time);

            arbitratorBandwidthPanel.Update();
            clientBandwidthPanel.Update();
        }

        /// <summary>
        ///     Invoked when the update timer ticks, updates the current state of the window.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void updateTimer_Tick(object sender, EventArgs e)
        {
            // Waiting to close?
            if (m_simulator.IsRunning == false && m_simulator.IsAborting == false && m_waitingForFormClose == true)
            {
                this.Close();
                return;
            }

            // Update simulation log list.
            UpdateSimulationLog();

            // Update window state.
            UpdateWindowState();

            // Update network node list.
            UpdateNetworkState();

            // Update graphs.
            UpdateGraphs();

            // Repaint the world.
            worldPanel.Visible = (m_simulator != null && m_simulator.Settings != null);
            worldPanel.Refresh();
        }

        /// <summary>
        ///     Invoked when the user clicks the start simulation button.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void startSimulationToolStripButton_Click(object sender, EventArgs e)
        {
            if (m_simulator.IsPaused == true)
            {
                BeginSimulation();
            }
            else
            {
                NewSimulationForm sim = new NewSimulationForm(m_simulationSettings);
                if (sim.ShowDialog(this) == DialogResult.OK)
                {
                    m_simulationSettings = sim.SimulationSettings;
                    BeginSimulation();
                }
            }
        }

        /// <summary>
        ///     Invoked when the network tree is right clicked.!
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void networkStateTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (networkStateTreeView.SelectedNode == null ||
                networkStateTreeView.SelectedNode.Text == "Database Server")
            {
                return;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                takeControlToolStripMenuItem.Enabled = (networkStateTreeView.SelectedNode.Tag is SimulatorClientState);
                disconnectToolStripMenuItem.Enabled  = true;

                treeContextMenuStrip.Show((Control)sender, e.X, e.Y);
            }
        }

        /// <summary>
        ///     Invoked when the user clicks the take control menu item.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void takeControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (networkStateTreeView.SelectedNode.Tag is SimulatorClientState)
            {
                SimulatorClientState client = networkStateTreeView.SelectedNode.Tag as SimulatorClientState;

                SimulatorClientState clientState = networkStateTreeView.SelectedNode.Tag as SimulatorClientState;
                foreach (SimulatorThread thread in m_simulator.Threads)
                {
                    if (thread is ClientSimulatorThread)
                    {
                        ClientSimulatorThread clientThread = thread as ClientSimulatorThread;
                        clientThread.PlayerControlled = (clientThread.Service.ClientID == (int)clientState.DatabaseSettings["id"]);
                    }
                }
            }
        }

        /// <summary>
        ///     Invoked when the user clicks the take disconnect menu item.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (networkStateTreeView.SelectedNode.Tag is SimulatorClientState)
            {
                SimulatorClientState clientState = networkStateTreeView.SelectedNode.Tag as SimulatorClientState;
                foreach (SimulatorThread thread in m_simulator.Threads)
                {
                    if (thread is ClientSimulatorThread)
                    {
                        ClientSimulatorThread clientThread = thread as ClientSimulatorThread;
                        if (clientThread.Service.ClientID == (int)clientState.DatabaseSettings["id"])
                        {
                            thread.Abort();
                        }
                    }
                }
            }
            else if (networkStateTreeView.SelectedNode.Tag is SimulatorArbitratorState)
            {
                SimulatorArbitratorState arbitratorState = networkStateTreeView.SelectedNode.Tag as SimulatorArbitratorState;
                foreach (SimulatorThread thread in m_simulator.Threads)
                {
                    if (thread is ArbitratorSimulatorThread)
                    {
                        ArbitratorSimulatorThread arbitratorThread = thread as ArbitratorSimulatorThread;
                        if (arbitratorThread.Service.DatabaseID == (int)arbitratorState.DatabaseSettings["id"])
                        {
                            thread.Abort();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Disables the arrow keys on the network state tree view, as it causes all kinds of crazy when we 
        ///     are directly control a peer.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void networkStateTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Invoked when the user clicks the pause simulation button.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void pauseSimulationToolStripButton_Click(object sender, EventArgs e)
        {
            PauseSimulation();
        }

        /// <summary>
        ///     Invoked when the user clicks the stop simulation button.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void stopSimulationToolStripButton_Click(object sender, EventArgs e)
        {
            StopSimulation();
        }

        /// <summary>
        ///     Invoked when the user changes the log verbosity combobox's selected item.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void logVerbosityComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.VerbosityLevel = (LoggerVerboseLevel)logVerbosityComboBox.SelectedIndex;
        }

        /// <summary>
        ///     Gets position information about a given client state.
        /// </summary>
        private bool GetStateInfo(SimulatorClientState state, out bool registering, out bool unregistering, out float x, out float y, out ClientSimulatorThread realClientThread)
        {
            bool found = false;

            registering = false;
            unregistering = false;
            x = state.Account.PeristentState.X;
            y = state.Account.PeristentState.Y;
            realClientThread = null;

            if (state.LastClientState != null)
            {
                x = state.LastClientState.X;
                y = state.LastClientState.Y;
            }

            foreach (SimulatorThread thread in m_simulator.Threads)
            {
                ClientSimulatorThread clientThread = (thread as ClientSimulatorThread);
                if (clientThread == null || clientThread.Service == null)
                {
                    continue;
                }

                if (clientThread.Service.CurrentZone == null ||
                    clientThread.Service.CurrentZone.ID != (int)state.DatabaseSettings["zone_id"])
                {
                    continue;
                }

                if (clientThread.Service.ClientID == (int)state.DatabaseSettings["id"])
                {
                    registering = clientThread.Service.RegisteringWithSuperPeers;
                    unregistering = clientThread.Service.UnregisteringWithSuperPeers;
                    realClientThread = clientThread;
                }

                // Look to see if the position information for this peer is in this clients world state.
                if (clientThread.Service.WorldState != null)
                {
                    foreach (SuperPeerWorldStatePlayerInfo peerInfo in clientThread.Service.WorldState.Peers)
                    {
                        if (peerInfo.ClientID == (int)state.DatabaseSettings["id"])
                        {
                            x = peerInfo.Account.PeristentState.X;
                            y = peerInfo.Account.PeristentState.Y;
                            state.LastClientState = peerInfo.Account.PeristentState;
                            found = true;
                            break;
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        ///     Paint our current representation of the world.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Arguments describing this event.</param>
        private void worldPanel_Paint(object sender, PaintEventArgs e)
        {
            if (m_simulator == null || m_simulator.Settings == null)
            {
                return;
            }

            // Make sure our panel is the correct size.
            worldPanel.Size = new System.Drawing.Size(m_simulator.Settings.WorldWidth, m_simulator.Settings.WorldHeight);
           
            // Grab all the resources we need.
            Graphics    gfx             = e.Graphics;
            Pen         thinBlackPen    = new Pen(Color.Black, 1);
            Brush       whiteBrush      = new SolidBrush(Color.White);
            Brush       lightBlueBrush  = new SolidBrush(Color.LightBlue);
            Pen         lightGreenPen   = new Pen(Color.LightGreen);
            Brush       redBrush        = new SolidBrush(Color.Red);
            Font        textFont        = new System.Drawing.Font(FontFamily.GenericMonospace, 8, FontStyle.Regular);
            Brush       textBrush       = new SolidBrush(Color.Black);
            Brush       greenTextBrush  = new SolidBrush(Color.LightGreen);
            Brush       blackBrush      = new SolidBrush(Color.Black);
            int         panelWidth      = worldPanel.Width;
            int         panelHeight     = worldPanel.Height;

            SimulatorArbitratorState selectedArbitrator = networkStateTreeView.SelectedNode == null ? null : networkStateTreeView.SelectedNode.Tag as SimulatorArbitratorState;
            SimulatorClientState     selectedClient     = networkStateTreeView.SelectedNode == null ? null : networkStateTreeView.SelectedNode.Tag as SimulatorClientState;

            // Clear the background.
            gfx.Clear(Color.White);

            // Render the zones.
            foreach (Zone zone in m_simulator.ZoneGrid.Zones)
            {
                if (zone.IsLeafZone == true)
                {
                    int zone_x = 0;
                    int zone_y = 0;
                    int zone_w = 0;
                    int zone_h = 0;
                    m_simulator.ZoneGrid.CalculateZoneBounds(zone, 0, 0, worldPanel.Width, worldPanel.Height, out zone_x, out zone_y, out zone_w, out zone_h);

                    gfx.DrawRectangle(thinBlackPen, zone_x, zone_y, zone_w, zone_h);
                    gfx.DrawString(zone.ID.ToString(), textFont, textBrush, zone_x + 2, zone_y + 2);
                }
            }

            // Render clients.
            foreach (SimulatorClientState state in m_simulator.ClientStates)
            {
                if (state.Account == null)
                {
                    continue;
                }

                // Get remote position.
                float x = state.Account.PeristentState.X;
                float y = state.Account.PeristentState.Y;
                bool registering = false;
                bool unregistering = false;

                // Create brushs we will be using.
                int elapsed_time = Environment.TickCount - state.CreateTime;
                float delta = Math.Min(1.0f, elapsed_time / 2000.0f);

                Random r                    = new Random(state.Account.ID);
                Brush stateBrush            = new SolidBrush(Color.FromArgb((int)(delta * 255.0f), r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)));
                Pen   statePen              = new Pen(Color.FromArgb((int)(delta * 255.0f), 0, 0, 0));
                Brush stateGreenTextBrush   = new SolidBrush(Color.FromArgb((int)(delta * 255.0f), 0, 255, 0));
                Brush stateBlackBrush       = new SolidBrush(Color.FromArgb((int)(delta * 255.0f), 0, 0, 0));

                // If we are simulating locally, lets try and get the position
                // this client is currently being simulated at.
                ClientSimulatorThread realClientThread = null;
                bool found = GetStateInfo(state, out registering, out unregistering, out x, out y, out realClientThread);

                string  txt                     = state.Account.Username;
                SizeF   txt_size                = gfx.MeasureString(txt, textFont);
                float   radius                  = 5;

                // Draw ID of user.
                txt = state.DatabaseSettings["id"].ToString();
                if (registering == true)
                {
                    txt += "+";
                }
                if (unregistering == true)
                {
                    txt += "-";
                }

                txt_size = gfx.MeasureString(txt, textFont);
                gfx.FillRectangle(stateBlackBrush, x - (txt_size.Width / 2), y - (txt_size.Height + radius), txt_size.Width, txt_size.Height);
                gfx.DrawString(txt, textFont, stateGreenTextBrush, x - (txt_size.Width / 2), y - (txt_size.Height + radius));

                if (realClientThread != null && realClientThread.PlayerControlled == true)
                {
                    txt = "Player Controlled";
                    txt_size = gfx.MeasureString(txt, textFont);
                    gfx.FillRectangle(stateBlackBrush, x - (txt_size.Width / 2), y - (txt_size.Height + radius) - 20, txt_size.Width, txt_size.Height);
                    gfx.DrawString(txt, textFont, stateGreenTextBrush, x - (txt_size.Width / 2), y - (txt_size.Height + radius) - 20);
                }

                // Work out if we should highlight this client.
                bool highlight = false;
                if (selectedArbitrator != null)
                {
                    if ((int)selectedArbitrator.DatabaseSettings["id"] == (int)state.DatabaseSettings["arbitrator_id"])
                    {
                        highlight = true;
                    }
                }
                else if (selectedClient != null)
                {
                    if ((int)selectedClient.DatabaseSettings["id"] == (int)state.DatabaseSettings["id"])
                    {
                        highlight = true;
                    }
                }

                // Draw client.
                if (highlight == true)
                {
                    gfx.FillEllipse(redBrush, x - radius, y - radius, radius * 2, radius * 2);
                }
                else
                {
                    gfx.FillEllipse(stateBrush, x - radius, y - radius, radius * 2, radius * 2);
                }
                gfx.DrawEllipse(statePen, x - radius, y - radius, radius * 2, radius * 2);

                // If we are highlighting, draw lines to super peers.
                if (highlight == true && realClientThread != null && selectedClient != null)
                {
                    foreach (ZoneSuperPeer peer in realClientThread.Service.RegisteredSuperPeers)
                    {
                        float start_x = x;
                        float start_y = y;
                        float super_x = 0;
                        float super_y = 0;

                        ClientSimulatorThread realSubClientThread = null;
                        bool subRegistering = false;
                        bool subUnregistering = false;

                        foreach (SimulatorClientState peer_state in m_simulator.ClientStates)
                        {
                            if (peer_state.Account != null &&
                                (int)peer_state.DatabaseSettings["id"] == peer.ClientID)
                            {
                                if (GetStateInfo(peer_state, out subRegistering, out subUnregistering, out super_x, out super_y, out realSubClientThread))
                                {
                                    break;
                                }
                            }
                        }

                        if (super_x != 0 || super_y != 0)
                        {
                            gfx.DrawLine(lightGreenPen, start_x, start_y, super_x, super_y);
                        }
                    }
                }
            }

            // Draw outline.
            gfx.DrawRectangle(thinBlackPen, 0, 0, panelWidth - 1, panelHeight - 1);
        }

        #endregion
    }
}
