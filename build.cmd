
@set msbuild="%ProgramFiles(x86)%\msbuild\15.0\Bin\MSBuild.exe"
@if not exist %msbuild% @set msbuild="%ProgramFiles%\MSBuild\15.0\Bin\MSBuild.exe"

%msbuild%  /m /p:Platform="Any CPU" /v:M  RazorGenerator.Runtime.sln