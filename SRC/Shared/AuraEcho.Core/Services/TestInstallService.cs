using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuraEcho.Core.Services
{
    public class TestInstallService : IPluginInstallService
    {
        TestInstallService(ILocalPluginRepository rp)
        {

        }
        public Task<LocalPluginModel> InstallAsync(string filePath)
        {
            throw new NotImplementedException();
        }
    }
}
