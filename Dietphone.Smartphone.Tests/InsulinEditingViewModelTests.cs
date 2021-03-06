﻿using System;
using System.Collections.Generic;
using Dietphone.Models;
using Dietphone.Tools;
using Dietphone.ViewModels;
using NSubstitute;
using NUnit.Framework;
using Ploeh.AutoFixture;
using System.Linq;
using Dietphone.Views;

namespace Dietphone.Smartphone.Tests
{
    public class InsulinEditingViewModelTests : TestBase
    {
        private Factories factories;
        private Navigator navigator;
        private StateProvider stateProvider;
        private InsulinEditingViewModel sut;
        private Insulin insulin;
        private ReplacementBuilderAndSugarEstimatorFacade facade;
        private Sugar sugar;
        private Settings settings;
        private Meal meal;
        private Clipboard clipboard;
        private MessageDialog messageDialog;
        private InsulinEditingViewModel.Navigation navigation;

        [SetUp]
        public void TestInitialize()
        {
            var fixture = new Fixture();
            factories = Substitute.For<Factories>();
            navigator = Substitute.For<Navigator>();
            stateProvider = Substitute.For<StateProvider>();
            facade = Substitute.For<ReplacementBuilderAndSugarEstimatorFacade>();
            clipboard = Substitute.For<Clipboard>();
            messageDialog = Substitute.For<MessageDialog>();
            navigation = new InsulinEditingViewModel.Navigation();
            CreateSut();
            insulin = fixture.Create<Insulin>();
            insulin.InitializeCircumstances(new List<Guid>());
            insulin.SetOwner(factories);
            sugar = new Sugar();
            sugar.SetOwner(factories);
            factories.InsulinCircumstances.Returns(fixture.CreateMany<InsulinCircumstance>().ToList());
            factories.CreateSugar().Returns(sugar);
            settings = new Settings { MaxBolus = 5 };
            factories.Settings.Returns(settings);
            meal = fixture.Create<Meal>();
            factories.Finder.FindMealByInsulin(insulin).Returns(meal);
            factories.Finder.FindInsulinById(insulin.Id).Returns(insulin);
            var replacementAndEstimatedSugars = new ReplacementAndEstimatedSugars();
            replacementAndEstimatedSugars.EstimatedSugars = new List<Sugar>();
            replacementAndEstimatedSugars.Replacement
                = new Replacement { InsulinTotal = new Insulin(), Items = new List<ReplacementItem>() };
            facade.GetReplacementAndEstimatedSugars(Arg.Any<Meal>(), Arg.Any<Insulin>(), Arg.Any<Sugar>())
                    .Returns(replacementAndEstimatedSugars);
            factories.MealNames.Returns(new List<MealName>());
            stateProvider.State.Returns(new Dictionary<string, object>());
        }

        private void CreateSut()
        {
            sut = new InsulinEditingViewModel(factories, facade, new BackgroundWorkerSyncFactory(), clipboard,
                messageDialog);
            sut.Navigator = navigator;
            sut.StateProvider = stateProvider;
            sut.Init(navigation);
        }

        private void InitializeViewModel()
        {
            factories.CreateInsulin().Returns(insulin);
            sut.Load();
        }

        private void ChooseCircumstance()
        {
            var circumstances = sut.Subject.Circumstances.ToList();
            circumstances.Add(sut.Circumstances.Except(circumstances).First());
            sut.Subject.Circumstances = circumstances;
        }

        private void TombstoneCreateInitialize()
        {
            sut.Tombstone();
            CreateSut();
            InitializeViewModel();
        }

        public class GeneralTests : InsulinEditingViewModelTests
        {
            [TestCase(true)]
            [TestCase(false)]
            public void FindAndCopyModelAndMakeViewModel(bool editingExisting)
            {
                if (editingExisting)
                    navigation.InsulinIdToEdit = insulin.Id;
                else
                    factories.CreateInsulin().Returns(insulin);
                sut.Load();
                Assert.AreEqual(insulin.Id, sut.Subject.Id);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void FindAndCopyModelCopiesDateTimeFromMealWhenNewInsulin(bool editingExisting)
            {
                if (editingExisting)
                    navigation.InsulinIdToEdit = insulin.Id;
                else
                    factories.CreateInsulin().Returns(insulin);
                sut.Load();
                if (editingExisting)
                {
                    Assert.AreNotEqual(meal.DateTime, insulin.DateTime);
                    Assert.AreNotEqual(meal.DateTime, sugar.DateTime);
                }
                else
                {
                    Assert.AreEqual(meal.DateTime, insulin.DateTime);
                    Assert.AreEqual(meal.DateTime, sugar.DateTime);
                    Assert.AreEqual(meal.DateTime.ToLocalTime(), sut.Subject.DateTime);
                }
            }

            [Test]
            public void MakeViewModelFindsSugar()
            {
                var sugar = new Sugar { BloodSugar = 150 };
                factories.Finder.FindSugarBeforeInsulin(insulin).Returns(sugar);
                InitializeViewModel();
                Assert.AreEqual(sugar.BloodSugar.ToString(), sut.CurrentSugar.BloodSugar);
            }

            [Test]
            public void MakeViewModelCreatesSugarIfCantFind()
            {
                sugar.BloodSugar = 150;
                InitializeViewModel();
                Assert.AreEqual(sugar.BloodSugar.ToString(), sut.CurrentSugar.BloodSugar);
            }

            [Test]
            public void MakeViewModelCopiesFoundSugar()
            {
                var sugar = new Sugar { BloodSugar = 150 };
                factories.Finder.FindSugarBeforeInsulin(insulin).Returns(sugar);
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "155";
                Assert.AreEqual(150, sugar.BloodSugar);
            }

            [Test]
            public void MakeViewModelCopiesCreatedSugar()
            {
                sugar.BloodSugar = 150;
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "155";
                Assert.AreEqual(150, sugar.BloodSugar);
            }

            [Test]
            public void MakeViewModelSetsOwnerOfCopiedSugar()
            {
                sugar.BloodSugar = 150;
                InitializeViewModel();
                Assert.AreEqual(150, sut.CurrentSugar.Sugar.BloodSugarInMgdL);
            }

            [Test]
            public void WhenMakeViewModelCreatesSugarItSetsItsDateToInsulinsDate()
            {
                InitializeViewModel();
                Assert.AreEqual(insulin.DateTime, sugar.DateTime);
            }

            [Test]
            public void WhenMakeViewModelFindsSugarItDoesntChangeItsDate()
            {
                factories.Finder.FindSugarBeforeInsulin(insulin).Returns(sugar);
                InitializeViewModel();
                Assert.AreNotEqual(insulin.DateTime, sugar.DateTime);
            }

            [Test]
            public void CurrentSugarCanBeWrittenAndRead()
            {
                factories.Settings.Returns(new Settings());
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "110";
                Assert.AreEqual("110", sut.CurrentSugar.BloodSugar);
            }

            [Test]
            public void CircumstancesCanBeRead()
            {
                InitializeViewModel();
                var expected = factories.InsulinCircumstances;
                var actual = sut.Circumstances;
                Assert.AreEqual(expected.Count, actual.Count);
            }

            [Test]
            public void CircumstancesAreBuffered()
            {
                InitializeViewModel();
                var expected = factories.InsulinCircumstances.First();
                var actual = sut.Circumstances.First(circumstance => circumstance.Id == expected.Id);
                Assert.AreEqual(expected.Name, actual.Name);
                expected.Name = "foo";
                Assert.AreNotEqual(expected.Name, actual.Name);
            }

            [TestCase("new")]
            [TestCase(null)]
            public void AddCircumstance(string name)
            {
                var newCircumstance = new InsulinCircumstance();
                factories.CreateInsulinCircumstance()
                    .Returns(newCircumstance)
                    .AndDoes(delegate { factories.InsulinCircumstances.Add(newCircumstance); });
                InitializeViewModel();
                var factoriesCountBefore = factories.InsulinCircumstances.Count;
                var sutCountBefore = sut.Circumstances.Count;
                var insulinCountBefore = sut.Subject.Circumstances.Count;
                messageDialog.Input(Translations.Name, Translations.AddCircumstance).Returns(name);
                sut.AddCircumstance.Call();
                Assert.AreEqual(factoriesCountBefore, factories.InsulinCircumstances.Count);
                if (name == null)
                    Assert.AreEqual(sutCountBefore, sut.Circumstances.Count);
                else
                {
                    Assert.AreEqual(sutCountBefore + 1, sut.Circumstances.Count);
                    Assert.AreEqual(insulinCountBefore, sut.Subject.Circumstances.Count);
                    Assert.AreEqual(name, sut.Circumstances.Last().Name);
                }
            }

            [TestCase(true, "newName")]
            [TestCase(true, null)]
            [TestCase(false, "newName")]
            public void EditCircumstance(bool circumstanceChoosed, string newName)
            {
                InitializeViewModel();
                if (circumstanceChoosed)
                    ChooseCircumstance();
                messageDialog.Input(Translations.Circumstance, Translations.EditCircumstance,
                    value: sut.NameOfFirstChoosenCircumstance).Returns(newName);
                var circumstanceEditCalled = false;
                sut.CircumstanceEdit += (_, action) =>
                {
                    Assert.AreNotEqual(newName, sut.NameOfFirstChoosenCircumstance);
                    action();
                    Assert.AreEqual(newName, sut.NameOfFirstChoosenCircumstance);
                    circumstanceEditCalled = true;
                };
                sut.EditCircumstance.Call();
                messageDialog.Received(circumstanceChoosed ? 0 : 1).Show(Translations.SelectCircumstanceFirst);
                Assert.AreEqual(circumstanceChoosed && newName != null, circumstanceEditCalled);
            }

            [Test]
            public void EditCircumstanceWhenCircumstanceEditEventNotHandled()
            {
                InitializeViewModel();
                ChooseCircumstance();
                messageDialog.Input(null, null, null).ReturnsForAnyArgs("newName");
                sut.EditCircumstance.Call();
                Assert.AreEqual("newName", sut.NameOfFirstChoosenCircumstance);
            }

            [TestCase(false, true, false)]
            [TestCase(true, false, false)]
            [TestCase(false, false, false)]
            [TestCase(true, true, false)]
            [TestCase(true, true, true)]
            public void DeleteCircumstance(bool circumstanceChoosed, bool moreThanOneCircumstance, bool confirmSetup)
            {
                if (!moreThanOneCircumstance)
                    factories.InsulinCircumstances.Returns(new Fixture().CreateMany<InsulinCircumstance>(1).ToList());
                InitializeViewModel();
                if (circumstanceChoosed)
                {
                    ChooseCircumstance();
                    if (moreThanOneCircumstance)
                        ChooseCircumstance();
                }
                var expected = sut.Subject.Circumstances.Skip(1).ToList();
                var circumstanceDeleteCalled = false;
                sut.CircumstanceDelete += (_, action) =>
                {
                    var actualBefore = sut.Subject.Circumstances;
                    Assert.AreNotEqual(expected, actualBefore);
                    action();
                    var actualAfter = sut.Subject.Circumstances;
                    Assert.AreEqual(expected, actualAfter);
                    circumstanceDeleteCalled = true;
                };
                var confirmCalled = false;
                messageDialog.Confirm(string.Format(Translations.AreYouSureYouWantToPermanentlyDeleteThisCircumstance,
                    sut.NameOfFirstChoosenCircumstance), Translations.DeleteCircumstance).Returns(_ =>
                    {
                        confirmCalled = true;
                        return confirmSetup;
                    });
                sut.DeleteCircumstance.Call();
                messageDialog.Received(circumstanceChoosed || !moreThanOneCircumstance ? 0 : 1)
                    .Show(Translations.SelectCircumstanceFirst);
                messageDialog.Received(moreThanOneCircumstance ? 0 : 1)
                    .Show(Translations.CannotDeleteCircumstanceBecauseOnlyOneLeft);
                Assert.AreEqual(circumstanceChoosed && moreThanOneCircumstance, confirmCalled);
                Assert.AreEqual(confirmCalled && confirmSetup, circumstanceDeleteCalled);
            }

            [Test]
            public void DeleteCircumstanceWhenCircumstanceDeleteEventNotHandled()
            {
                InitializeViewModel();
                ChooseCircumstance();
                ChooseCircumstance();
                var expected = sut.Subject.Circumstances.Skip(1).ToList();
                messageDialog.Confirm(null, null).ReturnsForAnyArgs(true);
                sut.DeleteCircumstance.Call();
                var actual = sut.Subject.Circumstances;
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void SaveAndReturn()
            {
                meal.DateTime = DateTime.Now.AddSeconds(-10);
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "140";
                ChooseCircumstance();
                sut.Subject.NormalBolus = "2.1";
                sut.Subject.SquareWaveBolus = "2.2";
                sut.Subject.SquareWaveBolusHours = "2.3";
                sut.Subject.Note = "note";
                sut.SaveAndReturn();
                Assert.AreEqual(140, sugar.BloodSugar);
                Assert.AreEqual(1, insulin.Circumstances.Count());
                Assert.AreEqual(sut.Circumstances.First().Id, insulin.ReadCircumstances().First());
                Assert.AreEqual(2.1, insulin.NormalBolus, 0.01);
                Assert.AreEqual(2.2, insulin.SquareWaveBolus, 0.01);
                Assert.AreEqual(2.3, insulin.SquareWaveBolusHours, 0.01);
                Assert.AreEqual("note", insulin.Note);
                Assert.AreEqual(DateTime.UtcNow.Ticks, insulin.DateTime.Ticks, TimeSpan.TicksPerSecond * 5);
                Assert.AreEqual(DateTime.UtcNow.Ticks, sugar.DateTime.Ticks, TimeSpan.TicksPerSecond * 5);
                navigator.Received().GoBack();
            }

            [Test]
            public void SaveAndReturnGoesForwardToMainPageInsteadOfGoingBackWhenRelatedMealIdGiven()
            {
                navigation.RelatedMealId = Guid.NewGuid();
                InitializeViewModel();
                sut.SaveAndReturn();
                navigator.Received().GoToMain();
                navigator.DidNotReceive().GoBack();
            }

            [Test]
            public void SaveAndReturnSavesCircumstances()
            {
                InitializeViewModel();
                var deletedCircumstanceId = sut.Circumstances.First().Id;
                var renamedCircumstanceId = sut.Circumstances.Skip(1).First().Id;
                var newCircumstance = new InsulinCircumstance();
                factories.CreateInsulinCircumstance().Returns(newCircumstance);
                ChooseCircumstance();
                sut.DeleteCircumstanceDo();
                ChooseCircumstance();
                sut.NameOfFirstChoosenCircumstance = "newname";
                sut.AddCircumstanceDo("foo");
                sut.SaveAndReturn();
                Assert.IsFalse(factories.InsulinCircumstances
                    .Any(circumstance => circumstance.Id == deletedCircumstanceId));
                Assert.AreEqual("newname", factories.InsulinCircumstances
                    .First(circumstance => circumstance.Id == renamedCircumstanceId).Name);
                Assert.IsTrue(factories.InsulinCircumstances.Contains(newCircumstance));
            }

            [Test]
            public void TombstoneAndUntombstone()
            {
                InitializeViewModel();
                sut.NotIsLockedDateTime = false;
                sut.CurrentSugar.BloodSugar = "120";
                sut.Tombstone();
                CreateSut();
                sut.dateTimeNow = () => DateTime.Now.AddHours(-1.5);
                InitializeViewModel();
                Assert.IsFalse(sut.NotIsLockedDateTime);
                Assert.AreEqual("120", sut.CurrentSugar.BloodSugar);
            }

            [Test]
            public void TombstoneAndUntombstoneDoesntCreateNewInsulin()
            {
                InitializeViewModel();
                sut.Subject.NormalBolus = "1.1";
                sut.Tombstone();
                CreateSut();
                factories.ClearReceivedCalls();
                InitializeViewModel();
                factories.DidNotReceive().CreateInsulin();
                sut.SaveAndReturn();
                Assert.AreEqual(1.1f, insulin.NormalBolus);
            }

            [Test]
            public void TombstoneAndUntombstoneCircumstances()
            {
                InitializeViewModel();
                var deletedCircumstanceId = sut.Circumstances.First().Id;
                var renamedCircumstanceId = sut.Circumstances.Skip(1).First().Id;
                var newCircumstance = new InsulinCircumstance();
                factories.CreateInsulinCircumstance().Returns(newCircumstance);
                ChooseCircumstance();
                sut.DeleteCircumstanceDo();
                ChooseCircumstance();
                sut.NameOfFirstChoosenCircumstance = "newname";
                sut.AddCircumstanceDo("foo");
                sut.Subject.NormalBolus = "1";
                TombstoneCreateInitialize();
                sut.SaveAndReturn();
                Assert.IsFalse(factories.InsulinCircumstances
                    .Any(circumstance => circumstance.Id == deletedCircumstanceId));
                Assert.AreEqual("newname", factories.InsulinCircumstances
                    .First(circumstance => circumstance.Id == renamedCircumstanceId).Name);
                Assert.IsTrue(factories.InsulinCircumstances.Contains(newCircumstance));
                Assert.AreEqual(1, sut.Subject.Circumstances.Count());
                Assert.AreEqual("newname", sut.NameOfFirstChoosenCircumstance);
                Assert.AreEqual("1", sut.Subject.NormalBolus);
            }

            [Test]
            public void NameOfFirstChoosenCircumstanceGetter()
            {
                InitializeViewModel();
                Assert.AreEqual(string.Empty, sut.NameOfFirstChoosenCircumstance);
                ChooseCircumstance();
                ChooseCircumstance();
                var expected = sut.Subject.Circumstances.First().Name;
                var actual = sut.NameOfFirstChoosenCircumstance;
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void NameOfFirstChoosenCircumstanceSetter()
            {
                InitializeViewModel();
                Assert.Throws<InvalidOperationException>(() =>
                {
                    sut.NameOfFirstChoosenCircumstance = "newname1";
                });
                ChooseCircumstance();
                ChooseCircumstance();
                sut.NameOfFirstChoosenCircumstance = "newname";
                var actual = sut.Subject.Circumstances.First().Name;
                Assert.AreEqual("newname", actual);
            }

            [Test]
            public void InvalidateCircumstancesInvalidatesWithoutChangingAnything()
            {
                factories.CreateInsulinCircumstance().Returns(new InsulinCircumstance());
                InitializeViewModel();
                ChooseCircumstance();
                ChooseCircumstance();
                sut.AddCircumstanceDo(string.Empty);
                sut.DeleteCircumstanceDo();
                sut.NameOfFirstChoosenCircumstance = "foo";
                var previousAll = sut.Circumstances;
                var previousAllIds = sut.Circumstances.Select(circumstance => circumstance.Id).ToList();
                var previousChoosen = sut.Subject.Circumstances;
                sut.ChangesProperty("Circumstances", () =>
                {
                    sut.InvalidateCircumstances();
                    Assert.AreNotSame(previousAll, sut.Circumstances);
                    Assert.AreEqual(previousAllIds, sut.Circumstances.Select(circumstance => circumstance.Id));
                    Assert.AreNotSame(previousChoosen, sut.Subject.Circumstances);
                });
                Assert.AreEqual(new InsulinCircumstanceViewModel[] { sut.Circumstances.First() }, sut.Subject.Circumstances);
                Assert.AreEqual("foo", sut.NameOfFirstChoosenCircumstance);
                Assert.AreNotEqual("foo", sut.Subject.Circumstances.First().Model.Name, "Should be buffered");
            }

            [Test]
            public void ShouldFocusSugarWhenNewSugar()
            {
                InitializeViewModel();
                Assert.IsTrue(sut.ShouldFocusSugar);
            }

            [Test]
            public void ShouldNotFocusSugarWhenExistingSugar()
            {
                var sugar = new Sugar { BloodSugar = 150 };
                factories.Finder.FindSugarBeforeInsulin(insulin).Returns(sugar);
                InitializeViewModel();
                Assert.IsFalse(sut.ShouldFocusSugar);
            }

            [Test]
            public void ChoosingCircumstanceSignalsIsDirty()
            {
                InitializeViewModel();
                Assert.IsFalse(sut.IsDirty);
                ChooseCircumstance();
                Assert.IsTrue(sut.IsDirty);
            }

            [Test]
            public void ChangingBolusSignalsIsDirty()
            {
                InitializeViewModel();
                Assert.IsFalse(sut.IsDirty);
                sut.Subject.NormalBolus = "1";
                Assert.IsTrue(sut.IsDirty);
            }

            [Test]
            public void ChangingCurrentSugarSignalsIsDirty()
            {
                InitializeViewModel();
                Assert.IsFalse(sut.IsDirty);
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsTrue(sut.IsDirty);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void DeleteAndSaveAndReturn(bool confirm)
            {
                var insulins = new List<Insulin> { insulin };
                factories.Insulins.Returns(insulins);
                factories.Sugars.Returns(new List<Sugar>());
                InitializeViewModel();
                ChooseCircumstance();
                var circumtanceId = sut.Subject.Circumstances.Single().Id;
                sut.NameOfFirstChoosenCircumstance = "foo";
                sut.CurrentSugar.BloodSugar = "100";
                sut.Subject.NormalBolus = "1";
                messageDialog.Confirm(string.Format(Translations.AreYouSureYouWantToPermanentlyDeleteThisInsulin,
                    sut.Subject.DateAndTime), Translations.DeleteInsulin).Returns(confirm);
                sut.DeleteAndSaveAndReturn();
                if (confirm)
                {
                    Assert.IsEmpty(insulins);
                    Assert.AreEqual("foo", factories.InsulinCircumstances.FindById(circumtanceId).Name);
                    Assert.AreNotEqual(100, sugar.BloodSugar);
                    Assert.AreNotEqual(1, insulin.NormalBolus);
                }
                else
                    Assert.IsNotEmpty(insulins);
                navigator.Received(confirm ? 1 : 0).GoBack();
            }

            [Test]
            public void DeleteAndSaveAndReturnDeletesTheNewlyCreatedSugar()
            {
                factories.Sugars.Returns(new List<Sugar> { sugar });
                factories.Insulins.Returns(new List<Insulin>());
                InitializeViewModel();
                messageDialog.Confirm(null, null).ReturnsForAnyArgs(true);
                sut.DeleteAndSaveAndReturn();
                Assert.IsEmpty(factories.Sugars);
            }

            [Test]
            public void DeleteAndSaveAndReturnDoesntDeleteTheFoundSugar()
            {
                var sugar = new Sugar();
                factories.Finder.FindSugarBeforeInsulin(insulin).Returns(sugar);
                factories.Sugars.Returns(new List<Sugar> { sugar });
                factories.Insulins.Returns(new List<Insulin>());
                InitializeViewModel();
                messageDialog.Confirm(null, null).ReturnsForAnyArgs(true);
                sut.DeleteAndSaveAndReturn();
                Assert.IsNotEmpty(factories.Sugars);
            }

            [Test]
            public void CancelAndReturnCallsTheBaseCancelAndReturn()
            {
                factories.Sugars.Returns(new List<Sugar>());
                InitializeViewModel();
                sut.CancelAndReturn();
                navigator.Received().GoBack();
            }

            [Test]
            public void CancelAndReturnDeletesTheNewlyCreatedSugar()
            {
                factories.Sugars.Returns(new List<Sugar> { sugar });
                InitializeViewModel();
                sut.CancelAndReturn();
                Assert.IsEmpty(factories.Sugars);
            }

            [Test]
            public void CancelAndReturnDoesntDeleteTheFoundSugar()
            {
                var sugar = new Sugar();
                factories.Finder.FindSugarBeforeInsulin(insulin).Returns(sugar);
                factories.Sugars.Returns(new List<Sugar> { sugar });
                InitializeViewModel();
                sut.CancelAndReturn();
                Assert.IsNotEmpty(factories.Sugars);
            }

            [TestCase(true, false)]
            [TestCase(false, true)]
            [TestCase(false, false)]
            public void GoToMealEditing(bool newMeal, bool relatedMealProvided)
            {
                InitializeViewModel();
                sut.IsDirty = true;
                sut.CurrentSugar.BloodSugar = "140";
                if (newMeal || relatedMealProvided)
                    factories.Finder.FindMealByInsulin(insulin).Returns((Meal)null);
                if (newMeal)
                    factories.CreateMeal().Returns(meal);
                if (relatedMealProvided)
                {
                    navigation.RelatedMealId = meal.Id;
                    factories.Finder.FindMealById(meal.Id).Returns(meal);
                }
                sut.GoToMealEditing();
                Assert.AreEqual(140, sugar.BloodSugar);
                Assert.IsFalse(sut.IsDirty);
                sut.Navigator.Received().GoToMealEditing(meal.Id);
            }

            [Test]
            public void MealScores()
            {
                InitializeViewModel();
                Assert.IsInstanceOf<ScoreSelector>(sut.MealScores);
                Assert.IsNotInstanceOf<EmptyScoreSelector>(sut.MealScores);
                Assert.IsTrue(sut.MealScoresVisible);
            }

            [Test]
            public void MealScoresWhenNoMeal()
            {
                factories.Finder.FindMealByInsulin(insulin).Returns((Meal)null);
                InitializeViewModel();
                Assert.IsInstanceOf<EmptyScoreSelector>(sut.MealScores);
                Assert.IsFalse(sut.MealScoresVisible);
            }

            [Test]
            public void OpenScoresSettings()
            {
                sut.OpenScoresSettings.Call();
                navigator.Received().GoToSettings();
            }

            [TestCase(true)]
            [TestCase(false)]
            public void ReturnedFromNavigationInvalidatesScoresIfWentToSettings(bool wentToSettings)
            {
                InitializeViewModel();
                if (wentToSettings)
                {
                    sut.OpenScoresSettings.Call();
                    sut.MealScores.ChangesProperty(string.Empty, () => sut.ReturnedFromNavigation());
                }
                sut.MealScores.NotChangesProperty(string.Empty, () => sut.ReturnedFromNavigation());
            }

            [TestCase(true)]
            [TestCase(false)]
            public void NoMealPresent(bool expectedNoMealPresent)
            {
                if (expectedNoMealPresent)
                    factories.Finder.FindMealByInsulin(insulin).Returns((Meal)null);
                sut.ChangesProperty("NoMealPresent", () => InitializeViewModel());
                Assert.AreEqual(expectedNoMealPresent, sut.NoMealPresent);
            }

            [Test]
            public void NoSugarEntered()
            {
                sut.ChangesProperty("NoSugarEntered", () => InitializeViewModel());
                Assert.IsTrue(sut.NoSugarEntered);
                sut.ChangesProperty("NoSugarEntered", () => sut.CurrentSugar.BloodSugar = "100");
                Assert.IsFalse(sut.NoSugarEntered);
            }

            [Test]
            public void CopyAsText()
            {
                InitializeViewModel();
                sut.CopyAsText();
                clipboard.Received().Set(sut.Subject.Text);
            }

            [Test]
            public void Messages()
            {
                Assert.AreEqual(Translations.AreYouSureYouWantToSaveThisInsulin, sut.Messages.CannotSaveCaption);
            }
        }

        public class ReplacementAndEstimatedSugarsTests : InsulinEditingViewModelTests
        {
            private ReplacementAndEstimatedSugars replacementAndEstimatedSugars;

            [SetUp]
            public new void TestInitialize()
            {
                replacementAndEstimatedSugars = new ReplacementAndEstimatedSugars
                {
                    Replacement = new Replacement
                    {
                        Items = new List<ReplacementItem> { new ReplacementItem() },
                        InsulinTotal
                            = new Insulin { NormalBolus = 2.5f, SquareWaveBolus = 2, SquareWaveBolusHours = 3 }
                    },
                    EstimatedSugars
                        = new List<Sugar> { new Sugar { BloodSugar = 100, DateTime = DateTime.Now.AddHours(2) },
                                            new Sugar { BloodSugar = 110, DateTime = DateTime.Now.AddHours(1) } }
                };
                meal.DateTime = DateTime.UtcNow.AddHours(0.5);
                facade.GetReplacementAndEstimatedSugars(meal,
                    Arg.Is<Insulin>(temp => temp.Id == insulin.Id),
                    Arg.Is<Sugar>(temp => temp.BloodSugar == 100f))
                    .Returns(replacementAndEstimatedSugars);
            }

            private void InitializeReplacementItems(IList<ReplacementItem> replacementItems)
            {
                foreach (var expectedItem in replacementItems)
                {
                    InitializePattern(expectedItem.Pattern);
                    foreach (var alternative in expectedItem.Alternatives)
                        InitializePattern(alternative);
                }
            }

            private void InitializePattern(Pattern pattern)
            {
                pattern.From.InitializeItems(new List<MealItem>());
                pattern.Insulin.InitializeCircumstances(new List<Guid>());
            }

            private void CheckTheCalculationAndTheSugarChartAreThere()
            {
                Assert.IsTrue(sut.IsCalculated);
                Assert.IsNotEmpty(sut.Calculated.Text);
                Assert.IsNotEmpty(sut.SugarChart);
            }

            private void CheckPatternViewModel(Pattern expected, PatternViewModel actual,
                IList<PatternViewModel> alternatives, bool tombstone)
            {
                if (!tombstone)
                {
                    Assert.AreSame(expected, actual.Pattern);
                    Assert.AreSame(expected.Match, actual.Match.Model);
                    Assert.AreSame(expected.From, actual.From.Meal);
                    Assert.AreSame(expected.Insulin, actual.Insulin.Insulin);
                    Assert.AreSame(expected.Before, actual.Before.Sugar);
                    Assert.AreSame(expected.After.ElementAt(1), actual.After[1].Sugar);
                    Assert.AreSame(expected.For, actual.For.Model);
                }
                Assert.IsNotNull(actual.Match.Scores.First);
                Assert.IsNotNull(actual.From.Scores.First);
                Assert.IsNotNull(actual.From.Name);
                actual.From.Meal.NameId = Guid.Empty;
                Assert.IsNotNull(actual.From.Name);
                Assert.IsNotNull(actual.Insulin.Circumstances);
                Assert.IsNotNull(actual.From.Products);
                Assert.IsNotEmpty(actual.Before.Text);
                Assert.IsNotEmpty(actual.After[1].Text);
                Assert.IsNotNull(actual.For.Scores.First);
                actual.Insulin.NormalBolus = "1";
                CheckPatternViewModelGoToMeal(expected, actual, tombstone: tombstone);
                CheckPatternViewModelGoToInsulin(expected, actual, tombstone: tombstone);
                CheckPatternViewModelShowAlternatives(actual, alternatives, tombstone: tombstone);
                if (tombstone)
                {
                    Assert.IsNotNull(actual.Match.ProductName);
                    actual.From.AddItem();
                    actual.Insulin.Insulin.AddCircumstance(new InsulinCircumstance());
                    Assert.IsNotEmpty(actual.Insulin.Insulin.Circumstances);
                    Assert.AreNotEqual(0, actual.Before.Sugar.BloodSugarInMgdL);
                    Assert.AreNotEqual(0, actual.After[1].Sugar.BloodSugarInMgdL);
                    Assert.IsNotNull(actual.For.ProductName);
                }
            }

            private void CheckPatternViewModelGoToMeal(Pattern expected, PatternViewModel actual, bool tombstone)
            {
                navigator.ClearReceivedCalls();
                sut.Subject.NormalBolus = "2.1";
                actual.GoToMeal.Call();
                if (!tombstone)
                    Assert.AreEqual(2.1, insulin.NormalBolus, 0.01);
                navigator.Received().GoToMealEditing(expected.From.Id);
            }

            private void CheckPatternViewModelGoToInsulin(Pattern expected, PatternViewModel actual, bool tombstone)
            {
                navigator.ClearReceivedCalls();
                sut.Subject.NormalBolus = "2.2";
                actual.GoToInsulin.Call();
                if (!tombstone)
                    Assert.AreEqual(2.2, insulin.NormalBolus, 0.01);
                navigator.Received().GoToInsulinEditing(expected.Insulin.Id);
            }

            private void CheckPatternViewModelShowAlternatives(PatternViewModel actual,
                IList<PatternViewModel> alternatives, bool tombstone)
            {
                if (!actual.HasAlternatives)
                {
                    Assert.Throws<InvalidOperationException>(() => actual.ShowAlternatives.Call());
                    return;
                }
                Assert.IsFalse(sut.CalculationDetailsAlternativesVisible);
                Assert.IsEmpty(sut.CalculationDetailsAlternatives);
                sut.ChangesProperty("CalculationDetailsAlternativesVisible", () =>
                {
                    sut.ChangesProperty("CalculationDetailsAlternatives", () =>
                    {
                        actual.ShowAlternatives.Call();
                    });
                });
                if (tombstone)
                    TombstoneCreateInitialize();
                Assert.IsTrue(sut.CalculationDetailsAlternativesVisible);
                if (!tombstone)
                    Assert.AreSame(alternatives, sut.CalculationDetailsAlternatives);
                Assert.AreEqual(alternatives.Select(pattern => pattern.Factor).ToArray(),
                    sut.CalculationDetailsAlternatives.Select(pattern => pattern.Factor).ToArray());
                sut.ChangesProperty("CalculationDetailsAlternativesVisible", () =>
                {
                    sut.CloseCalculationDetailsAlternatives();
                });
                Assert.IsFalse(sut.CalculationDetailsAlternativesVisible);
                if (!tombstone)
                {
                    actual.ShowAlternatives.Call();
                    sut.CloseCalculationDetailsOrAlternativesOnBackButton();
                    Assert.IsFalse(sut.CalculationDetailsAlternativesVisible);
                    Assert.IsTrue(sut.CalculationDetailsVisible);
                }
            }

            [Test]
            public void IsBusy()
            {
                Assert.IsFalse(sut.IsBusy);
                sut.ChangesProperty("IsBusy", () => { sut.IsBusy = true; });
                Assert.IsTrue(sut.IsBusy);
            }

            [Test]
            public void IsCalculatedIsFalseAfterOpen()
            {
                InitializeViewModel();
                Assert.IsFalse(sut.IsCalculated);
                Assert.IsFalse(sut.IsCalculationIncomplete);
                Assert.IsFalse(sut.IsCalculationEmpty);
            }

            [Test]
            public void CalculatedHasEmptyValueAfterOpen()
            {
                InitializeViewModel();
                Assert.IsEmpty(sut.Calculated.Text);
            }

            [Test]
            public void SugarChartIsEmptyAfterOpen()
            {
                InitializeViewModel();
                Assert.IsEmpty(sut.SugarChart);
            }

            [TestCase(true, false)]
            [TestCase(false, false)]
            [TestCase(true, true)]
            [TestCase(false, true)]
            public void CalculatesAfterSugarOrCircumstancesChanged(bool onSugarChange, bool openedWithNoBolus)
            {
                if (!onSugarChange)
                    sugar.BloodSugar = 100;
                if (openedWithNoBolus)
                    insulin.NormalBolus = insulin.SquareWaveBolus = 0;
                InitializeViewModel();
                sut.ChangesProperty("Calculated", () =>
                {
                    if (onSugarChange)
                        sut.CurrentSugar.BloodSugar = "100";
                    else
                        ChooseCircumstance();
                });
                Assert.AreEqual(2.5f, sut.Calculated.Insulin.NormalBolus);
                Assert.AreEqual(2f, sut.Calculated.Insulin.SquareWaveBolus);
                Assert.AreEqual(3f, sut.Calculated.Insulin.SquareWaveBolusHours);
                Assert.AreNotEqual(sut.Calculated.Text, sut.Subject.Text);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void SugarNeedsToBeEnteredForCalculation(bool openedWithNoBolus)
            {
                if (openedWithNoBolus)
                    insulin.NormalBolus = insulin.SquareWaveBolus = 0;
                InitializeViewModel();
                ChooseCircumstance();
                facade.DidNotReceiveWithAnyArgs().GetReplacementAndEstimatedSugars(null, null, null);
            }

            [Test]
            public void CalculationUpdatesIsCalculated()
            {
                InitializeViewModel();
                sut.ChangesProperty("IsCalculated", () =>
                {
                    sut.CurrentSugar.BloodSugar = "100";
                });
                Assert.IsTrue(sut.IsCalculated);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void CalculationUpdatesIsCalculationIncomplete(bool isComplete)
            {
                replacementAndEstimatedSugars.Replacement.IsComplete = isComplete;
                InitializeViewModel();
                sut.ChangesProperty("IsCalculationIncomplete", () =>
                {
                    sut.CurrentSugar.BloodSugar = "100";
                });
                Assert.AreNotEqual(isComplete, sut.IsCalculationIncomplete);
            }

            [Test]
            public void SetsIsBusyForTheTimeOfCalculation()
            {
                InitializeViewModel();
                sut.ChangesProperty("IsBusy", () =>
                {
                    sut.CurrentSugar.BloodSugar = "100";
                });
                Assert.IsFalse(sut.IsBusy);
            }

            [Test]
            public void WhenNoReplacementsFoundDoesNotShowCalculation()
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsNotEmpty(sut.Calculated.Text);
                replacementAndEstimatedSugars.Replacement.Items.Clear();
                ChooseCircumstance();
                Assert.IsEmpty(sut.Calculated.Text);
                Assert.IsFalse(sut.IsCalculated);
            }

            [Test]
            public void WhenNoReplacementsFoundDoesNotShowSugarChart()
            {
                InitializeViewModel();
                replacementAndEstimatedSugars.Replacement.Items.Clear();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsEmpty(sut.SugarChart);
            }

            [Test]
            public void WhenNoReplacementsFoundHidesPreviousCalculation()
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsNotEmpty(sut.Calculated.Text);
                Assert.IsTrue(sut.IsCalculationIncomplete);
                replacementAndEstimatedSugars.Replacement.Items.Clear();
                sut.ChangesProperty("IsCalculationIncomplete", () =>
                {
                    sut.ChangesProperty("IsCalculated", () =>
                    {
                        sut.ChangesProperty("Calculated", () =>
                        {
                            ChooseCircumstance();
                        });
                    });
                });
                Assert.IsEmpty(sut.Calculated.Text);
                Assert.IsFalse(sut.IsCalculated);
                Assert.IsFalse(sut.IsCalculationIncomplete);
            }

            [Test]
            public void WhenNoReplacementsFoundHidesPreviousSugarChart()
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsNotEmpty(sut.SugarChart);
                replacementAndEstimatedSugars.Replacement.Items.Clear();
                sut.ChangesProperty("SugarChart", () =>
                {
                    ChooseCircumstance();
                });
                Assert.IsEmpty(sut.SugarChart);
            }

            [Test]
            public void WhenNoReplacementsFoundSetsIsCalculationEmptyToTrueAndViceVersa()
            {
                InitializeViewModel();
                replacementAndEstimatedSugars.Replacement.Items.Clear();
                sut.ChangesProperty("IsCalculationEmpty", () =>
                {
                    sut.CurrentSugar.BloodSugar = "100";
                });
                Assert.IsTrue(sut.IsCalculationEmpty);
                replacementAndEstimatedSugars.Replacement.Items.Add(new ReplacementItem());
                sut.ChangesProperty("IsCalculationEmpty", () =>
                {
                    ChooseCircumstance();
                });
                Assert.IsFalse(sut.IsCalculationEmpty);
            }

            [Test]
            public void WhenABolusIsEditedKeepsTheCalculationAndTheSugarChart()
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                CheckTheCalculationAndTheSugarChartAreThere();
                sut.Subject.NormalBolus = "1.5";
                sut.Subject.SquareWaveBolus = "1.5";
                sut.Subject.SquareWaveBolusHours = "1.5";
                CheckTheCalculationAndTheSugarChartAreThere();
            }

            [Test]
            public void AfterBolusIsEditedDoesStillCalculate()
            {
                InitializeViewModel();
                sut.Subject.NormalBolus = "1.5";
                sut.Subject.SquareWaveBolus = "1.5";
                sut.Subject.SquareWaveBolusHours = "1.5";
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsTrue(sut.IsCalculated);
            }

            [Test]
            public void CalculationCanBeUpdated()
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.AreEqual(replacementAndEstimatedSugars.Replacement.InsulinTotal.NormalBolus,
                    sut.Calculated.Insulin.NormalBolus);
                replacementAndEstimatedSugars.Replacement.InsulinTotal.NormalBolus--;
                sut.ChangesProperty("Calculated", () => ChooseCircumstance());
                Assert.AreEqual(replacementAndEstimatedSugars.Replacement.InsulinTotal.NormalBolus,
                    sut.Calculated.Insulin.NormalBolus);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void WhenSugarAlreadyExistsCalculatesImmediately(bool openedWithNoBolus)
            {
                var sugar = new Sugar { BloodSugar = 100 };
                factories.Finder.FindSugarBeforeInsulin(insulin).Returns(sugar);
                if (openedWithNoBolus)
                    insulin.NormalBolus = insulin.SquareWaveBolus = 0;
                InitializeViewModel();
                Assert.IsTrue(sut.IsCalculated);
                facade.ReceivedWithAnyArgs().GetReplacementAndEstimatedSugars(null, null, null);
            }

            [Test]
            public void UsesRelatedMealIdWhenProvided()
            {
                var relatedMeal = new Meal { Id = Guid.NewGuid() };
                navigation.RelatedMealId = relatedMeal.Id;
                factories.Finder.FindMealById(relatedMeal.Id).Returns(relatedMeal);
                InitializeViewModel();
                var replacementAndEstimatedSugars = new ReplacementAndEstimatedSugars
                {
                    Replacement = new Replacement
                    {
                        InsulinTotal = new Insulin { NormalBolus = 1.5f },
                        Items = this.replacementAndEstimatedSugars.Replacement.Items
                    },
                    EstimatedSugars = this.replacementAndEstimatedSugars.EstimatedSugars
                };
                facade.GetReplacementAndEstimatedSugars(relatedMeal, Arg.Any<Insulin>(), Arg.Any<Sugar>())
                    .Returns(replacementAndEstimatedSugars);
                sut.CurrentSugar.BloodSugar = "100";
                Assert.AreEqual(1.5f, sut.Calculated.Insulin.NormalBolus);
            }

            [Test]
            public void CalculationPopulatesSugarChartWithCurrentAndEstimatedSugarsAfter()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                InitializeViewModel();
                sut.ChangesProperty("SugarChart", () =>
                {
                    sut.CurrentSugar.BloodSugar = "100";
                    Assert.AreEqual(3, sut.SugarChart.Count);
                    Assert.AreEqual(meal.DateTime, sut.SugarChart[0].DateTime.ToUniversalTime());
                    Assert.AreEqual(100, sut.SugarChart[0].BloodSugar);
                    Assert.AreEqual(estimatedSugars[1].DateTime, sut.SugarChart[1].DateTime);
                    Assert.AreEqual(estimatedSugars[1].BloodSugar, sut.SugarChart[1].BloodSugar);
                    Assert.AreEqual(estimatedSugars[0].DateTime, sut.SugarChart[2].DateTime);
                    Assert.AreEqual(estimatedSugars[0].BloodSugar, sut.SugarChart[2].BloodSugar);
                });
            }

            [Test]
            public void SugarChartMinumumReturnsMinimumOfChartMinus10WhenUnitIsMgdL()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                estimatedSugars[1].BloodSugar = 98.1f;
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.AreEqual(88.1f, sut.SugarChartMinimum);
            }

            [Test]
            public void SugarChartMinumumReturnsMinimumOfChartMinus0_55WhenUnitIsMmolL()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                estimatedSugars[1].BloodSugar = 95.1f;
                sugar.BloodSugar = 100;
                InitializeViewModel();
                settings.SugarUnit = SugarUnit.mmolL;
                ChooseCircumstance();
                Assert.AreEqual(94.55f, (double)sut.SugarChartMinimum, 0.001);
            }

            [Test]
            public void SugarChartMaximumReturnsMaximumOfChartPlus55WhenUnitIsMgdL()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                estimatedSugars[1].BloodSugar = 155.1f;
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.AreEqual(210.1f, sut.SugarChartMaximum);
            }

            [Test]
            public void SugarChartMaximumReturnsMaximumOfChartPlus33WhenUnitIsMgdLAndReduced()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                estimatedSugars[1].BloodSugar = 155.1f;
                InitializeViewModel();
                sut.ReducedSugarChartMargin = true;
                sut.CurrentSugar.BloodSugar = "100";
                Assert.AreEqual(188.1f, sut.SugarChartMaximum);
            }

            [Test]
            public void SugarChartMaximumReturnsMaximumOfChartPlus3_05WhenUnitIsMmolL()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                estimatedSugars[1].BloodSugar = 155.1f;
                sugar.BloodSugar = 100;
                InitializeViewModel();
                settings.SugarUnit = SugarUnit.mmolL;
                ChooseCircumstance();
                Assert.AreEqual(158.15f, (double)sut.SugarChartMaximum, 0.001);
            }

            [Test]
            public void SugarChartMaximumReturnsMaximumOfChartPlus1_83WhenUnitIsMmolLAndReduced()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                estimatedSugars[1].BloodSugar = 155.1f;
                sugar.BloodSugar = 100;
                InitializeViewModel();
                settings.SugarUnit = SugarUnit.mmolL;
                sut.ReducedSugarChartMargin = true;
                ChooseCircumstance();
                Assert.AreEqual(156.93f, (double)sut.SugarChartMaximum, 0.001);
            }

            [Test]
            public void SugarChartMinimumAndMaximumReturn100WhenChartIsEmpty()
            {
                InitializeViewModel();
                Assert.AreEqual(100, sut.SugarChartMinimum);
                Assert.AreEqual(100, sut.SugarChartMaximum);
            }

            [Test]
            public void SugarChartStartReturnsStartTimeMinusMargin()
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                var margin = InsulinEditingViewModel.SUGAR_CHART_TIME_MARGIN.TotalMinutes;
                Assert.AreEqual(meal.DateTime.ToLocalTime().AddMinutes(-margin), sut.SugarChartStart);
            }

            [Test]
            public void SugarChartEndReturnsEndTimePlusMargin()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                var margin = InsulinEditingViewModel.SUGAR_CHART_TIME_MARGIN.TotalMinutes;
                Assert.AreEqual(estimatedSugars[0].DateTime.AddMinutes(margin), sut.SugarChartEnd);
            }

            [Test]
            public void SugarChartStartAndEndReturnNowWhenChartIsEmpty()
            {
                InitializeViewModel();
                Assert.AreEqual(DateTime.Now.Ticks, sut.SugarChartStart.Ticks, TimeSpan.FromMinutes(1).Ticks);
                Assert.AreEqual(DateTime.Now.Ticks, sut.SugarChartEnd.Ticks, TimeSpan.FromMinutes(1).Ticks);
            }

            [TestCase(SugarUnit.mgdL)]
            [TestCase(SugarUnit.mmolL)]
            public void SugarChartAsTextReturnsTextualRepresentationOfChart(SugarUnit useSugarUnit)
            {
                sugar.BloodSugar = 100;
                settings.SugarUnit = useSugarUnit;
                string sugarUnit;
                if (useSugarUnit == SugarUnit.mgdL)
                    sugarUnit = Translations.BloodSugarMgdL;
                else
                    sugarUnit = Translations.BloodSugarMmolL;
                InitializeViewModel();
                var expected = Translations.EstimatedBloodSugar + Environment.NewLine;
                for (int i = 0; i < sut.SugarChart.Count; i++)
                {
                    expected += Environment.NewLine;
                    expected += sut.SugarChart[i].DateTime.ToString("t")
                        + "   " + sugarUnit.Replace("{0}", sut.SugarChart[i].BloodSugar.ToString());
                }
                Assert.IsNotEmpty(expected);
                var actual = sut.SugarChartAsText;
                Assert.AreEqual(expected, actual);
                sut.ShowSugarChartAsText.Call();
                messageDialog.Received().Show(expected);
            }

            [Test]
            public void ListOfMealItemsNotIncludedInCalculationReturnsListOfProductNamesAsText()
            {
                var fixture = new Fixture();
                var mealItems = fixture.CreateMany<MealItem>(3).ToList();
                var products = fixture.CreateMany<Product>(3).ToList();
                for (int i = 0; i < 3; i++)
                    factories.Finder.FindProductById(mealItems[i].ProductId).Returns(products[i]);
                factories.Products.Returns(products);
                meal.InitializeItems(mealItems);
                meal.SetOwner(factories);
                replacementAndEstimatedSugars.Replacement.Items[0].Pattern
                    = new Pattern { For = new MealItem { ProductId = mealItems[1].ProductId } };
                sugar.BloodSugar = 100;
                InitializeViewModel();
                var expected = Translations.IngredientsNotIncluded + Environment.NewLine;
                foreach (var i in new[] { 0, 2 })
                {
                    expected += Environment.NewLine;
                    expected += mealItems[i].Product.Name;
                }
                var actual = sut.ListOfMealItemsNotIncludedInCalculation;
                Assert.AreEqual(expected, actual);
                sut.ShowListOfMealItemsNotIncludedInCalculation.Call();
                messageDialog.Received().Show(expected);
            }

            [Test]
            public void CalculationChangesMinimumAndMaximumAndStartAndEnd()
            {
                var estimatedSugars = replacementAndEstimatedSugars.EstimatedSugars;
                InitializeViewModel();
                sut.ChangesProperty("SugarChartMinimum", () =>
                {
                    sut.ChangesProperty("SugarChartMaximum", () =>
                    {
                        sut.ChangesProperty("SugarChartStart", () =>
                        {
                            sut.ChangesProperty("SugarChartEnd", () =>
                            {
                                sut.CurrentSugar.BloodSugar = "100";
                            });
                        });
                    });
                });
            }

            [Test]
            public void TombstoneAndUntombstoneCalculationResults()
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                sut.Tombstone();
                Assert.IsFalse(stateProvider.State.Any(kvp => kvp.Value is Insulin),
                    "Don't wait for the runtime to serialize it.");
                CreateSut();
                InitializeViewModel();
                Assert.IsTrue(sut.IsCalculated);
                Assert.IsTrue(sut.IsCalculationIncomplete);
                Assert.IsNotEmpty(sut.Calculated.Text);
                Assert.AreEqual(3, sut.SugarChart.Count);
                Assert.AreEqual(100, sut.SugarChart[0].BloodSugar);
            }

            [TestCase(true)]
            [TestCase(false)]
            public void CalculationWorksAfterTombstoneAndUntombstone(bool bolusWasEdited)
            {
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                if (bolusWasEdited)
                    sut.Subject.NormalBolus = "1";
                sut.Tombstone();
                CreateSut();
                facade.ClearReceivedCalls();
                InitializeViewModel();
                facade.DidNotReceive().GetReplacementAndEstimatedSugars(Arg.Any<Meal>(), Arg.Any<Insulin>(),
                    Arg.Any<Sugar>());
                ChooseCircumstance();
                facade.Received().GetReplacementAndEstimatedSugars(Arg.Any<Meal>(), Arg.Any<Insulin>(),
                    Arg.Any<Sugar>());
                CheckTheCalculationAndTheSugarChartAreThere();
            }

            [Test]
            public void UseCalculationSetsSubjectAndPivot()
            {
                sugar.BloodSugar = 100;
                InitializeViewModel();
                sut.Pivot = 1;
                sut.Subject.ChangesProperty("NormalBolus", () =>
                    sut.Subject.ChangesProperty("SquareWaveBolus", () =>
                        sut.Subject.ChangesProperty("SquareWaveBolusHours", () =>
                            sut.ChangesProperty("Pivot", () =>
                                sut.UseCalculation.Call()))));
                Assert.AreEqual(sut.Calculated.Text, sut.Subject.Text);
                Assert.AreEqual(0, sut.Pivot);
            }

            [TestCase(false)]
            [TestCase(true)]
            public void CalculationDetailsAndCloseCalculationDetailsSetCalculationDetailsVisible(bool tombstone)
            {
                var fixture = new Fixture();
                replacementAndEstimatedSugars.Replacement.Items = fixture.CreateMany<ReplacementItem>().ToList();
                InitializeReplacementItems(replacementAndEstimatedSugars.Replacement.Items);
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsFalse(sut.CalculationDetailsVisible);
                sut.ChangesProperty("CalculationDetailsVisible", () =>
                {
                    sut.CalculationDetails.Call();
                });
                if (tombstone)
                    TombstoneCreateInitialize();
                Assert.IsTrue(sut.CalculationDetailsVisible);
                sut.ChangesProperty("CalculationDetailsVisible", () =>
                {
                    sut.CloseCalculationDetails();
                });
                Assert.IsFalse(sut.CalculationDetailsVisible);
                sut.CalculationDetails.Call();
                sut.CloseCalculationDetailsOrAlternativesOnBackButton();
                Assert.IsFalse(sut.CalculationDetailsVisible);
            }

            [TestCase(false)]
            [TestCase(true)]
            public void CalculationDetailsPopulatesReplacementItems(bool tombstone)
            {
                var fixture = new Fixture();
                var expected = fixture.CreateMany<ReplacementItem>().ToList();
                factories.Finder.FindProductById(Arg.Any<Guid>()).Returns(fixture.Create<Product>());
                factories.Finder.FindInsulinCircumstanceById(Arg.Any<Guid>()).Returns(new InsulinCircumstance());
                InitializeReplacementItems(expected);
                expected[1].Pattern.Factor = 1.177F;
                expected[2].Alternatives.Clear();
                replacementAndEstimatedSugars.Replacement.Items = expected;
                InitializeViewModel();
                sut.CurrentSugar.BloodSugar = "100";
                Assert.IsEmpty(sut.ReplacementItems);
                sut.ChangesProperty("ReplacementItems", () =>
                {
                    sut.CalculationDetails.Call();
                });
                if (tombstone)
                {
                    TombstoneCreateInitialize();
                    Assert.IsFalse(stateProvider.State.Any(kvp => kvp.Value is IList<ReplacementItem>));
                }
                var actual = sut.ReplacementItems;
                CheckPatternViewModel(expected[1].Pattern, actual[1].Pattern,
                    alternatives: actual[1].Alternatives, tombstone: tombstone);
                CheckPatternViewModel(expected[1].Alternatives[1], actual[1].Alternatives[1],
                    alternatives: new List<PatternViewModel>(), tombstone: tombstone);
                Assert.IsTrue(actual[1].Pattern.HasAlternatives);
                Assert.IsFalse(actual[2].Pattern.HasAlternatives);
                Assert.IsFalse(actual[1].Alternatives[1].HasAlternatives);
                Assert.AreEqual("118%", actual[1].Pattern.Factor);
            }

            [Test]
            public void CalculationDetailsThrowsExceptionIfNoCalculation()
            {
                InitializeViewModel();
                Assert.Throws<InvalidOperationException>(() => sut.CalculationDetails.Call());
            }
        }

        public class SugarChartItemViewModelTests
        {
            [Test]
            public void ConvertsTimeToLocal()
            {
                var sugar = new Sugar { DateTime = DateTime.UtcNow };
                var sut = new InsulinEditingViewModel.SugarChartItemViewModel(sugar);
                Assert.AreEqual(sugar.DateTime.ToLocalTime(), sut.DateTime);
            }
        }
    }
}
