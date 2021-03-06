﻿using System;
using Dietphone.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using Dietphone.Tools;
using System.Collections.Generic;
using Dietphone.Views;

namespace Dietphone.ViewModels
{
    public class MealViewModel : JournalItemViewModel
    {
        public Meal Meal { get; private set; }
        public IEnumerable<MealNameViewModel> Names { internal get; set; }
        public MealNameViewModel DefaultName { internal get; set; }
        private ObservableCollection<MealItemViewModel> items;
        private bool isNameCached;
        private bool isProductsHeadCached;
        private bool isProductsTailCached;
        private MealNameViewModel nameCache;
        private string productsHeadCache;
        private IEnumerable<string> productsTailCache;
        private readonly object itemsLock = new object();
        private readonly Factories factories;
        private readonly ScoreSelector scores;
        private const byte TAKE_PRODUCTS_TO_HEAD = 3;

        public MealViewModel(Meal meal, Factories factories)
        {
            Meal = meal;
            this.factories = factories;
            scores = new MealScoreSelector(this);
        }

        public override Guid Id
        {
            get
            {
                return Meal.Id;
            }
        }

        public override DateTime DateTime
        {
            get
            {
                var universal = Meal.DateTime;
                return universal.ToLocalTime();
            }
            set
            {
                var universal = value.ToUniversalTime();
                if (Meal.DateTime != universal)
                {
                    Meal.DateTime = universal;
                    OnPropertyChanged("DateTime");
                    OnPropertyChanged("DateOnly");
                    OnPropertyChanged("DateAndTime");
                    OnPropertyChanged("Time");
                }
            }
        }

        public MealNameViewModel Name
        {
            get
            {
                if (isNameCached)
                {
                    return nameCache;
                }
                nameCache = FindName();
                isNameCached = true;
                return nameCache;
            }
            set
            {
                if (value != null)
                {
                    SetName(value);
                }
            }
        }

        public string ProductsHead
        {
            get
            {
                if (isProductsHeadCached)
                {
                    return productsHeadCache;
                }
                productsHeadCache = MakeProductsHead();
                isProductsHeadCached = true;
                return productsHeadCache;
            }
        }

        public IEnumerable<string> ProductsTail
        {
            get
            {
                if (isProductsTailCached)
                {
                    return productsTailCache;
                }
                productsTailCache = MakeProductsTail();
                isProductsTailCached = true;
                return productsTailCache;
            }
        }

        public string Products
        {
            get
            {
                var all = ProductsTail.ToList();
                all.Insert(0, ProductsHead);
                return string.Join(" | ", all);
            }
        }

        public string Note
        {
            get
            {
                return Meal.Note;
            }
            set
            {
                if (value != Meal.Note)
                {
                    Meal.Note = value;
                    OnPropertyChanged("Note");
                }
            }
        }

        public override string Text
        {
            get { return string.Empty; }
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
            get { return false; }
        }

        public override bool IsMeal
        {
            get { return true; }
        }

        public override bool IsNotMeal
        {
            get { return false; }
        }

        public ObservableCollection<MealItemViewModel> Items
        {
            get
            {
                lock (itemsLock)
                {
                    if (items == null)
                    {
                        items = new ObservableCollection<MealItemViewModel>();
                        LoadItems();
                        items.CollectionChanged += OnItemsChanged;
                    }
                    return items;
                }
            }
        }

        public ScoreSelector Scores
        {
            get
            {
                return scores;
            }
        }

        public bool VisibleWhenIsNewerAndHasName
        {
            get
            {
                return IsNewer && HasName;
            }
        }

        public bool VisibleWhenIsNewerAndHasNoName
        {
            get
            {
                return IsNewer && !HasName;
            }
        }

        public bool VisibleWhenIsOlder
        {
            get
            {
                return !IsNewer;
            }
        }

        public bool HasName
        {
            get
            {
                return Name != DefaultName;
            }
        }

        public bool HasNote
        {
            get
            {
                return !string.IsNullOrEmpty(Note);
            }
        }

        public MealItemViewModel AddItem()
        {
            var itemModel = Meal.AddItem();
            var itemViewModel = CreateItemViewModel(itemModel);
            Items.Add(itemViewModel);
            return itemViewModel;
        }

        public void DeleteItem(MealItemViewModel itemViewModel)
        {
            ReleaseItemViewModel(itemViewModel);
            var itemModel = itemViewModel.Model;
            Meal.DeleteItem(itemModel);
            Items.Remove(itemViewModel);
        }

        public override bool FilterIn(string filter)
        {
            var name = Name;
            if (name != DefaultName)
            {
                var nameOfName = name.Name;
                if (nameOfName.ContainsIgnoringCase(filter))
                {
                    return true;
                }
            }
            if (ProductsHead.ContainsIgnoringCase(filter))
            {
                return true;
            }
            var tail = ProductsTail;
            foreach (var product in tail)
            {
                if (product.ContainsIgnoringCase(filter))
                {
                    return true;
                }
            }
            if (Note.ContainsIgnoringCase(filter))
            {
                return true;
            }
            return false;
        }

        public override void Choose(Navigator navigator)
        {
            navigator.GoToMealEditing(Id);
        }

        private string Energy
        {
            get
            {
                var result = Meal.Energy;
                return string.Format(Translations.Cal, result);
            }
        }

        private string Protein
        {
            get
            {
                var value = Meal.Protein;
                var rounded = (int)Math.Round(value);
                return string.Format(Translations.Prot, rounded);
            }
        }

        private string Fat
        {
            get
            {
                var value = Meal.Fat;
                var rounded = (int)Math.Round(value);
                return string.Format(Translations.Fat, rounded);
            }
        }

        private string DigestibleCarbs
        {
            get
            {
                var value = Meal.DigestibleCarbs;
                var rounded = (int)Math.Round(value);
                return string.Format(Translations.Carb, rounded);
            }
        }

        private string Cu
        {
            get
            {
                var result = Meal.Cu;
                return string.Format(Translations.Cu, result);
            }
        }

        private string Fpu
        {
            get
            {
                var result = Meal.Fpu;
                return string.Format(Translations.Fpu, result);
            }
        }

        private MealNameViewModel FindName()
        {
            if (Meal.NameId == Guid.Empty)
            {
                return DefaultName;
            }
            var result = from viewModel in Names
                         where viewModel.Id == Meal.NameId
                         select viewModel;
            result = result.DefaultIfEmpty(DefaultName);
            return result.FirstOrDefault();
        }

        private void SetName(MealNameViewModel value)
        {
            Meal.NameId = value.Id;
            isNameCached = false;
            OnPropertyChanged("Name");
            OnPropertyChanged("VisibleWhenIsNewerAndHasName");
            OnPropertyChanged("VisibleWhenIsNewerAndHasNoName");
        }

        private void LoadItems()
        {
            foreach (var itemModel in Meal.Items)
            {
                var itemViewModel = CreateItemViewModel(itemModel);
                items.Add(itemViewModel);
            }
        }

        private MealItemViewModel CreateItemViewModel(MealItem itemModel)
        {
            var itemViewModel = new MealItemViewModel(itemModel, factories);
            itemViewModel.ItemChanged += OnItemsChanged;
            return itemViewModel;
        }

        private void ReleaseItemViewModel(MealItemViewModel itemViewModel)
        {
            itemViewModel.ItemChanged -= OnItemsChanged;
        }

        private string MakeProductsHead()
        {
            var all = from item in Items
                      select item.ProductName;
            var nonEmpty = all.Where(name => !string.IsNullOrEmpty(name));
            var firstFew = nonEmpty.Take(TAKE_PRODUCTS_TO_HEAD);
            // Linq with Take() evaluates only such number of list items that is needed
            return string.Join(" | ", firstFew.ToArray());
        }

        private IEnumerable<string> MakeProductsTail()
        {
            var result = new List<string>();
            var items = Items;
            // Don't replace with Linq because Skip() in contrast to Take() evaluates whole list
            for (int i = TAKE_PRODUCTS_TO_HEAD; i < items.Count; i++)
            {
                var name = items[i].ProductName;
                result.Add(name);
            }
            return result;
        }

        protected void OnItemsChanged(object sender, EventArgs e)
        {
            isProductsHeadCached = false;
            isProductsTailCached = false;
            OnPropertyChanged("Scores");
            Scores.Invalidate();
        }

        private class MealScoreSelector : ScoreSelector
        {
            private readonly MealViewModel meal;

            public MealScoreSelector(MealViewModel meal)
                : base(meal.factories)
            {
                this.meal = meal;
            }

            protected override string GetCurrent()
            {
                if (settingsCopy.ScoreEnergy)
                {
                    return meal.Energy;
                }
                if (settingsCopy.ScoreProtein)
                {
                    return meal.Protein;
                }
                if (settingsCopy.ScoreDigestibleCarbs)
                {
                    return meal.DigestibleCarbs;
                }
                if (settingsCopy.ScoreFat)
                {
                    return meal.Fat;
                }
                if (settingsCopy.ScoreCu)
                {
                    return meal.Cu;
                }
                if (settingsCopy.ScoreFpu)
                {
                    return meal.Fpu;
                }
                return string.Empty;
            }
        }
    }
}
