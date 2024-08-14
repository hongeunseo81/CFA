using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WheelHetergeneousInspectionSystem.Models;
namespace CFA
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Config config = new Config();
            string path = "C:/Users/HONGEUNSEO/source/repos/CFA/CFA/bin/Debug/config.yml";
            Application.Run(new ConfigForm(path, config));
        }
    }
}
