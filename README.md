# RazorGenerator.MsBuild

RazorGenerator.MsBuild minimal build clone.   
For .cshtml files to compile on develpment machine, not on release web server.

```
  <Import Project="\RazorGenerator.MsBuild\RazorGenerator.MsBuild.targets" />
  <ItemGroup>
    <RazorSrcFiles Include="Views\*.cshtml" />
    <RazorSrcFiles Include="Views\Home\*.cshtml" />

```

Orinal source:
https://github.com/RazorGenerator/RazorGenerator
