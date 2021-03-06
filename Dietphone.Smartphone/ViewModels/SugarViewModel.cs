﻿using System;
using System.Globalization;
using Dietphone.Models;
using Dietphone.Tools;
using Dietphone.Views;

namespace Dietphone.ViewModels
{
    public class SugarViewModel : JournalItemViewModel
    {
        public Sugar Sugar { get; private set; }
        private bool settingBloodSugarWrapper;
        private readonly Factories factories;

        public SugarViewModel(Sugar sugar, Factories factories)
        {
            Sugar = sugar;
            this.factories = factories;
        }

        public override Guid Id
        {
            get
            {
                return Sugar.Id;
            }
        }

        public override DateTime DateTime
        {
            get
            {
                var universal = Sugar.DateTime;
                return universal.ToLocalTime();
            }
            set
            {
                var universal = value.ToUniversalTime();
                if (Sugar.DateTime != universal)
                {
                    Sugar.DateTime = universal;
                    NotifyDateTimeChange();
                }
            }
        }

        public string DateFormat
        {
            get
            {
                var culture = CultureInfo.CurrentCulture;
                return culture.GetShortDateAlternativeFormat();
            }
        }

        public string BloodSugar
        {
            get
            {
                var result = Sugar.BloodSugar;
                return result.ToStringOrEmpty();
            }
            set
            {
                var oldValue = Sugar.BloodSugar;
                var newValue = oldValue.TryGetValueOf(value);
                var settings = factories.Settings;
                var constrains = new Constrains { Max = settings.SugarUnit == SugarUnit.mgdL ? 540 : 30 };
                Sugar.BloodSugar = constrains.Constraint(newValue);
                OnPropertyChanged("BloodSugar");
                if (!settingBloodSugarWrapper)
                    OnPropertyChanged("BloodSugarWrapper");
            }
        }

        public string BloodSugarWrapper
        {
            get
            {
                return BloodSugar;
            }
            set
            {
                settingBloodSugarWrapper = true;
                try
                {
                    BloodSugar = value;
                }
                finally
                {
                    settingBloodSugarWrapper = false;
                }
            }
        }

        public override string Text
        {
            get
            {
                if (BloodSugar == string.Empty)
                    return string.Empty;
                else
                    return string.Format(factories.Settings.SugarUnit == SugarUnit.mgdL
                        ? Translations.BloodSugarMgdL : Translations.BloodSugarMmolL, BloodSugar);
            }
        }

        public override string Text2
        {
            get { return string.Empty; }
        }

        public override bool IsInsulin
        {
            get { return false; }
        }

        public override bool IsSugar
        {
            get { return true; }
        }

        public override bool IsMeal
        {
            get { return false; }
        }

        public override bool IsNotMeal
        {
            get { return true; }
        }
    }
}
