@echo off
echo =======================================
echo 启动原生透明窗口测试
echo =======================================
echo 说明:
echo - 窗口中的绿色区域应该完全透明
echo - 透明区域应该能看到桌面内容  
echo - 蓝色圆形和文字应该不透明
echo - 按空格键切换透明/不透明
echo - 按C键切换透明色（绿/蓝/红）
echo - 按ESC键退出
echo =======================================
echo.

cd /d "d:\Projects\bestHuman\NativeWindowTest"
dotnet run
pause
