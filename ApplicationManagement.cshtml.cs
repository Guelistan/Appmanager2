using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppManager.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using Microsoft.Web.Administration; // Für IIS-Verwaltung
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
namespace AppManager.Pages.Admin
{
    public class ApplicationManagementModel : PageModel
    {
        public List<AppManager.Models.Application> Applications { get; set; } = new();
        public AppManager.Models.Application NewApplication { get; set; } = new();
        public List<float> CpuLoads { get; set; } = new();
        public List<string> AppPoolNames { get; set; } = new();

        public void OnGet()
        {
            Applications = GetIISApplications();
            LoadCpuData();
        }

        // Korrigiere die Vergleiche von Guid und int zu Guid und Guid
        public IActionResult OnPostStart(Guid applicationId)
        {
            var app = Applications.FirstOrDefault(a => a.Id == applicationId);
            if (app != null && app.IsIISApplication)
            {
                using var server = new ServerManager();
                var pool = server.ApplicationPools[app.IISAppPoolName];
                pool.Start();
            }
            TempData["SuccessMessage"] = "Anwendung gestartet.";
            return RedirectToPage();
        }

        public IActionResult OnPostStop(Guid applicationId)
        {
            var app = Applications.FirstOrDefault(a => a.Id == applicationId);
            if (app != null && app.IsIISApplication)
            {
                using var server = new ServerManager();
                var pool = server.ApplicationPools[app.IISAppPoolName];
                pool.Stop();
            }
            TempData["SuccessMessage"] = "Anwendung gestoppt.";
            return RedirectToPage();
        }

        public IActionResult OnPostRestart(Guid applicationId)
        {
            var app = Applications.FirstOrDefault(a => a.Id == applicationId);
            if (app != null && app.IsIISApplication)
            {
                using var server = new ServerManager();
                var pool = server.ApplicationPools[app.IISAppPoolName];
                pool.Recycle();
            }
            TempData["SuccessMessage"] = "Anwendung neugestartet.";
            return RedirectToPage();
        }

        public IActionResult OnPostRecycle(Guid applicationId)
        {
            var app = Applications.FirstOrDefault(a => a.Id == applicationId);
            if (app != null && app.IsIISApplication)
            {
                using var server = new ServerManager();
                var pool = server.ApplicationPools[app.IISAppPoolName];
                pool.Recycle();
            }
            TempData["SuccessMessage"] = "AppPool recycelt.";
            return RedirectToPage();
        }

        private static List<AppManager.Models.Application> GetIISApplications()
        {
            var result = new List<AppManager.Models.Application>();
            try
            {
                using var server = new ServerManager();
                foreach (var site in server.Sites)
                {
                    foreach (var app in site.Applications)
                    {
                        bool isStarted = false;
                        try
                        {
                            isStarted = site.State == ObjectState.Started;
                        }
                        catch (NotImplementedException)
                        {
                            // Fallback: Status nicht verfügbar
                            isStarted = false;
                        }

                        result.Add(new AppManager.Models.Application
                        {
                            Id = Guid.NewGuid(),
                            Name = app.Path,
                            IsIISApplication = true,
                            IISAppPoolName = app.ApplicationPoolName,
                            IsStarted = isStarted,
                            LastLaunchTime = DateTime.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der IIS-Anwendungen: {ex}");
            }
            return result;
        }

        private void LoadCpuData()
        {
            using var server = new ServerManager();
            CpuLoads.Clear();
            AppPoolNames.Clear();
            foreach (var pool in server.ApplicationPools)
            {
                AppPoolNames.Add(pool.Name);
                float cpu = GetCpuUsageForAppPool(pool.Name);
                CpuLoads.Add(cpu);
            }
        }

        private float GetCpuUsageForAppPool(string appPoolName)
        {
#if WINDOWS
            // Beispiel: PerformanceCounter für IIS AppPool CPU-Last
            try
            {
                using var cpuCounter = new PerformanceCounter("Process", "% Processor Time", appPoolName, true);
                return cpuCounter.NextValue();
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return 0;
            }
#else
            // Nicht unterstützt auf anderen Plattformen
            return 0;
#endif
        }
    }
}
