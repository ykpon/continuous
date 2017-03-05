﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.ServiceProcess;

namespace Rescuer.Management.WindowsService.Shell
{
    public class WindowsServiceShell : IWindowsServiceShell
    {
        private readonly TimeSpan _timeout;

        private ServiceController _service;
        private readonly ScriptExecutor _executor;
        private readonly ScriptPathProvider _scriptsPath;

      
        public WindowsServiceShell()
        {
            ErrorLog = new List<string>();
            _timeout = TimeSpan.FromSeconds(5);
            _executor = new ScriptExecutor();

            _scriptsPath = new ScriptPathProvider();
        }

        public List<string> ErrorLog { get; set; }

        public ServiceControllerStatus GetServiceStatus()
        {
            ThrowExceptionIfNotConnectedToService();

            _service.Refresh();

            return _service.Status;
        }

        public void ConnectToService(string serviceName)
        {
            _service = ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName == serviceName);

           ThrowExceptionIfNotConnectedToService();
        }

        public void InstallService(string serviceName, string fullServicePath)
        {
            var parameters = new List<CommandParameter>
            {
                new CommandParameter(nameof(serviceName), serviceName),
                new CommandParameter(nameof(fullServicePath), fullServicePath)
            };

            _executor.Execute(_scriptsPath.InstallService, parameters);
        }

        public void UninstallService(string serviceName)
        { 
            var parameters = new List<CommandParameter>
            {
                new CommandParameter(nameof(serviceName), serviceName)
            };
            
            _executor.Execute(_scriptsPath.UninstallService, parameters);   
        }


        public void ClearErrorLog()
        {
            ErrorLog = new List<string>();
        }

        public bool StopService()
        {
            ThrowExceptionIfNotConnectedToService();

            if (!_service.CanStop)
            {
                ErrorLog.Add("service can't be stopped after start");
                return false;
            }

            _service.Stop();

            _service.WaitForStatus(ServiceControllerStatus.Stopped, _timeout);

            return true;
        }

        public bool StartService()
        {
            ThrowExceptionIfNotConnectedToService();

            if (_service.Status == ServiceControllerStatus.Running)
                return false;

            _service.Start();

            _service.WaitForStatus(ServiceControllerStatus.Running, _timeout);

            return true;
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        private void ThrowExceptionIfNotConnectedToService()
        {
            if (_service == null)
                throw new InvalidOperationException("Service is not connected");
        }
    }
}