using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SoftwareLicenseManage.Core;
using SoftwareLicenseManage.Modules.ModuleName.Views;

namespace SoftwareLicenseManage.Modules.ModuleName
{
    public class ModuleNameModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public ModuleNameModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "ViewA");
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ViewA>();
        }
    }
}