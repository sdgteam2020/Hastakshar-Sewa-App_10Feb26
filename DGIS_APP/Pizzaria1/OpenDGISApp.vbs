Set objShell = CreateObject("WScript.Shell")
Set objFSO = CreateObject("Scripting.FileSystemObject")

' Define the path to the file
filePath = objShell.ExpandEnvironmentStrings("%USERPROFILE%\Desktop\DGIS App.appref-ms")

' Check if the file exists
If objFSO.FileExists(filePath) Then
    ' Wait for 5 seconds
    WScript.Sleep 5000

    ' Start the file without displaying a window
    objShell.Run Chr(34) & filePath & Chr(34), 0
Else
    WScript.Echo "Error: file not found."
End If