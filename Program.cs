using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VoxelModelEditor.Document.Models;
using VoxelModelEditor.Windows;

namespace VoxelModelEditor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var main = new MainWindow();

            var test = new TestWindow();
            test.MdiParent = main;
            test.Show();

            var form = new DisplayForm();
            form.Model.LoadFromModel(HiyoriModel.CreateModel());
            form.MdiParent = main;
            form.Show();

            Application.Run(main);
        }
    }
}
