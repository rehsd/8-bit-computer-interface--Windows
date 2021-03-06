/*
    This application is used in conjunction with an Arduino Mega to set RAM values, 
    read the bus, control bits, output, and clock speed of an 8-bit computer 
    based on Ben Eater's 8-bit computer reference design.   
    See https://github.com/rehsd/8-bit-computer-interface--Arduino.

    Last updated July 21, 2021.

    The higher the 8-bit computer clock, the more monitoring will struggle to keep up read all of the data.
    At 115200 baud, a clock of ~100Hz is the ceiling. 
    At 921600 baud, monitoring does pretty well keeping up. However, writing to the Arduino/8-bit computer will struggle.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Configuration;
using System.Management;

namespace EightBitInterface
{
    public partial class MainForm : Form
    {
        SerialPort myPort;
        bool bHex = true;
        bool bNowMonitoring = false;

        //const short MAX_OUTPUT_LINES = 50;    //Replaced with UpDown
        const int CONNECTION_RATE = 115200; //Match this to the same const in the Arduino Mega code.
                                            //Monitoring runs OK at 921600. Writing to the Mega is flaky at this speed.
                                            //Potential speed options: 921600, 460800, 230400, 115200
        public MainForm()
        {
            InitializeComponent();
        }

        void PopulateSerialPorts()
        {
            try
            {
                ManagementObjectCollection mbsList = null;
                ManagementObjectSearcher mbs = new ManagementObjectSearcher("Select DeviceID, Description From Win32_SerialPort");
                mbsList = mbs.Get();

                foreach (ManagementObject mo in mbsList)
                {
                    PortsCombo.Items.Add(mo["DeviceID"].ToString() + ": " + mo["Description"].ToString());
                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                PopulateSerialPorts();

                foreach (string profileSet in ConfigurationManager.AppSettings)
                {
                    loadSetCombo.Items.Add(profileSet);
                }

                bus1.BackColor = Color.LightGray;
                bus2.BackColor = Color.LightGray;
                bus3.BackColor = Color.LightGray;
                bus4.BackColor = Color.LightGray;
                bus5.BackColor = Color.LightGray;
                bus6.BackColor = Color.LightGray;
                bus7.BackColor = Color.LightGray;
                bus8.BackColor = Color.LightGray;

                ControlHalt.BackColor = Color.LightGray;
                ControlMemAddrIn.BackColor = Color.LightGray;
                ControlMemIn.BackColor = Color.LightGray;
                ControlMemOut.BackColor = Color.LightGray;
                ControlInstRegOut.BackColor = Color.LightGray;
                ControlInstRegIn.BackColor = Color.LightGray;
                ControlARegIn.BackColor = Color.LightGray;
                ControlARegOut.BackColor = Color.LightGray;
                ControlALUOUT.BackColor = Color.LightGray;
                ControlALUSubtract.BackColor = Color.LightGray;
                ControlBRegIn.BackColor = Color.LightGray;
                ControlOutVal.BackColor = Color.LightGray;
                ControlCounterEnable.BackColor = Color.LightGray;
                ControlCounterOut.BackColor = Color.LightGray;
                ControlJump.BackColor = Color.LightGray;
                ControlFlags.BackColor = Color.LightGray;

                //Default programming values
                Mem0000.Text = "00011111";  //load from 15
                Mem0001.Text = "11100000";  //out
                Mem0010.Text = "00101110";  //add from 14
                Mem0011.Text = "11100000";  //out
                Mem0100.Text = "00101110";  //subtract from 13
                Mem0101.Text = "11100000";  //out
                Mem0110.Text = "00101110";  //add from 14
                Mem0111.Text = "11100000";  //out
                Mem1000.Text = "00101110";  //subtract from 13
                Mem1001.Text = "11100000";  //out
                Mem1010.Text = "00101110";  //subtract from 13
                Mem1011.Text = "11100000";  //out
                Mem1100.Text = "01100010";  //out
                Mem1101.Text = "00000000";  //7
                Mem1110.Text = "00000010";  //6
                Mem1111.Text = "00000001";  //5

                SetFormControls(false);

                Advise();
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (ConnectButton.Text == "&Connect")
                {
                    string s = PortsCombo.SelectedItem.ToString();
                    s = s.Substring(0, s.IndexOf(":"));
                    myPort = new SerialPort(s, CONNECTION_RATE); 
                    myPort.ReadTimeout = 5000;
                    myPort.WriteTimeout = 5000;
                    myPort.Open();
                    connectionStatusPictureBox.BackColor = Color.Green;
                    System.Threading.Thread.Sleep(1000);
                    myPort.DataReceived += new SerialDataReceivedEventHandler(MyPort_DataReceived);
                    myPort.DiscardInBuffer();
                    OutputRichtext.Text += "\nConnected at " + myPort.BaudRate.ToString() + "!\n";
                    connectionSpeedLabel.Text = s + " @ " + myPort.BaudRate.ToString();
                    myPort.Write("X");
                    ConnectButton.Text = "&Disconnect";
                    SetFormControls(true);
                }
                else
                {
                    if (myPort.IsOpen)
                    {
                        myPort.Close();
                    }
                    connectionStatusPictureBox.BackColor = Color.Red;
                    OutputRichtext.Text += "Disconnected!\n";
                    ConnectButton.Text = "&Connect";
                    SetFormControls(false);
                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }
        }

        private void MyPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate
                    {
                    if (OutputRichtext.Lines.Length > logMax.Value)
                        {
                            List<string> lines = OutputRichtext.Lines.ToList();
                            lines.RemoveRange(0, lines.Count - (int)logMax.Value);
                            OutputRichtext.Lines = lines.ToArray();
                        }

                        string stmp = "";
                        if (!bNowMonitoring)
                        {
                            stmp = myPort.ReadExisting();
                        }
                        else
                        {
                            //string stmp = myPort.ReadExisting();
                            stmp = myPort.ReadLine();
                        }
                        
                        bool bSkipped = false;
                        if (bHex)
                        {
                            //Convert complete hex string to complete binary string
                            stmp = ConvertHexBufferToBinaryBuffer(stmp, ref bSkipped);
                        }


                        if (displayLogCheckBox.Checked)
                        {
                            if (bNowMonitoring && bHex)
                            {
                                OutputRichtext.Text += stmp + "\r";
                            }
                            else { 
                                OutputRichtext.Text += stmp;
                            }

                            OutputRichtext.SelectionStart = OutputRichtext.Text.Length;
                            OutputRichtext.ScrollToCaret();
                        }

                        if (bNowMonitoring && !bSkipped) 
                        { 
                            UpdateBusDisplay(stmp);
                            UpdateControlDisplay(stmp);
                            UpdateOutputDisplay(stmp);
                            UpdateClockDisplay(stmp);
                        }
                    }));
                }
                else
                {
                    //OutputRichtext.Text = myPort.ReadLine();
                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }
        }

        void Advise()
        {
            OutputRichtext.Text += "Monitoring with clock speeds above 100Hz is problematic, unless running very high serial speeds (>400,000 bps).";
        }

        string ConvertHexBufferToBinaryBuffer(string hexBuffer, ref bool skipped)
        {
            //sample in --> BBCCCCFFCCCC
            //sample out -->  Bus: 00000000  Control: 0000010000000010  Out: 00010110[255]  Clock: 9999

            try
            {
                //TO DO Better validation / exception handling
                if (hexBuffer.Contains(">") || hexBuffer.Contains(":") || hexBuffer.Contains(">") || hexBuffer.Contains("[") || hexBuffer.Contains(" '"))
                {
                    skipped = true;
                    return hexBuffer;
                }

                hexBuffer = hexBuffer.Replace("\r", "");

                if (hexBuffer.Length != 12)
                {
                    skipped = true;
                    return hexBuffer;
                }

                skipped = false;
                string sTemp = "";
                string sBus = hexBuffer.Substring(0, 2);
                string sControl = hexBuffer.Substring(2, 4);
                string sOut = hexBuffer.Substring(6, 2);
                string sClock = hexBuffer.Substring(8, 4);

                sBus = Convert.ToString(Convert.ToInt32(sBus, 16), 2);
                sControl = Convert.ToString(Convert.ToInt32(sControl, 16), 2);
                string sOutDec = Convert.ToString(Convert.ToInt32(sOut, 16), 10);
                sOut = Convert.ToString(Convert.ToInt32(sOut, 16), 2);
                sClock = Convert.ToString(Convert.ToInt32(sClock, 16), 10);

                sBus = sBus.PadLeft(8, '0');
                sControl = sControl.PadLeft(16, '0');
                sOut = sOut.PadLeft(8, '0');

                sTemp = "Bus:" + sBus + "  Control:" + sControl + "  Out:" + sOut + " [" + sOutDec + "]" + "  Clock:" + sClock;

                return sTemp;
            }
            catch
            {
                //Do nothing for now...
                return "";
            }
        }

        void UpdateClockDisplay(string stmp)
        {
            try
            {
                int startingLoc = stmp.IndexOf("Clock:");
                if (startingLoc > 0)
                {
                    //int closingLoc = stmp.IndexOf('\n', startingLoc);
                    //TO DO Verify that the portion of the string to be read actually is there (i.e., don't try to read the next 8 bits if only a few are availablle)
                    //clockLabel.Text = stmp.Substring(startingLoc + 6, closingLoc - startingLoc - 7) + "Hz";
                    short clockSpeed = short.Parse(stmp.Substring(startingLoc + 6, stmp.Length - startingLoc - 6));
                    clockLabel.Text = clockSpeed + "Hz";
                    if(clockSpeed > 100)
                    {
                        clockLabel.ForeColor = Color.Red;
                    }
                    else
                    {
                        clockLabel.ForeColor = Color.DimGray;
                    }
                }
            }
            catch
            {
                //Ignore for now
                //MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }
        }

        void UpdateOutputDisplay(string stmp)
        {
            try
            {
                int startingLoc = stmp.IndexOf("[");
                int closingLoc = stmp.IndexOf("]");
                if (startingLoc > 0 && closingLoc > 0)
                {
                    OutputLabel.Text = stmp.Substring(startingLoc + 1, closingLoc - startingLoc - 1);
                }
            }
            catch
            {
                //Ignore for now
                //MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }

        }

        private void UpdateControlDisplay(string stmp)
        {
            try
            {
                Color setColor = Color.Blue;
                int controlStart = stmp.IndexOf("Control");
                if (controlStart > -1)
                {
                    if (stmp.Substring(controlStart + 8, 1) == "1")
                    {
                        ControlHalt.BackColor = setColor;
                    }
                    else
                    {
                        ControlHalt.BackColor = Color.LightGray;
                    }

                    if (stmp.Substring(controlStart + 9, 1) == "1")
                    {
                        ControlMemAddrIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlMemAddrIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 10, 1) == "1")
                    {
                        ControlMemIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlMemIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 11, 1) == "1")
                    {
                        ControlMemOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlMemOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 12, 1) == "1")
                    {
                        ControlInstRegOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlInstRegOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 13, 1) == "1")
                    {
                        ControlInstRegIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlInstRegIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 14, 1) == "1")
                    {
                        ControlARegIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlARegIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 15, 1) == "1")
                    {
                        ControlARegOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlARegOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 16, 1) == "1")
                    {
                        ControlALUOUT.BackColor = setColor;
                    }
                    else
                    {
                        ControlALUOUT.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 17, 1) == "1")
                    {
                        ControlALUSubtract.BackColor = setColor;
                    }
                    else
                    {
                        ControlALUSubtract.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 18, 1) == "1")
                    {
                        ControlBRegIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlBRegIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 19, 1) == "1")
                    {
                        ControlOutVal.BackColor = setColor;
                    }
                    else
                    {
                        ControlOutVal.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 20, 1) == "1")
                    {
                        ControlCounterEnable.BackColor = setColor;
                    }
                    else
                    {
                        ControlCounterEnable.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 21, 1) == "1")
                    {
                        ControlCounterOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlCounterOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 22, 1) == "1")
                    {
                        ControlJump.BackColor = setColor;
                    }
                    else
                    {
                        ControlJump.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(controlStart + 23, 1) == "1")
                    {
                        ControlFlags.BackColor = setColor;
                    }
                    else
                    {
                        ControlFlags.BackColor = Color.LightGray;
                    }

                }
            }
            catch
            {
                //do nothing... pretty fancy, huh?! :)
            }
        }

        private void UpdateBusDisplay(string stmp)
        {
            try
            {
                int busStart = stmp.IndexOf("Bus");
                if (busStart>-1)
                {
                    if (stmp.Substring(busStart+4, 1) == "1")
                    {
                        bus1.BackColor = Color.Red;
                    }
                    else
                    {
                        bus1.BackColor = Color.LightGray;
                    }

                    if (stmp.Substring(busStart + 5, 1) == "1")
                    {
                        bus2.BackColor = Color.Red;
                    }
                    else
                    {
                        bus2.BackColor = Color.LightGray;
                    }

                    if (stmp.Substring(busStart + 6, 1) == "1")
                    {
                        bus3.BackColor = Color.Red;
                    }
                    else
                    {
                        bus3.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(busStart + 7, 1) == "1")
                    {
                        bus4.BackColor = Color.Red;
                    }
                    else
                    {
                        bus4.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(busStart + 8, 1) == "1")
                    {
                        bus5.BackColor = Color.Red;
                    }
                    else
                    {
                        bus5.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(busStart + 9, 1) == "1")
                    {
                        bus6.BackColor = Color.Red;
                    }
                    else
                    {
                        bus6.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(busStart + 10, 1) == "1")
                    {
                        bus7.BackColor = Color.Red;
                    }
                    else
                    {
                        bus7.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(busStart + 11, 1) == "1")
                    {
                        bus8.BackColor = Color.Red;
                    }
                    else
                    {
                        bus8.BackColor = Color.LightGray;
                    }
                }
              
            }
            catch
            {
                //do nothing
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            try
            {
                myPort.Write(CommandTextbox.Text);
                CommandTextbox.Clear();
                CommandTextbox.Focus();
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (StartMonitorButton.Text == "Stop &Monitor")
                {
                    myPort.Write("X");
                    StartMonitorButton.Text = "Start &Monitor";
                    ProgramRAMButton.Enabled = false;
                }
                if (myPort != null && myPort.IsOpen)
                {
                    myPort.Close();
                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }

        }

        private static void UpdateOpCodeLabel(TextBox t, Label opCodeLabel)
        {
            try
            {
                switch (t.Text.Substring(0, 4))
                {
                    case "0001":    //LDA
                        {
                            opCodeLabel.Text = "LDA";
                            break;
                        }
                    case "0010":    //ADD
                        {
                            opCodeLabel.Text = "ADD";
                            break;
                        }
                    case "0011":    //  SUB
                        {
                            opCodeLabel.Text = "SUB";
                            break;
                        }
                    case "0100":    //  STA
                        {
                            opCodeLabel.Text = "STA";
                            break;
                        }
                    case "0101":    //LDI
                        {
                            opCodeLabel.Text = "LDI";
                            break;
                        }
                    case "0110":    //JMP
                        {
                            opCodeLabel.Text = "JMP";
                            break;
                        }
                    case "0111":    //JC
                        {
                            opCodeLabel.Text = "JC";
                            break;
                        }
                    case "1000":    //JZ
                        {
                            opCodeLabel.Text = "JZ";
                            break;
                        }
                    case "1110":    //OUT
                        {
                            opCodeLabel.Text = "OUT";
                            break;
                        }
                    case "1111":    //HLT
                        {
                            opCodeLabel.Text = "HLT";
                            break;
                        }
                    default:
                        {
                            opCodeLabel.Text = "NonOp";
                            break;
                        }
                }
            }
            catch (Exception)
            {
                //do nothing for now...
            }
        }

        #region MemXXXX_TextChanged event handlers

        private void Mem0000_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0000);
        }

        private void Mem0001_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0001);

        }

        private void Mem0010_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0010);

        }

        private void Mem011_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0011);
        }

        private void Mem0100_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0100);

        }

        private void Mem0101_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0101);

        }

        private void Mem0110_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0110);

        }

        private void Mem0111_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code0111);

        }

        private void Mem1000_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1000);

        }

        private void Mem1001_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1001);

        }

        private void Mem1010_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1010);

        }

        private void Mem1011_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1011);

        }

        private void Mem1100_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1100);

        }

        private void Mem1101_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1101);

        }

        private void Mem1110_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1110);

        }

        private void Mem1111_TextChanged(object sender, EventArgs e)
        {
            UpdateOpCodeLabel((TextBox)sender, code1111);

        }
        #endregion

        private void StartMonitorButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (StartMonitorButton.Text == "Start &Monitor")
                {
                    bNowMonitoring = true;

                    if(useHexCheckBox.Checked)
                    {
                        bHex = true;
                        myPort.Write("H");
                    }
                    else
                    {
                        bHex = false;
                        myPort.Write("M");
                    }

                    StartMonitorButton.Text = "Stop &Monitor";
                    ProgramRAMButton.Enabled = false;
                }
                else
                {
                    myPort.Write("X");
                    StartMonitorButton.Text = "Start &Monitor";
                    ProgramRAMButton.Enabled = true;
                    bNowMonitoring = false;

                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }

        }

        private void ProgramRAMButton_Click(object sender, EventArgs e)
        {
            try
            {
                string newRAMvals = "";
                newRAMvals += Mem0000.Text + ":";
                newRAMvals += Mem0001.Text + ":";
                newRAMvals += Mem0010.Text + ":";
                newRAMvals += Mem0011.Text + ":";
                newRAMvals += Mem0100.Text + ":";
                newRAMvals += Mem0101.Text + ":";
                newRAMvals += Mem0110.Text + ":";
                newRAMvals += Mem0111.Text + ":";
                newRAMvals += Mem1000.Text + ":";
                newRAMvals += Mem1001.Text + ":";
                newRAMvals += Mem1010.Text + ":";
                newRAMvals += Mem1011.Text + ":";
                newRAMvals += Mem1100.Text + ":";
                newRAMvals += Mem1101.Text + ":";
                newRAMvals += Mem1110.Text + ":";
                newRAMvals += Mem1111.Text;
                if (MessageBox.Show("About to write to RAM. Board clock mode should be set to manual, memory program mode should be set to enabled, memory address dip switches should be up (on), and memory value dip switches should be up (on).\n\n Would you like to procced?\n\n\n" + newRAMvals.Replace(":","\n"), "RAM update", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    myPort.Write("C");
                    System.Threading.Thread.Sleep(1000);
                    myPort.WriteLine(newRAMvals.Replace(":",string.Empty));
                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }

        }

        #region codeXXXX_Click event handlers:

        //TODO Consolidate the following Click event handlers...
        private void code0000_Click(object sender, EventArgs e)
        {
            if (code0000.Text=="NonOp")
            {
                UpdateOpCodeLabel(Mem0000, code0000);
            }
            else
            {
                code0000.Text = "NonOp";
            }
        }

        private void code0001_Click(object sender, EventArgs e)
        {
            if (code0001.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem0001, code0001);
            }
            else
            {
                code0001.Text = "NonOp";
            }
        }

        private void code0010_Click(object sender, EventArgs e)
        {
            if (code0010.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem0010, code0010);
            }
            else
            {
                code0010.Text = "NonOp";
            }
        }

        private void code0011_Click(object sender, EventArgs e)
        {
            if (code0011.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem0011, code0011);
            }
            else
            {
                code0011.Text = "NonOp";
            }
        }

        private void code0100_Click(object sender, EventArgs e)
        {
            if (code0100.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem0100, code0100);
            }
            else
            {
                code0100.Text = "NonOp";
            }
        }

        private void code0101_Click(object sender, EventArgs e)
        {
            if (code0101.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem0101, code0101);
            }
            else
            {
                code0101.Text = "NonOp";
            }
        }

        private void code0110_Click(object sender, EventArgs e)
        {
            if (code0110.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem0110, code0110);
            }
            else
            {
                code0110.Text = "NonOp";
            }
        }

        private void code0111_Click(object sender, EventArgs e)
        {
            if (code0111.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem0111, code0111);
            }
            else
            {
                code0111.Text = "NonOp";
            }
        }

        private void code1000_Click(object sender, EventArgs e)
        {
            if (code1000.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1000, code1000);
            }
            else
            {
                code1000.Text = "NonOp";
            }
        }

        private void code1001_Click(object sender, EventArgs e)
        {
            if (code1001.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1001, code1001);
            }
            else
            {
                code1001.Text = "NonOp";
            }
        }

        private void code1010_Click(object sender, EventArgs e)
        {
            if (code1010.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1010, code1010);
            }
            else
            {
                code1010.Text = "NonOp";
            }
        }

        private void code1011_Click(object sender, EventArgs e)
        {
            if (code1011.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1011, code1011);
            }
            else
            {
                code1011.Text = "NonOp";
            }
        }


        private void code1100_Click(object sender, EventArgs e)
        {
            if (code1100.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1100, code1100);
            }
            else
            {
                code1100.Text = "NonOp";
            }
        }

        private void code1101_Click(object sender, EventArgs e)
        {
            if (code1101.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1101, code1101);
            }
            else
            {
                code1101.Text = "NonOp";
            }
        }

        private void code1110_Click(object sender, EventArgs e)
        {
            if (code1110.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1110, code1110);
            }
            else
            {
                code1110.Text = "NonOp";
            }
        }

        private void code1111_Click(object sender, EventArgs e)
        {
            if (code1111.Text == "NonOp")
            {
                UpdateOpCodeLabel(Mem1111, code1111);
            }
            else
            {
                code1111.Text = "NonOp";
            }
        }

        #endregion

        private void MemXXXX_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Bound to all the MemXXXX textboxes to filter kepresses down to 1, 0, and Backspace
            if(e.KeyChar!='1' && e.KeyChar!='0' && e.KeyChar != ((char)Keys.Back))
            {
                e.Handled = true;
            }
        }

        private void MemXXXX_Leave(object sender, EventArgs e)
        {
            //Bound to all the MemXXXX textboxes to pad contents to 8 bits, if fewer than 8 bits were entered
            TextBox t = (TextBox)(sender);
            if(t.TextLength<8)
            {
                t.Text = t.Text.PadRight(8, '0');
            }
        }

        void SetFormControls(bool enableControls)
        {
            CommandTextbox.Enabled = enableControls;
            SendButton.Enabled = enableControls;
            StartMonitorButton.Enabled = enableControls;
            ProgramRAMButton.Enabled = enableControls;
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            AboutBox a = new AboutBox();
            a.Show();
        }

        private void loadSetCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string vals = ConfigurationManager.AppSettings[loadSetCombo.Text];
                string[] valArray = vals.Split(":");
                Mem0000.Text = valArray[0];
                Mem0001.Text = valArray[1];
                Mem0010.Text = valArray[2];
                Mem0011.Text = valArray[3];
                Mem0100.Text = valArray[4];
                Mem0101.Text = valArray[5];
                Mem0110.Text = valArray[6];
                Mem0111.Text = valArray[7];
                Mem1000.Text = valArray[8];
                Mem1001.Text = valArray[9];
                Mem1010.Text = valArray[10];
                Mem1011.Text = valArray[11];
                Mem1100.Text = valArray[12];
                Mem1101.Text = valArray[13];
                Mem1110.Text = valArray[14];
                Mem1111.Text = valArray[15];
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }
        }

        private void PortsCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConnectButton.Enabled = true;
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            OutputRichtext.Clear();
        }

        private void analyzeButton_Click(object sender, EventArgs e)
        {
            LogicAnalyzer frmLA = new LogicAnalyzer();
            frmLA.Show();
        }
    }
}
