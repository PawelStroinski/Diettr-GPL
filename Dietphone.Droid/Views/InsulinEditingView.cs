using System.ComponentModel;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using Dietphone.Controls;
using Dietphone.Tools;
using Dietphone.ViewModels;
using MvvmCross.Platform;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Xamarin.Android;

namespace Dietphone.Views
{
    [Activity]
    public class InsulinEditingView : ActivityBase<InsulinEditingViewModel>
    {
        private IMenuItem save, cancel, meal, copy, delete;
        private DateTimeAxis sugarChartDateTimeAxis;
        private LinearAxis sugarChartLinearAxis;
        private LineSeries sugarChartSeries;
        private PlotModel sugarChartOptions;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            InitializeViewModel();
            SetContentView(Resource.Layout.InsulinEditingView);
            Title = Translations.Insulin.Capitalize();
            this.InitializeTabs(Translations.General, Translations.Suggestion, Translations.Date);
            FormatSuggestedInsulinHeader();
            InitializeSugarChart();
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            ViewModel.ReturnedFromNavigation();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.insulineditingview_menu, menu);
            GetMenu(menu);
            TranslateMenu();
            BindMenuActions();
            BindMenuEnabled();
            return true;
        }

        private void InitializeViewModel()
        {
            ViewModel.ReducedSugarChartMargin = true;
            if (ViewModel.Navigator == null)
            {
                ViewModel.Navigator = Mvx.Resolve<Navigator>();
                ViewModel.Load();
            }
            else
                ViewModel.ReturnedFromNavigation();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void FormatSuggestedInsulinHeader()
        {
            var html = $@"
                <font color='#{this.ResourceColorToHex(Resource.Color.extreme_foreground)}'>
                    {Translations.SuggestedInsulinHeaderWarning}
                </font>
                {Translations.SuggestedInsulinHeader}
                <font color='#ff0000'>
                    {Translations.SuggestedInsulinHeaderWarning2}
                </font>";
            var target = FindViewById<TextView>(Resource.Id.suggested_insulin_header);
            target.TextFormatted = Html.FromHtml(html);
        }

        private void InitializeSugarChart()
        {
            sugarChartDateTimeAxis = new ExactDateTimeAxis(() => ViewModel.SugarChart.Select(item => item.DateTime))
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm",
                IsPanEnabled = false,
                IsZoomEnabled = false,
                TicklineColor = OxyColors.Transparent
            };
            sugarChartLinearAxis = new LinearAxis
            {
                IsAxisVisible = false,
                IsPanEnabled = false,
                IsZoomEnabled = false
            };
            sugarChartSeries = new LineSeries
            {
                DataFieldX = "DateTime",
                DataFieldY = "BloodSugar",
                LabelFormatString = "{1}",
                Smooth = true,
                Color = this.ResourceColorToOxyColor(Resource.Color.accent_material_dark)
            };
            sugarChartOptions = new PlotModel
            {
                Background = OxyColors.Transparent,
                TextColor = this.ResourceColorToOxyColor(Resource.Color.extreme_foreground),
                PlotAreaBorderColor = OxyColors.Transparent,
                DefaultFontSize = 8.5.ToPx(ComplexUnitType.Sp, this)
            };
            sugarChartOptions.Axes.Add(sugarChartDateTimeAxis);
            sugarChartOptions.Axes.Add(sugarChartLinearAxis);
            sugarChartOptions.Series.Add(sugarChartSeries);
            var sugarChart = FindViewById<PlotView>(Resource.Id.sugar_chart);
            sugarChart.Model = sugarChartOptions;
        }

        private void GetMenu(IMenu menu)
        {
            save = menu.FindItem(Resource.Id.insulineditingview_save);
            cancel = menu.FindItem(Resource.Id.insulineditingview_cancel);
            meal = menu.FindItem(Resource.Id.insulineditingview_meal);
            copy = menu.FindItem(Resource.Id.insulineditingview_copy);
            delete = menu.FindItem(Resource.Id.insulineditingview_delete);
        }

        private void TranslateMenu()
        {
            save.SetTitleCapitalized(Translations.Save);
            cancel.SetTitleCapitalized(Translations.Cancel);
            meal.SetTitleCapitalized(Translations.Meal);
            copy.SetTitleCapitalized(Translations.Copy);
            delete.SetTitleCapitalized(Translations.Delete);
        }

        private void BindMenuActions()
        {
            save.SetOnMenuItemClick(ViewModel.SaveAndReturn);
            cancel.SetOnMenuItemClick(ViewModel.CancelAndReturn);
            meal.SetOnMenuItemClick(ViewModel.GoToMealEditing);
            copy.SetOnMenuItemClick(ViewModel.CopyAsText);
            delete.SetOnMenuItemClick(ViewModel.DeleteAndSaveAndReturn);
        }

        private void BindMenuEnabled()
        {
            save.BindSaveEnabled(ViewModel);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SugarChart")
                InvalidateSugarChart();
        }

        private void InvalidateSugarChart()
        {
            sugarChartDateTimeAxis.Minimum = DateTimeAxis.ToDouble(ViewModel.SugarChartStart);
            sugarChartDateTimeAxis.Maximum = DateTimeAxis.ToDouble(ViewModel.SugarChartEnd);
            sugarChartLinearAxis.Minimum = (double)ViewModel.SugarChartMinimum;
            sugarChartLinearAxis.Maximum = (double)ViewModel.SugarChartMaximum;
            var hadSource = sugarChartSeries.ItemsSource != null;
            sugarChartSeries.ItemsSource = ViewModel.SugarChart;
            if (hadSource)
                sugarChartOptions.InvalidatePlot(true);
        }
    }
}