using System;

namespace Tomighty.Windows.Ui
{
    internal interface IUiDispatcher
    {
        void Post(Action action);
        bool IsOnUiThread { get; }
    }
}
