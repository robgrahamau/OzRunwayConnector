﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
//using Microsoft.FlightSimulator.SimConnect;
using LockheedMartin.Prepar3D.SimConnect;

namespace Ozrunways
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }
        System.Windows.Forms.Timer ozrequest;
        public const int WM_USER_SIMCONNECT = 0x0402;
        // simconnect object
        public SimConnect simconnect = null;
        bool simrunning = true;
        bool paused = false;
        const int Portnumber = 49002;
        private UDPPer udpconnection = new UDPPer();

        enum DEFINITIONS
        {
            OzRunways,
        }

        enum DATA_REQUESTS
        {      
            REQUEST_OzRunways,
    
        };

        enum GROUP_ID
        {
            ID,
        }

        enum EVENT_ID
        {
            SIMSTART,
            SIMSTOP,
            Sixhz,
            OneSec,
            FourSec,
            ObjectAdded,
            ObjectRemoved,
            DROP,
            Paused,
            Unpaused,

 
        };

        enum NOTIFICATION_GROUPS
        {
            GROUP0,
            GROUP1,
        };

 
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct OZRUNWAYS
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String title;
            public double latitude;
            public double latitudeg;
            public double longitude;
            public double longitudeg;
            public double altitude;
            public double altitudef;
            public double altitudeg;
            public double heading;
            public double headingm;
            public double headinggt;
            public double headinggm;
            public double groundtrack;
            public double pitch;
            public double bank;
            public double Vx;
            public double Vz;
            public double Vy;
        }

        // Simconnect client will send a win32 message when there is 
        // a packet to process. ReceiveMessage must be called to
        // trigger the events. This model keeps simconnect processing on the main thread.
        protected override void DefWndProc(ref Message m)
        {
            try
            {
                if (m.Msg == WM_USER_SIMCONNECT)
                {
                    if (simconnect != null)
                    {
                        simconnect.ReceiveMessage();
                    }
                }
                else
                {
                    base.DefWndProc(ref m);
                }
            }
            catch (COMException ex)
            {
                closeconnection();
            }
            catch (Exception ex)
            {
                closeconnection();
            }
        }

        /// <summary>
        /// Happens when we get a message from simconnect saying we opened a connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            textBox1.Text = "CONNECTED TO THE SIM";
        }

        void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
           
            closeconnection();
           

        }
        /// <summary>
        /// closes the SimConnect Connection fully.
        /// </summary>
        /// <returns></returns>
        public void closeconnection()
        {
            try
            {
                if (simconnect != null)
                {
                    try
                    {
                        //  Stop the flight timer
                        ozrequest.Enabled = false;
                        ozrequest.Stop();
                        // Check and disable the traffic Service if it's running!

                        // now we want to turn on the events.
                        simconnect.SetSystemEventState(EVENT_ID.SIMSTART, SIMCONNECT_STATE.OFF);
                        simconnect.SetSystemEventState(EVENT_ID.SIMSTOP, SIMCONNECT_STATE.OFF);
                        simconnect.SetSystemEventState(EVENT_ID.Sixhz, SIMCONNECT_STATE.OFF);
                        simconnect.SetSystemEventState(EVENT_ID.OneSec, SIMCONNECT_STATE.OFF);
                        simconnect.SetSystemEventState(EVENT_ID.ObjectRemoved, SIMCONNECT_STATE.OFF);
                        simconnect.SetSystemEventState(EVENT_ID.ObjectAdded, SIMCONNECT_STATE.OFF);
                        simconnect.SetSystemEventState(EVENT_ID.Paused, SIMCONNECT_STATE.OFF);
                        simconnect.SetSystemEventState(EVENT_ID.Unpaused, SIMCONNECT_STATE.OFF);

                    }
                    catch (COMException ex)
                    {
                        MessageBox.Show("Error Happened:" + ex.ToString(), "Error Stopping Events", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                    catch (Exception ex)
                    {

                    }
                    try
                    {

                        // UnSubscribe from the System Events
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.Sixhz);
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.SIMSTART);
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.SIMSTOP);
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.OneSec);
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.ObjectAdded);
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.ObjectRemoved);
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.Paused);
                        simconnect.UnsubscribeFromSystemEvent(EVENT_ID.Unpaused);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error Happened: " + ex.ToString(), "Error Unsubscribe Events", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    udpconnection.closesend();
                    simconnect.Dispose();
                    simconnect = null;
                    Application.DoEvents();
                    button1.Text = "Not Connected";
                    textBox1.Text = "We are not connected to the sim";
                    textBox3.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                ozrequest.Enabled = false;
                ozrequest.Stop();
                udpconnection.closesend();
                simconnect.Dispose();

            }
        }

        public void connect()
        {
  
            if (simconnect == null)
            {
                try
                {
                    // the constructor is similar to SimConnect_Open in the native API
                    simconnect = new SimConnect("OzRunways Connecter", this.Handle, WM_USER_SIMCONNECT, null, 0);
                    initDataRequest();
                    button1.Text = "Connected";
                    textBox3.Enabled = false;
                    textBox1.Text = "We are Connected to the sim";
                }
                catch (COMException ex)
                {
                    textBox1.Text = "Unable to communicate with the Sim!";
                    ozrequest.Enabled = false;
                    ozrequest.Stop();
                }

            }
            else
            {
                ozrequest.Enabled = false;
                ozrequest.Stop();
                closeconnection();
                textBox3.Enabled = true;
            }


        }
        private void initDataRequest()
        {
            try
            {
                // listen to connect and quit msgs
              
                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);
              
                // listen to exceptions
              
                simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);
                // Listen to events
              
                simconnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(simconnect_OnRecvEvent);
              
                simconnect.OnRecvAssignedObjectId += new SimConnect.RecvAssignedObjectIdEventHandler(simconnect_assigned_object_ID);
                simconnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simconnect_OnRecvSimobjectDataBytype);
              
                simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(simconnect_OnRecvSimobjectData);
                simconnect.OnRecvAssignedObjectId += new SimConnect.RecvAssignedObjectIdEventHandler(simconnect_OnRecvAssignedObjectId);
                // Subscribe to System Events
                simconnect.SubscribeToSystemEvent(EVENT_ID.Sixhz, "6HZ");
                simconnect.SubscribeToSystemEvent(EVENT_ID.SIMSTART, "SimStart");
                simconnect.SubscribeToSystemEvent(EVENT_ID.SIMSTOP, "SimStop");
                simconnect.SubscribeToSystemEvent(EVENT_ID.OneSec, "1sec");
                simconnect.SubscribeToSystemEvent(EVENT_ID.FourSec, "4sec");
                simconnect.SubscribeToSystemEvent(EVENT_ID.ObjectAdded, "ObjectAdded");
                simconnect.SubscribeToSystemEvent(EVENT_ID.ObjectRemoved, "ObjectRemoved");
                simconnect.SubscribeToSystemEvent(EVENT_ID.Paused, "paused");
                simconnect.SubscribeToSystemEvent(EVENT_ID.Unpaused, "Unpaused");
              
                
                // Define a data Structure for OzRunways 
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "GPS POSITION LAT", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "GPS POSITION LON", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Plane Altitude", "Meters", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Plane Altitude", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "GPS POSITION ALT", "Meters", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Plane Heading Degrees True", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "PLANE HEADING DEGREES MAGNETIC", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "GPS GROUND TRUE HEADING", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "GPS GROUND MAGNETIC TRACK", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "GPS GROUND TRUE TRACK", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Plane Pitch Degrees", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "Plane Bank Degrees", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "VELOCITY BODY X", "Meters per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "VELOCITY BODY Z", "Meters per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.OzRunways, "VELOCITY BODY Y", "Meters per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<OZRUNWAYS>(DEFINITIONS.OzRunways);
                                
                simconnect.SetSystemEventState(EVENT_ID.Sixhz, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENT_ID.OneSec, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENT_ID.SIMSTART, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENT_ID.SIMSTOP, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENT_ID.ObjectRemoved, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENT_ID.ObjectAdded, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENT_ID.Paused, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENT_ID.Unpaused, SIMCONNECT_STATE.ON);
                
            }
            catch (COMException ex)
            {
                textBox1.Text = "Unable to communicate with the Sim!";
                closeconnection();
            }
            catch (Exception Ex)
            {
                textBox1.Text = "Unable to communicate with the Sim!";
                closeconnection();
            }
        }
        public void requestozrunwaysdata()
        {
            try
            {
                simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_OzRunways, DEFINITIONS.OzRunways, 0, SIMCONNECT_SIMOBJECT_TYPE.USER); // we want the Ozrunways Data too 
            }
            catch (Exception ex)
            {
                textBox1.Text = "Unable to communicate with the sim";
                closeconnection();

            }
        }

        void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            switch (data.dwException)
            {
                case (uint)SIMCONNECT_EXCEPTION.CREATE_OBJECT_FAILED:
                    

                    break;
                default:
                    
                    break;
            }

        }

        void simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            switch (recEvent.uEventID)
            {
                // we do this on simstart
                case (uint)EVENT_ID.SIMSTART:
                    
                    simrunning = true;
                    break;
                // we do this on sim stop.
                case (uint)EVENT_ID.SIMSTOP:
                    
                    simrunning = false;
                    break;
                // every 6hz we do this.
                case (uint)EVENT_ID.Sixhz:

                    HeartBeat6hz();
                    

                    break;
                // every second we do this.
                case (uint)EVENT_ID.OneSec:
                    
                
                    //dropupdate();
                    break;
                // if we have a drop event (water rudder) we do this.
                case (uint)EVENT_ID.DROP:
                    
                    break;
                case (uint)EVENT_ID.ObjectAdded:
                    //displayText("Object was added:" + recEvent.uEventID + " ");
                    break;
                case (uint)EVENT_ID.ObjectRemoved:
                    //displayText("Object was Removed:" + recEvent.uEventID);
                    break;
                case (uint)EVENT_ID.Paused:
                    
                    paused = true;
                    break;
                case (uint)EVENT_ID.Unpaused:
                    
                    paused = false;
                    break;

            }
        }

        public void HeartBeat6hz()
        {
            if (simrunning)
            {
               // requestozrunwaysdata();
                if (ozrequest.Enabled == false)
                {
                    ozrequest.Enabled = true;
                    udpconnection.opensend();
                    ozrequest.Start();

                }

            }
            else
            {
                if (ozrequest.Enabled == true)
                {
                    ozrequest.Enabled = false;
                    udpconnection.closesend();
                    ozrequest.Stop();
                }
            }
        }

        void simconnect_assigned_object_ID(SimConnect sender, SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data)
        {

        }
        void simconnect_OnRecvAssignedObjectId(SimConnect sender, SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data)
        {

        }

        void simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {

            string buf = "";
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_OzRunways:
                    OZRUNWAYS o1 = (OZRUNWAYS)data.dwData[0];
                    if (Properties.Settings.Default.usebroad == true)
                    { 
                        broadcast(o1);
                    }
                    else
                    {
                        sendpacket(o1);
                    }
                    break;
                default:
                    break;
            }
        }
        void simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {

        }



        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }
        void sendpacket(OZRUNWAYS oz)
        {
            try
            {
 //               UdpClient client = new UdpClient();
 //               client.Connect(new System.Net.IPEndPoint(IPAddress.Parse(textBox3.Text), 49002));



                byte[] datapacket = new byte[1024];
                UdpClient server = new UdpClient(textBox3.Text, 49002);


                byte[] combined;
                byte[] header = { 68, 65, 84, 65, 64 };
                byte[] index1 = { 18, 0, 0, 0 };
                byte[] index2 = { 20, 0, 0, 0 };
                byte[] index3 = { 21, 0, 0, 0 };
                byte[] blank = { 0, 0, 0, 0 };
                byte[] ninenine = { 0, 192, 121, 196 };
                
                byte[] pitch = BitConverter.GetBytes(Convert.ToSingle(oz.pitch));//Encoding.UTF8.GetBytes(string.Format("{0:F2}",oz.pitch));
                byte[] roll = BitConverter.GetBytes(Convert.ToSingle(oz.bank)); // Encoding.UTF8.GetBytes(string.Format("{0:F2}", oz.bank));
                byte[] trueheading = BitConverter.GetBytes(Convert.ToSingle(oz.heading)); // Encoding.UTF8.GetBytes(string.Format("{0:F2}", oz.heading));
                byte[] magheading = BitConverter.GetBytes(Convert.ToSingle(oz.headingm)); // Encoding.UTF8.GetBytes(string.Format("{0:F2}", oz.headingm));
                byte[] lat_d = BitConverter.GetBytes(Convert.ToSingle(oz.latitude)); // Encoding.UTF8.GetBytes(string.Format("{0:F2}", oz.latitude));
                byte[] lon_d = BitConverter.GetBytes(Convert.ToSingle(oz.longitude));// Encoding.UTF8.GetBytes(string.Format("{0:F2}", oz.longitude));
                byte[] alt_ft = BitConverter.GetBytes(Convert.ToSingle(oz.altitudef)); // Encoding.UTF8.GetBytes(string.Format("{0:F2}", oz.altitude));
                byte[] alt_m = BitConverter.GetBytes(Convert.ToSingle(oz.altitude)); //Encoding.UTF8.GetBytes(string.Format("{0:F2}",oz.altitude));
                byte[] onrwy = BitConverter.GetBytes(Convert.ToSingle(0)); //Encoding.UTF8.GetBytes("0");
                byte[] alt_ind = BitConverter.GetBytes(Convert.ToSingle(oz.altitudef)); //Encoding.UTF8.GetBytes(string.Format("{0:F2}",oz.altitudef));
                byte[] lat_S = BitConverter.GetBytes(Convert.ToSingle(100)); //Encoding.UTF8.GetBytes("100");
                byte[] lon_W = BitConverter.GetBytes(Convert.ToSingle(100)); //Encoding.UTF8.GetBytes("100");
                byte[] posX = ninenine;
                byte[] posY = ninenine;
                byte[] posZ = ninenine;
                byte[] vX = BitConverter.GetBytes(Convert.ToSingle(oz.Vx)); //Encoding.UTF8.GetBytes(string.Format("{0:F2}",oz.Vx));
                byte[] vY = BitConverter.GetBytes(Convert.ToSingle(oz.Vy)); //Encoding.UTF8.GetBytes(string.Format("{0:F2}",oz.Vy));
                byte[] vZ = BitConverter.GetBytes(Convert.ToSingle(oz.Vz)); //Encoding.UTF8.GetBytes(string.Format("{0:F2}",oz.Vz));
                byte[] dist_ft = ninenine;
                byte[] dist_m = ninenine;
                if (Properties.Settings.Default.usegps == false)
                {
                    combined = Combine(header, index1, pitch, roll, trueheading, magheading, blank, blank, blank, blank, index2, lat_d, lon_d, alt_ft, alt_m, onrwy, alt_ind, lat_S, lon_W, index3, posX, posY, posZ, vX, vY, vZ, dist_ft, dist_m);
                    
                }
                else { 
                //byte[] header1 = Encoding.UTF8.GetBytes("XGPS"); // Encode the header.
                byte[] lon_g = BitConverter.GetBytes(Convert.ToSingle(oz.longitudeg)); // GPS Long.
                byte[] lat_g = BitConverter.GetBytes(Convert.ToSingle(oz.latitudeg)); // GPS lat.
                byte[] elv_g = BitConverter.GetBytes(Convert.ToSingle(oz.altitudeg)); // GPS alt.
                //combined = Combine(header1, lon_g, lat_g, elv_g, blank, blank);
                //server.Send(combined, combined.Length);
                //byte[] header2 = Encoding.UTF8.GetBytes("XATT");// encode the header.
                byte[] hdg = BitConverter.GetBytes(Convert.ToSingle(oz.headinggt));
                byte[] hdgm = BitConverter.GetBytes(Convert.ToSingle(oz.headinggm));
                //combined = Combine(header2, hdg, blank, blank, blank, blank, blank, vX, vY, vZ, blank, blank, blank);
                combined = Combine(header, index1, pitch, roll, hdg, hdgm, blank, blank, blank, blank, index2, lat_g, lon_g, alt_ft, elv_g, onrwy, alt_ind, lat_S, lon_W, index3, posX, posY, posZ, vX, vY, vZ, dist_ft, dist_m);
                
                }
                server.Send(combined, combined.Length);
            }
            catch (Exception ex)
            {
                textBox1.Text = "Unable to communicate with the Ipad! Please check your Ip";
            }
            

        }

        void broadcast(OZRUNWAYS oz)
        {
            try{
                string prepack1 = "";
                string prepack2 = "";
                //Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, 49002);
                //IPEndPoint iep2 = new IPEndPoint(IPAddress.Parse("10.0.0.255"), 49002);
                if (Properties.Settings.Default.usegps == true)
                { 
                    prepack1 = string.Format("XGPS ,{0:F6},{1:F6},{2:F4},180.0000,0.0000", oz.longitudeg, oz.latitudeg, oz.altitudeg);
                    prepack2 = string.Format("XATT ,{0:F1},-1.7,-0.0000,0.0000,0.0000,0.0,{1:F1},{2:F1},{3:F1},0.00,1.00,0.00", oz.headinggt, oz.Vx, oz.Vy, oz.Vz);
                }
                else
                {
                    prepack1 = string.Format("XGPS,{0:F6},{1:F6},{2:F4},180.0000,0.0000", oz.longitude, oz.latitude, oz.altitude);
                    prepack2 = string.Format("XATT,{0:F1},-1.7,-0.0000,0.0000,0.0000,0.0,{1:F1},{2:F1},{3:F1},0.00,1.00,0.00", oz.heading, oz.Vx, oz.Vy, oz.Vz);
                }
                udpconnection.send(prepack1);
                udpconnection.send(prepack2);
                //sock.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.Broadcast, 1);
                //sock.EnableBroadcast = true;
                //sock.SendTo(packet1, iep1);
                //sock.SendTo(packet2, iep1);
                
           
            textBox1.Text = "Broadcast Packet Sent";
            }
            catch (Exception ex)
            {
                textBox1.Text = "We had an Error!:"+ ex.ToString();
            }
        }
      
        private void button1_Click(object sender, EventArgs e)
        {
            if (simconnect == null)
            {
                connect();
            }
            else
            {
                closeconnection();
            }
        }

       
         void  oztick(object sender, EventArgs e)
        {
            requestozrunwaysdata();
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ipadip = textBox3.Text;
            Properties.Settings.Default.Save();
            updatevalues();
        }
        private void updatevalues()
        {
            if (checkBox2.Checked == true)
            {
                label1.Visible = false;
                textBox3.Visible = false;
            }
            else if (checkBox2.Checked == false)
            {
                label1.Visible = true;
                textBox3.Visible = true;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox3.Text = Properties.Settings.Default.ipadip;
            checkBox1.Checked = Properties.Settings.Default.usegps;
            checkBox2.Checked = Properties.Settings.Default.usebroad;
            updatevalues();
            button1.Text = "Not Connected";
            textBox1.Text = "Welcome. Version 1.0.6";
            ozrequest = new System.Windows.Forms.Timer();
            ozrequest.Interval= 100;
            ozrequest.Enabled = false;
            ozrequest.Tick += new EventHandler(oztick);
            ozrequest.Stop();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeconnection();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.usegps = checkBox1.Checked;
            updatevalues();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.usebroad = checkBox2.Checked;
            updatevalues();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("FSX/P3D To OzRunways Connector is a freeware simulation connector to OzRunways Pty Ltd's IPad based EFB" + Environment.NewLine + "OzRunways is Copyright and Trademarked by OzRunways Pty Ltd, their name is used with permission. " + Environment.NewLine + "FSX/P3D To OzRunway Connector created 2016 Robert Graham, if you have any issues please email: Rob@RobGraham.info");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    

    class UDPPer
    {
        const int PORT_NUMBER = 49002;
        Thread t = null;
        private readonly UdpClient udp = new UdpClient(PORT_NUMBER);
        IAsyncResult ar_ = null;
        private static string recievepacket;
        private static int lastpacket = 0;
        private static int lastpacketrequested = 0;
        public UdpClient client = new UdpClient();
        public static int LASTPACKET
        {
            get { return lastpacket; }
        }
        public static string RECIEVEPACKET
        {
            get {
                lastpacketrequested = lastpacket;
                return recievepacket; }
        }

        public void start()
        {
            if (t !=null)
            {
                throw new Exception("Already Started, Stop First");
            }
            StartListening();
        }

        public void stop()
        {
            try
            {
                udp.Close();
                lastpacket = 0;
            }
            catch { }
        }
        private void StartListening()
        {
            ar_ = udp.BeginReceive(Receive, new Object());
        }

        private void Receive(IAsyncResult ar)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, PORT_NUMBER);
            byte[] bytes = udp.EndReceive(ar, ref ip);
            recievepacket = Encoding.UTF8.GetString(bytes);
            if ((recievepacket != null) || (recievepacket != ""))
            { 
                lastpacket++;
            }
        }
        public void opensend()
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, PORT_NUMBER);
            
            client.Connect(ip);
        }
        public void send(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            
            client.Send(bytes, bytes.Length);
            
        }

        public void closesend()
        {
            client.Close();
        }
    }
}
