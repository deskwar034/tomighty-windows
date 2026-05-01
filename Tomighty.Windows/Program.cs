//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System;
using System.Windows.Forms;

namespace Tomighty.Windows
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => HandleUnhandledException(args.ExceptionObject);
            Application.ThreadException += (sender, args) => HandleUnhandledException(args.Exception);

            try
            {
                Application.Run(new TomightyApplication());
            }
            catch(Exception e)
            {
                Application.Run(new ErrorReportWindow(e));
            }
        }

        private static void HandleUnhandledException(object exceptionObject)
        {
            var exception = exceptionObject as Exception;
            var description = exception != null
                ? exception.ToString()
                : "Unhandled non-Exception object: " + (exceptionObject ?? "<null>");

            new ErrorReportWindow(description).Show();
        }
    }
}
