// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;

namespace QuickLook.Actions
{
    [ContentProperty("Actions")]
    public class ConditionalEventTrigger : FrameworkContentElement
    {
        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register("Condition", typeof(bool), typeof(ConditionalEventTrigger));

        public static readonly DependencyProperty TriggersProperty = DependencyProperty.RegisterAttached("Triggers",
            typeof(ConditionalEventTriggerCollection), typeof(ConditionalEventTrigger), new PropertyMetadata
            {
                PropertyChangedCallback = (obj, e) =>
                {
                    // When "Triggers" is set, register handlers for each trigger in the list 
                    var element = (FrameworkElement) obj;
                    var triggers = (List<ConditionalEventTrigger>) e.NewValue;
                    foreach (var trigger in triggers)
                        element.AddHandler(trigger.RoutedEvent, new RoutedEventHandler((obj2, e2) =>
                            trigger.OnRoutedEvent(element)));
                }
            });

        private static readonly RoutedEvent TriggerActionsEvent = EventManager.RegisterRoutedEvent("",
            RoutingStrategy.Direct,
            typeof(EventHandler), typeof(ConditionalEventTrigger));

        public ConditionalEventTrigger()
        {
            Actions = new List<TriggerAction>();
        }

        public RoutedEvent RoutedEvent { get; set; }
        public List<TriggerAction> Actions { get; set; }

        // Condition
        public bool Condition
        {
            get => (bool) GetValue(ConditionProperty);
            set => SetValue(ConditionProperty, value);
        }

        // "Triggers" attached property
        public static ConditionalEventTriggerCollection GetTriggers(DependencyObject obj)
        {
            return (ConditionalEventTriggerCollection) obj.GetValue(TriggersProperty);
        }

        public static void SetTriggers(DependencyObject obj, ConditionalEventTriggerCollection value)
        {
            obj.SetValue(TriggersProperty, value);
        }

        // When an event fires, check the condition and if it is true fire the actions 
        private void OnRoutedEvent(FrameworkElement element)
        {
            DataContext = element.DataContext; // Allow data binding to access element properties
            if (Condition)
            {
                // Construct an EventTrigger containing the actions, then trigger it 
                var dummyTrigger = new EventTrigger {RoutedEvent = TriggerActionsEvent};
                foreach (var action in Actions)
                    dummyTrigger.Actions.Add(action);

                element.Triggers.Add(dummyTrigger);
                try
                {
                    element.RaiseEvent(new RoutedEventArgs(TriggerActionsEvent));
                }
                finally
                {
                    element.Triggers.Remove(dummyTrigger);
                }
            }
        }
    }

    // Create collection type visible to XAML - since it is attached we cannot construct it in code 
    public class ConditionalEventTriggerCollection : List<ConditionalEventTrigger>
    {
    }
}