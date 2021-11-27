﻿using System;
using MFDLabs.Abstractions;

namespace MFDLabs.Grid.Bot.Utility
{
    internal sealed class GridDeployerUtility : SingletonBase<GridDeployerUtility>
    {
        public enum GridDeployerExitCodes
        {
            BadPort = -1,
            NoWebServerFound = -2,
            WebServerMaxLaunchAttemptsExceeded = -3,
            NoGridServerFound = -4
        }

        public void CheckHResult(int hResult)
        {
            if (hResult > 1) return;

            var rr = (GridDeployerExitCodes)hResult;

            switch (rr)
            {
                case GridDeployerExitCodes.BadPort:
                    throw new ApplicationException("The port specified when attempting to launch a grid server via the grid deployer was invalid or not specified.");
                case GridDeployerExitCodes.NoWebServerFound:
                    throw new ApplicationException("The grid deployer tried to launch the web server, but there was no web server at the specified path.");
                case GridDeployerExitCodes.WebServerMaxLaunchAttemptsExceeded:
                    throw new ApplicationException("The grid deployer exceeded it's maximum attempts when trying to launch the web server.");
                case GridDeployerExitCodes.NoGridServerFound:
                    throw new ApplicationException("The grid deployer tried to launch a grid server but it couldn't find the grid server's path in Win32 registry.");
                default: break;
            }
        }
    }
}
