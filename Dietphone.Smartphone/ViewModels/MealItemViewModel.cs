﻿using System;
using Dietphone.Models;
using Dietphone.Tools;
using System.Collections.Generic;
using System.Windows;
using Dietphone.Views;
using System.Linq;

namespace Dietphone.ViewModels
{
    public class MealItemViewModel : ViewModelWithBuffer<MealItem>
    {
        public event EventHandler ItemChanged;
        private bool settingValueWrapper;
        private readonly ScoreSelector scores;
        private static readonly Constrains big = new Constrains { Max = 10000 };

        public MealItemViewModel(MealItem model, Factories factories)
            : base(model, factories)
        {
            scores = new MealItemScoreSelector(this);
        }

        public Guid ProductId
        {
            get
            {
                return BufferOrModel.ProductId;
            }
            set
            {
                BufferOrModel.ProductId = value;
                OnProductNamePropertyChanged();
                OnItemChanged();
            }
        }

        public string ProductName
        {
            get
            {
                var product = BufferOrModel.Product;
                return product.Name;
            }
        }

        public string Value
        {
            get
            {
                var result = BufferOrModel.Value;
                return result.ToStringOrEmpty();
            }
            set
            {
                var oldValue = BufferOrModel.Value;
                var newValue = oldValue.TryGetValueOf(value);
                BufferOrModel.Value = big.Constraint(newValue);
                OnItemChanged();
            }
        }

        public string ValueWrapper
        {
            get
            {
                return Value;
            }
            set
            {
                settingValueWrapper = true;
                try
                {
                    Value = value;
                }
                finally
                {
                    settingValueWrapper = false;
                }
            }
        }

        public string ValueWithUnit
        {
            get
            {
                var value = BufferOrModel.Value;
                return string.Format("{0} {1}", value, Unit);
            }
        }

        public string Unit
        {
            get
            {
                var result = BufferOrModel.Unit;
                return result.GetAbbreviationOrServingSizeDesc(BufferOrModel.Product);
            }
        }

        // Note: changing UnitWithDetalis may change the Value with help of SetOneServingIfIsZeroServings().
        public string UnitWithDetalis
        {
            get
            {
                var result = BufferOrModel.Unit;
                return result.GetAbbreviationOrServingSizeDetalis(BufferOrModel.Product);
            }
            set
            {
                var oldValue = BufferOrModel.Unit;
                var newValue = oldValue.TryGetValueOfAbbreviationOrServingSizeDetalis(value, BufferOrModel.Product);
                BufferOrModel.Unit = newValue;
                SetOneServingIfIsZeroServings();
                OnItemChanged();
            }
        }

        public ScoreSelector Scores
        {
            get
            {
                return scores;
            }
        }

        public bool HasManyUsableUnits
        {
            get
            {
                return AllUsableUnitsWithDetalis.Count > 1;
            }
        }

        public List<string> AllUsableUnitsWithDetalis
        {
            get
            {
                var units = UnitAbbreviations
                    .GetAbbreviationsOrServingSizeDetalisFiltered(IsUnitUsable, BufferOrModel.Product);
                if (units.Any())
                    return units;
                var settings = factories.Settings;
                var unit = settings.Unit;
                return new List<string> { unit.GetAbbreviation() };
            }
        }

        public void CopyFromModel(MealItem model)
        {
            BufferOrModel.CopyFrom(model);
            OnProductNamePropertyChanged();
            OnItemChanged();
        }

        public string SerializeModel()
        {
            return BufferOrModel.Serialize(string.Empty);
        }

        public void Invalidate()
        {
            OnItemChanged();
        }

        public void InitializeUnit()
        {
            var usableUnits = MyEnum.GetValues<Unit>()
                .Where(IsUnitUsable);
            var settings = factories.Settings;
            if (usableUnits.Contains(settings.Unit) || !usableUnits.Any())
                BufferOrModel.Unit = settings.Unit;
            else
            {
                var product = BufferOrModel.Product;
                BufferOrModel.Unit = product.ServingSizeUnit;
            }
        }

        private string Energy
        {
            get
            {
                var result = BufferOrModel.Energy;
                return string.Format(Translations.Cal, result);
            }
        }

        private string Protein
        {
            get
            {
                var value = BufferOrModel.Protein;
                var rounded = (int)Math.Round(value);
                return string.Format(Translations.Prot, rounded);
            }
        }

        private string Fat
        {
            get
            {
                var value = BufferOrModel.Fat;
                var rounded = (int)Math.Round(value);
                return string.Format(Translations.Fat, rounded);
            }
        }

        private string DigestibleCarbs
        {
            get
            {
                var value = BufferOrModel.DigestibleCarbs;
                var rounded = (int)Math.Round(value);
                return string.Format(Translations.Carb, rounded);
            }
        }

        private string Cu
        {
            get
            {
                var result = BufferOrModel.Cu;
                return string.Format(Translations.Cu, result);
            }
        }

        private string Fpu
        {
            get
            {
                var result = BufferOrModel.Fpu;
                return string.Format(Translations.Fpu, result);
            }
        }

        private bool IsUnitUsable(Models.Unit unit)
        {
            var unitUsability = new UnitUsability()
            {
                Product = BufferOrModel.Product,
                Unit = unit
            };
            return unitUsability.AnyNutrientsPerUnitPresent;
        }

        private void SetOneServingIfIsZeroServings()
        {
            if (BufferOrModel.Unit == Models.Unit.ServingSize && BufferOrModel.Value == 0)
            {
                BufferOrModel.Value = 1;
            }
        }

        protected void OnProductNamePropertyChanged()
        {
            OnPropertyChanged("ProductName");
            OnPropertyChanged("AllUsableUnitsWithDetalis");
            OnPropertyChanged("HasManyUsableUnits");
        }

        protected void OnItemChanged()
        {
            OnPropertyChanged("Value");
            OnPropertyChanged("ValueWithUnit");
            OnPropertyChanged("Unit");
            OnPropertyChanged("UnitWithDetalis");
            OnPropertyChanged("Scores");
            if (ItemChanged != null)
            {
                ItemChanged(this, EventArgs.Empty);
            }
            if (!settingValueWrapper)
            {
                OnPropertyChanged("ValueWrapper");
            }
        }

        private class MealItemScoreSelector : ScoreSelector
        {
            private readonly MealItemViewModel item;

            public MealItemScoreSelector(MealItemViewModel item)
                : base(item.factories)
            {
                this.item = item;
            }

            protected override string GetCurrent()
            {
                if (settingsCopy.ScoreEnergy)
                {
                    return item.Energy;
                }
                if (settingsCopy.ScoreProtein)
                {
                    return item.Protein;
                }
                if (settingsCopy.ScoreDigestibleCarbs)
                {
                    return item.DigestibleCarbs;
                }
                if (settingsCopy.ScoreFat)
                {
                    return item.Fat;
                }
                if (settingsCopy.ScoreCu)
                {
                    return item.Cu;
                }
                if (settingsCopy.ScoreFpu)
                {
                    return item.Fpu;
                }
                return string.Empty;
            }
        }
    }
}
