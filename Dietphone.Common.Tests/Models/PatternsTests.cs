﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Moq;
using System.Text.RegularExpressions;

namespace Dietphone.Models.Tests
{
    public class PatternsTests
    {
        private Factories factories;
        private DateTime basedate;

        [SetUp]
        public void InitializeOwner()
        {
            basedate = new DateTime(2013, 07, 24);
            factories = new FactoriesImpl();
            factories.StorageCreator = new StorageCreatorStub();
            factories.Settings.SugarsAfterInsulinHours = 4;
            AddProduct(1, energy: 100, carbs: 100, protein: 100, fat: 100);
            AddProduct(2, energy: 100, carbs: 100, protein: 100, fat: 100);
            AddProduct(3, energy: 100, carbs: 100, protein: 100, fat: 100);
        }

        private PatternsImpl CreateSut()
        {
            var hourDifference = new Mock<HourDifference>();
            hourDifference
                .Setup(h => h.GetDifference(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
                .Returns(12);
            return new PatternsImpl(factories, hourDifference.Object);
        }

        private Product AddProduct(byte productId, byte energy, byte carbs, byte protein, byte fat)
        {
            var product = factories.CreateProduct();
            product.Id = productId.ToGuid();
            product.EnergyPer100g = energy;
            product.CarbsTotalPer100g = carbs;
            product.ProteinPer100g = protein;
            product.FatPer100g = fat;
            return product;
        }

        private Meal AddMealInsulinAndSugars(string meal, string insulinWithoutHour,
            string sugarsBeforeAndAfterWithoutHours = "")
        // e.g. ("12:00", "1", "100 120 140") -> ("12:00", "12:00 1", "12:00 100" "13:00 120" "14:00 140")
        {
            var addedMeal = AddMeal(meal);
            var mealHour = meal.Split(' ').First();
            AddInsulin(mealHour + " " + insulinWithoutHour);
            var sugarsSplet = sugarsBeforeAndAfterWithoutHours.Split(' ');
            for (int i = 0; i < sugarsSplet.Count(); i++)
            {
                var sugarHour = (TimeSpan.Parse(mealHour) + TimeSpan.FromHours(i)).ToString();
                AddSugars(sugarHour + " " + sugarsSplet[i]);
            }
            return addedMeal;
        }

        private Meal AddMeal(string hourAndProductIdsAndValues) // e.g. "12:00 1 100g 2 100g"
        {
            var splet = hourAndProductIdsAndValues.Split(' ');
            var meal = factories.CreateMeal();
            meal.DateTime = basedate + TimeSpan.Parse(splet[0]);
            for (int i = 1; i < splet.Count(); i += 2)
            {
                var item = meal.AddItem();
                item.ProductId = splet[i].ToGuid();
                var match = Regex.Match(splet[i + 1], @"(\d*)(\D*)");
                var value = match.Groups[1].Value;
                var unit = match.Groups[2].Value;
                item.Value = int.Parse(value);
                item.Unit = item.Unit.TryGetValueOfAbbreviation(unit);
            }
            return meal;
        }

        private Insulin AddInsulin(string hourAndNormalBolusAndSquareWaveBolusAndSquareWaveBolusHoursAndCircumstances)
        // e.g. "12:00 1", "12:00 1 0 0", "12:00 2 2 3 1 2 3"
        {
            var splet = hourAndNormalBolusAndSquareWaveBolusAndSquareWaveBolusHoursAndCircumstances.Split(' ');
            var insulin = factories.CreateInsulin();
            insulin.DateTime = basedate + TimeSpan.Parse(splet[0]);
            insulin.NormalBolus = int.Parse(splet[1]);
            if (splet.Length > 2)
            {
                insulin.SquareWaveBolus = int.Parse(splet[2]);
                insulin.SquareWaveBolusHours = int.Parse(splet[3]);
                foreach (var circumstanceId in splet.Skip(4).Select(s => byte.Parse(s).ToGuid()))
                    insulin.AddCircumstance(new InsulinCircumstance { Id = circumstanceId });
            }
            return insulin;
        }

        private List<Sugar> AddSugars(string hoursAndBloodSugars) // e.g. "12:00 100", "12:00 100 14:00 120"
        {
            var sugars = new List<Sugar>();
            var splet = hoursAndBloodSugars.Split(' ');
            for (int i = 0; i < splet.Count(); i += 2)
            {
                var sugar = factories.CreateSugar();
                sugar.DateTime = basedate + TimeSpan.Parse(splet[i]);
                sugar.BloodSugar = int.Parse(splet[i + 1]);
                sugars.Add(sugar);
            }
            return sugars;
        }

        [Test]
        public void IfNoMealForQueredInsulinThenReturnsEmpty()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreEqual(0, patterns.Count());
        }

        [Test]
        public void IfNoMatchingItemThenReturnsEmpty()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            AddMealInsulinAndSugars("10:00 2 100g", "1", "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreEqual(0, patterns.Count());
        }

        [Test]
        public void IfMatchingItemThenReturnsIt()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            var mealToFind = AddMealInsulinAndSugars("10:00 1 100g", "1", "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreSame(mealToFind.Items[0], patterns.Single().Match);
            Assert.AreSame(mealToFind, patterns.Single().From);
        }

        [Test]
        public void ReturnsMatchesForEveryItemInMeal()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g 2 200g");
            var mealToFind = AddMealInsulinAndSugars("10:00 1 100g 2 200g", "1", "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreEqual(2, patterns.Count);
            Assert.AreSame(mealToFind.Items[0], patterns[0].Match);
            Assert.AreSame(mealToFind.Items[1], patterns[1].Match);
            Assert.AreSame(mealToFind, patterns[0].From);
            Assert.AreSame(mealToFind, patterns[1].From);
        }

        [Test]
        public void ReturnsOnlyItemsHavingSimillarPercentageOfEnergyInMeal()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g 2 100g");
            var mealToFind1 = AddMealInsulinAndSugars("06:00 1 1000g 3 1000g", "1", "100 100");
            var mealNotFind = AddMealInsulinAndSugars("08:00 1 160g 3 100g", "1", "100 100");
            var mealToFind2 = AddMealInsulinAndSugars("10:00 1 150g 3 100g", "1", "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.IsTrue(patterns.Any(p => p.From == mealToFind1), "Same percentage so should match");
            Assert.IsFalse(patterns.Any(p => p.From == mealNotFind), "Percentage different by 11 so should fail");
            Assert.IsTrue(patterns.Any(p => p.From == mealToFind2), "Percentage different by no more than 10 so matches");
        }

        [Test]
        public void MoreSimillarPercentageOfEnergyInMealGivesMoreRightnessPoints()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g 2 100g");
            AddMealInsulinAndSugars("07:00 1 100g 3 100g", "1", "100 100");
            AddMealInsulinAndSugars("07:00 1 116g 3 84g", "1", "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreEqual(8, patterns[0].RightnessPoints - patterns[1].RightnessPoints, "10-2 so should be 8 points");
        }

        [Test]
        public void IfThereIsNoInsulinForFoundMealThenSkipsThatMeal()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            AddMeal("07:00 1 100g");
            AddSugars("07:00 100 08:00 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.IsEmpty(patterns);
        }

        [Test]
        public void IfThereIsInsulinForFoundMealThenReturnsThatMealWithInsulin()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            var mealToFind = AddMeal("07:00 1 100g");
            var insulinToFind = AddInsulin("07:00 1");
            AddSugars("07:00 100 08:00 100");
            var pattern = sut.GetPatternsFor(insulin).Single();
            Assert.AreSame(mealToFind, pattern.From);
            Assert.AreSame(insulinToFind, pattern.Insulin);
        }

        [Test]
        public void IfSugarBeforeCannotBeFoundThenMealIsNotReturned()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            AddMeal("07:00 1 100g");
            AddInsulin("07:00 1");
            AddSugars("08:00 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.IsEmpty(patterns);
        }

        [Test]
        public void IfSugarBeforeCanBeFoundThenMealIsReturnedWithThatSugar()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            var mealToFind = AddMeal("07:00 1 100g");
            AddInsulin("07:00 1");
            var sugarToFind = AddSugars("07:00 100 08:00 100").First();
            var pattern = sut.GetPatternsFor(insulin).Single();
            Assert.AreSame(mealToFind, pattern.From);
            Assert.AreSame(sugarToFind, pattern.Before);
        }

        [Test]
        public void IfSugarsAfterCannotBeFoundThenMealIsNotReturned()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            AddMealInsulinAndSugars("07:00 1 100g", "1", "100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.IsEmpty(patterns);
        }

        [Test]
        public void IfSugarsAfterCanBeFoundThenMealIsReturnedWithThoseSugars()
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            var mealToFind = AddMealInsulinAndSugars("07:00 1 100g", "1", "100");
            var sugarsToFind = AddSugars("07:30 120 08:30 125");
            var pattern = sut.GetPatternsFor(insulin).Single();
            Assert.AreSame(mealToFind, pattern.From);
            Assert.AreEqual(sugarsToFind, pattern.After);
        }

        [Test]
        public void SettingIsUsedToDetermineTimeWindowForSugarsAfter()
        {
            factories.Settings.SugarsAfterInsulinHours = 1;
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            AddMealInsulinAndSugars("07:00 1 100g", "1", "100");
            var sugarsToFind = AddSugars("08:00 120");
            AddSugars("09:00 120");
            var pattern = sut.GetPatternsFor(insulin).Single();
            Assert.AreEqual(sugarsToFind, pattern.After, "Only sugars in 1 hours after should be returned");
        }

        [TestCase(360, 361, 1, Description = "In 360 days = 1 extra point")]
        [TestCase(180, 181, 1, Description = "In 180 days = 1 extra point")]
        [TestCase(90, 91, 1, Description = "In 90 days = 1 extra point")]
        [TestCase(60, 61, 1, Description = "In 60 days = 1 extra point")]
        [TestCase(30, 31, 1, Description = "In 30 days = 1 extra point")]
        [TestCase(15, 16, 1, Description = "In 15 days = 1 extra point")]
        [TestCase(7, 8, 1, Description = "In 7 days = 1 extra point")]
        [TestCase(2, 3, 1, Description = "In 2 days = 1 extra point")]
        [TestCase(1, 2.1, 1, Description = "In 2 days = 1 extra point (by 0.1 day)")]
        public void RecentMealGetsMoreRightnessPoints(double addDays1, double addDays2, int expectedPointsDifference)
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1");
            AddMeal("12:00 1 100g");
            basedate = basedate.AddDays(addDays1);
            AddMealInsulinAndSugars("12:00 1 100g", "1", "100 100");
            basedate = basedate.AddDays(-addDays1).AddDays(addDays2);
            AddMealInsulinAndSugars("12:00 1 100g", "1", "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreEqual(expectedPointsDifference, patterns[0].RightnessPoints - patterns[1].RightnessPoints);
        }

        [TestCase("17:00", "05:00", 12, "17:00", Description = "00:00 and 12:00 difference = 12 extra points (12-0)")]
        [TestCase("17:30", "05:00", 12, "17:00", Description = "00:30 and 12:00 difference = 12 extra points (12-0)")]
        [TestCase("17:31", "05:00", 11, "17:00", Description = "00:31 and 12:00 difference = 11 extra points (11-0)")]
        [TestCase("08:00", "15:00", 05, "09:00", Description = "01:00 and 06:00 difference = 05 extra points (11-6)")]
        [TestCase("06:00", "21:00", -1, "14:00", Description = "08:00 and 07:00 difference = -1 extra points (4-5)")]
        [TestCase("08:00", "10:01", 00, "08:31", Description = "00:31 and 01:30 difference = 00 extra points (11-11)")]
        [TestCase("04:00", "08:00", -2, "07:10", Description = "03:10 and 00:50 difference = -2 extra points (9-11)")]
        [TestCase("00:00", "00:00", 00, "12:00", Description = "12:00 and 12:00 difference = 00 extra points (0-0)")]
        [TestCase("01:00", "23:00", 00, "12:00", Description = "11:00 and 11:00 difference = 00 extra points (1-1)")]
        [TestCase("02:00", "23:00", -1, "00:00", Description = "02:00 and 01:00 difference = -1 extra points (10-11)")]
        [TestCase("22:00", "05:00", 01, "01:00", Description = "03:00 and 04:00 difference = 01 extra points (9-8)")]
        [TestCase("22:31", "05:31", 01, "01:30", Description = "03:00 and 04:00 difference = 01 extra points (9-8)")]
        public void MealAtSimillarHourGetsMoreRightnessPoints(string hour1, string hour2,
            int expectedPointsDifference, string idealHour)
        {
            var sut = new PatternsImpl(factories, new HourDifferenceImpl());
            var insulin = AddInsulin(idealHour + " 1");
            AddMeal(idealHour + " 1 100g");
            basedate = basedate.AddDays(1); // To avoid same date time of searched and found
            AddMealInsulinAndSugars(hour1 + " 1 100g", "1", "100 100");
            AddMealInsulinAndSugars(hour2 + " 1 100g", "1", "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreEqual(expectedPointsDifference, patterns[0].RightnessPoints - patterns[1].RightnessPoints);
        }

        [TestCase("", "", 0)]
        [TestCase("1 2 3", "1 2 3", 0)]
        [TestCase("3", "", 5)]
        [TestCase("10", "", 0)]
        [TestCase("1 2 3", "", 15)]
        [TestCase("", "1 2 3", -15)]
        [TestCase("1 2 3", "4 5 6", 15)]
        [TestCase("1 2 3", "4 5 6 1", 10)]
        [TestCase("1 2 3", "2 1", 5)]
        public void InsulinWithSameCircumstancesGetsMoreRightessPoints(string circumstances1, string circumstances2,
            int expectedPointsDifference)
        {
            var sut = CreateSut();
            var insulin = AddInsulin("12:00 1 0 0 1 2 3");
            AddMeal("12:00 1 100g");
            AddMealInsulinAndSugars("07:00 1 100g", ("1 0 0 " + circumstances1).Trim(), "100 100");
            AddMealInsulinAndSugars("09:00 1 100g", ("1 0 0 " + circumstances2).Trim(), "100 100");
            var patterns = sut.GetPatternsFor(insulin);
            Assert.AreEqual(expectedPointsDifference, patterns[0].RightnessPoints - patterns[1].RightnessPoints);
        }
    }
}
