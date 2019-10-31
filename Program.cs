using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using HeartsAndMinds.Models;

namespace HeartsAndMinds {
    internal class Program {
        private static void Main() {
            // Set application culture.
            SetApplicationCulture();

            // Start bot logic. (blocking until done)
            Bot.Start(HeartsAndMinds.Strategy);
        }

        private static void SetApplicationCulture()
        {
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}