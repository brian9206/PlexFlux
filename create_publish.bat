@echo off
del /f /s /q bin\Publish
mkdir bin\Publish
copy /y bin\Release\*.* bin\Publish

del /f /s /q bin\Publish\*.pdb
del /f /s /q bin\Publish\GongSolutions.Wpf.DragDrop.xml
del /f /s /q bin\Publish\NAudio.xml
del /f /s /q bin\Publish\Octokit.xml