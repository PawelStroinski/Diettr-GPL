﻿using Dietphone.Models;
using Dietphone.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using Dietphone.Views;

namespace Dietphone.ViewModels
{
    public class InsulinEditingViewModel : EditingViewModelWithDate<Insulin, InsulinViewModel>
    {
        public ObservableCollection<InsulinCircumstanceViewModel> Circumstances { get; private set; }
        public SugarViewModel CurrentSugar { get; private set; }
        private List<InsulinCircumstanceViewModel> addedCircumstances = new List<InsulinCircumstanceViewModel>();
        private List<InsulinCircumstanceViewModel> deletedCircumstances = new List<InsulinCircumstanceViewModel>();
        private Sugar sugarSource;
        private Sugar sugarCopy;
        private bool isBusy;
        private bool isCalculated;
        private string isCalculatedText;
        private bool openedWithNoBolus;
        private Meal meal;
        private readonly ReplacementBuilderAndSugarEstimatorFacade facade;
        private readonly BackgroundWorkerFactory workerFactory;

        public InsulinEditingViewModel(Factories factories, ReplacementBuilderAndSugarEstimatorFacade facade,
            BackgroundWorkerFactory workerFactory)
            : base(factories)
        {
            this.facade = facade;
            this.IsCalculatedText = string.Empty;
            this.workerFactory = workerFactory;
        }

        public string NameOfFirstChoosenCircumstance
        {
            get
            {
                return Subject.Circumstances.Any() ? Subject.Circumstances.First().Name : string.Empty;
            }
            set
            {
                if (!Subject.Circumstances.Any())
                    throw new InvalidOperationException("No insulin circumstance choosen.");
                Subject.Circumstances.First().Name = value;
            }
        }

        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
            set
            {
                isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        public bool IsCalculated
        {
            get
            {
                return isCalculated;
            }
            private set
            {
                isCalculated = value;
                OnPropertyChanged("IsCalculated");
            }
        }

        public string IsCalculatedText
        {
            get
            {
                return isCalculatedText;
            }
            private set
            {
                isCalculatedText = value;
                OnPropertyChanged("IsCalculatedText");
            }
        }

        public void AddCircumstance(string name)
        {
            var tempModel = factories.CreateInsulinCircumstance();
            var models = factories.InsulinCircumstances;
            models.Remove(tempModel);
            var viewModel = new InsulinCircumstanceViewModel(tempModel, factories);
            viewModel.Name = name;
            Circumstances.Add(viewModel);
            addedCircumstances.Add(viewModel);
        }

        public bool CanEditCircumstance()
        {
            return Subject.Circumstances.Any();
        }

        public CanDeleteCircumstanceResult CanDeleteCircumstance()
        {
            if (Circumstances.Count < 2)
                return CanDeleteCircumstanceResult.NoThereIsOnlyOneCircumstance;
            if (!Subject.Circumstances.Any())
                return CanDeleteCircumstanceResult.NoCircumstanceChoosen;
            return CanDeleteCircumstanceResult.Yes;
        }

        public void DeleteCircumstance()
        {
            var toDelete = Subject.Circumstances.First();
            var choosenViewModels = Subject.Circumstances.ToList();
            choosenViewModels.Remove(toDelete);
            Subject.Circumstances = choosenViewModels;
            Circumstances.Remove(toDelete);
            deletedCircumstances.Add(toDelete);
        }

        public string SummaryForSelectedCircumstances()
        {
            return string.Join(", ",
                Subject.Circumstances.Select(circumstance => circumstance.Name));
        }

        public void InvalidateCircumstances()
        {
            var newCircumstances = new ObservableCollection<InsulinCircumstanceViewModel>();
            foreach (var circumstance in Circumstances)
            {
                var newCircumstance = new InsulinCircumstanceViewModel(circumstance.Model, factories);
                newCircumstance.MakeBuffer();
                newCircumstance.CopyFrom(circumstance);
                newCircumstances.Add(newCircumstance);
            }
            Circumstances = newCircumstances;
            Subject.InvalidateCircumstances(Circumstances);
            OnPropertyChanged("Circumstances");
        }

        protected override void FindAndCopyModel()
        {
            var id = Navigator.GetInsulinIdToEdit();
            if (id == Guid.Empty)
                modelSource = factories.CreateInsulin();
            else
                modelSource = finder.FindInsulinById(id);
            modelCopy = modelSource.GetCopy();
            modelCopy.SetOwner(factories);
            modelCopy.InitializeCircumstances(modelSource.ReadCircumstances().ToList());
        }

        protected override void OnModelReady()
        {
            openedWithNoBolus = modelSource.NormalBolus == 0 && modelSource.SquareWaveBolus == 0;
            var relatedMealId = Navigator.GetRelatedMealId();
            if (relatedMealId == Guid.Empty)
                meal = finder.FindMealByInsulin(modelSource);
            else
                meal = finder.FindMealById(relatedMealId);
        }

        protected override void MakeViewModel()
        {
            LoadCircumstances();
            MakeInsulinViewModelInternal();
            MakeSugarViewModel();
        }

        protected override string Validate()
        {
            return string.Empty;
        }

        private void LoadCircumstances()
        {
            var loader = new InsulinListingViewModel.CircumstancesAndInsulinsLoader(factories, true);
            Circumstances = loader.Circumstances;
            foreach (var circumstance in Circumstances)
                circumstance.MakeBuffer();
        }

        private void MakeInsulinViewModelInternal()
        {
            Subject = new InsulinViewModel(modelCopy, factories, allCircumstances: Circumstances);
            Subject.PropertyChanged += (_, eventArguments) =>
            {
                IsDirty = true;
                if (eventArguments.PropertyName == "Circumstances")
                    SugarOrCircumstancesChanged();
                if (new string[] { "NormalBolus", "SquareWaveBolus", "SquareWaveBolusHours" }
                    .Contains(eventArguments.PropertyName))
                {
                    BolusChanged();
                }
            };
        }

        private void MakeSugarViewModel()
        {
            sugarSource = finder.FindSugarBeforeInsulin(modelSource);
            if (sugarSource == null)
                sugarSource = factories.CreateSugar();
            sugarCopy = sugarSource.GetCopy();
            sugarCopy.SetOwner(factories);
            CurrentSugar = new SugarViewModel(sugarCopy, factories);
            CurrentSugar.PropertyChanged += (_, eventArguments) =>
            {
                if (eventArguments.PropertyName == "BloodSugar")
                    SugarOrCircumstancesChanged();
            };
        }

        private void SugarOrCircumstancesChanged()
        {
            if (openedWithNoBolus && meal != null)
                StartCalculation();
        }

        private void BolusChanged()
        {
            IsCalculated = false;
        }

        private void StartCalculation()
        {
            var worker = workerFactory.Create();
            worker.DoWork += DoCalculation;
            worker.RunWorkerCompleted += CalculationCompleted;
            IsBusy = true;
            worker.RunWorkerAsync();
        }

        private void DoCalculation(object sender, DoWorkEventArgs e)
        {
            e.Result = facade.GetReplacementAndEstimatedSugars(meal, modelCopy, sugarCopy);
        }

        private void CalculationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            var replacementAndEstimatedSugars = e.Result as ReplacementAndEstimatedSugars;
            var replacement = replacementAndEstimatedSugars.Replacement;
            if (replacement.Items.Any())
                ShowCalculation(replacement);
            else
                IsCalculated = false;
        }

        private void ShowCalculation(Replacement replacement)
        {
            var insulin = replacement.InsulinTotal;
            Subject.Insulin.NormalBolus = insulin.NormalBolus;
            Subject.Insulin.SquareWaveBolus = insulin.SquareWaveBolus;
            Subject.Insulin.SquareWaveBolusHours = insulin.SquareWaveBolusHours;
            Subject.NotifyBolusChange();
            IsCalculated = true;
            IsCalculatedText = replacement.IsComplete
                ? Translations.InsulinHeaderCalculated : Translations.InsulinHeaderIncomplete;
        }

        public enum CanDeleteCircumstanceResult { Yes, NoCircumstanceChoosen, NoThereIsOnlyOneCircumstance };
    }
}
