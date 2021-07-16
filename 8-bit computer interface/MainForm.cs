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

namespace EightBitInterface
{
    public partial class MainForm : Form
    {
        SerialPort myPort;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                foreach (string s in SerialPort.GetPortNames())
                {
                    PortsCombo.Items.Add(s);
                }
                PortsCombo.SelectedIndex = PortsCombo.Items.Count - 1;

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
                    myPort = new SerialPort(PortsCombo.SelectedItem.ToString(), 115200);
                    myPort.DataReceived += new SerialDataReceivedEventHandler(MyPort_DataReceived);
                    myPort.ReadTimeout = 2000;
                    myPort.WriteTimeout = 2000;
                    myPort.Open();
                    OutputRichtext.Text += "Connected!\n";
                    connectionStatusPictureBox.BackColor = Color.Green;
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
                        if (OutputRichtext.Lines.Length > 100)
                        {
                            List<string> lines = OutputRichtext.Lines.ToList();
                            lines.RemoveAt(0);
                            OutputRichtext.Lines = lines.ToArray();
                        }

                        string stmp = myPort.ReadExisting();
                        OutputRichtext.Text += stmp;
                        OutputRichtext.SelectionStart = OutputRichtext.Text.Length;
                        OutputRichtext.ScrollToCaret();
                        UpdateBusDisplay(stmp);
                        UpdateControlDisplay(stmp);
                        UpdateOutputDisplay(stmp);
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


        void UpdateOutputDisplay(string stmp)
        {
            try
            {
                int startingLoc = stmp.IndexOf("[");
                int closingLoc = stmp.IndexOf("]");
                if (startingLoc > 0 && closingLoc > 0)
                {
                    OutputLabel.Text = stmp.Substring(startingLoc+1, closingLoc - startingLoc-1);
                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }

        }

        private void UpdateControlDisplay(string stmp)
        {
            try
            {
                Color setColor = Color.Blue;
                if (stmp.Substring(0, 3) == "Bus")
                {
                    if (stmp.Substring(22, 1) == "1")
                    {
                        ControlHalt.BackColor = setColor;
                    }
                    else
                    {
                        ControlHalt.BackColor = Color.LightGray;
                    }

                    if (stmp.Substring(23, 1) == "1")
                    {
                        ControlMemAddrIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlMemAddrIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(24, 1) == "1")
                    {
                        ControlMemIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlMemIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(25, 1) == "1")
                    {
                        ControlMemOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlMemOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(26, 1) == "1")
                    {
                        ControlInstRegOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlInstRegOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(27, 1) == "1")
                    {
                        ControlInstRegIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlInstRegIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(28, 1) == "1")
                    {
                        ControlARegIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlARegIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(29, 1) == "1")
                    {
                        ControlARegOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlARegOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(30, 1) == "1")
                    {
                        ControlALUOUT.BackColor = setColor;
                    }
                    else
                    {
                        ControlALUOUT.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(31, 1) == "1")
                    {
                        ControlALUSubtract.BackColor = setColor;
                    }
                    else
                    {
                        ControlALUSubtract.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(32, 1) == "1")
                    {
                        ControlBRegIn.BackColor = setColor;
                    }
                    else
                    {
                        ControlBRegIn.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(33, 1) == "1")
                    {
                        ControlOutVal.BackColor = setColor;
                    }
                    else
                    {
                        ControlOutVal.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(34, 1) == "1")
                    {
                        ControlCounterEnable.BackColor = setColor;
                    }
                    else
                    {
                        ControlCounterEnable.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(35, 1) == "1")
                    {
                        ControlCounterOut.BackColor = setColor;
                    }
                    else
                    {
                        ControlCounterOut.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(36, 1) == "1")
                    {
                        ControlJump.BackColor = setColor;
                    }
                    else
                    {
                        ControlJump.BackColor = Color.LightGray;
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
                if (stmp.Substring(0, 3) == "Bus")
                {
                    if (stmp.Substring(4, 1) == "1")
                    {
                        bus1.BackColor = Color.Red;
                    }
                    else
                    {
                        bus1.BackColor = Color.LightGray;
                    }

                    if (stmp.Substring(5, 1) == "1")
                    {
                        bus2.BackColor = Color.Red;
                    }
                    else
                    {
                        bus2.BackColor = Color.LightGray;
                    }

                    if (stmp.Substring(6, 1) == "1")
                    {
                        bus3.BackColor = Color.Red;
                    }
                    else
                    {
                        bus3.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(7, 1) == "1")
                    {
                        bus4.BackColor = Color.Red;
                    }
                    else
                    {
                        bus4.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(8, 1) == "1")
                    {
                        bus5.BackColor = Color.Red;
                    }
                    else
                    {
                        bus5.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(9, 1) == "1")
                    {
                        bus6.BackColor = Color.Red;
                    }
                    else
                    {
                        bus6.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(10, 1) == "1")
                    {
                        bus7.BackColor = Color.Red;
                    }
                    else
                    {
                        bus7.BackColor = Color.LightGray;
                    }
                    if (stmp.Substring(11, 1) == "1")
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
                if(StartMonitorButton.Text=="Stop &Monitor")
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

        private void StartMonitorButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (StartMonitorButton.Text == "Start &Monitor")
                {
                    myPort.Write("M");
                    StartMonitorButton.Text = "Stop &Monitor";
                    ProgramRAMButton.Enabled = false;
                }
                else
                {
                    myPort.Write("X");
                    StartMonitorButton.Text = "Start &Monitor";
                    ProgramRAMButton.Enabled = false;
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
                newRAMvals += Mem0000.Text;
                newRAMvals += Mem0001.Text;
                newRAMvals += Mem0010.Text;
                newRAMvals += Mem0011.Text;
                newRAMvals += Mem0100.Text;
                newRAMvals += Mem0101.Text;
                newRAMvals += Mem0110.Text;
                newRAMvals += Mem0111.Text;
                newRAMvals += Mem1000.Text;
                newRAMvals += Mem1001.Text;
                newRAMvals += Mem1010.Text;
                newRAMvals += Mem1011.Text;
                newRAMvals += Mem1100.Text;
                newRAMvals += Mem1101.Text;
                newRAMvals += Mem1110.Text;
                newRAMvals += Mem1111.Text;
                if (MessageBox.Show("About to write to RAM. Board clock mode should be set to manual, memory program mode should be set to enabled, memory address dip switches should be up (on), and memory value dip switches should be up (on).\n\n Would you like to procced?", "RAM update", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    myPort.Write("C");
                    System.Threading.Thread.Sleep(1000);
                    myPort.WriteLine(newRAMvals);
                }
            }
            catch (Exception xcp)
            {
                MessageBox.Show(xcp.Message, "Ya'...., something failed...");
            }

        }

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

        private void MemXXXX_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar!='1' && e.KeyChar!='0' && e.KeyChar != ((char)Keys.Back))
            {
                e.Handled = true;
            }

        }

        private void MemXXXX_Leave(object sender, EventArgs e)
        {
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
    }
}
