@echo off
setlocal
set "GAME_DIAG_ARGS=%*"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\game-diag\game-diag.ps1"
exit /b %ERRORLEVEL%
