﻿using System;
using System.Collections.Generic;
using Dietphone.Models;
using Dietphone.Tools;
using Dietphone.ViewModels;
using NSubstitute;
using NUnit.Framework;
using Ploeh.AutoFixture;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using Dietphone.Views;
using System.Collections.ObjectModel;

namespace Dietphone.Common.Phone.Tests
{
    public class InsulinAndSugarListingViewModelTests
    {
        private Factories factories;
        private InsulinAndSugarListingViewModel sut;
        private SugarEditingViewModel sugarEditing;

        [SetUp]
        public void TestInitialize()
        {
            factories = Substitute.For<Factories>();
            factories.InsulinCircumstances.Returns(new List<InsulinCircumstance>());
            factories.Insulins.Returns(new List<Insulin>());
            factories.Sugars.Returns(new List<Sugar>());
            factories.Settings.Returns(new Settings());
            sugarEditing = Substitute.For<SugarEditingViewModel>();
            sut = new InsulinAndSugarListingViewModel(factories, new BackgroundWorkerSyncFactory(), sugarEditing);
        }

        [Test]
        public void LoadLoadsWhenNotLoadedAlready()
        {
            var loaded = false;
            sut.Loaded += delegate { loaded = true; };
            sut.ChangesProperty("IsBusy", () =>
            {
                sut.Load();
            });
            Assert.IsTrue(loaded);
        }

        [Test]
        public void LoadDoesntLoadWhenLoadedAlready()
        {
            sut.Load();
            sut.NotChangesProperty("IsBusy", () =>
            {
                sut.Load();
            });
        }

        [Test]
        public void RefreshDoesntLoadWhenNotLoadedAlready()
        {
            sut.NotChangesProperty("IsBusy", () =>
            {
                sut.Refresh();
            });
        }

        [Test]
        public void RefreshLoadsWhenNotLoadedAlready()
        {
            sut.Load();
            var refreshed = false;
            sut.Refreshed += delegate { refreshed = true; };
            sut.ChangesProperty("IsBusy", () =>
            {
                sut.Refresh();
            });
            Assert.IsTrue(refreshed);
        }

        [Test]
        public void FindInsulinOrSugar()
        {
            var sugar = new Sugar { DateTime = DateTime.Today };
            var insulin = new Insulin { DateTime = DateTime.Today.AddMinutes(1) };
            factories.Sugars.Add(sugar);
            factories.Insulins.Add(insulin);
            sut.Load();
            Assert.IsNull(sut.FindInsulinOrSugar(DateTime.Today.AddHours(1)));
            Assert.IsInstanceOf<SugarViewModel>(sut.FindInsulinOrSugar(sugar.DateTime));
            Assert.IsInstanceOf<InsulinViewModel>(sut.FindInsulinOrSugar(insulin.DateTime));
        }

        [Test]
        public void FindDate()
        {
            var sugar = new Sugar { DateTime = DateTime.Now };
            factories.Sugars.Add(sugar);
            sut.Load();
            Assert.AreEqual(sugar.DateTime.Date, sut.FindDate(sugar.DateTime.Date).Date);
        }

        [Test]
        public void ChooseWhenInsulin()
        {
            var navigator = Substitute.For<Navigator>();
            var insulin = new Insulin { Id = Guid.NewGuid() };
            var viewModel = new InsulinViewModel(insulin, factories, new List<InsulinCircumstanceViewModel>());
            sut.Navigator = navigator;
            sut.Choose(viewModel);
            navigator.Received().GoToInsulinEditing(insulin.Id);
        }

        [Test]
        public void ChooseWhenSugar()
        {
            var sugar = new Sugar { BloodSugar = 100 };
            var viewModel = new SugarViewModel(sugar, factories);
            sut.Choose(viewModel);
            sugarEditing.Received().Show(Arg.Is<SugarViewModel>(vm => "100" == vm.BloodSugar));
        }

        [Test]
        public void ChooseCreatesACopyOfSugar()
        {
            var sugar = new Sugar { BloodSugar = 100 };
            var viewModel = new SugarViewModel(sugar, factories);
            sut.Choose(viewModel);
            sugarEditing.Subject.BloodSugar = "110";
            Assert.AreEqual(100, sugar.BloodSugar);
        }

        [Test]
        public void ChooseAndSugarEditingConfirm()
        {
            var sugar = new Sugar { BloodSugar = 100 };
            var viewModel = new SugarViewModel(sugar, factories);
            sut.Choose(viewModel);
            sugarEditing.Subject.BloodSugar = "110";
            sugarEditing.Confirm();
            Assert.AreEqual(110, sugar.BloodSugar);
        }

        [Test]
        public void ChooseAndSugarEditingDelete()
        {
            var sugar = new Sugar();
            factories.Sugars.Add(sugar);
            sut.Load();
            var viewModel = new SugarViewModel(sugar, factories);
            sut.Choose(viewModel);
            Assert.IsTrue(sugarEditing.CanDelete);
            sugarEditing.Delete();
            Assert.IsEmpty(factories.Sugars);
            Assert.IsEmpty(sut.InsulinsAndSugars);
        }

        [Test]
        public void OnSearchChanged()
        {
            var sut = new SutAccessor();
            var updating = false;
            var update = true;
            var updated = false;
            sut.DescriptorsUpdating += delegate { updating = true; };
            sut.DescriptorsUpdated += delegate { updated = true; };
            sut.UpdateFilterDescriptorsEvent += delegate
            {
                Assert.IsTrue(updating);
                Assert.IsFalse(updated);
                update = true;
            };
            sut.OnSearchChanged();
            Assert.IsTrue(update);
            Assert.IsTrue(updated);
        }

        class SutAccessor : InsulinAndSugarListingViewModel
        {
            public event EventHandler UpdateFilterDescriptorsEvent;

            public SutAccessor()
                : base(Substitute.For<Factories>(), Substitute.For<BackgroundWorkerFactory>(),
                    Substitute.For<SugarEditingViewModel>())
            {
            }

            public new void OnSearchChanged()
            {
                base.OnSearchChanged();
            }

            protected override void UpdateFilterDescriptors()
            {
                UpdateFilterDescriptorsEvent(null, null);
            }
        }
    }
}
