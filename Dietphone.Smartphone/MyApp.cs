﻿using System;
using Dietphone.BinarySerializers;
using Dietphone.Models;

namespace Dietphone
{
    public class MyApp
    {
        public static BinaryStreamProvider StreamProvider { private get; set; }
        private static Factories factories = null;
        private static readonly object factoriesLock = new object();

        public static Factories Factories
        {
            get
            {
                lock (factoriesLock)
                {
                    if (factories == null)
                    {
                        CreateFactories();
                    }
                    return factories;
                }
            }
            set
            {
                if (value == null)
                {
                    throw new NullReferenceException("Factories");
                }
                factories = value;
            }
        }

        public static string CurrentUiCulture
        {
            get
            {
                var settings = Factories.Settings;
                return settings.CurrentUiCulture;
            }
        }

        private static void CreateFactories()
        {
            var storageCreator = new BinaryStorageCreator(StreamProvider);
            factories = new FactoriesImpl();
            factories.StorageCreator = storageCreator;
            storageCreator.CultureName = CurrentProductCulture;
        }

        private static string CurrentProductCulture
        {
            get
            {
                var settings = Factories.Settings;
                return settings.CurrentProductCulture;
            }
        }
    }
}