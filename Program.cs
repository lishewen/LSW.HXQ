﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace 混淆器
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //MessageBox.Show("这是试用版");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
