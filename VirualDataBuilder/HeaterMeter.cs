using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VirtualDataBuilder
{
    public partial class HeaterMeter : Form
    {
        //private string filePath = string.Empty;

        private string[] heatFiles;

        private string outputFile;
        public HeaterMeter()
        {
            InitializeComponent();
            this.textBox1.AllowDrop = true;
            textBox1.DragEnter += panel1_DragEnter;
            textBox1.DragDrop += panel1_DragDrop;

            this.textBox2.AllowDrop = true;
            textBox2.DragEnter += panel2_DragEnter;
            textBox2.DragDrop += panel2_DragDrop;
        }

        void panel1_DragDrop(object sender, DragEventArgs e)
        {
            textBox1.Text = string.Empty;
            this.heatFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var item in this.heatFiles)
            {
                textBox1.Text += item + Environment.NewLine;
            }
        }

        void panel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void panel2_DragDrop(object sender, DragEventArgs e)
        {
            textBox2.Text = string.Empty;
            this.outputFile = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

            textBox2.Text += outputFile;

        }
      
        private void panel2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                txtMsg.ResetForeColor();
                this.Cursor = Cursors.WaitCursor;
                txtMsg.Text = "处理中...";
                //heatFiles = new[] { @"E:\temp\20150526\meter_1.csv", @"E:\temp\20150526\meter_2.csv" };
                //outputFile = @"E:\temp\20150526\output.csv";
                HeaterMeterBuilder builder = new HeaterMeterBuilder(heatFiles, outputFile);
                txtMsg.Text = builder.Build();
            }
            catch (Exception ex)
            {
                txtMsg.ForeColor = Color.DarkRed;
                txtMsg.Text = "处理失败！" + Environment.NewLine + ex.Message;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
}
