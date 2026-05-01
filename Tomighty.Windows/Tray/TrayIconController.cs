//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System;
using System.Drawing;
using System.Windows.Forms;
using Tomighty.Events;
using Tomighty.Windows.Timer;
using Tomighty.Windows.Ui;

namespace Tomighty.Windows.Tray
{
    internal class TrayIconController : IDisposable
    {
        private readonly NotifyIcon notifyIcon;
        private readonly TimerWindowPresenter timerWindowPresenter;
        private readonly IUiDispatcher uiDispatcher;

        public TrayIconController(NotifyIcon notifyIcon, TimerWindowPresenter timerWindowPresenter, IEventHub eventHub, IUiDispatcher uiDispatcher)
        {
            this.notifyIcon = notifyIcon;
            this.timerWindowPresenter = timerWindowPresenter;
            this.uiDispatcher = uiDispatcher;

            notifyIcon.Click += OnNotifyIconClick;

            eventHub.Subscribe<TimerStarted>(OnTimerStarted);
            eventHub.Subscribe<TimerStopped>(OnTimerStopped);
        }
        
        public void Dispose()
        {
            notifyIcon.Container.Dispose();
        }

        private void OnNotifyIconClick(object sender, EventArgs @event)
        {
            if (((MouseEventArgs)@event).Button == MouseButtons.Left)
            {
                timerWindowPresenter.Toggle(Cursor.Position);
            }
        }

        private void OnTimerStarted(TimerStarted @event)
        {
            uiDispatcher.Post(() =>
            {
                if (notifyIcon == null || notifyIcon.Icon == null && notifyIcon.Container == null) return;
                notifyIcon.Icon = GetIcon(@event.IntervalType);
            });
        }

        private void OnTimerStopped(TimerStopped @event)
        {
            uiDispatcher.Post(() =>
            {
                if (notifyIcon == null || notifyIcon.Icon == null && notifyIcon.Container == null) return;
                notifyIcon.Icon = Properties.Resources.icon_tomato_white;
            });
        }

        private Icon GetIcon(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return Properties.Resources.icon_tomato_red;
            if (intervalType == IntervalType.ShortBreak) return Properties.Resources.icon_tomato_green;
            if (intervalType == IntervalType.LongBreak) return Properties.Resources.icon_tomato_blue;
            throw new ArgumentException($"Invalid interval type: {intervalType}");
        }
    }
}
