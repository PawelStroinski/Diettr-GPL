﻿using Dietphone.Models;
using Dietphone.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Telerik.Windows.Data;
using System.Threading;
using Dietphone.Tools;

namespace Dietphone.ViewModels
{
    public class ProductListingViewModel : SubViewModel
    {
        public ObservableCollection<ProductViewModel> Products { get; private set; }
        public ObservableCollection<CategoryViewModel> Categories { get; private set; }
        public ObservableCollection<DataDescriptor> GroupDescriptors { private get; set; }
        public ObservableCollection<DataDescriptor> SortDescriptors { private get; set; }
        public ObservableCollection<DataDescriptor> FilterDescriptors { private get; set; }
        public event EventHandler DescriptorsUpdating;
        public event EventHandler DescriptorsUpdated;
        public event EventHandler Refreshing;
        public event EventHandler Refreshed;
        private Factories factories;
        private MaxCuAndFpuInCategories maxCuAndFpu;
        private ProductViewModel selectedProduct;

        public ProductListingViewModel(Factories factories)
        {
            this.factories = factories;
            maxCuAndFpu = new MaxCuAndFpuInCategories(factories.Finder);
        }

        public ProductViewModel SelectedProduct
        {
            get
            {
                return selectedProduct;
            }
            set
            {
                if (selectedProduct != value)
                {
                    selectedProduct = value;
                    OnSelectedProductChanged();
                }
            }
        }

        public override void Load()
        {
            if (Categories == null && Products == null)
            {
                var loader = new CategoriesAndProductsLoader(this);
                loader.LoadAsync();
            }
        }

        public override void Refresh()
        {
            OnRefreshing();
            maxCuAndFpu.Reset();
            var loader = new CategoriesAndProductsLoader(this);
            loader.LoadAsync();
            loader.Loaded += delegate { OnRefreshed(); };
        }

        public override void Add()
        {
            var product = factories.CreateProduct();
            Navigator.GoToProductEditing(product.Id);
        }

        public void UpdateGroupDescriptors()
        {
            GroupDescriptors.Clear();
            var groupByCategory = new GenericGroupDescriptor<ProductViewModel, CategoryViewModel>(FindCategoryFromProduct);
            GroupDescriptors.Add(groupByCategory);
        }

        public void UpdateSortDescriptors()
        {
            SortDescriptors.Clear();
            var sortByName = new GenericSortDescriptor<ProductViewModel, string>(product => product.Name);
            SortDescriptors.Add(sortByName);
        }

        public ProductViewModel FindProduct(Guid productId)
        {
            var result = from product in Products
                         where product.Id == productId
                         select product;
            return result.FirstOrDefault();
        }

        public CategoryViewModel FindCategory(Guid categoryId)
        {
            var result = from category in Categories
                         where category.Id == categoryId
                         select category;
            return result.FirstOrDefault();
        }

        protected override void OnSearchChanged()
        {
            OnDescriptorsUpdating();
            UpdateFilterDescriptors();
            OnDescriptorsUpdated();
        }

        private void UpdateFilterDescriptors()
        {
            FilterDescriptors.Clear();
            if (!string.IsNullOrEmpty(search))
            {
                var filterByName = new GenericFilterDescriptor<ProductViewModel>(product => product.Name.ContainsIgnoringCase(search));
                FilterDescriptors.Add(filterByName);
            }
        }

        private CategoryViewModel FindCategoryFromProduct(ProductViewModel product)
        {
            if (product == null)
            {
                throw new NullReferenceException("product");
            }
            var result = from category in Categories
                         where category.Id == product.Product.CategoryId
                         select category;
            return result.FirstOrDefault();
        }

        protected void OnSelectedProductChanged()
        {
            if (SelectedProduct != null)
            {
                Navigator.GoToProductEditing(SelectedProduct.Id);
            }
        }

        protected void OnDescriptorsUpdating()
        {
            if (DescriptorsUpdating != null)
            {
                DescriptorsUpdating(this, EventArgs.Empty);
            }
        }

        protected void OnDescriptorsUpdated()
        {
            if (DescriptorsUpdated != null)
            {
                DescriptorsUpdated(this, EventArgs.Empty);
            }
        }

        protected void OnRefreshing()
        {
            if (Refreshing != null)
            {
                Refreshing(this, EventArgs.Empty);
            }
        }

        protected void OnRefreshed()
        {
            if (Refreshed != null)
            {
                Refreshed(this, EventArgs.Empty);
            }
        }

        public class CategoriesAndProductsLoader
        {
            public event EventHandler Loaded;
            private ObservableCollection<CategoryViewModel> categories = new ObservableCollection<CategoryViewModel>();
            private ObservableCollection<ProductViewModel> products = new ObservableCollection<ProductViewModel>();
            private ProductListingViewModel viewModel;
            private Factories factories;
            private MaxCuAndFpuInCategories maxCuAndFpu;
            private bool isLoading;

            public CategoriesAndProductsLoader(ProductListingViewModel viewModel)
            {
                this.viewModel = viewModel;
                factories = viewModel.factories;
                maxCuAndFpu = viewModel.maxCuAndFpu;
            }

            public CategoriesAndProductsLoader(Factories factories)
            {
                this.factories = factories;
            }

            public void LoadAsync()
            {
                if (viewModel == null)
                {
                    throw new InvalidOperationException("Use other constructor for this operation.");
                }
                if (isLoading)
                {
                    return;
                }
                var worker = new BackgroundWorker();
                worker.DoWork += delegate { DoWork(); };
                worker.RunWorkerCompleted += delegate { WorkCompleted(); };
                viewModel.IsBusy = true;
                isLoading = true;
                worker.RunWorkerAsync();
            }

            public ObservableCollection<CategoryViewModel> GetCategoriesReloaded()
            {
                categories.Clear();
                LoadCategories();
                return categories;
            }

            private void DoWork()
            {
                LoadCategories();
                LoadProducts();
            }

            private void WorkCompleted()
            {
                AssignCategories();
                AssignProducts();
                viewModel.IsBusy = false;
                isLoading = false;
                OnLoaded();
            }

            private void LoadCategories()
            {
                var models = factories.Categories;
                var unsortedViewModels = new List<CategoryViewModel>();
                foreach (var model in models)
                {
                    var viewModel = new CategoryViewModel(model);
                    unsortedViewModels.Add(viewModel);
                }
                var sortedViewModels = unsortedViewModels.OrderBy(category => category.Name);
                foreach (var viewModel in sortedViewModels)
                {
                    categories.Add(viewModel);
                }
            }

            private void LoadProducts()
            {
                var models = factories.Products;
                foreach (var model in models)
                {
                    var viewModel = new ProductViewModel(model, maxCuAndFpu);
                    products.Add(viewModel);
                }
            }

            private void AssignCategories()
            {
                viewModel.Categories = categories;
                viewModel.OnPropertyChanged("Categories");
            }

            private void AssignProducts()
            {
                viewModel.Products = products;
                viewModel.OnPropertyChanged("Products");
            }

            protected void OnLoaded()
            {
                if (Loaded != null)
                {
                    Loaded(this, EventArgs.Empty);
                }
            }
        }
    }
}
