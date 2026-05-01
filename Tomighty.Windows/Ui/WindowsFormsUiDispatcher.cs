using System;
using System.Windows.Forms;

namespace Tomighty.Windows.Ui
{
    internal class WindowsFormsUiDispatcher : IUiDispatcher
    {
        private readonly Control control;

        public WindowsFormsUiDispatcher(Control control)
        {
            this.control = control;
        }

        public bool IsOnUiThread => !control.InvokeRequired;

        public void Post(Action action)
        {
            if (action == null || control.IsDisposed || control.Disposing)
                return;

            if (control.InvokeRequired)
            {
                if (control.IsHandleCreated)
                    control.BeginInvoke(action);

                return;
            }

            action();
        }
    }
}
