﻿using System.Windows;
using Prism.Ioc;
using Prism.Modularity;
using SoftwareLicenseManage.Modules.ModuleName;
using SoftwareLicenseManage.Services;
using SoftwareLicenseManage.Services.Interfaces;
using SoftwareLicenseManage.Views;

namespace SoftwareLicenseManage
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMessageService, MessageService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ModuleNameModule>();
        }
    }
}
