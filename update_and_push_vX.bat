@echo off
title MAORUNNER - Quick Version Push
echo ============================================
echo   🚀 MAORUNNER - Fast Update & Tag Push
echo ============================================

set /p version="Введите новую версию (например v0.5): "

REM === Добавляем и коммитим изменения
echo [1/3] Добавляю и коммичу файлы...
git add .
git commit -m "Update version %version% ready"

REM === Отправляем изменения
echo [2/3] Отправляю изменения на GitHub...
git push

REM === Создаём и пушим тег
echo [3/3] Создаю тег %version%...
git tag %version%
git push origin %version%

echo ============================================
echo ✅ Версия %version% успешно загружена на GitHub!
echo ============================================
pause
