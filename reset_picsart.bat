@echo off
chcp 65001 >nul
title Tool Fix Picsart Dinh Pro - Multi-Device ADB
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0reset_picsart.ps1" %*
if %ERRORLEVEL% NEQ 0 pause
