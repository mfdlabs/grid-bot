@echo OFF

powershell -ExecutionPolicy Unrestricted ".\Deploy.ps1" -root "%CD%/" -config Release