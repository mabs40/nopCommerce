﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Web.Areas.Admin.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Factories;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the category model factory implementation
    /// </summary>
    public partial class CategoryModelFactory : ICategoryModelFactory
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly IAclSupportedModelFactory _aclSupportedModelFactory;
        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly ICategoryService _categoryService;
        private readonly IDiscountService _discountService;
        private readonly IDiscountSupportedModelFactory _discountSupportedModelFactory;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedModelFactory _localizedModelFactory;
        private readonly IProductService _productService;
        private readonly IStoreMappingSupportedModelFactory _storeMappingSupportedModelFactory;

        #endregion

        #region Ctor

        public CategoryModelFactory(CatalogSettings catalogSettings,
            IAclSupportedModelFactory aclSupportedModelFactory,
            IBaseAdminModelFactory baseAdminModelFactory,
            ICategoryService categoryService,
            IDiscountService discountService,
            IDiscountSupportedModelFactory discountSupportedModelFactory,
            ILocalizationService localizationService,
            ILocalizedModelFactory localizedModelFactory,
            IProductService productService,
            IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory)
        {
            this._catalogSettings = catalogSettings;
            this._aclSupportedModelFactory = aclSupportedModelFactory;
            this._baseAdminModelFactory = baseAdminModelFactory;
            this._categoryService = categoryService;
            this._discountService = discountService;
            this._discountSupportedModelFactory = discountSupportedModelFactory;
            this._localizationService = localizationService;
            this._localizedModelFactory = localizedModelFactory;
            this._productService = productService;
            this._storeMappingSupportedModelFactory = storeMappingSupportedModelFactory;
        }

        #endregion
        
        #region Methods

        /// <summary>
        /// Prepare category search model
        /// </summary>
        /// <param name="model">Category search model</param>
        /// <returns>Category search model</returns>
        public virtual CategorySearchModel PrepareCategorySearchModel(CategorySearchModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //prepare available stores
            _baseAdminModelFactory.PrepareStores(model.AvailableStores);

            return model;
        }

        /// <summary>
        /// Prepare paged category list model
        /// </summary>
        /// <param name="searchModel">Category search model</param>
        /// <returns>Category list model</returns>
        public virtual CategoryListModel PrepareCategoryListModel(CategorySearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));
            
            //get categories
            var categories = _categoryService.GetAllCategories(categoryName: searchModel.SearchCategoryName,
                showHidden: true,
                storeId: searchModel.SearchStoreId,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare grid model
            var model = new CategoryListModel
            {
                Data = categories.Select(category =>
                {
                    //fill in model values from the entity
                    var categoryModel = category.ToModel();

                    //fill in additional values (not existing in the entity)
                    categoryModel.Breadcrumb = category.GetFormattedBreadCrumb(_categoryService);

                    return categoryModel;
                }),
                Total = categories.TotalCount
            };

            return model;
        }

        /// <summary>
        /// Prepare category model
        /// </summary>
        /// <param name="model">Category model</param>
        /// <param name="category">Category</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Category model</returns>
        public virtual CategoryModel PrepareCategoryModel(CategoryModel model, Category category, bool excludeProperties = false)
        {
            Action<CategoryLocalizedModel, int> localizedModelConfiguration = null;

            if (category != null)
            {
                //fill in model values from the entity
                model = model ?? category.ToModel();

                //prepare nested search model
                PrepareCategoryProductSearchModel(model.CategoryProductSearchModel, category);

                //define localized model configuration action
                localizedModelConfiguration = (locale, languageId) =>
                {
                    locale.Name = category.GetLocalized(entity => entity.Name, languageId, false, false);
                    locale.Description = category.GetLocalized(entity => entity.Description, languageId, false, false);
                    locale.MetaKeywords = category.GetLocalized(entity => entity.MetaKeywords, languageId, false, false);
                    locale.MetaDescription = category.GetLocalized(entity => entity.MetaDescription, languageId, false, false);
                    locale.MetaTitle = category.GetLocalized(entity => entity.MetaTitle, languageId, false, false);
                    locale.SeName = category.GetSeName(languageId, false, false);
                };
            }

            //set default values for the new model
            if (category == null)
            {
                model.PageSize = _catalogSettings.DefaultCategoryPageSize;
                model.PageSizeOptions = _catalogSettings.DefaultCategoryPageSizeOptions;
                model.Published = true;
                model.IncludeInTopMenu = true;
                model.AllowCustomersToSelectPageSize = true;
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = _localizedModelFactory.PrepareLocalizedModels(localizedModelConfiguration);

            //prepare available category templates
            _baseAdminModelFactory.PrepareCategoryTemplates(model.AvailableCategoryTemplates, false);

            //prepare available parent categories
            _baseAdminModelFactory.PrepareCategories(model.AvailableCategories,
                defaultItemText: _localizationService.GetResource("Admin.Catalog.Categories.Fields.Parent.None"));

            //prepare model discounts
            var availableDiscounts = _discountService.GetAllDiscounts(DiscountType.AssignedToCategories, showHidden: true);
            _discountSupportedModelFactory.PrepareModelDiscounts(model, category, availableDiscounts, excludeProperties);

            //prepare model customer roles
            _aclSupportedModelFactory.PrepareModelCustomerRoles(model, category, excludeProperties);

            //prepare model stores
            _storeMappingSupportedModelFactory.PrepareModelStores(model, category, excludeProperties);

            return model;
        }

        /// <summary>
        /// Prepare category product search model
        /// </summary>
        /// <param name="model">Category product search model</param>
        /// <param name="category">Category</param>
        /// <returns>Category product search model</returns>
        public virtual CategoryProductSearchModel PrepareCategoryProductSearchModel(CategoryProductSearchModel model, Category category)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return model;
        }

        /// <summary>
        /// Prepare paged category product list model
        /// </summary>
        /// <param name="searchModel">Category product search model</param>
        /// <param name="category">Category</param>
        /// <returns>Category product list model</returns>
        public virtual CategoryProductListModel PrepareCategoryProductListModel(CategoryProductSearchModel searchModel, Category category)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (category == null)
                throw new ArgumentNullException(nameof(category));

            //get product categories
            var productCategories = _categoryService.GetProductCategoriesByCategoryId(category.Id,
                showHidden: true,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare grid model
            var model = new CategoryProductListModel
            {
                //fill in model values from the entity
                Data = productCategories.Select(productCategory => new CategoryProductModel
                {
                    Id = productCategory.Id,
                    CategoryId = productCategory.CategoryId,
                    ProductId = productCategory.ProductId,
                    ProductName = _productService.GetProductById(productCategory.ProductId)?.Name,
                    IsFeaturedProduct = productCategory.IsFeaturedProduct,
                    DisplayOrder = productCategory.DisplayOrder
                }),
                Total = productCategories.TotalCount
            };

            return model;
        }

        /// <summary>
        /// Prepare product search model to add to the category
        /// </summary>
        /// <param name="model">Product search model to add to the category</param>
        /// <returns>Product search model to add to the category</returns>
        public virtual AddProductToCategorySearchModel PrepareAddProductToCategorySearchModel(AddProductToCategorySearchModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //prepare available categories
            _baseAdminModelFactory.PrepareCategories(model.AvailableCategories);

            //prepare available manufacturers
            _baseAdminModelFactory.PrepareManufacturers(model.AvailableManufacturers);

            //prepare available stores
            _baseAdminModelFactory.PrepareStores(model.AvailableStores);

            //prepare available vendors
            _baseAdminModelFactory.PrepareVendors(model.AvailableVendors);

            //prepare available product types
            _baseAdminModelFactory.PrepareProductTypes(model.AvailableProductTypes);

            return model;
        }

        /// <summary>
        /// Prepare paged product list model to add to the category
        /// </summary>
        /// <param name="searchModel">Product search model to add to the category</param>
        /// <returns>Product list model to add to the category</returns>
        public virtual AddProductToCategoryListModel PrepareAddProductToCategoryListModel(AddProductToCategorySearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));
            
            //get products
            var products = _productService.SearchProducts(showHidden: true,
                categoryIds: new List<int> { searchModel.SearchCategoryId },
                manufacturerId: searchModel.SearchManufacturerId,
                storeId: searchModel.SearchStoreId,
                vendorId: searchModel.SearchVendorId,
                productType: searchModel.SearchProductTypeId > 0 ? (ProductType?)searchModel.SearchProductTypeId : null,
                keywords: searchModel.SearchProductName,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare grid model
            var model = new AddProductToCategoryListModel
            {
                //fill in model values from the entity
                Data = products.Select(product => product.ToModel()),
                Total = products.TotalCount
            };

            return model;
        }

        #endregion
    }
}