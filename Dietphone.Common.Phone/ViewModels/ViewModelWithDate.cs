﻿using System;
using Dietphone.Tools;

namespace Dietphone.ViewModels
{
    public abstract class ViewModelWithDate : ViewModelBase
    {
        public DateViewModel Date { get; set; }

        public abstract DateTime DateTime { get; set; }

        public DateTime DateOnly
        {
            get
            {
                return DateTime.Date;
            }
        }

        public string DateAndTime
        {
            get
            {
                var date = DateTime.ToShortDateInAlternativeFormat();
                return string.Format("{0} {1}", date, Time);
            }
        }

        public string Time
        {
            get
            {
                return DateTime.ToString("t");
            }
        }
    }
}
