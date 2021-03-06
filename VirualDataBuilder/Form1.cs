﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace VirtualDataBuilder
{
    using System.Diagnostics;

    public partial class Form1 : Form
    {
        private string filePath = string.Empty;

        private string[] heatFiles;
        private string[] controllerFiles;
        private string[] controllerFiles2;
        public Form1()
        {
            InitializeComponent();


        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            this.textBox1.AllowDrop = true;
            textBox1.DragEnter += panel1_DragEnter;
            textBox1.DragDrop += panel1_DragDrop;

            this.textBox2.AllowDrop = true;
            textBox2.DragEnter += panel2_DragEnter;
            textBox2.DragDrop += panel2_DragDrop;

            this.textBox3.AllowDrop = true;
            textBox3.DragEnter += panel2_DragEnter;
            textBox3.DragDrop += panel3_DragDrop;
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
            this.controllerFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var item in this.controllerFiles)
            {
                textBox2.Text += item + Environment.NewLine;
            }
        }

        private void panel3_DragDrop(object sender, DragEventArgs e)
        {
            textBox3.Text = string.Empty;
            this.controllerFiles2 = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var item in this.controllerFiles2)
            {
                textBox3.Text += item + Environment.NewLine;
            }
        }
        private void panel2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //string heatFile = @"E:\temp\20150521\heater.csv";
            //string controllerFile = @"E:\temp\20150521\controller.csv";
            try
            {
                txtMsg.ResetForeColor();
                this.Cursor = Cursors.WaitCursor;
                Builder builder = new Builder();
                txtMsg.Text = "处理中...";
                string resultFile = string.Empty;
                if (radioButton1.Checked)
                {
                    resultFile = builder.Build(controllerFiles[0], heatFiles[0]);
                }
                else
                {
                    if (controllerFiles2 == null || controllerFiles2.Length == 0)
                    {
                        txtMsg.Text = "缺少第二个控制器文件";
                        return;
                    }
                    resultFile = builder.Build2(controllerFiles[0], controllerFiles2[0], heatFiles[0]);
                }

                txtMsg.Text = "完成." + Environment.NewLine + resultFile;
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                textBox3.Visible = false;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                textBox3.Visible = true;
            }
        }


    }
}
