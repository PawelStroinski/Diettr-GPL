﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dietphone.Models;
using Dietphone.ViewModels;
using NSubstitute;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace Dietphone.Smartphone.Tests
{
    public class JournalLoaderTests
    {
        private Factories factories;
        private Fixture fixture;
        private JournalViewModel viewModel;
        private DateTime today = DateTime.Today;
        private DateTime yesterday = DateTime.Today.AddDays(-1);

        [SetUp]
        public void TestInitialize()
        {
            factories = Substitute.For<Factories>();
            fixture = new Fixture();
            factories.InsulinCircumstances.Returns(fixture.CreateMany<InsulinCircumstance>().ToList());
            factories.MealNames.Returns(fixture.CreateMany<MealName>().ToList());
            factories.Insulins.Returns(new List<Insulin>().ToList());
            factories.Sugars.Returns(new List<Sugar>().ToList());
            factories.Meals.Returns(new List<Meal>().ToList());
            factories.DefaultEntities.MealName.Returns(fixture.Create<MealName>());
            var sugarEditing = Substitute.For<SugarEditingViewModel>();
            viewModel = new JournalViewModel(factories, new BackgroundWorkerSyncFactory(), sugarEditing);
        }

        [Test]
        public void LoadsCircumstancesAndReturnsSorted()
        {
            var sut = new JournalViewModel.JournalLoader(factories, sortCircumstances: true, sortNames: false,
                workerFactory: new BackgroundWorkerSyncFactory());
            var expected = factories.InsulinCircumstances
                .OrderBy(circumstance => circumstance.Name);
            var actual = sut.Circumstances;
            Assert.AreEqual(expected.Select(circumstance => circumstance.Id).ToList(),
                actual.Select(circumstance => circumstance.Id).ToList());
        }

        [Test]
        public void LoadsCircumstancesAndReturnsUnsorted()
        {
            var sut = new JournalViewModel.JournalLoader(factories, sortCircumstances: false, sortNames: false,
                workerFactory: new BackgroundWorkerSyncFactory());
            var expected = factories.InsulinCircumstances;
            var actual = sut.Circumstances;
            Assert.AreEqual(expected.Select(circumstance => circumstance.Id).ToList(),
                actual.Select(circumstance => circumstance.Id).ToList());
        }

        [Test]
        public void LoadsNamesAndReturnsSorted()
        {
            var sut = new JournalViewModel.JournalLoader(factories, sortCircumstances: false, sortNames: true,
                workerFactory: new BackgroundWorkerSyncFactory());
            var expected = factories.MealNames
                .OrderBy(name => name.Name);
            var actual = sut.Names;
            Assert.AreEqual(expected.Select(name => name.Id).ToList(),
                actual.Select(name => name.Id).ToList());
        }

        [Test]
        public void LoadsNamesAndReturnsUnsorted()
        {
            var sut = new JournalViewModel.JournalLoader(factories, sortCircumstances: false, sortNames: false,
                workerFactory: new BackgroundWorkerSyncFactory());
            var expected = factories.MealNames;
            var actual = sut.Names;
            Assert.AreEqual(expected.Select(name => name.Id).ToList(),
                actual.Select(name => name.Id).ToList());
        }

        [Test]
        public void MakesDefaultName()
        {
            var sut = new JournalViewModel.JournalLoader(viewModel);
            Assert.AreEqual(factories.DefaultEntities.MealName.Name, sut.DefaultName.Name);
        }

        [Test]
        public void LoadsItems()
        {
            factories.Insulins.Returns(fixture.CreateMany<Insulin>(2).ToList());
            factories.Sugars.Returns(fixture.CreateMany<Sugar>(2).ToList());
            factories.Meals.Returns(fixture.CreateMany<Meal>(2).ToList());
            var date1 = DateTime.Today;
            var date2 = DateTime.Today.AddHours(3);
            factories.Meals.First().DateTime = date2;
            factories.Sugars.First().DateTime = date2;
            factories.Insulins.First().DateTime = date2;
            factories.Meals.ElementAt(1).DateTime = date1;
            factories.Sugars.ElementAt(1).DateTime = date1;
            factories.Insulins.ElementAt(1).DateTime = date1;
            var sut = new JournalViewModel.JournalLoader(viewModel);
            sut.LoadAsync();
            var expected = new DateTime[] { date1, date1, date1, date2, date2, date2 };
            var actual = viewModel.Items.Select(item => item.DateTime).ToList();
            Assert.AreEqual(expected, actual);
            var enumerator = viewModel.Items.GetEnumerator();
            enumerator.MoveNext();
            Assert.IsInstanceOf<MealViewModel>(enumerator.Current);
            enumerator.MoveNext();
            Assert.IsInstanceOf<SugarViewModel>(enumerator.Current);
            enumerator.MoveNext();
            Assert.IsInstanceOf<InsulinViewModel>(enumerator.Current);
            enumerator.MoveNext();
            Assert.IsInstanceOf<MealViewModel>(enumerator.Current);
            enumerator.MoveNext();
            Assert.IsInstanceOf<SugarViewModel>(enumerator.Current);
            enumerator.MoveNext();
            Assert.IsInstanceOf<InsulinViewModel>(enumerator.Current);
        }

        [Test]
        public void ProvidesAllCircumstancesToInsulinViewModel()
        {
            factories.Insulins.Returns(fixture.CreateMany<Insulin>(1).ToList());
            var sut = new JournalViewModel.JournalLoader(viewModel);
            sut.LoadAsync();
            Assert.AreEqual(sut.Circumstances, viewModel.Items.Cast<InsulinViewModel>().First().AllCircumstances());
        }

        [Test]
        public void ProvidesNamesToMealViewModel()
        {
            factories.Meals.Returns(fixture.CreateMany<Meal>(1).ToList());
            var sut = new JournalViewModel.JournalLoader(viewModel);
            sut.LoadAsync();
            Assert.AreSame(sut.Names, viewModel.Items.Cast<MealViewModel>().First().Names);
        }

        [Test]
        public void ProvidesDefaultNameToMealViewModel()
        {
            factories.Meals.Returns(fixture.CreateMany<Meal>(1).ToList());
            var sut = new JournalViewModel.JournalLoader(viewModel);
            sut.LoadAsync();
            Assert.AreSame(sut.DefaultName, viewModel.Items.Cast<MealViewModel>().First().DefaultName);
        }

        [Test]
        public void MakesDatesAndSortsItems()
        {
            factories.Insulins.Returns(fixture.CreateMany<Insulin>(50).ToList());
            factories.Sugars.Returns(fixture.CreateMany<Sugar>(1).ToList());
            factories.Insulins[0].DateTime = yesterday;
            factories.Insulins[1].DateTime = today;
            for (int i = 2; i < 49; i++)
                factories.Insulins[i].DateTime = today.AddDays(-i);
            factories.Insulins[49].DateTime = factories.Insulins[48].DateTime.AddMinutes(2);
            factories.Sugars.First().DateTime = factories.Insulins[48].DateTime;
            var sut = new JournalViewModel.JournalLoader(viewModel);
            sut.LoadAsync();
            Assert.AreEqual(today, viewModel.Dates[0].Date);
            Assert.AreEqual(yesterday, viewModel.Dates[1].Date);
            Assert.AreEqual(today, viewModel.Items[0].Date.Date);
            Assert.AreEqual(yesterday, viewModel.Items[1].Date.Date);
            Assert.IsFalse(viewModel.Dates[viewModel.Dates.Count - 2].IsGroupOfOlder);
            Assert.IsTrue(viewModel.Dates[viewModel.Dates.Count - 1].IsGroupOfOlder);
            Assert.IsTrue(
                viewModel.Items.IndexOf(viewModel.Items.First(vm => vm.DateTime == factories.Insulins[48].DateTime)) >
                viewModel.Items.IndexOf(viewModel.Items.First(vm => vm.DateTime == factories.Insulins[49].DateTime)));
            Assert.IsInstanceOf<SugarViewModel>(viewModel.Items.ElementAt(viewModel.Items.Count - 2));
        }

        [Test]
        public void MakesDatesAndSortsMeals()
        {
            factories.Meals.Returns(fixture.CreateMany<Meal>(100).ToList());
            factories.Meals[0].DateTime = yesterday;
            factories.Meals[1].DateTime = today;
            for (int i = 2; i < 100; i++)
                factories.Meals[i].DateTime = today.AddDays(-i);
            var sut = new JournalViewModel.JournalLoader(viewModel);
            sut.LoadAsync();
            Assert.AreEqual(today, viewModel.Dates[0].Date);
            Assert.AreEqual(yesterday, viewModel.Dates[1].Date);
            Assert.AreEqual(today, viewModel.Items[0].Date.Date);
            Assert.AreEqual(yesterday, viewModel.Items[1].Date.Date);
            Assert.IsFalse(viewModel.Dates[viewModel.Dates.Count - 2].IsGroupOfOlder);
            Assert.IsTrue(viewModel.Dates[viewModel.Dates.Count - 1].IsGroupOfOlder);
        }
    }
}
