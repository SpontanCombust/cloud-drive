using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDrive.App.Services
{
    public interface IAutoSyncService
    {
        void StartSync();
        void StopSync();
    }
}
