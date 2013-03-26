$test_items = Get-ChildItem -Path "testcode"

foreach ($item in $test_items)
{
  $compile_cmd = "MiniJavaCompiler/bin/Debug/MiniJavaCompiler.exe testcode/$($item.Name)"
  $peverify_cmd = '&"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\x64\PEVerify.exe" ./out.exe'
  Write-Host $item.Name
  Write-Host "=================="
  iex $compile_cmd
  if (Test-Path 'out.exe')
  {
    iex $peverify_cmd
    Write-Host "~~~~~~~~~~~~~~~~~~"
    iex './out.exe'
    Remove-Item 'out.exe'
  }
  Write-Host "=================="
}