//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using Tomighty.Events;
using Tomighty.Windows.Events;
using Windows.UI.Notifications;
using Tomighty.Windows.Ui;

namespace Tomighty.Windows.Notifications
{
    internal class NotificationsPresenter
    {
        private readonly IPomodoroEngine pomodoroEngine;
        private readonly IUserPreferences userPreferences;
        private readonly ToastNotifier toastNotifier = ToastNotificationManager.CreateToastNotifier("Tomighty");
        private readonly IUiDispatcher uiDispatcher;

        public NotificationsPresenter(IPomodoroEngine pomodoroEngine, IUserPreferences userPreferences, IEventHub eventHub, IUiDispatcher uiDispatcher)
        {
            this.pomodoroEngine = pomodoroEngine;
            this.userPreferences = userPreferences;
            this.uiDispatcher = uiDispatcher;
            eventHub.Subscribe<TimerStopped>(OnTimerStopped);
            eventHub.Subscribe<FirstRun>(OnFirstRun);
        }

        private void OnFirstRun(FirstRun @event)
        {
            var toast = Toasts.FirstRun();
            toastNotifier.Show(toast);
        }

        private void OnTimerStopped(TimerStopped @event)
        {
            if (@event.IsIntervalCompleted && userPreferences.ShowToastNotifications)
            {
                var toast = Toasts.IntervalCompleted(@event.IntervalType, pomodoroEngine.SuggestedBreakType);
                toast.Activated += OnToastActivated;
                toastNotifier.Show(toast);
            }
        }

        private void OnToastActivated(ToastNotification sender, object args)
        {
            if (args is ToastActivatedEventArgs)
            {
                var activation = args as ToastActivatedEventArgs;

                if (Toasts.TimerAction.WithArgs.ContainsKey(activation.Arguments))
                {
                    var timerAction = Toasts.TimerAction.WithArgs[activation.Arguments];

                    uiDispatcher.Post(() => pomodoroEngine.StartTimer(timerAction.IntervalType));
                }
            }
        }
    }
}
